using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;

public class CustomRaycastHits
{
    public Collider Collider { get; set; }
    public Vector3 Point { get; set; }
    public Vector3 Normal { get; set; }
    public float Distance { get; set; }
}

[RequireComponent(typeof(Rigidbody))]
public class WallMovement : MonoBehaviour
{
    public static WallMovement Instance;
    private Transform _mainCamera;
    private Rigidbody _rigidbody;
    private Vector3 _inputMovement;
    
    private bool _previousFacingUp;
    private float _groundCheckDistance = 0.5f;
    protected static RaycastHit[] hitCache = new RaycastHit[25];

    public Transform closestPointVisual;
    
    [Tooltip("Threshold angle for deciding between up and down")]
    [SerializeField]
    [Range(0, 90)]
    public float upFlipThreshold = 80.0f;

    [Tooltip("How fast the player will go")]
    [SerializeField]
    private float movementSpeed = 4f;

    private int _environmentLayer;
    
    private bool _isGrounded;
    private bool IsGrounded
    {
        get => _isGrounded;
        set
        {
            _isGrounded = value;
            OnGroundedChanged?.Invoke(_isGrounded);
        }
    }
    
    private Vector3 _relativeUp;
    private Vector3 RelativeUp
    {
        get => _relativeUp;
        set
        {
            _relativeUp = value;
            OnRelativeUpChanged?.Invoke(_relativeUp);
        }
    }

    private bool _facingUp;
    private bool FacingUp
    {
        get => _facingUp;
        set
        {
            _facingUp = value;
            OnFacingUpChange?.Invoke(_facingUp);
        }
    }
    
    // Actions
    public static event Action<Vector3> OnRelativeUpChanged;
    public static event Action<bool> OnGroundedChanged;
    public static event Action<float> OnSpeedChange;
    public static event Action<bool> OnFacingUpChange;
    public static event Action<bool> OnYawInvertedChange;
    public static event Action<float> OnYawChange;
    
    private void Awake()
    {
        if (Instance != this && Instance != null)
        {
            Destroy(gameObject);
        }

        Instance = this;
            
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.useGravity = false;
    }

    void Start()
    {
        _mainCamera = Camera.main?.transform;
        RelativeUp = transform.up;
        _environmentLayer = LayerMask.GetMask("Environment");
    }

    private void FixedUpdate()
    {
        GetGroundedState();
        _rigidbody.linearVelocity = GetDesiredMovement() * movementSpeed;
        var rotation = Quaternion.FromToRotation(Vector3.up, RelativeUp);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, 10 * Time.deltaTime);
        
        OnSpeedChange?.Invoke(_rigidbody.linearVelocity.magnitude);
    }
    
    private void Update()
    {
        GetInputMovement();
    }

    private Vector3 GetDesiredMovement()
    {
        var planeNormal = RelativeUp;
        
        var yaw = _mainCamera.eulerAngles.y;

        var dot = Vector3.Dot(Vector3.Project(planeNormal, Vector3.up), Vector3.up);
        // TODO: It maybe because if if the model is past 0 in dot, we are still facing up. We might have to be tight on that by removing that approx 
        FacingUp = dot > 0 || Mathf.Approximately(dot, 0);

        if (_previousFacingUp != FacingUp)
        {
            var angle = Vector3.Angle(planeNormal, _previousFacingUp ? Vector3.up : Vector3.down);
            if (Mathf.Abs(angle % 90) <= upFlipThreshold && !Mathf.Approximately(angle, 180))
            {
                FacingUp = _previousFacingUp;
            }
        }

        _previousFacingUp = FacingUp;
        var worldUp = FacingUp ? Vector3.up : Vector3.down;

        if (Mathf.Approximately(dot, -1))
        {
            yaw *= -1;
            OnYawInvertedChange?.Invoke(true);
        }
        else
        {
            OnYawInvertedChange?.Invoke(false);
        }
        
        OnYawChange?.Invoke(yaw);

        // It seems like we are always rotating from the world up. It's assuming that to be the default
        var planeRotation = Quaternion.FromToRotation(worldUp, planeNormal);
        var playerRotation = Quaternion.AngleAxis(yaw, worldUp);
        var movementForward = planeRotation * (playerRotation * Vector3.forward);

        Debug.DrawRay(transform.position, movementForward, Color.blue);
        //Debug.DrawRay(transform.position, worldUp, Color.red);

        var movementRotation = Quaternion.LookRotation(movementForward, planeNormal);
        var axisMovement = movementRotation * _inputMovement;

        return axisMovement * movementSpeed;
    }

    private void GetInputMovement()
    {
        var inputX = Input.GetAxis("Horizontal") * (FacingUp ? 1f : -1f);
        var inputY = Input.GetAxis("Vertical");
        _inputMovement = new Vector3(inputX, 0, inputY);
    }

    private void GetGroundedState()
    {
        var closest = new CustomRaycastHits
        {
            Distance = Mathf.Infinity
        };

        var hitSomething = false;
        var hits = GetOverlappingHits();
        foreach (var hit in hits)
        {
            if (hit.Distance < closest.Distance)
            {
                closest = hit;
            }
            hitSomething = true;
        }

        if (!hitSomething) return;

        IsGrounded = true;
        
        RelativeUp = closest.Normal;
        transform.position = closest.Point + (RelativeUp.normalized * 0.53f);
        closestPointVisual.position = closest.Point;
    }
    
    private List<CustomRaycastHits> GetOverlappingHits()
    {
        var overlappingColliders = new Collider[10];
        var hitCount = Physics.OverlapSphereNonAlloc(
            transform.position, 
            0.6f, 
            overlappingColliders, 
            _environmentLayer);

        var rayCasts = new List<CustomRaycastHits>();

        for (var i = 0; i < hitCount; i++)
        {
            var rayCast = new CustomRaycastHits
            {
                Collider = overlappingColliders[i]
            };

            rayCast.Point = rayCast.Collider.ClosestPoint(transform.position);
            rayCast.Normal = (transform.position - rayCast.Point).normalized;
            rayCast.Distance = (transform.position - (rayCast.Point + rayCast.Normal * 0.5f)).magnitude;
            
            rayCasts.Add(rayCast);
        }
        
        return rayCasts;
    }
}
