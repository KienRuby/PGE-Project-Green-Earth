#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class OneShot_SyncCritIconSettings
{
    static OneShot_SyncCritIconSettings()
    {
        OneShotEditorUtility.ExecuteOnceAndSelfDelete("OneShot_SyncCritIconSettings", DoSync);
    }

    [MenuItem("PGE/Combat/Đồng bộ kích thước Icon Chí Mạng")]
    public static void DoSync()
    {
        float targetSize = 0.314f;
        float targetSpacing = 0.03f;
        Vector2 targetOffset = new Vector2(0f, 0.01f);

        // 1. Cập nhật Prefab DamageNumber.prefab
        string prefabPath = "Assets/Prefabs/DamageNumber.prefab";
        GameObject prefabGo = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabGo != null)
        {
            DamageNumber dn = prefabGo.GetComponent<DamageNumber>();
            if (dn != null)
            {
                dn.CritIconSize = targetSize;
                dn.CritIconSpacing = targetSpacing;
                dn.CritIconOffset = targetOffset;
                dn.UpdateCritLayout();
                EditorUtility.SetDirty(dn);
            }
            PrefabUtility.SavePrefabAsset(prefabGo);
        }

        // 2. Cập nhật Scene GamePlay.unity
        Scene activeScene = SceneManager.GetActiveScene();
        bool isGamePlayScene = (activeScene.path == "Assets/Scenes/GamePlay.unity");
        if (!isGamePlayScene)
        {
            activeScene = EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity", OpenSceneMode.Single);
        }

        DamageNumberManager manager = Object.FindObjectOfType<DamageNumberManager>();
        if (manager != null)
        {
            manager.DefaultCritIconSize = targetSize;
            manager.DefaultCritIconSpacing = targetSpacing;
            manager.DefaultCritIconOffset = targetOffset;
            manager.DefaultCritScale = 1.25f;
            EditorUtility.SetDirty(manager);
        }

        DamageNumber[] all = Object.FindObjectsOfType<DamageNumber>();
        foreach (var preview in all)
        {
            if (preview != null && preview.gameObject.name == "[DamageNumber_Preview]")
            {
                preview.CritIconSize = targetSize;
                preview.CritIconSpacing = targetSpacing;
                preview.CritIconOffset = targetOffset;
                preview.UpdateCritLayout();
                EditorUtility.SetDirty(preview);
            }
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        AssetDatabase.SaveAssets();

        Debug.Log("[OneShot] Da dong bo hoan tat kich thuoc icon chi mang (0.314) cho Prefab, Scene va Manager!");
    }
}
#endif
