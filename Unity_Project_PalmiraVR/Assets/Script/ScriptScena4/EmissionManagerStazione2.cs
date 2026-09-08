using UnityEngine;

public class EmissionManagerStazione2 : MonoBehaviour
{
    [Header("Station")]
    public int stationIndex = 1;
    public bool debugEmission;

    [System.Serializable]
    public class ArchitecturalElement
    {
        public string elementName;
        public Renderer[] targetRenderers;
        [ColorUsage(true, true)]
        public Color emissionColor = new Color(1f, 0.8f, 0.4f);
        public float minEmission;
        public float maxEmission;

        [HideInInspector] public Material[][] runtimeMaterials;
    }

    public ArchitecturalElement[] elements;
    public float pulseSpeed;

    private int activeIndex = -1;

    private void Awake()
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

    private void OnEnable()
    {
        LessonFlowController.LessonCompleted += OnLessonCompleted;
    }

    private void Update()
    {
        for (int i = 0; i < elements.Length; i++)
        {
            if (elements[i].runtimeMaterials == null || elements[i].runtimeMaterials.Length == 0)
                continue;

            if (i == activeIndex)
            {
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                float intensity = Mathf.Lerp(elements[i].minEmission, elements[i].maxEmission, t);
                ApplyEmissionToElement(i, elements[i].emissionColor * intensity);
            }
            else
            {
                ApplyEmissionToElement(i, Color.black);
            }
        }
    }

    public void ToggleElementByIndex(int index)
    {
        if (index < 0 || index >= elements.Length)
            return;

        ExperienceAnalyticsLogger.Instance?.LogStation2ArchitectureSelection(elements[index].elementName);

        if (activeIndex == index)
        {
            ResetAllEmission();
            return;
        }

        activeIndex = index;
        ApplyEmissionToElement(index, elements[index].emissionColor * elements[index].maxEmission);

        if (debugEmission)
            LogElementDebugInfo(index);
    }

    public void ResetAllEmission()
    {
        activeIndex = -1;

        for (int i = 0; i < elements.Length; i++)
        {
            if (elements[i].runtimeMaterials == null)
                continue;

            ApplyEmissionToElement(i, Color.black);
        }
    }

    private void OnDisable()
    {
        LessonFlowController.LessonCompleted -= OnLessonCompleted;
        ResetAllEmission();
    }

    private void OnLessonCompleted(int completedStationIndex)
    {
        if (completedStationIndex == stationIndex)
            ResetAllEmission();
    }

    private void SetEmission(Material mat, Color emissionColor)
    {
        mat.SetColor("_EmissionColor", emissionColor);
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

    private void LogElementDebugInfo(int index)
    {
        ArchitecturalElement element = elements[index];
        Debug.Log($"[EmissionManagerStazione2] Toggle '{element.elementName}' index={index}");

        if (element.targetRenderers == null || element.targetRenderers.Length == 0)
        {
            Debug.LogWarning("[EmissionManagerStazione2] Nessun renderer assegnato.");
            return;
        }

        for (int i = 0; i < element.targetRenderers.Length; i++)
        {
            Renderer renderer = element.targetRenderers[i];

            if (renderer == null)
            {
                Debug.LogWarning($"[EmissionManagerStazione2] Renderer {i}: NULL");
                continue;
            }

            Material[] mats = renderer.materials;
            Debug.Log($"[EmissionManagerStazione2] Renderer {i}: '{renderer.name}' materials={mats.Length} enabled={renderer.enabled} activeInHierarchy={renderer.gameObject.activeInHierarchy}");
            Debug.Log($"[EmissionManagerStazione2]  - isPartOfStaticBatch={renderer.isPartOfStaticBatch}");

            for (int j = 0; j < mats.Length; j++)
            {
                Material mat = mats[j];
                if (mat == null)
                {
                    Debug.LogWarning($"[EmissionManagerStazione2]  - Material {j}: NULL");
                    continue;
                }

                bool hasEmission = mat.HasProperty("_EmissionColor");
                Color currentEmission = hasEmission ? mat.GetColor("_EmissionColor") : Color.clear;
                Debug.Log($"[EmissionManagerStazione2]  - Material {j}: '{mat.name}' shader='{mat.shader.name}' hasEmission={hasEmission} currentEmission={currentEmission}");
            }
        }
    }

    public void ToggleElement0() => ToggleElementByIndex(0);
    public void ToggleElement1() => ToggleElementByIndex(1);
    public void ToggleElement2() => ToggleElementByIndex(2);
    public void ToggleElement3() => ToggleElementByIndex(3);
    public void ToggleElement4() => ToggleElementByIndex(4);
}
