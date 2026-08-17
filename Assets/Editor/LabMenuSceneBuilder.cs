#if UNITY_EDITOR
using System;
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
    private sealed class SlotView
    {
        public GameObject lockedGroup;
        public GameObject unlockedGroup;
        public Image iconImage;
        public TMP_Text levelText;
        public TMP_Text nameText;
        public Image slotBackground;
    }

    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string BackupScenePath = "Assets/Scenes/MainMenu.before-lab-ui.unity";
    private const string BackgroundPath = "Assets/UI/Lab/Generated/lab-background.png";
    private const string IconAtlasPath = "Assets/UI/Lab/Generated/lab-icon-atlas.png";
    private const string PreviewPath = "Assets/UI/Lab/Generated/lab-menu-preview.png";
    private const string BuildRequestPath = "Assets/Editor/PGE_LabUI_BuildRequest.txt";

    private static readonly Color Navy = new Color32(8, 39, 69, 255);
    private static readonly Color Border = new Color32(8, 30, 42, 255);
    private static readonly Color TealBorder = new Color32(94, 213, 205, 255);
    private static readonly Color Panel = new Color32(31, 87, 94, 245);
    private static readonly Color PanelSelected = new Color32(48, 94, 111, 255);
    private static readonly Color BrightTeal = new Color32(76, 186, 178, 255);
    private static readonly Color MutedTeal = new Color32(27, 74, 82, 255);
    private static readonly Color Cream = new Color32(239, 247, 238, 255);
    private static readonly Color Yellow = new Color32(255, 190, 72, 255);

    private static TMP_FontAsset font;

    static LabMenuSceneBuilder()
    {
        EditorApplication.delayCall += TryBuildRequestedScene;
    }

    [MenuItem("PGE/UI/Rebuild Lab Main Menu")]
    public static void BuildFromMenu()
    {
        BuildLabMenuScene();
    }

    private static void TryBuildRequestedScene()
    {
        if (!File.Exists(BuildRequestPath))
        {
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += TryBuildRequestedScene;
            return;
        }

        BuildLabMenuScene();
        AssetDatabase.DeleteAsset(BuildRequestPath);
        AssetDatabase.Refresh();
    }

    private static void BuildLabMenuScene()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += BuildLabMenuScene;
            return;
        }

        ConfigureTextures();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        Scene previousScene = SceneManager.GetActiveScene();
        string previousPath = previousScene.path;
        bool replaceActiveMainMenu = string.Equals(previousPath, ScenePath, StringComparison.OrdinalIgnoreCase);

        if (replaceActiveMainMenu && !File.Exists(BackupScenePath))
        {
            EditorSceneManager.SaveScene(previousScene, BackupScenePath, true);
        }

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            replaceActiveMainMenu ? NewSceneMode.Single : NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        Camera camera = CreateCamera();
        CreateEventSystem();
        Canvas canvas = CreateInterface();

        EditorSceneManager.SaveScene(scene, ScenePath);
        UpdateBuildSettings();
        CapturePreview(canvas, camera);
        EditorSceneManager.SaveScene(scene, ScenePath);

        if (!replaceActiveMainMenu && previousScene.IsValid() && previousScene.isLoaded)
        {
            SceneManager.SetActiveScene(previousScene);
            EditorSceneManager.CloseScene(scene, true);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"PGE Lab UI created: {ScenePath}. Preview: {PreviewPath}");
    }

    private static void ConfigureTextures()
    {
        AssetDatabase.ImportAsset(BackgroundPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter backgroundImporter = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
        if (backgroundImporter != null)
        {
            bool backgroundChanged =
                backgroundImporter.textureType != TextureImporterType.Sprite ||
                backgroundImporter.spriteImportMode != SpriteImportMode.Single ||
                backgroundImporter.mipmapEnabled ||
                backgroundImporter.wrapMode != TextureWrapMode.Clamp ||
                backgroundImporter.filterMode != FilterMode.Bilinear;
            backgroundImporter.textureType = TextureImporterType.Sprite;
            backgroundImporter.spriteImportMode = SpriteImportMode.Single;
            backgroundImporter.spritePixelsPerUnit = 100f;
            backgroundImporter.mipmapEnabled = false;
            backgroundImporter.alphaIsTransparency = true;
            backgroundImporter.wrapMode = TextureWrapMode.Clamp;
            backgroundImporter.filterMode = FilterMode.Bilinear;
            backgroundImporter.maxTextureSize = 2048;
            if (backgroundChanged)
            {
                backgroundImporter.SaveAndReimport();
            }
        }

        AssetDatabase.ImportAsset(IconAtlasPath, ImportAssetOptions.ForceSynchronousImport);
        Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(IconAtlasPath);
        TextureImporter atlasImporter = AssetImporter.GetAtPath(IconAtlasPath) as TextureImporter;
        if (atlasImporter == null || atlas == null)
        {
            throw new InvalidOperationException("Could not import the generated Lab icon atlas.");
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

        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 5; column++)
            {
                sprites[spriteIndex++] = new SpriteRect
                {
                    name = iconNames[row, column],
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = GUID.Generate(),
                    rect = new Rect(
                        column * cellWidth,
                        (2 - row) * cellHeight,
                        cellWidth,
                        cellHeight)
                };
            }
        }

        bool importerChanged =
            atlasImporter.textureType != TextureImporterType.Sprite ||
            atlasImporter.spriteImportMode != SpriteImportMode.Multiple ||
            atlasImporter.mipmapEnabled ||
            atlasImporter.wrapMode != TextureWrapMode.Clamp ||
            atlasImporter.filterMode != FilterMode.Point;
        atlasImporter.textureType = TextureImporterType.Sprite;
        atlasImporter.spriteImportMode = SpriteImportMode.Multiple;
        atlasImporter.spritePixelsPerUnit = 100f;
        atlasImporter.mipmapEnabled = false;
        atlasImporter.alphaIsTransparency = true;
        atlasImporter.wrapMode = TextureWrapMode.Clamp;
        atlasImporter.filterMode = FilterMode.Point;
        atlasImporter.maxTextureSize = 2048;
        if (importerChanged)
        {
            atlasImporter.SaveAndReimport();
        }

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(atlasImporter);
        dataProvider.InitSpriteEditorDataProvider();
        SpriteRect[] currentSprites = dataProvider.GetSpriteRects();
        bool spriteLayoutChanged = currentSprites.Length != sprites.Length ||
            currentSprites.Select(sprite => sprite.name)
                .OrderBy(name => name)
                .SequenceEqual(sprites.Select(sprite => sprite.name).OrderBy(name => name)) == false;

        if (spriteLayoutChanged)
        {
            dataProvider.SetSpriteRects(sprites);
            dataProvider.Apply();
            atlasImporter.SaveAndReimport();
        }
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

        TMP_Text chipBalanceText = CreateTopBar(topBar);

        RectTransform content = CreateRect("Content", canvasRect);
        Stretch(content, Vector2.zero, Vector2.one, new Vector2(0f, 220f), new Vector2(0f, -175f));

        GameObject[] panels = new GameObject[5];
        panels[0] = CreatePlaceholderPanel(content, "ShopPanel", "SHOP", "Equipment store is being prepared");
        panels[1] = CreateLabPanel(content, chipBalanceText);
        panels[2] = CreatePlaceholderPanel(content, "ChapterPanel", "CHAPTER", "Mission map is being prepared");
        panels[3] = CreatePlaceholderPanel(content, "ChipsetPanel", "CHIPSET", "Chip configuration is being prepared");
        panels[4] = CreatePlaceholderPanel(content, "BuddyPanel", "BUDDY", "Drone hangar is being prepared");

        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].SetActive(i == 1);
        }

        CreateBottomNavigation(canvasObject, canvasRect, panels);
        return canvas;
    }

    private static TMP_Text CreateTopBar(RectTransform parent)
    {
        CreateResourceDisplay(parent, "Energy", 0.025f, 0.265f, "energy", "30/50", "offline", Cream);
        TMP_Text chips = CreateResourceDisplay(parent, "ChipCurrency", 0.285f, 0.525f, "chip-currency", "700", string.Empty, Cream);
        CreateResourceDisplay(parent, "RedCurrency", 0.545f, 0.765f, "red-currency", "10", string.Empty, Cream);
        CreateTopIconButton(parent, "MailButton", 0.79f, 0.885f, "mail");
        CreateTopIconButton(parent, "SettingButton", 0.895f, 0.99f, "settings");
        return chips;
    }

    private static TMP_Text CreateResourceDisplay(
        RectTransform parent,
        string name,
        float minX,
        float maxX,
        string iconName,
        string value,
        string subtitle,
        Color valueColor)
    {
        RectTransform root = CreateRect(name, parent);
        Stretch(root, new Vector2(minX, 1f), new Vector2(maxX, 1f), new Vector2(5f, -150f), new Vector2(-5f, -20f));

        Image plate = CreateImage("Plate", root, new Color32(11, 55, 72, 195), false);
        Stretch(plate.rectTransform, new Vector2(0.2f, 0.22f), new Vector2(1f, 0.8f), Vector2.zero, Vector2.zero);
        AddOutline(plate, Border, 3f);

        Image icon = CreateIcon("Icon", root, iconName, 90f);
        Anchor(icon.rectTransform, new Vector2(0.19f, 0.55f), new Vector2(-4f, 0f), new Vector2(92f, 92f));

        TMP_Text valueText = CreateText("Value", root, value, 38f, valueColor, TextAlignmentOptions.Center);
        Stretch(valueText.rectTransform, new Vector2(0.32f, 0.34f), new Vector2(0.98f, 0.84f), Vector2.zero, Vector2.zero);

        if (!string.IsNullOrEmpty(subtitle))
        {
            TMP_Text subtitleText = CreateText("Status", root, subtitle, 22f, Cream, TextAlignmentOptions.Center);
            Stretch(subtitleText.rectTransform, new Vector2(0.28f, 0f), new Vector2(1f, 0.34f), Vector2.zero, Vector2.zero);
        }

        return valueText;
    }

    private static void CreateTopIconButton(RectTransform parent, string name, float minX, float maxX, string iconName)
    {
        RectTransform root = CreateRect(name, parent);
        Stretch(root, new Vector2(minX, 1f), new Vector2(maxX, 1f), new Vector2(2f, -145f), new Vector2(-2f, -20f));
        Image icon = CreateIcon("Icon", root, iconName, 100f);
        Anchor(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(98f, 98f));
        icon.raycastTarget = true;
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = icon;
    }

    private static GameObject CreateLabPanel(RectTransform parent, TMP_Text chipBalanceText)
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

        string[] itemNames =
        {
            "DEF", "ATK", "HP", "SPD",
            "CRIT", "RANGE", "FIRE", "REGEN",
            "DODGE", "ARMOR", "POWER", "TECH",
            "LUCK", "GROWTH", "SHIELD", "DRONE"
        };
        string[] itemIconNames =
        {
            "armor", "shield", "plus", "energy",
            "red-currency", "chapter", "lab", "leaf",
            "buddy", "armor", "chip-currency", "chipset",
            "mail", "shop", "shield", "buddy"
        };
        float[] itemWeights =
        {
            14f, 12f, 12f, 10f,
            8f, 10f, 8f, 8f,
            7f, 7f, 6f, 6f,
            5f, 5f, 4f, 4f
        };
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
        }

        GameObject upgradeRoot = CreateFrame("UpgradeButton", statsPanel, new Color32(86, 183, 107, 255), Cream, out Image upgradeBackground);
        RectTransform upgradeRect = upgradeRoot.GetComponent<RectTransform>();
        Anchor(upgradeRect, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(470f, 165f), new Vector2(0.5f, 0f));
        // The Button needs a raycastable target graphic to receive pointer clicks.
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

        LabUpgradeController controller = panel.gameObject.AddComponent<LabUpgradeController>();
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("upgradeButton").objectReferenceValue = upgradeButton;
        serializedController.FindProperty("chipBalanceText").objectReferenceValue = chipBalanceText;
        serializedController.FindProperty("priceText").objectReferenceValue = priceText;
        serializedController.FindProperty("resultText").objectReferenceValue = resultText;
        serializedController.FindProperty("upgradeBackground").objectReferenceValue = upgradeBackground;

        SerializedProperty items = serializedController.FindProperty("items");
        items.arraySize = slotViews.Length;
        for (int i = 0; i < slotViews.Length; i++)
        {
            SerializedProperty item = items.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("itemName").stringValue = itemNames[i];
            item.FindPropertyRelative("itemIcon").objectReferenceValue = slotViews[i].iconImage.sprite;
            item.FindPropertyRelative("dropWeight").floatValue = itemWeights[i];
            item.FindPropertyRelative("startsUnlocked").boolValue = false;
            item.FindPropertyRelative("startingLevel").intValue = 1;
            item.FindPropertyRelative("lockedGroup").objectReferenceValue = slotViews[i].lockedGroup;
            item.FindPropertyRelative("unlockedGroup").objectReferenceValue = slotViews[i].unlockedGroup;
            item.FindPropertyRelative("iconImage").objectReferenceValue = slotViews[i].iconImage;
            item.FindPropertyRelative("levelText").objectReferenceValue = slotViews[i].levelText;
            item.FindPropertyRelative("nameText").objectReferenceValue = slotViews[i].nameText;
            item.FindPropertyRelative("slotBackground").objectReferenceValue = slotViews[i].slotBackground;
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

        RectTransform lockedGroup = CreateRect("LockedGroup", slotRect);
        Stretch(lockedGroup, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image lockIcon = CreateIcon("LockIcon", lockedGroup, "lock", 88f);
        Anchor(lockIcon.rectTransform, new Vector2(0.5f, 0.57f), Vector2.zero, new Vector2(84f, 84f));
        TMP_Text lockedText = CreateText("LockedText", lockedGroup, "LOCKED", 27f, Cream, TextAlignmentOptions.Center);
        Anchor(lockedText.rectTransform, new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(190f, 44f));

        RectTransform unlockedGroup = CreateRect("UnlockedGroup", slotRect);
        Stretch(unlockedGroup, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        TMP_Text levelText = CreateText("LevelText", unlockedGroup, "LV.01", 29f, Yellow, TextAlignmentOptions.Center);
        Anchor(levelText.rectTransform, new Vector2(0.5f, 0.81f), Vector2.zero, new Vector2(180f, 44f));
        Image itemIcon = CreateIcon("ItemIcon", unlockedGroup, itemIconName, 104f);
        Anchor(itemIcon.rectTransform, new Vector2(0.5f, 0.53f), Vector2.zero, new Vector2(104f, 104f));
        TMP_Text statName = CreateText("ItemName", unlockedGroup, itemName, 34f, Cream, TextAlignmentOptions.Center);
        Anchor(statName.rectTransform, new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(180f, 50f));

        lockedGroup.gameObject.SetActive(!unlocked);
        unlockedGroup.gameObject.SetActive(unlocked);

        view = new SlotView
        {
            lockedGroup = lockedGroup.gameObject,
            unlockedGroup = unlockedGroup.gameObject,
            iconImage = itemIcon,
            levelText = levelText,
            nameText = statName,
            slotBackground = slotBackground
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

    private static void CreateBottomNavigation(GameObject canvasObject, RectTransform canvas, GameObject[] panels)
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
        Image[] iconImages = new Image[5];
        TMP_Text[] labelTexts = new TMP_Text[5];

        for (int i = 0; i < 5; i++)
        {
            bool selected = i == 1;
            GameObject root = CreateFrame(
                names[i],
                nav,
                selected ? BrightTeal : MutedTeal,
                selected ? Cream : new Color32(39, 105, 110, 255),
                out backgrounds[i]);
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
        SerializedProperty items = serializedController.FindProperty("items");
        items.arraySize = 5;

        for (int i = 0; i < 5; i++)
        {
            SerializedProperty item = items.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("button").objectReferenceValue = buttons[i];
            item.FindPropertyRelative("panel").objectReferenceValue = panels[i];
            item.FindPropertyRelative("background").objectReferenceValue = backgrounds[i];
            item.FindPropertyRelative("icon").objectReferenceValue = iconImages[i];
            item.FindPropertyRelative("label").objectReferenceValue = labelTexts[i];
        }

        serializedController.FindProperty("defaultSelectedIndex").intValue = 1;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
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
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.outlineColor = Navy;
        text.outlineWidth = 0.16f;
        return text;
    }

    private static Sprite LoadIcon(string spriteName)
    {
        return AssetDatabase.LoadAllAssetRepresentationsAtPath(IconAtlasPath)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == spriteName);
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

            Directory.CreateDirectory(Path.GetDirectoryName(PreviewPath) ?? "Assets/UI/Lab/Generated");
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

        AssetDatabase.ImportAsset(PreviewPath, ImportAssetOptions.ForceSynchronousImport);
    }
}
#endif
