using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kỹ năng Spiky Discus (Đĩa Gai - Chipset ID 7).
/// Tạo các đĩa gai bay xoay tròn đều xung quanh Player và tự xoay tít quanh trục của nó (giống Lưỡi Dao Xoay).
/// Quản lý 5 cấp độ in-game (1 -> 3 đĩa, tốc độ 90°/s -> 280°/s, Chảy máu 5 HP/s, Cấp 5 phóng to x2 và chém bay đạn quái)
/// và nhận buff Khung Meta từ PlayerDataService (ID 7) (+1 Đĩa, Spin Speed +30%, +1 Đĩa, Spin Speed +35%).
/// </summary>
public class SpikyDiscusSkill : MonoBehaviour
{
    [System.Serializable]
    public struct DiscusLevelConfig
    {
        public int damage;
        public float orbitSpeed;
        public int discusCount;
        public bool hasBleed;
        public int bleedDps;
        public float bleedDuration;
        public bool isGiantScale;
        public bool destroyEnemyBullets;
    }

    [Header("Skill Status")]
    [Tooltip("Trạng thái mở khóa của kỹ năng.")]
    [SerializeField] private bool isUnlocked = false;

    [Tooltip("Cấp độ kỹ năng hiện tại trong trận đấu (1 -> 5).")]
    [SerializeField, Range(1, 5)] private int currentLevel = 1;

    [Header("5 Level Progression Configuration (Tùy chỉnh trong Inspector)")]
    [SerializeField]
    private DiscusLevelConfig[] levelConfigs = new DiscusLevelConfig[]
    {
        new DiscusLevelConfig { damage = 30, orbitSpeed = 90f, discusCount = 1, hasBleed = false, bleedDps = 0, bleedDuration = 0f, isGiantScale = false, destroyEnemyBullets = false },
        new DiscusLevelConfig { damage = 45, orbitSpeed = 130f, discusCount = 2, hasBleed = false, bleedDps = 0, bleedDuration = 0f, isGiantScale = false, destroyEnemyBullets = false },
        new DiscusLevelConfig { damage = 60, orbitSpeed = 170f, discusCount = 2, hasBleed = true, bleedDps = 5, bleedDuration = 3f, isGiantScale = false, destroyEnemyBullets = false },
        new DiscusLevelConfig { damage = 80, orbitSpeed = 220f, discusCount = 3, hasBleed = true, bleedDps = 5, bleedDuration = 3f, isGiantScale = false, destroyEnemyBullets = false },
        new DiscusLevelConfig { damage = 110, orbitSpeed = 280f, discusCount = 3, hasBleed = true, bleedDps = 5, bleedDuration = 3f, isGiantScale = true, destroyEnemyBullets = true }
    };

    [Header("Orbit Settings")]
    [Tooltip("Bán kính vòng quay quanh Player (mét).")]
    [Range(1.0f, 4.0f)]
    [SerializeField] private float orbitRadius = 1.8f;

    [Tooltip("Tốc độ tự xoay quanh trục của từng đĩa gai (độ/giây).")]
    [Range(120f, 1440f)]
    [SerializeField] private float selfSpinSpeed = 540f;

    [Header("Prefab References")]
    [SerializeField] private GameObject spikyDiscusPrefab;
    [SerializeField] private GameObject hitVfxPrefab;

    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private float currentOrbitAngle = 0f;
    private readonly List<SpikyDiscusProjectile> activeDiscusList = new List<SpikyDiscusProjectile>();

    // Meta Tier Bonuses
    private int metaDiscusCountBonus = 0;
    private float metaSpinSpeedMultiplier = 1.0f;

    public bool IsUnlocked => isUnlocked;
    public int CurrentLevel => currentLevel;
    public int ActiveDiscusCount => activeDiscusList.Count;

    private void Awake()
    {
        playerTransform = transform;
        playerHealth = GetComponent<PlayerHealth>();

        LoadPrefabsIfMissing();
        LoadMetaTierBonuses();
    }

    private void LoadPrefabsIfMissing()
    {
        if (spikyDiscusPrefab == null)
        {
            spikyDiscusPrefab = Resources.Load<GameObject>("Prefabs/Chipset/SpikyDiscus");
#if UNITY_EDITOR
            if (spikyDiscusPrefab == null)
            {
                spikyDiscusPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Chipset/SpikyDiscus.prefab");
            }
#endif
        }

        if (hitVfxPrefab == null)
        {
            hitVfxPrefab = Resources.Load<GameObject>("Prefabs/VFX Boom");
#if UNITY_EDITOR
            if (hitVfxPrefab == null)
            {
                hitVfxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX Boom.prefab");
            }
#endif
        }
    }

    private void Start()
    {
        LoadMetaTierBonuses();
    }

    /// <summary>
    /// Đọc cấp bậc Khung Thẻ Chipset Meta (Chip ID 7) từ PlayerDataService:
    /// - Tier 1 (Magic): Discus ATK: 30, Spin Speed: Normal
    /// - Tier 2 (Rare): +1 Discus
    /// - Tier 3 (Unique): Spin Speed +30%
    /// - Tier 4 (Epic): +1 Discus (Tổng +2 Discus)
    /// - Tier 5 (Holographic): Spin Speed +35% (Tổng +65% Spin Speed)
    /// </summary>
    public void LoadMetaTierBonuses()
    {
        ChipTier tier = PlayerDataService.GetChipTier(7);

        metaDiscusCountBonus = 0;
        metaSpinSpeedMultiplier = 1.0f;

        if (tier >= ChipTier.Rare)
        {
            metaDiscusCountBonus += 1; // +1 Discus
        }
        if (tier >= ChipTier.Unique)
        {
            metaSpinSpeedMultiplier += 0.30f; // Spin Speed +30%
        }
        if (tier >= ChipTier.Epic)
        {
            metaDiscusCountBonus += 1; // +1 Discus (Tổng +2 Discus)
        }
        if (tier == ChipTier.Holographic)
        {
            metaSpinSpeedMultiplier += 0.35f; // Spin Speed +35% (Tổng +65%)
        }
    }

    /// <summary>
    /// Mở khóa hoặc nâng cấp kỹ năng Spiky Discus trong trận đấu (Cấp 1 -> 5).
    /// </summary>
    public void UnlockOrUpgrade(int targetLevel)
    {
        isUnlocked = true;
        currentLevel = Mathf.Clamp(targetLevel, 1, 5);
        LoadMetaTierBonuses();

        SyncDiscusCount();
        UpdateAllDiscusAttributes();

        Debug.Log($"[SpikyDiscusSkill] Đĩa Gai đã lên Cấp {currentLevel}! (Total Discus: {GetTargetDiscusCount()}, Orbit Speed: {GetCalculatedOrbitSpeed():F1}°/s, Dmg: {GetCalculatedDamage()})");
    }

    private void Update()
    {
        if (!isUnlocked) return;
        if (playerHealth != null && playerHealth.IsDead)
        {
            HideAllDiscus();
            return;
        }

        if (activeDiscusList.Count != GetTargetDiscusCount())
        {
            SyncDiscusCount();
            UpdateAllDiscusAttributes();
        }

        UpdateOrbitMotion();
    }

    private void UpdateOrbitMotion()
    {
        float speed = GetCalculatedOrbitSpeed();
        currentOrbitAngle = (currentOrbitAngle + speed * Time.deltaTime) % 360f;

        int count = activeDiscusList.Count;
        if (count == 0) return;

        float angleStep = 360f / count;
        Vector3 playerPos = playerTransform.position;

        for (int i = 0; i < count; i++)
        {
            SpikyDiscusProjectile discus = activeDiscusList[i];
            if (discus == null) continue;

            float currentAngle = currentOrbitAngle + (i * angleStep);
            float rad = currentAngle * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * orbitRadius;
            discus.transform.position = playerPos + offset;
        }
    }

    private void SyncDiscusCount()
    {
        if (spikyDiscusPrefab == null) return;

        int targetCount = GetTargetDiscusCount();

        // 1. Nếu thiếu thì sinh thêm
        while (activeDiscusList.Count < targetCount)
        {
            GameObject obj = null;
            if (PoolManager.Instance != null)
            {
                obj = PoolManager.Instance.Spawn(spikyDiscusPrefab, playerTransform.position, Quaternion.identity);
            }
            else
            {
                obj = Instantiate(spikyDiscusPrefab, playerTransform.position, Quaternion.identity);
            }

            if (obj != null)
            {
                SpikyDiscusProjectile proj = obj.GetComponent<SpikyDiscusProjectile>();
                if (proj == null) proj = obj.AddComponent<SpikyDiscusProjectile>();
                activeDiscusList.Add(proj);
                ChipsetBattleStats.RecordAttack(7, 1);
            }
            else
            {
                break;
            }
        }

        // 2. Nếu thừa thì thu hồi bớt
        while (activeDiscusList.Count > targetCount)
        {
            int lastIndex = activeDiscusList.Count - 1;
            SpikyDiscusProjectile proj = activeDiscusList[lastIndex];
            activeDiscusList.RemoveAt(lastIndex);

            if (proj != null)
            {
                DespawnDiscus(proj.gameObject);
            }
        }
    }

    private void UpdateAllDiscusAttributes()
    {
        int finalDamage = GetCalculatedDamage();
        int bleedDps = currentLevel >= 3 ? 5 : 0;
        float bleedDur = currentLevel >= 3 ? 3.0f : 0f;
        bool destroyBullets = currentLevel >= 5;

        // Cấp 5 (Tối thượng): Đĩa phóng to gấp đôi
        float scaleMultiplier = currentLevel >= 5 ? 2.0f : 1.0f;
        Vector3 baseScale = new Vector3(0.1632f, 0.1632f, 0.1632f) * scaleMultiplier;

        for (int i = 0; i < activeDiscusList.Count; i++)
        {
            SpikyDiscusProjectile discus = activeDiscusList[i];
            if (discus == null) continue;

            discus.transform.localScale = baseScale;
            discus.Setup(finalDamage, bleedDps, bleedDur, destroyBullets, selfSpinSpeed, hitVfxPrefab);
        }
    }

    private void HideAllDiscus()
    {
        for (int i = 0; i < activeDiscusList.Count; i++)
        {
            if (activeDiscusList[i] != null)
            {
                DespawnDiscus(activeDiscusList[i].gameObject);
            }
        }
        activeDiscusList.Clear();
    }

    private void DespawnDiscus(GameObject obj)
    {
        PoolMember member = obj.GetComponent<PoolMember>();
        if (member != null && member.Pool != null)
        {
            member.ReturnToPool();
        }
        else if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnToPool(obj);
        }
        else
        {
            Destroy(obj);
        }
    }

    // --- TÍNH TOÁN CHỈ SỐ THEO CẤP ĐỘ VÀ META ---

    public int GetTargetDiscusCount()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, levelConfigs.Length - 1);
        int inGameCount = levelConfigs[index].discusCount;
        return inGameCount + metaDiscusCountBonus;
    }

    public int GetCalculatedDamage()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, levelConfigs.Length - 1);
        return levelConfigs[index].damage;
    }

    public float GetCalculatedOrbitSpeed()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, levelConfigs.Length - 1);
        float baseSpeed = levelConfigs[index].orbitSpeed;
        return baseSpeed * metaSpinSpeedMultiplier;
    }
}
