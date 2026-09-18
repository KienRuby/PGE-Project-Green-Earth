using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class BuildBodySystemTests
{
    public const string ReportPath = "Assets/Editor/BuildBodyTestReport.txt";

    [MenuItem("PGE/Tests/Run Build Body System Tests")]
    public static void RunFromMenu()
    {
        RunTests();
    }

    public static string RunTests()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return "Skipped: In Play Mode";
        }

        var sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine("BUILD BODY SYSTEM TEST REPORT");
        sb.AppendLine("================================================================================");

        int passed = 0;
        int failed = 0;

        void Assert(string testName, bool condition, string reason = "")
        {
            if (condition)
            {
                passed++;
                sb.AppendLine($"[PASS] {testName}");
            }
            else
            {
                failed++;
                sb.AppendLine($"[FAIL] {testName}: {reason}");
            }
        }

        int origChapter = PlayerDataService.UnlockedChapterIndex;
        int origSkin = BuildBodyController.EquippedSkinIndex;
        bool origTestMode = ChipManager.IsTestMode;
        ChipManager.IsTestMode = false;

        try
        {
            // Test 1: Chapter lock condition (< 3 chapters => locked, >= 3 chapters => unlocked)
            PlayerDataService.UnlockedChapterIndex = 0;
            var go = new GameObject("TestBuildBodyController");
            go.hideFlags = HideFlags.HideAndDontSave;
            var ctrl = go.AddComponent<BuildBodyController>();
            try
            {
                Assert("Test01_Locked_WhenChapter0", !ctrl.IsBuildBodyUnlocked, "Chapter 0 should be locked");

                PlayerDataService.UnlockedChapterIndex = 2; // 2 chapters cleared
                Assert("Test02_Locked_WhenChapter2", !ctrl.IsBuildBodyUnlocked, "Chapter 2 should be locked");

                PlayerDataService.UnlockedChapterIndex = 3; // 3 chapters cleared
                Assert("Test03_Unlocked_WhenChapter3", ctrl.IsBuildBodyUnlocked, "Chapter 3 should be unlocked");

                PlayerDataService.UnlockedChapterIndex = 4;
                Assert("Test04_Unlocked_WhenChapter4", ctrl.IsBuildBodyUnlocked, "Chapter 4 should be unlocked");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            // Test 2: Skin equipping & Background color / Status text logic
            PlayerPrefs.SetInt(BuildBodyController.GetBodyUnlockKey(2), 1);
            BuildBodyController.EquippedSkinIndex = 2; // AD Unit-2 (index 2)
            Assert("Test05_EquippedSkinIndex_Updates", BuildBodyController.EquippedSkinIndex == 2, "EquippedSkinIndex should be 2");

            // Test 3: Slot 0 (Default) is always unlocked from start, while AD Units 1..4 are initially locked
            PlayerPrefs.DeleteKey(BuildBodyController.GetBodyUnlockKey(0));
            PlayerPrefs.DeleteKey(BuildBodyController.GetBodyUnlockKey(1));
            PlayerPrefs.DeleteKey(BuildBodyController.GetBodyUnlockKey(2));
            PlayerPrefs.DeleteKey(BuildBodyController.GetBodyUnlockKey(3));
            PlayerPrefs.DeleteKey(BuildBodyController.GetBodyUnlockKey(4));
            Assert("Test06_DefaultUnlocked_And_UnitsInitiallyLocked",
                BuildBodyController.IsBodyUnlocked(0) &&
                !BuildBodyController.IsBodyUnlocked(1) &&
                !BuildBodyController.IsBodyUnlocked(2) &&
                !BuildBodyController.IsBodyUnlocked(3) &&
                !BuildBodyController.IsBodyUnlocked(4), "Default skin must be unlocked, units 1-4 locked initially");

            var costCtrlObj = new GameObject("CostTestCtrl");
            costCtrlObj.hideFlags = HideFlags.HideAndDontSave;
            var costCtrl = costCtrlObj.AddComponent<BuildBodyController>();
            try
            {
                Assert("Test07_BuildCosts_Match",
                    costCtrl.GetBuildCost(0) == 0 &&
                    costCtrl.GetBuildCost(1) == 1000 &&
                    costCtrl.GetBuildCost(2) == 1500 &&
                    costCtrl.GetBuildCost(3) == 2000 &&
                    costCtrl.GetBuildCost(4) == 3000, "Costs must be 0 (Default), 1000, 1500, 2000, 3000");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(costCtrlObj);
            }

            // Test 4: UI Builder execution
            try
            {
                BuildBodyUIBuilder.BuildUI();
                Assert("Test08_BuildBodyUIBuilder_Execution", true);
            }
            catch (Exception ex)
            {
                Assert("Test08_BuildBodyUIBuilder_Execution", false, ex.Message);
            }
        }
        catch (Exception ex)
        {
            failed++;
            sb.AppendLine($"[EXCEPTION] Unexpected test failure: {ex}");
        }
        finally
        {
            PlayerDataService.UnlockedChapterIndex = origChapter;
            BuildBodyController.EquippedSkinIndex = origSkin;
            ChipManager.IsTestMode = origTestMode;
        }

        sb.AppendLine("================================================================================");
        sb.AppendLine($"SUMMARY: Total={passed + failed}, Passed={passed}, Failed={failed}");
        sb.AppendLine(failed == 0 ? "VERDICT: CONFIRM_CORRECTNESS" : "VERDICT: TEST_FAILURES");
        sb.AppendLine("================================================================================");

        string report = sb.ToString();
        Debug.Log(report);
        try
        {
            File.WriteAllText(ReportPath, report);
        }
        catch { }

        return report;
    }
}
