#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Tool tự động cắt (slice) sạch sẽ và đặt tên chuẩn hóa cho sprite sheet:
/// Assets/Sprites/Doi/bảng thông tin chipset.png
/// </summary>
[InitializeOnLoad]
public static class ChipsetInfoSpriteSlicer
{
    public const string TexturePath = "Assets/Sprites/Doi/bảng thông tin chipset.png";
    private const string ResultLogPath = "OneShot_ChipsetSliceResult.txt";

    static ChipsetInfoSpriteSlicer()
    {
        EditorApplication.delayCall += CheckAndSliceIfEmpty;
    }

    public static void CheckAndSliceIfEmpty()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null) return;

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        if (dataProvider == null) return;
        dataProvider.InitSpriteEditorDataProvider();

        SpriteRect[] existingRects = dataProvider.GetSpriteRects();
        if (existingRects == null || existingRects.Length == 0 || importer.spriteImportMode != SpriteImportMode.Multiple)
        {
            SliceChipsetInfoSprites();
        }
    }

    [MenuItem("PGE/UI/Slice & Rename Chipset Info Sprites", false, 10)]
    [MenuItem("Tools/PGE/Slice & Rename Chipset Info Sprites", false, 10)]
    [MenuItem("Assets/PGE/Slice & Rename Chipset Info Sprites", false, 10)]
    public static void SliceChipsetInfoSprites()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[ChipsetInfoSpriteSlicer] Không tìm thấy TextureImporter tại đường dẫn: {TexturePath}");
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
        if (dataProvider == null)
        {
            Debug.LogError($"[ChipsetInfoSpriteSlicer] Không thể khởi tạo SpriteEditorDataProvider cho: {TexturePath}");
            return;
        }
        dataProvider.InitSpriteEditorDataProvider();

        // Danh sách 6 sprite với tọa độ Unity chuẩn xác (Bottom-Left origin, texture size: 2160 x 3840)
        var spriteDefinitions = new (string name, float x, float y, float w, float h)[]
        {
            ("Btn_Close",          1663f, 3462f, 195f,  198f),
            ("Panel_ChipsetInfo",   237f,  738f, 1677f, 2562f),
            ("Banner_Title",        720f, 3072f, 749f,  152f),
            ("Btn_Green",          1006f,  416f, 738f,  205f),
            ("Btn_Orange",         1006f,  150f, 738f,  205f),
            ("Btn_Equip",           414f,  147f, 429f,  241f)
        };

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

        string logMsg = $"SUCCESS: Đã cắt sạch sẽ và đặt tên thành công {spriteRects.Length} sprite cho '{TexturePath}' lúc {DateTime.Now}";
        Debug.Log($"[ChipsetInfoSpriteSlicer] {logMsg}");
        try
        {
            File.WriteAllText(ResultLogPath, logMsg);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ChipsetInfoSpriteSlicer] Không thể ghi log file: {ex.Message}");
        }
    }
}
#endif
