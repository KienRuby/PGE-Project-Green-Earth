using System;
using System.Globalization;
using UnityEngine;

public enum DailyLoginState
{
    [Tooltip("Ngày chưa tới (Locked).")]
    Locked,

    [Tooltip("Ngày hiện tại có thể nhận thưởng ngay (Available).")]
    Available,

    [Tooltip("Ngày đã nhận thưởng (Obtained).")]
    Obtained,

    [Tooltip("Hôm nay đã nhận, đang đếm ngược chờ reset sang ngày tiếp theo (CurrentDayWaiting).")]
    CurrentDayWaiting
}

/// <summary>
/// Quản lý tập trung toàn bộ logic Daily Login Reward:
/// - Chu kỳ 7 ngày (Day 01 - Day 07)
/// - Tính toán thời gian thực theo UTC (ngăn tua giờ hệ thống offline đơn giản)
/// - Đếm ngược Realtime tới lần reset tiếp theo (HH:mm:ss)
/// - 4 Trạng thái chuẩn: Locked, Available, Obtained, CurrentDayWaiting
/// - Chống spam / chống nhận thưởng trùng lặp
/// - Tự động cập nhật khi sang ngày mới lúc game đang mở
/// - Tích hợp chặt chẽ với RewardService & ChipManager
/// </summary>
[DisallowMultipleComponent]
public sealed class DailyLoginManager : MonoBehaviour
{
    private static DailyLoginManager instance;
    public static DailyLoginManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<DailyLoginManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("[DailyLoginManager]");
                    instance = go.AddComponent<DailyLoginManager>();
                    if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(go);
                    }
                }
            }
            return instance;
        }
    }

    public const string CurrentDayKey = "PGE.DailyLogin.CurrentDay";
    public const string LastLoginDateUtcKey = "PGE.DailyLogin.LastLoginDateUtc";
    public const string LastClaimDateUtcKey = "PGE.DailyLogin.LastClaimDateUtc";
    public const string LastAdClaimDateUtcKey = "PGE.DailyLogin.LastAdClaimDateUtc";
    public const string ClaimedMaskKey = "PGE.DailyLogin.ClaimedMask";
    public const string CycleCountKey = "PGE.DailyLogin.CycleCount";

    [Header("Configuration Database")]
    [SerializeField] private DailyLoginDatabase database;

    // Events
    public static event Action<int, RewardData[]> OnDailyRewardClaimed;
    public static event Action OnDailyLoginStateChanged;

    private bool isProcessingClaim = false;
    private float dayCheckTimer = 0f;
    private const float DayCheckInterval = 1f;

    public DailyLoginDatabase Database => database;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (instance == null)
        {
            DailyLoginManager found = FindObjectOfType<DailyLoginManager>();
            if (found != null)
            {
                instance = found;
                if (Application.isPlaying) DontDestroyOnLoad(found.gameObject);
            }
            else
            {
                GameObject go = new GameObject("[DailyLoginManager]");
                instance = go.AddComponent<DailyLoginManager>();
                if (Application.isPlaying) DontDestroyOnLoad(go);
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

        EnsureDatabaseLoaded();
        CheckAndUpdateLoginDay();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        dayCheckTimer += Time.unscaledDeltaTime;
        if (dayCheckTimer >= DayCheckInterval)
        {
            dayCheckTimer = 0f;
            // Kiểm tra xem đã bước sang ngày mới chưa (ví dụ game đang mở qua nửa đêm UTC)
            if (CheckAndUpdateLoginDay())
            {
                OnDailyLoginStateChanged?.Invoke();
            }
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            if (CheckAndUpdateLoginDay())
            {
                OnDailyLoginStateChanged?.Invoke();
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            if (CheckAndUpdateLoginDay())
            {
                OnDailyLoginStateChanged?.Invoke();
            }
        }
    }

    public void EnsureDatabaseLoaded()
    {
        if (database == null)
        {
#if UNITY_EDITOR
            database = UnityEditor.AssetDatabase.LoadAssetAtPath<DailyLoginDatabase>("Assets/Data/DailyLogin/DailyLoginDatabase.asset");
#endif
            if (database == null)
            {
                database = Resources.Load<DailyLoginDatabase>("DailyLoginDatabase");
            }

            if (database == null)
            {
                database = ScriptableObject.CreateInstance<DailyLoginDatabase>();
                database.PopulateDefault7Days();
            }
        }
    }

    // =========================================================================
    // PERSISTENT DATA PROPERTIES
    // =========================================================================

    /// <summary>
    /// Ngày đăng nhập hiện tại trong chu kỳ (1..7). Mặc định là Day 1.
    /// </summary>
    public int CurrentLoginDay
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(CurrentDayKey, 1), 1, 7);
        private set
        {
            PlayerPrefs.SetInt(CurrentDayKey, Mathf.Clamp(value, 1, 7));
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Chuỗi ngày claim gần nhất theo định dạng "yyyy-MM-dd".
    /// </summary>
    public string LastClaimDateUtc
    {
        get => PlayerPrefs.GetString(LastClaimDateUtcKey, string.Empty);
        private set
        {
            PlayerPrefs.SetString(LastClaimDateUtcKey, value ?? string.Empty);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Chuỗi ngày claim thêm qua xem quảng cáo gần nhất theo định dạng "yyyy-MM-dd".
    /// </summary>
    public string LastAdClaimDateUtc
    {
        get => PlayerPrefs.GetString(LastAdClaimDateUtcKey, string.Empty);
        private set
        {
            PlayerPrefs.SetString(LastAdClaimDateUtcKey, value ?? string.Empty);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Chuỗi ngày login gần nhất theo định dạng "yyyy-MM-dd".
    /// </summary>
    public string LastLoginDateUtc
    {
        get => PlayerPrefs.GetString(LastLoginDateUtcKey, string.Empty);
        private set
        {
            PlayerPrefs.SetString(LastLoginDateUtcKey, value ?? string.Empty);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Bitmask ghi nhận các ngày đã nhận thưởng trong chu kỳ hiện tại (bit 1 = Day 1, bit 2 = Day 2...).
    /// </summary>
    public int ClaimedMask
    {
        get => PlayerPrefs.GetInt(ClaimedMaskKey, 0);
        private set
        {
            PlayerPrefs.SetInt(ClaimedMaskKey, value);
            PlayerPrefs.Save();
        }
    }

    public int CycleCount
    {
        get => PlayerPrefs.GetInt(CycleCountKey, 0);
        private set
        {
            PlayerPrefs.SetInt(CycleCountKey, Mathf.Max(0, value));
            PlayerPrefs.Save();
        }
    }

    // =========================================================================
    // UTC DATE & RESET CALCULATION
    // =========================================================================

    public DateTime GetCurrentUtcTime()
    {
        return DateTime.UtcNow;
    }

    public DateTime GetEffectiveDateUtc()
    {
        int resetHour = database != null ? database.resetHourUtc : 0;
        DateTime utc = DateTime.UtcNow;
        return utc.AddHours(-resetHour).Date;
    }

    public string GetEffectiveDateStringUtc()
    {
        return GetEffectiveDateUtc().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Tính toán thời gian còn lại tới mốc reset ngày mới tiếp theo.
    /// </summary>
    public TimeSpan GetTimeUntilNextResetUtc()
    {
        int resetHour = database != null ? database.resetHourUtc : 0;
        DateTime nowUtc = DateTime.UtcNow;
        DateTime nextResetUtc = nowUtc.Date.AddHours(resetHour);
        if (nowUtc >= nextResetUtc)
        {
            nextResetUtc = nextResetUtc.AddDays(1);
        }
        TimeSpan remaining = nextResetUtc - nowUtc;
        return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    /// <summary>
    /// Định dạng chuỗi đếm ngược thời gian còn lại (ví dụ: "15:26:01").
    /// </summary>
    public string GetRemainingTimeFormatted()
    {
        TimeSpan span = GetTimeUntilNextResetUtc();
        return $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
    }

    // =========================================================================
    // STATE CHECK & LOGIN CYCLE LOGIC
    // =========================================================================

    public bool IsDayClaimed(int dayIndex)
    {
        if (dayIndex < 1 || dayIndex > 7) return false;
        return (ClaimedMask & (1 << (dayIndex - 1))) != 0;
    }

    public void MarkDayClaimed(int dayIndex)
    {
        if (dayIndex < 1 || dayIndex > 7) return;
        ClaimedMask |= (1 << (dayIndex - 1));
    }

    public bool HasClaimedToday()
    {
        string todayStr = GetEffectiveDateStringUtc();
        return string.Equals(LastClaimDateUtc, todayStr, StringComparison.OrdinalIgnoreCase);
    }

    public DailyLoginState GetDayState(int dayIndex)
    {
        int current = CurrentLoginDay;

        if (dayIndex == current)
        {
            if (HasClaimedToday() || IsDayClaimed(dayIndex))
            {
                return DailyLoginState.CurrentDayWaiting;
            }
            return DailyLoginState.Available;
        }

        if (dayIndex < current)
        {
            // Các ngày trước đó coi như đã nhận (hoặc đã qua)
            return DailyLoginState.Obtained;
        }

        // Ngày tương lai
        return DailyLoginState.Locked;
    }

    public bool CanClaimToday()
    {
        if (isProcessingClaim) return false;
        int current = CurrentLoginDay;
        DailyLoginState state = GetDayState(current);
        return state == DailyLoginState.Available;
    }

    public bool HasAnyClaimableReward()
    {
        return CanClaimToday();
    }

    /// <summary>
    /// Kiểm tra và cập nhật ngày đăng nhập khi bước sang ngày mới theo UTC.
    /// Trả về true nếu có sự thay đổi ngày/trạng thái.
    /// </summary>
    public bool CheckAndUpdateLoginDay()
    {
        EnsureDatabaseLoaded();

        string todayStr = GetEffectiveDateStringUtc();
        string lastLogin = LastLoginDateUtc;

        if (string.IsNullOrWhiteSpace(lastLogin))
        {
            // Lần đầu mở game
            LastLoginDateUtc = todayStr;
            if (CurrentLoginDay < 1 || CurrentLoginDay > 7)
            {
                CurrentLoginDay = 1;
            }
            return true;
        }

        if (string.Equals(lastLogin, todayStr, StringComparison.OrdinalIgnoreCase))
        {
            // Cùng một ngày, không đổi
            return false;
        }

        // Đã sang ngày mới!
        DateTime lastDate, todayDate;
        bool parseLast = DateTime.TryParseExact(lastLogin, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out lastDate);
        bool parseToday = DateTime.TryParseExact(todayStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out todayDate);

        int daysDifference = (parseLast && parseToday) ? (int)(todayDate - lastDate).TotalDays : 1;
        LastLoginDateUtc = todayStr;

        if (daysDifference >= 1)
        {
            int current = CurrentLoginDay;
            bool wasClaimed = IsDayClaimed(current);

            if (daysDifference > 1 && database.streakMode == StreakResetMode.ResetToDay1OnMissedDay)
            {
                // Bỏ lỡ > 1 ngày -> Reset về Day 1
                Debug.Log($"[DailyLoginManager] ⚠️ Bỏ lỡ {daysDifference} ngày. Reset streak về Day 1.");
                CurrentLoginDay = 1;
                ClaimedMask = 0;
            }
            else
            {
                if (wasClaimed)
                {
                    if (current < 7)
                    {
                        CurrentLoginDay = current + 1;
                    }
                    else
                    {
                        // Đã xong Day 7
                        if (database.day7Mode == Day7LoopMode.LoopToDay1)
                        {
                            Debug.Log("[DailyLoginManager] 🔄 Hoàn thành Day 7. Bắt đầu chu kỳ 7 ngày mới!");
                            CurrentLoginDay = 1;
                            ClaimedMask = 0;
                            CycleCount++;
                        }
                    }
                }
            }
            return true;
        }

        return false;
    }

    // =========================================================================
    // CLAIM REWARD TRANSACTION
    // =========================================================================

    public bool TryClaimTodayReward()
    {
        if (isProcessingClaim)
        {
            Debug.LogWarning("[DailyLoginManager] ⚠️ Đang xử lý claim, vui lòng không bấm liên tục!");
            return false;
        }

        if (!CanClaimToday())
        {
            Debug.LogWarning("[DailyLoginManager] ⚠️ Hôm nay không có phần thưởng hợp lệ để nhận!");
            return false;
        }

        isProcessingClaim = true;

        try
        {
            int current = CurrentLoginDay;
            EnsureDatabaseLoaded();
            DailyLoginDayData dayData = database.GetDayData(current);

            if (dayData == null || dayData.rewards == null || dayData.rewards.Length == 0)
            {
                Debug.LogError($"[DailyLoginManager] Không tìm thấy dữ liệu phần thưởng cho Day {current}!");
                return false;
            }

            // 1. Trao phần thưởng qua RewardService (cộng tiền tệ, energy, gems)
            RewardService.GrantRewards(dayData.rewards);

            // 2. Ghi nhận trạng thái đã nhận ngay lập tức
            MarkDayClaimed(current);
            LastClaimDateUtc = GetEffectiveDateStringUtc();
            PlayerPrefs.Save();

            Debug.Log($"[DailyLoginManager] ✅ Nhận thưởng Day {current:00} thành công!");

            // 3. Bắn event thông báo cho AchievementManager & UI
            OnDailyRewardClaimed?.Invoke(current, dayData.rewards);
            OnDailyLoginStateChanged?.Invoke();

            return true;
        }
        finally
        {
            isProcessingClaim = false;
        }
    }

    public bool HasClaimedAdToday()
    {
        string todayStr = GetEffectiveDateStringUtc();
        return string.Equals(LastAdClaimDateUtc, todayStr, StringComparison.OrdinalIgnoreCase);
    }

    public void MarkAdClaimedToday()
    {
        LastAdClaimDateUtc = GetEffectiveDateStringUtc();
        PlayerPrefs.Save();
        OnDailyLoginStateChanged?.Invoke();
    }

    /// <summary>
    /// Nhận thêm phần thưởng của ngày hôm nay sau khi xem quảng cáo (Claim Again).
    /// </summary>
    public bool TryClaimAgainWithAd()
    {
        if (isProcessingClaim) return false;

        if (HasClaimedAdToday())
        {
            Debug.LogWarning("[DailyLoginManager] ⚠️ Hôm nay đã nhận phần thưởng quảng cáo (Claim Again) rồi!");
            return false;
        }

        int current = CurrentLoginDay;
        EnsureDatabaseLoaded();
        DailyLoginDayData dayData = database != null ? database.GetDayData(current) : null;

        if (dayData == null || dayData.rewards == null || dayData.rewards.Length == 0)
        {
            Debug.LogError($"[DailyLoginManager] Không tìm thấy dữ liệu phần thưởng cho Day {current}!");
            return false;
        }

        isProcessingClaim = true;
        try
        {
            RewardService.GrantRewards(dayData.rewards);
            MarkAdClaimedToday();
            Debug.Log($"[DailyLoginManager] 🎬🎁 Đã nhận thêm phần thưởng Day {current:00} qua xem quảng cáo!");
            OnDailyRewardClaimed?.Invoke(current, dayData.rewards);
            OnDailyLoginStateChanged?.Invoke();
            return true;
        }
        finally
        {
            isProcessingClaim = false;
        }
    }

    // =========================================================================
    // IN-EDITOR DEBUG CONTEXT CHEATS
    // =========================================================================

#if UNITY_EDITOR
    [ContextMenu("Debug: Reset Daily Login System")]
    public void DebugResetDailyLogin()
    {
        PlayerPrefs.DeleteKey(CurrentDayKey);
        PlayerPrefs.DeleteKey(LastLoginDateUtcKey);
        PlayerPrefs.DeleteKey(LastClaimDateUtcKey);
        PlayerPrefs.DeleteKey(LastAdClaimDateUtcKey);
        PlayerPrefs.DeleteKey(ClaimedMaskKey);
        PlayerPrefs.DeleteKey(CycleCountKey);
        PlayerPrefs.Save();
        CurrentLoginDay = 1;
        OnDailyLoginStateChanged?.Invoke();
        Debug.Log("[DailyLoginManager] 🧹 Đã reset toàn bộ dữ liệu Daily Login!");
    }

    [ContextMenu("Debug: Advance 1 Day (Tua sang ngày tiếp theo)")]
    public void DebugAdvanceOneDay()
    {
        int current = CurrentLoginDay;
        MarkDayClaimed(current);
        if (current < 7)
        {
            CurrentLoginDay = current + 1;
        }
        else
        {
            CurrentLoginDay = 1;
            ClaimedMask = 0;
            CycleCount++;
        }
        // Giả lập claim của ngày trước
        DateTime fakePrevDate = GetEffectiveDateUtc().AddDays(-1);
        string prevDateStr = fakePrevDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        LastClaimDateUtc = prevDateStr;
        LastAdClaimDateUtc = prevDateStr;
        LastLoginDateUtc = GetEffectiveDateStringUtc();
        PlayerPrefs.Save();

        OnDailyLoginStateChanged?.Invoke();
        Debug.Log($"[DailyLoginManager] ⏩ Đã tua sang Day {CurrentLoginDay:00} (Available to claim).");
    }

    [ContextMenu("Debug: Force Make Today Available")]
    public void DebugMakeTodayAvailable()
    {
        int current = CurrentLoginDay;
        ClaimedMask &= ~(1 << (current - 1));
        LastClaimDateUtc = string.Empty;
        LastAdClaimDateUtc = string.Empty;
        PlayerPrefs.Save();
        OnDailyLoginStateChanged?.Invoke();
        Debug.Log($"[DailyLoginManager] 🔓 Đã mở khóa Day {current:00} sang trạng thái Available.");
    }
#endif
}
