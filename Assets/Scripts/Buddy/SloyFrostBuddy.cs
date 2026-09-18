using UnityEngine;

/// <summary>
/// Buddy 1: Sloy (Frost Sentinel) - Sprite: drone-snowflake (ID 1).
/// Bắn đạn pháo băng làm chậm tốc độ di chuyển của quái vật và gây sát thương.
/// </summary>
public class SloyFrostBuddy : BuddyCombatDrone
{
    [Header("Frost Specifics")]
    [Tooltip("Tỷ lệ làm chậm (0.3 = giảm 30% tốc độ chạy).")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float slowPercentage = 0.35f;

    [Tooltip("Thời gian hiệu lực của hiệu ứng làm chậm (giây).")]
    [SerializeField] private float slowDuration = 2.5f;

    [Tooltip("Prefab viên đạn bắn ra (Assets/Prefabs/Projectile.prefab).")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Prefab hiệu ứng trúng đích (Assets/Prefabs/VFX Boom.prefab).")]
    [SerializeField] private GameObject hitVfxPrefab;

    [Tooltip("Tốc độ đạn băng.")]
    [SerializeField] private float projectileSpeed = 12f;

    [SerializeField] private Color frostColor = new Color(0.4f, 0.85f, 1f, 1f);

    public GameObject ProjectilePrefab
    {
        get => projectilePrefab;
        set => projectilePrefab = value;
    }

    public GameObject HitVfxPrefab
    {
        get => hitVfxPrefab;
        set => hitVfxPrefab = value;
    }

    protected override void Awake()
    {
        base.Awake();
        baseDamage = 28;
        attackCooldown = 1.4f;
    }

    protected override void ApplyLevelAndTierScaling()
    {
        base.ApplyLevelAndTierScaling();
        int tierLevel = (int)currentTier;

        // Rare (Tier 2): 42% Slow, Unique+ (Tier 3+): 50% Slow
        if (tierLevel >= 3)
        {
            slowPercentage = 0.50f;
        }
        else if (tierLevel >= 2)
        {
            slowPercentage = 0.42f;
        }
        else
        {
            slowPercentage = 0.35f;
        }
    }

    protected override void ExecuteAttack(EnemyHealth target)
    {
        if (target == null || target.IsDead) return;

        Vector3 spawnPos = FirePoint.position;
        Vector2 direction = ((Vector2)target.transform.position - (Vector2)spawnPos).normalized;

        int tierLevel = (int)currentTier;
        bool isAreaSlow = tierLevel >= 4;
        bool isBlizzardBlast = tierLevel >= 5;
        int aoeDamage = isBlizzardBlast ? Mathf.RoundToInt(baseDamage * 0.30f) : 0;

        if (projectilePrefab != null)
        {
            GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            Projectile proj = projObj.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Setup(baseDamage, projectileSpeed, EffectiveAttackRange);
                proj.SetDirection(direction);
                proj.SetTarget(target.transform);
                proj.IsHoming = true;
            }

            // Đổi màu xanh băng cho đạn để nhận diện chiêu thức
            SpriteRenderer sr = projObj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = frostColor;
            }

            // Gắn component áp dụng hiệu ứng làm chậm khi trúng đích
            FrostHitEffect slowApplier = projObj.AddComponent<FrostHitEffect>();
            slowApplier.Setup(slowPercentage, slowDuration, hitVfxPrefab, isAreaSlow, isBlizzardBlast, aoeDamage, enemyLayer);
        }
        else
        {
            // Dự phòng gây sát thương và làm chậm trực tiếp nếu không có prefab đạn
            target.TakeDamage(baseDamage);
            ApplySlowToEnemy(target.gameObject);
            if (isAreaSlow)
            {
                ApplyAreaSlowAndBlast(target.transform.position, aoeDamage, isBlizzardBlast);
            }
            if (hitVfxPrefab != null)
            {
                Instantiate(hitVfxPrefab, target.transform.position, Quaternion.identity);
            }
        }
    }

    private void ApplyAreaSlowAndBlast(Vector3 center, int aoeDamage, bool isBlizzardBlast)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, 3.0f, enemyLayer.value != 0 ? enemyLayer : (1 << 7));
        if (hits != null)
        {
            System.Collections.Generic.HashSet<EnemyHealth> processed = new System.Collections.Generic.HashSet<EnemyHealth>();
            foreach (var hit in hits)
            {
                if (hit == null) continue;
                EnemyHealth target = hit.GetComponent<EnemyHealth>() ?? hit.GetComponentInParent<EnemyHealth>();
                if (target != null && !target.IsDead && processed.Add(target))
                {
                    ApplySlowToEnemy(target.gameObject);
                    if (isBlizzardBlast && aoeDamage > 0)
                    {
                        target.TakeDamage(aoeDamage);
                    }
                }
            }
        }
    }

    private void ApplySlowToEnemy(GameObject enemyObj)
    {
        if (enemyObj == null) return;
        EnemyMovement em = enemyObj.GetComponent<EnemyMovement>() ?? enemyObj.GetComponentInParent<EnemyMovement>();
        if (em != null)
        {
            em.ApplySlow(slowPercentage, slowDuration);
        }
        else
        {
            Enemy enemy = enemyObj.GetComponent<Enemy>() ?? enemyObj.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.ApplySlow(slowPercentage, slowDuration);
            }
        }
    }

    /// <summary>
    /// Component hỗ trợ kích hoạt hiệu ứng làm chậm khi đạn va chạm quái vật.
    /// </summary>
    private class FrostHitEffect : MonoBehaviour
    {
        private float slowPercent;
        private float duration;
        private GameObject vfx;
        private bool isAreaSlow;
        private bool isBlizzardBlast;
        private int aoeDamage;
        private LayerMask enemyLayer;

        public void Setup(float slowPct, float dur, GameObject vfxPrefab, bool areaSlow, bool blizzardBlast, int blizzardDmg, LayerMask mask)
        {
            slowPercent = slowPct;
            duration = dur;
            vfx = vfxPrefab;
            isAreaSlow = areaSlow;
            isBlizzardBlast = blizzardBlast;
            aoeDamage = blizzardDmg;
            enemyLayer = mask;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            EnemyHealth eh = other.GetComponent<EnemyHealth>() ?? other.GetComponentInParent<EnemyHealth>();
            if (eh != null)
            {
                Vector3 hitPos = eh.transform.position;
                if (isAreaSlow)
                {
                    ApplyAreaSlow(hitPos);
                }
                else
                {
                    EnemyMovement em = eh.GetComponent<EnemyMovement>() ?? eh.GetComponentInParent<EnemyMovement>();
                    if (em != null) em.ApplySlow(slowPercent, duration);
                    if (vfx != null) Instantiate(vfx, hitPos, Quaternion.identity);
                }
            }
        }

        private void ApplyAreaSlow(Vector3 center)
        {
            if (vfx != null) Instantiate(vfx, center, Quaternion.identity);

            Collider2D[] hits = Physics2D.OverlapCircleAll(center, 3.0f, enemyLayer.value != 0 ? enemyLayer : (1 << 7));
            if (hits != null)
            {
                System.Collections.Generic.HashSet<EnemyHealth> processed = new System.Collections.Generic.HashSet<EnemyHealth>();
                foreach (var hit in hits)
                {
                    if (hit == null) continue;
                    EnemyHealth target = hit.GetComponent<EnemyHealth>() ?? hit.GetComponentInParent<EnemyHealth>();
                    if (target != null && !target.IsDead && processed.Add(target))
                    {
                        EnemyMovement em = target.GetComponent<EnemyMovement>() ?? target.GetComponentInParent<EnemyMovement>();
                        if (em != null) em.ApplySlow(slowPercent, duration);

                        if (isBlizzardBlast && aoeDamage > 0)
                        {
                            target.TakeDamage(aoeDamage);
                        }
                    }
                }
            }
        }
    }
}
