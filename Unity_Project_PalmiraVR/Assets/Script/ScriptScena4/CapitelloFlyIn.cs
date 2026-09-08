using System.Collections;
using UnityEngine;

public class CapitelloFlyIn : MonoBehaviour
{
    [Header("References")]
    public GameObject capitelloGhost;
    public Transform startPoint;
    public Transform targetAnchor;

    [Header("Materials (optional)")]
    public Material ghostMaterial;
    public Material finalMaterial;

    [Header("Animation")]
    public float moveDuration = 1.0f;
    public bool alsoScale = true;
    public Vector3 startScale = Vector3.one * 0.7f;
    public Vector3 endScale = Vector3.one;

    [Header("Timing")]
    public float delay = 0.5f;

    
    private void OnEnable()
    {
        Play();
    }

    public void Play()
    {
        StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        if (capitelloGhost == null || targetAnchor == null)
            yield break;

        yield return new WaitForSeconds(delay);

        capitelloGhost.SetActive(true);

        Vector3 fromPos = startPoint != null ? startPoint.position : capitelloGhost.transform.position;
        Quaternion fromRot = startPoint != null ? startPoint.rotation : capitelloGhost.transform.rotation;

        Vector3 toPos = targetAnchor.position;
        Quaternion toRot = targetAnchor.rotation;

        capitelloGhost.transform.SetPositionAndRotation(fromPos, fromRot);

        if (alsoScale)
            capitelloGhost.transform.localScale = startScale;

        if (ghostMaterial != null)
            SetAllMaterials(capitelloGhost, ghostMaterial);

        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / moveDuration);

            capitelloGhost.transform.position = Vector3.Lerp(fromPos, toPos, u);
            capitelloGhost.transform.rotation = Quaternion.Slerp(fromRot, toRot, u);

            if (alsoScale)
                capitelloGhost.transform.localScale = Vector3.Lerp(startScale, endScale, u);

            yield return null;
        }

        capitelloGhost.transform.SetPositionAndRotation(toPos, toRot);
        if (alsoScale) capitelloGhost.transform.localScale = endScale;

        if (finalMaterial != null)
            SetAllMaterials(capitelloGhost, finalMaterial);
    }

    void SetAllMaterials(GameObject go, Material mat)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = mat;
            r.sharedMaterials = mats;
        }
    }
}
