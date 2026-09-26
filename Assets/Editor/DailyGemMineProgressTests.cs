#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class DailyGemMineProgressTests
{
    private const string CompletedKey = "PGE.DailyGemMine.HighestCompletedLevel";
    private const string SelectedKey = "PGE.DailyGemMine.SelectedLevel";
    private bool hadCompleted;
    private bool hadSelected;
    private int savedCompleted;
    private int savedSelected;

    [SetUp]
    public void SetUp()
    {
        hadCompleted = PlayerPrefs.HasKey(CompletedKey);
        hadSelected = PlayerPrefs.HasKey(SelectedKey);
        savedCompleted = PlayerPrefs.GetInt(CompletedKey);
        savedSelected = PlayerPrefs.GetInt(SelectedKey);
        PlayerPrefs.DeleteKey(CompletedKey);
        PlayerPrefs.DeleteKey(SelectedKey);
    }

    [TearDown]
    public void TearDown()
    {
        if (hadCompleted) PlayerPrefs.SetInt(CompletedKey, savedCompleted);
        else PlayerPrefs.DeleteKey(CompletedKey);
        if (hadSelected) PlayerPrefs.SetInt(SelectedKey, savedSelected);
        else PlayerPrefs.DeleteKey(SelectedKey);
        PlayerPrefs.Save();
    }

    [Test]
    public void LevelsUnlockOnlyAfterClearingThePreviousLevel()
    {
        // 1. Mặc định chỉ mở duy nhất Màn 1
        Assert.IsTrue(DailyGemMineProgress.IsLevelUnlocked(1), "Level 1 must be unlocked by default");
        Assert.IsFalse(DailyGemMineProgress.IsLevelUnlocked(2), "Level 2 must be locked initially");
        Assert.IsFalse(DailyGemMineProgress.CompleteLevel(2), "Cannot complete Level 2 before unlocking it");

        // 2. Tuần tự vượt qua từng màn để mở màn kế tiếp
        for (int level = 1; level <= DailyGemMineProgress.LevelCount; level++)
        {
            Assert.IsTrue(DailyGemMineProgress.IsLevelUnlocked(level), $"Level {level} must be unlocked");
            Assert.IsTrue(DailyGemMineProgress.CompleteLevel(level), $"CompleteLevel({level}) must succeed");
            Assert.AreEqual(level, DailyGemMineProgress.HighestCompletedLevel);
            if (level < DailyGemMineProgress.LevelCount)
            {
                Assert.IsTrue(DailyGemMineProgress.IsLevelUnlocked(level + 1), $"Level {level + 1} must unlock after completing Level {level}");
            }
        }

        Assert.AreEqual(DailyGemMineProgress.LevelCount, DailyGemMineProgress.HighestUnlockedLevel);
        Assert.IsFalse(DailyGemMineProgress.IsLevelUnlocked(6), "Level 6 must not exist/unlock");
        Assert.IsFalse(DailyGemMineProgress.CompleteLevel(5), "Cannot re-complete already highest completed level");
    }

    [Test]
    public void LockedLevelCannotBeSelected()
    {
        DailyGemMineProgress.SelectedLevel = 2;
        Assert.AreEqual(1, DailyGemMineProgress.SelectedLevel, "Locked level 2 cannot be selected");

        DailyGemMineProgress.CompleteLevel(1);
        DailyGemMineProgress.SelectedLevel = 2;
        Assert.AreEqual(2, DailyGemMineProgress.SelectedLevel, "Level 2 can be selected once unlocked");
    }

    [Test]
    public void RewardRanges_MatchesDisplaySpecification()
    {
        Assert.AreEqual("x60-120", DailyGemMineProgress.GetRewardRangeString(1));
        Assert.AreEqual("x120-220", DailyGemMineProgress.GetRewardRangeString(2));
        Assert.AreEqual("x200-350", DailyGemMineProgress.GetRewardRangeString(3));
        Assert.AreEqual("x300-500", DailyGemMineProgress.GetRewardRangeString(4));
        Assert.AreEqual("x450-700", DailyGemMineProgress.GetRewardRangeString(5));
    }

    [Test]
    public void RewardGeneration_ReturnsValueWithinRange()
    {
        for (int level = 1; level <= DailyGemMineProgress.LevelCount; level++)
        {
            DailyGemMineProgress.GetRewardRange(level, out int min, out int max);
            for (int i = 0; i < 50; i++)
            {
                int reward = DailyGemMineProgress.GenerateRewardAmount(level);
                Assert.GreaterOrEqual(reward, min, $"Reward for level {level} must be >= {min}");
                Assert.LessOrEqual(reward, max, $"Reward for level {level} must be <= {max}");
            }
        }
    }
}
#endif
