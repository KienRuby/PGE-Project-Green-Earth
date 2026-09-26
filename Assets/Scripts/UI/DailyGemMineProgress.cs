using UnityEngine;

/// <summary>
/// Quản lý tiến trình mở khóa tuần tự các màn Daily Gem Mine (Cấp 01 đến Cấp 05):
/// - Mặc định ban đầu chỉ mở duy nhất Màn 1 với nút Start.
/// - Chỉ khi vượt qua màn trước thì mới mở khóa màn tiếp theo (màn 2 mở sau khi thắng màn 1, v.v.).
/// - Lưu trữ persistent thông qua PlayerPrefs tương tự TowerDefProgress.
/// </summary>
public static class DailyGemMineProgress
{
    public const int LevelCount = 5;

    private const string CompletedLevelKey = "PGE.DailyGemMine.HighestCompletedLevel";
    private const string SelectedLevelKey = "PGE.DailyGemMine.SelectedLevel";

    public static int HighestCompletedLevel
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(CompletedLevelKey, 0), 0, LevelCount);
        set
        {
            PlayerPrefs.SetInt(CompletedLevelKey, Mathf.Clamp(value, 0, LevelCount));
            PlayerPrefs.Save();
        }
    }

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

        HighestCompletedLevel = level;
        return true;
    }

    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(CompletedLevelKey);
        PlayerPrefs.DeleteKey(SelectedLevelKey);
        PlayerPrefs.Save();
    }

    public static void GetRewardRange(int level, out int min, out int max)
    {
        switch (level)
        {
            case 1: min = 60; max = 120; break;
            case 2: min = 120; max = 220; break;
            case 3: min = 200; max = 350; break;
            case 4: min = 300; max = 500; break;
            case 5: min = 450; max = 700; break;
            default: min = 60; max = 120; break;
        }
    }

    public static string GetRewardRangeString(int level)
    {
        GetRewardRange(level, out int min, out int max);
        return $"x{min}-{max}";
    }

    public static int GenerateRewardAmount(int level)
    {
        GetRewardRange(level, out int min, out int max);
        return Random.Range(min, max + 1);
    }
}
