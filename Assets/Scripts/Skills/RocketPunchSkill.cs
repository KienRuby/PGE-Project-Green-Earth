using UnityEngine;

/// <summary>
/// Hiện súng Rocket Punch trên vai khi chọn chipset và bắn nắm đấm từ đầu nòng.
/// Sát thương, thời gian hồi chiêu và khả năng bám mục tiêu phụ thuộc cấp kỹ năng.
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

    [Header("Shoulder Launcher")]
    [SerializeField] private Transform shoulderGunPivot;
    [SerializeField] private Transform punchMuzzle;
    [Tooltip("Sprite khẩu súng phóng tên lửa trên vai.")]
    [SerializeField] private Sprite shoulderGunSprite;

    [Header("Orbit Tuning (Tùy chỉnh bay quanh Player)")]
    [Tooltip("Bán kính vòng quay xung quanh Player (mét).")]
    [Range(0.2f, 3.5f)]
    [SerializeField] private float orbitRadius = 0.8f;

    [Tooltip("Tốc độ bay xoay vòng quanh Player (độ/giây). Càng nhỏ càng bay chậm.")]
    [Range(60f, 720f)]
    [SerializeField] private float orbitSpeed = 220f;

    [Header("Launch Tuning (Tùy chỉnh khi phóng tới quái)")]
    [Tooltip("Vận tốc bay thẳng khi lao tới quái vật (mét/giây).")]
    [Range(4f, 40f)]
    [SerializeField] private float launchSpeed = 5.0f;

    [Tooltip("Hệ số nhân tốc độ đấm (Attack Speed Multiplier).")]
    [Range(0.2f, 5.0f)]
    [SerializeField] private float attackSpeedMultiplier = 1.0f;

    [Header("5 Level Progression Configuration")]
    [SerializeField]
    private RocketPunchLevelConfig[] levelConfigs = new RocketPunchLevelConfig[]
    {
        // Cấp 1: 70 dmg, CD 2.0s, AoE 1.25m (giảm 1/2 từ 2.5m)
        new RocketPunchLevelConfig { directDamage = 70, aoeDamage = 37, cooldown = 2.0f, aoeRadius = 1.25f, hasStun = false, stunDuration = 0f, hasLavaPool = false },
        // Cấp 2: 100 dmg, CD 1.7s, AoE 1.5m (giảm 1/2 từ 3.0m)
        new RocketPunchLevelConfig { directDamage = 100, aoeDamage = 55, cooldown = 1.7f, aoeRadius = 1.5f, hasStun = false, stunDuration = 0f, hasLavaPool = false },
        // Cấp 3: 140 dmg, CD 1.4s, AoE 2.0m (giảm 1/2 từ 4.0m)
        new RocketPunchLevelConfig { directDamage = 140, aoeDamage = 80, cooldown = 1.4f, aoeRadius = 2.0f, hasStun = false, stunDuration = 0f, hasLavaPool = false },
        // Cấp 4: 190 dmg, CD 1.1s, AoE 2.0m, Stun 1.0s (giảm 1/2 từ 4.0m)
        new RocketPunchLevelConfig { directDamage = 190, aoeDamage = 115, cooldown = 1.1f, aoeRadius = 2.0f, hasStun = true, stunDuration = 1.0f, hasLavaPool = false },
        // Cấp 5 (Tối thượng): 260 dmg, CD 0.8s, AoE 2.5m, Stun 1.0s (giảm 1/2 từ 5.0m)
        new RocketPunchLevelConfig { directDamage = 260, aoeDamage = 160, cooldown = 0.8f, aoeRadius = 2.5f, hasStun = true, stunDuration = 1.0f, hasLavaPool = false }
    };

    [Header("Runtime State (Debug)")]
    [SerializeField] private bool isUnlocked = false;
    [SerializeField] private int currentSkillLevel = 1;
    [SerializeField] private float currentCooldownTimer = 0f;
    private PlayerAutoShooter playerAutoShooter;

    public bool IsUnlocked => isUnlocked;
    public int CurrentSkillLevel => currentSkillLevel;
    public float OrbitRadius => orbitRadius;
    public float OrbitSpeed => orbitSpeed;
    public float LaunchSpeed => Mathf.Max(4.0f, launchSpeed);

    private void Awake()
    {
        playerAutoShooter = GetComponent<PlayerAutoShooter>();
        EnsureShoulderGunReferences();
        if (shoulderGunPivot != null)
        {
            shoulderGunPivot.gameObject.SetActive(isUnlocked);
        }
        if (rocketPunchPrefab == null)
        {
            rocketPunchPrefab = Resources.Load<GameObject>("Prefabs/Chipset/RocketPunch");
        }
        if (explosionVfxPrefab == null)
        {
            explosionVfxPrefab = Resources.Load<GameObject>("Prefabs/VFX Boom");
        }
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

        if (levelConfigs != null)
        {
            for (int i = 0; i < levelConfigs.Length; i++)
            {
                levelConfigs[i].hasLavaPool = false;
            }
        }
    }

    /// <summary>
    /// Mở khóa hoặc tăng cấp độ kỹ năng Rocket Punch (1 -> 5).
    /// </summary>
    public void UnlockOrUpgrade(int level)
    {
        currentSkillLevel = Mathf.Clamp(level, 1, 5);
        isUnlocked = true;
        EnsureShoulderGunReferences();
        if (shoulderGunPivot != null)
        {
            shoulderGunPivot.gameObject.SetActive(true);
        }
    }

    private void LateUpdate()
    {
        if (!isUnlocked) return;

        if (shoulderGunPivot == null)
        {
            EnsureShoulderGunReferences();
        }

        if (shoulderGunPivot != null)
        {
            if (!shoulderGunPivot.gameObject.activeSelf)
            {
                shoulderGunPivot.gameObject.SetActive(true);
            }
        }

        currentCooldownTimer -= Time.deltaTime;
        if (currentCooldownTimer > 0f) return;

        Transform target = FindTargetEnemy();
        if (target == null) return;

        FirePunch(target);
        currentCooldownTimer = GetCurrentCooldown();
    }

    private void FirePunch(Transform target)
    {
        if (rocketPunchPrefab == null) return;

        Transform muzzle = GetPunchMuzzle();
        Vector3 spawnPos = muzzle.position;
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
            LaunchSpeed,
            orbitRadius,
            orbitSpeed,
            config.hasStun,
            config.stunDuration,
            config.hasLavaPool,
            explosionVfxPrefab,
            lavaHazardPrefab,
            0f,
            null
        );
        proj.SetSharedTargetProvider(playerAutoShooter);
        EnemyHealth enemyHealth = target.GetComponentInParent<EnemyHealth>();
        Vector2 aimPoint = enemyHealth != null ? enemyHealth.AimPoint : (Vector2)target.position;
        Vector2 launchDirection = (aimPoint - (Vector2)spawnPos).normalized;
        proj.LaunchFromMuzzle(target, spawnPos, launchDirection);
        ChipsetBattleStats.RecordAttack(3, 1);
        AudioManager.Instance?.PlaySFX(SoundIdConst.SFX_PUNCH_SPAWN);
    }

    /// <summary>
    /// Phạm vi phóng của Rocket Punch khớp với tầm bắn của Player.
    /// </summary>
    public float EffectiveLaunchRange
    {
        get
        {
            if (playerAutoShooter != null)
            {
                return playerAutoShooter.SharedAttackRange;
            }
            return 12.0f;
        }
    }

    private Transform FindTargetEnemy()
    {
        if (playerAutoShooter != null && playerAutoShooter.CurrentTarget != null)
        {
            float dist = Vector2.Distance(GetPunchMuzzle().position, playerAutoShooter.CurrentTarget.position);
            if (dist <= EffectiveLaunchRange)
            {
                return playerAutoShooter.CurrentTarget;
            }
        }

        return null;
    }

    private Transform GetPunchMuzzle()
    {
        if (punchMuzzle != null) return punchMuzzle;
        if (shoulderGunPivot != null)
        {
            punchMuzzle = shoulderGunPivot.Find("GunPunchVisual/PunchMuzzle") ?? shoulderGunPivot.Find("PunchMuzzle");
            if (punchMuzzle != null) return punchMuzzle;
        }
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

    /// <summary>
    /// Tự động dò tìm hoặc tái tạo ShoulderGunPivot và PunchMuzzle nếu trên Player trong scene bị thiếu hoặc null.
    /// </summary>
    public void EnsureShoulderGunReferences()
    {
        Transform root = transform;
        Transform body = root.Find("thân") ?? root;

        // 1. Dò tìm ShoulderGunPivot đã có trong hierarchy của Player
        if (shoulderGunPivot == null)
        {
            shoulderGunPivot = body.Find("ShoulderGunPivot") ?? root.Find("ShoulderGunPivot");
            if (shoulderGunPivot == null)
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "ShoulderGunPivot")
                    {
                        shoulderGunPivot = t;
                        break;
                    }
                }
            }
        }

        // 2. Nếu hierarchy chưa có và có xương thân, tự động clone từ Prefab hoặc tạo mới
        if (shoulderGunPivot == null && body != null && body != root)
        {
            shoulderGunPivot = CreateOrCloneShoulderGun(body);
        }

        if (shoulderGunPivot != null)
        {
            shoulderGunPivot.gameObject.SetActive(isUnlocked);

            if (body != root && shoulderGunPivot.parent != body)
            {
                shoulderGunPivot.SetParent(body, false);
                shoulderGunPivot.localPosition = new Vector3(-4f, 7.5f, 0f);
            }

            // 3. Dò tìm PunchMuzzle
            if (punchMuzzle == null)
            {
                punchMuzzle = shoulderGunPivot.Find("GunPunchVisual/PunchMuzzle") ?? shoulderGunPivot.Find("PunchMuzzle");
                if (punchMuzzle == null)
                {
                    foreach (Transform t in shoulderGunPivot.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "PunchMuzzle")
                        {
                            punchMuzzle = t;
                            break;
                        }
                    }
                }
            }

            // 4. Đảm bảo SpriteRenderer của GunPunchVisual hiển thị đúng
            EnsureVisualRenderer();
        }
    }

    private Transform CreateOrCloneShoulderGun(Transform parentBody)
    {
        // 1. Thử load từ Prefab trong Resources
        GameObject pivotPrefab = Resources.Load<GameObject>("Prefabs/Chipset/ShoulderGunPivot");
#if UNITY_EDITOR
        if (pivotPrefab == null)
        {
            pivotPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Chipset/ShoulderGunPivot.prefab");
        }
        if (pivotPrefab == null)
        {
            GameObject playerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab != null)
            {
                Transform srcPivot = playerPrefab.transform.Find("thân/ShoulderGunPivot") ?? playerPrefab.transform.Find("ShoulderGunPivot");
                if (srcPivot != null)
                {
                    pivotPrefab = srcPivot.gameObject;
                }
            }
        }
#endif
        if (pivotPrefab != null)
        {
            GameObject clone = Instantiate(pivotPrefab, parentBody);
            clone.name = "ShoulderGunPivot";
            clone.transform.localPosition = new Vector3(-4f, 7.5f, 0f);
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one;
            return clone.transform;
        }

        // 2. Fallback tạo bằng code nếu không tìm thấy prefab
        GameObject newPivot = new GameObject("ShoulderGunPivot");
        newPivot.transform.SetParent(parentBody, false);
        newPivot.transform.localPosition = new Vector3(-4f, 7.5f, 0f);
        newPivot.transform.localRotation = Quaternion.identity;
        newPivot.transform.localScale = Vector3.one;

        GameObject visualObj = new GameObject("GunPunchVisual");
        visualObj.transform.SetParent(newPivot.transform, false);
        visualObj.transform.localPosition = Vector3.zero;
        visualObj.transform.localRotation = Quaternion.identity;
        visualObj.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        SpriteRenderer sr = visualObj.AddComponent<SpriteRenderer>();
        sr.sprite = GetShoulderGunSprite();
        sr.sharedMaterial = GetDefaultSpriteMaterial();
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 3;

        GameObject muzzleObj = new GameObject("PunchMuzzle");
        muzzleObj.transform.SetParent(visualObj.transform, false);
        muzzleObj.transform.localPosition = new Vector3(13.15f, 4.4f, 0f);
        muzzleObj.transform.localRotation = Quaternion.identity;
        muzzleObj.transform.localScale = Vector3.one;

        return newPivot.transform;
    }

    private void EnsureVisualRenderer()
    {
        if (shoulderGunPivot == null) return;

        Transform visual = shoulderGunPivot.Find("GunPunchVisual");
        if (visual == null)
        {
            foreach (Transform t in shoulderGunPivot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "GunPunchVisual")
                {
                    visual = t;
                    break;
                }
            }
        }

        if (visual == null)
        {
            GameObject visualObj = new GameObject("GunPunchVisual");
            visualObj.transform.SetParent(shoulderGunPivot, false);
            visualObj.transform.localPosition = Vector3.zero;
            visualObj.transform.localRotation = Quaternion.identity;
            visualObj.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            visual = visualObj.transform;
        }

        SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = visual.gameObject.AddComponent<SpriteRenderer>();
        }

        if (sr.sprite == null)
        {
            sr.sprite = GetShoulderGunSprite();
        }

        if (sr.sharedMaterial == null || sr.sharedMaterial.shader == null || sr.sharedMaterial.shader.name == "Hidden/InternalErrorShader")
        {
            sr.sharedMaterial = GetDefaultSpriteMaterial();
        }

        if (string.IsNullOrEmpty(sr.sortingLayerName) || sr.sortingLayerName == "Default" || sr.sortingLayerName == "Bullet")
        {
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 3;
        }

        sr.enabled = true;
        visual.gameObject.SetActive(true);
    }

    private Sprite GetShoulderGunSprite()
    {
        if (shoulderGunSprite != null) return shoulderGunSprite;
#if UNITY_EDITOR
        shoulderGunSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Character/súng phóng tên lửa bàn tay.png");
#endif
        if (shoulderGunSprite == null)
        {
            shoulderGunSprite = Resources.Load<Sprite>("Sprites/Character/súng phóng tên lửa bàn tay");
        }
        return shoulderGunSprite;
    }

    private Material GetDefaultSpriteMaterial()
    {
#if UNITY_EDITOR
        Material defaultMat = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        if (defaultMat != null) return defaultMat;
#endif
        Shader spriteShader = Shader.Find("Sprites/Default");
        return spriteShader != null ? new Material(spriteShader) : null;
    }
}
