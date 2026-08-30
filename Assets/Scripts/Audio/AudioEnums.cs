using System;

/// <summary>
/// Danh mục phân loại âm thanh trong game.
/// Cho phép điều chỉnh âm lượng và mute độc lập từng nhóm.
/// </summary>
public enum AudioCategory
{
    Master,
    BGM,
    SFX,
    VFX,
    UI,
    Ambient
}

/// <summary>
/// Chế độ làm mờ (Fade) âm thanh khi chuyển bài hoặc ngắt tiếng.
/// </summary>
public enum AudioFadeMode
{
    Linear,
    SmoothStep,
    Exponential
}

/// <summary>
/// Các hằng số định danh âm thanh chuẩn dùng chung trong dự án.
/// </summary>
public static class SoundIdConst
{
    // BGM
    public const string BGM_MAIN_MENU = "BGM_MainMenu";
    public const string BGM_COMBAT = "BGM_Combat";
    public const string BGM_BOSS = "BGM_Boss";
    public const string BGM_VICTORY = "BGM_Victory";
    public const string BGM_DEFEAT = "BGM_Defeat";

    // SFX Gameplay
    public const string SFX_GUN_SHOT_STANDARD = "SFX_GunShot_Standard";
    public const string SFX_GUN_SHOT_SHOTGUN = "SFX_GunShot_Shotgun";
    public const string SFX_GUN_SHOT_RIFLE = "SFX_GunShot_Rifle";
    public const string SFX_EXPLOSION_SMALL = "SFX_Explosion_Small";
    public const string SFX_EXPLOSION_LARGE = "SFX_Explosion_Large";
    public const string SFX_BULLET_HIT = "SFX_Bullet_Hit";
    public const string SFX_PLAYER_HURT = "SFX_Player_Hurt";
    public const string SFX_PLAYER_DEATH = "SFX_Player_Death";
    public const string SFX_PLAYER_DASH = "SFX_Player_Dash";
    public const string SFX_ENEMY_HURT = "SFX_Enemy_Hurt";
    public const string SFX_ENEMY_DEATH = "SFX_Enemy_Death";
    public const string SFX_BOSS_ROAR = "SFX_Boss_Roar";
    public const string SFX_PICKUP_EXP = "SFX_Pickup_EXP";
    public const string SFX_PICKUP_ITEM = "SFX_Pickup_Item";
    public const string SFX_LEVEL_UP = "SFX_Level_Up";

    // VFX Audio
    public const string VFX_LASER_BEAM = "VFX_Laser_Beam";
    public const string VFX_FIRE_BURST = "VFX_Fire_Burst";
    public const string VFX_ICE_SHATTER = "VFX_Ice_Shatter";
    public const string VFX_LIGHTNING_STRIKE = "VFX_Lightning_Strike";
    public const string VFX_SHIELD_ACTIVATE = "VFX_Shield_Activate";
    public const string VFX_SHIELD_BREAK = "VFX_Shield_Break";
    public const string VFX_CHARGE_UP = "VFX_Charge_Up";

    // UI Sound
    public const string UI_BUTTON_CLICK = "UI_ButtonClick";
    public const string UI_TAB_SWITCH = "UI_TabSwitch";
    public const string UI_MODAL_OPEN = "UI_ModalOpen";
    public const string UI_MODAL_CLOSE = "UI_ModalClose";
    public const string UI_REWARD_CLAIM = "UI_RewardClaim";
    public const string UI_UPGRADE_SUCCESS = "UI_UpgradeSuccess";
    public const string UI_ERROR_BUZZ = "UI_ErrorBuzz";
}
