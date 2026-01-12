using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class Nota : MonoBehaviour
{
    [Header("Legacy Settings")]
    public string mensaje;
    public TextMeshProUGUI textoInteractuar;

    [Header("VR Settings")]
    [SerializeField] private bool vrMode = false;

    private bool cercaDelJugador = false;
    private PlayerMovementQ jugador;
    private bool alreadyCollected = false;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    void Start()
    {
        jugador = FindObjectOfType<PlayerMovementQ>();

        // Detectar VR
        if (jugador != null && jugador.vrMode)
        {
            vrMode = true;

            if (textoInteractuar != null)
                textoInteractuar.gameObject.SetActive(false);

            SetupVRGrab();
        }
    }

    private void SetupVRGrab()
    {
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab == null)
        {
            grab = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            grab.throwOnDetach = false;
        }

        // ASIGNAR INTERACTION MANAGER AUTOMÁTICAMENTE
        XRInteractionManager manager = FindObjectOfType<XRInteractionManager>();
        if (manager != null)
        {
            grab.interactionManager = manager;
        }
        else
        {
            Debug.LogError("[Nota] No se encontró XRInteractionManager en la escena");
        }

        // Asignar collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            grab.colliders.Clear();
            grab.colliders.Add(col);
        }

        grab.selectEntered.AddListener(OnGrabbed);
    }


    private void Update()
    {
        // Legacy: recoger con E
        if (!vrMode && cercaDelJugador && Input.GetKeyDown(KeyCode.E))
        {
            RecogerNota();
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (alreadyCollected) return;
        RecogerNota();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (alreadyCollected || vrMode) return;

        if (other.CompareTag("Player"))
        {
            cercaDelJugador = true;

            if (textoInteractuar != null)
                textoInteractuar.enabled = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (vrMode) return;

        if (other.CompareTag("Player"))
        {
            cercaDelJugador = false;

            if (textoInteractuar != null)
                textoInteractuar.enabled = false;
        }
    }

    public void RecogerNota()
    {
        if (alreadyCollected) return;
        alreadyCollected = true;

        Debug.Log($"[Nota] Recogida: {mensaje}");

        // Sumar al contador legacy
        if (jugador != null)
        {
            jugador.notasRecogidas++;
        }

        // Notificar al Game Manager VR
        if (VRGameManager.Instance != null)
        {
            VRGameManager.Instance.OnNoteCollected();
        }

        // Haptics VR
        if (vrMode && VRHapticsManager.Instance != null)
        {
            VRHapticsManager.Instance.SendMediumBumpBoth();
        }

        Destroy(gameObject);
    }
}
