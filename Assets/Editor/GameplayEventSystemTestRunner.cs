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

        [MenuItem("PGE/Tools/Clean Test Garbage in Scene")]
        public static void CleanTestGarbage()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            int count = 0;
            var roots = scene.GetRootGameObjects();
            for (int i = roots.Length - 1; i >= 0; i--)
            {
                var root = roots[i];
                if (root == null) continue;
                string n = root.name;
                if (n == "Event1" || n == "Player" || n == "[DamageNumberManager]" || (n == "Canvas" && root.transform.childCount == 0))
                {
                    Undo.DestroyObjectImmediate(root);
                    count++;
                }
            }
            Debug.Log($"[Clean] Đã dọn dẹp sạch {count} đối tượng rác khỏi Scene!");
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

            void RunTest(string testName, Action<GameplayEventSystemTests> testAction)
            {
                var test = new GameplayEventSystemTests();
                try
                {
                    test.SetUp();
                    testAction(test);
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

            RunTest("Test01_Database_InitializesWithDefaultEvents", t => t.Test01_Database_InitializesWithDefaultEvents());
            RunTest("Test02_GameplayEventPickup_ActiveTracking", t => t.Test02_GameplayEventPickup_ActiveTracking());
            RunTest("Test03_OptionRewards_MoveSpeedPercent", t => t.Test03_OptionRewards_MoveSpeedPercent());
            RunTest("Test04_OptionRewards_HpConsumption", t => t.Test04_OptionRewards_HpConsumption());
            RunTest("Test05_OptionRewards_ArtifactGrant", t => t.Test05_OptionRewards_ArtifactGrant());
            RunTest("Test06_ModalLifecycle_PauseResume", t => t.Test06_ModalLifecycle_PauseResume());
            RunTest("Test07_ProceduralSprites_GeneratedWithoutError", t => t.Test07_ProceduralSprites_GeneratedWithoutError());
            RunTest("Test08_RewardsHiddenUntilOptionChosen", t => t.Test08_RewardsHiddenUntilOptionChosen());

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
