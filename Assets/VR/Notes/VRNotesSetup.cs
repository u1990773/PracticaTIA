using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VRNotesSetup : MonoBehaviour
{
    [Tooltip("Si está activo, apaga el texto 'Pulsa E' de la nota (si existe).")]
    public bool disablePromptText = true;

    public void SetupAllNotes()
    {
        var notes = FindObjectsOfType<Nota>(true);

        foreach (var note in notes)
            SetupOne(note);

        Debug.Log($"[VRNotesSetup] Notas configuradas para VR: {notes.Length}");
    }

    private void SetupOne(Nota note)
    {
        if (note == null) return;

        var go = note.gameObject;

        // 1) Apagar el prompt (si lo usa)
        if (disablePromptText && note.textoInteractuar != null)
            note.textoInteractuar.enabled = false;

        // 2) Asegurar colliders (XRI necesita colliders para interactuar)
        var colliders = go.GetComponentsInChildren<Collider>(true);
        if (colliders == null || colliders.Length == 0)
        {
            var bc = go.AddComponent<BoxCollider>();
            colliders = new Collider[] { bc };
        }

        // Para que no dependa del trigger de "cercaDelJugador"
        foreach (var c in colliders)
            c.isTrigger = false;

        // 3) Rigidbody (para XR Grab)
        var rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // 4) XRGrabInteractable
        var grab = go.GetComponent<XRGrabInteractable>();
        if (grab == null) grab = go.AddComponent<XRGrabInteractable>();

        grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        grab.throwOnDetach = false;  
        grab.trackPosition = true;
        grab.trackRotation = true;

        // Si la lista de colliders del interactable está vacía, la rellenamos
        if (grab.colliders.Count == 0)
        {
            foreach (var c in colliders)
                grab.colliders.Add(c);
        }

        // 5) Adaptador: al agarrar -> RecogerNota()
        var adapter = go.GetComponent<VRCollectNoteOnGrab>();
        if (adapter == null) adapter = go.AddComponent<VRCollectNoteOnGrab>();
        adapter.nota = note;
    }
}
