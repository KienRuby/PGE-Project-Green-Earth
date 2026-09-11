using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using PGE.Auth;

public static class ShopSecurityProbes
{
    private static int totalPassed = 0;
    private static int totalFailed = 0;
    private static readonly StringBuilder report = new();

    private static void Assert(bool condition, string testName, string message)
    {
        if (!condition)
        {
            throw new Exception($"Assertion Failed in {testName}: {message}");
        }
    }

    private static void RunProbe(string testName, Action action)
    {
        var sw = Stopwatch.StartNew();
        PlayerPrefs.ResetStore();
        Time.unscaledTime += 5.0f;

        try
        {
            action();
            sw.Stop();
            Console.WriteLine($"[PASS] {testName} ({sw.ElapsedMilliseconds} ms)");
            report.AppendLine($"[PASS] {testName} ({sw.ElapsedMilliseconds} ms)");
            totalPassed++;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Console.WriteLine($"[FAIL] {testName} ({sw.ElapsedMilliseconds} ms) - {ex.Message}");
            report.AppendLine($"[FAIL] {testName} ({sw.ElapsedMilliseconds} ms) - {ex.Message}");
            totalFailed++;
        }
    }

    public static int Main()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("PGE SHOP SECURITY & TRANSACTION PROBE SUITE");
        Console.WriteLine($"Execution Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine("================================================================================");

        var shop = new ShopController();

        // --------------------------------------------------------------------------------
        // 1. Normal Purchase
        // --------------------------------------------------------------------------------
        RunProbe("Test_01_NormalPurchase_DeductsExactCurrency_AndGrantsReward", () =>
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
            Assert(success, "Test_01", "TryPurchase must succeed");
            Assert(PlayerDataService.RedGems == 50, "Test_01", "50 RedGems must be deducted");
            Assert(PlayerDataService.DroneBoxes == 1, "Test_01", "1 DroneBox must be awarded");
        });

        // --------------------------------------------------------------------------------
        // 2. Exact Balance Purchase
        // --------------------------------------------------------------------------------
        RunProbe("Test_02_ExactBalancePurchase_Succeeds_BalanceBecomesZero", () =>
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
            Assert(success, "Test_02", "Exact balance purchase must succeed");
            Assert(PlayerDataService.RedGems == 0, "Test_02", "Balance must reach exactly 0");
            Assert(PlayerDataService.DroneBoxes == 1, "Test_02", "1 DroneBox awarded");
        });

        // --------------------------------------------------------------------------------
        // 3. Insufficient Currency
        // --------------------------------------------------------------------------------
        RunProbe("Test_03_InsufficientCurrency_Fails_NoDeduction_NoReward", () =>
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
            Assert(!success, "Test_03", "Purchase must fail when insufficient funds");
            Assert(PlayerDataService.RedGems == 49, "Test_03", "Funds must NOT be deducted");
            Assert(PlayerDataService.DroneBoxes == 0, "Test_03", "Reward must not be granted");
        });

        // --------------------------------------------------------------------------------
        // 4. Zero Currency
        // --------------------------------------------------------------------------------
        RunProbe("Test_04_ZeroCurrency_Fails_NoDeduction_NoReward", () =>
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
            Assert(!success, "Test_04", "Purchase must fail when zero funds");
            Assert(PlayerDataService.RedGems == 0, "Test_04", "Funds remain 0");
            Assert(PlayerDataService.DroneBoxes == 0, "Test_04", "Reward must not be granted");
        });

        // --------------------------------------------------------------------------------
        // 5. Negative Price Exploit Rejection
        // --------------------------------------------------------------------------------
        RunProbe("Test_05_NegativePriceExploit_RejectedByValidator", () =>
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
            Assert(!success, "Test_05", "Negative price offer must be rejected");
            Assert(PlayerDataService.RedGems == 100, "Test_05", "Balance must remain unchanged");
            Assert(PlayerDataService.DroneBoxes == 0, "Test_05", "No reward awarded");
        });

        // --------------------------------------------------------------------------------
        // 6. Free Currency With Non-Zero Price Rejection
        // --------------------------------------------------------------------------------
        RunProbe("Test_06_FreeCurrency_WithNonZeroPrice_RejectedByValidator", () =>
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
            Assert(!success, "Test_06", "Free currency with price > 0 must be rejected");
        });

        // --------------------------------------------------------------------------------
        // 7. Zero Or Negative Reward Amount Rejection
        // --------------------------------------------------------------------------------
        RunProbe("Test_07_ZeroOrNegativeRewardAmount_RejectedByValidator", () =>
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

            Assert(!shop.TryPurchase(zeroReward, bypassCooldown: true), "Test_07", "Zero reward rejected");
            Assert(!shop.TryPurchase(negativeReward, bypassCooldown: true), "Test_07", "Negative reward rejected");
            Assert(PlayerDataService.RedGems == 100, "Test_07", "Balance preserved");
        });

        // --------------------------------------------------------------------------------
        // 8. Reward Amount Overflow Rejection
        // --------------------------------------------------------------------------------
        RunProbe("Test_08_RewardAmountOverflow_RejectedByValidator", () =>
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
            Assert(!success, "Test_08", "Reward exceeding 1,000,000 must be rejected");
            Assert(PlayerDataService.RedGems == 100, "Test_08", "Balance preserved");
        });

        // --------------------------------------------------------------------------------
        // 9. Real Money VND Fails Closed
        // --------------------------------------------------------------------------------
        RunProbe("Test_09_RealMoneyVND_FailsClosed_RequiresNativeIAP", () =>
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
            Assert(!success, "Test_09", "VND purchase must fail-closed when native IAP unavailable");
        });

        // --------------------------------------------------------------------------------
        // 10. Once-Per-Day Daily Claim
        // --------------------------------------------------------------------------------
        RunProbe("Test_10_DailyClaim_OncePerDay_LockedOnSecondAttempt", () =>
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
            Assert(first, "Test_10", "First daily claim succeeds");
            Assert(PlayerDataService.RedGems == 50, "Test_10", "50 RedGems awarded");

            bool second = shop.TryPurchase(dailyGemOffer, bypassCooldown: true);
            Assert(!second, "Test_10", "Second daily claim on same day must fail");
            Assert(PlayerDataService.RedGems == 50, "Test_10", "Gems must not increase again");
        });

        // --------------------------------------------------------------------------------
        // 11. Anti-Spam Cooldown Rejection
        // --------------------------------------------------------------------------------
        RunProbe("Test_11_AntiSpamCooldown_BlocksRapidSuccessiveClicks", () =>
        {
            PlayerDataService.RedGems = 200;
            Time.unscaledTime += 1.0f;
            var offer = new ShopController.Offer
            {
                id = "Cooldown_Item",
                currency = ShopController.CurrencyType.RedGem,
                price = 50,
                reward = ShopController.RewardType.DroneBox,
                rewardAmount = 1
            };

            bool click1 = shop.TryPurchase(offer, bypassCooldown: false);
            Assert(click1, "Test_11", "Click 1 must succeed");

            bool click2 = shop.TryPurchase(offer, bypassCooldown: false);
            Assert(!click2, "Test_11", "Immediate click 2 must be blocked by debounce timer");

            Assert(PlayerDataService.RedGems == 150, "Test_11", "Only 50 gems deducted");
            Assert(PlayerDataService.DroneBoxes == 1, "Test_11", "Only 1 box awarded");
        });

        // --------------------------------------------------------------------------------
        // 12. Rapid Spam 100 Calls (Autoclicker Attack)
        // --------------------------------------------------------------------------------
        RunProbe("Test_12_RapidSpam100Calls_OnlyOneSucceeds", () =>
        {
            PlayerDataService.RedGems = 10000;
            Time.unscaledTime += 1.0f;
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

            Assert(success == 1, "Test_12", $"Exactly 1 purchase must succeed, got {success}");
            Assert(failed == 99, "Test_12", $"99 purchases must be blocked, got {failed}");
            Assert(PlayerDataService.RedGems == 9950, "Test_12", "Only 50 gems deducted");
            Assert(PlayerDataService.DroneBoxes == 1, "Test_12", "Only 1 box granted");
        });

        // --------------------------------------------------------------------------------
        // 13. Sequential 100 Purchases With Panel Reopen
        // --------------------------------------------------------------------------------
        RunProbe("Test_13_Sequential100Purchases_WithPanelReopen_MaintainsExactBalance", () =>
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
                Assert(res, "Test_13", $"Purchase {i + 1} must succeed");
            }

            Assert(PlayerDataService.RedGems == 5000, "Test_13", $"Balance must be exactly 5000, got {PlayerDataService.RedGems}");
            Assert(PlayerDataService.ChipsetBoxes == 100, "Test_13", $"ChipsetBoxes must be exactly 100, got {PlayerDataService.ChipsetBoxes}");
        });

        // --------------------------------------------------------------------------------
        // 14. Reopen Shop Panel 10 Times - No Duplicate Listeners
        // --------------------------------------------------------------------------------
        RunProbe("Test_14_ReopenShopPanel10Times_NoDuplicateListeners", () =>
        {
            var button = new Button();
            var testOffer = new ShopController.Offer
            {
                id = "Btn_Offer",
                button = button,
                currency = ShopController.CurrencyType.RedGem,
                price = 50,
                reward = ShopController.RewardType.DroneBox,
                rewardAmount = 1
            };

            // Set private offers array on shop using reflection
            var offersField = typeof(ShopController).GetField("offers", BindingFlags.NonPublic | BindingFlags.Instance);
            offersField.SetValue(shop, new[] { testOffer });

            var bindMethod = typeof(ShopController).GetMethod("BindButtons", BindingFlags.NonPublic | BindingFlags.Instance);

            // Bind 10 times in a row
            for (int i = 0; i < 10; i++)
            {
                bindMethod.Invoke(shop, null);
            }

            // Verify button has exactly 1 listener attached after BindButtons (since it cleans listeners before adding)
            PlayerDataService.RedGems = 100;
            Time.unscaledTime += 1.0f;
            button.onClick.Invoke();

            Assert(PlayerDataService.RedGems == 50, "Test_14", $"Only 1 transaction should execute on button click, balance: {PlayerDataService.RedGems}");
            Assert(PlayerDataService.DroneBoxes == 1, "Test_14", $"Only 1 box awarded, boxes: {PlayerDataService.DroneBoxes}");
        });

        // --------------------------------------------------------------------------------
        // 15. Box Inventory Clamping & Overflow Protection
        // --------------------------------------------------------------------------------
        RunProbe("Test_15_BoxInventoryClamping_AndOverflowProtection", () =>
        {
            PlayerDataService.DroneBoxes = int.MaxValue - 5;
            PlayerDataService.AddDroneBoxes(10);
            Assert(PlayerDataService.DroneBoxes == int.MaxValue, "Test_15", "Boxes must clamp to int.MaxValue, never negative");
        });

        // --------------------------------------------------------------------------------
        // 16. Null Offer Handling
        // --------------------------------------------------------------------------------
        RunProbe("Test_16_NullOffer_HandledGracefully", () =>
        {
            bool success = shop.TryPurchase((ShopController.Offer)null, bypassCooldown: true);
            Assert(!success, "Test_16", "Null offer must return false safely without crashing");
        });

        // --------------------------------------------------------------------------------
        // 17. Negative Currency Checks In PlayerDataService & ChipManager
        // --------------------------------------------------------------------------------
        RunProbe("Test_17_NegativeCurrencyChecks_InPlayerDataService_AndChipManager", () =>
        {
            PlayerDataService.RedGems = 100;
            PlayerDataService.DataChips = 100;
            PlayerDataService.Energy = 100;
            PlayerDataService.AdvanceStones = 100;

            Assert(!PlayerDataService.HasEnoughRedGems(-50), "Test_17", "Negative gems check must return false");
            Assert(!PlayerDataService.HasEnoughDataChips(-50), "Test_17", "Negative chips check must return false");
            Assert(!PlayerDataService.HasEnoughEnergy(-50), "Test_17", "Negative energy check must return false");
            Assert(!PlayerDataService.HasEnoughAdvanceStones(-50), "Test_17", "Negative stones check must return false");

            Assert(!ChipManager.HasEnoughRedGems(-100), "Test_17", "ChipManager negative gems check must return false");
            Assert(!ChipManager.HasEnoughDataChips(-100), "Test_17", "ChipManager negative chips check must return false");
            Assert(!ChipManager.HasEnoughEnergy(-100), "Test_17", "ChipManager negative energy check must return false");
            Assert(!ChipManager.HasEnoughAdvanceStones(-100), "Test_17", "ChipManager negative stones check must return false");
        });

        // --------------------------------------------------------------------------------
        // 18. Cloud Save Sync Includes Boxes
        // --------------------------------------------------------------------------------
        RunProbe("Test_18_CloudSaveSync_IncludesBoxes", () =>
        {
            PlayerDataService.ChipsetBoxes = 42;
            PlayerDataService.DroneBoxes = 99;

            var cloudData = new PlayerCloudData
            {
                chipsetBoxes = PlayerDataService.ChipsetBoxes,
                droneBoxes = PlayerDataService.DroneBoxes
            };

            string json = JsonUtility.ToJson(cloudData);
            Assert(json.Contains("\"chipsetBoxes\":42"), "Test_18", "JSON must contain chipsetBoxes:42");
            Assert(json.Contains("\"droneBoxes\":99"), "Test_18", "JSON must contain droneBoxes:99");
        });

        Console.WriteLine("================================================================================");
        Console.WriteLine($"TOTAL: {totalPassed + totalFailed} | PASSED: {totalPassed} | FAILED: {totalFailed}");
        Console.WriteLine($"STATUS: {(totalFailed == 0 ? "ALL SHOP SECURITY TESTS PASSED" : "TEST FAILURES ENCOUNTERED")}");
        Console.WriteLine("================================================================================");

        return totalFailed == 0 ? 0 : 1;
    }
}
