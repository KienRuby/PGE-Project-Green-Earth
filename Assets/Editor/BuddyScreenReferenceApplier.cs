using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Áp dụng các sprite Buddy gốc vào màn MainMenu hiện có mà không dựng lại
/// controller, modal chi tiết hoặc hệ thống điều hướng.
/// </summary>
[InitializeOnLoad]
public static class BuddyScreenReferenceApplier
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string IconSheetPath = "Assets/Sprites/UI/Buddy/icon buddy.png";
    private const string ButtonSheetPath = "Assets/Sprites/UI/Buddy/nút màn buddy.png";
    private const string AppliedKey = "PGE.BuddyScreenReferenceApplier.v3";

    static BuddyScreenReferenceApplier()
    {
        EditorApplication.delayCall += ApplyOnce;
    }

    [MenuItem("PGE/UI/Apply Buddy Reference Screen")]
    public static void ApplyFromMenu()
    {
        EditorPrefs.DeleteKey(AppliedKey);
        ApplyOnce();
    }

    private static void ApplyOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorPrefs.GetBool(AppliedKey, false)) return;

        AssetDatabase.ImportAsset(IconSheetPath, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(ButtonSheetPath, ImportAssetOptions.ForceSynchronousImport);

        // Import đồng bộ có thể kéo dài đủ lâu để người dùng bấm Play giữa chừng.
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Dictionary<string, Sprite> sourceIcons = LoadSprites(IconSheetPath);
        Dictionary<string, Sprite> sourceButtons = LoadSprites(ButtonSheetPath);

        string[] requiredIconSprites = { "openLocke", "Locke", "1", "2", "3", "drone-snowflake", "drone-spider", "drone-antenna-eye", "drone-cross-visor", "drone-stealth-wing" };
        string[] requiredButtonSprites = { "Drone", "Robot Pet On", "Robot Pet OFF", "khung", "Empty" };
        if (requiredIconSprites.Any(name => !sourceIcons.ContainsKey(name)) ||
            requiredButtonSprites.Any(name => !sourceButtons.ContainsKey(name)))
        {
            Debug.LogError("[BuddyScreenReferenceApplier] Sprite Buddy chưa được slice/import đầy đủ.");
            return;
        }

        Scene scene = SceneManager.GetSceneByPath(MainMenuScenePath);
        bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;
        if (openedTemporarily)
        {
            scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Additive);
        }

        try
        {
            BuddyController[] controllers = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<BuddyController>(true))
                .ToArray();
            if (controllers.Length == 0)
            {
                Debug.LogError("[BuddyScreenReferenceApplier] Không tìm thấy BuddyController trong MainMenu.");
                return;
            }

            foreach (BuddyController controller in controllers)
            {
                ApplyControllerSprites(controller, sourceIcons, sourceButtons);
                ApplyHierarchyVisuals(controller, sourceIcons, sourceButtons);
                EditorUtility.SetDirty(controller);
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorPrefs.SetBool(AppliedKey, true);
            Debug.Log($"[BuddyScreenReferenceApplier] Đã hoàn thiện {controllers.Length} BuddyPanel bằng asset gốc.");
        }
        finally
        {
            if (openedTemporarily && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static void ApplyControllerSprites(
        BuddyController controller,
        IReadOnlyDictionary<string, Sprite> sourceIcons,
        IReadOnlyDictionary<string, Sprite> sourceButtons)
    {
        SerializedObject serialized = new SerializedObject(controller);

        string[] iconKeys =
        {
            "drone-snowflake", "drone-spider", "drone-antenna-eye", "drone-cross-visor", "drone-snowflake",
            "drone-spider", "drone-antenna-eye", "drone-cross-visor", "drone-stealth-wing", "drone-stealth-wing",
            "drone-antenna-eye", "drone-spider"
        };
        SerializedProperty icons = serialized.FindProperty("droneIcons");
        icons.arraySize = iconKeys.Length;
        for (int i = 0; i < iconKeys.Length; i++)
        {
            sourceIcons.TryGetValue(iconKeys[i], out Sprite sprite);
            icons.GetArrayElementAtIndex(i).objectReferenceValue = sprite;
        }

        SerializedProperty frames = serialized.FindProperty("frameSprites");
        frames.arraySize = 4;
        frames.GetArrayElementAtIndex(0).objectReferenceValue = sourceIcons["openLocke"];
        frames.GetArrayElementAtIndex(1).objectReferenceValue = sourceIcons["openLocke"];
        frames.GetArrayElementAtIndex(2).objectReferenceValue = sourceIcons["openLocke"];
        frames.GetArrayElementAtIndex(3).objectReferenceValue = sourceIcons["openLocke"];
        serialized.FindProperty("emptySlotFrameSprite").objectReferenceValue = sourceButtons["Empty"];
        serialized.FindProperty("lockedSlotFrameSprite").objectReferenceValue = sourceIcons["Locke"];

        SetImageSprite(serialized.FindProperty("droneModeBg").objectReferenceValue as Image, sourceButtons["Drone"]);
        SetImageSprite(serialized.FindProperty("robotPetModeBg").objectReferenceValue as Image, sourceButtons["Robot Pet On"]);

        SetImageSprite(serialized.FindProperty("preset1Bg").objectReferenceValue as Image, sourceIcons["1"]);
        SetImageSprite(serialized.FindProperty("preset2Bg").objectReferenceValue as Image, sourceIcons["2"]);
        SetImageSprite(serialized.FindProperty("preset3Bg").objectReferenceValue as Image, sourceIcons["3"]);

        ClearText(serialized.FindProperty("preset1Text").objectReferenceValue as TMP_Text);
        ClearText(serialized.FindProperty("preset2Text").objectReferenceValue as TMP_Text);
        ClearText(serialized.FindProperty("preset3Text").objectReferenceValue as TMP_Text);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ApplyHierarchyVisuals(
        BuddyController controller,
        IReadOnlyDictionary<string, Sprite> sourceIcons,
        IReadOnlyDictionary<string, Sprite> sourceButtons)
    {
        Transform droneTab = FindDeep(controller.transform, "TabDrone");
        Transform robotTab = FindDeep(controller.transform, "TabRobotPet");
        HideChildren(droneTab, "Label", "Wave");
        HideChildren(robotTab, "Label", "Lock");

        Transform equippedRow = FindDeep(controller.transform, "EquippedRow");
        if (equippedRow != null && equippedRow.TryGetComponent(out HorizontalLayoutGroup equippedLayout))
        {
            equippedLayout.spacing = 28f;
        }

        foreach (BuddyCardUI card in controller.GetComponentsInChildren<BuddyCardUI>(true))
        {
            Transform bottomBar = card.transform.Find("NormalContentGroup/BottomBar");
            if (bottomBar != null && bottomBar.TryGetComponent(out Image bottomBarImage))
            {
                bottomBarImage.color = new Color(1f, 1f, 1f, 0f);
            }

            TMP_Text level = card.transform.Find("NormalContentGroup/LevelText")?.GetComponent<TMP_Text>();
            TMP_Text progress = card.transform.Find("NormalContentGroup/BottomBar/ProgressText")?.GetComponent<TMP_Text>();
            if (level != null)
            {
                level.color = Color.white;
                level.fontSize = 30f;
            }
            if (progress != null)
            {
                progress.color = Color.black;
                progress.fontSize = 27f;
            }
        }
    }

    private static Dictionary<string, Sprite> LoadSprites(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .GroupBy(sprite => sprite.name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    private static void SetImageSprite(Image image, Sprite sprite)
    {
        if (image == null || sprite == null) return;
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        EditorUtility.SetDirty(image);
    }

    private static void ClearText(TMP_Text text)
    {
        if (text == null) return;
        text.text = string.Empty;
        EditorUtility.SetDirty(text);
    }

    private static void HideChildren(Transform parent, params string[] names)
    {
        if (parent == null) return;
        foreach (string name in names)
        {
            Transform child = parent.Find(name);
            if (child != null) child.gameObject.SetActive(false);
        }
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(child.name, name, StringComparison.OrdinalIgnoreCase)) return child;
        }
        return null;
    }
}
