#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using PGE.Auth;

[TestFixture]
public class ShopSecurityAndTransactionTests
{
    private GameObject shopObj;
    private ShopController shop;

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteKey("PGE_RedGems");
        PlayerPrefs.DeleteKey("PGE_DataChips");
        PlayerPrefs.DeleteKey("PGE_Energy");
        PlayerPrefs.DeleteKey("PGE_AdvanceStones");
        PlayerPrefs.DeleteKey("PGE_ChipsetBoxes");
        PlayerPrefs.DeleteKey("PGE_DroneBoxes");
        PlayerPrefs.DeleteKey("PGE.Shop.Daily.Item_Daily_Gem_Free");
        DeleteDevelopmentPackageData();
        PlayerPrefs.Save();

        shopObj = new GameObject("Test_ShopPanel");
        shop = shopObj.AddComponent<ShopController>();
    }

    [TearDown]
    public void TearDown()
    {
        if (shopObj != null)
        {
            UnityEngine.Object.DestroyImmediate(shopObj);
        }

        DeleteDevelopmentPackageData();
        PlayerPrefs.Save();
    }

    [Test]
    public void Test_01_NormalPurchase_DeductsExactCurrency_AndGrantsReward()
    {
        PlayerDataService.RedGems = 100;
        var offer = new ShopController.Offer
        {
            id = "Test_Drone_1x",
            currency = ShopController.CurrencyType.RedGem,
            price = 50,
            reward = ShopController.RewardType.DroneBox,
            rewardAmount = 1
        };

        bool success = shop.TryPurchase(offer, bypassCooldown: true);
        Assert.That(success, Is.True, "TryPurchase must succeed");
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(50), "50 RedGems must be deducted");
        Assert.That(PlayerDataService.DroneBoxes, Is.EqualTo(1), "1 DroneBox must be awarded");
    }

    [Test]
    public void Test_02_ExactBalancePurchase_Succeeds_BalanceBecomesZero()
    {
        PlayerDataService.RedGems = 50;
        var offer = new ShopController.Offer
        {
            id = "Test_Exact",
            currency = ShopController.CurrencyType.RedGem,
            price = 50,
            reward = ShopController.RewardType.DroneBox,
            rewardAmount = 1
        };

        bool success = shop.TryPurchase(offer, bypassCooldown: true);
        Assert.That(success, Is.True, "Exact balance purchase must succeed");
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(0), "Balance must reach exactly 0");
        Assert.That(PlayerDataService.DroneBoxes, Is.EqualTo(1), "1 DroneBox awarded");
    }

    [Test]
    public void Test_03_InsufficientCurrency_Fails_NoDeduction_NoReward()
    {
        PlayerDataService.RedGems = 49;
        var offer = new ShopController.Offer
        {
            id = "Test_Insufficient",
            currency = ShopController.CurrencyType.RedGem,
            price = 50,
            reward = ShopController.RewardType.DroneBox,
            rewardAmount = 1
        };

        bool success = shop.TryPurchase(offer, bypassCooldown: true);
        Assert.That(success, Is.False, "Purchase must fail when insufficient funds");
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(49), "Funds must NOT be deducted");
        Assert.That(PlayerDataService.DroneBoxes, Is.EqualTo(0), "Reward must not be granted");
    }

    [Test]
    public void Test_04_ZeroCurrency_Fails_NoDeduction_NoReward()
    {
        PlayerDataService.RedGems = 0;
        var offer = new ShopController.Offer
        {
            id = "Test_Zero",
            currency = ShopController.CurrencyType.RedGem,
            price = 50,
            reward = ShopController.RewardType.DroneBox,
            rewardAmount = 1
        };

        bool success = shop.TryPurchase(offer, bypassCooldown: true);
        Assert.That(success, Is.False, "Purchase must fail when zero funds");
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(0), "Funds remain 0");
        Assert.That(PlayerDataService.DroneBoxes, Is.EqualTo(0), "Reward must not be granted");
    }

    [Test]
    public void Test_05_NegativePriceExploit_RejectedByValidator()
    {
        PlayerDataService.RedGems = 100;
        var maliciousOffer = new ShopController.Offer
        {
            id = "Exploit_Free_Money",
            currency = ShopController.CurrencyType.RedGem,
            price = -500,
            reward = ShopController.RewardType.DroneBox,
            rewardAmount = 1
        };

        bool success = shop.TryPurchase(maliciousOffer, bypassCooldown: true);
        Assert.That(success, Is.False, "Negative price offer must be rejected");
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(100), "Balance must remain unchanged");
        Assert.That(PlayerDataService.DroneBoxes, Is.EqualTo(0), "No reward awarded");
    }

    [Test]
    public void Test_06_FreeCurrency_WithNonZeroPrice_RejectedByValidator()
    {
        var invalidFreeOffer = new ShopController.Offer
        {
            id = "Invalid_Free_Item",
            currency = ShopController.CurrencyType.Free,
            price = 100,
            reward = ShopController.RewardType.RedGem,
            rewardAmount = 50
        };

        bool success = shop.TryPurchase(invalidFreeOffer, bypassCooldown: true);
        Assert.That(success, Is.False, "Free currency with price > 0 must be rejected");
    }

    [Test]
    public void Test_07_ZeroOrNegativeRewardAmount_RejectedByValidator()
    {
        PlayerDataService.RedGems = 100;
        var zeroReward = new ShopController.Offer
        {
            id = "Zero_Reward",
            currency = ShopController.CurrencyType.RedGem,
            price = 10,
            reward = ShopController.RewardType.DroneBox,
            rewardAmount = 0
        };
        var negativeReward = new ShopController.Offer
        {
            id = "Negative_Reward",
            currency = ShopController.CurrencyType.RedGem,
            price = 10,
            reward = ShopController.RewardType.DroneBox,
            rewardAmount = -5
        };

        Assert.That(shop.TryPurchase(zeroReward, bypassCooldown: true), Is.False, "Zero reward rejected");
        Assert.That(shop.TryPurchase(negativeReward, bypassCooldown: true), Is.False, "Negative reward rejected");
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(100), "Balance preserved");
    }

    [Test]
    public void Test_08_RewardAmountOverflow_RejectedByValidator()
    {
        PlayerDataService.RedGems = 100;
        var overflowOffer = new ShopController.Offer
        {
            id = "Overflow_Reward",
            currency = ShopController.CurrencyType.RedGem,
            price = 10,
            reward = ShopController.RewardType.DataChip,
            rewardAmount = 2000000
        };

        bool success = shop.TryPurchase(overflowOffer, bypassCooldown: true);
        Assert.That(success, Is.False, "Reward exceeding 1,000,000 must be rejected");
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(100), "Balance preserved");
    }

    [Test]
    public void Test_09_RealMoneyVND_FailsClosed_RequiresNativeIAP()
    {
        var vndOffer = new ShopController.Offer
        {
            id = "Card_VIP_Package",
            currency = ShopController.CurrencyType.VND,
            price = 499000,
            reward = ShopController.RewardType.RedGem,
            rewardAmount = 1000
        };

        bool success = shop.TryPurchase(vndOffer, bypassCooldown: true);
        Assert.That(success, Is.False, "VND purchase must fail-closed when native IAP unavailable");
    }

    [Test]
    public void Test_09B_WelcomePackage_DevelopmentPurchase_GrantsExactBundleOnce()
    {
        PlayerDataService.RedGems = 100;
        PlayerDataService.DataChips = 200;
        var welcomeOffer = new ShopController.Offer
        {
            id = "welcome-package",
            displayName = "WELCOME PACKAGE",
            currency = ShopController.CurrencyType.VND,
            reward = ShopController.RewardType.RedGem,
            rewardAmount = 1
        };

        Assert.That(shop.TryPurchase(welcomeOffer, bypassCooldown: true), Is.True);
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(3100));
        Assert.That(PlayerDataService.DataChips, Is.EqualTo(30200));
        Assert.That(PlayerDataService.LoadChipsetItemData(1, out _, out _, out int standardGunCount, out _, out _), Is.True);
        Assert.That(PlayerDataService.LoadChipsetItemData(3, out _, out _, out int rocketPunchCount, out _, out _), Is.True);
        Assert.That(standardGunCount, Is.EqualTo(12), "Default Standard Gun has 5 pieces, then receives exactly 7 more.");
        Assert.That(rocketPunchCount, Is.EqualTo(7));
        Assert.That(ShopController.WasPurchasedOnce("welcome-package"), Is.True);

        Assert.That(shop.TryPurchase(welcomeOffer, bypassCooldown: true), Is.False);
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(3100));
        Assert.That(PlayerDataService.DataChips, Is.EqualTo(30200));
    }

    [TestCase("intermediate-pack", 50000, 6, 8)]
    [TestCase("advanced-pack", 70000, 10, 7)]
    public void Test_09C_ChipsetPackages_GrantEveryDisplayedRewardOnce(
        string offerId,
        int expectedDataChips,
        int firstChipsetId,
        int secondChipsetId)
    {
        PlayerDataService.RedGems = 0;
        PlayerDataService.DataChips = 0;

        Assert.That(shop.TryPurchase(CreateVndOffer(offerId), bypassCooldown: true), Is.True);
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(3000));
        Assert.That(PlayerDataService.DataChips, Is.EqualTo(expectedDataChips));
        Assert.That(GetChipsetPieceCount(firstChipsetId), Is.EqualTo(7));
        Assert.That(GetChipsetPieceCount(secondChipsetId), Is.EqualTo(7));
        Assert.That(shop.TryPurchase(CreateVndOffer(offerId), bypassCooldown: true), Is.False);
    }

    [Test]
    public void Test_09D_GunPack_GrantsAllThreeDisplayedChipsets()
    {
        PlayerDataService.RedGems = 0;

        Assert.That(shop.TryPurchase(CreateVndOffer("gun-pack"), bypassCooldown: true), Is.True);
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(3000));
        Assert.That(GetChipsetPieceCount(1), Is.EqualTo(12));
        Assert.That(GetChipsetPieceCount(8), Is.EqualTo(7));
        Assert.That(GetChipsetPieceCount(2), Is.EqualTo(7));
    }

    [Test]
    public void Test_09E_DronePack_GrantsAndPersistsAllThreeDisplayedDrones()
    {
        PlayerDataService.RedGems = 0;

        Assert.That(shop.TryPurchase(CreateVndOffer("drone-pack"), bypassCooldown: true), Is.True);
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(3000));
        Assert.That(PlayerDataService.GetBuddyPieceCount(1), Is.EqualTo(7));
        Assert.That(PlayerDataService.GetBuddyPieceCount(6), Is.EqualTo(7));
        Assert.That(PlayerDataService.GetBuddyPieceCount(10), Is.EqualTo(7));
    }

    [TestCase("gem-1", 160)]
    [TestCase("gem-2", 1000)]
    [TestCase("gem-3", 2400)]
    [TestCase("gem-4", 5000)]
    [TestCase("gem-5", 13000)]
    [TestCase("gem-6", 28000)]
    public void Test_09F_GemPacks_GrantDisplayedAmountAndRemainConsumable(string offerId, int amount)
    {
        PlayerDataService.RedGems = 0;

        Assert.That(shop.TryPurchase(CreateVndOffer(offerId), bypassCooldown: true), Is.True);
        Assert.That(shop.TryPurchase(CreateVndOffer(offerId), bypassCooldown: true), Is.True);
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(amount * 2));
        Assert.That(ShopController.WasPurchasedOnce(offerId), Is.False);
    }

    [Test]
    public void Test_09G_VipPackage_UnlocksVipAndGrantsTenThousandGemsOnce()
    {
        PlayerDataService.RedGems = 0;

        Assert.That(shop.TryPurchase(CreateVndOffer("vip-package"), bypassCooldown: true), Is.True);
        Assert.That(PlayerDataService.IsVipOwned, Is.True);
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(10000));
        Assert.That(shop.TryPurchase(CreateVndOffer("vip-package"), bypassCooldown: true), Is.False);
    }

    [Test]
    public void Test_10_DailyClaim_OncePerDay_LockedOnSecondAttempt()
    {
        PlayerDataService.RedGems = 0;
        var dailyGemOffer = new ShopController.Offer
        {
            id = "Item_Daily_Gem_Free",
            currency = ShopController.CurrencyType.Free,
            price = 0,
            reward = ShopController.RewardType.RedGem,
            rewardAmount = 50,
            oncePerDay = true
        };

        bool first = shop.TryPurchase(dailyGemOffer, bypassCooldown: true);
        Assert.That(first, Is.True, "First daily claim succeeds");
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(50), "50 RedGems awarded");

        bool second = shop.TryPurchase(dailyGemOffer, bypassCooldown: true);
        Assert.That(second, Is.False, "Second daily claim on same day must fail");
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(50), "Gems must not increase again");
    }

    [Test]
    public void Test_11_AntiSpamCooldown_BlocksRapidSuccessiveClicks()
    {
        PlayerDataService.RedGems = 200;

        var offer = new ShopController.Offer
        {
            id = "Cooldown_Item",
            currency = ShopController.CurrencyType.RedGem,
            price = 50,
            reward = ShopController.RewardType.DroneBox,
            rewardAmount = 1
        };

        bool click1 = shop.TryPurchase(offer, bypassCooldown: false);
        Assert.That(click1, Is.True, "Click 1 must succeed");

        bool click2 = shop.TryPurchase(offer, bypassCooldown: false);
        Assert.That(click2, Is.False, "Immediate click 2 must be blocked by debounce timer");

        Assert.That(PlayerDataService.RedGems, Is.EqualTo(150), "Only 50 gems deducted");
        Assert.That(PlayerDataService.DroneBoxes, Is.EqualTo(1), "Only 1 box awarded");
    }

    [Test]
    public void Test_12_RapidSpam100Calls_OnlyOneSucceeds()
    {
        PlayerDataService.RedGems = 10000;
        var offer = new ShopController.Offer
        {
            id = "Spam_Target",
            currency = ShopController.CurrencyType.RedGem,
            price = 50,
            reward = ShopController.RewardType.DroneBox,
            rewardAmount = 1
        };

        int success = 0;
        int failed = 0;
        for (int i = 0; i < 100; i++)
        {
            if (shop.TryPurchase(offer, bypassCooldown: false)) success++;
            else failed++;
        }

        Assert.That(success, Is.EqualTo(1), "Exactly 1 purchase must succeed");
        Assert.That(failed, Is.EqualTo(99), "99 purchases must be blocked");
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(9950), "Only 50 gems deducted");
        Assert.That(PlayerDataService.DroneBoxes, Is.EqualTo(1), "Only 1 box granted");
    }

    [Test]
    public void Test_13_Sequential100Purchases_WithPanelReopen_MaintainsExactBalance()
    {
        PlayerDataService.RedGems = 10000;
        var offer = new ShopController.Offer
        {
            id = "Seq_Item",
            currency = ShopController.CurrencyType.RedGem,
            price = 50,
            reward = ShopController.RewardType.ChipsetBox,
            rewardAmount = 1
        };

        for (int i = 0; i < 100; i++)
        {
            shop.gameObject.SetActive(false);
            shop.gameObject.SetActive(true);
            bool res = shop.TryPurchase(offer, bypassCooldown: true);
            Assert.That(res, Is.True, $"Purchase {i + 1} must succeed");
        }

        Assert.That(PlayerDataService.RedGems, Is.EqualTo(5000), $"Balance must be exactly 5000, got {PlayerDataService.RedGems}");
        Assert.That(PlayerDataService.ChipsetBoxes, Is.EqualTo(100), $"ChipsetBoxes must be exactly 100, got {PlayerDataService.ChipsetBoxes}");
    }

    [Test]
    public void Test_14_ReopenShopPanel10Times_NoDuplicateListeners()
    {
        var btnObj = new GameObject("TestBtn");
        btnObj.transform.SetParent(shopObj.transform);
        var button = btnObj.AddComponent<Button>();

        var testOffer = new ShopController.Offer
        {
            id = "Btn_Offer",
            button = button,
            currency = ShopController.CurrencyType.RedGem,
            price = 50,
            reward = ShopController.RewardType.DroneBox,
            rewardAmount = 1
        };

        var offersField = typeof(ShopController).GetField("offers", BindingFlags.NonPublic | BindingFlags.Instance);
        offersField.SetValue(shop, new[] { testOffer });

        var bindMethod = typeof(ShopController).GetMethod("BindButtons", BindingFlags.NonPublic | BindingFlags.Instance);

        for (int i = 0; i < 10; i++)
        {
            bindMethod.Invoke(shop, null);
        }

        PlayerDataService.RedGems = 100;
        shop.TryPurchase(testOffer, bypassCooldown: true);

        Assert.That(PlayerDataService.RedGems, Is.EqualTo(50));
        Assert.That(PlayerDataService.DroneBoxes, Is.EqualTo(1));

        UnityEngine.Object.DestroyImmediate(btnObj);
    }

    [Test]
    public void Test_15_BoxInventoryClamping_AndOverflowProtection()
    {
        PlayerDataService.DroneBoxes = int.MaxValue - 5;
        PlayerDataService.AddDroneBoxes(10);
        Assert.That(PlayerDataService.DroneBoxes, Is.EqualTo(int.MaxValue), "Boxes must clamp to int.MaxValue, never negative");
    }

    [Test]
    public void Test_16_NullOffer_HandledGracefully()
    {
        bool success = shop.TryPurchase((ShopController.Offer)null, bypassCooldown: true);
        Assert.That(success, Is.False, "Null offer must return false safely without crashing");
    }

    [Test]
    public void Test_17_NegativeCurrencyChecks_InPlayerDataService_AndChipManager()
    {
        PlayerDataService.RedGems = 100;
        PlayerDataService.DataChips = 100;
        PlayerDataService.Energy = 100;
        PlayerDataService.AdvanceStones = 100;

        Assert.That(PlayerDataService.HasEnoughRedGems(-50), Is.False);
        Assert.That(PlayerDataService.HasEnoughDataChips(-50), Is.False);
        Assert.That(PlayerDataService.HasEnoughEnergy(-50), Is.False);
        Assert.That(PlayerDataService.HasEnoughAdvanceStones(-50), Is.False);

        Assert.That(ChipManager.HasEnoughRedGems(-100), Is.False);
        Assert.That(ChipManager.HasEnoughDataChips(-100), Is.False);
        Assert.That(ChipManager.HasEnoughEnergy(-100), Is.False);
        Assert.That(ChipManager.HasEnoughAdvanceStones(-100), Is.False);
    }

    [Test]
    public void Test_18_CloudSaveSync_IncludesBoxes()
    {
        PlayerDataService.ChipsetBoxes = 42;
        PlayerDataService.DroneBoxes = 99;

        var cloudData = new PlayerCloudData
        {
            chipsetBoxes = PlayerDataService.ChipsetBoxes,
            droneBoxes = PlayerDataService.DroneBoxes
        };

        string json = JsonUtility.ToJson(cloudData);
        Assert.That(json.Contains("\"chipsetBoxes\":42"), Is.True);
        Assert.That(json.Contains("\"droneBoxes\":99"), Is.True);
    }

    [Test]
    public void Test_19_CloudSave_IncludesDronePiecesAndOnceOnlyPurchases()
    {
        PlayerDataService.SetBuddyPieceCount(6, 7);
        PlayerPrefs.SetInt("PGE.Shop.Purchased.drone-pack", 1);

        GameSaveData cloudData = GameSaveData.Capture("test-player", 1);

        Assert.That(cloudData.values.Exists(value =>
            value.key == PlayerDataService.BuddyCountKeyPrefix + "6" && value.intValue == 7), Is.True);
        Assert.That(cloudData.values.Exists(value =>
            value.key == "PGE.Shop.Purchased.drone-pack" && value.intValue == 1), Is.True);
    }

    [Test]
    public void Test_20_BoxRoller_IsDeterministic_AndNeverMixesCatalogs()
    {
        var chipsetDropsA = ShopBoxDropRoller.Roll(
            ShopBoxDropRoller.BoxCategory.Chipset, 100, new System.Random(2468));
        var chipsetDropsB = ShopBoxDropRoller.Roll(
            ShopBoxDropRoller.BoxCategory.Chipset, 100, new System.Random(2468));
        var buddyDrops = ShopBoxDropRoller.Roll(
            ShopBoxDropRoller.BoxCategory.Buddy, 100, new System.Random(1357));

        Assert.That(chipsetDropsA.Count, Is.EqualTo(chipsetDropsB.Count));
        for (int i = 0; i < chipsetDropsA.Count; i++)
        {
            Assert.That(chipsetDropsA[i].ItemId, Is.InRange(1, 10));
            Assert.That(chipsetDropsA[i].ItemId, Is.EqualTo(chipsetDropsB[i].ItemId));
            Assert.That(chipsetDropsA[i].Pieces, Is.EqualTo(chipsetDropsB[i].Pieces));
        }

        for (int i = 0; i < buddyDrops.Count; i++)
        {
            Assert.That(buddyDrops[i].ItemId, Is.InRange(1, 12));
            Assert.That(buddyDrops[i].Pieces, Is.GreaterThan(0));
        }

        var boundaryDrops = ShopBoxDropRoller.Roll(
            ShopBoxDropRoller.BoxCategory.Chipset,
            4,
            new SequenceRandom(6, 0, 7, 1, 29, 2, 30, 3));
        Assert.That(boundaryDrops[0].Pieces, Is.EqualTo(7), "Rolls 0-6 are the 7% reward");
        Assert.That(boundaryDrops[1].Pieces, Is.EqualTo(3), "Rolls 7-29 are the 23% reward");
        Assert.That(boundaryDrops[2].Pieces, Is.EqualTo(3));
        Assert.That(boundaryDrops[3].Pieces, Is.EqualTo(1), "Rolls 30-99 are the 70% reward");
    }

    [Test]
    public void Test_21_ChipsetBox10_AddsExactRolledPieces_ToChipsetCards()
    {
        DeleteAllBoxDropData();
        int[] before = GetAllChipsetPieceCounts();
        PlayerDataService.RedGems = 3000;
        shop.SetBoxRandomSeedForTesting(777);

        var offer = new ShopController.Offer
        {
            id = "chipset-box-10",
            currency = ShopController.CurrencyType.RedGem,
            price = 2700,
            reward = ShopController.RewardType.ChipsetBox,
            rewardAmount = 10
        };

        Assert.That(shop.TryPurchase(offer, bypassCooldown: true), Is.True);
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(300));
        Assert.That(PlayerDataService.ChipsetBoxes, Is.EqualTo(0), "Shop opens the boxes immediately");

        int totalPieces = 0;
        foreach (ShopBoxDropRoller.Drop drop in shop.LastBoxDrops)
        {
            Assert.That(drop.ItemId, Is.InRange(1, 10));
            Assert.That(GetSavedChipsetPieceCount(drop.ItemId),
                Is.EqualTo(before[drop.ItemId] + drop.Pieces));
            totalPieces += drop.Pieces;
        }
        Assert.That(totalPieces, Is.InRange(10, 70));
    }

    [Test]
    public void Test_22_DroneBox10_AddsExactRolledPieces_ToBuddyCards()
    {
        DeleteAllBoxDropData();
        PlayerDataService.RedGems = 6000;
        shop.SetBoxRandomSeedForTesting(888);

        var offer = new ShopController.Offer
        {
            id = "drone-box-10",
            currency = ShopController.CurrencyType.RedGem,
            price = 5400,
            reward = ShopController.RewardType.DroneBox,
            rewardAmount = 10
        };

        Assert.That(shop.TryPurchase(offer, bypassCooldown: true), Is.True);
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(600));
        Assert.That(PlayerDataService.DroneBoxes, Is.EqualTo(0), "Shop opens the boxes immediately");

        int totalPieces = 0;
        foreach (ShopBoxDropRoller.Drop drop in shop.LastBoxDrops)
        {
            Assert.That(drop.ItemId, Is.InRange(1, 12));
            Assert.That(PlayerDataService.GetBuddyPieceCount(drop.ItemId), Is.EqualTo(drop.Pieces));
            totalPieces += drop.Pieces;
        }
        Assert.That(totalPieces, Is.InRange(10, 70));
    }

    private static int[] GetAllChipsetPieceCounts()
    {
        int[] result = new int[11];
        foreach (ChipItemData chip in ChipsetController.CreateSavedDatabase())
        {
            result[chip.id] = chip.count;
        }
        return result;
    }

    private static int GetSavedChipsetPieceCount(int id)
    {
        Assert.That(PlayerDataService.LoadChipsetItemData(id, out _, out _, out int count, out _, out _), Is.True);
        return count;
    }

    private static void DeleteAllBoxDropData()
    {
        for (int id = 1; id <= 10; id++) DeleteChipsetItemData(id);
        for (int id = 1; id <= 12; id++)
        {
            PlayerPrefs.DeleteKey($"{PlayerDataService.BuddyCountKeyPrefix}{id}");
        }
        PlayerPrefs.DeleteKey(PlayerDataService.ChipsetBoxesKey);
        PlayerPrefs.DeleteKey(PlayerDataService.DroneBoxesKey);
        PlayerPrefs.Save();
    }

    private static void DeleteChipsetItemData(int id)
    {
        string prefix = PlayerDataService.GetChipItemPrefix(id);
        PlayerPrefs.DeleteKey($"{prefix}Level");
        PlayerPrefs.DeleteKey($"{prefix}Tier");
        PlayerPrefs.DeleteKey($"{prefix}Count");
        PlayerPrefs.DeleteKey($"{prefix}ReqCount");
        PlayerPrefs.DeleteKey($"{prefix}HasStar");
    }

    private static ShopController.Offer CreateVndOffer(string id)
    {
        return new ShopController.Offer
        {
            id = id,
            displayName = id.ToUpperInvariant(),
            currency = ShopController.CurrencyType.VND,
            reward = ShopController.RewardType.RedGem,
            rewardAmount = 1
        };
    }

    private static int GetChipsetPieceCount(int id)
    {
        Assert.That(PlayerDataService.LoadChipsetItemData(id, out _, out _, out int count, out _, out _), Is.True);
        return count;
    }

    private static void DeleteDevelopmentPackageData()
    {
        string[] onceOnlyIds =
        {
            "vip-package", "welcome-package", "intermediate-pack", "advanced-pack", "gun-pack", "drone-pack"
        };
        for (int i = 0; i < onceOnlyIds.Length; i++)
        {
            PlayerPrefs.DeleteKey($"PGE.Shop.Purchased.{onceOnlyIds[i]}");
        }

        int[] chipsetIds = { 1, 2, 3, 6, 7, 8, 10 };
        for (int i = 0; i < chipsetIds.Length; i++) DeleteChipsetItemData(chipsetIds[i]);

        int[] buddyIds = { 1, 6, 10 };
        for (int i = 0; i < buddyIds.Length; i++)
        {
            PlayerPrefs.DeleteKey($"{PlayerDataService.BuddyCountKeyPrefix}{buddyIds[i]}");
            PlayerPrefs.DeleteKey($"{PlayerDataService.BuddyRequiredCountKeyPrefix}{buddyIds[i]}");
            PlayerPrefs.DeleteKey($"{PlayerDataService.BuddyEnhanceCostKeyPrefix}{buddyIds[i]}");
        }

        PlayerPrefs.DeleteKey(PlayerDataService.VipOwnedKey);
        DeleteAllBoxDropData();
    }

    private sealed class SequenceRandom : System.Random
    {
        private readonly int[] values;
        private int index;

        public SequenceRandom(params int[] values)
        {
            this.values = values;
        }

        public override int Next(int maxValue)
        {
            if (index >= values.Length) throw new InvalidOperationException("No configured random value remains.");
            int value = values[index++];
            if (value < 0 || value >= maxValue) throw new InvalidOperationException("Configured random value is outside the requested range.");
            return value;
        }
    }
}
#endif
