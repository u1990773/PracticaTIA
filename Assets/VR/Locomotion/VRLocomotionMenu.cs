using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Menú VR para cambiar entre modos de locomoción.
/// Puede ser un wrist menu o panel flotante.
/// </summary>
public class VRLocomotionMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VRLocomotionManager locomotionManager;
    [SerializeField] private Canvas menuCanvas;

    [Header("UI Elements")]
    [SerializeField] private Button continuousButton;
    [SerializeField] private Button teleportButton;
    [SerializeField] private Button bothButton;
    [SerializeField] private TextMeshProUGUI currentModeText;

    [Header("Toggle Menu Input")]
    [SerializeField] private InputActionProperty toggleMenuAction;
    [SerializeField] private KeyCode toggleMenuKey = KeyCode.Tab; // Fallback

    [Header("Settings")]
    [SerializeField] private bool startHidden = true;

    private bool isMenuOpen = false;

    private void Awake()
    {
        // Auto-find locomotion manager
        if (locomotionManager == null)
            locomotionManager = FindObjectOfType<VRLocomotionManager>();

        // Auto-find canvas si no está asignado
        if (menuCanvas == null)
            menuCanvas = GetComponent<Canvas>();

        // Setup botones
        if (continuousButton != null)
            continuousButton.onClick.AddListener(() => SetMode(VRLocomotionManager.LocomotionMode.Continuous));

        if (teleportButton != null)
            teleportButton.onClick.AddListener(() => SetMode(VRLocomotionManager.LocomotionMode.Teleport));

        if (bothButton != null)
            bothButton.onClick.AddListener(() => SetMode(VRLocomotionManager.LocomotionMode.Both));
    }

    private void Start()
    {
        if (startHidden)
            HideMenu();
        else
            ShowMenu();

        UpdateUI();
    }

    private void OnEnable()
    {
        toggleMenuAction.action?.Enable();
        if (toggleMenuAction.action != null)
            toggleMenuAction.action.performed += OnToggleMenu;
    }

    private void OnDisable()
    {
        if (toggleMenuAction.action != null)
            toggleMenuAction.action.performed -= OnToggleMenu;
        toggleMenuAction.action?.Disable();
    }

    private void Update()
    {
        // Fallback keyboard toggle
        if (Input.GetKeyDown(toggleMenuKey))
        {
            ToggleMenu();
        }
    }

    #region Menu Control

    private void OnToggleMenu(InputAction.CallbackContext context)
    {
        ToggleMenu();
    }

    public void ToggleMenu()
    {
        if (isMenuOpen)
            HideMenu();
        else
            ShowMenu();
    }

    public void ShowMenu()
    {
        isMenuOpen = true;
        if (menuCanvas != null)
            menuCanvas.enabled = true;
        UpdateUI();
        Debug.Log("[VRLocomotionMenu] Menu abierto.");
    }

    public void HideMenu()
    {
        isMenuOpen = false;
        if (menuCanvas != null)
            menuCanvas.enabled = false;
        Debug.Log("[VRLocomotionMenu] Menu cerrado.");
    }

    #endregion

    #region Mode Selection

    private void SetMode(VRLocomotionManager.LocomotionMode mode)
    {
        if (locomotionManager != null)
        {
            locomotionManager.SetLocomotionMode(mode);
            UpdateUI();

            // Haptic feedback
            if (VRHapticsManager.Instance != null)
                VRHapticsManager.Instance.SendLightTapBoth();
        }
    }

    private void UpdateUI()
    {
        if (locomotionManager == null) return;

        var currentMode = locomotionManager.GetCurrentMode();

        // Actualizar texto
        if (currentModeText != null)
        {
            currentModeText.text = $"Current Mode: {currentMode}";
        }

        // Destacar botón activo (opcional)
        HighlightButton(continuousButton, currentMode == VRLocomotionManager.LocomotionMode.Continuous);
        HighlightButton(teleportButton, currentMode == VRLocomotionManager.LocomotionMode.Teleport);
        HighlightButton(bothButton, currentMode == VRLocomotionManager.LocomotionMode.Both);
    }

    private void HighlightButton(Button button, bool highlight)
    {
        if (button == null) return;

        var colors = button.colors;
        colors.normalColor = highlight ? new Color(0.3f, 0.8f, 0.3f) : Color.white;
        button.colors = colors;
    }

    #endregion
}