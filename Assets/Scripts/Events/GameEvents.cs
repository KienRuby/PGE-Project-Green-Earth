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
    /// Phát khi người chơi nâng cấp tăng bậc (Tier) cho Drone / Buddy thành công.
    /// </summary>
    public static event Action OnDroneTierAdvanced;

    /// <summary>
    /// Phát khi người chơi bắt đầu chơi một Chapter (tham số: index chapter, 0-based).
    /// </summary>
    public static event Action<int> OnChapterPlayed;

    /// <summary>
    /// Phát khi người chơi vượt qua toàn bộ các wave và hoàn thành Chapter (tham số: chapterNumber, 1-based).
    /// </summary>
    public static event Action<int> OnChapterCleared;

    public static void RaiseEnemyKilled()
    {
        OnEnemyKilled?.Invoke();
    }

    public static void RaiseDroneTierAdvanced()
    {
        OnDroneTierAdvanced?.Invoke();
    }

    public static void RaiseChapterPlayed(int chapterIndex)
    {
        OnChapterPlayed?.Invoke(chapterIndex);
    }

    public static void RaiseChapterCleared(int chapterNumber)
    {
        OnChapterCleared?.Invoke(chapterNumber);
    }
}
