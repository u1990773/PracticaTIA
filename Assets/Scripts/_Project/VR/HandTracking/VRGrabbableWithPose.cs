using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Componente para objetos grabbables que aplican una pose específica al ser agarrados.
/// Ejemplo: pistola usa "grip pose", linterna usa "flashlight pose".
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class VRGrabbableWithPose : MonoBehaviour
{
    [Header("Pose Settings")]
    [SerializeField] private HandPose leftHandPose;
    [SerializeField] private HandPose rightHandPose;
    [SerializeField] private bool useSamePoseForBothHands = true;

    [Header("Auto-find System")]
    [SerializeField] private bool autoFindPoseSystem = true;
    [SerializeField] private VRHandPoseSystem poseSystem;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (autoFindPoseSystem && poseSystem == null)
        {
            poseSystem = FindObjectOfType<VRHandPoseSystem>();
        }

        // Si usamos la misma pose para ambas manos, copiar
        if (useSamePoseForBothHands && leftHandPose != null && rightHandPose == null)
        {
            rightHandPose = leftHandPose;
        }
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (poseSystem == null)
        {
            Debug.LogWarning($"[VRGrabbableWithPose] No pose system found for {gameObject.name}");
            return;
        }

        // Determinar qué mano agarró el objeto
        bool isLeftHand = IsLeftHand(args.interactorObject);
        HandPose poseToApply = isLeftHand ? leftHandPose : rightHandPose;

        if (poseToApply != null)
        {
            poseSystem.ApplyPose(isLeftHand, poseToApply);

            // Haptic feedback
            if (VRHapticsManager.Instance != null)
            {
                var controller = GetControllerFromInteractor(args.interactorObject);
                if (controller != null)
                    VRHapticsManager.Instance.SendLightTap(controller);
            }
        }
        else
        {
            Debug.LogWarning($"[VRGrabbableWithPose] No pose defined for {(isLeftHand ? "left" : "right")} hand on {gameObject.name}");
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (poseSystem == null) return;

        // Reset pose cuando se suelta
        bool isLeftHand = IsLeftHand(args.interactorObject);
        poseSystem.ResetPose(isLeftHand);
    }

    #region Helper Methods

    private bool IsLeftHand(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor)
    {
        if (interactor is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor controllerInteractor)
        {
            string name = controllerInteractor.name.ToLower();
            return name.Contains("left");
        }
        return false;
    }

    private ActionBasedController GetControllerFromInteractor(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor)
    {
        if (interactor is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor controllerInteractor)
        {
            return controllerInteractor.GetComponent<ActionBasedController>();
        }
        return null;
    }

    #endregion
}