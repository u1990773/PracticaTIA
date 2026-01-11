using UnityEngine;

/// <summary>
/// Bloquea inputs que causan problemas en VR (Shift, Space, etc).
/// Añade este script al Player legacy para evitar conflictos.
/// </summary>
public class VRMovementInputBlocker : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool blockShift = true; // Sprint
    [SerializeField] private bool blockSpace = true; // Jump
    [SerializeField] private bool blockWASD = true;  // Movimiento legacy

    [Header("Debug")]
    [SerializeField] private bool logBlockedInputs = false;

    private PlayerMovementQ playerMovement;

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovementQ>();

        if (playerMovement == null)
        {
            Debug.LogError("[VRMovementInputBlocker] No se encontró PlayerMovementQ.");
            enabled = false;
            return;
        }

        // Asegurar que vrMode está activado
        if (!playerMovement.vrMode)
        {
            playerMovement.vrMode = true;
            Debug.Log("[VRMovementInputBlocker] vrMode activado automáticamente.");
        }

        Debug.Log("[VRMovementInputBlocker] Inputs bloqueados para VR.");
    }

    private void Update()
    {
        if (playerMovement == null || !playerMovement.vrMode) return;

        // Bloquear Shift (Sprint)
        if (blockShift && Input.GetKey(KeyCode.LeftShift))
        {
            if (logBlockedInputs)
                Debug.Log("[VRMovementInputBlocker] Shift bloqueado");
            // No hacemos nada, simplemente no procesamos el sprint
        }

        // Bloquear Space (Jump)
        if (blockSpace && Input.GetKeyDown(KeyCode.Space))
        {
            if (logBlockedInputs)
                Debug.Log("[VRMovementInputBlocker] Space bloqueado");
            // El input se consume pero no hace nada
        }
    }

    /// <summary>
    /// Verifica si un input está bloqueado.
    /// </summary>
    public bool IsInputBlocked(KeyCode key)
    {
        if (!playerMovement.vrMode) return false;

        if (key == KeyCode.LeftShift && blockShift) return true;
        if (key == KeyCode.Space && blockSpace) return true;

        return false;
    }
}