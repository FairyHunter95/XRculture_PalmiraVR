using UnityEngine;

public class EmissionManagerStazione4 : MonoBehaviour
{
    [System.Serializable]
    public class ArchitecturalElement
    {
        public string elementName;
        public Renderer[] targetRenderers;
        public Color emissionColor = new Color(1f, 0.8f, 0.4f);
        public float minEmission = 0.2f;
        public float maxEmission = 0.8f;

        [HideInInspector] public Material[] runtimeMaterials;
    }

    public ArchitecturalElement[] elements;
    public float pulseSpeed = 2f;

    private int activeIndex = -1;

    private void Awake()
    {
        foreach (var el in elements)
        {
            if (el.targetRenderers == null || el.targetRenderers.Length == 0)
                continue;

            el.runtimeMaterials = new Material[el.targetRenderers.Length];

            for (int i = 0; i < el.targetRenderers.Length; i++)
            {
                Renderer targetRenderer = el.targetRenderers[i];
                if (targetRenderer == null)
                    continue;

                Material runtimeMaterial = targetRenderer.material;
                runtimeMaterial.EnableKeyword("_EMISSION");
                SetEmission(runtimeMaterial, Color.black);
                el.runtimeMaterials[i] = runtimeMaterial;
            }
        }
    }

    private void Update()
    {
        for (int i = 0; i < elements.Length; i++)
        {
            if (elements[i].runtimeMaterials == null || elements[i].runtimeMaterials.Length == 0)
                continue;

            Color targetColor = Color.black;
            if (i == activeIndex)
            {
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                float intensity = Mathf.Lerp(elements[i].minEmission, elements[i].maxEmission, t);
                targetColor = elements[i].emissionColor * intensity;
            }

            foreach (var runtimeMaterial in elements[i].runtimeMaterials)
            {
                if (runtimeMaterial != null)
                    SetEmission(runtimeMaterial, targetColor);
            }
        }
    }

    public void ActivateElementByIndex(int index)
    {
        if (index < 0 || index >= elements.Length)
            return;

        activeIndex = index;
    }

    public void ResetAllEmission()
    {
        activeIndex = -1;

        for (int i = 0; i < elements.Length; i++)
        {
            if (elements[i].runtimeMaterials == null)
                continue;

            foreach (var runtimeMaterial in elements[i].runtimeMaterials)
            {
                if (runtimeMaterial != null)
                    SetEmission(runtimeMaterial, Color.black);
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
}
