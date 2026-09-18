using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Các vị trí hiển thị quảng cáo trong game.
/// </summary>
public enum AdPlacement
{
    Global,
    DailyReward,
    Revive,
    Victory,
    Defeat
}

/// <summary>
/// Service quản lý hiển thị Rewarded Ad & Interstitial Ad cho toàn bộ game:
/// - Kiểm tra trạng thái kết nối mạng (Wifi / Cellular).
/// - Quản lý xem quảng cáo nhận thưởng (Hồi sinh 1 lần/trận, Daily Login 1 lần/ngày).
/// - Quản lý quảng cáo Interstitial toàn màn hình khi Thắng và Thua.
/// - Cung cấp hook cho các SDK quảng cáo bên thứ 3 (Google Mobile Ads / AdMob).
/// - Chế độ fallback mô phỏng thông minh cho Editor và non-production build.
/// </summary>
public static class AdRewardService
{
    public static event Action<Action<bool>> OnRewardedAdRequested;
    public static event Action<Action> OnInterstitialAdRequested;
    public static event Action<AdPlacement, float> OnAdCooldownStarted;

    public const float DefaultCooldownDuration = 60f; // 1 phút (60 giây)
    private const string CooldownKeyPrefix = "PGE.AdReward.CooldownEndUtc.";

    private static readonly Dictionary<AdPlacement, double> inMemoryCooldownEndUtc = new Dictionary<AdPlacement, double>();

    /// <summary>
    /// Cho phép cưỡng chế giả lập mất mạng để test trạng thái không có Wifi.
    /// </summary>
    public static bool ForceOfflineTestMode { get; set; } = false;

    /// <summary>
    /// Bỏ qua cooldown phục vụ unit test tự động.
    /// </summary>
    public static bool IgnoreCooldownForTesting { get; set; } = false;

    /// <summary>
    /// Kiểm tra xem thiết bị có kết nối mạng (Wifi hoặc 4G/5G) hay không.
    /// </summary>
    public static bool IsNetworkAvailable
    {
        get
        {
            if (ForceOfflineTestMode) return false;
            return Application.internetReachability != NetworkReachability.NotReachable;
        }
    }

    // =========================================================================
    // COOLDOWN SYSTEM (1 PHÚT / 60 GIÂY)
    // =========================================================================

    private static double CurrentTimeUtcSeconds => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

    /// <summary>
    /// Bắt đầu đếm ngược thời gian hồi (Cooldown) cho một vị trí quảng cáo.
    /// Mặc định: 60 giây (1 phút).
    /// </summary>
    public static void StartCooldown(AdPlacement placement, float durationSeconds = DefaultCooldownDuration)
    {
        if (durationSeconds <= 0f) return;

        double endUtc = CurrentTimeUtcSeconds + durationSeconds;
        inMemoryCooldownEndUtc[placement] = endUtc;

        try
        {
            PlayerPrefs.SetString(CooldownKeyPrefix + placement, endUtc.ToString("R", CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AdRewardService] Failed to save cooldown PlayerPrefs: {ex.Message}");
        }

        OnAdCooldownStarted?.Invoke(placement, durationSeconds);
    }

    /// <summary>
    /// Kiểm tra xem vị trí quảng cáo có đang trong thời gian Cooldown hay không.
    /// </summary>
    public static bool IsOnCooldown(AdPlacement placement = AdPlacement.Global)
    {
        if (IgnoreCooldownForTesting) return false;
        return GetRemainingCooldown(placement) > 0.05f;
    }

    /// <summary>
    /// Lấy số giây Cooldown còn lại của vị trí quảng cáo (trả về 0 nếu đã hết cooldown).
    /// </summary>
    public static float GetRemainingCooldown(AdPlacement placement = AdPlacement.Global)
    {
        if (IgnoreCooldownForTesting) return 0f;

        double now = CurrentTimeUtcSeconds;
        double endUtc = 0.0;

        if (inMemoryCooldownEndUtc.TryGetValue(placement, out double memEnd))
        {
            endUtc = memEnd;
        }
        else
        {
            string key = CooldownKeyPrefix + placement;
            if (PlayerPrefs.HasKey(key))
            {
                string stored = PlayerPrefs.GetString(key, string.Empty);
                if (double.TryParse(stored, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
                {
                    endUtc = parsed;
                    inMemoryCooldownEndUtc[placement] = endUtc;
                }
            }
        }

        double remaining = endUtc - now;
        return remaining > 0.0 ? (float)remaining : 0f;
    }

    /// <summary>
    /// Reset cooldown cho một vị trí hoặc toàn bộ (dùng cho test/debug).
    /// </summary>
    public static void ResetCooldown(AdPlacement placement)
    {
        inMemoryCooldownEndUtc.Remove(placement);
        string key = CooldownKeyPrefix + placement;
        if (PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }

    public static void ResetAllCooldowns()
    {
        inMemoryCooldownEndUtc.Clear();
        foreach (AdPlacement p in (AdPlacement[])Enum.GetValues(typeof(AdPlacement)))
        {
            string key = CooldownKeyPrefix + p;
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
            }
        }
        PlayerPrefs.Save();
    }

    // =========================================================================
    // SHOW REWARDED AD
    // =========================================================================

    /// <summary>
    /// Yêu cầu xem quảng cáo nhận thưởng toàn cục (vị trí Global).
    /// </summary>
    public static void ShowRewardedAd(Action<bool> onComplete)
    {
        ShowRewardedAd(AdPlacement.Global, onComplete);
    }

    /// <summary>
    /// Yêu cầu xem quảng cáo nhận thưởng cho một vị trí cụ thể (Hồi sinh, Daily...).
    /// - Tự động kiểm tra mạng (Wifi / 4G).
    /// - Tự động kiểm tra Cooldown 1 phút (60s).
    /// - Khi xem thành công -> tự động kích hoạt Cooldown 1 phút.
    /// </summary>
    public static void ShowRewardedAd(AdPlacement placement, Action<bool> onComplete)
    {
        if (!IsNetworkAvailable)
        {
            Debug.LogWarning("[AdRewardService] ⚠️ Không có kết nối mạng để tải quảng cáo!");
            onComplete?.Invoke(false);
            return;
        }

        if (IsOnCooldown(placement))
        {
            float remaining = GetRemainingCooldown(placement);
            Debug.LogWarning($"[AdRewardService] ⚠️ Quảng cáo '{placement}' đang trong thời gian Cooldown ({remaining:F1}s còn lại)!");
            onComplete?.Invoke(false);
            return;
        }

        void WrappedComplete(bool success)
        {
            onComplete?.Invoke(success);
        }

        if (OnRewardedAdRequested != null)
        {
            OnRewardedAdRequested.Invoke(WrappedComplete);
        }
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Simulation is deliberately restricted to non-production builds. A release
            // build must never issue a reward merely because the ads provider was omitted.
            Debug.LogWarning($"[AdRewardService] Simulating a rewarded ad for '{placement}' in a non-production build.");
            WrappedComplete(true);
#else
            Debug.LogError("[AdRewardService] Reward rejected: no rewarded-ad provider is configured.");
            WrappedComplete(false);
#endif
        }
    }

    // =========================================================================
    // SHOW INTERSTITIAL AD (VICTORY / DEFEAT / RUN END)
    // =========================================================================

    /// <summary>
    /// Yêu cầu hiển thị quảng cáo Interstitial toàn màn hình (khi Thắng hoặc Thua).
    /// Tự động kiểm tra mạng, nếu offline hoặc chưa cấu hình provider thì hoàn thành ngay (fallback an toàn).
    /// </summary>
    public static void ShowInterstitialAd(Action onComplete)
    {
        if (!IsNetworkAvailable)
        {
            Debug.LogWarning("[AdRewardService] ⚠️ Không có kết nối mạng để tải quảng cáo Interstitial.");
            onComplete?.Invoke();
            return;
        }

        if (OnInterstitialAdRequested != null)
        {
            OnInterstitialAdRequested.Invoke(onComplete);
        }
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("[AdRewardService] [Editor/Dev] Simulating interstitial ad completed.");
            onComplete?.Invoke();
#else
            onComplete?.Invoke();
#endif
        }
    }

    /// <summary>
    /// Hiển thị quảng cáo khi kết thúc lượt chơi (Thắng hoặc Thua) trước khi về MainMenu.
    /// </summary>
    public static void ShowRunEndAd(Action onComplete)
    {
        ShowInterstitialAd(onComplete);
    }
}
