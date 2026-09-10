using System;
using UnityEngine;

public enum EventRewardType
{
    None,
    StatBuff_MoveSpeedPercent,  // VD: Move Speed +5%
    Health_ConsumePercent,      // VD: HP -5%
    Health_HealPercent,         // VD: HP +10%
    Chipset_LevelUp,            // VD: [Big Battery] Level +1
    Artifact_Grant,             // VD: You've obtained Metal Band-aid
    ArtifactBox_Open,           // VD: He finds an Artifact Box
}

[Serializable]
public class GameplayEventOption
{
    [Tooltip("Nhãn hiển thị trên nút bấm (VD: 'To purge mutants', 'Go to the room on the left', 'Leave.').")]
    public string buttonText;

    [TextArea(2, 4)]
    [Tooltip("Đoạn dẫn truyện sau khi người chơi bấm vào lựa chọn này.")]
    public string resultStoryText;

    [Tooltip("Dòng chữ thưởng/thay đổi màu vàng kim (VD: 'Move Speed +5%', 'HP -5%\\n[Big Battery] Level +1').")]
    public string rewardText;

    [Tooltip("Loại hiệu ứng / phần thưởng.")]
    public EventRewardType rewardType = EventRewardType.None;

    [Tooltip("Giá trị số học (VD: 5 cho 5% tốc độ, hoặc 5 cho 5% HP).")]
    public float rewardValue = 0f;

    [Tooltip("ID của item/chipset tương ứng (VD: 'metal_band_aid', 'big-battery').")]
    public string rewardItemId = "";

    public GameplayEventOption() { }

    public GameplayEventOption(string buttonText, string resultStoryText, string rewardText, EventRewardType rewardType = EventRewardType.None, float rewardValue = 0f, string rewardItemId = "")
    {
        this.buttonText = buttonText;
        this.resultStoryText = resultStoryText;
        this.rewardText = rewardText;
        this.rewardType = rewardType;
        this.rewardValue = rewardValue;
        this.rewardItemId = rewardItemId;
    }
}
