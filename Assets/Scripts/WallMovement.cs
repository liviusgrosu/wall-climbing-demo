using NUnit;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
public class WallMovement : MonoBehaviour
{
    private Transform _mainCamera;
    private Rigidbody _rigidbody;
    private Vector3 _inputMovement;
    private bool _isGrounded;
    private Vector3 _relativeUp;
    private bool _previousFacingUp;
    private const string EnvironmentTag = "Environment";

    [Tooltip("Threshold angle for deciding between up and down")]
    [SerializeField]
    [Range(0, 90)]
    public float upFlipThreshold = 80.0f;

    [Tooltip("How fast the player will go")]
    [SerializeField]
    private float movementSpeed = 4f;
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.useGravity = false;
    }

    void Start()
    {
        _mainCamera = Camera.main?.transform;
        _relativeUp = transform.up;
    }

    private void FixedUpdate()
    {
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
        
        Debug.DrawRay(transform.position, planeNormal, Color.green, 0);
        Debug.DrawRay(transform.position, movementForward, Color.blue, 0);

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

    private void OnCollisionStay(Collision other)
    {
        if (!other.transform.CompareTag(EnvironmentTag)) return;
        _isGrounded = true;
        _relativeUp = other.GetContact(0).normal;
        Debug.DrawRay(transform.position, _relativeUp, Color.magenta);
    }

    /*private void OnCollisionExit(Collision other)
    {
        if (!other.transform.CompareTag(EnvironmentTag)) return;
        Debug.Log("Exiting for some reason?");
        _isGrounded = false;
        _relativeUp = Vector3.up;
    }*/
}
