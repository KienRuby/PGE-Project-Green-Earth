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
    private const string RewardStrokeMaterialPath = "Assets/Fonts/Nunito/Nunito SDF - RewardStroke.mat";
    private const string CompleteSpritePath = "Assets/Sprites/UI/nút chapter complete.png";
    private const string CurrencyAtlasPath = "Assets/Sprites/UI/icon tài nguyên.png";

    private static readonly Color Dim = new Color(0f, 0f, 0f, 0.45f);
    private static readonly Color Feedback = new Color32(255, 240, 116, 255);

    private static TMP_FontAsset font;
    private static Material fontMaterial;
    private static Material rewardFontMaterial;

    [InitializeOnLoadMethod]
    private static void AutoBuildOnCompile()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Build();
            }
        };
    }

    [MenuItem("PGE/UI/Build Victory Panel")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[VictorySceneBuilder] Không thể dựng UI khi đang Play Mode.");
            return;
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedAdditively = !scene.IsValid() || !scene.isLoaded;
        if (openedAdditively) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        GameObject canvas = FindRootObject(scene, "Canvas");
        if (!scene.IsValid() || canvas == null)
        {
            Debug.LogError("[VictorySceneBuilder] Không tìm thấy GamePlay scene hoặc Canvas.");
            if (openedAdditively && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        fontMaterial = AssetDatabase.LoadAssetAtPath<Material>(FontMaterialPath);
        rewardFontMaterial = AssetDatabase.LoadAssetAtPath<Material>(RewardStrokeMaterialPath);

        // Load 4 sprite chuẩn từ sheet nút chapter complete.png theo đúng ảnh mẫu (Image 2)
        Sprite completeCrestSprite = LoadSprite(CompleteSpritePath, "complete");
        Sprite detailsButtonSprite = LoadSprite(CompleteSpritePath, "Detail");
        Sprite normalButtonSprite = LoadSprite(CompleteSpritePath, "Get");
        Sprite tripleButtonSprite = LoadSprite(CompleteSpritePath, "Get x3");

        // Load 2 sprite icon tiền tệ từ icon tài nguyên.png
        Sprite dataChipSprite = LoadSprite(CurrencyAtlasPath, "data");
        Sprite redGemSprite = LoadSprite(CurrencyAtlasPath, "red");

        if (completeCrestSprite == null || detailsButtonSprite == null || normalButtonSprite == null || tripleButtonSprite == null)
        {
            Debug.LogError("[VictorySceneBuilder] Thiếu sprite trong 'Assets/Sprites/UI/nút chapter complete.png'.");
            if (openedAdditively) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        RemoveExisting(canvas.transform, "VictoryPanel");
        VictoryPanelController oldController = canvas.GetComponent<VictoryPanelController>();
        if (oldController != null) UnityEngine.Object.DestroyImmediate(oldController);

        // Khung nền tối mờ phủ toàn màn hình (cho phép thấy gameplay phía sau như ảnh mẫu)
        GameObject victoryPanel = CreateOverlay("VictoryPanel", canvas.transform);
        CanvasGroup canvasGroup = victoryPanel.AddComponent<CanvasGroup>();

        // Hiệu ứng pháo hoa giấy chúc mừng
        GameObject confettiObject = new GameObject("ConfettiRoot", typeof(RectTransform));
        confettiObject.transform.SetParent(victoryPanel.transform, false);
        RectTransform confettiRoot = confettiObject.GetComponent<RectTransform>();
        Stretch(confettiRoot);

        // 1. Huy hiệu chiến thắng có cánh vàng lớn ở trên cùng (Crest: 1237 x 1213)
        GameObject panelObject = CreateImage("CompletePanel", victoryPanel.transform, completeCrestSprite);
        RectTransform resultCard = panelObject.GetComponent<RectTransform>();
        SetRect(resultCard, new Vector2(0f, 390f), new Vector2(720f, 706f));

        // 2. Dòng thưởng Data Chip (Dịch sang trái x = -65f để chữ Get 1000 không bao giờ xuống dòng và không chạm nút Details)
        RectTransform dataRow = BuildRewardRow(
            victoryPanel.transform, "DataChipReward", new Vector2(-65f, 25f), dataChipSprite, "Get 1000", out TMP_Text dataChipRewardText);

        // 3. Dòng thưởng Red Gem (Dịch sang trái x = -65f)
        RectTransform redGemRow = BuildRewardRow(
            victoryPanel.transform, "RedGemReward", new Vector2(-65f, -90f), redGemSprite, "Get 20", out TMP_Text redGemRewardText);

        // 4. Nút Details (biểu đồ + chữ Details) đặt lệch sang bên phải ngay cạnh dòng Red Gem
        Button detailsButton = CreateSpriteButton(
            "DetailsButton", victoryPanel.transform, detailsButtonSprite, new Vector2(275f, -65f), new Vector2(120f, 133f));

        // 5. Nút Get reward (xanh dương bo tròn có chữ Get reward)
        Button normalButton = CreateSpriteButton(
            "GetRewardButton", victoryPanel.transform, normalButtonSprite, new Vector2(0f, -270f), new Vector2(440f, 224f));

        // 6. Nút Get x3 reward (xanh lá bo tròn có chữ Get x3 reward)
        Button tripleButton = CreateSpriteButton(
            "VipTripleButton", victoryPanel.transform, tripleButtonSprite, new Vector2(0f, -475f), new Vector2(440f, 224f));

        // Text thông báo ẩn khi cần (feedback)
        TMP_Text feedbackText = CreateText("FeedbackText", victoryPanel.transform, string.Empty, 24f, Feedback);
        SetRect(feedbackText.rectTransform, new Vector2(0f, -610f), new Vector2(600f, 40f));

        DamageDetailsPopup damageDetailsPopup = canvas.GetComponentInChildren<DamageDetailsPopup>(true);
        if (damageDetailsPopup == null)
        {
            damageDetailsPopup = PlayerRunEndSceneBuilder.BuildDamageDetailsModal(canvas.transform);
        }

        VictoryPanelController controller = canvas.AddComponent<VictoryPanelController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("enemySpawner").objectReferenceValue = UnityEngine.Object.FindObjectOfType<EnemySpawner>();
        serialized.FindProperty("playerRunEndController").objectReferenceValue = canvas.GetComponent<PlayerRunEndController>();
        serialized.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
        serialized.FindProperty("panelCanvasGroup").objectReferenceValue = canvasGroup;
        serialized.FindProperty("resultCard").objectReferenceValue = resultCard;
        serialized.FindProperty("confettiRoot").objectReferenceValue = confettiRoot;
        serialized.FindProperty("dataChipRewardText").objectReferenceValue = dataChipRewardText;
        serialized.FindProperty("redGemRewardText").objectReferenceValue = redGemRewardText;
        serialized.FindProperty("detailsButton").objectReferenceValue = detailsButton;
        serialized.FindProperty("damageDetailsPopup").objectReferenceValue = damageDetailsPopup;
        serialized.FindProperty("feedbackText").objectReferenceValue = feedbackText;
        serialized.FindProperty("vipTripleButton").objectReferenceValue = tripleButton;
        serialized.FindProperty("homeButton").objectReferenceValue = normalButton;

        SerializedProperty stagedItems = serialized.FindProperty("stagedRevealItems");
        stagedItems.arraySize = 5;
        stagedItems.GetArrayElementAtIndex(0).objectReferenceValue = dataRow;
        stagedItems.GetArrayElementAtIndex(1).objectReferenceValue = redGemRow;
        stagedItems.GetArrayElementAtIndex(2).objectReferenceValue = detailsButton.GetComponent<RectTransform>();
        stagedItems.GetArrayElementAtIndex(3).objectReferenceValue = normalButton.GetComponent<RectTransform>();
        stagedItems.GetArrayElementAtIndex(4).objectReferenceValue = tripleButton.GetComponent<RectTransform>();
        serialized.ApplyModifiedPropertiesWithoutUndo();

        detailsButton.onClick.AddListener(controller.ToggleDetails);

        victoryPanel.SetActive(false);
        victoryPanel.transform.SetAsLastSibling();
        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (openedAdditively) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[VictorySceneBuilder] Đã dựng VictoryPanel hoàn toàn theo ảnh mẫu Image 2 (Crest cánh vàng + Data/Gem rows + Details + 2 nút Get Reward).");
    }

    private static RectTransform BuildRewardRow(Transform parent, string name, Vector2 position, Sprite iconSprite, string defaultText, out TMP_Text rewardText)
    {
        GameObject row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        SetRect(rowRect, position, new Vector2(520f, 110f));

        GameObject iconObject = CreateImage("Icon", row.transform, iconSprite);
        Image icon = iconObject.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        SetRect(icon.rectTransform, new Vector2(-170f, 0f), new Vector2(96f, 96f));

        Material mat = rewardFontMaterial != null ? rewardFontMaterial : fontMaterial;
        rewardText = CreateText("Value", row.transform, defaultText, 68f, Color.white, mat);
        rewardText.alignment = TextAlignmentOptions.Left;
        rewardText.fontStyle = FontStyles.Bold;
        rewardText.fontWeight = FontWeight.Bold;
        rewardText.extraPadding = true;
        rewardText.enableWordWrapping = false;
        rewardText.overflowMode = TextOverflowModes.Overflow;
        SetRect(rewardText.rectTransform, new Vector2(80f, 0f), new Vector2(420f, 100f));
        return rowRect;
    }

    private static Button CreateSpriteButton(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = CreateImage(name, parent, sprite);
        Image image = buttonObject.GetComponent<Image>();
        image.preserveAspect = true;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.78f, 0.9f, 0.9f, 1f);
        colors.disabledColor = new Color(0.45f, 0.52f, 0.52f, 0.75f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        SetRect(buttonObject.GetComponent<RectTransform>(), position, size);
        return button;
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

    private static GameObject CreateImage(string name, Transform parent, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        return go;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float size, Color color, Material customMaterial = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text text = go.GetComponent<TMP_Text>();
        text.font = font;
        Material mat = customMaterial != null ? customMaterial : fontMaterial;
        if (mat != null) text.fontSharedMaterial = mat;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.fontWeight = FontWeight.Bold;
        text.color = color;
        text.extraPadding = true;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    private static Sprite LoadSprite(string path, string spriteName)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite && string.Equals(sprite.name, spriteName, StringComparison.OrdinalIgnoreCase)) return sprite;
        }
        return null;
    }

    private static Sprite LoadSpriteBySuffix(string path, string suffix)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite && sprite.name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return sprite;
        }
        return null;
    }

    private static GameObject FindRootObject(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == name) return roots[i];
        }
        return null;
    }

    private static void RemoveExisting(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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
}
#endif
