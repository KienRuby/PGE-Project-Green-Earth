using System;
using UnityEngine;

/// <summary>
/// Dịch vụ tập trung quản lý toàn bộ dữ liệu người chơi, số dư tiền tệ (Data Chips & Red Gems),
/// năng lượng (Energy) và cấp độ nâng cấp trong Lab thông qua PlayerPrefs.
/// Giúp code toàn bộ dự án không bị phụ thuộc vào chuỗi hardcoded string key rải rác.
/// </summary>
public static class PlayerDataService
{
    // =========================================================================
    // CONSTANTS: PLAYERPREFS KEYS
    // =========================================================================
    public const string DataChipsKey = "PGE.Shop.Balance.Chipsets";
    public const string ChipsetsKey = DataChipsKey;
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
    public static void InitializeApplicationSettings()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Screen.orientation = ScreenOrientation.Portrait;
    }

    // =========================================================================
    // EVENTS: THÔNG BÁO THAY ĐỔI DỮ LIỆU TIỀN TỆ
    // =========================================================================
    public static event Action<int> OnDataChipsChanged;
    public static event Action<int> OnChipsetsChanged
    {
        add => OnDataChipsChanged += value;
        remove => OnDataChipsChanged -= value;
    }

    public static event Action<int> OnRedGemsChanged;
    public static event Action<int> OnEnergyChanged;
    public static event Action<string> OnSelectedWeaponChanged;

    // =========================================================================
    // PROPERTIES: TIỀN TỆ & NĂNG LƯỢNG
    // =========================================================================

    /// <summary>
    /// Số lượng Data Chip (Chip xanh) dùng để quay nâng cấp các chỉ số trong phòng Lab.
    /// </summary>
    public static int DataChips
    {
        get => PlayerPrefs.GetInt(DataChipsKey, 0);
        set
        {
            int clamped = Mathf.Max(0, value);
            PlayerPrefs.SetInt(DataChipsKey, clamped);
            PlayerPrefs.Save();
            OnDataChipsChanged?.Invoke(clamped);
        }
    }

    /// <summary>
    /// Alias của DataChips (tương thích ngược với tên gọi cũ Chipsets).
    /// </summary>
    public static int Chipsets
    {
        get => DataChips;
        set => DataChips = value;
    }

    /// <summary>
    /// Số lượng Gem đỏ (Red Gems) dùng để mua sắm cao cấp, mở rương và đổi sang Data Chip.
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
    /// Năng lượng hiện có (Energy) của người chơi dùng để vào trận chơi.
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
    /// Số lần đã quay nâng cấp trong Lab (dùng để scale giá Data Chip nâng cấp).
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

    public const string SelectedChapterIndexKey = "PGE.Chapter.SelectedIndex";
    public const string UnlockedChapterIndexKey = "PGE.Chapter.UnlockedIndex";

    /// <summary>
    /// Index Chapter đang được người chơi lựa chọn (0 = Chapter 1, 3 = Chapter 4,...).
    /// </summary>
    public static int SelectedChapterIndex
    {
        get => PlayerPrefs.GetInt(SelectedChapterIndexKey, 0); // Mặc định hiển thị Chapter 1
        set
        {
            PlayerPrefs.SetInt(SelectedChapterIndexKey, Mathf.Max(0, value));
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Index Chapter cao nhất đã được mở khóa.
    /// </summary>
    public static int UnlockedChapterIndex
    {
        get => PlayerPrefs.GetInt(UnlockedChapterIndexKey, 0); // Mặc định Chapter 1
        set
        {
            PlayerPrefs.SetInt(UnlockedChapterIndexKey, Mathf.Max(0, value));
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// ID của vũ khí đang được trang bị/lựa chọn (mặc định: "blaster").
    /// </summary>
    public static string SelectedWeaponId
    {
        get
        {
            string saved = PlayerPrefs.GetString(SelectedWeaponIdKey, "blaster");
            return string.IsNullOrWhiteSpace(saved) ? "blaster" : saved;
        }
        set
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? "blaster" : value.Trim();
            PlayerPrefs.SetString(SelectedWeaponIdKey, normalized);
            PlayerPrefs.Save();
            OnSelectedWeaponChanged?.Invoke(normalized);
        }
    }


    // =========================================================================
    // TIỆN ÍCH QUẢN LÝ TIỀN TỆ (HELPER METHODS)
    // =========================================================================

    public static bool HasEnoughDataChips(int amount) => DataChips >= amount;
    public static bool HasEnoughRedGems(int amount) => RedGems >= amount;
    public static bool HasEnoughEnergy(int amount) => Energy >= amount;

    public static bool TrySpendDataChips(int amount)
    {
        if (amount < 0 || DataChips < amount) return false;
        DataChips -= amount;
        return true;
    }

    public static bool TrySpendRedGems(int amount)
    {
        if (amount < 0 || RedGems < amount) return false;
        RedGems -= amount;
        return true;
    }

    public static bool TrySpendEnergy(int amount)
    {
        if (amount < 0 || Energy < amount) return false;
        Energy -= amount;
        return true;
    }

    public static void AddDataChips(int amount)
    {
        if (amount > 0) DataChips += amount;
    }

    public static void AddRedGems(int amount)
    {
        if (amount > 0) RedGems += amount;
    }

    public static void AddEnergy(int amount)
    {
        if (amount > 0) Energy += amount;
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
