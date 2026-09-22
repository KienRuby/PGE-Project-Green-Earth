using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý kỹ năng Lưỡi Dao Xoay (Spinning Blade) bay xoay tròn xung quanh Player:
/// 1. Tốc độ quay chậm rãi, êm ái (`orbitSpeed = 80°/s`, tùy chỉnh từ 10°/s đến 360°/s).
/// 2. Khi xuất hiện cái thứ 1 xoay, cái thứ 2 xuất hiện NGAY SÁT BÊN CẠNH cái thứ 1 và cùng bay song hành.
/// 3. KHÔNG trượt giãn đều ra đối diện, các dao luôn giữ khoảng cách cố định ngay sát cạnh nhau (`bladeSpacingAngle = 25°`).
/// 4. Mọi thông số (tốc độ quay quanh Player, tốc độ tự xoay của dao, khoảng cách góc sát cạnh nhau, bán kính, hồi chiêu)
///    đều có thể chỉnh sửa trực tiếp trên Unity Inspector.
/// </summary>
public class SpinningBladeSkill : MonoBehaviour
{
    [System.Serializable]
    public struct SpinningBladeLevelConfig
    {
        public int damage;
        public float cooldown;
        public int hitsPerBlade;
        public int spawnCountPerWave;
        public int maxBladesOnField;
        public bool hasVortex;
        public float vortexDuration;
    }

    [Header("Prefab References")]
    [Tooltip("Prefab của Lưỡi Dao Xoay (Assets/Prefabs/Chipset/SpinningBlade.prefab).")]
    [SerializeField] private GameObject spinningBladePrefab;

    [Tooltip("Prefab hiệu ứng chém trúng quái (VFX Boom).")]
    [SerializeField] private GameObject hitVfxPrefab;

    [Header("Orbit Clustered Tuning (Bay ngay sát bên cạnh nhau)")]
    [Tooltip("Bán kính vòng quay quanh Player (mét).")]
    [Range(0.5f, 3.0f)]
    [SerializeField] private float orbitRadius = 0.925f;

    [Tooltip("Tốc độ bay xoay vòng quanh Player (độ/giây). Mặc định 80°/s (chậm rãi và dễ nhìn).")]
    [Range(10f, 360f)]
    [SerializeField] private float orbitSpeed = 80f;

    [Tooltip("Khoảng cách góc giữa các lưỡi dao nằm ngay sát cạnh nhau (độ).")]
    [Range(10f, 60f)]
    [SerializeField] private float bladeSpacingAngle = 25f;

    [Tooltip("Tốc độ tự xoay tròn của từng lưỡi dao quanh trục nó (độ/giây).")]
    [Range(120f, 1440f)]
    [SerializeField] private float selfSpinSpeed = 480f;

    [Header("Multipliers (Hệ số nhân)")]
    [Tooltip("Hệ số nhân tốc độ quay.")]
    [Range(0.2f, 3.0f)]
    [SerializeField] private float spinSpeedMultiplier = 1.0f;

    [Tooltip("Hệ số nhân tốc độ hồi dao.")]
    [Range(0.2f, 5.0f)]
    [SerializeField] private float attackSpeedMultiplier = 1.0f;

    [Header("5 Level Progression Configuration")]
    [SerializeField]
    private SpinningBladeLevelConfig[] levelConfigs = new SpinningBladeLevelConfig[]
    {
        // Cấp 1: 36 dmg, CD 3.0s, 1 hit, spawn 1, max 4
        new SpinningBladeLevelConfig { damage = 36, cooldown = 3.0f, hitsPerBlade = 1, spawnCountPerWave = 1, maxBladesOnField = 4, hasVortex = false, vortexDuration = 0f },
        // Cấp 2: 50 dmg, CD 2.5s, 1 hit, spawn 1, max 5 (quay nhanh hơn)
        new SpinningBladeLevelConfig { damage = 50, cooldown = 2.5f, hitsPerBlade = 1, spawnCountPerWave = 1, maxBladesOnField = 5, hasVortex = false, vortexDuration = 0f },
        // Cấp 3: 70 dmg, CD 2.0s, 2 hits (đâm xuyên 2 quái), spawn 1, max 6
        new SpinningBladeLevelConfig { damage = 70, cooldown = 2.0f, hitsPerBlade = 2, spawnCountPerWave = 1, maxBladesOnField = 6, hasVortex = false, vortexDuration = 0f },
        // Cấp 4: 95 dmg, CD 1.5s, 2 hits, spawn 2 dao/lần, max 8
        new SpinningBladeLevelConfig { damage = 95, cooldown = 1.5f, hitsPerBlade = 2, spawnCountPerWave = 2, maxBladesOnField = 8, hasVortex = false, vortexDuration = 0f },
        // Cấp 5 (Tối thượng): 130 dmg, CD 1.0s, 3 hits, spawn 2 dao/lần, max 10, Lốc xoáy 2s khi nổ
        new SpinningBladeLevelConfig { damage = 130, cooldown = 1.0f, hitsPerBlade = 3, spawnCountPerWave = 2, maxBladesOnField = 10, hasVortex = true, vortexDuration = 2.0f }
    };

    [Header("Runtime State (Debug)")]
    [SerializeField] private bool isUnlocked = false;
    [SerializeField] private int currentSkillLevel = 1;
    [SerializeField] private float currentCooldownTimer = 0f;
    [SerializeField] private bool isCooldownActive = false;
    [SerializeField] private float baseOrbitAngle = 0f;

    private readonly List<SpinningBladeProjectile> activeBlades = new List<SpinningBladeProjectile>();

    // Cache state to eliminate GC Alloc & PlayerPrefs string interpolation overhead in Update()
    private int cachedMetaTier = 1;
    private bool isMetaTierCached = false;
    private float cachedCooldownReduction = 0f;
    private float cachedSpeedMultiplier = 1.0f;
    private float cachedGenerationSpeedReduction = 0f;
    private SpikyDiscusSkill cachedDiscusSkill;
    private bool discusSkillCached = false;
    private System.Action<SpinningBladeProjectile> cachedOnBladeDestroyed;

    public bool IsUnlocked => isUnlocked;
    public int CurrentSkillLevel => currentSkillLevel;
    public int ActiveBladeCount => activeBlades.Count;
    public float OrbitRadius => GetEffectiveOrbitRadius();
    public float RawOrbitRadius => orbitRadius;
    public float OrbitSpeed => orbitSpeed;

    /// <summary>
    /// Bán kính quỹ đạo thực tế. Nếu có Đĩa Gai (SpikyDiscusSkill) cùng hoạt động ở vòng trong,
    /// Lưỡi Dao Xoay luôn bay ở vòng ngoài với khoảng cách tối thiểu 0.325m
    /// để 2 chipset không bao giờ bị đè lên nhau.
    /// </summary>
    public float GetEffectiveOrbitRadius()
    {
        float radius = orbitRadius;
        if (!discusSkillCached)
        {
            cachedDiscusSkill = GetComponent<SpikyDiscusSkill>();
            discusSkillCached = true;
        }

        if (cachedDiscusSkill != null && cachedDiscusSkill.IsUnlocked)
        {
            float discusRadius = cachedDiscusSkill.RawOrbitRadius;
            if (radius <= discusRadius + 0.25f)
            {
                radius = discusRadius + 0.325f;
            }
        }
        return radius;
    }

    private void Awake()
    {
        cachedOnBladeDestroyed = OnBladeDestroyed;
        RefreshMetaTier();

        if (levelConfigs != null && levelConfigs.Length > 0 && levelConfigs[0].cooldown < 2.0f)
        {
            if (levelConfigs.Length >= 1) levelConfigs[0].cooldown = 3.0f;
            if (levelConfigs.Length >= 2) levelConfigs[1].cooldown = 2.5f;
            if (levelConfigs.Length >= 3) levelConfigs[2].cooldown = 2.0f;
            if (levelConfigs.Length >= 4) levelConfigs[3].cooldown = 1.5f;
            if (levelConfigs.Length >= 5) levelConfigs[4].cooldown = 1.0f;
        }

#if UNITY_EDITOR
        if (spinningBladePrefab == null)
        {
            spinningBladePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Chipset/SpinningBlade.prefab");
        }
        if (hitVfxPrefab == null)
        {
            hitVfxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX Boom.prefab");
        }
#endif
    }

    /// <summary>
    /// Mở khóa hoặc tăng cấp độ kỹ năng Spinning Blade (1 -> 5).
    /// </summary>
    public void UnlockOrUpgrade(int level)
    {
        currentSkillLevel = Mathf.Clamp(level, 1, 5);
        isUnlocked = true;
        RefreshMetaTier();

        SpinningBladeLevelConfig config = GetCurrentConfig();
        if (activeBlades.Count == 0)
        {
            SpawnBlade();
            if (activeBlades.Count < config.maxBladesOnField)
            {
                isCooldownActive = true;
                currentCooldownTimer = GetCurrentCooldown();
            }
            else
            {
                isCooldownActive = false;
                currentCooldownTimer = 0f;
            }
        }
        else if (activeBlades.Count < config.maxBladesOnField && !isCooldownActive)
        {
            isCooldownActive = true;
            currentCooldownTimer = GetCurrentCooldown();
        }
    }

    private void Update()
    {
        if (!isUnlocked) return;

        // 1. Cập nhật góc quay của lưỡi dao dẫn đầu quanh Player (tốc độ chậm và êm ái)
        CalculateMetaTierBonuses(out _, out float speedBonusMultiplier);
        float currentSpeed = orbitSpeed * spinSpeedMultiplier * speedBonusMultiplier;

        baseOrbitAngle += currentSpeed * Time.deltaTime;
        if (baseOrbitAngle >= 360f) baseOrbitAngle -= 360f;

        // 2. Dọn dẹp các dao bị hủy hoặc chuyển sang chế độ lốc xoáy
        bool bladeRemoved = false;
        for (int i = activeBlades.Count - 1; i >= 0; i--)
        {
            if (activeBlades[i] == null || !activeBlades[i].IsActive || activeBlades[i].IsInVortexMode)
            {
                activeBlades.RemoveAt(i);
                bladeRemoved = true;
            }
        }

        SpinningBladeLevelConfig config = GetCurrentConfig();
        if (bladeRemoved && activeBlades.Count < config.maxBladesOnField && !isCooldownActive)
        {
            isCooldownActive = true;
            currentCooldownTimer = GetCurrentCooldown();
        }

        // 3. TẤT CẢ CÁC DAO LUÔN BAY NGAY SÁT BÊN CẠNH NHAU (Khóa khoảng cách góc bladeSpacingAngle)
        int bladeCount = activeBlades.Count;
        int metaTier = GetMetaTier();
        bool isGuaranteedPierceActive = metaTier >= 5 && ((Time.time % 20f) <= 5f);

        if (bladeCount > 0)
        {
            Vector3 playerPos = transform.position;
            float effectiveRadius = GetEffectiveOrbitRadius();

            for (int i = 0; i < bladeCount; i++)
            {
                SpinningBladeProjectile blade = activeBlades[i];
                if (blade == null) continue;

                blade.SetGuaranteedPierce(isGuaranteedPierceActive);

                // Dao thứ i nằm ngay sát sườn dao trước đó (cách nhau đúng bladeSpacingAngle, KHÔNG trượt tách rời)
                float bladeAngle = baseOrbitAngle - (i * bladeSpacingAngle);
                float rad = bladeAngle * Mathf.Deg2Rad;
                Vector3 bladePos = playerPos + new Vector3(Mathf.Cos(rad) * effectiveRadius, Mathf.Sin(rad) * effectiveRadius, 0f);

                blade.UpdateOrbitPosition(bladePos, bladeAngle);
            }
        }

        // 4. CHỈ đếm ngược hồi chiêu khi số dao trên sân < maxBladesOnField (chấm dứt đếm ngầm khi đang đầy dao)
        if (activeBlades.Count < config.maxBladesOnField)
        {
            if (!isCooldownActive)
            {
                isCooldownActive = true;
                currentCooldownTimer = GetCurrentCooldown();
            }
            else
            {
                currentCooldownTimer -= Time.deltaTime;
                if (currentCooldownTimer <= 0f)
                {
                    for (int wave = 0; wave < config.spawnCountPerWave; wave++)
                    {
                        if (activeBlades.Count < config.maxBladesOnField)
                        {
                            SpawnBlade();
                        }
                    }

                    if (activeBlades.Count < config.maxBladesOnField)
                    {
                        currentCooldownTimer = GetCurrentCooldown();
                    }
                    else
                    {
                        isCooldownActive = false;
                        currentCooldownTimer = 0f;
                    }
                }
            }
        }
        else
        {
            isCooldownActive = false;
            currentCooldownTimer = 0f;
        }
    }

    /// <summary>
    /// Sinh ra lưỡi dao mới xuất hiện ngay sát bên cạnh lưỡi dao đang có.
    /// </summary>
    private void SpawnBlade()
    {
        if (spinningBladePrefab == null) return;

        ChipsetBattleStats.RecordAttack(4, 1);

        int newIndex = activeBlades.Count;
        float bladeAngle = baseOrbitAngle - (newIndex * bladeSpacingAngle);
        float initRad = bladeAngle * Mathf.Deg2Rad;
        float spawnRadius = GetEffectiveOrbitRadius();
        Vector3 spawnPos = transform.position + new Vector3(Mathf.Cos(initRad) * spawnRadius, Mathf.Sin(initRad) * spawnRadius, 0f);
        GameObject bladeObj;

        if (PoolManager.Instance != null)
        {
            bladeObj = PoolManager.Instance.Spawn(spinningBladePrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            bladeObj = Instantiate(spinningBladePrefab, spawnPos, Quaternion.identity);
        }

        if (bladeObj == null) return;

        SpinningBladeProjectile proj = bladeObj.GetComponent<SpinningBladeProjectile>();
        if (proj == null)
        {
            proj = bladeObj.AddComponent<SpinningBladeProjectile>();
        }

        SpinningBladeLevelConfig config = GetCurrentConfig();
        CalculateMetaTierBonuses(out _, out float speedBonusMultiplier);

        int labBonusDmg = 0;
        PlayerStatsManager stats = GetComponent<PlayerStatsManager>();
        if (stats != null) labBonusDmg = stats.BonusDamage;

        int finalDamage = config.damage + labBonusDmg;

        proj.Initialize(
            finalDamage,
            config.hitsPerBlade,
            config.hasVortex,
            config.vortexDuration,
            selfSpinSpeed * speedBonusMultiplier,
            bladeAngle,
            hitVfxPrefab,
            cachedOnBladeDestroyed ?? (cachedOnBladeDestroyed = OnBladeDestroyed)
        );

        int metaTier = GetMetaTier();
        bool isGuaranteedPierceActive = metaTier >= 5 && ((Time.time % 20f) <= 5f);
        proj.SetGuaranteedPierce(isGuaranteedPierceActive);

        activeBlades.Add(proj);
    }

    private void OnBladeDestroyed(SpinningBladeProjectile blade)
    {
        if (activeBlades.Contains(blade))
        {
            activeBlades.Remove(blade);
        }

        SpinningBladeLevelConfig config = GetCurrentConfig();
        if (activeBlades.Count < config.maxBladesOnField && !isCooldownActive)
        {
            isCooldownActive = true;
            currentCooldownTimer = GetCurrentCooldown();
        }
    }

    public int GetMetaTier()
    {
        if (!isMetaTierCached)
        {
            RefreshMetaTier();
        }
        return cachedMetaTier;
    }

    public void RefreshMetaTier()
    {
        int metaTier = 1;
        if (PlayerDataService.LoadChipsetItemData(4, out _, out int savedTier, out _, out _, out _))
        {
            metaTier = Mathf.Clamp(savedTier, 1, 5);
        }
        cachedMetaTier = metaTier;
        isMetaTierCached = true;

        cachedCooldownReduction = 0f;
        cachedSpeedMultiplier = 1.0f;
        cachedGenerationSpeedReduction = 0f;

        // Tier 2 (Rare): ATK Speed +9%
        if (cachedMetaTier >= 2)
        {
            cachedCooldownReduction += 0.09f;
        }

        // Tier 3 (Unique): ATK Speed +18%, Generation speed -30%
        if (cachedMetaTier >= 3)
        {
            cachedCooldownReduction += 0.18f;
            cachedGenerationSpeedReduction += 0.30f;
        }

        // Tier 4 (Epic): Spin Speed +36%
        if (cachedMetaTier >= 4)
        {
            cachedSpeedMultiplier += 0.36f;
        }

        // Tier 5 (Holo): ATK Speed +36%, Generation speed -36%, Guaranteed Pierce (5s, CD 15s)
        if (cachedMetaTier >= 5)
        {
            cachedCooldownReduction += 0.36f;
            cachedGenerationSpeedReduction += 0.36f;
        }
    }

    public void CalculateMetaTierBonuses(out float cooldownReduction, out float speedMultiplier)
    {
        CalculateMetaTierBonuses(out cooldownReduction, out speedMultiplier, out _);
    }

    public void CalculateMetaTierBonuses(out float cooldownReduction, out float speedMultiplier, out float generationSpeedReduction)
    {
        if (!isMetaTierCached)
        {
            RefreshMetaTier();
        }

        cooldownReduction = cachedCooldownReduction;
        speedMultiplier = cachedSpeedMultiplier;
        generationSpeedReduction = cachedGenerationSpeedReduction;
    }

    public SpinningBladeLevelConfig GetCurrentConfig()
    {
        int index = Mathf.Clamp(currentSkillLevel - 1, 0, levelConfigs.Length - 1);
        return levelConfigs[index];
    }

    public float GetCurrentCooldown()
    {
        CalculateMetaTierBonuses(out float cooldownReduction, out _, out float generationSpeedReduction);
        float totalReduction = Mathf.Clamp(cooldownReduction + generationSpeedReduction, 0f, 0.85f);
        float baseCooldown = GetCurrentConfig().cooldown * (1.0f - totalReduction);
        float finalCooldown = baseCooldown / Mathf.Max(0.1f, attackSpeedMultiplier);
        return Mathf.Max(0.15f, finalCooldown);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, GetEffectiveOrbitRadius());
    }
}
