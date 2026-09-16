#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

[InitializeOnLoad]
public static class RobotPetSpriteSlicer
{
    public const string TexturePath = "Assets/Sprites/UI/Buddy/nút màn robot pet.png";

    static RobotPetSpriteSlicer()
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
        if (existingRects == null || existingRects.Length == 0)
        {
            SliceRobotPetTexture();
        }
    }

    [MenuItem("PGE/UI/Slice Robot Pet Texture")]
    public static void SliceRobotPetTexture()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[RobotPetSpriteSlicer] Cannot find TextureImporter at path: {TexturePath}");
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

        var spriteDefinitions = new (string name, float x, float y, float w, float h)[]
        {
            ("Tab_Drone", 114f, 3145f, 923f, 255f),
            ("Tab_RobotPet", 1122f, 3145f, 923f, 255f),
            ("Panel_RobotPet", 228f, 1870f, 1692f, 1094f),
            ("Button_Craft", 903f, 1445f, 392f, 172f),
            ("Card_Pet_Robot", 387f, 966f, 314f, 393f),
            ("Card_Pet_Bat", 925f, 966f, 314f, 393f),
            ("Card_Pet_Dog", 1455f, 966f, 314f, 393f),
            ("Card_Pet_Dog_Selected", 382f, 503f, 314f, 393f),
            ("Icon_Info", 265f, 2836f, 100f, 97f)
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

        Debug.Log($"[RobotPetSpriteSlicer] Successfully sliced {spriteRects.Length} sprites for {TexturePath}.");
    }
}
#endif
