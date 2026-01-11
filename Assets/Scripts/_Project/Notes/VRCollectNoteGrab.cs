using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Adaptador que conecta el sistema legacy de notas con VR.
/// Al agarrar una nota, muestra su contenido en la UI VR.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class VRCollectNoteOnGrab : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Nota nota; // Script legacy de la nota
    [SerializeField] private VRNoteUIManager uiManager;

    [Header("Settings")]
    [SerializeField] private bool autoFindUIManager = true;
    [SerializeField] private bool extractTextureFromMaterial = true;

    private XRGrabInteractable grabInteractable;
    private bool alreadyCollected = false;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Auto-encontrar la nota en este objeto
        if (nota == null)
            nota = GetComponent<Nota>();

        // Auto-encontrar el UI Manager
        if (autoFindUIManager && uiManager == null)
            uiManager = FindObjectOfType<VRNoteUIManager>();
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnNoteGrabbed);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnNoteGrabbed);
        }
    }

    private void OnNoteGrabbed(SelectEnterEventArgs args)
    {
        if (alreadyCollected)
        {
            Debug.LogWarning("[VRCollectNoteOnGrab] Esta nota ya fue recogida.");
            return;
        }

        if (nota == null)
        {
            Debug.LogError("[VRCollectNoteOnGrab] No hay referencia a Nota legacy.");
            return;
        }

        if (uiManager == null)
        {
            Debug.LogError("[VRCollectNoteOnGrab] No se encontró VRNoteUIManager.");
            return;
        }

        // Extraer textura del material (si existe)
        Texture noteTexture = null;
        if (extractTextureFromMaterial)
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                // Intentar _BaseMap (URP) o _MainTex (Built-in)
                noteTexture = renderer.material.GetTexture("_BaseMap");
                if (noteTexture == null)
                    noteTexture = renderer.material.GetTexture("_MainTex");
            }
        }

        // Mostrar la nota en UI
        string mensaje = nota.mensaje;
        uiManager.ShowNote(mensaje, noteTexture, OnConfirmCollect);

        Debug.Log($"[VRCollectNoteOnGrab] Nota agarrada: {mensaje.Substring(0, Mathf.Min(30, mensaje.Length))}...");
    }

    /// <summary>
    /// Callback cuando el jugador confirma recoger la nota.
    /// </summary>
    private void OnConfirmCollect()
    {
        if (alreadyCollected) return;

        alreadyCollected = true;

        // Añadir al diario
        uiManager.AddToJournal(nota.mensaje);

        // Llamar al método legacy de recoger (suma contador, cura, destruye)
        nota.RecogerNota();

        Debug.Log("[VRCollectNoteOnGrab] Nota recogida y guardada en diario.");

        // La nota se destruye en RecogerNota(), así que este script también se destruye
    }
}