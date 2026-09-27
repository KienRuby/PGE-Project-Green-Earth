using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Điều khiển giao diện popup Daily Gem Mine (chuẩn AAA Mobile theo mẫu Ảnh 2):
/// - Banner Monthly Premium ở trên cùng với nút bấm giá 90.000 đ.
/// - Panel chính Daily Gem Mine với icon thiên thạch đá đỏ, tiêu đề, mô tả.
/// - Đồng hồ đếm ngược reset dạng: Reset in: [09] Hour [26] Min Left (tự động đếm lùi thời gian thực).
/// - Số lượt vào còn lại dạng: Entrance: [5] Left (tự động giảm khi vào trận, khóa nút Start khi về 0).
/// - Lưu trữ tiến độ và lượt chơi bằng PlayerPrefs qua các phiên chơi.
/// </summary>
public class DailyGemMineModalController : MonoBehaviour
{
    private const string PrefsEntrancesKey = "PGE_DailyGemMine_Entrances";
    private const string PrefsLastResetDateKey = "PGE_DailyGemMine_LastResetDate";
    private const string PrefsMonthlyPremiumKey = "PGE_MonthlyPremium_Active";
    private const string PrefsSelectedLevelKey = "PGE_DailyGemMine_SelectedLevel";

    public const int DefaultMaxEntrances = 5;
    public const int PremiumBonusEntrances = 3;

    [Header("Modal Containers")]
    [Tooltip("Root GameObject của modal (bật/tắt khi mở/đóng popup).")]
    [SerializeField] private GameObject modalRoot;

    [Tooltip("Panel nội dung chính để nảy hiệu ứng pop/scale.")]
    [SerializeField] private RectTransform mainPanel;

    [Tooltip("CanvasGroup để điều chỉnh độ mờ khi chuyển cảnh.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Nút nền đen mờ phía sau để bấm ra ngoài thì đóng.")]
    [SerializeField] private Button backdropButton;

    [Tooltip("Nút đóng (X) tùy chọn.")]
    [SerializeField] private Button closeButton;

    [Header("Action Buttons")]
    [Tooltip("Nút mua gói Monthly Premium (90.000 đ).")]
    [SerializeField] private Button monthlyPremiumButton;

    [Tooltip("Nút Start màu hồng ở Card Level 1 (dùng cho backward compatibility).")]
    [SerializeField] private Button startLevel1Button;

    [Tooltip("Nút / Card Level 2 (tùy chọn).")]
    [SerializeField] private Button startLevel2Button;

    [Header("Scrollable Level Cards (5 Levels)")]
    [Tooltip("ScrollRect cuộn danh sách 5 Level.")]
    [SerializeField] private ScrollRect levelsScrollRect;

    [Tooltip("Danh sách 5 Level Card trong ScrollView.")]
    [SerializeField] private DailyGemMineLevelCard[] levelCards;

    [Header("Visual Asset Templates (Tự động tải nếu thiếu)")]
    [SerializeField] private Sprite level1PreviewSprite;
    [SerializeField] private Sprite level2PreviewSprite;
    [SerializeField] private Sprite pinkStartSprite;
    [SerializeField] private Sprite pinkStartPressedSprite;
    [SerializeField] private Sprite redGemIconSprite;

    [Header("Status Texts")]
    [Tooltip("Text hiển thị đồng hồ Reset in: 09 Hour 26 Min Left.")]
    [SerializeField] private TMP_Text resetTimerText;

    [Tooltip("Text hiển thị số lượt vào Entrance: 5 Left.")]
    [SerializeField] private TMP_Text entranceCountText;

    [Header("Scene Transition")]
    [Tooltip("Tên Scene sẽ chuyển sang khi nhấn nút Start (mặc định: GenMine).")]
    [SerializeField] private string gemMineSceneName = "GenMine";

    [Header("Runtime State")]
    [SerializeField] private int remainingHours = 9;
    [SerializeField] private int remainingMinutes = 26;
    [SerializeField] private int remainingSeconds = 0;
    [SerializeField] private int remainingEntrances = 5;
    [SerializeField] private int maxEntrances = DefaultMaxEntrances;
    [SerializeField] private bool useManualTime = false;

    private float nextTimerUpdate = 0f;

    public event Action<int> OnLevelStarted;
    public event Action OnMonthlyPremiumPurchased;
    public event Action OnModalOpened;
    public event Action OnModalClosed;

    public bool IsOpen => modalRoot != null ? modalRoot.activeSelf : gameObject.activeSelf;
    public string GemMineSceneName
    {
        get => gemMineSceneName;
        set => gemMineSceneName = value;
    }
    public int RemainingHours => remainingHours;
    public int RemainingMinutes => remainingMinutes;
    public int RemainingSeconds => remainingSeconds;
    public int RemainingEntrances => remainingEntrances;
    public int MaxEntrances => maxEntrances;
    public Button StartLevel1Button => startLevel1Button;
    public Button MonthlyPremiumButton => monthlyPremiumButton;
    public Button CloseButton => closeButton;
    public ScrollRect LevelsScrollRect => levelsScrollRect;
    public DailyGemMineLevelCard[] LevelCards => levelCards;
    public CanvasGroup CanvasGroup => canvasGroup;
    public int SelectedLevel
    {
        get => DailyGemMineProgress.SelectedLevel;
        set => DailyGemMineProgress.SelectedLevel = value;
    }

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        SanitizeMaterialsAndEffects();

        if (backdropButton != null)
        {
            backdropButton.onClick.RemoveListener(CloseModal);
            backdropButton.onClick.AddListener(CloseModal);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseModal);
            closeButton.onClick.AddListener(CloseModal);
        }

        if (monthlyPremiumButton != null)
        {
            monthlyPremiumButton.onClick.RemoveListener(OnMonthlyPremiumClicked);
            monthlyPremiumButton.onClick.AddListener(OnMonthlyPremiumClicked);
        }

        if (startLevel1Button != null)
        {
            startLevel1Button.onClick.RemoveListener(OnStartLevel1Clicked);
            startLevel1Button.onClick.AddListener(OnStartLevel1Clicked);
        }

        if (startLevel2Button != null)
        {
            startLevel2Button.onClick.RemoveListener(OnStartLevel2Clicked);
            startLevel2Button.onClick.AddListener(OnStartLevel2Clicked);
        }

        EnsureLevelsScrollView();
        RegisterLevelCardsEvents();
        LoadSavedData();
    }

    /// <summary>
    /// Đảm bảo hệ thống ScrollView và 5 Level Cards luôn được khởi tạo và kết nối chuẩn xác.
    /// Tự động tái tạo ScrollView nếu chưa có trong scene (tương tự EnsureTowerDefModal).
    /// </summary>
    public void EnsureLevelsScrollView()
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer == -1) uiLayer = 5;

        if (levelsScrollRect == null)
        {
            levelsScrollRect = GetComponentInChildren<ScrollRect>(true);
        }

        // Tìm panel chứa ScrollView
        RectTransform panelTr = null;
        DailyGemMineLayoutTuner tuner = GetComponent<DailyGemMineLayoutTuner>();
        if (tuner != null && tuner.DailyGemMinePanel != null)
        {
            panelTr = tuner.DailyGemMinePanel;
        }
        else
        {
            panelTr = transform.Find("ContentRoot/DailyGemMinePanel") as RectTransform
                ?? transform.Find("DailyGemMinePanel") as RectTransform;
        }

        if (panelTr == null && mainPanel != null)
        {
            Transform found = mainPanel.Find("DailyGemMinePanel");
            if (found != null) panelTr = found as RectTransform;
            else if (mainPanel.name == "DailyGemMinePanel") panelTr = mainPanel;
        }

        // Tự động khởi tạo nếu chưa có ScrollRect trong Panel
        if (levelsScrollRect == null && panelTr != null)
        {
            Transform legacyCard1 = panelTr.Find("Card_Level_01");
            Transform legacyCard2 = panelTr.Find("Card_Level_02");

            EnsureVisualTemplates(legacyCard1, legacyCard2);

            // Xóa các thẻ tĩnh cũ nếu còn sót lại ở ngoài Panel
            if (legacyCard1 != null && legacyCard1.parent == panelTr)
            {
                if (Application.isPlaying) Destroy(legacyCard1.gameObject);
                else DestroyImmediate(legacyCard1.gameObject);
            }
            if (legacyCard2 != null && legacyCard2.parent == panelTr)
            {
                if (Application.isPlaying) Destroy(legacyCard2.gameObject);
                else DestroyImmediate(legacyCard2.gameObject);
            }

            // 1. LevelsScrollView
            GameObject scrollObj = new GameObject("LevelsScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollObj.layer = uiLayer;
            scrollObj.transform.SetParent(panelTr, false);
            RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.5f, 1f);
            scrollRt.anchorMax = new Vector2(0.5f, 1f);
            scrollRt.pivot = new Vector2(0.5f, 1f);
            scrollRt.anchoredPosition = tuner != null ? tuner.scrollViewAnchoredPosition : new Vector2(0f, -325f);
            scrollRt.sizeDelta = tuner != null ? tuner.scrollViewSizeDelta : new Vector2(720f, 850f);

            levelsScrollRect = scrollObj.GetComponent<ScrollRect>();

            // 2. Viewport có Image bắt trọn raycast để vuốt lướt và RectMask2D ẩn phần tràn (không phụ thuộc alpha stencil)
            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportObj.layer = uiLayer;
            viewportObj.transform.SetParent(scrollRt, false);
            RectTransform vpRt = viewportObj.GetComponent<RectTransform>();
            StretchRect(vpRt);

            Image vpImg = viewportObj.GetComponent<Image>();
            vpImg.color = Color.clear;
            vpImg.raycastTarget = true;

            // 3. Content với VerticalLayoutGroup và ContentSizeFitter
            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.layer = uiLayer;
            contentObj.transform.SetParent(vpRt, false);
            RectTransform contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;

            VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 30);
            vlg.spacing = tuner != null ? tuner.cardSpacing : 20f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = contentObj.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            levelsScrollRect.viewport = vpRt;
            levelsScrollRect.content = contentRt;

            // 4. Tạo 5 thẻ cấp độ
            CreateDynamicLevelCards(contentRt);

            SetLayerRecursively(scrollObj, uiLayer);
        }

        if (levelsScrollRect != null)
        {
            RectTransform scrollRt = levelsScrollRect.GetComponent<RectTransform>();
            if (scrollRt != null)
            {
                scrollRt.anchorMin = new Vector2(0.5f, 1f);
                scrollRt.anchorMax = new Vector2(0.5f, 1f);
                scrollRt.pivot = new Vector2(0.5f, 1f);
                scrollRt.anchoredPosition = tuner != null ? tuner.scrollViewAnchoredPosition : new Vector2(0f, -325f);
                scrollRt.sizeDelta = tuner != null ? tuner.scrollViewSizeDelta : new Vector2(720f, 850f);
            }

            levelsScrollRect.horizontal = false;
            levelsScrollRect.vertical = true;
            levelsScrollRect.movementType = ScrollRect.MovementType.Clamped;
            levelsScrollRect.inertia = true;
            levelsScrollRect.decelerationRate = 0.135f;
            levelsScrollRect.scrollSensitivity = 40f;

            // Đảm bảo Viewport luôn có RectMask2D (gỡ bỏ Mask lỗi stencil alpha) và Image raycastTarget = true
            if (levelsScrollRect.viewport != null)
            {
                Mask legacyMask = levelsScrollRect.viewport.GetComponent<Mask>();
                if (legacyMask != null)
                {
                    if (Application.isPlaying) Destroy(legacyMask);
                    else DestroyImmediate(legacyMask);
                }

                RectMask2D rectMask = levelsScrollRect.viewport.GetComponent<RectMask2D>();
                if (rectMask == null)
                {
                    rectMask = levelsScrollRect.viewport.gameObject.AddComponent<RectMask2D>();
                }

                Image vpImage = levelsScrollRect.viewport.GetComponent<Image>();
                if (vpImage == null)
                {
                    vpImage = levelsScrollRect.viewport.gameObject.AddComponent<Image>();
                }
                vpImage.color = Color.clear;
                vpImage.raycastTarget = true;
            }

            EnsureVisualTemplates();

            // Đảm bảo Content có đủ thẻ màn và cập nhật sprite mới nhất
            if (levelsScrollRect.content != null)
            {
                if (levelsScrollRect.content.childCount == 0 || levelCards == null || levelCards.Length == 0)
                {
                    CreateDynamicLevelCards(levelsScrollRect.content);
                }
                else
                {
                    // Cập nhật lại sprite sạch, icon chuẩn, loại bỏ chữ Start thừa và đảm bảo huy hiệu Locked cho các thẻ đã sinh
                    for (int i = 0; i < levelsScrollRect.content.childCount; i++)
                    {
                        Transform child = levelsScrollRect.content.GetChild(i);
                        int level = i + 1;
                        Sprite preview = (level % 2 == 1) ? level1PreviewSprite : level2PreviewSprite;

                        Image bg = child.GetComponent<Image>();
                        if (bg != null && preview != null) bg.sprite = preview;

                        DailyGemMineLevelCard cardCtrl = child.GetComponent<DailyGemMineLevelCard>();
                        if (cardCtrl != null)
                        {
                            cardCtrl.RemoveRedundantStartLabel();
                            cardCtrl.EnsureLockBadge();
                        }

                        Transform hb = child.Find("HeaderBanner");
                        if (hb != null)
                        {
                            Transform gem = hb.Find("GemIcon");
                            if (gem != null)
                            {
                                Image gemImg = gem.GetComponent<Image>();
                                if (gemImg != null && redGemIconSprite != null) gemImg.sprite = redGemIconSprite;
                            }
                        }
                    }
                }
            }

            SetLayerRecursively(levelsScrollRect.gameObject, uiLayer);
        }

        var foundCards = GetComponentsInChildren<DailyGemMineLevelCard>(true);
        if (foundCards != null && foundCards.Length > 0)
        {
            levelCards = foundCards;
            foreach (var card in levelCards)
            {
                if (card != null)
                {
                    card.RemoveRedundantStartLabel();
                    card.EnsureLockBadge();
                    card.OptimizeRaycastTargetsForSwiping();
                }
            }
        }

        RegisterLevelCardsEvents();
        RefreshLevelCardsState();
    }

    private void EnsureVisualTemplates(Transform c1 = null, Transform c2 = null)
    {
#if UNITY_EDITOR
        level1PreviewSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/GemMine/preview_level_01.png");
        level2PreviewSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/GemMine/preview_level_02.png");
        pinkStartSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/GemMine/btn_pink_start.png");
        pinkStartPressedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/GemMine/btn_pink_start_pressed.png");
        redGemIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/GemMine/icon_red_gem.png");
#endif
        if (level1PreviewSprite == null && c1 != null)
        {
            Image img = c1.GetComponent<Image>();
            if (img != null && img.sprite != null) level1PreviewSprite = img.sprite;
        }
        if (level2PreviewSprite == null && c2 != null)
        {
            Image img = c2.GetComponent<Image>();
            if (img != null && img.sprite != null) level2PreviewSprite = img.sprite;
        }
        if (pinkStartSprite == null && c1 != null)
        {
            Transform sb = c1.Find("StartButton");
            if (sb != null)
            {
                Image img = sb.GetComponent<Image>();
                if (img != null && img.sprite != null) pinkStartSprite = img.sprite;
            }
        }
        if (redGemIconSprite == null && c1 != null)
        {
            Transform gi = c1.Find("HeaderBanner/GemIcon");
            if (gi != null)
            {
                Image img = gi.GetComponent<Image>();
                if (img != null && img.sprite != null) redGemIconSprite = img.sprite;
            }
        }
        if (pinkStartSprite == null && startLevel1Button != null && startLevel1Button.targetGraphic is Image startImg)
        {
            pinkStartSprite = startImg.sprite;
        }
    }

    private static void StretchRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        for (int i = 0; i < go.transform.childCount; i++)
        {
            SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
        }
    }

    private void CreateDynamicLevelCards(RectTransform content)
    {
        DailyGemMineLevelCard[] cards = new DailyGemMineLevelCard[5];

        string[] rewards = new string[5];
        for (int r = 0; r < 5; r++)
        {
            rewards[r] = DailyGemMineProgress.GetRewardRangeString(r + 1);
        }
        Color[] tints = {
            Color.white,
            Color.white,
            new Color32(215, 235, 255, 255),
            new Color32(255, 235, 205, 255),
            new Color32(235, 215, 255, 255)
        };

        for (int i = 0; i < 5; i++)
        {
            int level = i + 1;
            Sprite preview = (level % 2 == 1) ? level1PreviewSprite : level2PreviewSprite;
            cards[i] = CreateDynamicCard(content, level, $"Gem Mine LV.{level:D2}", rewards[i], preview, tints[i]);
        }

        levelCards = cards;
        if (cards[0] != null && cards[0].StartButton != null)
        {
            startLevel1Button = cards[0].StartButton;
        }
    }

    private DailyGemMineLevelCard CreateDynamicCard(
        Transform parent,
        int level,
        string title,
        string reward,
        Sprite previewSprite,
        Color tintColor)
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer == -1) uiLayer = 5;

        GameObject cardObj = new GameObject($"Card_Level_{level:D2}", typeof(RectTransform));
        cardObj.layer = uiLayer;
        cardObj.transform.SetParent(parent, false);
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(700f, 350f);
        cardRect.localScale = Vector3.one;
        cardRect.localPosition = Vector3.zero;
        cardRect.localRotation = Quaternion.identity;

        LayoutElement le = cardObj.AddComponent<LayoutElement>();
        le.preferredWidth = 700f;
        le.preferredHeight = 350f;
        le.minHeight = 350f;

        // Ảnh nền xem trước: raycastTarget = false để cử chỉ lướt truyền thẳng cho ScrollRect
        Image bgImg = cardObj.AddComponent<Image>();
        if (previewSprite != null) bgImg.sprite = previewSprite;
        bgImg.color = tintColor != default ? tintColor : Color.white;
        bgImg.raycastTarget = false;

        Shadow cardShadow = cardObj.AddComponent<Shadow>();
        cardShadow.effectColor = new Color32(0, 14, 24, 200);
        cardShadow.effectDistance = new Vector2(4f, -5f);

        // Header Banner
        GameObject headerObj = new GameObject("HeaderBanner", typeof(RectTransform), typeof(Image));
        headerObj.layer = uiLayer;
        headerObj.transform.SetParent(cardObj.transform, false);
        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0f, 70f);
        headerRect.localScale = Vector3.one;

        Image headerImg = headerObj.GetComponent<Image>();
        headerImg.color = new Color32(0, 0, 0, 180);
        headerImg.raycastTarget = false;

        TMP_FontAsset font = (resetTimerText != null && resetTimerText.font != null)
            ? resetTimerText.font
            : (entranceCountText != null && entranceCountText.font != null)
                ? entranceCountText.font
                : TMP_Settings.defaultFontAsset;

        // Title Text
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.layer = uiLayer;
        titleObj.transform.SetParent(headerObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(0f, 0.5f);
        titleRect.pivot = new Vector2(0f, 0.5f);
        titleRect.anchoredPosition = new Vector2(25f, 0f);
        titleRect.sizeDelta = new Vector2(320f, 50f);
        titleRect.localScale = Vector3.one;

        TextMeshProUGUI titleTxt = titleObj.GetComponent<TextMeshProUGUI>();
        titleTxt.text = title;
        titleTxt.fontSize = 36f;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color = Color.white;
        titleTxt.alignment = TextAlignmentOptions.Left;
        titleTxt.enableWordWrapping = false;
        titleTxt.raycastTarget = false;
        if (font != null) titleTxt.font = font;

        // Gem Icon
        GameObject gemIconObj = new GameObject("GemIcon", typeof(RectTransform), typeof(Image));
        gemIconObj.layer = uiLayer;
        gemIconObj.transform.SetParent(headerObj.transform, false);
        RectTransform gemRect = gemIconObj.GetComponent<RectTransform>();
        gemRect.anchorMin = new Vector2(1f, 0.5f);
        gemRect.anchorMax = new Vector2(1f, 0.5f);
        gemRect.pivot = new Vector2(1f, 0.5f);
        gemRect.anchoredPosition = new Vector2(-175f, 0f);
        gemRect.sizeDelta = new Vector2(34f, 44f);
        gemRect.localScale = Vector3.one;

        Image gemImg = gemIconObj.GetComponent<Image>();
        if (redGemIconSprite != null) gemImg.sprite = redGemIconSprite;
        gemImg.preserveAspect = true;
        gemImg.raycastTarget = false;

        // Reward Text
        GameObject rwdObj = new GameObject("RewardText", typeof(RectTransform), typeof(TextMeshProUGUI));
        rwdObj.layer = uiLayer;
        rwdObj.transform.SetParent(headerObj.transform, false);
        RectTransform rwdRect = rwdObj.GetComponent<RectTransform>();
        rwdRect.anchorMin = new Vector2(1f, 0.5f);
        rwdRect.anchorMax = new Vector2(1f, 0.5f);
        rwdRect.pivot = new Vector2(0f, 0.5f);
        rwdRect.anchoredPosition = new Vector2(-165f, 0f);
        rwdRect.sizeDelta = new Vector2(150f, 50f);
        rwdRect.localScale = Vector3.one;

        TextMeshProUGUI rwdTxt = rwdObj.GetComponent<TextMeshProUGUI>();
        rwdTxt.text = reward;
        rwdTxt.fontSize = 32f;
        rwdTxt.fontStyle = FontStyles.Bold;
        rwdTxt.color = Color.white;
        rwdTxt.alignment = TextAlignmentOptions.MidlineLeft;
        rwdTxt.enableWordWrapping = false;
        rwdTxt.overflowMode = TextOverflowModes.Overflow;
        rwdTxt.raycastTarget = false;
        if (font != null) rwdTxt.font = font;

        // Pink Start Button
        GameObject startBtnObj = new GameObject("StartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        startBtnObj.layer = uiLayer;
        startBtnObj.transform.SetParent(cardObj.transform, false);
        RectTransform startRect = startBtnObj.GetComponent<RectTransform>();
        startRect.anchorMin = new Vector2(1f, 0f);
        startRect.anchorMax = new Vector2(1f, 0f);
        startRect.pivot = new Vector2(1f, 0f);
        startRect.anchoredPosition = new Vector2(-20f, 18f);
        startRect.sizeDelta = new Vector2(190f, 72f);
        startRect.localScale = Vector3.one;

        Image startImg = startBtnObj.GetComponent<Image>();
        if (pinkStartSprite != null) startImg.sprite = pinkStartSprite;
        startImg.color = Color.white;
        startImg.raycastTarget = true;

        Button startBtn = startBtnObj.GetComponent<Button>();
        startBtn.targetGraphic = startImg;
        if (pinkStartPressedSprite != null)
        {
            startBtn.transition = Selectable.Transition.SpriteSwap;
            SpriteState ss = startBtn.spriteState;
            ss.pressedSprite = pinkStartPressedSprite;
            startBtn.spriteState = ss;
        }
        startBtn.onClick.AddListener(() => StartGemMineLevel(level));

        // Huy hiệu LOCKED hiển thị khi màn chơi bị khóa (chặn tia raycast, không đè text Start)
        GameObject lockBadgeObj = new GameObject("LockedBadge", typeof(RectTransform), typeof(Image), typeof(Button));
        lockBadgeObj.layer = uiLayer;
        lockBadgeObj.transform.SetParent(cardObj.transform, false);
        RectTransform lockRect = lockBadgeObj.GetComponent<RectTransform>();
        lockRect.anchorMin = new Vector2(1f, 0f);
        lockRect.anchorMax = new Vector2(1f, 0f);
        lockRect.pivot = new Vector2(1f, 0f);
        lockRect.anchoredPosition = new Vector2(-20f, 18f);
        lockRect.sizeDelta = new Vector2(190f, 72f);
        lockRect.localScale = Vector3.one;

        Image lockImg = lockBadgeObj.GetComponent<Image>();
        lockImg.color = new Color(0.12f, 0.12f, 0.16f, 0.92f);
        lockImg.raycastTarget = true;

        Button lockBtn = lockBadgeObj.GetComponent<Button>();
        if (lockBtn != null)
        {
            lockBtn.targetGraphic = lockImg;
            lockBtn.transition = Selectable.Transition.None;
            int lockedLvl = level;
            lockBtn.onClick.AddListener(() =>
            {
                Debug.Log($"[DailyGemMine] Màn chơi LV.{lockedLvl:D2} đang bị khóa. Hãy hoàn thành màn trước đó.");
            });
        }

        GameObject lockTextObj = new GameObject("LockedText", typeof(RectTransform), typeof(TextMeshProUGUI));
        lockTextObj.layer = uiLayer;
        lockTextObj.transform.SetParent(lockBadgeObj.transform, false);
        RectTransform lockTextRect = lockTextObj.GetComponent<RectTransform>();
        StretchRect(lockTextRect);

        TextMeshProUGUI lockTxt = lockTextObj.GetComponent<TextMeshProUGUI>();
        lockTxt.text = "LOCKED";
        lockTxt.fontSize = 30f;
        lockTxt.fontStyle = FontStyles.Bold;
        lockTxt.color = new Color32(200, 200, 200, 255);
        lockTxt.alignment = TextAlignmentOptions.Center;
        lockTxt.raycastTarget = false;
        if (font != null) lockTxt.font = font;

        lockBadgeObj.SetActive(false);

        DailyGemMineLevelCard cardCtrl = cardObj.AddComponent<DailyGemMineLevelCard>();
        cardCtrl.SetLockOverlay(lockBadgeObj, lockTxt);
        bool isUnlocked = DailyGemMineProgress.IsLevelUnlocked(level);
        cardCtrl.Setup(level, title, reward, previewSprite, !isUnlocked, isUnlocked ? "" : $"Clear LV.{level - 1:D2} to Unlock");
        cardCtrl.OptimizeRaycastTargetsForSwiping();

        return cardCtrl;
    }

    public void ScrollToLevel(int level)
    {
        if (levelsScrollRect == null) return;
        int total = levelCards != null && levelCards.Length > 0 ? levelCards.Length : 5;
        float targetNorm = total > 1 ? 1f - (float)Mathf.Clamp(level - 1, 0, total - 1) / (total - 1) : 1f;
        levelsScrollRect.verticalNormalizedPosition = targetNorm;
        levelsScrollRect.velocity = Vector2.zero;
    }

    private void RegisterLevelCardsEvents()
    {
        if (levelCards == null || levelCards.Length == 0)
        {
            levelCards = GetComponentsInChildren<DailyGemMineLevelCard>(true);
        }

        if (levelCards != null)
        {
            foreach (var card in levelCards)
            {
                if (card == null) continue;
                card.OnStartClicked -= StartGemMineLevel;
                card.OnStartClicked += StartGemMineLevel;
            }
        }
    }

    private void OnEnable()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        SanitizeMaterialsAndEffects();
        EnsureLevelsScrollView();
        RegisterLevelCardsEvents();
        CheckDailyReset();
        UpdateCountdownTime();
        RefreshStatusTexts();
    }

    private void Start()
    {
        SanitizeMaterialsAndEffects();
        LoadSavedData();
        RefreshStatusTexts();
    }

    private void Update()
    {
        if (useManualTime) return;

        if (Time.unscaledTime >= nextTimerUpdate)
        {
            nextTimerUpdate = Time.unscaledTime + 1f;
            UpdateCountdownTime();
            RefreshStatusTexts();
        }
    }

    /// <summary>
    /// Đọc dữ liệu lượt chơi và ngày reset đã lưu trong PlayerPrefs.
    /// </summary>
    public void LoadSavedData()
    {
        bool isPremium = PlayerPrefs.GetInt(PrefsMonthlyPremiumKey, 0) == 1;
        maxEntrances = DefaultMaxEntrances + (isPremium ? PremiumBonusEntrances : 0);

        string todayStr = DateTime.Now.ToString("yyyyMMdd");
        string savedDate = PlayerPrefs.GetString(PrefsLastResetDateKey, string.Empty);

        if (string.IsNullOrEmpty(savedDate) || savedDate != todayStr)
        {
            // Sang ngày mới -> Khôi phục đủ số lượt
            remainingEntrances = maxEntrances;
            PlayerPrefs.SetString(PrefsLastResetDateKey, todayStr);
            PlayerPrefs.SetInt(PrefsEntrancesKey, remainingEntrances);
            PlayerPrefs.Save();
        }
        else
        {
            remainingEntrances = PlayerPrefs.GetInt(PrefsEntrancesKey, maxEntrances);
            remainingEntrances = Mathf.Clamp(remainingEntrances, 0, maxEntrances);
        }

        UpdateCountdownTime();
    }

    /// <summary>
    /// Kiểm tra nếu qua 00:00:00 ngày mới thì tự động reset lại 5 lượt.
    /// </summary>
    public void CheckDailyReset()
    {
        string todayStr = DateTime.Now.ToString("yyyyMMdd");
        string savedDate = PlayerPrefs.GetString(PrefsLastResetDateKey, string.Empty);

        if (savedDate != todayStr)
        {
            remainingEntrances = maxEntrances;
            PlayerPrefs.SetString(PrefsLastResetDateKey, todayStr);
            PlayerPrefs.SetInt(PrefsEntrancesKey, remainingEntrances);
            PlayerPrefs.Save();
            Debug.Log($"[DailyGemMine] Đã qua ngày mới ({todayStr}). Tự động reset lại {remainingEntrances} lượt tham gia.");
        }
    }

    /// <summary>
    /// Tính toán thời gian còn lại đến 00:00:00 ngày tiếp theo.
    /// </summary>
    private void UpdateCountdownTime()
    {
        if (useManualTime) return;

        DateTime now = DateTime.Now;
        DateTime nextMidnight = now.Date.AddDays(1);
        TimeSpan diff = nextMidnight - now;

        if (diff.TotalSeconds <= 0)
        {
            CheckDailyReset();
            nextMidnight = DateTime.Now.Date.AddDays(1);
            diff = nextMidnight - DateTime.Now;
        }

        remainingHours = Mathf.Max(0, (int)diff.TotalHours);
        remainingMinutes = Mathf.Clamp(diff.Minutes, 0, 59);
        remainingSeconds = Mathf.Clamp(diff.Seconds, 0, 59);
    }

    /// <summary>
    /// Loại bỏ triệt để các component Dissolve hoặc Material Dissolve shader vô tình gắn vào,
    /// bảo đảm UI hiển thị sắc nét 100% không bị noise hay vỡ hạt.
    /// </summary>
    public void SanitizeMaterialsAndEffects()
    {
        // 1. Gỡ bỏ UIDissolveController / UIDissolveGroup nếu tồn tại
        MonoBehaviour[] allBehaviours = GetComponentsInChildren<MonoBehaviour>(true);
        if (allBehaviours != null)
        {
            foreach (MonoBehaviour mb in allBehaviours)
            {
                if (mb == null) continue;
                string typeName = mb.GetType().Name;
                if (typeName.IndexOf("UIDissolve", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(mb);
                    }
                    else
                    {
                        DestroyImmediate(mb);
                    }
                }
            }
        }

        // 2. Khôi phục toàn bộ Material của Graphic về mặc định (null) nếu đang dùng shader Dissolve
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        if (graphics != null)
        {
            foreach (Graphic g in graphics)
            {
                if (g == null) continue;
                if (g.material != null && g.material.shader != null &&
                    g.material.shader.name.IndexOf("Dissolve", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    g.material = null;
                }
            }
        }

        // 3. Khôi phục Material của TMP_Text về fontSharedMaterial gốc nếu đang dùng shader Dissolve
        TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
        if (tmps != null)
        {
            foreach (TMP_Text t in tmps)
            {
                if (t == null) continue;
                if (t.fontSharedMaterial != null && t.fontSharedMaterial.shader != null &&
                    t.fontSharedMaterial.shader.name.IndexOf("Dissolve", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (t.font != null && t.font.material != null)
                    {
                        t.fontSharedMaterial = t.font.material;
                    }
                }
            }
        }

        // 4. Xóa sạch các GameObject thừa từ phiên bản cũ nếu còn sót lại trong Scene (nút X đỏ, v.v.)
        Transform legacyClose = transform.Find("ContentRoot/CloseButton");
        if (legacyClose != null)
        {
            if (Application.isPlaying) Destroy(legacyClose.gameObject);
            else DestroyImmediate(legacyClose.gameObject);
        }

        Transform legacyPriceLabel = transform.Find("ContentRoot/MonthlyPremiumBanner/PriceButton/PriceLabel");
        if (legacyPriceLabel != null)
        {
            if (Application.isPlaying) Destroy(legacyPriceLabel.gameObject);
            else DestroyImmediate(legacyPriceLabel.gameObject);
        }
    }

    /// <summary>
    /// Mở giao diện Daily Gem Mine theo đúng mẫu ảnh 2.
    /// </summary>
    public void OpenModal()
    {
        transform.SetAsLastSibling();
        SanitizeMaterialsAndEffects();

        EnsureLevelsScrollView();

        DailyGemMineLayoutTuner tuner = GetComponent<DailyGemMineLayoutTuner>();
        if (tuner != null)
        {
            tuner.ApplyLayout();
        }

        CheckDailyReset();
        UpdateCountdownTime();

        if (modalRoot != null)
        {
            modalRoot.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        RefreshStatusTexts();

        Canvas.ForceUpdateCanvases();
        if (levelsScrollRect != null)
        {
            levelsScrollRect.verticalNormalizedPosition = 1f;
            levelsScrollRect.velocity = Vector2.zero;
        }

        // Hiệu ứng Pop nảy nhẹ nếu có UIPanelTransition
        UIPanelTransition transition = GetComponent<UIPanelTransition>()
            ?? (mainPanel != null ? mainPanel.GetComponent<UIPanelTransition>() : null);
        if (transition != null)
        {
            transition.PlayPopShow();
        }

        OnModalOpened?.Invoke();
        Debug.Log($"[DailyGemMineModalController] Mở giao diện Daily Gem Mine. Số lượt: {remainingEntrances}/{maxEntrances}.");
    }

    /// <summary>
    /// Đóng giao diện Daily Gem Mine và trở lại màn Chapter.
    /// </summary>
    public void CloseModal()
    {
        UIPanelTransition transition = GetComponent<UIPanelTransition>()
            ?? (mainPanel != null ? mainPanel.GetComponent<UIPanelTransition>() : null);

        if (transition != null)
        {
            transition.PlayPopHide(() =>
            {
                if (modalRoot != null) modalRoot.SetActive(false);
                else gameObject.SetActive(false);
            });
        }
        else
        {
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        OnModalClosed?.Invoke();
        Debug.Log("[DailyGemMineModalController] Đóng giao diện Daily Gem Mine.");
    }

    public void OnMonthlyPremiumClicked()
    {
        Debug.Log("[DailyGemMineModalController] Người chơi kích hoạt gói Monthly Premium (+3 Gem Mine entrances, +1 Survival, +500 gems).");
        PlayerPrefs.SetInt(PrefsMonthlyPremiumKey, 1);
        maxEntrances = DefaultMaxEntrances + PremiumBonusEntrances;
        remainingEntrances = Mathf.Min(maxEntrances, remainingEntrances + PremiumBonusEntrances);
        PlayerPrefs.SetInt(PrefsEntrancesKey, remainingEntrances);
        PlayerPrefs.Save();

        RefreshStatusTexts();
        OnMonthlyPremiumPurchased?.Invoke();
    }

    public void OnStartLevel1Clicked()
    {
        StartGemMineLevel(1);
    }

    public void OnStartLevel2Clicked()
    {
        StartGemMineLevel(2);
    }

    private void StartGemMineLevel(int level)
    {
        if (!DailyGemMineProgress.IsLevelUnlocked(level))
        {
            Debug.LogWarning($"[DailyGemMineModalController] Màn {level} đang bị khóa. Hãy hoàn thành màn {level - 1} trước!");
            return;
        }

        if (remainingEntrances <= 0)
        {
            Debug.LogWarning("[DailyGemMineModalController] Không thể bắt đầu: Đã hết lượt tham gia Daily Gem Mine hôm nay (Entrance: 0).");
            RefreshStatusTexts();
            return;
        }

        SelectedLevel = level;

        // Giảm 1 lượt vào trận và lưu vào PlayerPrefs
        remainingEntrances = Mathf.Max(0, remainingEntrances - 1);
        PlayerPrefs.SetInt(PrefsEntrancesKey, remainingEntrances);
        PlayerPrefs.Save();

        RefreshStatusTexts();

        Debug.Log($"[DailyGemMineModalController] Bắt đầu màn Gem Mine Cấp {level}. Lượt còn lại: {remainingEntrances}. Chuyển sang Scene: {gemMineSceneName}.");
        OnLevelStarted?.Invoke(level);

        if (Application.isPlaying && !string.IsNullOrEmpty(gemMineSceneName))
        {
            if (Application.CanStreamedLevelBeLoaded(gemMineSceneName))
            {
                SceneManager.LoadScene(gemMineSceneName);
            }
            else if (Application.CanStreamedLevelBeLoaded("GenMine"))
            {
                SceneManager.LoadScene("GenMine");
            }
            else if (Application.CanStreamedLevelBeLoaded("goalkeeper"))
            {
                SceneManager.LoadScene("goalkeeper");
            }
            else
            {
                SceneManager.LoadScene(gemMineSceneName);
            }
        }
    }

    public void SetGemMineSceneNameForTesting(string sceneName)
    {
        gemMineSceneName = sceneName;
    }

    /// <summary>
    /// Cập nhật thời gian reset thủ công (dùng cho test hoặc override đặc biệt).
    /// </summary>
    public void SetRemainingTime(int hours, int minutes, int seconds = 0)
    {
        useManualTime = true;
        remainingHours = Mathf.Max(0, hours);
        remainingMinutes = Mathf.Clamp(minutes, 0, 59);
        remainingSeconds = Mathf.Clamp(seconds, 0, 59);
        RefreshStatusTexts();
    }

    /// <summary>
    /// Cập nhật số lượt tham gia thủ công.
    /// </summary>
    public void SetEntrances(int remaining, int max = DefaultMaxEntrances)
    {
        maxEntrances = max;
        remainingEntrances = Mathf.Clamp(remaining, 0, maxEntrances);
        PlayerPrefs.SetInt(PrefsEntrancesKey, remainingEntrances);
        PlayerPrefs.Save();
        RefreshStatusTexts();
    }

    public void RefreshStatusTexts()
    {
        bool isFull = remainingEntrances >= maxEntrances;
        DailyGemMineLayoutTuner tuner = GetComponent<DailyGemMineLayoutTuner>();

        if (resetTimerText != null)
        {
            if (isFull)
            {
                // Khi còn đủ 5 lượt tối đa: KHÔNG hiển thị Reset in time
                resetTimerText.gameObject.SetActive(false);
                resetTimerText.text = string.Empty;
            }
            else
            {
                // Khi đã chơi (-1 lượt trở đi): Hiển thị Reset in time
                resetTimerText.gameObject.SetActive(true);
                Vector2 timerPos = tuner != null ? tuner.resetTimerPosition : new Vector2(0f, -225f);
                resetTimerText.rectTransform.anchoredPosition = timerPos;
                if (tuner != null)
                {
                    resetTimerText.rectTransform.sizeDelta = tuner.resetTimerSize;
                    resetTimerText.fontSize = tuner.resetTimerFontSize;
                }
                string hStr = remainingHours.ToString("D2");
                string mStr = remainingMinutes.ToString("D2");
                resetTimerText.text = $"Reset in: <color=#FFEE33>{hStr}</color> Hour <color=#FFEE33>{mStr}</color> Min Left";
            }
        }

        if (entranceCountText != null)
        {
            entranceCountText.text = $"Entrance: <color=#FFEE33>{remainingEntrances}</color> Left";
            if (tuner != null)
            {
                entranceCountText.rectTransform.anchoredPosition = isFull ? tuner.entrancePositionFull : tuner.entrancePositionActive;
                entranceCountText.rectTransform.sizeDelta = tuner.entranceSize;
                entranceCountText.fontSize = tuner.entranceFontSize;
            }
            else
            {
                // Khi đủ 5 lượt (không có timer), căn giữa entrance text hài hòa
                entranceCountText.rectTransform.anchoredPosition = isFull
                    ? new Vector2(0f, -250f)
                    : new Vector2(0f, -275f);
            }
        }

        // Khóa / mở nút Start tùy theo số lượt còn lại
        if (startLevel1Button != null)
        {
            bool hasEntrance = remainingEntrances > 0;
            startLevel1Button.interactable = hasEntrance;

            CanvasGroup btnCg = startLevel1Button.GetComponent<CanvasGroup>();
            if (btnCg != null)
            {
                btnCg.alpha = hasEntrance ? 1f : 0.6f;
            }
        }

        RefreshLevelCardsState();
    }

    /// <summary>
    /// Đồng bộ trạng thái mở khóa tuần tự (màn trước xong mới mở màn sau) và khả dụng theo số lượt.
    /// </summary>
    public void RefreshLevelCardsState()
    {
        bool hasEntrances = remainingEntrances > 0;

        if (levelCards != null && levelCards.Length > 0)
        {
            for (int i = 0; i < levelCards.Length; i++)
            {
                var card = levelCards[i];
                if (card == null) continue;
                int levelNum = card.LevelNumber > 0 ? card.LevelNumber : (i + 1);
                bool isUnlocked = DailyGemMineProgress.IsLevelUnlocked(levelNum);

                card.RemoveRedundantStartLabel();
                card.EnsureLockBadge();

                if (!isUnlocked)
                {
                    card.SetLocked(true, $"Clear LV.{levelNum - 1:D2} to Unlock");
                }
                else
                {
                    card.SetLocked(false);
                    card.SetInteractable(true, hasEntrances);
                }
            }
        }
    }

    /// <summary>
    /// Đánh dấu đã chiến thắng màn chơi hiện tại và mở khóa màn kế tiếp.
    /// </summary>
    public bool CompleteLevel(int level)
    {
        bool completed = DailyGemMineProgress.CompleteLevel(level);
        if (completed)
        {
            RefreshLevelCardsState();
        }
        return completed;
    }

    public void SetUIReferencesForTesting(
        GameObject root,
        Button premiumBtn,
        Button startLvl1,
        Button closeBtn,
        TMP_Text timerTxt,
        TMP_Text entranceTxt,
        DailyGemMineLevelCard[] cards = null,
        ScrollRect scrollRect = null)
    {
        modalRoot = root;
        monthlyPremiumButton = premiumBtn;
        startLevel1Button = startLvl1;
        closeButton = closeBtn;
        resetTimerText = timerTxt;
        entranceCountText = entranceTxt;
        levelCards = cards;
        levelsScrollRect = scrollRect;
        useManualTime = true;
        RegisterLevelCardsEvents();
        RefreshStatusTexts();
    }

    public void SetCanvasGroupForTesting(CanvasGroup cg)
    {
        canvasGroup = cg;
    }
}
