#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Helper cấu hình Prefab Spinning Blade và gắn vào Player trong GamePlay.unity.
/// Chỉ chạy khi người dùng chủ động chọn Menu, bảo toàn nguyên vẹn Sprite và Scale người dùng đã chỉnh.
/// </summary>
public static class SpinningBladeSetupHelper
{
    private const string GamePlayScenePath = "Assets/Scenes/GamePlay.unity";
    private const string PrefabDirectory = "Assets/Prefabs/Chipset";
    private const string SpinningBladePrefabPath = "Assets/Prefabs/Chipset/SpinningBlade.prefab";
    private const string HitVfxPrefabPath = "Assets/Prefabs/VFX Boom.prefab";
    private const string ChipsetSpriteSheetPath = "Assets/Sprites/UI/Chipset/icon chipset.png";
    private const string TrailMaterialPath = "Assets/Materials/RocketPunchTrail_Mat.mat";

    [MenuItem("PGE/Skills/Setup Spinning Blade Prefab and Player")]
    public static void SetupSpinningBladePrefabAndPlayer()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[SpinningBladeSetupHelper] Vui lòng dừng Play Mode trước khi cấu hình.");
            return;
        }

        SetupPrefabAndScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SpinningBladeSetupHelper] ✅ Đã cấu hình thành công SpinningBlade.prefab và Player trong GamePlay.unity!");
    }

    public static void SetupPrefabAndScene()
    {
        if (!Directory.Exists(PrefabDirectory))
        {
            Directory.CreateDirectory(PrefabDirectory);
        }

        Scene activeScene = SceneManager.GetActiveScene();
        bool wasGamePlayActive = string.Equals(activeScene.path, GamePlayScenePath, StringComparison.OrdinalIgnoreCase);

        Scene gamePlayScene;
        if (wasGamePlayActive)
        {
            gamePlayScene = activeScene;
        }
        else
        {
            gamePlayScene = EditorSceneManager.OpenScene(GamePlayScenePath, OpenSceneMode.Single);
        }

        // 1. Kiểm tra xem trong scene có SpinningBlade GameObject không
        GameObject sceneBladeObj = gamePlayScene.GetRootGameObjects()
            .FirstOrDefault(g => g.name.IndexOf("SpinningBlade", StringComparison.OrdinalIgnoreCase) >= 0
                              || g.name.IndexOf("Spinning Blade", StringComparison.OrdinalIgnoreCase) >= 0);

        Sprite bladeSprite = null;
        if (sceneBladeObj != null)
        {
            SpriteRenderer sceneSr = sceneBladeObj.GetComponent<SpriteRenderer>();
            if (sceneSr != null && sceneSr.sprite != null)
            {
                bladeSprite = sceneSr.sprite;
            }
        }

        if (bladeSprite == null)
        {
            Sprite[] allSprites = AssetDatabase.LoadAllAssetsAtPath(ChipsetSpriteSheetPath).OfType<Sprite>().ToArray();
            bladeSprite = allSprites.FirstOrDefault(s => s.name.IndexOf("blade", StringComparison.OrdinalIgnoreCase) >= 0 || s.name.IndexOf("dao", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? allSprites.FirstOrDefault(s => s.name == "icon chipset_3");
        }

        GameObject savedPrefab = null;

        // 2. Tạo Prefab nếu chưa có
        if (!File.Exists(SpinningBladePrefabPath))
        {
            GameObject tempObj = new GameObject("SpinningBlade");
            tempObj.tag = "BulletPlayer";
            tempObj.layer = LayerMask.NameToLayer("Default");
            if (tempObj.layer < 0) tempObj.layer = 0;

            SpriteRenderer sr = tempObj.AddComponent<SpriteRenderer>();
            sr.sprite = bladeSprite;
            sr.sortingOrder = 10;
            tempObj.transform.localScale = new Vector3(0.1f, 0.1f, 1f);

            CircleCollider2D col = tempObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            Rigidbody2D rb = tempObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.isKinematic = true;

            // TrailRenderer
            GameObject trailChild = new GameObject("Trail");
            trailChild.transform.SetParent(tempObj.transform, false);
            TrailRenderer trail = trailChild.AddComponent<TrailRenderer>();
            trail.time = 0.2f;
            trail.startWidth = 0.25f;
            trail.endWidth = 0.05f;

            Material trailMat = AssetDatabase.LoadAssetAtPath<Material>(TrailMaterialPath);
            if (trailMat != null) trail.sharedMaterial = trailMat;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.2f, 0.8f, 1f), 0f), new GradientColorKey(new Color(0f, 0.4f, 1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            trail.colorGradient = gradient;

            SpinningBladeProjectile proj = tempObj.AddComponent<SpinningBladeProjectile>();
            GameObject hitVfx = AssetDatabase.LoadAssetAtPath<GameObject>(HitVfxPrefabPath);

            SerializedObject so = new SerializedObject(proj);
            so.FindProperty("hitVfxPrefab").objectReferenceValue = hitVfx;
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("trailRenderer").objectReferenceValue = trail;
            so.ApplyModifiedPropertiesWithoutUndo();

            savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempObj, SpinningBladePrefabPath);
            GameObject.DestroyImmediate(tempObj);
        }
        else
        {
            savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpinningBladePrefabPath);
        }

        // 3. Xóa đối tượng thừa trên Scene nếu có
        if (sceneBladeObj != null)
        {
            GameObject.DestroyImmediate(sceneBladeObj);
            Debug.Log("[SpinningBladeSetupHelper] Đã dọn dẹp SpinningBlade GameObject tạm ở root scene.");
        }

        // 4. Gắn kỹ năng lên Player trong GamePlay Scene
        GameObject playerObj = gamePlayScene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(t => t.CompareTag("Player") || t.name == "Player")
            .Select(t => t.gameObject)
            .FirstOrDefault();

        if (playerObj != null)
        {
            SpinningBladeSkill bladeSkill = playerObj.GetComponent<SpinningBladeSkill>();
            if (bladeSkill == null)
            {
                bladeSkill = playerObj.AddComponent<SpinningBladeSkill>();
            }

            GameObject hitVfx = AssetDatabase.LoadAssetAtPath<GameObject>(HitVfxPrefabPath);

            SerializedObject skillSo = new SerializedObject(bladeSkill);
            if (savedPrefab != null) skillSo.FindProperty("spinningBladePrefab").objectReferenceValue = savedPrefab;
            if (hitVfx != null) skillSo.FindProperty("hitVfxPrefab").objectReferenceValue = hitVfx;
            skillSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bladeSkill);

            PlayerChipsetSkillManager skillMgr = playerObj.GetComponent<PlayerChipsetSkillManager>();
            if (skillMgr == null)
            {
                skillMgr = playerObj.AddComponent<PlayerChipsetSkillManager>();
            }

            SerializedObject mgrSo = new SerializedObject(skillMgr);
            mgrSo.FindProperty("spinningBladeSkill").objectReferenceValue = bladeSkill;
            mgrSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skillMgr);

            EditorUtility.SetDirty(playerObj);
            EditorSceneManager.MarkSceneDirty(gamePlayScene);
            EditorSceneManager.SaveScene(gamePlayScene);
        }
    }
}
#endif
