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
    private const string AppliedKey = "PGE.BuddyScreenReferenceApplier.v9";

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

    [MenuItem("PGE/UI/Apply Buddy Progress Fill in Scene")]
    public static void ApplyProgressFillFromMenu()
    {
        EditorPrefs.DeleteKey(AppliedKey);
        ApplyOnce();
    }

    [MenuItem("PGE/UI/Rebuild Equipped Slots From Template")]
    public static void RebuildEquippedSlotsFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            var controllers = UnityEngine.Object.FindObjectsOfType<BuddyController>(true);
            foreach (var c in controllers)
            {
                c.EnsureEquippedSlotsMatchTemplate();
                c.RefreshEquippedGrid();
            }
            Debug.Log($"[BuddyScreenReferenceApplier] Đã rebuild {controllers.Length} EquippedSlots trong Play mode.");
            return;
        }

        EditorPrefs.DeleteKey(AppliedKey);
        ApplyOnce();
    }

    private static void ApplyOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorPrefs.GetBool(AppliedKey, false)) return;

        AssetDatabase.ImportAsset(IconSheetPath, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(ButtonSheetPath, ImportAssetOptions.ForceSynchronousImport);

        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Dictionary<string, Sprite> sourceIcons = LoadSprites(IconSheetPath);
        Dictionary<string, Sprite> sourceButtons = LoadSprites(ButtonSheetPath);

        string[] requiredIconSprites = { "openLocke", "Locke", "drone-snowflake", "drone-spider", "drone-antenna-eye", "drone-cross-visor", "drone-stealth-wing" };
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
                controller.AutoWireSlotIconBuddyIfMissing();
                controller.AutoWireDetailModalReferencesIfMissing();
                controller.InitializeDatabase();
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
            "drone-snowflake", "drone-spider", "drone-antenna-eye", "drone-cross-visor", "drone-stealth-wing",
            "drone-snowflake", "drone-spider", "drone-antenna-eye", "drone-cross-visor", "drone-stealth-wing",
            "drone-cross-visor", "drone-antenna-eye"
        };
        SerializedProperty icons = serialized.FindProperty("droneIcons");
        icons.arraySize = iconKeys.Length;
        for (int i = 0; i < iconKeys.Length; i++)
        {
            sourceIcons.TryGetValue(iconKeys[i], out Sprite sprite);
            icons.GetArrayElementAtIndex(i).objectReferenceValue = sprite;
        }

        // Frame Sprites: 6 tiers: Common (Green), Magic (Blue), Rare (Purple), Unique (Yellow), Epic (Yellow), Holographic (Red)
        Sprite frameGreen = sourceIcons.TryGetValue("openLocke", out var g) ? g : null;
        Sprite frameBlue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Buddy/openLocke_Blue.png") ?? frameGreen;
        Sprite framePurple = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Buddy/openLocke_Purple.png") ?? frameGreen;
        Sprite frameYellow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Buddy/openLocke_Yellow.png") ?? frameGreen;
        Sprite frameRed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Buddy/openLocke_Red.png") ?? frameGreen;

        SerializedProperty frames = serialized.FindProperty("frameSprites");
        frames.arraySize = 6;
        frames.GetArrayElementAtIndex(0).objectReferenceValue = frameGreen;
        frames.GetArrayElementAtIndex(1).objectReferenceValue = frameBlue;
        frames.GetArrayElementAtIndex(2).objectReferenceValue = framePurple;
        frames.GetArrayElementAtIndex(3).objectReferenceValue = frameYellow;
        frames.GetArrayElementAtIndex(4).objectReferenceValue = frameYellow;
        frames.GetArrayElementAtIndex(5).objectReferenceValue = frameRed;

        serialized.FindProperty("emptySlotFrameSprite").objectReferenceValue = sourceButtons["Empty"];
        serialized.FindProperty("lockedSlotFrameSprite").objectReferenceValue = sourceIcons["Locke"];
        serialized.FindProperty("upgradeArrowSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Chipset/Frames/badge-upgrade.png");

        // Tier Lock Sprites
        string chipsetPath = "Assets/Sprites/UI/Chipset/khung chipset.png";
        var csSprites = AssetDatabase.LoadAllAssetsAtPath(chipsetPath).OfType<Sprite>().ToArray();
        SerializedProperty lockTierProps = serialized.FindProperty("lockTierSprites");
        lockTierProps.arraySize = 4;
        lockTierProps.GetArrayElementAtIndex(0).objectReferenceValue = csSprites?.FirstOrDefault(s => s.name.Equals("Lock_Blue", StringComparison.OrdinalIgnoreCase));
        lockTierProps.GetArrayElementAtIndex(1).objectReferenceValue = csSprites?.FirstOrDefault(s => s.name.Equals("Lock_Purple", StringComparison.OrdinalIgnoreCase));
        lockTierProps.GetArrayElementAtIndex(2).objectReferenceValue = csSprites?.FirstOrDefault(s => s.name.Equals("Lock_Yellow", StringComparison.OrdinalIgnoreCase));
        lockTierProps.GetArrayElementAtIndex(3).objectReferenceValue = csSprites?.FirstOrDefault(s => s.name.Equals("Lock_Red", StringComparison.OrdinalIgnoreCase));

        SetImageSprite(serialized.FindProperty("droneModeBg").objectReferenceValue as Image, sourceButtons["Drone"]);
        SetImageSprite(serialized.FindProperty("robotPetModeBg").objectReferenceValue as Image, sourceButtons["Robot Pet On"]);

        SerializedProperty pUnlocked = serialized.FindProperty("robotPetUnlockedSprite");
        if (pUnlocked != null)
        {
            sourceButtons.TryGetValue("Robot Pet On", out Sprite rOn);
            pUnlocked.objectReferenceValue = rOn ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Buddy/RobotPet_Sliced/Tab_RobotPet.png");
        }
        SerializedProperty pLocked = serialized.FindProperty("robotPetLockedSprite");
        if (pLocked != null)
        {
            sourceButtons.TryGetValue("Robot Pet OFF", out Sprite rOff);
            pLocked.objectReferenceValue = rOff;
        }

        if (sourceIcons.TryGetValue("1", out Sprite s1)) SetImageSprite(serialized.FindProperty("preset1Bg").objectReferenceValue as Image, s1);
        if (sourceIcons.TryGetValue("2", out Sprite s2)) SetImageSprite(serialized.FindProperty("preset2Bg").objectReferenceValue as Image, s2);
        if (sourceIcons.TryGetValue("3", out Sprite s3)) SetImageSprite(serialized.FindProperty("preset3Bg").objectReferenceValue as Image, s3);

        ClearText(serialized.FindProperty("preset1Text").objectReferenceValue as TMP_Text);
        ClearText(serialized.FindProperty("preset2Text").objectReferenceValue as TMP_Text);
        ClearText(serialized.FindProperty("preset3Text").objectReferenceValue as TMP_Text);

        // Update Drone 1 in allBuddies to Sloy (Common, LV.8, 0/10, 3500 cost)
        SerializedProperty allBuddiesProp = serialized.FindProperty("allBuddies");
        if (allBuddiesProp != null && allBuddiesProp.arraySize > 0)
        {
            SerializedProperty drone0 = allBuddiesProp.GetArrayElementAtIndex(0);
            drone0.FindPropertyRelative("buddyName").stringValue = "Sloy";
            drone0.FindPropertyRelative("iconKey").stringValue = "drone-snowflake";
            drone0.FindPropertyRelative("tier").enumValueIndex = (int)BuddyTier.Common;
            drone0.FindPropertyRelative("level").intValue = 8;
            drone0.FindPropertyRelative("count").intValue = 0;
            drone0.FindPropertyRelative("requiredCount").intValue = 10;
            drone0.FindPropertyRelative("enhanceCost").intValue = 3500;
            drone0.FindPropertyRelative("description").stringValue = "Fires shells that slow down enemies.";
            drone0.FindPropertyRelative("baseStatText").stringValue = "Drone ATK 20.4, Slow ATK Speed";
            for (int b = 0; b < allBuddiesProp.arraySize; b++)
            {
                SerializedProperty droneProp = allBuddiesProp.GetArrayElementAtIndex(b);
                int bId = droneProp.FindPropertyRelative("id").intValue;
                if (bId == 11 || bId == 12 || (bId >= 5 && bId <= 9))
                {
                    droneProp.FindPropertyRelative("tier").enumValueIndex = (int)BuddyTier.Common;
                    droneProp.FindPropertyRelative("count").intValue = 0;
                }
            }
        }

        PlayerPrefs.DeleteKey("PGE.Buddy.Tier.11");
        PlayerPrefs.DeleteKey("PGE.Buddy.Tier.12");
        PlayerPrefs.DeleteKey("PGE.Buddy.Count.11");
        PlayerPrefs.DeleteKey("PGE.Buddy.Count.12");
        PlayerPrefs.Save();

        // Auto-wire EquippedRow (3 slots) using exact clone of Buddy Card template
        controller.EnsureEquippedSlotsMatchTemplate();
        Transform equippedRow = FindDeep(controller.transform, "EquippedRow");
        if (equippedRow != null)
        {
            SerializedProperty equippedSlotsProp = serialized.FindProperty("equippedSlots");
            equippedSlotsProp.arraySize = 3;

            for (int i = 0; i < 3; i++)
            {
                Transform slotT = equippedRow.Find($"EquippedSlot_{i}");
                if (slotT == null && i < equippedRow.childCount) slotT = equippedRow.GetChild(i);
                if (slotT != null)
                {
                    BuddyCardUI card = slotT.GetComponent<BuddyCardUI>() ?? slotT.gameObject.AddComponent<BuddyCardUI>();
                    Button btn = slotT.GetComponent<Button>() ?? slotT.gameObject.AddComponent<Button>();
                    card.EnsureProgressBar();
                    equippedSlotsProp.GetArrayElementAtIndex(i).objectReferenceValue = card;
                }
            }
        }

        Transform slotIconBuddy = FindDeep(controller.transform, "SlotIconBuddy");
        if (slotIconBuddy != null)
        {
            serialized.FindProperty("slotIconBuddyContainer").objectReferenceValue = slotIconBuddy;
        }

        // Auto-wire BuddyDetailModal
        Transform detailModalT = FindDeep(controller.transform, "BuddyDetailModal") ?? FindDeep(controller.transform.root, "BuddyDetailModal");
        if (detailModalT != null)
        {
            serialized.FindProperty("detailModal").objectReferenceValue = detailModalT.gameObject;
            Transform modalBox = detailModalT.Find("ModalBox") ?? detailModalT;

            // TopCard / IconFrame
            Transform topCardT = modalBox.Find("TopCard") ?? modalBox.Find("IconFrame");
            if (topCardT != null)
            {
                var oldChipset = topCardT.GetComponent<ChipsetCardUI>();
                if (oldChipset != null) UnityEngine.Object.DestroyImmediate(oldChipset);
                BuddyCardUI card = topCardT.GetComponent<BuddyCardUI>() ?? topCardT.gameObject.AddComponent<BuddyCardUI>();
                card.EnsureProgressBar();
                serialized.FindProperty("detailTopCard").objectReferenceValue = card;
            }

            // Texts
            Transform nameT = modalBox.Find("Name");
            if (nameT != null) serialized.FindProperty("detailNameText").objectReferenceValue = nameT.GetComponent<TMP_Text>();

            Transform tierT = modalBox.Find("Tier");
            if (tierT != null) serialized.FindProperty("detailTierText").objectReferenceValue = tierT.GetComponent<TMP_Text>();

            Transform descT = modalBox.Find("Description");
            if (descT != null) serialized.FindProperty("detailDescText").objectReferenceValue = descT.GetComponent<TMP_Text>();

            Transform statT = modalBox.Find("BaseStat") ?? modalBox.Find("StatsBox/StatText") ?? modalBox.Find("StatsBox");
            if (statT != null) serialized.FindProperty("detailBaseStatText").objectReferenceValue = statT.GetComponent<TMP_Text>();

            // Buttons
            Transform eqBtnT = modalBox.Find("EquipBtn");
            if (eqBtnT != null)
            {
                serialized.FindProperty("detailEquipBtn").objectReferenceValue = eqBtnT.GetComponent<Button>();
                serialized.FindProperty("detailEquipBtnText").objectReferenceValue = eqBtnT.GetComponentInChildren<TMP_Text>(true);
            }

            Transform enhBtnT = modalBox.Find("EnhanceBtn");
            if (enhBtnT != null)
            {
                serialized.FindProperty("detailEnhanceBtn").objectReferenceValue = enhBtnT.GetComponent<Button>();
                serialized.FindProperty("detailEnhanceCostText").objectReferenceValue = enhBtnT.Find("CostRow/CostValue")?.GetComponent<TMP_Text>()
                    ?? enhBtnT.Find("CostValue")?.GetComponent<TMP_Text>()
                    ?? enhBtnT.Find("Cost")?.GetComponent<TMP_Text>();
            }

            Transform advBtnT = modalBox.Find("AdvanceTierBtn");
            if (advBtnT != null)
            {
                serialized.FindProperty("detailAdvanceTierBtn").objectReferenceValue = advBtnT.GetComponent<Button>();
                serialized.FindProperty("detailAdvanceTierText").objectReferenceValue = advBtnT.Find("Label")?.GetComponent<TMP_Text>() ?? advBtnT.GetComponentInChildren<TMP_Text>(true);
            }

            Transform closeBtnT = modalBox.Find("CloseBtn") ?? modalBox.Find("Close");
            if (closeBtnT != null) serialized.FindProperty("detailCloseBtn").objectReferenceValue = closeBtnT.GetComponent<Button>();

            // Perk rows
            SerializedProperty perkIconsProp = serialized.FindProperty("perkRowIcons");
            SerializedProperty perkTextsProp = serialized.FindProperty("perkRowTexts");
            perkIconsProp.arraySize = 4;
            perkTextsProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                Transform rowT = modalBox.Find($"PerkRow_{i}");
                if (rowT != null)
                {
                    Transform iconT = rowT.Find("LockIcon");
                    if (iconT != null) perkIconsProp.GetArrayElementAtIndex(i).objectReferenceValue = iconT.GetComponent<Image>();
                    Transform textT = rowT.Find("PerkText");
                    if (textT != null) perkTextsProp.GetArrayElementAtIndex(i).objectReferenceValue = textT.GetComponent<TMP_Text>();
                }
            }
        }

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

        // Ensure TopCard has BuddyCardUI component if missing, preserving all RectTransform positions & sizes
        Transform detailModalT = FindDeep(controller.transform, "BuddyDetailModal") ?? FindDeep(controller.transform.root, "BuddyDetailModal");
        if (detailModalT != null)
        {
            Transform modalBox = detailModalT.Find("ModalBox") ?? detailModalT;
            HideChildren(modalBox, "ModBadge");
            Transform topCardT = modalBox.Find("TopCard") ?? modalBox.Find("IconFrame");
            if (topCardT != null)
            {
                var oldChipset = topCardT.GetComponent<ChipsetCardUI>();
                if (oldChipset != null) UnityEngine.Object.DestroyImmediate(oldChipset);
                BuddyCardUI cardUI = topCardT.GetComponent<BuddyCardUI>();
                if (cardUI == null) cardUI = topCardT.gameObject.AddComponent<BuddyCardUI>();
                cardUI.EnsureProgressBar();

                Transform iconT = topCardT.Find("NormalContentGroup/Icon") ?? topCardT.Find("Icon");
                if (iconT != null)
                {
                    Image iconImg = iconT.GetComponent<Image>();
                    if (iconImg != null && sourceIcons != null && sourceIcons.TryGetValue("drone-snowflake", out Sprite snowSprite))
                    {
                        SetImageSprite(iconImg, snowSprite);
                    }
                }
            }
        }

        // Wire the 5 card templates under SlotIconBuddy to their matching exact sprites
        Transform slotIconBuddy = FindDeep(controller.transform, "SlotIconBuddy");
        if (slotIconBuddy != null)
        {
            foreach (Transform cardT in slotIconBuddy)
            {
                string cardName = cardT.name;
                Transform iconT = cardT.Find("NormalContentGroup/Icon") ?? cardT.Find("NormalContentGroup/DroneIcon") ?? cardT.Find("Icon") ?? cardT.Find("DroneIcon");
                if (iconT != null)
                {
                    Image iconImg = iconT.GetComponent<Image>();
                    if (iconImg != null && sourceIcons != null && sourceIcons.TryGetValue(cardName, out Sprite s))
                    {
                        SetImageSprite(iconImg, s);
                    }
                }
            }
        }

        // Reset all Fill / ProgressFill images to Color.white so they display their native asset colors without yellow tint
        foreach (var img in controller.GetComponentsInChildren<Image>(true))
        {
            if (img.gameObject.name.Equals("Fill", StringComparison.OrdinalIgnoreCase) ||
                img.gameObject.name.Equals("ProgressFill", StringComparison.OrdinalIgnoreCase))
            {
                img.color = Color.white;
                EditorUtility.SetDirty(img);
            }
        }

        // Standardize all Buddy Card numbers (Level & Progress text) to white with black stroke outline
        TMP_FontAsset nunitoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito/Nunito SDF.asset");
        Material strokeMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/Nunito/Nunito SDF - Stroke.mat");

        foreach (var card in controller.GetComponentsInChildren<BuddyCardUI>(true))
        {
            SerializedObject sCard = new SerializedObject(card);
            var prgProp = sCard.FindProperty("progressText");
            TMP_Text prg = prgProp != null ? prgProp.objectReferenceValue as TMP_Text : null;
            if (prg == null)
            {
                Transform pt = card.transform.Find("Quantity") ?? card.transform.Find("NormalContentGroup/BottomBar/ProgressText");
                if (pt != null) prg = pt.GetComponent<TMP_Text>();
            }
            if (prg != null)
            {
                BuddyCardUI.ConfigureProgressText(prg);
                if (nunitoFont != null) prg.font = nunitoFont;
                if (strokeMaterial != null) prg.fontSharedMaterial = strokeMaterial;
                prg.color = Color.white;
                prg.fontSize = BuddyCardUI.StandardProgressFontSize;
                prg.rectTransform.localScale = Vector3.one;
                EditorUtility.SetDirty(prg);
            }

            var lvlProp = sCard.FindProperty("levelText");
            TMP_Text lvl = lvlProp != null ? lvlProp.objectReferenceValue as TMP_Text : null;
            if (lvl == null)
            {
                Transform lt = card.transform.Find("Level") ?? card.transform.Find("NormalContentGroup/LevelText");
                if (lt != null) lvl = lt.GetComponent<TMP_Text>();
            }
            if (lvl != null)
            {
                BuddyCardUI.ConfigureLevelText(lvl);
                if (nunitoFont != null) lvl.font = nunitoFont;
                if (strokeMaterial != null) lvl.fontSharedMaterial = strokeMaterial;
                lvl.color = Color.white;
                lvl.fontSize = BuddyCardUI.StandardLevelFontSize;
                lvl.rectTransform.localScale = Vector3.one;
                EditorUtility.SetDirty(lvl);
            }

            EditorUtility.SetDirty(card);
        }

        // Also check any standalone Level / Quantity under SlotIconBuddy or EquippedRow
        if (slotIconBuddy != null)
        {
            foreach (Transform droneTemplate in slotIconBuddy)
            {
                Transform lvlT = droneTemplate.Find("Level");
                if (lvlT != null)
                {
                    TMP_Text lvlText = lvlT.GetComponent<TMP_Text>();
                    if (lvlText != null)
                    {
                        BuddyCardUI.ConfigureLevelText(lvlText);
                        if (nunitoFont != null) lvlText.font = nunitoFont;
                        if (strokeMaterial != null) lvlText.fontSharedMaterial = strokeMaterial;
                        lvlText.color = Color.white;
                        lvlText.fontSize = BuddyCardUI.StandardLevelFontSize;
                        lvlT.localScale = Vector3.one;
                        EditorUtility.SetDirty(lvlText);
                        EditorUtility.SetDirty(lvlT);
                    }
                }

                Transform qtyT = droneTemplate.Find("Quantity");
                if (qtyT != null)
                {
                    TMP_Text qtyText = qtyT.GetComponent<TMP_Text>();
                    if (qtyText != null)
                    {
                        BuddyCardUI.ConfigureProgressText(qtyText);
                        if (nunitoFont != null) qtyText.font = nunitoFont;
                        if (strokeMaterial != null) qtyText.fontSharedMaterial = strokeMaterial;
                        qtyText.color = Color.white;
                        qtyText.fontSize = BuddyCardUI.StandardProgressFontSize;
                        qtyT.localScale = Vector3.one;
                        EditorUtility.SetDirty(qtyText);
                        EditorUtility.SetDirty(qtyT);
                    }
                }
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
        image.raycastTarget = true;
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
