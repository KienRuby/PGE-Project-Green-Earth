using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component điều chỉnh vị trí (Position), kích thước (Size/Width/Height), tỉ lệ (Scale) 
/// cho toàn bộ các thành phần trong Daily Gem Mine:
/// - Hoạt động đồng bộ 100% giữa Edit Mode và Play Mode (ExecuteAlways).
/// - Cho phép kéo thanh trượt / chỉnh số realtime trong Scene View & Game View.
/// - Lưu trữ thông số trực tiếp vào RectTransform để không bị sai lệch khi chuyển chế độ chơi.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class DailyGemMineLayoutTuner : MonoBehaviour
{
    [Header("=== References (Tự động tìm) ===")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private RectTransform monthlyPremiumBanner;
    [SerializeField] private RectTransform priceButton;
    [SerializeField] private RectTransform dailyGemMinePanel;
    [SerializeField] private RectTransform resetTimerText;
    [SerializeField] private RectTransform entranceCountText;
    [SerializeField] private RectTransform levelsScrollView;
    [SerializeField] private RectTransform levelsContent;
    [SerializeField] private RectTransform cardLevel1;
    [SerializeField] private RectTransform startButton;
    [SerializeField] private Image backdropImage;

    [Header("=== 1. Content Root (Khung bao toàn bộ) ===")]
    public Vector2 contentAnchoredPosition = new Vector2(0f, 25f);
    public Vector2 contentSizeDelta = new Vector2(980f, 1530f);
    public Vector3 contentScale = Vector3.one;

    [Header("=== 2. Monthly Premium Banner ===")]
    public Vector2 bannerAnchoredPosition = new Vector2(0f, 0f);
    public Vector2 bannerSizeDelta = new Vector2(980f, 290f);

    [Header("=== 3. Hitbox nút 90.000 đ ===")]
    public Vector2 priceButtonAnchoredPosition = new Vector2(0f, 15f);
    public Vector2 priceButtonSizeDelta = new Vector2(300f, 90f);

    [Header("=== 4. Daily Gem Mine Main Panel ===")]
    public Vector2 panelAnchoredPosition = new Vector2(0f, -305f);
    public Vector2 panelSizeDelta = new Vector2(766f, 1200f);

    [Header("=== 5. Text Reset Timer ===")]
    public Vector2 resetTimerPosition = new Vector2(0f, -225f);
    public Vector2 resetTimerSize = new Vector2(700f, 40f);
    [Range(18f, 60f)] public float resetTimerFontSize = 30f;

    [Header("=== 6. Text Entrance Count ===")]
    public Vector2 entrancePositionFull = new Vector2(0f, -250f);
    public Vector2 entrancePositionActive = new Vector2(0f, -270f);
    public Vector2 entranceSize = new Vector2(700f, 40f);
    [Range(18f, 60f)] public float entranceFontSize = 32f;

    [Header("=== 7. Khung cuộn Levels ScrollView (5 Levels) ===")]
    public Vector2 scrollViewAnchoredPosition = new Vector2(0f, -325f);
    public Vector2 scrollViewSizeDelta = new Vector2(720f, 850f);
    [Range(0f, 60f)] public float cardSpacing = 20f;
    public Vector2 cardSizeDelta = new Vector2(700f, 350f);

    [Header("=== 8. Nút Pink Start trên các Card ===")]
    public Vector2 startButtonAnchoredPosition = new Vector2(-20f, 18f);
    public Vector2 startButtonSizeDelta = new Vector2(190f, 72f);
    [Range(18f, 60f)] public float startButtonFontSize = 34f;

    [Header("=== 9. Header & Thưởng trên các Card ===")]
    public float headerHeight = 70f;
    [Range(18f, 60f)] public float titleFontSize = 36f;
    public Vector2 titleAnchoredPosition = new Vector2(25f, 0f);
    public Vector2 titleSizeDelta = new Vector2(320f, 50f);
    public Vector2 gemIconAnchoredPosition = new Vector2(-175f, 0f);
    public Vector2 gemIconSizeDelta = new Vector2(34f, 44f);
    public Vector2 rewardAnchoredPosition = new Vector2(-165f, 0f);
    public Vector2 rewardSizeDelta = new Vector2(150f, 50f);
    [Range(18f, 60f)] public float rewardFontSize = 32f;

    [Header("=== 10. Nền tối Backdrop ===")]
    public Color backdropColor = new Color(0f, 0f, 0f, 0.86f);

    public RectTransform ContentRoot => contentRoot;
    public RectTransform MonthlyPremiumBanner => monthlyPremiumBanner;
    public RectTransform DailyGemMinePanel => dailyGemMinePanel;
    public RectTransform LevelsScrollView => levelsScrollView;

    private void Awake()
    {
        AutoFindReferences();
        ApplyLayout();
    }

    private void OnEnable()
    {
        AutoFindReferences();
        ApplyLayout();
    }

    private void Start()
    {
        ApplyLayout();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyLayout();
    }
#endif

    /// <summary>
    /// Tự động tìm kiếm toàn bộ các Transform/Image/Text con trong cây phân cấp.
    /// </summary>
    [ContextMenu("Auto-Find References")]
    public void AutoFindReferences()
    {
        if (contentRoot == null)
        {
            Transform cr = transform.Find("ContentRoot");
            if (cr != null) contentRoot = cr.GetComponent<RectTransform>();
        }

        if (backdropImage == null)
        {
            Transform bd = transform.Find("Backdrop");
            if (bd != null) backdropImage = bd.GetComponent<Image>();
        }

        if (contentRoot != null)
        {
            if (monthlyPremiumBanner == null)
            {
                Transform mpb = contentRoot.Find("MonthlyPremiumBanner");
                if (mpb != null) monthlyPremiumBanner = mpb.GetComponent<RectTransform>();
            }

            if (dailyGemMinePanel == null)
            {
                Transform dgmp = contentRoot.Find("DailyGemMinePanel");
                if (dgmp != null) dailyGemMinePanel = dgmp.GetComponent<RectTransform>();
            }
        }

        if (monthlyPremiumBanner != null && priceButton == null)
        {
            Transform pb = monthlyPremiumBanner.Find("PriceButton");
            if (pb != null) priceButton = pb.GetComponent<RectTransform>();
        }

        if (dailyGemMinePanel != null)
        {
            if (resetTimerText == null)
            {
                Transform rt = dailyGemMinePanel.Find("ResetTimerText");
                if (rt != null) resetTimerText = rt.GetComponent<RectTransform>();
            }

            if (entranceCountText == null)
            {
                Transform ec = dailyGemMinePanel.Find("EntranceCountText");
                if (ec != null) entranceCountText = ec.GetComponent<RectTransform>();
            }

            if (levelsScrollView == null)
            {
                Transform sv = dailyGemMinePanel.Find("LevelsScrollView");
                if (sv != null) levelsScrollView = sv.GetComponent<RectTransform>();
            }

            if (cardLevel1 == null)
            {
                Transform c1 = dailyGemMinePanel.Find("LevelsScrollView/Viewport/Content/Card_Level_01")
                    ?? dailyGemMinePanel.Find("Card_Level_01");
                if (c1 != null) cardLevel1 = c1.GetComponent<RectTransform>();
            }
        }

        if (levelsScrollView != null && levelsContent == null)
        {
            Transform cnt = levelsScrollView.Find("Viewport/Content");
            if (cnt != null) levelsContent = cnt.GetComponent<RectTransform>();
        }

        if (cardLevel1 != null && startButton == null)
        {
            Transform sb = cardLevel1.Find("StartButton");
            if (sb != null) startButton = sb.GetComponent<RectTransform>();
        }
    }

    /// <summary>
    /// Áp dụng toàn bộ thông số vị trí, kích cỡ, scale lên các đối tượng UI ngay lập tức.
    /// Đảm bảo Edit Mode và Play Mode hiển thị hoàn toàn giống nhau 100%.
    /// </summary>
    [ContextMenu("Apply Layout")]
    public void ApplyLayout()
    {
        AutoFindReferences();

        // 1. Content Root (Center-Center)
        if (contentRoot != null)
        {
            contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
            contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
            contentRoot.pivot = new Vector2(0.5f, 0.5f);
            contentRoot.anchoredPosition = contentAnchoredPosition;
            contentRoot.sizeDelta = contentSizeDelta;
            contentRoot.localScale = contentScale;
        }

        // 2. Monthly Premium Banner (Top-Center)
        if (monthlyPremiumBanner != null)
        {
            monthlyPremiumBanner.anchorMin = new Vector2(0.5f, 1f);
            monthlyPremiumBanner.anchorMax = new Vector2(0.5f, 1f);
            monthlyPremiumBanner.pivot = new Vector2(0.5f, 1f);
            monthlyPremiumBanner.anchoredPosition = bannerAnchoredPosition;
            monthlyPremiumBanner.sizeDelta = bannerSizeDelta;
        }

        // 3. Price Button Hitbox (Bottom-Center of Banner)
        if (priceButton != null)
        {
            priceButton.anchorMin = new Vector2(0.5f, 0f);
            priceButton.anchorMax = new Vector2(0.5f, 0f);
            priceButton.pivot = new Vector2(0.5f, 0f);
            priceButton.anchoredPosition = priceButtonAnchoredPosition;
            priceButton.sizeDelta = priceButtonSizeDelta;
        }

        // 4. Daily Gem Mine Main Panel (Top-Center of ContentRoot)
        if (dailyGemMinePanel != null)
        {
            dailyGemMinePanel.anchorMin = new Vector2(0.5f, 1f);
            dailyGemMinePanel.anchorMax = new Vector2(0.5f, 1f);
            dailyGemMinePanel.pivot = new Vector2(0.5f, 1f);
            dailyGemMinePanel.anchoredPosition = panelAnchoredPosition;
            dailyGemMinePanel.sizeDelta = panelSizeDelta;
        }

        // 5. Reset Timer Text (Top-Center of Panel)
        if (resetTimerText != null)
        {
            resetTimerText.anchorMin = new Vector2(0.5f, 1f);
            resetTimerText.anchorMax = new Vector2(0.5f, 1f);
            resetTimerText.pivot = new Vector2(0.5f, 1f);
            resetTimerText.anchoredPosition = resetTimerPosition;
            resetTimerText.sizeDelta = resetTimerSize;
            TMP_Text t = resetTimerText.GetComponent<TMP_Text>();
            if (t != null) t.fontSize = resetTimerFontSize;
        }

        // 6. Entrance Count Text (Top-Center of Panel)
        if (entranceCountText != null)
        {
            entranceCountText.anchorMin = new Vector2(0.5f, 1f);
            entranceCountText.anchorMax = new Vector2(0.5f, 1f);
            entranceCountText.pivot = new Vector2(0.5f, 1f);

            DailyGemMineModalController ctrl = GetComponent<DailyGemMineModalController>();
            bool isFull = ctrl == null || ctrl.RemainingEntrances >= ctrl.MaxEntrances;
            entranceCountText.anchoredPosition = isFull ? entrancePositionFull : entrancePositionActive;
            entranceCountText.sizeDelta = entranceSize;
            TMP_Text t = entranceCountText.GetComponent<TMP_Text>();
            if (t != null) t.fontSize = entranceFontSize;
        }

        // 7. Levels ScrollView (Cuộn 5 Level)
        if (levelsScrollView != null)
        {
            levelsScrollView.anchorMin = new Vector2(0.5f, 1f);
            levelsScrollView.anchorMax = new Vector2(0.5f, 1f);
            levelsScrollView.pivot = new Vector2(0.5f, 1f);
            levelsScrollView.anchoredPosition = scrollViewAnchoredPosition;
            levelsScrollView.sizeDelta = scrollViewSizeDelta;
        }

        if (levelsContent != null)
        {
            VerticalLayoutGroup vlg = levelsContent.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.spacing = cardSpacing;
            }

            // Cập nhật kích thước cho từng Level Card trong Content
            for (int i = 0; i < levelsContent.childCount; i++)
            {
                Transform child = levelsContent.GetChild(i);
                if (child is RectTransform childRect)
                {
                    childRect.sizeDelta = cardSizeDelta;
                    LayoutElement le = child.GetComponent<LayoutElement>();
                    if (le != null)
                    {
                        le.preferredWidth = cardSizeDelta.x;
                        le.preferredHeight = cardSizeDelta.y;
                        le.minHeight = cardSizeDelta.y;
                    }

                    // Start Button trên card
                    Transform sb = child.Find("StartButton");
                    if (sb is RectTransform sbRect)
                    {
                        sbRect.anchoredPosition = startButtonAnchoredPosition;
                        sbRect.sizeDelta = startButtonSizeDelta;

                        Transform startLbl = sb.Find("StartLabel");
                        if (startLbl != null)
                        {
                            if (Application.isPlaying) Destroy(startLbl.gameObject);
                            else DestroyImmediate(startLbl.gameObject);
                        }
                    }

                    // Locked Badge trên card
                    Transform lb = child.Find("LockedBadge");
                    if (lb is RectTransform lbRect)
                    {
                        lbRect.anchoredPosition = startButtonAnchoredPosition;
                        lbRect.sizeDelta = startButtonSizeDelta;
                    }

                    // HeaderBanner trên card
                    Transform hb = child.Find("HeaderBanner");
                    if (hb is RectTransform hbRect)
                    {
                        hbRect.sizeDelta = new Vector2(0f, headerHeight);

                        Transform title = hb.Find("TitleText");
                        if (title is RectTransform titleRect)
                        {
                            titleRect.anchoredPosition = titleAnchoredPosition;
                            titleRect.sizeDelta = titleSizeDelta;
                            TMP_Text t = title.GetComponent<TMP_Text>();
                            if (t != null)
                            {
                                t.fontSize = titleFontSize;
                                t.enableWordWrapping = false;
                            }
                        }

                        Transform gem = hb.Find("GemIcon");
                        if (gem is RectTransform gemRect)
                        {
                            gemRect.anchoredPosition = gemIconAnchoredPosition;
                            gemRect.sizeDelta = gemIconSizeDelta;
                        }

                        Transform rwd = hb.Find("RewardText");
                        if (rwd is RectTransform rwdRect)
                        {
                            rwdRect.anchoredPosition = rewardAnchoredPosition;
                            rwdRect.sizeDelta = rewardSizeDelta;
                            TMP_Text t = rwd.GetComponent<TMP_Text>();
                            if (t != null)
                            {
                                t.fontSize = rewardFontSize;
                                t.enableWordWrapping = false;
                            }
                        }
                    }
                }
            }
        }
        else if (cardLevel1 != null)
        {
            // Fallback nếu không có scrollview
            cardLevel1.anchoredPosition = scrollViewAnchoredPosition;
            cardLevel1.sizeDelta = cardSizeDelta;
        }

        // 8. Backdrop
        if (backdropImage != null)
        {
            backdropImage.color = backdropColor;
        }
    }

    /// <summary>
    /// Đọc thông số hiện tại từ các RectTransform trong Scene và lưu vào biến của component.
    /// </summary>
    [ContextMenu("Read From Current Scene Transforms")]
    public void ReadFromCurrentSceneTransforms()
    {
        AutoFindReferences();

        if (contentRoot != null)
        {
            contentAnchoredPosition = contentRoot.anchoredPosition;
            contentSizeDelta = contentRoot.sizeDelta;
            contentScale = contentRoot.localScale;
        }

        if (monthlyPremiumBanner != null)
        {
            bannerAnchoredPosition = monthlyPremiumBanner.anchoredPosition;
            bannerSizeDelta = monthlyPremiumBanner.sizeDelta;
        }

        if (priceButton != null)
        {
            priceButtonAnchoredPosition = priceButton.anchoredPosition;
            priceButtonSizeDelta = priceButton.sizeDelta;
        }

        if (dailyGemMinePanel != null)
        {
            panelAnchoredPosition = dailyGemMinePanel.anchoredPosition;
            panelSizeDelta = dailyGemMinePanel.sizeDelta;
        }

        if (resetTimerText != null)
        {
            resetTimerPosition = resetTimerText.anchoredPosition;
            resetTimerSize = resetTimerText.sizeDelta;
            TMP_Text t = resetTimerText.GetComponent<TMP_Text>();
            if (t != null) resetTimerFontSize = t.fontSize;
        }

        if (entranceCountText != null)
        {
            entrancePositionActive = entranceCountText.anchoredPosition;
            entranceSize = entranceCountText.sizeDelta;
            TMP_Text t = entranceCountText.GetComponent<TMP_Text>();
            if (t != null) entranceFontSize = t.fontSize;
        }

        if (levelsScrollView != null)
        {
            scrollViewAnchoredPosition = levelsScrollView.anchoredPosition;
            scrollViewSizeDelta = levelsScrollView.sizeDelta;
        }

        if (levelsContent != null)
        {
            VerticalLayoutGroup vlg = levelsContent.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                cardSpacing = vlg.spacing;
            }
        }

        if (cardLevel1 != null)
        {
            cardSizeDelta = cardLevel1.sizeDelta;
        }

        if (startButton != null)
        {
            startButtonAnchoredPosition = startButton.anchoredPosition;
            startButtonSizeDelta = startButton.sizeDelta;
            TMP_Text t = startButton.GetComponentInChildren<TMP_Text>(true);
            if (t != null) startButtonFontSize = t.fontSize;
        }

        if (backdropImage != null)
        {
            backdropColor = backdropImage.color;
        }
    }

    /// <summary>
    /// Khôi phục thông số chuẩn AAA theo ảnh mẫu số 2.
    /// </summary>
    [ContextMenu("Reset To Reference AAA Preset")]
    public void ResetToReferencePreset()
    {
        contentAnchoredPosition = new Vector2(0f, 25f);
        contentSizeDelta = new Vector2(980f, 1530f);
        contentScale = Vector3.one;

        bannerAnchoredPosition = new Vector2(0f, 0f);
        bannerSizeDelta = new Vector2(980f, 290f);

        priceButtonAnchoredPosition = new Vector2(0f, 15f);
        priceButtonSizeDelta = new Vector2(300f, 90f);

        panelAnchoredPosition = new Vector2(0f, -305f);
        panelSizeDelta = new Vector2(766f, 1200f);

        resetTimerPosition = new Vector2(0f, -225f);
        resetTimerSize = new Vector2(700f, 40f);
        resetTimerFontSize = 30f;

        entrancePositionFull = new Vector2(0f, -250f);
        entrancePositionActive = new Vector2(0f, -270f);
        entranceSize = new Vector2(700f, 40f);
        entranceFontSize = 32f;

        scrollViewAnchoredPosition = new Vector2(0f, -325f);
        scrollViewSizeDelta = new Vector2(720f, 850f);
        cardSpacing = 20f;
        cardSizeDelta = new Vector2(700f, 350f);

        startButtonAnchoredPosition = new Vector2(-20f, 18f);
        startButtonSizeDelta = new Vector2(190f, 72f);
        startButtonFontSize = 34f;

        headerHeight = 70f;
        titleFontSize = 36f;
        titleAnchoredPosition = new Vector2(25f, 0f);
        titleSizeDelta = new Vector2(320f, 50f);
        gemIconAnchoredPosition = new Vector2(-175f, 0f);
        gemIconSizeDelta = new Vector2(34f, 44f);
        rewardAnchoredPosition = new Vector2(-165f, 0f);
        rewardSizeDelta = new Vector2(150f, 50f);
        rewardFontSize = 32f;

        backdropColor = new Color(0f, 0f, 0f, 0.86f);

        ApplyLayout();
    }
}
