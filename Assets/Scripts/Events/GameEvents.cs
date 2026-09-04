using System;

/// <summary>
/// Event bus tĩnh tập trung cho toàn bộ sự kiện gameplay trong Project Green Earth.
/// Giúp phân tách triệt để giữa logic gameplay (Enemy, Combat, Player, Lab, Drone)
/// và các hệ thống meta-game như Achievements, Quests, Analytics mà không tạo phụ thuộc chéo.
/// </summary>
public static class GameEvents
{
    /// <summary>
    /// Phát khi một quái vật hoặc boss bị tiêu diệt.
    /// </summary>
    public static event Action OnEnemyKilled;

    /// <summary>
    /// Phát khi một quái vật bị tiêu diệt kèm giá trị exp thưởng.
    /// </summary>
    public static event Action<int> OnEnemyKilledWithExp;

    /// <summary>
    /// Phát khi người chơi nâng cấp tăng bậc (Tier) cho Drone / Buddy thành công.
    /// </summary>
    public static event Action OnDroneTierAdvanced;

    /// <summary>
    /// Phát khi Drone tăng bậc chi tiết (droneId, newTier).
    /// </summary>
    public static event Action<string, int> OnDroneTierAdvancedDetailed;

    /// <summary>
    /// Phát khi người chơi bắt đầu chơi một Chapter (tham số: index chapter, 0-based).
    /// </summary>
    public static event Action<int> OnChapterPlayed;

    /// <summary>
    /// Phát khi người chơi vượt qua toàn bộ các wave và hoàn thành Chapter (tham số: chapterNumber, 1-based).
    /// </summary>
    public static event Action<int> OnChapterCleared;

    /// <summary>
    /// Phát khi người chơi hoàn thành Chapter với số sao đạt được (chapterNumber, starsEarned).
    /// </summary>
    public static event Action<int, int> OnChapterClearedDetailed;

    /// <summary>
    /// Phát khi người chơi lên cấp trong trận đấu (tham số: newLevel).
    /// </summary>
    public static event Action<int> OnPlayerLevelUp;

    /// <summary>
    /// Phát khi số dư tiền tệ thay đổi (loại tiền, số lượng mới).
    /// </summary>
    public static event Action<string, int> OnCurrencyChanged;

    public static void RaiseEnemyKilled()
    {
        OnEnemyKilled?.Invoke();
    }

    public static void RaiseEnemyKilled(int expValue)
    {
        OnEnemyKilled?.Invoke();
        OnEnemyKilledWithExp?.Invoke(expValue);
    }

    public static void RaiseDroneTierAdvanced()
    {
        OnDroneTierAdvanced?.Invoke();
    }

    public static void RaiseDroneTierAdvanced(string droneId, int newTier)
    {
        OnDroneTierAdvanced?.Invoke();
        OnDroneTierAdvancedDetailed?.Invoke(droneId, newTier);
    }

    public static void RaiseChapterPlayed(int chapterIndex)
    {
        OnChapterPlayed?.Invoke(chapterIndex);
    }

    public static void RaiseChapterCleared(int chapterNumber)
    {
        OnChapterCleared?.Invoke(chapterNumber);
    }

    public static void RaiseChapterCleared(int chapterNumber, int starsEarned)
    {
        OnChapterCleared?.Invoke(chapterNumber);
        OnChapterClearedDetailed?.Invoke(chapterNumber, starsEarned);
    }

    public static void RaisePlayerLevelUp(int newLevel)
    {
        OnPlayerLevelUp?.Invoke(newLevel);
    }

    public static void RaiseCurrencyChanged(string currencyType, int newAmount)
    {
        OnCurrencyChanged?.Invoke(currencyType, newAmount);
    }
}
