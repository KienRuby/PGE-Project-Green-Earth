using UnityEngine;

/// <summary>
/// Quản lý kỹ năng Rocket Punch (Nắm Đấm Phản Lực) gắn trên Player:
/// 1. Triệu hồi nắm đấm xuất hiện bay lượn vòng tròn xung quanh Player với tốc độ chậm.
/// 2. Khi phát hiện quái vật trong tầm quét 360°, nắm đấm khóa hướng và lao thẳng tới quái vật theo đường thẳng.
/// 3. Sau khi nắm đấm đầu tiên phóng đi, Player phải chờ đúng 3 giây (Cooldown) mới triệu hồi nắm đấm thứ 2.
/// 4. Mọi thông số (tốc độ quay quanh Player, bán kính quay, tốc độ lao, thời gian chờ 3s) đều tùy chỉnh được trong Inspector.
/// </summary>
public class RocketPunchSkill : MonoBehaviour
{
    [System.Serializable]
    public struct RocketPunchLevelConfig
    {
        public int directDamage;
        public int aoeDamage;
        public float cooldown;
        public float aoeRadius;
        public bool hasStun;
        public float stunDuration;
        public bool hasLavaPool;
    }

    [Header("Prefabs References")]
    [Tooltip("Prefab của RocketPunch (Assets/Prefabs/Chipset/RocketPunch.prefab).")]
    [SerializeField] private GameObject rocketPunchPrefab;

    [Tooltip("Prefab hiệu ứng nổ (VFX Boom.prefab).")]
    [SerializeField] private GameObject explosionVfxPrefab;

    [Tooltip("Prefab vùng dung nham lửa (LavaHazardZone).")]
    [SerializeField] private GameObject lavaHazardPrefab;

    [Header("Orbit Tuning (Tùy chỉnh bay quanh Player)")]
    [Tooltip("Bán kính vòng quay xung quanh Player (mét).")]
    [Range(0.8f, 3.5f)]
    [SerializeField] private float orbitRadius = 1.6f;

    [Tooltip("Tốc độ bay xoay vòng quanh Player (độ/giây). Càng nhỏ càng bay chậm.")]
    [Range(60f, 720f)]
    [SerializeField] private float orbitSpeed = 220f;

    [Header("Launch Tuning (Tùy chỉnh khi phóng tới quái)")]
    [Tooltip("Vận tốc bay thẳng khi lao tới quái vật (mét/giây).")]
    [Range(4f, 40f)]
    [SerializeField] private float launchSpeed = 12.0f;

    [Tooltip("Hệ số nhân tốc độ đấm (Attack Speed Multiplier).")]
    [Range(0.2f, 5.0f)]
    [SerializeField] private float attackSpeedMultiplier = 1.0f;

    [Header("5 Level Progression Configuration")]
    [SerializeField]
    private RocketPunchLevelConfig[] levelConfigs = new RocketPunchLevelConfig[]
    {
        // Cấp 1: 70 dmg, CD 3.0s, AoE 2.5m
        new RocketPunchLevelConfig { directDamage = 70, aoeDamage = 37, cooldown = 3.0f, aoeRadius = 2.5f, hasStun = false, stunDuration = 0f, hasLavaPool = false },
        // Cấp 2: 100 dmg, CD 2.5s, AoE 3.0m
        new RocketPunchLevelConfig { directDamage = 100, aoeDamage = 55, cooldown = 2.5f, aoeRadius = 3.0f, hasStun = false, stunDuration = 0f, hasLavaPool = false },
        // Cấp 3: 140 dmg, CD 2.0s, AoE 4.0m
        new RocketPunchLevelConfig { directDamage = 140, aoeDamage = 80, cooldown = 2.0f, aoeRadius = 4.0f, hasStun = false, stunDuration = 0f, hasLavaPool = false },
        // Cấp 4: 190 dmg, CD 1.5s, AoE 4.0m, Stun 1.0s
        new RocketPunchLevelConfig { directDamage = 190, aoeDamage = 115, cooldown = 1.5f, aoeRadius = 4.0f, hasStun = true, stunDuration = 1.0f, hasLavaPool = false },
        // Cấp 5 (Tối thượng): 260 dmg, CD 1.0s, AoE 5.0m, Stun 1.0s, Dung nham 3s
        new RocketPunchLevelConfig { directDamage = 260, aoeDamage = 160, cooldown = 1.0f, aoeRadius = 5.0f, hasStun = true, stunDuration = 1.0f, hasLavaPool = true }
    };

    [Header("Runtime State (Debug)")]
    [SerializeField] private bool isUnlocked = false;
    [SerializeField] private int currentSkillLevel = 1;
    [SerializeField] private float currentCooldownTimer = 0f;
    [SerializeField] private RocketPunchProjectile activeOrbitingPunch;

    private PlayerAutoShooter playerAutoShooter;

    public bool IsUnlocked => isUnlocked;
    public int CurrentSkillLevel => currentSkillLevel;
    public float OrbitRadius => orbitRadius;
    public float OrbitSpeed => orbitSpeed;
    public float LaunchSpeed => launchSpeed;

    private void Awake()
    {
        playerAutoShooter = GetComponent<PlayerAutoShooter>();
#if UNITY_EDITOR
        if (rocketPunchPrefab == null)
        {
            rocketPunchPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Chipset/RocketPunch.prefab");
        }
        if (explosionVfxPrefab == null)
        {
            explosionVfxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX Boom.prefab");
        }
#endif

    }

    /// <summary>
    /// Mở khóa hoặc tăng cấp độ kỹ năng Rocket Punch (1 -> 5).
    /// </summary>
    public void UnlockOrUpgrade(int level)
    {
        currentSkillLevel = Mathf.Clamp(level, 1, 5);
        isUnlocked = true;

        if (activeOrbitingPunch == null && currentCooldownTimer <= 0f)
        {
            SpawnOrbitingPunch();
        }
    }

    private void Update()
    {
        if (!isUnlocked) return;

        // 1. Nếu chưa có nắm đấm xoay quanh Player và đã hết Cooldown (ví dụ 3 giây), triệu hồi nắm đấm mới
        if (activeOrbitingPunch == null)
        {
            currentCooldownTimer -= Time.deltaTime;
            if (currentCooldownTimer <= 0f)
            {
                SpawnOrbitingPunch();
            }
            return;
        }

        // 2. Nếu đang có nắm đấm xoay quanh Player, quét tìm quái vật để phóng tới
        if (activeOrbitingPunch.State == RocketPunchState.Orbiting)
        {
            Transform target = FindTargetEnemy();
            if (target != null)
            {
                activeOrbitingPunch.transform.position = GetSharedFirePoint().position;
                ChipsetBattleStats.RecordAttack(3, 1);
                // Phóng nắm đấm tới quái vật (kèm Transform để theo dõi và đổi mục tiêu nếu quái chết)
                activeOrbitingPunch.LaunchTowards(target);
                activeOrbitingPunch = null;

                // Bắt đầu đếm ngược thời gian chờ 3 giây cho cú đấm thứ 2
                currentCooldownTimer = GetCurrentCooldown();
            }
        }
    }

    /// <summary>
    /// Sinh ra nắm đấm bay lượn vòng tròn xung quanh Player.
    /// </summary>
    private void SpawnOrbitingPunch()
    {
        if (rocketPunchPrefab == null) return;

        Vector3 spawnPos = GetSharedFirePoint().position;
        GameObject punchObj;

        if (PoolManager.Instance != null)
        {
            punchObj = PoolManager.Instance.Spawn(rocketPunchPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            punchObj = Instantiate(rocketPunchPrefab, spawnPos, Quaternion.identity);
        }

        if (punchObj == null) return;

        RocketPunchProjectile proj = punchObj.GetComponent<RocketPunchProjectile>();
        if (proj == null)
        {
            proj = punchObj.AddComponent<RocketPunchProjectile>();
        }

        RocketPunchLevelConfig config = GetCurrentConfig();
        CalculateMetaTierBonuses(out float dmgMultiplier, out _, out float aoeMultiplier);

        int labBonusDmg = 0;
        PlayerStatsManager stats = GetComponent<PlayerStatsManager>();
        if (stats != null) labBonusDmg = stats.BonusDamage;

        int finalDirectDmg = Mathf.RoundToInt((config.directDamage + labBonusDmg) * dmgMultiplier);
        int finalAoeDmg = Mathf.RoundToInt((config.aoeDamage + labBonusDmg) * dmgMultiplier);
        float finalRadius = config.aoeRadius * aoeMultiplier;

        proj.SetupOrbit(
            transform,
            finalDirectDmg,
            finalAoeDmg,
            finalRadius,
            launchSpeed,
            orbitRadius,
            orbitSpeed,
            config.hasStun,
            config.stunDuration,
            config.hasLavaPool,
            explosionVfxPrefab,
            lavaHazardPrefab,
            Random.Range(0f, 360f),
            () =>
            {
                if (activeOrbitingPunch == proj)
                {
                    activeOrbitingPunch = null;
                }
            }
        );
        proj.SetSharedTargetProvider(playerAutoShooter);

        activeOrbitingPunch = proj;
    }

    private Transform FindTargetEnemy()
    {
        if (playerAutoShooter != null)
        {
            return playerAutoShooter.CurrentTarget;
        }

        return null;
    }

    private Transform GetSharedFirePoint()
    {
        return playerAutoShooter != null ? playerAutoShooter.FirePoint : transform;
    }

    public void CalculateMetaTierBonuses(out float damageMultiplier, out float cooldownMultiplier, out float aoeMultiplier)
    {
        damageMultiplier = 1.0f;
        cooldownMultiplier = 1.0f;
        aoeMultiplier = 1.0f;

        int metaTier = 1;
        if (PlayerDataService.LoadChipsetItemData(3, out _, out int savedTier, out _, out _, out _))
        {
            metaTier = Mathf.Clamp(savedTier, 1, 5);
        }

        // Tier 2 (Rare / Blue Frame): ATK +40%
        if (metaTier >= 2)
        {
            damageMultiplier += 0.40f;
        }

        // Tier 3 (Epic / Purple Frame): ATK Speed +40% (Cooldown giảm 40%)
        if (metaTier >= 3)
        {
            cooldownMultiplier -= 0.40f;
        }

        // Tier 4 (Legendary / Yellow Frame): AoE ATK Range +40%
        if (metaTier >= 4)
        {
            aoeMultiplier += 0.40f;
        }

        // Tier 5 (Secret / Red Frame): ATK +180%
        if (metaTier >= 5)
        {
            damageMultiplier += 1.80f;
        }
    }

    public RocketPunchLevelConfig GetCurrentConfig()
    {
        int index = Mathf.Clamp(currentSkillLevel - 1, 0, levelConfigs.Length - 1);
        return levelConfigs[index];
    }

    public float GetCurrentCooldown()
    {
        CalculateMetaTierBonuses(out _, out float cooldownMultiplier, out _);
        float baseCooldown = GetCurrentConfig().cooldown * cooldownMultiplier;
        float finalCooldown = baseCooldown / Mathf.Max(0.1f, attackSpeedMultiplier);
        return Mathf.Max(0.2f, finalCooldown);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, orbitRadius);

    }
}
