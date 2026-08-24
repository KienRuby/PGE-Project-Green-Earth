using System;
using UnityEngine;

/// <summary>
/// Định nghĩa các loại tài nguyên / phần thưởng trong Project Green Earth.
/// Tương thích hoàn toàn với ChipManager và PlayerDataService.
/// </summary>
public enum RewardType
{
    Energy,
    DataChip,
    RedGem,
    AdvanceStone
}

/// <summary>
/// Cấu trúc dữ liệu phần thưởng chung cho Daily Login, Achievements, Quests.
/// </summary>
[Serializable]
public struct RewardData
{
    [Tooltip("Loại tài nguyên nhận được.")]
    public RewardType type;

    [Tooltip("Số lượng tài nguyên.")]
    public int amount;

    [Tooltip("Icon tùy chỉnh cho phần thưởng. Nếu để trống sẽ tự động lấy icon mặc định từ atlas/database.")]
    public Sprite customIcon;

    public RewardData(RewardType type, int amount, Sprite customIcon = null)
    {
        this.type = type;
        this.amount = amount;
        this.customIcon = customIcon;
    }
}
