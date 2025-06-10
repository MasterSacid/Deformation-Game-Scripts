using UnityEngine;

public class OrbitCameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The object to orbit around. If null, will orbit around world origin.")]
    public Transform target;

    [Header("Distance Settings")]
    [Tooltip("Current distance from the target")]
    public float distance = 10f;
    [Tooltip("Minimum zoom distance")]
    public float minDistance = 2f;
    [Tooltip("Maximum zoom distance")]
    public float maxDistance = 30f;
    [Tooltip("How fast the camera zooms in/out")]
    public float zoomSpeed = 5f;
    [Tooltip("Smoothing factor for zoom (0 = instant, 1 = very smooth)")]
    public float zoomSmoothness = 0.1f;

    [Header("Rotation Settings")]
    [Tooltip("How fast the camera rotates with WASD")]
    public float rotationSpeed = 100f;
    [Tooltip("Smoothing factor for rotation (0 = instant, 1 = very smooth)")]
    public float rotationSmoothness = 0.1f;

    [Header("Vertical Angle Limits")]
    [Tooltip("Maximum upward angle (prevents camera flipping)")]
    public float maxVerticalAngle = 80f;
    [Tooltip("Maximum downward angle")]
    public float minVerticalAngle = -80f;

    [Header("Camera Behavior")]
    [Tooltip("If true, camera will auto-rotate slowly")]
    public bool autoRotate = false;
    [Tooltip("Speed of auto-rotation")]
    public float autoRotateSpeed = 10f;

    [Header("Initial Position")]
    [Tooltip("Starting horizontal angle (degrees)")]
    public float initialHorizontalAngle = 45f;
    [Tooltip("Starting vertical angle (degrees)")]
    public float initialVerticalAngle = 30f;

    // Internal variables for smooth movement
    private float currentHorizontalAngle;
    private float currentVerticalAngle;
    private float targetHorizontalAngle;
    private float targetVerticalAngle;
    private float currentDistance;
    private float targetDistance;

    // Cache the camera component
    private Camera cam;

    void Start()
    {
        // Get camera component
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("OrbitCameraController: No Camera component found on this GameObject. OrbitCameraController will be disabled.", this.gameObject);
            enabled = false; // Disable the script's Update loop if no camera
            return; // Stop further initialization
        }

        // Initialize angles with the initial values
        currentHorizontalAngle = targetHorizontalAngle = initialHorizontalAngle;
        currentVerticalAngle = targetVerticalAngle = initialVerticalAngle;
        currentDistance = targetDistance = distance;

        // Position camera at initial location
        UpdateCameraPosition();
    }

    void Update()
    {
        // If enabled is false (e.g. no camera found in Start), don't run Update logic
        if (!enabled) return;

        // Handle input
        HandleKeyboardInput();
        HandleMouseInput();
        HandleAutoRotation();

        // Smooth interpolation for rotation
        currentHorizontalAngle = Mathf.LerpAngle(currentHorizontalAngle, targetHorizontalAngle,
            1f - Mathf.Pow(rotationSmoothness, Time.deltaTime * 60f));
        currentVerticalAngle = Mathf.LerpAngle(currentVerticalAngle, targetVerticalAngle,
            1f - Mathf.Pow(rotationSmoothness, Time.deltaTime * 60f));

        // Smooth interpolation for zoom
        currentDistance = Mathf.Lerp(currentDistance, targetDistance,
            1f - Mathf.Pow(zoomSmoothness, Time.deltaTime * 60f));

        // Update camera position based on current values
        UpdateCameraPosition();
    }

    void HandleKeyboardInput()
    {
        float horizontalInput = 0f;
        float verticalInput = 0f;

        if (Input.GetKey(KeyCode.A)) horizontalInput = -1f;
        else if (Input.GetKey(KeyCode.D)) horizontalInput = 1f;

        if (Input.GetKey(KeyCode.W)) verticalInput = 1f;
        else if (Input.GetKey(KeyCode.S)) verticalInput = -1f;

        if (horizontalInput != 0f)
        {
            targetHorizontalAngle += horizontalInput * rotationSpeed * Time.deltaTime;
            targetHorizontalAngle %= 360f;
        }

        if (verticalInput != 0f)
        {
            targetVerticalAngle += verticalInput * rotationSpeed * Time.deltaTime;
            targetVerticalAngle = Mathf.Clamp(targetVerticalAngle, minVerticalAngle, maxVerticalAngle);
        }
    }

    void HandleMouseInput()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0f)
        {
            targetDistance -= scrollInput * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }
    }

    void HandleAutoRotation()
    {
        if (autoRotate)
        {
            if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
            {
                targetHorizontalAngle += autoRotateSpeed * Time.deltaTime;
                targetHorizontalAngle %= 360f;
            }
        }
    }

    void UpdateCameraPosition()
    {
        // This check is vital if UpdateCameraPosition could be called before Start or if cam could become null
        if (cam == null) return;

        float horizontalRad = currentHorizontalAngle * Mathf.Deg2Rad;
        float verticalRad = currentVerticalAngle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            currentDistance * Mathf.Cos(verticalRad) * Mathf.Sin(horizontalRad),
            currentDistance * Mathf.Sin(verticalRad),
            currentDistance * Mathf.Cos(verticalRad) * Mathf.Cos(horizontalRad)
        );

        Vector3 targetPosition = target != null ? target.position : Vector3.zero;
        transform.position = targetPosition + offset;
        transform.LookAt(targetPosition);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        // No need to call UpdateCameraPosition directly if Update loop is running
        // However, if you want an immediate snap, you can call it.
        // UpdateCameraPosition();
    }

    public void SetDistance(float newDistance)
    {
        targetDistance = Mathf.Clamp(newDistance, minDistance, maxDistance);
    }

    public void SetAngles(float horizontal, float vertical)
    {
        targetHorizontalAngle = horizontal;
        targetVerticalAngle = Mathf.Clamp(vertical, minVerticalAngle, maxVerticalAngle);
    }

    public void ResetToInitialPosition()
    {
        targetHorizontalAngle = initialHorizontalAngle;
        targetVerticalAngle = initialVerticalAngle;
        targetDistance = distance; // Use the public 'distance' as the base for reset
    }

    public void FocusOnObject(GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("FocusOnObject: obj is null.", this.gameObject);
            return;
        }

        // CRITICAL: Ensure 'cam' is initialized.
        if (cam == null)
        {
            // Try to re-acquire if Start might not have run or cam was lost
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                Debug.LogError("FocusOnObject: Camera reference (cam) is critically null. OrbitCameraController might not be on a Camera GameObject or Start() failed. Aborting focus.", this.gameObject);
                return; // Cannot proceed without a camera
            }
            Debug.LogWarning("FocusOnObject: Camera reference was null but re-acquired. Review setup for potential issues.", this.gameObject);
        }

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            if (bounds.size == Vector3.zero) {
                Debug.LogWarning($"FocusOnObject: Object '{obj.name}' has zero size bounds. Targeting object and using current/default distance.", this.gameObject);
                SetTarget(obj.transform);
                SetDistance(this.distance); // Use the public 'distance' field as a fallback
                return;
            }

            float maxExtent = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
             if (maxExtent <= 0f) {
                Debug.LogWarning($"FocusOnObject: Object '{obj.name}' has non-positive maxExtent ({maxExtent}) from bounds. Targeting object and using current/default distance.", this.gameObject);
                SetTarget(obj.transform);
                SetDistance(this.distance);
                return;
            }

            float fov = cam.fieldOfView * Mathf.Deg2Rad;
            float tanFovDiv2 = Mathf.Tan(fov / 2f);

            if (Mathf.Approximately(tanFovDiv2, 0f)) {
                Debug.LogWarning($"FocusOnObject: Camera FOV ({cam.fieldOfView} deg) results in tan(fov/2) being zero. Cannot calculate optimal distance. Targeting object and using current/default distance.", this.gameObject);
                SetTarget(obj.transform);
                SetDistance(this.distance);
                return;
            }

            float calculatedDistance = (maxExtent / 2f) / tanFovDiv2;
            calculatedDistance *= 1.5f; // Add some padding

            SetTarget(obj.transform);
            SetDistance(calculatedDistance);
        }
        else
        {
            Debug.LogWarning($"FocusOnObject: Object '{obj.name}' does not have a Renderer component. Cannot determine bounds for focusing. Targeting object at current distance.", this.gameObject);
            SetTarget(obj.transform);
            SetDistance(this.distance); // Fallback to current or default distance
        }
    }

    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(target.position, 0.5f);

            // Simplified horizontal orbit circle Gizmo
            // Ensure cam is not null for this Gizmo either, if it depends on cam properties
            if (cam != null) {
                Gizmos.color = Color.green;
                Vector3 center = target.position;
                // Calculate radius based on current camera position relative to target's XZ plane projection
                Vector3 camProjectedXZ = new Vector3(transform.position.x, target.position.y, transform.position.z);
                float radius = Vector3.Distance(camProjectedXZ, center);

                int segments = 36;
                Vector3 prevPoint = center + new Vector3(Mathf.Sin(0) * radius, 0, Mathf.Cos(0) * radius);
                for (int i = 1; i <= segments; i++)
                {
                    float angle = (i / (float)segments) * Mathf.PI * 2f;
                    Vector3 nextPoint = center + new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
                    Gizmos.DrawLine(prevPoint, nextPoint);
                    prevPoint = nextPoint;
                }
            }
        }
    }
}