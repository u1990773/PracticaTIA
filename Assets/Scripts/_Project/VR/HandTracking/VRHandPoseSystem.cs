using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Sistema de poses de mano para objetos VR.
/// Permite definir diferentes poses al agarrar objetos (pistola, linterna, etc).
/// </summary>
public class VRHandPoseSystem : MonoBehaviour
{
    [Header("Hand Models")]
    [SerializeField] private GameObject leftHandModel;
    [SerializeField] private GameObject rightHandModel;

    [Header("Auto-find Hand Models")]
    [SerializeField] private bool autoFindHands = true;

    private void Start()
    {
        if (autoFindHands)
        {
            // Buscar modelos de mano en XR Origin
            if (leftHandModel == null)
            {
                var hands = GetComponentsInChildren<Transform>(true);
                foreach (var hand in hands)
                {
                    if (hand.name.ToLower().Contains("left") && hand.name.ToLower().Contains("hand"))
                    {
                        leftHandModel = hand.gameObject;
                        break;
                    }
                }
            }

            if (rightHandModel == null)
            {
                var hands = GetComponentsInChildren<Transform>(true);
                foreach (var hand in hands)
                {
                    if (hand.name.ToLower().Contains("right") && hand.name.ToLower().Contains("hand"))
                    {
                        rightHandModel = hand.gameObject;
                        break;
                    }
                }
            }
        }

        Debug.Log($"[VRHandPoseSystem] Initialized. Left: {leftHandModel != null}, Right: {rightHandModel != null}");
    }

    /// <summary>
    /// Aplica una pose a la mano especificada.
    /// </summary>
    public void ApplyPose(bool isLeftHand, HandPose pose)
    {
        GameObject handModel = isLeftHand ? leftHandModel : rightHandModel;

        if (handModel == null)
        {
            Debug.LogWarning($"[VRHandPoseSystem] No hand model found for {(isLeftHand ? "left" : "right")} hand.");
            return;
        }

        if (pose == null)
        {
            Debug.LogWarning("[VRHandPoseSystem] Pose is null.");
            return;
        }

        // Aplicar la pose
        pose.ApplyToHand(handModel.transform);

        Debug.Log($"[VRHandPoseSystem] Applied pose '{pose.poseName}' to {(isLeftHand ? "left" : "right")} hand.");
    }

    /// <summary>
    /// Resetea la mano a su pose por defecto (abierta).
    /// </summary>
    public void ResetPose(bool isLeftHand)
    {
        GameObject handModel = isLeftHand ? leftHandModel : rightHandModel;

        if (handModel == null) return;

        // Aquí podrías implementar una pose por defecto
        // O simplemente dejar que la animación del controller tome el control

        Debug.Log($"[VRHandPoseSystem] Reset pose for {(isLeftHand ? "left" : "right")} hand.");
    }
}

/// <summary>
/// Define una pose de mano (posición/rotación de dedos).
/// </summary>
[CreateAssetMenu(fileName = "New Hand Pose", menuName = "VR/Hand Pose")]
public class HandPose : ScriptableObject
{
    public string poseName = "Default Pose";

    [Header("Thumb")]
    public Vector3 thumbRotation;

    [Header("Index")]
    public Vector3 indexRotation;

    [Header("Middle")]
    public Vector3 middleRotation;

    [Header("Ring")]
    public Vector3 ringRotation;

    [Header("Pinky")]
    public Vector3 pinkyRotation;

    [Header("Advanced: Full Finger Chain")]
    public FingerPose[] fingerPoses;

    /// <summary>
    /// Aplica esta pose a un transform de mano.
    /// Nota: Esta es una implementación simplificada.
    /// Para un sistema completo, necesitarías mapear cada hueso de cada dedo.
    /// </summary>
    public void ApplyToHand(Transform handRoot)
    {
        // Implementación básica: buscar bones por nombre
        ApplyFingerRotation(handRoot, "Thumb", thumbRotation);
        ApplyFingerRotation(handRoot, "Index", indexRotation);
        ApplyFingerRotation(handRoot, "Middle", middleRotation);
        ApplyFingerRotation(handRoot, "Ring", ringRotation);
        ApplyFingerRotation(handRoot, "Pinky", pinkyRotation);

        // Si tienes poses avanzadas definidas
        if (fingerPoses != null && fingerPoses.Length > 0)
        {
            foreach (var fingerPose in fingerPoses)
            {
                var bone = FindBoneRecursive(handRoot, fingerPose.boneName);
                if (bone != null)
                {
                    bone.localRotation = Quaternion.Euler(fingerPose.rotation);
                }
            }
        }
    }

    private void ApplyFingerRotation(Transform handRoot, string fingerName, Vector3 rotation)
    {
        var finger = FindBoneRecursive(handRoot, fingerName);
        if (finger != null)
        {
            finger.localRotation = Quaternion.Euler(rotation);
        }
    }

    private Transform FindBoneRecursive(Transform parent, string boneName)
    {
        if (parent.name.Contains(boneName))
            return parent;

        foreach (Transform child in parent)
        {
            var result = FindBoneRecursive(child, boneName);
            if (result != null)
                return result;
        }

        return null;
    }
}

/// <summary>
/// Pose detallada para un hueso específico.
/// </summary>
[System.Serializable]
public struct FingerPose
{
    public string boneName;
    public Vector3 rotation;
}