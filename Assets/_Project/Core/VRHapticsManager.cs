using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

/// <summary>
/// Sistema centralizado de haptics (vibración) para controladores VR.
/// Uso: VRHapticsManager.Instance.SendHaptic(controller, intensity, duration);
/// </summary>
public class VRHapticsManager : MonoBehaviour
{
    public static VRHapticsManager Instance { get; private set; }

    [Header("Presets")]
    [SerializeField] private HapticPreset lightTap = new HapticPreset(0.2f, 0.05f);
    [SerializeField] private HapticPreset mediumBump = new HapticPreset(0.5f, 0.1f);
    [SerializeField] private HapticPreset strongPulse = new HapticPreset(0.8f, 0.2f);
    [SerializeField] private HapticPreset gunShot = new HapticPreset(0.9f, 0.15f);
    [SerializeField] private HapticPreset impact = new HapticPreset(1.0f, 0.1f);

    [Header("References (auto-find)")]
    [SerializeField] private ActionBasedController leftController;
    [SerializeField] private ActionBasedController rightController;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Auto-find controllers si no están asignados
        if (leftController == null || rightController == null)
        {
            var controllers = FindObjectsOfType<ActionBasedController>();
            foreach (var ctrl in controllers)
            {
                if (ctrl.name.ToLower().Contains("left"))
                    leftController = ctrl;
                else if (ctrl.name.ToLower().Contains("right"))
                    rightController = ctrl;
            }
        }
    }

    #region Public API

    /// <summary>
    /// Envía vibración a un controlador específico.
    /// </summary>
    public void SendHaptic(ActionBasedController controller, float intensity, float duration)
    {
        if (controller == null)
        {
            Debug.LogWarning("[VRHapticsManager] Controller is null.");
            return;
        }

        controller.SendHapticImpulse(Mathf.Clamp01(intensity), duration);
    }

    /// <summary>
    /// Envía vibración a ambos controladores.
    /// </summary>
    public void SendHapticBoth(float intensity, float duration)
    {
        SendHaptic(leftController, intensity, duration);
        SendHaptic(rightController, intensity, duration);
    }

    /// <summary>
    /// Envía un preset de haptic a un controlador.
    /// </summary>
    public void SendHapticPreset(ActionBasedController controller, HapticPreset preset)
    {
        SendHaptic(controller, preset.intensity, preset.duration);
    }

    /// <summary>
    /// Envía vibración según el interactor que agarró un objeto.
    /// </summary>
    public void SendHapticFromInteractor(IXRSelectInteractor interactor, float intensity, float duration)
    {
        if (interactor is XRBaseControllerInteractor controllerInteractor)
        {
            var controller = controllerInteractor.GetComponent<ActionBasedController>();
            SendHaptic(controller, intensity, duration);
        }
    }

    /// <summary>
    /// Envía vibración en pulsos múltiples.
    /// </summary>
    public void SendHapticPulse(ActionBasedController controller, int pulseCount, float intensity, float pulseDuration, float delayBetween)
    {
        StartCoroutine(HapticPulseRoutine(controller, pulseCount, intensity, pulseDuration, delayBetween));
    }

    #endregion

    #region Presets Access

    public void SendLightTap(ActionBasedController controller) => SendHapticPreset(controller, lightTap);
    public void SendMediumBump(ActionBasedController controller) => SendHapticPreset(controller, mediumBump);
    public void SendStrongPulse(ActionBasedController controller) => SendHapticPreset(controller, strongPulse);
    public void SendGunShot(ActionBasedController controller) => SendHapticPreset(controller, gunShot);
    public void SendImpact(ActionBasedController controller) => SendHapticPreset(controller, impact);

    public void SendLightTapBoth() => SendHapticBoth(lightTap.intensity, lightTap.duration);
    public void SendMediumBumpBoth() => SendHapticBoth(mediumBump.intensity, mediumBump.duration);
    public void SendStrongPulseBoth() => SendHapticBoth(strongPulse.intensity, strongPulse.duration);

    #endregion

    #region Helper Methods

    private IEnumerator HapticPulseRoutine(ActionBasedController controller, int count, float intensity, float duration, float delay)
    {
        for (int i = 0; i < count; i++)
        {
            SendHaptic(controller, intensity, duration);
            yield return new WaitForSeconds(duration + delay);
        }
    }

    /// <summary>
    /// Obtiene el controlador izquierdo.
    /// </summary>
    public ActionBasedController GetLeftController() => leftController;

    /// <summary>
    /// Obtiene el controlador derecho.
    /// </summary>
    public ActionBasedController GetRightController() => rightController;

    #endregion
}

/// <summary>
/// Preset de haptic para reutilizar configuraciones comunes.
/// </summary>
[System.Serializable]
public struct HapticPreset
{
    [Range(0f, 1f)] public float intensity;
    [Range(0f, 1f)] public float duration;

    public HapticPreset(float intensity, float duration)
    {
        this.intensity = intensity;
        this.duration = duration;
    }
}