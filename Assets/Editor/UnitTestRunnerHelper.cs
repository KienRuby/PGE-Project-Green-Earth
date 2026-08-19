#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class UnitTestRunnerHelper
{
    private static TestRunnerApi api;

    [MenuItem("PGE/Tests/Run EditMode Tests")]
    public static void RunEditModeTestsFromMenu()
    {
        RunEditModeTests();
    }

    public static void RunEditModeTests()
    {
        if (api == null)
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
        }

        api.RegisterCallbacks(new TestCallbacks());
        api.Execute(new ExecutionSettings
        {
            filters = new[]
            {
                new Filter
                {
                    testMode = TestMode.EditMode
                }
            }
        });
    }

    private class TestCallbacks : ICallbacks
    {
        private readonly StringBuilder log = new StringBuilder();

        public void RunStarted(ITestAdaptor testsToRun)
        {
            log.AppendLine($"=== RUN STARTED: {testsToRun.TestCaseCount} test cases ===");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            log.AppendLine($"\n=== RUN FINISHED ===");
            log.AppendLine($"Total: {result.PassCount + result.FailCount + result.InconclusiveCount + result.SkipCount}");
            log.AppendLine($"Passed: {result.PassCount}");
            log.AppendLine($"Failed: {result.FailCount}");
            log.AppendLine($"Inconclusive: {result.InconclusiveCount}");
            log.AppendLine($"Skipped: {result.SkipCount}");

            File.WriteAllText("TestExecutionSummary.txt", log.ToString());
            Debug.Log(log.ToString());

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(result.FailCount > 0 ? 1 : 0);
            }
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.HasChildren) return;

            string status = result.TestStatus.ToString();
            log.AppendLine($"[{status}] {result.Test.FullName} ({result.Duration:F3}s)");
            if (result.TestStatus == TestStatus.Failed)
            {
                log.AppendLine($"  ERROR: {result.Message}");
                log.AppendLine($"  STACKTRACE: {result.StackTrace}");
            }
        }
    }
}
#endif
