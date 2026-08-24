using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Service trung tâm chịu trách nhiệm trao các phần thưởng (Energy, Data Chips, Red Gems, Advance Stones)
/// trực tiếp vào hệ thống tiền tệ hiện có (ChipManager & PlayerDataService).
/// Đảm bảo không tạo thêm hệ thống currency thứ 2 và chống trùng lặp dữ liệu.
/// </summary>
public static class RewardService
{
    public static event Action<RewardData> OnRewardGranted;

    /// <summary>
    /// Trao một phần thưởng đơn lẻ.
    /// </summary>
    public static bool GrantReward(RewardData reward)
    {
        if (reward.amount <= 0) return false;

        switch (reward.type)
        {
            case RewardType.Energy:
                ChipManager.AddEnergy(reward.amount);
                break;
            case RewardType.DataChip:
                ChipManager.AddDataChips(reward.amount);
                break;
            case RewardType.RedGem:
                ChipManager.AddRedGems(reward.amount);
                break;
            case RewardType.AdvanceStone:
                ChipManager.AddAdvanceStones(reward.amount);
                break;
            default:
                Debug.LogWarning($"[RewardService] Chưa hỗ trợ RewardType: {reward.type}");
                return false;
        }

        OnRewardGranted?.Invoke(reward);
        Debug.Log($"[RewardService] 🎁 Đã trao phần thưởng: +{reward.amount} {reward.type}");
        return true;
    }

    /// <summary>
    /// Trao danh sách nhiều phần thưởng cùng một lúc.
    /// </summary>
    public static void GrantRewards(IEnumerable<RewardData> rewards)
    {
        if (rewards == null) return;
        foreach (var reward in rewards)
        {
            GrantReward(reward);
        }
    }

    /// <summary>
    /// Định dạng số lượng phần thưởng hiển thị trên UI (ví dụ: "X30", "X300", "X1000", "X7000").
    /// </summary>
    public static string FormatRewardAmount(int amount)
    {
        return $"X{amount:N0}".Replace(",", ".");
    }
}
