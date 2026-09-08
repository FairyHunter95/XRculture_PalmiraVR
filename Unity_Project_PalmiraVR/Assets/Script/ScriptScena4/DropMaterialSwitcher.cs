using UnityEngine;
using TMPro;

public class DropdownMaterialSwitcher : MonoBehaviour
{
    [Header("Dropdown UI")]
    public TMP_Dropdown dropdown;

    [Header("Target Renderers (se vuoto li cerca da solo)")]
    public Renderer[] renderers;

    [Header("Materials (ordine = opzioni dropdown)")]
    public Material[] materials;

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        if (dropdown != null)
            dropdown.onValueChanged.AddListener(OnDropdownChanged);
    }

    private void OnDestroy()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
    }

    public void OnDropdownChanged(int index)
    {
        if (materials == null || materials.Length == 0) return;
        if (index < 0 || index >= materials.Length) return;

        ExperienceAnalyticsLogger.Instance?.LogCapitelloMaterialChanged();
        ApplyMaterial(materials[index]);
    }

    private void ApplyMaterial(Material mat)
    {
        foreach (var r in renderers)
        {
            if (r == null) continue;

            // Applica lo stesso materiale su tutte le submesh
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = mat;

            r.sharedMaterials = mats;
        }
    }
}
