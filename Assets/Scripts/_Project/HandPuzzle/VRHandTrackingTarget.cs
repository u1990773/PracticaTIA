using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Target individual que requiere MANTENER la mano X segundos.
/// Muestra barra de progreso visual mientras mantienes la mano.
/// </summary>
[RequireComponent(typeof(Collider))]
public class VRHandTrackingTarget : MonoBehaviour
{
    [Header("Target ID")]
    [SerializeField] private int targetIndex = 1;
    [SerializeField] private TextMeshPro label;

    [Header("Hold Settings")]
    [SerializeField] private float holdTimeRequired = 1.5f;

    [Header("Visual Feedback")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Material idleMaterial;
    [SerializeField] private Material activeMaterial; // Cuando la mano está tocando
    [SerializeField] private Material completedMaterial;

    [Header("Progress Bar")]
    [SerializeField] private Transform progressBar; // Barra que se llena
    [SerializeField] private bool autoCreateProgressBar = true;

    [Header("Animation")]
    [SerializeField] private bool pulseOnTouch = true;
    [SerializeField] private float pulseScale = 1.2f;
    [SerializeField] private float pulseSpeed = 5f;

    [Header("Events")]
    public UnityEvent onHandEnter;
    public UnityEvent onHandExit;
    public UnityEvent onHoldComplete;

    // Estado
    private bool isCompleted = false;
    private bool isHoldingHand = false;
    private float currentHoldTime = 0f;
    private Vector3 originalScale;
    private Vector3 originalProgressScale;
    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;

        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        originalScale = transform.localScale;

        // Setup label
        if (label == null)
            label = GetComponentInChildren<TextMeshPro>();

        if (label != null)
            label.text = targetIndex.ToString();

        // Crear progress bar si no existe
        if (progressBar == null && autoCreateProgressBar)
        {
            CreateProgressBar();
        }

        if (progressBar != null)
        {
            originalProgressScale = progressBar.localScale;
            progressBar.localScale = new Vector3(0, originalProgressScale.y, originalProgressScale.z);
        }

        // Material inicial
        if (targetRenderer != null && idleMaterial != null)
            targetRenderer.material = idleMaterial;
    }

    private void CreateProgressBar()
    {
        GameObject barObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barObj.name = "ProgressBar";
        barObj.transform.SetParent(transform);
        barObj.transform.localPosition = new Vector3(0, -0.15f, 0);
        barObj.transform.localScale = new Vector3(0.15f, 0.02f, 0.02f);

        // Color verde
        var barRenderer = barObj.GetComponent<Renderer>();
        if (barRenderer != null)
        {
            barRenderer.material = new Material(Shader.Find("Standard"));
            barRenderer.material.color = Color.green;
        }

        // Sin collider
        Destroy(barObj.GetComponent<Collider>());

        progressBar = barObj.transform;
        originalProgressScale = progressBar.localScale;
    }

    private void Update()
    {
        // Actualizar hold time
        if (isHoldingHand && !isCompleted)
        {
            currentHoldTime += Time.deltaTime;

            // Actualizar progress bar
            UpdateProgressBar();

            // Animación de pulso
            if (pulseOnTouch)
            {
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.05f;
                transform.localScale = originalScale * pulse;
            }

            // Check si completó el hold
            if (currentHoldTime >= holdTimeRequired)
            {
                CompleteTarget();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCompleted) return;

        if (IsHand(other))
        {
            OnHandEnter();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isCompleted) return;

        if (IsHand(other))
        {
            OnHandExit();
        }
    }

    private void OnHandEnter()
    {
        if (isCompleted || isHoldingHand) return;

        isHoldingHand = true;
        currentHoldTime = 0f;

        // Cambiar material
        if (targetRenderer != null && activeMaterial != null)
        {
            targetRenderer.material = activeMaterial;
        }

        Debug.Log($"[VRHandTrackingTarget] Mano entró en target {targetIndex}. Mantén {holdTimeRequired}s...");

        onHandEnter?.Invoke();
    }

    private void OnHandExit()
    {
        if (!isHoldingHand) return;

        isHoldingHand = false;
        currentHoldTime = 0f;

        // Volver a material idle si no completado
        if (!isCompleted && targetRenderer != null && idleMaterial != null)
        {
            targetRenderer.material = idleMaterial;
        }

        // Reset progress bar
        if (progressBar != null)
        {
            progressBar.localScale = new Vector3(0, originalProgressScale.y, originalProgressScale.z);
        }

        // Reset scale
        transform.localScale = originalScale;

        Debug.Log($"[VRHandTrackingTarget] Mano salió de target {targetIndex}");

        onHandExit?.Invoke();
    }

    private void CompleteTarget()
    {
        if (isCompleted) return;

        isCompleted = true;
        isHoldingHand = false;

        // Material completado
        if (targetRenderer != null && completedMaterial != null)
        {
            targetRenderer.material = completedMaterial;
        }

        // Label a check
        if (label != null)
        {
            label.text = "✓";
            label.color = Color.green;
        }

        // Progress bar llena
        if (progressBar != null)
        {
            progressBar.localScale = originalProgressScale;
        }

        // Reset scale
        transform.localScale = originalScale;

        Debug.Log($"[VRHandTrackingTarget] Target {targetIndex} COMPLETADO!");

        onHoldComplete?.Invoke();
    }

    private void UpdateProgressBar()
    {
        if (progressBar == null) return;

        float progress = Mathf.Clamp01(currentHoldTime / holdTimeRequired);
        progressBar.localScale = new Vector3(
            originalProgressScale.x * progress,
            originalProgressScale.y,
            originalProgressScale.z
        );
    }

    private bool IsHand(Collider other)
    {
        string name = other.name.ToLower();

        // Por nombre
        if (name.Contains("hand") || name.Contains("controller") ||
            name.Contains("palm") || name.Contains("finger"))
            return true;

        // Por tag
        if (other.CompareTag("Hand") || other.CompareTag("Player"))
            return true;

        // Por componente XR
        if (other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.XRBaseController>() != null)
            return true;

        // Si es parte del XR Origin
        if (other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null)
            return true;

        return false;
    }

    #region Public API

    public void SetTargetIndex(int index)
    {
        targetIndex = index;
        if (label != null)
            label.text = index.ToString();
    }

    public void SetHoldTimeRequired(float time)
    {
        holdTimeRequired = time;
    }

    public void ResetTarget()
    {
        isCompleted = false;
        isHoldingHand = false;
        currentHoldTime = 0f;

        if (targetRenderer != null && idleMaterial != null)
            targetRenderer.material = idleMaterial;

        if (label != null)
        {
            label.text = targetIndex.ToString();
            label.color = Color.white;
        }

        if (progressBar != null)
            progressBar.localScale = new Vector3(0, originalProgressScale.y, originalProgressScale.z);

        transform.localScale = originalScale;
    }

    public bool IsCompleted() => isCompleted;

    /// <summary>
    /// Simula toque de mano (para Device Simulator con teclado).
    /// </summary>
    public void SimulateHandTouch()
    {
        if (!isHoldingHand && !isCompleted)
        {
            OnHandEnter();
        }
    }

    public void SimulateHandExit()
    {
        if (isHoldingHand)
        {
            OnHandExit();
        }
    }

    #endregion
}