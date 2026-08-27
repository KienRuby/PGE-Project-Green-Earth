using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý kỹ năng Gun Turret gắn trên người chơi (Player).
/// Quản lý chính xác 5 cấp độ nâng cấp từ bảng Level Up:
/// - Cấp 1: Sát thương 27 | Hồi chiêu 8.4s | Tồn tại 10.0s | Tốc độ bắn Nhanh | 1 Tháp
/// - Cấp 2: Sát thương 40 | Hồi chiêu 7.5s | Tồn tại 12.0s | Tốc độ bắn Nhanh | 1 Tháp
/// - Cấp 3: Sát thương 55 | Hồi chiêu 6.5s | Tồn tại 12.0s | Tốc độ bắn Rất nhanh | 30% Đạn nổ diện rộng | 1 Tháp
/// - Cấp 4: Sát thương 75 | Hồi chiêu 5.5s | Tồn tại 15.0s | Tốc độ bắn Rất nhanh | Tự động hồi máu | 1 Tháp
/// - Cấp 5 (Tối thượng): Sát thương 105 | Hồi chiêu 4.0s | Tồn tại 15.0s | Tốc độ bắn Cực nhanh | Đặt tối đa 2 Tháp cùng lúc
/// </summary>
public class GunTurretSkill : MonoBehaviour
{
    [System.Serializable]
    public struct TurretLevelConfig
    {
        public int damage;
        public float cooldown;
        public float duration;
        public float fireRate;
        public float explosiveChance;
        public bool hasHealthRegen;
        public int maxSimultaneousTurrets;
        public int turretHealth;
    }

    [Header("Prefab References")]
    [Tooltip("Prefab của GunTurret (Assets/Prefabs/Chipset/GunTurret.prefab).")]
    [SerializeField] private GameObject turretPrefab;

    [Tooltip("Prefab viên đạn bắn ra.")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Prefab hiệu ứng nổ đạn diện rộng (VFX Boom).")]
    [SerializeField] private GameObject explosionVfxPrefab;

    [Header("5 Level Progression Configuration")]
    [SerializeField]
    private TurretLevelConfig[] levelConfigs = new TurretLevelConfig[]
    {
        // Cấp 1: 27 dmg, 8.4s cd, 10.0s dur, fireRate 3.0/s, 1 tháp
        new TurretLevelConfig { damage = 27, cooldown = 8.4f, duration = 10.0f, fireRate = 3.0f, explosiveChance = 0.0f, hasHealthRegen = false, maxSimultaneousTurrets = 1, turretHealth = 200 },
        // Cấp 2: 40 dmg, 7.5s cd, 12.0s dur, fireRate 3.2f/s, 1 tháp
        new TurretLevelConfig { damage = 40, cooldown = 7.5f, duration = 12.0f, fireRate = 3.2f, explosiveChance = 0.0f, hasHealthRegen = false, maxSimultaneousTurrets = 1, turretHealth = 250 },
        // Cấp 3: 55 dmg, 6.5s cd, 12.0s dur, fireRate 4.5f/s, 30% nổ, 1 tháp
        new TurretLevelConfig { damage = 55, cooldown = 6.5f, duration = 12.0f, fireRate = 4.5f, explosiveChance = 0.30f, hasHealthRegen = false, maxSimultaneousTurrets = 1, turretHealth = 300 },
        // Cấp 4: 75 dmg, 5.5s cd, 15.0s dur, fireRate 4.8f/s, 30% nổ, hồi máu, 1 tháp
        new TurretLevelConfig { damage = 75, cooldown = 5.5f, duration = 15.0f, fireRate = 4.8f, explosiveChance = 0.30f, hasHealthRegen = true, maxSimultaneousTurrets = 1, turretHealth = 350 },
        // Cấp 5 (Tối thượng): 105 dmg, 4.0s cd, 15.0s dur, fireRate 6.0f/s, 30% nổ, hồi máu, Tối đa 2 tháp
        new TurretLevelConfig { damage = 105, cooldown = 4.0f, duration = 15.0f, fireRate = 6.0f, explosiveChance = 0.30f, hasHealthRegen = true, maxSimultaneousTurrets = 2, turretHealth = 400 }
    };

    [Header("Runtime State (Debug)")]
    [SerializeField] private bool isUnlocked = false;
    [SerializeField] private int currentSkillLevel = 1;
    [SerializeField] private float currentCooldownTimer = 0f;
    [SerializeField] private int activeTurretCount = 0;

    private readonly List<GunTurret> activeTurrets = new List<GunTurret>();

    public bool IsUnlocked => isUnlocked;
    public int CurrentSkillLevel => currentSkillLevel;
    public int ActiveTurretCount => activeTurrets.Count;
    public int MaxAllowedTurrets => GetCurrentConfig().maxSimultaneousTurrets;

    private void Awake()
    {
#if UNITY_EDITOR
        if (turretPrefab == null)
        {
            turretPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Chipset/GunTurret.prefab");
        }
        if (projectilePrefab == null)
        {
            projectilePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
        }
        if (explosionVfxPrefab == null)
        {
            explosionVfxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX Boom.prefab");
        }
#endif
    }

    /// <summary>
    /// Kích hoạt mở khóa hoặc nâng cấp cấp độ của kỹ năng Gun Turret từ bảng Level Up (1 -> 5).
    /// </summary>
    public void UnlockOrUpgrade(int level)
    {
        currentSkillLevel = Mathf.Clamp(level, 1, 5);
        isUnlocked = true;

        CleanNullTurrets();

        // Nếu số lượng tháp trên sân chưa đạt mức tối đa cho phép của cấp hiện tại, spawn ngay hoặc bắt đầu hồi chiêu
        if (activeTurrets.Count < MaxAllowedTurrets)
        {
            if (activeTurrets.Count == 0)
            {
                SpawnTurret();
            }
            else if (currentCooldownTimer <= 0f)
            {
                currentCooldownTimer = GetCurrentCooldown();
            }
        }
    }

    private void Update()
    {
        if (!isUnlocked) return;

        CleanNullTurrets();
        activeTurretCount = activeTurrets.Count;

        // Nếu số tháp trên sân ít hơn số lượng tối đa được phép (Cấp 1..4 là 1, Cấp 5 là 2)
        if (activeTurrets.Count < MaxAllowedTurrets)
        {
            currentCooldownTimer -= Time.deltaTime;
            if (currentCooldownTimer <= 0f)
            {
                SpawnTurret();

                // Nếu vẫn còn thiếu tháp (ví dụ Cấp 5 mới đặt 1 tháp), tiếp tục reset cooldown cho tháp tiếp theo
                if (activeTurrets.Count < MaxAllowedTurrets)
                {
                    currentCooldownTimer = GetCurrentCooldown();
                }
            }
        }
        else
        {
            // Đã đủ tháp trên sân, giữ cooldown sẵn sàng
            currentCooldownTimer = 0f;
        }
    }

    /// <summary>
    /// Triệu hồi một GunTurret tại vị trí hiện tại của Player.
    /// </summary>
    public void SpawnTurret()
    {
        if (turretPrefab == null)
        {
            Debug.LogWarning("[GunTurretSkill] Chưa gán turretPrefab cho GunTurretSkill trên Player!");
            return;
        }

        Vector3 spawnPos = transform.position;
        Quaternion spawnRot = Quaternion.identity;

        GameObject turretObj;
        if (PoolManager.Instance != null)
        {
            turretObj = PoolManager.Instance.Spawn(turretPrefab, spawnPos, spawnRot);
        }
        else
        {
            turretObj = Instantiate(turretPrefab, spawnPos, spawnRot);
        }

        if (turretObj == null) return;

        GunTurret turretComponent = turretObj.GetComponent<GunTurret>();
        if (turretComponent == null)
        {
            turretComponent = turretObj.AddComponent<GunTurret>();
        }

        activeTurrets.Add(turretComponent);
        activeTurretCount = activeTurrets.Count;

        // Lấy cấu hình chỉ số theo cấp hiện tại
        TurretLevelConfig config = GetCurrentConfig();
        int bonusDmg = 0;
        PlayerStatsManager stats = GetComponent<PlayerStatsManager>();
        if (stats != null)
        {
            bonusDmg = stats.BonusDamage;
        }

        int finalDamage = config.damage + bonusDmg;
        float fireRate = config.fireRate;
        float attackRange = 10f;
        float bulletSpeed = 12f;
        CalculateMetaTierBonuses(out float durationMultiplier, out _);
        float duration = config.duration * durationMultiplier;
        float explosiveChance = config.explosiveChance;
        bool hasRegen = config.hasHealthRegen;
        int health = config.turretHealth;

        turretComponent.Initialize(
            finalDamage,
            fireRate,
            bulletSpeed,
            duration,
            explosiveChance,
            hasRegen,
            health,
            projectilePrefab,
            explosionVfxPrefab,
            () => OnSingleTurretDespawned(turretComponent)
        );
    }

    private void OnSingleTurretDespawned(GunTurret turret)
    {
        if (turret != null)
        {
            activeTurrets.Remove(turret);
        }
        CleanNullTurrets();
        activeTurretCount = activeTurrets.Count;

        // Nếu số lượng tháp trên sân còn thiếu, kích hoạt đếm hồi chiêu
        if (activeTurrets.Count < MaxAllowedTurrets && currentCooldownTimer <= 0f)
        {
            currentCooldownTimer = GetCurrentCooldown();
        }
    }

    private void CleanNullTurrets()
    {
        for (int i = activeTurrets.Count - 1; i >= 0; i--)
        {
            if (activeTurrets[i] == null || !activeTurrets[i].gameObject.activeInHierarchy)
            {
                activeTurrets.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Tính toán các chỉ số thưởng thêm khi nâng cấp khung Chipset trong MainMenu (Tier 1..5):
    /// - Tier 1 (Common / Green Frame): Base
    /// - Tier 2 (Rare / Blue Frame): Turret Duration +20%
    /// - Tier 3 (Epic / Purple Frame): Turret Cooldown -30%
    /// - Tier 4 (Legendary / Yellow Frame): Turret Duration +20%
    /// - Tier 5 (Secret / Red Frame): Turret Duration +30%
    /// </summary>
    public void CalculateMetaTierBonuses(out float durationMultiplier, out float cooldownMultiplier)
    {
        durationMultiplier = 1.0f;
        cooldownMultiplier = 1.0f;

        int metaTier = 1;
        if (PlayerDataService.LoadChipsetItemData(6, out _, out int savedTier, out _, out _, out _))
        {
            metaTier = Mathf.Clamp(savedTier, 1, 5);
        }

        // Tier 2 (Rare / Blue Frame): Turret Duration +20%
        if (metaTier >= 2)
        {
            durationMultiplier += 0.20f;
        }

        // Tier 3 (Epic / Purple Frame): Turret Cooldown -30%
        if (metaTier >= 3)
        {
            cooldownMultiplier -= 0.30f;
        }

        // Tier 4 (Legendary / Yellow Frame): Turret Duration +20%
        if (metaTier >= 4)
        {
            durationMultiplier += 0.20f;
        }

        // Tier 5 (Secret / Red Frame): Turret Duration +30%
        if (metaTier >= 5)
        {
            durationMultiplier += 0.30f;
        }
    }

    public TurretLevelConfig GetCurrentConfig()
    {
        int index = Mathf.Clamp(currentSkillLevel - 1, 0, levelConfigs.Length - 1);
        return levelConfigs[index];
    }

    public int GetCurrentDamage()
    {
        return GetCurrentConfig().damage;
    }

    public float GetCurrentDuration()
    {
        CalculateMetaTierBonuses(out float durationMultiplier, out _);
        return GetCurrentConfig().duration * durationMultiplier;
    }

    public float GetCurrentCooldown()
    {
        CalculateMetaTierBonuses(out _, out float cooldownMultiplier);
        return Mathf.Max(1.0f, GetCurrentConfig().cooldown * cooldownMultiplier);
    }

    public float GetCurrentFireRate()
    {
        return GetCurrentConfig().fireRate;
    }
}
