using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("UI Panel")]
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private bool startWithUIOpen = false;
    
    [Header("Player References")]
    [SerializeField] private AdvancedPhysicsMovement playerMovement;
    [SerializeField] private OptimizedProjectileShooter projectileShooter;
    
    [Header("Cube References")]
    [SerializeField] private RoundedCubeVisualizer cubeVisualizer;
    [SerializeField] private MeshDeformer meshDeformer;
    [SerializeField] private Rigidbody cubeRigidbody;

    [Header("Spawn Settings")]
    [SerializeField] private Vector3 cubeSpawnPosition = new Vector3(0, 5, 0); // Set your desired position
    
    [Header("Deformation Settings UI - Expert")]
    [SerializeField] private TMP_Dropdown falloffModeDropdown;
    [SerializeField] private Slider falloffStartRadiusSlider;
    [SerializeField] private Slider falloffEndRadiusSlider;
    [SerializeField] private Slider falloffPowerSlider;
    [SerializeField] private Slider forceMultiplierSlider;
    [SerializeField] private Slider maxDisplacementSlider;
    
    [Header("Deformation Settings UI - Easy")]
    [SerializeField] private Slider springForceSlider;
    [SerializeField] private Slider dampingSlider;
    
    [Header("Bullet Settings UI")]
    [SerializeField] private Slider projectileSpeedSlider;
    [SerializeField] private Slider fireRateSlider;
    [SerializeField] private Slider burstCountSlider;
    [SerializeField] private Slider burstSpreadSlider;
    
    [Header("Cube Settings UI")]
    [SerializeField] private Slider xSizeSlider;
    [SerializeField] private Slider ySizeSlider;
    [SerializeField] private Slider zSizeSlider;
    [SerializeField] private Slider roundnessSlider;
    [SerializeField] private Toggle enablePhysicsToggle;
    [SerializeField] private Button createCubeButton;
    
    [Header("UI Value Display (Optional)")]
    [SerializeField] private TextMeshProUGUI falloffStartText;
    [SerializeField] private TextMeshProUGUI falloffEndText;
    [SerializeField] private TextMeshProUGUI falloffPowerText;
    [SerializeField] private TextMeshProUGUI forceMultiplierText;
    [SerializeField] private TextMeshProUGUI maxDisplacementText;
    [SerializeField] private TextMeshProUGUI springForceText;
    [SerializeField] private TextMeshProUGUI dampingText;
    [SerializeField] private TextMeshProUGUI projectileSpeedText;
    [SerializeField] private TextMeshProUGUI fireRateText;
    [SerializeField] private TextMeshProUGUI burstCountText;
    [SerializeField] private TextMeshProUGUI burstSpreadText;
    [SerializeField] private TextMeshProUGUI xSizeText;
    [SerializeField] private TextMeshProUGUI ySizeText;
    [SerializeField] private TextMeshProUGUI zSizeText;
    [SerializeField] private TextMeshProUGUI roundnessText;
    
    private bool isUIOpen = false;
    private Vector3 originalCubePosition;
    private Quaternion originalCubeRotation;
    
    void Start()
    {
        // Check for EventSystem and create if missing
        CheckAndCreateEventSystem();
        
        // Initialize UI state
        isUIOpen = startWithUIOpen;
        uiPanel.SetActive(isUIOpen);
        
        // Store original cube transform if cube exists
        if (cubeRigidbody != null)
        {
            originalCubePosition = cubeRigidbody.transform.position;
            originalCubeRotation = cubeRigidbody.transform.rotation;
        }
        
        // Setup all UI bindings
        SetupUIBindings();
        
        // Initialize UI values from current script values
        InitializeUIValues();
        
        // Set initial game state based on UI visibility
        UpdateGameState();
    }
    
    void Update()
    {
        // Toggle UI with ESC key
        if (Input.GetKeyDown(KeyCode.P))
        {
            ToggleUI();
        }
    }

    public void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name to load cannot be null or empty!");
            return;
        }
        Debug.Log("Attempting to load scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }


    
    void ToggleUI()
    {
        isUIOpen = !isUIOpen;
        uiPanel.SetActive(isUIOpen);
        UpdateGameState();
    }
    
    void UpdateGameState()
    {
        // Enable/disable player controls based on UI state
        if (playerMovement != null)
            playerMovement.enabled = !isUIOpen;
            
        if (projectileShooter != null)
            projectileShooter.enabled = !isUIOpen;
        
        // For mouse-aiming games, we keep cursor always visible but control its lock state
        Cursor.visible = true;  // Always visible for aiming
        Cursor.lockState = isUIOpen ? CursorLockMode.None : CursorLockMode.Confined;
        
        // CursorLockMode explanation:
        // - None: Cursor can move anywhere (good for UI interaction)
        // - Confined: Cursor stays within game window but can move (good for mouse aiming)
        // - Locked: Cursor locked to center (good for FPS with invisible cursor)
        
        // Pause/unpause time if needed (optional)
        // Time.timeScale = isUIOpen ? 0f : 1f;
    }
    
    void SetupUIBindings()
    {
        // Deformation Settings - Expert
        if (falloffModeDropdown != null)
        {
            falloffModeDropdown.onValueChanged.AddListener(OnFalloffModeChanged);
        }
        
        if (falloffStartRadiusSlider != null)
        {
            falloffStartRadiusSlider.onValueChanged.AddListener(OnFalloffStartRadiusChanged);
        }
        
        if (falloffEndRadiusSlider != null)
        {
            falloffEndRadiusSlider.onValueChanged.AddListener(OnFalloffEndRadiusChanged);
        }
        
        if (falloffPowerSlider != null)
        {
            falloffPowerSlider.onValueChanged.AddListener(OnFalloffPowerChanged);
        }
        
        if (forceMultiplierSlider != null)
        {
            forceMultiplierSlider.onValueChanged.AddListener(OnForceMultiplierChanged);
        }
        
        if (maxDisplacementSlider != null)
        {
            maxDisplacementSlider.onValueChanged.AddListener(OnMaxDisplacementChanged);
        }
        
        // Deformation Settings - Easy
        if (springForceSlider != null)
        {
            springForceSlider.onValueChanged.AddListener(OnSpringForceChanged);
        }
        
        if (dampingSlider != null)
        {
            dampingSlider.onValueChanged.AddListener(OnDampingChanged);
        }
        
        // Bullet Settings
        if (projectileSpeedSlider != null)
        {
            projectileSpeedSlider.onValueChanged.AddListener(OnProjectileSpeedChanged);
        }
        
        if (fireRateSlider != null)
        {
            fireRateSlider.onValueChanged.AddListener(OnFireRateChanged);
        }
        
        if (burstCountSlider != null)
        {
            burstCountSlider.onValueChanged.AddListener(OnBurstCountChanged);
        }
        
        if (burstSpreadSlider != null)
        {
            burstSpreadSlider.onValueChanged.AddListener(OnBurstSpreadChanged);
        }
        
        // Cube Settings
        if (xSizeSlider != null)
        {
            xSizeSlider.onValueChanged.AddListener(OnXSizeChanged);
        }
        
        if (ySizeSlider != null)
        {
            ySizeSlider.onValueChanged.AddListener(OnYSizeChanged);
        }
        
        if (zSizeSlider != null)
        {
            zSizeSlider.onValueChanged.AddListener(OnZSizeChanged);
        }
        
        if (roundnessSlider != null)
        {
            roundnessSlider.onValueChanged.AddListener(OnRoundnessChanged);
        }
        
        if (enablePhysicsToggle != null)
        {
            enablePhysicsToggle.onValueChanged.AddListener(OnEnablePhysicsChanged);
        }
        
        if (createCubeButton != null)
        {
            createCubeButton.onClick.AddListener(OnCreateCubeClicked);
        }
    }
    
    void InitializeUIValues()
    {
        // Initialize sliders with current values from scripts
        if (meshDeformer != null)
        {
            if (falloffModeDropdown != null)
                falloffModeDropdown.value = (int)meshDeformer.falloffMode;
                
            if (falloffStartRadiusSlider != null)
                falloffStartRadiusSlider.value = meshDeformer.falloffStartRadius;
                
            if (falloffEndRadiusSlider != null)
                falloffEndRadiusSlider.value = meshDeformer.falloffEndRadius;
                
            if (falloffPowerSlider != null)
                falloffPowerSlider.value = meshDeformer.falloffPower;
                
            if (forceMultiplierSlider != null)
                forceMultiplierSlider.value = meshDeformer.forceMultiplier;
                
            if (maxDisplacementSlider != null)
                maxDisplacementSlider.value = meshDeformer.maxDisplacement;
                
            if (springForceSlider != null)
                springForceSlider.value = meshDeformer.springForce;
                
            if (dampingSlider != null)
                dampingSlider.value = meshDeformer.damping;
        }
        
        if (projectileShooter != null)
        {
            if (projectileSpeedSlider != null)
                projectileSpeedSlider.value = projectileShooter.projectileSpeed;
                
            if (fireRateSlider != null)
                fireRateSlider.value = projectileShooter.fireRate;
                
            if (burstCountSlider != null)
                burstCountSlider.value = projectileShooter.burstCount;
                
            if (burstSpreadSlider != null)
                burstSpreadSlider.value = projectileShooter.burstSpread;
        }
        
        if (cubeVisualizer != null)
        {
            if (xSizeSlider != null)
                xSizeSlider.value = cubeVisualizer.xSize;
                
            if (ySizeSlider != null)
                ySizeSlider.value = cubeVisualizer.ySize;
                
            if (zSizeSlider != null)
                zSizeSlider.value = cubeVisualizer.zSize;
                
            if (roundnessSlider != null)
                roundnessSlider.value = cubeVisualizer.roundness;
        }
        
        // Update all display texts
        UpdateAllDisplayTexts();
    }
    
    // Callback methods for UI changes
    void OnFalloffModeChanged(int value)
    {
        if (meshDeformer != null)
            meshDeformer.falloffMode = (MeshDeformer.FalloffMode)value;
    }
    
    void OnFalloffStartRadiusChanged(float value)
    {
        if (meshDeformer != null)
            meshDeformer.falloffStartRadius = value;
        UpdateDisplayText(falloffStartText, value, "{0:F2}");
    }
    
    void OnFalloffEndRadiusChanged(float value)
    {
        if (meshDeformer != null)
            meshDeformer.falloffEndRadius = value;
        UpdateDisplayText(falloffEndText, value, "{0:F2}");
    }
    
    void OnFalloffPowerChanged(float value)
    {
        if (meshDeformer != null)
            meshDeformer.falloffPower = value;
        UpdateDisplayText(falloffPowerText, value, "{0:F2}");
    }
    
    void OnForceMultiplierChanged(float value)
    {
        if (meshDeformer != null)
            meshDeformer.forceMultiplier = value;
        UpdateDisplayText(forceMultiplierText, value, "{0:F2}");
    }
    
    void OnMaxDisplacementChanged(float value)
    {
        if (meshDeformer != null)
            meshDeformer.maxDisplacement = value;
        UpdateDisplayText(maxDisplacementText, value, "{0:F2}");
    }
    
    void OnSpringForceChanged(float value)
    {
        if (meshDeformer != null)
            meshDeformer.springForce = value;
        UpdateDisplayText(springForceText, value, "{0:F1}");
    }
    
    void OnDampingChanged(float value)
    {
        if (meshDeformer != null)
            meshDeformer.damping = value;
        UpdateDisplayText(dampingText, value, "{0:F1}");
    }
    
    void OnProjectileSpeedChanged(float value)
    {
        if (projectileShooter != null)
            projectileShooter.projectileSpeed = value;
        UpdateDisplayText(projectileSpeedText, value, "{0:F0}");
    }
    
    void OnFireRateChanged(float value)
    {
        if (projectileShooter != null)
            projectileShooter.fireRate = value;
        UpdateDisplayText(fireRateText, value, "{0:F2}");
    }
    
    void OnBurstCountChanged(float value)
    {
        if (projectileShooter != null)
            projectileShooter.burstCount = Mathf.RoundToInt(value);
        UpdateDisplayText(burstCountText, value, "{0:F0}");
    }
    
    void OnBurstSpreadChanged(float value)
    {
        if (projectileShooter != null)
            projectileShooter.burstSpread = value;
        UpdateDisplayText(burstSpreadText, value, "{0:F1}°");
    }
    
    void OnXSizeChanged(float value)
    {
        if (cubeVisualizer != null)
            cubeVisualizer.xSize = Mathf.RoundToInt(value);
        UpdateDisplayText(xSizeText, value, "{0:F0}");
    }
    
    void OnYSizeChanged(float value)
    {
        if (cubeVisualizer != null)
            cubeVisualizer.ySize = Mathf.RoundToInt(value);
        UpdateDisplayText(ySizeText, value, "{0:F0}");
    }
    
    void OnZSizeChanged(float value)
    {
        if (cubeVisualizer != null)
            cubeVisualizer.zSize = Mathf.RoundToInt(value);
        UpdateDisplayText(zSizeText, value, "{0:F0}");
    }
    
    void OnRoundnessChanged(float value)
    {
        if (cubeVisualizer != null)
            cubeVisualizer.roundness = Mathf.RoundToInt(value);
        UpdateDisplayText(roundnessText, value, "{0:F0}");
    }
    
   void OnEnablePhysicsChanged(bool enabled)
{
    if (cubeRigidbody != null)
    {
        // Don't allow physics changes during generation
        if (cubeVisualizer != null && cubeVisualizer.IsGenerating())
        {
            Debug.Log("Cannot change physics during cube generation");
            // Reset the toggle to its previous state
            enablePhysicsToggle.SetIsOnWithoutNotify(!enabled);
            return;
        }
        
        cubeRigidbody.isKinematic = !enabled;
    }
}
    
    void OnCreateCubeClicked()
    {
       if (cubeVisualizer != null)
        {
        // Move the cube GameObject to the spawn position
        cubeVisualizer.transform.position = cubeSpawnPosition;
        cubeVisualizer.transform.rotation = Quaternion.identity; // Reset rotation if needed
        
        // Trigger cube generation
        cubeVisualizer.StartCoroutine(cubeVisualizer.GenerateWithVisualization());
        }

    }
    void CheckAndCreateEventSystem()
    {
        // Check if an EventSystem already exists in the scene
        EventSystem existingEventSystem = FindObjectOfType<EventSystem>();
        
        if (existingEventSystem == null)
        {
            // No EventSystem found, so we need to create one
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
            
            Debug.Log("EventSystem was missing and has been created automatically.");
        }
        else
        {
            Debug.Log("EventSystem found in scene.");
        }
    }
    
    void UpdateDisplayText(TextMeshProUGUI textComponent, float value, string format)
    {
        if (textComponent != null)
            textComponent.text = string.Format(format, value);
    }
    
    // Update all display texts at once
    void UpdateAllDisplayTexts()
    {
        if (meshDeformer != null)
        {
            UpdateDisplayText(falloffStartText, meshDeformer.falloffStartRadius, "{0:F2}");
            UpdateDisplayText(falloffEndText, meshDeformer.falloffEndRadius, "{0:F2}");
            UpdateDisplayText(falloffPowerText, meshDeformer.falloffPower, "{0:F2}");
            UpdateDisplayText(forceMultiplierText, meshDeformer.forceMultiplier, "{0:F2}");
            UpdateDisplayText(maxDisplacementText, meshDeformer.maxDisplacement, "{0:F2}");
            UpdateDisplayText(springForceText, meshDeformer.springForce, "{0:F1}");
            UpdateDisplayText(dampingText, meshDeformer.damping, "{0:F1}");
        }
        
        if (projectileShooter != null)
        {
            UpdateDisplayText(projectileSpeedText, projectileShooter.projectileSpeed, "{0:F0}");
            UpdateDisplayText(fireRateText, projectileShooter.fireRate, "{0:F2}");
            UpdateDisplayText(burstCountText, projectileShooter.burstCount, "{0:F0}");
            UpdateDisplayText(burstSpreadText, projectileShooter.burstSpread, "{0:F1}°");
        }
        
        if (cubeVisualizer != null)
        {
            UpdateDisplayText(xSizeText, cubeVisualizer.xSize, "{0:F0}");
            UpdateDisplayText(ySizeText, cubeVisualizer.ySize, "{0:F0}");
            UpdateDisplayText(zSizeText, cubeVisualizer.zSize, "{0:F0}");
            UpdateDisplayText(roundnessText, cubeVisualizer.roundness, "{0:F0}");
        }
    }
}