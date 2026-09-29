#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class TowerDefProgressTests
{
    private const string CompletedKey = "PGE.TowerDef.HighestCompletedLevel";
    private const string SelectedKey = "PGE.TowerDef.SelectedLevel";
    private const string ChapterKey = PlayerDataService.UnlockedChapterIndexKey;
    private bool hadCompleted;
    private bool hadSelected;
    private bool hadChapter;
    private int savedCompleted;
    private int savedSelected;
    private int savedChapter;

    [SetUp]
    public void SetUp()
    {
        hadCompleted = PlayerPrefs.HasKey(CompletedKey);
        hadSelected = PlayerPrefs.HasKey(SelectedKey);
        hadChapter = PlayerPrefs.HasKey(ChapterKey);
        savedCompleted = PlayerPrefs.GetInt(CompletedKey);
        savedSelected = PlayerPrefs.GetInt(SelectedKey);
        savedChapter = PlayerPrefs.GetInt(ChapterKey);
        PlayerPrefs.DeleteKey(CompletedKey);
        PlayerPrefs.DeleteKey(SelectedKey);
        PlayerPrefs.DeleteKey(ChapterKey);
    }

    [TearDown]
    public void TearDown()
    {
        if (hadCompleted) PlayerPrefs.SetInt(CompletedKey, savedCompleted);
        else PlayerPrefs.DeleteKey(CompletedKey);
        if (hadSelected) PlayerPrefs.SetInt(SelectedKey, savedSelected);
        else PlayerPrefs.DeleteKey(SelectedKey);
        if (hadChapter) PlayerPrefs.SetInt(ChapterKey, savedChapter);
        else PlayerPrefs.DeleteKey(ChapterKey);
        PlayerPrefs.Save();
    }

    [Test]
    public void LevelsUnlockAfterEveryThreeClearedChapters()
    {
        Assert.IsFalse(TowerDefProgress.IsLevelUnlocked(1));
        PlayerPrefs.SetInt(ChapterKey, 2);
        Assert.IsFalse(TowerDefProgress.IsLevelUnlocked(1));
        PlayerPrefs.SetInt(ChapterKey, 3);
        Assert.IsTrue(TowerDefProgress.IsLevelUnlocked(1));
        Assert.IsFalse(TowerDefProgress.IsLevelUnlocked(2));
        Assert.IsFalse(TowerDefProgress.CompleteLevel(2));

        Assert.IsTrue(TowerDefProgress.CompleteLevel(1));
        Assert.IsFalse(TowerDefProgress.IsLevelUnlocked(2));

        for (int level = 1; level <= TowerDefProgress.LevelCount; level++)
        {
            PlayerPrefs.SetInt(ChapterKey, level * 3 - 1);
            Assert.IsFalse(TowerDefProgress.IsLevelUnlocked(level));
            PlayerPrefs.SetInt(ChapterKey, level * 3);
            Assert.IsTrue(TowerDefProgress.IsLevelUnlocked(level));
            if (level > 1)
            {
                Assert.IsTrue(TowerDefProgress.CompleteLevel(level));
                Assert.AreEqual(level, TowerDefProgress.HighestCompletedLevel);
            }
            if (level < TowerDefProgress.LevelCount)
                Assert.IsFalse(TowerDefProgress.IsLevelUnlocked(level + 1));
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
        PlayerPrefs.SetInt(ChapterKey, 3);
        TowerDefProgress.CompleteLevel(1);
        TowerDefProgress.SelectedLevel = 2;
        Assert.AreEqual(1, TowerDefProgress.SelectedLevel);
        PlayerPrefs.SetInt(ChapterKey, 6);
        TowerDefProgress.SelectedLevel = 2;
        Assert.AreEqual(2, TowerDefProgress.SelectedLevel);
    }
}
#endif
