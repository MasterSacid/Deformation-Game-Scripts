using UnityEngine;

public class AdvancedPhysicsMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float friction = 10f;
    [SerializeField] private float rotationSpeed = 15f;

    [Header("Feel Settings")]
    [SerializeField] private AnimationCurve accelerationCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] private bool useMouseAiming = true;

    private Rigidbody rb;
    private Camera mainCamera;
    private Vector3 movementInput;
    private Vector3 currentVelocity;

    // --- Animator References ---
    private Animator animator;
    // --- Animator Parameter Hashes (for efficiency) ---
    private int speedParameterHash;
    private int runDirectionMultiplierParameterHash; // New hash for direction

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found on this GameObject.", this);
            enabled = false;
            return;
        }

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.useGravity = true;
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found. Make sure your camera is tagged 'MainCamera'.", this);
        }

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Animator component not found on this GameObject. Animations will not play.", this);
        }
        else
        {
            speedParameterHash = Animator.StringToHash("Speed");
            // --- Store the hash for the new parameter ---
            runDirectionMultiplierParameterHash = Animator.StringToHash("RunDirectionMultiplier");
        }
    }

    void Update()
    {
        // Get input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        movementInput = new Vector3(horizontal, 0f, vertical).normalized;

        // Handle rotation
        if (useMouseAiming && mainCamera != null)
        {
            HandleMouseRotation();
        }
        else if (movementInput != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movementInput);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                rotationSpeed * Time.deltaTime);
        }

        // Update Animator
        if (animator != null)
        {
            float horizontalSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
            animator.SetFloat(speedParameterHash, horizontalSpeed);

            // --- Determine Run Direction Multiplier ---
            if (horizontalSpeed > 0.1f) // Only if moving significantly
            {
                Vector3 characterForward = transform.forward;
                // Use the Rigidbody's actual velocity direction on the XZ plane
                Vector3 actualMoveDirection = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

                if (actualMoveDirection.sqrMagnitude > 0.01f) // Ensure it's not a zero vector before normalizing
                {
                    actualMoveDirection.Normalize();
                    float dotProduct = Vector3.Dot(characterForward, actualMoveDirection);

                    // If dotProduct is negative, character is moving backward relative to its facing direction
                    // Using a threshold like -0.3f to define "mostly backward"
                    if (dotProduct < -0.3f)
                    {
                        animator.SetFloat(runDirectionMultiplierParameterHash, -1.0f); // Play animation in reverse
                    }
                    else
                    {
                        animator.SetFloat(runDirectionMultiplierParameterHash, 1.0f);  // Play animation forward (for forward or strafing)
                    }
                }
                else
                {
                     // Velocity very low, default to forward playback
                    animator.SetFloat(runDirectionMultiplierParameterHash, 1.0f);
                }
            }
            else
            {
                // Not running or speed is very low, set multiplier to 1 (forward)
                // This ensures that when transitioning out of run, it's set to a normal state
                animator.SetFloat(runDirectionMultiplierParameterHash, 1.0f);
            }
        }
    }

    void HandleMouseRotation()
    {
        if (mainCamera == null) return;
    

        Plane playerPlane = new Plane(Vector3.up, transform.position);
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (playerPlane.Raycast(ray, out float hitDistance))
        {
            Vector3 targetPoint = ray.GetPoint(hitDistance);
            Vector3 direction = targetPoint - transform.position;
            direction.y = 0f;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                    rotationSpeed * Time.deltaTime);
            }
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        Vector3 targetVelocity = movementInput * maxSpeed;
        float inputMagnitude = movementInput.magnitude;
        float curveValue = accelerationCurve.Evaluate(inputMagnitude);
        targetVelocity *= curveValue;

        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity,
            acceleration * Time.fixedDeltaTime);

        if (movementInput.magnitude < 0.1f)
        {
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero,
                friction * Time.fixedDeltaTime);
        }

        rb.linearVelocity = new Vector3(currentVelocity.x, rb.linearVelocity.y, currentVelocity.z);
    }
}