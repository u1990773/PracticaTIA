using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Game Manager que controla el objetivo del juego:
/// - Cuenta notas recogidas
/// - Detecta victoria (todas las notas)
/// - Muestra UI de progreso
/// </summary>
public class VRGameManager : MonoBehaviour
{
    [Header("Objective")]
    [SerializeField] private int totalNotes = 5;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI notesCounterText;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TextMeshProUGUI victoryText;

    [Header("Victory")]
    [SerializeField] private UnityEvent onVictory;
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private GameObject victoryEffect;

    [Header("Auto-Setup")]
    [SerializeField] private bool autoCountNotesInScene = true;

    private int notesCollected = 0;
    private bool hasWon = false;
    private AudioSource audioSource;

    // Singleton
    public static VRGameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        // Auto-contar notas en la escena
        if (autoCountNotesInScene)
        {
            var notes = FindObjectsOfType<VRNoteSimpleCollect>(true);
            totalNotes = notes.Length;
            Debug.Log($"[VRGameManager] Total de notas en escena: {totalNotes}");
        }

        // Ocultar panel de victoria
        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        UpdateUI();
    }

    /// <summary>
    /// Llamar cuando se recoge una nota.
    /// </summary>
    public void OnNoteCollected()
    {
        if (hasWon) return;

        notesCollected++;

        Debug.Log($"[VRGameManager] Nota recogida! {notesCollected}/{totalNotes}");

        UpdateUI();

        // Check victoria
        if (notesCollected >= totalNotes)
        {
            TriggerVictory();
        }
    }

    private void UpdateUI()
    {
        if (notesCounterText != null)
        {
            notesCounterText.text = $"Notes: {notesCollected}/{totalNotes}";

            // Color según progreso
            if (notesCollected >= totalNotes)
                notesCounterText.color = Color.green;
            else if (notesCollected >= totalNotes / 2)
                notesCounterText.color = Color.yellow;
            else
                notesCounterText.color = Color.white;
        }
    }

    private void TriggerVictory()
    {
        if (hasWon) return;
        hasWon = true;

        Debug.Log("[VRGameManager] ¡VICTORIA! Todas las notas recogidas!");

        // Mostrar panel de victoria
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }

        if (victoryText != null)
        {
            victoryText.text = $"¡VICTORIA!\n\nRecogiste las {totalNotes} notas místicas\n\nPuedes escapar";
        }

        // Sonido
        if (victorySound != null && audioSource != null)
        {
            audioSource.PlayOneShot(victorySound);
        }

        // Efecto
        if (victoryEffect != null)
        {
            victoryEffect.SetActive(true);
        }

        // Haptic celebración
        if (VRHapticsManager.Instance != null)
        {
            for (int i = 0; i < 3; i++)
            {
                VRHapticsManager.Instance.SendHapticPulse(
                    VRHapticsManager.Instance.GetLeftController(), 3, 0.9f, 0.2f, 0.3f
                );
                VRHapticsManager.Instance.SendHapticPulse(
                    VRHapticsManager.Instance.GetRightController(), 3, 0.9f, 0.2f, 0.3f
                );
            }
        }

        // Evento
        onVictory?.Invoke();
    }

    public int GetNotesCollected() => notesCollected;
    public int GetTotalNotes() => totalNotes;
    public bool HasWon() => hasWon;
}