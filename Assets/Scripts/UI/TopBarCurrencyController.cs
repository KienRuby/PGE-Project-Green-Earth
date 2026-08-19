using System;
using System.Globalization;
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
    [SerializeField] private Button settingsButton;
    [SerializeField] private GameObject questNotificationBadge;

    [Header("Bottom Navigation Quick Link")]
    [Tooltip("Tham chiếu tới BottomNavigationController để chuyển tab khi bấm nút nạp (mở Shop).")]
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
    }

    private void Start()
    {
        RefreshAllBalances();
    }

    private void OnEnable()
    {
        ChipManager.OnDataChipsChanged += HandleDataChipsChanged;
        ChipManager.OnRedGemsChanged += HandleRedGemsChanged;
        ChipManager.OnEnergyChanged += HandleEnergyChanged;
        ChipManager.OnTestModeChanged += HandleTestModeChanged;

        RefreshAllBalances();
    }

    private void OnDisable()
    {
        ChipManager.OnDataChipsChanged -= HandleDataChipsChanged;
        ChipManager.OnRedGemsChanged -= HandleRedGemsChanged;
        ChipManager.OnEnergyChanged -= HandleEnergyChanged;
        ChipManager.OnTestModeChanged -= HandleTestModeChanged;
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

    /// <summary>
    /// Định dạng số tiền theo phong cách ảnh mẫu (ví dụ: 49.181 hoặc 1.25M nếu số quá lớn).
    /// </summary>
    private string FormatCurrency(int amount)
    {
        if (amount >= 10_000_000)
        {
            return (amount / 1_000_000d).ToString("0.#", CultureInfo.InvariantCulture) + "M";
        }

        // Sử dụng dấu chấm phân cách hàng nghìn như ảnh mẫu (49.181)
        var nfi = new NumberFormatInfo { NumberGroupSeparator = ".", NumberDecimalDigits = 0 };
        return amount.ToString("N0", nfi);
    }

    private void OnAddEnergyClicked()
    {
        // Chuyển sang tab Shop nếu có
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
        Debug.Log("[TopBar] Đã bấm nút Quest Book.");
    }

    private void OnSettingsClicked()
    {
        Debug.Log("[TopBar] Đã bấm nút Settings.");
    }

    public void SetTextsForTesting(TMP_Text energy, TMP_Text chips, TMP_Text gems)
    {
        energyText = energy;
        dataChipText = chips;
        redGemText = gems;
    }
}
