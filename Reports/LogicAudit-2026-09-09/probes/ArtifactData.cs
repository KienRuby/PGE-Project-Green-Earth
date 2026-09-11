using System;
using UnityEngine;

/// <summary>
/// Các loại chỉ số buff mà Artifact có thể cung cấp cho người chơi trong suốt trận đấu.
/// </summary>
public enum ArtifactStatType
{
    MaxHealthPercent,          // Ví dụ: Spare Battery (HP +15%)
    RangedDefensePercent,      // Ví dụ: Carbon Scales (Ranged DEF +10%)
    TurretAttackSpeedPercent,  // Ví dụ: Strong Cooler (Turret ATK Speed +20%)
    AllWeaponsDamagePercent,   // Ví dụ: Kung Fu Data USB (All Weapons' ATK +9%)
    MoveSpeedPercent,          // Tăng % Tốc độ di chuyển
    CritRatePercent,           // Tăng % Tỉ lệ chí mạng
    HealthRegenPerSec,         // Tự hồi máu mỗi giây
    DamageReduction            // Giảm trừ sát thương trực tiếp
}

/// <summary>
/// ScriptableObject định nghĩa một Cổ Vật (Artifact) rơi trong gameplay.
/// Cho phép cấu hình qua Inspector hoặc nạp từ cơ sở dữ liệu.
/// </summary>
[CreateAssetMenu(fileName = "NewArtifact", menuName = "PGE/Data/Artifact", order = 50)]
public class ArtifactData : ScriptableObject
{
    [Header("Basic Identification")]
    [Tooltip("ID duy nhất của Artifact (ví dụ: 'spare_battery', 'carbon_scales').")]
    public string id;

    [Tooltip("Tên hiển thị của Artifact trên UI (màu vàng cam).")]
    public string artifactName;

    [TextArea(2, 4)]
    [Tooltip("Câu thoại mô tả lore / flavor text xuất hiện dưới tên vật phẩm.")]
    public string loreDescription;

    [Header("Visual Asset")]
    [Tooltip("Icon minh họa cho Artifact (dạng pixel art). Có thể để trống, hệ thống sẽ tự động dùng hình fallback/placeholder cho đến khi bạn bổ sung.")]
    public Sprite icon;

    [Tooltip("Màu viền thẻ Artifact (mặc định là xanh ngọc Cyan đặc trưng).")]
    public Color badgeBorderColor = new Color32(46, 229, 240, 255);

    [Tooltip("Màu nền thẻ Artifact (xanh đen thẫm).")]
    public Color badgeBgColor = new Color32(11, 45, 60, 255);

    [Header("Stat Modifiers")]
    [Tooltip("Loại chỉ số cộng thêm khi người chơi nhận vật phẩm này.")]
    public ArtifactStatType statType = ArtifactStatType.MaxHealthPercent;

    [Tooltip("Giá trị phần trăm hoặc số lượng cộng thêm (ví dụ 15 nghĩa là +15%).")]
    public float statValue = 15f;

    /// <summary>
    /// Định dạng chuỗi hiển thị chỉ số chính xác như trong các ảnh mẫu (ví dụ: 'HP +15%', 'Turret ATK Speed +20%').
    /// </summary>
    public string GetFormattedStatText()
    {
        switch (statType)
        {
            case ArtifactStatType.MaxHealthPercent:
                return $"HP +{statValue:0.#}%";
            case ArtifactStatType.RangedDefensePercent:
                return $"Ranged DEF +{statValue:0.#}%";
            case ArtifactStatType.TurretAttackSpeedPercent:
                return $"Turret ATK Speed +{statValue:0.#}%";
            case ArtifactStatType.AllWeaponsDamagePercent:
                return $"All Weapons' ATK +{statValue:0.#}%";
            case ArtifactStatType.MoveSpeedPercent:
                return $"Move Speed +{statValue:0.#}%";
            case ArtifactStatType.CritRatePercent:
                return $"Crit Rate +{statValue:0.#}%";
            case ArtifactStatType.HealthRegenPerSec:
                return $"Auto Recovery +{statValue:0.#}/sec";
            case ArtifactStatType.DamageReduction:
                return $"DEF +{statValue:0.#}";
            default:
                return $"{statType} +{statValue:0.#}%";
        }
    }
}
