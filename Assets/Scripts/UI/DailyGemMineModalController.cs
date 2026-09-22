using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Điều khiển giao diện popup Daily Gem Mine (chuẩn AAA Mobile theo mẫu Ảnh 2):
/// - Banner Monthly Premium ở trên cùng với nút bấm giá 90.000 đ.
/// - Panel chính Daily Gem Mine với icon thiên thạch đá đỏ, tiêu đề, mô tả.
/// - Đồng hồ đếm ngược reset dạng: Reset in: [09] Hour [26] Min Left.
/// - Số lượt vào còn lại dạng: Entrance: [5] Left.
/// - Các card cấp độ (Level 01 có nút Start hồng, Level 02 có thưởng gem x120-220).
/// - Nút đóng (X) và click backdrop để quay lại màn hình Chapter.
/// </summary>
public class DailyGemMineModalController : MonoBehaviour
{
    [Header("Modal Containers")]
    [Tooltip("Root GameObject của modal (bật/tắt khi mở/đóng popup).")]
    [SerializeField] private GameObject modalRoot;

    [Tooltip("Panel nội dung chính để nảy hiệu ứng pop/scale.")]
    [SerializeField] private RectTransform mainPanel;

    [Tooltip("CanvasGroup để điều chỉnh độ mờ khi chuyển cảnh.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Nút nền đen mờ phía sau để bấm ra ngoài thì đóng.")]
    [SerializeField] private Button backdropButton;

    [Tooltip("Nút đóng (X) ở góc trên modal.")]
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

    [Header("Runtime State")]
    [SerializeField] private int remainingHours = 9;
    [SerializeField] private int remainingMinutes = 26;
    [SerializeField] private int remainingEntrances = 5;
    [SerializeField] private int maxEntrances = 5;

    public event Action<int> OnLevelStarted;
    public event Action OnMonthlyPremiumPurchased;
    public event Action OnModalOpened;
    public event Action OnModalClosed;

    public bool IsOpen => modalRoot != null && modalRoot.activeSelf;
    public int RemainingHours => remainingHours;
    public int RemainingMinutes => remainingMinutes;
    public int RemainingEntrances => remainingEntrances;
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
    }

    private void OnEnable()
    {
        SanitizeMaterialsAndEffects();
    }

    private void Start()
    {
        SanitizeMaterialsAndEffects();
        RefreshStatusTexts();
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
    }

    /// <summary>
    /// Mở giao diện Daily Gem Mine theo đúng mẫu ảnh 2.
    /// </summary>
    public void OpenModal()
    {
        SanitizeMaterialsAndEffects();

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
        Debug.Log("[DailyGemMineModalController] Mở giao diện Daily Gem Mine.");
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
        Debug.Log("[DailyGemMineModalController] Người chơi bấm mua Monthly Premium (90.000 đ).");
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
            Debug.LogWarning("[DailyGemMineModalController] Đã hết lượt tham gia Daily Gem Mine hôm nay.");
            return;
        }

        remainingEntrances = Mathf.Max(0, remainingEntrances - 1);
        RefreshStatusTexts();

        Debug.Log($"[DailyGemMineModalController] Bắt đầu màn Gem Mine Cấp {level}. Lượt còn lại: {remainingEntrances}.");
        OnLevelStarted?.Invoke(level);
    }

    /// <summary>
    /// Cập nhật thời gian reset (ví dụ: 09 Hour 26 Min Left với chữ số màu vàng).
    /// </summary>
    public void SetRemainingTime(int hours, int minutes)
    {
        remainingHours = Mathf.Max(0, hours);
        remainingMinutes = Mathf.Clamp(minutes, 0, 59);
        RefreshStatusTexts();
    }

    /// <summary>
    /// Cập nhật số lượt tham gia (ví dụ: Entrance: 5 Left).
    /// </summary>
    public void SetEntrances(int remaining, int max = 5)
    {
        maxEntrances = max;
        remainingEntrances = Mathf.Clamp(remaining, 0, maxEntrances);
        RefreshStatusTexts();
    }

    public void RefreshStatusTexts()
    {
        if (resetTimerText != null)
        {
            string hStr = remainingHours.ToString("D2");
            string mStr = remainingMinutes.ToString("D2");
            resetTimerText.text = $"Reset in: <color=#FFEE33>{hStr}</color> Hour <color=#FFEE33>{mStr}</color> Min Left";
        }

        if (entranceCountText != null)
        {
            entranceCountText.text = $"Entrance: <color=#FFEE33>{remainingEntrances}</color> Left";
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
        RefreshStatusTexts();
    }
}
