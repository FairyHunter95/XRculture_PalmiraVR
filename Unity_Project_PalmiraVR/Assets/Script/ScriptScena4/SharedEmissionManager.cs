using UnityEngine;

public class SharedEmissionManager : MonoBehaviour
{
    [System.Serializable]
    public class ArchitecturalElement
    {
        public string elementName;
        public Renderer[] targetRenderers;
        [ColorUsage(true, true)]
        public Color emissionColor = new Color(1f, 0.8f, 0.4f);
        public float minEmission = 0.2f;
        public float maxEmission = 0.8f;

        [HideInInspector] public Material[][] runtimeMaterials;
    }

    public ArchitecturalElement[] elements;
    public float pulseSpeed = 2f;

    private int activeIndex = -1;

    private void Awake()
    {
        InitializeMaterials();
    }

    private void Update()
    {
        for (int i = 0; i < elements.Length; i++)
        {
            if (elements[i].runtimeMaterials == null || elements[i].runtimeMaterials.Length == 0)
                continue;

            Color emissionColor = Color.black;
            if (i == activeIndex)
            {
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                float intensity = Mathf.Lerp(elements[i].minEmission, elements[i].maxEmission, t);
                emissionColor = elements[i].emissionColor * intensity;
            }

            ApplyEmissionToElement(i, emissionColor);
        }
    }

    public void ToggleElementByIndex(int index)
    {
        if (index < 0 || index >= elements.Length)
            return;

        if (activeIndex == index)
        {
            ResetAllEmission();
            return;
        }

        activeIndex = index;
        ApplyEmissionToElement(index, elements[index].emissionColor * elements[index].maxEmission);
    }

    public void ActivateElementByIndex(int index)
    {
        if (index < 0 || index >= elements.Length)
            return;

        activeIndex = index;
        ApplyEmissionToElement(index, elements[index].emissionColor * elements[index].maxEmission);
    }

    public void ResetAllEmission()
    {
        activeIndex = -1;

        for (int i = 0; i < elements.Length; i++)
            ApplyEmissionToElement(i, Color.black);
    }

    private void OnDisable()
    {
        ResetAllEmission();
    }

    private void InitializeMaterials()
    {
        foreach (var el in elements)
        {
            if (el.targetRenderers == null || el.targetRenderers.Length == 0)
                continue;

            el.runtimeMaterials = new Material[el.targetRenderers.Length][];

            for (int i = 0; i < el.targetRenderers.Length; i++)
            {
                Renderer targetRenderer = el.targetRenderers[i];
                if (targetRenderer == null)
                    continue;

                Material[] rendererMaterials = targetRenderer.materials;
                if (rendererMaterials == null || rendererMaterials.Length == 0)
                    continue;

                for (int j = 0; j < rendererMaterials.Length; j++)
                {
                    Material runtimeMaterial = rendererMaterials[j];
                    if (runtimeMaterial == null)
                        continue;

                    runtimeMaterial.EnableKeyword("_EMISSION");
                    SetEmission(runtimeMaterial, Color.black);
                }

                el.runtimeMaterials[i] = rendererMaterials;
            }
        }
    }

    private void ApplyEmissionToElement(int index, Color emissionColor)
    {
        if (index < 0 || index >= elements.Length)
            return;

        if (elements[index].runtimeMaterials == null)
            return;

        foreach (var rendererMaterials in elements[index].runtimeMaterials)
        {
            if (rendererMaterials == null)
                continue;

            foreach (var runtimeMaterial in rendererMaterials)
            {
                if (runtimeMaterial != null)
                    SetEmission(runtimeMaterial, emissionColor);
            }
        }
    }

    private void SetEmission(Material mat, Color emissionColor)
    {
        mat.SetColor("_EmissionColor", emissionColor);
    }
}
