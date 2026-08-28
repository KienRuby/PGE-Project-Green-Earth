#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PlayerRunEndSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/GamePlay.unity";
    private const string FontPath = "Assets/Fonts/Nunito/Nunito SDF.asset";
    private const string FontMaterialPath = "Assets/Fonts/Nunito/Nunito SDF - Stroke.mat";
    private const string ReviveUiPath = "Assets/Sprites/UI/UI Player/nút màn revive.png";
    private const string CurrencyAtlasPath = "Assets/Sprites/UI/icon tài nguyên.png";
    private const string GameOverPanelPath = "Assets/Sprites/UI/UI Player/popup game over.png";
    private const string RewardButtonsPath = "Assets/Sprites/UI/UI Player/nút màn chapter complete_game over.png";
    private const string DetailsChartIconPath = "Assets/Sprites/UI/icon-damage-details.png";

    private static readonly Color Dim = new Color32(5, 12, 10, 220);
    private static readonly Color Dark = new Color32(18, 28, 34, 255);
    private static readonly Color Border = new Color32(7, 11, 14, 255);
    private static readonly Color Slate = new Color32(99, 126, 143, 255);
    private static readonly Color Navy = new Color32(20, 31, 52, 255);
    private static readonly Color Gold = new Color32(255, 190, 61, 255);
    private static readonly Color Cream = new Color32(248, 248, 238, 255);
    private static readonly Color Coral = new Color32(231, 108, 65, 255);
    private static readonly Color Cyan = new Color32(78, 206, 196, 255);
    private static readonly Color Orange = new Color32(255, 160, 32, 255);

    private static TMP_FontAsset font;
    private static Material fontMaterial;

    [MenuItem("PGE/UI/Build Revive & Game Over")]
    public static void BuildFromMenu()
    {
        Build();
    }

    [MenuItem("PGE/UI/Build Revive Panel Only")]
    public static void BuildReviveOnly()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[PlayerRunEndSceneBuilder] Không thể build khi đang Play Mode.");
            return;
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool wasAlreadyLoaded = scene.IsValid() && scene.isLoaded;
        if (!wasAlreadyLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        GameObject canvas = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Canvas");
        PlayerRunEndController controller = canvas != null ? canvas.GetComponent<PlayerRunEndController>() : null;
        if (canvas == null || controller == null)
        {
            Debug.LogError("[PlayerRunEndSceneBuilder] Không tìm thấy Canvas hoặc PlayerRunEndController.");
            if (!wasAlreadyLoaded && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        fontMaterial = AssetDatabase.LoadAssetAtPath<Material>(FontMaterialPath);
        RemoveExisting(canvas.transform, "RevivePanel");
        GameObject revivePanel = BuildRevivePanel(
            canvas.transform,
            out CanvasGroup reviveCanvasGroup,
            out RectTransform reviveContent,
            out Button noButton,
            out Button gemButton,
            out Button adButton,
            out TMP_Text reviveFeedback);

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("revivePanel").objectReferenceValue = revivePanel;
        serialized.FindProperty("reviveCanvasGroup").objectReferenceValue = reviveCanvasGroup;
        serialized.FindProperty("reviveContent").objectReferenceValue = reviveContent;
        serialized.FindProperty("noButton").objectReferenceValue = noButton;
        serialized.FindProperty("gemReviveButton").objectReferenceValue = gemButton;
        serialized.FindProperty("adReviveButton").objectReferenceValue = adButton;
        serialized.FindProperty("reviveFeedbackText").objectReferenceValue = reviveFeedback;
        serialized.FindProperty("reviveGemCost").intValue = 200;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        revivePanel.SetActive(false);
        revivePanel.transform.SetAsLastSibling();
        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (!wasAlreadyLoaded) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[PlayerRunEndSceneBuilder] Đã setup Revive Panel độc lập.");
    }

    [MenuItem("PGE/UI/Build Game Over Panel Only")]
    public static void BuildGameOverOnly()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[PlayerRunEndSceneBuilder] Không thể build khi đang Play Mode.");
            return;
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool wasAlreadyLoaded = scene.IsValid() && scene.isLoaded;
        if (!wasAlreadyLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        GameObject canvas = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Canvas");
        PlayerRunEndController controller = canvas != null ? canvas.GetComponent<PlayerRunEndController>() : null;
        if (canvas == null || controller == null)
        {
            Debug.LogError("[PlayerRunEndSceneBuilder] Không tìm thấy Canvas hoặc PlayerRunEndController.");
            if (!wasAlreadyLoaded && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        fontMaterial = AssetDatabase.LoadAssetAtPath<Material>(FontMaterialPath);
        RemoveExisting(canvas.transform, "GameOverPanel");
        RemoveExisting(canvas.transform, "DamageDetailsModal");

        DamageDetailsPopup damageDetailsModal = BuildDamageDetailsModal(canvas.transform);

        GameObject gameOverPanel = BuildGameOverPanel(
            canvas.transform,
            out TMP_Text chipRewardText,
            out TMP_Text gemRewardText,
            out Button detailsButton,
            out Button getRewardButton,
            out Button tripleButton,
            out TMP_Text feedbackText);

        if (gameOverPanel == null)
        {
            Debug.LogError("[PlayerRunEndSceneBuilder] Thiếu sprite Game Over hoặc reward button.");
            if (!wasAlreadyLoaded && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
        serialized.FindProperty("chapterText").objectReferenceValue = null;
        serialized.FindProperty("wavesText").objectReferenceValue = null;
        serialized.FindProperty("progressText").objectReferenceValue = null;
        serialized.FindProperty("dataChipRewardText").objectReferenceValue = chipRewardText;
        serialized.FindProperty("redGemRewardText").objectReferenceValue = gemRewardText;
        serialized.FindProperty("detailsButton").objectReferenceValue = detailsButton;
        serialized.FindProperty("damageDetailsPopup").objectReferenceValue = damageDetailsModal;
        serialized.FindProperty("getRewardButton").objectReferenceValue = getRewardButton;
        serialized.FindProperty("vipTripleButton").objectReferenceValue = tripleButton;
        serialized.FindProperty("gameOverFeedbackText").objectReferenceValue = feedbackText;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        gameOverPanel.SetActive(false);
        gameOverPanel.transform.SetAsLastSibling();
        if (damageDetailsModal != null)
        {
            damageDetailsModal.gameObject.SetActive(false);
            damageDetailsModal.transform.SetAsLastSibling();
        }

        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (!wasAlreadyLoaded) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[PlayerRunEndSceneBuilder] Đã setup Game Over và Damage Details Modal đồng bộ.");
    }

    public static void Build()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[PlayerRunEndSceneBuilder] Không thể build khi đang Play Mode.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject canvas = GameObject.Find("Canvas");
        if (!scene.IsValid() || canvas == null)
        {
            Debug.LogError("[PlayerRunEndSceneBuilder] Không tìm thấy GamePlay scene hoặc Canvas.");
            return;
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        fontMaterial = AssetDatabase.LoadAssetAtPath<Material>(FontMaterialPath);
        RemoveExisting(canvas.transform, "RevivePanel");
        RemoveExisting(canvas.transform, "GameOverPanel");
        RemoveExisting(canvas.transform, "DamageDetailsModal");

        PlayerRunEndController oldController = canvas.GetComponent<PlayerRunEndController>();
        if (oldController != null) Object.DestroyImmediate(oldController);

        DamageDetailsPopup damageDetailsModal = BuildDamageDetailsModal(canvas.transform);

        GameObject revivePanel = BuildRevivePanel(
            canvas.transform,
            out CanvasGroup reviveCanvasGroup,
            out RectTransform reviveContent,
            out Button noButton,
            out Button gemButton,
            out Button adButton,
            out TMP_Text reviveFeedback);
        GameObject gameOverPanel = BuildGameOverPanel(
            canvas.transform,
            out TMP_Text chipRewardText,
            out TMP_Text gemRewardText,
            out Button detailsButton,
            out Button getRewardButton,
            out Button tripleButton,
            out TMP_Text gameOverFeedback);

        PlayerRunEndController controller = canvas.AddComponent<PlayerRunEndController>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("playerHealth").objectReferenceValue = player != null ? player.GetComponent<PlayerHealth>() : null;
        serialized.FindProperty("playerDeathController").objectReferenceValue = player != null ? player.GetComponent<PlayerDeathController>() : null;
        serialized.FindProperty("enemySpawner").objectReferenceValue = Object.FindObjectOfType<EnemySpawner>();
        serialized.FindProperty("revivePanel").objectReferenceValue = revivePanel;
        serialized.FindProperty("reviveCanvasGroup").objectReferenceValue = reviveCanvasGroup;
        serialized.FindProperty("reviveContent").objectReferenceValue = reviveContent;
        serialized.FindProperty("noButton").objectReferenceValue = noButton;
        serialized.FindProperty("gemReviveButton").objectReferenceValue = gemButton;
        serialized.FindProperty("adReviveButton").objectReferenceValue = adButton;
        serialized.FindProperty("reviveFeedbackText").objectReferenceValue = reviveFeedback;
        serialized.FindProperty("reviveGemCost").intValue = 200;
        serialized.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
        serialized.FindProperty("chapterText").objectReferenceValue = null;
        serialized.FindProperty("wavesText").objectReferenceValue = null;
        serialized.FindProperty("progressText").objectReferenceValue = null;
        serialized.FindProperty("dataChipRewardText").objectReferenceValue = chipRewardText;
        serialized.FindProperty("redGemRewardText").objectReferenceValue = gemRewardText;
        serialized.FindProperty("detailsButton").objectReferenceValue = detailsButton;
        serialized.FindProperty("damageDetailsPopup").objectReferenceValue = damageDetailsModal;
        serialized.FindProperty("getRewardButton").objectReferenceValue = getRewardButton;
        serialized.FindProperty("vipTripleButton").objectReferenceValue = tripleButton;
        serialized.FindProperty("gameOverFeedbackText").objectReferenceValue = gameOverFeedback;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        revivePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        if (damageDetailsModal != null)
        {
            damageDetailsModal.gameObject.SetActive(false);
            damageDetailsModal.transform.SetAsLastSibling();
        }

        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[PlayerRunEndSceneBuilder] Đã tạo Revive Panel, Game Over Panel và Damage Details Popup.");
    }

    private static GameObject BuildRevivePanel(
        Transform parent,
        out CanvasGroup canvasGroup,
        out RectTransform content,
        out Button noButton,
        out Button gemButton,
        out Button adButton,
        out TMP_Text feedback)
    {
        GameObject root = CreateOverlay("RevivePanel", parent);
        root.GetComponent<Image>().color = new Color32(0, 0, 0, 128);
        canvasGroup = root.AddComponent<CanvasGroup>();

        Sprite bgSprite = LoadSprite(ReviveUiPath, "nút màn revive_2");
        Sprite gemBtnSprite = LoadSprite(ReviveUiPath, "nút màn revive_1");
        Sprite adBtnSprite = LoadSprite(ReviveUiPath, "nút màn revive_0");
        Sprite cancelSprite = LoadSprite(ReviveUiPath, "nút màn revive_3");

        Image panel = CreateSpriteImage("ReviveContent", root.transform, bgSprite);
        content = panel.rectTransform;
        SetRect(content, new Vector2(0f, 0f), new Vector2(924f, 1374f));

        adButton = CreateSpriteButton("AdReviveButton", panel.transform, adBtnSprite, new Vector2(0f, -270f), new Vector2(620f, 172f));
        gemButton = CreateSpriteButton("GemReviveButton", panel.transform, gemBtnSprite, new Vector2(0f, -440f), new Vector2(400f, 136f));
        noButton = CreateSpriteButton("NoButton", panel.transform, cancelSprite, new Vector2(0f, -575f), new Vector2(280f, 85f));

        feedback = CreateText("FeedbackText", panel.transform, string.Empty, 28f, Coral);
        SetRect(feedback.rectTransform, new Vector2(0f, -200f), new Vector2(760f, 50f));
        return root;
    }

    private static Image CreateSpriteImage(string name, Transform parent, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        return image;
    }

    private static Button CreateSpriteButton(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
    {
        Image image = CreateSpriteImage(name, parent, sprite);
        image.raycastTarget = true;
        SetRect(image.rectTransform, position, size);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.78f, 0.9f, 0.9f, 1f);
        colors.disabledColor = new Color(0.45f, 0.52f, 0.52f, 0.75f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return button;
    }

    private static RectTransform BuildRewardRow(Transform parent, string name, Vector2 position, Sprite iconSprite, out TMP_Text rewardText)
    {
        GameObject row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        SetRect(rowRect, position, new Vector2(340f, 118f));

        Image icon = CreateSpriteImage("Icon", row.transform, iconSprite);
        SetRect(icon.rectTransform, new Vector2(-95f, 0f), new Vector2(88f, 88f));

        rewardText = CreateText("Value", row.transform, "Get 0", 44f, Color.white);
        rewardText.alignment = TextAlignmentOptions.Left;
        SetRect(rewardText.rectTransform, new Vector2(55f, 0f), new Vector2(210f, 95f));
        return rowRect;
    }

    private static Sprite LoadSprite(string path, string spriteName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => string.Equals(sprite.name, spriteName, System.StringComparison.OrdinalIgnoreCase));
    }

    private static Sprite LoadSpriteBySuffix(string path, string suffix)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name.EndsWith(suffix, System.StringComparison.Ordinal));
    }

    private static GameObject BuildGameOverPanel(
        Transform parent,
        out TMP_Text chipRewardText,
        out TMP_Text gemRewardText,
        out Button detailsButton,
        out Button getRewardButton,
        out Button tripleButton,
        out TMP_Text feedbackText)
    {
        Sprite panelSprite = LoadSprite(GameOverPanelPath, "Game over");
        Sprite normalButtonSprite = LoadSpriteBySuffix(RewardButtonsPath, "_0");
        Sprite tripleButtonSprite = LoadSpriteBySuffix(RewardButtonsPath, "_1");
        Sprite dataIcon = LoadSprite(CurrencyAtlasPath, "data");
        Sprite gemIcon = LoadSprite(CurrencyAtlasPath, "red");
        Sprite chartSprite = LoadSprite(DetailsChartIconPath, "icon-damage-details");

        if (panelSprite == null || normalButtonSprite == null || tripleButtonSprite == null)
        {
            chipRewardText = null;
            gemRewardText = null;
            detailsButton = null;
            getRewardButton = null;
            tripleButton = null;
            feedbackText = null;
            return null;
        }

        GameObject root = CreateOverlay("GameOverPanel", parent);
        root.GetComponent<Image>().color = new Color32(4, 9, 13, 176);

        Image panel = CreateSpriteImage("GameOverContent", root.transform, panelSprite);
        SetRect(panel.rectTransform, new Vector2(0f, -8f), new Vector2(780f, 1218f));

        BuildRewardRow(panel.transform, "DataChipReward", new Vector2(-110f, 135f), dataIcon, out chipRewardText);
        BuildRewardRow(panel.transform, "RedGemReward", new Vector2(-110f, 10f), gemIcon, out gemRewardText);

        // Details Button (Icon biểu đồ + Chữ Details màu cam)
        GameObject detailsBtnObj = new GameObject("DetailsButton", typeof(RectTransform), typeof(Image), typeof(Button));
        detailsBtnObj.transform.SetParent(panel.transform, false);
        SetRect(detailsBtnObj.GetComponent<RectTransform>(), new Vector2(175f, 72f), new Vector2(140f, 180f));
        Image detailsBtnImg = detailsBtnObj.GetComponent<Image>();
        detailsBtnImg.color = Color.clear;
        detailsBtnImg.raycastTarget = true;
        detailsButton = detailsBtnObj.GetComponent<Button>();

        Image detailsIcon = CreateSpriteImage("Icon", detailsBtnObj.transform, chartSprite);
        SetRect(detailsIcon.rectTransform, new Vector2(0f, 28f), new Vector2(72f, 72f));

        TMP_Text detailsLabel = CreateText("Label", detailsBtnObj.transform, "Details", 30f, Orange);
        SetRect(detailsLabel.rectTransform, new Vector2(0f, -34f), new Vector2(140f, 40f));

        getRewardButton = CreateSpriteButton(
            "GetRewardButton", panel.transform, normalButtonSprite, new Vector2(0f, -230f), new Vector2(320f, 158f));
        tripleButton = CreateSpriteButton(
            "VipTripleButton", panel.transform, tripleButtonSprite, new Vector2(0f, -405f), new Vector2(320f, 158f));

        feedbackText = CreateText("FeedbackText", panel.transform, string.Empty, 24f, new Color32(255, 240, 116, 255));
        SetRect(feedbackText.rectTransform, new Vector2(0f, -535f), new Vector2(600f, 52f));
        return root;
    }

    public static DamageDetailsPopup BuildDamageDetailsModal(Transform parent)
    {
        RemoveExisting(parent, "DamageDetailsModal");

        GameObject root = CreateOverlay("DamageDetailsModal", parent);
        root.GetComponent<Image>().color = new Color32(4, 9, 13, 235);
        CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();

        // Nút bấm ngoài nền để đóng popup
        GameObject bgCloseObj = new GameObject("BackgroundCloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        bgCloseObj.transform.SetParent(root.transform, false);
        Stretch(bgCloseObj.GetComponent<RectTransform>());
        Image bgCloseImg = bgCloseObj.GetComponent<Image>();
        bgCloseImg.color = Color.clear;
        bgCloseImg.raycastTarget = true;
        Button bgCloseBtn = bgCloseObj.GetComponent<Button>();

        // Khung chính Modal: 840 x 1060
        GameObject frameObj = CreateFrame("MainFrame", root.transform, new Vector2(0f, 0f), new Vector2(840f, 1060f), new Color32(11, 24, 34, 255));
        RectTransform frameRt = frameObj.GetComponent<RectTransform>();

        Sprite chartSprite = LoadSprite(DetailsChartIconPath, "icon-damage-details");

        // Header Title: Icon biểu đồ + "Damage Details"
        GameObject titleContainer = new GameObject("TitleContainer", typeof(RectTransform));
        titleContainer.transform.SetParent(frameObj.transform, false);
        RectTransform titleRt = titleContainer.GetComponent<RectTransform>();
        SetRect(titleRt, new Vector2(0f, 475f), new Vector2(500f, 70f));

        Image headerIcon = CreateSpriteImage("ChartIcon", titleContainer.transform, chartSprite);
        SetRect(headerIcon.rectTransform, new Vector2(-150f, 0f), new Vector2(50f, 50f));

        TMP_Text titleText = CreateText("Title", titleContainer.transform, "Damage Details", 38f, Orange);
        titleText.alignment = TextAlignmentOptions.Left;
        SetRect(titleText.rectTransform, new Vector2(40f, 0f), new Vector2(320f, 60f));

        // Nút Đóng 'X' góc trên bên phải
        GameObject closeObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeObj.transform.SetParent(frameObj.transform, false);
        SetRect(closeObj.GetComponent<RectTransform>(), new Vector2(365f, 475f), new Vector2(54f, 54f));
        Image closeImg = closeObj.GetComponent<Image>();
        closeImg.color = new Color32(24, 48, 62, 255);
        Button closeBtn = closeObj.GetComponent<Button>();
        TMP_Text closeText = CreateText("Label", closeObj.transform, "X", 32f, Cream);
        Stretch(closeText.rectTransform);

        // Header Bảng: CHIPSET, DPS, D %, Damage, Time
        GameObject headerRow = new GameObject("TableHeader", typeof(RectTransform), typeof(Image));
        headerRow.transform.SetParent(frameObj.transform, false);
        headerRow.GetComponent<Image>().color = new Color32(16, 42, 56, 255);
        SetRect(headerRow.GetComponent<RectTransform>(), new Vector2(0f, 410f), new Vector2(780f, 46f));

        TMP_Text hChip = CreateText("H_CHIPSET", headerRow.transform, "CHIPSET", 22f, Cyan);
        hChip.alignment = TextAlignmentOptions.Left;
        SetRect(hChip.rectTransform, new Vector2(-260f, 0f), new Vector2(200f, 40f));

        TMP_Text hDPS = CreateText("H_DPS", headerRow.transform, "DPS", 22f, Cyan);
        SetRect(hDPS.rectTransform, new Vector2(-75f, 0f), new Vector2(90f, 40f));

        TMP_Text hD = CreateText("H_DPercent", headerRow.transform, "D %", 22f, Cyan);
        SetRect(hD.rectTransform, new Vector2(35f, 0f), new Vector2(90f, 40f));

        TMP_Text hDmg = CreateText("H_Damage", headerRow.transform, "Damage", 22f, Cyan);
        SetRect(hDmg.rectTransform, new Vector2(165f, 0f), new Vector2(130f, 40f));

        TMP_Text hTime = CreateText("H_Time", headerRow.transform, "Time", 22f, Cyan);
        SetRect(hTime.rectTransform, new Vector2(295f, 0f), new Vector2(90f, 40f));

        // Khung cuộn danh sách (ScrollRect)
        GameObject scrollObj = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(frameObj.transform, false);
        SetRect(scrollObj.GetComponent<RectTransform>(), new Vector2(0f, -60f), new Vector2(780f, 840f));
        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 25f;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollObj.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.GetComponent<Image>().color = Color.white;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(6, 6, 6, 6);

        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRt;

        // Row Template
        GameObject rowTemplate = BuildRowTemplate(content.transform);
        rowTemplate.SetActive(false);

        // Load Chipset Icons
        Sprite[] chipIcons = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/Chipset/icon chipset.png").OfType<Sprite>().ToArray();
        if (chipIcons == null || chipIcons.Length == 0)
        {
            chipIcons = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/Chipset/icon chipset (1) 1.png").OfType<Sprite>().ToArray();
        }

        DamageDetailsPopup popup = root.AddComponent<DamageDetailsPopup>();
        SerializedObject popupSo = new SerializedObject(popup);
        popupSo.FindProperty("popupRoot").objectReferenceValue = root;
        popupSo.FindProperty("popupCanvasGroup").objectReferenceValue = canvasGroup;
        popupSo.FindProperty("modalFrame").objectReferenceValue = frameRt;
        popupSo.FindProperty("rowsContainer").objectReferenceValue = content.transform;
        popupSo.FindProperty("rowPrefab").objectReferenceValue = rowTemplate;
        popupSo.FindProperty("titleText").objectReferenceValue = titleText;
        popupSo.FindProperty("chartIcon").objectReferenceValue = headerIcon;
        popupSo.FindProperty("closeButton").objectReferenceValue = closeBtn;
        popupSo.FindProperty("backgroundCloseButton").objectReferenceValue = bgCloseBtn;

        SerializedProperty iconsProp = popupSo.FindProperty("chipIcons");
        iconsProp.arraySize = chipIcons.Length;
        for (int i = 0; i < chipIcons.Length; i++)
        {
            iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = chipIcons[i];
        }

        popupSo.ApplyModifiedPropertiesWithoutUndo();
        root.SetActive(false);
        return popup;
    }

    private static GameObject BuildRowTemplate(Transform parent)
    {
        GameObject row = new GameObject("DamageDetailRowTemplate", typeof(RectTransform), typeof(Image), typeof(DamageDetailRowUI), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(768f, 74f);
        row.GetComponent<Image>().color = new Color32(13, 34, 46, 255);
        row.GetComponent<LayoutElement>().minHeight = 74f;
        row.GetComponent<LayoutElement>().preferredHeight = 74f;

        // Thanh tiến trình nền Teal / Cyan thể hiện tỷ lệ % sát thương
        GameObject fillObj = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(row.transform, false);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(0.5f, 1f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        Image fillImg = fillObj.GetComponent<Image>();
        fillImg.color = new Color32(46, 125, 122, 235);

        // Khung Icon vũ khí
        GameObject iconFrame = new GameObject("IconFrame", typeof(RectTransform), typeof(Image));
        iconFrame.transform.SetParent(row.transform, false);
        SetRect(iconFrame.GetComponent<RectTransform>(), new Vector2(-320f, 0f), new Vector2(56f, 56f));
        iconFrame.GetComponent<Image>().color = new Color32(24, 64, 76, 255);

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(iconFrame.transform, false);
        Stretch(iconObj.GetComponent<RectTransform>());
        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.preserveAspect = true;

        // Tên Chipset
        TMP_Text nameText = CreateText("Name", row.transform, "Standard Gun", 22f, Color.white);
        nameText.alignment = TextAlignmentOptions.Left;
        SetRect(nameText.rectTransform, new Vector2(-215f, 0f), new Vector2(140f, 60f));

        // DPS
        TMP_Text dpsText = CreateText("DPS", row.transform, "357", 24f, Color.white);
        SetRect(dpsText.rectTransform, new Vector2(-75f, 0f), new Vector2(90f, 60f));

        // D %
        TMP_Text percentText = CreateText("DPercent", row.transform, "59.8%", 24f, Color.white);
        SetRect(percentText.rectTransform, new Vector2(35f, 0f), new Vector2(90f, 60f));

        // Damage
        TMP_Text damageText = CreateText("Damage", row.transform, "5,108", 24f, Color.white);
        SetRect(damageText.rectTransform, new Vector2(165f, 0f), new Vector2(130f, 60f));

        // Time
        TMP_Text timeText = CreateText("Time", row.transform, "00:14", 24f, Color.white);
        SetRect(timeText.rectTransform, new Vector2(295f, 0f), new Vector2(90f, 60f));

        DamageDetailRowUI rowUI = row.GetComponent<DamageDetailRowUI>();
        SerializedObject rSo = new SerializedObject(rowUI);
        rSo.FindProperty("rowRect").objectReferenceValue = rowRt;
        rSo.FindProperty("progressFillRect").objectReferenceValue = fillRt;
        rSo.FindProperty("progressFillImage").objectReferenceValue = fillImg;
        rSo.FindProperty("iconImage").objectReferenceValue = iconImg;
        rSo.FindProperty("nameText").objectReferenceValue = nameText;
        rSo.FindProperty("dpsText").objectReferenceValue = dpsText;
        rSo.FindProperty("percentText").objectReferenceValue = percentText;
        rSo.FindProperty("damageText").objectReferenceValue = damageText;
        rSo.FindProperty("timeText").objectReferenceValue = timeText;
        rSo.ApplyModifiedPropertiesWithoutUndo();

        return row;
    }

    private static GameObject CreateOverlay(string name, Transform parent)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);
        Stretch(root.GetComponent<RectTransform>());
        Image image = root.GetComponent<Image>();
        image.color = Dim;
        image.raycastTarget = true;
        return root;
    }

    private static GameObject CreateFrame(string name, Transform parent, Vector2 position, Vector2 size, Color fill)
    {
        GameObject border = new GameObject(name, typeof(RectTransform), typeof(Image));
        border.transform.SetParent(parent, false);
        border.GetComponent<Image>().color = Border;
        SetRect(border.GetComponent<RectTransform>(), position, size);

        GameObject inner = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(border.transform, false);
        inner.GetComponent<Image>().color = fill;
        RectTransform rect = inner.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(7f, 7f);
        rect.offsetMax = new Vector2(-7f, -7f);
        return border;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text text = go.GetComponent<TMP_Text>();
        text.font = font;
        if (fontMaterial != null) text.fontSharedMaterial = fontMaterial;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void RemoveExisting(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null) Object.DestroyImmediate(child.gameObject);
    }
}
#endif
