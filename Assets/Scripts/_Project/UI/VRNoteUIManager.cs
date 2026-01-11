using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Gestiona la UI de las notas en VR (World Space Canvas).
/// Muestra nota al agarrar, permite confirmar/cerrar con botones VR, y gestiona diario.
/// </summary>
public class VRNoteUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas noteUICanvas;
    [SerializeField] private CanvasGroup notePanelGroup;
    [SerializeField] private TextMeshProUGUI noteBodyTMP;
    [SerializeField] private TextMeshProUGUI hintTMP;
    [SerializeField] private RawImage noteRawImage;

    [Header("Journal")]
    [SerializeField] private CanvasGroup journalPanelGroup;
    [SerializeField] private TextMeshProUGUI journalTMP;

    [Header("UI Settings")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private string confirmHint = "Press A/Trigger to collect";
    [SerializeField] private string closeHint = "Press B/Grip to close";
    [SerializeField] private string journalHint = "Press Y to open journal";

    [Header("VR Input Actions")]
    [SerializeField] private InputActionProperty confirmAction;
    [SerializeField] private InputActionProperty closeAction;
    [SerializeField] private InputActionProperty toggleJournalAction;

    // Estado
    private bool isNoteOpen = false;
    private bool isJournalOpen = false;
    private System.Action currentOnConfirm;
    private List<string> journalEntries = new List<string>();

    private void OnEnable()
    {
        // Suscribirse a los input actions
        confirmAction.action?.Enable();
        closeAction.action?.Enable();
        toggleJournalAction.action?.Enable();

        if (confirmAction.action != null)
            confirmAction.action.performed += OnConfirmPressed;
        if (closeAction.action != null)
            closeAction.action.performed += OnClosePressed;
        if (toggleJournalAction.action != null)
            toggleJournalAction.action.performed += OnToggleJournalPressed;
    }

    private void OnDisable()
    {
        // Desuscribirse
        if (confirmAction.action != null)
            confirmAction.action.performed -= OnConfirmPressed;
        if (closeAction.action != null)
            closeAction.action.performed -= OnClosePressed;
        if (toggleJournalAction.action != null)
            toggleJournalAction.action.performed -= OnToggleJournalPressed;

        confirmAction.action?.Disable();
        closeAction.action?.Disable();
        toggleJournalAction.action?.Disable();
    }

    private void Start()
    {
        // Ocultar todo al inicio
        SetPanelVisible(notePanelGroup, false);
        SetPanelVisible(journalPanelGroup, false);

        if (noteUICanvas != null)
            noteUICanvas.enabled = true; // Asegurar que el canvas está activo
    }

    #region Input Callbacks

    private void OnConfirmPressed(InputAction.CallbackContext context)
    {
        if (isNoteOpen && currentOnConfirm != null)
        {
            currentOnConfirm?.Invoke();
            CloseNote();
        }
    }

    private void OnClosePressed(InputAction.CallbackContext context)
    {
        if (isNoteOpen)
        {
            CloseNote();
        }
        else if (isJournalOpen)
        {
            CloseJournal();
        }
    }

    private void OnToggleJournalPressed(InputAction.CallbackContext context)
    {
        if (isJournalOpen)
            CloseJournal();
        else
            OpenJournal();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Muestra una nota con su texto, textura opcional y callback de confirmación.
    /// </summary>
    public void ShowNote(string message, Texture texture, System.Action onConfirm)
    {
        if (isNoteOpen)
        {
            Debug.LogWarning("[VRNoteUIManager] Ya hay una nota abierta.");
            return;
        }

        isNoteOpen = true;
        currentOnConfirm = onConfirm;

        // Establecer contenido
        if (noteBodyTMP != null)
            noteBodyTMP.text = message;

        if (noteRawImage != null && texture != null)
        {
            noteRawImage.texture = texture;
            noteRawImage.enabled = true;
        }
        else if (noteRawImage != null)
        {
            noteRawImage.enabled = false;
        }

        // Actualizar hint
        if (hintTMP != null)
            hintTMP.text = $"{confirmHint}\n{closeHint}\n{journalHint}";

        // Mostrar panel
        SetPanelVisible(notePanelGroup, true);

        Debug.Log("[VRNoteUIManager] Nota mostrada.");
    }

    /// <summary>
    /// Cierra la nota actual.
    /// </summary>
    public void CloseNote()
    {
        if (!isNoteOpen) return;

        isNoteOpen = false;
        currentOnConfirm = null;

        SetPanelVisible(notePanelGroup, false);

        Debug.Log("[VRNoteUIManager] Nota cerrada.");
    }

    /// <summary>
    /// Añade un mensaje al diario.
    /// </summary>
    public void AddToJournal(string message)
    {
        if (!journalEntries.Contains(message))
        {
            journalEntries.Add(message);
            Debug.Log($"[VRNoteUIManager] Añadido al diario: {message.Substring(0, Mathf.Min(30, message.Length))}...");
        }
    }

    /// <summary>
    /// Abre el diario mostrando todas las entradas.
    /// </summary>
    public void OpenJournal()
    {
        if (isJournalOpen) return;

        isJournalOpen = true;

        // Construir texto del diario
        if (journalTMP != null)
        {
            if (journalEntries.Count == 0)
            {
                journalTMP.text = "No notes collected yet.";
            }
            else
            {
                string journalText = "=== JOURNAL ===\n\n";
                for (int i = 0; i < journalEntries.Count; i++)
                {
                    journalText += $"[Note {i + 1}]\n{journalEntries[i]}\n\n";
                }
                journalTMP.text = journalText;
            }
        }

        SetPanelVisible(journalPanelGroup, true);

        Debug.Log("[VRNoteUIManager] Diario abierto.");
    }

    /// <summary>
    /// Cierra el diario.
    /// </summary>
    public void CloseJournal()
    {
        if (!isJournalOpen) return;

        isJournalOpen = false;
        SetPanelVisible(journalPanelGroup, false);

        Debug.Log("[VRNoteUIManager] Diario cerrado.");
    }

    /// <summary>
    /// Obtiene el número de notas recogidas.
    /// </summary>
    public int GetCollectedNotesCount()
    {
        return journalEntries.Count;
    }

    #endregion

    #region Helper Methods

    private void SetPanelVisible(CanvasGroup group, bool visible)
    {
        if (group == null) return;

        StopAllCoroutines();

        if (visible)
        {
            group.gameObject.SetActive(true);
            StartCoroutine(FadeCanvasGroup(group, group.alpha, 1f, fadeDuration));
        }
        else
        {
            StartCoroutine(FadeCanvasGroup(group, group.alpha, 0f, fadeDuration, () =>
            {
                group.gameObject.SetActive(false);
            }));
        }
    }

    private System.Collections.IEnumerator FadeCanvasGroup(CanvasGroup group, float start, float end, float duration, System.Action onComplete = null)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        group.alpha = end;
        onComplete?.Invoke();
    }

    #endregion
}