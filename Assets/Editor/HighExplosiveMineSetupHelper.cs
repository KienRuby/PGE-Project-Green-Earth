#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class HighExplosiveMineSetupHelper
{
    private const string PrefabDirectory = "Assets/Prefabs/Chipset";
    private const string PrefabPath = "Assets/Prefabs/Chipset/HighExplosiveMine.prefab";
    private const string IconAtlasPath = "Assets/Sprites/UI/Chipset/icon chipset.png";
    private const string ExplosionVfxPath = "Assets/Prefabs/VFX Boom.prefab";

    [MenuItem("PGE/Skills/Create High Explosive Mine Prefab")]
    public static void CreateHighExplosiveMinePrefab()
    {
        if (!Directory.Exists(PrefabDirectory))
        {
            Directory.CreateDirectory(PrefabDirectory);
        }

        GameObject mineGo = new GameObject("HighExplosiveMine");

        // 1. Transform: scale (0.1632, 0.1632, 0.1632)
        mineGo.transform.position = Vector3.zero;
        mineGo.transform.rotation = Quaternion.identity;
        mineGo.transform.localScale = new Vector3(0.1632f, 0.1632f, 0.1632f);

        // 2. SpriteRenderer
        SpriteRenderer sr = mineGo.AddComponent<SpriteRenderer>();
        sr.color = Color.white;
        sr.material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        sr.sortingOrder = 5;

        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(IconAtlasPath).OfType<Sprite>().ToArray();
        Sprite mineSprite = sprites.FirstOrDefault(s => s.name.Contains("High-Explosive") || s.name.Contains("Mìn"));
        if (mineSprite != null)
        {
            sr.sprite = mineSprite;
        }

        // 3. Rigidbody2D (Kinematic)
        Rigidbody2D rb = mineGo.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // 4. CircleCollider2D (Trigger)
        CircleCollider2D col = mineGo.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1.2f;

        // 5. HighExplosiveMine Script Component
        HighExplosiveMine mineScript = mineGo.AddComponent<HighExplosiveMine>();
        GameObject explosionVfx = AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionVfxPath);

        // Save Prefab
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(mineGo, PrefabPath);
        Object.DestroyImmediate(mineGo);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[HighExplosiveMineSetupHelper] ✅ Đã tạo Prefab thành công tại: {PrefabPath} với scale (0.1632, 0.1632, 0.1632)!");
    }
}
#endif
