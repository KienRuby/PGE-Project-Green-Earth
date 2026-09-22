#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

[InitializeOnLoad]
public static class BossBulletSpriteSlicer
{
    private const string TexturePath = "Assets/Sprites/Enemy/Boss/đạn boss.png";
    private const string SessionKey = "BossBulletSpriteSlicer_Executed";

    static BossBulletSpriteSlicer()
    {
        EditorApplication.delayCall += OnEditorReady;
    }

    private static void OnEditorReady()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        SliceSprites();
    }

    [MenuItem("PGE/Sprites/Slice Boss Bullet Sprites", false, 10)]
    [MenuItem("Tools/PGE/Slice Boss Bullet Sprites", false, 10)]
    public static void SliceSprites()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[BossBulletSpriteSlicer] Không tìm thấy texture tại: {TexturePath}");
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
        if (dataProvider == null)
        {
            Debug.LogError("[BossBulletSpriteSlicer] Không thể khởi tạo SpriteEditorDataProvider.");
            return;
        }
        dataProvider.InitSpriteEditorDataProvider();

        var spriteDefinitions = new (string name, float x, float y, float w, float h)[]
        {
            ("đạn boss_0", 89f, 3248f, 126f, 200f),
            ("đạn boss_1", 584f, 3241f, 158f, 234f),
            ("đạn boss_2", 1042f, 3244f, 141f, 214f),
            ("đạn boss_3", 1363f, 3241f, 271f, 221f),
            ("đạn boss_4", 1635f, 3274f, 421f, 170f),
            ("đạn boss_5", 355f, 2897f, 394f, 164f),
            ("đạn boss_6", 839f, 2910f, 348f, 148f),
            ("đạn boss_7", 1372f, 2918f, 259f, 131f),
            ("đạn boss_8", 1743f, 2932f, 329f, 101f)
        };

        SpriteRect[] existingRects = dataProvider.GetSpriteRects();
        var existingGuids = existingRects != null
            ? existingRects.Where(r => !string.IsNullOrEmpty(r.name)).ToDictionary(r => r.name, r => r.spriteID)
            : new Dictionary<string, GUID>();

        SpriteRect[] spriteRects = new SpriteRect[spriteDefinitions.Length];

        for (int i = 0; i < spriteDefinitions.Length; i++)
        {
            var def = spriteDefinitions[i];
            GUID guid = GUID.Generate();
            if (existingGuids.TryGetValue(def.name, out GUID exactId))
            {
                guid = exactId;
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

        Debug.Log($"[BossBulletSpriteSlicer] ✅ Đã cắt thành công {spriteDefinitions.Length} sprite đạn boss sạch đẹp!");
    }
}
#endif
