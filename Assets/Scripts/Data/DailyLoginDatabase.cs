using System;
using System.Collections.Generic;
using UnityEngine;

public enum StreakResetMode
{
    [Tooltip("Nếu người chơi bỏ lỡ 1 ngày không đăng nhập, streak sẽ reset về Day 1.")]
    ResetToDay1OnMissedDay,

    [Tooltip("Không bao giờ reset streak. Người chơi đăng nhập ngày kế tiếp sẽ tiếp tục chuỗi ngày hiện tại (ví dụ: Day 3 -> Day 4).")]
    ContinuousKeepStreak
}

public enum Day7LoopMode
{
    [Tooltip("Sau khi nhận xong Day 7, ngày tiếp theo sẽ tự động xoay vòng về Day 1.")]
    LoopToDay1,

    [Tooltip("Sau khi nhận xong Day 7, giữ trạng thái chờ đến khi bắt đầu một chu kỳ sự kiện mới.")]
    WaitForNextCycle
}

[Serializable]
public class DailyLoginDayData
{
    [Tooltip("Số thứ tự ngày (1 đến 7).")]
    [Range(1, 7)]
    public int dayIndex = 1;

    [Tooltip("Danh sách các phần thưởng trong ngày này (Energy, RedGems, DataChips...).")]
    public RewardData[] rewards;

    public DailyLoginDayData() { }

    public DailyLoginDayData(int day, params RewardData[] rewards)
    {
        this.dayIndex = day;
        this.rewards = rewards;
    }
}

/// <summary>
/// ScriptableObject cấu hình toàn bộ dữ liệu Daily Login Reward 7 ngày.
/// Cho phép designer tùy biến phần thưởng, chế độ streak và hành vi lặp lại trong Inspector.
/// </summary>
[CreateAssetMenu(fileName = "DailyLoginDatabase", menuName = "PGE/Daily Login Database", order = 10)]
public class DailyLoginDatabase : ScriptableObject
{
    [Header("Cycle Configuration")]
    [Tooltip("Chế độ xử lý khi người chơi bỏ lỡ 1 ngày đăng nhập.")]
    public StreakResetMode streakMode = StreakResetMode.ContinuousKeepStreak;

    [Tooltip("Hành vi sau khi đã nhận toàn bộ 7 ngày.")]
    public Day7LoopMode day7Mode = Day7LoopMode.LoopToDay1;

    [Tooltip("Giờ reset trong ngày theo giờ chuẩn quốc tế UTC (0 = 00:00 UTC = 07:00 AM VN).")]
    [Range(0, 23)]
    public int resetHourUtc = 0;

    [Header("7 Days Reward Data")]
    [SerializeField]
    private List<DailyLoginDayData> days = new List<DailyLoginDayData>();

    public IReadOnlyList<DailyLoginDayData> Days => days;

    public DailyLoginDayData GetDayData(int dayIndex)
    {
        if (days == null || days.Count == 0) return null;
        return days.Find(d => d != null && d.dayIndex == dayIndex)
            ?? (dayIndex >= 1 && dayIndex <= days.Count ? days[dayIndex - 1] : null);
    }

    /// <summary>
    /// Khởi tạo cấu hình 7 ngày chuẩn xác theo đúng ảnh tham chiếu Visual Reference (Image 2).
    /// </summary>
    public void PopulateDefault7Days()
    {
        days = new List<DailyLoginDayData>
        {
            // DAY 01: Energy x30, RedGem x300, DataChip x1000
            new DailyLoginDayData(1,
                new RewardData(RewardType.Energy, 30),
                new RewardData(RewardType.RedGem, 300),
                new RewardData(RewardType.DataChip, 1000)),

            // DAY 02: Energy x30, RedGem x300, DataChip x1000
            new DailyLoginDayData(2,
                new RewardData(RewardType.Energy, 30),
                new RewardData(RewardType.RedGem, 300),
                new RewardData(RewardType.DataChip, 1000)),

            // DAY 03: Energy x60, RedGem x500, DataChip x3000
            new DailyLoginDayData(3,
                new RewardData(RewardType.Energy, 60),
                new RewardData(RewardType.RedGem, 500),
                new RewardData(RewardType.DataChip, 3000)),

            // DAY 04: Energy x30, RedGem x300, DataChip x1000
            new DailyLoginDayData(4,
                new RewardData(RewardType.Energy, 30),
                new RewardData(RewardType.RedGem, 300),
                new RewardData(RewardType.DataChip, 1000)),

            // DAY 05: Energy x30, RedGem x300, DataChip x1000
            new DailyLoginDayData(5,
                new RewardData(RewardType.Energy, 30),
                new RewardData(RewardType.RedGem, 300),
                new RewardData(RewardType.DataChip, 1000)),

            // DAY 06: Energy x60, RedGem x500, DataChip x3000
            new DailyLoginDayData(6,
                new RewardData(RewardType.Energy, 60),
                new RewardData(RewardType.RedGem, 500),
                new RewardData(RewardType.DataChip, 3000)),

            // DAY 07: Energy x90, RedGem x500, DataChip x7000
            new DailyLoginDayData(7,
                new RewardData(RewardType.Energy, 90),
                new RewardData(RewardType.RedGem, 500),
                new RewardData(RewardType.DataChip, 7000))
        };
    }

    private void Reset()
    {
        PopulateDefault7Days();
    }
}
