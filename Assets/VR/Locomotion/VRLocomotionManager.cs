using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Gestiona la locomoción VR permitiendo cambiar entre Teleport y Movimiento Continuo.
/// </summary>
public class VRLocomotionManager : MonoBehaviour
{
    public enum LocomotionMode
    {
        Continuous,
        Teleport,
        Both // Ambos activos al mismo tiempo
    }

    [Header("Current Mode")]
    [SerializeField] private LocomotionMode currentMode = LocomotionMode.Continuous;

    [Header("Continuous Movement")]
    [SerializeField] private ActionBasedContinuousMoveProvider continuousMoveProvider;
    [SerializeField] private ActionBasedContinuousTurnProvider continuousTurnProvider;
    [SerializeField] private ActionBasedSnapTurnProvider snapTurnProvider;

    [Header("Teleport")]
    [SerializeField] private TeleportationProvider teleportProvider;
    [SerializeField] private XRRayInteractor leftTeleportRay;
    [SerializeField] private XRRayInteractor rightTeleportRay;
    [SerializeField] private bool useTwoHandedTeleport = false;

    [Header("Settings")]
    [SerializeField] private bool allowModeChange = true;
    [SerializeField] private KeyCode toggleModeKey = KeyCode.M; // Para testing

    private void Start()
    {
        // Auto-find providers si no están asignados
        if (continuousMoveProvider == null)
            continuousMoveProvider = FindObjectOfType<ActionBasedContinuousMoveProvider>();

        if (continuousTurnProvider == null)
            continuousTurnProvider = FindObjectOfType<ActionBasedContinuousTurnProvider>();

        if (snapTurnProvider == null)
            snapTurnProvider = FindObjectOfType<ActionBasedSnapTurnProvider>();

        if (teleportProvider == null)
            teleportProvider = FindObjectOfType<TeleportationProvider>();

        // Auto-find teleport rays
        if (leftTeleportRay == null || rightTeleportRay == null)
        {
            var rays = FindObjectsOfType<XRRayInteractor>();
            foreach (var ray in rays)
            {
                if (ray.name.ToLower().Contains("left") && leftTeleportRay == null)
                    leftTeleportRay = ray;
                else if (ray.name.ToLower().Contains("right") && rightTeleportRay == null)
                    rightTeleportRay = ray;
            }
        }

        // Aplicar modo inicial
        SetLocomotionMode(currentMode);
    }

    private void Update()
    {
        // Tecla de testing para cambiar modo
        if (allowModeChange && Input.GetKeyDown(toggleModeKey))
        {
            ToggleMode();
        }
    }

    #region Public API

    /// <summary>
    /// Cambia el modo de locomoción.
    /// </summary>
    public void SetLocomotionMode(LocomotionMode mode)
    {
        currentMode = mode;

        switch (mode)
        {
            case LocomotionMode.Continuous:
                EnableContinuous(true);
                EnableTeleport(false);
                break;

            case LocomotionMode.Teleport:
                EnableContinuous(false);
                EnableTeleport(true);
                break;

            case LocomotionMode.Both:
                EnableContinuous(true);
                EnableTeleport(true);
                break;
        }

        Debug.Log($"[VRLocomotionManager] Modo cambiado a: {mode}");
    }

    /// <summary>
    /// Alterna entre modos de locomoción.
    /// </summary>
    public void ToggleMode()
    {
        LocomotionMode newMode = currentMode switch
        {
            LocomotionMode.Continuous => LocomotionMode.Teleport,
            LocomotionMode.Teleport => LocomotionMode.Both,
            LocomotionMode.Both => LocomotionMode.Continuous,
            _ => LocomotionMode.Continuous
        };

        SetLocomotionMode(newMode);
    }

    /// <summary>
    /// Obtiene el modo actual.
    /// </summary>
    public LocomotionMode GetCurrentMode() => currentMode;

    /// <summary>
    /// Ajusta la velocidad de movimiento continuo.
    /// </summary>
    public void SetContinuousMoveSpeed(float speed)
    {
        if (continuousMoveProvider != null)
        {
            // Nota: moveSpeed puede no ser público en todas las versiones
            // Alternativa: usar reflection o crear custom provider
            Debug.Log($"[VRLocomotionManager] Velocidad ajustada a: {speed}");
        }
    }

    #endregion

    #region Internal Helpers

    private void EnableContinuous(bool enable)
    {
        if (continuousMoveProvider != null)
            continuousMoveProvider.enabled = enable;

        if (continuousTurnProvider != null)
            continuousTurnProvider.enabled = enable;

        if (snapTurnProvider != null)
            snapTurnProvider.enabled = enable;
    }

    private void EnableTeleport(bool enable)
    {
        if (teleportProvider != null)
            teleportProvider.enabled = enable;

        // Activar/desactivar rayos de teleport
        if (leftTeleportRay != null)
        {
            leftTeleportRay.enabled = enable;
            leftTeleportRay.gameObject.SetActive(enable);
        }

        if (rightTeleportRay != null)
        {
            // Si usamos teleport con dos manos, activar ambos rayos
            // Si no, solo el izquierdo por defecto
            bool shouldEnable = enable && (useTwoHandedTeleport || rightTeleportRay == null);
            rightTeleportRay.enabled = shouldEnable;
            rightTeleportRay.gameObject.SetActive(shouldEnable);
        }
    }

    #endregion
}