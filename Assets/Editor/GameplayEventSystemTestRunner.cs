using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PGE.Tests
{
    [InitializeOnLoad]
    public class GameplayEventSystemTestRunner
    {
        public const string ReportPath = "Assets/Editor/GameplayEventTestReport.txt";

        static GameplayEventSystemTestRunner()
        {
            EditorApplication.delayCall += () =>
            {
                RunAllTestsAndSaveReport();
            };
        }

        [MenuItem("PGE/Tests/Run Gameplay Event Tests")]
        public static void RunFromMenu()
        {
            RunAllTestsAndSaveReport();
        }

        public static string RunAllTestsAndSaveReport()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("GAMEPLAY EVENT SYSTEM EXECUTION REPORT");
            sb.AppendLine($"Timestamp (UTC): {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
            sb.AppendLine("================================================================================");

            int passed = 0;
            int failed = 0;

            void RunTest(string testName, Action testAction)
            {
                var test = new GameplayEventSystemTests();
                try
                {
                    test.SetUp();
                    testAction();
                    passed++;
                    sb.AppendLine($"[PASS] {testName}");
                }
                catch (Exception ex)
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

            var runner = new GameplayEventSystemTests();

            RunTest("Test01_Database_InitializesWithDefaultEvents", () => runner.Test01_Database_InitializesWithDefaultEvents());
            RunTest("Test02_GameplayEventPickup_ActiveTracking", () => runner.Test02_GameplayEventPickup_ActiveTracking());
            RunTest("Test03_OptionRewards_MoveSpeedPercent", () => runner.Test03_OptionRewards_MoveSpeedPercent());
            RunTest("Test04_OptionRewards_HpConsumption", () => runner.Test04_OptionRewards_HpConsumption());
            RunTest("Test05_OptionRewards_ArtifactGrant", () => runner.Test05_OptionRewards_ArtifactGrant());
            RunTest("Test06_ModalLifecycle_PauseResume", () => runner.Test06_ModalLifecycle_PauseResume());
            RunTest("Test07_ProceduralSprites_GeneratedWithoutError", () => runner.Test07_ProceduralSprites_GeneratedWithoutError());
            RunTest("Test08_RewardsHiddenUntilOptionChosen", () => runner.Test08_RewardsHiddenUntilOptionChosen());

            sb.AppendLine("================================================================================");
            sb.AppendLine($"SUMMARY: Total={passed + failed}, Passed={passed}, Failed={failed}");
            sb.AppendLine($"VERDICT: {(failed == 0 ? "CONFIRM_CORRECTNESS" : "HAS_FAILURES")}");
            sb.AppendLine("================================================================================");

            string report = sb.ToString();
            Debug.Log(report);

            try
            {
                File.WriteAllText(ReportPath, report);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameplayEventSystemTestRunner] Không thể ghi file report: {ex.Message}");
            }

            return report;
        }
    }
}
