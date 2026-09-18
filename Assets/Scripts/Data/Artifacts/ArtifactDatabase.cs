using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Cơ sở dữ liệu chứa toàn bộ các Cổ Vật (Artifact) trong game.
/// Cho phép truy vấn, lấy ngẫu nhiên và tự động sinh dữ liệu mặc định nếu chưa gán Asset.
/// </summary>
[CreateAssetMenu(fileName = "ArtifactDatabase", menuName = "PGE/Data/Artifact Database", order = 51)]
public class ArtifactDatabase : ScriptableObject
{
    private static ArtifactDatabase runtimeInstance;
    public static ArtifactDatabase Instance
    {
        get
        {
            if (runtimeInstance == null)
            {
                runtimeInstance = Resources.Load<ArtifactDatabase>("ArtifactDatabase");
                if (runtimeInstance == null)
                {
                    runtimeInstance = CreateInstance<ArtifactDatabase>();
                    runtimeInstance.InitializeDefaults();
                }
            }
            return runtimeInstance;
        }
    }

    [Header("Artifact Catalog")]
    [Tooltip("Danh sách toàn bộ các loại Artifact trong game.")]
    public List<ArtifactData> artifacts = new List<ArtifactData>();

    private void OnEnable()
    {
        if (artifacts == null || artifacts.Count == 0)
        {
            InitializeDefaults();
        }
    }

    /// <summary>
    /// Khởi tạo 7 Cổ vật chính thức nếu danh mục đang trống.
    /// </summary>
    public void InitializeDefaults()
    {
        if (artifacts == null) artifacts = new List<ArtifactData>();
        if (artifacts.Count > 0) return;

        // 1. Data Disc (CD) - Crit Rate +10%
        ArtifactData disc = CreateInstance<ArtifactData>();
        disc.id = "data_disc";
        disc.artifactName = "Data Disc";
        disc.loreDescription = "Shiny optical disc storing lost battle simulations and ancient data.";
        disc.statType = ArtifactStatType.CritRatePercent;
        disc.statValue = 10f;
        disc.icon = Resources.Load<Sprite>("UI/Artifact/data_disc");
        artifacts.Add(disc);

        // 2. Modular Brick (LEGO) - DEF +12
        ArtifactData brick = CreateInstance<ArtifactData>();
        brick.id = "modular_brick";
        brick.artifactName = "Modular Brick";
        brick.loreDescription = "Interlocking plastic toy brick. Incredibly durable construction.";
        brick.statType = ArtifactStatType.DamageReduction;
        brick.statValue = 12f;
        brick.icon = Resources.Load<Sprite>("UI/Artifact/modular_brick");
        artifacts.Add(brick);

        // 3. Kung Fu Data USB (USB) - All Weapons' ATK +9%
        ArtifactData usb = CreateInstance<ArtifactData>();
        usb.id = "kung_fu_usb";
        usb.artifactName = "Kung Fu Data USB";
        usb.loreDescription = "Does it actually have the Epic tome of Kung Fu in it?";
        usb.statType = ArtifactStatType.AllWeaponsDamagePercent;
        usb.statValue = 9f;
        usb.icon = Resources.Load<Sprite>("UI/Artifact/kung_fu_usb");
        artifacts.Add(usb);

        // 4. Energy Butter (Bơ / Phô mai) - HP +15%
        ArtifactData butter = CreateInstance<ArtifactData>();
        butter.id = "energy_butter";
        butter.artifactName = "Energy Butter";
        butter.loreDescription = "High-calorie organic nutrient block that enhances biological vitality.";
        butter.statType = ArtifactStatType.MaxHealthPercent;
        butter.statValue = 15f;
        butter.icon = Resources.Load<Sprite>("UI/Artifact/energy_butter");
        artifacts.Add(butter);

        // 5. Strong Cooler (Quạt tản nhiệt) - Turret ATK Speed +20%
        ArtifactData cooler = CreateInstance<ArtifactData>();
        cooler.id = "strong_cooler";
        cooler.artifactName = "Strong Cooler";
        cooler.loreDescription = "Cools down Turrets when they overheat.";
        cooler.statType = ArtifactStatType.TurretAttackSpeedPercent;
        cooler.statValue = 20f;
        cooler.icon = Resources.Load<Sprite>("UI/Artifact/strong_cooler");
        artifacts.Add(cooler);

        // 6. Signal Rocket (Tên lửa / Pháo) - Move Speed +12%
        ArtifactData rocket = CreateInstance<ArtifactData>();
        rocket.id = "signal_rocket";
        rocket.artifactName = "Signal Rocket";
        rocket.loreDescription = "Miniature rocket propulsion unit. Boosts overall movement speed.";
        rocket.statType = ArtifactStatType.MoveSpeedPercent;
        rocket.statValue = 12f;
        rocket.icon = Resources.Load<Sprite>("UI/Artifact/signal_rocket");
        artifacts.Add(rocket);

        // 7. Quantum Microchip (Chip vi mạch) - Ranged DEF +15%
        ArtifactData chip = CreateInstance<ArtifactData>();
        chip.id = "quantum_chip";
        chip.artifactName = "Quantum Microchip";
        chip.loreDescription = "Advanced silicon processor that calculates incoming ranged projectile vectors.";
        chip.statType = ArtifactStatType.RangedDefensePercent;
        chip.statValue = 15f;
        chip.icon = Resources.Load<Sprite>("UI/Artifact/quantum_chip");
        artifacts.Add(chip);
    }

    /// <summary>
    /// Lấy ngẫu nhiên 1 Artifact từ kho dữ liệu, có thể loại trừ các ID đã sở hữu nếu muốn.
    /// </summary>
    public ArtifactData GetRandomArtifact(IReadOnlyList<string> excludeIds = null)
    {
        if (artifacts == null || artifacts.Count == 0) InitializeDefaults();

        List<ArtifactData> pool = new List<ArtifactData>();
        foreach (var art in artifacts)
        {
            if (art == null) continue;
            if (excludeIds != null)
            {
                bool isExcluded = false;
                for (int i = 0; i < excludeIds.Count; i++)
                {
                    if (string.Equals(excludeIds[i], art.id, System.StringComparison.Ordinal))
                    {
                        isExcluded = true;
                        break;
                    }
                }
                if (isExcluded) continue;
            }
            pool.Add(art);
        }

        if (pool.Count == 0)
        {
            // Nếu đã sở hữu hết hoặc pool rỗng, lấy từ toàn bộ danh sách
            if (artifacts.Count > 0)
                return artifacts[Random.Range(0, artifacts.Count)];
            return null;
        }

        return pool[Random.Range(0, pool.Count)];
    }

    /// <summary>
    /// Tìm Artifact theo ID.
    /// </summary>
    public ArtifactData GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (artifacts == null || artifacts.Count == 0) InitializeDefaults();

        var found = artifacts.Find(a => a != null && a.id == id);
        if (found != null) return found;

        // Fallback: nếu chưa có trong asset database (ví dụ asset chưa được cập nhật từ code/event)
        var fallbackDb = CreateInstance<ArtifactDatabase>();
        fallbackDb.InitializeDefaults();
        found = fallbackDb.artifacts.Find(a => a != null && a.id == id);
        if (found != null)
        {
            artifacts.Add(found);
            return found;
        }

        return null;
    }
}
