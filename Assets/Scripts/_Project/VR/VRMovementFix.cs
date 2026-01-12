using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// VERSIÓN MEJORADA v2 - Soluciona problema de "Player dejó el suelo" constante.
/// Mejoras:
/// - Detección de suelo más confiable
/// - Gravedad adaptativa
/// - No spam de logs
/// </summary>
[RequireComponent(typeof(XROrigin))]
public class VRMovementFix : MonoBehaviour
{
    [Header("Character Controller")]
    [SerializeField] private bool autoAddCharacterController = true;
    [SerializeField] private float controllerHeight = 1.8f;
    [SerializeField] private float controllerRadius = 0.3f;
    [SerializeField] private Vector3 centerOffset = new Vector3(0, 0.9f, 0);

    [Header("Gravity")]
    [SerializeField] private bool applyGravity = true;
    [SerializeField] private float gravity = -20f; // Más fuerte
    [SerializeField] private float groundingForce = -5f; // Más fuerte para mantener pegado

    [Header("Grounding - MEJORADO")]
    [SerializeField] private bool stickToGround = true;
    [SerializeField] private float groundCheckDistance = 0.3f; // Aumentado
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private bool debugGrounding = false; // DESACTIVADO para no spam logs
    [SerializeField] private float groundCheckRadius = 0.25f; // Radio para SphereCast

    [Header("Movement Settings")]
    [SerializeField] private float minMoveDistance = 0.001f;
    [SerializeField] private float skinWidth = 0.08f;
    [SerializeField] private float stepOffset = 0.3f;

    [Header("Anti-Float Settings")]
    [SerializeField] private float maxFallSpeed = -53f; // Velocidad máxima de caída
    [SerializeField] private float groundedThreshold = 0.1f; // Threshold para considerar "grounded"

    private CharacterController characterController;
    private XROrigin xrOrigin;
    private Vector3 velocity;
    private bool wasGrounded;
    private float timeInAir = 0f;
    private float lastGroundCheckTime;
    private const float GROUND_CHECK_INTERVAL = 0.05f; // Check cada 0.05s en vez de cada frame
    private bool movementEnabled = false;

    private void Start()
    {
        xrOrigin = GetComponent<XROrigin>();

        characterController = GetComponent<CharacterController>();

        if (characterController == null && autoAddCharacterController)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            Debug.Log("[VRMovementFix] Character Controller añadido automáticamente.");
        }

        if (characterController != null)
        {
            ConfigureCharacterController();
        }
        else
        {
            Debug.LogError("[VRMovementFix] No hay Character Controller. El movimiento no funcionará correctamente.");
            enabled = false;
        }
    }

    private void ConfigureCharacterController()
    {
        characterController.height = controllerHeight;
        characterController.radius = controllerRadius;
        characterController.center = centerOffset;
        characterController.skinWidth = skinWidth;
        characterController.minMoveDistance = minMoveDistance;
        characterController.stepOffset = stepOffset;

        Debug.Log("[VRMovementFix] Character Controller configurado correctamente.");
    }

    private void Update()
    {
        if (!movementEnabled) return;

        if (characterController == null) return;

        HandleGravity();

        if (Time.time - lastGroundCheckTime < GROUND_CHECK_INTERVAL)
        {
            return;
        }
        HandleGrounding();
        lastGroundCheckTime = Time.time;
    }

    public void EnableMovement()
    {
        movementEnabled = true;
    }

    private void HandleGravity()
    {
        if (!applyGravity) return;

        bool isGrounded = IsGroundedReliable();

        if (isGrounded)
        {
            // En el suelo: resetear velocidad vertical y aplicar fuerza de anclaje
            velocity.y = groundingForce;
            timeInAir = 0f;

            if (!wasGrounded && debugGrounding)
            {
                Debug.Log("[VRMovementFix] Player tocó el suelo");
            }

            wasGrounded = true;
        }
        else
        {
            // En el aire: aplicar gravedad
            timeInAir += Time.deltaTime;

            // Solo aplicar gravedad si llevamos un tiempo en el aire (evita falsos positivos)
            if (timeInAir > 0.1f)
            {
                velocity.y += gravity * Time.deltaTime;
                velocity.y = Mathf.Max(velocity.y, maxFallSpeed);
            }

            if (wasGrounded && debugGrounding && timeInAir > 0.2f)
            {
                Debug.Log("[VRMovementFix] Player dejó el suelo");
            }

            // Solo cambiar wasGrounded si llevamos suficiente tiempo en el aire
            if (timeInAir > 0.2f)
            {
                wasGrounded = false;
            }
        }

        // Aplicar movimiento vertical
        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleGrounding()
    {
        if (!stickToGround) return;
        if (!IsGroundedReliable()) return;

        // SphereCast mejorado para detectar suelo de forma más confiable
        Vector3 origin = transform.position + Vector3.up * (groundCheckRadius + 0.1f);

        if (!Physics.SphereCast(origin, groundCheckRadius, Vector3.down, out RaycastHit hit,
            groundCheckDistance + groundCheckRadius, groundLayers))
        {
            // No hay suelo cerca, aplicar fuerza hacia abajo suave
            characterController.Move(Vector3.down * 0.1f * Time.deltaTime);
        }
    }

    /// <summary>
    /// Detección de suelo más confiable usando múltiples métodos.
    /// </summary>
    private bool IsGroundedReliable()
    {
        if (characterController == null) return false;

        // Método 1: CharacterController.isGrounded (rápido pero a veces impreciso)
        if (characterController.isGrounded)
        {
            return true;
        }

        // Método 2: SphereCast desde el centro del controller
        Vector3 origin = transform.position + centerOffset;
        float distance = (controllerHeight / 2f) + groundedThreshold;

        if (Physics.SphereCast(origin, groundCheckRadius, Vector3.down, out RaycastHit hit,
            distance, groundLayers))
        {
            return true;
        }

        // Método 3: Raycast desde múltiples puntos del círculo base
        Vector3 baseCenter = transform.position + centerOffset - Vector3.up * (controllerHeight / 2f);
        int rayCount = 4;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = (360f / rayCount) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (controllerRadius * 0.8f);
            Vector3 rayOrigin = baseCenter + offset + Vector3.up * 0.1f;

            if (Physics.Raycast(rayOrigin, Vector3.down, groundedThreshold + 0.1f, groundLayers))
            {
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (characterController == null) return;

        // Color según estado
        Gizmos.color = IsGroundedReliable() ? Color.green : Color.red;

        Vector3 center = transform.position + centerOffset;

        // Cilindro del character controller
        Gizmos.DrawWireSphere(center + Vector3.up * (controllerHeight / 2), controllerRadius);
        Gizmos.DrawWireSphere(center - Vector3.up * (controllerHeight / 2), controllerRadius);

        // Línea de altura
        Gizmos.DrawLine(
            center - Vector3.up * (controllerHeight / 2),
            center + Vector3.up * (controllerHeight / 2)
        );

        // Visualizar ground checks
        if (stickToGround)
        {
            Gizmos.color = Color.yellow;
            Vector3 origin = transform.position + Vector3.up * (groundCheckRadius + 0.1f);

            // SphereCast principal
            Gizmos.DrawWireSphere(origin, groundCheckRadius);
            Gizmos.DrawLine(origin, origin + Vector3.down * (groundCheckDistance + groundCheckRadius));

            // Raycasts múltiples
            Vector3 baseCenter = transform.position + centerOffset - Vector3.up * (controllerHeight / 2f);
            int rayCount = 4;

            for (int i = 0; i < rayCount; i++)
            {
                float angle = (360f / rayCount) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (controllerRadius * 0.8f);
                Vector3 rayOrigin = baseCenter + offset + Vector3.up * 0.1f;

                Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * (groundedThreshold + 0.1f));
            }
        }
    }

    #region Public API

    public void ForceGrounded()
    {
        if (characterController != null)
        {
            velocity.y = groundingForce;
            characterController.Move(Vector3.down * 0.5f);
            timeInAir = 0f;
            wasGrounded = true;
        }
    }

    public bool IsGrounded()
    {
        return IsGroundedReliable();
    }

    public void TeleportTo(Vector3 position)
    {
        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = position;
            characterController.enabled = true;
            velocity = Vector3.zero;
            velocity.y = groundingForce;
            timeInAir = 0f;
            wasGrounded = true;
        }
    }

    /// <summary>
    /// Desactiva temporalmente los logs de grounding.
    /// </summary>
    public void SetDebugGrounding(bool enabled)
    {
        debugGrounding = enabled;
    }

    #endregion
}