using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller trung tâm quản lý toàn bộ Popup Thưởng (RewardPopup) gồm 2 Tab:
/// 1. Daily Login Reward
/// 2. Achievements
/// 
/// Chức năng:
/// - Chuyển đổi qua lại giữa 2 Tab mượt mà (không destroy/recreate panel mỗi lần chuyển).
/// - Tab active sáng màu cyan/turquoise, nổi lên phía trước giống ảnh mẫu.
/// - Tab inactive màu tối hơn, panel tương ứng ẩn đi.
/// - Quản lý Notification Badge (dấu chấm đỏ) trên từng Tab và trên nút mở popup ngoài TopBar.
/// - Đóng/Mở popup linh hoạt với Dim Background và nút Close.
/// </summary>
public class RewardPopupController : MonoBehaviour
{
    private static RewardPopupController instance;
    public static RewardPopupController Instance => instance;

    [Header("Popup Root & Background")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button dimBackgroundButton;
    [SerializeField] private Button closeButton;

    [Header("Tab Buttons & Headers")]
    [SerializeField] private Button dailyTabButton;
    [SerializeField] private Image dailyTabBg;
    [SerializeField] private TMP_Text dailyTabText;
    [SerializeField] private GameObject dailyTabBadge;

    [SerializeField] private Button achievementTabButton;
    [SerializeField] private Image achievementTabBg;
    [SerializeField] private TMP_Text achievementTabText;
    [SerializeField] private GameObject achievementTabBadge;

    [Header("Panels")]
    [SerializeField] private GameObject dailyLoginPanel;
    [SerializeField] private GameObject achievementPanel;

    [SerializeField] private DailyLoginPanelUI dailyPanelUI;
    [SerializeField] private AchievementPanelUI achievementPanelUI;

    [Header("UX Motion Settings (Matching BottomNavigation)")]
    [SerializeField] private float tabSelectedYOffset = 6f;
    [SerializeField] private float tabPressYOffset = -2.5f;
    [SerializeField] private float tabPressScaleX = 1.03f;
    [SerializeField] private float tabPressScaleY = 0.95f;
    [SerializeField] private float tabPressDuration = 0.05f;
    [SerializeField] private float panelSlideDistance = 50f;
    [SerializeField] private float panelTransitionDuration = 0.18f;

    // Visual tab colors matching Image 1 & 2
    private static readonly Color ActiveTabBgColor = new Color32(64, 218, 210, 255);    // Bright cyan/turquoise
    private static readonly Color InactiveTabBgColor = new Color32(20, 70, 85, 255);   // Dark teal
    private static readonly Color ActiveTabTextColor = new Color32(255, 255, 255, 255); // White with dark outline
    private static readonly Color InactiveTabTextColor = new Color32(140, 200, 205, 255); // Muted teal

    private int currentTab = 0; // 0 = Daily, 1 = Achievements

    private RectTransform windowRect;
    private CanvasGroup windowCanvasGroup;
    private CanvasGroup dimCanvasGroup;
    private RectTransform dailyTabRect;
    private RectTransform achTabRect;
    private RectTransform dailyPanelRect;
    private RectTransform achPanelRect;
    private CanvasGroup dailyPanelCanvasGroup;
    private CanvasGroup achPanelCanvasGroup;
    private Coroutine popupOpenCloseRoutine;
    private Coroutine tabSwitchRoutine;

    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;
    public int CurrentTab => currentTab;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        if (popupRoot == null)
        {
            popupRoot = gameObject;
        }

        EnsureAnimationComponentsCached();
        SetupListeners();
    }

    private void Start()
    {
        UpdateNotificationBadges();
    }

    private void OnEnable()
    {
        DailyLoginManager.OnDailyLoginStateChanged += HandleNotificationUpdate;
        DailyLoginManager.OnDailyRewardClaimed += HandleDailyRewardClaimed;
        AchievementManager.OnAchievementUpdated += HandleNotificationUpdate;
        AchievementManager.OnAchievementClaimed += HandleAchievementClaimed;

        UpdateNotificationBadges();
    }

    private void OnDisable()
    {
        DailyLoginManager.OnDailyLoginStateChanged -= HandleNotificationUpdate;
        DailyLoginManager.OnDailyRewardClaimed -= HandleDailyRewardClaimed;
        AchievementManager.OnAchievementUpdated -= HandleNotificationUpdate;
        AchievementManager.OnAchievementClaimed -= HandleAchievementClaimed;

        if (popupOpenCloseRoutine != null)
        {
            StopCoroutine(popupOpenCloseRoutine);
            popupOpenCloseRoutine = null;
        }
        if (tabSwitchRoutine != null)
        {
            StopCoroutine(tabSwitchRoutine);
            tabSwitchRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void SetupListeners()
    {
        if (dailyTabButton != null)
        {
            dailyTabButton.onClick.RemoveListener(OnDailyTabClicked);
            dailyTabButton.onClick.AddListener(OnDailyTabClicked);
        }

        if (achievementTabButton != null)
        {
            achievementTabButton.onClick.RemoveListener(OnAchievementTabClicked);
            achievementTabButton.onClick.AddListener(OnAchievementTabClicked);
        }

        if (dimBackgroundButton != null)
        {
            dimBackgroundButton.onClick.RemoveListener(ClosePopup);
            dimBackgroundButton.onClick.AddListener(ClosePopup);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePopup);
            closeButton.onClick.AddListener(ClosePopup);
        }
    }

    private void EnsureAnimationComponentsCached()
    {
        if (popupRoot != null)
        {
            if (dimBackgroundButton != null && dimCanvasGroup == null)
            {
                dimCanvasGroup = dimBackgroundButton.GetComponent<CanvasGroup>() ?? dimBackgroundButton.gameObject.AddComponent<CanvasGroup>();
            }

            if (windowRect == null)
            {
                Transform winTr = popupRoot.transform.Find("Window");
                if (winTr != null)
                {
                    windowRect = winTr as RectTransform;
                    windowCanvasGroup = winTr.GetComponent<CanvasGroup>() ?? winTr.gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        if (dailyTabButton != null && dailyTabRect == null) dailyTabRect = dailyTabButton.transform as RectTransform;
        if (achievementTabButton != null && achTabRect == null) achTabRect = achievementTabButton.transform as RectTransform;

        if (dailyLoginPanel != null)
        {
            if (dailyPanelRect == null) dailyPanelRect = dailyLoginPanel.transform as RectTransform;
            if (dailyPanelCanvasGroup == null) dailyPanelCanvasGroup = dailyLoginPanel.GetComponent<CanvasGroup>() ?? dailyLoginPanel.AddComponent<CanvasGroup>();
        }

        if (achievementPanel != null)
        {
            if (achPanelRect == null) achPanelRect = achievementPanel.transform as RectTransform;
            if (achPanelCanvasGroup == null) achPanelCanvasGroup = achievementPanel.GetComponent<CanvasGroup>() ?? achievementPanel.AddComponent<CanvasGroup>();
        }
    }

    // =========================================================================
    // POPUP VISIBILITY & TAB NAVIGATION
    // =========================================================================

    public void OpenPopup(int defaultTab = 0)
    {
        EnsureAnimationComponentsCached();

        if (popupOpenCloseRoutine != null) StopCoroutine(popupOpenCloseRoutine);
        if (popupRoot != null) popupRoot.SetActive(true);

        SwitchTab(defaultTab, animated: false);
        UpdateNotificationBadges();

        if (isActiveAndEnabled)
        {
            popupOpenCloseRoutine = StartCoroutine(AnimateOpenPopupRoutine());
        }
    }

    public void ClosePopup()
    {
        EnsureAnimationComponentsCached();

        if (popupOpenCloseRoutine != null) StopCoroutine(popupOpenCloseRoutine);

        if (isActiveAndEnabled && popupRoot != null && popupRoot.activeInHierarchy)
        {
            popupOpenCloseRoutine = StartCoroutine(AnimateClosePopupRoutine());
        }
        else if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }

        UpdateNotificationBadges();
    }

    public void TogglePopup()
    {
        if (IsOpen)
        {
            ClosePopup();
        }
        else
        {
            OpenPopup(0);
        }
    }

    private void OnDailyTabClicked()
    {
        SwitchTab(0, animated: true);
    }

    private void OnAchievementTabClicked()
    {
        SwitchTab(1, animated: true);
    }

    public void SwitchTab(int tabIndex, bool animated = true)
    {
        if (tabIndex == currentTab && animated) return;
        EnsureAnimationComponentsCached();

        int previousTab = currentTab;
        currentTab = tabIndex;

        if (tabSwitchRoutine != null)
        {
            StopCoroutine(tabSwitchRoutine);
            tabSwitchRoutine = null;
        }

        if (animated && isActiveAndEnabled && popupRoot != null && popupRoot.activeInHierarchy)
        {
            tabSwitchRoutine = StartCoroutine(AnimateTabSwitchRoutine(previousTab, currentTab));
        }
        else
        {
            ApplyInstantTabState(currentTab);
        }

        UpdateNotificationBadges();
    }

    private void ApplyInstantTabState(int tabIndex)
    {
        bool isDaily = (tabIndex == 0);

        if (dailyLoginPanel != null)
        {
            dailyLoginPanel.SetActive(isDaily);
            if (isDaily && dailyPanelUI != null) dailyPanelUI.RefreshAll();
            if (dailyPanelRect != null) dailyPanelRect.anchoredPosition = Vector2.zero;
            if (dailyPanelCanvasGroup != null) dailyPanelCanvasGroup.alpha = 1f;
        }

        if (achievementPanel != null)
        {
            achievementPanel.SetActive(!isDaily);
            if (!isDaily && achievementPanelUI != null) achievementPanelUI.RefreshAll();
            if (achPanelRect != null) achPanelRect.anchoredPosition = Vector2.zero;
            if (achPanelCanvasGroup != null) achPanelCanvasGroup.alpha = 1f;
        }

        ApplyTabHeaderColors(tabIndex);

        if (dailyTabRect != null)
        {
            dailyTabRect.anchoredPosition = new Vector2(dailyTabRect.anchoredPosition.x, isDaily ? tabSelectedYOffset : 0f);
            dailyTabRect.localScale = Vector3.one;
        }
        if (achTabRect != null)
        {
            achTabRect.anchoredPosition = new Vector2(achTabRect.anchoredPosition.x, !isDaily ? tabSelectedYOffset : 0f);
            achTabRect.localScale = Vector3.one;
        }
    }

    private void ApplyTabHeaderColors(int tabIndex)
    {
        bool isDaily = (tabIndex == 0);

        if (dailyTabBg != null) dailyTabBg.color = isDaily ? ActiveTabBgColor : InactiveTabBgColor;
        if (dailyTabText != null)
        {
            dailyTabText.color = isDaily ? ActiveTabTextColor : InactiveTabTextColor;
            dailyTabText.fontStyle = isDaily ? FontStyles.Bold : FontStyles.Normal;
        }

        if (achievementTabBg != null) achievementTabBg.color = !isDaily ? ActiveTabBgColor : InactiveTabBgColor;
        if (achievementTabText != null)
        {
            achievementTabText.color = !isDaily ? ActiveTabTextColor : InactiveTabTextColor;
            achievementTabText.fontStyle = !isDaily ? FontStyles.Bold : FontStyles.Normal;
        }

        if (isDaily && dailyTabButton != null) dailyTabButton.transform.SetAsLastSibling();
        else if (!isDaily && achievementTabButton != null) achievementTabButton.transform.SetAsLastSibling();
    }

    private IEnumerator AnimateOpenPopupRoutine()
    {
        if (dimCanvasGroup != null) dimCanvasGroup.alpha = 0f;
        if (windowCanvasGroup != null) windowCanvasGroup.alpha = 0f;
        if (windowRect != null) windowRect.localScale = new Vector3(0.85f, 0.85f, 1f);

        float duration = 0.22f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (dimCanvasGroup != null) dimCanvasGroup.alpha = Mathf.Clamp01(elapsed / 0.14f);

            // Pop 3-phase: 0.85 -> 1.06 (60%) -> 0.98 (85%) -> 1.00 (100%)
            float scale;
            if (t < 0.6f)
            {
                float subT = t / 0.6f;
                scale = Mathf.Lerp(0.85f, 1.06f, 1f - (1f - subT) * (1f - subT));
            }
            else if (t < 0.85f)
            {
                float subT = (t - 0.6f) / 0.25f;
                scale = Mathf.Lerp(1.06f, 0.98f, subT * subT);
            }
            else
            {
                float subT = (t - 0.85f) / 0.15f;
                scale = Mathf.Lerp(0.98f, 1.00f, 1f - (1f - subT) * (1f - subT));
            }

            if (windowRect != null) windowRect.localScale = new Vector3(scale, scale, 1f);
            if (windowCanvasGroup != null) windowCanvasGroup.alpha = Mathf.Clamp01(elapsed / 0.10f);

            yield return null;
        }

        if (dimCanvasGroup != null) dimCanvasGroup.alpha = 1f;
        if (windowCanvasGroup != null) windowCanvasGroup.alpha = 1f;
        if (windowRect != null) windowRect.localScale = Vector3.one;
        popupOpenCloseRoutine = null;
    }

    private IEnumerator AnimateClosePopupRoutine()
    {
        float duration = 0.13f;
        float elapsed = 0f;
        Vector3 startScale = windowRect != null ? windowRect.localScale : Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = t * t;

            if (windowRect != null) windowRect.localScale = Vector3.Lerp(startScale, new Vector3(0.90f, 0.90f, 1f), ease);
            if (windowCanvasGroup != null) windowCanvasGroup.alpha = 1f - ease;
            if (dimCanvasGroup != null) dimCanvasGroup.alpha = 1f - ease;

            yield return null;
        }

        if (popupRoot != null) popupRoot.SetActive(false);
        if (windowRect != null) windowRect.localScale = Vector3.one;
        if (windowCanvasGroup != null) windowCanvasGroup.alpha = 1f;
        if (dimCanvasGroup != null) dimCanvasGroup.alpha = 1f;
        popupOpenCloseRoutine = null;
    }

    private IEnumerator AnimateTabSwitchRoutine(int prevTab, int nextTab)
    {
        bool isDaily = (nextTab == 0);
        RectTransform activeTabRect = isDaily ? dailyTabRect : achTabRect;
        RectTransform inactiveTabRect = isDaily ? achTabRect : dailyTabRect;

        // Layer 1 - Touch Squash (~0.05s)
        if (activeTabRect != null)
        {
            Vector3 startScale = activeTabRect.localScale;
            Vector3 pressedScale = new Vector3(tabPressScaleX, tabPressScaleY, 1f);
            Vector2 startPos = activeTabRect.anchoredPosition;
            Vector2 pressedPos = new Vector2(startPos.x, tabPressYOffset);

            float elapsed = 0f;
            while (elapsed < tabPressDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / tabPressDuration);
                activeTabRect.localScale = Vector3.LerpUnclamped(startScale, pressedScale, t);
                activeTabRect.anchoredPosition = Vector2.LerpUnclamped(startPos, pressedPos, t);
                yield return null;
            }
        }

        ApplyTabHeaderColors(nextTab);

        GameObject enterPanel = isDaily ? dailyLoginPanel : achievementPanel;
        GameObject exitPanel = isDaily ? achievementPanel : dailyLoginPanel;
        CanvasGroup enterCg = isDaily ? dailyPanelCanvasGroup : achPanelCanvasGroup;
        CanvasGroup exitCg = isDaily ? achPanelCanvasGroup : dailyPanelCanvasGroup;
        RectTransform enterRect = isDaily ? dailyPanelRect : achPanelRect;
        RectTransform exitRect = isDaily ? achPanelRect : dailyPanelRect;

        if (isDaily && dailyPanelUI != null) dailyPanelUI.RefreshAll();
        if (!isDaily && achievementPanelUI != null) achievementPanelUI.RefreshAll();

        if (enterPanel != null) enterPanel.SetActive(true);
        if (exitPanel != null) exitPanel.SetActive(true);

        float dir = (nextTab > prevTab) ? 1f : -1f;
        Vector2 enterStartPos = new Vector2(dir * panelSlideDistance, 0f);
        Vector2 exitEndPos = new Vector2(-dir * panelSlideDistance, 0f);

        float transitionDuration = panelTransitionDuration;
        float elapsedTotal = 0f;

        while (elapsedTotal < transitionDuration)
        {
            elapsedTotal += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsedTotal / transitionDuration);
            float easeOut = 1f - (1f - normalized) * (1f - normalized);

            if (activeTabRect != null)
            {
                float tabY = Mathf.Lerp(tabPressYOffset, tabSelectedYOffset, easeOut);
                activeTabRect.anchoredPosition = new Vector2(activeTabRect.anchoredPosition.x, tabY);
                activeTabRect.localScale = Vector3.Lerp(new Vector3(tabPressScaleX, tabPressScaleY, 1f), Vector3.one, easeOut);
            }
            if (inactiveTabRect != null)
            {
                float inactY = Mathf.Lerp(inactiveTabRect.anchoredPosition.y, 0f, easeOut);
                inactiveTabRect.anchoredPosition = new Vector2(inactiveTabRect.anchoredPosition.x, inactY);
                inactiveTabRect.localScale = Vector3.Lerp(inactiveTabRect.localScale, Vector3.one, easeOut);
            }

            if (enterRect != null) enterRect.anchoredPosition = Vector2.LerpUnclamped(enterStartPos, Vector2.zero, easeOut);
            if (enterCg != null) enterCg.alpha = easeOut;

            if (exitRect != null) exitRect.anchoredPosition = Vector2.LerpUnclamped(Vector2.zero, exitEndPos, normalized);
            if (exitCg != null) exitCg.alpha = 1f - normalized;

            yield return null;
        }

        if (exitPanel != null) exitPanel.SetActive(false);
        if (enterRect != null) enterRect.anchoredPosition = Vector2.zero;
        if (enterCg != null) enterCg.alpha = 1f;
        if (exitRect != null) exitRect.anchoredPosition = Vector2.zero;
        if (exitCg != null) exitCg.alpha = 1f;

        if (activeTabRect != null) activeTabRect.anchoredPosition = new Vector2(activeTabRect.anchoredPosition.x, tabSelectedYOffset);
        if (inactiveTabRect != null) inactiveTabRect.anchoredPosition = new Vector2(inactiveTabRect.anchoredPosition.x, 0f);

        tabSwitchRoutine = null;
    }

    // =========================================================================
    // NOTIFICATION BADGES
    // =========================================================================

    public void UpdateNotificationBadges()
    {
        bool hasDailyReward = DailyLoginManager.Instance != null && DailyLoginManager.Instance.HasAnyClaimableReward();
        bool hasAchievementReward = AchievementManager.Instance != null && AchievementManager.Instance.HasAnyClaimableAchievement();

        if (dailyTabBadge != null)
        {
            dailyTabBadge.SetActive(hasDailyReward);
        }

        if (achievementTabBadge != null)
        {
            achievementTabBadge.SetActive(hasAchievementReward);
        }

        // Cập nhật dấu chấm đỏ trên TopBar nếu có TopBarCurrencyController
        TopBarCurrencyController topBar = FindObjectOfType<TopBarCurrencyController>();
        if (topBar != null)
        {
            bool hasAny = hasDailyReward || hasAchievementReward;
            topBar.SetNotificationBadgeVisible(hasAny);
        }
    }

    private void HandleNotificationUpdate()
    {
        UpdateNotificationBadges();
    }

    private void HandleDailyRewardClaimed(int _, RewardData[] __)
    {
        UpdateNotificationBadges();
    }

    private void HandleAchievementClaimed(AchievementDefinition _)
    {
        UpdateNotificationBadges();
    }

    public void SetReferencesForBuilder(
        GameObject root,
        Button dimBg,
        Button closeBtn,
        Button dTabBtn,
        Image dTabBg,
        TMP_Text dTabTxt,
        GameObject dTabBadge,
        Button aTabBtn,
        Image aTabBg,
        TMP_Text aTabTxt,
        GameObject aTabBadge,
        GameObject dPanel,
        GameObject aPanel,
        DailyLoginPanelUI dPanelUI,
        AchievementPanelUI aPanelUI)
    {
        popupRoot = root;
        dimBackgroundButton = dimBg;
        closeButton = closeBtn;
        dailyTabButton = dTabBtn;
        dailyTabBg = dTabBg;
        dailyTabText = dTabTxt;
        dailyTabBadge = dTabBadge;
        achievementTabButton = aTabBtn;
        achievementTabBg = aTabBg;
        achievementTabText = aTabTxt;
        achievementTabBadge = aTabBadge;
        dailyLoginPanel = dPanel;
        achievementPanel = aPanel;
        dailyPanelUI = dPanelUI;
        achievementPanelUI = aPanelUI;

        SetupListeners();
    }

    // =========================================================================
    // RUNTIME AUTO-INSTALLATION & FALLBACK POPUP BUILDER
    // =========================================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInstallInMainMenuScene()
    {
        if (instance != null) return;
        RewardPopupController found = FindObjectOfType<RewardPopupController>(true);
        if (found != null)
        {
            instance = found;
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            CreateRuntimePopup(canvas.transform as RectTransform);
        }
    }

    public static RewardPopupController CreateRuntimePopup(RectTransform canvasParent)
    {
        if (canvasParent == null) return null;

        RewardPopupController existing = canvasParent.GetComponentInChildren<RewardPopupController>(true);
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (defaultFont == null)
        {
            TMP_Text sampleText = canvasParent.GetComponentInChildren<TMP_Text>(true);
            if (sampleText != null) defaultFont = sampleText.font;
        }

        GameObject popupObj = new GameObject("RewardPopup", typeof(RectTransform));
        popupObj.transform.SetParent(canvasParent, false);
        RectTransform popupRect = popupObj.GetComponent<RectTransform>();
        StretchRect(popupRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Dim Background Button
        GameObject dimObj = CreateRuntimeRect("DimBackground", popupObj.transform);
        StretchRect(dimObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image dimImg = dimObj.AddComponent<Image>();
        dimImg.color = new Color32(4, 10, 16, 215);
        Button dimBtn = dimObj.AddComponent<Button>();

        // Window Frame
        GameObject windowObj = CreateRuntimeFrame("Window", popupObj.transform, new Color32(11, 35, 55, 250), new Color32(94, 213, 205, 255), out _);
        RectTransform windowRect = windowObj.GetComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.anchoredPosition = new Vector2(0f, -40f);
        windowRect.sizeDelta = new Vector2(1000f, 1400f);

        // Tabs Header
        RectTransform tabsHeader = CreateRuntimeRect("Tabs", windowObj.transform).GetComponent<RectTransform>();
        tabsHeader.anchorMin = new Vector2(0f, 1f);
        tabsHeader.anchorMax = new Vector2(1f, 1f);
        tabsHeader.pivot = new Vector2(0.5f, 0f);
        tabsHeader.anchoredPosition = new Vector2(0f, -2f);
        tabsHeader.sizeDelta = new Vector2(-30f, 85f);

        // Daily Login Tab (Left half)
        GameObject dailyTabObj = CreateRuntimeTabButton("DailyLoginTab", tabsHeader, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(-6f, 0f), "Daily Login Reward", 32f, ActiveTabBgColor, new Color32(94, 213, 205, 255), defaultFont, out Image dailyTabBg, out TMP_Text dailyTabTxt, out GameObject dailyTabDot);
        Button dailyTabBtn = dailyTabObj.GetComponent<Button>();

        // Achievements Tab (Right half)
        GameObject achTabObj = CreateRuntimeTabButton("AchievementTab", tabsHeader, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(6f, 0f), new Vector2(0f, 0f), "Achievements", 32f, InactiveTabBgColor, new Color32(94, 213, 205, 255), defaultFont, out Image achTabBg, out TMP_Text achTabTxt, out GameObject achTabDot);
        Button achTabBtn = achTabObj.GetComponent<Button>();

        // Panels
        GameObject dailyPanelObj = BuildRuntimeDailyLoginPanel(windowObj.transform, defaultFont, out DailyLoginPanelUI dailyPanelUI);
        GameObject achPanelObj = BuildRuntimeAchievementPanel(windowObj.transform, defaultFont, out AchievementPanelUI achPanelUI);

        RewardPopupController popupCtrl = popupObj.AddComponent<RewardPopupController>();
        popupCtrl.SetReferencesForBuilder(
            popupObj, dimBtn, null,
            dailyTabBtn, dailyTabBg, dailyTabTxt, dailyTabDot,
            achTabBtn, achTabBg, achTabTxt, achTabDot,
            dailyPanelObj, achPanelObj,
            dailyPanelUI, achPanelUI
        );

        popupCtrl.SwitchTab(0);
        popupObj.SetActive(false);
        instance = popupCtrl;

        return popupCtrl;
    }

    private static GameObject BuildRuntimeDailyLoginPanel(Transform parent, TMP_FontAsset fontAsset, out DailyLoginPanelUI panelUI)
    {
        GameObject panelObj = CreateRuntimeRect("DailyLoginPanel", parent);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        StretchRect(panelRect, Vector2.zero, Vector2.one, new Vector2(16f, 20f), new Vector2(-16f, -30f));

        ScrollRect scroll = panelObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 25f;

        RectTransform viewport = CreateRuntimeRect("Viewport", panelObj.transform).GetComponent<RectTransform>();
        StretchRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<RectMask2D>();
        scroll.viewport = viewport;

        RectTransform content = CreateRuntimeRect("Content", viewport).GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 1200f);
        scroll.content = content;

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 16f;
        layout.padding = new RectOffset(10, 10, 15, 15);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        DailyLoginItemUI[] items = new DailyLoginItemUI[7];
        for (int i = 0; i < 7; i++)
        {
            items[i] = CreateRuntimeDailyItem(content, i + 1, fontAsset);
        }

        panelUI = panelObj.AddComponent<DailyLoginPanelUI>();
        panelUI.SetReferencesForBuilder(scroll, content, items, null, null, null);

        return panelObj;
    }

    private static DailyLoginItemUI CreateRuntimeDailyItem(Transform parent, int dayIndex, TMP_FontAsset fontAsset)
    {
        GameObject itemObj = CreateRuntimeFrame($"Day{dayIndex:00}", parent, new Color32(14, 48, 68, 255), new Color32(64, 180, 195, 255), out Image bg);
        RectTransform rect = itemObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(920f, 150f);
        LayoutElement le = itemObj.AddComponent<LayoutElement>();
        le.preferredHeight = 150f;
        le.minHeight = 150f;

        CanvasGroup cg = itemObj.AddComponent<CanvasGroup>();
        Image border = itemObj.GetComponent<Image>();

        // Day Header
        RectTransform dayHeader = CreateRuntimeRect("DayHeader", itemObj.transform).GetComponent<RectTransform>();
        dayHeader.anchorMin = new Vector2(0f, 0.5f);
        dayHeader.anchorMax = new Vector2(0f, 0.5f);
        dayHeader.pivot = new Vector2(0.5f, 0.5f);
        dayHeader.anchoredPosition = new Vector2(80f, 0f);
        dayHeader.sizeDelta = new Vector2(120f, 120f);

        TMP_Text dayLabel = CreateRuntimeText("DayLabel", dayHeader, "DAY", 26f, new Color32(160, 180, 195, 255), TextAlignmentOptions.Center, fontAsset);
        dayLabel.rectTransform.anchoredPosition = new Vector2(0f, 25f);
        dayLabel.rectTransform.sizeDelta = new Vector2(100f, 35f);

        TMP_Text dayNumber = CreateRuntimeText("DayNumber", dayHeader, $"{dayIndex:00}", 48f, new Color32(255, 190, 72, 255), TextAlignmentOptions.Center, fontAsset);
        dayNumber.rectTransform.anchoredPosition = new Vector2(0f, -20f);
        dayNumber.rectTransform.sizeDelta = new Vector2(100f, 55f);

        // Rewards Container
        RectTransform rewardsTr = CreateRuntimeRect("RewardsContainer", itemObj.transform).GetComponent<RectTransform>();
        rewardsTr.anchorMin = new Vector2(0f, 0.5f);
        rewardsTr.anchorMax = new Vector2(1f, 0.5f);
        rewardsTr.pivot = new Vector2(0f, 0.5f);
        rewardsTr.anchoredPosition = new Vector2(160f, 0f);
        rewardsTr.sizeDelta = new Vector2(-460f, 120f);

        HorizontalLayoutGroup rLayout = rewardsTr.gameObject.AddComponent<HorizontalLayoutGroup>();
        rLayout.spacing = 14f;
        rLayout.childAlignment = TextAnchor.MiddleLeft;
        rLayout.childControlWidth = false;
        rLayout.childControlHeight = false;

        // State Right
        RectTransform stateRight = CreateRuntimeRect("StateRight", itemObj.transform).GetComponent<RectTransform>();
        stateRight.anchorMin = new Vector2(1f, 0.5f);
        stateRight.anchorMax = new Vector2(1f, 0.5f);
        stateRight.pivot = new Vector2(1f, 0.5f);
        stateRight.anchoredPosition = new Vector2(-25f, 0f);
        stateRight.sizeDelta = new Vector2(280f, 120f);

        GameObject getBtnObj = CreateRuntimeButton("ClaimButton", stateRight, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-120f, -42.5f), new Vector2(120f, 42.5f), "Get", 36f, new Color32(56, 189, 248, 255), new Color32(94, 213, 205, 255), fontAsset, out _);
        Button claimBtn = getBtnObj.GetComponent<Button>();
        TMP_Text claimBtnTxt = getBtnObj.transform.Find("Label")?.GetComponent<TMP_Text>();

        GameObject obtainedRoot = CreateRuntimeFrame("ObtainedRoot", stateRight, new Color32(35, 50, 65, 255), new Color32(45, 65, 80, 255), out _);
        obtainedRoot.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        obtainedRoot.GetComponent<RectTransform>().sizeDelta = new Vector2(240f, 85f);
        TMP_Text obtainedTxt = CreateRuntimeText("ObtainedLabel", obtainedRoot.transform, "Obtained", 34f, new Color32(160, 180, 195, 255), TextAlignmentOptions.Center, fontAsset);
        StretchRect(obtainedTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        obtainedRoot.SetActive(false);

        GameObject countRoot = CreateRuntimeRect("CountdownRoot", stateRight);
        countRoot.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        countRoot.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 100f);

        TMP_Text countLbl = CreateRuntimeText("CountdownLabel", countRoot.transform, "Time Remaining", 26f, Color.white, TextAlignmentOptions.Center, fontAsset);
        countLbl.rectTransform.anchoredPosition = new Vector2(0f, 22f);
        countLbl.rectTransform.sizeDelta = new Vector2(260f, 40f);

        TMP_Text countTxt = CreateRuntimeText("CountdownTime", countRoot.transform, "15:26:01", 34f, Color.white, TextAlignmentOptions.Center, fontAsset);
        countTxt.rectTransform.anchoredPosition = new Vector2(0f, -22f);
        countTxt.rectTransform.sizeDelta = new Vector2(260f, 45f);
        countRoot.SetActive(false);

        DailyLoginItemUI itemUI = itemObj.AddComponent<DailyLoginItemUI>();
        itemUI.SetReferencesForBuilder(dayLabel, dayNumber, rewardsTr, claimBtn, claimBtnTxt, obtainedRoot, obtainedTxt, countRoot, countLbl, countTxt, bg, border, cg);
        return itemUI;
    }

    private static GameObject BuildRuntimeAchievementPanel(Transform parent, TMP_FontAsset fontAsset, out AchievementPanelUI panelUI)
    {
        GameObject panelObj = CreateRuntimeRect("AchievementPanel", parent);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        StretchRect(panelRect, Vector2.zero, Vector2.one, new Vector2(16f, 20f), new Vector2(-16f, -30f));

        ScrollRect scroll = panelObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 25f;

        RectTransform viewport = CreateRuntimeRect("Viewport", panelObj.transform).GetComponent<RectTransform>();
        StretchRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<RectMask2D>();
        scroll.viewport = viewport;

        RectTransform content = CreateRuntimeRect("Content", viewport).GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 1200f);
        scroll.content = content;

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 16f;
        layout.padding = new RectOffset(10, 10, 15, 15);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var items = new System.Collections.Generic.List<AchievementItemUI>();
        for (int i = 0; i < 5; i++)
        {
            items.Add(CreateRuntimeAchievementItem(content, i, fontAsset));
        }

        panelUI = panelObj.AddComponent<AchievementPanelUI>();
        panelUI.SetReferencesForBuilder(scroll, content, items, null, null, null);
        return panelObj;
    }

    private static AchievementItemUI CreateRuntimeAchievementItem(Transform parent, int index, TMP_FontAsset fontAsset)
    {
        GameObject itemObj = CreateRuntimeFrame($"AchievementItem_{index}", parent, new Color32(14, 48, 68, 255), new Color32(64, 180, 195, 255), out Image bg);
        RectTransform rect = itemObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(920f, 210f);
        LayoutElement le = itemObj.AddComponent<LayoutElement>();
        le.preferredHeight = 210f;
        le.minHeight = 210f;

        Image border = itemObj.GetComponent<Image>();

        TMP_Text title = CreateRuntimeText("TitleText", itemObj.transform, "Achievement Title", 36f, Color.white, TextAlignmentOptions.Left, fontAsset);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(30f, -18f);
        title.rectTransform.sizeDelta = new Vector2(-360f, 45f);

        GameObject barBgObj = CreateRuntimeFrame("ProgressBarBg", itemObj.transform, new Color32(12, 32, 45, 255), new Color32(94, 213, 205, 255), out Image barBg);
        RectTransform barRect = barBgObj.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 1f);
        barRect.anchorMax = new Vector2(0f, 1f);
        barRect.pivot = new Vector2(0f, 1f);
        barRect.anchoredPosition = new Vector2(30f, -70f);
        barRect.sizeDelta = new Vector2(560f, 32f);

        GameObject fillObj = CreateRuntimeImage("ProgressFill", barBgObj.transform, new Color32(40, 180, 245, 255), false);
        Image fillImg = fillObj.GetComponent<Image>();
        StretchRect(fillObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TMP_Text progressTxt = CreateRuntimeText("ProgressText", itemObj.transform, "0/0", 26f, Color.white, TextAlignmentOptions.Center, fontAsset);
        progressTxt.rectTransform.anchorMin = new Vector2(0f, 1f);
        progressTxt.rectTransform.anchorMax = new Vector2(0f, 1f);
        progressTxt.rectTransform.pivot = new Vector2(0.5f, 1f);
        progressTxt.rectTransform.anchoredPosition = new Vector2(310f, -104f);
        progressTxt.rectTransform.sizeDelta = new Vector2(560f, 30f);

        RectTransform rewardsTr = CreateRuntimeRect("RewardsContainer", itemObj.transform).GetComponent<RectTransform>();
        rewardsTr.anchorMin = new Vector2(0f, 0f);
        rewardsTr.anchorMax = new Vector2(0f, 0f);
        rewardsTr.pivot = new Vector2(0f, 0f);
        rewardsTr.anchoredPosition = new Vector2(30f, 15f);
        rewardsTr.sizeDelta = new Vector2(560f, 65f);

        HorizontalLayoutGroup rLayout = rewardsTr.gameObject.AddComponent<HorizontalLayoutGroup>();
        rLayout.spacing = 14f;
        rLayout.childAlignment = TextAnchor.MiddleLeft;
        rLayout.childControlWidth = false;
        rLayout.childControlHeight = false;

        GameObject btnObj = CreateRuntimeButton("ActionButton", itemObj.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-280f, -47.5f), new Vector2(-30f, 47.5f), "Get", 38f, new Color32(56, 189, 248, 255), new Color32(94, 213, 205, 255), fontAsset, out Image btnImg);
        Button actBtn = btnObj.GetComponent<Button>();
        TMP_Text btnTxt = btnObj.transform.Find("Label")?.GetComponent<TMP_Text>();

        GameObject dot = CreateRuntimeImage("NotificationDot", btnObj.transform, new Color32(235, 60, 60, 255), false);
        RectTransform dotRect = dot.GetComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(1f, 1f);
        dotRect.anchorMax = new Vector2(1f, 1f);
        dotRect.pivot = new Vector2(0.5f, 0.5f);
        dotRect.anchoredPosition = new Vector2(-10f, -10f);
        dotRect.sizeDelta = new Vector2(22f, 22f);
        dot.SetActive(false);

        AchievementItemUI itemUI = itemObj.AddComponent<AchievementItemUI>();
        itemUI.SetReferencesForBuilder(title, progressTxt, fillImg, barBg, rewardsTr, actBtn, btnImg, btnTxt, dot, border, bg);
        return itemUI;
    }

    private static GameObject CreateRuntimeTabButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, string label, float fontSize, Color fill, Color borderCol, TMP_FontAsset fontAsset, out Image bg, out TMP_Text text, out GameObject dot)
    {
        GameObject btnObj = CreateRuntimeFrame(name, parent, fill, borderCol, out bg);
        bg.raycastTarget = true;
        Image border = btnObj.GetComponent<Image>();
        if (border != null) border.raycastTarget = true;

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;

        text = CreateRuntimeText("Label", btnObj.transform, label, fontSize, Color.white, TextAlignmentOptions.Center, fontAsset);
        text.raycastTarget = false;
        StretchRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        dot = CreateRuntimeImage("Badge", btnObj.transform, new Color32(235, 60, 60, 255), false);
        RectTransform dotRect = dot.GetComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(1f, 1f);
        dotRect.anchorMax = new Vector2(1f, 1f);
        dotRect.pivot = new Vector2(0.5f, 0.5f);
        dotRect.anchoredPosition = new Vector2(-15f, -15f);
        dotRect.sizeDelta = new Vector2(24f, 24f);
        dot.SetActive(false);

        return btnObj;
    }

    private static GameObject CreateRuntimeButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, string label, float fontSize, Color fill, Color borderCol, TMP_FontAsset fontAsset, out Image background)
    {
        GameObject btnObj = CreateRuntimeFrame(name, parent, fill, borderCol, out background);
        background.raycastTarget = true;
        Image border = btnObj.GetComponent<Image>();
        if (border != null) border.raycastTarget = true;

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = background;

        TMP_Text txt = CreateRuntimeText("Label", btnObj.transform, label, fontSize, Color.white, TextAlignmentOptions.Center, fontAsset);
        txt.raycastTarget = false;
        StretchRect(txt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        return btnObj;
    }

    private static GameObject CreateRuntimeFrame(string name, Transform parent, Color fillColor, Color borderColor, out Image background)
    {
        GameObject root = CreateRuntimeRect(name, parent);
        Image borderImage = root.AddComponent<Image>();
        borderImage.color = borderColor;
        borderImage.raycastTarget = false;

        Shadow shadow = root.AddComponent<Shadow>();
        shadow.effectColor = new Color32(0, 14, 24, 210);
        shadow.effectDistance = new Vector2(5f, -6f);
        shadow.useGraphicAlpha = true;

        GameObject bgObj = CreateRuntimeImage("Background", root.transform, fillColor, false);
        background = bgObj.GetComponent<Image>();
        StretchRect(bgObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        GameObject topHighlight = CreateRuntimeImage("TopHighlight", root.transform, new Color32(151, 240, 226, 75), false);
        StretchRect(topHighlight.GetComponent<RectTransform>(), new Vector2(0.04f, 0.92f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);

        return root;
    }

    private static GameObject CreateRuntimeRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        return go;
    }

    private static GameObject CreateRuntimeImage(string name, Transform parent, Color color, bool raycast)
    {
        GameObject go = CreateRuntimeRect(name, parent);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return go;
    }

    private static TMP_Text CreateRuntimeText(string name, Transform parent, string value, float fontSize, Color color, TextAlignmentOptions alignment, TMP_FontAsset fontAsset)
    {
        GameObject go = CreateRuntimeRect(name, parent);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) text.font = fontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.outlineColor = new Color32(8, 30, 42, 255);
        text.outlineWidth = 0.16f;
        return text;
    }

    private static void StretchRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
    }
}
