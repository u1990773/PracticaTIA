using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Puzzle simple: tocar 3 botones en secuencia correcta.
/// Adaptado para funcionar con XR Device Simulator.
/// </summary>
public class VRSimplePuzzle : MonoBehaviour
{
    [Header("Puzzle Configuration")]
    [SerializeField] private List<VRPuzzleButton> buttons = new List<VRPuzzleButton>();
    [SerializeField] private bool requireSequentialOrder = true;
    [SerializeField] private float resetTimeAfterMistake = 2f;

    [Header("Completion")]
    [SerializeField] private UnityEvent onPuzzleCompleted;
    [SerializeField] private UnityEvent onPuzzleReset;
    [SerializeField] private GameObject completionReward; // Objeto que se activa al completar

    [Header("Feedback")]
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private AudioClip progressSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Visual")]
    [SerializeField] private GameObject completionEffect;

    [Header("Testing (Device Simulator)")]
    [SerializeField] private bool enableKeyboardShortcuts = true;
    [SerializeField] private KeyCode[] buttonKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3 };

    // Estado
    private int currentButtonIndex = 0;
    private bool isPuzzleCompleted = false;
    private bool isResetting = false;

    private void Start()
    {
        // Auto-find buttons si no están asignados
        if (buttons.Count == 0)
        {
            buttons.AddRange(GetComponentsInChildren<VRPuzzleButton>());
        }

        // Setup callbacks para cada botón
        for (int i = 0; i < buttons.Count; i++)
        {
            int index = i; // Captura para closure
            buttons[i].SetButtonIndex(index + 1);
            buttons[i].onButtonPressed.AddListener(() => OnButtonPressed(index));
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        ResetPuzzle();

        Debug.Log($"[VRSimplePuzzle] Puzzle inicializado con {buttons.Count} botones.");
    }

    private void Update()
    {
        // Shortcuts de teclado para testing sin VR
        if (enableKeyboardShortcuts && !isPuzzleCompleted)
        {
            for (int i = 0; i < Mathf.Min(buttonKeys.Length, buttons.Count); i++)
            {
                if (Input.GetKeyDown(buttonKeys[i]))
                {
                    buttons[i].SimulatePress();
                }
            }
        }
    }

    private void OnButtonPressed(int buttonIndex)
    {
        if (isPuzzleCompleted || isResetting)
            return;

        if (requireSequentialOrder)
        {
            // Debe tocar en orden
            if (buttonIndex == currentButtonIndex)
            {
                // ¡Correcto!
                buttons[buttonIndex].SetState(VRPuzzleButton.ButtonState.Completed);
                currentButtonIndex++;

                PlaySound(progressSound);

                // Haptic feedback
                if (VRHapticsManager.Instance != null)
                    VRHapticsManager.Instance.SendMediumBumpBoth();

                Debug.Log($"[VRSimplePuzzle] Botón {buttonIndex + 1} correcto. Progreso: {currentButtonIndex}/{buttons.Count}");

                // Check si completó todo
                if (currentButtonIndex >= buttons.Count)
                {
                    CompletePuzzle();
                }
            }
            else
            {
                // Error! Tocó fuera de orden
                Debug.LogWarning($"[VRSimplePuzzle] Error! Tocó botón {buttonIndex + 1} pero esperaba {currentButtonIndex + 1}");
                PlaySound(errorSound);

                // Haptic error
                if (VRHapticsManager.Instance != null)
                {
                    VRHapticsManager.Instance.SendHapticPulse(
                        VRHapticsManager.Instance.GetLeftController(), 2, 0.8f, 0.1f, 0.1f
                    );
                }

                StartCoroutine(ResetAfterDelay());
            }
        }
        else
        {
            // Cualquier orden es válido
            if (buttons[buttonIndex].GetState() != VRPuzzleButton.ButtonState.Completed)
            {
                buttons[buttonIndex].SetState(VRPuzzleButton.ButtonState.Completed);
                currentButtonIndex++;

                PlaySound(progressSound);

                if (currentButtonIndex >= buttons.Count)
                {
                    CompletePuzzle();
                }
            }
        }
    }

    private void CompletePuzzle()
    {
        isPuzzleCompleted = true;

        Debug.Log("[VRSimplePuzzle] ¡Puzzle completado!");

        PlaySound(successSound);

        // Efecto visual
        if (completionEffect != null)
        {
            completionEffect.SetActive(true);
        }

        // Activar recompensa
        if (completionReward != null)
        {
            completionReward.SetActive(true);
        }

        // Haptic celebración
        if (VRHapticsManager.Instance != null)
        {
            VRHapticsManager.Instance.SendHapticPulse(
                VRHapticsManager.Instance.GetLeftController(), 3, 0.6f, 0.1f, 0.15f
            );
            VRHapticsManager.Instance.SendHapticPulse(
                VRHapticsManager.Instance.GetRightController(), 3, 0.6f, 0.1f, 0.15f
            );
        }

        // Invocar evento
        onPuzzleCompleted?.Invoke();
    }

    public void ResetPuzzle()
    {
        currentButtonIndex = 0;
        isPuzzleCompleted = false;
        isResetting = false;

        foreach (var button in buttons)
        {
            button.SetState(VRPuzzleButton.ButtonState.Idle);
        }

        if (completionEffect != null)
        {
            completionEffect.SetActive(false);
        }

        if (completionReward != null)
        {
            completionReward.SetActive(false);
        }

        onPuzzleReset?.Invoke();

        Debug.Log("[VRSimplePuzzle] Puzzle reseteado.");
    }

    private System.Collections.IEnumerator ResetAfterDelay()
    {
        isResetting = true;
        yield return new WaitForSeconds(resetTimeAfterMistake);
        ResetPuzzle();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Obtiene el progreso actual (0-1).
    /// </summary>
    public float GetProgress()
    {
        return (float)currentButtonIndex / buttons.Count;
    }

    /// <summary>
    /// Verifica si el puzzle está completado.
    /// </summary>
    public bool IsCompleted()
    {
        return isPuzzleCompleted;
    }
}