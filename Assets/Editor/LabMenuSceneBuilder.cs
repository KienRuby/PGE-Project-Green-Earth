#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class LabMenuSceneBuilder
{
    static LabMenuSceneBuilder()
    {
        EditorApplication.update += TryBuildRequestedScene;
        EditorApplication.update += TryBuildRequestedShopPanel;
        EditorApplication.update += TryBuildRequestedChipsetPanel;
        EditorApplication.update += TryApplyRequestedGreenChipsetFrames;
        EditorApplication.update += TryApplyRequestedSelectedBottomBarLayout;
        EditorApplication.update += TryApplyRequestedUpgradeArrowLayout;
        EditorApplication.update += TryBuildRequestedBuddyPanel;
        EditorApplication.update += TryUpdateRequestedLabStats;
    }

    private sealed class SlotView
    {
        public GameObject lockedGroup;
        public GameObject unlockedGroup;
        public Image iconImage;
        public TMP_Text levelText;
        public TMP_Text nameText;
        public Image slotBackground;
        public Button slotButton;
    }

    private sealed class ShopOfferView
    {
        public Button button;
        public TMP_Text priceText;
    }

    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string BackupScenePath = "Assets/Scenes/MainMenu.before-lab-ui.unity";
    private const string BackgroundPath = "Assets/UI/Lab/Generated/lab-background.png";
    private const string IconAtlasPath = "Assets/UI/Lab/Generated/lab-icon-atlas.png";
    private const string StatSpriteSheetPath = "Assets/Sprites/UI/Chiso_NewStats.png";
    private const string ChipsetAtlasPath = "Assets/UI/Chipset/Generated/chipset-atlas.png";
    private const string BuddyAtlasPath = "Assets/UI/Buddy/Generated/buddy-atlas.png";
    private const string PreviewPath = "Assets/UI/Lab/Generated/lab-menu-preview.png";
    private const string ChipsetPreviewPath = "Assets/UI/Chipset/Generated/chipset-menu-preview.png";
    private const string ChipsetEquippedFramePath = "Assets/Sprites/UI/Chipset/Extracted/Frame_Equipped_Box.png";
    private const string BuddyPreviewPath = "Assets/UI/Buddy/Generated/buddy-menu-preview.png";
    private const string BuildRequestPath = "Assets/Editor/PGE_LabUI_BuildRequest.txt";
    private const string ShopBuildRequestPath = "Assets/Editor/PGE_ShopUI_BuildRequest.txt";
    private const string ChipsetBuildRequestPath = "Assets/Editor/PGE_ChipsetUI_BuildRequest.txt";
    private const string ChipsetGreenFramesRequestPath = "Assets/Editor/PGE_ChipsetGreenFrames_Request.txt";
    private const string ChipsetBottomBarLayoutRequestPath = "Assets/Editor/PGE_ChipsetBottomBarLayout_Request.txt";
    private const string ChipsetUpgradeArrowLayoutRequestPath = "Assets/Editor/PGE_ChipsetUpgradeArrowLayout_Request.txt";
    private const string BuddyBuildRequestPath = "Assets/Editor/PGE_BuddyUI_BuildRequest.txt";
    private const string LabStatsBuildRequestPath = "Assets/Editor/PGE_LabStats_BuildRequest.txt";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    private static readonly Color Navy = new Color32(8, 39, 69, 255);
    private static readonly Color Border = new Color32(8, 30, 42, 255);
    private static readonly Color TealBorder = new Color32(94, 213, 205, 255);
    private static readonly Color BrightCyan = new Color32(80, 225, 220, 255);
    private static readonly Color Panel = new Color32(31, 87, 94, 245);
    private static readonly Color DarkPanel = new Color32(11, 48, 62, 250);
    private static readonly Color PanelSelected = new Color32(48, 94, 111, 255);
    private static readonly Color BrightTeal = new Color32(76, 186, 178, 255);
    private static readonly Color MutedTeal = new Color32(27, 74, 82, 255);
    private static readonly Color Cream = new Color32(239, 247, 238, 255);
    private static readonly Color Yellow = new Color32(255, 203, 73, 255);
    private static readonly Color FieryRed = new Color32(130, 20, 20, 255);
    private static readonly Color FieryOrange = new Color32(255, 120, 30, 255);
    private static readonly string[] LabStatNames =
    {
        "HP", "RECOVERY", "AUTO RECOVERY", "DEF",
        "ATK", "CRIT RATE", "CRIT DAMAGE", "OBTAINED CHIPS",
        "RANGED DEFENSE", "DRONE ATK", "TURRET ATK", "TURRET DURATION",
        "EVADE", "LIFE STEAL", "MOVE SPEED", "CHIPSET SELECTION"
    };
    private static readonly string[] LabStatSpriteNames =
    {
        "hp", "recovery", "auto-recovery", "def",
        "atk", "crit-rate", "crit-damage", "obtained-chips",
        "ranged-defense", "drone-atk", "turret-atk", "turret-duration",
        "evade", "life-steal", "move-speed", "chipset-selection"
    };
    private static readonly Color[] LabRarityColors =
    {
        new Color32(245, 245, 245, 255),
        new Color32(38, 82, 145, 255),
        new Color32(94, 55, 142, 255),
        new Color32(170, 128, 35, 255)
    };

    private static TMP_FontAsset font;

    [MenuItem("PGE/UI/Rebuild Full Main Menu (Chipset, Buddy & Lab)")]
    public static void BuildFromMenu()
    {
        BuildLabMenuScene();
    }

    [MenuItem("PGE/UI/Rebuild Buddy Panel")]
    public static void RebuildBuddyPanel()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[LabMenuSceneBuilder] Stop Play Mode before rebuilding the Buddy panel.");
            return;
        }

        ConfigureTextures();
        ConfigureBuddyTextures();

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            throw new InvalidOperationException($"[LabMenuSceneBuilder] Font not found at {FontPath}.");
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        RectTransform content = canvas != null ? canvas.transform.Find("Content") as RectTransform : null;
        if (content == null)
        {
            throw new InvalidOperationException("[LabMenuSceneBuilder] Canvas/Content was not found in MainMenu.");
        }

        RemoveLegacyContentLayout(content);

        Transform existingBuddyPanel = content.Find("BuddyPanel");
        if (existingBuddyPanel != null)
        {
            UnityEngine.Object.DestroyImmediate(existingBuddyPanel.gameObject);
        }

        TopBarCurrencyController topBar = UnityEngine.Object.FindObjectOfType<TopBarCurrencyController>();
        TMP_Text energyText = null, chipText = null, redText = null;
        if (topBar != null)
        {
            SerializedObject tbSO = new SerializedObject(topBar);
            energyText = tbSO.FindProperty("energyText")?.objectReferenceValue as TMP_Text;
            chipText = tbSO.FindProperty("dataChipText")?.objectReferenceValue as TMP_Text;
            redText = tbSO.FindProperty("redGemText")?.objectReferenceValue as TMP_Text;
        }

        GameObject buddyPanel = CreateBuddyPanel(content, canvas.GetComponent<RectTransform>(), energyText, chipText, redText);
        buddyPanel.name = "BuddyPanel";
        buddyPanel.SetActive(true);

        BottomNavigationController bottomNav = UnityEngine.Object.FindObjectOfType<BottomNavigationController>();
        if (bottomNav != null)
        {
            SerializedObject navSO = new SerializedObject(bottomNav);
            SerializedProperty items = GetRequiredProperty(navSO, "items");
            if (items.arraySize > 4)
            {
                GetRequiredRelativeProperty(items.GetArrayElementAtIndex(4), "panel").objectReferenceValue = buddyPanel;
                navSO.ApplyModifiedPropertiesWithoutUndo();
            }
            bottomNav.Select(4);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[LabMenuSceneBuilder] Functional BuddyPanel rebuilt in MainMenu.");
    }

    [MenuItem("PGE/UI/Rebuild Chipset Panel")]
    public static void RebuildChipsetPanel()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[LabMenuSceneBuilder] Stop Play Mode before rebuilding the Chipset panel.");
            return;
        }

        ConfigureTextures();
        ConfigureStatTexture();
        ConfigureChipsetTextures();
        ConfigureBuddyTextures();

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            throw new InvalidOperationException($"[LabMenuSceneBuilder] Font not found at {FontPath}.");
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        RectTransform content = canvas != null ? canvas.transform.Find("Content") as RectTransform : null;
        if (content == null)
        {
            throw new InvalidOperationException("[LabMenuSceneBuilder] Canvas/Content was not found in MainMenu.");
        }

        RemoveLegacyContentLayout(content);

        Transform existingChipsetPanel = content.Find("ChipsetPanel");
        if (existingChipsetPanel != null)
        {
            UnityEngine.Object.DestroyImmediate(existingChipsetPanel.gameObject);
        }

        TopBarCurrencyController topBar = UnityEngine.Object.FindObjectOfType<TopBarCurrencyController>();
        TMP_Text energyText = null, chipText = null, redText = null;
        if (topBar != null)
        {
            SerializedObject tbSO = new SerializedObject(topBar);
            energyText = tbSO.FindProperty("energyText")?.objectReferenceValue as TMP_Text;
            chipText = tbSO.FindProperty("dataChipText")?.objectReferenceValue as TMP_Text;
            redText = tbSO.FindProperty("redGemText")?.objectReferenceValue as TMP_Text;
        }

        GameObject chipsetPanel = CreateChipsetPanel(content, canvas.GetComponent<RectTransform>(), energyText, chipText, redText);
        chipsetPanel.name = "ChipsetPanel";
        chipsetPanel.SetActive(true);

        BottomNavigationController bottomNav = UnityEngine.Object.FindObjectOfType<BottomNavigationController>();
        if (bottomNav != null)
        {
            SerializedObject navSO = new SerializedObject(bottomNav);
            SerializedProperty items = GetRequiredProperty(navSO, "items");
            if (items.arraySize > 3)
            {
                GetRequiredRelativeProperty(items.GetArrayElementAtIndex(3), "panel").objectReferenceValue = chipsetPanel;
                navSO.ApplyModifiedPropertiesWithoutUndo();
            }
            bottomNav.Select(3);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[LabMenuSceneBuilder] Functional ChipsetPanel rebuilt in MainMenu.");
    }

    [MenuItem("PGE/UI/Apply Green Chipset Card Frames")]
    public static void ApplyGreenChipsetCardFrames()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[LabMenuSceneBuilder] Stop Play Mode before applying green Chipset frames.");
            return;
        }

        CacheChipsetSprites();
        Sprite greenFrame = LoadChipsetSprite("Green") ?? LoadChipsetSprite("card-frame-tier1-green") ?? LoadChipsetSprite("card-frame-common");
        if (greenFrame == null)
        {
            throw new InvalidOperationException("[LabMenuSceneBuilder] Green Chipset frame sprite was not found.");
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        RectTransform chipsetPanel = canvas != null ? canvas.transform.Find("Content/ChipsetPanel") as RectTransform : null;
        if (chipsetPanel == null)
        {
            throw new InvalidOperationException("[LabMenuSceneBuilder] Canvas/Content/ChipsetPanel was not found in MainMenu.");
        }

        ChipsetCardUI[] cards = chipsetPanel.GetComponentsInChildren<ChipsetCardUI>(true);
        foreach (ChipsetCardUI card in cards)
        {
            SerializedObject cardObject = new SerializedObject(card);
            UnityEngine.UI.Image frameImage = card.GetComponent<UnityEngine.UI.Image>();
            if (frameImage != null)
            {
                frameImage.sprite = greenFrame;
                EditorUtility.SetDirty(frameImage);
            }

            UnityEngine.UI.Image iconImage = cardObject.FindProperty("iconImage")?.objectReferenceValue as UnityEngine.UI.Image;
            RectTransform iconRect = iconImage != null
                ? iconImage.rectTransform
                : card.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(rect => rect.name == "Icon");
            if (iconRect != null)
            {
                iconRect.anchoredPosition = new Vector2(iconRect.anchoredPosition.x, 13f);
                EditorUtility.SetDirty(iconRect);
            }

            TMP_Text levelText = cardObject.FindProperty("levelText")?.objectReferenceValue as TMP_Text;
            RectTransform levelRect = levelText != null
                ? levelText.rectTransform
                : card.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(rect => rect.name == "LevelText");
            if (levelRect != null)
            {
                levelRect.anchoredPosition = new Vector2(levelRect.anchoredPosition.x, -8f);
                EditorUtility.SetDirty(levelRect);
            }
        }

        ChipsetController controller = chipsetPanel.GetComponent<ChipsetController>();
        if (controller != null)
        {
            SerializedObject controllerObject = new SerializedObject(controller);
            SerializedProperty frames = controllerObject.FindProperty("frameSprites");
            if (frames != null)
            {
                frames.arraySize = 5;
                for (int i = 0; i < frames.arraySize; i++)
                {
                    frames.GetArrayElementAtIndex(i).objectReferenceValue = greenFrame;
                }
                controllerObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[LabMenuSceneBuilder] Applied green frames and card element positions to {cards.Length} Chipset cards.");
    }

    [MenuItem("PGE/UI/Apply Selected BottomBar Layout To All Chipset Cards")]
    public static void ApplySelectedBottomBarLayoutToAllChipsetCards()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[LabMenuSceneBuilder] Stop Play Mode before copying the selected BottomBar layout.");
            return;
        }

        RectTransform source = Selection.activeTransform as RectTransform;
        if (source == null || !string.Equals(source.name, "BottomBar", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("[LabMenuSceneBuilder] Select the source Chipset BottomBar in the Hierarchy first.");
        }

        Scene scene = SceneManager.GetActiveScene();
        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        RectTransform chipsetPanel = canvas != null ? canvas.transform.Find("Content/ChipsetPanel") as RectTransform : null;
        if (chipsetPanel == null)
        {
            throw new InvalidOperationException("[LabMenuSceneBuilder] Canvas/Content/ChipsetPanel was not found in the active scene.");
        }

        ChipsetCardUI[] cards = chipsetPanel.GetComponentsInChildren<ChipsetCardUI>(true);
        int updatedCount = 0;
        foreach (ChipsetCardUI card in cards)
        {
            RectTransform target = card.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(rect => rect.name == "BottomBar");
            if (target == null) continue;

            Undo.RecordObject(target, "Copy Chipset BottomBar Layout");
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.offsetMin = source.offsetMin;
            target.offsetMax = source.offsetMax;
            Vector3 anchoredPosition3D = target.anchoredPosition3D;
            anchoredPosition3D.z = source.anchoredPosition3D.z;
            target.anchoredPosition3D = anchoredPosition3D;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
            EditorUtility.SetDirty(target);
            updatedCount++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[LabMenuSceneBuilder] Copied selected BottomBar RectTransform to {updatedCount} Chipset cards.");
    }

    [MenuItem("PGE/UI/Apply UpgradeArrow Layout To All Chipset Cards")]
    public static void ApplyUpgradeArrowLayoutToAllChipsetCards()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[LabMenuSceneBuilder] Stop Play Mode before copying the UpgradeArrow layout.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        RectTransform chipsetPanel = canvas != null ? canvas.transform.Find("Content/ChipsetPanel") as RectTransform : null;
        if (chipsetPanel == null)
        {
            throw new InvalidOperationException("[LabMenuSceneBuilder] Canvas/Content/ChipsetPanel was not found in the scene.");
        }

        RectTransform source = Selection.activeTransform as RectTransform;
        bool hasSource = source != null && string.Equals(source.name, "UpgradeArrowGroup", StringComparison.Ordinal);

        Vector2 targetAnchor = hasSource ? source.anchorMin : new Vector2(0.88f, 0.17f);
        Vector2 targetPivot = hasSource ? source.pivot : new Vector2(0.5f, 0.5f);
        Vector2 targetPos = hasSource ? source.anchoredPosition : new Vector2(0f, 23.8f);
        Vector2 targetSize = hasSource ? source.sizeDelta : new Vector2(44f, 44f);
        Vector3 targetScale = hasSource ? source.localScale : Vector3.one;
        Quaternion targetRot = hasSource ? source.localRotation : Quaternion.identity;

        ChipsetCardUI[] cards = chipsetPanel.GetComponentsInChildren<ChipsetCardUI>(true);
        int updatedCount = 0;
        foreach (ChipsetCardUI card in cards)
        {
            RectTransform target = card.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(rect => rect.name == "UpgradeArrowGroup");
            if (target == null) continue;

            Undo.RecordObject(target, "Apply UpgradeArrow Layout");
            target.anchorMin = targetAnchor;
            target.anchorMax = targetAnchor;
            target.pivot = targetPivot;
            target.anchoredPosition = targetPos;
            target.sizeDelta = targetSize;
            target.localRotation = targetRot;
            target.localScale = targetScale;
            EditorUtility.SetDirty(target);
            updatedCount++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[LabMenuSceneBuilder] Applied UpgradeArrowGroup layout (Pos: {targetPos}, Size: {targetSize}) to {updatedCount} Chipset cards.");
    }

    [MenuItem("PGE/UI/Apply Fill Bar To All Chipset Cards")]
    public static void ApplyFillBarToAllChipsetCards()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[LabMenuSceneBuilder] Stop Play Mode before applying fill bars.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        RectTransform chipsetPanel = canvas != null ? canvas.transform.Find("Content/ChipsetPanel") as RectTransform : null;
        if (chipsetPanel == null)
        {
            throw new InvalidOperationException("[LabMenuSceneBuilder] Canvas/Content/ChipsetPanel was not found in the scene.");
        }

        ChipsetCardUI[] cards = chipsetPanel.GetComponentsInChildren<ChipsetCardUI>(true);
        int updatedCount = 0;
        foreach (ChipsetCardUI card in cards)
        {
            card.EnsureProgressBar();
            TMP_Text progressText = card.transform.Find("NormalContentGroup/BottomBar/ProgressText")?.GetComponent<TMP_Text>();
            if (progressText != null)
            {
                float ratio = ChipsetCardUI.ParseFillRatioFromProgressText(progressText.text);
                card.UpdateProgressBar(ratio);
            }
            EditorUtility.SetDirty(card.gameObject);
            updatedCount++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[LabMenuSceneBuilder] Applied progress fill bar to {updatedCount} Chipset cards in MainMenu.");
    }

    [MenuItem("PGE/UI/Rebuild Shop Panel")]
    public static void RebuildShopPanel()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[LabMenuSceneBuilder] Stop Play Mode before rebuilding the Shop panel.");
            return;
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            throw new InvalidOperationException($"[LabMenuSceneBuilder] Font not found at {FontPath}.");
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        RectTransform content = canvas != null ? canvas.transform.Find("Content") as RectTransform : null;
        if (content == null)
        {
            throw new InvalidOperationException("[LabMenuSceneBuilder] Canvas/Content was not found in MainMenu.");
        }

        RemoveLegacyContentLayout(content);

        Transform existingShopPanel = content.Find("ShopPanel");
        GameObject shopPanel;
        if (existingShopPanel != null &&
            existingShopPanel.GetComponent<ScrollRect>() != null &&
            existingShopPanel.GetComponent<ShopController>() != null)
        {
            shopPanel = existingShopPanel.gameObject;
        }
        else
        {
            if (existingShopPanel != null)
            {
                UnityEngine.Object.DestroyImmediate(existingShopPanel.gameObject);
            }

            shopPanel = CreateShopPanel(content, null, null, null);
            shopPanel.name = "ShopPanel";
            shopPanel.SetActive(false);
        }

        BottomNavigationController bottomNav = UnityEngine.Object.FindObjectOfType<BottomNavigationController>();
        if (bottomNav == null)
        {
            throw new InvalidOperationException("[LabMenuSceneBuilder] BottomNavigationController was not found in MainMenu.");
        }

        SerializedObject navSO = new SerializedObject(bottomNav);
        SerializedProperty items = GetRequiredProperty(navSO, "items");
        if (items.arraySize == 0)
        {
            throw new InvalidOperationException("[LabMenuSceneBuilder] BottomNavigationController has no navigation items.");
        }

        GetRequiredRelativeProperty(items.GetArrayElementAtIndex(0), "panel").objectReferenceValue = shopPanel;
        int defaultSelectedIndex = Mathf.Clamp(
            GetRequiredProperty(navSO, "defaultSelectedIndex").intValue,
            0,
            items.arraySize - 1);
        navSO.ApplyModifiedPropertiesWithoutUndo();

        // Persist one visible tab in Edit Mode as well as at runtime, where Start() calls Select().
        bottomNav.Select(defaultSelectedIndex);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[LabMenuSceneBuilder] Functional ShopPanel rebuilt in MainMenu.");
    }

    private static void TryBuildRequestedShopPanel()
    {
        if (!File.Exists(ShopBuildRequestPath) ||
            EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling ||
            EditorApplication.isUpdating)
        {
            return;
        }

        RebuildShopPanel();
        AssetDatabase.DeleteAsset(ShopBuildRequestPath);
        AssetDatabase.Refresh();
    }

    private static void TryBuildRequestedChipsetPanel()
    {
        if (!File.Exists(ChipsetBuildRequestPath) ||
            EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling ||
            EditorApplication.isUpdating)
        {
            return;
        }

        RebuildChipsetPanel();
        try
        {
            File.Delete(ChipsetBuildRequestPath);
        }
        catch {}
        AssetDatabase.Refresh();
    }

    private static void TryApplyRequestedGreenChipsetFrames()
    {
        if (!File.Exists(ChipsetGreenFramesRequestPath) ||
            EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling ||
            EditorApplication.isUpdating)
        {
            return;
        }

        ApplyGreenChipsetCardFrames();
        try
        {
            File.Delete(ChipsetGreenFramesRequestPath);
        }
        catch {}
        AssetDatabase.Refresh();
    }

    private static void TryApplyRequestedSelectedBottomBarLayout()
    {
        if (!File.Exists(ChipsetBottomBarLayoutRequestPath) ||
            EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling ||
            EditorApplication.isUpdating)
        {
            return;
        }

        ApplySelectedBottomBarLayoutToAllChipsetCards();
        try
        {
            File.Delete(ChipsetBottomBarLayoutRequestPath);
        }
        catch {}
        AssetDatabase.Refresh();
    }

    private static void TryApplyRequestedUpgradeArrowLayout()
    {
        if (!File.Exists(ChipsetUpgradeArrowLayoutRequestPath) ||
            EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling ||
            EditorApplication.isUpdating)
        {
            return;
        }

        ApplyUpgradeArrowLayoutToAllChipsetCards();
        try
        {
            File.Delete(ChipsetUpgradeArrowLayoutRequestPath);
        }
        catch {}
        AssetDatabase.Refresh();
    }

    private static void TryBuildRequestedBuddyPanel()
    {
        if (!File.Exists(BuddyBuildRequestPath) ||
            EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling ||
            EditorApplication.isUpdating)
        {
            return;
        }

        RebuildBuddyPanel();
        try
        {
            File.Delete(BuddyBuildRequestPath);
        }
        catch {}
        AssetDatabase.Refresh();
    }

    private static void RemoveLegacyContentLayout(RectTransform content)
    {
        ContentSizeFitter contentSizeFitter = content.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
        {
            UnityEngine.Object.DestroyImmediate(contentSizeFitter);
        }

        GridLayoutGroup gridLayout = content.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            UnityEngine.Object.DestroyImmediate(gridLayout);
        }
    }

    private static void TryBuildRequestedScene()
    {
        if (!File.Exists(BuildRequestPath))
        {
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        try
        {
            File.Delete(BuildRequestPath);
        }
        catch {}

        BuildLabMenuScene();
    }

    [MenuItem("PGE/UI/Update Lab 16 Stats, Lock Icons & Tooltip")]
    public static void UpdateLabStatsMenu()
    {
        File.WriteAllText(LabStatsBuildRequestPath, "update");
        TryUpdateRequestedLabStats();
    }

    private static void TryUpdateRequestedLabStats()
    {
        if (!File.Exists(LabStatsBuildRequestPath) ||
            EditorApplication.isPlaying ||
            EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling ||
            EditorApplication.isUpdating)
        {
            return;
        }

        try
        {
            File.Delete(LabStatsBuildRequestPath);
        }
        catch {}

        LabSpriteSlicer.SliceLabTexture();

        Scene previousScene = SceneManager.GetActiveScene();
        bool mainMenuWasActive = string.Equals(previousScene.path, ScenePath, StringComparison.OrdinalIgnoreCase);
        Scene mainMenuScene = mainMenuWasActive
            ? previousScene
            : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        LabUpgradeController controller = mainMenuScene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<LabUpgradeController>(true))
            .FirstOrDefault();
        if (controller == null)
        {
            throw new InvalidOperationException("[LabMenuSceneBuilder] LabUpgradeController not found in MainMenu.");
        }

        SerializedObject serializedController = new SerializedObject(controller);
        SerializedProperty items = GetRequiredProperty(serializedController, "items");
        GetRequiredProperty(serializedController, "commonBackgroundColor").colorValue = LabRarityColors[0];
        GetRequiredProperty(serializedController, "eliteBackgroundColor").colorValue = LabRarityColors[1];
        GetRequiredProperty(serializedController, "epicBackgroundColor").colorValue = LabRarityColors[2];
        GetRequiredProperty(serializedController, "legendBackgroundColor").colorValue = LabRarityColors[3];
        if (items.arraySize != LabStatNames.Length)
        {
            throw new InvalidOperationException(
                $"[LabMenuSceneBuilder] Expected {LabStatNames.Length} Lab stat slots, found {items.arraySize}.");
        }

        Sprite lockIconSprite = LoadLockIconSprite();
        SerializedProperty lockIconProp = serializedController.FindProperty("lockIconSprite");
        if (lockIconProp != null)
        {
            lockIconProp.objectReferenceValue = lockIconSprite;
        }

        Transform statsPanelTransform = controller.transform.Find("StatsPanel") ?? controller.transform;
        Transform existingTooltip = statsPanelTransform.Find("StatDetailTooltip");
        LabStatTooltip statTooltip = existingTooltip != null ? existingTooltip.GetComponent<LabStatTooltip>() : null;
        if (statTooltip == null)
        {
            statTooltip = CreateStatDetailTooltip(statsPanelTransform as RectTransform);
        }
        SerializedProperty statTooltipProp = serializedController.FindProperty("statTooltip");
        if (statTooltipProp != null)
        {
            statTooltipProp.objectReferenceValue = statTooltip;
        }

        for (int i = 0; i < items.arraySize; i++)
        {
            SerializedProperty item = items.GetArrayElementAtIndex(i);
            Sprite statSprite = LoadStatSprite(LabStatSpriteNames[i]);
            GetRequiredRelativeProperty(item, "itemName").stringValue = LabStatNames[i];
            GetRequiredRelativeProperty(item, "itemIcon").objectReferenceValue = statSprite;
            GetRequiredRelativeProperty(item, "rarity").enumValueIndex = i / 4;

            Image slotBackground = GetRequiredRelativeProperty(item, "slotBackground").objectReferenceValue as Image;
            if (slotBackground != null)
            {
                slotBackground.color = LabRarityColors[i / 4];
                slotBackground.raycastTarget = true;
                GameObject slotObj = slotBackground.transform.parent.gameObject;
                Button slotBtn = slotObj.GetComponent<Button>() ?? slotObj.AddComponent<Button>();
                slotBtn.targetGraphic = slotBackground;
                SerializedProperty slotButtonProp = item.FindPropertyRelative("slotButton");
                if (slotButtonProp != null)
                {
                    slotButtonProp.objectReferenceValue = slotBtn;
                }
                EditorUtility.SetDirty(slotBtn);
                EditorUtility.SetDirty(slotBackground);
            }

            GameObject lockedGroup = GetRequiredRelativeProperty(item, "lockedGroup").objectReferenceValue as GameObject;
            Image lockedCard = lockedGroup != null ? lockedGroup.transform.Find("LockIcon")?.GetComponent<Image>() : null;
            if (lockedCard != null)
            {
                lockedCard.sprite = lockIconSprite;
                lockedCard.color = Color.white;
                lockedCard.preserveAspect = true;
                lockedCard.rectTransform.anchorMin = new Vector2(0.5f, 0.52f);
                lockedCard.rectTransform.anchorMax = new Vector2(0.5f, 0.52f);
                lockedCard.rectTransform.anchoredPosition = Vector2.zero;
                lockedCard.rectTransform.sizeDelta = new Vector2(100f, 120f);
                EditorUtility.SetDirty(lockedCard);
            }

            Image iconImage = GetRequiredRelativeProperty(item, "iconImage").objectReferenceValue as Image;
            if (iconImage != null)
            {
                iconImage.sprite = statSprite;
                iconImage.color = Color.white;
                iconImage.preserveAspect = true;
                iconImage.rectTransform.anchorMin = new Vector2(0.5f, 0.52f);
                iconImage.rectTransform.anchorMax = new Vector2(0.5f, 0.52f);
                iconImage.rectTransform.anchoredPosition = Vector2.zero;
                iconImage.rectTransform.sizeDelta = new Vector2(140f, 140f);
                EditorUtility.SetDirty(iconImage);
            }

            TMP_Text nameText = GetRequiredRelativeProperty(item, "nameText").objectReferenceValue as TMP_Text;
            if (nameText != null)
            {
                nameText.text = LabStatNames[i];
                nameText.gameObject.SetActive(false);
                EditorUtility.SetDirty(nameText);
            }
        }

        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.SaveScene(mainMenuScene);

        if (!mainMenuWasActive)
        {
            SceneManager.SetActiveScene(previousScene);
            EditorSceneManager.CloseScene(mainMenuScene, true);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[LabMenuSceneBuilder] Updated 16 Lab stats, lock icons & tooltip in MainMenu.");
    }

    private static void BuildLabMenuScene()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += BuildLabMenuScene;
            return;
        }

        ConfigureTextures();
        ConfigureStatTexture();
        ConfigureChipsetTextures();
        ConfigureBuddyTextures();

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        Scene previousScene = SceneManager.GetActiveScene();
        string previousPath = previousScene.path;
        bool replaceActiveMainMenu = string.Equals(previousPath, ScenePath, StringComparison.OrdinalIgnoreCase);
        bool replaceUntitledScene = string.IsNullOrEmpty(previousPath);

        if (replaceActiveMainMenu && !File.Exists(BackupScenePath))
        {
            EditorSceneManager.SaveScene(previousScene, BackupScenePath, true);
        }

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            replaceActiveMainMenu || replaceUntitledScene ? NewSceneMode.Single : NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        Camera camera = CreateCamera();
        CreateEventSystem();
        Canvas canvas = CreateInterface();
        ValidateShopHierarchy();

        EditorSceneManager.SaveScene(scene, ScenePath);
        UpdateBuildSettings();
        CapturePreview(canvas, camera);
        ClearGeneratedFallbackGlyph();
        EditorSceneManager.SaveScene(scene, ScenePath);

        if (!replaceActiveMainMenu && previousScene.IsValid() && previousScene.isLoaded)
        {
            SceneManager.SetActiveScene(previousScene);
            EditorSceneManager.CloseScene(scene, true);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"PGE Main Menu UI created successfully: {ScenePath}. Preview: {ChipsetPreviewPath}");
    }

    private static void ClearGeneratedFallbackGlyph()
    {
        const uint VietnameseDongUnicode = 273;
        TMP_FontAsset fallbackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset");

        if (fallbackFont == null ||
            fallbackFont.characterTable.Count != 1 ||
            fallbackFont.characterTable[0].unicode != VietnameseDongUnicode)
        {
            return;
        }

        fallbackFont.ClearFontAssetData(true);
        EditorUtility.SetDirty(fallbackFont);
        AssetDatabase.SaveAssetIfDirty(fallbackFont);
    }

    private static void ValidateShopHierarchy()
    {
        GameObject panel = GameObject.Find("Canvas/Content/ShopPanel (Scrollable)");
        if (panel == null || panel.GetComponent<ScrollRect>() == null || panel.GetComponent<ShopController>() == null)
        {
            throw new InvalidOperationException("The functional ShopPanel was not created correctly.");
        }

        string[] requiredChildren =
        {
            "Viewport/ShopContent/DailyShopSection/Title",
            "Viewport/ShopContent/DailyShopSection/FreeGemItem",
            "Viewport/ShopContent/DailyShopSection/DroneBoxItem_1",
            "Viewport/ShopContent/DailyShopSection/DroneBoxItem_2",
            "Viewport/ShopContent/BoxSection/Title",
            "Viewport/ShopContent/BoxSection/ChipsetBoxGrid",
            "Viewport/ShopContent/BoxSection/DroneBoxGrid",
            "Viewport/ShopContent/GemSection/Title"
        };

        for (int i = 0; i < requiredChildren.Length; i++)
        {
            if (panel.transform.Find(requiredChildren[i]) == null)
            {
                throw new InvalidOperationException($"Missing Shop hierarchy object: {requiredChildren[i]}");
            }
        }
    }

    private static void ConfigureTextures()
    {
        AssetDatabase.ImportAsset(BackgroundPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter backgroundImporter = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
        if (backgroundImporter != null)
        {
            backgroundImporter.textureType = TextureImporterType.Sprite;
            backgroundImporter.spriteImportMode = SpriteImportMode.Single;
            backgroundImporter.spritePixelsPerUnit = 100f;
            backgroundImporter.mipmapEnabled = false;
            backgroundImporter.alphaIsTransparency = true;
            backgroundImporter.wrapMode = TextureWrapMode.Clamp;
            backgroundImporter.filterMode = FilterMode.Bilinear;
            backgroundImporter.maxTextureSize = 2048;
            backgroundImporter.SaveAndReimport();
        }

        AssetDatabase.ImportAsset(IconAtlasPath, ImportAssetOptions.ForceSynchronousImport);
        Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(IconAtlasPath);
        TextureImporter atlasImporter = AssetImporter.GetAtPath(IconAtlasPath) as TextureImporter;
        if (atlasImporter == null || atlas == null)
        {
            return;
        }

        string[,] iconNames =
        {
            { "energy", "chip-currency", "red-currency", "mail", "settings" },
            { "shop", "lab", "chapter", "chipset", "buddy" },
            { "lock", "armor", "plus", "leaf", "shield" }
        };

        float cellWidth = atlas.width / 5f;
        float cellHeight = atlas.height / 3f;
        SpriteRect[] sprites = new SpriteRect[15];
        int spriteIndex = 0;

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(atlasImporter);
        dataProvider.InitSpriteEditorDataProvider();
        SpriteRect[] existingRects = dataProvider.GetSpriteRects();
        Dictionary<string, GUID> existingGuids = existingRects != null
            ? existingRects.Where(r => !string.IsNullOrEmpty(r.name)).ToDictionary(r => r.name, r => r.spriteID)
            : new Dictionary<string, GUID>();

        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 5; column++)
            {
                string sName = iconNames[row, column];
                GUID spGuid = existingGuids.TryGetValue(sName, out GUID existingId) ? existingId : GUID.Generate();
                sprites[spriteIndex++] = new SpriteRect
                {
                    name = sName,
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = spGuid,
                    rect = new Rect(
                        column * cellWidth,
                        (2 - row) * cellHeight,
                        cellWidth,
                        cellHeight)
                };
            }
        }

        atlasImporter.textureType = TextureImporterType.Sprite;
        atlasImporter.spriteImportMode = SpriteImportMode.Multiple;
        atlasImporter.spritePixelsPerUnit = 100f;
        atlasImporter.mipmapEnabled = false;
        atlasImporter.alphaIsTransparency = true;
        atlasImporter.wrapMode = TextureWrapMode.Clamp;
        atlasImporter.filterMode = FilterMode.Point;
        atlasImporter.maxTextureSize = 2048;
        atlasImporter.SaveAndReimport();

        dataProvider = factories.GetSpriteEditorDataProviderFromObject(atlasImporter);
        dataProvider.InitSpriteEditorDataProvider();
        dataProvider.SetSpriteRects(sprites);
        dataProvider.Apply();
        atlasImporter.SaveAndReimport();
    }

    private static void ConfigureStatTexture()
    {
        AssetDatabase.ImportAsset(StatSpriteSheetPath, ImportAssetOptions.ForceSynchronousImport);
        Texture2D sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(StatSpriteSheetPath);
        TextureImporter importer = AssetImporter.GetAtPath(StatSpriteSheetPath) as TextureImporter;
        if (sheet == null || importer == null)
        {
            throw new InvalidOperationException($"[LabMenuSceneBuilder] Stat sheet not found: {StatSpriteSheetPath}");
        }

        const int columns = 4;
        const int rows = 4;
        float cellWidth = sheet.width / (float)columns;
        float cellHeight = sheet.height / (float)rows;

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();
        SpriteRect[] existingRects = dataProvider.GetSpriteRects();
        Dictionary<string, GUID> existingGuids = existingRects != null
            ? existingRects.Where(rect => !string.IsNullOrEmpty(rect.name))
                .ToDictionary(rect => rect.name, rect => rect.spriteID)
            : new Dictionary<string, GUID>();

        SpriteRect[] sprites = new SpriteRect[LabStatSpriteNames.Length];
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int index = row * columns + column;
                string spriteName = LabStatSpriteNames[index];
                GUID spriteGuid = existingGuids.TryGetValue(spriteName, out GUID existingGuid)
                    ? existingGuid
                    : GUID.Generate();

                sprites[index] = new SpriteRect
                {
                    name = spriteName,
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = spriteGuid,
                    rect = new Rect(
                        column * cellWidth,
                        (rows - 1 - row) * cellHeight,
                        cellWidth,
                        cellHeight)
                };
            }
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();

        dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();
        dataProvider.SetSpriteRects(sprites);
        dataProvider.Apply();
        importer.SaveAndReimport();
    }

    private static void ConfigureChipsetTextures()
    {
        AssetDatabase.ImportAsset(ChipsetAtlasPath, ImportAssetOptions.ForceSynchronousImport);
        Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(ChipsetAtlasPath);
        TextureImporter atlasImporter = AssetImporter.GetAtPath(ChipsetAtlasPath) as TextureImporter;
        if (atlasImporter == null || atlas == null)
        {
            return;
        }

        string[,] iconNames =
        {
            { "standard-gun", "rifle", "rocket-punch", "spinning-blade", "multigun", "gun-turret" },
            { "spiky-discus", "shotgun", "energy-jumper-cables", "high-explosive-mine", "aiming-lens", "plasma-field" },
            { "laser-eye", "biochemical-mine", "tesla-coil", "atk-module", "black-hole-mine", "sonic-boom" },
            { "big-battery", "turret-module", "ice-turret", "invincible-shield", "healing-turret", "flamethrower" },
            { "card-frame-common", "card-frame-rare", "card-frame-epic", "card-frame-holographic", "badge-upgrade", "icon-lock" },
            { "wave-circuit", "icon-star", "furnace-border", "power-battery", "advance-stone", "badge-advance" }
        };

        float cellWidth = 256f;
        float cellHeight = 256f;
        SpriteRect[] sprites = new SpriteRect[36];

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(atlasImporter);
        dataProvider.InitSpriteEditorDataProvider();
        SpriteRect[] existingRects = dataProvider.GetSpriteRects();
        Dictionary<string, GUID> existingGuids = existingRects != null
            ? existingRects.Where(r => !string.IsNullOrEmpty(r.name)).ToDictionary(r => r.name, r => r.spriteID)
            : new Dictionary<string, GUID>();

        for (int row = 0; row < 6; row++)
        {
            for (int column = 0; column < 6; column++)
            {
                int index = row * 6 + column;
                string sName = iconNames[row, column];
                GUID spGuid = existingGuids.TryGetValue(sName, out GUID existingId) ? existingId : GUID.Generate();
                sprites[index] = new SpriteRect
                {
                    name = sName,
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = spGuid,
                    rect = new Rect(
                        column * cellWidth,
                        (5 - row) * cellHeight,
                        cellWidth,
                        cellHeight)
                };
            }
        }

        atlasImporter.textureType = TextureImporterType.Sprite;
        atlasImporter.spriteImportMode = SpriteImportMode.Multiple;
        atlasImporter.spritePixelsPerUnit = 100f;
        atlasImporter.mipmapEnabled = false;
        atlasImporter.alphaIsTransparency = true;
        atlasImporter.wrapMode = TextureWrapMode.Clamp;
        atlasImporter.filterMode = FilterMode.Point;
        atlasImporter.maxTextureSize = 2048;
        atlasImporter.SaveAndReimport();

        dataProvider = factories.GetSpriteEditorDataProviderFromObject(atlasImporter);
        dataProvider.InitSpriteEditorDataProvider();
        dataProvider.SetSpriteRects(sprites);
        dataProvider.Apply();
        atlasImporter.SaveAndReimport();

        AssetDatabase.ImportAsset(ChipsetAtlasPath, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        CacheChipsetSprites();
    }

    private static Sprite[] cachedChipsetSprites;

    private static void CacheChipsetSprites()
    {
        List<Sprite> list = new List<Sprite>();

        // 1. Tải toàn bộ sub-sprites từ icon chipset và khung chipset mà user đã up
        string[] sheetPaths = {
            "Assets/Sprites/UI/Chipset/icon chipset.png",
            "Assets/Sprites/UI/Chipset/khung chipset.png",
            "Assets/UI/Chipset/Generated/icon chipset.png",
            ChipsetAtlasPath
        };
        foreach (string sheet in sheetPaths)
        {
            if (File.Exists(sheet))
            {
                Sprite[] subs = AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>().ToArray();
                foreach (Sprite s in subs)
                {
                    if (s != null && !list.Any(existing => existing.name == s.name)) list.Add(s);
                }
            }
        }

        // 2. Tải các khung bậc riêng lẻ từ Assets/Sprites/UI/Chipset/Frames/
        string framesDir = "Assets/Sprites/UI/Chipset/Frames";
        if (Directory.Exists(framesDir))
        {
            foreach (string file in Directory.GetFiles(framesDir, "*.png"))
            {
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(file.Replace("\\", "/"));
                if (s != null && !list.Contains(s)) list.Add(s);
            }
        }

        // 3. Tải các icon riêng lẻ từ Assets/Sprites/UI/Chipset/Icons/
        string iconsDir = "Assets/Sprites/UI/Chipset/Icons";
        if (Directory.Exists(iconsDir))
        {
            foreach (string file in Directory.GetFiles(iconsDir, "*.png"))
            {
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(file.Replace("\\", "/"));
                if (s != null && !list.Contains(s)) list.Add(s);
            }
        }

        cachedChipsetSprites = list.ToArray();
        Debug.Log($"[Chipset] Cached {cachedChipsetSprites.Length} user uploaded and sliced sprites.");
    }

    private static void ConfigureBuddyTextures()
    {
        AssetDatabase.ImportAsset(BuddyAtlasPath, ImportAssetOptions.ForceSynchronousImport);
        Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(BuddyAtlasPath);
        TextureImporter atlasImporter = AssetImporter.GetAtPath(BuddyAtlasPath) as TextureImporter;
        if (atlasImporter == null || atlas == null)
        {
            return;
        }

        string[,] iconNames =
        {
            { "drone-snowflake", "drone-spider", "drone-antenna-eye", "drone-cross-visor", "drone-capsule", "drone-spiky-mine" },
            { "drone-octagon-shield", "drone-claw-magnet", "drone-dual-rotor", "drone-stealth-wing", "drone-laser-sentry", "drone-plasma-orb" },
            { "buddy-frame-normal", "buddy-frame-rare", "buddy-frame-epic", "buddy-frame-holographic", "icon-lock-buddy", "badge-upgrade-green" },
            { "wave-pulse-cyan", "icon-drone-tab", "icon-lock-blue", "icon-lock-purple", "icon-lock-yellow", "icon-lock-pink" },
            { "icon-lock-unlocked", "btn-enhance-plate", "btn-advance-plate", "btn-equip-plate", "mini-chip-icon", "mini-red-gem" }
        };

        float cellWidth = 256f;
        float cellHeight = 256f;
        List<SpriteRect> sprites = new List<SpriteRect>();
        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(atlasImporter);
        dataProvider.InitSpriteEditorDataProvider();
        SpriteRect[] existingRects = dataProvider.GetSpriteRects();
        Dictionary<string, GUID> existingGuids = existingRects != null
            ? existingRects.Where(r => !string.IsNullOrEmpty(r.name)).ToDictionary(r => r.name, r => r.spriteID)
            : new Dictionary<string, GUID>();

        for (int row = 0; row < 5; row++)
        {
            for (int column = 0; column < 6; column++)
            {
                string sName = iconNames[row, column];
                if (string.IsNullOrEmpty(sName)) continue;
                GUID spGuid = existingGuids.TryGetValue(sName, out GUID existingId) ? existingId : GUID.Generate();
                sprites.Add(new SpriteRect
                {
                    name = sName,
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = spGuid,
                    rect = new Rect(
                        column * cellWidth,
                        (4 - row) * cellHeight,
                        cellWidth,
                        cellHeight)
                });
            }
        }

        atlasImporter.textureType = TextureImporterType.Sprite;
        atlasImporter.spriteImportMode = SpriteImportMode.Multiple;
        atlasImporter.spritePixelsPerUnit = 100f;
        atlasImporter.mipmapEnabled = false;
        atlasImporter.alphaIsTransparency = true;
        atlasImporter.wrapMode = TextureWrapMode.Clamp;
        atlasImporter.filterMode = FilterMode.Point;
        atlasImporter.maxTextureSize = 2048;
        atlasImporter.SaveAndReimport();

        dataProvider = factories.GetSpriteEditorDataProviderFromObject(atlasImporter);
        dataProvider.InitSpriteEditorDataProvider();
        dataProvider.SetSpriteRects(sprites.ToArray());
        dataProvider.Apply();
        atlasImporter.SaveAndReimport();

        AssetDatabase.ImportAsset(BuddyAtlasPath, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        CacheBuddySprites();
    }

    private static Sprite[] cachedBuddySprites;

    private static void CacheBuddySprites()
    {
        cachedBuddySprites = AssetDatabase.LoadAllAssetsAtPath(BuddyAtlasPath).OfType<Sprite>().ToArray();
        Debug.Log($"[Buddy] Cached {cachedBuddySprites.Length} sprites: {string.Join(", ", cachedBuddySprites.Select(s => s.name))}");
    }

    private static Sprite LoadBuddySprite(string spriteName)
    {
        if (cachedBuddySprites == null || cachedBuddySprites.Length == 0)
        {
            CacheBuddySprites();
        }
        return cachedBuddySprites?.FirstOrDefault(sprite => string.Equals(sprite.name, spriteName, StringComparison.OrdinalIgnoreCase));
    }

    private static Image CreateBuddyIcon(string name, Transform parent, string spriteName, float size)
    {
        Image image = CreateImage(name, parent, Color.white, false);
        image.sprite = LoadBuddySprite(spriteName);
        image.preserveAspect = true;
        image.rectTransform.sizeDelta = new Vector2(size, size);
        return image;
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Navy;
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        return camera;
    }

    private static void CreateEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static Canvas CreateInterface()
    {
        GameObject canvasObject = new GameObject(
            "Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.localScale = Vector3.one;

        Image background = CreateImage("Background", canvasRect, Color.white, false);
        Stretch(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        background.preserveAspect = false;

        RectTransform topBar = CreateRect("TopBar", canvasRect);
        Stretch(topBar, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -175f), Vector2.zero);
        Image topTint = topBar.gameObject.AddComponent<Image>();
        topTint.color = new Color32(7, 45, 65, 76);
        topTint.raycastTarget = false;

        TopBarCurrencyController topBarCtrl = CreateTopBar(
            topBar,
            out TMP_Text energyBalanceText,
            out TMP_Text chipBalanceText,
            out TMP_Text redChipBalanceText);

        RectTransform content = CreateRect("Content", canvasRect);
        Stretch(content, Vector2.zero, Vector2.one, new Vector2(0f, 220f), new Vector2(0f, -175f));

        GameObject[] panels = new GameObject[5];
        panels[0] = CreateShopPanel(content, energyBalanceText, chipBalanceText, redChipBalanceText);
        panels[1] = CreateLabPanel(content, energyBalanceText, chipBalanceText, redChipBalanceText);
        panels[2] = ChapterMenuSceneBuilder.BuildChapterPanel(content, font);
        panels[3] = CreateChipsetPanel(content, canvasRect, energyBalanceText, chipBalanceText, redChipBalanceText);
        panels[4] = CreateBuddyPanel(content, canvasRect, energyBalanceText, chipBalanceText, redChipBalanceText);

        // Default to Chapter Tab (index 2)
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].SetActive(i == 2);
        }

        BottomNavigationController bottomNav = CreateBottomNavigation(canvasObject, canvasRect, panels, 2);
        if (topBarCtrl != null && bottomNav != null)
        {
            SerializedObject topBarSO = new SerializedObject(topBarCtrl);
            topBarSO.FindProperty("bottomNavController").objectReferenceValue = bottomNav;
            topBarSO.ApplyModifiedPropertiesWithoutUndo();
        }

        return canvas;
    }

    private static TopBarCurrencyController CreateTopBar(
        RectTransform parent,
        out TMP_Text energyBalanceText,
        out TMP_Text chipBalanceText,
        out TMP_Text redChipBalanceText)
    {
        energyBalanceText = CreateResourceDisplay(parent, "Energy", 0.025f, 0.265f, "energy", "24/50", "05:46", Cream);
        chipBalanceText = CreateResourceDisplay(parent, "ChipCurrency", 0.285f, 0.525f, "chip-currency", "134,936", string.Empty, Cream);
        redChipBalanceText = CreateResourceDisplay(parent, "RedCurrency", 0.545f, 0.765f, "red-currency", "15,516", string.Empty, Cream);
        CreateTopIconButton(parent, "MailButton", 0.79f, 0.885f, "mail", true);
        CreateTopIconButton(parent, "SettingButton", 0.895f, 0.99f, "settings", false);

        TopBarCurrencyController topBarCtrl = parent.gameObject.AddComponent<TopBarCurrencyController>();
        SerializedObject topBarSO = new SerializedObject(topBarCtrl);
        topBarSO.FindProperty("energyText").objectReferenceValue = energyBalanceText;
        topBarSO.FindProperty("dataChipText").objectReferenceValue = chipBalanceText;
        topBarSO.FindProperty("redGemText").objectReferenceValue = redChipBalanceText;

        Button mailBtn = parent.Find("MailButton")?.GetComponent<Button>();
        Button settingBtn = parent.Find("SettingButton")?.GetComponent<Button>();
        topBarSO.FindProperty("questBookButton").objectReferenceValue = mailBtn;
        topBarSO.FindProperty("settingsButton").objectReferenceValue = settingBtn;
        topBarSO.ApplyModifiedPropertiesWithoutUndo();

        return topBarCtrl;
    }

    private static TMP_Text CreateResourceDisplay(
        RectTransform parent,
        string name,
        float minX,
        float maxX,
        string iconName,
        string value,
        string badge,
        Color valueColor)
    {
        RectTransform root = CreateRect(name, parent);
        Stretch(root, new Vector2(minX, 1f), new Vector2(maxX, 1f), new Vector2(5f, -150f), new Vector2(-5f, -20f));

        Image plate = CreateImage("Plate", root, new Color32(11, 55, 72, 215), false);
        Stretch(plate.rectTransform, new Vector2(0.2f, 0.22f), new Vector2(1f, 0.8f), Vector2.zero, Vector2.zero);
        AddOutline(plate, Border, 3f);

        Image icon = CreateIcon("Icon", root, iconName, 90f);
        Anchor(icon.rectTransform, new Vector2(0.19f, 0.55f), new Vector2(-4f, 0f), new Vector2(92f, 92f));

        if (!string.IsNullOrEmpty(badge))
        {
            Image badgeImg = CreateIcon("Badge", icon.rectTransform, "plus", 34f);
            Anchor(badgeImg.rectTransform, new Vector2(0.85f, 0.85f), Vector2.zero, new Vector2(34f, 34f));
        }

        TMP_Text valueText = CreateText("Value", root, value, 34f, valueColor, TextAlignmentOptions.Center);
        Stretch(valueText.rectTransform, new Vector2(0.28f, 0.24f), new Vector2(0.98f, 0.78f), Vector2.zero, Vector2.zero);

        return valueText;
    }

    private static void CreateTopIconButton(RectTransform parent, string name, float minX, float maxX, string iconName, bool hasBadge)
    {
        RectTransform root = CreateRect(name, parent);
        Stretch(root, new Vector2(minX, 1f), new Vector2(maxX, 1f), new Vector2(2f, -145f), new Vector2(-2f, -20f));
        Image icon = CreateIcon("Icon", root, iconName, 96f);
        Anchor(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 96f));
        icon.raycastTarget = true;
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = icon;

        if (hasBadge)
        {
            Image dot = CreateImage("NotifDot", icon.rectTransform, new Color32(235, 60, 40, 255), false);
            Anchor(dot.rectTransform, new Vector2(0.82f, 0.85f), Vector2.zero, new Vector2(24f, 24f));
            AddOutline(dot, Color.white, 2f);
        }
    }

    private static GameObject CreateChipsetPanel(
        RectTransform parent,
        RectTransform canvasRect,
        TMP_Text energyText,
        TMP_Text chipCurrencyText,
        TMP_Text redCurrencyText)
    {
        RectTransform panel = CreateRect("ChipsetPanel", parent);
        Stretch(panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // 1. Top Tabs (Chipset vs High-Tech Chipset)
        RectTransform topTabs = CreateRect("TopTabs", panel);
        topTabs.anchorMin = new Vector2(0f, 1f);
        topTabs.anchorMax = new Vector2(1f, 1f);
        topTabs.pivot = new Vector2(0.5f, 1f);
        topTabs.anchoredPosition = new Vector2(0f, -10f);
        topTabs.sizeDelta = new Vector2(0f, 105f);

        // Left tab: Chipset (Active)
        GameObject tabChipsetObj = CreateFrame("TabChipset", topTabs, BrightTeal, BrightCyan, out Image tabChipsetBg);
        RectTransform tabChipsetRect = tabChipsetObj.GetComponent<RectTransform>();
        Stretch(tabChipsetRect, new Vector2(0.03f, 0f), new Vector2(0.485f, 1f), Vector2.zero, Vector2.zero);
        TMP_Text tabChipsetText = CreateText("Label", tabChipsetRect, "Chipset", 44f, Color.white, TextAlignmentOptions.Center);
        Stretch(tabChipsetText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Button tabChipsetBtn = tabChipsetObj.AddComponent<Button>();
        tabChipsetBtn.targetGraphic = tabChipsetBg;

        // Right tab: High-Tech Chipset (Locked)
        GameObject tabHighTechObj = CreateFrame("TabHighTech", topTabs, new Color32(16, 52, 54, 235), new Color32(12, 38, 42, 255), out Image tabHighTechBg);
        RectTransform tabHighTechRect = tabHighTechObj.GetComponent<RectTransform>();
        Stretch(tabHighTechRect, new Vector2(0.515f, 0f), new Vector2(0.97f, 1f), Vector2.zero, Vector2.zero);
        TMP_Text tabHighTechText = CreateText("Label", tabHighTechRect, "High-Tech Chipset", 36f, new Color32(60, 110, 110, 255), TextAlignmentOptions.Center);
        Stretch(tabHighTechText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Button tabHighTechBtn = tabHighTechObj.AddComponent<Button>();
        tabHighTechBtn.targetGraphic = tabHighTechBg;

        // 2. Preset selector overlapping the equipped board, matching the portrait reference layout.
        RectTransform presetBar = CreateRect("PresetBar", panel);
        presetBar.anchorMin = new Vector2(0.5f, 1f);
        presetBar.anchorMax = new Vector2(0.5f, 1f);
        presetBar.pivot = new Vector2(0.5f, 0.5f);
        presetBar.anchoredPosition = new Vector2(0f, -205f);
        presetBar.sizeDelta = new Vector2(390f, 82f);

        Button p1Btn = CreatePresetButton(presetBar, "Preset1", 0.18f, "1", true, out Image p1Bg, out TMP_Text p1Text);
        Button p2Btn = CreatePresetButton(presetBar, "Preset2", 0.5f, "2", false, out Image p2Bg, out TMP_Text p2Text);
        Button p3Btn = CreatePresetButton(presetBar, "Preset3", 0.82f, "3", false, out Image p3Bg, out TMP_Text p3Text);

        // 3. Equipped Chipset Board
        GameObject boardObj = CreateFrame("EquippedBoard", panel, DarkPanel, TealBorder, out Image boardBg);
        RectTransform boardRect = boardObj.GetComponent<RectTransform>();
        boardRect.anchorMin = new Vector2(0.5f, 1f);
        boardRect.anchorMax = new Vector2(0.5f, 1f);
        boardRect.pivot = new Vector2(0.5f, 1f);
        boardRect.anchoredPosition = new Vector2(0f, -230f);
        boardRect.sizeDelta = new Vector2(980f, 600f);

        Sprite equippedFrame = AssetDatabase.LoadAssetAtPath<Sprite>(ChipsetEquippedFramePath);
        if (equippedFrame != null)
        {
            boardBg.sprite = equippedFrame;
            boardBg.type = Image.Type.Simple;
            boardBg.color = Color.white;
        }

        // Keep the furnace hierarchy/controller reference for compatibility, but hide it in this layout.
        RectTransform boardHeader = CreateRect("BoardHeader", boardRect);
        boardHeader.anchorMin = new Vector2(0f, 1f);
        boardHeader.anchorMax = new Vector2(1f, 1f);
        boardHeader.pivot = new Vector2(0.5f, 1f);
        boardHeader.anchoredPosition = new Vector2(0f, -12f);
        boardHeader.sizeDelta = new Vector2(0f, 65f);

        GameObject furnaceBtnObj = CreateFrame("BlastFurnaceBtn", boardHeader, FieryRed, FieryOrange, out Image furnaceBg);
        RectTransform furnaceRect = furnaceBtnObj.GetComponent<RectTransform>();
        Anchor(furnaceRect, new Vector2(0.85f, 0.5f), Vector2.zero, new Vector2(230f, 64f));
        TMP_Text furnaceLabel = CreateText("Label", furnaceRect, "Blast\nFurnace", 23f, Yellow, TextAlignmentOptions.Center);
        Stretch(furnaceLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        furnaceBg.raycastTarget = true;
        Button furnaceBtn = furnaceBtnObj.AddComponent<Button>();
        furnaceBtn.targetGraphic = furnaceBg;
        furnaceBtnObj.SetActive(false);
        boardHeader.gameObject.SetActive(false);

        // Equipped Grid (2 rows x 5 columns)
        RectTransform equippedGrid = CreateRect("EquippedGrid", boardRect);
        equippedGrid.anchorMin = new Vector2(0f, 0f);
        equippedGrid.anchorMax = new Vector2(1f, 1f);
        equippedGrid.offsetMin = new Vector2(45f, 35f);
        equippedGrid.offsetMax = new Vector2(-45f, -35f);

        GridLayoutGroup equippedLayout = equippedGrid.gameObject.AddComponent<GridLayoutGroup>();
        equippedLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        equippedLayout.constraintCount = 5;
        equippedLayout.cellSize = new Vector2(165f, 225f);
        equippedLayout.spacing = new Vector2(15f, 25f);
        equippedLayout.childAlignment = TextAnchor.UpperCenter;

        ChipsetCardUI[] equippedCardSlots = new ChipsetCardUI[10];
        for (int i = 0; i < 10; i++)
        {
            equippedCardSlots[i] = CreateChipCardUI(equippedGrid, $"EquippedSlot_{i:00}", new Vector2(165f, 225f));
        }

        // Lower Section Background Tint
        Image invBgTint = CreateImage("InventoryBgTint", panel, new Color32(18, 62, 74, 210), false);
        Stretch(invBgTint.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -930f));

        // 3. Sort Filter Bar
        RectTransform sortBar = CreateRect("SortFilterBar", panel);
        sortBar.anchorMin = new Vector2(0.5f, 1f);
        sortBar.anchorMax = new Vector2(0.5f, 1f);
        sortBar.pivot = new Vector2(0.5f, 1f);
        sortBar.anchoredPosition = new Vector2(0f, -850f);
        sortBar.sizeDelta = new Vector2(760f, 78f);

        GameObject byTierObj = CreateFrame("ByTierBtn", sortBar, Yellow, Border, out Image byTierBg);
        RectTransform byTierRect = byTierObj.GetComponent<RectTransform>();
        Anchor(byTierRect, new Vector2(0.31f, 0.5f), Vector2.zero, new Vector2(275f, 68f));
        TMP_Text byTierText = CreateText("Label", byTierRect, "By Tier", 34f, new Color32(10, 20, 30, 255), TextAlignmentOptions.Center);
        Stretch(byTierText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        byTierBg.raycastTarget = true;
        Button byTierBtn = byTierObj.AddComponent<Button>();
        byTierBtn.targetGraphic = byTierBg;

        GameObject byQtyObj = CreateFrame("ByQtyBtn", sortBar, new Color32(18, 58, 68, 255), BrightCyan, out Image byQtyBg);
        RectTransform byQtyRect = byQtyObj.GetComponent<RectTransform>();
        Anchor(byQtyRect, new Vector2(0.69f, 0.5f), Vector2.zero, new Vector2(290f, 68f));
        TMP_Text byQtyText = CreateText("Label", byQtyRect, "By Quantity", 34f, Color.white, TextAlignmentOptions.Center);
        Stretch(byQtyText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        byQtyBg.raycastTarget = true;
        Button byQtyBtn = byQtyObj.AddComponent<Button>();
        byQtyBtn.targetGraphic = byQtyBg;

        // 4. Inventory Scroll View
        RectTransform scrollRoot = CreateRect("InventoryScrollView", panel);
        Stretch(scrollRoot, Vector2.zero, Vector2.one, new Vector2(20f, 15f), new Vector2(-20f, -950f));

        ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 25f;

        RectTransform viewport = CreateRect("Viewport", scrollRoot);
        Stretch(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport;

        RectTransform invContent = CreateRect("Content", viewport);
        invContent.anchorMin = new Vector2(0f, 1f);
        invContent.anchorMax = new Vector2(1f, 1f);
        invContent.pivot = new Vector2(0.5f, 1f);
        invContent.anchoredPosition = Vector2.zero;
        invContent.sizeDelta = new Vector2(0f, 900f);
        scrollRect.content = invContent;

        GridLayoutGroup invLayout = invContent.gameObject.AddComponent<GridLayoutGroup>();
        invLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        invLayout.constraintCount = 4;
        invLayout.cellSize = new Vector2(190f, 240f);
        invLayout.spacing = new Vector2(35f, 35f);
        invLayout.padding = new RectOffset(70, 70, 20, 30);
        invLayout.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter fitter = invContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Pre-configure the 10 equipped cards to match Preset 3 screenshot
        string[] eqIcons = {
            "Standard gun", "Rifle", "Rocket Punch", "Spinning Blade", "Multigun",
            "Gun Turret", "Spiky Discus", "Shotgun", "Energy Jumper Cable", "High Explosive Mine"
        };
        string[] eqFrames = {
            "Green", "Green", "Green", "Green", "Green",
            "Green", "Green", "Green", "Green", "Green"
        };
        string[] eqLevels = {
            "LV.18", "LV.24", "LV.06", "LV.14", "LV.06",
            "LV.01", "LV.01", "LV.09", "LV.01", "LV.01"
        };
        string[] eqProgress = {
            "451/15", "449", "470/7", "468/9", "423/3",
            "501/3", "479/3", "450/7", "391/3", "390/3"
        };
        bool[] eqStars = { false, false, false, false, false, false, false, false, true, false };
        bool[] eqArrows = { true, false, true, true, true, true, true, true, true, true };

        for (int i = 0; i < 10; i++)
        {
            ConfigureCardStaticView(equippedCardSlots[i], eqIcons[i], eqFrames[i], eqLevels[i], eqProgress[i], eqStars[i], eqArrows[i]);
        }

        // Pre-populate Inventory cards matching Image 1:
        string[] invIcons = {
            "Rocket Punch", "sonic-boom", "healing-turret", "aiming-lens",
            "Gun Turret", "ice-turret", "Multigun", "flamethrower",
            "atk-module", "laser-eye", "black-hole-mine", "invincible-shield"
        };
        string[] invFrames = {
            "Green", "Green", "Green", "Green",
            "Green", "Green", "Green", "Green",
            "Green", "Green", "Green", "Green"
        };
        string[] invLevels = {
            "LV.01", "LV.01", "LV.01", "LV.01",
            "LV.01", "LV.01", "LV.01", "LV.18",
            "LV.01", "LV.01", "LV.01", "LV.01"
        };
        string[] invProgress = {
            "547/3", "513/3", "502/3", "498/3",
            "497/3", "494/3", "489/3", "486/15",
            "483/3", "473/3", "467/3", "458/3"
        };
        bool[] invStars = {
            false, false, false, true,
            false, false, false, false,
            false, false, true, false
        };
        bool[] invArrows = {
            true, true, true, true,
            true, true, true, true,
            true, true, true, true
        };

        for (int i = 0; i < invIcons.Length; i++)
        {
            ChipsetCardUI invCard = CreateChipCardUI(invContent, $"StaticInvCard_{i:00}", new Vector2(190f, 240f));
            ConfigureCardStaticView(invCard, invIcons[i], invFrames[i], invLevels[i], invProgress[i], invStars[i], invArrows[i]);
        }

        // Card Prefab template for dynamic instantiation at runtime
        GameObject cardPrefab = CreateChipCardUI(invContent, "CardTemplate", new Vector2(190f, 240f)).gameObject;
        cardPrefab.SetActive(false);

        // 5. Detail Modal
        GameObject detailModal = CreateDetailModal(canvasRect, out TMP_Text dModBadge, out ChipsetCardUI dTopCard,
            out TMP_Text dName, out TMP_Text dTier, out TMP_Text dDesc, out TMP_Text dBaseStats, out Image[] dPerkIcons,
            out TMP_Text[] dPerkTexts, out Button dEnhanceBtn, out TMP_Text dEnhanceCostText, out CanvasGroup dEnhanceBtnCg,
            out Button dAdvanceTierBtn, out TMP_Text dAdvanceTierText, out CanvasGroup dAdvanceTierBtnCg,
            out GameObject dFragNotice, out GameObject dChipNotice,
            out Button dEquipBtn, out TMP_Text dEquipBtnText, out Button dCloseBtn);
        detailModal.SetActive(false);

        // 6. Blast Furnace Modal
        GameObject furnaceModal = CreateFurnaceModal(canvasRect, out TMP_Text fDesc, out Button fDismantleBtn, out Button fCloseBtn);
        furnaceModal.SetActive(false);

        // 7. Toast Message
        GameObject toastRoot = CreateToastRoot(canvasRect, out TMP_Text toastText);
        toastRoot.SetActive(false);

        // Controller Setup
        ChipsetController controller = panel.gameObject.AddComponent<ChipsetController>();
        SerializedObject sController = new SerializedObject(controller);

        sController.FindProperty("energyText").objectReferenceValue = energyText;
        sController.FindProperty("chipCurrencyText").objectReferenceValue = chipCurrencyText;
        sController.FindProperty("redCurrencyText").objectReferenceValue = redCurrencyText;
        sController.FindProperty("advanceStonesText").objectReferenceValue = null;

        sController.FindProperty("chipsetModeBtn").objectReferenceValue = tabChipsetBtn;
        sController.FindProperty("highTechModeBtn").objectReferenceValue = tabHighTechBtn;
        sController.FindProperty("chipsetModeBg").objectReferenceValue = tabChipsetBg;
        sController.FindProperty("highTechModeBg").objectReferenceValue = tabHighTechBg;

        sController.FindProperty("preset1Btn").objectReferenceValue = p1Btn;
        sController.FindProperty("preset2Btn").objectReferenceValue = p2Btn;
        sController.FindProperty("preset3Btn").objectReferenceValue = p3Btn;
        sController.FindProperty("preset1Bg").objectReferenceValue = p1Bg;
        sController.FindProperty("preset2Bg").objectReferenceValue = p2Bg;
        sController.FindProperty("preset3Bg").objectReferenceValue = p3Bg;
        sController.FindProperty("preset1Text").objectReferenceValue = p1Text;
        sController.FindProperty("preset2Text").objectReferenceValue = p2Text;
        sController.FindProperty("preset3Text").objectReferenceValue = p3Text;
        sController.FindProperty("blastFurnaceBtn").objectReferenceValue = furnaceBtn;

        SerializedProperty sEquipped = sController.FindProperty("equippedSlots");
        sEquipped.arraySize = 10;
        for (int i = 0; i < 10; i++)
        {
            sEquipped.GetArrayElementAtIndex(i).objectReferenceValue = equippedCardSlots[i];
        }

        sController.FindProperty("byTierBtn").objectReferenceValue = byTierBtn;
        sController.FindProperty("byQuantityBtn").objectReferenceValue = byQtyBtn;
        sController.FindProperty("byTierBg").objectReferenceValue = byTierBg;
        sController.FindProperty("byQuantityBg").objectReferenceValue = byQtyBg;
        sController.FindProperty("byTierText").objectReferenceValue = byTierText;
        sController.FindProperty("byQuantityText").objectReferenceValue = byQtyText;

        sController.FindProperty("inventoryContent").objectReferenceValue = invContent;
        sController.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;

        sController.FindProperty("detailModal").objectReferenceValue = detailModal;
        sController.FindProperty("detailModBadgeText").objectReferenceValue = dModBadge;
        sController.FindProperty("detailTopCard").objectReferenceValue = dTopCard;
        sController.FindProperty("detailNameText").objectReferenceValue = dName;
        sController.FindProperty("detailTierText").objectReferenceValue = dTier;
        sController.FindProperty("detailDescText").objectReferenceValue = dDesc;
        sController.FindProperty("detailBaseStatsText").objectReferenceValue = dBaseStats;

        SerializedProperty sPerkIcons = sController.FindProperty("perkRowIcons");
        sPerkIcons.arraySize = 4;
        for (int i = 0; i < 4; i++) sPerkIcons.GetArrayElementAtIndex(i).objectReferenceValue = dPerkIcons[i];

        SerializedProperty sPerkTexts = sController.FindProperty("perkRowTexts");
        sPerkTexts.arraySize = 4;
        for (int i = 0; i < 4; i++) sPerkTexts.GetArrayElementAtIndex(i).objectReferenceValue = dPerkTexts[i];

        sController.FindProperty("detailEnhanceBtn").objectReferenceValue = dEnhanceBtn;
        sController.FindProperty("detailEnhanceCostText").objectReferenceValue = dEnhanceCostText;
        sController.FindProperty("enhanceBtnCanvasGroup").objectReferenceValue = dEnhanceBtnCg;
        sController.FindProperty("detailAdvanceTierBtn").objectReferenceValue = dAdvanceTierBtn;
        sController.FindProperty("detailAdvanceTierText").objectReferenceValue = dAdvanceTierText;
        sController.FindProperty("advanceTierBtnCanvasGroup").objectReferenceValue = dAdvanceTierBtnCg;
        sController.FindProperty("detailEquipBtn").objectReferenceValue = dEquipBtn;
        sController.FindProperty("detailEquipBtnText").objectReferenceValue = dEquipBtnText;
        sController.FindProperty("detailCloseBtn").objectReferenceValue = dCloseBtn;

        sController.FindProperty("notEnoughFragmentsNotice").objectReferenceValue = dFragNotice;
        sController.FindProperty("notEnoughChipsNotice").objectReferenceValue = dChipNotice;

        sController.FindProperty("furnaceModal").objectReferenceValue = furnaceModal;
        sController.FindProperty("furnaceDescText").objectReferenceValue = fDesc;
        sController.FindProperty("furnaceDismantleBtn").objectReferenceValue = fDismantleBtn;
        sController.FindProperty("furnaceCloseBtn").objectReferenceValue = fCloseBtn;

        sController.FindProperty("toastRoot").objectReferenceValue = toastRoot;
        sController.FindProperty("toastText").objectReferenceValue = toastText;

        // Load Sprites into database
        if (cachedChipsetSprites == null || cachedChipsetSprites.Length == 0) CacheChipsetSprites();

        string[] iconKeys = {
            "standard-gun", "rifle", "rocket-punch", "spinning-blade", "multigun",
            "gun-turret", "spiky-discus", "shotgun", "energy-jumper-cables", "high-explosive-mine",
            "aiming-lens", "plasma-field", "laser-eye", "biochemical-mine", "tesla-coil",
            "atk-module", "black-hole-mine", "sonic-boom", "big-battery", "turret-module",
            "ice-turret", "invincible-shield", "healing-turret", "flamethrower"
        };
        Sprite[] chipIcons = iconKeys.Select(k => LoadChipsetSprite(k)).Where(s => s != null).ToArray();
        Sprite greenFrame = LoadChipsetSprite("Green") ?? LoadChipsetSprite("card-frame-tier1-green") ?? LoadChipsetSprite("card-frame-common");
        Sprite[] frameSprites = { greenFrame, greenFrame, greenFrame, greenFrame, greenFrame };

        SerializedProperty sIcons = sController.FindProperty("chipIcons");
        sIcons.arraySize = chipIcons.Length;
        for (int i = 0; i < chipIcons.Length; i++) sIcons.GetArrayElementAtIndex(i).objectReferenceValue = chipIcons[i];

        SerializedProperty sFrames = sController.FindProperty("frameSprites");
        sFrames.arraySize = frameSprites.Length;
        for (int i = 0; i < frameSprites.Length; i++) sFrames.GetArrayElementAtIndex(i).objectReferenceValue = frameSprites[i];

        sController.FindProperty("starSprite").objectReferenceValue = LoadChipsetSprite("icon-star");
        sController.FindProperty("upgradeArrowSprite").objectReferenceValue = LoadChipsetSprite("badge-upgrade");
        sController.FindProperty("advanceStoneSprite").objectReferenceValue = LoadChipsetSprite("advance-stone");

        SerializedProperty sTierLocks = sController.FindProperty("lockTierSprites");
        sTierLocks.arraySize = 4;
        sTierLocks.GetArrayElementAtIndex(0).objectReferenceValue = LoadChipsetSprite("Lock_Blue");
        sTierLocks.GetArrayElementAtIndex(1).objectReferenceValue = LoadChipsetSprite("Lock_Purple");
        sTierLocks.GetArrayElementAtIndex(2).objectReferenceValue = LoadChipsetSprite("Lock_Yellow");
        sTierLocks.GetArrayElementAtIndex(3).objectReferenceValue = LoadChipsetSprite("Lock_Red");

        SerializedProperty sUnlockedLocks = sController.FindProperty("unlockedTierSprites");
        sUnlockedLocks.arraySize = 4;
        sUnlockedLocks.GetArrayElementAtIndex(0).objectReferenceValue = LoadChipsetSprite("Lock_Blue_Open");
        sUnlockedLocks.GetArrayElementAtIndex(1).objectReferenceValue = LoadChipsetSprite("Lock_Purple_Open");
        sUnlockedLocks.GetArrayElementAtIndex(2).objectReferenceValue = LoadChipsetSprite("Lock_Yellow_Open");
        sUnlockedLocks.GetArrayElementAtIndex(3).objectReferenceValue = LoadChipsetSprite("Lock_Red_Open");

        Sprite[] allBuddySprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(BuddyAtlasPath).OfType<Sprite>().ToArray();
        sController.FindProperty("unlockedCheckSprite").objectReferenceValue = allBuddySprites.FirstOrDefault(s => s.name == "icon-lock-unlocked");

        sController.ApplyModifiedPropertiesWithoutUndo();

        return panel.gameObject;
    }

    private static GameObject CreateBuddyPanel(
        RectTransform parent,
        RectTransform canvasRect,
        TMP_Text energyText,
        TMP_Text chipCurrencyText,
        TMP_Text redCurrencyText)
    {
        RectTransform panel = CreateRect("BuddyPanel", parent);
        Stretch(panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // 1. Top Tabs (Drone vs Robot Pet)
        RectTransform topTabs = CreateRect("TopTabs", panel);
        topTabs.anchorMin = new Vector2(0f, 1f);
        topTabs.anchorMax = new Vector2(1f, 1f);
        topTabs.pivot = new Vector2(0.5f, 1f);
        topTabs.anchoredPosition = new Vector2(0f, -10f);
        topTabs.sizeDelta = new Vector2(0f, 105f);

        // Left tab: Drone (Active)
        GameObject tabDroneObj = CreateFrame("TabDrone", topTabs, BrightTeal, BrightCyan, out Image tabDroneBg);
        RectTransform tabDroneRect = tabDroneObj.GetComponent<RectTransform>();
        Stretch(tabDroneRect, new Vector2(0.03f, 0f), new Vector2(0.485f, 1f), Vector2.zero, Vector2.zero);
        TMP_Text tabDroneText = CreateText("Label", tabDroneRect, "Drone", 44f, Color.white, TextAlignmentOptions.Center);
        Anchor(tabDroneText.rectTransform, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(300f, 50f));
        Image waveImg = CreateBuddyIcon("Wave", tabDroneRect, "wave-pulse-cyan", 140f);
        Anchor(waveImg.rectTransform, new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(140f, 30f));
        Button tabDroneBtn = tabDroneObj.AddComponent<Button>();
        tabDroneBtn.targetGraphic = tabDroneBg;

        // Right tab: Robot Pet (Locked)
        GameObject tabRobotPetObj = CreateFrame("TabRobotPet", topTabs, new Color32(16, 52, 54, 235), new Color32(12, 38, 42, 255), out Image tabRobotPetBg);
        RectTransform tabRobotPetRect = tabRobotPetObj.GetComponent<RectTransform>();
        Stretch(tabRobotPetRect, new Vector2(0.515f, 0f), new Vector2(0.97f, 1f), Vector2.zero, Vector2.zero);
        TMP_Text tabRobotPetText = CreateText("Label", tabRobotPetRect, "Robot Pet", 36f, new Color32(40, 95, 95, 255), TextAlignmentOptions.Center);
        Anchor(tabRobotPetText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 50f));
        Image lockImg = CreateBuddyIcon("Lock", tabRobotPetRect, "icon-lock-buddy", 60f);
        Anchor(lockImg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
        Button tabRobotPetBtn = tabRobotPetObj.AddComponent<Button>();
        tabRobotPetBtn.targetGraphic = tabRobotPetBg;

        // 2. Equipped Buddy Board
        GameObject boardObj = CreateFrame("EquippedBoard", panel, DarkPanel, TealBorder, out Image boardBg);
        RectTransform boardRect = boardObj.GetComponent<RectTransform>();
        boardRect.anchorMin = new Vector2(0.5f, 1f);
        boardRect.anchorMax = new Vector2(0.5f, 1f);
        boardRect.pivot = new Vector2(0.5f, 1f);
        boardRect.anchoredPosition = new Vector2(0f, -125f);
        boardRect.sizeDelta = new Vector2(1020f, 530f);

        // Header inside Board: Presets 1, 2, 3
        RectTransform boardHeader = CreateRect("BoardHeader", boardRect);
        boardHeader.anchorMin = new Vector2(0f, 1f);
        boardHeader.anchorMax = new Vector2(1f, 1f);
        boardHeader.pivot = new Vector2(0.5f, 1f);
        boardHeader.anchoredPosition = new Vector2(0f, 10f);
        boardHeader.sizeDelta = new Vector2(0f, 75f);

        // Preset buttons
        Button p1Btn = CreatePresetButton(boardHeader, "Preset1", 0.40f, "1", true, out Image p1Bg, out TMP_Text p1Text);
        Button p2Btn = CreatePresetButton(boardHeader, "Preset2", 0.50f, "2", false, out Image p2Bg, out TMP_Text p2Text);
        Button p3Btn = CreatePresetButton(boardHeader, "Preset3", 0.60f, "3", false, out Image p3Bg, out TMP_Text p3Text);

        // Equipped Slots (1 row x 3 columns)
        RectTransform equippedRow = CreateRect("EquippedRow", boardRect);
        equippedRow.anchorMin = new Vector2(0f, 0f);
        equippedRow.anchorMax = new Vector2(1f, 1f);
        equippedRow.offsetMin = new Vector2(30f, 25f);
        equippedRow.offsetMax = new Vector2(-30f, -55f);

        HorizontalLayoutGroup eqLayout = equippedRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        eqLayout.childAlignment = TextAnchor.MiddleCenter;
        eqLayout.spacing = 35f;
        eqLayout.childForceExpandWidth = false;
        eqLayout.childForceExpandHeight = false;
        eqLayout.childControlWidth = false;
        eqLayout.childControlHeight = false;

        BuddyCardUI[] equippedCardSlots = new BuddyCardUI[3];
        equippedCardSlots[0] = CreateBuddyCardUI(equippedRow, "EquippedSlot_0", new Vector2(250f, 320f));
        equippedCardSlots[1] = CreateBuddyCardUI(equippedRow, "EquippedSlot_1", new Vector2(250f, 320f));
        equippedCardSlots[2] = CreateBuddyCardUI(equippedRow, "EquippedSlot_2", new Vector2(250f, 320f));

        // Lower Section Background Tint
        Image invBgTint = CreateImage("InventoryBgTint", panel, new Color32(18, 62, 74, 210), false);
        Stretch(invBgTint.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -670f));

        // 3. Sort Filter Bar
        RectTransform sortBar = CreateRect("SortFilterBar", panel);
        sortBar.anchorMin = new Vector2(0.5f, 1f);
        sortBar.anchorMax = new Vector2(0.5f, 1f);
        sortBar.pivot = new Vector2(0.5f, 1f);
        sortBar.anchoredPosition = new Vector2(0f, -690f);
        sortBar.sizeDelta = new Vector2(1020f, 70f);

        GameObject byTierObj = CreateFrame("ByTierBtn", sortBar, new Color32(18, 58, 68, 255), BrightCyan, out Image byTierBg);
        RectTransform byTierRect = byTierObj.GetComponent<RectTransform>();
        Anchor(byTierRect, new Vector2(0.36f, 0.5f), Vector2.zero, new Vector2(240f, 62f));
        TMP_Text byTierText = CreateText("Label", byTierRect, "By Tier", 34f, Color.white, TextAlignmentOptions.Center);
        Stretch(byTierText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        byTierBg.raycastTarget = true;
        Button byTierBtn = byTierObj.AddComponent<Button>();
        byTierBtn.targetGraphic = byTierBg;

        GameObject byQtyObj = CreateFrame("ByQtyBtn", sortBar, Yellow, Border, out Image byQtyBg);
        RectTransform byQtyRect = byQtyObj.GetComponent<RectTransform>();
        Anchor(byQtyRect, new Vector2(0.64f, 0.5f), Vector2.zero, new Vector2(250f, 62f));
        TMP_Text byQtyText = CreateText("Label", byQtyRect, "By Quantity", 34f, new Color32(10, 20, 30, 255), TextAlignmentOptions.Center);
        Stretch(byQtyText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        byQtyBg.raycastTarget = true;
        Button byQtyBtn = byQtyObj.AddComponent<Button>();
        byQtyBtn.targetGraphic = byQtyBg;

        // 4. Inventory Scroll View (3 columns)
        RectTransform scrollRoot = CreateRect("InventoryScrollView", panel);
        Stretch(scrollRoot, Vector2.zero, Vector2.one, new Vector2(20f, 15f), new Vector2(-20f, -770f));

        ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 25f;

        RectTransform viewport = CreateRect("Viewport", scrollRoot);
        Stretch(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport;

        RectTransform invContent = CreateRect("Content", viewport);
        invContent.anchorMin = new Vector2(0f, 1f);
        invContent.anchorMax = new Vector2(1f, 1f);
        invContent.pivot = new Vector2(0.5f, 1f);
        invContent.anchoredPosition = Vector2.zero;
        invContent.sizeDelta = new Vector2(0f, 900f);
        scrollRect.content = invContent;

        GridLayoutGroup invLayout = invContent.gameObject.AddComponent<GridLayoutGroup>();
        invLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        invLayout.constraintCount = 3;
        invLayout.cellSize = new Vector2(285f, 330f);
        invLayout.spacing = new Vector2(35f, 25f);
        invLayout.padding = new RectOffset(20, 20, 10, 30);
        invLayout.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter fitter = invContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Pre-configure the 3 equipped slots
        ConfigureBuddyCardStaticView(equippedCardSlots[0], "drone-snowflake", "buddy-frame-normal", "LV.01", "65/3", true);
        ConfigureBuddyCardEmptyStaticView(equippedCardSlots[1], "buddy-frame-normal");
        ConfigureBuddyCardLockedStaticView(equippedCardSlots[2], "buddy-frame-normal");

        // Template Prefab
        GameObject cardPrefab = CreateBuddyCardUI(invContent, "BuddyCardTemplate", new Vector2(285f, 330f)).gameObject;
        cardPrefab.SetActive(false);

        // 5. Detail Modal
        GameObject detailModal = CreateBuddyDetailModal(canvasRect, out BuddyCardUI dTopCard, out TMP_Text dName, out TMP_Text dTier,
            out TMP_Text dDesc, out TMP_Text dBaseStat, out Image[] dPerkIcons, out TMP_Text[] dPerkTexts,
            out Button dEnhanceBtn, out TMP_Text dEnhanceCostText, out Button dAdvanceTierBtn, out TMP_Text dAdvanceTierText,
            out Button dEquipBtn, out TMP_Text dEquipBtnText, out Button dCloseBtn);
        detailModal.SetActive(false);

        // 6. Toast Message
        GameObject toastRoot = CreateToastRoot(canvasRect, out TMP_Text toastText);
        toastRoot.SetActive(false);

        // Controller Setup
        BuddyController controller = panel.gameObject.AddComponent<BuddyController>();
        SerializedObject sController = new SerializedObject(controller);

        sController.FindProperty("energyText").objectReferenceValue = energyText;
        sController.FindProperty("chipCurrencyText").objectReferenceValue = chipCurrencyText;
        sController.FindProperty("redCurrencyText").objectReferenceValue = redCurrencyText;

        sController.FindProperty("droneModeBtn").objectReferenceValue = tabDroneBtn;
        sController.FindProperty("robotPetModeBtn").objectReferenceValue = tabRobotPetBtn;
        sController.FindProperty("droneModeBg").objectReferenceValue = tabDroneBg;
        sController.FindProperty("robotPetModeBg").objectReferenceValue = tabRobotPetBg;

        sController.FindProperty("preset1Btn").objectReferenceValue = p1Btn;
        sController.FindProperty("preset2Btn").objectReferenceValue = p2Btn;
        sController.FindProperty("preset3Btn").objectReferenceValue = p3Btn;
        sController.FindProperty("preset1Bg").objectReferenceValue = p1Bg;
        sController.FindProperty("preset2Bg").objectReferenceValue = p2Bg;
        sController.FindProperty("preset3Bg").objectReferenceValue = p3Bg;
        sController.FindProperty("preset1Text").objectReferenceValue = p1Text;
        sController.FindProperty("preset2Text").objectReferenceValue = p2Text;
        sController.FindProperty("preset3Text").objectReferenceValue = p3Text;

        SerializedProperty sEquipped = sController.FindProperty("equippedSlots");
        sEquipped.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            sEquipped.GetArrayElementAtIndex(i).objectReferenceValue = equippedCardSlots[i];
        }

        sController.FindProperty("byTierBtn").objectReferenceValue = byTierBtn;
        sController.FindProperty("byQuantityBtn").objectReferenceValue = byQtyBtn;
        sController.FindProperty("byTierBg").objectReferenceValue = byTierBg;
        sController.FindProperty("byQuantityBg").objectReferenceValue = byQtyBg;
        sController.FindProperty("byTierText").objectReferenceValue = byTierText;
        sController.FindProperty("byQuantityText").objectReferenceValue = byQtyText;

        sController.FindProperty("inventoryContent").objectReferenceValue = invContent;
        sController.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;

        sController.FindProperty("detailModal").objectReferenceValue = detailModal;
        sController.FindProperty("detailTopCard").objectReferenceValue = dTopCard;
        sController.FindProperty("detailNameText").objectReferenceValue = dName;
        sController.FindProperty("detailTierText").objectReferenceValue = dTier;
        sController.FindProperty("detailDescText").objectReferenceValue = dDesc;
        sController.FindProperty("detailBaseStatText").objectReferenceValue = dBaseStat;

        SerializedProperty sPerkIcons = sController.FindProperty("perkRowIcons");
        sPerkIcons.arraySize = 4;
        for (int i = 0; i < 4; i++) sPerkIcons.GetArrayElementAtIndex(i).objectReferenceValue = dPerkIcons[i];

        SerializedProperty sPerkTexts = sController.FindProperty("perkRowTexts");
        sPerkTexts.arraySize = 4;
        for (int i = 0; i < 4; i++) sPerkTexts.GetArrayElementAtIndex(i).objectReferenceValue = dPerkTexts[i];

        sController.FindProperty("detailEnhanceBtn").objectReferenceValue = dEnhanceBtn;
        sController.FindProperty("detailEnhanceCostText").objectReferenceValue = dEnhanceCostText;
        sController.FindProperty("detailAdvanceTierBtn").objectReferenceValue = dAdvanceTierBtn;
        sController.FindProperty("detailAdvanceTierText").objectReferenceValue = dAdvanceTierText;
        sController.FindProperty("detailEquipBtn").objectReferenceValue = dEquipBtn;
        sController.FindProperty("detailEquipBtnText").objectReferenceValue = dEquipBtnText;
        sController.FindProperty("detailCloseBtn").objectReferenceValue = dCloseBtn;

        sController.FindProperty("toastRoot").objectReferenceValue = toastRoot;
        sController.FindProperty("toastText").objectReferenceValue = toastText;

        // Load Sprites into database
        Sprite[] allBuddySprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(BuddyAtlasPath).OfType<Sprite>().ToArray();
        string[] iconKeys = {
            "drone-snowflake", "drone-spider", "drone-antenna-eye", "drone-cross-visor", "drone-capsule",
            "drone-spiky-mine", "drone-octagon-shield", "drone-claw-magnet", "drone-dual-rotor", "drone-stealth-wing",
            "drone-laser-sentry", "drone-plasma-orb"
        };
        Sprite[] droneIcons = iconKeys.Select(k => allBuddySprites.FirstOrDefault(s => s.name == k)).Where(s => s != null).ToArray();
        Sprite[] frameSprites = new[] {
            allBuddySprites.FirstOrDefault(s => s.name == "buddy-frame-normal"),
            allBuddySprites.FirstOrDefault(s => s.name == "buddy-frame-rare"),
            allBuddySprites.FirstOrDefault(s => s.name == "buddy-frame-epic"),
            allBuddySprites.FirstOrDefault(s => s.name == "buddy-frame-holographic")
        };

        SerializedProperty sIcons = sController.FindProperty("droneIcons");
        sIcons.arraySize = droneIcons.Length;
        for (int i = 0; i < droneIcons.Length; i++) sIcons.GetArrayElementAtIndex(i).objectReferenceValue = droneIcons[i];

        SerializedProperty sFrames = sController.FindProperty("frameSprites");
        sFrames.arraySize = frameSprites.Length;
        for (int i = 0; i < frameSprites.Length; i++) sFrames.GetArrayElementAtIndex(i).objectReferenceValue = frameSprites[i];

        sController.FindProperty("upgradeArrowSprite").objectReferenceValue = allBuddySprites.FirstOrDefault(s => s.name == "badge-upgrade-green");
        sController.FindProperty("lockSlotSprite").objectReferenceValue = allBuddySprites.FirstOrDefault(s => s.name == "icon-lock-buddy");

        SerializedProperty sTierLocks = sController.FindProperty("lockTierSprites");
        sTierLocks.arraySize = 4;
        sTierLocks.GetArrayElementAtIndex(0).objectReferenceValue = allBuddySprites.FirstOrDefault(s => s.name == "icon-lock-blue");
        sTierLocks.GetArrayElementAtIndex(1).objectReferenceValue = allBuddySprites.FirstOrDefault(s => s.name == "icon-lock-purple");
        sTierLocks.GetArrayElementAtIndex(2).objectReferenceValue = allBuddySprites.FirstOrDefault(s => s.name == "icon-lock-yellow");
        sTierLocks.GetArrayElementAtIndex(3).objectReferenceValue = allBuddySprites.FirstOrDefault(s => s.name == "icon-lock-pink");

        sController.FindProperty("unlockedCheckSprite").objectReferenceValue = allBuddySprites.FirstOrDefault(s => s.name == "icon-lock-unlocked");

        sController.ApplyModifiedPropertiesWithoutUndo();

        return panel.gameObject;
    }

    private static BuddyCardUI CreateBuddyCardUI(RectTransform parent, string name, Vector2 size)
    {
        RectTransform cardRoot = CreateRect(name, parent);
        cardRoot.sizeDelta = size;

        // Card Frame Image
        Image cardFrame = cardRoot.gameObject.AddComponent<Image>();
        cardFrame.sprite = LoadBuddySprite("buddy-frame-normal");
        cardFrame.type = Image.Type.Simple;
        cardFrame.preserveAspect = false;
        cardFrame.raycastTarget = true;

        Button cardBtn = cardRoot.gameObject.AddComponent<Button>();
        cardBtn.targetGraphic = cardFrame;

        // 1. Normal Content Group
        RectTransform normalGroup = CreateRect("NormalContentGroup", cardRoot);
        Stretch(normalGroup, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Top Level Text
        TMP_Text levelText = CreateText("LevelText", normalGroup, "LV.01", 26f, Yellow, TextAlignmentOptions.Center);
        Anchor(levelText.rectTransform, new Vector2(0.5f, 0.81f), Vector2.zero, new Vector2(size.x * 0.9f, 32f));

        // Center Drone Icon
        Image centerIcon = CreateBuddyIcon("DroneIcon", normalGroup, "drone-snowflake", 140f);
        Anchor(centerIcon.rectTransform, new Vector2(0.5f, 0.50f), Vector2.zero, new Vector2(140f, 140f));

        // Bottom Progress Bar
        RectTransform bottomBar = CreateRect("BottomBar", normalGroup);
        Stretch(bottomBar, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.24f), Vector2.zero, Vector2.zero);
        Image bottomBarBg = bottomBar.gameObject.AddComponent<Image>();
        bottomBarBg.color = new Color32(20, 140, 60, 240);
        bottomBarBg.raycastTarget = false;

        TMP_Text progressText = CreateText("ProgressText", bottomBar, "65/3", 26f, Color.white, TextAlignmentOptions.Center);
        Stretch(progressText.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 0f), new Vector2(-36f, 0f));

        // Upgrade Green Arrow Button
        GameObject upgradeArrowObj = new GameObject("UpgradeArrowGroup", typeof(RectTransform));
        RectTransform arrowRect = upgradeArrowObj.GetComponent<RectTransform>();
        arrowRect.SetParent(normalGroup, false);
        Anchor(arrowRect, new Vector2(0.86f, 0.16f), Vector2.zero, new Vector2(46f, 46f));

        Image arrowIcon = upgradeArrowObj.AddComponent<Image>();
        arrowIcon.sprite = LoadBuddySprite("badge-upgrade-green");
        arrowIcon.preserveAspect = true;
        arrowIcon.raycastTarget = true;

        Button upgradeBtn = upgradeArrowObj.AddComponent<Button>();
        upgradeBtn.targetGraphic = arrowIcon;

        // 2. Empty Slot Group
        RectTransform emptyGroup = CreateRect("EmptySlotGroup", cardRoot);
        Stretch(emptyGroup, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        TMP_Text emptyText = CreateText("EmptyLabel", emptyGroup, "Empty", 36f, BrightCyan, TextAlignmentOptions.Center);
        Stretch(emptyText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        emptyGroup.gameObject.SetActive(false);

        // 3. Locked Slot Group
        RectTransform lockedGroup = CreateRect("LockedSlotGroup", cardRoot);
        Stretch(lockedGroup, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image lockIcon = CreateBuddyIcon("LockIcon", lockedGroup, "icon-lock-buddy", 80f);
        Anchor(lockIcon.rectTransform, new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(80f, 80f));
        TMP_Text lockedText = CreateText("LockedLabel", lockedGroup, "LOCKED", 30f, BrightCyan, TextAlignmentOptions.Center);
        Anchor(lockedText.rectTransform, new Vector2(0.5f, 0.30f), Vector2.zero, new Vector2(200f, 40f));
        lockedGroup.gameObject.SetActive(false);

        // Wire Component
        BuddyCardUI cardComp = cardRoot.gameObject.AddComponent<BuddyCardUI>();
        cardComp.InitializeReferences(cardFrame, centerIcon, levelText, progressText, cardBtn, upgradeBtn, upgradeArrowObj, normalGroup.gameObject, emptyGroup.gameObject, lockedGroup.gameObject);

        return cardComp;
    }

    private static void ConfigureBuddyCardStaticView(
        BuddyCardUI card,
        string iconName,
        string frameName,
        string level,
        string progress,
        bool canUpgrade)
    {
        if (card == null) return;
        Sprite frameSprite = LoadBuddySprite(frameName);
        Sprite iconSprite = LoadBuddySprite(iconName);
        BuddyItemData dummy = new BuddyItemData
        {
            buddyName = iconName,
            iconKey = iconName,
            level = 1,
            count = 65,
            requiredCount = 3
        };
        card.Setup(dummy, iconSprite, frameSprite);
        EditorUtility.SetDirty(card.gameObject);
    }

    private static void ConfigureBuddyCardEmptyStaticView(BuddyCardUI card, string frameName)
    {
        if (card == null) return;
        Sprite frameSprite = LoadBuddySprite(frameName);
        card.SetupEmpty(frameSprite);
        EditorUtility.SetDirty(card.gameObject);
    }

    private static void ConfigureBuddyCardLockedStaticView(BuddyCardUI card, string frameName)
    {
        if (card == null) return;
        Sprite frameSprite = LoadBuddySprite(frameName);
        card.SetupLocked(frameSprite);
        EditorUtility.SetDirty(card.gameObject);
    }

    private static GameObject CreateBuddyDetailModal(
        RectTransform canvasRect,
        out BuddyCardUI topCard,
        out TMP_Text nameText,
        out TMP_Text tierText,
        out TMP_Text descText,
        out TMP_Text baseStatText,
        out Image[] perkIcons,
        out TMP_Text[] perkTexts,
        out Button enhanceBtn,
        out TMP_Text enhanceCostText,
        out Button advanceTierBtn,
        out TMP_Text advanceTierText,
        out Button equipBtn,
        out TMP_Text equipBtnText,
        out Button closeBtn)
    {
        RectTransform modalRoot = CreateRect("BuddyDetailModal", canvasRect);
        Stretch(modalRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image dim = modalRoot.gameObject.AddComponent<Image>();
        dim.color = new Color32(5, 20, 30, 225);
        dim.raycastTarget = true;

        Button dimBtn = modalRoot.gameObject.AddComponent<Button>();
        dimBtn.targetGraphic = dim;

        GameObject cardBox = CreateFrame("ModalBox", modalRoot, DarkPanel, BrightCyan, out Image boxBg);
        RectTransform boxRect = cardBox.GetComponent<RectTransform>();
        Anchor(boxRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 1380f));
        boxBg.raycastTarget = true;

        // Close Button (Top Right)
        GameObject closeObj = CreateFrame("CloseBtn", boxRect, FieryRed, FieryOrange, out Image closeBg);
        RectTransform closeRect = closeObj.GetComponent<RectTransform>();
        Anchor(closeRect, new Vector2(0.93f, 0.96f), Vector2.zero, new Vector2(54f, 54f));
        TMP_Text closeTxt = CreateText("X", closeRect, "X", 32f, Color.white, TextAlignmentOptions.Center);
        Stretch(closeTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        closeBg.raycastTarget = true;
        closeBtn = closeObj.AddComponent<Button>();
        closeBtn.targetGraphic = closeBg;
        dimBtn.onClick.AddListener(() => modalRoot.gameObject.SetActive(false));

        // 1. Top Card Display
        topCard = CreateBuddyCardUI(boxRect, "TopCard", new Vector2(285f, 335f));
        Anchor(topCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(285f, 335f));

        // 2. Drone Title
        nameText = CreateText("Name", boxRect, "Turret Buffer", 44f, Color.white, TextAlignmentOptions.Center);
        Anchor(nameText.rectTransform, new Vector2(0.5f, 0.695f), Vector2.zero, new Vector2(800f, 52f));

        // 3. Drone Tier Subtitle
        tierText = CreateText("Tier", boxRect, "Common", 28f, new Color32(190, 225, 235, 255), TextAlignmentOptions.Center);
        Anchor(tierText.rectTransform, new Vector2(0.5f, 0.655f), Vector2.zero, new Vector2(800f, 40f));

        // 4. Description
        descText = CreateText("Description", boxRect, "Improves the skills of all Turrets.", 26f, Color.white, TextAlignmentOptions.Center);
        Anchor(descText.rectTransform, new Vector2(0.5f, 0.610f), Vector2.zero, new Vector2(800f, 40f));

        // 5. Base Stat Line
        baseStatText = CreateText("BaseStat", boxRect, "All Turrets' Duration <color=#FFCB49>10%</color>", 28f, Color.white, TextAlignmentOptions.Center);
        Anchor(baseStatText.rectTransform, new Vector2(0.5f, 0.555f), Vector2.zero, new Vector2(800f, 44f));

        // 6. 4 Tier Perk Rows
        perkIcons = new Image[4];
        perkTexts = new TMP_Text[4];

        string[] defaultLockSprites = { "Lock_Blue", "Lock_Purple", "Lock_Yellow", "Lock_Red" };
        string[] defaultPerks = {
            "Turret Duration +20%(<color=#38BDF8>Rare</color>Unlock)",
            "Turret Duration +30%(<color=#C084FC>Unique</color>Unlock)",
            "Turret Duration +30%(<color=#FACC15>Epic</color>Unlock)",
            "Turret Duration +30%(<color=#FB7185>Holo</color>Unlock)"
        };

        float startY = 0.485f;
        float deltaY = 0.055f;

        for (int i = 0; i < 4; i++)
        {
            RectTransform rowRect = CreateRect($"PerkRow_{i}", boxRect);
            Anchor(rowRect, new Vector2(0.5f, startY - i * deltaY), Vector2.zero, new Vector2(780f, 50f));

            Sprite lockSp = LoadChipsetSprite(defaultLockSprites[i]);
            Image lockImg = CreateImage("LockIcon", rowRect, Color.white, false);
            lockImg.sprite = lockSp;
            lockImg.preserveAspect = true;
            if (i == 3 && lockSp != null)
            {
                lockImg.material = ChipsetFrameShimmerMaterial.Get(lockSp);
            }
            Anchor(lockImg.rectTransform, new Vector2(0.08f, 0.5f), Vector2.zero, new Vector2(42f, 42f));
            perkIcons[i] = lockImg;

            TMP_Text pText = CreateText("PerkText", rowRect, defaultPerks[i], 26f, Color.white, TextAlignmentOptions.Left);
            Anchor(pText.rectTransform, new Vector2(0.57f, 0.5f), Vector2.zero, new Vector2(650f, 44f));
            perkTexts[i] = pText;
        }

        // 7. Action Buttons
        // Equip Button (Bottom Left)
        GameObject eqBtnObj = CreateFrame("EquipBtn", boxRect, Yellow, Border, out Image eqBg);
        eqBg.sprite = LoadBuddySprite("btn-equip-plate");
        RectTransform eqBtnRect = eqBtnObj.GetComponent<RectTransform>();
        Anchor(eqBtnRect, new Vector2(0.26f, 0.12f), Vector2.zero, new Vector2(280f, 135f));
        equipBtnText = CreateText("Label", eqBtnRect, "EQUIP", 36f, new Color32(10, 20, 30, 255), TextAlignmentOptions.Center);
        Stretch(equipBtnText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        eqBg.raycastTarget = true;
        equipBtn = eqBtnObj.AddComponent<Button>();
        equipBtn.targetGraphic = eqBg;

        // Enhance Button (Bottom Right Top)
        GameObject enhBtnObj = CreateFrame("EnhanceBtn", boxRect, new Color32(34, 197, 94, 255), BrightCyan, out Image enhBg);
        enhBg.sprite = LoadBuddySprite("btn-enhance-plate");
        RectTransform enhBtnRect = enhBtnObj.GetComponent<RectTransform>();
        Anchor(enhBtnRect, new Vector2(0.71f, 0.16f), Vector2.zero, new Vector2(360f, 90f));
        enhBg.raycastTarget = true;
        enhanceBtn = enhBtnObj.AddComponent<Button>();
        enhanceBtn.targetGraphic = enhBg;

        TMP_Text enhLabel = CreateText("Label", enhBtnRect, "Enhance", 26f, new Color32(10, 20, 30, 255), TextAlignmentOptions.Center);
        Anchor(enhLabel.rectTransform, new Vector2(0.5f, 0.70f), Vector2.zero, new Vector2(320f, 32f));

        RectTransform costRow = CreateRect("CostRow", enhBtnRect);
        Anchor(costRow, new Vector2(0.5f, 0.30f), Vector2.zero, new Vector2(200f, 34f));
        HorizontalLayoutGroup costLayout = costRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        costLayout.childAlignment = TextAnchor.MiddleCenter;
        costLayout.spacing = 8f;
        costLayout.childControlWidth = false;
        costLayout.childControlHeight = false;

        enhanceCostText = CreateText("CostValue", costRow, "500", 24f, new Color32(10, 20, 30, 255), TextAlignmentOptions.Center);
        enhanceCostText.rectTransform.sizeDelta = new Vector2(70f, 30f);
        Image chipMini = CreateBuddyIcon("ChipIcon", costRow, "mini-chip-icon", 30f);
        chipMini.rectTransform.sizeDelta = new Vector2(30f, 30f);

        // Advance Tier Button (Bottom Right Bottom)
        GameObject advBtnObj = CreateFrame("AdvanceTierBtn", boxRect, new Color32(132, 204, 22, 255), Yellow, out Image advBg);
        advBg.sprite = LoadBuddySprite("btn-advance-plate");
        RectTransform advBtnRect = advBtnObj.GetComponent<RectTransform>();
        Anchor(advBtnRect, new Vector2(0.71f, 0.075f), Vector2.zero, new Vector2(360f, 80f));
        advBg.raycastTarget = true;
        advanceTierBtn = advBtnObj.AddComponent<Button>();
        advanceTierBtn.targetGraphic = advBg;

        advanceTierText = CreateText("Label", advBtnRect, "Advance Tier (79/3)", 24f, new Color32(10, 20, 30, 255), TextAlignmentOptions.Center);
        Stretch(advanceTierText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        return modalRoot.gameObject;
    }

    private static Button CreatePresetButton(
        RectTransform parent,
        string name,
        float xAnchor,
        string text,
        bool active,
        out Image bg,
        out TMP_Text label)
    {
        GameObject root = CreateFrame(name, parent, active ? Yellow : new Color32(18, 58, 68, 255), BrightCyan, out bg);
        RectTransform rect = root.GetComponent<RectTransform>();
        Anchor(rect, new Vector2(xAnchor, 0.5f), Vector2.zero, new Vector2(88f, 72f));
        label = CreateText("Label", rect, text, 36f, active ? new Color32(10, 20, 30, 255) : Color.white, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bg.raycastTarget = true;
        Button btn = root.AddComponent<Button>();
        btn.targetGraphic = bg;
        return btn;
    }

    private static ChipsetCardUI CreateChipCardUI(RectTransform parent, string name, Vector2 size)
    {
        RectTransform cardRoot = CreateRect(name, parent);
        cardRoot.sizeDelta = size;

        // Card Frame Image (pins top/bottom)
        Image cardFrame = cardRoot.gameObject.AddComponent<Image>();
        cardFrame.sprite = LoadChipsetSprite("Green") ?? LoadChipsetSprite("card-frame-tier1-green");
        cardFrame.type = Image.Type.Simple;
        cardFrame.preserveAspect = false;
        cardFrame.raycastTarget = true;

        Button cardBtn = cardRoot.gameObject.AddComponent<Button>();
        cardBtn.targetGraphic = cardFrame;

        // 1. Normal Content Group
        RectTransform normalGroup = CreateRect("NormalContentGroup", cardRoot);
        Stretch(normalGroup, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Top Level Text
        TMP_Text levelText = CreateText("LevelText", normalGroup, "LV.01", 24f, Color.white, TextAlignmentOptions.Center);
        levelText.fontStyle = FontStyles.Bold;
        levelText.outlineColor = Color.black;
        levelText.outlineWidth = 0.25f;
        Anchor(levelText.rectTransform, new Vector2(0.5f, 0.86f), new Vector2(0f, -8f), new Vector2(size.x * 0.85f, 32f));

        // Star Icon (Top Right)
        Image starImg = CreateChipsetIcon("Star", normalGroup, "icon-star", 28f);
        Anchor(starImg.rectTransform, new Vector2(0.82f, 0.84f), Vector2.zero, new Vector2(28f, 28f));
        starImg.gameObject.SetActive(false);

        // Center Icon
        Image centerIcon = CreateChipsetIcon("Icon", normalGroup, "standard-gun", 110f);
        Anchor(centerIcon.rectTransform, new Vector2(0.5f, 0.52f), new Vector2(0f, 13f), new Vector2(110f, 110f));
        centerIcon.preserveAspect = true;

        // Bottom Progress Bar
        RectTransform bottomBar = CreateRect("BottomBar", normalGroup);
        bottomBar.anchorMin = new Vector2(0.09573685f, 0.19583333f);
        bottomBar.anchorMax = new Vector2(0.92f, 0.35000002f);
        bottomBar.pivot = new Vector2(0.5f, 0.5f);
        bottomBar.anchoredPosition = new Vector2(-1.3580017f, -0.049995422f);
        bottomBar.sizeDelta = new Vector2(-3.5190032f, -1.1000135f);
        bottomBar.localRotation = Quaternion.identity;
        bottomBar.localScale = Vector3.one;
        Image bottomBarBg = bottomBar.gameObject.AddComponent<Image>();
        bottomBarBg.color = new Color32(14, 38, 32, 235);
        bottomBarBg.raycastTarget = false;

        RectTransform fillRect = CreateRect("ProgressFill", bottomBar);
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImg = fillRect.gameObject.AddComponent<Image>();
        fillImg.color = new Color32(74, 222, 128, 255);
        fillImg.raycastTarget = false;

        TMP_Text progressText = CreateText("ProgressText", bottomBar, "22/3", 22f, Color.white, TextAlignmentOptions.Center);
        progressText.fontStyle = FontStyles.Bold;
        progressText.outlineColor = Color.black;
        progressText.outlineWidth = 0.25f;
        Stretch(progressText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Upgrade Green Arrow Button
        GameObject upgradeArrowObj = new GameObject("UpgradeArrowGroup", typeof(RectTransform));
        RectTransform arrowRect = upgradeArrowObj.GetComponent<RectTransform>();
        arrowRect.SetParent(normalGroup, false);
        Anchor(arrowRect, new Vector2(0.88f, 0.17f), new Vector2(0f, 23.8f), new Vector2(44f, 44f));

        Image arrowIcon = upgradeArrowObj.AddComponent<Image>();
        arrowIcon.sprite = LoadChipsetSprite("badge-upgrade");
        arrowIcon.preserveAspect = true;
        arrowIcon.raycastTarget = true;

        Button upgradeBtn = upgradeArrowObj.AddComponent<Button>();
        upgradeBtn.targetGraphic = arrowIcon;

        // 2. Empty Slot Group
        RectTransform emptyGroup = CreateRect("EmptySlotGroup", cardRoot);
        Stretch(emptyGroup, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        TMP_Text emptyText = CreateText("EmptyLabel", emptyGroup, "Empty", 32f, BrightCyan, TextAlignmentOptions.Center);
        Stretch(emptyText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        emptyGroup.gameObject.SetActive(false);

        // Wire Component
        ChipsetCardUI cardComp = cardRoot.gameObject.AddComponent<ChipsetCardUI>();
        SerializedObject sCard = new SerializedObject(cardComp);
        sCard.FindProperty("cardFrameImage").objectReferenceValue = cardFrame;
        sCard.FindProperty("iconImage").objectReferenceValue = centerIcon;
        sCard.FindProperty("levelText").objectReferenceValue = levelText;
        sCard.FindProperty("progressText").objectReferenceValue = progressText;
        sCard.FindProperty("cardButton").objectReferenceValue = cardBtn;
        sCard.FindProperty("upgradeButton").objectReferenceValue = upgradeBtn;
        sCard.FindProperty("upgradeArrowGroup").objectReferenceValue = upgradeArrowObj;
        sCard.FindProperty("starObject").objectReferenceValue = starImg.gameObject;
        sCard.FindProperty("bottomProgressBar").objectReferenceValue = bottomBarBg;
        sCard.FindProperty("progressFillImage").objectReferenceValue = fillImg;
        sCard.FindProperty("progressFillRect").objectReferenceValue = fillRect;
        sCard.FindProperty("normalContentGroup").objectReferenceValue = normalGroup.gameObject;
        sCard.FindProperty("emptySlotGroup").objectReferenceValue = emptyGroup.gameObject;
        sCard.ApplyModifiedPropertiesWithoutUndo();

        cardComp.InitializeReferences(
            cardFrame,
            centerIcon,
            levelText,
            progressText,
            cardBtn,
            upgradeBtn,
            upgradeArrowObj,
            starImg.gameObject,
            bottomBarBg,
            normalGroup.gameObject,
            emptyGroup.gameObject,
            fillImg,
            fillRect);

        return cardComp;
    }

    private static void ConfigureCardStaticView(
        ChipsetCardUI card,
        string iconName,
        string frameName,
        string level,
        string progress,
        bool hasStar,
        bool canUpgrade)
    {
        if (card == null) return;
        Sprite frameSprite = LoadChipsetSprite(frameName);
        Sprite iconSprite = LoadChipsetSprite(iconName);
        card.SetDirectVisual(frameSprite, iconSprite, level, progress, hasStar, canUpgrade);
        EditorUtility.SetDirty(card.gameObject);
    }

    private static GameObject CreateDetailModal(
        RectTransform canvasRect,
        out TMP_Text modBadgeText,
        out ChipsetCardUI topCard,
        out TMP_Text nameText,
        out TMP_Text tierText,
        out TMP_Text descText,
        out TMP_Text baseStatsText,
        out Image[] perkIcons,
        out TMP_Text[] perkTexts,
        out Button enhanceBtn,
        out TMP_Text enhanceCostText,
        out CanvasGroup enhanceBtnCg,
        out Button advanceTierBtn,
        out TMP_Text advanceTierText,
        out CanvasGroup advanceTierBtnCg,
        out GameObject fragNotice,
        out GameObject chipNotice,
        out Button equipBtn,
        out TMP_Text equipBtnText,
        out Button closeBtn)
    {
        RectTransform modalRoot = CreateRect("ChipsetDetailModal", canvasRect);
        Stretch(modalRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image dim = modalRoot.gameObject.AddComponent<Image>();
        dim.color = new Color32(5, 20, 30, 225);
        dim.raycastTarget = true;

        Button dimBtn = modalRoot.gameObject.AddComponent<Button>();
        dimBtn.targetGraphic = dim;

        GameObject cardBox = CreateFrame("ModalBox", modalRoot, DarkPanel, BrightCyan, out Image boxBg);
        RectTransform boxRect = cardBox.GetComponent<RectTransform>();
        Anchor(boxRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 1380f));
        boxBg.raycastTarget = true;

        // Close Button (Top Right)
        GameObject closeObj = CreateFrame("CloseBtn", boxRect, FieryRed, FieryOrange, out Image closeBg);
        RectTransform closeRect = closeObj.GetComponent<RectTransform>();
        Anchor(closeRect, new Vector2(0.93f, 0.96f), Vector2.zero, new Vector2(54f, 54f));
        TMP_Text closeTxt = CreateText("X", closeRect, "X", 32f, Color.white, TextAlignmentOptions.Center);
        Stretch(closeTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        closeBg.raycastTarget = true;
        closeBtn = closeObj.AddComponent<Button>();
        closeBtn.targetGraphic = closeBg;
        dimBtn.onClick.AddListener(() => modalRoot.gameObject.SetActive(false));

        // Top Mod Badge (Wrench Badge)
        GameObject modBadgeObj = CreateFrame("ModBadge", boxRect, new Color32(45, 35, 110, 255), new Color32(110, 95, 220, 255), out Image badgeBg);
        RectTransform badgeRect = modBadgeObj.GetComponent<RectTransform>();
        Anchor(badgeRect, new Vector2(0.5f, 0.96f), Vector2.zero, new Vector2(460f, 48f));
        modBadgeText = CreateText("BadgeLabel", badgeRect, "🔧 Mod able (up to LV24) 🔧", 24f, Color.white, TextAlignmentOptions.Center);
        Stretch(modBadgeText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // 1. Top Card Display
        topCard = CreateChipCardUI(boxRect, "TopCard", new Vector2(285f, 335f));
        Anchor(topCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.81f), Vector2.zero, new Vector2(285f, 335f));

        // 2. Chip Title
        nameText = CreateText("Name", boxRect, "Sonic Boom", 44f, Color.white, TextAlignmentOptions.Center);
        Anchor(nameText.rectTransform, new Vector2(0.5f, 0.665f), Vector2.zero, new Vector2(800f, 52f));

        // 3. Chip Tier Subtitle
        tierText = CreateText("Tier", boxRect, "Common", 28f, new Color32(190, 225, 235, 255), TextAlignmentOptions.Center);
        Anchor(tierText.rectTransform, new Vector2(0.5f, 0.625f), Vector2.zero, new Vector2(800f, 40f));

        // 4. Description
        descText = CreateText("Description", boxRect, "Inflicts a Sonic attack on enemies in a large area.", 26f, Color.white, TextAlignmentOptions.Center);
        Anchor(descText.rectTransform, new Vector2(0.5f, 0.580f), Vector2.zero, new Vector2(800f, 40f));

        // 5. Base Stat Line
        baseStatsText = CreateText("BaseStat", boxRect, "ATK <color=#FFCB49>33</color>\n<color=#FFCB49>Very slow</color> ATK Speed", 28f, Color.white, TextAlignmentOptions.Center);
        Anchor(baseStatsText.rectTransform, new Vector2(0.5f, 0.515f), Vector2.zero, new Vector2(800f, 56f));

        // 6. 4 Tier Perk Rows
        perkIcons = new Image[4];
        perkTexts = new TMP_Text[4];

        string[] defaultLockSprites = { "icon-lock-blue", "icon-lock-purple", "icon-lock-yellow", "icon-lock-pink" };
        string[] defaultPerks = {
            "ATK +15%(<color=#38BDF8>Magic</color>Unlock)",
            "AoE ATK Range +15%(<color=#C084FC>Rare</color>Unlock)",
            "ATK +30%(<color=#FACC15>Unique</color>Unlock)",
            "AoE ATK Range +35%(<color=#FB7185>Epic</color>Unlock)"
        };

        float startY = 0.445f;
        float deltaY = 0.055f;

        for (int i = 0; i < 4; i++)
        {
            RectTransform rowRect = CreateRect($"PerkRow_{i}", boxRect);
            Anchor(rowRect, new Vector2(0.5f, startY - i * deltaY), Vector2.zero, new Vector2(780f, 50f));

            Image lockImg = CreateBuddyIcon("LockIcon", rowRect, defaultLockSprites[i], 42f);
            Anchor(lockImg.rectTransform, new Vector2(0.08f, 0.5f), Vector2.zero, new Vector2(42f, 42f));
            perkIcons[i] = lockImg;

            TMP_Text pText = CreateText("PerkText", rowRect, defaultPerks[i], 26f, Color.white, TextAlignmentOptions.Left);
            Anchor(pText.rectTransform, new Vector2(0.57f, 0.5f), Vector2.zero, new Vector2(650f, 44f));
            perkTexts[i] = pText;
        }

        // 7. Action Buttons
        // Equip Button (Bottom Left)
        GameObject eqBtnObj = CreateFrame("EquipBtn", boxRect, Yellow, Border, out Image eqBg);
        eqBg.sprite = LoadBuddySprite("btn-equip-plate");
        RectTransform eqBtnRect = eqBtnObj.GetComponent<RectTransform>();
        Anchor(eqBtnRect, new Vector2(0.26f, 0.12f), Vector2.zero, new Vector2(280f, 135f));
        equipBtnText = CreateText("Label", eqBtnRect, "EQUIP", 36f, new Color32(10, 20, 30, 255), TextAlignmentOptions.Center);
        Stretch(equipBtnText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        eqBg.raycastTarget = true;
        equipBtn = eqBtnObj.AddComponent<Button>();
        equipBtn.targetGraphic = eqBg;

        // Enhance Button (Bottom Right Top)
        GameObject enhBtnObj = CreateFrame("EnhanceBtn", boxRect, new Color32(34, 197, 94, 255), BrightCyan, out Image enhBg);
        enhBg.sprite = LoadBuddySprite("btn-enhance-plate");
        RectTransform enhBtnRect = enhBtnObj.GetComponent<RectTransform>();
        Anchor(enhBtnRect, new Vector2(0.71f, 0.16f), Vector2.zero, new Vector2(360f, 90f));
        enhBg.raycastTarget = true;
        enhanceBtn = enhBtnObj.AddComponent<Button>();
        enhanceBtn.targetGraphic = enhBg;
        enhanceBtnCg = enhBtnObj.AddComponent<CanvasGroup>();

        TMP_Text enhLabel = CreateText("Label", enhBtnRect, "Enhance", 26f, new Color32(10, 20, 30, 255), TextAlignmentOptions.Center);
        Anchor(enhLabel.rectTransform, new Vector2(0.5f, 0.70f), Vector2.zero, new Vector2(320f, 32f));

        RectTransform costRow = CreateRect("CostRow", enhBtnRect);
        Anchor(costRow, new Vector2(0.5f, 0.30f), Vector2.zero, new Vector2(200f, 34f));
        HorizontalLayoutGroup costLayout = costRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        costLayout.childAlignment = TextAnchor.MiddleCenter;
        costLayout.spacing = 8f;
        costLayout.childControlWidth = false;
        costLayout.childControlHeight = false;

        enhanceCostText = CreateText("CostValue", costRow, "500", 24f, new Color32(10, 20, 30, 255), TextAlignmentOptions.Center);
        enhanceCostText.rectTransform.sizeDelta = new Vector2(70f, 30f);
        Image chipMini = CreateBuddyIcon("ChipIcon", costRow, "mini-chip-icon", 30f);
        chipMini.rectTransform.sizeDelta = new Vector2(30f, 30f);

        // Advance Tier Button (Bottom Right Bottom)
        GameObject advBtnObj = CreateFrame("AdvanceTierBtn", boxRect, new Color32(132, 204, 22, 255), Yellow, out Image advBg);
        advBg.sprite = LoadBuddySprite("btn-advance-plate");
        RectTransform advBtnRect = advBtnObj.GetComponent<RectTransform>();
        Anchor(advBtnRect, new Vector2(0.71f, 0.075f), Vector2.zero, new Vector2(360f, 80f));
        advBg.raycastTarget = true;
        advanceTierBtn = advBtnObj.AddComponent<Button>();
        advanceTierBtn.targetGraphic = advBg;
        advanceTierBtnCg = advBtnObj.AddComponent<CanvasGroup>();

        advanceTierText = CreateText("Label", advBtnRect, "Advance Tier (439/3)", 24f, new Color32(10, 20, 30, 255), TextAlignmentOptions.Center);
        Stretch(advanceTierText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // 8. Notice Panels (Missing Currency / Fragments)
        // Panel 1: Not Enough Fragments Notice (Bảng ở Hình 1)
        GameObject fragNoticeObj = CreateFrame("NotEnoughFragmentsNotice", boxRect, new Color32(11, 40, 56, 252), BrightCyan, out Image fragBg);
        RectTransform fragNoticeRect = fragNoticeObj.GetComponent<RectTransform>();
        Anchor(fragNoticeRect, new Vector2(0.5f, 0.54f), Vector2.zero, new Vector2(830f, 270f));
        fragBg.raycastTarget = false;

        TMP_Text fragLine1 = CreateText("TitleText", fragNoticeRect, "You need to collect more Chipsets.", 32f, Color.white, TextAlignmentOptions.Center);
        fragLine1.fontStyle = FontStyles.Bold;
        fragLine1.outlineColor = Color.black;
        fragLine1.outlineWidth = 0.25f;
        Anchor(fragLine1.rectTransform, new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(790f, 70f));

        TMP_Text fragLine2 = CreateText("SubText", fragNoticeRect, "You can purchase Chipset Boxes at the\nShop.", 26f, new Color32(254, 209, 66, 255), TextAlignmentOptions.Center);
        fragLine2.fontStyle = FontStyles.Bold;
        fragLine2.outlineColor = Color.black;
        fragLine2.outlineWidth = 0.25f;
        Anchor(fragLine2.rectTransform, new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(790f, 90f));

        fragNoticeObj.AddComponent<UIDissolveController>();
        fragNoticeObj.SetActive(false);
        fragNotice = fragNoticeObj;

        // Panel 2: Not Enough Data Chips Notice (Bảng ở Hình 2)
        GameObject chipNoticeObj = CreateFrame("NotEnoughChipsNotice", boxRect, new Color32(11, 40, 56, 252), BrightCyan, out Image chipBg);
        RectTransform chipNoticeRect = chipNoticeObj.GetComponent<RectTransform>();
        Anchor(chipNoticeRect, new Vector2(0.5f, 0.54f), Vector2.zero, new Vector2(830f, 270f));
        chipBg.raycastTarget = false;

        TMP_Text chipLine = CreateText("TitleText", chipNoticeRect, "Not enough Data Chips", 34f, Color.white, TextAlignmentOptions.Center);
        chipLine.fontStyle = FontStyles.Bold;
        chipLine.outlineColor = Color.black;
        chipLine.outlineWidth = 0.25f;
        Stretch(chipLine.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 20f), new Vector2(-20f, -20f));

        chipNoticeObj.AddComponent<UIDissolveController>();
        chipNoticeObj.SetActive(false);
        chipNotice = chipNoticeObj;

        return modalRoot.gameObject;
    }

    private static GameObject CreateFurnaceModal(
        RectTransform canvasRect,
        out TMP_Text descText,
        out Button dismantleBtn,
        out Button closeBtn)
    {
        RectTransform modalRoot = CreateRect("BlastFurnaceModal", canvasRect);
        Stretch(modalRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image dim = modalRoot.gameObject.AddComponent<Image>();
        dim.color = new Color32(20, 5, 5, 230);
        dim.raycastTarget = true;

        GameObject box = CreateFrame("FurnaceBox", modalRoot, new Color32(40, 10, 10, 255), FieryOrange, out _);
        RectTransform boxRect = box.GetComponent<RectTransform>();
        Anchor(boxRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 760f));

        TMP_Text title = CreateText("Title", boxRect, "BLAST FURNACE - RECYCLE", 44f, Yellow, TextAlignmentOptions.Center);
        Anchor(title.rectTransform, new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(800f, 55f));

        GameObject closeObj = CreateFrame("CloseBtn", boxRect, FieryRed, FieryOrange, out Image closeBg);
        RectTransform closeRect = closeObj.GetComponent<RectTransform>();
        Anchor(closeRect, new Vector2(0.93f, 0.90f), Vector2.zero, new Vector2(60f, 60f));
        TMP_Text closeTxt = CreateText("X", closeRect, "X", 36f, Color.white, TextAlignmentOptions.Center);
        Stretch(closeTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        closeBg.raycastTarget = true;
        closeBtn = closeObj.AddComponent<Button>();
        closeBtn.targetGraphic = closeBg;

        descText = CreateText("Desc", boxRect, "Dismantle spare chip fragments into Chipset Currency!", 32f, Color.white, TextAlignmentOptions.Center);
        Anchor(descText.rectTransform, new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(820f, 300f));

        GameObject disObj = CreateFrame("DismantleBtn", boxRect, FieryRed, Yellow, out Image disBg);
        RectTransform disRect = disObj.GetComponent<RectTransform>();
        Anchor(disRect, new Vector2(0.5f, 0.16f), Vector2.zero, new Vector2(500f, 100f));
        TMP_Text disTxt = CreateText("Label", disRect, "DISMANTLE ALL SPARES", 34f, Yellow, TextAlignmentOptions.Center);
        Stretch(disTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        disBg.raycastTarget = true;
        dismantleBtn = disObj.AddComponent<Button>();
        dismantleBtn.targetGraphic = disBg;

        return modalRoot.gameObject;
    }

    private static GameObject CreateToastRoot(RectTransform canvasRect, out TMP_Text toastText)
    {
        RectTransform toast = CreateRect("ToastMessage", canvasRect);
        Anchor(toast, new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(800f, 75f));
        Image bg = toast.gameObject.AddComponent<Image>();
        bg.color = new Color32(8, 30, 48, 245);
        AddOutline(bg, BrightCyan, 3f);

        toastText = CreateText("ToastText", toast, "Notification", 32f, Yellow, TextAlignmentOptions.Center);
        Stretch(toastText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return toast.gameObject;
    }

    private static GameObject CreateShopPanel(
        RectTransform parent,
        TMP_Text energyBalanceText,
        TMP_Text chipBalanceText,
        TMP_Text redChipBalanceText)
    {
        RectTransform panel = CreateRect("ShopPanel (Scrollable)", parent);
        Stretch(panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        ScrollRect scrollRect = panel.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.12f;
        scrollRect.scrollSensitivity = 55f;

        Image viewportImage = CreateImage("Viewport", panel, new Color32(255, 255, 255, 1), false);
        Stretch(viewportImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 58f), Vector2.zero);
        Mask viewportMask = viewportImage.gameObject.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        scrollRect.viewport = viewportImage.rectTransform;

        RectTransform shopContent = CreateRect("ShopContent", viewportImage.rectTransform);
        shopContent.anchorMin = new Vector2(0f, 1f);
        shopContent.anchorMax = new Vector2(1f, 1f);
        shopContent.pivot = new Vector2(0.5f, 1f);
        shopContent.anchoredPosition = Vector2.zero;
        shopContent.sizeDelta = new Vector2(0f, 1900f);
        scrollRect.content = shopContent;

        List<ShopOfferView> offerViews = new List<ShopOfferView>(7);

        RectTransform dailySection = CreateShopSection("DailyShopSection", shopContent, 0f, 470f);
        CreateShopSectionHeader(dailySection, "Daily Shop");
        offerViews.Add(CreateShopOfferCard(
            dailySection, "FreeGemItem", 0.025f, 0.34f, "Gem", "x80", "FREE", "red-currency",
            new Color32(88, 174, 108, 255), false));
        offerViews.Add(CreateShopOfferCard(
            dailySection, "DroneBoxItem_1", 0.35f, 0.665f, "Drone Box", "x1", "x180", "drone-box",
            new Color32(26, 57, 77, 255), true));
        offerViews.Add(CreateShopOfferCard(
            dailySection, "DroneBoxItem_2", 0.675f, 0.99f, "Drone Box", "x1", "x180", "drone-box",
            new Color32(26, 57, 77, 255), true));

        RectTransform boxSection = CreateShopSection("BoxSection", shopContent, 480f, 845f);
        CreateShopSectionHeader(boxSection, "Box");

        RectTransform chipsetGrid = CreateRect("ChipsetBoxGrid", boxSection);
        Stretch(chipsetGrid, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -455f), new Vector2(0f, -105f));
        offerViews.Add(CreateShopOfferCard(
            chipsetGrid, "Open1Item", 0.03f, 0.495f, "Open Chipset Box", "1 time", "x300", "chipset-box",
            new Color32(25, 58, 76, 255), false, 0f));
        offerViews.Add(CreateShopOfferCard(
            chipsetGrid, "Open10Item", 0.505f, 0.97f, "Open Chipset Box", "10 times", "x2,700", "chipset-box",
            new Color32(25, 58, 76, 255), false, 0f));

        RectTransform droneGrid = CreateRect("DroneBoxGrid", boxSection);
        Stretch(droneGrid, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -810f), new Vector2(0f, -460f));
        offerViews.Add(CreateShopOfferCard(
            droneGrid, "Open1Item", 0.03f, 0.495f, "Open Drone Box", "1 time", "x600", "drone-box",
            new Color32(25, 58, 76, 255), false, 0f));
        offerViews.Add(CreateShopOfferCard(
            droneGrid, "Open5Item", 0.505f, 0.97f, "Open Drone Box", "5 times", "x2,700", "drone-box",
            new Color32(25, 58, 76, 255), false, 0f));

        RectTransform gemSection = CreateShopSection("GemSection", shopContent, 1335f, 540f);
        CreateShopSectionHeader(gemSection, "Gem");
        CreateGemPreviewCard(gemSection, "GemPack_80", 0.03f, 0.335f, "x80", "20,000 VND");
        CreateGemPreviewCard(gemSection, "GemPack_500", 0.3475f, 0.6525f, "x500", "97,000 VND");
        CreateGemPreviewCard(gemSection, "GemPack_1200", 0.665f, 0.97f, "x1,200", "198,000 VND");

        TMP_Text feedbackText = CreateText(
            "ShopFeedback",
            panel,
            "DAILY SHOP READY",
            24f,
            new Color32(151, 240, 226, 255),
            TextAlignmentOptions.Center);
        Stretch(feedbackText.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(25f, 6f), new Vector2(-25f, 54f));

        ShopController controller = panel.gameObject.AddComponent<ShopController>();
        SerializedObject serializedController = new SerializedObject(controller);
        GetRequiredProperty(serializedController, "energyText").objectReferenceValue = energyBalanceText;
        GetRequiredProperty(serializedController, "dataChipText").objectReferenceValue = chipBalanceText;
        GetRequiredProperty(serializedController, "redGemText").objectReferenceValue = redChipBalanceText;
        GetRequiredProperty(serializedController, "feedbackText").objectReferenceValue = feedbackText;

        SerializedProperty offers = GetRequiredProperty(serializedController, "offers");
        offers.arraySize = offerViews.Count;
        ConfigureShopOffer(offers.GetArrayElementAtIndex(0), offerViews[0], "free-gem", "FREE GEM", 0, ShopController.CurrencyType.Free, ShopController.RewardType.RedGem, 80, true);
        ConfigureShopOffer(offers.GetArrayElementAtIndex(1), offerViews[1], "daily-drone-1", "DRONE BOX", 180, ShopController.CurrencyType.RedGem, ShopController.RewardType.DroneBox, 1, true);
        ConfigureShopOffer(offers.GetArrayElementAtIndex(2), offerViews[2], "daily-drone-2", "DRONE BOX", 180, ShopController.CurrencyType.RedGem, ShopController.RewardType.DroneBox, 1, true);
        ConfigureShopOffer(offers.GetArrayElementAtIndex(3), offerViews[3], "chipset-box-1", "CHIPSET BOX", 300, ShopController.CurrencyType.RedGem, ShopController.RewardType.ChipsetBox, 1, false);
        ConfigureShopOffer(offers.GetArrayElementAtIndex(4), offerViews[4], "chipset-box-10", "CHIPSET BOX", 2700, ShopController.CurrencyType.RedGem, ShopController.RewardType.ChipsetBox, 10, false);
        ConfigureShopOffer(offers.GetArrayElementAtIndex(5), offerViews[5], "drone-box-1", "DRONE BOX", 600, ShopController.CurrencyType.RedGem, ShopController.RewardType.DroneBox, 1, false);
        ConfigureShopOffer(offers.GetArrayElementAtIndex(6), offerViews[6], "drone-box-5", "DRONE BOX", 2700, ShopController.CurrencyType.RedGem, ShopController.RewardType.DroneBox, 5, false);
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        return panel.gameObject;
    }

    private static RectTransform CreateShopSection(string name, RectTransform parent, float top, float height)
    {
        RectTransform section = CreateRect(name, parent);
        section.anchorMin = new Vector2(0f, 1f);
        section.anchorMax = new Vector2(1f, 1f);
        section.pivot = new Vector2(0.5f, 1f);
        section.anchoredPosition = new Vector2(0f, -top);
        section.sizeDelta = new Vector2(0f, height);
        return section;
    }

    private static void CreateShopSectionHeader(RectTransform parent, string title)
    {
        GameObject header = CreateFrame("Title", parent, new Color32(203, 68, 74, 255), new Color32(255, 142, 137, 255), out _);
        RectTransform headerRect = header.GetComponent<RectTransform>();
        Stretch(headerRect, new Vector2(0f, 1f), Vector2.one, new Vector2(28f, -95f), new Vector2(-28f, -10f));
        TMP_Text titleText = CreateText("Label", headerRect, title, 44f, Color.white, TextAlignmentOptions.Center);
        Stretch(titleText.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 4f), new Vector2(-10f, -4f));
    }

    private static ShopOfferView CreateShopOfferCard(
        RectTransform parent,
        string name,
        float minX,
        float maxX,
        string title,
        string quantity,
        string price,
        string iconName,
        Color fillColor,
        bool showDiscount,
        float topInset = 105f)
    {
        GameObject card = CreateFrame(name, parent, fillColor, new Color32(143, 239, 224, 255), out Image cardBackground);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        Stretch(cardRect, new Vector2(minX, 0f), new Vector2(maxX, 1f), new Vector2(5f, 8f), new Vector2(-5f, -topInset));
        cardBackground.raycastTarget = true;
        Button button = card.AddComponent<Button>();
        button.targetGraphic = cardBackground;

        TMP_Text titleText = CreateText("Title", cardRect, title, 29f, Color.white, TextAlignmentOptions.Center);
        Stretch(titleText.rectTransform, new Vector2(0.03f, 0.77f), new Vector2(0.97f, 0.98f), Vector2.zero, Vector2.zero);

        TMP_Text quantityText = CreateText("Quantity", cardRect, quantity, 34f, Color.white, TextAlignmentOptions.Center);
        Stretch(quantityText.rectTransform, new Vector2(0.15f, 0.52f), new Vector2(0.85f, 0.69f), Vector2.zero, Vector2.zero);

        CreateShopItemIcon(cardRect, iconName);

        GameObject pricePlate = CreateFrame("Price", cardRect, new Color32(52, 132, 166, 255), new Color32(124, 211, 232, 255), out _);
        RectTransform priceRect = pricePlate.GetComponent<RectTransform>();
        Stretch(priceRect, new Vector2(0.15f, 0.03f), new Vector2(0.85f, 0.24f), Vector2.zero, Vector2.zero);
        Image priceIcon = CreateIcon("CurrencyIcon", priceRect, "red-currency", 45f);
        Anchor(priceIcon.rectTransform, new Vector2(0.24f, 0.5f), Vector2.zero, new Vector2(43f, 43f));
        TMP_Text priceText = CreateText("Value", priceRect, price, 27f, Color.white, TextAlignmentOptions.Center);
        Stretch(priceText.rectTransform, new Vector2(0.3f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero);

        if (showDiscount)
        {
            GameObject badge = CreateFrame("DiscountBadge", cardRect, new Color32(86, 183, 174, 255), Cream, out _);
            RectTransform badgeRect = badge.GetComponent<RectTransform>();
            Anchor(badgeRect, new Vector2(0.88f, 0.92f), Vector2.zero, new Vector2(105f, 70f));
            TMP_Text badgeText = CreateText("Label", badgeRect, "70%\nDiscount", 18f, Color.white, TextAlignmentOptions.Center);
            Stretch(badgeText.rectTransform, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
        }

        return new ShopOfferView { button = button, priceText = priceText };
    }

    private static void CreateShopItemIcon(RectTransform parent, string iconName)
    {
        if (iconName == "red-currency")
        {
            Image gemIcon = CreateIcon("ItemIcon", parent, iconName, 82f);
            Anchor(gemIcon.rectTransform, new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(82f, 82f));
            return;
        }

        RectTransform chest = CreateRect("ItemIcon", parent);
        Anchor(chest, new Vector2(0.5f, 0.36f), Vector2.zero, new Vector2(112f, 90f));
        Color bodyColor = iconName == "chipset-box"
            ? new Color32(65, 195, 181, 255)
            : new Color32(47, 116, 210, 255);
        Image body = CreateImage("Body", chest, bodyColor, false);
        Stretch(body.rectTransform, new Vector2(0.08f, 0f), new Vector2(0.92f, 0.62f), Vector2.zero, Vector2.zero);
        AddOutline(body, Border, 3f);
        Image lid = CreateImage(
            "Lid",
            chest,
            new Color(bodyColor.r + 0.18f, bodyColor.g + 0.18f, bodyColor.b + 0.18f, 1f),
            false);
        Stretch(lid.rectTransform, new Vector2(0f, 0.55f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        AddOutline(lid, Border, 3f);
        Image lockPlate = CreateImage("Lock", chest, Cream, false);
        Anchor(lockPlate.rectTransform, new Vector2(0.5f, 0.31f), Vector2.zero, new Vector2(20f, 24f));
        AddOutline(lockPlate, Border, 2f);
    }

    private static void CreateGemPreviewCard(
        RectTransform parent,
        string name,
        float minX,
        float maxX,
        string quantity,
        string price)
    {
        GameObject card = CreateFrame(name, parent, new Color32(83, 179, 171, 255), Cream, out _);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        Stretch(cardRect, new Vector2(minX, 0f), new Vector2(maxX, 1f), new Vector2(5f, 20f), new Vector2(-5f, -110f));
        TMP_Text quantityText = CreateText("Quantity", cardRect, quantity, 38f, Color.white, TextAlignmentOptions.Center);
        Stretch(quantityText.rectTransform, new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);
        Image gemIcon = CreateIcon("GemIcon", cardRect, "red-currency", 100f);
        Anchor(gemIcon.rectTransform, new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(100f, 100f));
        TMP_Text priceText = CreateText("Price", cardRect, price, 30f, Color.white, TextAlignmentOptions.Center);
        Stretch(priceText.rectTransform, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.22f), Vector2.zero, Vector2.zero);
    }

    private static void ConfigureShopOffer(
        SerializedProperty property,
        ShopOfferView view,
        string id,
        string displayName,
        int price,
        ShopController.CurrencyType currency,
        ShopController.RewardType reward,
        int rewardAmount,
        bool oncePerDay)
    {
        GetRequiredRelativeProperty(property, "id").stringValue = id;
        GetRequiredRelativeProperty(property, "displayName").stringValue = displayName;
        GetRequiredRelativeProperty(property, "button").objectReferenceValue = view.button;
        GetRequiredRelativeProperty(property, "priceText").objectReferenceValue = view.priceText;
        GetRequiredRelativeProperty(property, "currency").enumValueIndex = (int)currency;
        GetRequiredRelativeProperty(property, "price").intValue = price;
        GetRequiredRelativeProperty(property, "reward").enumValueIndex = (int)reward;
        GetRequiredRelativeProperty(property, "rewardAmount").intValue = rewardAmount;
        GetRequiredRelativeProperty(property, "oncePerDay").boolValue = oncePerDay;
    }

    private static LabStatTooltip CreateStatDetailTooltip(RectTransform parent)
    {
        GameObject tooltipObj = CreateFrame(
            "StatDetailTooltip",
            parent,
            new Color32(11, 48, 62, 248),
            TealBorder,
            out Image tooltipBg);
        RectTransform tooltipRect = tooltipObj.GetComponent<RectTransform>();
        tooltipRect.anchorMin = new Vector2(0.5f, 1f);
        tooltipRect.anchorMax = new Vector2(0.5f, 1f);
        tooltipRect.pivot = new Vector2(0.5f, 1f);
        tooltipRect.anchoredPosition = new Vector2(0f, -250f);
        tooltipRect.sizeDelta = new Vector2(920f, 150f);

        RectTransform arrowRect = CreateRect("ArrowPointer", tooltipRect);
        arrowRect.anchorMin = new Vector2(0.5f, 1f);
        arrowRect.anchorMax = new Vector2(0.5f, 1f);
        arrowRect.pivot = new Vector2(0.5f, 0f);
        arrowRect.anchoredPosition = new Vector2(0f, -2f);
        arrowRect.sizeDelta = new Vector2(40f, 26f);
        Image arrowImage = arrowRect.gameObject.AddComponent<Image>();
        arrowImage.sprite = LoadTooltipPointerSprite();
        arrowImage.preserveAspect = true;
        arrowImage.raycastTarget = false;

        TMP_Text detailText = CreateText(
            "DetailText",
            tooltipRect,
            "",
            27f,
            Cream,
            TextAlignmentOptions.TopLeft);
        detailText.richText = true;
        detailText.lineSpacing = 10f;
        detailText.enableWordWrapping = true;
        Stretch(detailText.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);

        LabStatTooltip tooltipComp = tooltipObj.AddComponent<LabStatTooltip>();
        SerializedObject so = new SerializedObject(tooltipComp);
        so.FindProperty("panelRect").objectReferenceValue = tooltipRect;
        so.FindProperty("arrowPointer").objectReferenceValue = arrowRect;
        so.FindProperty("arrowPointerImage").objectReferenceValue = arrowImage;
        so.FindProperty("detailText").objectReferenceValue = detailText;
        so.ApplyModifiedPropertiesWithoutUndo();

        tooltipObj.SetActive(false);
        return tooltipComp;
    }

    private static GameObject CreateLabPanel(
        RectTransform parent,
        TMP_Text energyBalanceText,
        TMP_Text chipBalanceText,
        TMP_Text redChipBalanceText)
    {
        RectTransform panel = CreateRect("LabPanel", parent);
        Stretch(panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform topTabs = CreateRect("TopTabs", panel);
        topTabs.anchorMin = new Vector2(0f, 1f);
        topTabs.anchorMax = new Vector2(1f, 1f);
        topTabs.pivot = new Vector2(0.5f, 1f);
        topTabs.anchoredPosition = new Vector2(0f, -15f);
        topTabs.sizeDelta = new Vector2(0f, 120f);

        CreateTopTab(topTabs, "StatsButton", 0.03f, 0.48f, "STATS", true, false);
        CreateTopTab(topTabs, "BuildBodyButton", 0.52f, 0.97f, "BUILD BODY", false, true);

        RectTransform statsPanel = CreateRect("StatsPanel", panel);
        Stretch(statsPanel, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -150f));

        RectTransform grid = CreateRect("UpgradeGrid", statsPanel);
        grid.anchorMin = new Vector2(0.5f, 1f);
        grid.anchorMax = new Vector2(0.5f, 1f);
        grid.pivot = new Vector2(0.5f, 1f);
        grid.anchoredPosition = new Vector2(0f, -10f);
        grid.sizeDelta = new Vector2(960f, 950f);

        GridLayoutGroup gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 4;
        gridLayout.cellSize = new Vector2(216f, 218f);
        gridLayout.spacing = new Vector2(16f, 16f);
        gridLayout.padding = new RectOffset(24, 24, 12, 14);
        gridLayout.childAlignment = TextAnchor.UpperCenter;

        string[] itemNames = LabStatNames;
        string[] itemIconNames = LabStatSpriteNames;
        float[] itemWeights =
        {
            14f, 12f, 12f, 10f,
            8f, 10f, 8f, 8f,
            7f, 7f, 6f, 6f,
            5f, 5f, 4f, 4f
        };
        Color[] rarityBackgroundColors = LabRarityColors;
        SlotView[] slotViews = new SlotView[16];

        for (int i = 0; i < slotViews.Length; i++)
        {
            CreateUpgradeSlot(
                grid,
                i + 1,
                false,
                itemNames[i],
                itemIconNames[i],
                out slotViews[i]);
            slotViews[i].slotBackground.color = rarityBackgroundColors[i / 4];
        }

        GameObject upgradeRoot = CreateFrame("UpgradeButton", statsPanel, new Color32(86, 183, 107, 255), Cream, out Image upgradeBackground);
        RectTransform upgradeRect = upgradeRoot.GetComponent<RectTransform>();
        Anchor(upgradeRect, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(470f, 165f), new Vector2(0.5f, 0f));
        upgradeBackground.raycastTarget = true;
        Button upgradeButton = upgradeRoot.AddComponent<Button>();
        upgradeButton.targetGraphic = upgradeBackground;

        TMP_Text upgradeLabel = CreateText("UpgradeText", upgradeRect, "UPGRADE", 46f, Cream, TextAlignmentOptions.Center);
        Stretch(upgradeLabel.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 0.95f), Vector2.zero, Vector2.zero);

        TMP_Text priceText = CreateText("PriceText", upgradeRect, "300", 42f, Cream, TextAlignmentOptions.Center);
        Anchor(priceText.rectTransform, new Vector2(0.47f, 0.27f), new Vector2(-15f, 0f), new Vector2(180f, 55f));

        Image priceIcon = CreateIcon("CurrencyIcon", upgradeRect, "chip-currency", 60f);
        Anchor(priceIcon.rectTransform, new Vector2(0.72f, 0.27f), Vector2.zero, new Vector2(58f, 58f));

        TMP_Text resultText = CreateText(
            "RollResultText",
            statsPanel,
            "ROLL FOR A RANDOM UPGRADE",
            25f,
            new Color32(151, 240, 226, 255),
            TextAlignmentOptions.Center);
        Anchor(resultText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 235f), new Vector2(720f, 45f));

        RectTransform buildBodyPanel = CreateRect("BuildBodyPanel", panel);
        Stretch(buildBodyPanel, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -150f));
        buildBodyPanel.gameObject.SetActive(false);

        LabStatTooltip statTooltip = CreateStatDetailTooltip(statsPanel);

        LabUpgradeController controller = panel.gameObject.AddComponent<LabUpgradeController>();
        SerializedObject serializedController = new SerializedObject(controller);
        GetRequiredProperty(serializedController, "upgradeButton").objectReferenceValue = upgradeButton;
        GetRequiredProperty(serializedController, "energyBalanceText").objectReferenceValue = energyBalanceText;
        GetRequiredProperty(serializedController, "chipBalanceText").objectReferenceValue = chipBalanceText;
        GetRequiredProperty(serializedController, "redChipBalanceText").objectReferenceValue = redChipBalanceText;
        GetRequiredProperty(serializedController, "priceText").objectReferenceValue = priceText;
        GetRequiredProperty(serializedController, "resultText").objectReferenceValue = resultText;
        GetRequiredProperty(serializedController, "upgradeBackground").objectReferenceValue = upgradeBackground;
        GetRequiredProperty(serializedController, "statTooltip").objectReferenceValue = statTooltip;
        GetRequiredProperty(serializedController, "lockIconSprite").objectReferenceValue = LoadLockIconSprite();
        GetRequiredProperty(serializedController, "commonLevelColor").colorValue = new Color32(255, 233, 92, 255);
        GetRequiredProperty(serializedController, "eliteLevelColor").colorValue = new Color32(255, 240, 106, 255);
        GetRequiredProperty(serializedController, "epicLevelColor").colorValue = new Color32(255, 244, 184, 255);
        GetRequiredProperty(serializedController, "legendLevelColor").colorValue = new Color32(22, 50, 79, 255);

        SerializedProperty items = GetRequiredProperty(serializedController, "items");
        items.arraySize = slotViews.Length;
        for (int i = 0; i < slotViews.Length; i++)
        {
            SerializedProperty item = items.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("itemName").stringValue = itemNames[i];
            item.FindPropertyRelative("itemIcon").objectReferenceValue = slotViews[i].iconImage.sprite;
            item.FindPropertyRelative("rarity").enumValueIndex = i / 4;
            item.FindPropertyRelative("dropWeight").floatValue = itemWeights[i];
            item.FindPropertyRelative("startsUnlocked").boolValue = false;
            item.FindPropertyRelative("startingLevel").intValue = 1;
            item.FindPropertyRelative("lockedGroup").objectReferenceValue = slotViews[i].lockedGroup;
            item.FindPropertyRelative("unlockedGroup").objectReferenceValue = slotViews[i].unlockedGroup;
            item.FindPropertyRelative("iconImage").objectReferenceValue = slotViews[i].iconImage;
            item.FindPropertyRelative("levelText").objectReferenceValue = slotViews[i].levelText;
            item.FindPropertyRelative("nameText").objectReferenceValue = slotViews[i].nameText;
            item.FindPropertyRelative("slotBackground").objectReferenceValue = slotViews[i].slotBackground;
            item.FindPropertyRelative("slotButton").objectReferenceValue = slotViews[i].slotButton;
        }

        serializedController.ApplyModifiedPropertiesWithoutUndo();

        return panel.gameObject;
    }

    private static void CreateTopTab(
        RectTransform parent,
        string name,
        float minX,
        float maxX,
        string label,
        bool selected,
        bool locked)
    {
        GameObject root = CreateFrame(
            name,
            parent,
            selected ? BrightTeal : new Color32(20, 74, 72, 235),
            selected ? Cream : Border,
            out Image background);
        RectTransform rect = root.GetComponent<RectTransform>();
        Stretch(rect, new Vector2(minX, 0f), new Vector2(maxX, 1f), Vector2.zero, Vector2.zero);

        TMP_Text text = CreateText("Label", rect, label, 46f, selected ? Cream : new Color32(38, 94, 92, 255), TextAlignmentOptions.Center);
        Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 5f), new Vector2(-10f, -5f));

        background.raycastTarget = !locked;
        Button button = root.AddComponent<Button>();
        button.targetGraphic = background;
        button.interactable = !locked;

        if (locked)
        {
            Image lockIcon = CreateIcon("LockIcon", rect, "lock", 72f);
            Anchor(lockIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(72f, 72f));
        }
    }

    private static void CreateUpgradeSlot(
        RectTransform parent,
        int index,
        bool unlocked,
        string itemName,
        string itemIconName,
        out SlotView view)
    {
        GameObject slot = CreateFrame(
            $"Slot{index:00}",
            parent,
            unlocked ? PanelSelected : Panel,
            unlocked ? Cream : TealBorder,
            out Image slotBackground);
        RectTransform slotRect = slot.GetComponent<RectTransform>();

        slotBackground.raycastTarget = true;
        Button slotButton = slot.AddComponent<Button>();
        slotButton.targetGraphic = slotBackground;

        RectTransform lockedGroup = CreateRect("LockedGroup", slotRect);
        Stretch(lockedGroup, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image lockIcon = CreateImage("LockIcon", lockedGroup, Color.white, false);
        lockIcon.sprite = LoadLockIconSprite();
        lockIcon.preserveAspect = true;
        Anchor(lockIcon.rectTransform, new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(100f, 120f));
        TMP_Text lockedText = CreateText("LockedText", lockedGroup, "LOCKED", 27f, Cream, TextAlignmentOptions.Center);
        Anchor(lockedText.rectTransform, new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(190f, 44f));

        RectTransform unlockedGroup = CreateRect("UnlockedGroup", slotRect);
        Stretch(unlockedGroup, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Color levelColor = (index / 4) switch
        {
            0 => new Color32(255, 233, 92, 255), // #FFE95C
            1 => new Color32(255, 240, 106, 255), // #FFF06A
            2 => new Color32(255, 244, 184, 255), // #FFF4B8
            _ => new Color32(22, 50, 79, 255)     // #16324F
        };
        TMP_Text levelText = CreateText("LevelText", unlockedGroup, "LV.01", 29f, levelColor, TextAlignmentOptions.Center);
        Anchor(levelText.rectTransform, new Vector2(0.5f, 0.81f), Vector2.zero, new Vector2(180f, 44f));
        Image itemIcon = CreateImage("ItemIcon", unlockedGroup, Color.white, false);
        itemIcon.sprite = LoadStatSprite(itemIconName);
        itemIcon.preserveAspect = true;
        Anchor(itemIcon.rectTransform, new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(140f, 140f));

        // Tên đã được vẽ sẵn trong từng card của Chiso.png. Vẫn giữ TMP_Text làm
        // reference dữ liệu cho LabUpgradeController, nhưng không vẽ đè tên lần hai.
        TMP_Text statName = CreateText("ItemName", unlockedGroup, itemName, 1f, Color.clear, TextAlignmentOptions.Center);
        statName.gameObject.SetActive(false);

        lockedGroup.gameObject.SetActive(!unlocked);
        unlockedGroup.gameObject.SetActive(unlocked);

        view = new SlotView
        {
            lockedGroup = lockedGroup.gameObject,
            unlockedGroup = unlockedGroup.gameObject,
            iconImage = itemIcon,
            levelText = levelText,
            nameText = statName,
            slotBackground = slotBackground,
            slotButton = slotButton
        };
    }

    private static GameObject CreatePlaceholderPanel(RectTransform parent, string name, string title, string subtitle)
    {
        RectTransform panel = CreateRect(name, parent);
        Stretch(panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject card = CreateFrame("ComingSoonCard", panel, new Color32(24, 75, 83, 230), TealBorder, out _);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        Anchor(cardRect, new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(720f, 300f));

        TMP_Text heading = CreateText("Title", cardRect, title, 64f, Cream, TextAlignmentOptions.Center);
        Stretch(heading.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 0.9f), Vector2.zero, Vector2.zero);
        TMP_Text note = CreateText("Subtitle", cardRect, subtitle, 28f, new Color32(150, 216, 210, 255), TextAlignmentOptions.Center);
        Stretch(note.rectTransform, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.48f), Vector2.zero, Vector2.zero);
        return panel.gameObject;
    }

    private static BottomNavigationController CreateBottomNavigation(GameObject canvasObject, RectTransform canvas, GameObject[] panels, int defaultTab = 0)
    {
        RectTransform nav = CreateRect("BottomNavigation", canvas);
        Stretch(nav, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 220f));
        Image navTint = nav.gameObject.AddComponent<Image>();
        navTint.color = new Color32(5, 35, 53, 242);

        string[] names = { "ShopButton", "LabButton", "ChapterButton", "ChipsetButton", "BuddyButton" };
        string[] labels = { "Shop", "Lab", "Chapter", "Chipset", "Buddy" };
        string[] icons = { "shop", "lab", "chapter", "chipset", "buddy" };
        Button[] buttons = new Button[5];
        Image[] backgrounds = new Image[5];
        Image[] borderImages = new Image[5];
        Image[] iconImages = new Image[5];
        TMP_Text[] labelTexts = new TMP_Text[5];

        for (int i = 0; i < 5; i++)
        {
            bool selected = i == defaultTab;
            GameObject root = CreateFrame(
                names[i],
                nav,
                selected ? BrightTeal : MutedTeal,
                selected ? Cream : new Color32(39, 105, 110, 255),
                out backgrounds[i]);
            borderImages[i] = root.GetComponent<Image>();
            RectTransform rect = root.GetComponent<RectTransform>();
            Stretch(rect, new Vector2(i * 0.2f, 0f), new Vector2((i + 1) * 0.2f, 1f), new Vector2(4f, 6f), new Vector2(-4f, -6f));

            backgrounds[i].raycastTarget = true;
            buttons[i] = root.AddComponent<Button>();
            buttons[i].targetGraphic = backgrounds[i];
            iconImages[i] = CreateIcon("Icon", rect, icons[i], 100f);
            Anchor(iconImages[i].rectTransform, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(102f, 102f));
            iconImages[i].color = selected ? Color.white : new Color32(54, 117, 124, 255);

            labelTexts[i] = CreateText("Label", rect, labels[i], 30f, selected ? Color.white : new Color32(54, 117, 124, 255), TextAlignmentOptions.Center);
            Anchor(labelTexts[i].rectTransform, new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(190f, 48f));
        }

        BottomNavigationController controller = canvasObject.AddComponent<BottomNavigationController>();
        SerializedObject serializedController = new SerializedObject(controller);
        SerializedProperty items = GetRequiredProperty(serializedController, "items");
        items.arraySize = 5;

        for (int i = 0; i < 5; i++)
        {
            SerializedProperty item = items.GetArrayElementAtIndex(i);
            GetRequiredRelativeProperty(item, "name").stringValue = labels[i];
            GetRequiredRelativeProperty(item, "button").objectReferenceValue = buttons[i];
            GetRequiredRelativeProperty(item, "panel").objectReferenceValue = panels[i];
            GetRequiredRelativeProperty(item, "buttonImage").objectReferenceValue = backgrounds[i];
            GetRequiredRelativeProperty(item, "background").objectReferenceValue = backgrounds[i];
            GetRequiredRelativeProperty(item, "icon").objectReferenceValue = iconImages[i];
            GetRequiredRelativeProperty(item, "label").objectReferenceValue = labelTexts[i];
        }

        GetRequiredProperty(serializedController, "defaultSelectedIndex").intValue = defaultTab;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        return controller;
    }

    private static GameObject CreateFrame(
        string name,
        Transform parent,
        Color fillColor,
        Color borderColor,
        out Image background)
    {
        RectTransform root = CreateRect(name, parent);
        Image borderImage = root.gameObject.AddComponent<Image>();
        borderImage.color = borderColor;
        borderImage.raycastTarget = false;
        Shadow shadow = root.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(0, 14, 24, 210);
        shadow.effectDistance = new Vector2(7f, -8f);
        shadow.useGraphicAlpha = true;

        background = CreateImage("Background", root, fillColor, false);
        Stretch(background.rectTransform, Vector2.zero, Vector2.one, new Vector2(7f, 7f), new Vector2(-7f, -7f));

        Image topHighlight = CreateImage("TopHighlight", root, new Color32(151, 240, 226, 120), false);
        Stretch(topHighlight.rectTransform, new Vector2(0.04f, 0.9f), new Vector2(0.96f, 0.94f), Vector2.zero, Vector2.zero);
        return root.gameObject;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        return rect;
    }

    private static Image CreateImage(string name, Transform parent, Color color, bool raycast)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    private static Image CreateIcon(string name, Transform parent, string spriteName, float size)
    {
        Image image = CreateImage(name, parent, Color.white, false);
        image.sprite = LoadIcon(spriteName);
        image.preserveAspect = true;
        image.rectTransform.sizeDelta = new Vector2(size, size);
        return image;
    }

    private static Image CreateChipsetIcon(string name, Transform parent, string spriteName, float size)
    {
        Image image = CreateImage(name, parent, Color.white, false);
        image.sprite = LoadChipsetSprite(spriteName);
        image.preserveAspect = true;
        image.rectTransform.sizeDelta = new Vector2(size, size);
        return image;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset activeFont = font;
        if (activeFont == null)
        {
            activeFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito/Nunito SDF.asset")
                ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        }
        if (activeFont != null) text.font = activeFont;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.outlineColor = Color.black;
        text.outlineWidth = 0.2f;
        return text;
    }

    private static Sprite LoadIcon(string spriteName)
    {
        return AssetDatabase.LoadAllAssetRepresentationsAtPath(IconAtlasPath)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == spriteName);
    }

    private static Sprite LoadLockIconSprite()
    {
        string path = "Assets/Sprites/UI/Lab/Extracted/Icon_Locked.png";
        if (File.Exists(path))
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;
        }

        return AssetDatabase.LoadAllAssetRepresentationsAtPath("Assets/Sprites/UI/Lab/nút màn lab 1.png")
            .OfType<Sprite>()
            .FirstOrDefault(s => s.name == "Locked");
    }

    private static Sprite LoadTooltipPointerSprite()
    {
        string path = "Assets/Sprites/UI/Lab/Extracted/Tooltip_Pointer.png";
        if (File.Exists(path))
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return null;
    }

    private static Sprite LoadStatSprite(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return null;

        string clean = spriteName.Replace("-", "_").Replace(" ", "_");

        string[] candidates = new string[]
        {
            $"Assets/Sprites/UI/Lab/Extracted/{clean}.png",
            $"Assets/Sprites/UI/Lab/Extracted/{spriteName}.png",
            $"Assets/Sprites/UI/Lab/Extracted/{clean.ToUpperInvariant()}.png",
            $"Assets/Sprites/UI/Lab/Extracted/{spriteName.ToUpperInvariant()}.png"
        };

        foreach (string path in candidates)
        {
            if (File.Exists(path))
            {
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null) return s;
            }
        }

        Sprite fromLab1 = AssetDatabase.LoadAllAssetRepresentationsAtPath("Assets/Sprites/UI/Lab/nút màn lab 1.png")
            .OfType<Sprite>()
            .FirstOrDefault(s => string.Equals(s.name, spriteName, StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(s.name.Replace(" ", "").Replace("-", "").Replace("_", ""), clean.Replace("_", ""), StringComparison.OrdinalIgnoreCase));
        if (fromLab1 != null) return fromLab1;

        if (File.Exists(StatSpriteSheetPath))
        {
            Sprite candidate = AssetDatabase.LoadAllAssetRepresentationsAtPath(StatSpriteSheetPath)
                .OfType<Sprite>()
                .FirstOrDefault(s => string.Equals(s.name, spriteName, StringComparison.OrdinalIgnoreCase));
            if (candidate != null) return candidate;
        }

        return null;
    }

    private static Sprite LoadChipsetSprite(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return null;

        if (cachedChipsetSprites == null || cachedChipsetSprites.Length == 0)
        {
            CacheChipsetSprites();
        }

        string cleanName = spriteName.Replace(" ", "").Replace("-", "").Replace("_", "").ToLowerInvariant();

        // 1. Khớp chính xác tên trong cache
        Sprite match = cachedChipsetSprites?.FirstOrDefault(sprite => 
            sprite != null && (
                string.Equals(sprite.name, spriteName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sprite.name.Replace(" ", "").Replace("-", "").Replace("_", "").ToLowerInvariant(), cleanName)
            )
        );
        if (match != null) return match;

        // 2. Ánh xạ số cho các icon trong icon chipset (1..10)
        string numKey = null;
        if (cleanName.Contains("highexplosive") || cleanName.Contains("mine") && !cleanName.Contains("blackhole") && !cleanName.Contains("biochemical")) numKey = "1";
        else if (cleanName.Contains("energyjumper") || cleanName.Contains("jumpercable")) numKey = "2";
        else if (cleanName.Contains("shotgun")) numKey = "3";
        else if (cleanName.Contains("spiky") || cleanName.Contains("discus") || cleanName.Contains("spicky")) numKey = "4";
        else if (cleanName.Contains("gunturret") || cleanName.Equals("turret")) numKey = "5";
        else if (cleanName.Contains("multigun")) numKey = "6";
        else if (cleanName.Contains("spinningblade") || cleanName.Contains("blade")) numKey = "7";
        else if (cleanName.Contains("rocketpunch") || cleanName.Contains("punch")) numKey = "8";
        else if (cleanName.Contains("standardgun") || cleanName.Equals("gun") || cleanName.Equals("pistol")) numKey = "9";
        else if (cleanName.Contains("rifle") || cleanName.Contains("assault")) numKey = "10";

        if (!string.IsNullOrEmpty(numKey))
        {
            match = cachedChipsetSprites?.FirstOrDefault(s => s != null && s.name == numKey);
            if (match != null) return match;
        }

        // 3. Ánh xạ khung thẻ Bậc 1-5
        if (cleanName.Contains("green") || cleanName.Contains("tier1") || cleanName.Contains("magic") || cleanName.Contains("common"))
        {
            match = cachedChipsetSprites?.FirstOrDefault(s => s != null && (s.name == "Green" || s.name == "card-frame-tier1-green"));
            if (match != null) return match;
        }
        else if (cleanName.Contains("blue") || cleanName.Contains("tier2") || cleanName.Contains("rare") || cleanName.Contains("blu"))
        {
            match = cachedChipsetSprites?.FirstOrDefault(s => s != null && (s.name == "Blu" || s.name == "card-frame-tier2-blue"));
            if (match != null) return match;
        }
        else if (cleanName.Contains("purple") || cleanName.Contains("tier3") || cleanName.Contains("unique") || cleanName.Contains("tim") || cleanName.Contains("tím"))
        {
            match = cachedChipsetSprites?.FirstOrDefault(s => s != null && (s.name == "Tím" || s.name.Contains("T") && s.name.Contains("m") || s.name == "card-frame-tier3-purple"));
            if (match != null) return match;
        }
        else if (cleanName.Contains("yellow") || cleanName.Contains("tier4") || cleanName.Contains("epic") || cleanName.Contains("yello") || cleanName.Contains("gold"))
        {
            match = cachedChipsetSprites?.FirstOrDefault(s => s != null && (s.name == "Yello" || s.name == "card-frame-tier4-yellow"));
            if (match != null) return match;
        }
        else if (cleanName.Contains("holo") || cleanName.Contains("rainbow") || cleanName.Contains("tier5") || cleanName.Contains("red"))
        {
            match = cachedChipsetSprites?.FirstOrDefault(s => s != null && (s.name == "card-frame-tier5-holographic" || s.name == "Red" || s.name == "card-frame-tier5-red"));
            if (match != null) return match;
        }

        // 4. Kiểm tra file riêng lẻ
        string rootPath = $"Assets/Sprites/UI/Chipset/{spriteName}.png";
        if (File.Exists(rootPath))
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(rootPath);
            if (s != null) return s;
        }
        string locksPath = $"Assets/Resources/UI/Chipset/Locks/{spriteName}.png";
        if (File.Exists(locksPath))
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(locksPath);
            if (s != null) return s;
        }
        string framePath = $"Assets/Sprites/UI/Chipset/Frames/{spriteName}.png";
        if (File.Exists(framePath))
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
            if (s != null) return s;
        }
        string iconPath = $"Assets/Sprites/UI/Chipset/Icons/{spriteName}.png";
        if (File.Exists(iconPath))
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (s != null) return s;
        }

        return null;
    }

    private static void AddOutline(Graphic graphic, Color color, float size)
    {
        Outline outline = graphic.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(size, size);
        outline.useGraphicAlpha = true;
    }

    private static void Stretch(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
    }

    private static void Anchor(
        RectTransform rect,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 size,
        Vector2? pivot = null)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void UpdateBuildSettings()
    {
        EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
        EditorBuildSettingsScene gameplay = existingScenes.FirstOrDefault(
            scene => string.Equals(scene.path, "Assets/Scenes/GamePlay.unity", StringComparison.OrdinalIgnoreCase));

        EditorBuildSettings.scenes = gameplay == null
            ? new[] { new EditorBuildSettingsScene(ScenePath, true) }
            : new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(gameplay.path, gameplay.enabled)
            };
    }

    private static void CapturePreview(Canvas canvas, Camera camera)
    {
        const int width = 540;
        const int height = 960;
        RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D preview = new Texture2D(width, height, TextureFormat.RGBA32, false);
        RenderTexture previousActive = RenderTexture.active;

        try
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            camera.targetTexture = renderTexture;
            camera.aspect = width / (float)height;

            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = renderTexture;
            preview.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            preview.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(ChipsetPreviewPath) ?? "Assets/UI/Chipset/Generated");
            File.WriteAllBytes(ChipsetPreviewPath, preview.EncodeToPNG());
            File.WriteAllBytes(PreviewPath, preview.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            canvas.worldCamera = null;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            RenderTexture.active = previousActive;
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(preview);
        }

        AssetDatabase.ImportAsset(ChipsetPreviewPath, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(PreviewPath, ImportAssetOptions.ForceSynchronousImport);
    }

    private static SerializedProperty GetRequiredProperty(SerializedObject so, string propertyName)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            throw new InvalidOperationException($"[LabMenuSceneBuilder] Required SerializedProperty '{propertyName}' not found on target '{so.targetObject.GetType().Name}'.");
        }
        return prop;
    }

    private static SerializedProperty GetRequiredRelativeProperty(SerializedProperty parentProp, string relativePath)
    {
        SerializedProperty prop = parentProp.FindPropertyRelative(relativePath);
        if (prop == null)
        {
            throw new InvalidOperationException($"[LabMenuSceneBuilder] Required RelativeProperty '{relativePath}' not found on parent '{parentProp.name}'.");
        }
        return prop;
    }
}
#endif
