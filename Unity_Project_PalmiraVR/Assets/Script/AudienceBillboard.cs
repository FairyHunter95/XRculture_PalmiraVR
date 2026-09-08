using Unity.XR.CoreUtils;
using UnityEngine;

public class AudienceBillboard : MonoBehaviour
{
    public XROrigin xrOrigin;
    public Transform targetOverride;
    public bool yawOnly = true;
    public float rotationSpeed = 6f;
    public Vector3 eulerOffset = new Vector3(0f, 180f, 0f);

    private Transform m_Target;

    private void LateUpdate()
    {
        if (m_Target == null)
            ResolveTarget();

        if (m_Target == null)
            return;

        Vector3 direction = m_Target.position - transform.position;
        if (yawOnly)
            direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up) * Quaternion.Euler(eulerOffset);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void ResolveTarget()
    {
        if (targetOverride != null)
        {
            m_Target = targetOverride;
            return;
        }

        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            m_Target = xrOrigin.Camera.transform;
            return;
        }

        if (Camera.main != null)
            m_Target = Camera.main.transform;
    }
}
