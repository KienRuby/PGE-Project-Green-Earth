#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Công cụ cắt Sprite tự động sạch sẽ cho 2 bản đồ mới (prop.png và prop (1).png)
/// - Cắt chính xác từng chướng ngại vật theo tọa độ bounding box, không tạo rác/mảnh vụn.
/// - Thiết lập SpriteMode: Multiple, MaxTextureSize: 4096 (giữ nguyên độ phân giải sắc nét 3000x3000).
/// - Giữ nguyên hoặc sinh mới GUID nhất quán, alignment Center (pivot 0.5, 0.5).
/// - Tự động chạy khi load và có Menu PGE/Map để chạy lại bất cứ lúc nào.
/// </summary>
[InitializeOnLoad]
public static class MapPropSpriteSlicer
{
    private const string SessionKey = "MapPropSpriteSlicer_AutoExecuted_v2";

    public const string TexturePathProp = "Assets/Sprites/Backround/Map/prop.png";
    public const string TexturePathProp1 = "Assets/Sprites/Backround/Map/prop (1).png";

    static MapPropSpriteSlicer()
    {
        EditorApplication.delayCall += AutoExecuteOnLoad;
    }

    private static void AutoExecuteOnLoad()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);
        SliceAllMapProps();
    }

    [MenuItem("PGE/Map/Slice New Map Obstacles (All)")]
    public static void SliceAllMapProps()
    {
        SlicePropTexture();
        SliceProp1Texture();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("PGE/Map/Slice Obstacles (prop.png)")]
    public static void SlicePropTexture()
    {
        // Tọa độ bounding box đã tính toán chính xác trên texture 3000x3000 (padding 2px)
        var spriteDefs = new (string name, float x, float y, float w, float h)[]
        {
            ("nam_tim", 262f, 1837f, 302f, 368f),
            ("cay_hoa_xoan", 831f, 1676f, 497f, 577f),
            ("bui_cay", 1587f, 1805f, 453f, 391f),
            ("bui_co_1", 2296f, 1956f, 216f, 146f),
            ("bui_co_2", 2478f, 1957f, 203f, 119f),
            ("bui_co_doi", 2296f, 1956f, 385f, 146f)
        };

        SliceTexture(TexturePathProp, spriteDefs);
    }

    [MenuItem("PGE/Map/Slice Obstacles (prop (1).png)")]
    public static void SliceProp1Texture()
    {
        // Tọa độ bounding box đã tính toán chính xác trên texture 3000x3000 (padding 2px)
        var spriteDefs = new (string name, float x, float y, float w, float h)[]
        {
            ("tang_da", 304f, 2011f, 336f, 188f),
            ("cay_bup_cam", 1098f, 1930f, 274f, 513f),
            ("mam_vang", 1012f, 1935f, 140f, 217f),
            ("mam_xanh", 1308f, 1929f, 185f, 223f),
            ("cum_cay_cam", 1012f, 1929f, 481f, 514f),
            ("nam_bach_tuoc", 1834f, 1939f, 352f, 363f),
            ("bui_hoa_xanh", 2563f, 2000f, 262f, 220f)
        };

        SliceTexture(TexturePathProp1, spriteDefs);
    }

    private static void SliceTexture(string assetPath, (string name, float x, float y, float w, float h)[] spriteDefinitions)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[MapPropSpriteSlicer] Không tìm thấy TextureImporter tại đường dẫn: {assetPath}");
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
            Debug.LogError($"[MapPropSpriteSlicer] Không thể lấy ISpriteEditorDataProvider cho {assetPath}");
            return;
        }

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

    }
}
#endif
