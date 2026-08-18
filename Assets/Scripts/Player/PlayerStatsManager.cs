using UnityEngine;

/// <summary>
/// Quản lý việc đọc các chỉ số nâng cấp từ Lab (qua PlayerPrefs)
/// và áp dụng trực tiếp vào các hệ thống: PlayerHealth, PlayerMovement, PlayerAutoShooter.
/// </summary>
[DefaultExecutionOrder(-50)]
public class PlayerStatsManager : MonoBehaviour
{
    [Header("Base Scaling Multipliers")]
    [Tooltip("Lượng máu tối đa cộng thêm mỗi cấp HP (+10 HP/cấp).")]
    [SerializeField] private int hpBonusPerLevel = 10;

    [Tooltip("Lượng sát thương cộng thêm mỗi cấp ATK (+3 Sát thương/cấp).")]
    [SerializeField] private int damageBonusPerLevel = 3;

    [Tooltip("Tỷ lệ tăng tốc độ chạy mỗi cấp SPD (+0.25m/s mỗi cấp).")]
    [SerializeField] private float speedBonusPerLevel = 0.25f;

    [Tooltip("Tỷ lệ tăng tốc độ bắn mỗi cấp FIRE (+0.3 phát/s mỗi cấp).")]
    [SerializeField] private float fireRateBonusPerLevel = 0.3f;

    [Tooltip("Khoảng cách tầm bắn cộng thêm mỗi cấp RANGE (+0.5m/cấp).")]
    [SerializeField] private float rangeBonusPerLevel = 0.5f;

    [Tooltip("Lượng máu tự hồi phục mỗi giây theo cấp REGEN (+0.5 HP/giây/cấp).")]
    [SerializeField] private float regenPerSecondPerLevel = 0.5f;

    [Tooltip("Sát thương giảm trừ theo cấp DEF / ARMOR (+1 giảm sát thương/cấp).")]
    [SerializeField] private int damageReductionPerLevel = 1;

    [Tooltip("Tốc độ bay của đạn cộng thêm mỗi cấp TECH (+0.5m/s/cấp).")]
    [SerializeField] private float bulletSpeedBonusPerLevel = 0.5f;

    [Tooltip("Tỷ lệ chí mạng mỗi cấp CRIT (+2% mỗi cấp).")]
    [SerializeField] private float critChancePerLevel = 0.02f;

    private PlayerHealth playerHealth;
    private PlayerMovement playerMovement;
    private PlayerAutoShooter playerAutoShooter;

    public int BonusMaxHealth { get; private set; }
    public int BonusDamage { get; private set; }
    public float BonusSpeed { get; private set; }
    public float BonusFireRate { get; private set; }
    public float BonusRange { get; private set; }
    public float HealthRegenPerSecond { get; private set; }
    public int DamageReduction { get; private set; }
    public float BonusBulletSpeed { get; private set; }
    public float CritChance { get; private set; }

    private float regenAccumulator;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerMovement = GetComponent<PlayerMovement>();
        playerAutoShooter = GetComponent<PlayerAutoShooter>();

        LoadAndApplyStats();
    }

    public void LoadAndApplyStats()
    {
        int defLevel = GetStatLevel("DEF");
        int atkLevel = GetStatLevel("ATK");
        int hpLevel = GetStatLevel("HP");
        int spdLevel = GetStatLevel("SPD");
        int critLevel = GetStatLevel("CRIT");
        int rangeLevel = GetStatLevel("RANGE");
        int fireLevel = GetStatLevel("FIRE");
        int regenLevel = GetStatLevel("REGEN");
        int armorLevel = GetStatLevel("ARMOR");
        int powerLevel = GetStatLevel("POWER");
        int techLevel = GetStatLevel("TECH");

        BonusMaxHealth = hpLevel * hpBonusPerLevel;
        BonusDamage = (atkLevel * damageBonusPerLevel) + (powerLevel * 2);
        BonusSpeed = spdLevel * speedBonusPerLevel;
        BonusFireRate = fireLevel * fireRateBonusPerLevel;
        BonusRange = rangeLevel * rangeBonusPerLevel;
        HealthRegenPerSecond = regenLevel * regenPerSecondPerLevel;
        DamageReduction = (defLevel * damageReductionPerLevel) + (armorLevel * damageReductionPerLevel);
        BonusBulletSpeed = techLevel * bulletSpeedBonusPerLevel;
        CritChance = Mathf.Clamp01(critLevel * critChancePerLevel);

        if (playerHealth != null && BonusMaxHealth > 0)
        {
            playerHealth.SetMaxHealth(playerHealth.MaxHealth + BonusMaxHealth, true);
        }

        if (playerHealth != null && DamageReduction > 0)
        {
            playerHealth.SetDamageReduction(DamageReduction);
        }

        if (playerMovement != null && BonusSpeed > 0f)
        {
            playerMovement.SetMoveSpeedBonus(BonusSpeed);
        }

        if (playerAutoShooter != null)
        {
            playerAutoShooter.ApplyStatBonuses(BonusDamage, BonusFireRate, BonusRange, BonusBulletSpeed, CritChance);
        }
    }

    private void Update()
    {
        if (HealthRegenPerSecond > 0f && playerHealth != null && !playerHealth.IsDead)
        {
            regenAccumulator += HealthRegenPerSecond * Time.deltaTime;
            if (regenAccumulator >= 1f)
            {
                int healPoints = Mathf.FloorToInt(regenAccumulator);
                regenAccumulator -= healPoints;
                playerHealth.Heal(healPoints);
            }
        }
    }

    public static int GetStatLevel(string statName)
    {
        string key = $"{LabUpgradeController.ItemLevelKeyPrefix}{statName.Trim().ToUpperInvariant()}";
        return Mathf.Max(0, PlayerPrefs.GetInt(key, 0));
    }
}
