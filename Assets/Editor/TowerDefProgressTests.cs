#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class TowerDefProgressTests
{
    private const string CompletedKey = "PGE.TowerDef.HighestCompletedLevel";
    private const string SelectedKey = "PGE.TowerDef.SelectedLevel";
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
        Assert.IsTrue(TowerDefProgress.IsLevelUnlocked(1));
        Assert.IsFalse(TowerDefProgress.IsLevelUnlocked(2));
        Assert.IsFalse(TowerDefProgress.CompleteLevel(2));

        for (int level = 1; level <= TowerDefProgress.LevelCount; level++)
        {
            Assert.IsTrue(TowerDefProgress.IsLevelUnlocked(level));
            Assert.IsTrue(TowerDefProgress.CompleteLevel(level));
            Assert.AreEqual(level, TowerDefProgress.HighestCompletedLevel);
            if (level < TowerDefProgress.LevelCount)
                Assert.IsTrue(TowerDefProgress.IsLevelUnlocked(level + 1));
        }

        Assert.AreEqual(TowerDefProgress.LevelCount, TowerDefProgress.HighestUnlockedLevel);
        Assert.IsFalse(TowerDefProgress.IsLevelUnlocked(11));
        Assert.IsFalse(TowerDefProgress.CompleteLevel(10));
    }

    [Test]
    public void LockedLevelCannotBeSelected()
    {
        TowerDefProgress.SelectedLevel = 2;
        Assert.AreEqual(1, TowerDefProgress.SelectedLevel);
        TowerDefProgress.CompleteLevel(1);
        TowerDefProgress.SelectedLevel = 2;
        Assert.AreEqual(2, TowerDefProgress.SelectedLevel);
    }
}
#endif
