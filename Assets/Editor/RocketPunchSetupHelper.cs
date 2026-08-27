#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Helper chỉ chạy KHI NGƯỜI DÙNG CHỦ ĐỘNG BẤM MENU.
/// TUYỆT ĐỐI KHÔNG tự động chạy ngầm, KHÔNG ghi đè làm mất Sprite / Scale bạn đã chỉnh trên Prefab.
/// </summary>
public static class RocketPunchSetupHelper
{
    private const string GamePlayScenePath = "Assets/Scenes/GamePlay.unity";
    private const string RocketPunchPrefabPath = "Assets/Prefabs/Chipset/RocketPunch.prefab";
    private const string ExplosionPrefabPath = "Assets/Prefabs/VFX Boom.prefab";

    [MenuItem("PGE/Skills/Setup Rocket Punch Prefab and Player")]
    public static void SetupRocketPunchPrefabAndPlayer()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[RocketPunchSetupHelper] Vui lòng dừng Play Mode trước khi cấu hình.");
            return;
        }

        // Chỉ kiểm tra và gắn reference vào Player trong Scene, KHÔNG tạo lại hay xóa đè Prefab
        SetupPlayerInScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RocketPunchSetupHelper] ✅ Đã lưu và bảo toàn nguyên vẹn Sprite/Scale của RocketPunch.prefab!");
    }

    private static void SetupPlayerInScene()
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

        if (playerObj != null)
        {
            RocketPunchSkill punchSkill = playerObj.GetComponent<RocketPunchSkill>();
            if (punchSkill == null)
            {
                punchSkill = playerObj.AddComponent<RocketPunchSkill>();
            }

            GameObject punchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RocketPunchPrefabPath);
            GameObject boomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPrefabPath);

            SerializedObject skillSo = new SerializedObject(punchSkill);
            if (punchPrefab != null) skillSo.FindProperty("rocketPunchPrefab").objectReferenceValue = punchPrefab;
            if (boomPrefab != null) skillSo.FindProperty("explosionVfxPrefab").objectReferenceValue = boomPrefab;
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

            EditorUtility.SetDirty(playerObj);
            EditorSceneManager.MarkSceneDirty(gamePlayScene);
            EditorSceneManager.SaveScene(gamePlayScene);
        }
    }
}
#endif
