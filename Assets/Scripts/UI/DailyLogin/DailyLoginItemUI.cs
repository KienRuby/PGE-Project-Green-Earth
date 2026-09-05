using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum DailyButtonState
{
    Hidden,
    Get,
    ClaimAgain,
    Obtained
}

/// <summary>
/// Quản lý hiển thị một dòng phần thưởng đăng nhập hàng ngày (Day 01 .. Day 07)
/// Tích hợp 3 nút chuẩn theo yêu cầu:
/// 1. "Get" (Btn_Get): Nhận thưởng lần đầu trong ngày.
/// 2. "Claim again" (Btn_Claim_Again): Xem quảng cáo để nhận thêm quà ngày hôm nay.
/// 3. "Obtained" (Btn_Obtained): Hiển thị khi không có mạng wifi hoặc đã xem quảng cáo rồi,
///    hoặc các ngày trước đó đã nhận. Nút này KHÔNG CHO PHÉP BẤM (interactable = false).
/// </summary>
public class DailyLoginItemUI : MonoBehaviour
{
    [Header("Day Header")]
    [SerializeField] private TMP_Text dayLabelText;
    [SerializeField] private TMP_Text dayNumberText;

    [Header("Reward Badges Container")]
    [SerializeField] private Transform rewardsContainer;
    [SerializeField] private GameObject rewardBadgePrefab;

    [Header("State Containers & Buttons")]
    [Tooltip("Nút hành động chính (sẽ đổi sprite giữa Get, Claim Again, Obtained).")]
    [SerializeField] private Button claimButton;
    [SerializeField] private TMP_Text claimButtonText;
    [SerializeField] private Image claimButtonImage;

    [Header("Button Sprites (Get / Claim Again / Obtained)")]
    [SerializeField] private Sprite btnGetSprite;
    [SerializeField] private Sprite btnClaimAgainSprite;
    [SerializeField] private Sprite btnObtainedSprite;

    [Tooltip("Overlay cũ hiển thị khi ngày đã nhận thưởng (giữ để tương thích ngược).")]
    [SerializeField] private GameObject obtainedRoot;
    [SerializeField] private TMP_Text obtainedText;

    [Tooltip("Khu vực đếm ngược (giữ để tương thích ngược).")]
    [SerializeField] private GameObject countdownRoot;
    [SerializeField] private TMP_Text countdownLabelText;
    [SerializeField] private TMP_Text countdownTimeText;

    [Header("Visual Styling Elements")]
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image cardBorder;
    [SerializeField] private CanvasGroup itemCanvasGroup;

    private int currentDayIndex = 1;
    private Action<int> onClaimCallback;
    private bool isAvailable = false;
    private DailyButtonState currentButtonState = DailyButtonState.Hidden;

    private static readonly Color AvailableBorderColor = new Color32(94, 213, 205, 255);
    private static readonly Color NormalBorderColor = new Color32(31, 87, 94, 255);
    private static readonly Color LockedBorderColor = new Color32(20, 50, 60, 255);

    public int CurrentDayIndex => currentDayIndex;
    public DailyButtonState CurrentButtonState => currentButtonState;
    public Button ClaimButton => claimButton;
    public Image ClaimButtonImage => claimButtonImage;
    public Sprite BtnGetSprite => btnGetSprite;
    public Sprite BtnClaimAgainSprite => btnClaimAgainSprite;
    public Sprite BtnObtainedSprite => btnObtainedSprite;

    private void Awake()
    {
        EnsureButtonSpritesLoaded();
        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(OnClaimButtonClicked);
            claimButton.onClick.AddListener(OnClaimButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(OnClaimButtonClicked);
        }
    }

    /// <summary>
    /// Cung cấp 3 sprite từ ngoài vào (Editor builder hoặc test).
    /// </summary>
    public void SetButtonSprites(Sprite getSprite, Sprite claimAgainSprite, Sprite obtainedSprite)
    {
        if (getSprite != null) btnGetSprite = getSprite;
        if (claimAgainSprite != null) btnClaimAgainSprite = claimAgainSprite;
        if (obtainedSprite != null) btnObtainedSprite = obtainedSprite;
    }

    /// <summary>
    /// Tự động nạp 3 sprite từ thư mục Assets/Sprites/UI/Reward/ nếu chưa được gán.
    /// </summary>
    public void EnsureButtonSpritesLoaded()
    {
        if (claimButton != null && claimButtonImage == null)
        {
            claimButtonImage = claimButton.GetComponent<Image>();
        }

        if (btnGetSprite != null && btnClaimAgainSprite != null && btnObtainedSprite != null)
            return;

#if UNITY_EDITOR
        if (btnGetSprite == null)
            btnGetSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Btn_Get.png");
        if (btnClaimAgainSprite == null)
            btnClaimAgainSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Btn_Claim_Again.png");
        if (btnObtainedSprite == null)
            btnObtainedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Btn_Obtained.png");

        if (btnGetSprite == null || btnClaimAgainSprite == null || btnObtainedSprite == null)
        {
            Sprite[] sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/Reward/nút daily login.png")
                ?.OfType<Sprite>().ToArray();
            if (sprites != null)
            {
                foreach (var s in sprites)
                {
                    if (btnGetSprite == null && s.name == "Btn_Get") btnGetSprite = s;
                    else if (btnClaimAgainSprite == null && s.name == "Btn_Claim_Again") btnClaimAgainSprite = s;
                    else if (btnObtainedSprite == null && s.name == "Btn_Obtained") btnObtainedSprite = s;
                }
            }
        }
#endif
    }

    /// <summary>
    /// Cấu hình dữ liệu và giao diện cho item ngày này.
    /// </summary>
    public void Setup(
        DailyLoginDayData dayData,
        DailyLoginState state,
        Action<int> claimCallback,
        Func<RewardType, Sprite> iconResolver)
    {
        if (dayData == null) return;

        EnsureButtonSpritesLoaded();
        currentDayIndex = dayData.dayIndex;
        onClaimCallback = claimCallback;

        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(OnClaimButtonClicked);
            claimButton.onClick.AddListener(OnClaimButtonClicked);
        }

        // 1. Text DAY & Số ngày (01..07)
        if (dayLabelText != null) dayLabelText.text = "DAY";
        if (dayNumberText != null) dayNumberText.text = $"{currentDayIndex:00}";

        // 2. Render các phần thưởng (Energy, RedGems, DataChips...)
        RenderRewards(dayData.rewards, iconResolver);

        // 3. Cập nhật trạng thái hiển thị
        UpdateState(state);
    }

    private void RenderRewards(RewardData[] rewards, Func<RewardType, Sprite> iconResolver)
    {
        if (rewardsContainer == null || rewards == null) return;

        // Xóa hoặc ẩn các badge cũ
        for (int i = rewardsContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = rewardsContainer.GetChild(i);
            if (rewardBadgePrefab != null && child.gameObject == rewardBadgePrefab)
            {
                child.gameObject.SetActive(false);
                continue;
            }
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // Tạo badge mới cho từng reward
        foreach (var reward in rewards)
        {
            GameObject badgeObj;
            if (rewardBadgePrefab != null)
            {
                badgeObj = Instantiate(rewardBadgePrefab, rewardsContainer);
                badgeObj.SetActive(true);
            }
            else
            {
                badgeObj = CreateFallbackRewardBadge(rewardsContainer);
            }

            // Bind icon & text
            Image iconImg = badgeObj.transform.Find("Icon")?.GetComponent<Image>()
                ?? badgeObj.GetComponentInChildren<Image>();
            TMP_Text amountTxt = badgeObj.transform.Find("AmountText")?.GetComponent<TMP_Text>()
                ?? badgeObj.GetComponentInChildren<TMP_Text>();

            if (iconImg != null)
            {
                Sprite icon = reward.customIcon != null ? reward.customIcon : (iconResolver != null ? iconResolver(reward.type) : null);
                if (icon != null)
                {
                    iconImg.sprite = icon;
                    iconImg.enabled = true;
                }
            }

            if (amountTxt != null)
            {
                amountTxt.text = RewardService.FormatRewardAmount(reward.amount);
            }
        }
    }

    /// <summary>
    /// Thay đổi visual hiển thị của nút: Get, Claim Again, Obtained hoặc Hidden.
    /// </summary>
    public void SetButtonVisual(DailyButtonState state)
    {
        EnsureButtonSpritesLoaded();
        currentButtonState = state;

        if (claimButton == null) return;
        if (claimButtonImage == null) claimButtonImage = claimButton.GetComponent<Image>();

        RectTransform btnRect = claimButton.GetComponent<RectTransform>();

        // Ẩn các overlay/highlight thủ công cũ nếu có
        Transform oldBg = claimButton.transform.Find("Background");
        if (oldBg != null) oldBg.gameObject.SetActive(false);
        Transform oldHighlight = claimButton.transform.Find("TopHighlight");
        if (oldHighlight != null) oldHighlight.gameObject.SetActive(false);
        Shadow oldShadow = claimButton.GetComponent<Shadow>();
        if (oldShadow != null) oldShadow.enabled = false;

        switch (state)
        {
            case DailyButtonState.Get:
                claimButton.gameObject.SetActive(true);
                claimButton.interactable = true;
                if (claimButtonImage != null)
                {
                    if (btnGetSprite != null) claimButtonImage.sprite = btnGetSprite;
                    claimButtonImage.preserveAspect = true;
                    claimButtonImage.color = Color.white;
                }
                if (btnRect != null)
                {
                    btnRect.sizeDelta = new Vector2(240f, 105f);
                }
                if (claimButtonText != null)
                {
                    claimButtonText.gameObject.SetActive(false);
                }
                var colorsGet = claimButton.colors;
                colorsGet.disabledColor = Color.white;
                claimButton.colors = colorsGet;
                break;

            case DailyButtonState.ClaimAgain:
                claimButton.gameObject.SetActive(true);
                claimButton.interactable = true;
                if (claimButtonImage != null)
                {
                    if (btnClaimAgainSprite != null) claimButtonImage.sprite = btnClaimAgainSprite;
                    claimButtonImage.preserveAspect = true;
                    claimButtonImage.color = Color.white;
                }
                if (btnRect != null)
                {
                    // Tỷ lệ 373 x 174 -> 255 x 119
                    btnRect.sizeDelta = new Vector2(255f, 119f);
                }
                if (claimButtonText != null)
                {
                    claimButtonText.gameObject.SetActive(false);
                }
                var colorsClaim = claimButton.colors;
                colorsClaim.disabledColor = Color.white;
                claimButton.colors = colorsClaim;
                break;

            case DailyButtonState.Obtained:
                claimButton.gameObject.SetActive(true);
                claimButton.interactable = false; // "nút obtainer ko cho phép bấm"
                if (claimButtonImage != null)
                {
                    if (btnObtainedSprite != null) claimButtonImage.sprite = btnObtainedSprite;
                    claimButtonImage.preserveAspect = true;
                    claimButtonImage.color = Color.white;
                }
                if (btnRect != null)
                {
                    btnRect.sizeDelta = new Vector2(240f, 105f);
                }
                if (claimButtonText != null)
                {
                    claimButtonText.gameObject.SetActive(false);
                }
                var colorsObt = claimButton.colors;
                colorsObt.disabledColor = Color.white; // Không làm xỉn màu sprite gốc
                claimButton.colors = colorsObt;
                break;

            case DailyButtonState.Hidden:
            default:
                claimButton.gameObject.SetActive(false);
                break;
        }
    }

    /// <summary>
    /// Cập nhật trạng thái hiển thị của item theo DailyLoginState.
    /// </summary>
    public void UpdateState(DailyLoginState state)
    {
        EnsureButtonSpritesLoaded();
        isAvailable = (state == DailyLoginState.Available);

        // Ẩn các visual cũ để thay thế bằng hệ thống 3 nút chuẩn
        if (obtainedRoot != null) obtainedRoot.SetActive(false);
        if (countdownRoot != null) countdownRoot.SetActive(false);

        // A. Trạng thái Obtained (Các ngày trước đã nhận)
        if (state == DailyLoginState.Obtained)
        {
            SetButtonVisual(DailyButtonState.Obtained);
            if (itemCanvasGroup != null) itemCanvasGroup.alpha = 0.55f;
            if (cardBorder != null) cardBorder.color = NormalBorderColor;
            return;
        }

        // B. Trạng thái CurrentDayWaiting (Hôm nay đã nhận Get, chờ claim lại bằng Ad hoặc reset ngày)
        if (state == DailyLoginState.CurrentDayWaiting)
        {
            bool hasClaimedAd = DailyLoginManager.Instance != null && DailyLoginManager.Instance.HasClaimedAdToday();
            bool isNetworkAvailable = AdRewardService.IsNetworkAvailable;

            if (!hasClaimedAd && isNetworkAvailable)
            {
                // Có mạng và chưa nhận quà quảng cáo hôm nay -> hiện nút Claim again
                SetButtonVisual(DailyButtonState.ClaimAgain);
            }
            else
            {
                // Không có mạng wifi hoặc đã xem quảng cáo rồi -> hiện nút Obtained (không cho bấm)
                SetButtonVisual(DailyButtonState.Obtained);
            }

            if (itemCanvasGroup != null) itemCanvasGroup.alpha = 1.0f;
            if (cardBorder != null) cardBorder.color = AvailableBorderColor;
            return;
        }

        // C. Trạng thái Available (Hôm nay chưa nhận -> Hiện nút Get)
        if (state == DailyLoginState.Available)
        {
            SetButtonVisual(DailyButtonState.Get);
            if (itemCanvasGroup != null) itemCanvasGroup.alpha = 1.0f;
            if (cardBorder != null) cardBorder.color = AvailableBorderColor;
            return;
        }

        // D. Trạng thái Locked (Ngày tương lai)
        if (state == DailyLoginState.Locked)
        {
            SetButtonVisual(DailyButtonState.Hidden);
            if (itemCanvasGroup != null) itemCanvasGroup.alpha = 0.85f;
            if (cardBorder != null) cardBorder.color = LockedBorderColor;
        }
    }

    public void UpdateCountdownText(string formattedTime)
    {
        if (countdownTimeText != null && countdownRoot != null && countdownRoot.activeSelf)
        {
            countdownTimeText.text = formattedTime;
        }
    }

    /// <summary>
    /// Xử lý sự kiện khi bấm nút claimButton.
    /// </summary>
    public void OnClaimButtonClicked()
    {
        EnsureButtonSpritesLoaded();

        // Nút Obtained tuyệt đối không cho phép bấm
        if (currentButtonState == DailyButtonState.Obtained || (claimButton != null && !claimButton.interactable))
        {
            return;
        }

        // TRƯỜNG HỢP 1: Bấm nút "Get"
        if (currentButtonState == DailyButtonState.Get)
        {
            if (claimButton != null && isActiveAndEnabled)
            {
                StartCoroutine(PunchScaleRoutine(claimButton.transform));
            }

            // Gọi callback nhận thưởng ngày
            if (onClaimCallback != null)
            {
                onClaimCallback.Invoke(currentDayIndex);
            }
            else if (DailyLoginManager.Instance != null)
            {
                DailyLoginManager.Instance.TryClaimTodayReward();
            }

            // Sau khi nhận Get:
            // Nếu có mạng wifi và chưa xem quảng cáo -> Hiện "Claim again"
            // Nếu không có mạng wifi hoặc đã xem quảng cáo -> Hiện "Obtained"
            bool hasClaimedAd = DailyLoginManager.Instance != null && DailyLoginManager.Instance.HasClaimedAdToday();
            bool isNetworkAvailable = AdRewardService.IsNetworkAvailable;

            if (!hasClaimedAd && isNetworkAvailable)
            {
                SetButtonVisual(DailyButtonState.ClaimAgain);
            }
            else
            {
                SetButtonVisual(DailyButtonState.Obtained);
            }
            return;
        }

        // TRƯỜNG HỢP 2: Bấm nút "Claim again" (Xem quảng cáo nhận quà x2)
        if (currentButtonState == DailyButtonState.ClaimAgain)
        {
            // Kiểm tra kết nối mạng Wifi/4G
            if (!AdRewardService.IsNetworkAvailable)
            {
                Debug.LogWarning("[DailyLoginItemUI] ⚠️ Không có kết nối mạng! Chuyển ngay sang nút Obtained.");
                SetButtonVisual(DailyButtonState.Obtained);
                return;
            }

            // Kiểm tra xem đã xem quảng cáo hôm nay chưa
            if (DailyLoginManager.Instance != null && DailyLoginManager.Instance.HasClaimedAdToday())
            {
                Debug.LogWarning("[DailyLoginItemUI] ⚠️ Đã xem quảng cáo nhận thưởng hôm nay rồi! Chuyển sang nút Obtained.");
                SetButtonVisual(DailyButtonState.Obtained);
                return;
            }

            if (claimButton != null && isActiveAndEnabled)
            {
                StartCoroutine(PunchScaleRoutine(claimButton.transform));
            }

            // Kích hoạt xem quảng cáo nhận thưởng
            AdRewardService.ShowRewardedAd((success) =>
            {
                if (success)
                {
                    // Trao thêm quà cho ngày hôm nay
                    if (DailyLoginManager.Instance != null)
                    {
                        DailyLoginManager.Instance.TryClaimAgainWithAd();
                    }
                    SetButtonVisual(DailyButtonState.Obtained);
                }
                else
                {
                    // Nếu thất bại do mất kết nối mạng
                    if (!AdRewardService.IsNetworkAvailable)
                    {
                        SetButtonVisual(DailyButtonState.Obtained);
                    }
                }
            });
            return;
        }
    }

    private IEnumerator PunchScaleRoutine(Transform targetTr)
    {
        if (targetTr == null) yield break;
        Vector3 orig = Vector3.one;

        float el = 0f;
        while (el < 0.05f)
        {
            el += Time.unscaledDeltaTime;
            targetTr.localScale = Vector3.Lerp(orig, new Vector3(1.08f, 0.88f, 1f), el / 0.05f);
            yield return null;
        }

        el = 0f;
        while (el < 0.06f)
        {
            el += Time.unscaledDeltaTime;
            targetTr.localScale = Vector3.Lerp(new Vector3(1.08f, 0.88f, 1f), new Vector3(0.95f, 1.10f, 1f), el / 0.06f);
            yield return null;
        }

        el = 0f;
        while (el < 0.05f)
        {
            el += Time.unscaledDeltaTime;
            targetTr.localScale = Vector3.Lerp(new Vector3(0.95f, 1.10f, 1f), orig, el / 0.05f);
            yield return null;
        }

        targetTr.localScale = orig;
    }

    private GameObject CreateFallbackRewardBadge(Transform parent)
    {
        GameObject badge = new GameObject("RewardBadge", typeof(RectTransform), typeof(Image));
        badge.transform.SetParent(parent, false);
        RectTransform rt = badge.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100f, 100f);
        badge.GetComponent<Image>().color = new Color32(11, 45, 60, 240);

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(badge.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(0f, 10f);
        iconRt.sizeDelta = new Vector2(50f, 50f);
        iconObj.GetComponent<Image>().preserveAspect = true;

        GameObject textObj = new GameObject("AmountText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(badge.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0f, 0f);
        textRt.anchorMax = new Vector2(1f, 0.35f);
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        TMP_Text txt = textObj.GetComponent<TMP_Text>();
        txt.fontSize = 20f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;

        return badge;
    }

    public void SetReferencesForBuilder(
        TMP_Text dayLabel,
        TMP_Text dayNumber,
        Transform rewardsTr,
        Button claimBtn,
        TMP_Text claimBtnTxt,
        GameObject obtainedRt,
        TMP_Text obtainedTxt,
        GameObject countRoot,
        TMP_Text countLbl,
        TMP_Text countTxt,
        Image bg,
        Image border,
        CanvasGroup cg,
        Sprite getSprite = null,
        Sprite claimAgainSprite = null,
        Sprite obtainedSprite = null)
    {
        dayLabelText = dayLabel;
        dayNumberText = dayNumber;
        rewardsContainer = rewardsTr;
        claimButton = claimBtn;
        claimButtonText = claimBtnTxt;
        obtainedRoot = obtainedRt;
        obtainedText = obtainedTxt;
        countdownRoot = countRoot;
        countdownLabelText = countLbl;
        countdownTimeText = countTxt;
        cardBackground = bg;
        cardBorder = border;
        itemCanvasGroup = cg;

        if (getSprite != null) btnGetSprite = getSprite;
        if (claimAgainSprite != null) btnClaimAgainSprite = claimAgainSprite;
        if (obtainedSprite != null) btnObtainedSprite = obtainedSprite;

        if (claimButton != null)
        {
            claimButtonImage = claimButton.GetComponent<Image>();
        }
        EnsureButtonSpritesLoaded();
    }
}
