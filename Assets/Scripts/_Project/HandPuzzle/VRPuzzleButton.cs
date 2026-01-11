using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

/// <summary>
/// Botón individual del puzzle que puede ser activado con:
/// - XR Grab (agarrar)
/// - XR Ray Interactor (apuntar y trigger)
/// - Colisión con mano (hand tracking)
/// - Teclado (para testing)
/// </summary>
[RequireComponent(typeof(Collider))]
public class VRPuzzleButton : MonoBehaviour
{
    public enum ButtonState
    {
        Idle,       // Sin tocar
        Active,     // Siendo tocado
        Completed   // Ya completado
    }

    [Header("Button ID")]
    [SerializeField] private int buttonIndex = 1;
    [SerializeField] private TextMeshPro buttonLabel; // Muestra el número

    [Header("Visual Feedback")]
    [SerializeField] private Renderer buttonRenderer;
    [SerializeField] private Material idleMaterial;
    [SerializeField] private Material activeMaterial;
    [SerializeField] private Material completedMaterial;

    [Header("Animation")]
    [SerializeField] private bool animateOnPress = true;
    [SerializeField] private float pressDepth = 0.1f;
    [SerializeField] private float animationSpeed = 10f;

    [Header("Interaction")]
    [SerializeField] private bool canBeGrabbed = false;
    [SerializeField] private bool useRayInteractor = true;

    [Header("Events")]
    public UnityEvent onButtonPressed;

    // Estado
    private ButtonState currentState = ButtonState.Idle;
    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private bool isAnimating = false;
    private XRSimpleInteractable interactable;

    private void Awake()
    {
        // Setup collider
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        // Setup renderer
        if (buttonRenderer == null)
            buttonRenderer = GetComponent<Renderer>();

        originalPosition = transform.localPosition;
        targetPosition = originalPosition;

        // Setup XR Interaction si se usa ray
        if (useRayInteractor)
        {
            SetupXRInteraction();
        }

        // Setup label
        if (buttonLabel == null)
        {
            buttonLabel = GetComponentInChildren<TextMeshPro>();
        }

        if (buttonLabel != null)
        {
            buttonLabel.text = buttonIndex.ToString();
        }
    }

    private void SetupXRInteraction()
    {
        // Añadir XRSimpleInteractable para poder usar con Ray
        interactable = gameObject.AddComponent<XRSimpleInteractable>();
        interactable.selectEntered.AddListener((args) => OnXRInteraction());
    }

    private void Update()
    {
        // Animar botón
        if (isAnimating)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                targetPosition,
                Time.deltaTime * animationSpeed
            );

            if (Vector3.Distance(transform.localPosition, targetPosition) < 0.001f)
            {
                transform.localPosition = targetPosition;
                isAnimating = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Detectar mano o controller
        if (currentState != ButtonState.Completed && IsHandOrController(other))
        {
            PressButton();
        }
    }

    private void OnXRInteraction()
    {
        if (currentState != ButtonState.Completed)
        {
            PressButton();
        }
    }

    /// <summary>
    /// Presiona el botón.
    /// </summary>
    public void PressButton()
    {
        if (currentState == ButtonState.Completed)
            return;

        Debug.Log($"[VRPuzzleButton] Botón {buttonIndex} presionado.");

        // Animación de presionar
        if (animateOnPress)
        {
            targetPosition = originalPosition - transform.forward * pressDepth;
            isAnimating = true;

            // Volver después de un momento
            StartCoroutine(ReturnToPosition());
        }

        // Cambiar estado temporalmente
        SetState(ButtonState.Active);

        // Invocar evento
        onButtonPressed?.Invoke();

        // Haptic feedback
        if (VRHapticsManager.Instance != null)
        {
            VRHapticsManager.Instance.SendMediumBumpBoth();
        }
    }

    /// <summary>
    /// Simula presionar el botón (para keyboard shortcuts).
    /// </summary>
    public void SimulatePress()
    {
        PressButton();
    }

    private System.Collections.IEnumerator ReturnToPosition()
    {
        yield return new WaitForSeconds(0.2f);
        targetPosition = originalPosition;
    }

    /// <summary>
    /// Establece el estado del botón.
    /// </summary>
    public void SetState(ButtonState newState)
    {
        currentState = newState;

        // Actualizar material
        if (buttonRenderer != null)
        {
            Material targetMaterial = newState switch
            {
                ButtonState.Idle => idleMaterial,
                ButtonState.Active => activeMaterial,
                ButtonState.Completed => completedMaterial,
                _ => idleMaterial
            };

            if (targetMaterial != null)
            {
                buttonRenderer.material = targetMaterial;
            }
        }

        // Actualizar label
        if (buttonLabel != null && newState == ButtonState.Completed)
        {
            buttonLabel.text = "✓";
        }
        else if (buttonLabel != null && newState == ButtonState.Idle)
        {
            buttonLabel.text = buttonIndex.ToString();
        }
    }

    public ButtonState GetState() => currentState;

    public void SetButtonIndex(int index)
    {
        buttonIndex = index;
        if (buttonLabel != null)
        {
            buttonLabel.text = index.ToString();
        }
    }

    /// <summary>
    /// Verifica si el collider es una mano o controller.
    /// </summary>
    private bool IsHandOrController(Collider other)
    {
        string name = other.name.ToLower();

        // Por nombre
        if (name.Contains("hand") || name.Contains("controller") ||
            name.Contains("palm") || name.Contains("finger"))
            return true;

        // Por componente XR
        if (other.GetComponentInParent<ActionBasedController>() != null)
            return true;

        if (other.GetComponentInParent<XRBaseController>() != null)
            return true;

        return false;
    }
}