using UnityEngine;
using TMPro;

/// <summary>
/// Script legacy de Nota, ahora compatible con VR.
/// Funciona tanto en modo legacy (tecla E) como en VR (tocar).
/// </summary>
public class Nota : MonoBehaviour
{
    [Header("Legacy Settings")]
    public string mensaje;
    public TextMeshProUGUI textoInteractuar;

    [Header("VR Settings")]
    [SerializeField] private bool vrMode = false; // Auto-detecta si está en VR
    [SerializeField] private bool autoCollectOnTouch = true; // En VR, recoger al tocar

    private bool cercaDelJugador = false;
    private PlayerMovementQ jugador;
    private bool alreadyCollected = false;

    void Start()
    {
        jugador = FindObjectOfType<PlayerMovementQ>();

        // Auto-detectar VR mode
        if (jugador != null && jugador.vrMode)
        {
            vrMode = true;

            // Desactivar texto de interacción en VR
            if (textoInteractuar != null)
            {
                textoInteractuar.gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        // Solo procesar tecla E si NO está en VR
        if (!vrMode && cercaDelJugador && Input.GetKeyDown(KeyCode.E))
        {
            RecogerNota();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (alreadyCollected) return;

        if (other.CompareTag("Player"))
        {
            cercaDelJugador = true;

            // Legacy mode: mostrar texto "Press E"
            if (!vrMode && textoInteractuar != null)
            {
                textoInteractuar.enabled = true;
            }

            // VR mode: recoger automáticamente al tocar
            if (vrMode && autoCollectOnTouch)
            {
                RecogerNota();
            }
        }

        // En VR, también puede ser el XR Origin/controllers
        if (vrMode && IsVRPlayer(other))
        {
            if (autoCollectOnTouch)
            {
                RecogerNota();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            cercaDelJugador = false;

            if (!vrMode && textoInteractuar != null)
            {
                textoInteractuar.enabled = false;
            }
        }
    }

    /// <summary>
    /// Método para recoger la nota.
    /// Compatible con VR y legacy.
    /// </summary>
    public void RecogerNota()
    {
        if (alreadyCollected) return;
        alreadyCollected = true;

        Debug.Log($"[Nota] Recogida: {mensaje.Substring(0, Mathf.Min(20, mensaje.Length))}...");

        // Incrementar contador legacy
        if (jugador != null)
        {
            jugador.notasRecogidas++;
        }

        // Notificar al Game Manager (VR)
        if (VRGameManager.Instance != null)
        {
            VRGameManager.Instance.OnNoteCollected();
        }

        // Haptic feedback en VR
        if (vrMode && VRHapticsManager.Instance != null)
        {
            VRHapticsManager.Instance.SendMediumBumpBoth();
        }

        // Destruir nota
        Destroy(gameObject);
    }

    /// <summary>
    /// Verifica si el collider es parte del sistema VR.
    /// </summary>
    private bool IsVRPlayer(Collider other)
    {
        string name = other.name.ToLower();

        // Por nombre
        if (name.Contains("origin") || name.Contains("controller") ||
            name.Contains("hand") || name.Contains("camera"))
            return true;

        // Por componente
        if (other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null)
            return true;

        return false;
    }

    /// <summary>
    /// Fuerza el modo VR (llamado por VRNotesSetup si se usa).
    /// </summary>
    public void SetVRMode(bool enable)
    {
        vrMode = enable;

        if (vrMode && textoInteractuar != null)
        {
            textoInteractuar.gameObject.SetActive(false);
        }
    }
}