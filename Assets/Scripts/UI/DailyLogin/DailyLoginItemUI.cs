using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý hiển thị một dòng phần thưởng đăng nhập hàng ngày (ví dụ DAY 01 .. DAY 07)
/// theo đúng bố cục và visual reference trong Image 2:
/// - Tiêu đề ngày: DAY + số ngày (01..07)
/// - Danh sách các icon phần thưởng và số lượng (X30, X300, X1000...)
/// - 4 Trạng thái: Locked, Available (nút Get), Obtained (overlay làm tối), CurrentDayWaiting (Time Remaining HH:mm:ss)
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
    [Tooltip("Nút nhận thưởng khi ngày hiện tại có thể claim (Available).")]
    [SerializeField] private Button claimButton;
    [SerializeField] private TMP_Text claimButtonText;

    [Tooltip("Overlay và nhãn hiển thị khi ngày đã nhận thưởng (Obtained).")]
    [SerializeField] private GameObject obtainedRoot;
    [SerializeField] private TMP_Text obtainedText;

    [Tooltip("Khu vực hiển thị đếm ngược khi hôm nay đã nhận thưởng (CurrentDayWaiting).")]
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

    private static readonly Color AvailableBorderColor = new Color32(94, 213, 205, 255);
    private static readonly Color NormalBorderColor = new Color32(31, 87, 94, 255);
    private static readonly Color LockedBorderColor = new Color32(20, 50, 60, 255);

    private void Awake()
    {
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
    /// Cấu hình dữ liệu và giao diện cho item ngày này.
    /// </summary>
    public void Setup(
        DailyLoginDayData dayData,
        DailyLoginState state,
        Action<int> claimCallback,
        Func<RewardType, Sprite> iconResolver)
    {
        if (dayData == null) return;

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

        // 3. Cập nhật trạng thái
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

    public void UpdateState(DailyLoginState state)
    {
        isAvailable = (state == DailyLoginState.Available);

        // A. Trạng thái Obtained (Đã nhận)
        if (state == DailyLoginState.Obtained)
        {
            if (obtainedRoot != null) obtainedRoot.SetActive(true);
            if (obtainedText != null) obtainedText.text = "Obtained";
            if (claimButton != null) claimButton.gameObject.SetActive(false);
            if (countdownRoot != null) countdownRoot.SetActive(false);
            if (itemCanvasGroup != null) itemCanvasGroup.alpha = 0.55f;
            if (cardBorder != null) cardBorder.color = NormalBorderColor;
            return;
        }

        // B. Trạng thái CurrentDayWaiting (Hôm nay đã nhận, chờ đếm ngược)
        if (state == DailyLoginState.CurrentDayWaiting)
        {
            if (obtainedRoot != null) obtainedRoot.SetActive(false);
            if (claimButton != null) claimButton.gameObject.SetActive(false);
            if (countdownRoot != null)
            {
                countdownRoot.SetActive(true);
                if (countdownLabelText != null) countdownLabelText.text = "Time Remaining";
                UpdateCountdownText(DailyLoginManager.Instance != null ? DailyLoginManager.Instance.GetRemainingTimeFormatted() : "00:00:00");
            }
            if (itemCanvasGroup != null) itemCanvasGroup.alpha = 1.0f;
            if (cardBorder != null) cardBorder.color = AvailableBorderColor;
            return;
        }

        // C. Trạng thái Available (Hôm nay chưa nhận -> Có nút Get)
        if (state == DailyLoginState.Available)
        {
            if (obtainedRoot != null) obtainedRoot.SetActive(false);
            if (countdownRoot != null) countdownRoot.SetActive(false);
            if (claimButton != null)
            {
                claimButton.gameObject.SetActive(true);
                claimButton.interactable = true;
                if (claimButtonText != null) claimButtonText.text = "Get";
            }
            if (itemCanvasGroup != null) itemCanvasGroup.alpha = 1.0f;
            if (cardBorder != null) cardBorder.color = AvailableBorderColor;
            return;
        }

        // D. Trạng thái Locked (Ngày tương lai)
        if (state == DailyLoginState.Locked)
        {
            if (obtainedRoot != null) obtainedRoot.SetActive(false);
            if (claimButton != null) claimButton.gameObject.SetActive(false);
            if (countdownRoot != null) countdownRoot.SetActive(false);
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

    private void OnClaimButtonClicked()
    {
        if (isAvailable && onClaimCallback != null)
        {
            if (claimButton != null && isActiveAndEnabled)
            {
                StartCoroutine(PunchScaleRoutine(claimButton.transform));
            }
            onClaimCallback.Invoke(currentDayIndex);
        }
    }

    private System.Collections.IEnumerator PunchScaleRoutine(Transform targetTr)
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
        CanvasGroup cg)
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
    }
}
