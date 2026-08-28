using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kỹ năng High-Explosive Mine (Mìn Nổ - Chipset ID 10).
/// Định kỳ đặt các quả mìn nổ mạnh mẽ trên mặt đất theo đường di chuyển của Player.
/// Quản lý 5 cấp độ in-game (Sát thương AoE, Làm chậm, Mìn mẹ văng 3 mìn con) và nhận buff Khung Meta từ PlayerDataService (ID 10).
/// </summary>
public class HighExplosiveMineSkill : MonoBehaviour
{
    [System.Serializable]
    public struct MineLevelConfig
    {
        public int damage;
        public float cooldown;
        public float explosionRadius;
        public bool hasSlow;
        public bool hasSubMines;
    }

    [Header("Skill Status")]
    [Tooltip("Trạng thái mở khóa của kỹ năng.")]
    [SerializeField] private bool isUnlocked = false;

    [Tooltip("Cấp độ kỹ năng hiện tại trong trận đấu (1 -> 5).")]
    [SerializeField, Range(1, 5)] private int currentLevel = 1;

    [Header("5 Level Progression Configuration (Tùy chỉnh trong Inspector)")]
    [SerializeField]
    private MineLevelConfig[] levelConfigs = new MineLevelConfig[]
    {
        new MineLevelConfig { damage = 30, cooldown = 6.0f, explosionRadius = 2.0f, hasSlow = false, hasSubMines = false },
        new MineLevelConfig { damage = 45, cooldown = 5.0f, explosionRadius = 2.5f, hasSlow = false, hasSubMines = false },
        new MineLevelConfig { damage = 65, cooldown = 4.0f, explosionRadius = 2.5f, hasSlow = true, hasSubMines = false },
        new MineLevelConfig { damage = 90, cooldown = 3.0f, explosionRadius = 3.5f, hasSlow = true, hasSubMines = false },
        new MineLevelConfig { damage = 145, cooldown = 2.5f, explosionRadius = 4.0f, hasSlow = true, hasSubMines = true }
    };

    [Header("Prefab References")]
    [SerializeField] private GameObject minePrefab;
    [SerializeField] private GameObject explosionVfxPrefab;

    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private float nextDropTime;

    // Meta Tier Bonuses
    private float metaDamageMultiplier = 1.0f;
    private float metaCooldownMultiplier = 1.0f;

    public bool IsUnlocked => isUnlocked;
    public int CurrentLevel => currentLevel;

    private void Awake()
    {
        playerTransform = transform;
        playerHealth = GetComponent<PlayerHealth>();

        LoadPrefabsIfMissing();
        LoadMetaTierBonuses();
    }

    private void LoadPrefabsIfMissing()
    {
        if (minePrefab == null)
        {
            minePrefab = Resources.Load<GameObject>("Prefabs/Chipset/HighExplosiveMine");
#if UNITY_EDITOR
            if (minePrefab == null)
            {
                minePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Chipset/HighExplosiveMine.prefab");
            }
#endif
        }

        if (explosionVfxPrefab == null)
        {
            explosionVfxPrefab = Resources.Load<GameObject>("Prefabs/VFX Boom");
#if UNITY_EDITOR
            if (explosionVfxPrefab == null)
            {
                explosionVfxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX Boom.prefab");
            }
#endif
        }
    }

    private void Start()
    {
        LoadMetaTierBonuses();
    }

    /// <summary>
    /// Đọc cấp bậc Khung Thẻ Chipset Meta (Chip ID 10) từ PlayerDataService:
    /// - Tier 1 (Magic): Mine AoE ATK: 27, Cooldown: 5.55s.
    /// - Tier 2 (Rare): ATK +20%
    /// - Tier 3 (Unique): Cooldown -20%
    /// - Tier 4 (Epic): ATK +55%
    /// - Tier 5 (Holographic): ATK +144%
    /// </summary>
    public void LoadMetaTierBonuses()
    {
        ChipTier tier = PlayerDataService.GetChipTier(10);

        metaDamageMultiplier = 1.0f;
        metaCooldownMultiplier = 1.0f;

        if (tier >= ChipTier.Rare)
        {
            metaDamageMultiplier += 0.20f; // ATK +20%
        }
        if (tier >= ChipTier.Unique)
        {
            metaCooldownMultiplier *= 0.80f; // Cooldown -20%
        }
        if (tier >= ChipTier.Epic)
        {
            metaDamageMultiplier += 0.55f; // ATK +55%
        }
        if (tier == ChipTier.Holographic)
        {
            metaDamageMultiplier += 1.44f; // ATK +144%
        }
    }

    /// <summary>
    /// Mở khóa hoặc nâng cấp kỹ năng High-Explosive Mine trong trận đấu (Cấp 1 -> 5).
    /// </summary>
    public void UnlockOrUpgrade(int targetLevel)
    {
        bool wasUnlocked = isUnlocked;
        isUnlocked = true;
        currentLevel = Mathf.Clamp(targetLevel, 1, 5);
        LoadMetaTierBonuses();

        if (!wasUnlocked)
        {
            DropMineAtPlayerPosition();
            nextDropTime = Time.time + GetCalculatedCooldown();
        }

        Debug.Log($"[HighExplosiveMineSkill] Mìn Nổ đã lên Cấp {currentLevel}! (Dmg: {GetCalculatedDamage()}, CD: {GetCalculatedCooldown():F2}s, Radius: {GetExplosionRadius():F1}m)");
    }

    private void Update()
    {
        if (!isUnlocked) return;
        if (playerHealth != null && playerHealth.IsDead) return;

        if (Time.time >= nextDropTime)
        {
            DropMineAtPlayerPosition();
            nextDropTime = Time.time + GetCalculatedCooldown();
        }
    }

    private void DropMineAtPlayerPosition()
    {
        if (minePrefab == null) return;

        ChipsetBattleStats.RecordAttack(10, 1);

        Vector3 dropPos = playerTransform.position;
        GameObject mineObj = null;

        if (PoolManager.Instance != null)
        {
            mineObj = PoolManager.Instance.Spawn(minePrefab, dropPos, Quaternion.identity);
        }
        else
        {
            mineObj = Instantiate(minePrefab, dropPos, Quaternion.identity);
        }

        if (mineObj != null)
        {
            HighExplosiveMine mineScript = mineObj.GetComponent<HighExplosiveMine>();
            if (mineScript == null)
            {
                mineScript = mineObj.AddComponent<HighExplosiveMine>();
            }

            int finalDamage = GetCalculatedDamage();
            float radius = GetExplosionRadius();
            float slowDur = currentLevel >= 3 ? 2.0f : 0f;
            bool spawnSubMines = currentLevel >= 5;

            mineScript.Setup(
                finalDamage,
                radius,
                slowDur,
                spawnSubMines,
                explosionVfxPrefab,
                minePrefab
            );
        }
    }

    // --- TÍNH TOÁN CHỈ SỐ THEO CẤP ĐỘ VÀ META ---

    public int GetCalculatedDamage()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, levelConfigs.Length - 1);
        int baseDmg = levelConfigs[index].damage;
        return Mathf.RoundToInt(baseDmg * metaDamageMultiplier);
    }

    public float GetCalculatedCooldown()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, levelConfigs.Length - 1);
        float baseCooldown = levelConfigs[index].cooldown;
        return Mathf.Max(0.5f, baseCooldown * metaCooldownMultiplier);
    }

    public float GetExplosionRadius()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, levelConfigs.Length - 1);
        return levelConfigs[index].explosionRadius;
    }
}
