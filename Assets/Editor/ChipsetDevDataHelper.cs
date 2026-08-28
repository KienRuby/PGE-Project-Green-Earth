#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ChipsetDevDataHelper
{
    [MenuItem("PGE/Account Data/Reset to Fresh Account (All Chips LV.1 Magic, 0 Cards)")]
    public static void ResetToFreshAccount()
    {
        // 1. Xóa sạch dữ liệu lưu cũ của Chipset trong PlayerPrefs
        for (int id = 1; id <= 30; id++)
        {
            string pfx = PlayerDataService.GetChipItemPrefix(id);
            PlayerPrefs.DeleteKey($"{pfx}Level");
            PlayerPrefs.DeleteKey($"{pfx}Tier");
            PlayerPrefs.DeleteKey($"{pfx}Count");
            PlayerPrefs.DeleteKey($"{pfx}ReqCount");
            PlayerPrefs.DeleteKey($"{pfx}HasStar");
            PlayerPrefs.DeleteKey($"{pfx}TierEnhanceCount");
            PlayerPrefs.DeleteKey($"{pfx}EnhanceCost");
        }

        // Lưu Standard Gun khởi đầu ở Level 1, Tier Magic, 3 mảnh thẻ (sẵn sàng trang bị)
        PlayerDataService.SaveChipsetItemData(1, 1, 1, 3, 3, false);

        // Đưa các chip còn lại (2 đến 24) về Level 1, Tier Magic, 0 mảnh thẻ
        for (int id = 2; id <= 24; id++)
        {
            PlayerDataService.SaveChipsetItemData(id, 1, 1, 0, 3, false);
        }

        PlayerPrefs.Save();
        Debug.Log("[ChipsetDevDataHelper] ✅ Đã thiết lập TÀI KHOẢN MỚI HOÀN TOÀN: Toàn bộ 15+ Chipset đều ở CẤP 1 (Khung Xanh Lá Magic, 0 mảnh thẻ)!");
    }

    [MenuItem("PGE/Account Data/Apply Dev Test Profile (High Levels & Holo/Epic/Unique)")]
    public static void ApplyDevTestProfile()
    {
        // Thiết lập bộ profile test theo thiết kế:
        // 1. Standard Gun (LV.18 Epic)
        PlayerDataService.SaveChipsetItemData(1, 18, (int)ChipTier.Epic, 451, 15, false);

        // 2. Rifle (LV.24 Holo)
        PlayerDataService.SaveChipsetItemData(2, 24, (int)ChipTier.Holographic, 449, 0, false);

        // 3. Rocket Punch (LV.06 Rare)
        PlayerDataService.SaveChipsetItemData(3, 6, (int)ChipTier.Rare, 470, 7, false);

        // 4. Spinning Blade (LV.14 Unique)
        PlayerDataService.SaveChipsetItemData(4, 14, (int)ChipTier.Unique, 468, 9, false);

        // 5. Multigun (LV.06 Rare)
        PlayerDataService.SaveChipsetItemData(5, 6, (int)ChipTier.Rare, 423, 3, false);

        // 6. Gun Turret (LV.01 Magic)
        PlayerDataService.SaveChipsetItemData(6, 1, (int)ChipTier.Magic, 501, 3, false);

        // 7. Spiky Discus (LV.01 Magic)
        PlayerDataService.SaveChipsetItemData(7, 1, (int)ChipTier.Magic, 479, 3, false);

        // 8. Shotgun (LV.09 Rare)
        PlayerDataService.SaveChipsetItemData(8, 9, (int)ChipTier.Rare, 450, 7, false);

        // 9. Energy Jumper Cables (LV.01 Magic Star)
        PlayerDataService.SaveChipsetItemData(9, 1, (int)ChipTier.Magic, 391, 3, true);

        // 10. High-Explosive Mine (LV.01 Magic)
        PlayerDataService.SaveChipsetItemData(10, 1, (int)ChipTier.Magic, 390, 3, false);

        // 11. Sonic Boom
        PlayerDataService.SaveChipsetItemData(11, 1, (int)ChipTier.Magic, 513, 3, false);

        // 12. Healing Turret
        PlayerDataService.SaveChipsetItemData(12, 1, (int)ChipTier.Magic, 502, 3, false);

        // 13. Aiming Lens
        PlayerDataService.SaveChipsetItemData(13, 1, (int)ChipTier.Magic, 498, 3, true);

        // 14. Ice Turret
        PlayerDataService.SaveChipsetItemData(14, 1, (int)ChipTier.Magic, 494, 3, false);

        // 15. Flamethrower (LV.18 Epic)
        PlayerDataService.SaveChipsetItemData(15, 18, (int)ChipTier.Epic, 486, 15, false);

        PlayerPrefs.Save();
        Debug.Log("[ChipsetDevDataHelper] ✅ Đã nạp thành công PROFILE TEST DEV: Rifle LV.24 Đỏ, Standard LV.18 Vàng, Blade LV.14 Tím, Shotgun LV.09 Xanh, v.v.!");
    }
}
#endif
