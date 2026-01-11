using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Puzzle que requiere HAND TRACKING.
/// El jugador debe tocar objetos con sus MANOS FÍSICAS en secuencia.
/// Más sofisticado: requiere mantener la mano X segundos en cada objetivo.
/// </summary>
public class VRHandTrackingPuzzle : MonoBehaviour
{
    [Header("Puzzle Configuration")]
    [SerializeField] private List<VRHandTrackingTarget> targets = new List<VRHandTrackingTarget>();
    [SerializeField] private bool requireSequentialOrder = true;
    [SerializeField] private float holdTimeRequired = 1.5f; // Segundos que debes mantener la mano
    [SerializeField] private float resetTimeAfterMistake = 3f;

    [Header("Reward")]
    [SerializeField] private GameObject rewardObject; // La nota que aparece
    [SerializeField] private Transform rewardSpawnPoint;
    [SerializeField] private bool showRewardOnCompletion = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onPuzzleCompleted;
    [SerializeField] private UnityEvent onPuzzleReset;
    [SerializeField] private UnityEvent onTargetProgress;

    [Header("Feedback")]
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private AudioClip progressSound;
    [SerializeField] private AudioClip holdingSound; // Sonido mientras mantiene
    [SerializeField] private AudioSource audioSource;

    [Header("Visual")]
    [SerializeField] private GameObject completionEffect;

    [Header("Testing (Device Simulator)")]
    [SerializeField] private bool enableKeyboardShortcuts = true;
    [SerializeField] private KeyCode[] targetKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4 };

    // Estado
    private int currentTargetIndex = 0;
    private bool isPuzzleCompleted = false;
    private bool isResetting = false;
    private float currentHoldTime = 0f;
    private bool isHolding = false;

    private void Start()
    {
        // Auto-find targets
        if (targets.Count == 0)
        {
            targets.AddRange(GetComponentsInChildren<VRHandTrackingTarget>());
        }

        // Setup callbacks
        for (int i = 0; i < targets.Count; i++)
        {
            int index = i;
            targets[i].SetTargetIndex(index + 1);
            targets[i].onHandEnter.AddListener(() => OnHandEnterTarget(index));
            targets[i].onHandExit.AddListener(() => OnHandExitTarget(index));
            targets[i].onHoldComplete.AddListener(() => OnTargetCompleted(index));
        }

        // Set hold time en cada target
        foreach (var target in targets)
        {
            target.SetHoldTimeRequired(holdTimeRequired);
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Desactivar recompensa inicialmente
        if (rewardObject != null)
        {
            rewardObject.SetActive(false);
        }

        ResetPuzzle();

        Debug.Log($"[VRHandTrackingPuzzle] Puzzle inicializado con {targets.Count} targets. Hold time: {holdTimeRequired}s");
    }

    private void Update()
    {
        // Keyboard shortcuts para testing
        if (enableKeyboardShortcuts && !isPuzzleCompleted)
        {
            for (int i = 0; i < Mathf.Min(targetKeys.Length, targets.Count); i++)
            {
                if (Input.GetKey(targetKeys[i]))
                {
                    targets[i].SimulateHandTouch();
                }

                if (Input.GetKeyUp(targetKeys[i]))
                {
                    targets[i].SimulateHandExit();
                }
            }
        }
    }

    #region Target Callbacks

    private void OnHandEnterTarget(int targetIndex)
    {
        if (isPuzzleCompleted || isResetting) return;

        if (requireSequentialOrder)
        {
            if (targetIndex == currentTargetIndex)
            {
                // Correcto - empieza a contar
                isHolding = true;

                // Sonido de "mantener"
                if (holdingSound != null && audioSource != null)
                {
                    audioSource.clip = holdingSound;
                    audioSource.loop = true;
                    audioSource.Play();
                }

                Debug.Log($"[VRHandTrackingPuzzle] Mano en target {targetIndex + 1}. Mantén {holdTimeRequired}s...");
            }
            else
            {
                // Error - tocó el target incorrecto
                Debug.LogWarning($"[VRHandTrackingPuzzle] Error! Tocaste target {targetIndex + 1} pero esperaba {currentTargetIndex + 1}");
                PlaySound(errorSound);

                // Haptic fuerte
                if (VRHapticsManager.Instance != null)
                {
                    VRHapticsManager.Instance.SendHapticPulse(
                        VRHapticsManager.Instance.GetLeftController(), 3, 1f, 0.1f, 0.1f
                    );
                    VRHapticsManager.Instance.SendHapticPulse(
                        VRHapticsManager.Instance.GetRightController(), 3, 1f, 0.1f, 0.1f
                    );
                }

                StartCoroutine(ResetAfterDelay());
            }
        }
        else
        {
            // Modo libre - cualquier orden
            if (!targets[targetIndex].IsCompleted())
            {
                isHolding = true;

                if (holdingSound != null && audioSource != null)
                {
                    audioSource.clip = holdingSound;
                    audioSource.loop = true;
                    audioSource.Play();
                }
            }
        }
    }

    private void OnHandExitTarget(int targetIndex)
    {
        isHolding = false;

        // Parar sonido de mantener
        if (audioSource != null && audioSource.clip == holdingSound)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }

        // Reset progress del target si no completó
        if (targetIndex == currentTargetIndex && !targets[targetIndex].IsCompleted())
        {
            Debug.Log($"[VRHandTrackingPuzzle] Mano retirada antes de completar target {targetIndex + 1}");
        }
    }

    private void OnTargetCompleted(int targetIndex)
    {
        if (isPuzzleCompleted || isResetting) return;

        isHolding = false;

        // Parar sonido
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }

        if (requireSequentialOrder && targetIndex == currentTargetIndex)
        {
            // Target completado correctamente
            currentTargetIndex++;

            PlaySound(progressSound);

            // Haptic de progreso
            if (VRHapticsManager.Instance != null)
            {
                VRHapticsManager.Instance.SendMediumBumpBoth();
            }

            Debug.Log($"[VRHandTrackingPuzzle] Target {targetIndex + 1} completado! Progreso: {currentTargetIndex}/{targets.Count}");

            onTargetProgress?.Invoke();

            // Check si completó todo
            if (currentTargetIndex >= targets.Count)
            {
                CompletePuzzle();
            }
        }
    }

    #endregion

    #region Puzzle Completion

    private void CompletePuzzle()
    {
        isPuzzleCompleted = true;

        Debug.Log("[VRHandTrackingPuzzle] ¡PUZZLE COMPLETADO!");

        PlaySound(successSound);

        // Efecto visual
        if (completionEffect != null)
        {
            completionEffect.SetActive(true);
        }

        // Mostrar recompensa (nota)
        if (showRewardOnCompletion && rewardObject != null)
        {
            SpawnReward();
        }

        // Haptic celebración
        if (VRHapticsManager.Instance != null)
        {
            VRHapticsManager.Instance.SendHapticPulse(
                VRHapticsManager.Instance.GetLeftController(), 5, 0.8f, 0.15f, 0.2f
            );
            VRHapticsManager.Instance.SendHapticPulse(
                VRHapticsManager.Instance.GetRightController(), 5, 0.8f, 0.15f, 0.2f
            );
        }

        onPuzzleCompleted?.Invoke();
    }

    private void SpawnReward()
    {
        if (rewardObject == null) return;

        if (rewardSpawnPoint != null)
        {
            rewardObject.transform.position = rewardSpawnPoint.position;
            rewardObject.transform.rotation = rewardSpawnPoint.rotation;
        }

        rewardObject.SetActive(true);

        // Animación de aparición
        StartCoroutine(AnimateRewardSpawn());

        Debug.Log("[VRHandTrackingPuzzle] Recompensa (nota) spawneada!");
    }

    private System.Collections.IEnumerator AnimateRewardSpawn()
    {
        if (rewardObject == null) yield break;

        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one;
        float duration = 0.5f;
        float elapsed = 0f;

        rewardObject.transform.localScale = startScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f); // Ease out

            rewardObject.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            rewardObject.transform.Rotate(Vector3.up, 360f * Time.deltaTime);

            yield return null;
        }

        rewardObject.transform.localScale = endScale;
    }

    #endregion

    public void ResetPuzzle()
    {
        currentTargetIndex = 0;
        isPuzzleCompleted = false;
        isResetting = false;
        isHolding = false;

        foreach (var target in targets)
        {
            target.ResetTarget();
        }

        if (completionEffect != null)
        {
            completionEffect.SetActive(false);
        }

        if (rewardObject != null)
        {
            rewardObject.SetActive(false);
        }

        onPuzzleReset?.Invoke();

        Debug.Log("[VRHandTrackingPuzzle] Puzzle reseteado.");
    }

    private System.Collections.IEnumerator ResetAfterDelay()
    {
        isResetting = true;
        yield return new WaitForSeconds(resetTimeAfterMistake);
        ResetPuzzle();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null && clip != holdingSound)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public float GetProgress() => (float)currentTargetIndex / targets.Count;
    public bool IsCompleted() => isPuzzleCompleted;
}