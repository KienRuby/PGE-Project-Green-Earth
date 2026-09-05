#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor script tự động duyệt qua các Scene trong project, gắn UIDissolveController lên toàn bộ các Popup/Modal/Window/Dialog,
/// thiết lập thông số mặc định chuẩn AAA, và lưu Scene lại vĩnh viễn.
/// </summary>
public static class UIDissolveSceneInstaller
{
    private static readonly string[] MainScenePaths = new string[]
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/GamePlay.unity",
        "Assets/Scenes/KhoiDau.unity"
    };

    [MenuItem("Tools/PGE/Auto-Attach UI Dissolve To All Scene Popups", priority = 100)]
    public static void InstallInAllScenesAndSave()
    {
        string currentActiveScene = EditorSceneManager.GetActiveScene().path;
        int totalAttached = 0;

        foreach (string scenePath in MainScenePaths)
        {
            if (!System.IO.File.Exists(scenePath)) continue;

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int attachedInScene = InstallInScene(scene);
            totalAttached += attachedInScene;

            if (attachedInScene > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[UIDissolveSceneInstaller] Đã gắn {attachedInScene} controller trong scene {scenePath} và lưu scene.");
            }
        }

        if (!string.IsNullOrEmpty(currentActiveScene) && System.IO.File.Exists(currentActiveScene))
        {
            EditorSceneManager.OpenScene(currentActiveScene, OpenSceneMode.Single);
        }

        EditorUtility.DisplayDialog(
            "UI Dissolve Auto-Installer",
            $"Hoàn tất!\nĐã tự động thêm UIDissolveController vào {totalAttached} Popup/Modal/Window/Dialog trên các Scene chính.",
            "OK");
    }

    [MenuItem("Tools/PGE/Auto-Attach UI Dissolve To Current Active Scene Only", priority = 101)]
    public static void InstallInActiveSceneOnly()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        int count = InstallInScene(scene);

        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        EditorUtility.DisplayDialog(
            "UI Dissolve Auto-Installer",
            $"Đã tự động thêm UIDissolveController vào {count} Popup/Modal trong scene hiện tại ({scene.name}).",
            "OK");
    }

    public static int InstallInScene(Scene scene)
    {
        int count = 0;
        GameObject[] rootObjects = scene.GetRootGameObjects();

        foreach (GameObject root in rootObjects)
        {
            Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas c in canvases)
            {
                RectTransform[] rects = c.GetComponentsInChildren<RectTransform>(true);
                foreach (RectTransform rt in rects)
                {
                    if (rt == null || rt == c.transform) continue;

                    if (UIDissolveAutoInstaller.IsEligiblePopup(rt.gameObject.name))
                    {
                        if (AttachControllerToGameObject(rt.gameObject))
                        {
                            count++;
                        }
                    }
                }
            }
        }

        return count;
    }

    private static bool AttachControllerToGameObject(GameObject go)
    {
        if (go == null) return false;

        UIDissolveController controller = go.GetComponent<UIDissolveController>();
        bool wasAdded = false;

        if (controller == null)
        {
            controller = Undo.AddComponent<UIDissolveController>(go);
            wasAdded = true;
        }

        controller.InitializeIfNeeded();

        // Tự động kết nối nút Close / DimBackground
        UIDissolveAutoInstaller.AutoWireCloseButtons(go, controller);

        EditorUtility.SetDirty(go);
        return wasAdded;
    }
}
#endif
