using System;
using UnityEngine;

/// <summary>
/// Quản lý tập trung TOÀN BỘ các đơn vị tiền tệ (Data Chip, Red Gem, Energy) xuyên suốt toàn bộ game:
/// Lab, Shop, Chipset, Buddy, Chapter, v.v.
/// Gắn trên 1 GameObject duy nhất (Singleton DontDestroyOnLoad).
/// Có tính năng TEST MODE: Khi bật Test Mode trong Editor, người chơi có VÔ HẠN CHIP để thử nghiệm thoải mái.
/// TÍNH NĂNG BẢO VỆ: Khi Build ra APK / Thiết bị thực tế, code TỰ ĐỘNG CƯỠNG CHẾ TẮT Test Mode và Vô Hạn Chip
/// (kể cả khi bạn quên tắt trong Unity Editor trước khi bấm Build).
/// </summary>
[DisallowMultipleComponent]
public sealed class ChipManager : MonoBehaviour
{
    private static ChipManager instance;
    public static ChipManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ChipManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("[ChipManager]");
                    instance = go.AddComponent<ChipManager>();
                    if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(go);
                    }
                }
            }
            return instance;
        }
    }

    // =========================================================================
    // INSPECTOR CONFIGURATION: TEST MODE & BALANCES
    // =========================================================================
    [Header("=== TEST MODE CONFIGURATION (VÔ HẠN CHIP TRONG EDITOR) ===")]
    [Tooltip("Bật chế độ Test để có vô hạn chip thử nghiệm toàn bộ Lab, Shop, Chipset, Buddy, Chapter.")]
    [SerializeField] private bool enableTestMode = false;

    [Tooltip("Khi bật Test Mode, số chip sẽ không bị trừ khi mua/nâng cấp (luôn luôn thành công).")]
    [SerializeField] private bool infiniteChipsInTestMode = true;

    [Tooltip("Tự động cưỡng chế TẮT Test Mode và Vô Hạn Chip khi build ra APK / Mobile Release (ngăn chặn hoàn toàn rủi ro quên tắt trước khi build).")]
    [SerializeField] private bool autoDisableInNonEditorBuilds = true;

    [Header("=== TEST MODE BALANCES (SỐ DƯ DÙNG THỬ TRONG EDITOR) ===")]
    [Min(0)]
    [Tooltip("Số Data Chip (Chipset xanh) hiển thị trong chế độ Test.")]
    [SerializeField] private int testDataChips = 9999999;

    [Min(0)]
    [Tooltip("Số Red Gem (Gem đỏ) hiển thị trong chế độ Test.")]
    [SerializeField] private int testRedGems = 9999999;

    [Min(0)]
    [Tooltip("Số Năng Lượng (Energy) hiển thị trong chế độ Test.")]
    [SerializeField] private int testEnergy = 100;

    [Header("=== DEFAULT BALANCES (CHẾ ĐỘ THƯỜNG) ===")]
    [Min(0)]
    [SerializeField] private int defaultStartingDataChips = 134936;

    [Min(0)]
    [SerializeField] private int defaultStartingRedGems = 15516;

    [Min(0)]
    [SerializeField] private int defaultStartingEnergy = 50;

    [Min(1)]
    [SerializeField] private int maximumEnergy = 100;

    // =========================================================================
    // EVENTS (THÔNG BÁO THAY ĐỔI TIỀN TỆ CHO TOÀN BỘ HỆ THỐNG UI)
    // =========================================================================
    public static event Action<int> OnDataChipsChanged;
    public static event Action<int> OnRedGemsChanged;
    public static event Action<int> OnEnergyChanged;
    public static event Action<bool> OnTestModeChanged;

    // =========================================================================
    // LIFECYCLE & INITIALIZATION
    // =========================================================================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (instance == null)
        {
            ChipManager found = FindObjectOfType<ChipManager>();
            if (found != null)
            {
                instance = found;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(found.gameObject);
                }
            }
            else
            {
                GameObject go = new GameObject("[ChipManager]");
                instance = go.AddComponent<ChipManager>();
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(go);
                }
            }
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }

#if !UNITY_EDITOR
        // TỰ ĐỘNG TẮT VÔ HẠN CHIP VÀ TEST MODE TRÊN BẢN BUILD APK THỰC TẾ
        if (autoDisableInNonEditorBuilds)
        {
            enableTestMode = false;
            infiniteChipsInTestMode = false;
            Debug.Log("[ChipManager] Đã tự động TẮT Test Mode & Vô Hạn Chip cho bản Build APK.");
        }
#endif

        InitializeDefaultBalances();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnValidate()
    {
        // Khi thay đổi checkbox enableTestMode trong Inspector lúc đang chạy game
        if (Application.isPlaying)
        {
            NotifyAllBalancesChanged();
            OnTestModeChanged?.Invoke(enableTestMode);
        }
    }

    private void InitializeDefaultBalances()
    {
        if (!PlayerPrefs.HasKey(PlayerDataService.DataChipsKey))
        {
            PlayerDataService.DataChips = defaultStartingDataChips;
        }

        if (!PlayerPrefs.HasKey(PlayerDataService.RedGemsKey))
        {
            PlayerDataService.RedGems = defaultStartingRedGems;
        }

        if (!PlayerPrefs.HasKey(PlayerDataService.EnergyKey))
        {
            PlayerDataService.Energy = defaultStartingEnergy;
        }
    }

    // =========================================================================
    // PROPERTIES & ACCESSORS (STATIC & INSTANCE)
    // =========================================================================

    public static bool IsTestMode
    {
        get
        {
#if !UNITY_EDITOR
            if (Instance != null && Instance.autoDisableInNonEditorBuilds)
            {
                return false;
            }
#endif
            return Instance != null && Instance.enableTestMode;
        }
        set
        {
#if !UNITY_EDITOR
            if (Instance != null && Instance.autoDisableInNonEditorBuilds)
            {
                Instance.enableTestMode = false;
                return;
            }
#endif
            if (Instance != null)
            {
                Instance.enableTestMode = value;
                Instance.NotifyAllBalancesChanged();
                OnTestModeChanged?.Invoke(value);
            }
        }
    }

    public static bool IsInfiniteInTest
    {
        get
        {
#if !UNITY_EDITOR
            if (Instance != null && Instance.autoDisableInNonEditorBuilds)
            {
                return false;
            }
#endif
            return Instance != null && Instance.enableTestMode && Instance.infiniteChipsInTestMode;
        }
    }

    public bool AutoDisableInNonEditorBuilds
    {
        get => autoDisableInNonEditorBuilds;
        set => autoDisableInNonEditorBuilds = value;
    }

    /// <summary>
    /// Số lượng Data Chip (Chip xanh) hiện có.
    /// </summary>
    public static int DataChips
    {
        get
        {
            if (IsTestMode)
            {
                return Instance.testDataChips;
            }
            return PlayerDataService.DataChips;
        }
        set
        {
            if (IsTestMode)
            {
                Instance.testDataChips = Mathf.Max(0, value);
                OnDataChipsChanged?.Invoke(Instance.testDataChips);
            }
            else
            {
                PlayerDataService.DataChips = value;
                OnDataChipsChanged?.Invoke(PlayerDataService.DataChips);
            }
        }
    }

    /// <summary>
    /// Số lượng Red Gem (Gem đỏ) hiện có.
    /// </summary>
    public static int RedGems
    {
        get
        {
            if (IsTestMode)
            {
                return Instance.testRedGems;
            }
            return PlayerDataService.RedGems;
        }
        set
        {
            if (IsTestMode)
            {
                Instance.testRedGems = Mathf.Max(0, value);
                OnRedGemsChanged?.Invoke(Instance.testRedGems);
            }
            else
            {
                PlayerDataService.RedGems = value;
                OnRedGemsChanged?.Invoke(PlayerDataService.RedGems);
            }
        }
    }

    /// <summary>
    /// Năng lượng (Energy) hiện có.
    /// </summary>
    public static int Energy
    {
        get
        {
            if (IsTestMode)
            {
                return Instance.testEnergy;
            }
            return PlayerDataService.Energy;
        }
        set
        {
            if (IsTestMode)
            {
                Instance.testEnergy = Mathf.Clamp(value, 0, Instance.maximumEnergy);
                OnEnergyChanged?.Invoke(Instance.testEnergy);
            }
            else
            {
                PlayerDataService.Energy = value;
                OnEnergyChanged?.Invoke(PlayerDataService.Energy);
            }
        }
    }

    public static int MaxEnergy => Instance != null ? Instance.maximumEnergy : 100;

    // =========================================================================
    // TRANSACTION HELPER METHODS (GIAO DỊCH TIỀN TỆ)
    // =========================================================================

    public static bool HasEnoughDataChips(int amount)
    {
        if (IsInfiniteInTest) return true;
        return DataChips >= amount;
    }

    public static bool HasEnoughRedGems(int amount)
    {
        if (IsInfiniteInTest) return true;
        return RedGems >= amount;
    }

    public static bool HasEnoughEnergy(int amount)
    {
        if (IsInfiniteInTest) return true;
        return Energy >= amount;
    }

    /// <summary>
    /// Trừ Data Chip (Chip xanh). Khi Test Mode bật trong Editor, luôn thành công và không bị cạn kiệt.
    /// </summary>
    public static bool TrySpendDataChips(int amount)
    {
        if (amount < 0) return false;

        if (IsInfiniteInTest)
        {
            return true;
        }

        if (DataChips < amount) return false;

        DataChips -= amount;
        return true;
    }

    /// <summary>
    /// Trừ Red Gem (Gem đỏ). Khi Test Mode bật trong Editor, luôn thành công và không bị cạn kiệt.
    /// </summary>
    public static bool TrySpendRedGems(int amount)
    {
        if (amount < 0) return false;

        if (IsInfiniteInTest)
        {
            return true;
        }

        if (RedGems < amount) return false;

        RedGems -= amount;
        return true;
    }

    /// <summary>
    /// Trừ Năng Lượng. Khi Test Mode bật trong Editor, luôn thành công.
    /// </summary>
    public static bool TrySpendEnergy(int amount)
    {
        if (amount < 0) return false;

        if (IsInfiniteInTest)
        {
            return true;
        }

        if (Energy < amount) return false;

        Energy -= amount;
        return true;
    }

    public static void AddDataChips(int amount)
    {
        if (amount <= 0) return;
        DataChips += amount;
    }

    public static void AddRedGems(int amount)
    {
        if (amount <= 0) return;
        RedGems += amount;
    }

    public static void AddEnergy(int amount)
    {
        if (amount <= 0) return;
        Energy += amount;
    }

    public void NotifyAllBalancesChanged()
    {
        OnDataChipsChanged?.Invoke(DataChips);
        OnRedGemsChanged?.Invoke(RedGems);
        OnEnergyChanged?.Invoke(Energy);
    }

    // =========================================================================
    // IN-EDITOR CONTEXT MENU CHEATS (Chuột phải vào ChipManager component)
    // =========================================================================

    [ContextMenu("Toggle Test Mode (Bật/Tắt Vô Hạn Chip)")]
    public void ToggleTestMode()
    {
        enableTestMode = !enableTestMode;
        NotifyAllBalancesChanged();
        OnTestModeChanged?.Invoke(enableTestMode);
        Debug.Log($"[ChipManager] Test Mode: {(enableTestMode ? "BẬT (VÔ HẠN CHIP)" : "TẮT (Bình thường)")}");
    }

    [ContextMenu("Set 9,999,999 Data Chips")]
    public void CheatMaxDataChips()
    {
        DataChips = 9999999;
        Debug.Log($"[ChipManager] Đã đặt DataChips = {DataChips:N0}");
    }

    [ContextMenu("Set 9,999,999 Red Gems")]
    public void CheatMaxRedGems()
    {
        RedGems = 9999999;
        Debug.Log($"[ChipManager] Đã đặt RedGems = {RedGems:N0}");
    }

    [ContextMenu("Reset Balances to Defaults")]
    public void ResetBalancesToDefault()
    {
        enableTestMode = false;
        PlayerDataService.DataChips = defaultStartingDataChips;
        PlayerDataService.RedGems = defaultStartingRedGems;
        PlayerDataService.Energy = defaultStartingEnergy;
        PlayerPrefs.Save();
        NotifyAllBalancesChanged();
        Debug.Log("[ChipManager] Đã reset toàn bộ số dư về mặc định ban đầu.");
    }
}
