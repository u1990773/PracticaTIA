using UnityEngine;

/// <summary>
/// Añade un laser sight al arma para ver dónde apunta.
/// CRÍTICO para saber dónde vas a disparar en VR.
/// </summary>
public class VRGunLaserSight : MonoBehaviour
{
    [Header("Laser Settings")]
    [SerializeField] private bool laserEnabled = true;
    [SerializeField] private float laserMaxDistance = 50f;
    [SerializeField] private Color laserColor = Color.red;
    [SerializeField] private float laserWidth = 0.005f;

    [Header("References")]
    [SerializeField] private Transform muzzle; // Punto de origen del laser
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private GameObject laserDot; // Punto al final del laser (opcional)

    [Header("Auto-Setup")]
    [SerializeField] private bool autoCreateComponents = true;

    [Header("Hit Detection")]
    [SerializeField] private LayerMask hitLayers = ~0;

    private VRGunWeapon gunWeapon;

    private void Start()
    {
        gunWeapon = GetComponent<VRGunWeapon>();

        // Auto-encontrar muzzle
        if (muzzle == null)
        {
            muzzle = transform.Find("Muzzle");
            if (muzzle == null)
            {
                // Crear muzzle
                GameObject muzzleObj = new GameObject("Muzzle");
                muzzleObj.transform.SetParent(transform);
                muzzleObj.transform.localPosition = Vector3.forward * 0.3f;
                muzzle = muzzleObj.transform;
            }
        }

        // Crear LineRenderer si no existe
        if (lineRenderer == null && autoCreateComponents)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            ConfigureLineRenderer();
        }

        // Crear laser dot si no existe
        if (laserDot == null && autoCreateComponents)
        {
            laserDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            laserDot.name = "LaserDot";
            laserDot.transform.SetParent(transform);
            laserDot.transform.localScale = Vector3.one * 0.02f;

            // Hacer que brille
            var renderer = laserDot.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.SetColor("_EmissionColor", laserColor * 2f);
                renderer.material.EnableKeyword("_EMISSION");
            }

            // Quitar collider
            Destroy(laserDot.GetComponent<Collider>());
        }

        Debug.Log("[VRGunLaserSight] Laser sight configurado.");
    }

    private void ConfigureLineRenderer()
    {
        lineRenderer.startWidth = laserWidth;
        lineRenderer.endWidth = laserWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = laserColor;
        lineRenderer.endColor = laserColor;

        // Hacer que brille
        lineRenderer.material.EnableKeyword("_EMISSION");
        lineRenderer.material.SetColor("_EmissionColor", laserColor * 2f);
    }

    private void Update()
    {
        // Solo mostrar laser si el arma está agarrada
        bool shouldShowLaser = laserEnabled && gunWeapon != null && gunWeapon.IsGrabbed();

        if (lineRenderer != null)
        {
            lineRenderer.enabled = shouldShowLaser;
        }

        if (laserDot != null)
        {
            laserDot.SetActive(shouldShowLaser);
        }

        if (!shouldShowLaser) return;

        // Calcular donde apunta el laser
        Ray ray = new Ray(muzzle.position, muzzle.forward);
        RaycastHit hit;

        Vector3 endPoint;

        if (Physics.Raycast(ray, out hit, laserMaxDistance, hitLayers))
        {
            // Impacta con algo
            endPoint = hit.point;
        }
        else
        {
            // No impacta, extender al máximo
            endPoint = muzzle.position + muzzle.forward * laserMaxDistance;
        }

        // Actualizar LineRenderer
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, muzzle.position);
            lineRenderer.SetPosition(1, endPoint);
        }

        // Actualizar laser dot
        if (laserDot != null)
        {
            laserDot.transform.position = endPoint;
        }
    }

    /// <summary>
    /// Activa/desactiva el laser.
    /// </summary>
    public void SetLaserEnabled(bool enabled)
    {
        laserEnabled = enabled;
    }

    /// <summary>
    /// Cambia el color del laser.
    /// </summary>
    public void SetLaserColor(Color color)
    {
        laserColor = color;

        if (lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }
}