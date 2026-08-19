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
    private sealed class SlotView
    {
        public GameObject lockedGroup;
        public GameObject unlockedGroup;
        public Image iconImage;
        public TMP_Text levelText;
        public TMP_Text nameText;
        public Image slotBackground;
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
    private const string ChipsetAtlasPath = "Assets/UI/Chipset/Generated/chipset-atlas.png";
    private const string PreviewPath = "Assets/UI/Lab/Generated/lab-menu-preview.png";
    private const string ChipsetPreviewPath = "Assets/UI/Chipset/Generated/chipset-menu-preview.png";
    private const string BuildRequestPath = "Assets/Editor/PGE_LabUI_BuildRequest.txt";

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

    private static TMP_FontAsset font;

    static LabMenuSceneBuilder()
    {
        EditorApplication.update += TryBuildRequestedScene;
    }

    [MenuItem("PGE/UI/Rebuild Full Main Menu (Chipset & Lab)")]
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

    private static void BuildLabMenuScene()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += BuildLabMenuScene;
            return;
        }

        ConfigureTextures();
        ConfigureChipsetTextures();

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

        atlasImporter.textureType = TextureImporterType.Sprite;
        atlasImporter.spriteImportMode = SpriteImportMode.Multiple;
        atlasImporter.spritePixelsPerUnit = 100f;
        atlasImporter.mipmapEnabled = false;
        atlasImporter.alphaIsTransparency = true;
        atlasImporter.wrapMode = TextureWrapMode.Clamp;
        atlasImporter.filterMode = FilterMode.Point;
        atlasImporter.maxTextureSize = 2048;
        atlasImporter.SaveAndReimport();

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(atlasImporter);
        dataProvider.InitSpriteEditorDataProvider();
        dataProvider.SetSpriteRects(sprites);
        dataProvider.Apply();
        atlasImporter.SaveAndReimport();
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
            { "standard-gun", "rifle", "rocket-punch", "spinning-blade", "multigun" },
            { "gun-turret", "spiky-discus", "shotgun", "energy-jumper-cables", "high-explosive-mine" },
            { "aiming-lens", "plasma-field", "laser-eye", "biochemical-mine", "tesla-coil" },
            { "card-frame-common", "card-frame-rare", "card-frame-epic", "card-frame-holographic", "badge-upgrade" },
            { "icon-lock", "wave-circuit", "icon-star", "furnace-border", "power-battery" }
        };

        float cellWidth = 256f;
        float cellHeight = 256f;
        SpriteRect[] sprites = new SpriteRect[25];
        for (int row = 0; row < 5; row++)
        {
            for (int column = 0; column < 5; column++)
            {
                int index = row * 5 + column;
                sprites[index] = new SpriteRect
                {
                    name = iconNames[row, column],
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = GUID.Generate(),
                    rect = new Rect(
                        column * cellWidth,
                        (4 - row) * cellHeight,
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

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(atlasImporter);
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
        cachedChipsetSprites = AssetDatabase.LoadAllAssetsAtPath(ChipsetAtlasPath).OfType<Sprite>().ToArray();
        Debug.Log($"[Chipset] Cached {cachedChipsetSprites.Length} sprites: {string.Join(", ", cachedChipsetSprites.Select(s => s.name))}");
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
        panels[4] = CreatePlaceholderPanel(content, "BuddyPanel", "BUDDY", "Drone hangar is being prepared");

        // Default to Chapter Tab (index 2) or Shop Tab (index 0)
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].SetActive(i == 0);
        }

        BottomNavigationController bottomNav = CreateBottomNavigation(canvasObject, canvasRect, panels, 0);
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
        Anchor(tabChipsetText.rectTransform, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(300f, 50f));
        Image waveImg = CreateChipsetIcon("Wave", tabChipsetRect, "wave-circuit", 140f);
        Anchor(waveImg.rectTransform, new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(140f, 30f));
        Button tabChipsetBtn = tabChipsetObj.AddComponent<Button>();
        tabChipsetBtn.targetGraphic = tabChipsetBg;

        // Right tab: High-Tech Chipset (Locked)
        GameObject tabHighTechObj = CreateFrame("TabHighTech", topTabs, new Color32(16, 52, 54, 235), new Color32(12, 38, 42, 255), out Image tabHighTechBg);
        RectTransform tabHighTechRect = tabHighTechObj.GetComponent<RectTransform>();
        Stretch(tabHighTechRect, new Vector2(0.515f, 0f), new Vector2(0.97f, 1f), Vector2.zero, Vector2.zero);
        TMP_Text tabHighTechText = CreateText("Label", tabHighTechRect, "High-Tech Chipset", 36f, new Color32(40, 95, 95, 255), TextAlignmentOptions.Center);
        Anchor(tabHighTechText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 50f));
        Image lockImg = CreateChipsetIcon("Lock", tabHighTechRect, "icon-lock", 60f);
        Anchor(lockImg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
        Button tabHighTechBtn = tabHighTechObj.AddComponent<Button>();
        tabHighTechBtn.targetGraphic = tabHighTechBg;

        // 2. Equipped Chipset Board
        GameObject boardObj = CreateFrame("EquippedBoard", panel, DarkPanel, TealBorder, out Image boardBg);
        RectTransform boardRect = boardObj.GetComponent<RectTransform>();
        boardRect.anchorMin = new Vector2(0.5f, 1f);
        boardRect.anchorMax = new Vector2(0.5f, 1f);
        boardRect.pivot = new Vector2(0.5f, 1f);
        boardRect.anchoredPosition = new Vector2(0f, -125f);
        boardRect.sizeDelta = new Vector2(1020f, 650f);

        // Header inside Board: Presets 1, 2, 3 + Blast Furnace
        RectTransform boardHeader = CreateRect("BoardHeader", boardRect);
        boardHeader.anchorMin = new Vector2(0f, 1f);
        boardHeader.anchorMax = new Vector2(1f, 1f);
        boardHeader.pivot = new Vector2(0.5f, 1f);
        boardHeader.anchoredPosition = new Vector2(0f, -12f);
        boardHeader.sizeDelta = new Vector2(0f, 65f);

        // Preset buttons
        Button p1Btn = CreatePresetButton(boardHeader, "Preset1", 0.35f, "1", false, out Image p1Bg, out TMP_Text p1Text);
        Button p2Btn = CreatePresetButton(boardHeader, "Preset2", 0.45f, "2", false, out Image p2Bg, out TMP_Text p2Text);
        Button p3Btn = CreatePresetButton(boardHeader, "Preset3", 0.55f, "3", true, out Image p3Bg, out TMP_Text p3Text);

        // Blast Furnace Button
        GameObject furnaceBtnObj = CreateFrame("BlastFurnaceBtn", boardHeader, FieryRed, FieryOrange, out Image furnaceBg);
        RectTransform furnaceRect = furnaceBtnObj.GetComponent<RectTransform>();
        Anchor(furnaceRect, new Vector2(0.85f, 0.5f), Vector2.zero, new Vector2(230f, 64f));
        TMP_Text furnaceLabel = CreateText("Label", furnaceRect, "Blast\nFurnace", 23f, Yellow, TextAlignmentOptions.Center);
        Stretch(furnaceLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        furnaceBg.raycastTarget = true;
        Button furnaceBtn = furnaceBtnObj.AddComponent<Button>();
        furnaceBtn.targetGraphic = furnaceBg;

        // Equipped Grid (2 rows x 5 columns)
        RectTransform equippedGrid = CreateRect("EquippedGrid", boardRect);
        equippedGrid.anchorMin = new Vector2(0f, 0f);
        equippedGrid.anchorMax = new Vector2(1f, 1f);
        equippedGrid.offsetMin = new Vector2(18f, 15f);
        equippedGrid.offsetMax = new Vector2(-18f, -75f);

        GridLayoutGroup equippedLayout = equippedGrid.gameObject.AddComponent<GridLayoutGroup>();
        equippedLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        equippedLayout.constraintCount = 5;
        equippedLayout.cellSize = new Vector2(180f, 255f);
        equippedLayout.spacing = new Vector2(16f, 16f);
        equippedLayout.childAlignment = TextAnchor.UpperCenter;

        ChipsetCardUI[] equippedCardSlots = new ChipsetCardUI[10];
        for (int i = 0; i < 10; i++)
        {
            equippedCardSlots[i] = CreateChipCardUI(equippedGrid, $"EquippedSlot_{i:00}", new Vector2(180f, 255f));
        }

        // Lower Section Background Tint
        Image invBgTint = CreateImage("InventoryBgTint", panel, new Color32(18, 62, 74, 210), false);
        Stretch(invBgTint.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -770f));

        // 3. Sort Filter Bar
        RectTransform sortBar = CreateRect("SortFilterBar", panel);
        sortBar.anchorMin = new Vector2(0.5f, 1f);
        sortBar.anchorMax = new Vector2(0.5f, 1f);
        sortBar.pivot = new Vector2(0.5f, 1f);
        sortBar.anchoredPosition = new Vector2(0f, -790f);
        sortBar.sizeDelta = new Vector2(1020f, 70f);

        GameObject byTierObj = CreateFrame("ByTierBtn", sortBar, new Color32(18, 58, 68, 255), BrightCyan, out Image byTierBg);
        RectTransform byTierRect = byTierObj.GetComponent<RectTransform>();
        Anchor(byTierRect, new Vector2(0.36f, 0.5f), Vector2.zero, new Vector2(230f, 62f));
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

        // 4. Inventory Scroll View
        RectTransform scrollRoot = CreateRect("InventoryScrollView", panel);
        Stretch(scrollRoot, Vector2.zero, Vector2.one, new Vector2(20f, 15f), new Vector2(-20f, -870f));

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
        invLayout.cellSize = new Vector2(225f, 290f);
        invLayout.spacing = new Vector2(20f, 22f);
        invLayout.padding = new RectOffset(15, 15, 10, 30);
        invLayout.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter fitter = invContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Pre-configure the 10 equipped cards to match Preset 3 screenshot
        string[] eqIcons = {
            "standard-gun", "rifle", "rocket-punch", "spinning-blade", "multigun",
            "gun-turret", "spiky-discus", "shotgun", "energy-jumper-cables", "high-explosive-mine"
        };
        string[] eqFrames = {
            "card-frame-common", "card-frame-holographic", "card-frame-common", "card-frame-epic", "card-frame-common",
            "card-frame-common", "card-frame-common", "card-frame-rare", "card-frame-common", "card-frame-common"
        };
        string[] eqLevels = {
            "LV.01", "LV.24", "LV.01", "LV.14", "LV.06",
            "LV.01", "LV.01", "LV.09", "LV.01", "LV.01"
        };
        string[] eqProgress = {
            "22/3", "20", "50/3", "31/9", "37/3",
            "49/3", "30/3", "49/7", "38/3", "24/3"
        };
        bool[] eqStars = { false, false, false, false, false, false, false, false, true, false };
        bool[] eqArrows = { true, false, true, true, true, true, true, true, true, true };

        for (int i = 0; i < 10; i++)
        {
            ConfigureCardStaticView(equippedCardSlots[i], eqIcons[i], eqFrames[i], eqLevels[i], eqProgress[i], eqStars[i], eqArrows[i]);
        }

        // Pre-populate Inventory cards to match screenshot
        string[] invIcons = {
            "aiming-lens", "laser-eye", "plasma-field", "biochemical-mine",
            "tesla-coil", "standard-gun", "spiky-discus", "multigun",
            "power-battery", "rocket-punch", "gun-turret", "plasma-field"
        };
        string[] invFrames = {
            "card-frame-common", "card-frame-common", "card-frame-common", "card-frame-common",
            "card-frame-common", "card-frame-rare", "card-frame-common", "card-frame-common",
            "card-frame-common", "card-frame-common", "card-frame-common", "card-frame-common"
        };
        string[] invLevels = {
            "LV.01", "LV.01", "LV.01", "LV.01",
            "LV.01", "LV.09", "LV.01", "LV.01",
            "LV.01", "LV.01", "LV.01", "LV.01"
        };
        string[] invProgress = {
            "63/3", "58/3", "52/3", "48/3",
            "33/3", "31/7", "31/3", "30/3",
            "30/3", "26/3", "23/3", "22/3"
        };
        bool[] invStars = {
            true, false, false, false,
            false, false, false, false,
            false, false, false, false
        };

        for (int i = 0; i < invIcons.Length; i++)
        {
            ChipsetCardUI invCard = CreateChipCardUI(invContent, $"InvCard_{i:00}", new Vector2(225f, 290f));
            ConfigureCardStaticView(invCard, invIcons[i], invFrames[i], invLevels[i], invProgress[i], invStars[i], true);
        }

        // Card Prefab template for dynamic instantiation at runtime
        GameObject cardPrefab = CreateChipCardUI(invContent, "CardTemplate", new Vector2(225f, 290f)).gameObject;
        cardPrefab.SetActive(false);

        // 5. Detail Modal
        GameObject detailModal = CreateDetailModal(canvasRect, out Image dIcon, out TMP_Text dName, out TMP_Text dLevel,
            out TMP_Text dTier, out TMP_Text dBaseStats, out TMP_Text dMagic, out TMP_Text dRare, out TMP_Text dUnique,
            out TMP_Text dEpic, out Button dUpgradeBtn, out TMP_Text dUpgradeBtnText, out Button dEquipBtn,
            out TMP_Text dEquipBtnText, out Button dCloseBtn);
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
        sController.FindProperty("detailIcon").objectReferenceValue = dIcon;
        sController.FindProperty("detailNameText").objectReferenceValue = dName;
        sController.FindProperty("detailLevelText").objectReferenceValue = dLevel;
        sController.FindProperty("detailTierText").objectReferenceValue = dTier;
        sController.FindProperty("detailBaseStatsText").objectReferenceValue = dBaseStats;
        sController.FindProperty("detailMagicText").objectReferenceValue = dMagic;
        sController.FindProperty("detailRareText").objectReferenceValue = dRare;
        sController.FindProperty("detailUniqueText").objectReferenceValue = dUnique;
        sController.FindProperty("detailEpicText").objectReferenceValue = dEpic;
        sController.FindProperty("detailUpgradeBtn").objectReferenceValue = dUpgradeBtn;
        sController.FindProperty("detailUpgradeBtnText").objectReferenceValue = dUpgradeBtnText;
        sController.FindProperty("detailEquipBtn").objectReferenceValue = dEquipBtn;
        sController.FindProperty("detailEquipBtnText").objectReferenceValue = dEquipBtnText;
        sController.FindProperty("detailCloseBtn").objectReferenceValue = dCloseBtn;

        sController.FindProperty("furnaceModal").objectReferenceValue = furnaceModal;
        sController.FindProperty("furnaceDescText").objectReferenceValue = fDesc;
        sController.FindProperty("furnaceDismantleBtn").objectReferenceValue = fDismantleBtn;
        sController.FindProperty("furnaceCloseBtn").objectReferenceValue = fCloseBtn;

        sController.FindProperty("toastRoot").objectReferenceValue = toastRoot;
        sController.FindProperty("toastText").objectReferenceValue = toastText;

        // Load Sprites into database
        Sprite[] allChipsetSprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(ChipsetAtlasPath).OfType<Sprite>().ToArray();
        string[] iconKeys = {
            "standard-gun", "rifle", "rocket-punch", "spinning-blade", "multigun",
            "gun-turret", "spiky-discus", "shotgun", "energy-jumper-cables", "high-explosive-mine",
            "aiming-lens", "plasma-field", "laser-eye", "biochemical-mine", "tesla-coil"
        };
        Sprite[] chipIcons = iconKeys.Select(k => allChipsetSprites.FirstOrDefault(s => s.name == k)).Where(s => s != null).ToArray();
        Sprite[] frameSprites = new[] {
            allChipsetSprites.FirstOrDefault(s => s.name == "card-frame-common"),
            allChipsetSprites.FirstOrDefault(s => s.name == "card-frame-rare"),
            allChipsetSprites.FirstOrDefault(s => s.name == "card-frame-epic"),
            allChipsetSprites.FirstOrDefault(s => s.name == "card-frame-holographic")
        };

        SerializedProperty sIcons = sController.FindProperty("chipIcons");
        sIcons.arraySize = chipIcons.Length;
        for (int i = 0; i < chipIcons.Length; i++) sIcons.GetArrayElementAtIndex(i).objectReferenceValue = chipIcons[i];

        SerializedProperty sFrames = sController.FindProperty("frameSprites");
        sFrames.arraySize = frameSprites.Length;
        for (int i = 0; i < frameSprites.Length; i++) sFrames.GetArrayElementAtIndex(i).objectReferenceValue = frameSprites[i];

        sController.FindProperty("starSprite").objectReferenceValue = allChipsetSprites.FirstOrDefault(s => s.name == "icon-star");
        sController.FindProperty("upgradeArrowSprite").objectReferenceValue = allChipsetSprites.FirstOrDefault(s => s.name == "badge-upgrade");

        sController.ApplyModifiedPropertiesWithoutUndo();

        return panel.gameObject;
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
        Anchor(rect, new Vector2(xAnchor, 0.5f), Vector2.zero, new Vector2(74f, 62f));
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
        cardFrame.sprite = LoadChipsetSprite("card-frame-common");
        cardFrame.type = Image.Type.Simple;
        cardFrame.preserveAspect = false;
        cardFrame.raycastTarget = true;

        Button cardBtn = cardRoot.gameObject.AddComponent<Button>();
        cardBtn.targetGraphic = cardFrame;

        // Top Level Text
        TMP_Text levelText = CreateText("LevelText", cardRoot, "LV.01", 24f, Yellow, TextAlignmentOptions.Center);
        Anchor(levelText.rectTransform, new Vector2(0.5f, 0.79f), Vector2.zero, new Vector2(size.x * 0.9f, 30f));

        // Star Icon (Top Right)
        Image starImg = CreateChipsetIcon("Star", cardRoot, "icon-star", 28f);
        Anchor(starImg.rectTransform, new Vector2(0.84f, 0.80f), Vector2.zero, new Vector2(28f, 28f));
        starImg.gameObject.SetActive(false);

        // Center Icon
        Image centerIcon = CreateChipsetIcon("Icon", cardRoot, "standard-gun", 105f);
        Anchor(centerIcon.rectTransform, new Vector2(0.5f, 0.50f), Vector2.zero, new Vector2(105f, 105f));

        // Bottom Progress Bar
        RectTransform bottomBar = CreateRect("BottomBar", cardRoot);
        Stretch(bottomBar, new Vector2(0.08f, 0.09f), new Vector2(0.92f, 0.25f), Vector2.zero, Vector2.zero);
        Image bottomBarBg = bottomBar.gameObject.AddComponent<Image>();
        bottomBarBg.color = new Color32(20, 120, 50, 230);
        bottomBarBg.raycastTarget = false;

        TMP_Text progressText = CreateText("ProgressText", bottomBar, "22/3", 24f, Color.white, TextAlignmentOptions.Center);
        Stretch(progressText.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 0f), new Vector2(-36f, 0f));

        // Upgrade Green Arrow Button
        GameObject upgradeArrowObj = new GameObject("UpgradeArrowGroup", typeof(RectTransform));
        RectTransform arrowRect = upgradeArrowObj.GetComponent<RectTransform>();
        arrowRect.SetParent(cardRoot, false);
        Anchor(arrowRect, new Vector2(0.85f, 0.17f), Vector2.zero, new Vector2(44f, 44f));

        Image arrowIcon = upgradeArrowObj.AddComponent<Image>();
        arrowIcon.sprite = LoadChipsetSprite("badge-upgrade");
        arrowIcon.preserveAspect = true;
        arrowIcon.raycastTarget = true;

        Button upgradeBtn = upgradeArrowObj.AddComponent<Button>();
        upgradeBtn.targetGraphic = arrowIcon;

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
        sCard.ApplyModifiedPropertiesWithoutUndo();

        cardComp.InitializeReferences(cardFrame, centerIcon, levelText, progressText, cardBtn, upgradeBtn, upgradeArrowObj, starImg.gameObject, bottomBarBg);

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
        out Image detailIcon,
        out TMP_Text nameText,
        out TMP_Text levelText,
        out TMP_Text tierText,
        out TMP_Text baseStatsText,
        out TMP_Text magicText,
        out TMP_Text rareText,
        out TMP_Text uniqueText,
        out TMP_Text epicText,
        out Button upgradeBtn,
        out TMP_Text upgradeBtnText,
        out Button equipBtn,
        out TMP_Text equipBtnText,
        out Button closeBtn)
    {
        RectTransform modalRoot = CreateRect("ChipsetDetailModal", canvasRect);
        Stretch(modalRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image dim = modalRoot.gameObject.AddComponent<Image>();
        dim.color = new Color32(5, 20, 30, 225);
        dim.raycastTarget = true;

        GameObject cardBox = CreateFrame("ModalBox", modalRoot, DarkPanel, BrightCyan, out _);
        RectTransform boxRect = cardBox.GetComponent<RectTransform>();
        Anchor(boxRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 1260f));

        // Title Header
        TMP_Text title = CreateText("Title", boxRect, "CHIPSET INSPECTOR", 42f, BrightCyan, TextAlignmentOptions.Center);
        Anchor(title.rectTransform, new Vector2(0.5f, 0.94f), Vector2.zero, new Vector2(600f, 50f));

        // Close Button
        GameObject closeObj = CreateFrame("CloseBtn", boxRect, FieryRed, FieryOrange, out Image closeBg);
        RectTransform closeRect = closeObj.GetComponent<RectTransform>();
        Anchor(closeRect, new Vector2(0.93f, 0.94f), Vector2.zero, new Vector2(60f, 60f));
        TMP_Text closeTxt = CreateText("X", closeRect, "X", 36f, Color.white, TextAlignmentOptions.Center);
        Stretch(closeTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        closeBg.raycastTarget = true;
        closeBtn = closeObj.AddComponent<Button>();
        closeBtn.targetGraphic = closeBg;

        // Big Icon Frame
        GameObject iconFrameObj = CreateFrame("IconFrame", boxRect, new Color32(8, 30, 48, 255), BrightCyan, out _);
        RectTransform iconFrameRect = iconFrameObj.GetComponent<RectTransform>();
        Anchor(iconFrameRect, new Vector2(0.24f, 0.77f), Vector2.zero, new Vector2(220f, 220f));
        detailIcon = CreateChipsetIcon("Icon", iconFrameRect, "standard-gun", 160f);
        Anchor(detailIcon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 160f));

        // Name & Tier Info
        nameText = CreateText("Name", boxRect, "Standard Gun", 46f, Color.white, TextAlignmentOptions.Left);
        Anchor(nameText.rectTransform, new Vector2(0.68f, 0.83f), Vector2.zero, new Vector2(460f, 55f));

        levelText = CreateText("Level", boxRect, "LEVEL 01", 34f, Yellow, TextAlignmentOptions.Left);
        Anchor(levelText.rectTransform, new Vector2(0.68f, 0.76f), Vector2.zero, new Vector2(460f, 45f));

        tierText = CreateText("Tier", boxRect, "COMMON", 30f, BrightCyan, TextAlignmentOptions.Left);
        Anchor(tierText.rectTransform, new Vector2(0.68f, 0.70f), Vector2.zero, new Vector2(460f, 40f));

        // Base Stats Box
        GameObject statsBox = CreateFrame("StatsBox", boxRect, new Color32(14, 42, 54, 255), TealBorder, out _);
        RectTransform statsBoxRect = statsBox.GetComponent<RectTransform>();
        Anchor(statsBoxRect, new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(880f, 140f));

        TMP_Text statsHeader = CreateText("StatsHeader", statsBoxRect, "BASE SPECIFICATIONS", 28f, BrightCyan, TextAlignmentOptions.Left);
        Anchor(statsHeader.rectTransform, new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(820f, 35f));

        baseStatsText = CreateText("BaseStats", statsBoxRect, "• ATK 42 | Tốc độ đánh: Fast", 30f, Color.white, TextAlignmentOptions.Left);
        Anchor(baseStatsText.rectTransform, new Vector2(0.5f, 0.35f), Vector2.zero, new Vector2(820f, 60f));

        // Perks Tier Box
        GameObject perksBox = CreateFrame("PerksBox", boxRect, new Color32(14, 42, 54, 255), TealBorder, out _);
        RectTransform perksBoxRect = perksBox.GetComponent<RectTransform>();
        Anchor(perksBoxRect, new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(880f, 380f));

        TMP_Text perksHeader = CreateText("PerksHeader", perksBoxRect, "TIER ENHANCEMENTS", 28f, BrightCyan, TextAlignmentOptions.Left);
        Anchor(perksHeader.rectTransform, new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(820f, 35f));

        magicText = CreateText("Magic", perksBoxRect, "• Magic: ATK +15%", 28f, new Color32(140, 220, 255, 255), TextAlignmentOptions.Left);
        Anchor(magicText.rectTransform, new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(820f, 40f));

        rareText = CreateText("Rare", perksBoxRect, "• Rare: ATK Speed +15%", 28f, new Color32(100, 160, 255, 255), TextAlignmentOptions.Left);
        Anchor(rareText.rectTransform, new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(820f, 40f));

        uniqueText = CreateText("Unique", perksBoxRect, "• Unique: +5% Life Steal", 28f, new Color32(255, 200, 80, 255), TextAlignmentOptions.Left);
        Anchor(uniqueText.rectTransform, new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(820f, 40f));

        epicText = CreateText("Epic", perksBoxRect, "• Epic: Adds Penetration Skill", 28f, new Color32(230, 100, 255, 255), TextAlignmentOptions.Left);
        Anchor(epicText.rectTransform, new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(820f, 40f));

        // Action Buttons: Equip & Upgrade
        GameObject equipObj = CreateFrame("EquipBtn", boxRect, BrightTeal, BrightCyan, out Image equipBg);
        RectTransform equipRect = equipObj.GetComponent<RectTransform>();
        Anchor(equipRect, new Vector2(0.3f, 0.07f), Vector2.zero, new Vector2(340f, 95f));
        equipBtnText = CreateText("Label", equipRect, "EQUIP", 38f, Color.white, TextAlignmentOptions.Center);
        Stretch(equipBtnText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        equipBg.raycastTarget = true;
        equipBtn = equipObj.AddComponent<Button>();
        equipBtn.targetGraphic = equipBg;

        GameObject upgradeObj = CreateFrame("UpgradeBtn", boxRect, Yellow, Border, out Image upgradeBg);
        RectTransform upgradeRect = upgradeObj.GetComponent<RectTransform>();
        Anchor(upgradeRect, new Vector2(0.72f, 0.07f), Vector2.zero, new Vector2(400f, 95f));
        upgradeBtnText = CreateText("Label", upgradeRect, "UPGRADE", 36f, new Color32(10, 20, 30, 255), TextAlignmentOptions.Center);
        Stretch(upgradeBtnText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        upgradeBg.raycastTarget = true;
        upgradeBtn = upgradeObj.AddComponent<Button>();
        upgradeBtn.targetGraphic = upgradeBg;

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
        Color[] rarityBackgroundColors =
        {
            new Color32(48, 94, 111, 255),
            new Color32(38, 82, 145, 255),
            new Color32(94, 55, 142, 255),
            new Color32(170, 128, 35, 255)
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

        LabUpgradeController controller = panel.gameObject.AddComponent<LabUpgradeController>();
        SerializedObject serializedController = new SerializedObject(controller);
        GetRequiredProperty(serializedController, "upgradeButton").objectReferenceValue = upgradeButton;
        GetRequiredProperty(serializedController, "energyBalanceText").objectReferenceValue = energyBalanceText;
        GetRequiredProperty(serializedController, "chipBalanceText").objectReferenceValue = chipBalanceText;
        GetRequiredProperty(serializedController, "redChipBalanceText").objectReferenceValue = redChipBalanceText;
        GetRequiredProperty(serializedController, "priceText").objectReferenceValue = priceText;
        GetRequiredProperty(serializedController, "resultText").objectReferenceValue = resultText;
        GetRequiredProperty(serializedController, "upgradeBackground").objectReferenceValue = upgradeBackground;

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
            GetRequiredRelativeProperty(item, "button").objectReferenceValue = buttons[i];
            GetRequiredRelativeProperty(item, "panel").objectReferenceValue = panels[i];
            GetRequiredRelativeProperty(item, "background").objectReferenceValue = backgrounds[i];
            GetRequiredRelativeProperty(item, "border").objectReferenceValue = borderImages[i];
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
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
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

    private static Sprite LoadChipsetSprite(string spriteName)
    {
        if (cachedChipsetSprites == null || cachedChipsetSprites.Length == 0)
        {
            CacheChipsetSprites();
        }
        return cachedChipsetSprites?.FirstOrDefault(sprite => string.Equals(sprite.name, spriteName, StringComparison.OrdinalIgnoreCase));
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
