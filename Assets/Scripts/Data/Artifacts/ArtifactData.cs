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

    /// <summary>
    /// Lấy Icon chính thức của Artifact. Nếu chưa được gán trên Inspector, hàm sẽ tự động tìm kiếm trong Resources/Database theo ID, statType hoặc sprite dự phòng.
    /// </summary>
    public Sprite GetIcon()
    {
        if (icon != null) return icon;

        if (!string.IsNullOrEmpty(id))
        {
            // 1. Tìm trực tiếp theo ID trong Resources
            Sprite res = Resources.Load<Sprite>($"UI/Artifact/{id}");
            if (res != null) { icon = res; return res; }

            // Thử tên chuẩn hóa thường (lowercase, gạch dưới)
            string normalized = id.ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
            res = Resources.Load<Sprite>($"UI/Artifact/{normalized}");
            if (res != null) { icon = res; return res; }

            // 2. Tìm theo Database nếu có
            if (ArtifactDatabase.Instance != null)
            {
                var dbArt = ArtifactDatabase.Instance.GetById(id);
                if (dbArt != null && dbArt.icon != null) { icon = dbArt.icon; return dbArt.icon; }
            }

            // 3. Fallback theo từ khóa định danh đặc biệt
            if (normalized.Contains("band_aid") || normalized.Contains("hop_mau") || normalized.Contains("first_aid"))
            {
                Sprite s = Resources.Load<Sprite>("UI/Artifact/metal_band_aid") ?? Resources.Load<Sprite>("UI/ArtifactChest/hop_mau_nho");
                if (s != null) { icon = s; return s; }
            }
        }

#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(id))
        {
            string cleanId = id.ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
            string[] candidatePaths = new string[]
            {
                $"Assets/Sprites/UI/Artifact/{cleanId}.png",
                $"Assets/Sprites/UI/Artifact/{id}.png",
                $"Assets/Sprites/UI/Artifact/{cleanId.Replace("energy_", "").Replace("quantum_", "")}.png",
                "Assets/Sprites/UI/artifact 1/hop_mau_nho.png"
            };
            foreach (var p in candidatePaths)
            {
                Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (s != null) { icon = s; return s; }
            }
        }
#endif

        // 4. Fallback theo StatType từ Database
        if (ArtifactDatabase.Instance != null && ArtifactDatabase.Instance.artifacts != null)
        {
            var match = ArtifactDatabase.Instance.artifacts.Find(a => a != null && a.statType == statType && a.icon != null);
            if (match != null) { icon = match.icon; return icon; }
        }

        // 5. Fallback cuối cùng: nạp bất kỳ sprite artifact có sẵn nào để tuyệt đối không bao giờ bị ô vuông màu lỗi
        Sprite defaultFallback = Resources.Load<Sprite>("UI/Artifact/metal_band_aid")
                              ?? Resources.Load<Sprite>("UI/Artifact/quantum_core")
                              ?? Resources.Load<Sprite>("UI/Artifact/kung_fu_data_usb")
                              ?? Resources.Load<Sprite>("UI/Artifact/carbon_scales");
        if (defaultFallback != null) { icon = defaultFallback; return defaultFallback; }

        return null;
    }
}
