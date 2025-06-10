using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 
using TMPro;
using System.Collections.Generic; 
using System.Collections;
using UnityEngine.SceneManagement;

//================== THE UI SCRIPT IS MADE BY AI ENTIRELY ===================
public class RayLabUIManager : MonoBehaviour
{
    [Header("UI Panel")]
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private bool startWithUIOpen = true;
    [SerializeField] private Toggle animateGenerationToggle;

    [Header("Object References & Prefabs")]
    [Tooltip("Prefab for the Rounded Cube Visualizer.")]
    [SerializeField] private RoundedCubeVisualizer cubePrefab;
    [Tooltip("Prefab for the Cube Sphere Visualizer.")]
    [SerializeField] private CubeSphere spherePrefab;
    [Tooltip("Position where new objects will be spawned.")]
    [SerializeField] private Vector3 objectSpawnPosition = new Vector3(0, 1, 0);

    [Header("Scene References")]
    [Tooltip("Reference to the MeshDeformerInput script in the scene (likely on the camera).")]
    [SerializeField] private MeshDeformerInput meshDeformerInput;
    [Tooltip("Optional: Reference to the OrbitCameraController for focusing.")]
    [SerializeField] private OrbitCameraController orbitCameraController;

    [Header("Deformation Settings UI - MeshDeformer")]
    [SerializeField] private TMP_Dropdown falloffModeDropdown;
    [SerializeField] private Slider falloffStartRadiusSlider;
    [SerializeField] private TextMeshProUGUI falloffStartRadiusText;
    [SerializeField] private Slider falloffEndRadiusSlider;
    [SerializeField] private TextMeshProUGUI falloffEndText;
    [SerializeField] private Slider falloffPowerSlider;
    [SerializeField] private TextMeshProUGUI falloffPowerText;
    [SerializeField] private Slider forceMultiplierSlider;
    [SerializeField] private TextMeshProUGUI forceMultiplierText;
    [SerializeField] private Slider maxDisplacementSlider;
    [SerializeField] private TextMeshProUGUI maxDisplacementText;
    [SerializeField] private Slider springForceSlider;
    [SerializeField] private TextMeshProUGUI springForceText;
    [SerializeField] private Slider dampingSlider;
    [SerializeField] private TextMeshProUGUI dampingText;
    [SerializeField] private Button resetDeformationButton;



    [Header("Ray Input Settings UI - MeshDeformerInput")]
    [SerializeField] private Slider rayForceSlider;
    [SerializeField] private TextMeshProUGUI rayForceText;
    [SerializeField] private Slider rayForceOffsetSlider;
    [SerializeField] private TextMeshProUGUI rayForceOffsetText;

    [Header("Cube Settings UI - RoundedCubeVisualizer")]
    [SerializeField] private Slider xSizeSlider;
    [SerializeField] private TextMeshProUGUI xSizeText;
    [SerializeField] private Slider ySizeSlider;
    [SerializeField] private TextMeshProUGUI ySizeText;
    [SerializeField] private Slider zSizeSlider;
    [SerializeField] private TextMeshProUGUI zSizeText;
    [SerializeField] private Slider roundnessSlider;
    [SerializeField] private TextMeshProUGUI roundnessText;
    [SerializeField] private Button createCubeButton;

    [Header("Sphere Settings UI - CubeSphere")]
    [SerializeField] private Slider sphereGridSizeSlider;
    [SerializeField] private TextMeshProUGUI sphereGridSizeText;
    [SerializeField] private Slider sphereRadiusSlider;
    [SerializeField] private TextMeshProUGUI sphereRadiusText;
    [SerializeField] private Button createSphereButton;

    // Internal state
    private bool isUIOpen = false;
    private GameObject currentActiveObjectInstance;
    private MeshDeformer currentMeshDeformer;

    // Store desired generation parameters (to apply before instantiation)
    private float desiredXSize = 4, desiredYSize = 4, desiredZSize = 4, desiredRoundness = 1;
    private float desiredSphereGrid = 4, desiredSphereRadius = 1;


    void Start()
    {
        CheckAndCreateEventSystem();

        isUIOpen = startWithUIOpen;
        if (uiPanel != null) uiPanel.SetActive(isUIOpen);

        if (animateGenerationToggle == null)
        {
            Debug.LogWarning("RayLabUIManager: Animate Generation Toggle is not assigned. Defaulting to true.");
        }


        SetupUIBindings();
        InitializeUIValues(); // Initialize sliders and text to defaults
        UpdateGameState();
    }

    void Update()
    {
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
        if (uiPanel != null) uiPanel.SetActive(isUIOpen);
        UpdateGameState();
    }

    void UpdateGameState()
    {
        if (orbitCameraController != null)
        {
            // Assuming OrbitCameraController has an 'enabled' property or similar
            // to control its input processing. If not, you might need to adapt this.
            orbitCameraController.enabled = !isUIOpen;
        }

        if (meshDeformerInput != null)
        {
            meshDeformerInput.enabled = !isUIOpen;
        }

        Cursor.visible = true; // Always visible for ray interaction or UI
        Cursor.lockState = isUIOpen ? CursorLockMode.None : CursorLockMode.Confined;
    }

    void CheckAndCreateEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
            Debug.Log("EventSystem created as it was missing.");
        }
    }

    void SetupUIBindings()
    {
        // Deformation Settings (MeshDeformer)
        if (falloffModeDropdown != null)
        {
            falloffModeDropdown.ClearOptions();
            List<string> options = new List<string>();
            foreach (MeshDeformer.FalloffMode mode in System.Enum.GetValues(typeof(MeshDeformer.FalloffMode)))
            {
                options.Add(mode.ToString());
            }
            falloffModeDropdown.AddOptions(options);
            falloffModeDropdown.onValueChanged.AddListener(OnFalloffModeChanged);
        }
        if (falloffStartRadiusSlider != null) falloffStartRadiusSlider.onValueChanged.AddListener(OnFalloffStartRadiusChanged);
        if (falloffEndRadiusSlider != null) falloffEndRadiusSlider.onValueChanged.AddListener(OnFalloffEndRadiusChanged);
        if (falloffPowerSlider != null) falloffPowerSlider.onValueChanged.AddListener(OnFalloffPowerChanged);
        if (forceMultiplierSlider != null) forceMultiplierSlider.onValueChanged.AddListener(OnForceMultiplierChanged);
        if (maxDisplacementSlider != null) maxDisplacementSlider.onValueChanged.AddListener(OnMaxDisplacementChanged);
        if (springForceSlider != null) springForceSlider.onValueChanged.AddListener(OnSpringForceChanged);
        if (dampingSlider != null) dampingSlider.onValueChanged.AddListener(OnDampingChanged);
        if (resetDeformationButton != null) resetDeformationButton.onClick.AddListener(HandleResetDeformationClicked);



        // Ray Input Settings (MeshDeformerInput)
        if (rayForceSlider != null) rayForceSlider.onValueChanged.AddListener(OnRayForceChanged);
        if (rayForceOffsetSlider != null) rayForceOffsetSlider.onValueChanged.AddListener(OnRayForceOffsetChanged);

        // Cube Settings (RoundedCubeVisualizer)
        if (xSizeSlider != null) xSizeSlider.onValueChanged.AddListener(OnXSizeChanged);
        if (ySizeSlider != null) ySizeSlider.onValueChanged.AddListener(OnYSizeChanged);
        if (zSizeSlider != null) zSizeSlider.onValueChanged.AddListener(OnZSizeChanged);
        if (roundnessSlider != null) roundnessSlider.onValueChanged.AddListener(OnRoundnessChanged);
        if (createCubeButton != null) createCubeButton.onClick.AddListener(HandleCreateCubeClicked);

        // Sphere Settings (CubeSphere)
        if (sphereGridSizeSlider != null) sphereGridSizeSlider.onValueChanged.AddListener(OnSphereGridSizeChanged);
        if (sphereRadiusSlider != null) sphereRadiusSlider.onValueChanged.AddListener(OnSphereRadiusChanged);
        if (createSphereButton != null) createSphereButton.onClick.AddListener(HandleCreateSphereClicked);
    }

    void InitializeUIValues()
    {
        // Initialize MeshDeformerInput sliders
        if (meshDeformerInput != null)
        {
            if (rayForceSlider != null) rayForceSlider.value = meshDeformerInput.force;
            if (rayForceOffsetSlider != null) rayForceOffsetSlider.value = meshDeformerInput.forceOffset;
        }
        else
        {
            if (rayForceSlider != null) rayForceSlider.value = 10f; // Default
            if (rayForceOffsetSlider != null) rayForceOffsetSlider.value = 0.1f; // Default
        }

        // Initialize Cube generation sliders to stored desired values
        if (xSizeSlider != null) xSizeSlider.value = desiredXSize;
        if (ySizeSlider != null) ySizeSlider.value = desiredYSize;
        if (zSizeSlider != null) zSizeSlider.value = desiredZSize;
        if (roundnessSlider != null) roundnessSlider.value = desiredRoundness;

        // Initialize Sphere generation sliders
        if (sphereGridSizeSlider != null) sphereGridSizeSlider.value = desiredSphereGrid;
        if (sphereRadiusSlider != null) sphereRadiusSlider.value = desiredSphereRadius;

        // Initialize deformation sliders (will be based on currentMeshDeformer if it exists, or defaults)
        RefreshDeformationControls();
        UpdateAllDisplayTexts();
    }

    void ClearCurrentActiveObject()
    {
        if (currentActiveObjectInstance != null)
        {
            // If the visualizer scripts have specific cleanup for their vertex spheres etc.
            // they should handle it in their OnDestroy or a specific clear method.
            // For UIManager, just destroy the main GameObject.
            Destroy(currentActiveObjectInstance);
            currentActiveObjectInstance = null;
            currentMeshDeformer = null;
        }
    }

    void HandleCreateCubeClicked()
    {
        if (cubePrefab == null)
        {
            Debug.LogError("Cube Prefab not assigned in UIManager!");
            return;
        }
        ClearCurrentActiveObject();

        currentActiveObjectInstance = Instantiate(cubePrefab.gameObject, objectSpawnPosition, Quaternion.identity);
        RoundedCubeVisualizer visualizer = currentActiveObjectInstance.GetComponent<RoundedCubeVisualizer>();
        currentMeshDeformer = currentActiveObjectInstance.GetComponent<MeshDeformer>();

        if (visualizer != null)
        {
            // Set all properties first
            visualizer.xSize = (int)desiredXSize;
            visualizer.ySize = (int)desiredYSize;
            visualizer.zSize = (int)desiredZSize;
            visualizer.roundness = (int)desiredRoundness;
            visualizer.animateGeneration = (animateGenerationToggle != null) ? animateGenerationToggle.isOn : true;

            // Start generation after a frame delay to ensure Start() has completed
            StartCoroutine(DelayedGeneration(visualizer));
        }
        else
        {
            Debug.LogError("Instantiated Cube Prefab is missing RoundedCubeVisualizer script!");
        }

        PostObjectCreationSetup();
    }
    private IEnumerator DelayedGeneration(RoundedCubeVisualizer visualizer)
    {
        yield return null; // Wait one frame

        if (visualizer.animateGeneration)
        {
            StartCoroutine(visualizer.GenerateWithVisualization());
        }
        else
        {
            visualizer.GenerateInstant();
        }
    }


    void HandleCreateSphereClicked()
    {
        if (spherePrefab == null)
        {
            Debug.LogError("Sphere Prefab not assigned in UIManager!");
            return;
        }
        ClearCurrentActiveObject();

        currentActiveObjectInstance = Instantiate(spherePrefab.gameObject, objectSpawnPosition, Quaternion.identity);
        CubeSphere visualizer = currentActiveObjectInstance.GetComponent<CubeSphere>();
        currentMeshDeformer = currentActiveObjectInstance.GetComponent<MeshDeformer>();

        if (visualizer != null)
        {
            // Set all properties first
            visualizer.gridSize = (int)desiredSphereGrid;
            visualizer.radius = desiredSphereRadius;
            visualizer.animateGeneration = (animateGenerationToggle != null) ? animateGenerationToggle.isOn : true;

            // Start generation after a frame delay
            StartCoroutine(DelayedSphereGeneration(visualizer)); // <<< MODIFIED HERE
        }
        else
        {
            Debug.LogError("Instantiated Sphere Prefab is missing CubeSphere script!");
        }

        PostObjectCreationSetup();
    }

    private IEnumerator DelayedSphereGeneration(CubeSphere visualizer)
    {
        yield return null; // Wait one frame

        if (visualizer.animateGeneration)
        {
            StartCoroutine(visualizer.GenerateWithVisualization());
        }
        else
        {
            visualizer.GenerateInstant(); // Direct call to public method
        }
    }


    void PostObjectCreationSetup()
    {
        RefreshDeformationControls(); // Update UI based on the new MeshDeformer
        if (orbitCameraController != null && currentActiveObjectInstance != null)
        {
            orbitCameraController.FocusOnObject(currentActiveObjectInstance);
        }
    }


    void HandleResetDeformationClicked()
    {
        if (currentMeshDeformer != null)
        {
            currentMeshDeformer.ResetDeformation();
            RefreshDeformationControls(); // Reflect reset state in UI
        }
    }

    // --- Callbacks for MeshDeformer Settings ---
    void RefreshDeformationControls()
    {
        if (currentMeshDeformer != null)
        {
            if (falloffModeDropdown != null) falloffModeDropdown.value = (int)currentMeshDeformer.falloffMode;
            if (falloffStartRadiusSlider != null) falloffStartRadiusSlider.value = currentMeshDeformer.falloffStartRadius;
            if (falloffEndRadiusSlider != null) falloffEndRadiusSlider.value = currentMeshDeformer.falloffEndRadius;
            if (falloffPowerSlider != null) falloffPowerSlider.value = currentMeshDeformer.falloffPower;
            if (forceMultiplierSlider != null) forceMultiplierSlider.value = currentMeshDeformer.forceMultiplier;
            if (maxDisplacementSlider != null) maxDisplacementSlider.value = currentMeshDeformer.maxDisplacement;
            if (springForceSlider != null) springForceSlider.value = currentMeshDeformer.springForce;
            if (dampingSlider != null) dampingSlider.value = currentMeshDeformer.damping;
        }
        else // Default values if no deformer active
        {
            if (falloffModeDropdown != null) falloffModeDropdown.value = 0; // Default to first option
            if (falloffStartRadiusSlider != null) falloffStartRadiusSlider.value = 0.5f;
            if (falloffEndRadiusSlider != null) falloffEndRadiusSlider.value = 3f;
            if (falloffPowerSlider != null) falloffPowerSlider.value = 2f;
            if (forceMultiplierSlider != null) forceMultiplierSlider.value = 1f;
            if (maxDisplacementSlider != null) maxDisplacementSlider.value = 2f;
            if (springForceSlider != null) springForceSlider.value = 20f;
            if (dampingSlider != null) dampingSlider.value = 5f;
        }
        UpdateAllDisplayTexts();
    }


    void OnFalloffModeChanged(int value) { if (currentMeshDeformer != null) currentMeshDeformer.falloffMode = (MeshDeformer.FalloffMode)value; }
    void OnFalloffStartRadiusChanged(float value) { if (currentMeshDeformer != null) currentMeshDeformer.falloffStartRadius = value; UpdateDisplayText(falloffStartRadiusText, value, "{0:F2}"); }
    void OnFalloffEndRadiusChanged(float value) { if (currentMeshDeformer != null) currentMeshDeformer.falloffEndRadius = value; UpdateDisplayText(falloffEndText, value, "{0:F2}"); }
    void OnFalloffPowerChanged(float value) { if (currentMeshDeformer != null) currentMeshDeformer.falloffPower = value; UpdateDisplayText(falloffPowerText, value, "{0:F2}"); }
    void OnForceMultiplierChanged(float value) { if (currentMeshDeformer != null) currentMeshDeformer.forceMultiplier = value; UpdateDisplayText(forceMultiplierText, value, "{0:F2}"); }
    void OnMaxDisplacementChanged(float value) { if (currentMeshDeformer != null) currentMeshDeformer.maxDisplacement = value; UpdateDisplayText(maxDisplacementText, value, "{0:F2}"); }
    void OnSpringForceChanged(float value) { if (currentMeshDeformer != null) currentMeshDeformer.springForce = value; UpdateDisplayText(springForceText, value, "{0:F1}"); }
    void OnDampingChanged(float value) { if (currentMeshDeformer != null) currentMeshDeformer.damping = value; UpdateDisplayText(dampingText, value, "{0:F1}"); }

    // --- Callbacks for MeshDeformerInput Settings ---
    void OnRayForceChanged(float value) { if (meshDeformerInput != null) meshDeformerInput.force = value; UpdateDisplayText(rayForceText, value, "{0:F1}"); }
    void OnRayForceOffsetChanged(float value) { if (meshDeformerInput != null) meshDeformerInput.forceOffset = value; UpdateDisplayText(rayForceOffsetText, value, "{0:F2}"); }

    // --- Callbacks for Cube Generation Settings ---
    void OnXSizeChanged(float value) { desiredXSize = Mathf.RoundToInt(value); UpdateDisplayText(xSizeText, desiredXSize, "{0:F0}"); }
    void OnYSizeChanged(float value) { desiredYSize = Mathf.RoundToInt(value); UpdateDisplayText(ySizeText, desiredYSize, "{0:F0}"); }
    void OnZSizeChanged(float value) { desiredZSize = Mathf.RoundToInt(value); UpdateDisplayText(zSizeText, desiredZSize, "{0:F0}"); }
    void OnRoundnessChanged(float value) { desiredRoundness = Mathf.RoundToInt(value); UpdateDisplayText(roundnessText, desiredRoundness, "{0:F0}"); }

    // --- Callbacks for Sphere Generation Settings ---
    void OnSphereGridSizeChanged(float value) { desiredSphereGrid = Mathf.RoundToInt(value); UpdateDisplayText(sphereGridSizeText, desiredSphereGrid, "{0:F0}"); }
    void OnSphereRadiusChanged(float value) { desiredSphereRadius = value; UpdateDisplayText(sphereRadiusText, value, "{0:F2}"); }

    // --- Helper for Text Updates ---
    void UpdateDisplayText(TextMeshProUGUI textComponent, float value, string format)
    {
        if (textComponent != null)
        {
            textComponent.text = string.Format(format, value);
        }
    }

    void UpdateAllDisplayTexts()
    {
        // MeshDeformer texts
        if (currentMeshDeformer != null)
        {
            UpdateDisplayText(falloffStartRadiusText, currentMeshDeformer.falloffStartRadius, "{0:F2}");
            UpdateDisplayText(falloffEndText, currentMeshDeformer.falloffEndRadius, "{0:F2}");
            UpdateDisplayText(falloffPowerText, currentMeshDeformer.falloffPower, "{0:F2}");
            UpdateDisplayText(forceMultiplierText, currentMeshDeformer.forceMultiplier, "{0:F2}");
            UpdateDisplayText(maxDisplacementText, currentMeshDeformer.maxDisplacement, "{0:F2}");
            UpdateDisplayText(springForceText, currentMeshDeformer.springForce, "{0:F1}");
            UpdateDisplayText(dampingText, currentMeshDeformer.damping, "{0:F1}");
        }
        else
        {
            UpdateDisplayText(falloffStartRadiusText, 0.5f, "{0:F2}");
            UpdateDisplayText(falloffEndText, 3f, "{0:F2}");
            UpdateDisplayText(falloffPowerText, 2f, "{0:F2}");
            UpdateDisplayText(forceMultiplierText, 1f, "{0:F2}");
            UpdateDisplayText(maxDisplacementText, 2f, "{0:F2}");
            UpdateDisplayText(springForceText, 20f, "{0:F1}");
            UpdateDisplayText(dampingText, 5f, "{0:F1}");
        }

        // MeshDeformerInput texts
        if (meshDeformerInput != null)
        {
            UpdateDisplayText(rayForceText, meshDeformerInput.force, "{0:F1}");
            UpdateDisplayText(rayForceOffsetText, meshDeformerInput.forceOffset, "{0:F2}");
        }
        else
        {
            UpdateDisplayText(rayForceText, 10f, "{0:F1}");
            UpdateDisplayText(rayForceOffsetText, 0.1f, "{0:F2}");
        }

        // Cube generation texts
        UpdateDisplayText(xSizeText, desiredXSize, "{0:F0}");
        UpdateDisplayText(ySizeText, desiredYSize, "{0:F0}");
        UpdateDisplayText(zSizeText, desiredZSize, "{0:F0}");
        UpdateDisplayText(roundnessText, desiredRoundness, "{0:F0}");

        // Sphere generation texts
        UpdateDisplayText(sphereGridSizeText, desiredSphereGrid, "{0:F0}");
        UpdateDisplayText(sphereRadiusText, desiredSphereRadius, "{0:F2}");
    }

}
