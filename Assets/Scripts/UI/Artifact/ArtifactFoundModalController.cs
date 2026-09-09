using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller điều khiển giao diện Popup "Artifact found" xuất hiện khi mở hộp mù trong gameplay.
/// Quản lý việc tạm dừng game (Time.timeScale = 0), kích hoạt hiệu ứng tia sáng Sunburst,
/// hiển thị thông tin thẻ Artifact và xử lý 2 lựa chọn Get / Throw away.
/// </summary>
public class ArtifactFoundModalController : MonoBehaviour
{
    private static ArtifactFoundModalController runtimeInstance;
    private static bool isInitializing = false;

    public static ArtifactFoundModalController Instance
    {
        get
        {
            if (runtimeInstance == null && !isInitializing)
            {
                isInitializing = true;
                try
                {
                    runtimeInstance = FindObjectOfType<ArtifactFoundModalController>(true);
                    if (runtimeInstance == null)
                    {
                        runtimeInstance = EnsureModalInScene();
                    }
                }
                finally
                {
                    isInitializing = false;
                }
            }
            return runtimeInstance;
        }
        private set => runtimeInstance = value;
    }

    [Header("Root & Backdrop")]
    [Tooltip("GameObject gốc của toàn bộ Modal.")]
    [SerializeField] private GameObject modalRoot;

    [Tooltip("Lớp phủ làm tối màn hình phía sau.")]
    [SerializeField] private Image dimBackground;

    [Tooltip("Hiệu ứng các tia sáng xoay tròn phía sau thẻ.")]
    [SerializeField] private SunburstRayEffect sunburstEffect;

    [Header("Card & Info Components")]
    [Tooltip("Text tiêu đề (mặc định: 'Artifact found').")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("Khung viền thẻ chứa icon Artifact.")]
    [SerializeField] private Image badgeFrame;

    [Tooltip("Icon minh họa cho Artifact.")]
    [SerializeField] private Image iconImage;

    [Tooltip("Text nhãn 'ARTIFACT' dưới chân icon.")]
    [SerializeField] private TMP_Text artifactLabelText;

    [Tooltip("Text tên Artifact (màu vàng cam nổi bật).")]
    [SerializeField] private TMP_Text artifactNameText;

    [Tooltip("Text mô tả lore / flavor text.")]
    [SerializeField] private TMP_Text loreText;

    [Tooltip("Text hiển thị chỉ số buff (ví dụ: 'HP +15%').")]
    [SerializeField] private TMP_Text statBuffText;

    [Header("Action Buttons")]
    [Tooltip("Nút vứt bỏ (Throw away).")]
    [SerializeField] private Button throwAwayButton;

    [Tooltip("Nút nhận cổ vật (Get).")]
    [SerializeField] private Button getButton;

    public void ConfigureReferences(
        GameObject root,
        Image dimBg,
        SunburstRayEffect sunburst,
        TMP_Text title,
        Image frame,
        Image icon,
        TMP_Text label,
        TMP_Text nameTxt,
        TMP_Text lore,
        TMP_Text stat,
        Button throwBtn,
        Button getBtn)
    {
        modalRoot = root;
        dimBackground = dimBg;
        sunburstEffect = sunburst;
        titleText = title;
        badgeFrame = frame;
        iconImage = icon;
        artifactLabelText = label;
        artifactNameText = nameTxt;
        loreText = lore;
        statBuffText = stat;
        throwAwayButton = throwBtn;
        getButton = getBtn;
    }

    private float previousTimeScale = 1f;
    private bool ownsTimeScale = false;
    private Action onGetCallback;
    private Action onThrowAwayCallback;
    private ArtifactData currentArtifact;
    private bool isShowing = false;

    private void Awake()
    {
        if (runtimeInstance == null)
        {
            runtimeInstance = this;
        }

        if (modalRoot == null)
        {
            modalRoot = gameObject;
        }

        if (throwAwayButton != null)
        {
            throwAwayButton.onClick.RemoveAllListeners();
            throwAwayButton.onClick.AddListener(OnThrowAwayClicked);
        }

        if (getButton != null)
        {
            getButton.onClick.RemoveAllListeners();
            getButton.onClick.AddListener(OnGetClicked);
        }

        // Chỉ ẩn nếu không phải đang trong quá trình Show()
        if (modalRoot != null && !isShowing)
        {
            modalRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (runtimeInstance == this)
        {
            runtimeInstance = null;
        }

        if (ownsTimeScale)
        {
            Time.timeScale = previousTimeScale;
            ownsTimeScale = false;
        }
    }

    /// <summary>
    /// Hiển thị Popup "Artifact found" với dữ liệu Cổ vật tương ứng.
    /// Tự động pause game trong suốt thời gian hiển thị.
    /// </summary>
    public void Show(ArtifactData artifact, Action onGet, Action onThrowAway)
    {
        if (artifact == null)
        {
            Debug.LogError("[ArtifactFoundModalController] Gọi Show với Artifact null!");
            return;
        }

        isShowing = true;

        if (modalRoot == null)
        {
            modalRoot = gameObject;
        }

        currentArtifact = artifact;
        onGetCallback = onGet;
        onThrowAwayCallback = onThrowAway;

        // 1. Tạm dừng thời gian trong game
        if (!ownsTimeScale)
        {
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            ownsTimeScale = true;
        }
        Time.timeScale = 0f;

        // 2. Điền dữ liệu vào giao diện
        if (titleText != null)
        {
            titleText.text = "Artifact found";
        }

        if (artifactNameText != null)
        {
            artifactNameText.text = artifact.artifactName;
        }

        if (loreText != null)
        {
            loreText.text = artifact.loreDescription;
        }

        if (statBuffText != null)
        {
            statBuffText.text = artifact.GetFormattedStatText();
        }

        if (artifactLabelText != null)
        {
            artifactLabelText.text = "ARTIFACT";
        }

        // Cập nhật Icon (dùng placeholder nếu sprite chưa gán)
        if (iconImage != null)
        {
            if (artifact.icon != null)
            {
                iconImage.sprite = artifact.icon;
                iconImage.color = Color.white;
            }
            else
            {
                EnsurePlaceholderIcon(iconImage, artifact);
            }
            iconImage.preserveAspect = true;
        }

        // Cập nhật màu sắc viền thẻ
        if (badgeFrame != null)
        {
            badgeFrame.color = artifact.badgeBorderColor;
        }

        // Đảm bảo các nút bấm luôn bắt sự kiện
        if (throwAwayButton != null)
        {
            throwAwayButton.onClick.RemoveAllListeners();
            throwAwayButton.onClick.AddListener(OnThrowAwayClicked);
        }

        if (getButton != null)
        {
            getButton.onClick.RemoveAllListeners();
            getButton.onClick.AddListener(OnGetClicked);
        }

        // 3. Kích hoạt Modal
        modalRoot.transform.SetAsLastSibling();
        modalRoot.SetActive(true);

        CanvasGroup cg = modalRoot.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        if (sunburstEffect != null)
        {
            sunburstEffect.EnsureRayGraphic();
        }

        Debug.Log($"[ArtifactFoundModalController] 🎉 ĐÃ MỞ BẢNG CỔ VẬT: {artifact.artifactName} - {artifact.GetFormattedStatText()}");
    }

    /// <summary>
    /// Tạo icon placeholder tạm thời nếu bạn chưa gán sprite chính thức cho Artifact.
    /// </summary>
    private void EnsurePlaceholderIcon(Image img, ArtifactData artifact)
    {
        if (img == null) return;

        Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color fillCol = artifact.statType == ArtifactStatType.MaxHealthPercent ? new Color32(235, 60, 60, 255)
            : (artifact.statType == ArtifactStatType.RangedDefensePercent ? new Color32(90, 180, 230, 255)
            : (artifact.statType == ArtifactStatType.TurretAttackSpeedPercent ? new Color32(240, 180, 30, 255)
            : new Color32(180, 90, 240, 255)));

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                if (x >= 4 && x <= 27 && y >= 4 && y <= 27)
                    tex.SetPixel(x, y, fillCol);
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        img.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        img.color = Color.white;
    }

    /// <summary>
    /// Xử lý khi người chơi bấm nút "Get" (Nhận Artifact).
    /// </summary>
    public void OnGetClicked()
    {
        Hide();
        onGetCallback?.Invoke();
    }

    /// <summary>
    /// Xử lý khi người chơi bấm nút "Throw away" (Bỏ qua Artifact).
    /// </summary>
    public void OnThrowAwayClicked()
    {
        Hide();
        onThrowAwayCallback?.Invoke();
    }

    /// <summary>
    /// Đóng Popup và khôi phục lại Time.timeScale.
    /// </summary>
    public void Hide()
    {
        isShowing = false;
        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }

        if (ownsTimeScale)
        {
            Time.timeScale = previousTimeScale;
            ownsTimeScale = false;
        }
    }

    // =========================================================================
    // RUNTIME BUILDER / FACTORY (Tự động khởi tạo Modal vào Canvas nếu chưa có)
    // =========================================================================

    public static ArtifactFoundModalController EnsureModalInScene(Canvas targetCanvas = null)
    {
        if (runtimeInstance != null) return runtimeInstance;

        ArtifactFoundModalController existing = FindObjectOfType<ArtifactFoundModalController>(true);
        if (existing != null)
        {
            runtimeInstance = existing;
            return existing;
        }

        if (targetCanvas == null)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            foreach (var c in canvases)
            {
                if (c != null && c.isRootCanvas && c.renderMode != RenderMode.WorldSpace)
                {
                    targetCanvas = c;
                    break;
                }
            }
            if (targetCanvas == null)
            {
                targetCanvas = FindObjectOfType<Canvas>();
            }
        }

        if (targetCanvas == null)
        {
            Debug.LogError("[ArtifactFoundModalController] Không tìm thấy Canvas để khởi tạo Modal!");
            return null;
        }

        GameObject prefab = Resources.Load<GameObject>("UI/ArtifactFoundModal");
        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab, targetCanvas.transform, false);
            instance.name = "ArtifactFoundModal";
            ArtifactFoundModalController ctrl = instance.GetComponent<ArtifactFoundModalController>();
            runtimeInstance = ctrl;
            instance.SetActive(false);
            return ctrl;
        }

        return CreateRuntimeModal(targetCanvas.transform as RectTransform);
    }

    public static ArtifactFoundModalController CreateRuntimeModal(RectTransform canvasParent)
    {
        if (canvasParent == null) return null;

        ArtifactFoundModalController existing = canvasParent.GetComponentInChildren<ArtifactFoundModalController>(true);
        if (existing != null)
        {
            runtimeInstance = existing;
            return existing;
        }

        TMP_FontAsset defaultFont = null;
#if UNITY_EDITOR
        defaultFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito/Nunito SDF.asset");
#endif
        if (defaultFont == null)
        {
            defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }
        if (defaultFont == null)
        {
            TMP_Text sampleText = canvasParent.GetComponentInChildren<TMP_Text>(true);
            if (sampleText != null) defaultFont = sampleText.font;
        }
        if (defaultFont == null)
        {
            defaultFont = TMP_Settings.defaultFontAsset;
        }

        GameObject root = new GameObject("ArtifactFoundModal", typeof(RectTransform));
        root.transform.SetParent(canvasParent, false);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        Stretch(rootRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Đảm bảo Modal luôn vẽ ở trên cùng và bắt sự kiện click
        Canvas modalCanvas = root.AddComponent<Canvas>();
        modalCanvas.overrideSorting = true;
        modalCanvas.sortingOrder = 500;
        root.AddComponent<GraphicRaycaster>();

        // 1. Dim Background
        GameObject dimObj = new GameObject("DimBackground", typeof(RectTransform), typeof(Image));
        dimObj.transform.SetParent(root.transform, false);
        RectTransform dimRt = dimObj.GetComponent<RectTransform>();
        Stretch(dimRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image dimImg = dimObj.GetComponent<Image>();
        dimImg.color = new Color32(0, 0, 0, 195);
        dimImg.raycastTarget = true;

        // 2. Sunburst Ray Effect (Behind the card)
        GameObject sunObj = new GameObject("SunburstEffect", typeof(RectTransform), typeof(Image), typeof(SunburstRayEffect));
        sunObj.transform.SetParent(root.transform, false);
        RectTransform sunRt = sunObj.GetComponent<RectTransform>();
        sunRt.anchorMin = new Vector2(0.5f, 0.5f);
        sunRt.anchorMax = new Vector2(0.5f, 0.5f);
        sunRt.pivot = new Vector2(0.5f, 0.5f);
        sunRt.anchoredPosition = new Vector2(0f, 160f);
        sunRt.sizeDelta = new Vector2(850f, 850f);
        SunburstRayEffect sunburst = sunObj.GetComponent<SunburstRayEffect>();
        Image sunImg = sunObj.GetComponent<Image>();
        if (sunImg != null) sunImg.raycastTarget = false;

        // 3. Title Text: "Artifact found"
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(root.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.anchoredPosition = new Vector2(0f, 480f);
        titleRt.sizeDelta = new Vector2(800f, 100f);
        TextMeshProUGUI titleTxt = titleObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) titleTxt.font = defaultFont;
        titleTxt.text = "Artifact found";
        titleTxt.fontSize = 56f;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.color = Color.white;
        titleTxt.raycastTarget = false;

        // 4. Artifact Badge Frame
        GameObject frameObj = new GameObject("BadgeFrame", typeof(RectTransform), typeof(Image));
        frameObj.transform.SetParent(root.transform, false);
        RectTransform frameRt = frameObj.GetComponent<RectTransform>();
        frameRt.anchorMin = new Vector2(0.5f, 0.5f);
        frameRt.anchorMax = new Vector2(0.5f, 0.5f);
        frameRt.pivot = new Vector2(0.5f, 0.5f);
        frameRt.anchoredPosition = new Vector2(0f, 180f);
        frameRt.sizeDelta = new Vector2(250f, 300f);
        Image frameImg = frameObj.GetComponent<Image>();
        frameImg.color = new Color32(46, 229, 240, 255);
        frameImg.raycastTarget = false;

        // Frame inner background
        GameObject frameInner = new GameObject("InnerBg", typeof(RectTransform), typeof(Image));
        frameInner.transform.SetParent(frameObj.transform, false);
        RectTransform innerRt = frameInner.GetComponent<RectTransform>();
        Stretch(innerRt, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));
        Image innerImg = frameInner.GetComponent<Image>();
        innerImg.color = new Color32(11, 45, 60, 255);
        innerImg.raycastTarget = false;

        // Icon Image
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(frameInner.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(0f, 25f);
        iconRt.sizeDelta = new Vector2(150f, 150f);
        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // "ARTIFACT" label tag
        GameObject labelObj = new GameObject("ArtifactTag", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(frameInner.transform, false);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(1f, 0f);
        labelRt.pivot = new Vector2(0.5f, 0f);
        labelRt.anchoredPosition = new Vector2(0f, 18f);
        labelRt.sizeDelta = new Vector2(0f, 40f);
        TextMeshProUGUI labelTxt = labelObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) labelTxt.font = defaultFont;
        labelTxt.text = "ARTIFACT";
        labelTxt.fontSize = 28f;
        labelTxt.fontStyle = FontStyles.Bold;
        labelTxt.alignment = TextAlignmentOptions.Center;
        labelTxt.color = Color.white;
        labelTxt.raycastTarget = false;

        // 5. Artifact Name Text (Golden Yellow)
        GameObject nameObj = new GameObject("ArtifactName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(root.transform, false);
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0.5f, 0.5f);
        nameRt.anchorMax = new Vector2(0.5f, 0.5f);
        nameRt.pivot = new Vector2(0.5f, 0.5f);
        nameRt.anchoredPosition = new Vector2(0f, -40f);
        nameRt.sizeDelta = new Vector2(800f, 65f);
        TextMeshProUGUI nameTxt = nameObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) nameTxt.font = defaultFont;
        nameTxt.text = "Artifact Name";
        nameTxt.fontSize = 44f;
        nameTxt.fontStyle = FontStyles.Bold;
        nameTxt.alignment = TextAlignmentOptions.Center;
        nameTxt.color = new Color32(255, 184, 28, 255); // Golden-Orange
        nameTxt.raycastTarget = false;

        // 6. Lore / Flavor Text
        GameObject loreObj = new GameObject("LoreDescription", typeof(RectTransform), typeof(TextMeshProUGUI));
        loreObj.transform.SetParent(root.transform, false);
        RectTransform loreRt = loreObj.GetComponent<RectTransform>();
        loreRt.anchorMin = new Vector2(0.5f, 0.5f);
        loreRt.anchorMax = new Vector2(0.5f, 0.5f);
        loreRt.pivot = new Vector2(0.5f, 0.5f);
        loreRt.anchoredPosition = new Vector2(0f, -125f);
        loreRt.sizeDelta = new Vector2(750f, 80f);
        TextMeshProUGUI loreTxt = loreObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) loreTxt.font = defaultFont;
        loreTxt.text = "Flavor text description.";
        loreTxt.fontSize = 28f;
        loreTxt.alignment = TextAlignmentOptions.Center;
        loreTxt.color = new Color32(240, 240, 240, 255);
        loreTxt.raycastTarget = false;

        // 7. Stat Buff Text (HP +15%, etc.)
        GameObject statObj = new GameObject("StatBuffText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statObj.transform.SetParent(root.transform, false);
        RectTransform statRt = statObj.GetComponent<RectTransform>();
        statRt.anchorMin = new Vector2(0.5f, 0.5f);
        statRt.anchorMax = new Vector2(0.5f, 0.5f);
        statRt.pivot = new Vector2(0.5f, 0.5f);
        statRt.anchoredPosition = new Vector2(0f, -220f);
        statRt.sizeDelta = new Vector2(750f, 60f);
        TextMeshProUGUI statTxt = statObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) statTxt.font = defaultFont;
        statTxt.text = "HP +15%";
        statTxt.fontSize = 36f;
        statTxt.fontStyle = FontStyles.Bold;
        statTxt.alignment = TextAlignmentOptions.Center;
        statTxt.color = Color.white;
        statTxt.raycastTarget = false;

        // 8. Buttons Container
        GameObject btnContainer = new GameObject("ButtonsContainer", typeof(RectTransform));
        btnContainer.transform.SetParent(root.transform, false);
        RectTransform btnContainerRt = btnContainer.GetComponent<RectTransform>();
        btnContainerRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnContainerRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnContainerRt.pivot = new Vector2(0.5f, 0.5f);
        btnContainerRt.anchoredPosition = new Vector2(0f, -360f);
        btnContainerRt.sizeDelta = new Vector2(700f, 120f);

        // 8a. Throw away Button (Slate Gray)
        GameObject throwBtnObj = CreateButton("ThrowAwayButton", btnContainer.transform, new Vector2(-180f, 0f), new Vector2(300f, 100f), "Throw\naway", 32f, new Color32(78, 105, 125, 255), Color.white, defaultFont, out Button throwBtn);

        // 8b. Get Button (Teal / Cyan)
        GameObject getBtnObj = CreateButton("GetButton", btnContainer.transform, new Vector2(180f, 0f), new Vector2(300f, 100f), "Get", 36f, new Color32(46, 175, 185, 255), Color.white, defaultFont, out Button getBtn);

        // 9. Attach & Configure Controller
        ArtifactFoundModalController ctrl = root.AddComponent<ArtifactFoundModalController>();
        ctrl.modalRoot = root;
        ctrl.dimBackground = dimImg;
        ctrl.sunburstEffect = sunburst;
        ctrl.titleText = titleTxt;
        ctrl.badgeFrame = frameImg;
        ctrl.iconImage = iconImg;
        ctrl.artifactLabelText = labelTxt;
        ctrl.artifactNameText = nameTxt;
        ctrl.loreText = loreTxt;
        ctrl.statBuffText = statTxt;
        ctrl.throwAwayButton = throwBtn;
        ctrl.getButton = getBtn;

        throwBtn.onClick.AddListener(ctrl.OnThrowAwayClicked);
        getBtn.onClick.AddListener(ctrl.OnGetClicked);

        runtimeInstance = ctrl;
        root.SetActive(false);

        return ctrl;
    }

    private static GameObject CreateButton(string name, Transform parent, Vector2 anchoredPos, Vector2 size, string label, float fontSize, Color bgColor, Color textColor, TMP_FontAsset font, out Button button)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        img.color = bgColor;
        button = go.GetComponent<Button>();
        button.targetGraphic = img;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(go.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        Stretch(txtRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textColor;
        tmp.raycastTarget = false;

        return go;
    }

    private static void Stretch(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }
}
