using System;
using System.Collections.Generic;

/// <summary>
/// Dữ liệu cấu hình thiết kế tĩnh (Static Design Definition) của một loại Buddy.
/// Tách biệt hoàn toàn với dữ liệu runtime / save của người chơi (BuddyItemData).
/// </summary>
[Serializable]
public class BuddyDefinition
{
    public int id;
    public string buddyName;
    public string iconKey;
    public BuddyTier defaultTier = BuddyTier.Common;
    public int defaultLevel = 1;
    public int defaultCount = 0;
    public int defaultRequiredCount = 10;
    public int defaultEnhanceCost = 500;
    public string description;
    public string baseStatText;
    public string magicPerkText;
    public string rarePerkText;
    public string uniquePerkText;
    public string epicPerkText;
}

/// <summary>
/// Database tập trung quản lý định nghĩa thiết kế của toàn bộ Buddy trong dự án.
/// Single source of truth cho thông số mặc định của mọi loại Companion Drone.
/// </summary>
public static class BuddyDatabase
{
    public static readonly int[] PlayableIds = { 1, 2, 3, 4, 10 };

    private static readonly Dictionary<int, BuddyDefinition> Definitions = new Dictionary<int, BuddyDefinition>
    {
        {
            1, new BuddyDefinition
            {
                id = 1,
                buddyName = "Sloy",
                iconKey = "drone-snowflake",
                defaultTier = BuddyTier.Common,
                defaultLevel = 1,
                defaultCount = 0,
                defaultRequiredCount = 10,
                defaultEnhanceCost = 3500,
                description = "Fires shells that slow down enemies.",
                baseStatText = "Drone ATK 20.4, Slow ATK Speed",
                magicPerkText = "Frost Shell +20%",
                rarePerkText = "Frost Shell +20%",
                uniquePerkText = "Area Slow +30%",
                epicPerkText = "Blizzard Blast +30%"
            }
        },
        {
            2, new BuddyDefinition
            {
                id = 2,
                buddyName = "Turret Buffer",
                iconKey = "drone-spider",
                defaultTier = BuddyTier.Common,
                defaultLevel = 1,
                defaultCount = 0,
                defaultRequiredCount = 10,
                defaultEnhanceCost = 500,
                description = "Improves the skills of all Turrets.",
                baseStatText = "All Turrets' Duration <color=#FFCB49>10%</color>",
                magicPerkText = "Turret Duration +20%",
                rarePerkText = "Turret Duration +30%",
                uniquePerkText = "Turret Duration +30%",
                epicPerkText = "Turret Duration +30%"
            }
        },
        {
            3, new BuddyDefinition
            {
                id = 3,
                buddyName = "Radar Eye",
                iconKey = "drone-antenna-eye",
                defaultTier = BuddyTier.Common,
                defaultLevel = 1,
                defaultCount = 0,
                defaultRequiredCount = 10,
                defaultEnhanceCost = 500,
                description = "Scans hostiles and pinpoints critical weaknesses.",
                baseStatText = "All Weapons' CRIT Rate <color=#FFCB49>+5%</color>",
                magicPerkText = "CRIT Damage +20%",
                rarePerkText = "Scan Range +30%",
                uniquePerkText = "Weakpoint Bonus +30%",
                epicPerkText = "Target Lock +30%"
            }
        },
        {
            4, new BuddyDefinition
            {
                id = 4,
                buddyName = "Assault Blaster",
                iconKey = "drone-cross-visor",
                defaultTier = BuddyTier.Common,
                defaultLevel = 1,
                defaultCount = 0,
                defaultRequiredCount = 10,
                defaultEnhanceCost = 500,
                description = "Continuous twin blaster providing direct firepower.",
                baseStatText = "All Weapons' ATK <color=#FFCB49>+12%</color>",
                magicPerkText = "Blaster ATK +20%",
                rarePerkText = "Fire Rate +30%",
                uniquePerkText = "Dual Shot ATK +30%",
                epicPerkText = "Overheat Surge +30%"
            }
        },
        {
            10, new BuddyDefinition
            {
                id = 10,
                buddyName = "Purifying Drone",
                iconKey = "drone-stealth-wing",
                defaultTier = BuddyTier.Common,
                defaultLevel = 1,
                defaultCount = 0,
                defaultRequiredCount = 10,
                defaultEnhanceCost = 500,
                description = "Increase the ratio of\nAilment Resistance",
                baseStatText = "Ailment Resistance 5%",
                magicPerkText = "Ailment Resistance +5%",
                rarePerkText = "Ailment Resistance +7%",
                uniquePerkText = "Ailment Resistance +9%",
                epicPerkText = "Remove Ailment Instantly (cooldown 30s)"
            }
        }
    };

    public static bool IsPlayable(int id)
    {
        return Definitions.ContainsKey(id);
    }

    public static BuddyDefinition GetDefinition(int id)
    {
        return Definitions.TryGetValue(id, out var def) ? def : null;
    }

    public static BuddyItemData CreateDefaultRuntimeData(int id)
    {
        var def = GetDefinition(id);
        if (def == null) return null;

        return new BuddyItemData
        {
            id = def.id,
            buddyName = def.buddyName,
            iconKey = def.iconKey,
            tier = def.defaultTier,
            level = def.defaultLevel,
            count = def.defaultCount,
            requiredCount = def.defaultRequiredCount,
            enhanceCost = def.defaultEnhanceCost,
            isUnlocked = true,
            description = def.description,
            baseStatText = def.baseStatText,
            magicPerkText = def.magicPerkText,
            rarePerkText = def.rarePerkText,
            uniquePerkText = def.uniquePerkText,
            epicPerkText = def.epicPerkText
        };
    }
}
