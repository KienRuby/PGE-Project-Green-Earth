#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MainMenuTowerDefCleaner
{
    public const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    static MainMenuTowerDefCleaner()
    {
        EditorApplication.delayCall += DelayClean;
    }

    private static void DelayClean()
    {
        CleanMainMenuTowerDefArtifacts();
    }

    [MenuItem("PGE/UI/Clean TowerDef From MainMenu")]
    public static void CleanMainMenuTowerDefArtifacts()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        bool anyCleaned = false;

        // 1. Kiểm tra các scene đang mở trong Hierarchy
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.isLoaded && (s.name == "MainMenu" || s.path == MainMenuScenePath))
            {
                if (CleanSceneObjects(s))
                {
                    anyCleaned = true;
                    EditorSceneManager.MarkSceneDirty(s);
                    EditorSceneManager.SaveScene(s);
                }
            }
        }

        // 2. Nếu MainMenu chưa được mở, mở additive để dọn dẹp và lưu lại
        Scene currentActive = SceneManager.GetActiveScene();
        if (currentActive.path != MainMenuScenePath)
        {
            Scene mmScene = SceneManager.GetSceneByPath(MainMenuScenePath);
            bool needClose = false;
            if (!mmScene.IsValid() || !mmScene.isLoaded)
            {
                mmScene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Additive);
                needClose = true;
            }

            if (mmScene.IsValid() && mmScene.isLoaded)
            {
                if (CleanSceneObjects(mmScene))
                {
                    anyCleaned = true;
                    EditorSceneManager.MarkSceneDirty(mmScene);
                    EditorSceneManager.SaveScene(mmScene);
                }

                if (needClose)
                {
                    if (currentActive.IsValid()) EditorSceneManager.SetActiveScene(currentActive);
                    EditorSceneManager.CloseScene(mmScene, true);
                }
            }
        }

        if (anyCleaned)
        {
            Debug.Log("[MainMenuTowerDefCleaner] Đã dọn dẹp và xóa sạch toàn bộ bản đồ Tower Def khỏi scene MainMenu thành công!");
        }
    }

    private static bool CleanSceneObjects(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return false;

        bool dirty = false;
        var roots = scene.GetRootGameObjects();

        foreach (var root in roots)
        {
            if (root == null) continue;

            // Xóa bất kỳ đối tượng TowerDef nào nằm sâu trong cây phân cấp
            Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);
            for (int i = allChildren.Length - 1; i >= 0; i--)
            {
                Transform tr = allChildren[i];
                if (tr == null) continue;

                if (tr.name == "PlayArea" || tr.name == "BackgroundGrid" || tr.name == "UpperGrid" ||
                    tr.name == "WallAndGate" || tr.name == "DefenseGrid" || tr.name == "TowerDefCanvas")
                {
                    Debug.Log($"[MainMenuTowerDefCleaner] Xóa đối tượng {tr.name} khỏi {scene.name}");
                    Undo.DestroyObjectImmediate(tr.gameObject);
                    dirty = true;
                }
            }

            // Xóa bất kỳ component TowerDefGameManager nào bị lạc trong MainMenu
            TowerDefGameManager[] gms = root.GetComponentsInChildren<TowerDefGameManager>(true);
            for (int i = gms.Length - 1; i >= 0; i--)
            {
                if (gms[i] != null)
                {
                    Debug.Log($"[MainMenuTowerDefCleaner] Xóa TowerDefGameManager khỏi {scene.name}");
                    Undo.DestroyObjectImmediate(gms[i].gameObject);
                    dirty = true;
                }
            }

            // Xóa bất kỳ Gate, Turret, Cell nào bị lạc ngoài scene TowerDef
            TowerDefGate[] gates = root.GetComponentsInChildren<TowerDefGate>(true);
            for (int i = gates.Length - 1; i >= 0; i--)
            {
                if (gates[i] != null)
                {
                    Debug.Log($"[MainMenuTowerDefCleaner] Xóa TowerDefGate khỏi {scene.name}");
                    Undo.DestroyObjectImmediate(gates[i].gameObject);
                    dirty = true;
                }
            }

            TowerDefGridCell[] cells = root.GetComponentsInChildren<TowerDefGridCell>(true);
            for (int i = cells.Length - 1; i >= 0; i--)
            {
                if (cells[i] != null)
                {
                    Debug.Log($"[MainMenuTowerDefCleaner] Xóa TowerDefGridCell khỏi {scene.name}");
                    Undo.DestroyObjectImmediate(cells[i].gameObject);
                    dirty = true;
                }
            }
        }

        return dirty;
    }
}
#endif
