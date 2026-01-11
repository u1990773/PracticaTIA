using UnityEngine;
using Unity.XR.CoreUtils;

public class VRPlayerSync : MonoBehaviour
{
    public XROrigin xrOrigin;
    public Transform legacyPlayer; // el Player con tag "Player"
    public bool lockY = true;

    private CharacterController legacyCC;

    void Start()
    {
        if (xrOrigin == null) xrOrigin = FindObjectOfType<XROrigin>(true);

        if (legacyPlayer == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null) legacyPlayer = go.transform;
        }

        if (legacyPlayer != null)
            legacyCC = legacyPlayer.GetComponent<CharacterController>();
    }

    void LateUpdate()
    {
        if (legacyPlayer == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null)
            {
                legacyPlayer = go.transform;
                legacyCC = go.GetComponent<CharacterController>();
                Debug.Log("[VRPlayerSync] Legacy Player encontrado: " + go.name);
            }
        }

        if (xrOrigin == null || xrOrigin.Camera == null || legacyPlayer == null) return;

        var headPos = xrOrigin.Camera.transform.position;

        if (lockY) headPos.y = legacyPlayer.position.y;

        var delta = headPos - legacyPlayer.position;

        if (legacyCC != null && legacyCC.enabled)
            legacyCC.Move(delta);
        else
            legacyPlayer.position = headPos;

        // rotación solo en Y
        var yaw = xrOrigin.Camera.transform.eulerAngles.y;
        legacyPlayer.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}
