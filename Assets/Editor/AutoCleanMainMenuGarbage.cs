#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AutoCleanMainMenuGarbage
{
    private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

    static AutoCleanMainMenuGarbage()
    {
        EditorApplication.delayCall += ExecuteClean;
    }

    [MenuItem("PGE/Tools/Force Clean MainMenu Scene Now")]
    public static void ExecuteClean()
    {
        Scene scene = EditorSceneManager.GetActiveScene();

        if (scene.path != MainMenuPath)
        {
            scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
        }

        if (!scene.IsValid()) return;

        var roots = scene.GetRootGameObjects();
        int deletedCount = 0;
        var toDelete = new List<GameObject>();

        foreach (var root in roots)
        {
            if (root == null) continue;
            string n = root.name;

            if (n == "Event1" || n == "Player" || n == "[DamageNumberManager]")
            {
                toDelete.Add(root);
            }
            else if (n == "Canvas" && root.transform.childCount == 0)
            {
                toDelete.Add(root);
            }
        }

        foreach (var obj in toDelete)
        {
            Debug.Log($"[AutoClean] Đã tự động xóa đối tượng rác: {obj.name}");
            Undo.DestroyObjectImmediate(obj);
            deletedCount++;
        }

        if (deletedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[AutoClean] >>> ĐÃ DỌN DẸP THÀNH CÔNG {deletedCount} ĐỐI TƯỢNG RÁC VÀ ĐÃ LƯU SCENE MAINMENU! <<<");
        }
        else
        {
            Debug.Log("[AutoClean] Scene MainMenu đã hoàn toàn sạch sẽ, không còn đối tượng rác nào.");
        }
    }
}
#endif
