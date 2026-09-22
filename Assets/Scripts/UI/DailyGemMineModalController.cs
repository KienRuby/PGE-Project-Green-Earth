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

    [Tooltip("Nút Start màu hồng ở Card Level 1.")]
    [SerializeField] private Button startLevel1Button;

    [Tooltip("Nút / Card Level 2 (tùy chọn).")]
    [SerializeField] private Button startLevel2Button;

    [Header("Status Texts")]
    [Tooltip("Text hiển thị đồng hồ Reset in: 09 Hour 26 Min Left.")]
    [SerializeField] private TMP_Text resetTimerText;

    [Tooltip("Text hiển thị số lượt vào Entrance: 5 Left.")]
    [SerializeField] private TMP_Text entranceCountText;

    [Header("Scene Transition")]
    [Tooltip("Tên Scene sẽ chuyển sang khi nhấn nút Start (mặc định: goalkeeper).")]
    [SerializeField] private string gemMineSceneName = "goalkeeper";

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

    private void Awake()
    {
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

        LoadSavedData();
    }

    private void OnEnable()
    {
        SanitizeMaterialsAndEffects();
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
        if (remainingEntrances <= 0)
        {
            Debug.LogWarning("[DailyGemMineModalController] Không thể bắt đầu: Đã hết lượt tham gia Daily Gem Mine hôm nay (Entrance: 0).");
            RefreshStatusTexts();
            return;
        }

        // Giảm 1 lượt vào trận và lưu vào PlayerPrefs
        remainingEntrances = Mathf.Max(0, remainingEntrances - 1);
        PlayerPrefs.SetInt(PrefsEntrancesKey, remainingEntrances);
        PlayerPrefs.Save();

        RefreshStatusTexts();

        Debug.Log($"[DailyGemMineModalController] Bắt đầu màn Gem Mine Cấp {level}. Lượt còn lại: {remainingEntrances}. Chuyển sang Scene: {gemMineSceneName}.");
        OnLevelStarted?.Invoke(level);

        if (Application.isPlaying && !string.IsNullOrEmpty(gemMineSceneName))
        {
            SceneManager.LoadScene(gemMineSceneName);
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
    }

    public void SetUIReferencesForTesting(
        GameObject root,
        Button premiumBtn,
        Button startLvl1,
        Button closeBtn,
        TMP_Text timerTxt,
        TMP_Text entranceTxt)
    {
        modalRoot = root;
        monthlyPremiumButton = premiumBtn;
        startLevel1Button = startLvl1;
        closeButton = closeBtn;
        resetTimerText = timerTxt;
        entranceCountText = entranceTxt;
        useManualTime = true;
        RefreshStatusTexts();
    }
}
