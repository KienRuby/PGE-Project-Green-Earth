using System;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý thanh tiền tệ trên cùng (Top Bar Header):
/// Hiển thị Năng lượng (Energy), Data Chip, Red Gem theo định dạng chuẩn như ảnh mẫu.
/// Đồng bộ tự động thời gian thực với ChipManager và PlayerDataService.
/// </summary>
public class TopBarCurrencyController : MonoBehaviour
{
    [Header("Currency Texts")]
    [Tooltip("Text hiển thị Năng lượng (ví dụ: '50/50').")]
    [SerializeField] private TMP_Text energyText;

    [Tooltip("Text hiển thị Data Chip (ví dụ: '49.181').")]
    [SerializeField] private TMP_Text dataChipText;

    [Tooltip("Text hiển thị Red Gem (ví dụ: '31.868').")]
    [SerializeField] private TMP_Text redGemText;

    [Header("Currency Add Buttons (Optional)")]
    [SerializeField] private Button addEnergyButton;
    [SerializeField] private Button addDataChipButton;
    [SerializeField] private Button addRedGemButton;

    [Header("Top Right Action Buttons")]
    [SerializeField] private Button questBookButton;
    [SerializeField] private GameObject questNotificationBadge;
    [SerializeField] private Button settingsButton;

    [Header("Navigation Controller")]
    [SerializeField] private BottomNavigationController bottomNavController;

    private void Awake()
    {
        if (bottomNavController == null)
        {
            bottomNavController = FindObjectOfType<BottomNavigationController>();
        }

        if (addEnergyButton != null)
        {
            addEnergyButton.onClick.AddListener(OnAddEnergyClicked);
        }

        if (addDataChipButton != null)
        {
            addDataChipButton.onClick.AddListener(OnAddDataChipClicked);
        }

        if (addRedGemButton != null)
        {
            addRedGemButton.onClick.AddListener(OnAddRedGemClicked);
        }

        if (questBookButton != null)
        {
            questBookButton.onClick.AddListener(OnQuestBookClicked);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        EnsureNotificationBadgeFound();
    }

    private void Start()
    {
        RefreshAllBalances();
        RefreshNotificationBadge();
    }

    private void OnEnable()
    {
        ChipManager.OnDataChipsChanged += HandleDataChipsChanged;
        ChipManager.OnRedGemsChanged += HandleRedGemsChanged;
        ChipManager.OnEnergyChanged += HandleEnergyChanged;
        ChipManager.OnTestModeChanged += HandleTestModeChanged;

        DailyLoginManager.OnDailyLoginStateChanged += RefreshNotificationBadge;
        DailyLoginManager.OnDailyRewardClaimed += HandleDailyRewardClaimed;
        AchievementManager.OnAchievementUpdated += RefreshNotificationBadge;
        AchievementManager.OnAchievementClaimed += HandleAchievementClaimed;

        RefreshAllBalances();
        RefreshNotificationBadge();
    }

    private void OnDisable()
    {
        ChipManager.OnDataChipsChanged -= HandleDataChipsChanged;
        ChipManager.OnRedGemsChanged -= HandleRedGemsChanged;
        ChipManager.OnEnergyChanged -= HandleEnergyChanged;
        ChipManager.OnTestModeChanged -= HandleTestModeChanged;

        DailyLoginManager.OnDailyLoginStateChanged -= RefreshNotificationBadge;
        DailyLoginManager.OnDailyRewardClaimed -= HandleDailyRewardClaimed;
        AchievementManager.OnAchievementUpdated -= RefreshNotificationBadge;
        AchievementManager.OnAchievementClaimed -= HandleAchievementClaimed;
    }

    private void HandleDailyRewardClaimed(int dayIndex, RewardData[] rewards)
    {
        RefreshNotificationBadge();
    }

    private void HandleAchievementClaimed(AchievementDefinition def)
    {
        RefreshNotificationBadge();
    }

    public void RefreshNotificationBadge()
    {
        bool hasDaily = DailyLoginManager.Instance != null && DailyLoginManager.Instance.HasAnyClaimableReward();
        bool hasAch = AchievementManager.Instance != null && AchievementManager.Instance.HasAnyClaimableAchievement();
        SetNotificationBadgeVisible(hasDaily || hasAch);
    }

    private void HandleDataChipsChanged(int amount)
    {
        if (dataChipText != null)
        {
            dataChipText.text = FormatCurrency(amount);
        }
    }

    private void HandleRedGemsChanged(int amount)
    {
        if (redGemText != null)
        {
            redGemText.text = FormatCurrency(amount);
        }
    }

    private void HandleEnergyChanged(int amount)
    {
        if (energyText != null)
        {
            energyText.text = $"{amount}/{ChipManager.MaxEnergy}";
        }
    }

    private void HandleTestModeChanged(bool isTest)
    {
        RefreshAllBalances();
    }

    public void RefreshAllBalances()
    {
        if (energyText != null)
        {
            energyText.text = $"{ChipManager.Energy}/{ChipManager.MaxEnergy}";
        }

        if (dataChipText != null)
        {
            dataChipText.text = FormatCurrency(ChipManager.DataChips);
        }

        if (redGemText != null)
        {
            redGemText.text = FormatCurrency(ChipManager.RedGems);
        }
    }

    private string FormatCurrency(int amount)
    {
        if (amount >= 1000)
        {
            return amount.ToString("N0", new CultureInfo("vi-VN")).Replace(',', '.');
        }
        return amount.ToString();
    }

    private void OnAddEnergyClicked()
    {
        if (bottomNavController != null) bottomNavController.Select(0);
    }

    private void OnAddDataChipClicked()
    {
        if (bottomNavController != null) bottomNavController.Select(0);
    }

    private void OnAddRedGemClicked()
    {
        if (bottomNavController != null) bottomNavController.Select(0);
    }

    private void OnQuestBookClicked()
    {
        RewardPopupController popup = RewardPopupController.Instance;
        if (popup == null)
        {
            popup = FindObjectOfType<RewardPopupController>(true);
        }

        if (popup == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                popup = RewardPopupController.CreateRuntimePopup(canvas.transform as RectTransform);
            }
        }

        if (popup != null)
        {
            popup.TogglePopup();
        }
        else
        {
            Debug.LogWarning("[TopBar] Không tìm thấy Canvas để hiển thị RewardPopup.");
        }
    }

    private void EnsureNotificationBadgeFound()
    {
        if (questNotificationBadge != null) return;
        if (questBookButton == null) return;

        Transform dotTr = questBookButton.transform.Find("Icon/NotifDot")
            ?? questBookButton.transform.Find("NotifDot")
            ?? questBookButton.transform.Find("NotificationDot")
            ?? questBookButton.transform.Find("Badge")
            ?? questBookButton.transform.Find("RedDot");

        if (dotTr == null)
        {
            dotTr = questBookButton.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t != questBookButton.transform && 
                                    (t.name.IndexOf("notif", StringComparison.OrdinalIgnoreCase) >= 0 || 
                                     t.name.IndexOf("dot", StringComparison.OrdinalIgnoreCase) >= 0 || 
                                     t.name.IndexOf("badge", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        if (dotTr != null)
        {
            questNotificationBadge = dotTr.gameObject;
        }
    }

    public void SetNotificationBadgeVisible(bool visible)
    {
        EnsureNotificationBadgeFound();

        if (questNotificationBadge != null)
        {
            questNotificationBadge.SetActive(visible);
        }
    }

    private void OnSettingsClicked()
    {
        SettingsPanelController panel = SettingsPanelController.Instance;
        if (panel == null)
        {
            panel = FindObjectOfType<SettingsPanelController>(true);
        }

        if (panel == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                panel = SettingsPanelController.CreateRuntimePanel(canvas.transform as RectTransform);
            }
        }

        if (panel != null)
        {
            panel.Toggle();
        }
        else
        {
            Debug.LogWarning("[TopBar] Không tìm thấy Canvas để hiển thị SettingsPanel.");
        }
    }

    public void SetTextsForTesting(TMP_Text energy, TMP_Text chips, TMP_Text gems)
    {
        energyText = energy;
        dataChipText = chips;
        redGemText = gems;
    }
}
