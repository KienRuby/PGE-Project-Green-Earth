using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Alias / Wrapper điều khiển Popup Level-Up trong gameplay:
/// Cung cấp 4 thẻ kỹ năng ngẫu nhiên không trùng lặp, hỗ trợ đổi lại (Reroll) tối đa 2 lần với 20 Red Gems,
/// đóng băng thời gian trong lúc chọn và khóa lại khi màn chơi chiến thắng.
/// </summary>
public class LevelUpPopupController : ChipsetLevelUpPopup
{
    // Kế thừa toàn bộ chức năng từ ChipsetLevelUpPopup.
    // Cung cấp API trực tiếp cho các hệ thống và test runners tham chiếu LevelUpPopupController.
}
