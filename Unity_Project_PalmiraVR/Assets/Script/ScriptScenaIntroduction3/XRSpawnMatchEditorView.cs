using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRSpawnMatchEditorView : MonoBehaviour
{
    public XROrigin xrOrigin;
    public Transform targetSpawn;   // dove vuoi la CAMERA (world)
    public Transform targetYaw;     // direzione desiderata (usa la forward)

    public bool alignOnStart = true;
    public bool forceOriginY = false;
    public float forcedOriginYValue = 0f;

    void Start()
    {
        if (alignOnStart) AlignNow();
    }

    [ContextMenu("AlignNow")]
    public void AlignNow()
    {
        if (xrOrigin == null || targetSpawn == null || targetYaw == null) return;

        Transform cam = xrOrigin.Camera.transform;

        // 1) posizione: porta la CAMERA esattamente sullo spawn
        Vector3 camToOrigin = cam.position - xrOrigin.transform.position;
        xrOrigin.transform.position = targetSpawn.position - camToOrigin;

        // 2) rotazione: allinea la yaw della CAMERA alla yaw del target
        float desiredYaw = targetYaw.eulerAngles.y;
        float currentCamYaw = cam.eulerAngles.y;
        float deltaYaw = desiredYaw - currentCamYaw;

        xrOrigin.transform.Rotate(0f, deltaYaw, 0f, Space.World);

        if (forceOriginY)
        {
            Vector3 originPosition = xrOrigin.transform.position;
            originPosition.y = forcedOriginYValue;
            xrOrigin.transform.position = originPosition;
        }
    }
}
