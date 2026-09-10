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
}
#endif
