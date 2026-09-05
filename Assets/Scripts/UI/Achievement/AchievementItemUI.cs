using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý hiển thị một mục thành tựu (Achievement Item) theo đúng bố cục trong Image 1:
/// - Title (tiêu đề nhiệm vụ)
/// - Progress Bar (thanh tiến độ clamped 0-100%)
/// - Progress Text (ví dụ: '2025/2500', '19/3')
/// - Reward Badges (danh sách phần thưởng X200, X1000...)
/// - Action Button:
///     + Hoàn thành chưa nhận: Nút 'Get' màu cyan sáng, click nhận thưởng
///     + Đang thực hiện: Nút 'Not achieved' màu xám, disabled
///     + Đã nhận: Nút 'Obtained' màu tối, disabled
/// </summary>
public class AchievementItemUI : MonoBehaviour
{
    [Header("Title & Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text progressText;

    [Header("Progress Bar")]
    [SerializeField] private Image progressFillImage;
    [SerializeField] private Image progressBgImage;

    [Header("Reward Badges Container")]
    [SerializeField] private Transform rewardsContainer;
    [SerializeField] private GameObject rewardBadgePrefab;

    [Header("Action Button")]
    [SerializeField] private Button actionButton;
    [SerializeField] private Image actionButtonImage;
    [SerializeField] private TMP_Text actionButtonText;
    [SerializeField] private GameObject buttonNotificationDot;

    [Header("Visual Frame")]
    [SerializeField] private Image itemBorder;
    [SerializeField] private Image itemBackground;

    private string achievementId;
    private Action<string> onClaimCallback;
    private bool isClaimable = false;

    // Button Colors matching Image 1
    private static readonly Color GetButtonColor = new Color32(56, 189, 248, 255);        // Cyan bright
    private static readonly Color NotAchievedButtonColor = new Color32(65, 80, 95, 255);  // Gray
    private static readonly Color ObtainedButtonColor = new Color32(35, 50, 65, 255);     // Dark gray
    private static readonly Color TextWhite = new Color32(245, 255, 255, 255);
    private static readonly Color TextGray = new Color32(160, 180, 195, 255);

    public void EnsureUIReferences()
    {
        if (actionButton == null)
        {
            actionButton = GetComponentInChildren<Button>(true);
        }
        if (actionButton != null)
        {
            if (actionButtonImage == null)
            {
                actionButtonImage = actionButton.GetComponent<Image>();
            }
            if (actionButtonText == null)
            {
                actionButtonText = actionButton.GetComponentInChildren<TMP_Text>(true);
            }
        }

        if (titleText == null)
        {
            Transform t = transform.Find("Title") ?? transform.Find("TitleText") ?? transform.Find("Text_Title");
            if (t != null) titleText = t.GetComponent<TMP_Text>();
        }

        if (progressText == null)
        {
            Transform p = transform.Find("ProgressText") ?? transform.Find("ProgressBar/ProgressText") ?? transform.Find("Text_Progress");
            if (p != null) progressText = p.GetComponent<TMP_Text>();
        }

        if (progressFillImage == null)
        {
            Transform f = transform.Find("ProgressBar/Fill") ?? transform.Find("ProgressBar/ProgressFill") ?? transform.Find("Fill");
            if (f != null) progressFillImage = f.GetComponent<Image>();
        }

        if (progressBgImage == null)
        {
            Transform bg = transform.Find("ProgressBar/Background") ?? transform.Find("ProgressBar/ProgressBg") ?? transform.Find("ProgressBar");
            if (bg != null) progressBgImage = bg.GetComponent<Image>();
        }

        if (rewardsContainer == null)
        {
            Transform r = transform.Find("RewardIcons") ?? transform.Find("RewardsContainer") ?? transform.Find("Rewards");
            if (r != null) rewardsContainer = r;
        }

        if (buttonNotificationDot == null && actionButton != null)
        {
            Transform dot = actionButton.transform.Find("NotificationDot") ?? actionButton.transform.Find("RedDot") ?? transform.Find("NotificationDot");
            if (dot != null) buttonNotificationDot = dot.gameObject;
        }
    }

    public TMP_Text ActionButtonText => actionButtonText;
    public Button ActionButton => actionButton;

    private void Awake()
    {
        EnsureUIReferences();
        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(OnActionButtonClicked);
            actionButton.onClick.AddListener(OnActionButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(OnActionButtonClicked);
        }
    }

    public void Setup(
        AchievementDefinition definition,
        int currentProgress,
        AchievementState state,
        Action<string> claimCallback,
        Func<RewardType, Sprite> iconResolver)
    {
        EnsureUIReferences();
        if (definition == null) return;

        achievementId = definition.id;
        onClaimCallback = claimCallback;

        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(OnActionButtonClicked);
            actionButton.onClick.AddListener(OnActionButtonClicked);
        }

        // 1. Tiêu đề
        if (titleText != null)
        {
            titleText.text = definition.title;
        }

        // 2. Tiến độ & Thanh Progress Bar
        int target = Mathf.Max(1, definition.targetValue);
        if (progressText != null)
        {
            progressText.text = $"{currentProgress}/{target}";
        }

        if (progressFillImage != null)
        {
            float fillRatio = Mathf.Clamp01((float)currentProgress / target);
            if (progressFillImage.type == Image.Type.Filled)
            {
                progressFillImage.fillAmount = fillRatio;
            }
            else
            {
                // Fallback nếu không dùng Type.Filled -> chỉnh localScale.x
                progressFillImage.rectTransform.anchorMax = new Vector2(fillRatio, 1f);
                progressFillImage.rectTransform.offsetMax = Vector2.zero;
            }
        }

        // 3. Render các phần thưởng
        RenderRewards(definition.rewards, iconResolver);

        // 4. Cập nhật nút bấm theo State
        UpdateState(state);
    }

    private void RenderRewards(RewardData[] rewards, Func<RewardType, Sprite> iconResolver)
    {
        if (rewardsContainer == null || rewards == null) return;

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

    public void UpdateState(AchievementState state)
    {
        EnsureUIReferences();
        isClaimable = (state == AchievementState.Completed);

        if (state == AchievementState.Completed)
        {
            // Trạng thái hoàn thành -> Nút "Get" sáng cyan
            if (actionButton != null)
            {
                actionButton.interactable = true;
            }
            if (actionButtonImage != null)
            {
                actionButtonImage.color = GetButtonColor;
            }
            if (actionButtonText != null)
            {
                actionButtonText.text = "Get";
                actionButtonText.color = TextWhite;
            }
            if (buttonNotificationDot != null)
            {
                buttonNotificationDot.SetActive(true);
            }
            return;
        }

        if (state == AchievementState.InProgress)
        {
            // Trạng thái đang thực hiện -> Nút "Not achieved" xám
            if (actionButton != null)
            {
                actionButton.interactable = false;
            }
            if (actionButtonImage != null)
            {
                actionButtonImage.color = NotAchievedButtonColor;
            }
            if (actionButtonText != null)
            {
                actionButtonText.text = "Not achieved";
                actionButtonText.color = TextGray;
            }
            if (buttonNotificationDot != null)
            {
                buttonNotificationDot.SetActive(false);
            }
            return;
        }

        if (state == AchievementState.Claimed)
        {
            // Trạng thái đã nhận -> Nút "Obtained" tối
            if (actionButton != null)
            {
                actionButton.interactable = false;
            }
            if (actionButtonImage != null)
            {
                actionButtonImage.color = ObtainedButtonColor;
            }
            if (actionButtonText != null)
            {
                actionButtonText.text = "Obtained";
                actionButtonText.color = TextGray;
            }
            if (buttonNotificationDot != null)
            {
                buttonNotificationDot.SetActive(false);
            }
        }
    }

    private void OnActionButtonClicked()
    {
        if (isClaimable && onClaimCallback != null && !string.IsNullOrWhiteSpace(achievementId))
        {
            if (actionButton != null)
            {
                if (isActiveAndEnabled) StartCoroutine(PunchScaleRoutine(actionButton.transform));
                actionButton.interactable = false;
            }
            onClaimCallback.Invoke(achievementId);
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
        rt.sizeDelta = new Vector2(85f, 85f);
        badge.GetComponent<Image>().color = new Color32(11, 45, 60, 240);

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(badge.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(0f, 10f);
        iconRt.sizeDelta = new Vector2(46f, 46f);
        iconObj.GetComponent<Image>().preserveAspect = true;

        GameObject textObj = new GameObject("AmountText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(badge.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0f, 0f);
        textRt.anchorMax = new Vector2(1f, 0.35f);
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        TMP_Text txt = textObj.GetComponent<TMP_Text>();
        txt.fontSize = 18f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;

        return badge;
    }

    public void SetReferencesForBuilder(
        TMP_Text title,
        TMP_Text progress,
        Image fill,
        Image barBg,
        Transform rewardsTr,
        Button btn,
        Image btnImg,
        TMP_Text btnTxt,
        GameObject dot,
        Image border,
        Image bg)
    {
        titleText = title;
        progressText = progress;
        progressFillImage = fill;
        progressBgImage = barBg;
        rewardsContainer = rewardsTr;
        actionButton = btn;
        actionButtonImage = btnImg;
        actionButtonText = btnTxt;
        buttonNotificationDot = dot;
        itemBorder = border;
        itemBackground = bg;
    }
}
