#if UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[TestFixture]
[InitializeOnLoad]
public class GemMineRewardTests
{
    private const string ReportDir = "Reports";
    private const string ReportFile = "Reports/gem_mine_reward_test_result.txt";

    static GemMineRewardTests()
    {
        EditorApplication.delayCall += AutoRunTestsOnLoad;
    }

    [MenuItem("PGE/Tests/Run Gem Mine Reward Tests")]
    public static void RunTestsFromMenu()
    {
        AutoRunTestsOnLoad();
    }

    [InitializeOnLoadMethod]
    private static void AutoRunTestsOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            try
            {
                var t = new GemMineRewardTests();
                t.EnemySpawner_InGemMine_StageVictoryRewardHasZeroChips_OnlyRedGems();
                t.EnemySpawner_TriggerStageVictory_InGemMine_GrantsOnlyRedGems();
                t.VictoryPanelController_InGemMine_HidesDataChipRow_AndCentersRedGems();
                t.PlayerRunEndController_InGemMine_HidesDataChipRow_AndZeroChips();
                t.GenMineScene_EnemySpawner_HasZeroDataChipReward();

                string report = "[GemMineRewardTests] ALL 5 TESTS PASSED SUCCESSFULLY!\n" +
                                "1. EnemySpawner_InGemMine_StageVictoryRewardHasZeroChips_OnlyRedGems: PASSED\n" +
                                "2. EnemySpawner_TriggerStageVictory_InGemMine_GrantsOnlyRedGems: PASSED\n" +
                                "3. VictoryPanelController_InGemMine_HidesDataChipRow_AndCentersRedGems: PASSED\n" +
                                "4. PlayerRunEndController_InGemMine_HidesDataChipRow_AndZeroChips: PASSED\n" +
                                "5. GenMineScene_EnemySpawner_HasZeroDataChipReward: PASSED\n" +
                                $"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                Directory.CreateDirectory(ReportDir);
                File.WriteAllText(ReportFile, report);
                Debug.Log("<color=green>[GemMineRewardTests] ALL 5 TESTS PASSED SUCCESSFULLY!</color>");
            }
            catch (Exception ex)
            {
                string err = $"[GemMineRewardTests] TEST FAILED: {ex.Message}\n{ex.StackTrace}";
                Directory.CreateDirectory(ReportDir);
                File.WriteAllText(ReportFile, err);
                Debug.LogError(err);
            }
        };
    }

    [Test]
    public void EnemySpawner_InGemMine_StageVictoryRewardHasZeroChips_OnlyRedGems()
    {
        GameObject go = new GameObject("TestEnemySpawner_GemMine");
        try
        {
            EnemySpawner spawner = go.AddComponent<EnemySpawner>();
            // Use reflection or serialized object to set useSceneWaveConfiguration = true (Gem Mine mode)
            SerializedObject so = new SerializedObject(spawner);
            so.FindProperty("useSceneWaveConfiguration").boolValue = true;
            so.FindProperty("stageVictoryDataChipReward").intValue = 1000;
            so.FindProperty("stageVictoryRedGemReward").intValue = 89;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsTrue(spawner.IsGemMineScene(), "Spawner must identify as Gem Mine scene");
            Assert.AreEqual(0, spawner.StageVictoryDataChipReward, "StageVictoryDataChipReward must be 0 in Gem Mine");
            Assert.AreEqual(89, spawner.StageVictoryRedGemReward, "StageVictoryRedGemReward must retain red gems");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void EnemySpawner_TriggerStageVictory_InGemMine_GrantsOnlyRedGems()
    {
        int initialChips = ChipManager.DataChips;
        int initialGems = ChipManager.RedGems;

        GameObject playerGo = new GameObject("Player");
        playerGo.tag = "Player";
        GameObject spawnerGo = new GameObject("TestEnemySpawner_Victory");

        try
        {
            EnemySpawner spawner = spawnerGo.AddComponent<EnemySpawner>();
            SerializedObject so = new SerializedObject(spawner);
            so.FindProperty("useSceneWaveConfiguration").boolValue = true;
            so.FindProperty("stageVictoryDataChipReward").intValue = 1000;
            so.FindProperty("stageVictoryRedGemReward").intValue = 150;
            so.ApplyModifiedPropertiesWithoutUndo();

            spawner.TriggerStageVictory();

            Assert.AreEqual(initialChips, ChipManager.DataChips, "No data chips should be granted upon victory in Gem Mine");
            Assert.AreEqual(initialGems + 150, ChipManager.RedGems, "Red gems must be granted upon victory in Gem Mine");
        }
        finally
        {
            ChipManager.DataChips = initialChips;
            ChipManager.RedGems = initialGems;
            UnityEngine.Object.DestroyImmediate(playerGo);
            UnityEngine.Object.DestroyImmediate(spawnerGo);
        }
    }

    [Test]
    public void VictoryPanelController_InGemMine_HidesDataChipRow_AndCentersRedGems()
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        GameObject spawnerGo = new GameObject("EnemySpawner");
        EnemySpawner spawner = spawnerGo.AddComponent<EnemySpawner>();
        SerializedObject spawnerSo = new SerializedObject(spawner);
        spawnerSo.FindProperty("useSceneWaveConfiguration").boolValue = true;
        spawnerSo.ApplyModifiedPropertiesWithoutUndo();

        try
        {
            VictoryPanelController controller = canvasGo.AddComponent<VictoryPanelController>();

            // Build mock rows
            GameObject chipRow = new GameObject("DataChipReward", typeof(RectTransform));
            chipRow.transform.SetParent(canvasGo.transform, false);
            GameObject chipTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            chipTextObj.transform.SetParent(chipRow.transform, false);
            TMP_Text chipText = chipTextObj.GetComponent<TMP_Text>();

            GameObject gemRow = new GameObject("RedGemReward", typeof(RectTransform));
            gemRow.transform.SetParent(canvasGo.transform, false);
            GameObject gemTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            gemTextObj.transform.SetParent(gemRow.transform, false);
            TMP_Text gemText = gemTextObj.GetComponent<TMP_Text>();

            GameObject detailsBtnObj = new GameObject("DetailsButton", typeof(RectTransform), typeof(Button));
            detailsBtnObj.transform.SetParent(canvasGo.transform, false);
            Button detailsBtn = detailsBtnObj.GetComponent<Button>();

            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("enemySpawner").objectReferenceValue = spawner;
            controllerSo.FindProperty("dataChipRewardText").objectReferenceValue = chipText;
            controllerSo.FindProperty("redGemRewardText").objectReferenceValue = gemText;
            controllerSo.FindProperty("detailsButton").objectReferenceValue = detailsBtn;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            controller.EnsureRewardRowLayout();

            Assert.IsFalse(chipRow.activeSelf, "Data chip reward row must be hidden in Gem Mine");
            RectTransform gemRect = gemRow.GetComponent<RectTransform>();
            Assert.AreEqual(-35f, gemRect.anchoredPosition.y, 0.1f, "Red gem row must be vertically centered at y = -35f");
            RectTransform btnRect = detailsBtnObj.GetComponent<RectTransform>();
            Assert.AreEqual(-35f, btnRect.anchoredPosition.y, 0.1f, "Details button must be vertically aligned at y = -35f");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(canvasGo);
            UnityEngine.Object.DestroyImmediate(spawnerGo);
        }
    }

    [Test]
    public void PlayerRunEndController_InGemMine_HidesDataChipRow_AndZeroChips()
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        GameObject spawnerGo = new GameObject("EnemySpawner");
        EnemySpawner spawner = spawnerGo.AddComponent<EnemySpawner>();
        SerializedObject spawnerSo = new SerializedObject(spawner);
        spawnerSo.FindProperty("useSceneWaveConfiguration").boolValue = true;
        spawnerSo.ApplyModifiedPropertiesWithoutUndo();

        try
        {
            PlayerRunEndController controller = canvasGo.AddComponent<PlayerRunEndController>();

            // Build mock rows
            GameObject chipRow = new GameObject("DataChipReward", typeof(RectTransform));
            chipRow.transform.SetParent(canvasGo.transform, false);
            GameObject chipTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            chipTextObj.transform.SetParent(chipRow.transform, false);
            TMP_Text chipText = chipTextObj.GetComponent<TMP_Text>();

            GameObject gemRow = new GameObject("RedGemReward", typeof(RectTransform));
            gemRow.transform.SetParent(canvasGo.transform, false);
            GameObject gemTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            gemTextObj.transform.SetParent(gemRow.transform, false);
            TMP_Text gemText = gemTextObj.GetComponent<TMP_Text>();

            GameObject detailsBtnObj = new GameObject("DetailsButton", typeof(RectTransform), typeof(Button));
            detailsBtnObj.transform.SetParent(canvasGo.transform, false);
            Button detailsBtn = detailsBtnObj.GetComponent<Button>();

            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("enemySpawner").objectReferenceValue = spawner;
            controllerSo.FindProperty("dataChipRewardText").objectReferenceValue = chipText;
            controllerSo.FindProperty("redGemRewardText").objectReferenceValue = gemText;
            controllerSo.FindProperty("detailsButton").objectReferenceValue = detailsBtn;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            controller.EnsureRewardRowLayout();

            Assert.IsFalse(chipRow.activeSelf, "Data chip reward row must be hidden in Gem Mine on game over panel");
            RectTransform gemRect = gemRow.GetComponent<RectTransform>();
            Assert.AreEqual(-35f, gemRect.anchoredPosition.y, 0.1f, "Red gem row must be vertically centered at y = -35f");
            RectTransform btnRect = detailsBtnObj.GetComponent<RectTransform>();
            Assert.AreEqual(-35f, btnRect.anchoredPosition.y, 0.1f, "Details button must be vertically aligned at y = -35f");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(canvasGo);
            UnityEngine.Object.DestroyImmediate(spawnerGo);
        }
    }

    [Test]
    public void GenMineScene_EnemySpawner_HasZeroDataChipReward()
    {
        string scenePath = "Assets/Scenes/GenMine.unity";
        Assert.IsTrue(File.Exists(scenePath), "GenMine scene file must exist");
        string sceneContent = File.ReadAllText(scenePath);
        Assert.IsTrue(sceneContent.Contains("stageVictoryDataChipReward: 0"),
            "GenMine scene file must have stageVictoryDataChipReward set to 0");
        Assert.IsFalse(sceneContent.Contains("m_Name: DataChipReward\n  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1"),
            "DataChipReward GameObjects in GenMine.unity must be deactivated (m_IsActive: 0)");
    }
}
#endif
