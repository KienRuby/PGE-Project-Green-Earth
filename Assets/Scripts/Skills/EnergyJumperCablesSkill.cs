using UnityEngine;

/// <summary>
/// Kỹ năng Energy Jumper Cables (Cáp Hồi Máu - Chipset ID 9).
/// Hút máu kẻ địch khi gây sát thương và hồi phục máu/khiên cho Player.
/// Quản lý 5 cấp độ in-game (Hút máu 2% -> 6%, Hút máu toàn bộ vũ khí, Khiên 10% HP, Nhân đôi hút máu khi HP < 20%)
/// và nhận buff Khung Meta từ PlayerDataService (ID 9).
/// </summary>
public class EnergyJumperCablesSkill : MonoBehaviour
{
    public static EnergyJumperCablesSkill Instance { get; private set; }

    [System.Serializable]
    public struct CablesLevelConfig
    {
        [Tooltip("Tỷ lệ hút máu (0.02 = 2.0%, 0.06 = 6.0%)")]
        public float lifeStealPercent;
        public bool allWeapons;
        public bool overhealToShield;
        public bool doubleHealLowHp;
    }

    [Header("Skill Status")]
    [Tooltip("Trạng thái mở khóa của kỹ năng.")]
    [SerializeField] private bool isUnlocked = false;

    [Tooltip("Cấp độ kỹ năng hiện tại trong trận đấu (1 -> 5).")]
    [SerializeField, Range(1, 5)] private int currentLevel = 1;

    [Header("5 Level Progression Configuration (Tùy chỉnh trong Inspector)")]
    [SerializeField]
    private CablesLevelConfig[] levelConfigs = new CablesLevelConfig[]
    {
        new CablesLevelConfig { lifeStealPercent = 0.020f, allWeapons = false, overhealToShield = false, doubleHealLowHp = false },
        new CablesLevelConfig { lifeStealPercent = 0.025f, allWeapons = false, overhealToShield = false, doubleHealLowHp = false },
        new CablesLevelConfig { lifeStealPercent = 0.035f, allWeapons = true, overhealToShield = false, doubleHealLowHp = false },
        new CablesLevelConfig { lifeStealPercent = 0.045f, allWeapons = true, overhealToShield = true, doubleHealLowHp = false },
        new CablesLevelConfig { lifeStealPercent = 0.060f, allWeapons = true, overhealToShield = true, doubleHealLowHp = true }
    };

    private PlayerHealth playerHealth;

    // Meta Tier Bonuses
    private float metaLifeStealBonus = 0f;

    public bool IsUnlocked => isUnlocked;
    public int CurrentLevel => currentLevel;

    private void Awake()
    {
        Instance = this;
        playerHealth = GetComponent<PlayerHealth>();

        LoadMetaTierBonuses();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        LoadMetaTierBonuses();
    }

    /// <summary>
    /// Đọc cấp bậc Khung Thẻ Chipset Meta (Chip ID 9) từ PlayerDataService:
    /// - Tier 1 (Magic): Life Steal 2.3%
    /// - Tier 2 (Rare): All Weapons' +1% Life Steal
    /// - Tier 3 (Unique): All Weapons' +1% Life Steal (Tổng +2%)
    /// - Tier 4 (Epic): All Weapons' +1% Life Steal (Tổng +3%)
    /// - Tier 5 (Holographic): All Weapons' +2% Life Steal (Tổng +5%)
    /// </summary>
    public void LoadMetaTierBonuses()
    {
        ChipTier tier = PlayerDataService.GetChipTier(9);

        metaLifeStealBonus = 0f;

        if (tier >= ChipTier.Rare)
        {
            metaLifeStealBonus += 0.01f; // +1% Life Steal
        }
        if (tier >= ChipTier.Unique)
        {
            metaLifeStealBonus += 0.01f; // +1% Life Steal (Tổng +2%)
        }
        if (tier >= ChipTier.Epic)
        {
            metaLifeStealBonus += 0.01f; // +1% Life Steal (Tổng +3%)
        }
        if (tier == ChipTier.Holographic)
        {
            metaLifeStealBonus += 0.02f; // +2% Life Steal (Tổng +5%)
        }
    }

    /// <summary>
    /// Mở khóa hoặc nâng cấp kỹ năng Energy Jumper Cables trong trận đấu (Cấp 1 -> 5).
    /// </summary>
    public void UnlockOrUpgrade(int targetLevel)
    {
        isUnlocked = true;
        currentLevel = Mathf.Clamp(targetLevel, 1, 5);
        LoadMetaTierBonuses();

        // Cấp 4+: Thiết lập giới hạn Khiên bảo vệ tối đa 10% MaxHP
        if (playerHealth != null && currentLevel >= 4)
        {
            int shieldCap = Mathf.Max(1, Mathf.RoundToInt(playerHealth.MaxHealth * 0.10f));
            playerHealth.SetMaxShield(shieldCap);
        }

        Debug.Log($"[EnergyJumperCablesSkill] Cáp Hồi Máu đã lên Cấp {currentLevel}! (Life Steal: {GetCalculatedLifeStealPercent() * 100f:F1}%, All Weapons: {currentLevel >= 3})");
    }

    /// <summary>
    /// Kích hoạt Hút Máu toàn cục khi Player hoặc bất kỳ vũ khí/kỹ năng nào gây sát thương lên kẻ địch.
    /// </summary>
    public static void TriggerLifeSteal(int damageDealt, bool isMainWeapon = false)
    {
        if (Instance == null || damageDealt <= 0) return;
        Instance.ProcessDamageDealt(damageDealt, isMainWeapon);
    }

    private void ProcessDamageDealt(int damageDealt, bool isMainWeapon)
    {
        if (!isUnlocked && metaLifeStealBonus <= 0f) return;
        if (playerHealth == null || playerHealth.IsDead) return;

        // Cấp 1-2: chỉ áp dụng trên đòn đánh vũ khí chính (hoặc nếu có buff meta All Weapons)
        bool appliesToWeapon = isMainWeapon || currentLevel >= 3 || metaLifeStealBonus > 0f;
        if (!appliesToWeapon) return;

        float lifeStealRate = GetCalculatedLifeStealPercent();

        // Cấp 5 (Tối thượng): Hồi sinh lực bùng nổ - Nhân đôi tỷ lệ hút máu khi HP dưới 20%
        if (currentLevel >= 5 && playerHealth.CurrentHealth < playerHealth.MaxHealth * 0.20f)
        {
            lifeStealRate *= 2.0f;
        }

        int healAmount = Mathf.RoundToInt(damageDealt * lifeStealRate);
        if (healAmount > 0)
        {
            bool canOverhealToShield = currentLevel >= 4;
            int healthAndShieldBefore = playerHealth.CurrentHealth + playerHealth.CurrentShield;
            playerHealth.Heal(healAmount, canOverhealToShield);
            int actualHealing = (playerHealth.CurrentHealth + playerHealth.CurrentShield) - healthAndShieldBefore;
            if (actualHealing > 0)
            {
                ChipsetBattleStats.RecordAttack(9, 0);
                ChipsetBattleStats.RecordHealing(9, actualHealing);
            }
        }
    }

    // --- TÍNH TOÁN CHỈ SỐ THEO CẤP ĐỘ VÀ META ---

    public float GetCalculatedLifeStealPercent()
    {
        float baseRate = 0f;
        if (isUnlocked)
        {
            int index = Mathf.Clamp(currentLevel - 1, 0, levelConfigs.Length - 1);
            baseRate = levelConfigs[index].lifeStealPercent;
        }
        else
        {
            baseRate = 0.023f; // Base Common 2.3%
        }

        return baseRate + metaLifeStealBonus;
    }
}
