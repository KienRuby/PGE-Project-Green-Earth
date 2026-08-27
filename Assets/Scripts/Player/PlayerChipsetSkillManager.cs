using UnityEngine;

/// <summary>
/// Quản lý việc tiếp nhận các lựa chọn Chipset khi người chơi Lên Cấp (Level Up)
/// và kích hoạt/nâng cấp các kỹ năng tương ứng trên Player (ví dụ: Gun Turret, v.v.).
/// </summary>
[RequireComponent(typeof(GunTurretSkill))]
public class PlayerChipsetSkillManager : MonoBehaviour
{
    [Header("Skill References")]
    [SerializeField] private GunTurretSkill gunTurretSkill;
    [SerializeField] private RocketPunchSkill rocketPunchSkill;
    [SerializeField] private SpinningBladeSkill spinningBladeSkill;

    private void Awake()
    {
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

        // 1. Kiểm tra nếu là Gun Turret (ID 6 hoặc key "gun-turret")
        if (id == 6 || key.Contains("turret") || name.IndexOf("Turret", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (gunTurretSkill != null)
            {
                gunTurretSkill.UnlockOrUpgrade(runtimeLevel);
                Debug.Log($"[PlayerChipsetSkillManager] Đã kích hoạt/nâng cấp Gun Turret lên cấp {runtimeLevel}!");
            }
            return;
        }

        // 2. Kiểm tra nếu là Rocket Punch (ID 3 hoặc key "rocket-punch" / "punch")
        if (id == 3 || key.Contains("punch") || key.Contains("rocket") || name.IndexOf("Punch", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (rocketPunchSkill != null)
            {
                rocketPunchSkill.UnlockOrUpgrade(runtimeLevel);
                Debug.Log($"[PlayerChipsetSkillManager] Đã kích hoạt/nâng cấp Rocket Punch lên cấp {runtimeLevel}!");
            }
            return;
        }

        // 3. Kiểm tra nếu là Spinning Blade (ID 4 hoặc key "spinning-blade" / "blade")
        if (id == 4 || key.Contains("blade") || name.IndexOf("Blade", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (spinningBladeSkill != null)
            {
                spinningBladeSkill.UnlockOrUpgrade(runtimeLevel);
                Debug.Log($"[PlayerChipsetSkillManager] Đã kích hoạt/nâng cấp Spinning Blade lên cấp {runtimeLevel}!");
            }
            return;
        }
    }
}
