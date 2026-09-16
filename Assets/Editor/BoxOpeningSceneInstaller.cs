#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BoxOpeningSceneInstaller
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string BoxFolder = "Assets/Sprites/UI/Shop/Boxes/";
    private const string BackgroundPath = "Assets/Sprites/Backround/màn chapter.png";
    private const string GreenFramePath = "Assets/Sprites/UI/Chipset/Frames/card-frame-tier1-green.png";
    private const string SunburstPath = "Assets/Sprites/UI/Artifact/sunburst_ray.png";
    private const string BurstCyanPath = "Assets/Sprites/UI/Shop/Extracted/VFX_Reward_Burst_Cyan.png";
    private const string BurstGlowPath = "Assets/Sprites/UI/Shop/Extracted/VFX_Reward_Burst_Glow.png";
    private const string BlankButtonPath = "Assets/Sprites/UI/Reward/Extracted/Btn_Blank_Cyan.png";
    private const string SmokePuffPath = "Assets/Sprites/UI/Shop/dot_white.png";
    private const string ResultButtonPath = "Assets/Sprites/UI/Reward/Extracted/Btn_Get.png";
    private const string FontPath = "Assets/Fonts/Nunito/Nunito SDF.asset";
    private const string RewardStrokeMaterialPath = "Assets/Fonts/Nunito/Nunito SDF - RewardStroke.mat";
    private const string StrokeMaterialPath = "Assets/Fonts/Nunito/Nunito SDF - Stroke.mat";
    [MenuItem("PGE/UI/Install Box Opening Flow")]
    public static void InstallFromMenu()
    {
        InstallIntoMainMenu();
    }

    public static void InstallIntoMainMenu()
    {
        Scene scene = SceneManager.GetSceneByPath(MainMenuScenePath);
        bool openedAdditively = !scene.IsValid() || !scene.isLoaded;
        if (openedAdditively) scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Additive);

        try
        {
            UnityEngine.UI.CanvasScaler canvasScaler = FindInScene<UnityEngine.UI.CanvasScaler>(scene);
            Canvas canvas = canvasScaler != null ? canvasScaler.GetComponent<Canvas>() : FindInScene<Canvas>(scene);
            ShopController[] shopControllers = FindAllInScene<ShopController>(scene);
            ShopController shopController = shopControllers.FirstOrDefault(s => s.gameObject.activeInHierarchy)
                                            ?? shopControllers.FirstOrDefault();
            if (canvas == null || shopController == null)
            {
                Debug.LogError("[BoxOpeningInstaller] MainMenu requires an existing Canvas and ShopController.");
                return;
            }

            Transform existing = FindTransformInScene(scene, "BoxOpeningSystem");
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Material rewardStrokeMaterial = AssetDatabase.LoadAssetAtPath<Material>(RewardStrokeMaterialPath);
            Material subtleStrokeMaterial = AssetDatabase.LoadAssetAtPath<Material>(StrokeMaterialPath);
            Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            Sprite greenFrame = AssetDatabase.LoadAssetAtPath<Sprite>(GreenFramePath);
            Sprite sunburst = AssetDatabase.LoadAssetAtPath<Sprite>(SunburstPath);
            Sprite burstCyan = AssetDatabase.LoadAssetAtPath<Sprite>(BurstCyanPath) ?? sunburst;
            Sprite burstGlow = AssetDatabase.LoadAssetAtPath<Sprite>(BurstGlowPath) ?? sunburst;
            Sprite blankButton = AssetDatabase.LoadAssetAtPath<Sprite>(BlankButtonPath);
            Sprite puffSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SmokePuffPath);
            Sprite buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ResultButtonPath);
            Sprite skipButtonSprite = blankButton != null ? blankButton : buttonSprite;

            GameObject driver = CreateRect("BoxOpeningSystem", canvas.transform);
            Stretch(driver.GetComponent<RectTransform>());
            BoxOpeningController controller = driver.AddComponent<BoxOpeningController>();

            // Dark cyan background with signature game monster pattern
            GameObject overlay = CreateImage("BoxOpeningCanvas", driver.transform, backgroundSprite, new Color(0.1f, 0.54f, 0.57f, 1f), true);
            Stretch(overlay.GetComponent<RectTransform>());
            UnityEngine.UI.Image overlayImg = overlay.GetComponent<UnityEngine.UI.Image>();
            overlayImg.type = UnityEngine.UI.Image.Type.Simple;
            overlayImg.preserveAspect = false;
            CanvasGroup overlayGroup = overlay.AddComponent<CanvasGroup>();

            // Ambient ray in background - disabled to prevent static ray mismatch behind spinning reward VFX
            UnityEngine.UI.Image ambientRay = CreateImage("BackgroundGlow", overlay.transform, burstGlow, Color.clear, false).GetComponent<UnityEngine.UI.Image>();
            ambientRay.gameObject.SetActive(false);
            SetCentered(ambientRay.rectTransform, new Vector2(0f, 40f), new Vector2(1450f, 1450f));

            // Full-screen invisible tap target to trigger "Tap to continue"
            GameObject tapTarget = CreateRect("FullScreenTapTarget", overlay.transform);
            Stretch(tapTarget.GetComponent<RectTransform>());
            UnityEngine.UI.Image tapImg = tapTarget.AddComponent<UnityEngine.UI.Image>();
            tapImg.color = Color.clear;
            tapImg.raycastTarget = true;
            UnityEngine.UI.Button fullScreenTap = tapTarget.AddComponent<UnityEngine.UI.Button>();
            fullScreenTap.transition = UnityEngine.UI.Selectable.Transition.None;
            fullScreenTap.targetGraphic = tapImg;

            // Skip button top right (using blank cyan button without baked-in text)
            UnityEngine.UI.Button skipButton = CreateButton("SkipButton", overlay.transform, "SKIP", font, rewardStrokeMaterial, skipButtonSprite);
            RectTransform skipRect = skipButton.GetComponent<RectTransform>();
            skipRect.anchorMin = skipRect.anchorMax = skipRect.pivot = Vector2.one;
            skipRect.anchoredPosition = new Vector2(-55f, -65f);
            skipRect.sizeDelta = new Vector2(220f, 96f);

            // Title: item name (e.g. "Rifle") at top (Y = 676)
            TMP_Text rewardName = CreateText("RewardNameText", overlay.transform, string.Empty, 86f, font, rewardStrokeMaterial, TextAlignmentOptions.Center);
            SetCentered(rewardName.rectTransform, new Vector2(0f, 676f), new Vector2(900f, 130f));

            // Subtitle: item description (e.g. "Weapon that fires a quick barrage") at (Y = 494)
            TMP_Text rewardDesc = CreateText("RewardDescText", overlay.transform, string.Empty, 38f, font, subtleStrokeMaterial, TextAlignmentOptions.Center);
            SetCentered(rewardDesc.rectTransform, new Vector2(0f, 494f), new Vector2(920f, 80f));

            // Chest root (bottom third at Y = -505)
            GameObject chestObject = CreateRect("ChestRoot", overlay.transform);
            RectTransform chestRoot = chestObject.GetComponent<RectTransform>();
            SetCentered(chestRoot, new Vector2(0f, -505f), new Vector2(325f, 431f));

            UnityEngine.UI.Image closed = CreateImage("ChestClosed", chestRoot, null, Color.white, false).GetComponent<UnityEngine.UI.Image>();
            UnityEngine.UI.Image chestBase = CreateImage("ChestBody", chestRoot, null, Color.white, false).GetComponent<UnityEngine.UI.Image>();
            UnityEngine.UI.Image lid = CreateImage("ChestLid", chestRoot, null, Color.white, false).GetComponent<UnityEngine.UI.Image>();
            UnityEngine.UI.Image opened = CreateImage("ChestOpen", chestRoot, null, Color.white, false).GetComponent<UnityEngine.UI.Image>();
            SetCentered(closed.rectTransform, Vector2.zero, new Vector2(325f, 362f));
            SetCentered(chestBase.rectTransform, Vector2.zero, new Vector2(325f, 260f));
            SetCentered(lid.rectTransform, Vector2.zero, new Vector2(325f, 166f));
            SetCentered(opened.rectTransform, Vector2.zero, new Vector2(325f, 431f));

            // Smoke
            GameObject smokeObject = CreateRect("OpenVFXRoot", overlay.transform);
            RectTransform smokeRoot = smokeObject.GetComponent<RectTransform>();
            SetCentered(smokeRoot, new Vector2(0f, -420f), new Vector2(580f, 400f));
            UnityEngine.UI.Image[] puffs = new UnityEngine.UI.Image[8];
            for (int i = 0; i < puffs.Length; i++)
            {
                puffs[i] = CreateImage($"SmokePuff_{i + 1:00}", smokeRoot, puffSprite, Color.white, false).GetComponent<UnityEngine.UI.Image>();
                SetCentered(puffs[i].rectTransform, Vector2.zero, new Vector2(115f + (i % 3) * 16f, 115f + (i % 3) * 16f));
            }

            // RewardRoot (Card + Burst) at Y = 44 (hovering above open chest)
            GameObject rewardObject = CreateRect("RewardRoot", overlay.transform);
            RectTransform rewardRoot = rewardObject.GetComponent<RectTransform>();
            SetCentered(rewardRoot, new Vector2(0f, 44f), new Vector2(276f, 347f));
            CanvasGroup rewardGroup = rewardObject.AddComponent<CanvasGroup>();

            UnityEngine.UI.Image glow = CreateImage("RewardGlow", rewardRoot, burstGlow, new Color32(255, 255, 255, 200), false).GetComponent<UnityEngine.UI.Image>();
            SetCentered(glow.rectTransform, Vector2.zero, new Vector2(920f, 920f));

            UnityEngine.UI.Image burst = CreateImage("RewardBurst", rewardRoot, burstCyan, new Color32(255, 255, 255, 240), false).GetComponent<UnityEngine.UI.Image>();
            SetCentered(burst.rectTransform, Vector2.zero, new Vector2(920f, 920f));

            UnityEngine.UI.Image rewardFrame = CreateImage("RewardFrame", rewardRoot, greenFrame, Color.white, false).GetComponent<UnityEngine.UI.Image>();
            SetCentered(rewardFrame.rectTransform, Vector2.zero, new Vector2(276f, 347f));

            UnityEngine.UI.Image rewardIcon = CreateImage("RewardIcon", rewardRoot, null, Color.white, false).GetComponent<UnityEngine.UI.Image>();
            rewardIcon.preserveAspect = true;
            SetCentered(rewardIcon.rectTransform, new Vector2(0f, 40f), new Vector2(220f, 165f));

            // Reward amount (x1) placed inside bottom strip of card (Y = -86)
            TMP_Text rewardAmount = CreateText("RewardAmountText", rewardRoot, string.Empty, 36f, font, rewardStrokeMaterial, TextAlignmentOptions.Center);
            rewardAmount.enableAutoSizing = true;
            rewardAmount.fontSizeMin = 22f;
            rewardAmount.fontSizeMax = 42f;
            rewardAmount.color = new Color32(255, 205, 67, 255);
            SetCentered(rewardAmount.rectTransform, new Vector2(0f, -86f), new Vector2(220f, 55f));

            // Tap to continue at bottom (Y = -780)
            TMP_Text tapText = CreateText("TapToContinueText", overlay.transform, "Tap to continue", 42f, font, rewardStrokeMaterial, TextAlignmentOptions.Center);
            SetCentered(tapText.rectTransform, new Vector2(0f, -780f), new Vector2(600f, 60f));

            // Result Panel
            GameObject resultPanel = CreateImage("ResultPanel", overlay.transform, null, new Color32(7, 25, 42, 248), true);
            Stretch(resultPanel.GetComponent<RectTransform>());
            CanvasGroup resultGroup = resultPanel.AddComponent<CanvasGroup>();
            TMP_Text resultTitle = CreateText("Title", resultPanel.transform, "BOX REWARDS", 74f, font, rewardStrokeMaterial, TextAlignmentOptions.Center);
            resultTitle.color = new Color32(77, 238, 255, 255);
            SetCentered(resultTitle.rectTransform, new Vector2(0f, 720f), new Vector2(940f, 120f));

            GameObject gridObject = CreateRect("RewardGrid", resultPanel.transform);
            RectTransform gridRect = gridObject.GetComponent<RectTransform>();
            SetCentered(gridRect, new Vector2(0f, 20f), new Vector2(960f, 1240f));
            UnityEngine.UI.GridLayoutGroup grid = gridObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            grid.cellSize = new Vector2(286f, 280f);
            grid.spacing = new Vector2(28f, 24f);
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;

            BoxOpeningController.ResultItemView[] resultViews = new BoxOpeningController.ResultItemView[10];
            for (int i = 0; i < resultViews.Length; i++)
            {
                GameObject item = CreateRect($"RewardItem_{i + 1:00}", gridRect);
                UnityEngine.UI.Image quantityEffect = CreateImage("QuantityEffect", item.transform, burstCyan, Color.clear, false).GetComponent<UnityEngine.UI.Image>();
                SetCentered(quantityEffect.rectTransform, new Vector2(0f, 28f), new Vector2(255f, 255f));
                UnityEngine.UI.Image itemFrame = CreateImage("Frame", item.transform, null, Color.white, false).GetComponent<UnityEngine.UI.Image>();
                SetCentered(itemFrame.rectTransform, new Vector2(0f, 28f), new Vector2(210f, 210f));
                UnityEngine.UI.Image itemIcon = CreateImage("Icon", item.transform, null, Color.white, false).GetComponent<UnityEngine.UI.Image>();
                itemIcon.preserveAspect = true;
                SetCentered(itemIcon.rectTransform, new Vector2(0f, 32f), new Vector2(148f, 148f));
                TMP_Text itemName = CreateText("Name", item.transform, string.Empty, 27f, font, rewardStrokeMaterial, TextAlignmentOptions.Center);
                itemName.enableAutoSizing = true;
                itemName.fontSizeMin = 20f;
                itemName.fontSizeMax = 27f;
                SetCentered(itemName.rectTransform, new Vector2(0f, -105f), new Vector2(276f, 55f));
                TMP_Text itemAmount = CreateText("Amount", item.transform, string.Empty, 38f, font, rewardStrokeMaterial, TextAlignmentOptions.Center);
                itemAmount.color = new Color32(255, 205, 67, 255);
                SetCentered(itemAmount.rectTransform, new Vector2(82f, 98f), new Vector2(110f, 55f));
                resultViews[i] = new BoxOpeningController.ResultItemView
                {
                    root = item,
                    quantityEffect = quantityEffect,
                    frame = itemFrame,
                    icon = itemIcon,
                    nameText = itemName,
                    amountText = itemAmount
                };
            }

            UnityEngine.UI.Button closeButton = CreateButton("GetButton", resultPanel.transform, string.Empty, font, rewardStrokeMaterial, buttonSprite);
            SetCentered(closeButton.GetComponent<RectTransform>(), new Vector2(0f, -755f), new Vector2(360f, 122f));

            BoxOpeningController.BoxVisualSet[] boxSets = BuildBoxVisualSets();
            BoxOpeningController.RewardVisualEntry[] chipsetVisuals = BuildChipsetVisuals(scene);
            BoxOpeningController.RewardVisualEntry[] buddyVisuals = BuildBuddyVisuals(scene);

            SerializedObject serialized = new SerializedObject(controller);
            SetObject(serialized, "shopPanel", shopController.gameObject);
            SetObject(serialized, "overlayRoot", overlay);
            SetObject(serialized, "overlayCanvasGroup", overlayGroup);
            SetObject(serialized, "fullScreenTapButton", fullScreenTap);
            SetObject(serialized, "skipButton", skipButton);
            SetObject(serialized, "closeResultButton", closeButton);
            SetObject(serialized, "chestRoot", chestRoot);
            SetObject(serialized, "chestClosedImage", closed);
            SetObject(serialized, "chestBaseImage", chestBase);
            SetObject(serialized, "chestLidImage", lid);
            SetObject(serialized, "chestOpenImage", opened);
            SetObject(serialized, "smokeRoot", smokeRoot);
            SetObjectArray(serialized, "smokePuffs", puffs.Cast<UnityEngine.Object>().ToArray());
            SetObject(serialized, "rewardRoot", rewardRoot);
            SetObject(serialized, "rewardCanvasGroup", rewardGroup);
            SetObject(serialized, "rewardGlowImage", glow);
            SetObject(serialized, "rewardBurstImage", burst);
            SetObject(serialized, "rewardFrameImage", rewardFrame);
            SetObject(serialized, "rewardIconImage", rewardIcon);
            SetObject(serialized, "rewardNameText", rewardName);
            SetObject(serialized, "rewardDescText", rewardDesc);
            SetObject(serialized, "rewardAmountText", rewardAmount);
            SetObject(serialized, "tapToContinueText", tapText);
            SetObject(serialized, "resultPanel", resultPanel);
            SetObject(serialized, "resultCanvasGroup", resultGroup);
            SetResultViews(serialized.FindProperty("resultItems"), resultViews);
            SetBoxSets(serialized.FindProperty("boxVisualSets"), boxSets);
            SetRewardVisuals(serialized.FindProperty("chipsetRewardVisuals"), chipsetVisuals);
            SetRewardVisuals(serialized.FindProperty("buddyRewardVisuals"), buddyVisuals);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            WireShopControllers(shopControllers, controller);
            overlay.SetActive(false);
            driver.transform.SetAsLastSibling();
            EditorUtility.SetDirty(driver);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[BoxOpeningInstaller] Installed exact reference box flow with {boxSets.Length} chest sets, {chipsetVisuals.Length} chipset visuals, and {buddyVisuals.Length} buddy visuals.");
        }
        finally
        {
            if (openedAdditively && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static BoxOpeningController.BoxVisualSet[] BuildBoxVisualSets()
    {
        return new[]
        {
            LoadBoxSet(ShopController.RewardType.ChipsetBox, false, "Box_Chipset_1x"),
            LoadBoxSet(ShopController.RewardType.ChipsetBox, true, "Box_Chipset_10x"),
            LoadBoxSet(ShopController.RewardType.DroneBox, false, "Box_Drone_1x"),
            LoadBoxSet(ShopController.RewardType.DroneBox, true, "Box_Drone_10x")
        };
    }

    private static BoxOpeningController.BoxVisualSet LoadBoxSet(ShopController.RewardType type, bool multi, string prefix)
    {
        return new BoxOpeningController.BoxVisualSet
        {
            rewardType = type,
            multiOpen = multi,
            closedSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{BoxFolder}{prefix}_Closed.png"),
            baseSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{BoxFolder}{prefix}_Base.png"),
            lidSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{BoxFolder}{prefix}_Lid.png"),
            openSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{BoxFolder}{prefix}_Open.png")
        };
    }

    private static BoxOpeningController.RewardVisualEntry[] BuildChipsetVisuals(Scene scene)
    {
        ChipsetController controller = FindInScene<ChipsetController>(scene);
        List<ChipItemData> data = ChipsetController.CreateDefaultDatabase();
        Sprite greenFrame = AssetDatabase.LoadAssetAtPath<Sprite>(GreenFramePath);
        var result = new List<BoxOpeningController.RewardVisualEntry>(data.Count);
        foreach (ChipItemData item in data)
        {
            string desc = item.id == 2 ? "Weapon that fires a quick barrage" : (item.description ?? string.Empty);
            Sprite frame = greenFrame != null ? greenFrame : (controller != null ? controller.GetFrameSprite(ChipTier.Magic) : null);
            result.Add(new BoxOpeningController.RewardVisualEntry
            {
                itemId = item.id,
                displayName = item.chipName,
                description = desc,
                icon = controller != null ? controller.GetIconSprite(item.iconKey) : null,
                frame = frame
            });
        }
        return result.ToArray();
    }

    private static BoxOpeningController.RewardVisualEntry[] BuildBuddyVisuals(Scene scene)
    {
        BuddyController controller = FindInScene<BuddyController>(scene);
        if (controller == null) return Array.Empty<BoxOpeningController.RewardVisualEntry>();
        if (controller.AllBuddies == null || controller.AllBuddies.Count == 0) controller.InitializeDatabase();

        var result = new List<BoxOpeningController.RewardVisualEntry>(controller.AllBuddies.Count);
        foreach (BuddyItemData item in controller.AllBuddies)
        {
            if (item == null) continue;
            result.Add(new BoxOpeningController.RewardVisualEntry
            {
                itemId = item.id,
                displayName = item.buddyName,
                description = !string.IsNullOrEmpty(item.description) ? item.description : "Combat drone companion",
                icon = controller.GetIconSprite(item),
                frame = controller.GetFrameSprite(BuddyTier.Common)
            });
        }
        return result.ToArray();
    }

    private static void WireShopController(ShopController shopController, BoxOpeningController controller)
    {
        SerializedObject shop = new SerializedObject(shopController);
        SerializedProperty property = shop.FindProperty("boxOpeningController");
        if (property != null && property.objectReferenceValue != controller)
        {
            property.objectReferenceValue = controller;
            shop.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(shopController);
            EditorSceneManager.MarkSceneDirty(shopController.gameObject.scene);
            EditorSceneManager.SaveScene(shopController.gameObject.scene);
        }
    }

    private static void UpgradeExistingResultPresentation(Scene scene, Transform systemRoot, BoxOpeningController controller)
    {
        Transform grid = systemRoot.Find("BoxOpeningCanvas/ResultPanel/RewardGrid");
        Sprite sunburst = AssetDatabase.LoadAssetAtPath<Sprite>(SunburstPath);
        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty items = serialized.FindProperty("resultItems");

        if (grid != null && items != null)
        {
            int count = Mathf.Min(grid.childCount, items.arraySize);
            for (int i = 0; i < count; i++)
            {
                Transform item = grid.GetChild(i);
                Transform effectTransform = item.Find("QuantityEffect");
                UnityEngine.UI.Image effect;
                if (effectTransform == null)
                {
                    effect = CreateImage("QuantityEffect", item, sunburst, Color.clear, false).GetComponent<UnityEngine.UI.Image>();
                    SetCentered(effect.rectTransform, new Vector2(0f, 28f), new Vector2(255f, 255f));
                    effect.transform.SetAsFirstSibling();
                }
                else
                {
                    effect = effectTransform.GetComponent<UnityEngine.UI.Image>();
                    if (effect != null) effect.sprite = sunburst;
                }

                items.GetArrayElementAtIndex(i).FindPropertyRelative("quantityEffect").objectReferenceValue = effect;
            }
        }

        Transform getLabel = systemRoot.Find("BoxOpeningCanvas/ResultPanel/GetButton/Label");
        TMP_Text getText = getLabel != null ? getLabel.GetComponent<TMP_Text>() : null;
        if (getText != null) getText.text = string.Empty;

        SetRewardVisuals(serialized.FindProperty("chipsetRewardVisuals"), BuildChipsetVisuals(scene));
        SetRewardVisuals(serialized.FindProperty("buddyRewardVisuals"), BuildBuddyVisuals(scene));
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[BoxOpeningInstaller] Upgraded existing result grid with {items?.arraySize ?? 0} quantity-effect references.");
    }

    private static void WireShopControllers(IEnumerable<ShopController> shopControllers, BoxOpeningController controller)
    {
        foreach (ShopController shopController in shopControllers)
        {
            if (shopController != null) WireShopController(shopController, controller);
        }
    }

    private static T[] FindAllInScene<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true))
            .ToArray();
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T match = root.GetComponentInChildren<T>(true);
            if (match != null) return match;
        }
        return null;
    }

    private static Transform FindTransformInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == objectName);
            if (match != null) return match;
        }
        return null;
    }

    private static GameObject CreateRect(string name, Transform parent)
    {
        GameObject result = new GameObject(name, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        return result;
    }

    private static GameObject CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycast)
    {
        GameObject result = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        result.transform.SetParent(parent, false);
        UnityEngine.UI.Image image = result.GetComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = raycast;
        image.preserveAspect = sprite != null;
        return result;
    }

    private static UnityEngine.UI.Button CreateButton(string name, Transform parent, string label, TMP_FontAsset font, Material material, Sprite sprite)
    {
        GameObject result = CreateImage(name, parent, sprite, sprite != null ? Color.white : new Color32(25, 177, 203, 255), true);
        UnityEngine.UI.Image image = result.GetComponent<UnityEngine.UI.Image>();
        UnityEngine.UI.Button button = result.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        if (!string.IsNullOrEmpty(label))
        {
            TMP_Text text = CreateText("Label", result.transform, label, 42f, font, material, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
        }
        return button;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float size, TMP_FontAsset font, Material material, TextAlignmentOptions alignment)
    {
        GameObject result = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        result.transform.SetParent(parent, false);
        TMP_Text text = result.GetComponent<TMP_Text>();
        text.text = value;
        text.font = font;
        if (material != null) text.fontSharedMaterial = material;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetCentered(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetObject(SerializedObject serialized, string name, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null) property.objectReferenceValue = value;
    }

    private static void SetObjectArray(SerializedObject serialized, string name, UnityEngine.Object[] values)
    {
        SerializedProperty property = serialized.FindProperty(name);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static void SetResultViews(SerializedProperty property, BoxOpeningController.ResultItemView[] values)
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("root").objectReferenceValue = values[i].root;
            element.FindPropertyRelative("quantityEffect").objectReferenceValue = values[i].quantityEffect;
            element.FindPropertyRelative("frame").objectReferenceValue = values[i].frame;
            element.FindPropertyRelative("icon").objectReferenceValue = values[i].icon;
            element.FindPropertyRelative("nameText").objectReferenceValue = values[i].nameText;
            element.FindPropertyRelative("amountText").objectReferenceValue = values[i].amountText;
        }
    }

    private static void SetBoxSets(SerializedProperty property, BoxOpeningController.BoxVisualSet[] values)
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("rewardType").enumValueIndex = (int)values[i].rewardType;
            element.FindPropertyRelative("multiOpen").boolValue = values[i].multiOpen;
            element.FindPropertyRelative("closedSprite").objectReferenceValue = values[i].closedSprite;
            element.FindPropertyRelative("baseSprite").objectReferenceValue = values[i].baseSprite;
            element.FindPropertyRelative("lidSprite").objectReferenceValue = values[i].lidSprite;
            element.FindPropertyRelative("openSprite").objectReferenceValue = values[i].openSprite;
        }
    }

    private static void SetRewardVisuals(SerializedProperty property, BoxOpeningController.RewardVisualEntry[] values)
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("itemId").intValue = values[i].itemId;
            element.FindPropertyRelative("displayName").stringValue = values[i].displayName;
            SerializedProperty descProp = element.FindPropertyRelative("description");
            if (descProp != null) descProp.stringValue = values[i].description;
            element.FindPropertyRelative("icon").objectReferenceValue = values[i].icon;
            element.FindPropertyRelative("frame").objectReferenceValue = values[i].frame;
        }
    }
}
#endif
