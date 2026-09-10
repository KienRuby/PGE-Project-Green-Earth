#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Tool tự động cắt và đặt tên chuẩn hóa cho sprite sheet Cổ vật: Assets/Sprites/UI/nút artifact.png
/// </summary>
public static class ArtifactSpriteSlicer
{
    private const string TexturePath = "Assets/Sprites/UI/nút artifact.png";

    [MenuItem("PGE/UI/Slice & Rename Artifact Sprites", false, 10)]
    [MenuItem("Tools/PGE/Slice & Rename Artifact Sprites", false, 10)]
    [MenuItem("Assets/PGE/Slice & Rename Artifact Sprites", false, 10)]
    public static void SliceAndRenameArtifactSprites()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[ArtifactSpriteSlicer] Không tìm thấy texture tại: {TexturePath}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 4096;

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();


        // Định nghĩa 9 sprite chuẩn xác theo tọa độ và kích thước của sprite sheet
        var spriteDefinitions = new (string name, float x, float y, float w, float h)[]
        {
            ("Artifact_CD",        496f,  2903f, 700f, 842f),
            ("Artifact_Lego",      1266f, 2903f, 700f, 842f),
            ("Artifact_USB",       496f,  2003f, 700f, 842f),
            ("Artifact_Butter",    1266f, 2003f, 700f, 842f),
            ("Artifact_Cooler",    496f,  1101f, 700f, 842f),
            ("Artifact_Rocket",    1257f, 1101f, 700f, 842f),
            ("Btn_ThrowAway",      298f,  616f,  654f, 298f),
            ("Btn_Get",            298f,  219f,  652f, 298f),
            ("Artifact_Chip",      1277f, 217f,  700f, 842f)
        };

        SpriteRect[] existingRects = dataProvider.GetSpriteRects();
        var existingGuids = existingRects != null
            ? existingRects.Where(r => !string.IsNullOrEmpty(r.name)).ToDictionary(r => r.name, r => r.spriteID)
            : new Dictionary<string, GUID>();

        // Map tên cũ sang GUID nếu đã có sẵn
        Dictionary<string, string> legacyNameToNewName = new Dictionary<string, string>
        {
            { "CD", "Artifact_CD" },
            { "LEGO", "Artifact_Lego" },
            { "USB", "Artifact_USB" },
            { "Phomai", "Artifact_Butter" },
            { "Quat", "Artifact_Cooler" },
            { "Phao", "Artifact_Rocket" },
            { "nút artifact_6", "Btn_ThrowAway" },
            { "nút artifact_7", "Btn_Get" },
            { "Chip", "Artifact_Chip" }
        };

        SpriteRect[] spriteRects = new SpriteRect[spriteDefinitions.Length];
        for (int i = 0; i < spriteDefinitions.Length; i++)
        {
            var def = spriteDefinitions[i];
            GUID guid = GUID.Generate();

            if (existingGuids.TryGetValue(def.name, out GUID exactId))
            {
                guid = exactId;
            }
            else
            {
                foreach (var pair in legacyNameToNewName)
                {
                    if (pair.Value == def.name && existingGuids.TryGetValue(pair.Key, out GUID oldId))
                    {
                        guid = oldId;
                        break;
                    }
                }
            }

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

        Debug.Log($"[ArtifactSpriteSlicer] ✅ Đã cắt và chuẩn hóa tên thành công cho 9 sprite trong {TexturePath}!");
    }
}
#endif
