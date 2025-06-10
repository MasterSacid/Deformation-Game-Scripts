using UnityEngine;

public class DynamicTopdownCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target; // The player to follow
    [SerializeField] private float heightOffset = 15f; // How high above the target
    [SerializeField] private float forwardOffset = 0f; // Offset in front of target
    
    [Header("Camera Angle")]
    [SerializeField] private float cameraAngle = 62f; // The X rotation angle (90 = straight down)
    [SerializeField] private Vector3 additionalOffset = new Vector3(0, 0, -3f); // Fine-tune position
    [SerializeField] private bool dynamicAngle = false; // Allow angle to change based on action
    [SerializeField] private float combatAngle = 58f; // Lower angle during combat
    [SerializeField] private float explorationAngle = 65f; // Higher angle when exploring
    [SerializeField] private float angleTransitionSpeed = 2f; // How fast angle changes
    
    [Header("Follow Behavior")]
    [SerializeField] private float followSmoothness = 8f; // How quickly camera catches up
    [SerializeField] private float rotationSmoothness = 10f; // How quickly camera rotates
    [SerializeField] private bool useFixedUpdate = true; // Use FixedUpdate for physics sync
    
    [Header("Look Ahead")]
    [SerializeField] private bool enableLookAhead = true;
    [SerializeField] private float lookAheadDistance = 3f; // How far to look ahead
    [SerializeField] private float lookAheadSmoothness = 5f;
    [SerializeField] private LookAheadMode lookAheadMode = LookAheadMode.Movement;
    
    [Header("Dynamic Zoom")]
    [SerializeField] private bool enableDynamicZoom = true;
    [SerializeField] private float baseFieldOfView = 60f;
    [SerializeField] private float maxSpeedForZoom = 15f; // Speed at which max zoom occurs
    [SerializeField] private float maxZoomOut = 75f; // Maximum FOV when moving fast
    [SerializeField] private float zoomSmoothness = 3f;
    
    [Header("Camera Shake")]
    [SerializeField] private bool enableCameraShake = false;
    [SerializeField] private float shakeIntensity = 0.1f;
    [SerializeField] private float shakeFrequency = 10f;
    
    [Header("Boundaries")]
    [SerializeField] private bool useBoundaries = false;
    [SerializeField] private Vector2 minBounds = new Vector2(-50, -50);
    [SerializeField] private Vector2 maxBounds = new Vector2(50, 50);
    
    [Header("Mouse Influence")]
    [SerializeField] private bool enableMouseInfluence = true;
    [SerializeField] private float mouseInfluenceRadius = 5f; // How far mouse can pull camera
    [SerializeField] private float mouseInfluenceStrength = 0.3f; // 0-1, how much mouse affects camera
    
    // Private variables
    private Camera cam;
    private Vector3 currentLookAheadOffset;
    private Vector3 targetPosition;
    private float currentFieldOfView;
    private Vector3 shakeOffset;
    private float shakeTimer;
    private Rigidbody targetRigidbody;
    private Vector3 previousTargetPosition;
    private Vector3 mouseWorldPosition;
    private float currentAngle;
    private bool inCombat = false;
    
    public enum LookAheadMode
    {
        Movement,    // Look in the direction of movement
        MouseAim,    // Look towards where the mouse is pointing
        Combined     // Blend between movement and mouse aim
    }
    
    void Start()
    {
        // Get camera component
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("DynamicTopdownCamera requires a Camera component!");
            enabled = false;
            return;
        }
        
        // Set initial values
        currentFieldOfView = baseFieldOfView;
        cam.fieldOfView = baseFieldOfView;
        currentAngle = cameraAngle;
        
        // Try to get target's rigidbody for velocity-based calculations
        if (target != null)
        {
            targetRigidbody = target.GetComponent<Rigidbody>();
            previousTargetPosition = target.position;
        }
        
        // Position camera initially
        if (target != null)
        {
            Vector3 initialPosition = CalculateDesiredPosition();
            transform.position = initialPosition;
            UpdateCameraRotation();
        }
    }
    
    void Update()
    {
        if (!useFixedUpdate)
        {
            UpdateCamera(Time.deltaTime);
        }
        
        // Always update mouse position in Update for accuracy
        UpdateMouseWorldPosition();
        
        // Update camera shake if enabled
        if (enableCameraShake)
        {
            UpdateCameraShake(Time.deltaTime);
        }
    }
    
    void FixedUpdate()
    {
        if (useFixedUpdate)
        {
            UpdateCamera(Time.fixedDeltaTime);
        }
    }
    
    void UpdateCamera(float deltaTime)
    {
        if (target == null) return;
        
        // Update dynamic angle if enabled
        if (dynamicAngle)
        {
            UpdateDynamicAngle(deltaTime);
        }
        
        // Calculate the base desired position
        Vector3 desiredPosition = CalculateDesiredPosition();
        
        // Apply look ahead offset
        if (enableLookAhead)
        {
            UpdateLookAhead(deltaTime);
            desiredPosition += currentLookAheadOffset;
        }
        
        // Apply mouse influence
        if (enableMouseInfluence)
        {
            Vector3 mouseInfluence = CalculateMouseInfluence();
            desiredPosition += mouseInfluence;
        }
        
        // Apply boundaries if enabled
        if (useBoundaries)
        {
            desiredPosition = ApplyBoundaries(desiredPosition);
        }
        
        // Smoothly move to the desired position
        targetPosition = Vector3.Lerp(targetPosition, desiredPosition, followSmoothness * deltaTime);
        transform.position = targetPosition + shakeOffset;
        
        // Update dynamic zoom
        if (enableDynamicZoom)
        {
            UpdateDynamicZoom(deltaTime);
        }
        
        // Update camera rotation based on angle
        UpdateCameraRotation();
    }
    
    Vector3 CalculateDesiredPosition()
    {
        // Base position at the target
        Vector3 position = target.position;
        
        // Calculate camera position based on angle
        // When angle is 90 (straight down), camera is directly above
        // As angle decreases, camera moves back
        float angleInRadians = currentAngle * Mathf.Deg2Rad;
        float horizontalDistance = heightOffset * Mathf.Tan((90f - currentAngle) * Mathf.Deg2Rad);
        
        // Add height offset
        position.y += heightOffset;
        
        // Move camera back based on angle
        position.z -= horizontalDistance;
        
        // Add forward offset based on target's facing direction
        if (forwardOffset != 0f)
        {
            Vector3 forward = target.forward;
            forward.y = 0f;
            forward.Normalize();
            position += forward * forwardOffset;
        }
        
        // Apply additional manual offset
        position += additionalOffset;
        
        return position;
    }
    
    void UpdateCameraRotation()
    {
        // Smoothly rotate to the desired angle
        Quaternion desiredRotation = Quaternion.Euler(currentAngle, 0f, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothness * Time.deltaTime);
    }
    
    void UpdateDynamicAngle(float deltaTime)
    {
        // Determine target angle based on game state
        float targetAngle = inCombat ? combatAngle : explorationAngle;
        
        // Smoothly transition to target angle
        currentAngle = Mathf.Lerp(currentAngle, targetAngle, angleTransitionSpeed * deltaTime);
    }
    
    void UpdateLookAhead(float deltaTime)
    {
        Vector3 lookDirection = Vector3.zero;
        
        switch (lookAheadMode)
        {
            case LookAheadMode.Movement:
                // Look in the direction of movement
                if (targetRigidbody != null)
                {
                    lookDirection = targetRigidbody.linearVelocity;
                    lookDirection.y = 0f;
                }
                else
                {
                    // Fallback to position-based movement detection
                    Vector3 movementDelta = target.position - previousTargetPosition;
                    lookDirection = movementDelta / deltaTime;
                    lookDirection.y = 0f;
                    previousTargetPosition = target.position;
                }
                break;
                
            case LookAheadMode.MouseAim:
                // Look towards where the player is aiming (mouse position)
                lookDirection = mouseWorldPosition - target.position;
                lookDirection.y = 0f;
                break;
                
            case LookAheadMode.Combined:
                // Blend between movement and mouse aim
                Vector3 moveDir = Vector3.zero;
                if (targetRigidbody != null)
                {
                    moveDir = targetRigidbody.linearVelocity;
                    moveDir.y = 0f;
                }
                
                Vector3 aimDir = mouseWorldPosition - target.position;
                aimDir.y = 0f;
                
                // Blend based on movement speed (more movement = more movement influence)
                float blendFactor = Mathf.Clamp01(moveDir.magnitude / 5f);
                lookDirection = Vector3.Lerp(aimDir.normalized, moveDir.normalized, blendFactor) * moveDir.magnitude;
                break;
        }
        
        // Normalize and apply look ahead distance
        if (lookDirection.magnitude > 0.1f)
        {
            lookDirection.Normalize();
            Vector3 desiredLookAhead = lookDirection * lookAheadDistance;
            currentLookAheadOffset = Vector3.Lerp(currentLookAheadOffset, desiredLookAhead, lookAheadSmoothness * deltaTime);
        }
        else
        {
            // Gradually return to center when not moving
            currentLookAheadOffset = Vector3.Lerp(currentLookAheadOffset, Vector3.zero, lookAheadSmoothness * deltaTime);
        }
    }
    
    void UpdateMouseWorldPosition()
    {
        // Create a plane at the player's height
        Plane playerPlane = new Plane(Vector3.up, target.position);
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        
        float hitDistance;
        if (playerPlane.Raycast(ray, out hitDistance))
        {
            mouseWorldPosition = ray.GetPoint(hitDistance);
        }
    }
    
    Vector3 CalculateMouseInfluence()
    {
        // Calculate offset from target to mouse
        Vector3 mouseOffset = mouseWorldPosition - target.position;
        mouseOffset.y = 0f;
        
        // Clamp the influence to the specified radius
        float distance = mouseOffset.magnitude;
        if (distance > mouseInfluenceRadius)
        {
            mouseOffset = mouseOffset.normalized * mouseInfluenceRadius;
        }
        
        // Apply influence strength
        return mouseOffset * mouseInfluenceStrength;
    }
    
    void UpdateDynamicZoom(float deltaTime)
    {
        float targetFOV = baseFieldOfView;
        
        if (targetRigidbody != null)
        {
            // Calculate zoom based on velocity
            float speed = targetRigidbody.linearVelocity.magnitude;
            float speedPercent = Mathf.Clamp01(speed / maxSpeedForZoom);
            
            // Interpolate FOV based on speed
            targetFOV = Mathf.Lerp(baseFieldOfView, maxZoomOut, speedPercent);
        }
        
        // Smoothly adjust FOV
        currentFieldOfView = Mathf.Lerp(currentFieldOfView, targetFOV, zoomSmoothness * deltaTime);
        cam.fieldOfView = currentFieldOfView;
    }
    
    void UpdateCameraShake(float deltaTime)
    {
        if (shakeTimer > 0f || enableCameraShake)
        {
            // Create shake using Perlin noise for smooth random movement
            float shakeX = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) - 0.5f) * 2f;
            float shakeY = (Mathf.PerlinNoise(0f, Time.time * shakeFrequency) - 0.5f) * 2f;
            
            shakeOffset = new Vector3(shakeX, shakeY, 0f) * shakeIntensity;
            
            // Decay shake timer if we're using triggered shakes
            if (shakeTimer > 0f)
            {
                shakeTimer -= deltaTime;
                if (shakeTimer <= 0f)
                {
                    shakeOffset = Vector3.zero;
                }
            }
        }
    }
    
    Vector3 ApplyBoundaries(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, minBounds.x, maxBounds.x);
        position.z = Mathf.Clamp(position.z, minBounds.y, maxBounds.y);
        return position;
    }
    
    // Public methods for external control
    public void TriggerShake(float duration, float intensity)
    {
        shakeTimer = duration;
        shakeIntensity = intensity;
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            targetRigidbody = target.GetComponent<Rigidbody>();
            previousTargetPosition = target.position;
        }
    }
    
    public void SetLookAheadMode(LookAheadMode mode)
    {
        lookAheadMode = mode;
    }
    
    public void SetCameraAngle(float angle)
    {
        cameraAngle = Mathf.Clamp(angle, 30f, 90f);
        if (!dynamicAngle)
        {
            currentAngle = cameraAngle;
        }
    }
    
    public void SetCombatMode(bool combat)
    {
        inCombat = combat;
    }
    
    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (target == null) return;
        
        // Draw camera angle visualization
        Gizmos.color = Color.cyan;
        Vector3 cameraPos = CalculateDesiredPosition();
        Gizmos.DrawLine(target.position, cameraPos);
        Gizmos.DrawWireSphere(cameraPos, 0.5f);
        
        // Draw view direction
        Vector3 viewDirection = Quaternion.Euler(currentAngle, 0, 0) * Vector3.forward;
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(cameraPos, viewDirection * 5f);
        
        // Draw boundary box
        if (useBoundaries)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2f, target.position.y, (minBounds.y + maxBounds.y) / 2f);
            Vector3 size = new Vector3(maxBounds.x - minBounds.x, 0.1f, maxBounds.y - minBounds.y);
            Gizmos.DrawWireCube(center, size);
        }
        
        // Draw mouse influence radius
        if (enableMouseInfluence && Application.isPlaying)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(target.position, mouseInfluenceRadius);
            
            // Draw line to mouse position
            Gizmos.color = Color.green;
            Gizmos.DrawLine(target.position, mouseWorldPosition);
        }
        
        // Draw look ahead direction
        if (enableLookAhead && Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(target.position, currentLookAheadOffset);
        }
    }
}