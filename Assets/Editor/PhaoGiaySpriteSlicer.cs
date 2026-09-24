#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Tool cắt và cấu hình Sprite cho pháo giấy (Assets/Sprites/Backround/pháo giấy.png)
/// - Cắt 14 mảnh confetti theo tọa độ bounding box chính xác.
/// - Thiết lập SpriteMode: Multiple, MaxTextureSize: 4096 (giữ nguyên độ phân giải 2160x1040).
/// - Alignment Center (pivot 0.5, 0.5).
/// </summary>
public static class PhaoGiaySpriteSlicer
{
    public const string TexturePath = "Assets/Sprites/Backround/pháo giấy.png";

    [MenuItem("PGE/Sprites/Slice Pháo Giấy", false, 20)]
    [MenuItem("Tools/PGE/Slice Pháo Giấy", false, 20)]
    public static void SlicePhaoGiayTexture()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[PhaoGiaySpriteSlicer] Không tìm thấy TextureImporter tại: {TexturePath}");
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
            Debug.LogError($"[PhaoGiaySpriteSlicer] Không thể lấy ISpriteEditorDataProvider cho: {TexturePath}");
            return;
        }

        dataProvider.InitSpriteEditorDataProvider();

        var spriteDefinitions = new (string name, float x, float y, float w, float h)[]
        {
            ("pháo giấy_0", 821f, 711f, 76f, 75f),
            ("pháo giấy_1", 1491f, 706f, 76f, 75f),
            ("pháo giấy_2", 616f, 581f, 72f, 148f),
            ("pháo giấy_3", 1286f, 576f, 72f, 148f),
            ("pháo giấy_4", 924f, 504f, 60f, 65f),
            ("pháo giấy_5", 1594f, 499f, 61f, 66f),
            ("pháo giấy_6", 455f, 341f, 142f, 91f),
            ("pháo giấy_7", 756f, 344f, 141f, 180f),
            ("pháo giấy_8", 986f, 254f, 49f, 113f),
            ("pháo giấy_9", 1125f, 336f, 142f, 91f),
            ("pháo giấy_10", 1426f, 339f, 141f, 180f),
            ("pháo giấy_11", 1656f, 249f, 49f, 113f),
            ("pháo giấy_12", 654f, 241f, 119f, 59f),
            ("pháo giấy_13", 1324f, 236f, 119f, 59f)
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
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PhaoGiaySpriteSlicer] ✅ Đã cắt thành công {spriteRects.Length} sprite pháo giấy!");
    }
}
#endif
