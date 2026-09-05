#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class RewardSpriteSlicer
{
    private const string TexturePathDailyLogin = "Assets/Sprites/UI/Reward/nút daily login.png";
    private const string TexturePathKhungDailyLogin = "Assets/Sprites/UI/Reward/nút khung daily login.png";

    // Không dùng [InitializeOnLoad] để tránh tự động ghi đè kích thước cắt thủ công của người dùng trong Sprite Editor.
    // Chỉ chạy khi người dùng chủ động bấm menu: PGE > UI > Slice Reward Textures (Daily Login)

    [MenuItem("PGE/UI/Slice Reward Textures (Daily Login)")]
    public static void SliceAllRewardTextures()
    {
        SliceDailyLoginTexture();
        SliceKhungDailyLoginTexture();
    }

    public static void SliceDailyLoginTexture()
    {
        var spriteDefinitions = new (string name, float x, float y, float w, float h)[]
        {
            ("Btn_Obtained", 1466f, 3233f, 348f, 152f),
            ("Btn_Get", 1466f, 2921f, 348f, 152f),
            ("Btn_Claim_Again", 1441f, 2598f, 373f, 174f),
            ("Icon_Energy", 385f, 3009f, 164f, 168f),
            ("Icon_Data_Chip", 636f, 3009f, 164f, 168f),
            ("Icon_Red_Gem", 885f, 3009f, 164f, 168f),
            ("Row_Banner_Blue", 266f, 2231f, 1650f, 268f),
            ("Row_Banner_Grey", 269f, 1853f, 1645f, 264f)
        };

        SliceTexture(TexturePathDailyLogin, spriteDefinitions);
    }

    public static void SliceKhungDailyLoginTexture()
    {
        var spriteDefinitions = new (string name, float x, float y, float w, float h)[]
        {
            ("Tab_Daily_Login_Active", 369f, 3419f, 687f, 232f),
            ("Tab_Achievements_Active", 1124f, 3419f, 687f, 232f),
            ("Tab_Daily_Login_Inactive", 369f, 3081f, 687f, 202f),
            ("Tab_Achievements_Inactive", 1124f, 3081f, 687f, 202f),
            ("Frame_Daily_Login_Main", 233f, 610f, 1714f, 2427f)
        };

        SliceTexture(TexturePathKhungDailyLogin, spriteDefinitions);
    }

    private static void SliceTexture(string assetPath, (string name, float x, float y, float w, float h)[] spriteDefinitions)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[RewardSpriteSlicer] Cannot find TextureImporter at path: {assetPath}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 4096;
        importer.SaveAndReimport();

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        SpriteRect[] existingRects = dataProvider.GetSpriteRects();
        var existingGuids = existingRects != null
            ? existingRects.Where(r => !string.IsNullOrEmpty(r.name)).ToDictionary(r => r.name, r => r.spriteID)
            : new Dictionary<string, GUID>();

        SpriteRect[] spriteRects = new SpriteRect[spriteDefinitions.Length];
        for (int i = 0; i < spriteDefinitions.Length; i++)
        {
            var def = spriteDefinitions[i];
            GUID guid = existingGuids.TryGetValue(def.name, out GUID existingId) ? existingId : GUID.Generate();
            spriteRects[i] = new SpriteRect
            {
                name = def.name,
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = guid,
                rect = new Rect(def.x, def.y, def.w, def.h)
            };
        }

        dataProvider.SetSpriteRects(spriteRects);
        dataProvider.Apply();
        importer.SaveAndReimport();

        Debug.Log($"[RewardSpriteSlicer] Successfully sliced {spriteRects.Length} sprites for {assetPath} with MaxTextureSize 4096.");
    }
}
#endif
