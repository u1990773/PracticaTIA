using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// VERSIÓN ARREGLADA - Simplificado porque el Player legacy ahora es hijo del XR Origin.
/// Este script ahora es OPCIONAL y puede desactivarse si no necesitas sincronización adicional.
/// </summary>
public class VRPlayerSync : MonoBehaviour
{
    [Header("NOTA: Este script es OPCIONAL")]
    [Tooltip("Si VRBootstrapLoader hace el Player hijo del XR Origin, este script NO es necesario.")]
    [SerializeField] private bool enableSync = false; // ⭐ Desactivado por defecto

    [Header("References (si enableSync = true)")]
    public XROrigin xrOrigin;
    public Transform legacyPlayer;

    [Header("Settings")]
    public bool lockY = true;
    public bool useCharacterController = false; // ⭐ Desactivado porque ya no hay CC en legacy player

    private CharacterController legacyCC;

    void Start()
    {
        if (!enableSync)
        {
            Debug.Log("[VRPlayerSync] Sync desactivado. El Player legacy ya es hijo del XR Origin.");
            enabled = false;
            return;
        }

        if (xrOrigin == null) xrOrigin = FindObjectOfType<XROrigin>(true);

        if (legacyPlayer == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null) legacyPlayer = go.transform;
        }

        if (legacyPlayer != null && useCharacterController)
            legacyCC = legacyPlayer.GetComponent<CharacterController>();

        Debug.Log("[VRPlayerSync] Sync activado manualmente. Esto puede causar conflictos.");
    }

    void LateUpdate()
    {
        if (!enableSync) return;

        if (legacyPlayer == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null)
            {
                legacyPlayer = go.transform;
                if (useCharacterController)
                    legacyCC = go.GetComponent<CharacterController>();
                Debug.Log("[VRPlayerSync] Legacy Player encontrado: " + go.name);
            }
        }

        if (xrOrigin == null || xrOrigin.Camera == null || legacyPlayer == null) return;

        // ⭐ IMPORTANTE: Si el Player es hijo del XR Origin, este código no debería ejecutarse
        if (legacyPlayer.parent == xrOrigin.transform)
        {
            Debug.LogWarning("[VRPlayerSync] Player legacy es hijo de XR Origin. Desactiva este script.");
            enabled = false;
            return;
        }

        var headPos = xrOrigin.Camera.transform.position;

        if (lockY) headPos.y = legacyPlayer.position.y;

        var delta = headPos - legacyPlayer.position;

        if (legacyCC != null && legacyCC.enabled && useCharacterController)
            legacyCC.Move(delta);
        else
            legacyPlayer.position = headPos;

        // Rotación solo en Y
        var yaw = xrOrigin.Camera.transform.eulerAngles.y;
        legacyPlayer.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}