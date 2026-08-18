using System;
using UnityEngine;

/// <summary>
/// Dịch vụ tập trung quản lý toàn bộ dữ liệu người chơi, số dư tiền tệ, năng lượng
/// và cấp độ nâng cấp trong Lab thông qua PlayerPrefs.
/// Giúp code toàn bộ dự án không bị phụ thuộc vào chuỗi hardcoded string key rải rác.
/// </summary>
public static class PlayerDataService
{
    // =========================================================================
    // CONSTANTS: PLAYERPREFS KEYS
    // =========================================================================
    public const string ChipsetsKey = "PGE.Shop.Balance.Chipsets";
    public const string RedGemsKey = "PGE.Shop.Balance.RedGems";
    public const string EnergyKey = "PGE.Lab.Balance.Energy";
    public const string NextEnergyUtcKey = "PGE.Lab.NextEnergyUtc";
    public const string CompletedRollsKey = "PGE.Lab.CompletedRolls";
    public const string ItemLevelKeyPrefix = "PGE.Lab.ItemLevel.";
    public const string SelectedWeaponIdKey = "SelectedWeaponId";

    // =========================================================================
    // INITIALIZATION: TARGET FRAMERATE
    // =========================================================================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeApplicationSettings()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }

    // =========================================================================
    // EVENTS: THÔNG BÁO THAY ĐỔI DỮ LIỆU
    // =========================================================================
    public static event Action<int> OnChipsetsChanged;
    public static event Action<int> OnRedGemsChanged;
    public static event Action<int> OnEnergyChanged;
    public static event Action<string> OnSelectedWeaponChanged;

    // =========================================================================
    // PROPERTIES: TIỀN TỆ & NĂNG LƯỢNG
    // =========================================================================

    /// <summary>
    /// Số lượng Chip xanh (Chipsets) dùng để quay nâng cấp Lab.
    /// </summary>
    public static int Chipsets
    {
        get => PlayerPrefs.GetInt(ChipsetsKey, 0);
        set
        {
            int clamped = Mathf.Max(0, value);
            PlayerPrefs.SetInt(ChipsetsKey, clamped);
            PlayerPrefs.Save();
            OnChipsetsChanged?.Invoke(clamped);
        }
    }

    /// <summary>
    /// Số lượng Gem đỏ (Red Gems) dùng để mua sắm cao cấp.
    /// </summary>
    public static int RedGems
    {
        get => PlayerPrefs.GetInt(RedGemsKey, 0);
        set
        {
            int clamped = Mathf.Max(0, value);
            PlayerPrefs.SetInt(RedGemsKey, clamped);
            PlayerPrefs.Save();
            OnRedGemsChanged?.Invoke(clamped);
        }
    }

    /// <summary>
    /// Năng lượng hiện có (Energy) của người chơi.
    /// </summary>
    public static int Energy
    {
        get => PlayerPrefs.GetInt(EnergyKey, 30);
        set
        {
            int clamped = Mathf.Max(0, value);
            PlayerPrefs.SetInt(EnergyKey, clamped);
            PlayerPrefs.Save();
            OnEnergyChanged?.Invoke(clamped);
        }
    }

    /// <summary>
    /// Thời điểm UTC phục hồi 1 năng lượng tiếp theo.
    /// </summary>
    public static string NextEnergyUtc
    {
        get => PlayerPrefs.GetString(NextEnergyUtcKey, string.Empty);
        set
        {
            PlayerPrefs.SetString(NextEnergyUtcKey, value ?? string.Empty);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Số lần đã quay nâng cấp trong Lab (dùng để scale giá tiền).
    /// </summary>
    public static int CompletedRolls
    {
        get => PlayerPrefs.GetInt(CompletedRollsKey, 0);
        set
        {
            PlayerPrefs.SetInt(CompletedRollsKey, Mathf.Max(0, value));
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// ID của khẩu súng đang được trang bị (ví dụ: "blaster", "gatling").
    /// </summary>
    public static string SelectedWeaponId
    {
        get => PlayerPrefs.GetString(SelectedWeaponIdKey, "blaster");
        set
        {
            string id = value ?? "blaster";
            PlayerPrefs.SetString(SelectedWeaponIdKey, id);
            PlayerPrefs.Save();
            OnSelectedWeaponChanged?.Invoke(id);
        }
    }

    // =========================================================================
    // LAB UPGRADE ITEM LEVELS
    // =========================================================================

    public static string FormatItemLevelKey(string itemName)
    {
        string normalized = (itemName ?? string.Empty).Trim().ToUpperInvariant();
        return $"{ItemLevelKeyPrefix}{normalized}";
    }

    public static int GetItemLevel(string itemName)
    {
        string key = FormatItemLevelKey(itemName);
        return Mathf.Max(0, PlayerPrefs.GetInt(key, 0));
    }

    public static void SetItemLevel(string itemName, int level)
    {
        string key = FormatItemLevelKey(itemName);
        PlayerPrefs.SetInt(key, Mathf.Max(0, level));
        PlayerPrefs.Save();
    }

    public static void IncrementItemLevel(string itemName, int amount = 1)
    {
        int current = GetItemLevel(itemName);
        SetItemLevel(itemName, current + amount);
    }
}
