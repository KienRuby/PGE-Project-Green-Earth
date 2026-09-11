using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ChipTier
{
    Magic = 1,       // Tier 1: Max LV. 6
    Rare = 2,        // Tier 2: Max LV. 9
    Unique = 3,      // Tier 3: Max LV. 14
    Epic = 4,        // Tier 4: Max LV. 18
    Holographic = 5  // Tier 5 / Advance Tier: Max LV. 24 (Requires 10 Advance Stones to Breakthrough)
}

[Serializable]
public class ChipItemData
{
    public int id;
    public string chipName;
    public string iconKey;
    public ChipTier tier = ChipTier.Magic;
    public int level = 1;
    public int count = 0;
    public int requiredCount = 5;
    public int enhanceCost = 500;
    public int tierEnhanceCount;
    public bool hasStar;

    [NonSerialized] private bool tierUnlockRulesEnabled;
    [NonSerialized] private int requiredTierEnhances = 10;
    [NonSerialized] private int greenToBlueFragmentCost = 5;
    [NonSerialized] private int blueToPurpleFragmentCost = 10;
    [NonSerialized] private int purpleToYellowFragmentCost = 15;
    [NonSerialized] private int yellowToRedFragmentCost = 20;
    [NonSerialized] private int yellowToRedDataChipCost = 100;

    [Header("Stats Description")]
    [TextArea(2, 4)]
    public string description;
    public string baseStatsSummary;
    public string magicBonus;
    public string rareBonus;
    public string uniqueBonus;
    public string epicBonus;

    public static int GetMaxLevelForTier(ChipTier tier)
    {
        switch (tier)
        {
            case ChipTier.Magic: return 6;
            case ChipTier.Rare: return 9;
            case ChipTier.Unique: return 14;
            case ChipTier.Epic: return 18;
            case ChipTier.Holographic: return 24;
            default: return 6;
        }
    }

    public int MaxLevel => GetMaxLevelForTier(tier);
    public bool IsAtTierCap => level >= MaxLevel;
    public bool IsMaxOverall => tier == ChipTier.Holographic && level >= 24;
    public bool NeedsAdvanceStones => tier == ChipTier.Epic && IsAtTierCap;
    public int AdvanceStoneCost => NeedsAdvanceStones ? 10 : 0;

    public int RequiredTierEnhances => requiredTierEnhances;
    public bool IsTierUnlockReady => tierUnlockRulesEnabled
        ? tierEnhanceCount >= requiredTierEnhances
        : IsAtTierCap;
    public bool UsesRedDataChipForAdvance => tierUnlockRulesEnabled && tier == ChipTier.Epic;
    public int YellowToRedDataChipCost => yellowToRedDataChipCost;
    public int YellowToRedFragmentCost => yellowToRedFragmentCost;

    public int CurrentAdvanceCost
    {
        get
        {
            switch (tier)
            {
                case ChipTier.Magic: return greenToBlueFragmentCost;
                case ChipTier.Rare: return blueToPurpleFragmentCost;
                case ChipTier.Unique: return purpleToYellowFragmentCost;
                case ChipTier.Epic: return yellowToRedFragmentCost;
                default: return 0;
            }
        }
    }

    public bool HasAdvanceCurrency
    {
        get
        {
            if (tier >= ChipTier.Holographic) return false;
            if (tier == ChipTier.Epic && tierUnlockRulesEnabled)
            {
                bool hasFragments = count >= yellowToRedFragmentCost;
                bool hasRedGems = yellowToRedDataChipCost <= 0 || ChipManager.HasEnoughRedGems(yellowToRedDataChipCost);
                return hasFragments && hasRedGems;
            }
            return count >= CurrentAdvanceCost;
        }
    }

    public bool IsMaxEnhanceForCurrentFrame
    {
        get
        {
            if (tier >= ChipTier.Holographic)
            {
                return IsMaxOverall || level >= MaxLevel;
            }
            if (tierUnlockRulesEnabled)
            {
                return tierEnhanceCount >= requiredTierEnhances || level >= MaxLevel;
            }
            return IsAtTierCap;
        }
    }

    public bool CanEnhance => !IsMaxEnhanceForCurrentFrame && ChipManager.HasEnoughDataChips(enhanceCost);
    public bool CanUpgrade => !tierUnlockRulesEnabled && !IsAtTierCap && count >= requiredCount && requiredCount > 0;
    public bool CanAdvanceTier => tierUnlockRulesEnabled
        ? tier < ChipTier.Holographic && IsTierUnlockReady && HasAdvanceCurrency
        : IsAtTierCap && tier < ChipTier.Holographic;

    public void ConfigureTierUnlockRules(
        int enhancesRequired,
        int greenToBlueCost,
        int blueToPurpleCost,
        int purpleToYellowCost,
        int yellowToRedCost,
        int yellowToRedFragments = 0)
    {
        tierUnlockRulesEnabled = true;
        requiredTierEnhances = Mathf.Max(1, enhancesRequired);
        greenToBlueFragmentCost = Mathf.Max(0, greenToBlueCost);
        blueToPurpleFragmentCost = Mathf.Max(0, blueToPurpleCost);
        purpleToYellowFragmentCost = Mathf.Max(0, purpleToYellowCost);
        yellowToRedDataChipCost = Mathf.Max(0, yellowToRedCost);
        yellowToRedFragmentCost = Mathf.Max(0, yellowToRedFragments);
        tierEnhanceCount = Mathf.Clamp(tierEnhanceCount, 0, requiredTierEnhances);

        if (tier < ChipTier.Holographic)
        {
            requiredCount = CurrentAdvanceCost;
        }
    }

    public bool Enhance()
    {
        if (!CanEnhance) return false;
        if (!ChipManager.TrySpendDataChips(enhanceCost)) return false;
        if (tierUnlockRulesEnabled)
        {
            if (tier < ChipTier.Holographic)
            {
                tierEnhanceCount++;
            }
            level = Mathf.Min(level + 1, MaxLevel);
        }
        else
        {
            level++;
        }
        enhanceCost = Mathf.RoundToInt(enhanceCost * 1.35f);
        return true;
    }

    public void Upgrade()
    {
        if (!CanUpgrade) return;
        count -= requiredCount;
        level++;
        if (level >= MaxLevel)
        {
            requiredCount = 0;
        }
        else
        {
            requiredCount = Mathf.RoundToInt(requiredCount * 1.4f) + 1;
        }
    }

    public bool AdvanceTier()
    {
        if (!CanAdvanceTier) return false;

        if (tierUnlockRulesEnabled)
        {
            if (tier == ChipTier.Epic)
            {
                if (yellowToRedDataChipCost > 0 && !ChipManager.TrySpendRedGems(yellowToRedDataChipCost))
                {
                    return false;
                }
                if (yellowToRedFragmentCost > 0)
                {
                    count -= yellowToRedFragmentCost;
                }
            }
            else
            {
                count -= CurrentAdvanceCost;
            }
        }
        else if (NeedsAdvanceStones)
        {
            if (!ChipManager.TrySpendAdvanceStones(10))
            {
                return false;
            }
        }

        tier = (ChipTier)((int)tier + 1);
        if (tierUnlockRulesEnabled)
        {
            tierEnhanceCount = 0;
            if (tier < ChipTier.Holographic)
            {
                requiredCount = CurrentAdvanceCost;
            }
            else
            {
                requiredCount = 0;
            }
        }
        else
        {
            requiredCount = Mathf.Max(3, level + 1);
        }
        return true;
    }

    public ChipItemData Clone()
    {
        return new ChipItemData
        {
            id = this.id,
            chipName = this.chipName,
            iconKey = this.iconKey,
            tier = this.tier,
            level = this.level,
            count = this.count,
            requiredCount = this.requiredCount,
            enhanceCost = this.enhanceCost,
            tierEnhanceCount = this.tierEnhanceCount,
            hasStar = this.hasStar,
            description = this.description,
            baseStatsSummary = this.baseStatsSummary,
            magicBonus = this.magicBonus,
            rareBonus = this.rareBonus,
            uniqueBonus = this.uniqueBonus,
            epicBonus = this.epicBonus,
            tierUnlockRulesEnabled = this.tierUnlockRulesEnabled,
            requiredTierEnhances = this.requiredTierEnhances,
            greenToBlueFragmentCost = this.greenToBlueFragmentCost,
            blueToPurpleFragmentCost = this.blueToPurpleFragmentCost,
            purpleToYellowFragmentCost = this.purpleToYellowFragmentCost,
            yellowToRedFragmentCost = this.yellowToRedFragmentCost,
            yellowToRedDataChipCost = this.yellowToRedDataChipCost
        };
    }
}

