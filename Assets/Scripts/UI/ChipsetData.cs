using System;
using UnityEngine;

public enum ChipTier
{
    Common = 0,
    Magic = 1,
    Rare = 2,
    Unique = 3,
    Epic = 4,
    Holographic = 5
}

[Serializable]
public class ChipItemData
{
    public int id;
    public string chipName;
    public string iconKey;
    public ChipTier tier;
    public int level = 1;
    public int count = 0;
    public int requiredCount = 3;
    public bool hasStar;

    [Header("Stats Description")]
    [TextArea(2, 4)]
    public string baseStatsSummary;
    public string magicBonus;
    public string rareBonus;
    public string uniqueBonus;
    public string epicBonus;

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
            hasStar = this.hasStar,
            baseStatsSummary = this.baseStatsSummary,
            magicBonus = this.magicBonus,
            rareBonus = this.rareBonus,
            uniqueBonus = this.uniqueBonus,
            epicBonus = this.epicBonus
        };
    }

    public bool CanUpgrade => count >= requiredCount && requiredCount > 0;

    public void Upgrade()
    {
        if (!CanUpgrade) return;
        count -= requiredCount;
        level++;
        // Required fragments increase progressively
        requiredCount = Mathf.RoundToInt(requiredCount * 1.5f) + 1;
    }
}
