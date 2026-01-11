using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Adapta la UI legacy (Screen Space) a VR (World Space).
/// Busca el Canvas legacy y lo convierte a seguir al jugador.
/// </summary>
public class VRHUDAdapter : MonoBehaviour
{
    [Header("Auto-Find Settings")]
    [SerializeField] private bool autoFindLegacyCanvas = true;
    [SerializeField] private string legacyCanvasName = "Canvas"; // Ajusta según tu Canvas

    [Header("VR HUD Settings")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private float distanceFromCamera = 2f;
    [SerializeField] private float heightOffset = -0.5f; // Abajo del centro de visión
    [SerializeField] private float hudScale = 0.001f; // Escala del HUD en VR

    [Header("Follow Camera")]
    [SerializeField] private bool followCamera = true;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private bool lockYRotation = false; // Solo gira en Y

    [Header("References (Auto-found)")]
    [SerializeField] private Camera vrCamera;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI notesText;

    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 0.1f; // Actualizar cada 0.1s

    // Estado
    private GameObject legacyPlayer;
    private VRGunWeapon currentWeapon;
    private float nextUpdateTime;

    private void Start()
    {
        StartCoroutine(InitializeDelayed());
    }

    /// <summary>
    /// Inicializa después de que Main se haya cargado.
    /// </summary>
    private IEnumerator InitializeDelayed()
    {
        // Esperar a que Main cargue
        yield return new WaitForSeconds(0.5f);

        // Buscar cámara VR
        if (vrCamera == null)
        {
            vrCamera = Camera.main;
            if (vrCamera == null)
            {
                Debug.LogError("[VRHUDAdapter] No se encontró cámara principal.");
                yield break;
            }
        }

        // Buscar Canvas legacy
        if (autoFindLegacyCanvas && targetCanvas == null)
        {
            targetCanvas = FindLegacyCanvas();
        }

        if (targetCanvas == null)
        {
            Debug.LogError("[VRHUDAdapter] No se encontró Canvas para adaptar.");
            yield break;
        }

        // Convertir Canvas a World Space
        ConvertCanvasToWorldSpace();

        // Buscar elementos de UI
        FindUIElements();

        // Buscar referencias de juego
        legacyPlayer = GameObject.FindWithTag("Player");

        Debug.Log("[VRHUDAdapter] HUD adaptado a VR correctamente.");
    }

    private void Update()
    {
        if (targetCanvas == null || vrCamera == null) return;

        // Hacer que el HUD siga a la cámara
        if (followCamera)
        {
            UpdateHUDPosition();
        }

        // Actualizar datos del HUD
        if (Time.time >= nextUpdateTime)
        {
            UpdateHUDData();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    #region Canvas Conversion

    private Canvas FindLegacyCanvas()
    {
        // Buscar por nombre
        GameObject canvasObj = GameObject.Find(legacyCanvasName);
        if (canvasObj != null)
        {
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"[VRHUDAdapter] Canvas encontrado: {legacyCanvasName}");
                return canvas;
            }
        }

        // Fallback: buscar primer Canvas de Screen Space
        Canvas[] allCanvas = FindObjectsOfType<Canvas>();
        foreach (var canvas in allCanvas)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay ||
                canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                Debug.Log($"[VRHUDAdapter] Canvas encontrado (fallback): {canvas.name}");
                return canvas;
            }
        }

        return null;
    }

    private void ConvertCanvasToWorldSpace()
    {
        // Cambiar a World Space
        targetCanvas.renderMode = RenderMode.WorldSpace;

        // Asignar cámara VR como Event Camera
        targetCanvas.worldCamera = vrCamera;

        // Ajustar escala
        targetCanvas.transform.localScale = Vector3.one * hudScale;

        // Posicionar inicialmente
        PositionHUDInFrontOfCamera();

        Debug.Log("[VRHUDAdapter] Canvas convertido a World Space.");
    }

    private void PositionHUDInFrontOfCamera()
    {
        Vector3 forward = vrCamera.transform.forward;
        Vector3 position = vrCamera.transform.position + forward * distanceFromCamera;
        position.y += heightOffset;

        targetCanvas.transform.position = position;
        targetCanvas.transform.rotation = Quaternion.LookRotation(forward);
    }

    #endregion

    #region HUD Follow

    private void UpdateHUDPosition()
    {
        Vector3 targetPosition = vrCamera.transform.position +
                                 vrCamera.transform.forward * distanceFromCamera;
        targetPosition.y += heightOffset;

        // Smooth follow
        targetCanvas.transform.position = Vector3.Lerp(
            targetCanvas.transform.position,
            targetPosition,
            Time.deltaTime * followSpeed
        );

        // Rotación hacia cámara
        Vector3 directionToCamera = vrCamera.transform.position - targetCanvas.transform.position;
        Quaternion targetRotation;

        if (lockYRotation)
        {
            // Solo rotar en Y (mantener horizontal)
            directionToCamera.y = 0;
            targetRotation = Quaternion.LookRotation(-directionToCamera);
        }
        else
        {
            targetRotation = Quaternion.LookRotation(-directionToCamera);
        }

        targetCanvas.transform.rotation = Quaternion.Slerp(
            targetCanvas.transform.rotation,
            targetRotation,
            Time.deltaTime * followSpeed
        );
    }

    #endregion

    #region Find UI Elements

    private void FindUIElements()
    {
        // Buscar todos los TextMeshProUGUI en el Canvas
        var allTexts = targetCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (var text in allTexts)
        {
            string name = text.name.ToLower();

            // Detectar por nombre común
            if (healthText == null && (name.Contains("vida") || name.Contains("health") || name.Contains("hp")))
            {
                healthText = text;
                Debug.Log($"[VRHUDAdapter] Health text encontrado: {text.name}");
            }
            else if (ammoText == null && (name.Contains("ammo") || name.Contains("municion") || name.Contains("balas")))
            {
                ammoText = text;
                Debug.Log($"[VRHUDAdapter] Ammo text encontrado: {text.name}");
            }
            else if (waveText == null && (name.Contains("wave") || name.Contains("oleada") || name.Contains("ola") || name.Contains("ronda")))
            {
                waveText = text;
                Debug.Log($"[VRHUDAdapter] Wave text encontrado: {text.name}");
            }
            else if (notesText == null && (name.Contains("nota") || name.Contains("note")))
            {
                notesText = text;
                Debug.Log($"[VRHUDAdapter] Notes text encontrado: {text.name}");
            }
        }

        // Log si no se encuentran elementos
        if (healthText == null) Debug.LogWarning("[VRHUDAdapter] No se encontró texto de vida.");
        if (ammoText == null) Debug.LogWarning("[VRHUDAdapter] No se encontró texto de munición.");
        if (waveText == null) Debug.LogWarning("[VRHUDAdapter] No se encontró texto de oleada.");
        if (notesText == null) Debug.LogWarning("[VRHUDAdapter] No se encontró texto de notas.");
    }

    #endregion

    #region Update HUD Data

    private void UpdateHUDData()
    {
        // Actualizar vida
        if (healthText != null && legacyPlayer != null)
        {
            var playerMovement = legacyPlayer.GetComponent<PlayerMovementQ>();
            if (playerMovement != null)
            {
                // Asume que PlayerMovementQ tiene una variable "vida"
                // Ajusta según tu código
                try
                {
                    var vidaField = playerMovement.GetType().GetField("vida");
                    if (vidaField != null)
                    {
                        float vida = (float)vidaField.GetValue(playerMovement);
                        healthText.text = $"HP: {vida:F0}";
                    }
                }
                catch
                {
                    healthText.text = "HP: --";
                }
            }
        }

        // Actualizar munición
        if (ammoText != null)
        {
            // Buscar arma actual
            if (currentWeapon == null)
            {
                currentWeapon = FindObjectOfType<VRGunWeapon>();
            }

            if (currentWeapon != null)
            {
                int current = currentWeapon.GetCurrentAmmo();
                int max = currentWeapon.GetMaxAmmo();
                ammoText.text = $"{current}/{max}";
            }
            else
            {
                ammoText.text = "-- / --";
            }
        }

        // Actualizar oleada
        if (waveText != null)
        {
            var waveSystem = FindObjectOfType<ZombieWaveSystem>();
            if (waveSystem != null)
            {
                try
                {
                    var waveField = waveSystem.GetType().GetField("waveNumber");
                    if (waveField != null)
                    {
                        int wave = (int)waveField.GetValue(waveSystem);
                        waveText.text = $"Wave: {wave}";
                    }
                }
                catch
                {
                    waveText.text = "Wave: --";
                }
            }
        }

        // Actualizar notas
        if (notesText != null)
        {
            var uiManager = FindObjectOfType<VRNoteUIManager>();
            if (uiManager != null)
            {
                int collected = uiManager.GetCollectedNotesCount();
                notesText.text = $"Notes: {collected}/5";
            }
            else
            {
                notesText.text = "Notes: --/5";
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Fuerza actualización inmediata del HUD.
    /// </summary>
    public void ForceUpdate()
    {
        UpdateHUDData();
    }

    /// <summary>
    /// Muestra u oculta el HUD.
    /// </summary>
    public void SetHUDVisible(bool visible)
    {
        if (targetCanvas != null)
        {
            targetCanvas.enabled = visible;
        }
    }

    /// <summary>
    /// Asignar manualmente elementos de UI si auto-find falla.
    /// </summary>
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