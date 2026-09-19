#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AutoCleanMainMenuGarbage
{
    private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("PGE/Tools/Force Clean MainMenu Scene Now")]
    public static void ExecuteClean()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        Scene scene = EditorSceneManager.GetActiveScene();

        if (scene.path != MainMenuPath)
        {
            if (!EditorUtility.DisplayDialog("Clean MainMenu Scene", "Bạn có muốn mở Scene MainMenu để dọn dẹp không?", "Đồng ý", "Hủy"))
            {
                return;
            }
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

            if (n == "Event1" || n == "Player" || n == "[DamageNumberManager]" || n == "TestBuildBodyController" || n.StartsWith("TestBuildBody") || n.StartsWith("BuddyController_Test"))
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
            Undo.DestroyObjectImmediate(obj);
            deletedCount++;
        }

        if (deletedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // Tự động gắn các nút Info và Modal tỷ lệ mở hộp nếu chưa có
        GameObject chipCard = null;
        foreach (var r in scene.GetRootGameObjects())
        {
            var match = System.Linq.Enumerable.FirstOrDefault(r.GetComponentsInChildren<Transform>(true), t => t.name == "Box_Chipset_1x");
            if (match != null) { chipCard = match.gameObject; break; }
        }

        if (chipCard != null && chipCard.transform.Find("Button_Info") == null)
        {
        }
    }
}
#endif
