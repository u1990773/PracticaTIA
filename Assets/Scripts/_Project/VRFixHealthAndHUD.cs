using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Script simple que desactiva HUD legacy y activa sistema de vida VR.
/// Añadir al XR Origin.
/// </summary>
public class VRFixHealthAndHUD : MonoBehaviour
{
    private void Start()
    {
        // Esperar 1 segundo a que todo cargue
        Invoke(nameof(ApplyFixes), 1f);
    }

    private void ApplyFixes()
    {
        // FIX 1: Desactivar TODOS los Canvas 2D legacy
        foreach (var canvas in FindObjectsOfType<Canvas>(true))
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay ||
                canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                canvas.gameObject.SetActive(false);
                Debug.Log($"[VRFix] Canvas legacy desactivado: {canvas.name}");
            }
        }

        // FIX 2: Reactivar PlayerMovementQ para que gestione vida
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var pm = player.GetComponent<PlayerMovementQ>();
            if (pm != null)
            {
                pm.enabled = true; // Reactivar para vida
                pm.vrMode = true;  // Mantener en modo VR (sin movimiento)
                Debug.Log("[VRFix] PlayerMovementQ reactivado para sistema de vida.");
            }
        }

        Debug.Log("[VRFix] Fixes aplicados correctamente.");
    }
}