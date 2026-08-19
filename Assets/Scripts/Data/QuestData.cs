using UnityEngine;

/// <summary>
/// ScriptableObject cấu hình nhiệm vụ chính (Quest Widget) hiển thị ở góc trên màn hình Chapter.
/// </summary>
[CreateAssetMenu(fileName = "QuestData_01", menuName = "PGE/Quest Data", order = 20)]
public class QuestData : ScriptableObject
{
    public enum RewardType
    {
        RedGem,
        DataChip,
        Energy
    }

    [Header("Quest Information")]
    [Tooltip("Tiêu đề Quest (mặc định: 'Quest').")]
    public string questTitle = "Quest";

    [Tooltip("Mô tả nhiệm vụ cần thực hiện (ví dụ: 'Upgrade stats at the lab').")]
    public string questDescription = "Upgrade stats at the lab";

    [Header("Reward")]
    [Tooltip("Loại phần thưởng khi hoàn thành nhiệm vụ.")]
    public RewardType rewardType = RewardType.RedGem;

    [Tooltip("Số lượng phần thưởng.")]
    public int rewardAmount = 200;

    [Tooltip("Icon phần thưởng (ngọc đỏ, chip xanh hoặc năng lượng).")]
    public Sprite rewardIcon;

    [Header("Target & Status")]
    [Tooltip("Số lượng mục tiêu cần hoàn thành.")]
    public int targetProgress = 1;

    [Tooltip("ID sự kiện / Key dùng để lưu trạng thái đã nhận thưởng trong PlayerPrefs.")]
    public string questId = "quest_lab_upgrade_01";
}
