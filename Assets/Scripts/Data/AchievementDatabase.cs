using System;
using System.Collections.Generic;
using UnityEngine;

public enum AchievementType
{
    [Tooltip("Số lần nhận thưởng đăng nhập hàng ngày (Daily Login Reward).")]
    LoginRewardClaimed,

    [Tooltip("Số lần nâng cấp tăng bậc (Tier) cho Drone / Buddy.")]
    DroneTierAdvanced,

    [Tooltip("Số lần vào chơi các Chapter.")]
    ChapterPlayed,

    [Tooltip("Số lượng quái vật / boss đã tiêu diệt.")]
    EnemyKilled,

    [Tooltip("Số Chapter đã vượt qua thành công.")]
    ChapterCleared
}

[Serializable]
public class AchievementDefinition
{
    [Tooltip("ID duy nhất cố định của Achievement (ví dụ: 'login_reward_2', 'enemy_kill_2500'). Không dùng Title làm ID.")]
    public string id;

    [Tooltip("Tiêu đề nhiệm vụ hiển thị trên UI (ví dụ: 'Kill 2500 enemies').")]
    public string title;

    [Tooltip("Loại sự kiện gameplay tương ứng.")]
    public AchievementType type;

    [Tooltip("Chỉ số mục tiêu cần đạt (ví dụ: 2500).")]
    [Min(1)]
    public int targetValue = 1;

    [Tooltip("Danh sách phần thưởng nhận được khi hoàn thành.")]
    public RewardData[] rewards;

    [Tooltip("Thứ tự hiển thị ưu tiên.")]
    public int sortPriority = 0;

    public AchievementDefinition() { }

    public AchievementDefinition(string id, string title, AchievementType type, int targetValue, params RewardData[] rewards)
    {
        this.id = id;
        this.title = title;
        this.type = type;
        this.targetValue = targetValue;
        this.rewards = rewards;
    }
}

/// <summary>
/// ScriptableObject cấu hình danh sách toàn bộ Achievements trong Project Green Earth.
/// Data-driven, cho phép dễ dàng thêm bớt nhiệm vụ mà không cần sửa code.
/// </summary>
[CreateAssetMenu(fileName = "AchievementDatabase", menuName = "PGE/Achievement Database", order = 11)]
public class AchievementDatabase : ScriptableObject
{
    [SerializeField]
    private List<AchievementDefinition> achievements = new List<AchievementDefinition>();

    public IReadOnlyList<AchievementDefinition> Achievements => achievements;

    public AchievementDefinition GetAchievement(string id)
    {
        if (achievements == null || string.IsNullOrWhiteSpace(id)) return null;
        return achievements.Find(a => a != null && string.Equals(a.id, id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Khởi tạo 5 thành tựu chuẩn xác theo đúng Visual Reference (Image 1).
    /// </summary>
    public void PopulateDefaultAchievements()
    {
        achievements = new List<AchievementDefinition>
        {
            // 1. Get 2 times log in reward (2/2) -> RedGem x200
            new AchievementDefinition(
                "login_reward_2",
                "Get 2 times log in reward",
                AchievementType.LoginRewardClaimed,
                2,
                new RewardData(RewardType.RedGem, 200)
            ) { sortPriority = 10 },

            // 2. Advance Drone Tier 3 time(s) (19/3) -> RedGem x200, DataChip x1000
            new AchievementDefinition(
                "drone_upgrade_3",
                "Advance Drone Tier 3 time(s)",
                AchievementType.DroneTierAdvanced,
                3,
                new RewardData(RewardType.RedGem, 200),
                new RewardData(RewardType.DataChip, 1000)
            ) { sortPriority = 20 },

            // 3. Play chapter 15 time(s) (14/15) -> RedGem x200, DataChip x800
            new AchievementDefinition(
                "chapter_play_15",
                "Play chapter 15 time(s)",
                AchievementType.ChapterPlayed,
                15,
                new RewardData(RewardType.RedGem, 200),
                new RewardData(RewardType.DataChip, 800)
            ) { sortPriority = 30 },

            // 4. Kill 2500 enemies (2025/2500) -> RedGem x200, DataChip x1200
            new AchievementDefinition(
                "enemy_kill_2500",
                "Kill 2500 enemies",
                AchievementType.EnemyKilled,
                2500,
                new RewardData(RewardType.RedGem, 200),
                new RewardData(RewardType.DataChip, 1200)
            ) { sortPriority = 40 },

            // 5. Clear chapter 5 (4/5) -> RedGem x200, DataChip x2000, Energy x10
            new AchievementDefinition(
                "chapter_clear_5",
                "Clear chapter 5",
                AchievementType.ChapterCleared,
                5,
                new RewardData(RewardType.RedGem, 200),
                new RewardData(RewardType.DataChip, 2000),
                new RewardData(RewardType.Energy, 10)
            ) { sortPriority = 50 }
        };
    }

    private void Reset()
    {
        PopulateDefaultAchievements();
    }
}
