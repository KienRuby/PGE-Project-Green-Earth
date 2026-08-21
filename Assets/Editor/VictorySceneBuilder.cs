#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VictorySceneBuilder
{
    private const string ScenePath = "Assets/Scenes/GamePlay.unity";
    private const string FontPath = "Assets/Fonts/Nunito/Nunito SDF.asset";
    private const string FontMaterialPath = "Assets/Fonts/Nunito/Nunito SDF - Stroke.mat";
    private const string CurrencyAtlasPath = "Assets/Sprites/UI/icon tài nguyên.png";

    private static readonly Color Dim = new Color32(8, 14, 12, 222);
    private static readonly Color Border = new Color32(6, 10, 12, 255);
    private static readonly Color Dark = new Color32(20, 30, 33, 255);
    private static readonly Color Card = new Color32(156, 87, 52, 255);
    private static readonly Color Ribbon = new Color32(163, 151, 113, 255);
    private static readonly Color Navy = new Color32(18, 30, 52, 255);
    private static readonly Color Gold = new Color32(255, 190, 61, 255);
    private static readonly Color Cream = new Color32(250, 250, 242, 255);
    private static readonly Color Cyan = new Color32(74, 211, 202, 255);
    private static readonly Color Red = new Color32(224, 71, 77, 255);

    private static TMP_FontAsset font;
    private static Material fontMaterial;

    [MenuItem("PGE/UI/Build Victory Panel")]
    public static void Build()
    {
        Build(true);
    }

    private static void Build(bool rebuildExisting)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[VictorySceneBuilder] Không thể dựng UI khi đang Play Mode.");
            return;
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedAdditively = !scene.IsValid() || !scene.isLoaded;
        if (openedAdditively)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        GameObject canvas = FindRootObject(scene, "Canvas");
        if (!scene.IsValid() || canvas == null)
        {
            Debug.LogError("[VictorySceneBuilder] Không tìm thấy GamePlay scene hoặc Canvas.");
            if (openedAdditively && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        if (!rebuildExisting
            && canvas.transform.Find("VictoryPanel") != null
            && canvas.GetComponent<VictoryPanelController>() != null)
        {
            if (openedAdditively) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        fontMaterial = AssetDatabase.LoadAssetAtPath<Material>(FontMaterialPath);
        if (font == null)
        {
            Debug.LogError("[VictorySceneBuilder] Không tìm thấy font Nunito SDF.");
            if (openedAdditively) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        RemoveExisting(canvas.transform, "VictoryPanel");
        VictoryPanelController oldController = canvas.GetComponent<VictoryPanelController>();
        if (oldController != null) UnityEngine.Object.DestroyImmediate(oldController);

        GameObject victoryPanel = CreateOverlay("VictoryPanel", canvas.transform);
        CanvasGroup canvasGroup = victoryPanel.AddComponent<CanvasGroup>();

        GameObject confettiObject = new GameObject("ConfettiRoot", typeof(RectTransform));
        confettiObject.transform.SetParent(victoryPanel.transform, false);
        RectTransform confettiRoot = confettiObject.GetComponent<RectTransform>();
        Stretch(confettiRoot);

        TMP_Text title = CreateText("Title", victoryPanel.transform, "CHAPTER COMPLETE!", 88f, Gold);
        SetRect(title.rectTransform, new Vector2(0f, 650f), new Vector2(1010f, 150f));

        RectTransform resultCard = BuildResultCard(
            victoryPanel.transform,
            out TMP_Text chapterText,
            out TMP_Text waveNumberText);

        BuildRewardRow(victoryPanel.transform, "DataChipReward", new Vector2(-60f, -275f),
            LoadSprite("data"), Cyan, out TMP_Text dataRewardText);
        BuildRewardRow(victoryPanel.transform, "RedGemReward", new Vector2(-60f, -405f),
            LoadSprite("red"), Red, out TMP_Text gemRewardText);

        Button detailsButton = CreateButton(
            "DetailsButton",
            victoryPanel.transform,
            "DETAILS",
            new Vector2(330f, -340f),
            new Vector2(245f, 170f),
            Dark,
            36f);

        TMP_Text feedbackText = CreateText("FeedbackText", victoryPanel.transform, "CHAPTER UNLOCKED!", 31f, Gold);
        SetRect(feedbackText.rectTransform, new Vector2(0f, -535f), new Vector2(850f, 65f));

        Button vipButton = CreateButton(
            "VipTripleButton",
            victoryPanel.transform,
            "VIP  GET 3X REWARD",
            new Vector2(0f, -680f),
            new Vector2(650f, 145f),
            Navy,
            44f);
        TMP_Text vipButtonText = vipButton.transform.Find("Label").GetComponent<TMP_Text>();

        Button homeButton = CreateButton(
            "HomeButton",
            victoryPanel.transform,
            "HOME",
            new Vector2(0f, -850f),
            new Vector2(390f, 105f),
            new Color32(78, 111, 128, 255),
            38f);

        GameObject detailsPanel = BuildDetailsPanel(
            victoryPanel.transform,
            out TMP_Text detailsText,
            out Button closeDetailsButton);

        VictoryPanelController controller = canvas.AddComponent<VictoryPanelController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("enemySpawner").objectReferenceValue = UnityEngine.Object.FindObjectOfType<EnemySpawner>();
        serialized.FindProperty("playerRunEndController").objectReferenceValue = canvas.GetComponent<PlayerRunEndController>();
        serialized.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
        serialized.FindProperty("panelCanvasGroup").objectReferenceValue = canvasGroup;
        serialized.FindProperty("resultCard").objectReferenceValue = resultCard;
        serialized.FindProperty("confettiRoot").objectReferenceValue = confettiRoot;
        serialized.FindProperty("chapterText").objectReferenceValue = chapterText;
        serialized.FindProperty("waveNumberText").objectReferenceValue = waveNumberText;
        serialized.FindProperty("dataChipRewardText").objectReferenceValue = dataRewardText;
        serialized.FindProperty("redGemRewardText").objectReferenceValue = gemRewardText;
        serialized.FindProperty("feedbackText").objectReferenceValue = feedbackText;
        serialized.FindProperty("detailsPanel").objectReferenceValue = detailsPanel;
        serialized.FindProperty("detailsText").objectReferenceValue = detailsText;
        serialized.FindProperty("detailsButton").objectReferenceValue = detailsButton;
        serialized.FindProperty("closeDetailsButton").objectReferenceValue = closeDetailsButton;
        serialized.FindProperty("vipTripleButton").objectReferenceValue = vipButton;
        serialized.FindProperty("vipButtonText").objectReferenceValue = vipButtonText;
        serialized.FindProperty("homeButton").objectReferenceValue = homeButton;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        victoryPanel.SetActive(false);
        detailsPanel.SetActive(false);
        victoryPanel.transform.SetAsLastSibling();
        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (openedAdditively) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[VictorySceneBuilder] Đã tạo và liên kết hoàn chỉnh Victory Panel trong GamePlay scene.");
    }

    private static RectTransform BuildResultCard(Transform parent, out TMP_Text chapterText, out TMP_Text waveNumberText)
    {
        GameObject leftRibbon = CreateFrame("RibbonLeft", parent, new Vector2(-360f, 130f), new Vector2(250f, 220f), Ribbon);
        GameObject rightRibbon = CreateFrame("RibbonRight", parent, new Vector2(360f, 130f), new Vector2(250f, 220f), Ribbon);
        leftRibbon.transform.SetSiblingIndex(1);
        rightRibbon.transform.SetSiblingIndex(1);

        GameObject card = CreateFrame("ResultCard", parent, new Vector2(0f, 120f), new Vector2(520f, 590f), Card);
        chapterText = CreateText("ChapterText", card.transform, "CHAPTER. 01", 47f, Cream);
        SetRect(chapterText.rectTransform, new Vector2(0f, 205f), new Vector2(470f, 80f));

        waveNumberText = CreateText("WaveNumberText", card.transform, "09", 150f, Gold);
        SetRect(waveNumberText.rectTransform, new Vector2(0f, 35f), new Vector2(470f, 220f));

        TMP_Text wavesLabel = CreateText("WavesLabel", card.transform, "WAVES", 65f, Cream);
        SetRect(wavesLabel.rectTransform, new Vector2(0f, -150f), new Vector2(470f, 100f));
        return card.GetComponent<RectTransform>();
    }

    private static void BuildRewardRow(
        Transform parent,
        string name,
        Vector2 position,
        Sprite iconSprite,
        Color iconColor,
        out TMP_Text rewardText)
    {
        GameObject row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        SetRect(row.GetComponent<RectTransform>(), position, new Vector2(640f, 105f));

        if (iconSprite != null)
        {
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(row.transform, false);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = iconSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            SetRect(icon.rectTransform, new Vector2(-225f, 0f), new Vector2(82f, 82f));
        }
        else
        {
            TMP_Text fallback = CreateText("Icon", row.transform, "◆", 64f, iconColor);
            SetRect(fallback.rectTransform, new Vector2(-225f, 0f), new Vector2(90f, 90f));
        }

        rewardText = CreateText("Value", row.transform, "GET 0", 52f, Cream);
        rewardText.alignment = TextAlignmentOptions.Left;
        SetRect(rewardText.rectTransform, new Vector2(90f, 0f), new Vector2(470f, 95f));
    }

    private static GameObject BuildDetailsPanel(Transform parent, out TMP_Text detailsText, out Button closeButton)
    {
        GameObject overlay = CreateOverlay("DetailsPanel", parent);
        overlay.GetComponent<Image>().color = new Color32(4, 8, 9, 235);

        GameObject card = CreateFrame("DetailsCard", overlay.transform, Vector2.zero, new Vector2(780f, 720f), Navy);
        TMP_Text title = CreateText("Title", card.transform, "VICTORY DETAILS", 56f, Gold);
        SetRect(title.rectTransform, new Vector2(0f, 255f), new Vector2(700f, 90f));

        detailsText = CreateText("DetailsText", card.transform, "CHAPTER COMPLETE", 40f, Cream);
        detailsText.alignment = TextAlignmentOptions.Left;
        detailsText.lineSpacing = 18f;
        SetRect(detailsText.rectTransform, new Vector2(0f, 25f), new Vector2(620f, 340f));

        closeButton = CreateButton("CloseButton", card.transform, "CLOSE", new Vector2(0f, -260f), new Vector2(360f, 105f), Ribbon, 38f);
        return overlay;
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
        rect.offsetMin = new Vector2(8f, 8f);
        rect.offsetMax = new Vector2(-8f, -8f);
        return border;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Vector2 position,
        Vector2 size,
        Color fill,
        float fontSize)
    {
        GameObject frame = CreateFrame(name, parent, position, size, fill);
        Button button = frame.AddComponent<Button>();
        button.targetGraphic = frame.transform.Find("Fill").GetComponent<Image>();
        TMP_Text text = CreateText("Label", frame.transform, label, fontSize, Cream);
        Stretch(text.rectTransform);
        return button;
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

    private static Sprite LoadSprite(string spriteName)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(CurrencyAtlasPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite && string.Equals(sprite.name, spriteName, StringComparison.OrdinalIgnoreCase))
            {
                return sprite;
            }
        }
        return null;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
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

    private static void RemoveExisting(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
    }

    private static GameObject FindRootObject(Scene scene, string objectName)
    {
        if (!scene.IsValid()) return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == objectName) return roots[i];
        }
        return null;
    }
}
#endif
