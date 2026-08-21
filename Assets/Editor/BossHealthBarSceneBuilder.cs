#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tool xây dựng RIÊNG THANH MÁU BOSS (Boss Health Bar) cho PGE:
/// - Tự động tìm Sprite 'brHpBoss' (nền) và 'HpBoss' (thanh máu) trong project.
/// - Thiết lập chuẩn vị trí nằm ngay dưới thanh Level (Y = -175).
/// - Tạo đầy đủ: CanvasGroup (ẩn/hiện tự động), Tên Boss, Thanh ruột đỏ (Filled), Thanh bóng Ghost (tụt chậm), Số máu.
/// - Tự động liên kết 100% vào BossHealthBarUI script.
/// 
/// Menu: PGE > UI > Build Boss Health Bar Only
/// </summary>
public static class BossHealthBarSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/GamePlay.unity";
    private const string SpriteSheetPath = "Assets/Sprites/UI/nút màn play (1).png";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("PGE/UI/Build Boss Health Bar Only")]
    public static void BuildBossHealthBarOnlyMenu()
    {
        BuildBossHealthBar();
    }

    public static void BuildBossHealthBar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[BossHealthBarSceneBuilder] Không thể thao tác khi đang trong Play Mode.");
            return;
        }

        // 1. Mở Scene nếu chưa mở
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            if (File.Exists(ScenePath))
            {
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }

        // 2. Tìm Canvas & TopHUD
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            Canvas foundCanvas = GameObject.FindObjectOfType<Canvas>();
            if (foundCanvas != null)
            {
                canvasObj = foundCanvas.gameObject;
            }
            else
            {
                canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Canvas c = canvasObj.GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
            }
        }

        Transform parentTransform = canvasObj.transform.Find("TopHUD");
        if (parentTransform == null)
        {
            parentTransform = canvasObj.transform;
        }

        // 3. Load font & sprite từ sprite sheet
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Sprite bgSprite = FindSpriteInSheet(SpriteSheetPath, "brHpBoss");
        Sprite fillSprite = FindSpriteInSheet(SpriteSheetPath, "HpBoss");
        Sprite ghostSprite = FindSpriteInSheet(SpriteSheetPath, "bongdo") ?? fillSprite;

        // 4. Xóa BossHealthContainer cũ nếu có để tạo mới sạch sẽ
        Transform existingBossBar = parentTransform.Find("BossHealthContainer");
        if (existingBossBar == null)
        {
            existingBossBar = canvasObj.transform.Find("BossHealthContainer");
        }
        if (existingBossBar != null)
        {
            GameObject.DestroyImmediate(existingBossBar.gameObject);
        }

        // 5. Tạo BossHealthContainer gốc
        GameObject bossContainerObj = new GameObject("BossHealthContainer", typeof(RectTransform), typeof(CanvasGroup));
        bossContainerObj.transform.SetParent(parentTransform, false);
        RectTransform containerRt = bossContainerObj.GetComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0.5f, 1f);
        containerRt.anchorMax = new Vector2(0.5f, 1f);
        containerRt.pivot = new Vector2(0.5f, 0.5f);
        containerRt.anchoredPosition = new Vector2(0f, -175f);
        containerRt.sizeDelta = new Vector2(540f, 75f);

        CanvasGroup canvasGroup = bossContainerObj.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f; // Mặc định ẩn khi chưa có Boss
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // 6. Text Tên Boss (BossNameText)
        GameObject nameObj = new GameObject("BossNameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(bossContainerObj.transform, false);
        TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
        if (font != null) nameText.font = font;
        nameText.text = "⚠️ BOSS FIGHT ⚠️";
        nameText.fontSize = 22f;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = new Color32(255, 90, 90, 255);
        nameText.alignment = TextAlignmentOptions.Center;
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0.5f, 0.5f);
        nameRt.anchorMax = new Vector2(0.5f, 0.5f);
        nameRt.pivot = new Vector2(0.5f, 0.5f);
        nameRt.anchoredPosition = new Vector2(0f, 24f);
        nameRt.sizeDelta = new Vector2(500f, 30f);

        // 7. Khung HealthBar
        GameObject healthBarObj = new GameObject("HealthBar", typeof(RectTransform));
        healthBarObj.transform.SetParent(bossContainerObj.transform, false);
        RectTransform healthBarRt = healthBarObj.GetComponent<RectTransform>();
        healthBarRt.anchorMin = new Vector2(0.5f, 0.5f);
        healthBarRt.anchorMax = new Vector2(0.5f, 0.5f);
        healthBarRt.pivot = new Vector2(0.5f, 0.5f);
        healthBarRt.anchoredPosition = new Vector2(0f, -12f);
        healthBarRt.sizeDelta = new Vector2(520f, 32f);

        // 7A. Background Image (brHpBoss)
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(healthBarObj.transform, false);
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.sprite = bgSprite;
        bgImg.type = bgSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        bgImg.color = bgSprite != null ? Color.white : new Color32(40, 10, 15, 255);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        Stretch(bgRt);

        // 7B. Ghost Fill Image (Bóng máu tụt chậm)
        GameObject ghostObj = new GameObject("GhostFill", typeof(RectTransform), typeof(Image));
        ghostObj.transform.SetParent(healthBarObj.transform, false);
        Image ghostImg = ghostObj.GetComponent<Image>();
        ghostImg.sprite = ghostSprite;
        ghostImg.type = Image.Type.Filled;
        ghostImg.fillMethod = Image.FillMethod.Horizontal;
        ghostImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        ghostImg.fillAmount = 1f;
        ghostImg.color = new Color32(180, 50, 40, 255);
        RectTransform ghostRt = ghostObj.GetComponent<RectTransform>();
        Stretch(ghostRt, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        // 7C. Health Fill Image (Thanh máu chính HpBoss)
        GameObject fillObj = new GameObject("HealthFill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(healthBarObj.transform, false);
        Image fillImg = fillObj.GetComponent<Image>();
        fillImg.sprite = fillSprite;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillOriginLeft(fillImg);
        fillImg.fillAmount = 1f;
        fillImg.color = fillSprite != null ? Color.white : new Color32(230, 40, 40, 255);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        Stretch(fillRt, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        // 7D. Số máu (HealthNumberText)
        GameObject numObj = new GameObject("HealthNumberText", typeof(RectTransform), typeof(TextMeshProUGUI));
        numObj.transform.SetParent(healthBarObj.transform, false);
        TextMeshProUGUI numText = numObj.GetComponent<TextMeshProUGUI>();
        if (font != null) numText.font = font;
        numText.text = "";
        numText.fontSize = 18f;
        numText.fontStyle = FontStyles.Bold;
        numText.color = Color.white;
        numText.alignment = TextAlignmentOptions.Center;
        RectTransform numRt = numObj.GetComponent<RectTransform>();
        Stretch(numRt);

        // 8. Thêm & Liên kết Component BossHealthBarUI
        BossHealthBarUI bossCtrl = bossContainerObj.AddComponent<BossHealthBarUI>();
        EnemySpawner spawner = GameObject.FindObjectOfType<EnemySpawner>();

        SerializedObject so = new SerializedObject(bossCtrl);
        so.FindProperty("enemySpawner").objectReferenceValue = spawner;
        so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        so.FindProperty("bossNameText").objectReferenceValue = nameText;
        so.FindProperty("healthFillImage").objectReferenceValue = fillImg;
        so.FindProperty("damageGhostFillImage").objectReferenceValue = ghostImg;
        so.FindProperty("healthNumberText").objectReferenceValue = numText;
        so.FindProperty("smoothTransition").boolValue = true;
        so.FindProperty("smoothSpeed").floatValue = 10f;
        so.FindProperty("ghostSpeed").floatValue = 3f;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(bossContainerObj);
        EditorUtility.SetDirty(bossCtrl);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log("[BossHealthBarSceneBuilder] 🎉 ĐÃ XÂY DỰNG XONG THANH MÁU BOSS CHUẨN ĐẸP 100%!");
        Selection.activeGameObject = bossContainerObj;
    }

    private static void fillOriginLeft(Image img)
    {
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    private static void Stretch(RectTransform rect, Vector2? offsetMin = null, Vector2? offsetMax = null)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin ?? Vector2.zero;
        rect.offsetMax = offsetMax ?? Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static Sprite FindSpriteInSheet(string path, string spriteName)
    {
        if (string.IsNullOrEmpty(path)) return null;

        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        if (allAssets == null || allAssets.Length == 0) return null;

        foreach (Object asset in allAssets)
        {
            if (asset is Sprite s && string.Equals(s.name, spriteName, System.StringComparison.OrdinalIgnoreCase))
            {
                return s;
            }
        }

        // Fallback tìm gần đúng nếu không khớp chính xác
        foreach (Object asset in allAssets)
        {
            if (asset is Sprite s && s.name.ToLower().Contains(spriteName.ToLower()))
            {
                return s;
            }
        }

        return null;
    }
}
#endif
