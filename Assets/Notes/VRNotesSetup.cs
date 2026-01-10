using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Convierte automáticamente todas las notas legacy en notas VR grabbables.
/// Ejecutado por VRBootstrapLoader después de cargar la escena Main.
/// </summary>
public class VRNotesSetup : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool disableLegacyInteraction = true;
    [SerializeField] private bool hideLegacyPrompts = true;
    [SerializeField] private LayerMask interactionLayer;

    [Header("Grab Settings")]
    [SerializeField] private bool throwOnDetach = false;
    [SerializeField] private float smoothingAmount = 5f;
    [SerializeField] private bool useDynamicAttach = true;

    [Header("Debug")]
    [SerializeField] private bool logSetup = true;

    /// <summary>
    /// Busca todas las notas en la escena y las convierte a VR.
    /// </summary>
    public void SetupAllNotes()
    {
        Nota[] allNotes = FindObjectsOfType<Nota>(true);

        if (allNotes.Length == 0)
        {
            Debug.LogWarning("[VRNotesSetup] No se encontraron notas en la escena.");
            return;
        }

        int setupCount = 0;

        foreach (Nota note in allNotes)
        {
            if (SetupSingleNote(note))
                setupCount++;
        }

        if (logSetup)
            Debug.Log($"[VRNotesSetup] {setupCount}/{allNotes.Length} notas convertidas a VR.");
    }

    /// <summary>
    /// Configura una nota individual para VR.
    /// </summary>
    private bool SetupSingleNote(Nota note)
    {
        if (note == null) return false;

        GameObject noteObj = note.gameObject;

        // 1) Ocultar prompts legacy (texto "Press E to collect")
        if (hideLegacyPrompts)
        {
            var textInteract = noteObj.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (textInteract != null && textInteract.name.ToLower().Contains("interact"))
            {
                textInteract.gameObject.SetActive(false);
            }
        }

        // 2) Desactivar script legacy para que no responda a teclas
        if (disableLegacyInteraction)
        {
            note.enabled = false;
        }

        // 3) Asegurar Collider
        Collider col = noteObj.GetComponent<Collider>();
        if (col == null)
        {
            // Crear BoxCollider por defecto
            BoxCollider box = noteObj.AddComponent<BoxCollider>();

            // Intentar calcular tamaño basado en renderer
            Renderer rend = noteObj.GetComponent<Renderer>();
            if (rend != null)
            {
                box.size = rend.bounds.size;
                box.center = rend.bounds.center - noteObj.transform.position;
            }
            else
            {
                box.size = Vector3.one * 0.1f; // Tamaño por defecto
            }

            col = box;
        }

        // 4) Asegurar Rigidbody (kinematic para evitar física)
        Rigidbody rb = noteObj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = noteObj.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;

        // 5) Añadir XRGrabInteractable si no existe
        XRGrabInteractable grabInteractable = noteObj.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = noteObj.AddComponent<XRGrabInteractable>();
        }

        // Configurar XRGrabInteractable
        grabInteractable.throwOnDetach = throwOnDetach;
        grabInteractable.smoothPosition = true;
        grabInteractable.smoothPositionAmount = smoothingAmount;
        grabInteractable.smoothRotation = true;
        grabInteractable.smoothRotationAmount = smoothingAmount;
        grabInteractable.useDynamicAttach = useDynamicAttach;

        // Asignar capa de interacción si está configurada
        if (interactionLayer != 0)
        {
            noteObj.layer = LayerMaskToLayer(interactionLayer);
        }

        // 6) Añadir VRCollectNoteOnGrab si no existe
        VRCollectNoteOnGrab collectScript = noteObj.GetComponent<VRCollectNoteOnGrab>();
        if (collectScript == null)
        {
            collectScript = noteObj.AddComponent<VRCollectNoteOnGrab>();
        }

        if (logSetup)
            Debug.Log($"[VRNotesSetup] Nota '{noteObj.name}' configurada para VR.");

        return true;
    }

    /// <summary>
    /// Convierte un LayerMask al índice del primer layer activo.
    /// </summary>
    private int LayerMaskToLayer(LayerMask mask)
    {
        int layerNumber = 0;
        int layer = mask.value;
        while (layer > 1)
        {
            layer >>= 1;
            layerNumber++;
        }
        return layerNumber;
    }
}