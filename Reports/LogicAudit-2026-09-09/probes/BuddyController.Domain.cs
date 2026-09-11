using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum BuddyTier
{
    Common = 0,
    Magic = 1,
    Rare = 2,
    Unique = 3,
    Epic = 4,
    Holographic = 5
}

[Serializable]
public class BuddyItemData
{
    public int id;
    public string buddyName;
    public string iconKey;
    public BuddyTier tier = BuddyTier.Common;
    public int level = 1;
    public int count = 0;
    public int requiredCount = 3;
    public int enhanceCost = 500;
    public string description;
    public string baseStatText;
    public string magicPerkText;
    public string rarePerkText;
    public string uniquePerkText;
    public string epicPerkText;

    public bool CanEnhance => ChipManager.DataChips >= enhanceCost;
    public bool CanAdvanceTier => count >= requiredCount && requiredCount > 0;
    public bool CanUpgrade => CanEnhance || CanAdvanceTier;

    public bool Enhance()
    {
        if (ChipManager.DataChips < enhanceCost) return false;
        if (!ChipManager.TrySpendDataChips(enhanceCost)) return false;
        level++;
        enhanceCost = Mathf.RoundToInt(enhanceCost * 1.35f);
        return true;
    }

    public bool AdvanceTier()
    {
        if (!CanAdvanceTier) return false;
        count -= requiredCount;
        tier = (BuddyTier)Mathf.Min((int)tier + 1, (int)BuddyTier.Holographic);
        requiredCount = Mathf.RoundToInt(requiredCount * 1.6f) + 1;
        return true;
    }

    public BuddyItemData Clone()
    {
        return new BuddyItemData
        {
            id = this.id,
            buddyName = this.buddyName,
            iconKey = this.iconKey,
            tier = this.tier,
            level = this.level,
            count = this.count,
            requiredCount = this.requiredCount,
            enhanceCost = this.enhanceCost,
            description = this.description,
            baseStatText = this.baseStatText,
            magicPerkText = this.magicPerkText,
            rarePerkText = this.rarePerkText,
            uniquePerkText = this.uniquePerkText,
            epicPerkText = this.epicPerkText
        };
    }
}

