using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Adapta la UI legacy (Screen Space) a VR (World Space) y actualiza datos.
/// - Notas: VRGameManager.Instance (contador real)
/// - Ammo: VRGunWeapon (GetCurrentAmmo / GetMaxAmmo)
/// </summary>
public class VRHUDAdapter : MonoBehaviour
{
    [Header("Auto-Find Settings")]
    [SerializeField] private bool autoFindLegacyCanvas = true;
    [SerializeField] private string legacyCanvasName = "Canvas"; // Pon aquí el nombre real si tu canvas se llama distinto

    [Header("VR HUD Settings")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private float distanceFromCamera = 2f;
    [SerializeField] private float heightOffset = -0.5f;
    [SerializeField] private float hudScale = 0.001f;

    [Header("Follow Camera")]
    [SerializeField] private bool followCamera = true;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private bool lockYRotation = false;

    [Header("References (Auto-found)")]
    [SerializeField] private Camera vrCamera;
    [SerializeField] private Transform legacyPlayer;

    [Header("UI Elements (Auto-found)")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI notesText;

    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 0.1f;

    private float nextUpdateTime;
    private bool initialized;

    private void Start()
    {
        StartCoroutine(InitializeWhenReady());
    }

    /// <summary>
    /// Inicializa de forma robusta: reintenta por frames en vez de depender de 0.5s exactos.
    /// </summary>
    private IEnumerator InitializeWhenReady()
    {
        const int maxFrames = 300; // ~5s a 60fps

        for (int i = 0; i < maxFrames; i++)
        {
            if (vrCamera == null)
                vrCamera = Camera.main;

            if (legacyPlayer == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                    legacyPlayer = playerObj.transform;
            }

            if (autoFindLegacyCanvas && targetCanvas == null)
                targetCanvas = FindLegacyCanvas();

            if (targetCanvas != null)
                FindUIElements();

            if (targetCanvas != null && vrCamera != null)
            {
                ConvertCanvasToWorldSpace();
                initialized = true;
                ForceUpdate();
                Debug.Log("[VRHUDAdapter] Inicialización completada.");
                yield break;
            }

            yield return null;
        }

        Debug.LogWarning("[VRHUDAdapter] No se pudo inicializar: falta Canvas o Cámara (tras reintentos).");
    }

    private void Update()
    {
        if (!initialized) return;

        if (followCamera)
            UpdateHUDPosition();

        if (Time.time >= nextUpdateTime)
        {
            UpdateHUDData();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    #region Canvas + Position

    private Canvas FindLegacyCanvas()
    {
        // 1) Por nombre
        if (!string.IsNullOrEmpty(legacyCanvasName))
        {
            var canvasObj = GameObject.Find(legacyCanvasName);
            if (canvasObj != null)
            {
                var c = canvasObj.GetComponent<Canvas>();
                if (c != null) return c;
            }
        }

        // 2) Fallback: primer canvas ScreenSpace encontrado (incluye inactivos) - Unity 6
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay ||
                c.renderMode == RenderMode.ScreenSpaceCamera)
                return c;
        }

        return null;
    }

    private void ConvertCanvasToWorldSpace()
    {
        if (targetCanvas == null || vrCamera == null) return;

        targetCanvas.renderMode = RenderMode.WorldSpace;
        targetCanvas.worldCamera = vrCamera;

        targetCanvas.transform.localScale = Vector3.one * hudScale;
        UpdateHUDPosition(true);

        Debug.Log("[VRHUDAdapter] Canvas convertido a World Space.");
    }

    private void UpdateHUDPosition(bool instant = false)
    {
        if (targetCanvas == null || vrCamera == null) return;

        Vector3 forward = vrCamera.transform.forward;
        Vector3 desiredPos = vrCamera.transform.position + forward * distanceFromCamera;
        desiredPos.y += heightOffset;

        Quaternion desiredRot;
        if (lockYRotation)
        {
            Vector3 flatForward = new Vector3(forward.x, 0f, forward.z);
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = targetCanvas.transform.forward;
            desiredRot = Quaternion.LookRotation(flatForward.normalized);
        }
        else
        {
            desiredRot = Quaternion.LookRotation(forward);
        }

        if (instant)
        {
            targetCanvas.transform.position = desiredPos;
            targetCanvas.transform.rotation = desiredRot;
        }
        else
        {
            targetCanvas.transform.position = Vector3.Lerp(
                targetCanvas.transform.position,
                desiredPos,
                Time.deltaTime * followSpeed
            );

            targetCanvas.transform.rotation = Quaternion.Slerp(
                targetCanvas.transform.rotation,
                desiredRot,
                Time.deltaTime * followSpeed
            );
        }
    }

    #endregion

    #region Find UI

    private void FindUIElements()
    {
        if (targetCanvas == null) return;

        // Ajusta estos nombres si en tu Canvas se llaman diferente:
        // (por tus capturas / versiones anteriores suelen ser así)
        healthText = FindTextInCanvas("textSalut", healthText);
        ammoText = FindTextInCanvas("ammoText", ammoText);
        waveText = FindTextInCanvas("waveText", waveText);
        notesText = FindTextInCanvas("contadorNotas", notesText);
    }

    private TextMeshProUGUI FindTextInCanvas(string name, TextMeshProUGUI current)
    {
        if (current != null) return current;
        if (targetCanvas == null) return null;

        // intento 1: Find directo en hijos
        Transform t = targetCanvas.transform.Find(name);
        if (t != null)
        {
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp != null) return tmp;
        }

        // intento 2: buscar por nombre entre todos los TMP hijos
        var all = targetCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var tmp in all)
        {
            if (tmp != null && tmp.name == name)
                return tmp;
        }

        return null;
    }

    #endregion

    #region Update HUD Data

    private void UpdateHUDData()
    {
        UpdateHealth();
        UpdateAmmo();
        UpdateWave();   // (si lo usas)
        UpdateNotes();  // ✅ VRGameManager
    }

    private void UpdateHealth()
    {
        if (healthText == null || legacyPlayer == null) return;

        var playerMovement = legacyPlayer.GetComponent<PlayerMovementQ>();
        if (playerMovement == null) return;

        // Tu script original sacaba "vida" por reflection.
        // Lo mantengo igual para no romper tu código.
        try
        {
            var vidaField = playerMovement.GetType().GetField("vida");
            if (vidaField != null)
            {
                int vida = (int)vidaField.GetValue(playerMovement);
                healthText.text = $"HP: {vida}";
            }
        }
        catch
        {
            // Si falla, no hacemos nada.
        }
    }

    private void UpdateAmmo()
    {
        if (ammoText == null) return;

        // Si hay varias armas, preferimos la que esté agarrada.
        var weapons = Object.FindObjectsByType<VRGunWeapon>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        VRGunWeapon chosen = null;

        if (weapons != null && weapons.Length > 0)
        {
            // Prioridad: agarrada
            foreach (var w in weapons)
            {
                if (w != null && w.IsGrabbed())
                {
                    chosen = w;
                    break;
                }
            }

            // Fallback: primera
            if (chosen == null)
                chosen = weapons[0];
        }

        if (chosen != null)
        {
            // Tu VRGunWeapon expone estos getters. :contentReference[oaicite:1]{index=1}
            int cur = chosen.GetCurrentAmmo();
            int max = chosen.GetMaxAmmo();

            // Si quieres ocultar cuando no está agarrada, descomenta:
            // if (!chosen.IsGrabbed()) { ammoText.text = $"Ammo: --/{max}"; return; }

            ammoText.text = $"Ammo: {cur}/{max}";
        }
        else
        {
            ammoText.text = "Ammo: --/--";
        }
    }

    private void UpdateWave()
    {
        if (waveText == null) return;

        // Si tienes un sistema real de oleadas, pon aquí el getter.
        // Lo dejo sin romper, mostrando "--" si no hay nada.
        waveText.text = waveText.text; // no-op, conserva lo que ya tengas si lo actualiza otro script
    }

    private void UpdateNotes()
    {
        if (notesText == null) return;

        // ✅ Contador real: VRGameManager
        VRGameManager gm = VRGameManager.Instance;
        if (gm == null)
            gm = Object.FindAnyObjectByType<VRGameManager>(FindObjectsInactive.Include);

        if (gm != null)
        {
            notesText.text = $"Notes: {gm.GetNotesCollected()}/{gm.GetTotalNotes()}";
        }
        else
        {
            notesText.text = "Notes: --/--";
        }
    }

    #endregion

    #region Public

    public void ForceUpdate()
    {
        UpdateHUDData();
        UpdateHUDPosition(true);
    }

    public void SetUIElements(TextMeshProUGUI health, TextMeshProUGUI ammo, TextMeshProUGUI wave, TextMeshProUGUI notes)
    {
        healthText = health;
        ammoText = ammo;
        waveText = wave;
        notesText = notes;
        Debug.Log("[VRHUDAdapter] Elementos de UI asignados manualmente.");
    }

    #endregion
}
