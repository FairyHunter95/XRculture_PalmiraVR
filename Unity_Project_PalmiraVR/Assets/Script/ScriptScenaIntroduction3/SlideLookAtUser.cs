using Unity.XR.CoreUtils;
using UnityEngine;

public class SlideLookAtUser : MonoBehaviour
{
    [Header("Target")]
    public XROrigin xrOrigin;
    public Transform targetOverride;

    [Header("Rotation")]
    [Tooltip("Se attivo, ruota solo sull'asse Y per mantenere la slide dritta.")]
    public bool yawOnly = true;

    [Tooltip("Velocita' di rotazione della slide verso l'utente.")]
    public float rotationSpeed = 4f;

    [Tooltip("Angolo minimo in gradi prima che la slide inizi a ruotare verso l'utente.")]
    public float minAngleToRotate = 8f;

    [Tooltip("Offset aggiuntivo in gradi applicato dopo il look-at.")]
    public Vector3 eulerOffset;

    [Tooltip("Se attivo, la slide mantiene la rotazione iniziale su assi non gestiti.")]
    public bool preserveInitialTilt = true;

    private Transform m_Target;
    private Quaternion m_InitialRotation;

    private void Awake()
    {
        m_InitialRotation = transform.rotation;
        ResolveTarget();
    }

    private void LateUpdate()
    {
        if (m_Target == null)
        {
            ResolveTarget();
            if (m_Target == null)
                return;
        }

        Vector3 targetPosition = m_Target.position;
        Vector3 direction = targetPosition - transform.position;

        if (yawOnly)
            direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        if (preserveInitialTilt && yawOnly)
        {
            Vector3 euler = lookRotation.eulerAngles;
            lookRotation = Quaternion.Euler(m_InitialRotation.eulerAngles.x, euler.y, m_InitialRotation.eulerAngles.z);
        }

        Quaternion finalRotation = lookRotation * Quaternion.Euler(0f, 180f, 0f) * Quaternion.Euler(eulerOffset);

        float angleToTarget = Quaternion.Angle(transform.rotation, finalRotation);
        if (angleToTarget < minAngleToRotate)
            return;

        transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, rotationSpeed * Time.deltaTime);
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
        {
            m_Target = Camera.main.transform;
        }
    }
}
