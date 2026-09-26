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

    [Header("Button & Banner Sprites")]
    [SerializeField] private Sprite btnGetSprite;
    [SerializeField] private Sprite btnNotAchievedSprite;
    [SerializeField] private Sprite btnObtainedSprite;
    [SerializeField] private Sprite cardBannerSprite;
    [SerializeField] private Sprite progressBarBgSprite;
    [SerializeField] private Sprite progressBarFillSprite;

    private string achievementId;
    private Action<string> onClaimCallback;
    private bool isClaimable = false;

    // Button Colors matching Image 1
    private static readonly Color GetButtonColor = new Color32(56, 189, 248, 255);        // Cyan bright
    private static readonly Color GetBorderColor = new Color32(94, 213, 205, 255);        // Cyan border
    private static readonly Color InProgressButtonColor = new Color32(23, 68, 88, 255);   // Dark teal matching Image 1
    private static readonly Color InProgressBorderColor = new Color32(11, 35, 48, 255);   // Dark border
    private static readonly Color ObtainedButtonColor = new Color32(78, 140, 147, 255);  // Grayish teal matching Image 1
    private static readonly Color ObtainedBorderColor = new Color32(38, 77, 85, 255);   // Grayish border
    private static readonly Color TextWhite = new Color32(245, 255, 255, 255);
    private static readonly Color TextInProgress = new Color32(35, 95, 120, 255);         // Dim dark teal text matching Image 1
    private static readonly Color TextObtained = new Color32(35, 80, 95, 255);            // Dim dark cyan text matching Image 1

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

        EnsureSpritesLoaded();
    }

    public void EnsureSpritesLoaded()
    {
        if (btnGetSprite == null)
            btnGetSprite = Resources.Load<Sprite>("UI/Reward/Extracted/Btn_Get");
        if (btnNotAchievedSprite == null)
            btnNotAchievedSprite = Resources.Load<Sprite>("UI/Reward/Extracted/Btn_Not_Achieved");
        if (btnObtainedSprite == null)
            btnObtainedSprite = Resources.Load<Sprite>("UI/Reward/Extracted/Btn_Obtained");
        if (cardBannerSprite == null)
            cardBannerSprite = Resources.Load<Sprite>("UI/Reward/Extracted/Row_Banner_Achievement");
        if (progressBarBgSprite == null)
            progressBarBgSprite = Resources.Load<Sprite>("UI/Reward/Extracted/Progress_Bar_Bg");
        if (progressBarFillSprite == null)
            progressBarFillSprite = Resources.Load<Sprite>("UI/Reward/Extracted/Progress_Bar_Fill");

#if UNITY_EDITOR
        if (btnGetSprite == null)
            btnGetSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Btn_Get.png");
        if (btnNotAchievedSprite == null)
            btnNotAchievedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Btn_Not_Achieved.png");
        if (btnObtainedSprite == null)
            btnObtainedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Btn_Obtained.png");
        if (cardBannerSprite == null)
            cardBannerSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Row_Banner_Achievement.png");
        if (progressBarBgSprite == null)
            progressBarBgSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Progress_Bar_Bg.png");
        if (progressBarFillSprite == null)
            progressBarFillSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Progress_Bar_Fill.png");

        if (btnGetSprite == null || btnNotAchievedSprite == null || cardBannerSprite == null)
        {
            var sps = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/Reward/nút màn achievements.png");
            if (sps != null)
            {
                foreach (var obj in sps)
                {
                    if (obj is Sprite sp)
                    {
                        if (btnGetSprite == null && sp.name == "Btn_Get") btnGetSprite = sp;
                        if (btnNotAchievedSprite == null && sp.name == "Btn_Not_Achieved") btnNotAchievedSprite = sp;
                        if (cardBannerSprite == null && sp.name == "Row_Banner_Achievement") cardBannerSprite = sp;
                        if (progressBarBgSprite == null && sp.name == "Progress_Bar_Bg") progressBarBgSprite = sp;
                        if (progressBarFillSprite == null && sp.name == "Progress_Bar_Fill") progressBarFillSprite = sp;
                    }
                }
            }
        }

        if (btnObtainedSprite == null || btnGetSprite == null)
        {
            var sps = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/Reward/nút daily login.png");
            if (sps != null)
            {
                foreach (var obj in sps)
                {
                    if (obj is Sprite sp)
                    {
                        if (btnObtainedSprite == null && sp.name == "Btn_Obtained") btnObtainedSprite = sp;
                        if (btnGetSprite == null && sp.name == "Btn_Get") btnGetSprite = sp;
                    }
                }
            }
        }
#endif
    }

    public void SetSprites(
        Sprite getSp,
        Sprite notSp,
        Sprite obSp,
        Sprite bannerSp = null,
        Sprite barBgSp = null,
        Sprite barFillSp = null)
    {
        if (getSp != null) btnGetSprite = getSp;
        if (notSp != null) btnNotAchievedSprite = notSp;
        if (obSp != null) btnObtainedSprite = obSp;
        if (bannerSp != null) cardBannerSprite = bannerSp;
        if (barBgSp != null) progressBarBgSprite = barBgSp;
        if (barFillSp != null) progressBarFillSprite = barFillSp;

        ApplyStaticVisualSprites();
    }

    public void ApplyStaticVisualSprites()
    {
        if (cardBannerSprite != null)
        {
            if (itemBackground != null)
            {
                itemBackground.sprite = cardBannerSprite;
                itemBackground.color = Color.white;
            }
            if (itemBorder != null)
            {
                itemBorder.color = itemBorder == itemBackground ? Color.white : Color.clear;
            }
        }
        if (progressBarBgSprite != null && progressBgImage != null)
        {
            progressBgImage.sprite = progressBarBgSprite;
            progressBgImage.color = Color.white;
        }
        if (progressBarFillSprite != null && progressFillImage != null)
        {
            progressFillImage.sprite = progressBarFillSprite;
            progressFillImage.color = Color.white;
        }

        SanitizeRewardsLayout();
    }

    /// <summary>
    /// Đồng bộ và bảo vệ Layout cho RewardsContainer:
    /// - childControlWidth = false, childControlHeight = false: Không ghi đè kích thước sizeDelta của badge do user chỉnh trong Editor.
    /// - childForceExpandWidth = false, childForceExpandHeight = false: Không ép dãn badge.
    /// - Spacing không âm (< 0 -> reset về 18f).
    /// - Tắt wordWrapping cho toàn bộ text AmountText để số lượng không bao giờ bị ngắt dòng thành hàng dọc.
    /// - Bảo toàn 100% RectTransform do user chỉnh trong Edit Mode.
    /// </summary>
    public void SanitizeRewardsLayout()
    {
        if (rewardsContainer == null) return;

        if (rewardsContainer.TryGetComponent<HorizontalLayoutGroup>(out var hlg))
        {
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            if (hlg.spacing < 0f)
            {
                hlg.spacing = 18f;
            }
        }

        for (int i = 0; i < rewardsContainer.childCount; i++)
        {
            Transform child = rewardsContainer.GetChild(i);
            if (child == null) continue;

            if (child.TryGetComponent<Image>(out var badgeImg))
            {
                if (badgeImg.color.a != 0f)
                {
                    badgeImg.color = new Color(badgeImg.color.r, badgeImg.color.g, badgeImg.color.b, 0f);
                }
            }

            TMP_Text amountTxt = child.Find("AmountText")?.GetComponent<TMP_Text>()
                ?? child.GetComponentInChildren<TMP_Text>();
            if (amountTxt != null)
            {
                amountTxt.enableWordWrapping = false;
                amountTxt.overflowMode = TextOverflowModes.Overflow;
                amountTxt.alignment = TextAlignmentOptions.Center;
            }

            Image iconImg = child.Find("Icon")?.GetComponent<Image>()
                ?? child.GetComponentInChildren<Image>();
            if (iconImg != null)
            {
                iconImg.preserveAspect = true;
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null)
            {
                EnsureUIReferences();
                SanitizeRewardsLayout();
                ApplyStaticVisualSprites();
            }
        };
    }

    [ContextMenu("Sync Layout & Visuals")]
    public void ContextMenuSync()
    {
        EnsureUIReferences();
        EnsureSpritesLoaded();
        SanitizeRewardsLayout();
        ApplyStaticVisualSprites();
    }
#endif

    public TMP_Text ActionButtonText => actionButtonText;
    public Button ActionButton => actionButton;

    private void Awake()
    {
        EnsureUIReferences();
        SanitizeRewardsLayout();
        ApplyStaticVisualSprites();
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
        SanitizeRewardsLayout();
        ApplyStaticVisualSprites();
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

        SanitizeRewardsLayout();

        // Tái sử dụng các badge đã được thiết lập sẵn trong scene/hierarchy để giữ nguyên 100% Transform (X, Y, size, anchors)
        int rewardIndex = 0;
        int childCount = rewardsContainer.childCount;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = rewardsContainer.GetChild(i);
            if (rewardBadgePrefab != null && child.gameObject == rewardBadgePrefab)
            {
                child.gameObject.SetActive(false);
                continue;
            }

            if (rewardIndex < rewards.Length)
            {
                child.gameObject.SetActive(true);
                RewardData reward = rewards[rewardIndex];

                if (child.TryGetComponent<Image>(out var badgeImg))
                {
                    Color c = badgeImg.color;
                    if (c.a != 0f)
                    {
                        badgeImg.color = new Color(c.r, c.g, c.b, 0f);
                    }
                }

                Image iconImg = child.Find("Icon")?.GetComponent<Image>()
                    ?? child.GetComponentInChildren<Image>();
                TMP_Text amountTxt = child.Find("AmountText")?.GetComponent<TMP_Text>()
                    ?? child.GetComponentInChildren<TMP_Text>();

                if (iconImg != null)
                {
                    Sprite icon = reward.customIcon != null ? reward.customIcon : (iconResolver != null ? iconResolver(reward.type) : null);
                    if (icon != null)
                    {
                        iconImg.sprite = icon;
                        iconImg.enabled = true;
                    }
                    iconImg.preserveAspect = true;
                }

                if (amountTxt != null)
                {
                    amountTxt.enableWordWrapping = false;
                    amountTxt.overflowMode = TextOverflowModes.Overflow;
                    amountTxt.alignment = TextAlignmentOptions.Center;
                    amountTxt.text = $"x{reward.amount}";
                }

                rewardIndex++;
            }
            else
            {
                child.gameObject.SetActive(false);
            }
        }

        // Nếu số lượng phần thưởng nhiều hơn số badge sẵn có, nhân bản từ badge đầu tiên
        while (rewardIndex < rewards.Length)
        {
            RewardData reward = rewards[rewardIndex];
            GameObject badgeObj;
            if (rewardsContainer.childCount > 0)
            {
                badgeObj = Instantiate(rewardsContainer.GetChild(0).gameObject, rewardsContainer);
                badgeObj.name = $"RewardBadge_{rewardIndex}";
            }
            else if (rewardBadgePrefab != null)
            {
                badgeObj = Instantiate(rewardBadgePrefab, rewardsContainer);
            }
            else
            {
                badgeObj = CreateFallbackRewardBadge(rewardsContainer);
            }
            badgeObj.SetActive(true);

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
                iconImg.preserveAspect = true;
            }

            if (amountTxt != null)
            {
                amountTxt.enableWordWrapping = false;
                amountTxt.overflowMode = TextOverflowModes.Overflow;
                amountTxt.alignment = TextAlignmentOptions.Center;
                amountTxt.text = $"x{reward.amount}";
            }

            rewardIndex++;
        }
    }

    public void UpdateState(AchievementState state)
    {
        EnsureUIReferences();
        EnsureSpritesLoaded();
        ApplyStaticVisualSprites();
        isClaimable = (state == AchievementState.Completed);

        Image border = actionButton != null ? actionButton.GetComponent<Image>() : null;

        if (state == AchievementState.Completed)
        {
            // Trạng thái hoàn thành -> Nút "Get"
            if (actionButton != null)
            {
                actionButton.interactable = true;
                var cols = actionButton.colors;
                cols.disabledColor = Color.white;
                actionButton.colors = cols;
            }

            if (actionButtonImage != null)
            {
                if (btnGetSprite != null)
                {
                    actionButtonImage.sprite = btnGetSprite;
                    actionButtonImage.preserveAspect = true;
                    actionButtonImage.color = Color.white;
                }
                else
                {
                    actionButtonImage.color = GetButtonColor;
                }
            }
            ApplyReferenceButtonSize(btnGetSprite);
            if (border != null)
            {
                border.color = (btnGetSprite != null) ? Color.clear : GetBorderColor;
            }
            if (actionButtonText != null)
            {
                actionButtonText.gameObject.SetActive(btnGetSprite == null);
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
            // Trạng thái đang thực hiện -> Nút "Not Achieved" / "Get" tối màu
            if (actionButton != null)
            {
                actionButton.interactable = false;
                var cols = actionButton.colors;
                cols.disabledColor = Color.white;
                actionButton.colors = cols;
            }

            Sprite inactiveSprite = btnNotAchievedSprite ?? btnGetSprite;
            if (actionButtonImage != null)
            {
                if (inactiveSprite != null)
                {
                    actionButtonImage.sprite = inactiveSprite;
                    actionButtonImage.preserveAspect = true;
                    actionButtonImage.color = (btnNotAchievedSprite != null) ? Color.white : new Color(1f, 1f, 1f, 0.5f);
                }
                else
                {
                    actionButtonImage.color = InProgressButtonColor;
                }
            }
            ApplyReferenceButtonSize(inactiveSprite);
            if (border != null)
            {
                border.color = (inactiveSprite != null) ? Color.clear : InProgressBorderColor;
            }
            if (actionButtonText != null)
            {
                actionButtonText.gameObject.SetActive(inactiveSprite == null);
                actionButtonText.text = "Get";
                actionButtonText.color = TextInProgress;
            }
            if (buttonNotificationDot != null)
            {
                buttonNotificationDot.SetActive(false);
            }
            return;
        }

        if (state == AchievementState.Claimed)
        {
            // Trạng thái đã nhận -> Nút "Obtained"
            if (actionButton != null)
            {
                actionButton.interactable = false;
                var cols = actionButton.colors;
                cols.disabledColor = Color.white;
                actionButton.colors = cols;
            }

            if (actionButtonImage != null)
            {
                if (btnObtainedSprite != null)
                {
                    actionButtonImage.sprite = btnObtainedSprite;
                    actionButtonImage.preserveAspect = true;
                    actionButtonImage.color = Color.white;
                }
                else
                {
                    actionButtonImage.color = ObtainedButtonColor;
                }
            }
            ApplyReferenceButtonSize(btnObtainedSprite);
            if (border != null)
            {
                border.color = (btnObtainedSprite != null) ? Color.clear : ObtainedBorderColor;
            }
            if (actionButtonText != null)
            {
                actionButtonText.gameObject.SetActive(btnObtainedSprite == null);
                actionButtonText.text = "Obtained";
                actionButtonText.color = TextObtained;
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
        rt.sizeDelta = new Vector2(82f, 84f);
        Image badgeImg = badge.GetComponent<Image>();
        badgeImg.color = new Color32(11, 45, 60, 0);
        badgeImg.raycastTarget = false;

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(badge.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(0f, 8f);
        iconRt.sizeDelta = new Vector2(82f, 84f);
        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        GameObject textObj = new GameObject("AmountText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(badge.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0f, 0f);
        textRt.anchorMax = new Vector2(1f, 0.38f);
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        TMP_Text txt = textObj.GetComponent<TMP_Text>();
        txt.fontSize = 20f;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        txt.raycastTarget = false;
        txt.enableWordWrapping = false;
        txt.overflowMode = TextOverflowModes.Overflow;

        return badge;
    }

    private void ApplyReferenceButtonSize(Sprite sprite)
    {
        if (actionButton == null) return;

        RectTransform rect = actionButton.transform as RectTransform;
        if (rect != null)
        {
            rect.sizeDelta = sprite != null ? sprite.rect.size * 0.5f : new Vector2(174f, 76f);
        }
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
