using UnityEngine;

/// <summary>
/// Persistent, sequential unlock state for the ten Tower Def levels.
/// Tower Def gameplay calls CompleteLevel after a real victory.
/// </summary>
public static class TowerDefProgress
{
    public const int LevelCount = 10;

    private const string CompletedLevelKey = "PGE.TowerDef.HighestCompletedLevel";
    private const string SelectedLevelKey = "PGE.TowerDef.SelectedLevel";

    public static int HighestCompletedLevel => Mathf.Clamp(PlayerPrefs.GetInt(CompletedLevelKey, 0), 0, LevelCount);
    public static int HighestUnlockedLevel => Mathf.Min(LevelCount, HighestCompletedLevel + 1);

    public static int SelectedLevel
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(SelectedLevelKey, 1), 1, LevelCount);
        set
        {
            if (!IsLevelUnlocked(value)) return;
            PlayerPrefs.SetInt(SelectedLevelKey, value);
            PlayerPrefs.Save();
        }
    }

    public static bool IsLevelUnlocked(int level)
    {
        return level >= 1 && level <= HighestUnlockedLevel;
    }

    public static bool CompleteLevel(int level)
    {
        if (level < 1 || level > LevelCount || !IsLevelUnlocked(level) || level <= HighestCompletedLevel)
            return false;

        PlayerPrefs.SetInt(CompletedLevelKey, level);
        PlayerPrefs.Save();
        return true;
    }
}
