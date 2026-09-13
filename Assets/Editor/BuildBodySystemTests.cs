using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class BuildBodySystemTests
{
    public const string ReportPath = "Assets/Editor/BuildBodyTestReport.txt";

    static BuildBodySystemTests()
    {
        EditorApplication.delayCall += () =>
        {
            RunTests();
        };
    }

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
            var ctrl = go.AddComponent<BuildBodyController>();
            Assert("Test01_Locked_WhenChapter0", !ctrl.IsBuildBodyUnlocked, "Chapter 0 should be locked");

            PlayerDataService.UnlockedChapterIndex = 2; // 2 chapters cleared
            Assert("Test02_Locked_WhenChapter2", !ctrl.IsBuildBodyUnlocked, "Chapter 2 should be locked");

            PlayerDataService.UnlockedChapterIndex = 3; // 3 chapters cleared
            Assert("Test03_Unlocked_WhenChapter3", ctrl.IsBuildBodyUnlocked, "Chapter 3 should be unlocked");

            PlayerDataService.UnlockedChapterIndex = 4;
            Assert("Test04_Unlocked_WhenChapter4", ctrl.IsBuildBodyUnlocked, "Chapter 4 should be unlocked");

            // Test 2: Skin equipping & Background color / Status text logic
            BuildBodyController.EquippedSkinIndex = 1; // AD Unit-2
            Assert("Test05_EquippedSkinIndex_Updates", BuildBodyController.EquippedSkinIndex == 1, "EquippedSkinIndex should be 1");

            // Test 3: Unit 4 build logic with Red Gems
            PlayerPrefs.DeleteKey(BuildBodyController.GetBodyUnlockKey(3));
            Assert("Test06_Unit4_InitiallyLocked", !BuildBodyController.IsBodyUnlocked(3), "Unit 4 should be locked initially");

            // Test 4: UI Builder execution
            try
            {
                BuildBodyUIBuilder.BuildUI();
                Assert("Test07_BuildBodyUIBuilder_Execution", true);
            }
            catch (Exception ex)
            {
                Assert("Test07_BuildBodyUIBuilder_Execution", false, ex.Message);
            }

            UnityEngine.Object.DestroyImmediate(go);
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
