using UnityEngine;

public class MarkerLabelAutoHide : MonoBehaviour
{
    [Header("Refs")]
    public Transform playerCamera;     // Main Camera (XR)
    public GameObject labelObject;     // Label (TMP)

    [Header("Behavior")]
    public float hideDistance = 3.0f;  // sotto questa distanza la label sparisce

    void Reset()
    {
        labelObject = gameObject;
        if (Camera.main != null) playerCamera = Camera.main.transform;
    }

    void Update()
    {
        if (playerCamera == null || labelObject == null) return;

        float d = Vector3.Distance(playerCamera.position, transform.position);
        bool shouldShow = d > hideDistance;

        if (labelObject.activeSelf != shouldShow)
            labelObject.SetActive(shouldShow);
    }
}
