using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        //Debug.DrawRay(transform.position, RelativeUp, Color.magenta);
    }

    private Vector3 GetDesiredMovement()
    {
        // 1. Default 
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
                // This is still considered facing any ways. This is just for edge cases
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

        // It seems like we are always rotating from the world up. It's assuming that to be the default
        var planeRotation = Quaternion.FromToRotation(worldUp, planeNormal);
        var playerRotation = Quaternion.AngleAxis(yaw, worldUp);
        var movementForward = planeRotation * (playerRotation * Vector3.forward);

        Debug.DrawRay(transform.position, movementForward, Color.blue);
        Debug.DrawRay(transform.position, worldUp, Color.red);

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
        var closest = new RaycastHit() { distance = Mathf.Infinity };
        var hitSomething = false;
        var hits = GetHits();
        foreach (var hit in hits)
        {
            if (hit.distance < closest.distance)
            {
                closest = hit;
            }
            hitSomething = true;
        }

        if (!hitSomething) return;

        IsGrounded = true;
        
        RelativeUp = closest.normal;
        Debug.DrawRay(transform.position, RelativeUp, Color.yellow);
        
        var directionToGround = closest.distance * -RelativeUp;
        //Debug.Log($"distance: {closest.distance - 0.25f}");
        
        // Snap player down
        transform.position += Vector3.ClampMagnitude(directionToGround, Mathf.Infinity * Time.fixedDeltaTime);
        closestPointVisual.position = closest.point;
    }

    private IEnumerable<RaycastHit> GetHits()
    {
        hitCache = new RaycastHit[25];
        var hitCount = Physics.SphereCastNonAlloc(
            transform.position, 
            0.45f, 
            -RelativeUp, 
            hitCache, 
            _groundCheckDistance,
            _environmentLayer);

        return Enumerable.Range(0, hitCount).Select(i => hitCache[i])
            .Where(hit => hit.transform != transform);
    }
    
    /*private void OnCollisionStay(Collision other)
    {
        if (!other.transform.CompareTag(EnvironmentTag)) return;
        IsGrounded = true;
        RelativeUp = other.GetContact(0).normal;
        Debug.DrawRay(transform.position, RelativeUp, Color.magenta);
    }*/

    /*private void OnCollisionExit(Collision other)
    {
        if (!other.transform.CompareTag(EnvironmentTag)) return;
        IsGrounded = false;
    }*/
}
