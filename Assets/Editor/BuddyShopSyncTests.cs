#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class BuddyShopSyncTests
{
    private static readonly int[] ActiveDroneIds = { 1, 2, 3, 4, 10 };

    private static void ClearStaticEventSubscriptions()
    {
        var piecesField = typeof(PlayerDataService).GetField("OnBuddyPiecesChanged",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        piecesField?.SetValue(null, null);

        var dataChipsField = typeof(ChipManager).GetField("OnDataChipsChanged",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        dataChipsField?.SetValue(null, null);

        var redGemsField = typeof(ChipManager).GetField("OnRedGemsChanged",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        redGemsField?.SetValue(null, null);
    }

    [SetUp]
    public void SetUp()
    {
        ClearStaticEventSubscriptions();
        foreach (int id in ActiveDroneIds)
        {
            PlayerPrefs.DeleteKey($"{PlayerDataService.BuddyCountKeyPrefix}{id}");
            PlayerPrefs.DeleteKey($"{PlayerDataService.BuddyLevelKeyPrefix}{id}");
            PlayerPrefs.DeleteKey($"{PlayerDataService.BuddyTierKeyPrefix}{id}");
            PlayerPrefs.DeleteKey($"{PlayerDataService.BuddyRequiredCountKeyPrefix}{id}");
            PlayerPrefs.DeleteKey($"{PlayerDataService.BuddyEnhanceCostKeyPrefix}{id}");
        }
        for (int i = 0; i < 3; i++)
        {
            PlayerPrefs.DeleteKey(PlayerDataService.GetBuddyDeckKey(i));
        }
        PlayerPrefs.DeleteKey(PlayerDataService.BuddyActiveDeckKey);
        PlayerPrefs.Save();
    }

    [TearDown]
    public void TearDown()
    {
        ClearStaticEventSubscriptions();
        foreach (int id in ActiveDroneIds)
        {
            PlayerPrefs.DeleteKey($"{PlayerDataService.BuddyCountKeyPrefix}{id}");
            PlayerPrefs.DeleteKey($"{PlayerDataService.BuddyLevelKeyPrefix}{id}");
            PlayerPrefs.DeleteKey($"{PlayerDataService.BuddyTierKeyPrefix}{id}");
            PlayerPrefs.DeleteKey($"{PlayerDataService.BuddyRequiredCountKeyPrefix}{id}");
            PlayerPrefs.DeleteKey($"{PlayerDataService.BuddyEnhanceCostKeyPrefix}{id}");
        }
        for (int i = 0; i < 3; i++)
        {
            PlayerPrefs.DeleteKey(PlayerDataService.GetBuddyDeckKey(i));
        }
        PlayerPrefs.DeleteKey(PlayerDataService.BuddyActiveDeckKey);
        PlayerPrefs.Save();
    }

    [Test]
    public void BuddyList_AlwaysHasExactlyFiveCanonicalDrones_WithoutDuplicates()
    {
        GameObject go = new GameObject("BuddyController_Test", typeof(BuddyController));
        go.hideFlags = HideFlags.HideAndDontSave;
        BuddyController ctrl = go.GetComponent<BuddyController>();
        try
        {
            ctrl.InitializeDatabase();

            Assert.AreEqual(5, ctrl.AllBuddies.Count, "There must be exactly 5 drone types.");
            int[] actualIds = ctrl.AllBuddies.Select(b => b.id).ToArray();
            CollectionAssert.AreEqual(ActiveDroneIds, actualIds, "Drone IDs must be exactly 1, 2, 3, 4, 10.");

            Assert.AreEqual("Sloy", ctrl.AllBuddies[0].buddyName);
            Assert.AreEqual("Turret Buffer", ctrl.AllBuddies[1].buddyName);
            Assert.AreEqual("Radar Eye", ctrl.AllBuddies[2].buddyName);
            Assert.AreEqual("Assault Blaster", ctrl.AllBuddies[3].buddyName);
            Assert.AreEqual("Purifying Drone", ctrl.AllBuddies[4].buddyName);

            // Ensure no duplicate IDs exist
            int uniqueCount = ctrl.AllBuddies.Select(b => b.id).Distinct().Count();
            Assert.AreEqual(5, uniqueCount, "No duplicate drone records allowed.");
        }
        finally
        {
            if (ctrl != null)
            {
                var onDisableMethod = typeof(BuddyController).GetMethod("OnDisable",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                onDisableMethod?.Invoke(ctrl, null);
            }
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    [Test]
    public void BuddyShopSync_WhenPiecesGrantedWhileDisabled_SyncsAccuratelyOnReEnable()
    {
        GameObject go = new GameObject("BuddyController_Test", typeof(BuddyController));
        go.hideFlags = HideFlags.HideAndDontSave;
        BuddyController ctrl = go.GetComponent<BuddyController>();
        try
        {
            ctrl.InitializeDatabase();

            var startMethod = typeof(BuddyController).GetMethod("Start",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod.Invoke(ctrl, null);

            int initialCountSloy = ctrl.AllBuddies.First(b => b.id == 1).count;
            int initialCountTurretBuffer = ctrl.AllBuddies.First(b => b.id == 2).count;
            Assert.AreEqual(0, initialCountSloy);
            Assert.AreEqual(0, initialCountTurretBuffer);

            // 1. Simulate leaving Buddy screen to Shop (OnDisable)
            var onDisableMethod = typeof(BuddyController).GetMethod("OnDisable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onDisableMethod.Invoke(ctrl, null);

            // 2. Simulate opening Drone Box in Shop (ShopController calls PlayerDataService.AddBuddyPieces)
            int droppedPiecesSloy = 7;
            int droppedPiecesTurretBuffer = 3;
            PlayerDataService.AddBuddyPieces(1, droppedPiecesSloy);
            PlayerDataService.AddBuddyPieces(2, droppedPiecesTurretBuffer);

            // Verify PlayerPrefs stored the pieces
            Assert.AreEqual(droppedPiecesSloy, PlayerDataService.GetBuddyPieceCount(1));
            Assert.AreEqual(droppedPiecesTurretBuffer, PlayerDataService.GetBuddyPieceCount(2));

            // 3. Simulate returning to Buddy screen (OnEnable)
            var onEnableMethod = typeof(BuddyController).GetMethod("OnEnable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onEnableMethod.Invoke(ctrl, null);

            // 4. Verify BuddyController data is synchronized
            int updatedCountSloy = ctrl.AllBuddies.First(b => b.id == 1).count;
            int updatedCountTurretBuffer = ctrl.AllBuddies.First(b => b.id == 2).count;

            Assert.AreEqual(droppedPiecesSloy, updatedCountSloy, "Sloy pieces must match rewarded amount after reopening Buddy.");
            Assert.AreEqual(droppedPiecesTurretBuffer, updatedCountTurretBuffer, "Turret Buffer pieces must match rewarded amount after reopening Buddy.");
        }
        finally
        {
            if (ctrl != null)
            {
                var onDisableMethod = typeof(BuddyController).GetMethod("OnDisable",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                onDisableMethod?.Invoke(ctrl, null);
            }
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    [Test]
    public void BuddyShopSync_DetailModalOpen_UpdatesPieceCountAndAdvanceTierInteractability()
    {
        GameObject go = new GameObject("BuddyController_Test", typeof(BuddyController));
        go.hideFlags = HideFlags.HideAndDontSave;
        BuddyController ctrl = go.GetComponent<BuddyController>();
        try
        {
            var startMethod = typeof(BuddyController).GetMethod("Start",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod.Invoke(ctrl, null);

            // Select Drone 3 (Radar Eye)
            BuddyItemData radarEye = ctrl.AllBuddies.First(b => b.id == 3);
            radarEye.requiredCount = 10;
            ctrl.OpenDetailModal(radarEye);

            Assert.IsFalse(radarEye.CanAdvanceTier, "Should not be able to advance tier with 0 pieces.");

            // User leaves Buddy screen with modal open
            var onDisableMethod = typeof(BuddyController).GetMethod("OnDisable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onDisableMethod.Invoke(ctrl, null);

            // Player opens boxes in Shop and receives 12 pieces of Radar Eye
            PlayerDataService.AddBuddyPieces(3, 12);

            // User returns to Buddy screen
            var onEnableMethod = typeof(BuddyController).GetMethod("OnEnable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onEnableMethod.Invoke(ctrl, null);

            // Verify detail buddy count is updated to 12 and CanAdvanceTier is now true
            BuddyItemData updatedRadarEye = ctrl.AllBuddies.First(b => b.id == 3);
            Assert.AreEqual(12, updatedRadarEye.count, "Radar Eye count must be 12.");
            Assert.IsTrue(updatedRadarEye.CanAdvanceTier, "Radar Eye CanAdvanceTier should be true after receiving enough pieces.");
        }
        finally
        {
            if (ctrl != null)
            {
                var onDisableMethod = typeof(BuddyController).GetMethod("OnDisable",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                onDisableMethod?.Invoke(ctrl, null);
            }
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    [Test]
    public void BuddyShopSync_RepeatedTabSwitches_NeverDoubleGrantsOrOverwrites()
    {
        GameObject go = new GameObject("BuddyController_Test", typeof(BuddyController));
        go.hideFlags = HideFlags.HideAndDontSave;
        BuddyController ctrl = go.GetComponent<BuddyController>();
        try
        {
            var startMethod = typeof(BuddyController).GetMethod("Start",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod.Invoke(ctrl, null);

            // Reward 5 pieces to Drone 4 (Assault Blaster)
            PlayerDataService.AddBuddyPieces(4, 5);

            var onDisableMethod = typeof(BuddyController).GetMethod("OnDisable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var onEnableMethod = typeof(BuddyController).GetMethod("OnEnable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Switch tabs 6 times
            for (int i = 0; i < 6; i++)
            {
                onDisableMethod.Invoke(ctrl, null);
                onEnableMethod.Invoke(ctrl, null);
            }

            Assert.AreEqual(5, ctrl.AllBuddies.First(b => b.id == 4).count, "Pieces must remain strictly 5 across multiple tab switches.");
            Assert.AreEqual(5, PlayerDataService.GetBuddyPieceCount(4), "PlayerPrefs must remain strictly 5.");
        }
        finally
        {
            if (ctrl != null)
            {
                var onDisableMethod = typeof(BuddyController).GetMethod("OnDisable",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                onDisableMethod?.Invoke(ctrl, null);
            }
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    [Test]
    public void BuddyShopSync_RelaunchGame_PreservesSavedCounts()
    {
        // 1. Give pieces
        PlayerDataService.AddBuddyPieces(10, 8); // Purifying Drone
        Assert.AreEqual(8, PlayerDataService.GetBuddyPieceCount(10));

        // 2. Create brand new BuddyController (simulating fresh game launch)
        GameObject go = new GameObject("BuddyController_Fresh", typeof(BuddyController));
        go.hideFlags = HideFlags.HideAndDontSave;
        BuddyController ctrl = go.GetComponent<BuddyController>();
        try
        {
            var startMethod = typeof(BuddyController).GetMethod("Start",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod.Invoke(ctrl, null);

            Assert.AreEqual(8, ctrl.AllBuddies.First(b => b.id == 10).count, "Relaunch must preserve saved pieces from PlayerPrefs.");
        }
        finally
        {
            if (ctrl != null)
            {
                var onDisableMethod = typeof(BuddyController).GetMethod("OnDisable",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                onDisableMethod?.Invoke(ctrl, null);
            }
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}

public static class BuddyShopSyncTestRunner
{
    public const string ReportPath = "Assets/Editor/BuddyShopSyncTestReport.txt";

    [UnityEditor.MenuItem("PGE/Tests/Run Buddy Shop Sync Tests")]
    public static void RunFromMenu()
    {
        RunAllTestsAndSaveReport();
    }

    public static string RunAllTestsAndSaveReport()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine("BUDDY SHOP SYNC SYSTEM TEST REPORT");
        sb.AppendLine($"Timestamp (UTC): {System.DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
        sb.AppendLine("================================================================================");

        int passed = 0;
        int failed = 0;

        void RunTest(string testName, System.Action<BuddyShopSyncTests> testAction)
        {
            var test = new BuddyShopSyncTests();
            try
            {
                test.SetUp();
                testAction(test);
                passed++;
                sb.AppendLine($"[PASS] {testName}");
            }
            catch (System.Exception ex)
            {
                failed++;
                sb.AppendLine($"[FAIL] {testName}: {ex.Message}");
                Debug.LogException(ex);
            }
            finally
            {
                test.TearDown();
            }
        }

        RunTest("Test01_BuddyList_AlwaysHasExactlyFiveCanonicalDrones_WithoutDuplicates",
            t => t.BuddyList_AlwaysHasExactlyFiveCanonicalDrones_WithoutDuplicates());
        RunTest("Test02_BuddyShopSync_WhenPiecesGrantedWhileDisabled_SyncsAccuratelyOnReEnable",
            t => t.BuddyShopSync_WhenPiecesGrantedWhileDisabled_SyncsAccuratelyOnReEnable());
        RunTest("Test03_BuddyShopSync_DetailModalOpen_UpdatesPieceCountAndAdvanceTierInteractability",
            t => t.BuddyShopSync_DetailModalOpen_UpdatesPieceCountAndAdvanceTierInteractability());
        RunTest("Test04_BuddyShopSync_RepeatedTabSwitches_NeverDoubleGrantsOrOverwrites",
            t => t.BuddyShopSync_RepeatedTabSwitches_NeverDoubleGrantsOrOverwrites());
        RunTest("Test05_BuddyShopSync_RelaunchGame_PreservesSavedCounts",
            t => t.BuddyShopSync_RelaunchGame_PreservesSavedCounts());

        sb.AppendLine("================================================================================");
        sb.AppendLine($"SUMMARY: Total={passed + failed}, Passed={passed}, Failed={failed}");
        sb.AppendLine($"VERDICT: {(failed == 0 ? "CONFIRM_CORRECTNESS" : "HAS_FAILURES")}");
        sb.AppendLine("================================================================================");

        string report = sb.ToString();

        try
        {
            System.IO.File.WriteAllText(ReportPath, report);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[BuddyShopSyncTestRunner] Cannot write test report: {ex.Message}");
        }

        return report;
    }
}
#endif
