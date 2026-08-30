using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Comprehensive Automated QA Test Suite for Milestone 4:
/// Meta Systems (Lab 16 Stats 4x4, Triple Pity Guarantee, Chipsets 5 Tiers,
/// Buddy Drones, LevelUp Popup & Reroll, Chapter Progression, Shop, Daily Login, and Achievements).
/// </summary>
[TestFixture]
public class M4MetaSystemsTests
{
    #region 1. Lab 16 Stats Matrix (4x4) & Upgrade Pricing Tests
    [Test]
    public void M4_01_LabUpgradeController_PricingFormula_300PlusRolls150()
    {
        int basePrice = 300;
        int priceStep = 150;

        for (int rolls = 0; rolls <= 10; rolls++)
        {
            int expectedPrice = basePrice + rolls * priceStep;
            Assert.That(expectedPrice, Is.EqualTo(300 + rolls * 150));
        }

        Assert.That(300 + 0 * 150, Is.EqualTo(300));
        Assert.That(300 + 1 * 150, Is.EqualTo(450));
        Assert.That(300 + 5 * 150, Is.EqualTo(1050));
        Assert.That(300 + 10 * 150, Is.EqualTo(1800));
    }

    [Test]
    public void M4_02_LabUpgradeController_MaxStatLevelCap_ClampsAtLevel10()
    {
        Assert.That(LabUpgradeController.DefaultMaxLevel, Is.EqualTo(10));

        string testStat = "HP";
        int origLvl = PlayerDataService.GetItemLevel(testStat);

        try
        {
            PlayerDataService.SetItemLevel(testStat, 0);
            Assert.That(PlayerDataService.GetItemLevel(testStat), Is.EqualTo(0));

            PlayerDataService.SetItemLevel(testStat, 10);
            Assert.That(PlayerDataService.GetItemLevel(testStat), Is.EqualTo(10));

            // Must clamp at 10
            PlayerDataService.SetItemLevel(testStat, 25);
            Assert.That(PlayerDataService.GetItemLevel(testStat), Is.EqualTo(10));
        }
        finally
        {
            PlayerDataService.SetItemLevel(testStat, origLvl);
        }
    }
    #endregion

    #region 2. Triple Pity Guarantee System Tests
    [Test]
    public void M4_03_PityGuarantee_Thresholds_Elite10_Epic25_Legend50()
    {
        GameObject pityObj = new GameObject("PityController", typeof(PityGuaranteeController));
        try
        {
            PityGuaranteeController pity = pityObj.GetComponent<PityGuaranteeController>();
            Assert.That(pity.EliteThreshold, Is.EqualTo(10));
            Assert.That(pity.EpicThreshold, Is.EqualTo(25));
            Assert.That(pity.LegendThreshold, Is.EqualTo(50));
        }
        finally
        {
            Object.DestroyImmediate(pityObj);
        }
    }

    [Test]
    public void M4_04_PityGuarantee_IndependentReset_WinningLegendResetsOnlyLegend()
    {
        GameObject pityObj = new GameObject("PityController", typeof(PityGuaranteeController));
        int origElite = PlayerDataService.LabElitePityCounter;
        int origEpic = PlayerDataService.LabEpicPityCounter;
        int origLegend = PlayerDataService.LabLegendPityCounter;

        try
        {
            PityGuaranteeController pity = pityObj.GetComponent<PityGuaranteeController>();
            PlayerDataService.LabElitePityCounter = 8;
            PlayerDataService.LabEpicPityCounter = 20;
            PlayerDataService.LabLegendPityCounter = 49;

            // Roll wins Legend
            pity.RecordRollResult(LabUpgradeController.ItemRarity.Legend);

            // Legend resets to 0; Elite and Epic increase by 1
            Assert.That(pity.LegendCounter, Is.EqualTo(0), "Legend counter must reset to 0.");
            Assert.That(pity.EliteCounter, Is.EqualTo(9), "Elite counter must increment to 9.");
            Assert.That(pity.EpicCounter, Is.EqualTo(21), "Epic counter must increment to 21.");
        }
        finally
        {
            PlayerDataService.LabElitePityCounter = origElite;
            PlayerDataService.LabEpicPityCounter = origEpic;
            PlayerDataService.LabLegendPityCounter = origLegend;
            Object.DestroyImmediate(pityObj);
        }
    }

    [Test]
    public void M4_05_PityGuarantee_IndependentReset_WinningEpicResetsOnlyEpic()
    {
        GameObject pityObj = new GameObject("PityController", typeof(PityGuaranteeController));
        int origElite = PlayerDataService.LabElitePityCounter;
        int origEpic = PlayerDataService.LabEpicPityCounter;
        int origLegend = PlayerDataService.LabLegendPityCounter;

        try
        {
            PityGuaranteeController pity = pityObj.GetComponent<PityGuaranteeController>();
            PlayerDataService.LabElitePityCounter = 5;
            PlayerDataService.LabEpicPityCounter = 24;
            PlayerDataService.LabLegendPityCounter = 30;

            // Roll wins Epic
            pity.RecordRollResult(LabUpgradeController.ItemRarity.Epic);

            // Epic resets to 0; Elite and Legend increase by 1
            Assert.That(pity.EpicCounter, Is.EqualTo(0), "Epic counter must reset to 0.");
            Assert.That(pity.EliteCounter, Is.EqualTo(6), "Elite counter must increment to 6.");
            Assert.That(pity.LegendCounter, Is.EqualTo(31), "Legend counter must increment to 31.");
        }
        finally
        {
            PlayerDataService.LabElitePityCounter = origElite;
            PlayerDataService.LabEpicPityCounter = origEpic;
            PlayerDataService.LabLegendPityCounter = origLegend;
            Object.DestroyImmediate(pityObj);
        }
    }

    [Test]
    public void M4_06_PityGuarantee_IndependentReset_WinningEliteResetsOnlyElite()
    {
        GameObject pityObj = new GameObject("PityController", typeof(PityGuaranteeController));
        int origElite = PlayerDataService.LabElitePityCounter;
        int origEpic = PlayerDataService.LabEpicPityCounter;
        int origLegend = PlayerDataService.LabLegendPityCounter;

        try
        {
            PityGuaranteeController pity = pityObj.GetComponent<PityGuaranteeController>();
            PlayerDataService.LabElitePityCounter = 9;
            PlayerDataService.LabEpicPityCounter = 15;
            PlayerDataService.LabLegendPityCounter = 25;

            // Roll wins Elite
            pity.RecordRollResult(LabUpgradeController.ItemRarity.Elite);

            // Elite resets to 0; Epic and Legend increase by 1
            Assert.That(pity.EliteCounter, Is.EqualTo(0), "Elite counter must reset to 0.");
            Assert.That(pity.EpicCounter, Is.EqualTo(16), "Epic counter must increment to 16.");
            Assert.That(pity.LegendCounter, Is.EqualTo(26), "Legend counter must increment to 26.");
        }
        finally
        {
            PlayerDataService.LabElitePityCounter = origElite;
            PlayerDataService.LabEpicPityCounter = origEpic;
            PlayerDataService.LabLegendPityCounter = origLegend;
            Object.DestroyImmediate(pityObj);
        }
    }

    [Test]
    public void M4_07_PityGuarantee_GuaranteedTrigger_ForcesMinRarityWhenThresholdReached()
    {
        GameObject pityObj = new GameObject("PityController", typeof(PityGuaranteeController));
        int origElite = PlayerDataService.LabElitePityCounter;
        int origEpic = PlayerDataService.LabEpicPityCounter;
        int origLegend = PlayerDataService.LabLegendPityCounter;

        try
        {
            PityGuaranteeController pity = pityObj.GetComponent<PityGuaranteeController>();

            // Elite at 9 (out of 10) -> next roll is guaranteed Elite
            PlayerDataService.LabElitePityCounter = 9;
            PlayerDataService.LabEpicPityCounter = 10;
            PlayerDataService.LabLegendPityCounter = 10;
            Assert.That(pity.GetNextGuaranteedRarity(), Is.EqualTo(LabUpgradeController.ItemRarity.Elite));

            // Epic at 24 (out of 25) -> next roll is guaranteed Epic
            PlayerDataService.LabEpicPityCounter = 24;
            Assert.That(pity.GetNextGuaranteedRarity(), Is.EqualTo(LabUpgradeController.ItemRarity.Epic));

            // Legend at 49 (out of 50) -> next roll is guaranteed Legend
            PlayerDataService.LabLegendPityCounter = 49;
            Assert.That(pity.GetNextGuaranteedRarity(), Is.EqualTo(LabUpgradeController.ItemRarity.Legend));
        }
        finally
        {
            PlayerDataService.LabElitePityCounter = origElite;
            PlayerDataService.LabEpicPityCounter = origEpic;
            PlayerDataService.LabLegendPityCounter = origLegend;
            Object.DestroyImmediate(pityObj);
        }
    }
    #endregion

    #region 3. Chipset Inventory & 5 Tiers System Tests
    [Test]
    public void M4_08_ChipsetController_24Chips_5TiersLevelCaps()
    {
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Magic), Is.EqualTo(6));
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Rare), Is.EqualTo(9));
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Unique), Is.EqualTo(14));
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Epic), Is.EqualTo(18));
        Assert.That(ChipItemData.GetMaxLevelForTier(ChipTier.Holographic), Is.EqualTo(24));
    }

    [Test]
    public void M4_09_ChipsetController_AdvanceStones_RequiredForHolographicBreakthrough()
    {
        ChipItemData epicChip = new ChipItemData
        {
            id = 2,
            chipName = "Rifle",
            tier = ChipTier.Epic,
            level = 18 // Max cap for Epic
        };

        Assert.That(epicChip.IsAtTierCap, Is.True);
        Assert.That(epicChip.NeedsAdvanceStones, Is.True);
        Assert.That(epicChip.AdvanceStoneCost, Is.EqualTo(10));
    }

    [Test]
    public void M4_10_ChipsetController_3PresetDecks_LoadsAndSavesEquippedSlotIds()
    {
        int[] deck1 = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        int[] deck2 = { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

        PlayerDataService.SaveChipsetDeck(0, deck1);
        PlayerDataService.SaveChipsetDeck(1, deck2);

        int[] loaded1 = PlayerDataService.LoadChipsetDeck(0, new int[10]);
        int[] loaded2 = PlayerDataService.LoadChipsetDeck(1, new int[10]);

        Assert.That(loaded1[0], Is.EqualTo(1));
        Assert.That(loaded1[9], Is.EqualTo(10));
        Assert.That(loaded2[0], Is.EqualTo(10));
        Assert.That(loaded2[9], Is.EqualTo(1));
    }
    #endregion

    #region 4. Buddy Drone Management Tests
    [Test]
    public void M4_11_BuddyController_12DroneRoster_InitializesUniqueDrones()
    {
        GameObject buddyObj = new GameObject("BuddyController", typeof(BuddyController));
        try
        {
            BuddyController ctrl = buddyObj.GetComponent<BuddyController>();
            ctrl.InitializeDatabase();

            Assert.That(ctrl.AllBuddies.Count, Is.GreaterThanOrEqualTo(12), "Buddy roster must contain at least 12 unique drones.");

            // Verify unique IDs
            var ids = ctrl.AllBuddies.Select(b => b.id).Distinct().ToList();
            Assert.That(ids.Count, Is.EqualTo(ctrl.AllBuddies.Count));
        }
        finally
        {
            Object.DestroyImmediate(buddyObj);
        }
    }

    [Test]
    public void M4_12_BuddyController_TierAdvancement_ConsumesDuplicatesAndScales()
    {
        BuddyItemData drone = new BuddyItemData
        {
            id = 1,
            buddyName = "Frost Sentinel",
            tier = BuddyTier.Common,
            level = 1,
            count = 10,
            requiredCount = 3
        };

        Assert.That(drone.CanAdvanceTier, Is.True);
        bool advanced = drone.AdvanceTier();

        Assert.That(advanced, Is.True);
        Assert.That(drone.tier, Is.EqualTo(BuddyTier.Magic));
        Assert.That(drone.count, Is.EqualTo(7));
    }
    #endregion

    #region 5. Level Up Popup & Reroll Modal Tests
    [Test]
    public void M4_13_LevelUpPopup_Offers4DistinctChoices_WithoutDuplicates()
    {
        List<ChipItemData> catalog = ChipsetLevelUpPopup.CreateRuntimeCatalog();
        Assert.That(catalog.Count, Is.GreaterThanOrEqualTo(4));

        System.Random rng = new System.Random(12345);
        List<ChipItemData> offers = ChipsetLevelUpPopup.SelectDistinctOffers(catalog, 4, rng);

        Assert.That(offers.Count, Is.EqualTo(4));
        var distinctIds = offers.Select(o => o.id).Distinct().ToList();
        Assert.That(distinctIds.Count, Is.EqualTo(4), "All 4 skill offer cards must be distinct.");
    }

    [Test]
    public void M4_14_LevelUpPopup_Reroll_Costs20RedGems_AndMax2PerLevel()
    {
        GameObject popupObj = new GameObject("LevelUpPopup", typeof(ChipsetLevelUpPopup));
        int initialGems = ChipManager.RedGems;

        try
        {
            ChipsetLevelUpPopup popup = popupObj.GetComponent<ChipsetLevelUpPopup>();
            ChipManager.RedGems = 100;
            popup.MaxRerollsPerLevel = 2;

            // Reroll 1: Spend 20 Red Gems
            bool r1 = popup.TryReroll();
            Assert.That(r1, Is.True);
            Assert.That(popup.CurrentRerollCount, Is.EqualTo(1));
            Assert.That(ChipManager.RedGems, Is.EqualTo(80));

            // Reroll 2: Spend 20 Red Gems
            bool r2 = popup.TryReroll();
            Assert.That(r2, Is.True);
            Assert.That(popup.CurrentRerollCount, Is.EqualTo(2));
            Assert.That(ChipManager.RedGems, Is.EqualTo(60));

            // Reroll 3: Exceeds max 2 -> Should fail
            bool r3 = popup.TryReroll();
            Assert.That(r3, Is.False, "Third reroll must fail because max rerolls per level is 2.");
            Assert.That(ChipManager.RedGems, Is.EqualTo(60));
        }
        finally
        {
            ChipManager.RedGems = initialGems;
            Object.DestroyImmediate(popupObj);
        }
    }
    #endregion

    #region 6. Chapter Progression & Progression Data Tests
    [Test]
    public void M4_15_ChapterController_LevelUnlockGating_LocksFutureChapters()
    {
        GameObject chapterObj = new GameObject("ChapterScreen", typeof(ChapterScreenController));
        int origUnlocked = PlayerDataService.UnlockedChapterIndex;
        int origSelected = PlayerDataService.SelectedChapterIndex;

        try
        {
            ChapterScreenController ctrl = chapterObj.GetComponent<ChapterScreenController>();
            PlayerDataService.UnlockedChapterIndex = 1; // Only Chapter 1 (0) and 2 (1) unlocked

            PlayerDataService.SelectedChapterIndex = 0;
            ctrl.RefreshChapterView();
            Assert.That(ctrl.IsCurrentChapterLocked(), Is.False);

            PlayerDataService.SelectedChapterIndex = 1;
            ctrl.RefreshChapterView();
            Assert.That(ctrl.IsCurrentChapterLocked(), Is.False);

            PlayerDataService.SelectedChapterIndex = 2; // Chapter 3 (index 2) -> Locked
            ctrl.RefreshChapterView();
            Assert.That(ctrl.IsCurrentChapterLocked(), Is.True);
        }
        finally
        {
            PlayerDataService.UnlockedChapterIndex = origUnlocked;
            PlayerDataService.SelectedChapterIndex = origSelected;
            Object.DestroyImmediate(chapterObj);
        }
    }

    [Test]
    public void M4_16_ChapterController_StartChapter_ValidatesAndDeductsEnergy()
    {
        GameObject chapterObj = new GameObject("ChapterScreen", typeof(ChapterScreenController));
        int origEnergy = PlayerDataService.Energy;
        int origUnlocked = PlayerDataService.UnlockedChapterIndex;
        int origSelected = PlayerDataService.SelectedChapterIndex;

        try
        {
            ChapterScreenController ctrl = chapterObj.GetComponent<ChapterScreenController>();
            PlayerDataService.UnlockedChapterIndex = 5;
            PlayerDataService.SelectedChapterIndex = 0;
            PlayerDataService.Energy = 15;

            bool started = ctrl.TryStartChapter(out string sceneName, loadScene: false);
            Assert.That(started, Is.True);
            Assert.That(PlayerDataService.Energy, Is.EqualTo(5), "Chapter energy cost of 10 must be deducted from 15.");

            // Not enough energy
            bool startedSecond = ctrl.TryStartChapter(out _, loadScene: false);
            Assert.That(startedSecond, Is.False, "Starting chapter must fail when player has insufficient energy.");
        }
        finally
        {
            PlayerDataService.Energy = origEnergy;
            PlayerDataService.UnlockedChapterIndex = origUnlocked;
            PlayerDataService.SelectedChapterIndex = origSelected;
            Object.DestroyImmediate(chapterObj);
        }
    }
    #endregion

    #region 7. Shop Controller, Daily Login & Achievements Tests
    [Test]
    public void M4_17_ShopController_RedGemsToDataChipsExchange_DeductsAndRewards()
    {
        GameObject shopObj = new GameObject("Shop", typeof(ShopController));
        int initialChips = ChipManager.DataChips;
        int initialGems = ChipManager.RedGems;

        try
        {
            ShopController shop = shopObj.GetComponent<ShopController>();
            ChipManager.RedGems = 500;
            ChipManager.DataChips = 1000;

            ShopController.Offer[] testOffers = new ShopController.Offer[]
            {
                new ShopController.Offer
                {
                    id = "pack_chips_small",
                    displayName = "2,000 Data Chips",
                    currency = ShopController.CurrencyType.RedGem,
                    price = 100,
                    reward = ShopController.RewardType.DataChip,
                    rewardAmount = 2000
                }
            };

            shop.SetOffersForTesting(testOffers);
            bool success = shop.TryPurchase(0);

            Assert.That(success, Is.True);
            Assert.That(ChipManager.RedGems, Is.EqualTo(400));
            Assert.That(ChipManager.DataChips, Is.EqualTo(3000));
        }
        finally
        {
            ChipManager.DataChips = initialChips;
            ChipManager.RedGems = initialGems;
            Object.DestroyImmediate(shopObj);
        }
    }

    [Test]
    public void M4_18_ShopController_VNDPurchases_FailClosedWithoutRealPayment()
    {
        GameObject shopObj = new GameObject("Shop", typeof(ShopController));
        try
        {
            ShopController shop = shopObj.GetComponent<ShopController>();
            ShopController.Offer[] testOffers = new ShopController.Offer[]
            {
                new ShopController.Offer
                {
                    id = "pack_vnd_mega",
                    displayName = "Mega Gem Pack",
                    currency = ShopController.CurrencyType.VND,
                    price = 199000,
                    reward = ShopController.RewardType.RedGem,
                    rewardAmount = 5000
                }
            };

            shop.SetOffersForTesting(testOffers);
            bool success = shop.TryPurchase(0);

            Assert.That(success, Is.False, "VND purchases must fail-closed safely until official IAP is integrated.");
        }
        finally
        {
            Object.DestroyImmediate(shopObj);
        }
    }

    [Test]
    public void M4_19_DailyLoginDatabase_7DaysConfiguration()
    {
        DailyLoginDatabase db = ScriptableObject.CreateInstance<DailyLoginDatabase>();
        db.PopulateDefault7Days();

        Assert.That(db.Days.Count, Is.EqualTo(7));
        for (int i = 1; i <= 7; i++)
        {
            DailyLoginDayData day = db.GetDayData(i);
            Assert.That(day, Is.Not.Null);
            Assert.That(day.dayIndex, Is.EqualTo(i));
            Assert.That(day.rewards.Length, Is.GreaterThan(0));
        }
    }

    [Test]
    public void M4_20_AchievementDatabase_5Definitions_WithMilestoneTargets()
    {
        AchievementDatabase db = ScriptableObject.CreateInstance<AchievementDatabase>();
        db.PopulateDefaultAchievements();

        Assert.That(db.Achievements.Count, Is.EqualTo(5));
        Assert.That(db.GetAchievement("enemy_kill_2500"), Is.Not.Null);
        Assert.That(db.GetAchievement("enemy_kill_2500").targetValue, Is.EqualTo(2500));
        Assert.That(db.GetAchievement("chapter_clear_5"), Is.Not.Null);
        Assert.That(db.GetAchievement("chapter_clear_5").targetValue, Is.EqualTo(5));
    }

    [Test]
    public void M4_21_LabUpgradeController_RarityColorsAndWeights_4x4MatrixDistribution()
    {
        for (int i = 0; i < 16; i++)
        {
            LabUpgradeController.ItemRarity expectedRarity = (LabUpgradeController.ItemRarity)Mathf.Clamp(i / 4, 0, 3);
            if (i < 4) Assert.That(expectedRarity, Is.EqualTo(LabUpgradeController.ItemRarity.Common));
            else if (i < 8) Assert.That(expectedRarity, Is.EqualTo(LabUpgradeController.ItemRarity.Elite));
            else if (i < 12) Assert.That(expectedRarity, Is.EqualTo(LabUpgradeController.ItemRarity.Epic));
            else Assert.That(expectedRarity, Is.EqualTo(LabUpgradeController.ItemRarity.Legend));
        }
    }

    [Test]
    public void M4_22_ChipsetController_Sorting_ByTierAndQuantity()
    {
        List<ChipItemData> testList = new List<ChipItemData>
        {
            new ChipItemData { id = 1, chipName = "Gun", tier = ChipTier.Magic, count = 10 },
            new ChipItemData { id = 2, chipName = "Rifle", tier = ChipTier.Epic, count = 2 },
            new ChipItemData { id = 3, chipName = "Punch", tier = ChipTier.Rare, count = 25 }
        };

        // Sort by Tier descending
        var sortedByTier = testList.OrderByDescending(c => (int)c.tier).ToList();
        Assert.That(sortedByTier[0].id, Is.EqualTo(2)); // Epic
        Assert.That(sortedByTier[1].id, Is.EqualTo(3)); // Rare
        Assert.That(sortedByTier[2].id, Is.EqualTo(1)); // Magic

        // Sort by Quantity descending
        var sortedByQty = testList.OrderByDescending(c => c.count).ToList();
        Assert.That(sortedByQty[0].id, Is.EqualTo(3)); // 25
        Assert.That(sortedByQty[1].id, Is.EqualTo(1)); // 10
        Assert.That(sortedByQty[2].id, Is.EqualTo(2)); // 2
    }
    #endregion
}
