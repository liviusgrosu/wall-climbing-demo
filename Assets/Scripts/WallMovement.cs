using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WallMovement : MonoBehaviour
{
    private Transform _mainCamera;
    private Rigidbody _rigidbody;
    private Vector3 _inputMovement;
    private bool _isGrounded;
    private Vector3 _relativeUp;
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
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.useGravity = false;
    }

    void Start()
    {
        _mainCamera = Camera.main?.transform;
        _relativeUp = transform.up;
        _environmentLayer = LayerMask.GetMask("Environment");
    }

    private void FixedUpdate()
    {
        GetGroundedState();
        _rigidbody.linearVelocity = GetDesiredMovement() * movementSpeed;
        var rotation = Quaternion.FromToRotation(Vector3.up, _relativeUp);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, 10 * Time.deltaTime);
    }
    
    private void Update()
    {
        GetInputMovement();
        //Debug.DrawRay(transform.position, _relativeUp, Color.magenta);
    }

    private Vector3 GetDesiredMovement()
    {
        var planeNormal = transform.up;
        
        var yaw = _mainCamera.eulerAngles.y;

        var dot = Vector3.Dot(Vector3.Project(planeNormal, Vector3.up), Vector3.up);
        var facingUp = dot > 0 || Mathf.Approximately(dot, 0);
        if (_previousFacingUp != facingUp)
        {
            var angle = Vector3.Angle(planeNormal, _previousFacingUp ? Vector3.up : Vector3.down);
            if (Mathf.Abs(angle % 90) <= upFlipThreshold && !Mathf.Approximately(angle, 180))
            {
                // This is still considered facing any ways. This is just for edge cases
                facingUp = _previousFacingUp;
            }
        }

        _previousFacingUp = facingUp;
        var worldUp = facingUp ? Vector3.up : Vector3.down;

        if (Mathf.Approximately(dot, -1))
        {
            yaw *= -1;
        }

        // It seems like we are always rotating from the world up. Its assuming that to be the default
        var planeRotation = Quaternion.FromToRotation(worldUp, planeNormal);
        var playerRotation = Quaternion.AngleAxis(yaw, worldUp);
        var movementForward = planeRotation * (playerRotation * Vector3.forward);

        var movementRotation = Quaternion.LookRotation(movementForward, planeNormal);
        var axisMovement = movementRotation * _inputMovement;

        return axisMovement * movementSpeed;
    }

    private void GetInputMovement()
    {
        var inputX = Input.GetAxis("Horizontal");
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

        _isGrounded = true;
        _relativeUp = closest.normal;
        var directionToGround = closest.distance * -_relativeUp;
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
            -_relativeUp, 
            hitCache, 
            _groundCheckDistance,
            _environmentLayer);

        return Enumerable.Range(0, hitCount).Select(i => hitCache[i])
            .Where(hit => hit.transform != transform);
    }
    

    /*private void OnCollisionStay(Collision other)
    {
        if (!other.transform.CompareTag(EnvironmentTag)) return;
        _isGrounded = true;
        _relativeUp = other.GetContact(0).normal;
        Debug.DrawRay(transform.position, _relativeUp, Color.magenta);
    }*/

    /*private void OnCollisionExit(Collision other)
    {
        if (!other.transform.CompareTag(EnvironmentTag)) return;
        _isGrounded = false;
    }*/
}
