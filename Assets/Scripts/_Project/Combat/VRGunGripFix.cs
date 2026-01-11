using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Configura el agarre perfecto del arma VR.
/// - Posición correcta (cerca del cuerpo)
/// - Rotación correcta (apunta adelante, no abajo)
/// - Attach point optimizado
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class VRGunGripFix : MonoBehaviour
{
    [Header("Attach Point Configuration")]
    [SerializeField] private Transform attachPoint;
    [SerializeField] private bool autoCreateAttachPoint = true;

    [Header("Position (relativo al arma)")]
    [Tooltip("Hacia atrás = grip de pistola")]
    [SerializeField] private Vector3 attachPointLocalPosition = new Vector3(0, -0.05f, -0.15f);
    // X = izq/der, Y = arriba/abajo, Z = adelante/atrás

    [Header("Rotation (para que apunte adelante)")]
    [Tooltip("Ajusta hasta que el arma apunte adelante cuando la agarres")]
    [SerializeField] private Vector3 attachPointLocalRotation = new Vector3(0, 0, 0);
    // Prueba: (0,0,0), (-90,0,0), (0,-90,0), (0,0,-90)

    [Header("Distance from Body")]
    [Tooltip("Distancia del arma al cuerpo (menor = más cerca)")]
    [SerializeField] private float distanceFromController = 0.05f;

    [Header("Grab Settings")]
    [SerializeField] private bool useDynamicAttach = false;
    [SerializeField] private bool throwOnDetach = false;
    [SerializeField] private float snapToColliderDistance = 0.5f;

    [Header("Visual Helpers")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.cyan;

    private XRGrabInteractable grabInteractable;

    private void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Crear attach point si no existe
        if (attachPoint == null && autoCreateAttachPoint)
        {
            CreateAttachPoint();
        }

        // Configurar grab settings
        ConfigureGrabInteractable();

        Debug.Log("[VRGunGripFix] Arma configurada. Attach point en posición: " + attachPoint.localPosition);
    }

    private void CreateAttachPoint()
    {
        GameObject attachObj = new GameObject("GunGripAttachPoint");
        attachObj.transform.SetParent(transform);
        attachObj.transform.localPosition = attachPointLocalPosition;
        attachObj.transform.localEulerAngles = attachPointLocalRotation;
        attachPoint = attachObj.transform;

        Debug.Log("[VRGunGripFix] Attach point creado automáticamente.");
    }

    private void ConfigureGrabInteractable()
    {
        if (grabInteractable == null) return;

        // CRÍTICO: Asignar attach transform
        grabInteractable.attachTransform = attachPoint;

        // Configurar opciones de agarre
        grabInteractable.useDynamicAttach = useDynamicAttach;
        grabInteractable.throwOnDetach = throwOnDetach;

        // Smooth movement para transición suave
        grabInteractable.smoothPosition = true;
        grabInteractable.smoothPositionAmount = 15f; // Aumentado para agarre más rápido
        grabInteractable.smoothRotation = true;
        grabInteractable.smoothRotationAmount = 15f;

        // Snap settings
        grabInteractable.selectMode = InteractableSelectMode.Single;

        // Movement type - IMPORTANTE
        grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;

        // Track position/rotation
        grabInteractable.trackPosition = true;
        grabInteractable.trackRotation = true;

        Debug.Log("[VRGunGripFix] XRGrabInteractable configurado.");
    }

    /// <summary>
    /// Ajusta la posición del attach point en runtime.
    /// Útil para testing.
    /// </summary>
    public void SetAttachPosition(Vector3 localPos)
    {
        if (attachPoint != null)
        {
            attachPoint.localPosition = localPos;
            attachPointLocalPosition = localPos;
        }
    }

    /// <summary>
    /// Ajusta la rotación del attach point en runtime.
    /// </summary>
    public void SetAttachRotation(Vector3 localRot)
    {
        if (attachPoint != null)
        {
            attachPoint.localEulerAngles = localRot;
            attachPointLocalRotation = localRot;
        }
    }

    /// <summary>
    /// Presets comunes de rotación.
    /// </summary>
    public void ApplyRotationPreset(string presetName)
    {
        Vector3 rotation = presetName switch
        {
            "Forward" => new Vector3(0, 0, 0),
            "Up" => new Vector3(-90, 0, 0),
            "Right" => new Vector3(0, -90, 0),
            "Down" => new Vector3(90, 0, 0),
            _ => Vector3.zero
        };

        SetAttachRotation(rotation);
        Debug.Log($"[VRGunGripFix] Preset aplicado: {presetName} = {rotation}");
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos || attachPoint == null) return;

        // Dibujar punto de agarre
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(attachPoint.position, 0.03f);

        // Dibujar dirección del agarre
        Gizmos.color = Color.red;
        Gizmos.DrawLine(attachPoint.position, attachPoint.position + attachPoint.forward * 0.15f);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(attachPoint.position, attachPoint.position + attachPoint.up * 0.1f);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(attachPoint.position, attachPoint.position + attachPoint.right * 0.1f);

#if UNITY_EDITOR
        // Label
        UnityEditor.Handles.Label(attachPoint.position + Vector3.up * 0.05f, "Grip Point");
#endif
    }

#if UNITY_EDITOR
    // Botones en el Inspector para testing rápido
    [ContextMenu("Test: Rotate Forward (0,0,0)")]
    void TestForward() => ApplyRotationPreset("Forward");
    
    [ContextMenu("Test: Rotate Up (-90,0,0)")]
    void TestUp() => ApplyRotationPreset("Up");
    
    [ContextMenu("Test: Rotate Right (0,-90,0)")]
    void TestRight() => ApplyRotationPreset("Right");
    
    [ContextMenu("Test: Move Closer to Body")]
    void TestCloser() => SetAttachPosition(attachPointLocalPosition + Vector3.back * 0.05f);
    
    [ContextMenu("Test: Move Further from Body")]
    void TestFurther() => SetAttachPosition(attachPointLocalPosition + Vector3.forward * 0.05f);
#endif
}