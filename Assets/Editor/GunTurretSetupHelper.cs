#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class GunTurretSetupHelper
{
    private const string GamePlayScenePath = "Assets/Scenes/GamePlay.unity";
    private const string TurretPrefabPath = "Assets/Prefabs/Chipset/GunTurret.prefab";
    private const string ProjectilePrefabPath = "Assets/Prefabs/Projectile.prefab";
    private const string ExplosionPrefabPath = "Assets/Prefabs/VFX Boom.prefab";
    private const string RequestTriggerPath = "Assets/Editor/PGE_GunTurretSetup_Request.txt";

    static GunTurretSetupHelper()
    {
        EditorApplication.update += TryAutoSetupGunTurret;
    }

    [MenuItem("PGE/Skills/Setup Gun Turret Prefab and Player")]
    public static void SetupGunTurretAndPlayer()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[GunTurretSetupHelper] Stop Play Mode before setting up Gun Turret.");
            return;
        }

        SetupGunTurretPrefab();
        SetupPlayerInGamePlayScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GunTurretSetupHelper] ✅ Đã cấu hình thành công GunTurret.prefab và Player trong GamePlay.unity!");
    }

    public static void SetupGunTurretPrefab()
    {
        if (!File.Exists(TurretPrefabPath))
        {
            Debug.LogError($"[GunTurretSetupHelper] Không tìm thấy prefab tại {TurretPrefabPath}");
            return;
        }

        GameObject prefabContents = PrefabUtility.LoadPrefabContents(TurretPrefabPath);
        if (prefabContents == null) return;

        try
        {
            GunTurret turret = prefabContents.GetComponent<GunTurret>();
            if (turret == null)
            {
                turret = prefabContents.AddComponent<GunTurret>();
            }

            Transform aimPivot = prefabContents.transform.Find("AimPivot");
            Transform firePoint = aimPivot != null ? aimPivot.Find("FirePoint") : null;
            Transform gunTr = aimPivot != null ? aimPivot.Find("GunSprite") : null;
            SpriteRenderer gunSr = gunTr != null ? gunTr.GetComponent<SpriteRenderer>() : null;
            GameObject bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            GameObject boomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPrefabPath);

            SerializedObject so = new SerializedObject(turret);
            so.FindProperty("aimPivot").objectReferenceValue = aimPivot;
            so.FindProperty("firePoint").objectReferenceValue = firePoint;
            so.FindProperty("gunSpriteRenderer").objectReferenceValue = gunSr;
            so.FindProperty("projectilePrefab").objectReferenceValue = bulletPrefab;
            so.FindProperty("explosionVfxPrefab").objectReferenceValue = boomPrefab;
            so.FindProperty("damage").intValue = 27;
            so.FindProperty("fireRate").floatValue = 3f;
            so.FindProperty("bulletSpeed").floatValue = 12f;
            so.FindProperty("duration").floatValue = 10f;
            so.FindProperty("detectionWidth").floatValue = 6f;
            so.FindProperty("detectionHeight").floatValue = 10f;

            LayerMask enemyMask = LayerMask.GetMask("Enemy");
            if (enemyMask.value == 0) enemyMask = 1 << 7;
            so.FindProperty("enemyLayer").intValue = enemyMask.value;

            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefabContents, TurretPrefabPath);
            Debug.Log("[GunTurretSetupHelper] ✅ Đã cập nhật GunTurret.prefab với đầy đủ liên kết nòng súng và đạn.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }

    public static void SetupPlayerInGamePlayScene()
    {
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

        GameObject playerObj = gamePlayScene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(t => t.CompareTag("Player") || t.name == "Player")
            .Select(t => t.gameObject)
            .FirstOrDefault();

        if (playerObj == null)
        {
            Debug.LogWarning("[GunTurretSetupHelper] Không tìm thấy Player GameObject trong GamePlay.unity");
            return;
        }

        // 1. Thêm / cập nhật GunTurretSkill
        GunTurretSkill turretSkill = playerObj.GetComponent<GunTurretSkill>();
        if (turretSkill == null)
        {
            turretSkill = playerObj.AddComponent<GunTurretSkill>();
        }

        GameObject turretPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TurretPrefabPath);
        GameObject bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
        GameObject boomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPrefabPath);

        SerializedObject skillSo = new SerializedObject(turretSkill);
        skillSo.FindProperty("turretPrefab").objectReferenceValue = turretPrefab;
        skillSo.FindProperty("projectilePrefab").objectReferenceValue = bulletPrefab;
        skillSo.FindProperty("explosionVfxPrefab").objectReferenceValue = boomPrefab;
        skillSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(turretSkill);

        // 2. Thêm / cập nhật PlayerChipsetSkillManager
        PlayerChipsetSkillManager skillMgr = playerObj.GetComponent<PlayerChipsetSkillManager>();
        if (skillMgr == null)
        {
            skillMgr = playerObj.AddComponent<PlayerChipsetSkillManager>();
        }

        SerializedObject mgrSo = new SerializedObject(skillMgr);
        mgrSo.FindProperty("gunTurretSkill").objectReferenceValue = turretSkill;
        mgrSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(skillMgr);

        EditorUtility.SetDirty(playerObj);
        EditorSceneManager.MarkSceneDirty(gamePlayScene);
        EditorSceneManager.SaveScene(gamePlayScene);
        Debug.Log("[GunTurretSetupHelper] ✅ Đã cấu hình GunTurretSkill & PlayerChipsetSkillManager trên Player trong GamePlay.unity");
    }

    private static void TryAutoSetupGunTurret()
    {
        if (!File.Exists(RequestTriggerPath) ||
            EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling ||
            EditorApplication.isUpdating)
        {
            return;
        }

        try
        {
            File.Delete(RequestTriggerPath);
        }
        catch {}

        SetupGunTurretAndPlayer();
    }
}
#endif
