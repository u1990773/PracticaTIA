using UnityEngine;

/// <summary>
/// Crea automáticamente efectos visuales para el arma si no existen.
/// - Bullet Trail (LineRenderer)
/// - Impact Effect (partículas simples)
/// - Muzzle Flash (partículas)
/// </summary>
public class VRGunEffectsAuto : MonoBehaviour
{
    [Header("Auto-Create Settings")]
    [SerializeField] private bool autoCreateBulletTrail = true;
    [SerializeField] private bool autoCreateImpactEffect = true;
    [SerializeField] private bool autoCreateMuzzleFlash = true;

    [Header("Bullet Trail")]
    [SerializeField] private Color trailColor = Color.yellow;
    [SerializeField] private float trailWidth = 0.02f;
    [SerializeField] private float trailDuration = 0.1f;

    [Header("Impact Effect")]
    [SerializeField] private Color impactColor = Color.white;
    [SerializeField] private int impactParticleCount = 10;

    [Header("Muzzle Flash")]
    [SerializeField] private Color muzzleColor = new Color(1f, 0.5f, 0f); // Naranja
    [SerializeField] private int muzzleParticleCount = 20;

    private VRGunWeapon gunWeapon;
    private LineRenderer bulletTrail;
    private GameObject impactEffectPrefab;
    private ParticleSystem muzzleFlash;

    private void Start()
    {
        gunWeapon = GetComponent<VRGunWeapon>();

        if (gunWeapon == null)
        {
            Debug.LogError("[VRGunEffectsAuto] No se encontró VRGunWeapon.");
            enabled = false;
            return;
        }

        SetupEffects();
    }

    private void SetupEffects()
    {
        // 1. Bullet Trail
        if (autoCreateBulletTrail)
        {
            bulletTrail = CreateBulletTrail();
            AssignToGunWeapon("bulletTrail", bulletTrail);
        }

        // 2. Impact Effect Prefab
        if (autoCreateImpactEffect)
        {
            impactEffectPrefab = CreateImpactEffectPrefab();
            AssignToGunWeapon("impactEffectPrefab", impactEffectPrefab);
        }

        // 3. Muzzle Flash
        if (autoCreateMuzzleFlash)
        {
            muzzleFlash = CreateMuzzleFlash();
            AssignToGunWeapon("muzzleFlash", muzzleFlash);
        }

        Debug.Log("[VRGunEffectsAuto] Efectos creados y asignados automáticamente.");
    }

    #region Bullet Trail

    private LineRenderer CreateBulletTrail()
    {
        GameObject trailObj = new GameObject("BulletTrail");
        trailObj.transform.SetParent(transform);
        trailObj.transform.localPosition = Vector3.zero;

        LineRenderer lr = trailObj.AddComponent<LineRenderer>();

        // Configurar LineRenderer
        lr.startWidth = trailWidth;
        lr.endWidth = trailWidth / 2f; // Más delgado al final
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.enabled = false; // Desactivado por defecto

        // Material con emisión
        Material trailMat = new Material(Shader.Find("Sprites/Default"));
        trailMat.color = trailColor;
        lr.material = trailMat;

        // Colores
        lr.startColor = trailColor;
        lr.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0.5f);

        Debug.Log("[VRGunEffectsAuto] Bullet Trail creado.");
        return lr;
    }

    #endregion

    #region Impact Effect

    private GameObject CreateImpactEffectPrefab()
    {
        GameObject impactObj = new GameObject("ImpactEffect");
        impactObj.transform.SetParent(transform); // Temporal

        // Añadir ParticleSystem
        ParticleSystem ps = impactObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = 0.3f;
        main.startSpeed = 3f;
        main.startSize = 0.05f;
        main.startColor = impactColor;
        main.maxParticles = impactParticleCount;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, impactParticleCount)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        // Auto-destruir después de reproducir
        impactObj.AddComponent<DestroyAfterParticles>();

        // Hacer prefab (simular)
        impactObj.SetActive(false);

        Debug.Log("[VRGunEffectsAuto] Impact Effect prefab creado.");
        return impactObj;
    }

    #endregion

    #region Muzzle Flash

    private ParticleSystem CreateMuzzleFlash()
    {
        // Buscar Muzzle transform
        Transform muzzle = transform.Find("Muzzle");
        if (muzzle == null)
        {
            // Crear si no existe
            GameObject muzzleObj = new GameObject("Muzzle");
            muzzleObj.transform.SetParent(transform);
            muzzleObj.transform.localPosition = Vector3.forward * 0.3f;
            muzzle = muzzleObj.transform;
        }

        GameObject flashObj = new GameObject("MuzzleFlash");
        flashObj.transform.SetParent(muzzle);
        flashObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = flashObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = 0.1f;
        main.loop = false;
        main.startLifetime = 0.05f;
        main.startSpeed = 0.5f;
        main.startSize = 0.2f;
        main.startColor = muzzleColor;
        main.maxParticles = muzzleParticleCount;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, muzzleParticleCount)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.05f;

        ps.Stop(); // No reproducir automáticamente

        Debug.Log("[VRGunEffectsAuto] Muzzle Flash creado.");
        return ps;
    }

    #endregion

    #region Assign to VRGunWeapon

    private void AssignToGunWeapon(string fieldName, Object value)
    {
        var field = gunWeapon.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            field.SetValue(gunWeapon, value);
            Debug.Log($"[VRGunEffectsAuto] {fieldName} asignado a VRGunWeapon.");
        }
        else
        {
            Debug.LogWarning($"[VRGunEffectsAuto] No se pudo asignar {fieldName}.");
        }
    }

    #endregion
}

/// <summary>
/// Helper: Destruye el GameObject después de que las partículas terminen.
/// </summary>
public class DestroyAfterParticles : MonoBehaviour
{
    private void Start()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            Destroy(gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        else
        {
            Destroy(gameObject, 1f);
        }
    }
}