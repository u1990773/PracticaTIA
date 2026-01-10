using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Minijuego de puzzle que requiere hand tracking.
/// Ejemplo: tocar 3 orbes en secuencia con las manos para desbloquear.
/// </summary>
public class VRHandPuzzle : MonoBehaviour
{
    [Header("Puzzle Configuration")]
    [SerializeField] private List<VRHandTouchTarget> touchTargets = new List<VRHandTouchTarget>();
    [SerializeField] private bool requireSequentialOrder = true;
    [SerializeField] private float resetTimeAfterMistake = 2f;

    [Header("Events")]
    [SerializeField] private UnityEvent onPuzzleCompleted;
    [SerializeField] private UnityEvent onPuzzleReset;

    [Header("Feedback")]
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private AudioClip progressSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Visual")]
    [SerializeField] private GameObject completionEffect;

    private int currentTargetIndex = 0;
    private bool isPuzzleCompleted = false;
    private bool isResetting = false;

    private void Start()
    {
        // Auto-find targets si no están asignados
        if (touchTargets.Count == 0)
        {
            touchTargets.AddRange(GetComponentsInChildren<VRHandTouchTarget>());
        }

        // Setup callbacks para cada target
        for (int i = 0; i < touchTargets.Count; i++)
        {
            int index = i; // Captura para closure
            touchTargets[i].onTouched.AddListener(() => OnTargetTouched(index));
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        ResetPuzzle();

        Debug.Log($"[VRHandPuzzle] Puzzle inicializado con {touchTargets.Count} targets.");
    }

    private void OnTargetTouched(int targetIndex)
    {
        if (isPuzzleCompleted || isResetting)
            return;

        if (requireSequentialOrder)
        {
            // Debe tocar en orden
            if (targetIndex == currentTargetIndex)
            {
                // Correcto!
                touchTargets[targetIndex].SetCompleted(true);
                currentTargetIndex++;

                PlaySound(progressSound);

                // Haptic feedback
                if (VRHapticsManager.Instance != null)
                    VRHapticsManager.Instance.SendMediumBumpBoth();

                Debug.Log($"[VRHandPuzzle] Target {targetIndex} correcto. Progreso: {currentTargetIndex}/{touchTargets.Count}");

                // Check si completó todo
                if (currentTargetIndex >= touchTargets.Count)
                {
                    CompletePuzzle();
                }
            }
            else
            {
                // Error! Tocó fuera de orden
                Debug.LogWarning($"[VRHandPuzzle] Error! Tocó target {targetIndex} pero esperaba {currentTargetIndex}");
                PlaySound(errorSound);

                // Haptic error
                if (VRHapticsManager.Instance != null)
                    VRHapticsManager.Instance.SendHapticPulse(
                        VRHapticsManager.Instance.GetLeftController(), 2, 0.8f, 0.1f, 0.1f
                    );

                StartCoroutine(ResetAfterDelay());
            }
        }
        else
        {
            // Cualquier orden es válido
            if (!touchTargets[targetIndex].IsCompleted())
            {
                touchTargets[targetIndex].SetCompleted(true);
                currentTargetIndex++;

                PlaySound(progressSound);

                if (currentTargetIndex >= touchTargets.Count)
                {
                    CompletePuzzle();
                }
            }
        }
    }

    private void CompletePuzzle()
    {
        isPuzzleCompleted = true;

        Debug.Log("[VRHandPuzzle] ¡Puzzle completado!");

        PlaySound(successSound);

        // Efecto visual
        if (completionEffect != null)
        {
            completionEffect.SetActive(true);
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
        currentTargetIndex = 0;
        isPuzzleCompleted = false;
        isResetting = false;

        foreach (var target in touchTargets)
        {
            target.SetCompleted(false);
        }

        if (completionEffect != null)
        {
            completionEffect.SetActive(false);
        }

        onPuzzleReset?.Invoke();

        Debug.Log("[VRHandPuzzle] Puzzle reseteado.");
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
}