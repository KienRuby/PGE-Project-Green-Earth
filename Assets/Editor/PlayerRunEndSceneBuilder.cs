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

    private static readonly Color Dim = new Color32(5, 12, 10, 220);
    private static readonly Color Dark = new Color32(18, 28, 34, 255);
    private static readonly Color Border = new Color32(7, 11, 14, 255);
    private static readonly Color Slate = new Color32(99, 126, 143, 255);
    private static readonly Color Navy = new Color32(20, 31, 52, 255);
    private static readonly Color Gold = new Color32(255, 190, 61, 255);
    private static readonly Color Cream = new Color32(248, 248, 238, 255);
    private static readonly Color Coral = new Color32(231, 108, 65, 255);
    private static readonly Color Cyan = new Color32(78, 206, 196, 255);

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
            out TMP_Text feedback);

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("revivePanel").objectReferenceValue = revivePanel;
        serialized.FindProperty("reviveCanvasGroup").objectReferenceValue = reviveCanvasGroup;
        serialized.FindProperty("reviveContent").objectReferenceValue = reviveContent;
        serialized.FindProperty("noButton").objectReferenceValue = noButton;
        serialized.FindProperty("gemReviveButton").objectReferenceValue = gemButton;
        serialized.FindProperty("adReviveButton").objectReferenceValue = adButton;
        serialized.FindProperty("reviveFeedbackText").objectReferenceValue = feedback;
        serialized.FindProperty("reviveGemCost").intValue = 200;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        revivePanel.SetActive(false);
        revivePanel.transform.SetAsLastSibling();
        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (!wasAlreadyLoaded) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[PlayerRunEndSceneBuilder] Đã setup riêng Revive Panel theo sprite reference.");
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
        GameObject gameOverPanel = BuildGameOverPanel(
            canvas.transform,
            out TMP_Text chipRewardText,
            out TMP_Text gemRewardText,
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
        serialized.FindProperty("getRewardButton").objectReferenceValue = getRewardButton;
        serialized.FindProperty("vipTripleButton").objectReferenceValue = tripleButton;
        serialized.FindProperty("gameOverFeedbackText").objectReferenceValue = feedbackText;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        gameOverPanel.SetActive(false);
        gameOverPanel.transform.SetAsLastSibling();
        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (!wasAlreadyLoaded) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[PlayerRunEndSceneBuilder] Đã setup Game Over đồng bộ với popup Complete.");
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

        PlayerRunEndController oldController = canvas.GetComponent<PlayerRunEndController>();
        if (oldController != null) Object.DestroyImmediate(oldController);

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
        serialized.FindProperty("getRewardButton").objectReferenceValue = getRewardButton;
        serialized.FindProperty("vipTripleButton").objectReferenceValue = tripleButton;
        serialized.FindProperty("gameOverFeedbackText").objectReferenceValue = gameOverFeedback;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        revivePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[PlayerRunEndSceneBuilder] Đã tạo Revive Panel và Game Over Panel.");
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

        GameObject contentObject = new GameObject("ReviveContent", typeof(RectTransform));
        contentObject.transform.SetParent(root.transform, false);
        content = contentObject.GetComponent<RectTransform>();
        Stretch(content);

        Sprite[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(ReviveUiPath).OfType<Sprite>().ToArray();
        Image title = CreateSpriteImage("Title", content, FindSprite(sprites, "Revire"));
        SetRect(title.rectTransform, new Vector2(0f, 650f), new Vector2(640f, 160f));

        Image player = CreateSpriteImage("BrokenPlayer", content, FindSprite(sprites, "player"));
        SetRect(player.rectTransform, new Vector2(0f, 120f), new Vector2(640f, 705f));

        noButton = CreateSpriteButton("NoButton", content, FindSprite(sprites, "no"), new Vector2(0f, -470f));
        gemButton = CreateSpriteButton("GemReviveButton", content, FindSprite(sprites, "Use gems"), new Vector2(0f, -635f));
        adButton = CreateSpriteButton("AdReviveButton", content, FindSprite(sprites, "Watch ads"), new Vector2(0f, -800f));

        feedback = CreateText("Feedback", content, string.Empty, 25f, Gold);
        SetRect(feedback.rectTransform, new Vector2(0f, -915f), new Vector2(850f, 58f));
        return root;
    }

    private static Image CreateSpriteImage(string name, Transform parent, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static Button CreateSpriteButton(string name, Transform parent, Sprite sprite, Vector2 position)
    {
        Image image = CreateSpriteImage(name, parent, sprite);
        image.raycastTarget = true;
        SetRect(image.rectTransform, position, new Vector2(470f, 147f));
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.76f, 0.9f, 0.86f, 1f);
        colors.disabledColor = new Color(0.42f, 0.48f, 0.46f, 0.72f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return button;
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
        SetRect(rowRect, position, new Vector2(610f, 118f));

        Image icon = CreateSpriteImage("Icon", row.transform, iconSprite);
        SetRect(icon.rectTransform, new Vector2(-148f, 0f), new Vector2(92f, 92f));

        rewardText = CreateText("Value", row.transform, "Get 0", 46f, Color.white);
        rewardText.alignment = TextAlignmentOptions.Left;
        SetRect(rewardText.rectTransform, new Vector2(98f, 0f), new Vector2(380f, 95f));
        return rowRect;
    }

    private static Sprite FindSprite(Sprite[] sprites, string spriteName)
    {
        return sprites.FirstOrDefault(sprite =>
            sprite != null && string.Equals(sprite.name, spriteName, System.StringComparison.OrdinalIgnoreCase));
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
        out Button getRewardButton,
        out Button tripleButton,
        out TMP_Text feedbackText)
    {
        Sprite panelSprite = LoadSprite(GameOverPanelPath, "Game over");
        Sprite normalButtonSprite = LoadSpriteBySuffix(RewardButtonsPath, "_0");
        Sprite tripleButtonSprite = LoadSpriteBySuffix(RewardButtonsPath, "_1");
        Sprite dataIcon = LoadSprite(CurrencyAtlasPath, "data");
        Sprite gemIcon = LoadSprite(CurrencyAtlasPath, "red");
        if (panelSprite == null || normalButtonSprite == null || tripleButtonSprite == null)
        {
            chipRewardText = null;
            gemRewardText = null;
            getRewardButton = null;
            tripleButton = null;
            feedbackText = null;
            return null;
        }

        GameObject root = CreateOverlay("GameOverPanel", parent);
        root.GetComponent<Image>().color = new Color32(4, 9, 13, 176);

        Image panel = CreateSpriteImage("GameOverContent", root.transform, panelSprite);
        SetRect(panel.rectTransform, new Vector2(0f, -8f), new Vector2(780f, 1218f));

        BuildRewardRow(panel.transform, "DataChipReward", new Vector2(0f, 145f), dataIcon, out chipRewardText);
        BuildRewardRow(panel.transform, "RedGemReward", new Vector2(0f, 2f), gemIcon, out gemRewardText);

        getRewardButton = CreateSpriteButton(
            "GetRewardButton", panel.transform, normalButtonSprite, new Vector2(0f, -230f), new Vector2(320f, 158f));
        tripleButton = CreateSpriteButton(
            "VipTripleButton", panel.transform, tripleButtonSprite, new Vector2(0f, -405f), new Vector2(320f, 158f));

        feedbackText = CreateText("FeedbackText", panel.transform, string.Empty, 24f, new Color32(255, 240, 116, 255));
        SetRect(feedbackText.rectTransform, new Vector2(0f, -535f), new Vector2(600f, 52f));
        return root;
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

    private static Button CreateButton(string name, Transform parent, string label, Color fill, Vector2 position)
    {
        GameObject frame = CreateFrame(name, parent, position, new Vector2(650f, 145f), fill);
        Button button = frame.AddComponent<Button>();
        button.targetGraphic = frame.transform.Find("Fill").GetComponent<Image>();
        TMP_Text text = CreateText("Label", frame.transform, label, 48f, Cream);
        Stretch(text.rectTransform);
        AddOutline(text, Border, 0.2f);
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

    private static void AddOutline(TMP_Text text, Color color, float width)
    {
        text.outlineColor = color;
        text.outlineWidth = width;
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
