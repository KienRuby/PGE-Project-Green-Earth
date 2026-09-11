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
    /// Khởi tạo 4 Artifact mẫu theo 4 ảnh chụp thực tế nếu danh mục đang trống.
    /// </summary>
    public void InitializeDefaults()
    {
        if (artifacts == null) artifacts = new List<ArtifactData>();
        if (artifacts.Count > 0) return;

        // 1. Spare Battery (HP +15%)
        ArtifactData battery = CreateInstance<ArtifactData>();
        battery.id = "spare_battery";
        battery.artifactName = "Spare Battery";
        battery.loreDescription = "Eco-friendly product you can recharge.";
        battery.statType = ArtifactStatType.MaxHealthPercent;
        battery.statValue = 15f;
        artifacts.Add(battery);

        // 2. Carbon Scales (Ranged DEF +10%)
        ArtifactData scales = CreateInstance<ArtifactData>();
        scales.id = "carbon_scales";
        scales.artifactName = "Carbon Scales";
        scales.loreDescription = "Vinyl 1, it likes me.\nVinyl 2, it doesn't like me.";
        scales.statType = ArtifactStatType.RangedDefensePercent;
        scales.statValue = 10f;
        artifacts.Add(scales);

        // 3. Strong Cooler (Turret ATK Speed +20%)
        ArtifactData cooler = CreateInstance<ArtifactData>();
        cooler.id = "strong_cooler";
        cooler.artifactName = "Strong Cooler";
        cooler.loreDescription = "Cools down Turrets when they overheat.";
        cooler.statType = ArtifactStatType.TurretAttackSpeedPercent;
        cooler.statValue = 20f;
        artifacts.Add(cooler);

        // 4. Kung Fu Data USB (All Weapons' ATK +9%)
        ArtifactData usb = CreateInstance<ArtifactData>();
        usb.id = "kung_fu_usb";
        usb.artifactName = "Kung Fu Data USB";
        usb.loreDescription = "Does it actually have the Epic tome of Kung Fu in it?";
        usb.statType = ArtifactStatType.AllWeaponsDamagePercent;
        usb.statValue = 9f;
        artifacts.Add(usb);

        // 5. Titanium Fabric (DEF +10)
        ArtifactData titanium = CreateInstance<ArtifactData>();
        titanium.id = "titanium_fabric";
        titanium.artifactName = "Titanium Fabric";
        titanium.loreDescription = "Sturdy titanium. Covers the body.";
        titanium.statType = ArtifactStatType.DamageReduction;
        titanium.statValue = 10f;
        artifacts.Add(titanium);

        // 6. Metal Band-aid (HP +12%)
        ArtifactData bandAid = CreateInstance<ArtifactData>();
        bandAid.id = "metal_band_aid";
        bandAid.artifactName = "Metal Band-aid";
        bandAid.loreDescription = "A specialized nanotech medical bandage found in abandoned bunkers.";
        bandAid.statType = ArtifactStatType.MaxHealthPercent;
        bandAid.statValue = 12f;
        artifacts.Add(bandAid);
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
