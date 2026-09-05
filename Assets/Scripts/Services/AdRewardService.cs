using System;
using UnityEngine;

/// <summary>
/// Service quản lý hiển thị Rewarded Ad (Xem quảng cáo nhận thưởng) cho toàn bộ game:
/// - Kiểm tra trạng thái kết nối mạng (Wifi / Cellular).
/// - Cung cấp hook cho các SDK quảng cáo bên thứ 3 (IronSource, LevelPlay, Unity Ads, AdMob).
/// - Chế độ fallback mô phỏng thông minh: Cho phép test mượt mà trong Editor và thiết bị khi chưa cắm SDK.
/// </summary>
public static class AdRewardService
{
    public static event Action<Action<bool>> OnRewardedAdRequested;

    /// <summary>
    /// Cho phép cưỡng chế giả lập mất mạng để test trạng thái không có Wifi.
    /// </summary>
    public static bool ForceOfflineTestMode { get; set; } = false;

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

    /// <summary>
    /// Yêu cầu xem quảng cáo nhận thưởng.
    /// - onComplete(true): Xem thành công, trao thưởng.
    /// - onComplete(false): Thất bại hoặc hủy giữa chừng / không có mạng.
    /// </summary>
    public static void ShowRewardedAd(Action<bool> onComplete)
    {
        if (!IsNetworkAvailable)
        {
            Debug.LogWarning("[AdRewardService] ⚠️ Không có kết nối mạng để tải quảng cáo!");
            onComplete?.Invoke(false);
            return;
        }

        if (OnRewardedAdRequested != null)
        {
            OnRewardedAdRequested.Invoke(onComplete);
        }
        else
        {
            // Chế độ mô phỏng khi chưa cắm SDK thật
            Debug.Log("[AdRewardService] 🎬 Đang phát Rewarded Ad (Mô phỏng)...");
            onComplete?.Invoke(true);
        }
    }
}
