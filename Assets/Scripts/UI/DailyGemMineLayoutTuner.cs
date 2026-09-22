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
    [SerializeField] private RectTransform cardLevel1;
    [SerializeField] private RectTransform startButton;
    [SerializeField] private RectTransform cardLevel2;
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
    public Vector2 panelSizeDelta = new Vector2(980f, 1200f);

    [Header("=== 5. Text Reset Timer ===")]
    public Vector2 resetTimerPosition = new Vector2(0f, -225f);
    public Vector2 resetTimerSize = new Vector2(880f, 44f);
    [Range(18f, 60f)] public float resetTimerFontSize = 34f;

    [Header("=== 6. Text Entrance Count ===")]
    public Vector2 entrancePositionFull = new Vector2(0f, -250f);
    public Vector2 entrancePositionActive = new Vector2(0f, -275f);
    public Vector2 entranceSize = new Vector2(880f, 44f);
    [Range(18f, 60f)] public float entranceFontSize = 34f;

    [Header("=== 7. Card Level 01 ===")]
    public Vector2 card1AnchoredPosition = new Vector2(0f, -335f);
    public Vector2 card1SizeDelta = new Vector2(890f, 440f);

    [Header("=== 8. Nút Pink Start ===")]
    public Vector2 startButtonAnchoredPosition = new Vector2(-25f, 22f);
    public Vector2 startButtonSizeDelta = new Vector2(220f, 85f);
    [Range(18f, 60f)] public float startButtonFontSize = 40f;

    [Header("=== 9. Card Level 02 ===")]
    public Vector2 card2AnchoredPosition = new Vector2(0f, -795f);
    public Vector2 card2SizeDelta = new Vector2(890f, 380f);

    [Header("=== 10. Nền tối Backdrop ===")]
    public Color backdropColor = new Color(0f, 0f, 0f, 0.86f);

    public RectTransform ContentRoot => contentRoot;
    public RectTransform MonthlyPremiumBanner => monthlyPremiumBanner;
    public RectTransform DailyGemMinePanel => dailyGemMinePanel;

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

            if (cardLevel1 == null)
            {
                Transform c1 = dailyGemMinePanel.Find("Card_Level_01");
                if (c1 != null) cardLevel1 = c1.GetComponent<RectTransform>();
            }

            if (cardLevel2 == null)
            {
                Transform c2 = dailyGemMinePanel.Find("Card_Level_02");
                if (c2 != null) cardLevel2 = c2.GetComponent<RectTransform>();
            }
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

        // 7. Card Level 1 (Top-Center of Panel)
        if (cardLevel1 != null)
        {
            cardLevel1.anchorMin = new Vector2(0.5f, 1f);
            cardLevel1.anchorMax = new Vector2(0.5f, 1f);
            cardLevel1.pivot = new Vector2(0.5f, 1f);
            cardLevel1.anchoredPosition = card1AnchoredPosition;
            cardLevel1.sizeDelta = card1SizeDelta;
        }

        // 8. Start Button (Bottom-Right of Card 1)
        if (startButton != null)
        {
            startButton.anchorMin = new Vector2(1f, 0f);
            startButton.anchorMax = new Vector2(1f, 0f);
            startButton.pivot = new Vector2(1f, 0f);
            startButton.anchoredPosition = startButtonAnchoredPosition;
            startButton.sizeDelta = startButtonSizeDelta;
            TMP_Text t = startButton.GetComponentInChildren<TMP_Text>(true);
            if (t != null) t.fontSize = startButtonFontSize;
        }

        // 9. Card Level 2 (Top-Center of Panel)
        if (cardLevel2 != null)
        {
            cardLevel2.anchorMin = new Vector2(0.5f, 1f);
            cardLevel2.anchorMax = new Vector2(0.5f, 1f);
            cardLevel2.pivot = new Vector2(0.5f, 1f);
            cardLevel2.anchoredPosition = card2AnchoredPosition;
            cardLevel2.sizeDelta = card2SizeDelta;
        }

        // 10. Backdrop
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

        if (cardLevel1 != null)
        {
            card1AnchoredPosition = cardLevel1.anchoredPosition;
            card1SizeDelta = cardLevel1.sizeDelta;
        }

        if (startButton != null)
        {
            startButtonAnchoredPosition = startButton.anchoredPosition;
            startButtonSizeDelta = startButton.sizeDelta;
            TMP_Text t = startButton.GetComponentInChildren<TMP_Text>(true);
            if (t != null) startButtonFontSize = t.fontSize;
        }

        if (cardLevel2 != null)
        {
            card2AnchoredPosition = cardLevel2.anchoredPosition;
            card2SizeDelta = cardLevel2.sizeDelta;
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
        panelSizeDelta = new Vector2(980f, 1200f);

        resetTimerPosition = new Vector2(0f, -225f);
        resetTimerSize = new Vector2(880f, 44f);
        resetTimerFontSize = 34f;

        entrancePositionFull = new Vector2(0f, -250f);
        entrancePositionActive = new Vector2(0f, -275f);
        entranceSize = new Vector2(880f, 44f);
        entranceFontSize = 34f;

        card1AnchoredPosition = new Vector2(0f, -335f);
        card1SizeDelta = new Vector2(890f, 440f);

        startButtonAnchoredPosition = new Vector2(-25f, 22f);
        startButtonSizeDelta = new Vector2(220f, 85f);
        startButtonFontSize = 40f;

        card2AnchoredPosition = new Vector2(0f, -795f);
        card2SizeDelta = new Vector2(890f, 380f);

        backdropColor = new Color(0f, 0f, 0f, 0.86f);

        ApplyLayout();
    }
}
