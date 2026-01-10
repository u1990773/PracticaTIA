using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

/// <summary>
/// Arma VR grabbable con disparo, recarga, munición y feedback.
/// Compatible con sistema legacy de daño (busca zombies, etc).
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class VRGunWeapon : MonoBehaviour
{
    [Header("Gun Stats")]
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private int currentAmmo = 30;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float fireRate = 0.1f; // Tiempo entre disparos
    [SerializeField] private float range = 100f;
    [SerializeField] private float reloadTime = 2f;

    [Header("References")]
    [SerializeField] private Transform muzzle; // Punto de disparo
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioClip emptySound;

    [Header("VR Input")]
    [SerializeField] private InputActionProperty fireAction;
    [SerializeField] private InputActionProperty reloadAction;

    [Header("Visual Feedback")]
    [SerializeField] private LineRenderer bulletTrail;
    [SerializeField] private float trailDuration = 0.1f;
    [SerializeField] private GameObject impactEffectPrefab;

    [Header("Layers")]
    [SerializeField] private LayerMask hitLayers = ~0; // Todo por defecto

    // Estado
    private XRGrabInteractable grabInteractable;
    private ActionBasedController currentController;
    private float lastFireTime;
    private bool isReloading = false;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (muzzle == null)
        {
            // Crear muzzle por defecto en la punta del arma
            GameObject muzzleObj = new GameObject("Muzzle");
            muzzleObj.transform.SetParent(transform);
            muzzleObj.transform.localPosition = Vector3.forward * 0.3f;
            muzzle = muzzleObj.transform;
        }

        // Configurar LineRenderer para trail
        if (bulletTrail != null)
        {
            bulletTrail.enabled = false;
        }
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);

        fireAction.action?.Enable();
        reloadAction.action?.Enable();
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);

        fireAction.action?.Disable();
        reloadAction.action?.Disable();
    }

    private void Update()
    {
        // Solo procesar input si el arma está agarrada
        if (currentController == null || isReloading) return;

        // Disparar
        if (fireAction.action != null && fireAction.action.IsPressed())
        {
            if (Time.time - lastFireTime >= fireRate)
            {
                Fire();
                lastFireTime = Time.time;
            }
        }

        // Recargar
        if (reloadAction.action != null && reloadAction.action.WasPressedThisFrame())
        {
            StartReload();
        }
    }

    #region Grab/Release

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // Obtener el controlador que agarró el arma
        if (args.interactorObject is XRBaseControllerInteractor controllerInteractor)
        {
            currentController = controllerInteractor.GetComponent<ActionBasedController>();
        }

        // Haptic feedback al agarrar
        if (VRHapticsManager.Instance != null && currentController != null)
        {
            VRHapticsManager.Instance.SendLightTap(currentController);
        }

        Debug.Log("[VRGunWeapon] Arma agarrada.");
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        currentController = null;
        Debug.Log("[VRGunWeapon] Arma soltada.");
    }

    #endregion

    #region Fire

    private void Fire()
    {
        if (currentAmmo <= 0)
        {
            PlayEmptySound();
            return;
        }

        currentAmmo--;

        // Raycast desde el muzzle
        Ray ray = new Ray(muzzle.position, muzzle.forward);
        RaycastHit hit;

        bool didHit = Physics.Raycast(ray, out hit, range, hitLayers);

        // Visual: muzzle flash
        if (muzzleFlash != null)
            muzzleFlash.Play();

        // Audio
        if (audioSource != null && fireSound != null)
            audioSource.PlayOneShot(fireSound);

        // Trail visual
        if (bulletTrail != null)
        {
            Vector3 endPoint = didHit ? hit.point : muzzle.position + muzzle.forward * range;
            StartCoroutine(ShowBulletTrail(muzzle.position, endPoint));
        }

        // Haptic feedback
        if (VRHapticsManager.Instance != null && currentController != null)
        {
            VRHapticsManager.Instance.SendGunShot(currentController);
        }

        // Daño
        if (didHit)
        {
            ProcessHit(hit);
        }

        Debug.Log($"[VRGunWeapon] Disparo! Munición: {currentAmmo}/{maxAmmo}");
    }

    private void ProcessHit(RaycastHit hit)
    {
        // Efecto de impacto
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        }

        // Intentar hacer daño (compatible con sistema legacy)
        // Busca interfaz IDamageable o script legacy tipo "Enemy", "Zombie", etc.

        var damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            Debug.Log($"[VRGunWeapon] Daño a {hit.collider.name}: {damage}");
            return;
        }

        // Fallback: buscar método legacy "RecibirDaño" o "TakeDamage"
        var target = hit.collider.gameObject;
        target.SendMessage("RecibirDaño", damage, SendMessageOptions.DontRequireReceiver);
        target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
    }

    private System.Collections.IEnumerator ShowBulletTrail(Vector3 start, Vector3 end)
    {
        if (bulletTrail == null) yield break;

        bulletTrail.SetPosition(0, start);
        bulletTrail.SetPosition(1, end);
        bulletTrail.enabled = true;

        yield return new WaitForSeconds(trailDuration);

        bulletTrail.enabled = false;
    }

    #endregion

    #region Reload

    private void StartReload()
    {
        if (isReloading || currentAmmo == maxAmmo)
        {
            Debug.Log("[VRGunWeapon] No se puede recargar ahora.");
            return;
        }

        StartCoroutine(ReloadRoutine());
    }

    private System.Collections.IEnumerator ReloadRoutine()
    {
        isReloading = true;

        Debug.Log("[VRGunWeapon] Recargando...");

        // Audio
        if (audioSource != null && reloadSound != null)
            audioSource.PlayOneShot(reloadSound);

        // Haptic feedback durante recarga (pulso)
        if (VRHapticsManager.Instance != null && currentController != null)
        {
            VRHapticsManager.Instance.SendHapticPulse(currentController, 3, 0.3f, 0.1f, 0.3f);
        }

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;

        // Haptic de confirmación
        if (VRHapticsManager.Instance != null && currentController != null)
        {
            VRHapticsManager.Instance.SendMediumBump(currentController);
        }

        Debug.Log("[VRGunWeapon] Recarga completada!");
    }

    #endregion

    #region Audio

    private void PlayEmptySound()
    {
        if (audioSource != null && emptySound != null)
            audioSource.PlayOneShot(emptySound);
    }

    #endregion

    #region Public API

    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => maxAmmo;
    public bool IsReloading() => isReloading;
    public void AddAmmo(int amount) => currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);

    #endregion
}

/// <summary>
/// Interfaz para objetos que pueden recibir daño.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount);
}