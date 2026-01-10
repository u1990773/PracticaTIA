using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Target que detecta cuando una mano lo toca (para hand tracking puzzle).
/// Usa detección por collider (trigger).
/// </summary>
[RequireComponent(typeof(Collider))]
public class VRHandTouchTarget : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string handTag = "Hand"; // O usar layer
    [SerializeField] private bool oneTimeUse = false;

    [Header("Visual Feedback")]
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material completedMaterial;
    [SerializeField] private Renderer targetRenderer;

    [Header("Scale Animation")]
    [SerializeField] private bool animateOnTouch = true;
    [SerializeField] private float touchScaleMultiplier = 1.2f;
    [SerializeField] private float animationSpeed = 5f;

    [Header("Events")]
    public UnityEvent onTouched;

    private Collider targetCollider;
    private bool isCompleted = false;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isAnimating = false;

    private void Awake()
    {
        targetCollider = GetComponent<Collider>();
        targetCollider.isTrigger = true;

        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        originalScale = transform.localScale;
        targetScale = originalScale;

        // Set default material
        if (targetRenderer != null && defaultMaterial != null)
            targetRenderer.material = defaultMaterial;
    }

    private void Update()
    {
        // Animar escala
        if (isAnimating)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);

            if (Vector3.Distance(transform.localScale, targetScale) < 0.01f)
            {
                transform.localScale = targetScale;
                isAnimating = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verificar si es una mano
        if (!isCompleted && IsHand(other))
        {
            OnHandTouch(other);
        }
    }

    private void OnHandTouch(Collider handCollider)
    {
        Debug.Log($"[VRHandTouchTarget] Hand touched {gameObject.name}");

        // Feedback visual
        if (animateOnTouch)
        {
            targetScale = originalScale * touchScaleMultiplier;
            isAnimating = true;
        }

        if (targetRenderer != null && highlightMaterial != null)
        {
            targetRenderer.material = highlightMaterial;
        }

        // Invocar evento
        onTouched?.Invoke();

        // Si es one-time, desactivar
        if (oneTimeUse)
        {
            SetCompleted(true);
        }
    }

    /// <summary>
    /// Marca el target como completado.
    /// </summary>
    public void SetCompleted(bool completed)
    {
        isCompleted = completed;

        if (completed)
        {
            if (targetRenderer != null && completedMaterial != null)
            {
                targetRenderer.material = completedMaterial;
            }

            // Volver a escala original
            if (animateOnTouch)
            {
                targetScale = originalScale;
                isAnimating = true;
            }

            Debug.Log($"[VRHandTouchTarget] {gameObject.name} completado.");
        }
        else
        {
            // Reset
            if (targetRenderer != null && defaultMaterial != null)
            {
                targetRenderer.material = defaultMaterial;
            }

            transform.localScale = originalScale;
            targetScale = originalScale;
        }
    }

    public bool IsCompleted() => isCompleted;

    /// <summary>
    /// Verifica si el collider pertenece a una mano VR.
    /// </summary>
    private bool IsHand(Collider other)
    {
        // Opción 1: Por tag
        if (!string.IsNullOrEmpty(handTag) && other.CompareTag(handTag))
            return true;

        // Opción 2: Por nombre (fallback)
        string name = other.name.ToLower();
        if (name.Contains("hand") || name.Contains("palm") || name.Contains("controller"))
            return true;

        // Opción 3: Buscar componente XR en el parent
        var xrController = other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.XRBaseController>();
        if (xrController != null)
            return true;

        return false;
    }

    /// <summary>
    /// Cambia el material highlight.
    /// </summary>
    public void SetHighlightMaterial(Material mat)
    {
        highlightMaterial = mat;
    }

    /// <summary>
    /// Cambia el material completed.
    /// </summary>
    public void SetCompletedMaterial(Material mat)
    {
        completedMaterial = mat;
    }
}