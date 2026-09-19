#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

[InitializeOnLoad]
public static class ShopSpriteSlicer
{
    public const string TexturePath = "Assets/Sprites/UI/Shop/khung màn shop.png";
    public const string BoxShopTexturePath = "Assets/Sprites/UI/Shop/box shop.png";

    static ShopSpriteSlicer()
    {
        EditorApplication.delayCall += CheckAndSliceIfEmpty;
    }

    public static void CheckAndSliceIfEmpty()
    {
        CheckAndSliceShopAtlas();
        CheckAndSliceBoxShop();
    }

    private static void CheckAndSliceShopAtlas()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null) return;

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        if (dataProvider == null) return;
        dataProvider.InitSpriteEditorDataProvider();

        SpriteRect[] existingRects = dataProvider.GetSpriteRects();
        if (existingRects == null || existingRects.Length == 0)
        {
            SliceShopTexture();
        }
    }

    private static void CheckAndSliceBoxShop()
    {
        TextureImporter importer = AssetImporter.GetAtPath(BoxShopTexturePath) as TextureImporter;
        if (importer == null) return;

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        if (dataProvider == null) return;
        dataProvider.InitSpriteEditorDataProvider();

        SpriteRect[] existingRects = dataProvider.GetSpriteRects();
        if (existingRects == null || existingRects.Length == 0)
        {
            SliceBoxShopTexture();
        }
    }

    [MenuItem("PGE/UI/Slice Shop Texture")]
    public static void SliceShopTexture()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[ShopSpriteSlicer] Cannot find TextureImporter at path: {TexturePath}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 8192;
        importer.SaveAndReimport();

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        SpriteRect[] existingRects = dataProvider.GetSpriteRects();
        var existingGuids = existingRects != null
            ? existingRects.Where(r => !string.IsNullOrEmpty(r.name)).ToDictionary(r => r.name, r => r.spriteID)
            : new Dictionary<string, GUID>();

        var spriteDefinitions = new (string name, float x, float y, float w, float h)[]
        {
            ("Card_VIP_Package", 178f, 9015f, 1813f, 1164f),
            ("Header_Special_Item", 173f, 8545f, 1818f, 254f),
            ("Card_Welcome_Package", 178f, 7254f, 1907f, 1143f),
            ("Card_Intermediate_Pack", 178f, 5908f, 1907f, 1143f),
            ("Card_Advanced_Pack", 178f, 4610f, 1907f, 1143f),
            ("Header_Daily_Shop", 173f, 3938f, 1818f, 254f),
            ("Item_Daily_Gem_Free", 175f, 3201f, 559f, 659f),
            ("Item_Daily_Drone_Box_1", 801f, 3201f, 615f, 703f),
            ("Item_Daily_Drone_Box_2", 1431f, 3201f, 613f, 703f),
            ("Header_Box", 173f, 2854f, 1818f, 254f),
            ("Box_Chipset_1x", 177f, 2031f, 897f, 790f),
            ("Box_Chipset_10x", 1133f, 2031f, 896f, 786f),
            ("Box_Drone_1x", 177f, 1223f, 897f, 790f),
            ("Box_Drone_10x", 1133f, 1224f, 896f, 789f),
            ("Header_Meta_Shop", 2173f, 9391f, 1818f, 254f),
            ("Card_Gun_Pack", 2178f, 8143f, 1907f, 1143f),
            ("Card_Drone_Pack", 2178f, 6859f, 1907f, 1143f),
            ("Header_Data_Chip", 2180f, 6388f, 1818f, 254f),
            ("Item_Data_Chip_1", 2181f, 5613f, 559f, 659f),
            ("Item_Data_Chip_2", 2809f, 5613f, 559f, 659f),
            ("Item_Data_Chip_3", 3437f, 5613f, 559f, 659f),
            ("Header_Gem_Event", 2130f, 5000f, 1880f, 288f),
            ("Item_Gem_1", 2193f, 4238f, 559f, 659f),
            ("Item_Gem_2", 2823f, 4238f, 559f, 659f),
            ("Item_Gem_3", 3450f, 4238f, 559f, 659f),
            ("Item_Gem_4", 2193f, 3507f, 559f, 659f),
            ("Item_Gem_5", 2823f, 3507f, 559f, 659f),
            ("Item_Gem_6", 3450f, 3507f, 559f, 659f)
        };

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

    [MenuItem("PGE/UI/Slice All Shop Textures")]
    public static void SliceAllShopTextures()
    {
        SliceShopTexture();
        SliceBoxShopTexture();
    }

    [MenuItem("PGE/UI/Slice Box Shop Texture")]
    public static void SliceBoxShopTexture()
    {
        TextureImporter importer = AssetImporter.GetAtPath(BoxShopTexturePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[ShopSpriteSlicer] Cannot find TextureImporter at path: {BoxShopTexturePath}");
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

        var boxDefinitions = new (string name, float x, float y, float w, float h)[]
        {
            // Chipset 1x (Small Green/Cyan 'C')
            ("Box_Chipset_1x_Closed", 496f, 2764f, 239f, 267f),
            ("Box_Chipset_1x_Open", 496f, 2346f, 240f, 318f),
            ("Box_Chipset_1x_Lid", 499f, 2545f, 233f, 119f),
            ("Box_Chipset_1x_Base", 496f, 2346f, 240f, 193f),

            // Chipset 10x (Big Green/Cyan 'C')
            ("Box_Chipset_10x_Closed", 1400f, 2751f, 326f, 335f),
            ("Box_Chipset_10x_Open", 1400f, 2214f, 326f, 479f),
            ("Box_Chipset_10x_Lid", 1400f, 2517f, 326f, 176f),
            ("Box_Chipset_10x_Base", 1400f, 2214f, 326f, 280f),

            // Drone 1x (Small Blue 'D')
            ("Box_Drone_1x_Closed", 495f, 1797f, 241f, 269f),
            ("Box_Drone_1x_Open", 495f, 1282f, 241f, 319f),
            ("Box_Drone_1x_Lid", 499f, 1482f, 233f, 119f),
            ("Box_Drone_1x_Base", 495f, 1282f, 241f, 194f),

            // Drone 10x (Big Blue 'D')
            ("Box_Drone_10x_Closed", 1400f, 1558f, 326f, 335f),
            ("Box_Drone_10x_Open", 1400f, 962f, 326f, 479f),
            ("Box_Drone_10x_Lid", 1400f, 1265f, 326f, 176f),
            ("Box_Drone_10x_Base", 1400f, 962f, 326f, 280f)
        };

        SpriteRect[] spriteRects = new SpriteRect[boxDefinitions.Length];
        for (int i = 0; i < boxDefinitions.Length; i++)
        {
            var def = boxDefinitions[i];
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
