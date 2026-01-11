using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Sistema SIMPLIFICADO de recogida de notas.
/// Al tocar/agarrar la nota, se recoge inmediatamente y desaparece.
/// NO muestra UI de lectura.
/// </summary>
[RequireComponent(typeof(Collider))]
public class VRNoteSimpleCollect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool useGrabToCollect = false; // false = solo tocar
    [SerializeField] private float collectDelay = 0.5f; // Tiempo antes de recoger

    [Header("Feedback")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private GameObject collectEffect; // Partículas al recoger
    [SerializeField] private bool playHapticFeedback = true;

    [Header("Animation")]
    [SerializeField] private bool animateBeforeCollect = true;
    [SerializeField] private float floatHeight = 0.3f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float rotationSpeed = 90f;

    private Nota notaLegacy;
    private bool isCollected = false;
    private bool isCollecting = false;
    private Vector3 startPosition;
    private AudioSource audioSource;

    private void Awake()
    {
        // Buscar script legacy de Nota
        notaLegacy = GetComponent<Nota>();

        // Setup collider como trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        startPosition = transform.position;

        // Si usa grab, añadir XRGrabInteractable
        if (useGrabToCollect)
        {
            SetupGrabInteraction();
        }
    }

    private void Update()
    {
        // Animación flotante
        if (animateBeforeCollect && !isCollected && !isCollecting)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

    private void SetupGrabInteraction()
    {
        XRGrabInteractable grab = GetComponent<XRGrabInteractable>();
        if (grab == null)
        {
            grab = gameObject.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = false;
        }

        grab.selectEntered.AddListener(OnGrabbed);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Solo si NO usa grab, recoger al tocar
        if (!useGrabToCollect && !isCollected && !isCollecting)
        {
            // Verificar si es el jugador o su mano
            if (IsPlayerOrHand(other))
            {
                StartCoroutine(CollectNote());
            }
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // Si usa grab, recoger al agarrar
        if (useGrabToCollect && !isCollected && !isCollecting)
        {
            StartCoroutine(CollectNote());
        }
    }

    private System.Collections.IEnumerator CollectNote()
    {
        if (isCollected || isCollecting) yield break;

        isCollecting = true;

        Debug.Log($"[VRNoteSimpleCollect] Recogiendo nota: {gameObject.name}");

        // Haptic feedback
        if (playHapticFeedback && VRHapticsManager.Instance != null)
        {
            VRHapticsManager.Instance.SendMediumBumpBoth();
        }

        // Sonido
        if (collectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collectSound);
        }

        // Efecto visual
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        // Animación de recogida (opcional)
        if (animateBeforeCollect)
        {
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.up * 2f; // Sube

            while (elapsed < collectDelay)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / collectDelay;

                // Mover hacia arriba y hacer más pequeña
                transform.position = Vector3.Lerp(startPos, endPos, t);
                transform.localScale = Vector3.one * (1f - t);

                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(collectDelay);
        }

        // Marcar como recogida
        isCollected = true;

        // Llamar método legacy si existe
        if (notaLegacy != null)
        {
            notaLegacy.RecogerNota();
        }
        else
        {
            // Si no hay script legacy, simplemente destruir
            Destroy(gameObject);
        }

        Debug.Log($"[VRNoteSimpleCollect] Nota recogida: {gameObject.name}");
    }

    private bool IsPlayerOrHand(Collider other)
    {
        // Verificar si es el jugador
        if (other.CompareTag("Player"))
            return true;

        // Verificar si es parte del XR Origin
        string name = other.name.ToLower();
        if (name.Contains("player") || name.Contains("origin") ||
            name.Contains("hand") || name.Contains("controller"))
            return true;

        // Verificar componentes XR
        if (other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null)
            return true;

        return false;
    }

    /// <summary>
    /// Fuerza la recogida de la nota (para testing).
    /// </summary>
    public void ForceCollect()
    {
        if (!isCollected && !isCollecting)
        {
            StartCoroutine(CollectNote());
        }
    }
}