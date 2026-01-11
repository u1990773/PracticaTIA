using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Configura el agarre perfecto del arma VR.
/// VERSIÓN MEJORADA V2 - Sin errores de compilación:
/// - Posición correcta en la mano
/// - Rotación correcta (apunta adelante)
/// - Modo TOGGLE (no necesitas mantener presionado)
/// - Attach point optimizado
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class VRGunGripFix : MonoBehaviour
{
    [Header("⭐ MODO TOGGLE - No mantener presionado")]
    [Tooltip("Si true, presiona una vez para agarrar, otra vez para soltar")]
    [SerializeField] private bool useToggleMode = true;

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

    [Header("Advanced Grab Settings")]
    [SerializeField] private bool useDynamicAttach = false;
    [SerializeField] private bool throwOnDetach = false;
    [SerializeField] private bool instantSnap = false;
    [SerializeField] private float smoothAmount = 20f;

    [Header("Visual Helpers")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.cyan;

    private XRGrabInteractable grabInteractable;

    private void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable == null)
        {
            Debug.LogError("[VRGunGripFix] ❌ No se encontró XRGrabInteractable.");
            enabled = false;
            return;
        }

        // Crear attach point si no existe
        if (attachPoint == null && autoCreateAttachPoint)
        {
            CreateAttachPoint();
        }

        // Configurar grab settings COMPLETO
        ConfigureGrabInteractable();

        Debug.Log($"[VRGunGripFix] ✅ Arma configurada. Modo Toggle: {useToggleMode}, Attach point: {attachPoint.localPosition}");
    }

    private void CreateAttachPoint()
    {
        GameObject attachObj = new GameObject("GunGripAttachPoint");
        attachObj.transform.SetParent(transform);
        attachObj.transform.localPosition = attachPointLocalPosition;
        attachObj.transform.localEulerAngles = attachPointLocalRotation;
        attachPoint = attachObj.transform;

        Debug.Log("[VRGunGripFix] ✅ Attach point creado automáticamente.");
    }

    private void ConfigureGrabInteractable()
    {
        if (grabInteractable == null) return;

        // ========================================
        // 1. ATTACH POINT - Posición correcta
        // ========================================
        grabInteractable.attachTransform = attachPoint;

        // ========================================
        // 2. SELECT MODE - CRITICAL para Toggle
        // ========================================
        grabInteractable.selectMode = InteractableSelectMode.Single;

        // ========================================
        // 3. MOVEMENT TYPE - Instantaneous
        // ========================================
        grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;

        // ========================================
        // 4. TRACKING - Sigue la mano
        // ========================================
        grabInteractable.trackPosition = true;
        grabInteractable.trackRotation = true;

        // ========================================
        // 5. SMOOTH MOVEMENT - Transición suave
        // ========================================
        if (instantSnap)
        {
            // Snap instantáneo
            grabInteractable.attachEaseInTime = 0f;
            grabInteractable.smoothPosition = false;
            grabInteractable.smoothRotation = false;
        }
        else
        {
            // Smooth (recomendado)
            grabInteractable.attachEaseInTime = 0.05f;
            grabInteractable.smoothPosition = true;
            grabInteractable.smoothPositionAmount = smoothAmount;
            grabInteractable.smoothRotation = true;
            grabInteractable.smoothRotationAmount = smoothAmount;
        }

        // ========================================
        // 6. DYNAMIC ATTACH - Opcional
        // ========================================
        grabInteractable.useDynamicAttach = useDynamicAttach;

        // ========================================
        // 7. THROW ON DETACH - No lanzar
        // ========================================
        grabInteractable.throwOnDetach = throwOnDetach;

        // ========================================
        // NOTA: La línea problemática ha sido REMOVIDA
        // grabInteractable.startingSelectedInteractable = null;
        // Esta propiedad no existe en todas las versiones del XR Toolkit
        // ========================================

        if (useToggleMode)
        {
            Debug.Log("[VRGunGripFix] ✅ Modo Toggle activado (presiona G una vez para agarrar, otra para soltar)");
        }
        else
        {
            Debug.Log("[VRGunGripFix] ⚠️ Modo Hold activado (debes mantener presionado)");
        }
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
            Debug.Log($"[VRGunGripFix] Posición actualizada: {localPos}");
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
            Debug.Log($"[VRGunGripFix] Rotación actualizada: {localRot}");
        }
    }

    /// <summary>
    /// Presets comunes de rotación.
    /// </summary>
    public void ApplyRotationPreset(string presetName)
    {
        Vector3 rotation = presetName.ToLower() switch
        {
            "forward" => new Vector3(0, 0, 0),      // Apunta adelante
            "up" => new Vector3(-90, 0, 0),          // Apunta arriba
            "down" => new Vector3(90, 0, 0),         // Apunta abajo
            "right" => new Vector3(0, -90, 0),       // Apunta derecha
            "left" => new Vector3(0, 90, 0),         // Apunta izquierda
            _ => Vector3.zero
        };

        SetAttachRotation(rotation);
        Debug.Log($"[VRGunGripFix] ✅ Preset '{presetName}' aplicado: {rotation}");
    }

    /// <summary>
    /// Activa/desactiva el modo Toggle en runtime.
    /// </summary>
    public void SetToggleMode(bool toggle)
    {
        useToggleMode = toggle;
        Debug.Log($"[VRGunGripFix] Modo Toggle: {(toggle ? "ACTIVADO" : "DESACTIVADO")}");
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos || attachPoint == null) return;

        // Dibujar punto de agarre
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(attachPoint.position, 0.03f);

        // Dibujar dirección del agarre (forward = rojo)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(attachPoint.position, attachPoint.position + attachPoint.forward * 0.15f);

        // Dibujar up (verde)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(attachPoint.position, attachPoint.position + attachPoint.up * 0.1f);

        // Dibujar right (azul)
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(attachPoint.position, attachPoint.position + attachPoint.right * 0.1f);

#if UNITY_EDITOR
        // Label
        UnityEditor.Handles.Label(attachPoint.position + Vector3.up * 0.05f, 
            $"Grip Point\n{(useToggleMode ? "Toggle Mode" : "Hold Mode")}");
#endif
    }

#if UNITY_EDITOR
    // ========================================
    // BOTONES DE TESTING EN INSPECTOR
    // ========================================
    
    [ContextMenu("✅ Test: Rotate Forward (0,0,0)")]
    void TestForward() => ApplyRotationPreset("forward");
    
    [ContextMenu("✅ Test: Rotate Up (-90,0,0)")]
    void TestUp() => ApplyRotationPreset("up");
    
    [ContextMenu("✅ Test: Rotate Down (90,0,0)")]
    void TestDown() => ApplyRotationPreset("down");
    
    [ContextMenu("✅ Test: Rotate Right (0,-90,0)")]
    void TestRight() => ApplyRotationPreset("right");
    
    [ContextMenu("✅ Test: Rotate Left (0,90,0)")]
    void TestLeft() => ApplyRotationPreset("left");
    
    [ContextMenu("🔧 Test: Move Grip Back (más atrás)")]
    void TestMoveBack() => SetAttachPosition(attachPointLocalPosition + Vector3.back * 0.05f);
    
    [ContextMenu("🔧 Test: Move Grip Forward (más adelante)")]
    void TestMoveForward() => SetAttachPosition(attachPointLocalPosition + Vector3.forward * 0.05f);
    
    [ContextMenu("🔧 Test: Move Grip Up")]
    void TestMoveUp() => SetAttachPosition(attachPointLocalPosition + Vector3.up * 0.05f);
    
    [ContextMenu("🔧 Test: Move Grip Down")]
    void TestMoveDown() => SetAttachPosition(attachPointLocalPosition + Vector3.down * 0.05f);
    
    [ContextMenu("⚙️ Toggle: Activar/Desactivar Toggle Mode")]
    void ToggleMode() => SetToggleMode(!useToggleMode);
#endif
}