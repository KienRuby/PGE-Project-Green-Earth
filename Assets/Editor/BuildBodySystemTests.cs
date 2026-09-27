using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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

            // Test 5: Verify StatsButton and BuildBodyButton can be clicked and switched back and forth
            try
            {
                GameObject topTabsGo = GameObject.Find("TopTabs");
                BuildBodyController controller = topTabsGo != null ? topTabsGo.GetComponent<BuildBodyController>() : null;
                Assert("Test09a_Controller_Found", controller != null, "BuildBodyController must exist on TopTabs");

                if (controller != null)
                {
                    SerializedObject so = new SerializedObject(controller);
                    Button statsBtn = so.FindProperty("statsTabButton").objectReferenceValue as Button;
                    Button buildBtn = so.FindProperty("buildBodyTabButton").objectReferenceValue as Button;
                    GameObject statsPnl = so.FindProperty("statsPanel").objectReferenceValue as GameObject;
                    GameObject buildPnl = so.FindProperty("buildBodyPanel").objectReferenceValue as GameObject;

                    Assert("Test09b_StatsButton_Raycastable",
                        statsBtn != null && statsBtn.interactable && statsBtn.targetGraphic != null && statsBtn.targetGraphic.raycastTarget,
                        "StatsButton must be interactable with raycastTarget=true");

                    Assert("Test09c_BuildBodyButton_Raycastable",
                        buildBtn != null && buildBtn.interactable && buildBtn.targetGraphic != null && buildBtn.targetGraphic.raycastTarget,
                        "BuildBodyButton must be interactable with raycastTarget=true");

                    // Set unlocked chapter to 3 to allow switching
                    PlayerDataService.UnlockedChapterIndex = 3;

                    // Switch to Build Body
                    controller.OnBuildBodyTabClicked();
                    Assert("Test09d_SwitchToBuildBody_Success",
                        (statsPnl == null || !statsPnl.activeSelf) && (buildPnl != null && buildPnl.activeSelf),
                        "BuildBodyPanel must be active and StatsPanel inactive");

                    // Switch back to Stats
                    controller.OnStatsTabClicked();
                    Assert("Test09e_SwitchBackToStats_Success",
                        (statsPnl != null && statsPnl.activeSelf) && (buildPnl == null || !buildPnl.activeSelf),
                        "StatsPanel must be active and BuildBodyPanel inactive when switching back");
                }
            }
            catch (Exception ex)
            {
                Assert("Test09_TabSwitching_Exception", false, ex.Message);
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
        try
        {
            File.WriteAllText(ReportPath, report);
        }
        catch { }

        return report;
    }
}
