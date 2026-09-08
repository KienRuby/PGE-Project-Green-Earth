using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGameplayEvent", menuName = "PGE/Gameplay Event Data")]
public class GameplayEventData : ScriptableObject
{
    [Tooltip("ID duy nhất của sự kiện (VD: 'electric_car', 'underground_bunker', 'assasinator').")]
    public string eventId;

    [Tooltip("Tiêu đề của sự kiện hiển thị trên đầu Modal (VD: 'Electric Car', 'Underground Bunker', 'Assasinator').")]
    public string eventTitle;

    [Tooltip("Hình ảnh pixel CRT scanline minh họa cho sự kiện.")]
    public Sprite illustrationSprite;

    [TextArea(3, 6)]
    [Tooltip("Nội dung dẫn truyện/lời thoại ban đầu trước khi chọn.")]
    public string introDialogueText;

    [Tooltip("Nếu true: Bỏ qua bước chọn nút, hiển thị ngay kết quả và nút Leave (như màn Electric Car trong ảnh 1).")]
    public bool isInstantResult = false;

    [Tooltip("Dòng kết quả thưởng tức thì (nếu isInstantResult = true).")]
    public string instantRewardText = "";

    [Tooltip("Loại thưởng tức thì (nếu isInstantResult = true).")]
    public EventRewardType instantRewardType = EventRewardType.None;

    [Tooltip("Giá trị thưởng tức thì.")]
    public float instantRewardValue = 0f;

    [Tooltip("Item ID thưởng tức thì.")]
    public string instantRewardItemId = "";

    [Tooltip("Danh sách các lựa chọn tương tác (1 đến 3 lựa chọn).")]
    public List<GameplayEventOption> options = new List<GameplayEventOption>();
}
