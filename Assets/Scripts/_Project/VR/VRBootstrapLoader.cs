using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// VERSIÓN ARREGLADA - Soluciona problemas de movimiento flotante y bugueado
/// </summary>
public class VRBootstrapLoader : MonoBehaviour
{
    [Header("Scene names (must match Build Settings)")]
    [SerializeField] private string mainSceneName = "Main";

    [Header("XR Rig reference (optional, auto-find if empty)")]
    [SerializeField] private XROrigin xrOrigin;

    [Header("Legacy Player (Main scene)")]
    [SerializeField] private string legacyPlayerTag = "Player";
    [SerializeField] private bool setLegacyPlayerVrMode = true;

    [Header("Optional cleanup to avoid duplicates")]
    [SerializeField] private bool disableScreenSpaceCanvases = false;
    [SerializeField] private bool disableLegacyMouseLook = true;
    [SerializeField] private bool disableAllCamerasExceptXr = true;
    [SerializeField] private bool fixEventSystems = true;

    [Header("Optional: hide legacy FPS gun model")]
    [SerializeField] private bool hideLegacyGun = true;
    [SerializeField] private string legacyGunChildName = "NewGun_auto";

    [Header("VR Notes")]
    [SerializeField] private bool setupNotesForVR = true;

    private IEnumerator Start()
    {
        // 1) Cargar Main en Additive
        if (!SceneManager.GetSceneByName(mainSceneName).isLoaded)
        {
            var op = SceneManager.LoadSceneAsync(mainSceneName, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
        }

        // 2) Encontrar XR Origin
        if (xrOrigin == null) xrOrigin = FindObjectOfType<XROrigin>(true);

        // 3) Encontrar Player legacy (Main)
        GameObject legacyPlayer = GameObject.FindWithTag(legacyPlayerTag);
        if (legacyPlayer == null)
        {
            var pmFallback = FindObjectOfType<PlayerMovementQ>(true);
            if (pmFallback != null) legacyPlayer = pmFallback.gameObject;
        }

        // 4) Posicionar el XR rig en el spawn del Player legacy
        if (legacyPlayer != null && xrOrigin != null)
        {
            xrOrigin.transform.position = legacyPlayer.transform.position;
            xrOrigin.transform.rotation = Quaternion.Euler(0f, legacyPlayer.transform.eulerAngles.y, 0f);

            // ⭐ ARREGLO CRÍTICO: Desactivar COMPLETAMENTE el sistema de movimiento legacy
            if (setLegacyPlayerVrMode)
            {
                var pm = legacyPlayer.GetComponent<PlayerMovementQ>();
                if (pm != null)
                {
                    pm.vrMode = true;
                    pm.enabled = false; // ⭐ Desactivar script completamente (evita conflictos)
                    Debug.Log("[VRBootstrapLoader] PlayerMovementQ desactivado.");
                }

                // Desactivar CharacterController legacy (evita conflicto de física)
                var cc = legacyPlayer.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.enabled = false;
                    Debug.Log("[VRBootstrapLoader] CharacterController legacy desactivado.");
                }

                // Desactivar Rigidbody si existe
                var rb = legacyPlayer.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    Debug.Log("[VRBootstrapLoader] Rigidbody legacy en modo kinematic.");
                }

                // Hacer el Player legacy hijo del XR Origin (se mueve junto al headset)
                legacyPlayer.transform.SetParent(xrOrigin.transform);
                legacyPlayer.transform.localPosition = Vector3.zero;
                legacyPlayer.transform.localRotation = Quaternion.identity;
                Debug.Log("[VRBootstrapLoader] Player legacy ahora es hijo de XR Origin.");
            }

            // Oculta el arma FPS legacy 
            if (hideLegacyGun)
            {
                var gun = legacyPlayer.transform.Find(legacyGunChildName);
                if (gun != null) gun.gameObject.SetActive(false);
            }
        }

        // 5) Apagar mouse look legacy (si existe)
        if (disableLegacyMouseLook)
        {
            var camCtrl = FindObjectOfType<CameraController>(true);
            if (camCtrl != null) camCtrl.enabled = false;
        }

        // 6) Apagar canvases 2D 
        if (disableScreenSpaceCanvases)
        {
            foreach (var c in FindObjectsOfType<Canvas>(true))
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
                    c.enabled = false;
            }
        }

        // 7) Dejar solo 1 cámara + 1 AudioListener (la cámara del XR rig)
        if (disableAllCamerasExceptXr && xrOrigin != null)
        {
            var xrCam = xrOrigin.Camera;

            foreach (var cam in FindObjectsOfType<Camera>(true))
            {
                if (xrCam != null && cam == xrCam) continue;

                cam.enabled = false;

                var al = cam.GetComponent<AudioListener>();
                if (al != null) al.enabled = false;
            }

            // Asegurar AudioListener en la cámara VR
            if (xrCam != null)
            {
                var xrListener = xrCam.GetComponent<AudioListener>();
                if (xrListener == null) xrCam.gameObject.AddComponent<AudioListener>();
                else xrListener.enabled = true;
            }
        }

        // 8) Dejar solo 1 EventSystem (evita warning de duplicados)
        if (fixEventSystems)
        {
            var systems = FindObjectsOfType<EventSystem>(true);
            if (systems.Length > 1)
            {
                EventSystem keep = null;

#if ENABLE_INPUT_SYSTEM
                // Preferimos el EventSystem con InputSystemUIInputModule (VR)
                foreach (var es in systems)
                {
                    if (es.GetComponent<InputSystemUIInputModule>() != null)
                    {
                        keep = es;
                        break;
                    }
                }
#endif
                // Fallback: si no encontramos uno "VR", nos quedamos con el primero
                if (keep == null) keep = systems[0];

                foreach (var es in systems)
                {
                    if (es != keep) es.gameObject.SetActive(false);
                }
            }
        }

        // 9) Espera 1 frame extra para que corran Start() de objetos en Main (Notas, etc.)
        yield return null;


        Debug.Log("[VRBootstrapLoader] Main cargada y VR preparado correctamente.");
    }
}