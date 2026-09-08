using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class AudienceCrowdSetupEditor
{
    private const string MaterialPath = "Assets/Materials/CrowdUnlitTransparent.mat";

    [MenuItem("Tools/Palmira/Setup Selected Crowd Sprites")]
    public static void SetupSelectedCrowdSprites()
    {
        if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Crowd Setup", "Seleziona il GameObject del pubblico o le sue sagome.", "OK");
            return;
        }

        OptimizeSelectedCrowdTextures();

        var material = GetOrCreateCrowdMaterial();
        var xrOrigin = Object.FindFirstObjectByType<XROrigin>();

        int billboardCount = 0;
        int rendererCount = 0;

        foreach (var selected in Selection.gameObjects)
        {
            foreach (var renderer in selected.GetComponentsInChildren<Renderer>(true))
            {
                ConfigureRenderer(renderer, material);
                rendererCount++;

                var billboard = renderer.GetComponent<AudienceBillboard>();
                if (billboard == null)
                    billboard = Undo.AddComponent<AudienceBillboard>(renderer.gameObject);

                billboard.xrOrigin = xrOrigin;
                billboard.yawOnly = true;
                billboard.rotationSpeed = 4f;
                billboard.eulerOffset = new Vector3(0f, 180f, 0f);
                EditorUtility.SetDirty(billboard);
                billboardCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Crowd Setup",
            $"Renderer configurati: {rendererCount}\nBillboard applicati: {billboardCount}\n\nLe sagome ora useranno un materiale unlit trasparente e non prenderanno ombre/luci pesanti.",
            "OK");
    }

    [MenuItem("Tools/Palmira/Optimize Selected Crowd Texture Quality")]
    public static void OptimizeSelectedCrowdTextures()
    {
        if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Crowd Quality", "Seleziona il GameObject del pubblico o le sue sagome.", "OK");
            return;
        }

        var processedPaths = new HashSet<string>();
        var updatedCount = 0;

        foreach (var selected in Selection.gameObjects)
        {
            foreach (var canvas in selected.GetComponentsInChildren<Canvas>(true))
            {
                OptimizeWorldSpaceCanvas(canvas);
            }

            foreach (var image in selected.GetComponentsInChildren<Image>(true))
            {
                string texturePath = ExtractTexturePath(image);
                if (string.IsNullOrEmpty(texturePath) || processedPaths.Contains(texturePath))
                    continue;

                var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (importer == null)
                    continue;

                processedPaths.Add(texturePath);
                OptimizeTextureImporter(importer);
                updatedCount++;
            }

            foreach (var renderer in selected.GetComponentsInChildren<Renderer>(true))
            {
                string texturePath = ExtractTexturePath(renderer);
                if (string.IsNullOrEmpty(texturePath) || processedPaths.Contains(texturePath))
                    continue;

                var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (importer == null)
                    continue;

                processedPaths.Add(texturePath);
                OptimizeTextureImporter(importer);
                updatedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Crowd Quality",
            $"Texture ottimizzate: {updatedCount}\n\nHo aumentato la qualita' delle sagome senza cambiare impostazioni globali del progetto.",
            "OK");
    }

    private static Material GetOrCreateCrowdMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material != null)
            return material;

        Shader shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        material = new Material(shader);
        material.name = "CrowdUnlitTransparent";

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_AlphaClip"))
            material.SetFloat("_AlphaClip", 0f);

        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }

    private static void ConfigureRenderer(Renderer renderer, Material sharedMaterial)
    {
        Undo.RecordObject(renderer, "Setup crowd renderer");

        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.allowOcclusionWhenDynamic = false;

        if (renderer is SpriteRenderer spriteRenderer)
        {
            spriteRenderer.sharedMaterial = sharedMaterial;
            spriteRenderer.color = Color.white;
            EditorUtility.SetDirty(spriteRenderer);
            return;
        }

        var materials = renderer.sharedMaterials;
        if (materials == null || materials.Length == 0)
        {
            renderer.sharedMaterial = sharedMaterial;
        }
        else
        {
            var updated = new List<Material>(materials.Length);
            foreach (var _ in materials)
                updated.Add(sharedMaterial);
            renderer.sharedMaterials = updated.ToArray();
        }

        EditorUtility.SetDirty(renderer);
    }

    private static string ExtractTexturePath(Renderer renderer)
    {
        if (renderer is SpriteRenderer spriteRenderer && spriteRenderer.sprite != null)
            return AssetDatabase.GetAssetPath(spriteRenderer.sprite.texture);

        foreach (var material in renderer.sharedMaterials)
        {
            if (material == null)
                continue;

            Texture texture = null;
            if (material.HasProperty("_BaseMap"))
                texture = material.GetTexture("_BaseMap");
            if (texture == null && material.HasProperty("_MainTex"))
                texture = material.GetTexture("_MainTex");

            if (texture != null)
                return AssetDatabase.GetAssetPath(texture);
        }

        return null;
    }

    private static string ExtractTexturePath(Image image)
    {
        if (image == null || image.sprite == null)
            return null;

        return AssetDatabase.GetAssetPath(image.sprite.texture);
    }

    private static void OptimizeTextureImporter(TextureImporter importer)
    {
        var changed = false;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }

        if (importer.filterMode != FilterMode.Trilinear)
        {
            importer.filterMode = FilterMode.Trilinear;
            changed = true;
        }

        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            changed = true;
        }

        if (importer.crunchedCompression)
        {
            importer.crunchedCompression = false;
            changed = true;
        }

        if (importer.maxTextureSize < 4096)
        {
            importer.maxTextureSize = 4096;
            changed = true;
        }

        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }

        if (!importer.mipmapEnabled)
        {
            importer.mipmapEnabled = true;
            changed = true;
        }

        if (!importer.mipMapsPreserveCoverage)
        {
            importer.mipMapsPreserveCoverage = true;
            changed = true;
        }

        if (importer.alphaTestReferenceValue < 0.3f || importer.alphaTestReferenceValue > 0.7f)
        {
            importer.alphaTestReferenceValue = 0.5f;
            changed = true;
        }

        if (importer.anisoLevel < 4)
        {
            importer.anisoLevel = 4;
            changed = true;
        }

        if (importer.spritePixelsPerUnit != 100f)
        {
            importer.spritePixelsPerUnit = 100f;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
    }

    private static void OptimizeWorldSpaceCanvas(Canvas canvas)
    {
        if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
            return;

        Undo.RecordObject(canvas, "Optimize world space crowd canvas");
        canvas.pixelPerfect = false;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
        EditorUtility.SetDirty(canvas);

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);

        Undo.RecordObject(scaler, "Optimize crowd canvas scaler");
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.referencePixelsPerUnit = 100f;
        scaler.dynamicPixelsPerUnit = 30f;
        scaler.scaleFactor = 1f;
        EditorUtility.SetDirty(scaler);

        var raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            Undo.RecordObject(raycaster, "Disable crowd raycaster");
            raycaster.enabled = false;
            EditorUtility.SetDirty(raycaster);
        }

        foreach (var image in canvas.GetComponentsInChildren<Image>(true))
        {
            Undo.RecordObject(image, "Optimize crowd image");
            image.preserveAspect = true;
            image.useSpriteMesh = true;
            image.color = Color.white;
            EditorUtility.SetDirty(image);
        }
    }
}
