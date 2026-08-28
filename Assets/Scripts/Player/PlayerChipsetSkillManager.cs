using UnityEngine;

/// <summary>
/// Quản lý việc tiếp nhận các lựa chọn Chipset khi người chơi Lên Cấp (Level Up)
/// và kích hoạt/nâng cấp các kỹ năng tương ứng trên Player (ví dụ: Gun Turret, v.v.).
/// </summary>
[RequireComponent(typeof(GunTurretSkill))]
public class PlayerChipsetSkillManager : MonoBehaviour
{
    [Header("Skill References")]
    [SerializeField] private PlayerAutoShooter playerAutoShooter;
    [SerializeField] private HighExplosiveMineSkill highExplosiveMineSkill;
    [SerializeField] private EnergyJumperCablesSkill energyJumperCablesSkill;
    [SerializeField] private SpikyDiscusSkill spikyDiscusSkill;
    [SerializeField] private GunTurretSkill gunTurretSkill;
    [SerializeField] private RocketPunchSkill rocketPunchSkill;
    [SerializeField] private SpinningBladeSkill spinningBladeSkill;

    private void Awake()
    {
        ChipsetBattleStats.Reset();
        if (playerAutoShooter == null) playerAutoShooter = GetComponent<PlayerAutoShooter>();

        // Bốn chipset dạng súng dùng chung khẩu súng mặc định. Nếu scene cũ còn
        // component tự bắn riêng thì tắt chúng để không tạo đạn/VFX tại tâm Player.
        DisableLegacyGunSkill(GetComponent<StandardGunSkill>());
        DisableLegacyGunSkill(GetComponent<RifleSkill>());
        DisableLegacyGunSkill(GetComponent<ShotgunSkill>());
        DisableLegacyGunSkill(GetComponent<MultigunSkill>());

        if (highExplosiveMineSkill == null)
        {
            highExplosiveMineSkill = GetComponent<HighExplosiveMineSkill>();
            if (highExplosiveMineSkill == null)
            {
                highExplosiveMineSkill = gameObject.AddComponent<HighExplosiveMineSkill>();
            }
        }

        if (energyJumperCablesSkill == null)
        {
            energyJumperCablesSkill = GetComponent<EnergyJumperCablesSkill>();
            if (energyJumperCablesSkill == null)
            {
                energyJumperCablesSkill = gameObject.AddComponent<EnergyJumperCablesSkill>();
            }
        }

        if (spikyDiscusSkill == null)
        {
            spikyDiscusSkill = GetComponent<SpikyDiscusSkill>();
            if (spikyDiscusSkill == null)
            {
                spikyDiscusSkill = gameObject.AddComponent<SpikyDiscusSkill>();
            }
        }

        if (gunTurretSkill == null)
        {
            gunTurretSkill = GetComponent<GunTurretSkill>();
            if (gunTurretSkill == null)
            {
                gunTurretSkill = gameObject.AddComponent<GunTurretSkill>();
            }
        }

        if (rocketPunchSkill == null)
        {
            rocketPunchSkill = GetComponent<RocketPunchSkill>();
            if (rocketPunchSkill == null)
            {
                rocketPunchSkill = gameObject.AddComponent<RocketPunchSkill>();
            }
        }

        if (spinningBladeSkill == null)
        {
            spinningBladeSkill = GetComponent<SpinningBladeSkill>();
            if (spinningBladeSkill == null)
            {
                spinningBladeSkill = gameObject.AddComponent<SpinningBladeSkill>();
            }
        }
    }

    private static void DisableLegacyGunSkill(Behaviour skill)
    {
        if (skill != null) skill.enabled = false;
    }

    private void OnEnable()
    {
        ChipsetLevelUpPopup.OnRuntimeChipsetSelected += HandleChipsetSelected;
    }

    private void OnDisable()
    {
        ChipsetLevelUpPopup.OnRuntimeChipsetSelected -= HandleChipsetSelected;
    }

    private void HandleChipsetSelected(ChipItemData selectedChip, int runtimeLevel)
    {
        if (selectedChip == null) return;

        string name = selectedChip.chipName ?? string.Empty;
        string key = selectedChip.iconKey ?? string.Empty;
        int id = selectedChip.id;

        // 1. Kiểm tra nếu là Standard Gun (ID 1 hoặc key "standard-gun" / "Standard Gun")
        if (id == 1 || key.Contains("standard") || name.IndexOf("Standard", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Tiêu Chuẩn", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (playerAutoShooter != null)
            {
                playerAutoShooter.ApplyChipsetWeaponUpgrade(1, runtimeLevel);
                Debug.Log($"[PlayerChipsetSkillManager] Standard Gun cấp {runtimeLevel} đã tăng trực tiếp khẩu súng mặc định.");
            }
            return;
        }

        // 2. Kiểm tra nếu là Rifle (ID 2 hoặc key "rifle" / "Súng Trường")
        if (id == 2 || key.Contains("rifle") || name.IndexOf("Rifle", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Trường", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (playerAutoShooter != null)
            {
                playerAutoShooter.ApplyChipsetWeaponUpgrade(2, runtimeLevel);
                Debug.Log($"[PlayerChipsetSkillManager] Rifle cấp {runtimeLevel} đã tăng trực tiếp khẩu súng mặc định.");
            }
            return;
        }

        // 3. Kiểm tra nếu là Shotgun (ID 8 hoặc key "shotgun" / "Súng Săn")
        if (id == 8 || key.Contains("shotgun") || name.IndexOf("Shotgun", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Súng Săn", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (playerAutoShooter != null)
            {
                playerAutoShooter.ApplyChipsetWeaponUpgrade(8, runtimeLevel);
                Debug.Log($"[PlayerChipsetSkillManager] Shotgun cấp {runtimeLevel} đã tăng trực tiếp khẩu súng mặc định.");
            }
            return;
        }

        // 4. Kiểm tra nếu là Multigun (ID 5 hoặc key "multigun" / "Súng Đa Tia")
        if (id == 5 || key.Contains("multigun") || name.IndexOf("Multigun", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Đa Tia", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (playerAutoShooter != null)
            {
                playerAutoShooter.ApplyChipsetWeaponUpgrade(5, runtimeLevel);
                Debug.Log($"[PlayerChipsetSkillManager] Multigun cấp {runtimeLevel} đã tăng trực tiếp khẩu súng mặc định.");
            }
            return;
        }

        // 5. Kiểm tra nếu là High-Explosive Mine (ID 10 hoặc key "high-explosive-mine" / "Mìn Nổ")
        if (id == 10 || key.Contains("mine") || name.IndexOf("Mine", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Mìn", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (highExplosiveMineSkill != null)
            {
                highExplosiveMineSkill.UnlockOrUpgrade(runtimeLevel);
                ChipsetBattleStats.RegisterChipset(10, runtimeLevel, highExplosiveMineSkill.GetCalculatedDamage());
                Debug.Log($"[PlayerChipsetSkillManager] Đã kích hoạt/nâng cấp High-Explosive Mine lên cấp {runtimeLevel}!");
            }
            return;
        }

        // 6. Kiểm tra nếu là Energy Jumper Cables (ID 9 hoặc key "energy-jumper-cables" / "Cáp Hồi Máu")
        if (id == 9 || key.Contains("jumper") || key.Contains("cable") || name.IndexOf("Jumper", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Cáp", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (energyJumperCablesSkill != null)
            {
                energyJumperCablesSkill.UnlockOrUpgrade(runtimeLevel);
                ChipsetBattleStats.RegisterChipset(9, runtimeLevel, 0);
                Debug.Log($"[PlayerChipsetSkillManager] Đã kích hoạt/nâng cấp Energy Jumper Cables lên cấp {runtimeLevel}!");
            }
            return;
        }

        // 7. Kiểm tra nếu là Spiky Discus (ID 7 hoặc key "spiky-discus" / "Đĩa Gai")
        if (id == 7 || key.Contains("discus") || name.IndexOf("Discus", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Gai", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (spikyDiscusSkill != null)
            {
                spikyDiscusSkill.UnlockOrUpgrade(runtimeLevel);
                ChipsetBattleStats.RegisterChipset(7, runtimeLevel, spikyDiscusSkill.GetCalculatedDamage());
                Debug.Log($"[PlayerChipsetSkillManager] Đã kích hoạt/nâng cấp Spiky Discus lên cấp {runtimeLevel}!");
            }
            return;
        }

        // 8. Kiểm tra nếu là Gun Turret (ID 6 hoặc key "gun-turret")
        if (id == 6 || key.Contains("turret") || name.IndexOf("Turret", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (gunTurretSkill != null)
            {
                gunTurretSkill.UnlockOrUpgrade(runtimeLevel);
                ChipsetBattleStats.RegisterChipset(6, runtimeLevel, gunTurretSkill.GetCurrentDamage());
                Debug.Log($"[PlayerChipsetSkillManager] Đã kích hoạt/nâng cấp Gun Turret lên cấp {runtimeLevel}!");
            }
            return;
        }

        // 9. Kiểm tra nếu là Rocket Punch (ID 3 hoặc key "rocket-punch" / "punch")
        if (id == 3 || key.Contains("punch") || key.Contains("rocket") || name.IndexOf("Punch", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (rocketPunchSkill != null)
            {
                rocketPunchSkill.UnlockOrUpgrade(runtimeLevel);
                ChipsetBattleStats.RegisterChipset(3, runtimeLevel, rocketPunchSkill.GetCurrentConfig().directDamage);
                Debug.Log($"[PlayerChipsetSkillManager] Đã kích hoạt/nâng cấp Rocket Punch lên cấp {runtimeLevel}!");
            }
            return;
        }

        // 10. Kiểm tra nếu là Spinning Blade (ID 4 hoặc key "spinning-blade" / "blade")
        if (id == 4 || key.Contains("blade") || name.IndexOf("Blade", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (spinningBladeSkill != null)
            {
                spinningBladeSkill.UnlockOrUpgrade(runtimeLevel);
                ChipsetBattleStats.RegisterChipset(4, runtimeLevel, spinningBladeSkill.GetCurrentConfig().damage);
                Debug.Log($"[PlayerChipsetSkillManager] Đã kích hoạt/nâng cấp Spinning Blade lên cấp {runtimeLevel}!");
            }
            return;
        }
    }
}
