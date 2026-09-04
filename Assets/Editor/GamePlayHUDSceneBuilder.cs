#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
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
public static class GamePlayHUDSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/GamePlay.unity";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string ChipsetAtlasPath = "Assets/UI/Chipset/Generated/chipset-atlas.png";
    private const string ChipsetLevelVisualLibraryPath = "Assets/Resources/ChipsetLevelVisualLibrary.asset";
    private const string LevelUpUiPath = "Assets/Sprites/UI/UI Player/nút màn level up.png";

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

    [MenuItem("PGE/UI/Build GamePlay HUD")]
    public static void BuildFromMenu()
    {
        BuildGamePlayHUD();
    }

    [MenuItem("PGE/UI/Build Chipset Level Up Popup Only")]
    public static void BuildChipsetLevelUpPopupOnly()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[GamePlayHUDSceneBuilder] Không thể build khi đang Play Mode.");
            return;
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool wasAlreadyLoaded = scene.IsValid() && scene.isLoaded;
        if (!wasAlreadyLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        if (!scene.IsValid())
        {
            Debug.LogError($"[GamePlayHUDSceneBuilder] Không tìm thấy scene tại {ScenePath}");
            return;
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        CreateProceduralSprites();

        GameObject canvasObject = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
            .Select(canvas => canvas.gameObject)
            .FirstOrDefault(gameObject => gameObject.name == "Canvas");
        PlayerLevelController levelController = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PlayerLevelController>(true))
            .FirstOrDefault();

        if (canvasObject == null)
        {
            Debug.LogError("[GamePlayHUDSceneBuilder] GamePlay scene không có Canvas.");
            if (!wasAlreadyLoaded) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        BuildChipsetLevelUpPopup(canvasObject.transform, levelController, font);
        EditorUtility.SetDirty(canvasObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (!wasAlreadyLoaded)
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        Debug.Log("[GamePlayHUDSceneBuilder] ✅ Đã build riêng Chipset Level Up Popup vào GamePlay scene.");
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

        // 2. Tìm hoặc thêm PlayerLevelController, GunTurretSkill, PlayerChipsetSkillManager vào Player hoặc Manager
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            if (playerObj.GetComponent<PlayerLevelController>() == null)
            {
                playerObj.AddComponent<PlayerLevelController>();
            }

            GunTurretSkill turretSkill = playerObj.GetComponent<GunTurretSkill>();
            if (turretSkill == null)
            {
                turretSkill = playerObj.AddComponent<GunTurretSkill>();
            }

            RocketPunchSkill punchSkill = playerObj.GetComponent<RocketPunchSkill>();
            if (punchSkill == null)
            {
                punchSkill = playerObj.AddComponent<RocketPunchSkill>();
            }

            SpinningBladeSkill bladeSkill = playerObj.GetComponent<SpinningBladeSkill>();
            if (bladeSkill == null)
            {
                bladeSkill = playerObj.AddComponent<SpinningBladeSkill>();
            }

            PlayerChipsetSkillManager skillMgr = playerObj.GetComponent<PlayerChipsetSkillManager>();
            if (skillMgr == null)
            {
                skillMgr = playerObj.AddComponent<PlayerChipsetSkillManager>();
            }

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
        // B2. THANH MÁU BOSS (NGAY DƯỚI THANH LEVEL)
        // ==========================================
        GameObject bossHealthContainerObj = new GameObject("BossHealthContainer", typeof(RectTransform), typeof(CanvasGroup));
        bossHealthContainerObj.transform.SetParent(topHudRect, false);
        RectTransform bossHealthRt = bossHealthContainerObj.GetComponent<RectTransform>();
        bossHealthRt.anchorMin = new Vector2(0.5f, 1f);
        bossHealthRt.anchorMax = new Vector2(0.5f, 1f);
        bossHealthRt.pivot = new Vector2(0.5f, 0.5f);
        bossHealthRt.anchoredPosition = new Vector2(0f, -175f);
        bossHealthRt.sizeDelta = new Vector2(540f, 65f);

        CanvasGroup bossCanvasGroup = bossHealthContainerObj.GetComponent<CanvasGroup>();
        bossCanvasGroup.alpha = 0f;

        // Tên Boss
        TMP_Text bossNameTxt = CreateText("BossNameText", bossHealthContainerObj.transform, "BOSS FIGHT", 22f, new Color32(255, 90, 90, 255), TextAlignmentOptions.Center);
        bossNameTxt.rectTransform.anchoredPosition = new Vector2(0f, 20f);
        bossNameTxt.fontStyle = FontStyles.Bold;

        // Khung viền thanh máu Boss
        Image bossHpBorder = CreateImage("BossHpBorder", bossHealthContainerObj.transform, DarkBorder, rectSprite);
        bossHpBorder.rectTransform.anchoredPosition = new Vector2(0f, -10f);
        bossHpBorder.rectTransform.sizeDelta = new Vector2(520f, 26f);

        // Nền tối thanh máu Boss
        Color32 bossBgColor = new Color32(40, 10, 15, 255);
        Image bossHpBg = CreateImage("BossHpBg", bossHpBorder.transform, bossBgColor, rectSprite);
        bossHpBg.rectTransform.anchoredPosition = Vector2.zero;
        bossHpBg.rectTransform.sizeDelta = new Vector2(512f, 20f);

        // Thanh máu bóng (Ghost Damage Fill - tụt chậm)
        Color32 bossGhostColor = new Color32(180, 50, 40, 255);
        Image bossGhostFill = CreateImage("BossGhostFill", bossHpBorder.transform, bossGhostColor, rectSprite);
        bossGhostFill.rectTransform.anchoredPosition = Vector2.zero;
        bossGhostFill.rectTransform.sizeDelta = new Vector2(512f, 20f);
        bossGhostFill.type = Image.Type.Filled;
        bossGhostFill.fillMethod = Image.FillMethod.Horizontal;
        bossGhostFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        bossGhostFill.fillAmount = 1.0f;

        // Thanh máu chính Boss (Bright Red Fill)
        Color32 bossRedColor = new Color32(230, 40, 40, 255);
        Image bossHpFill = CreateImage("BossHpFill", bossHpBorder.transform, bossRedColor, rectSprite);
        bossHpFill.rectTransform.anchoredPosition = Vector2.zero;
        bossHpFill.rectTransform.sizeDelta = new Vector2(512f, 20f);
        bossHpFill.type = Image.Type.Filled;
        bossHpFill.fillMethod = Image.FillMethod.Horizontal;
        bossHpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        bossHpFill.fillAmount = 1.0f;

        // Số máu chi tiết
        TMP_Text bossHpNumTxt = CreateText("BossHpText", bossHpBorder.transform, "", 16f, Cream, TextAlignmentOptions.Center);
        bossHpNumTxt.rectTransform.anchoredPosition = new Vector2(0f, 0f);
        bossHpNumTxt.fontStyle = FontStyles.Bold;

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
        // D. PAUSE MODAL (STATS, CHIPSET, ARTIFACT)
        // ==========================================
        GameObject pauseModalObj = BuildPauseModal(canvasObj.transform, font);
        PauseModalController pauseModalCtrl = pauseModalObj.GetComponent<PauseModalController>();

        // ==========================================
        // E. CONTROLLERS LINKING
        // ==========================================
        EnemySpawner spawner = GameObject.FindObjectOfType<EnemySpawner>();
        PlayerLevelController levelCtrl = playerObj != null ? playerObj.GetComponent<PlayerLevelController>() : GameObject.FindObjectOfType<PlayerLevelController>();

        // Popup Level Up / Select Chipset (visual theo video tham chiếu)
        BuildChipsetLevelUpPopup(canvasObj.transform, levelCtrl, font);

        // 1. BossHealthBarUI linking
        BossHealthBarUI bossUiCtrl = bossHealthContainerObj.GetComponent<BossHealthBarUI>();
        if (bossUiCtrl == null)
        {
            bossUiCtrl = bossHealthContainerObj.AddComponent<BossHealthBarUI>();
        }
        SerializedObject bossSo = new SerializedObject(bossUiCtrl);
        bossSo.FindProperty("enemySpawner").objectReferenceValue = spawner;
        bossSo.FindProperty("canvasGroup").objectReferenceValue = bossCanvasGroup;
        bossSo.FindProperty("bossNameText").objectReferenceValue = bossNameTxt;
        bossSo.FindProperty("healthFillImage").objectReferenceValue = bossHpFill;
        bossSo.FindProperty("damageGhostFillImage").objectReferenceValue = bossGhostFill;
        bossSo.FindProperty("healthNumberText").objectReferenceValue = bossHpNumTxt;
        bossSo.ApplyModifiedProperties();
        EditorUtility.SetDirty(bossUiCtrl);

        // 2. WaveHUDController linking
        WaveHUDController hudCtrl = canvasObj.GetComponent<WaveHUDController>();
        if (hudCtrl == null)
        {
            hudCtrl = canvasObj.AddComponent<WaveHUDController>();
        }

        SerializedObject so = new SerializedObject(hudCtrl);
        so.FindProperty("enemySpawner").objectReferenceValue = spawner;
        so.FindProperty("playerLevelController").objectReferenceValue = levelCtrl;
        so.FindProperty("waveRadialFillImage").objectReferenceValue = waveRadialFill;
        so.FindProperty("waveLabelText").objectReferenceValue = waveLabel;
        so.FindProperty("waveNumberText").objectReferenceValue = waveNumber;
        so.FindProperty("levelText").objectReferenceValue = levelText;
        so.FindProperty("expFillImage").objectReferenceValue = expFill;
        so.FindProperty("pauseButton").objectReferenceValue = pauseBtn;
        so.FindProperty("pauseModalController").objectReferenceValue = pauseModalCtrl;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(hudCtrl);
        EditorUtility.SetDirty(canvasObj);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[GamePlayHUDSceneBuilder] ✅ Đã khởi tạo thành công HUD, Boss Health Bar & Pause Menu chuẩn pixel cho GamePlay!");
    }

    private static GameObject BuildChipsetLevelUpPopup(
        Transform canvasTransform,
        PlayerLevelController levelController,
        TMP_FontAsset fontAsset)
    {
        Transform existingRoot = canvasTransform.Find("ChipsetLevelUpPopup");
        if (existingRoot != null)
        {
            GameObject.DestroyImmediate(existingRoot.gameObject);
        }

        GameObject root = new GameObject(
            "ChipsetLevelUpPopup",
            typeof(RectTransform),
            typeof(CanvasGroup));
        root.transform.SetParent(canvasTransform, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        CanvasGroup rootGroup = root.GetComponent<CanvasGroup>();
        rootGroup.alpha = 1f;
        rootGroup.interactable = true;
        rootGroup.blocksRaycasts = true;

        // Gameplay vẫn đọc được rõ phía sau như reference, nhưng input bị khóa bởi dimmer.
        UnityEngine.UI.Image dimmer = CreateImage(
            "Dimmer",
            root.transform,
            new Color32(0, 0, 0, 128),
            rectSprite);
        Stretch(dimmer.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        dimmer.raycastTarget = true;

        // Lớp particle cơ khí: dùng glyph placeholder cho tới khi user gắn asset thật.
        GameObject particleLayerObject = new GameObject("MechanicalParticleLayer", typeof(RectTransform));
        particleLayerObject.transform.SetParent(root.transform, false);
        RectTransform particleRect = particleLayerObject.GetComponent<RectTransform>();
        particleRect.anchorMin = new Vector2(0f, 1f);
        particleRect.anchorMax = new Vector2(1f, 1f);
        particleRect.pivot = new Vector2(0.5f, 1f);
        particleRect.anchoredPosition = Vector2.zero;
        particleRect.sizeDelta = new Vector2(0f, 680f);
        ChipsetLevelUpParticleField particleField = particleLayerObject.AddComponent<ChipsetLevelUpParticleField>();
        particleField.SetParticleAssets(fontAsset, Array.Empty<Sprite>());

        GameObject safeArea = new GameObject("SafeArea", typeof(RectTransform));
        safeArea.transform.SetParent(root.transform, false);
        RectTransform safeRect = safeArea.GetComponent<RectTransform>();
        Stretch(safeRect, Vector2.zero, Vector2.one, new Vector2(42f, 35f), new Vector2(-42f, -35f));

        GameObject header = new GameObject("Header", typeof(RectTransform));
        header.transform.SetParent(safeArea.transform, false);
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0f, 560f);

        Sprite[] levelUpUiSprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(LevelUpUiPath)
            .OfType<Sprite>()
            .ToArray();
        Sprite levelUpTitleSprite = FindSprite(levelUpUiSprites, "Lever up");
        Sprite selectChipsetSprite = FindSprite(levelUpUiSprites, "Select chipset");
        Sprite drawAgainSprite = FindSprite(levelUpUiSprites, "Draw again");

        UnityEngine.UI.Image levelUpTitle = CreateImage(
            "LevelUpTitle",
            header.transform,
            Color.white,
            levelUpTitleSprite);
        levelUpTitle.preserveAspect = true;
        levelUpTitle.raycastTarget = false;
        levelUpTitle.rectTransform.anchorMin = levelUpTitle.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        levelUpTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
        levelUpTitle.rectTransform.anchoredPosition = new Vector2(0f, -265f);
        levelUpTitle.rectTransform.sizeDelta = new Vector2(700f, 137f);

        UnityEngine.UI.Image selectLabel = CreateImage(
            "SelectChipsetLabel",
            header.transform,
            Color.white,
            selectChipsetSprite);
        selectLabel.preserveAspect = true;
        selectLabel.raycastTarget = false;
        selectLabel.rectTransform.anchorMin = selectLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        selectLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
        selectLabel.rectTransform.anchoredPosition = new Vector2(0f, -412f);
        selectLabel.rectTransform.sizeDelta = new Vector2(510f, 100f);

        // ScrollView theo hierarchy chuẩn: ScrollRect > Viewport/Content + Scrollbar.
        GameObject scrollObject = new GameObject(
            "ChoicesScrollRect",
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image),
            typeof(UnityEngine.UI.ScrollRect));
        scrollObject.transform.SetParent(safeArea.transform, false);
        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = scrollRectTransform.anchorMax = new Vector2(0.5f, 1f);
        scrollRectTransform.pivot = new Vector2(0.5f, 1f);
        scrollRectTransform.anchoredPosition = new Vector2(0f, -600f);
        scrollRectTransform.sizeDelta = new Vector2(760f, 850f);
        UnityEngine.UI.Image scrollBackground = scrollObject.GetComponent<UnityEngine.UI.Image>();
        scrollBackground.color = Color.clear;
        scrollBackground.raycastTarget = false;

        GameObject viewportObject = new GameObject(
            "Viewport",
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image),
            typeof(UnityEngine.UI.Mask));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        Stretch(viewportRect, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-24f, 0f));
        UnityEngine.UI.Image viewportImage = viewportObject.GetComponent<UnityEngine.UI.Image>();
        viewportImage.color = new Color32(255, 255, 255, 1);
        viewportImage.raycastTarget = true;
        viewportObject.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;

        GameObject contentObject = new GameObject(
            "Content",
            typeof(RectTransform),
            typeof(UnityEngine.UI.VerticalLayoutGroup),
            typeof(UnityEngine.UI.ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        UnityEngine.UI.VerticalLayoutGroup layout = contentObject.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        UnityEngine.UI.ContentSizeFitter fitter = contentObject.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        Sprite[] allAtlasSprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(ChipsetAtlasPath)
            .OfType<Sprite>()
            .ToArray();
        ChipsetLevelVisualLibrary levelVisualLibrary = AssetDatabase.LoadAssetAtPath<ChipsetLevelVisualLibrary>(ChipsetLevelVisualLibraryPath);
        Sprite commonFrame = levelVisualLibrary != null && levelVisualLibrary.tierLeverFrames.Length > 0
            ? levelVisualLibrary.tierLeverFrames[0]
            : FindSprite(allAtlasSprites, "card-frame-common");

        ChipsetChoiceCardUI[] cards = new ChipsetChoiceCardUI[4];
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i] = CreateLevelUpChoiceCard(contentObject.transform, i + 1, commonFrame, fontAsset);
        }

        GameObject scrollbarObject = new GameObject(
            "Scrollbar",
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image),
            typeof(UnityEngine.UI.Scrollbar));
        scrollbarObject.transform.SetParent(scrollObject.transform, false);
        RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 0.5f);
        scrollbarRect.offsetMin = new Vector2(-14f, 8f);
        scrollbarRect.offsetMax = new Vector2(-2f, -8f);
        scrollbarObject.GetComponent<UnityEngine.UI.Image>().color = Color.clear;

        GameObject slidingArea = new GameObject("SlidingArea", typeof(RectTransform));
        slidingArea.transform.SetParent(scrollbarObject.transform, false);
        Stretch(slidingArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        handleObject.transform.SetParent(slidingArea.transform, false);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        Stretch(handleRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        UnityEngine.UI.Image handleImage = handleObject.GetComponent<UnityEngine.UI.Image>();
        handleImage.color = Color.clear;

        UnityEngine.UI.Scrollbar scrollbar = scrollbarObject.GetComponent<UnityEngine.UI.Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        scrollbar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
        scrollbar.size = 0.42f;

        UnityEngine.UI.ScrollRect scrollRect = scrollObject.GetComponent<UnityEngine.UI.ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 35f;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;
        scrollRect.verticalScrollbarSpacing = 8f;

        GameObject rerollObject = new GameObject(
            "DrawAgainButton",
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image),
            typeof(UnityEngine.UI.Button),
            typeof(CanvasGroup));
        rerollObject.transform.SetParent(safeArea.transform, false);
        RectTransform rerollRect = rerollObject.GetComponent<RectTransform>();
        rerollRect.anchorMin = rerollRect.anchorMax = new Vector2(0.5f, 0f);
        rerollRect.pivot = new Vector2(0.5f, 0f);
        rerollRect.anchoredPosition = new Vector2(0f, 225f);
        rerollRect.sizeDelta = new Vector2(470f, 151f);
        UnityEngine.UI.Image rerollImage = rerollObject.GetComponent<UnityEngine.UI.Image>();
        rerollImage.sprite = drawAgainSprite;
        rerollImage.color = Color.white;
        rerollImage.preserveAspect = true;
        UnityEngine.UI.Button rerollButton = rerollObject.GetComponent<UnityEngine.UI.Button>();
        rerollButton.targetGraphic = rerollImage;
        ColorBlock rerollColors = rerollButton.colors;
        rerollColors.normalColor = Color.white;
        rerollColors.highlightedColor = Color.white;
        rerollColors.pressedColor = new Color(0.76f, 0.9f, 0.82f, 1f);
        rerollColors.disabledColor = new Color(0.42f, 0.48f, 0.44f, 0.75f);
        rerollColors.fadeDuration = 0.08f;
        rerollButton.colors = rerollColors;

        ChipsetLevelUpPopup controller = canvasTransform.GetComponent<ChipsetLevelUpPopup>();
        if (controller == null)
        {
            controller = canvasTransform.gameObject.AddComponent<ChipsetLevelUpPopup>();
        }

        string[] iconKeys =
        {
            "standard-gun", "rifle", "rocket-punch", "spinning-blade", "multigun", "gun-turret",
            "spiky-discus", "shotgun", "energy-jumper-cables", "high-explosive-mine", "aiming-lens", "plasma-field",
            "laser-eye", "biochemical-mine", "tesla-coil", "atk-module", "black-hole-mine", "sonic-boom",
            "big-battery", "turret-module", "ice-turret", "invincible-shield", "healing-turret", "flamethrower"
        };
        Sprite[] icons = levelVisualLibrary != null
            ? levelVisualLibrary.primaryChipIcons
            : iconKeys.Select(key => FindSprite(allAtlasSprites, key)).Where(sprite => sprite != null).ToArray();
        Sprite[] frames = levelVisualLibrary != null
            ? levelVisualLibrary.tierLeverFrames
            : Array.Empty<Sprite>();
        Sprite[] levelPips = levelVisualLibrary != null
            ? levelVisualLibrary.levelPipSprites
            : Array.Empty<Sprite>();

        controller.InitializeReferences(
            levelController,
            root,
            rootGroup,
            levelUpTitle.rectTransform,
            cards,
            rerollButton,
            null,
            null,
            icons,
            frames,
            levelPips,
            Array.Empty<Sprite>());

        root.SetActive(false);
        EditorUtility.SetDirty(controller);
        return root;
    }

    private static ChipsetChoiceCardUI CreateLevelUpChoiceCard(
        Transform parent,
        int index,
        Sprite defaultFrame,
        TMP_FontAsset fontAsset)
    {
        GameObject root = new GameObject(
            $"ChipsetChoiceCard_{index:00}",
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image),
            typeof(CanvasGroup),
            typeof(UnityEngine.UI.Button),
            typeof(UnityEngine.UI.LayoutElement));
        root.transform.SetParent(parent, false);

        UnityEngine.UI.LayoutElement layoutElement = root.GetComponent<UnityEngine.UI.LayoutElement>();
        layoutElement.preferredHeight = 190f;
        layoutElement.minHeight = 190f;

        UnityEngine.UI.Image border = root.GetComponent<UnityEngine.UI.Image>();
        border.color = new Color32(116, 244, 239, 255);

        UnityEngine.UI.Image background = CreateImage(
            "Background",
            root.transform,
            new Color32(10, 58, 83, 245),
            rectSprite);
        Stretch(background.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        UnityEngine.UI.Button button = root.GetComponent<UnityEngine.UI.Button>();
        button.targetGraphic = background;

        UnityEngine.UI.Image iconFrame = CreateImage("IconFrameAssetSlot", root.transform, Color.white, defaultFrame);
        iconFrame.rectTransform.anchorMin = iconFrame.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        iconFrame.rectTransform.pivot = new Vector2(0f, 0.5f);
        iconFrame.rectTransform.anchoredPosition = new Vector2(22f, 0f);
        iconFrame.rectTransform.sizeDelta = new Vector2(172f, 172f);
        iconFrame.preserveAspect = true;
        iconFrame.raycastTarget = false;

        UnityEngine.UI.Image icon = CreateImage("ChipIcon", iconFrame.transform, Color.white, null);
        // Designer-approved Level Up layout saved in GamePlay.unity.
        icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        icon.rectTransform.anchoredPosition = new Vector2(-16.9f, 19.8f);
        icon.rectTransform.sizeDelta = new Vector2(107.569725f, 82.56f);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        TMP_Text title = CreateText(
            "Title",
            root.transform,
            "Chipset LV.01",
            31f,
            new Color32(255, 177, 31, 255),
            TextAlignmentOptions.Left);
        if (fontAsset != null) title.font = fontAsset;
        title.fontStyle = FontStyles.Bold;
        title.outlineColor = Color.black;
        title.outlineWidth = 0.14f;
        title.rectTransform.anchorMin = new Vector2(0f, 0.53f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.offsetMin = new Vector2(215f, 0f);
        title.rectTransform.offsetMax = new Vector2(-18f, -12f);
        title.raycastTarget = false;

        TMP_Text description = CreateText(
            "Description",
            root.transform,
            "Chipset description.",
            27f,
            Color.white,
            TextAlignmentOptions.TopLeft);
        if (fontAsset != null) description.font = fontAsset;
        description.fontStyle = FontStyles.Bold;
        description.enableWordWrapping = true;
        description.rectTransform.anchorMin = new Vector2(0f, 0f);
        description.rectTransform.anchorMax = new Vector2(1f, 0.62f);
        description.rectTransform.offsetMin = new Vector2(215f, 14f);
        description.rectTransform.offsetMax = new Vector2(-18f, -7f);
        description.raycastTarget = false;

        ChipsetChoiceCardUI card = root.AddComponent<ChipsetChoiceCardUI>();
        card.InitializeReferences(
            border,
            background,
            iconFrame,
            icon,
            title,
            description,
            button,
            root.GetComponent<CanvasGroup>());
        return card;
    }

    private static Sprite FindSprite(Sprite[] sprites, string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName) || sprites == null || sprites.Length == 0) return null;

        string cleanName = spriteName.Replace(" ", "").Replace("-", "").Replace("_", "").ToLowerInvariant();

        // 1. Khớp chính xác
        Sprite match = sprites.FirstOrDefault(s => s != null && (
            string.Equals(s.name, spriteName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(s.name.Replace(" ", "").Replace("-", "").Replace("_", "").ToLowerInvariant(), cleanName)
        ));
        if (match != null) return match;

        // 2. Numeric slice mapping cho icon chipset
        string numKey = null;
        if (cleanName.Contains("highexplosive") || cleanName.Contains("mine") && !cleanName.Contains("blackhole") && !cleanName.Contains("biochemical")) numKey = "1";
        else if (cleanName.Contains("energyjumper") || cleanName.Contains("jumpercable")) numKey = "2";
        else if (cleanName.Contains("shotgun")) numKey = "3";
        else if (cleanName.Contains("spiky") || cleanName.Contains("discus") || cleanName.Contains("spicky")) numKey = "4";
        else if (cleanName.Contains("gunturret") || cleanName.Equals("turret")) numKey = "5";
        else if (cleanName.Contains("multigun")) numKey = "6";
        else if (cleanName.Contains("spinningblade") || cleanName.Contains("blade")) numKey = "7";
        else if (cleanName.Contains("rocketpunch") || cleanName.Contains("punch")) numKey = "8";
        else if (cleanName.Contains("standardgun") || cleanName.Equals("gun") || cleanName.Equals("pistol")) numKey = "9";
        else if (cleanName.Contains("rifle") || cleanName.Contains("assault")) numKey = "10";

        if (!string.IsNullOrEmpty(numKey))
        {
            match = sprites.FirstOrDefault(s => s != null && s.name == numKey);
            if (match != null) return match;
        }

        // 3. Khung bậc
        if (cleanName.Contains("green") || cleanName.Contains("tier1") || cleanName.Contains("magic") || cleanName.Contains("common"))
            return sprites.FirstOrDefault(s => s != null && (s.name == "Green" || s.name == "card-frame-tier1-green"));
        if (cleanName.Contains("blue") || cleanName.Contains("tier2") || cleanName.Contains("rare") || cleanName.Contains("blu"))
            return sprites.FirstOrDefault(s => s != null && (s.name == "Blu" || s.name == "card-frame-tier2-blue"));
        if (cleanName.Contains("purple") || cleanName.Contains("tier3") || cleanName.Contains("unique") || cleanName.Contains("tim") || cleanName.Contains("tím"))
            return sprites.FirstOrDefault(s => s != null && (s.name == "Tím" || s.name.Contains("T") && s.name.Contains("m") || s.name == "card-frame-tier3-purple"));
        if (cleanName.Contains("yellow") || cleanName.Contains("tier4") || cleanName.Contains("epic") || cleanName.Contains("yello") || cleanName.Contains("gold"))
            return sprites.FirstOrDefault(s => s != null && (s.name == "Yello" || s.name == "card-frame-tier4-yellow"));
        if (cleanName.Contains("holo") || cleanName.Contains("rainbow") || cleanName.Contains("tier5") || cleanName.Contains("red"))
            return sprites.FirstOrDefault(s => s != null && (s.name == "card-frame-tier5-holographic" || s.name == "Red" || s.name == "card-frame-tier5-red"));

        return null;
    }

    private static GameObject BuildPauseModal(Transform canvasTr, TMP_FontAsset fontAsset)
    {
        Transform existing = canvasTr.Find("PauseModal");
        if (existing != null)
        {
            GameObject.DestroyImmediate(existing.gameObject);
        }

        GameObject modalRoot = new GameObject("PauseModal", typeof(RectTransform));
        modalRoot.transform.SetParent(canvasTr, false);
        RectTransform modalRect = modalRoot.GetComponent<RectTransform>();
        Stretch(modalRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Backdrop Dimmer
        Image overlayBg = modalRoot.AddComponent<Image>();
        overlayBg.color = new Color32(5, 15, 22, 230);
        overlayBg.raycastTarget = true;

        // Title "Pause" (Top)
        TMP_Text titleText = CreateText("PauseTitle", modalRoot.transform, "Pause", 64f, Cream, TextAlignmentOptions.Center);
        titleText.fontStyle = FontStyles.Bold;
        titleText.outlineColor = DarkBorder;
        titleText.outlineWidth = 0.2f;
        titleText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        titleText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        titleText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        titleText.rectTransform.anchoredPosition = new Vector2(0f, 650f);
        titleText.rectTransform.sizeDelta = new Vector2(500f, 80f);

        // Main Frame Container (940 x 1080)
        GameObject frameObj = CreateFrame("MainFrame", modalRoot.transform, new Color32(24, 52, 68, 255), new Color32(88, 172, 178, 255), out Image frameBg);
        RectTransform frameRt = frameObj.GetComponent<RectTransform>();
        frameRt.anchorMin = new Vector2(0.5f, 0.5f);
        frameRt.anchorMax = new Vector2(0.5f, 0.5f);
        frameRt.pivot = new Vector2(0.5f, 0.5f);
        frameRt.anchoredPosition = new Vector2(0f, 30f);
        frameRt.sizeDelta = new Vector2(940f, 1080f);

        // 3 Main Tab Buttons Row (Anchored to modalRoot)
        // STATS Tab
        GameObject statsTabObj = CreateFrame("StatsTabButton", modalRoot.transform, new Color32(88, 172, 178, 255), new Color32(88, 172, 178, 255), out Image statsTabBg);
        RectTransform statsTabRt = statsTabObj.GetComponent<RectTransform>();
        statsTabRt.anchorMin = new Vector2(0.5f, 0.5f);
        statsTabRt.anchorMax = new Vector2(0.5f, 0.5f);
        statsTabRt.pivot = new Vector2(0.5f, 0.5f);
        statsTabRt.anchoredPosition = new Vector2(-280f, 600f);
        statsTabRt.sizeDelta = new Vector2(270f, 64f);
        Button statsTabBtn = statsTabObj.AddComponent<Button>();
        statsTabBtn.targetGraphic = statsTabBg;
        TMP_Text statsTabTxt = CreateText("Label", statsTabObj.transform, "STATS", 28f, new Color32(14, 28, 36, 255), TextAlignmentOptions.Center);
        statsTabTxt.fontStyle = FontStyles.Bold;
        Stretch(statsTabTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // CHIPSET Tab
        GameObject chipTabObj = CreateFrame("ChipsetTabButton", modalRoot.transform, new Color32(36, 70, 86, 255), new Color32(88, 172, 178, 255), out Image chipTabBg);
        RectTransform chipTabRt = chipTabObj.GetComponent<RectTransform>();
        chipTabRt.anchorMin = new Vector2(0.5f, 0.5f);
        chipTabRt.anchorMax = new Vector2(0.5f, 0.5f);
        chipTabRt.pivot = new Vector2(0.5f, 0.5f);
        chipTabRt.anchoredPosition = new Vector2(0f, 600f);
        chipTabRt.sizeDelta = new Vector2(270f, 64f);
        Button chipTabBtn = chipTabObj.AddComponent<Button>();
        chipTabBtn.targetGraphic = chipTabBg;
        TMP_Text chipTabTxt = CreateText("Label", chipTabObj.transform, "CHIPSET", 28f, new Color32(160, 200, 205, 255), TextAlignmentOptions.Center);
        chipTabTxt.fontStyle = FontStyles.Bold;
        Stretch(chipTabTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // ARTIFACT Tab
        GameObject artTabObj = CreateFrame("ArtifactTabButton", modalRoot.transform, new Color32(36, 70, 86, 255), new Color32(88, 172, 178, 255), out Image artTabBg);
        RectTransform artTabRt = artTabObj.GetComponent<RectTransform>();
        artTabRt.anchorMin = new Vector2(0.5f, 0.5f);
        artTabRt.anchorMax = new Vector2(0.5f, 0.5f);
        artTabRt.pivot = new Vector2(0.5f, 0.5f);
        artTabRt.anchoredPosition = new Vector2(280f, 600f);
        artTabRt.sizeDelta = new Vector2(270f, 64f);
        Button artTabBtn = artTabObj.AddComponent<Button>();
        artTabBtn.targetGraphic = artTabBg;
        TMP_Text artTabTxt = CreateText("Label", artTabObj.transform, "ARTIFACT", 28f, new Color32(160, 200, 205, 255), TextAlignmentOptions.Center);
        artTabTxt.fontStyle = FontStyles.Bold;
        Stretch(artTabTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // ==========================================
        // 1. STATS PANEL
        // ==========================================
        GameObject statsPanelObj = new GameObject("StatsPanel", typeof(RectTransform));
        statsPanelObj.transform.SetParent(frameObj.transform, false);
        Stretch(statsPanelObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Sub-tabs row: DEF, Attack, Other
        GameObject defSubObj = CreateFrame("DefSubTab", statsPanelObj.transform, new Color32(88, 172, 178, 255), new Color32(88, 172, 178, 255), out Image defSubBg);
        RectTransform defSubRt = defSubObj.GetComponent<RectTransform>();
        defSubRt.anchoredPosition = new Vector2(-280f, 470f);
        defSubRt.sizeDelta = new Vector2(270f, 54f);
        Button defSubBtn = defSubObj.AddComponent<Button>();
        defSubBtn.targetGraphic = defSubBg;
        TMP_Text defSubTxt = CreateText("Label", defSubObj.transform, "DEF", 26f, new Color32(14, 28, 36, 255), TextAlignmentOptions.Center);
        defSubTxt.fontStyle = FontStyles.Bold;
        Stretch(defSubTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject atkSubObj = CreateFrame("AttackSubTab", statsPanelObj.transform, new Color32(48, 80, 96, 255), new Color32(88, 172, 178, 255), out Image atkSubBg);
        RectTransform atkSubRt = atkSubObj.GetComponent<RectTransform>();
        atkSubRt.anchoredPosition = new Vector2(0f, 470f);
        atkSubRt.sizeDelta = new Vector2(270f, 54f);
        Button atkSubBtn = atkSubObj.AddComponent<Button>();
        atkSubBtn.targetGraphic = atkSubBg;
        TMP_Text atkSubTxt = CreateText("Label", atkSubObj.transform, "Attack", 26f, new Color32(160, 200, 205, 255), TextAlignmentOptions.Center);
        atkSubTxt.fontStyle = FontStyles.Bold;
        Stretch(atkSubTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject othSubObj = CreateFrame("OtherSubTab", statsPanelObj.transform, new Color32(48, 80, 96, 255), new Color32(88, 172, 178, 255), out Image othSubBg);
        RectTransform othSubRt = othSubObj.GetComponent<RectTransform>();
        othSubRt.anchoredPosition = new Vector2(280f, 470f);
        othSubRt.sizeDelta = new Vector2(270f, 54f);
        Button othSubBtn = othSubObj.AddComponent<Button>();
        othSubBtn.targetGraphic = othSubBg;
        TMP_Text othSubTxt = CreateText("Label", othSubObj.transform, "Other", 26f, new Color32(160, 200, 205, 255), TextAlignmentOptions.Center);
        othSubTxt.fontStyle = FontStyles.Bold;
        Stretch(othSubTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Left Side: Character Card Box (350 x 850)
        GameObject charCardObj = CreateFrame("CharacterCard", statsPanelObj.transform, new Color32(18, 40, 52, 255), new Color32(88, 172, 178, 255), out _);
        RectTransform charCardRt = charCardObj.GetComponent<RectTransform>();
        charCardRt.anchoredPosition = new Vector2(-265f, -60f);
        charCardRt.sizeDelta = new Vector2(340f, 850f);

        // Avatar Image
        Sprite playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Character/thân.png");
        Image avatarImg = CreateImage("Avatar", charCardObj.transform, Color.white, playerSprite);
        avatarImg.preserveAspect = true;
        avatarImg.rectTransform.anchoredPosition = new Vector2(0f, 100f);
        avatarImg.rectTransform.sizeDelta = new Vector2(220f, 220f);

        // Character Name "Bernard"
        TMP_Text charNameTxt = CreateText("CharName", charCardObj.transform, "Bernard", 36f, Cream, TextAlignmentOptions.Center);
        charNameTxt.fontStyle = FontStyles.Bold;
        charNameTxt.rectTransform.anchoredPosition = new Vector2(0f, -140f);
        charNameTxt.rectTransform.sizeDelta = new Vector2(300f, 50f);

        // Character Level "LV.01 (0,00%)"
        TMP_Text charLvlTxt = CreateText("CharLevel", charCardObj.transform, "LV.01 (0,00%)", 28f, new Color32(245, 195, 75, 255), TextAlignmentOptions.Center);
        charLvlTxt.fontStyle = FontStyles.Bold;
        charLvlTxt.rectTransform.anchoredPosition = new Vector2(0f, -200f);
        charLvlTxt.rectTransform.sizeDelta = new Vector2(300f, 50f);

        // Right Side: Sub-Panels Container
        GameObject statsSubContainer = new GameObject("StatsSubContainer", typeof(RectTransform));
        statsSubContainer.transform.SetParent(statsPanelObj.transform, false);
        RectTransform statsSubRt = statsSubContainer.GetComponent<RectTransform>();
        statsSubRt.anchoredPosition = new Vector2(185f, -60f);
        statsSubRt.sizeDelta = new Vector2(490f, 850f);

        // 1A. DEF STATS PANEL
        GameObject defPanelObj = new GameObject("DefStatsPanel", typeof(RectTransform));
        defPanelObj.transform.SetParent(statsSubContainer.transform, false);
        Stretch(defPanelObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TMP_Text hpVal = CreateStatRow("HpRow", defPanelObj.transform, new Vector2(0f, 360f), "HP", "260/260");
        TMP_Text defVal = CreateStatRow("DefRow", defPanelObj.transform, new Vector2(0f, 240f), "DEF", "9");
        TMP_Text rangedDefVal = CreateStatRow("RangedDefRow", defPanelObj.transform, new Vector2(0f, 120f), "RANGED DEFENSE", "0%");
        TMP_Text evasionVal = CreateStatRow("EvasionRow", defPanelObj.transform, new Vector2(0f, 0f), "EVASION RATE", "3%");
        TMP_Text kitRecovVal = CreateStatRow("KitRecoveryRow", defPanelObj.transform, new Vector2(0f, -120f), "KIT RECOVERY", "30%");
        TMP_Text autoRecovVal = CreateStatRow("AutoRecoveryRow", defPanelObj.transform, new Vector2(0f, -240f), "AUTO RECOVERY", "1,1/sec");
        TMP_Text ailmentVal = CreateStatRow("AilmentRow", defPanelObj.transform, new Vector2(0f, -360f), "AILMENT RESISTANCE", "0%");

        // 1B. ATTACK STATS PANEL
        GameObject atkPanelObj = new GameObject("AttackStatsPanel", typeof(RectTransform));
        atkPanelObj.transform.SetParent(statsSubContainer.transform, false);
        Stretch(atkPanelObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TMP_Text atkVal = CreateStatRow("AtkRow", atkPanelObj.transform, new Vector2(0f, 240f), "ATK", "3,5%");
        TMP_Text atkSpdVal = CreateStatRow("AtkSpeedRow", atkPanelObj.transform, new Vector2(0f, 120f), "ATK SPEED", "0%");
        TMP_Text critAtkVal = CreateStatRow("CritAtkRow", atkPanelObj.transform, new Vector2(0f, 0f), "CRIT ATK", "150%");
        TMP_Text critRateVal = CreateStatRow("CritRateRow", atkPanelObj.transform, new Vector2(0f, -120f), "CRIT RATE", "3,5%");
        TMP_Text lifeStealVal = CreateStatRow("LifeStealRow", atkPanelObj.transform, new Vector2(0f, -240f), "LIFE STEAL", "0%");
        atkPanelObj.SetActive(false);

        // 1C. OTHER STATS PANEL
        GameObject othPanelObj = new GameObject("OtherStatsPanel", typeof(RectTransform));
        othPanelObj.transform.SetParent(statsSubContainer.transform, false);
        Stretch(othPanelObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TMP_Text moveSpdVal = CreateStatRow("MoveSpeedRow", othPanelObj.transform, new Vector2(0f, 300f), "MOVE SPEED", "2%");
        TMP_Text chipsVal = CreateStatRow("ObtainedChipsRow", othPanelObj.transform, new Vector2(0f, 180f), "OBTAINED CHIPS", "2%");
        TMP_Text chipSelVal = CreateStatRow("ChipSelectionRow", othPanelObj.transform, new Vector2(0f, 60f), "CHIPSET SELECTION +1", "3%");
        TMP_Text droneAtkVal = CreateStatRow("DroneAtkRow", othPanelObj.transform, new Vector2(0f, -60f), "DRONE ATK", "0%");
        TMP_Text turretAtkVal = CreateStatRow("TurretAtkRow", othPanelObj.transform, new Vector2(0f, -180f), "TURRET ATK", "0%");
        TMP_Text turretDurVal = CreateStatRow("TurretDurationRow", othPanelObj.transform, new Vector2(0f, -300f), "TURRET DURATION", "0%");
        othPanelObj.SetActive(false);

        // ==========================================
        // 2. CHIPSET PANEL
        // ==========================================
        GameObject chipPanelObj = new GameObject("ChipsetPanel", typeof(RectTransform));
        chipPanelObj.transform.SetParent(frameObj.transform, false);
        Stretch(chipPanelObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Weapon/Chip Card
        GameObject chipCardObj = CreateFrame("EquippedChipCard", chipPanelObj.transform, new Color32(18, 40, 52, 255), new Color32(88, 172, 178, 255), out _);
        RectTransform chipCardRt = chipCardObj.GetComponent<RectTransform>();
        chipCardRt.anchoredPosition = new Vector2(-320f, 360f);
        chipCardRt.sizeDelta = new Vector2(170f, 210f);

        ChipsetLevelVisualLibrary visualLib = Resources.Load<ChipsetLevelVisualLibrary>("ChipsetLevelVisualLibrary");
        Sprite leverGreenSprite = null;
        if (visualLib != null && visualLib.tierLeverFrames != null && visualLib.tierLeverFrames.Length > 0)
        {
            leverGreenSprite = visualLib.tierLeverFrames[0];
        }
        else
        {
            string frameAtlasPath = "Assets/Sprites/UI/Chipset/khung chipset (1).png";
            var frames = AssetDatabase.LoadAllAssetsAtPath(frameAtlasPath).OfType<Sprite>().ToArray();
            leverGreenSprite = ChipsetLevelUpPopup.ResolveGreenLeverFrame(frames);
        }

        Image iconFrameImg = CreateImage("IconFrame", chipCardObj.transform, Color.white, leverGreenSprite);
        iconFrameImg.preserveAspect = true;
        iconFrameImg.rectTransform.anchoredPosition = new Vector2(0f, 30f);
        iconFrameImg.rectTransform.sizeDelta = new Vector2(120f, 120f);

        Sprite gunSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Gun.png");
        Image gunImg = CreateImage("GunIcon", chipCardObj.transform, Color.white, gunSprite);
        gunImg.preserveAspect = true;
        gunImg.rectTransform.anchoredPosition = new Vector2(0f, 34f);
        gunImg.rectTransform.sizeDelta = new Vector2(85f, 65f);

        GameObject lvlBadge = CreateFrame("LvlBadge", chipCardObj.transform, new Color32(88, 172, 178, 255), DarkBorder, out _);
        RectTransform lvlBadgeRt = lvlBadge.GetComponent<RectTransform>();
        lvlBadgeRt.anchoredPosition = new Vector2(0f, -65f);
        lvlBadgeRt.sizeDelta = new Vector2(130f, 38f);
        TMP_Text lvlBadgeTxt = CreateText("Label", lvlBadge.transform, "LV.01", 24f, new Color32(14, 28, 36, 255), TextAlignmentOptions.Center);
        lvlBadgeTxt.fontStyle = FontStyles.Bold;
        Stretch(lvlBadgeTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        chipPanelObj.SetActive(false);

        // ==========================================
        // 3. ARTIFACT PANEL
        // ==========================================
        GameObject artPanelObj = new GameObject("ArtifactPanel", typeof(RectTransform));
        artPanelObj.transform.SetParent(frameObj.transform, false);
        Stretch(artPanelObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TMP_Text artMsgTxt = CreateText("ArtifactMessage", artPanelObj.transform, "You can get it from a\nshiny box in the field.", 34f, Cream, TextAlignmentOptions.Center);
        artMsgTxt.rectTransform.anchoredPosition = new Vector2(0f, 0f);
        artMsgTxt.rectTransform.sizeDelta = new Vector2(800f, 200f);
        artPanelObj.SetActive(false);

        // ==========================================
        // 4. BOTTOM ACTION BUTTONS
        // ==========================================
        GameObject resumeBtnObj = CreateFrame("ResumeButton", modalRoot.transform, new Color32(64, 158, 166, 255), DarkBorder, out Image resumeBtnBg);
        RectTransform resumeRt = resumeBtnObj.GetComponent<RectTransform>();
        resumeRt.anchoredPosition = new Vector2(-110f, -680f);
        resumeRt.sizeDelta = new Vector2(170f, 110f);
        Button resumeBtn = resumeBtnObj.AddComponent<Button>();
        resumeBtn.targetGraphic = resumeBtnBg;
        TMP_Text resumeTxt = CreateText("Icon", resumeBtnObj.transform, "▶", 48f, Color.white, TextAlignmentOptions.Center);
        Stretch(resumeTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject homeBtnObj = CreateFrame("HomeButton", modalRoot.transform, new Color32(64, 158, 166, 255), DarkBorder, out Image homeBtnBg);
        RectTransform homeRt = homeBtnObj.GetComponent<RectTransform>();
        homeRt.anchoredPosition = new Vector2(110f, -680f);
        homeRt.sizeDelta = new Vector2(170f, 110f);
        Button homeBtn = homeBtnObj.AddComponent<Button>();
        homeBtn.targetGraphic = homeBtnBg;
        TMP_Text homeTxt = CreateText("Icon", homeBtnObj.transform, "⌂", 56f, Color.white, TextAlignmentOptions.Center);
        Stretch(homeTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // ==========================================
        // 5. QUIT CONFIRMATION DIALOG (MODAL OVERLAY)
        // ==========================================
        GameObject confirmModalObj = new GameObject("QuitConfirmDialog", typeof(RectTransform));
        confirmModalObj.transform.SetParent(modalRoot.transform, false);
        RectTransform confirmModalRt = confirmModalObj.GetComponent<RectTransform>();
        Stretch(confirmModalRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image confirmOverlay = confirmModalObj.AddComponent<Image>();
        confirmOverlay.color = new Color32(0, 0, 0, 160);
        confirmOverlay.raycastTarget = true;

        GameObject confirmCardObj = CreateFrame("ConfirmCard", confirmModalObj.transform, new Color32(24, 52, 68, 255), new Color32(88, 172, 178, 255), out _);
        RectTransform confirmCardRt = confirmCardObj.GetComponent<RectTransform>();
        confirmCardRt.anchorMin = new Vector2(0.5f, 0.5f);
        confirmCardRt.anchorMax = new Vector2(0.5f, 0.5f);
        confirmCardRt.pivot = new Vector2(0.5f, 0.5f);
        confirmCardRt.anchoredPosition = new Vector2(0f, 0f);
        confirmCardRt.sizeDelta = new Vector2(880f, 440f);

        TMP_Text confirmMsgTxt = CreateText("Message", confirmCardObj.transform, "You will return to the main\nmenu.\nThe game progress will be reset.", 34f, Cream, TextAlignmentOptions.Center);
        confirmMsgTxt.fontStyle = FontStyles.Bold;
        confirmMsgTxt.rectTransform.anchoredPosition = new Vector2(0f, 65f);
        confirmMsgTxt.rectTransform.sizeDelta = new Vector2(800f, 160f);

        GameObject noBtnObj = CreateFrame("NoButton", confirmCardObj.transform, new Color32(96, 120, 136, 255), DarkBorder, out Image noBtnBg);
        RectTransform noBtnRt = noBtnObj.GetComponent<RectTransform>();
        noBtnRt.anchoredPosition = new Vector2(-190f, -110f);
        noBtnRt.sizeDelta = new Vector2(320f, 100f);
        Button noBtn = noBtnObj.AddComponent<Button>();
        noBtn.targetGraphic = noBtnBg;
        TMP_Text noTxt = CreateText("Label", noBtnObj.transform, "No", 34f, Cream, TextAlignmentOptions.Center);
        noTxt.fontStyle = FontStyles.Bold;
        Stretch(noTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject okBtnObj = CreateFrame("OkButton", confirmCardObj.transform, new Color32(88, 172, 178, 255), DarkBorder, out Image okBtnBg);
        RectTransform okBtnRt = okBtnObj.GetComponent<RectTransform>();
        okBtnRt.anchoredPosition = new Vector2(190f, -110f);
        okBtnRt.sizeDelta = new Vector2(320f, 100f);
        Button okBtn = okBtnObj.AddComponent<Button>();
        okBtn.targetGraphic = okBtnBg;
        TMP_Text okTxt = CreateText("Label", okBtnObj.transform, "OK", 34f, new Color32(14, 28, 36, 255), TextAlignmentOptions.Center);
        okTxt.fontStyle = FontStyles.Bold;
        Stretch(okTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        confirmModalObj.SetActive(false);

        // Controller component
        PauseModalController pauseCtrl = modalRoot.AddComponent<PauseModalController>();
        SerializedObject so = new SerializedObject(pauseCtrl);
        so.FindProperty("modalRoot").objectReferenceValue = modalRoot;
        so.FindProperty("statsMainTabButton").objectReferenceValue = statsTabBtn;
        so.FindProperty("chipsetMainTabButton").objectReferenceValue = chipTabBtn;
        so.FindProperty("artifactMainTabButton").objectReferenceValue = artTabBtn;
        so.FindProperty("statsTabBg").objectReferenceValue = statsTabBg;
        so.FindProperty("chipsetTabBg").objectReferenceValue = chipTabBg;
        so.FindProperty("artifactTabBg").objectReferenceValue = artTabBg;
        so.FindProperty("statsTabText").objectReferenceValue = statsTabTxt;
        so.FindProperty("chipsetTabText").objectReferenceValue = chipTabTxt;
        so.FindProperty("artifactTabText").objectReferenceValue = artTabTxt;
        so.FindProperty("statsPanel").objectReferenceValue = statsPanelObj;
        so.FindProperty("chipsetPanel").objectReferenceValue = chipPanelObj;
        so.FindProperty("equippedChipCardTemplate").objectReferenceValue = chipCardObj;
        so.FindProperty("visualLibrary").objectReferenceValue = visualLib;
        so.FindProperty("artifactPanel").objectReferenceValue = artPanelObj;
        so.FindProperty("defSubTabButton").objectReferenceValue = defSubBtn;
        so.FindProperty("attackSubTabButton").objectReferenceValue = atkSubBtn;
        so.FindProperty("otherSubTabButton").objectReferenceValue = othSubBtn;
        so.FindProperty("defSubTabBg").objectReferenceValue = defSubBg;
        so.FindProperty("attackSubTabBg").objectReferenceValue = atkSubBg;
        so.FindProperty("otherSubTabBg").objectReferenceValue = othSubBg;
        so.FindProperty("defSubTabText").objectReferenceValue = defSubTxt;
        so.FindProperty("attackSubTabText").objectReferenceValue = atkSubTxt;
        so.FindProperty("otherSubTabText").objectReferenceValue = othSubTxt;
        so.FindProperty("defStatsPanel").objectReferenceValue = defPanelObj;
        so.FindProperty("attackStatsPanel").objectReferenceValue = atkPanelObj;
        so.FindProperty("otherStatsPanel").objectReferenceValue = othPanelObj;
        so.FindProperty("characterAvatarImage").objectReferenceValue = avatarImg;
        so.FindProperty("characterNameText").objectReferenceValue = charNameTxt;
        so.FindProperty("characterLevelExpText").objectReferenceValue = charLvlTxt;
        so.FindProperty("hpValueText").objectReferenceValue = hpVal;
        so.FindProperty("defValueText").objectReferenceValue = defVal;
        so.FindProperty("rangedDefValueText").objectReferenceValue = rangedDefVal;
        so.FindProperty("evasionRateValueText").objectReferenceValue = evasionVal;
        so.FindProperty("kitRecoveryValueText").objectReferenceValue = kitRecovVal;
        so.FindProperty("autoRecoveryValueText").objectReferenceValue = autoRecovVal;
        so.FindProperty("ailmentResistValueText").objectReferenceValue = ailmentVal;
        so.FindProperty("atkValueText").objectReferenceValue = atkVal;
        so.FindProperty("atkSpeedValueText").objectReferenceValue = atkSpdVal;
        so.FindProperty("critAtkValueText").objectReferenceValue = critAtkVal;
        so.FindProperty("critRateValueText").objectReferenceValue = critRateVal;
        so.FindProperty("lifeStealValueText").objectReferenceValue = lifeStealVal;
        so.FindProperty("moveSpeedValueText").objectReferenceValue = moveSpdVal;
        so.FindProperty("obtainedChipsValueText").objectReferenceValue = chipsVal;
        so.FindProperty("chipsetSelectValueText").objectReferenceValue = chipSelVal;
        so.FindProperty("droneAtkValueText").objectReferenceValue = droneAtkVal;
        so.FindProperty("turretAtkValueText").objectReferenceValue = turretAtkVal;
        so.FindProperty("turretDurationValueText").objectReferenceValue = turretDurVal;
        so.FindProperty("resumeButton").objectReferenceValue = resumeBtn;
        so.FindProperty("homeButton").objectReferenceValue = homeBtn;
        so.FindProperty("quitConfirmPanel").objectReferenceValue = confirmModalObj;
        so.FindProperty("confirmNoButton").objectReferenceValue = noBtn;
        so.FindProperty("confirmOkButton").objectReferenceValue = okBtn;
        so.FindProperty("confirmMessageText").objectReferenceValue = confirmMsgTxt;
        so.ApplyModifiedProperties();

        modalRoot.SetActive(false);
        return modalRoot;
    }

    private static TMP_Text CreateStatRow(string name, Transform parent, Vector2 pos, string label, string defaultValue)
    {
        GameObject rowObj = CreateFrame(name, parent, new Color32(16, 32, 42, 255), new Color32(28, 54, 70, 255), out _);
        RectTransform rt = rowObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(480f, 75f);

        TMP_Text lblTxt = CreateText("Label", rowObj.transform, label, 22f, Cream, TextAlignmentOptions.Left);
        lblTxt.fontStyle = FontStyles.Bold;
        lblTxt.rectTransform.anchoredPosition = new Vector2(25f, 0f);
        lblTxt.rectTransform.sizeDelta = new Vector2(300f, 60f);

        TMP_Text valTxt = CreateText("Value", rowObj.transform, defaultValue, 24f, new Color32(245, 195, 75, 255), TextAlignmentOptions.Right);
        valTxt.fontStyle = FontStyles.Bold;
        valTxt.rectTransform.anchoredPosition = new Vector2(-25f, 0f);
        valTxt.rectTransform.sizeDelta = new Vector2(160f, 60f);

        return valTxt;
    }

    private static GameObject CreateFrame(string name, Transform parent, Color fill, Color border, out Image fillImage)
    {
        GameObject borderGo = new GameObject(name, typeof(RectTransform), typeof(Image));
        borderGo.transform.SetParent(parent, false);
        Image borderImg = borderGo.GetComponent<Image>();
        borderImg.color = border;

        GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(borderGo.transform, false);
        fillImage = fillGo.GetComponent<Image>();
        fillImage.color = fill;
        Stretch(fillImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        return borderGo;
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
