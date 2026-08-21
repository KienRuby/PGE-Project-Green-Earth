#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PlayerRunEndSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/GamePlay.unity";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

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

    [MenuItem("PGE/UI/Build Revive & Game Over")]
    public static void BuildFromMenu()
    {
        Build();
    }

    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
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
        RemoveExisting(canvas.transform, "RevivePanel");
        RemoveExisting(canvas.transform, "GameOverPanel");

        PlayerRunEndController oldController = canvas.GetComponent<PlayerRunEndController>();
        if (oldController != null) Object.DestroyImmediate(oldController);

        GameObject revivePanel = BuildRevivePanel(canvas.transform, out Button noButton, out Button reviveButton, out TMP_Text reviveFeedback);
        GameObject gameOverPanel = BuildGameOverPanel(
            canvas.transform,
            out TMP_Text chapterText,
            out TMP_Text wavesText,
            out TMP_Text progressText,
            out TMP_Text chipRewardText,
            out TMP_Text gemRewardText,
            out Button homeButton);

        PlayerRunEndController controller = canvas.AddComponent<PlayerRunEndController>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("playerHealth").objectReferenceValue = player != null ? player.GetComponent<PlayerHealth>() : null;
        serialized.FindProperty("playerDeathController").objectReferenceValue = player != null ? player.GetComponent<PlayerDeathController>() : null;
        serialized.FindProperty("enemySpawner").objectReferenceValue = Object.FindObjectOfType<EnemySpawner>();
        serialized.FindProperty("revivePanel").objectReferenceValue = revivePanel;
        serialized.FindProperty("noButton").objectReferenceValue = noButton;
        serialized.FindProperty("vipReviveButton").objectReferenceValue = reviveButton;
        serialized.FindProperty("reviveFeedbackText").objectReferenceValue = reviveFeedback;
        serialized.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
        serialized.FindProperty("chapterText").objectReferenceValue = chapterText;
        serialized.FindProperty("wavesText").objectReferenceValue = wavesText;
        serialized.FindProperty("progressText").objectReferenceValue = progressText;
        serialized.FindProperty("dataChipRewardText").objectReferenceValue = chipRewardText;
        serialized.FindProperty("redGemRewardText").objectReferenceValue = gemRewardText;
        serialized.FindProperty("homeButton").objectReferenceValue = homeButton;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        revivePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[PlayerRunEndSceneBuilder] Đã tạo Revive Panel và Game Over Panel.");
    }

    private static GameObject BuildRevivePanel(Transform parent, out Button noButton, out Button reviveButton, out TMP_Text feedback)
    {
        GameObject root = CreateOverlay("RevivePanel", parent);

        TMP_Text title = CreateText("Title", root.transform, "REVIVE?", 112f, Gold);
        SetRect(title.rectTransform, new Vector2(0f, 610f), new Vector2(850f, 160f));
        AddOutline(title, Border, 0.28f);

        TMP_Text heart = CreateText("Heart", root.transform, "HP", 260f, Coral);
        SetRect(heart.rectTransform, new Vector2(0f, 120f), new Vector2(700f, 650f));
        AddOutline(heart, Border, 0.18f);

        TMP_Text face = CreateText("HeartFace", root.transform, "X   X", 92f, Dark);
        SetRect(face.rectTransform, new Vector2(0f, 190f), new Vector2(430f, 140f));

        noButton = CreateButton("NoButton", root.transform, "NO", Slate, new Vector2(0f, -470f));
        reviveButton = CreateButton("VipReviveButton", root.transform, "VIP REVIVE", Navy, new Vector2(0f, -660f));

        feedback = CreateText("Feedback", root.transform, "VIP REQUIRED - BUY VIP IN SHOP", 28f, Gold);
        SetRect(feedback.rectTransform, new Vector2(0f, -800f), new Vector2(850f, 70f));
        return root;
    }

    private static GameObject BuildGameOverPanel(
        Transform parent,
        out TMP_Text chapterText,
        out TMP_Text wavesText,
        out TMP_Text progressText,
        out TMP_Text chipRewardText,
        out TMP_Text gemRewardText,
        out Button homeButton)
    {
        GameObject root = CreateOverlay("GameOverPanel", parent);

        TMP_Text title = CreateText("Title", root.transform, "GAME OVER", 108f, Gold);
        SetRect(title.rectTransform, new Vector2(0f, 650f), new Vector2(950f, 160f));
        AddOutline(title, Border, 0.28f);

        GameObject card = CreateFrame("ProgressCard", root.transform, new Vector2(0f, 170f), new Vector2(620f, 590f), Slate);
        chapterText = CreateText("ChapterText", card.transform, "CHAPTER. 01", 46f, Cream);
        SetRect(chapterText.rectTransform, new Vector2(0f, 195f), new Vector2(560f, 75f));

        wavesText = CreateText("WavesText", card.transform, "01 / 10 WAVES", 80f, Gold);
        SetRect(wavesText.rectTransform, new Vector2(0f, 40f), new Vector2(570f, 145f));
        AddOutline(wavesText, Border, 0.2f);

        progressText = CreateText("ProgressText", card.transform, "STAGE PROGRESS  0%", 38f, Cream);
        SetRect(progressText.rectTransform, new Vector2(0f, -155f), new Vector2(570f, 90f));

        GameObject chipRow = CreateFrame("DataChipReward", root.transform, new Vector2(0f, -250f), new Vector2(650f, 110f), Dark);
        TMP_Text chipIcon = CreateText("Icon", chipRow.transform, "[ ]", 60f, Cyan);
        SetRect(chipIcon.rectTransform, new Vector2(-220f, 0f), new Vector2(100f, 90f));
        chipRewardText = CreateText("Value", chipRow.transform, "GET 0", 46f, Cream);
        SetRect(chipRewardText.rectTransform, new Vector2(65f, 0f), new Vector2(420f, 90f));

        GameObject gemRow = CreateFrame("RedGemReward", root.transform, new Vector2(0f, -385f), new Vector2(650f, 110f), Dark);
        TMP_Text gemIcon = CreateText("Icon", gemRow.transform, "<>", 58f, new Color32(226, 72, 76, 255));
        SetRect(gemIcon.rectTransform, new Vector2(-220f, 0f), new Vector2(100f, 90f));
        gemRewardText = CreateText("Value", gemRow.transform, "GET 0", 46f, Cream);
        SetRect(gemRewardText.rectTransform, new Vector2(65f, 0f), new Vector2(420f, 90f));

        homeButton = CreateButton("HomeButton", root.transform, "HOME", Navy, new Vector2(0f, -650f));
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
