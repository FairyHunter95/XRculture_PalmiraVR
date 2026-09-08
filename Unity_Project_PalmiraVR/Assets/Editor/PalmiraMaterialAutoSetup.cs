using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PalmiraMaterialAutoSetup
{
    private const string MaterialsFolder = "Assets/ModelloFinale/MaterialsPalmira";
    private const string TexturesFolder = "Assets/ModelloFinale/Plamira3dunity_Textures";
    private const string GeneratedFolder = "Assets/ModelloFinale/GeneratedPalmiraMaps";
    private static readonly Dictionary<string, string> BakedTextureMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["base colonne"] = "baseultima_bake",
        ["MAT_CAVEA"] = "caveaultima_Bake",
        ["MAT_COLONNE ESTERNE"] = "COLONNE_ESTERNE_BAKE",
        ["MAT_COLONNE SCENA CENTRALE"] = "colonnecentraliultime_bake",
        ["MAT_COLONNE SCENA DESTRA"] = "colonnedxultima_bake",
        ["MAT_COLONNE SCENA SINISTRA"] = "colonnesinistraultimo_ake",
        ["MAT_FRONTONE DECORAZIONE"] = "frontonedecorazioneultima_bake",
        ["MAT_FRONTONE"] = "frontoneultimo_bake",
        ["MAT_MURO CENTRALE"] = "murograndeultimo_bake",
        ["MAT_MURO DESTRA"] = "sfondodxultimo_bake",
        ["MAT_MURO SINISTRA"] = "murosinistraultimo_bake",
        ["MAT_PROSCAENIUM"] = "proscenioultimo_bake",
        ["MAT_PULPITUM"] = "pulpitumultimo_bake",
        ["MAT_SABBIA"] = "sabbiaultimo_bake",
        ["MAT_TRABEAZIONE DECORAZIONE"] = "trabeazioneultimo_bake",
        ["MAT_TRABEAZIONE DESTRA"] = "trabeazionedxultimo_bake",
        ["MAT_TRABEAZIONE SINISTRA"] = "trabeazionesx_bake",
        ["MAT_TRABEAZIONI ESTERNE"] = "trabeazioneesterna_bake",
    };

    [MenuItem("Tools/Palmira/Auto Setup Extracted Materials")]
    public static void AutoSetupExtractedMaterials()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            EditorUtility.DisplayDialog(
                "Palmira Materials",
                "Shader URP/Lit non trovato. Verifica che il progetto usi URP.",
                "OK");
            return;
        }

        EnsureFolder("Assets/ModelloFinale", "GeneratedPalmiraMaps");

        var texturePaths = AssetDatabase.FindAssets("t:Texture2D", new[] { TexturesFolder });
        var texturesByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var guid in texturePaths)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (!texturesByName.ContainsKey(fileName))
            {
                texturesByName[fileName] = path;
            }
        }

        var materialGuids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder });
        var updatedCount = 0;

        foreach (var guid in materialGuids)
        {
            var materialPath = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                continue;
            }

            Undo.RecordObject(material, "Auto setup Palmira material");
            material.shader = shader;

            var materialName = material.name;
            var prefix = ResolveTexturePrefix(materialName, texturesByName);

            AssignBaseMap(material, prefix, texturesByName);
            AssignNormalMap(material, prefix, texturesByName);
            AssignOcclusionMap(material, prefix, texturesByName);
            AssignMetallicSmoothness(material, prefix, texturesByName);

            material.SetFloat("_WorkflowMode", 1f);
            material.SetFloat("_Metallic", 0f);
            if (material.GetTexture("_MetallicGlossMap") == null)
            {
                material.SetFloat("_Smoothness", 0.22f);
            }
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_OCCLUSIONMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");

            EditorUtility.SetDirty(material);
            updatedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Palmira Materials",
            $"Materiali aggiornati: {updatedCount}. Controlla il modello in scena.",
            "OK");
    }

    [MenuItem("Tools/Palmira/Apply Baked Look")]
    public static void ApplyBakedLook()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Palmira Materials", "Shader URP/Lit non trovato.", "OK");
            return;
        }

        var texturePaths = AssetDatabase.FindAssets("t:Texture2D", new[] { TexturesFolder });
        var texturesByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var guid in texturePaths)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (!texturesByName.ContainsKey(fileName))
            {
                texturesByName[fileName] = path;
            }
        }

        var materialGuids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder });
        var updatedCount = 0;

        foreach (var guid in materialGuids)
        {
            var materialPath = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                continue;
            }

            if (!BakedTextureMap.TryGetValue(material.name, out var bakedName))
            {
                continue;
            }

            var bakedPath = FindTexturePath(bakedName, texturesByName);
            if (string.IsNullOrEmpty(bakedPath))
            {
                continue;
            }

            var bakedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(bakedPath);
            if (bakedTexture == null)
            {
                continue;
            }

            Undo.RecordObject(material, "Apply Palmira baked look");
            material.shader = shader;
            material.SetTexture("_BaseMap", bakedTexture);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_MetallicGlossMap", null);
            material.SetTexture("_OcclusionMap", null);
            material.SetTexture("_BumpMap", null);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0f);
            material.DisableKeyword("_METALLICSPECGLOSSMAP");
            material.DisableKeyword("_NORMALMAP");
            material.DisableKeyword("_OCCLUSIONMAP");
            EditorUtility.SetDirty(material);
            updatedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Palmira Materials",
            $"Applicato look baked a {updatedCount} materiali.",
            "OK");
    }

    private static string ResolveTexturePrefix(string materialName, Dictionary<string, string> texturesByName)
    {
        string[] candidates =
        {
            materialName,
            materialName.Replace(".001", string.Empty),
            materialName.Replace("_", " "),
        };

        foreach (var candidate in candidates)
        {
            if (HasTexture(candidate + "_Base_color", texturesByName) ||
                HasTexture(candidate + "_BaseColor", texturesByName) ||
                HasTexture(candidate + "_Normal", texturesByName))
            {
                return candidate;
            }
        }

        if (materialName.Equals("base colonne", StringComparison.OrdinalIgnoreCase))
        {
            return "base colonne";
        }

        return materialName;
    }

    private static void AssignBaseMap(Material material, string prefix, Dictionary<string, string> texturesByName)
    {
        var texture = LoadTexture(prefix + "_Base_color", texturesByName)
                      ?? LoadTexture(prefix + "_BaseColor", texturesByName);
        if (texture == null)
        {
            return;
        }

        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
    }

    private static void AssignNormalMap(Material material, string prefix, Dictionary<string, string> texturesByName)
    {
        var texturePath = FindTexturePath(prefix + "_Normal", texturesByName);
        if (string.IsNullOrEmpty(texturePath))
        {
            texturePath = FindTexturePath(prefix + "_Normal_OpenGL", texturesByName);
        }

        if (string.IsNullOrEmpty(texturePath))
        {
            return;
        }

        EnsureNormalMapImporter(texturePath);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            return;
        }

        material.SetTexture("_BumpMap", texture);
        material.SetFloat("_BumpScale", 1f);
    }

    private static void AssignOcclusionMap(Material material, string prefix, Dictionary<string, string> texturesByName)
    {
        var texture = LoadTexture(prefix + "_Mixed_AO", texturesByName);
        if (texture == null)
        {
            return;
        }

        material.SetTexture("_OcclusionMap", texture);
        material.SetFloat("_OcclusionStrength", 1f);
    }

    private static void AssignMetallicSmoothness(Material material, string prefix, Dictionary<string, string> texturesByName)
    {
        var metallicPath = FindTexturePath(prefix + "_Metallic", texturesByName);
        var roughnessPath = FindTexturePath(prefix + "_Roughness", texturesByName);
        if (string.IsNullOrEmpty(metallicPath) && string.IsNullOrEmpty(roughnessPath))
        {
            return;
        }

        var generatedPath = BuildMetallicSmoothnessTexture(prefix, metallicPath, roughnessPath);
        if (string.IsNullOrEmpty(generatedPath))
        {
            return;
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(generatedPath);
        if (texture == null)
        {
            return;
        }

        material.SetTexture("_MetallicGlossMap", texture);
        material.SetFloat("_Smoothness", 1f);
    }

    private static Texture2D LoadTexture(string textureName, Dictionary<string, string> texturesByName)
    {
        var path = FindTexturePath(textureName, texturesByName);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static string FindTexturePath(string textureName, Dictionary<string, string> texturesByName)
    {
        return texturesByName.TryGetValue(textureName, out var path) ? path : null;
    }

    private static bool HasTexture(string textureName, Dictionary<string, string> texturesByName)
    {
        return texturesByName.ContainsKey(textureName);
    }

    private static void EnsureNormalMapImporter(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null || importer.textureType == TextureImporterType.NormalMap)
        {
            return;
        }

        importer.textureType = TextureImporterType.NormalMap;
        importer.SaveAndReimport();
    }

    private static void EnsureFolder(string parent, string child)
    {
        var full = $"{parent}/{child}";
        if (AssetDatabase.IsValidFolder(full))
        {
            return;
        }

        AssetDatabase.CreateFolder(parent, child);
    }

    private static string BuildMetallicSmoothnessTexture(string prefix, string metallicPath, string roughnessPath)
    {
        Texture2D metallicTexture = null;
        Texture2D roughnessTexture = null;

        if (!string.IsNullOrEmpty(metallicPath))
        {
            EnsureReadableImporter(metallicPath);
            metallicTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
        }

        if (!string.IsNullOrEmpty(roughnessPath))
        {
            EnsureReadableImporter(roughnessPath);
            roughnessTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(roughnessPath);
        }

        var width = metallicTexture != null ? metallicTexture.width : roughnessTexture.width;
        var height = metallicTexture != null ? metallicTexture.height : roughnessTexture.height;
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        var output = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        var fill = new Color32[width * height];

        var metallicPixels = metallicTexture != null ? ResizeToReadable(metallicTexture, width, height).GetPixels32() : null;
        var roughnessPixels = roughnessTexture != null ? ResizeToReadable(roughnessTexture, width, height).GetPixels32() : null;

        for (var i = 0; i < fill.Length; i++)
        {
            var metallic = metallicPixels != null ? metallicPixels[i].r : (byte)0;
            var roughness = roughnessPixels != null ? roughnessPixels[i].r : (byte)200;
            var smoothness = (byte)(255 - roughness);
            fill[i] = new Color32(metallic, 0, 0, smoothness);
        }

        output.SetPixels32(fill);
        output.Apply();

        var safeName = MakeSafeFileName(prefix) + "_MetallicSmoothness.png";
        var assetPath = $"{GeneratedFolder}/{safeName}";
        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), assetPath), output.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(output);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        ConfigureGeneratedMaskImporter(assetPath);
        return assetPath;
    }

    private static Texture2D ResizeToReadable(Texture2D source, int width, int height)
    {
        if (source.width == width && source.height == height)
        {
            return source;
        }

        var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Graphics.Blit(source, rt);
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var result = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    private static void EnsureReadableImporter(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        var changed = false;
        if (!importer.isReadable)
        {
            importer.isReadable = true;
            changed = true;
        }

        if (importer.sRGBTexture)
        {
            importer.sRGBTexture = false;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static void ConfigureGeneratedMaskImporter(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.sRGBTexture = false;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.isReadable = false;
        importer.SaveAndReimport();
    }

    private static string MakeSafeFileName(string name)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }

        return name.Replace(' ', '_');
    }
}
