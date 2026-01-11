using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Arregla el problema de movimiento flotante/atravesar suelos.
/// Asegura que el XR Origin tenga física correcta.
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
    [SerializeField] private float gravity = -9.81f;

    [Header("Grounding")]
    [SerializeField] private bool stickToGround = true;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayers = ~0;

    private CharacterController characterController;
    private XROrigin xrOrigin;
    private Vector3 velocity;

    private void Start()
    {
        xrOrigin = GetComponent<XROrigin>();

        // Añadir o configurar Character Controller
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
            Debug.LogWarning("[VRMovementFix] No hay Character Controller. El movimiento puede ser inestable.");
        }
    }

    private void ConfigureCharacterController()
    {
        characterController.height = controllerHeight;
        characterController.radius = controllerRadius;
        characterController.center = centerOffset;
        characterController.skinWidth = 0.08f;
        characterController.minMoveDistance = 0.001f;

        Debug.Log("[VRMovementFix] Character Controller configurado correctamente.");
    }

    private void Update()
    {
        if (characterController == null) return;

        // Aplicar gravedad
        if (applyGravity)
        {
            if (characterController.isGrounded)
            {
                // En el suelo: resetear velocidad vertical
                if (velocity.y < 0)
                {
                    velocity.y = -2f; // Pequeña fuerza hacia abajo para mantener grounded
                }
            }
            else
            {
                // En el aire: aplicar gravedad
                velocity.y += gravity * Time.deltaTime;
            }

            // Aplicar movimiento vertical (gravedad)
            characterController.Move(velocity * Time.deltaTime);
        }

        // Stick to ground (evita flotar)
        if (stickToGround && characterController.isGrounded)
        {
            // Pequeño raycast hacia abajo para asegurar que está en el suelo
            if (!Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayers))
            {
                // No hay suelo cerca, aplicar fuerza hacia abajo
                characterController.Move(Vector3.down * 0.1f);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualizar Character Controller
        if (characterController != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + centerOffset, controllerRadius);
            Gizmos.DrawLine(
                transform.position + centerOffset - Vector3.up * (controllerHeight / 2),
                transform.position + centerOffset + Vector3.up * (controllerHeight / 2)
            );
        }
    }
}