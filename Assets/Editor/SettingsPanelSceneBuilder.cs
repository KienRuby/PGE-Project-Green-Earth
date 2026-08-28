#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tự động xây dựng và đặt GameObject SettingsPanel tĩnh vào Hierarchy của Scene MainMenu.
/// Menu: PGE > UI > Build Settings Panel Only
/// </summary>
[InitializeOnLoad]
public static class SettingsPanelSceneBuilder
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string PrefabDir = "Assets/Prefabs/UI";
    private const string PrefabPath = "Assets/Prefabs/UI/SettingsPanel.prefab";
    private const string BuildRequestPath = "Assets/Editor/PGE_SettingsPanel_BuildRequest.txt";

    static SettingsPanelSceneBuilder()
    {
        EditorApplication.update += TryBuildRequestedUI;
    }

    private static void TryBuildRequestedUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        if (!File.Exists(BuildRequestPath))
        {
            return;
        }

        try
        {
            File.Delete(BuildRequestPath);
        }
        catch { }

        BuildStaticSettingsPanelInMainMenu();
    }

    [MenuItem("PGE/UI/Build Settings Panel Only", priority = 100)]
    [MenuItem("PGE/UI/Rebuild Settings Panel", priority = 101)]
    public static void BuildStaticSettingsPanelInMainMenu()
    {
        // 1. Mở Scene MainMenu nếu chưa mở
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != MainMenuScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            activeScene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        }

        if (!activeScene.IsValid())
        {
            Debug.LogError($"[SettingsPanel] Không tìm thấy scene tại {MainMenuScenePath}");
            return;
        }

        // 2. Tìm Canvas trong scene
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[SettingsPanel] Không tìm thấy Canvas trong MainMenu scene!");
            return;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;

        // 3. Kiểm tra xem SettingsPanel đã tồn tại trong Hierarchy chưa
        SettingsPanelController existing = canvas.GetComponentInChildren<SettingsPanelController>(true);
        if (existing != null)
        {
            Debug.Log($"[SettingsPanel] SettingsPanel đã tồn tại: {existing.gameObject.name}. Đang cập nhật lại tham chiếu...");
            existing.AutoWireReferencesIfMissing();
            existing.BindButtonListeners();
            existing.RefreshLabels();
            existing.gameObject.SetActive(false);

            EditorUtility.SetDirty(existing.gameObject);
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("<color=#00FF88>[SettingsPanel] Đã cập nhật xong SettingsPanel trong Hierarchy!</color>");
            return;
        }

        // 4. Tạo cấu trúc phân cấp tĩnh cho SettingsPanel
        SettingsPanelController panel = SettingsPanelController.BuildPanelHierarchy(canvasRect);
        if (panel == null)
        {
            Debug.LogError("[SettingsPanel] Không thể khởi tạo SettingsPanel!");
            return;
        }

        panel.gameObject.name = "SettingsPanel";
        panel.gameObject.SetActive(false); // Ẩn mặc định khi bắt đầu game
        panel.transform.SetAsLastSibling();

        Undo.RegisterCreatedObjectUndo(panel.gameObject, "Create Static SettingsPanel in Hierarchy");

        // 5. Lưu thành Prefab để tái sử dụng
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder(PrefabDir))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }
        PrefabUtility.SaveAsPrefabAssetAndConnect(panel.gameObject, PrefabPath, InteractionMode.AutomatedAction);

        // 6. Đánh dấu dirty và lưu scene
        EditorUtility.SetDirty(panel.gameObject);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Selection.activeGameObject = panel.gameObject;
        Debug.Log($"<color=#00FF88>[SettingsPanel] ĐÃ TẠO THÀNH CÔNG GameObject 'SettingsPanel' tĩnh trong Hierarchy của {MainMenuScenePath}!</color>");
    }
}
#endif
