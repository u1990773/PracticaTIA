using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class VRCollectNoteOnGrab : MonoBehaviour
{
    public Nota nota;

    [Header("Preview")]
    public bool showMaterialTexture = true;

    [Header("Anti auto-open at start")]
    public float ignoreOpenBeforeSeconds = 0.5f;

    private XRGrabInteractable _grab;
    private bool _opened;

    private void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _grab.selectEntered.AddListener(OnGrab);
    }

    private void OnDestroy()
    {
        if (_grab != null) _grab.selectEntered.RemoveListener(OnGrab);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        // Evita que se abra sola nada más cargar escena
        if (Time.timeSinceLevelLoad < ignoreOpenBeforeSeconds) return;

        if (_opened) return;
        _opened = true;

        var ui = FindObjectOfType<VRNoteUIManager>(true);
        if (ui == null)
        {
            // Si no hay UI, recoge directamente
            StartCoroutine(CollectNextFrame());
            return;
        }

        string msg = (nota != null) ? nota.mensaje : "(sin mensaje)"; // tu Nota usa 'mensaje' :contentReference[oaicite:1]{index=1}
        Texture tex = showMaterialTexture ? TryGetNoteTexture(gameObject) : null;

        ui.ShowNote(msg, tex, () =>
        {
            ui.AddToJournal(msg);
            StartCoroutine(CollectNextFrame());
        });
    }

    private IEnumerator CollectNextFrame()
    {
        yield return null;

        // Evita problemas al destruir mientras está "selected"
        if (_grab != null) _grab.enabled = false;

        if (nota != null)
            nota.RecogerNota(); // suma y Destroy :contentReference[oaicite:2]{index=2}
        else
            Destroy(gameObject);
    }

    private static Texture TryGetNoteTexture(GameObject noteGO)
    {
        var r = noteGO.GetComponentInChildren<Renderer>(true);
        if (r == null || r.sharedMaterial == null) return null;

        var mat = r.sharedMaterial;

        if (mat.HasProperty("_BaseMap")) return mat.GetTexture("_BaseMap"); // URP
        if (mat.HasProperty("_MainTex")) return mat.GetTexture("_MainTex"); // Built-in

        return null;
    }
}
