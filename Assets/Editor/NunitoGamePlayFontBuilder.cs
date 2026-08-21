using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

public static class NunitoGamePlayFontBuilder
{
    private const string SourceFontPath = "Assets/Fonts/Nunito/Nunito-Variable.ttf";
    private const string FontAssetPath = "Assets/Fonts/Nunito/Nunito SDF.asset";
    private const string OutlineMaterialPath = "Assets/Fonts/Nunito/Nunito SDF - Stroke.mat";
    private const string FallbackFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("PGE/UI/Áp dụng Nunito có viền cho toàn bộ scene")]
    public static void BuildAndApply()
    {
        AssetDatabase.Refresh();

        TMP_FontAsset fontAsset = LoadOrCreateFontAsset();
        if (fontAsset == null)
        {
            Debug.LogError($"[Nunito Font] Không thể tạo TMP Font Asset từ {SourceFontPath}.");
            return;
        }

        Material strokeMaterial = LoadOrCreateStrokeMaterial(fontAsset);
        string originalScenePath = SceneManager.GetActiveScene().path;
        int totalChangedCount = 0;
        int changedSceneCount = 0;

        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (!buildScene.enabled || string.IsNullOrEmpty(buildScene.path))
            {
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            int sceneChangedCount = ApplyToScene(scene, fontAsset, strokeMaterial);
            if (sceneChangedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                totalChangedCount += sceneChangedCount;
                changedSceneCount++;
            }
        }

        if (!string.IsNullOrEmpty(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Nunito Font] Đã áp dụng Nunito có viền cho {totalChangedCount} chữ trong {changedSceneCount} scene thuộc Build Settings.");
    }

    private static int ApplyToScene(Scene scene, TMP_FontAsset fontAsset, Material strokeMaterial)
    {
        TMP_Text[] allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        int changedCount = 0;

        foreach (TMP_Text text in allTexts)
        {
            if (text == null || text.gameObject.scene != scene)
            {
                continue;
            }

            Undo.RecordObject(text, "Áp dụng font Nunito có viền");
            text.font = fontAsset;
            text.fontSharedMaterial = strokeMaterial;
            EditorUtility.SetDirty(text);
            changedCount++;
        }

        return changedCount;
    }

    private static TMP_FontAsset LoadOrCreateFontAsset()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null)
        {
            EnsureFallback(existing);
            return existing;
        }

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            return null;
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic,
            true);

        if (fontAsset == null)
        {
            return null;
        }

        fontAsset.name = "Nunito SDF";
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

        if (fontAsset.atlasTextures != null)
        {
            foreach (Texture2D atlas in fontAsset.atlasTextures)
            {
                if (atlas != null && !AssetDatabase.Contains(atlas))
                {
                    atlas.name = "Nunito SDF Atlas";
                    AssetDatabase.AddObjectToAsset(atlas, fontAsset);
                }
            }
        }

        if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material))
        {
            fontAsset.material.name = "Nunito SDF Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        EnsureFallback(fontAsset);
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        return fontAsset;
    }

    private static void EnsureFallback(TMP_FontAsset fontAsset)
    {
        TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);
        if (fallback == null || fontAsset.fallbackFontAssetTable.Contains(fallback))
        {
            return;
        }

        fontAsset.fallbackFontAssetTable.Add(fallback);
        EditorUtility.SetDirty(fontAsset);
    }

    private static Material LoadOrCreateStrokeMaterial(TMP_FontAsset fontAsset)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
        if (material == null)
        {
            material = new Material(fontAsset.material)
            {
                name = "Nunito SDF - Stroke"
            };
            AssetDatabase.CreateAsset(material, OutlineMaterialPath);
        }

        ShaderUtilities.GetShaderPropertyIDs();
        material.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.18f);
        EditorUtility.SetDirty(material);
        return material;
    }
}
