using UnityEngine;

public class EmissionManager : MonoBehaviour
{
    [System.Serializable]
    public class ArchitecturalElement
    {
        public string elementName;
        public Renderer targetRenderer;
        public Color emissionColor = new Color(1f, 0.8f, 0.4f);
        public float minEmission = 0f;
        public float maxEmission = 2f;

        [HideInInspector] public Material runtimeMaterial;
    }

    public ArchitecturalElement[] elements;
    public float pulseSpeed = 2f;

    private int activeIndex = -1;

    private void Awake()
    {
        foreach (var el in elements)
        {
            if (el.targetRenderer != null)
            {
                el.runtimeMaterial = el.targetRenderer.material;
                el.runtimeMaterial.EnableKeyword("_EMISSION");
                SetEmission(el.runtimeMaterial, Color.black);
            }
        }
    }

    private void Update()
    {
        for (int i = 0; i < elements.Length; i++)
        {
            if (elements[i].runtimeMaterial == null) continue;

            if (i == activeIndex)
            {
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                float intensity = Mathf.Lerp(elements[i].minEmission, elements[i].maxEmission, t);
                SetEmission(elements[i].runtimeMaterial, elements[i].emissionColor * intensity);
            }
            else
            {
                SetEmission(elements[i].runtimeMaterial, Color.black);
            }
        }
    }

    public void HighlightElementByIndex(int index)
    {
        if (index < 0 || index >= elements.Length)
            return;

        if (activeIndex == index)
        {
            ResetAllEmission();
            return;
        }

        activeIndex = index;
    }

    public void ResetAllEmission()
    {
        activeIndex = -1;

        for (int i = 0; i < elements.Length; i++)
        {
            if (elements[i].runtimeMaterial != null)
            {
                SetEmission(elements[i].runtimeMaterial, Color.black);
            }
        }
    }

    private void OnDisable()
    {
        ResetAllEmission();
    }

    private void SetEmission(Material mat, Color emissionColor)
    {
        mat.SetColor("_EmissionColor", emissionColor);
    }

    public void HighlightElement0() => HighlightElementByIndex(0);
    public void HighlightElement1() => HighlightElementByIndex(1);
    public void HighlightElement2() => HighlightElementByIndex(2);
    public void HighlightElement3() => HighlightElementByIndex(3);
    public void HighlightElement4() => HighlightElementByIndex(4);
}
