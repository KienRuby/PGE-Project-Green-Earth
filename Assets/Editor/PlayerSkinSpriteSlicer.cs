#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class PlayerSkinSpriteSlicer
{
    private const string SkinsRoot = "Assets/Sprites/Character/Skin";

    [MenuItem("PGE/Character/Re-slice All Player Skins")]
    public static void SliceAllPlayerSkins()
    {
        for (int i = 1; i <= 4; i++)
        {
            string folder = $"{SkinsRoot}/{i}";
            if (!Directory.Exists(folder)) continue;

            string[] pngFiles = Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly);
            foreach (string pngPath in pngFiles)
            {
                SliceSkinTexture(pngPath.Replace("\\", "/"), i);
            }
        }
        AssetDatabase.Refresh();
        Debug.Log("[PlayerSkinSpriteSlicer] All player skins in folders 1, 2, 3, 4 successfully re-sliced.");
    }

    private static void SliceSkinTexture(string assetPath, int skinIndex)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

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
        if (dataProvider == null) return;
        dataProvider.InitSpriteEditorDataProvider();

        SpriteRect[] existing = dataProvider.GetSpriteRects();
        if (existing != null && existing.Length > 0)
        {
            // Already configured
            return;
        }

        dataProvider.Apply();
        importer.SaveAndReimport();
    }
}
#endif
