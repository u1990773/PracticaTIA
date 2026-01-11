using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

/// <summary>
/// SOLUCIÓN DEFINITIVA V5: Toggle con protección contra detección prematura.
/// 
/// Arregla el bug donde detectaba el agarre inicial como "toggle off".
/// 
/// REEMPLAZA completamente tu VRGunGripFix.cs actual.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class VRGunGripFix : MonoBehaviour
{
    [Header("⭐ MODO TOGGLE FORZADO (Re-Grab V2)")]
    [SerializeField] private bool useToggleMode = true;

    [Header("Attach Point Configuration")]
    [SerializeField] private Transform attachPoint;
    [SerializeField] private bool autoCreateAttachPoint = true;

    [Header("Position (relativo al arma)")]
    [SerializeField] private Vector3 attachPointLocalPosition = new Vector3(0, -0.05f, -0.15f);

    [Header("Rotation (para que apunte adelante)")]
    [SerializeField] private Vector3 attachPointLocalRotation = new Vector3(0, 0, 0);

    [Header("Grab Settings")]
    [SerializeField] private float smoothAmount = 20f;

    [Header("Toggle Settings")]
    [Tooltip("Frames a esperar después de agarrar antes de permitir toggle")]
    [SerializeField] private int toggleCooldownFrames = 5;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private XRGrabInteractable grabInteractable;
    private IXRSelectInteractor lastInteractor;
    private bool isManuallyGrabbed = false;
    private bool allowRelease = false;
    private bool isReGrabbing = false;

    // Input detection
    private bool wasGripPressed = false;

    // Cooldown para evitar toggle inmediato al agarrar
    private int framesSinceGrab = 999;

    private void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable == null)
        {
            Debug.LogError("[VRGunGripFix] No XRGrabInteractable found!");
            enabled = false;
            return;
        }

        if (attachPoint == null && autoCreateAttachPoint)
        {
            CreateAttachPoint();
        }

        ConfigureGrabInteractable();

        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);

        if (showDebugLogs)
            Debug.Log($"[VRGunGripFix] Toggle Mode FORZADO (V5) activado");
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void Update()
    {
        if (!useToggleMode) return;

        // Incrementar contador de frames
        if (isManuallyGrabbed)
        {
            framesSinceGrab++;
        }

        // Detectar input
        bool gripPressed = DetectGripInput();
        bool gripJustPressed = gripPressed && !wasGripPressed;

        // Solo procesar toggle si:
        // 1. El arma está agarrada
        // 2. Han pasado suficientes frames desde el agarre
        // 3. El usuario acaba de presionar el botón
        if (gripJustPressed && isManuallyGrabbed && framesSinceGrab >= toggleCooldownFrames)
        {
            if (showDebugLogs)
                Debug.Log("[VRGunGripFix] Toggle OFF - Usuario quiere soltar");

            allowRelease = true;
            ForceRelease();
        }

        wasGripPressed = gripPressed;
    }

    private bool DetectGripInput()
    {
#if UNITY_EDITOR
        return Input.GetKey(KeyCode.G);
#else
        return false;
#endif
    }

    private void CreateAttachPoint()
    {
        GameObject attachObj = new GameObject("GunGripAttachPoint");
        attachObj.transform.SetParent(transform);
        attachObj.transform.localPosition = attachPointLocalPosition;
        attachObj.transform.localEulerAngles = attachPointLocalRotation;
        attachPoint = attachObj.transform;

        if (showDebugLogs)
            Debug.Log("[VRGunGripFix] Attach point creado");
    }

    private void ConfigureGrabInteractable()
    {
        grabInteractable.attachTransform = attachPoint;
        grabInteractable.selectMode = InteractableSelectMode.Single;
        grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grabInteractable.trackPosition = true;
        grabInteractable.trackRotation = true;
        grabInteractable.attachEaseInTime = 0.05f;
        grabInteractable.smoothPosition = true;
        grabInteractable.smoothPositionAmount = smoothAmount;
        grabInteractable.smoothRotation = true;
        grabInteractable.smoothRotationAmount = smoothAmount;
        grabInteractable.throwOnDetach = false;
        grabInteractable.useDynamicAttach = false;
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        lastInteractor = args.interactorObject;

        if (!isReGrabbing)
        {
            // Agarre inicial del usuario
            isManuallyGrabbed = true;
            allowRelease = false;
            framesSinceGrab = 0; // CRÍTICO: Resetear contador

            if (showDebugLogs)
                Debug.Log("[VRGunGripFix] Arma AGARRADA (Toggle ON)");
        }
        else
        {
            // Re-grab automático
            if (showDebugLogs)
                Debug.Log("[VRGunGripFix] Re-grab automático");
        }

        isReGrabbing = false;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (allowRelease)
        {
            // Release permitido (usuario quiere soltar)
            isManuallyGrabbed = false;
            allowRelease = false;
            lastInteractor = null;
            framesSinceGrab = 999;

            if (showDebugLogs)
                Debug.Log("[VRGunGripFix] Arma SOLTADA (Toggle OFF)");
        }
        else if (isManuallyGrabbed && useToggleMode)
        {
            // Release NO permitido → Re-agarrar
            if (showDebugLogs)
                Debug.Log("[VRGunGripFix] Release no permitido - RE-AGARRANDO");

            StartCoroutine(ReGrabAfterFrame(args.interactorObject));
        }
    }

    private IEnumerator ReGrabAfterFrame(IXRSelectInteractor interactor)
    {
        yield return null;

        if (!isManuallyGrabbed) yield break;
        if (interactor == null) yield break;

        isReGrabbing = true;

        var interactionManager = grabInteractable.interactionManager;
        if (interactionManager != null)
        {
            interactionManager.SelectEnter(interactor, grabInteractable);

            if (showDebugLogs)
                Debug.Log("[VRGunGripFix] RE-GRAB exitoso");
        }
    }

    private void ForceRelease()
    {
        if (lastInteractor == null) return;

        var interactionManager = grabInteractable.interactionManager;
        if (interactionManager != null)
        {
            interactionManager.SelectExit(lastInteractor, grabInteractable);

            if (showDebugLogs)
                Debug.Log("[VRGunGripFix] Release FORZADO");
        }
    }

    public void SetAttachPosition(Vector3 localPos)
    {
        if (attachPoint != null)
        {
            attachPoint.localPosition = localPos;
            attachPointLocalPosition = localPos;
        }
    }

    public void SetAttachRotation(Vector3 localRot)
    {
        if (attachPoint != null)
        {
            attachPoint.localEulerAngles = localRot;
            attachPointLocalRotation = localRot;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attachPoint == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(attachPoint.position, 0.03f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(attachPoint.position, attachPoint.position + attachPoint.forward * 0.15f);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(attachPoint.position, attachPoint.position + attachPoint.up * 0.1f);
    }
}