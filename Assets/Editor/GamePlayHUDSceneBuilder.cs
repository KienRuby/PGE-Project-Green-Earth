#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor Scene Builder cho HUD màn chơi GamePlay:
/// Xây dựng chính xác theo hình ảnh mẫu:
/// - Góc trên bên trái: Vòng tròn thời gian Wave quay 360 độ (WAVE, 01/10)
/// - Ở giữa phía trên: Level Text ('Lv01') và thanh nạp Kinh Nghiệm (EXP Bar)
/// - Góc trên bên phải: Nút Pause ('||')
/// Menu: PGE > UI > Build GamePlay HUD
/// </summary>
[InitializeOnLoad]
public static class GamePlayHUDSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/GamePlay.unity";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    private static readonly Color DarkBg = new Color32(22, 29, 36, 255);
    private static readonly Color InnerBg = new Color32(28, 40, 48, 255);
    private static readonly Color CyanTeal = new Color32(64, 203, 181, 255);
    private static readonly Color ExpTeal = new Color32(44, 181, 168, 255);
    private static readonly Color Cream = new Color32(239, 247, 238, 255);
    private static readonly Color DarkBorder = new Color32(14, 18, 22, 255);
    private static readonly Color PauseButtonBg = new Color32(36, 64, 76, 255);

    private static TMP_FontAsset font;
    private static Sprite circleSprite;
    private static Sprite rectSprite;

    private const string BuildRequestPath = "Assets/Editor/PGE_GamePlayHUD_BuildRequest.txt";

    static GamePlayHUDSceneBuilder()
    {
        EditorApplication.delayCall += TryBuildRequestedScene;
    }

    [MenuItem("PGE/UI/Build GamePlay HUD")]
    public static void BuildFromMenu()
    {
        BuildGamePlayHUD();
    }

    public static void RequestBuild()
    {
        File.WriteAllText(BuildRequestPath, "build");
        EditorApplication.delayCall += TryBuildRequestedScene;
    }

    private static void TryBuildRequestedScene()
    {
        if (!File.Exists(BuildRequestPath)) return;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += TryBuildRequestedScene;
            return;
        }

        BuildGamePlayHUD();
        if (File.Exists(BuildRequestPath))
        {
            AssetDatabase.DeleteAsset(BuildRequestPath);
        }
        AssetDatabase.Refresh();
    }

    public static void BuildGamePlayHUD()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[GamePlayHUDSceneBuilder] Không thể build khi đang Play Mode.");
            return;
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[GamePlayHUDSceneBuilder] Không tìm thấy scene tại {ScenePath}");
            return;
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        CreateProceduralSprites();

        // 1. Tìm hoặc thiết lập Canvas
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        }

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        // 2. Tìm hoặc thêm PlayerLevelController vào Player hoặc Manager
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null && playerObj.GetComponent<PlayerLevelController>() == null)
        {
            playerObj.AddComponent<PlayerLevelController>();
            EditorUtility.SetDirty(playerObj);
        }

        // 3. Xây dựng TopHUD Container
        Transform existingTopHUD = canvasObj.transform.Find("TopHUD");
        if (existingTopHUD != null)
        {
            GameObject.DestroyImmediate(existingTopHUD.gameObject);
        }

        GameObject topHudObj = new GameObject("TopHUD", typeof(RectTransform));
        topHudObj.transform.SetParent(canvasObj.transform, false);
        RectTransform topHudRect = topHudObj.GetComponent<RectTransform>();
        topHudRect.anchorMin = new Vector2(0f, 1f);
        topHudRect.anchorMax = new Vector2(1f, 1f);
        topHudRect.pivot = new Vector2(0.5f, 1f);
        topHudRect.anchoredPosition = new Vector2(0f, 0f);
        topHudRect.sizeDelta = new Vector2(0f, 260f);

        // ==========================================
        // A. VÒNG TRÒN WAVE TIẾN TRÌNH (GÓC TRÊN BÊN TRÁI)
        // ==========================================
        GameObject waveTrackerObj = new GameObject("WaveProgressTracker", typeof(RectTransform));
        waveTrackerObj.transform.SetParent(topHudRect, false);
        RectTransform waveTrackerRect = waveTrackerObj.GetComponent<RectTransform>();
        waveTrackerRect.anchorMin = new Vector2(0f, 1f);
        waveTrackerRect.anchorMax = new Vector2(0f, 1f);
        waveTrackerRect.pivot = new Vector2(0.5f, 0.5f);
        waveTrackerRect.anchoredPosition = new Vector2(120f, -120f);
        waveTrackerRect.sizeDelta = new Vector2(150f, 150f);

        // Viền ngoài đen
        Image waveOuterBorder = CreateImage("OuterBorder", waveTrackerObj.transform, DarkBorder, circleSprite);
        waveOuterBorder.rectTransform.sizeDelta = new Vector2(150f, 150f);

        // Vòng tròn nền tối
        Image waveBg = CreateImage("Background", waveTrackerObj.transform, DarkBg, circleSprite);
        waveBg.rectTransform.sizeDelta = new Vector2(142f, 142f);

        // Vòng tròn Radial Fill (Xoay 360 độ theo thời gian Wave)
        Image waveRadialFill = CreateImage("RadialFill", waveTrackerObj.transform, CyanTeal, circleSprite);
        waveRadialFill.rectTransform.sizeDelta = new Vector2(142f, 142f);
        waveRadialFill.type = Image.Type.Filled;
        waveRadialFill.fillMethod = Image.FillMethod.Radial360;
        waveRadialFill.fillOrigin = (int)Image.Origin360.Top;
        waveRadialFill.fillClockwise = true;
        waveRadialFill.fillAmount = 0.25f;

        // Vòng tròn trong (Inner Circle che tâm để tạo thành hình vành khăn/bánh xe)
        Image waveInner = CreateImage("InnerCircle", waveTrackerObj.transform, InnerBg, circleSprite);
        waveInner.rectTransform.sizeDelta = new Vector2(110f, 110f);

        // Nhãn chữ "WAVE"
        TMP_Text waveLabel = CreateText("WaveLabel", waveTrackerObj.transform, "WAVE", 22f, Cream, TextAlignmentOptions.Center);
        waveLabel.rectTransform.anchoredPosition = new Vector2(0f, 16f);
        waveLabel.fontStyle = FontStyles.Bold;

        // Nhãn số "01/10"
        TMP_Text waveNumber = CreateText("WaveNumber", waveTrackerObj.transform, "01/10", 30f, Cream, TextAlignmentOptions.Center);
        waveNumber.rectTransform.anchoredPosition = new Vector2(0f, -16f);
        waveNumber.fontStyle = FontStyles.Bold;

        // ==========================================
        // B. THANH LEVEL & EXP BAR (Ở GIỮA PHÍA TRÊN)
        // ==========================================
        GameObject levelContainerObj = new GameObject("LevelContainer", typeof(RectTransform));
        levelContainerObj.transform.SetParent(topHudRect, false);
        RectTransform levelContainerRect = levelContainerObj.GetComponent<RectTransform>();
        levelContainerRect.anchorMin = new Vector2(0.5f, 1f);
        levelContainerRect.anchorMax = new Vector2(0.5f, 1f);
        levelContainerRect.pivot = new Vector2(0.5f, 0.5f);
        levelContainerRect.anchoredPosition = new Vector2(0f, -105f);
        levelContainerRect.sizeDelta = new Vector2(540f, 90f);

        // Text "Lv01"
        TMP_Text levelText = CreateText("LevelText", levelContainerObj.transform, "Lv01", 38f, Cream, TextAlignmentOptions.Center);
        levelText.rectTransform.anchoredPosition = new Vector2(0f, 32f);
        levelText.fontStyle = FontStyles.Bold;

        // Khung viền EXP Bar
        Image expBorder = CreateImage("ExpBorder", levelContainerObj.transform, DarkBorder, rectSprite);
        expBorder.rectTransform.anchoredPosition = new Vector2(0f, -15f);
        expBorder.rectTransform.sizeDelta = new Vector2(520f, 36f);

        // Nền tối EXP Bar
        Image expBg = CreateImage("ExpBg", expBorder.transform, DarkBg, rectSprite);
        expBg.rectTransform.anchoredPosition = Vector2.zero;
        expBg.rectTransform.sizeDelta = new Vector2(512f, 28f);

        // Thanh Fill EXP Bar (Horizontal Fill)
        Image expFill = CreateImage("ExpFill", expBorder.transform, ExpTeal, rectSprite);
        expFill.rectTransform.anchoredPosition = Vector2.zero;
        expFill.rectTransform.sizeDelta = new Vector2(512f, 28f);
        expFill.type = Image.Type.Filled;
        expFill.fillMethod = Image.FillMethod.Horizontal;
        expFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        expFill.fillAmount = 0.0f;

        // ==========================================
        // C. NÚT PAUSE '||' (GÓC TRÊN BÊN PHẢI)
        // ==========================================
        GameObject pauseBtnObj = new GameObject("PauseButton", typeof(RectTransform), typeof(Button));
        pauseBtnObj.transform.SetParent(topHudRect, false);
        RectTransform pauseBtnRect = pauseBtnObj.GetComponent<RectTransform>();
        pauseBtnRect.anchorMin = new Vector2(1f, 1f);
        pauseBtnRect.anchorMax = new Vector2(1f, 1f);
        pauseBtnRect.pivot = new Vector2(0.5f, 0.5f);
        pauseBtnRect.anchoredPosition = new Vector2(-120f, -120f);
        pauseBtnRect.sizeDelta = new Vector2(130f, 130f);

        Image pauseBorder = CreateImage("PauseBorder", pauseBtnObj.transform, DarkBorder, circleSprite);
        pauseBorder.rectTransform.sizeDelta = new Vector2(130f, 130f);

        Image pauseBg = CreateImage("PauseBg", pauseBtnObj.transform, PauseButtonBg, circleSprite);
        pauseBg.rectTransform.sizeDelta = new Vector2(122f, 122f);

        Button pauseBtn = pauseBtnObj.GetComponent<Button>();
        pauseBtn.targetGraphic = pauseBg;

        TMP_Text pauseIcon = CreateText("PauseIcon", pauseBtnObj.transform, "||", 48f, DarkBorder, TextAlignmentOptions.Center);
        pauseIcon.rectTransform.anchoredPosition = new Vector2(0f, 2f);
        pauseIcon.fontStyle = FontStyles.Bold;

        // ==========================================
        // D. WAVE HUD CONTROLLER LINKING
        // ==========================================
        WaveHUDController hudCtrl = canvasObj.GetComponent<WaveHUDController>();
        if (hudCtrl == null)
        {
            hudCtrl = canvasObj.AddComponent<WaveHUDController>();
        }

        EnemySpawner spawner = GameObject.FindObjectOfType<EnemySpawner>();
        PlayerLevelController levelCtrl = playerObj != null ? playerObj.GetComponent<PlayerLevelController>() : GameObject.FindObjectOfType<PlayerLevelController>();

        SerializedObject so = new SerializedObject(hudCtrl);
        so.FindProperty("enemySpawner").objectReferenceValue = spawner;
        so.FindProperty("playerLevelController").objectReferenceValue = levelCtrl;
        so.FindProperty("waveRadialFillImage").objectReferenceValue = waveRadialFill;
        so.FindProperty("waveLabelText").objectReferenceValue = waveLabel;
        so.FindProperty("waveNumberText").objectReferenceValue = waveNumber;
        so.FindProperty("levelText").objectReferenceValue = levelText;
        so.FindProperty("expFillImage").objectReferenceValue = expFill;
        so.FindProperty("pauseButton").objectReferenceValue = pauseBtn;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(hudCtrl);
        EditorUtility.SetDirty(canvasObj);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[GamePlayHUDSceneBuilder] ✅ Đã khởi tạo thành công HUD chuẩn pixel cho GamePlay!");
    }

    private static Image CreateImage(string name, Transform parent, Color color, Sprite sprite = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        if (sprite != null) img.sprite = sprite;
        return img;
    }

    private static TMP_Text CreateText(string name, Transform parent, string content, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        return text;
    }

    private static void CreateProceduralSprites()
    {
        if (circleSprite == null)
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size / 2f;
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius - 1f)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                    else if (dist <= radius)
                    {
                        float alpha = 1f - (dist - (radius - 1f));
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        if (rectSprite == null)
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            rectSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        }
    }
}
#endif
