#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SpikyDiscusSetupHelper
{
    private const string PrefabDirectory = "Assets/Prefabs/Chipset";
    private const string PrefabPath = "Assets/Prefabs/Chipset/SpikyDiscus.prefab";
    private const string IconAtlasPath = "Assets/Sprites/UI/Chipset/icon chipset.png";
    private const string HitVfxPath = "Assets/Prefabs/VFX Boom.prefab";

    [MenuItem("PGE/Skills/Create Spiky Discus Prefab")]
    public static void CreateSpikyDiscusPrefab()
    {
        if (!Directory.Exists(PrefabDirectory))
        {
            Directory.CreateDirectory(PrefabDirectory);
        }

        GameObject discusGo = new GameObject("SpikyDiscus");

        // 1. Transform: scale (0.1632, 0.1632, 0.1632)
        discusGo.transform.position = Vector3.zero;
        discusGo.transform.rotation = Quaternion.identity;
        discusGo.transform.localScale = new Vector3(0.1632f, 0.1632f, 0.1632f);

        // 2. SpriteRenderer
        SpriteRenderer sr = discusGo.AddComponent<SpriteRenderer>();
        sr.color = Color.white;
        sr.material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        sr.sortingOrder = 6;

        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(IconAtlasPath).OfType<Sprite>().ToArray();
        Sprite discusSprite = sprites.FirstOrDefault(s => s.name.Contains("Spiky") || s.name.Contains("Đĩa"));
        if (discusSprite != null)
        {
            sr.sprite = discusSprite;
        }

        // 3. Rigidbody2D (Kinematic)
        Rigidbody2D rb = discusGo.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // 4. CircleCollider2D (Trigger)
        CircleCollider2D col = discusGo.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1.2f;

        // 5. SpikyDiscusProjectile Script Component
        SpikyDiscusProjectile projScript = discusGo.AddComponent<SpikyDiscusProjectile>();

        // Save Prefab
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(discusGo, PrefabPath);
        Object.DestroyImmediate(discusGo);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SpikyDiscusSetupHelper] ✅ Đã tạo Prefab thành công tại: {PrefabPath} với scale (0.1632, 0.1632, 0.1632)!");
    }
}
#endif
