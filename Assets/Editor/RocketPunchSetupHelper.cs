#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Helper cấu hình và kết nối đồng bộ Rocket Punch (ShoulderGunPivot, PunchMuzzle, Prefabs)
/// cho Player.prefab và tất cả các scene gameplay (GamePlay.unity, GenMine.unity, v.v.).
/// </summary>
public static class RocketPunchSetupHelper
{
    private const string GamePlayScenePath = "Assets/Scenes/GamePlay.unity";
    private const string GenMineScenePath = "Assets/Scenes/GenMine.unity";
    private const string RocketPunchPrefabPath = "Assets/Prefabs/Chipset/RocketPunch.prefab";
    private const string ExplosionPrefabPath = "Assets/Prefabs/VFX Boom.prefab";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string ShoulderGunSpritePath = "Assets/Sprites/Character/súng phóng tên lửa bàn tay.png";

    [MenuItem("PGE/Skills/Setup Rocket Punch Prefab and Player")]
    public static void SetupRocketPunchPrefabAndPlayer()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[RocketPunchSetupHelper] Vui lòng dừng Play Mode trước khi cấu hình.");
            return;
        }

        SetupPrefab();
        SetupSceneAtPath(GamePlayScenePath);
        SetupSceneAtPath(GenMineScenePath);

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.isLoaded)
        {
            SetupPlayerInScene(activeScene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>[RocketPunchSetupHelper] Setup Rocket Punch for Player prefab and scenes completed!</color>");
    }

    [InitializeOnLoadMethod]
    private static void AutoSyncRocketPunchOnCompile()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            SetupPrefab();
            Scene active = SceneManager.GetActiveScene();
            if (active.isLoaded)
            {
                SetupPlayerInScene(active);
            }
        };
    }

    private static void SetupPrefab()
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab == null) return;

        ConfigurePlayerObject(playerPrefab);
        EditorUtility.SetDirty(playerPrefab);
    }

    private static void SetupSceneAtPath(string scenePath)
    {
        if (!File.Exists(scenePath)) return;

        Scene activeScene = SceneManager.GetActiveScene();
        bool wasActive = string.Equals(activeScene.path, scenePath, StringComparison.OrdinalIgnoreCase);

        Scene sceneToProcess;
        bool openedAdditive = false;

        if (wasActive)
        {
            sceneToProcess = activeScene;
        }
        else
        {
            sceneToProcess = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            openedAdditive = true;
        }

        try
        {
            SetupPlayerInScene(sceneToProcess);
            EditorSceneManager.MarkSceneDirty(sceneToProcess);
            EditorSceneManager.SaveScene(sceneToProcess);
        }
        finally
        {
            if (openedAdditive && sceneToProcess.IsValid() && sceneToProcess.isLoaded)
            {
                EditorSceneManager.CloseScene(sceneToProcess, true);
            }
        }
    }

    private static void SetupPlayerInScene(Scene scene)
    {
        GameObject playerObj = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(t => t.CompareTag("Player") || t.name == "Player")
            .Select(t => t.gameObject)
            .FirstOrDefault();

        if (playerObj != null)
        {
            ConfigurePlayerObject(playerObj);
            EditorUtility.SetDirty(playerObj);
        }
    }

    private static void ConfigurePlayerObject(GameObject playerObj)
    {
        RocketPunchSkill punchSkill = playerObj.GetComponent<RocketPunchSkill>();
        if (punchSkill == null)
        {
            punchSkill = playerObj.AddComponent<RocketPunchSkill>();
        }

        punchSkill.EnsureShoulderGunReferences();

        GameObject punchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RocketPunchPrefabPath);
        GameObject boomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPrefabPath);
        Sprite gunSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShoulderGunSpritePath);

        SerializedObject skillSo = new SerializedObject(punchSkill);
        if (punchPrefab != null) skillSo.FindProperty("rocketPunchPrefab").objectReferenceValue = punchPrefab;
        if (boomPrefab != null) skillSo.FindProperty("explosionVfxPrefab").objectReferenceValue = boomPrefab;
        if (gunSprite != null) skillSo.FindProperty("shoulderGunSprite").objectReferenceValue = gunSprite;

        Transform pivot = playerObj.transform.Find("thân/ShoulderGunPivot") ?? playerObj.transform.Find("ShoulderGunPivot");
        if (pivot != null)
        {
            skillSo.FindProperty("shoulderGunPivot").objectReferenceValue = pivot;
            Transform muzzle = pivot.Find("GunPunchVisual/PunchMuzzle") ?? pivot.Find("PunchMuzzle");
            if (muzzle != null)
            {
                skillSo.FindProperty("punchMuzzle").objectReferenceValue = muzzle;
            }
        }

        skillSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(punchSkill);

        PlayerChipsetSkillManager skillMgr = playerObj.GetComponent<PlayerChipsetSkillManager>();
        if (skillMgr == null)
        {
            skillMgr = playerObj.AddComponent<PlayerChipsetSkillManager>();
        }

        SerializedObject mgrSo = new SerializedObject(skillMgr);
        mgrSo.FindProperty("rocketPunchSkill").objectReferenceValue = punchSkill;
        mgrSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(skillMgr);
    }
}
#endif
