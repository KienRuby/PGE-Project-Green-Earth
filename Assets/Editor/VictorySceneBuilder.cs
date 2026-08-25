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
    private const string CompletePanelPath = "Assets/Sprites/UI/UI Player/popup chapter complete.png";
    private const string CompleteButtonsPath = "Assets/Sprites/UI/UI Player/nút màn chapter complete_game over.png";

    private static readonly Color Dim = new Color32(4, 9, 13, 176);
    private static readonly Color White = new Color32(255, 255, 255, 255);
    private static readonly Color Feedback = new Color32(255, 240, 116, 255);

    private static TMP_FontAsset font;
    private static Material fontMaterial;

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
        Sprite completePanelSprite = LoadSprite(CompletePanelPath, "Complete!");
        Sprite normalButtonSprite = LoadSpriteBySuffix(CompleteButtonsPath, "_0");
        Sprite tripleButtonSprite = LoadSpriteBySuffix(CompleteButtonsPath, "_1");
        if (font == null || completePanelSprite == null || normalButtonSprite == null || tripleButtonSprite == null)
        {
            Debug.LogError("[VictorySceneBuilder] Thiếu font hoặc sprite Complete. Kiểm tra Assets/Sprites/UI/UI Player.");
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

        GameObject panelObject = CreateImage("CompletePanel", victoryPanel.transform, completePanelSprite);
        RectTransform resultCard = panelObject.GetComponent<RectTransform>();
        SetRect(resultCard, new Vector2(0f, -8f), new Vector2(780f, 1218f));

        RectTransform dataRow = BuildRewardRow(panelObject.transform, "DataChipReward", new Vector2(0f, 145f),
            LoadSprite(CurrencyAtlasPath, "data"), out TMP_Text dataRewardText);
        RectTransform gemRow = BuildRewardRow(panelObject.transform, "RedGemReward", new Vector2(0f, 2f),
            LoadSprite(CurrencyAtlasPath, "red"), out TMP_Text gemRewardText);

        Button normalButton = CreateSpriteButton(
            "GetRewardButton", panelObject.transform, normalButtonSprite, new Vector2(0f, -230f), new Vector2(320f, 158f));
        Button tripleButton = CreateSpriteButton(
            "VipTripleButton", panelObject.transform, tripleButtonSprite, new Vector2(0f, -405f), new Vector2(320f, 158f));

        TMP_Text feedbackText = CreateText("FeedbackText", panelObject.transform, string.Empty, 24f, Feedback);
        SetRect(feedbackText.rectTransform, new Vector2(0f, -535f), new Vector2(600f, 52f));

        VictoryPanelController controller = canvas.AddComponent<VictoryPanelController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("enemySpawner").objectReferenceValue = UnityEngine.Object.FindObjectOfType<EnemySpawner>();
        serialized.FindProperty("playerRunEndController").objectReferenceValue = canvas.GetComponent<PlayerRunEndController>();
        serialized.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
        serialized.FindProperty("panelCanvasGroup").objectReferenceValue = canvasGroup;
        serialized.FindProperty("resultCard").objectReferenceValue = resultCard;
        serialized.FindProperty("confettiRoot").objectReferenceValue = confettiRoot;
        serialized.FindProperty("dataChipRewardText").objectReferenceValue = dataRewardText;
        serialized.FindProperty("redGemRewardText").objectReferenceValue = gemRewardText;
        serialized.FindProperty("feedbackText").objectReferenceValue = feedbackText;
        serialized.FindProperty("vipTripleButton").objectReferenceValue = tripleButton;
        serialized.FindProperty("homeButton").objectReferenceValue = normalButton;

        SerializedProperty stagedItems = serialized.FindProperty("stagedRevealItems");
        stagedItems.arraySize = 4;
        stagedItems.GetArrayElementAtIndex(0).objectReferenceValue = dataRow;
        stagedItems.GetArrayElementAtIndex(1).objectReferenceValue = gemRow;
        stagedItems.GetArrayElementAtIndex(2).objectReferenceValue = normalButton.GetComponent<RectTransform>();
        stagedItems.GetArrayElementAtIndex(3).objectReferenceValue = tripleButton.GetComponent<RectTransform>();
        serialized.ApplyModifiedPropertiesWithoutUndo();

        victoryPanel.SetActive(false);
        victoryPanel.transform.SetAsLastSibling();
        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (openedAdditively) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[VictorySceneBuilder] Đã setup popup COMPLETE theo reference và liên kết reward buttons.");
    }

    private static RectTransform BuildRewardRow(Transform parent, string name, Vector2 position, Sprite iconSprite, out TMP_Text rewardText)
    {
        GameObject row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        SetRect(rowRect, position, new Vector2(610f, 118f));

        GameObject iconObject = CreateImage("Icon", row.transform, iconSprite);
        Image icon = iconObject.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        SetRect(icon.rectTransform, new Vector2(-148f, 0f), new Vector2(92f, 92f));

        rewardText = CreateText("Value", row.transform, "Get 0", 46f, White);
        rewardText.alignment = TextAlignmentOptions.Left;
        SetRect(rewardText.rectTransform, new Vector2(98f, 0f), new Vector2(380f, 95f));
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
            if (assets[i] is Sprite sprite && sprite.name.EndsWith(suffix, StringComparison.Ordinal)) return sprite;
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
