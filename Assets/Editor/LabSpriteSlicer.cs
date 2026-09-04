#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

[InitializeOnLoad]
public static class LabSpriteSlicer
{
    private const string TexturePath = "Assets/Sprites/UI/Lab/nút màn lab 1.png";

    static LabSpriteSlicer()
    {
        EditorApplication.delayCall += SliceLabTexture;
    }

    [MenuItem("PGE/UI/Slice Lab Texture 1")]
    public static void SliceLabTexture()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null)
        {
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

        var spriteDefinitions = new (string name, float x, float y, float w, float h)[]
        {
            ("Stats", 112f, 3451f, 927f, 258f),
            ("Build Body", 1135f, 3451f, 927f, 258f),
            ("Build Body Locked", 1135f, 3113f, 927f, 258f),
            ("HP", 289f, 2589f, 381f, 443f),
            ("Recovery", 696f, 2589f, 381f, 443f),
            ("Auto Recovery", 1106f, 2589f, 381f, 443f),
            ("DEF", 1513f, 2589f, 381f, 443f),
            ("ATK", 289f, 2125f, 381f, 443f),
            ("CRIT Rate", 696f, 2125f, 381f, 443f),
            ("CRIT Damage", 1106f, 2123f, 381f, 443f),
            ("Obtained Chips", 1513f, 2125f, 381f, 443f),
            ("Ranged Defense", 289f, 1664f, 381f, 443f),
            ("Drone ATK", 696f, 1664f, 381f, 443f),
            ("Turret ATK", 1106f, 1664f, 381f, 443f),
            ("Turret Duration", 1513f, 1664f, 381f, 443f),
            ("Evade", 289f, 1204f, 381f, 443f),
            ("Life Steal", 696f, 1204f, 381f, 443f),
            ("Move Speed", 1105f, 1204f, 381f, 443f),
            ("Chipset Selection", 1513f, 1205f, 381f, 443f),
            ("Locked", 292f, 676f, 384f, 443f),
            ("Upgrade", 704f, 192f, 778f, 390f)
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

        Debug.Log($"[LabSpriteSlicer] Successfully sliced {spriteRects.Length} sprites for {TexturePath} with MaxTextureSize 4096.");
    }
}
#endif
