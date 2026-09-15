using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hệ thống bản địa hóa (Localization) trung tâm cho Project Green Earth.
/// Cung cấp từ điển song ngữ Tiếng Việt & English hoàn chỉnh cho toàn bộ giao diện game,
/// tự động đồng bộ khi GameSettings.Language thay đổi.
/// </summary>
public static class PGELocalization
{
    public static event Action OnLanguageChanged;

    public static string CurrentLanguage => GameSettings.Language;
    public static bool IsVietnamese => GameSettings.IsVietnamese;

    static PGELocalization()
    {
        Init();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init()
    {
        GameSettings.Changed -= HandleGameSettingsChanged;
        GameSettings.Changed += HandleGameSettingsChanged;
    }

    private static void HandleGameSettingsChanged()
    {
        OnLanguageChanged?.Invoke();
    }

    private static readonly Dictionary<string, (string en, string vi)> StringDictionary = new Dictionary<string, (string en, string vi)>(StringComparer.OrdinalIgnoreCase)
    {
        // Common / Actions
        { "common.confirm", ("Confirm", "Xác nhận") },
        { "common.cancel", ("Cancel", "Hủy bỏ") },
        { "common.close", ("Close", "Đóng") },
        { "common.back", ("Back", "Quay lại") },
        { "common.yes", ("Yes", "Có") },
        { "common.no", ("No", "Không") },
        { "common.start", ("Start", "Bắt đầu") },
        { "common.locked", ("Locked", "Đã khóa") },
        { "common.claim", ("Claim", "Nhận") },
        { "common.get_reward", ("Get Reward", "Nhận thưởng") },
        { "common.copied", ("Copied!", "Đã sao chép!") },
        { "common.copy_id", ("Copy ID", "Sao chép ID") },
        { "common.retry", ("Retry", "Thử lại") },
        { "common.loading", ("Loading...", "Đang tải...") },

        // Settings Panel
        { "settings.title", ("SETTINGS", "CÀI ĐẶT") },
        { "settings.account_prompt", ("Log in and save your data!", "Đăng nhập để lưu dữ liệu của bạn!") },
        { "settings.bgm", ("BGM", "NHẠC NỀN") },
        { "settings.sfx", ("SFX", "HIỆU ỨNG") },
        { "settings.language", ("Language", "Ngôn ngữ") },
        { "settings.select_language", ("SELECT LANGUAGE", "CHỌN NGÔN NGỮ") },
        { "settings.show_damage", ("Show Damage", "Hiện sát thương") },
        { "settings.joystick", ("Dynamic/ Fixed Pad", "Cần điều khiển") },
        { "settings.screen_shake", ("Screen Shake", "Rung màn hình") },
        { "settings.write_review", ("Write a review", "Viết đánh giá") },
        { "settings.close_hint", ("Tap the gear again to close", "Chạm bánh răng lần nữa để đóng") },
        { "settings.local_autosave_on", ("LOCAL AUTO SAVE  <color=#FFC236>ON</color>\n<size=23>Progress is stored on this device</size>", "TỰ ĐỘNG LƯU CỤC BỘ  <color=#FFC236>BẬT</color>\n<size=23>Tiến trình được lưu trên thiết bị này</size>") },
        { "settings.cloud_synced", ("CLOUD SYNCED", "ĐÃ ĐỒNG BỘ CLOUD") },
        { "settings.syncing", ("SYNCING...", "ĐANG ĐỒNG BỘ...") },
        { "settings.saved_locally", ("SAVED LOCALLY", "ĐÃ LƯU CỤC BỘ") },
        { "settings.checking_cloud", ("Checking cloud...", "Đang kiểm tra cloud...") },
        { "settings.google_login", ("LOG IN WITH GOOGLE", "ĐĂNG NHẬP GOOGLE") },
        { "settings.google_logged_in", ("GOOGLE: LOGGED IN", "GOOGLE: ĐÃ ĐĂNG NHẬP") },
        { "settings.apple_login", ("SIGN IN WITH APPLE", "ĐĂNG NHẬP APPLE") },
        { "settings.apple_logged_in", ("APPLE: SIGNED IN", "APPLE: ĐÃ ĐĂNG NHẬP") },
        { "settings.signing_in", ("SIGNING IN...", "ĐANG ĐĂNG NHẬP...") },
        { "settings.state_on", ("ON", "BẬT") },
        { "settings.state_off", ("OFF", "TẮT") },

        // Pause Modal
        { "pause.title", ("PAUSED", "TẠM DỪNG") },
        { "pause.tab_stats", ("STATS", "CHỈ SỐ") },
        { "pause.tab_chipset", ("CHIPSET", "CHIPSET") },
        { "pause.tab_artifact", ("ARTIFACT", "CỔ VẬT") },
        { "pause.sub_def", ("DEF", "PHÒNG THỦ") },
        { "pause.sub_attack", ("ATTACK", "TẤN CÔNG") },
        { "pause.sub_other", ("OTHER", "KHÁC") },
        { "pause.resume", ("RESUME", "TIẾP TỤC") },
        { "pause.home", ("HOME", "TRANG CHỦ") },
        { "pause.level", ("Lv.", "Cấp ") },
        { "pause.equipped", ("Equipped", "Đã trang bị") },
        { "pause.empty_slot", ("Empty Slot", "Ô trống") },

        // Chapter & Gameplay
        { "chapter.select", ("Select Chapter", "Chọn Chương") },
        { "chapter.chapter_prefix", ("CHAPTER", "CHƯƠNG") },
        { "chapter.waves_count", ("{0} / {1} WAVES", "{0} / {1} ĐỢT") },
        { "chapter.stage_progress", ("STAGE PROGRESS {0}%", "TIẾN TRÌNH {0}%") },
        { "chapter.energy_cost", ("Energy: {0}", "Năng lượng: {0}") },

        // Game Over & Victory
        { "gameover.title", ("GAME OVER", "THẤT BẠI") },
        { "victory.title", ("VICTORY", "CHIẾN THẮNG") },
        { "gameover.revive_title", ("REVIVE?", "HỒI SINH?") },
        { "gameover.revive_prompt", ("Continue the fight?", "Tiếp tục chiến đấu?") },
        { "gameover.revive_gem", ("Revive ({0} Gems)", "Hồi sinh ({0} Ngọc)") },
        { "gameover.revive_ad", ("Free Revive (Ad)", "Hồi sinh miễn phí (QC)") },
        { "gameover.give_up", ("Give Up", "Bỏ cuộc") },
        { "gameover.stage_progress", ("STAGE PROGRESS  {0}%", "TIẾN TRÌNH  {0}%") },
        { "gameover.completed_waves", ("{0} / {1} WAVES", "{0} / {1} ĐỢT QUÁI") },
        { "gameover.get_reward", ("Get {0}", "Nhận {0}") },
        { "gameover.details", ("Battle Details", "Chi tiết trận đấu") },

        // Chipset Level Up
        { "chipset.reroll", ("x{0} Draw again", "x{0} Quay lại") },
        { "chipset.choose_upgrade", ("Choose Upgrade", "Chọn Nâng Cấp") },
        { "chipset.level_up", ("LEVEL UP!", "LÊN CẤP!") },

        // Bottom Navigation
        { "nav.shop", ("Shop", "Cửa hàng") },
        { "nav.lab", ("Lab", "Phòng thí nghiệm") },
        { "nav.chapter", ("Battle", "Chiến đấu") },
        { "nav.chipset", ("Chipset", "Chipset") },
        { "nav.buddy", ("Buddy", "Đồng đội") },
        { "nav.artifact", ("Artifact", "Cổ vật") },

        // Currency / TopBar
        { "topbar.energy", ("Energy", "Năng lượng") },
        { "topbar.gold", ("Gold", "Vàng") },
        { "topbar.gem", ("Gems", "Ngọc") },

        // Settings Extensions
        { "settings.guest", ("GUEST", "KHÁCH") },
        { "settings.account", ("Account", "Tài khoản") },
        { "settings.joystick_dynamic", ("Dynamic Pad", "Cần điều khiển động") },
        { "settings.joystick_fixed", ("Fixed Pad", "Cần điều khiển cố định") },
        { "settings.joystick_off", ("Dynamic/Fixed Pad OFF", "Tắt cần điều khiển") },

        // Chipset Detail Modal & Notices ("Ô vuông to")
        { "chipset.not_enough_chips", ("Not enough Data Chips", "Không đủ Chip Dữ Liệu") },
        { "chipset.not_enough_chips_toast", ("Not enough Data Chips to enhance!", "Không đủ Chip Dữ Liệu để nâng cấp!") },
        { "chipset.not_enough_fragments", ("You need to collect more Chipsets.", "Bạn cần thu thập thêm Chipset.") },
        { "chipset.not_enough_fragments_sub", ("You can purchase Chipset Boxes at the\nShop.", "Bạn có thể mua Hộp Chipset tại\nCửa hàng.") },
        { "chipset.not_enough_fragments_toast", ("Not enough fragments to advance tier!", "Không đủ mảnh để đột phá bậc!") },
        { "chipset.mod_badge", ("MOD • UP TO LV{0:00}", "MOD • TỐI ĐA CẤP {0:00}") },
        { "chipset.equip", ("EQUIP", "TRANG BỊ") },
        { "chipset.unequip", ("UNEQUIP", "THÁO") },
        { "chipset.enhance", ("Enhance", "Nâng Cấp") },
        { "chipset.advance_tier", ("Advance Tier ({0}/{1})", "Đột Phá Bậc ({0}/{1})") },
        { "chipset.max_tier", ("MAX TIER", "BẬC TỐI ĐA") },
        { "chipset.active", ("[ACTIVE]", "[KÍCH HOẠT]") },
        { "chipset.unlock", ("{0}Unlock", "Mở khóa {0}") },
        { "chipset.level_prefix", ("LV.{0:00}", "CẤP {0:00}") },

        // Buddy Detail Modal & Notices
        { "buddy.not_enough_chips_toast", ("Not enough Data Chips to enhance!", "Không đủ Chip Dữ Liệu để nâng cấp!") },
        { "buddy.not_enough_fragments_toast", ("Not enough fragments to advance tier!", "Không đủ mảnh để đột phá bậc!") },
        { "buddy.advance_tier", ("Advance Tier ({0}/{1})", "Đột Phá ({0}/{1})") },
        { "buddy.enhance", ("Enhance", "Nâng Cấp") },
        { "buddy.equip", ("EQUIP", "TRANG BỊ") },
        { "buddy.unequip", ("UNEQUIP", "THÁO") },
        { "buddy.max_tier", ("MAX TIER", "BẬC TỐI ĐA") },

        // Shop Items & Overlay
        { "shop.vip_title", ("VIP Package", "Gói VIP") },
        { "shop.ad_free_forever", ("AD FREE FOREVER", "KHÔNG QUẢNG CÁO VĨNH VIỄN") },
        { "shop.special_item", ("Special Item", "Vật Phẩm Đặc Biệt") },
        { "shop.welcome_package", ("Welcome Package", "Gói Chào Mừng") },
        { "shop.purchasable_once", ("Purchasable 1 time", "Chỉ mua được 1 lần") },
        { "shop.value_5x", ("More than 5x Value", "Giá trị hơn 5x") },
        { "shop.daily_shop", ("Daily Shop", "Cửa Hàng Hàng Ngày") },
        { "shop.box", ("Box", "Rương Vật Phẩm") },
        { "shop.meta_shop", ("Meta Shop", "Cửa Hàng Nâng Cao") },
        { "shop.gem_event", ("Gem Event", "Sự Kiện Ngọc") },
        { "shop.daily_shop_ready", ("DAILY SHOP READY", "CỬA HÀNG ĐÃ SẴN SÀNG") },
        { "shop.not_enough_gems", ("NOT ENOUGH RED GEMS", "KHÔNG ĐỦ NGỌC") },
        { "shop.item_piece_limit", ("ITEM PIECE LIMIT REACHED", "ĐÃ ĐẠT GIỚI HẠN MẢNH VẬT PHẨM") },
        { "shop.package_limit", ("PACKAGE INVENTORY LIMIT REACHED", "ĐÃ ĐẠT GIỚI HẠN TÚI ĐỒ") },
        { "shop.transaction_failed", ("TRANSACTION FAILED", "GIAO DỊCH THẤT BẠI") },
        { "shop.gem_label", ("Gem", "Ngọc") },
        { "shop.data_chip_label", ("Data Chip", "Chip Dữ Liệu") },
        { "shop.standard_gun_label", ("Standard Gun", "Súng Tiêu Chuẩn") },
        { "shop.rocket_punch_label", ("Rocket Punch", "Cú Đấm Tên Lửa") },
        { "shop.claimed", ("CLAIMED", "ĐÃ NHẬN") },
        { "shop.free", ("FREE", "MIỄN PHÍ") },
        { "shop.intermediate_package", ("Intermediate Pack", "Gói Trung Cấp") },
        { "shop.advanced_package", ("Advanced Pack", "Gói Cao Cấp") },
        { "shop.gun_pack", ("Gun Pack", "Gói Vũ Khí") },
        { "shop.drone_pack", ("Drone Pack", "Gói Drone") }
    };

    private static readonly Dictionary<string, string> ChipNameVi = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Standard Gun", "Súng Tiêu Chuẩn" },
        { "Rifle", "Súng Trường" },
        { "Rocket Punch", "Nắm Đấm Tên Lửa" },
        { "Spinning Blade", "Lưỡi Dao Xoay" },
        { "Multigun", "Súng Đa Nòng" },
        { "Shotgun", "Súng Hoa Cải" },
        { "Laser", "Tia Laser" },
        { "Laser Beam", "Tia Laser" },
        { "Force Field", "Trường Lực" },
        { "Drones", "Drone Hỗ Trợ" },
        { "Guardian Drone", "Drone Hộ Vệ" },
        { "Attack Drone", "Drone Tấn Công" },
        { "Laser Drone", "Drone Laser" },
        { "Missile Drone", "Drone Tên Lửa" },
        { "Repair Drone", "Drone Hồi Phục" },
        { "Speed Drone", "Drone Tốc Độ" },
        { "Data Chip", "Chip Dữ Liệu" },
        { "Gem", "Ngọc" },
        { "Red Gem", "Ngọc Đỏ" }
    };

    /// <summary>
    /// Lấy tên Chipset / Drone theo ngôn ngữ hiện tại.
    /// </summary>
    public static string GetChipName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return string.Empty;
        if (!IsVietnamese) return rawName;
        return ChipNameVi.TryGetValue(rawName, out var viName) ? viName : rawName;
    }

    /// <summary>
    /// Dịch nội dung thuộc tính Perk sang Tiếng Việt nếu đang ở ngôn ngữ Tiếng Việt.
    /// </summary>
    public static string GetPerkText(string rawPerk)
    {
        if (string.IsNullOrEmpty(rawPerk) || !IsVietnamese) return rawPerk;

        return rawPerk
            .Replace("ATK Speed", "Tốc độ bắn")
            .Replace("ATK", "Tấn công")
            .Replace("Life Steal", "Hút máu")
            .Replace("chance to ricochet again", "tỉ lệ nảy đạn lần nữa")
            .Replace("Spin Speed", "Tốc độ quay")
            .Replace("AoE ATK Range", "Phạm vi nổ")
            .Replace("AoE ATK", "Sát thương lan")
            .Replace("Generation speed", "Thời gian hồi")
            .Replace("Guaranteed Pierce", "Chắc chắn xuyên thấu")
            .Replace("shells", "viên")
            .Replace("Fast", "Nhanh")
            .Replace("Slow", "Chậm")
            .Replace("Normal", "Bình thường")
            .Replace("Always equipped, even when it is not included in your deck.", "Luôn được trang bị, kể cả khi không nằm trong bộ bài.")
            .Replace("Fires a rapid burst of bullets.", "Bắn ra loạt đạn chùm với tốc độ cao.")
            .Replace("Launches a rocket-powered fist that deals area damage.", "Phóng nắm đấm tên lửa gây sát thương diện rộng.")
            .Replace("Throws a spinning blade that pierces enemies and returns to the player.", "Ném lưỡi dao xoay xuyên qua kẻ địch và quay lại.")
            .Replace("Fires a rain of bullets in multiple directions at once.", "Bắn mưa đạn theo nhiều hướng cùng lúc.");
    }

    /// <summary>
    /// Dịch thông báo và toast của Cửa hàng (Shop) sang Tiếng Việt nếu đang ở ngôn ngữ Tiếng Việt.
    /// </summary>
    public static string GetShopMessage(string raw)
    {
        if (string.IsNullOrEmpty(raw) || !IsVietnamese) return raw;

        return raw
            .Replace("DAILY SHOP READY", "CỬA HÀNG ĐÃ SẴN SÀNG")
            .Replace("NOT ENOUGH RED GEMS", "KHÔNG ĐỦ NGỌC")
            .Replace("ITEM PIECE LIMIT REACHED", "ĐÃ ĐẠT GIỚI HẠN MẢNH VẬT PHẨM")
            .Replace("PACKAGE INVENTORY LIMIT REACHED", "ĐÃ ĐẠT GIỚI HẠN TÚI ĐỒ")
            .Replace("TRANSACTION FAILED", "GIAO DỊCH THẤT BẠI")
            .Replace("CLAIMED", "ĐÃ NHẬN")
            .Replace("FREE", "MIỄN PHÍ")
            .Replace("VIP UNLOCKED", "ĐÃ MỞ KHÓA VIP")
            .Replace("GEMS RECEIVED", "NGỌC ĐÃ NHẬN")
            .Replace("RECEIVED", "NHẬN ĐƯỢC")
            .Replace("RED GEMS", "NGỌC")
            .Replace("GEMS", "NGỌC")
            .Replace("DATA CHIPS", "CHIP DỮ LIỆU")
            .Replace("RESTORED", "HỒI PHỤC")
            .Replace("ENERGY", "NĂNG LƯỢNG")
            .Replace("OPENED", "ĐÃ MỞ")
            .Replace("CHIPSET BOXES", "RƯƠNG CHIPSET")
            .Replace("DRONE BOXES", "RƯƠNG DRONE")
            .Replace("TOTAL", "TỔNG CỘNG")
            .Replace("BOX OPENED", "ĐÃ MỞ RƯƠNG")
            .Replace("PIECES ADDED", "MẢNH ĐƯỢC THÊM")
            .Replace("STANDARD GUN", "SÚNG TIÊU CHUẨN")
            .Replace("ROCKET PUNCH", "NẮM ĐẤM TÊN LỬA")
            .Replace("GUN TURRET", "TRỤ SÚNG")
            .Replace("SHOTGUN", "SÚNG HOA CẢI")
            .Replace("HIGH-EXPLOSIVE MINE", "MÌN NỔ CAO")
            .Replace("SPIKY DISCUS", "ĐĨA GAI")
            .Replace("RIFLE", "SÚNG TRƯỜNG");
    }

    /// <summary>
    /// Lấy chuỗi đã bản địa hóa theo key. Nếu không tìm thấy, trả về fallback hoặc chính key.
    /// </summary>
    public static string Get(string key, string fallback = null)
    {
        if (string.IsNullOrEmpty(key)) return fallback ?? string.Empty;

        if (StringDictionary.TryGetValue(key, out var pair))
        {
            return IsVietnamese ? pair.vi : pair.en;
        }

        return fallback ?? key;
    }

    /// <summary>
    /// Lấy chuỗi đã bản địa hóa và định dạng các tham số args.
    /// </summary>
    public static string GetFormat(string key, params object[] args)
    {
        string raw = Get(key);
        if (args == null || args.Length == 0) return raw;
        try
        {
            return string.Format(raw, args);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PGELocalization] Format error for key '{key}': {ex.Message}");
            return raw;
        }
    }

    /// <summary>
    /// Cho phép đăng ký bổ sung các cặp từ khóa bản địa hóa tùy biến từ bên ngoài.
    /// </summary>
    public static void Register(string key, string english, string vietnamese)
    {
        if (string.IsNullOrEmpty(key)) return;
        StringDictionary[key] = (english, vietnamese);
    }
}
