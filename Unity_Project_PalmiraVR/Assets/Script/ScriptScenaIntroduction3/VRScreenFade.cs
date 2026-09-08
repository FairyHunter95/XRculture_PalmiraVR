using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

public class VRScreenFade : MonoBehaviour
{
    [Header("Target")]
    public XROrigin xrOrigin;
    public Transform targetCameraOverride;

    [Header("Fade")]
    public Color fadeColor = Color.black;
    public float planeDistance = 0.3f;
    public Vector2 planeSize = new Vector2(4f, 4f);

    private MeshRenderer m_FadeRenderer;
    private Material m_FadeMaterial;

    public IEnumerator FadeToBlack(float duration)
    {
        EnsureOverlay();
        if (m_FadeMaterial == null)
            yield break;

        float elapsed = 0f;
        fadeColor = Color.black;
        SetAlpha(0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * t;
            SetAlpha(t);
            yield return null;
        }

        SetAlpha(1f);
    }

    public IEnumerator FadeFromBlack(float duration)
    {
        EnsureOverlay();
        if (m_FadeMaterial == null)
            yield break;

        float elapsed = 0f;
        fadeColor = Color.black;
        SetAlpha(1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * t;
            SetAlpha(1f - t);
            yield return null;
        }

        SetAlpha(0f);
    }

    private void EnsureOverlay()
    {
        if (m_FadeRenderer != null && m_FadeMaterial != null)
            return;

        Transform targetCamera = ResolveTargetCamera();
        if (targetCamera == null)
            return;

        var fadeRoot = new GameObject("VR Fade Quad");
        fadeRoot.transform.SetParent(targetCamera, false);
        fadeRoot.transform.localPosition = new Vector3(0f, 0f, planeDistance);
        fadeRoot.transform.localRotation = Quaternion.identity;
        fadeRoot.transform.localScale = new Vector3(planeSize.x, planeSize.y, 1f);

        var meshFilter = fadeRoot.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateQuadMesh();

        m_FadeRenderer = fadeRoot.AddComponent<MeshRenderer>();

        var fadeShader = Shader.Find("Sprites/Default");
        if (fadeShader == null)
            fadeShader = Shader.Find("Unlit/Transparent");

        m_FadeMaterial = new Material(fadeShader);
        m_FadeMaterial.mainTexture = Texture2D.whiteTexture;
        m_FadeMaterial.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        m_FadeRenderer.sharedMaterial = m_FadeMaterial;
        m_FadeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        m_FadeRenderer.receiveShadows = false;
        m_FadeRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        m_FadeRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        SetAlpha(0f);
    }

    private Transform ResolveTargetCamera()
    {
        if (targetCameraOverride != null)
            return targetCameraOverride;

        if (xrOrigin != null && xrOrigin.Camera != null)
            return xrOrigin.Camera.transform;

        if (Camera.main != null)
            return Camera.main.transform;

        return null;
    }

    private void SetAlpha(float alpha)
    {
        if (m_FadeMaterial == null)
            return;

        Color color = Color.black;
        color.a = alpha;
        m_FadeMaterial.color = color;
    }

    private static Mesh CreateQuadMesh()
    {
        var mesh = new Mesh();
        mesh.name = "FadeQuad";
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f),
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        return mesh;
    }
}
