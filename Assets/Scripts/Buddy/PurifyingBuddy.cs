using UnityEngine;

/// <summary>
/// Buddy 3: Purifying Drone - Sprite: drone-stealth-wing (ID 10).
/// Phi thuyền tàng hình bay hộ tống, bắn xung lượng tử và định kỳ phát
/// sóng thanh tẩy diện rộng (Purification Pulse) gây sát thương và đẩy lùi (Knockback) quái vật.
/// </summary>
public class PurifyingBuddy : BuddyCombatDrone
{
    [Header("Purifying Specifics")]
    [Tooltip("Bán kính sóng thanh tẩy diện rộng (mét).")]
    [SerializeField] private float pulseRadius = 3.8f;

    [Tooltip("Khoảng thời gian kích hoạt sóng thanh tẩy (giây).")]
    [SerializeField] private float pulseInterval = 3.5f;

    [Tooltip("Lực đẩy lùi kẻ thù trong phạm vi sóng.")]
    [SerializeField] private float knockbackForce = 4.0f;

    [Tooltip("Prefab viên đạn bắn ra.")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Prefab hiệu ứng sóng thanh tẩy.")]
    [SerializeField] private GameObject pulseVfxPrefab;

    [SerializeField] private Color stealthWingColor = new Color(0.1f, 0.7f, 1f, 1f);

    private float pulseTimer;

    public GameObject ProjectilePrefab
    {
        get => projectilePrefab;
        set => projectilePrefab = value;
    }

    public GameObject PulseVfxPrefab
    {
        get => pulseVfxPrefab;
        set => pulseVfxPrefab = value;
    }

    protected override void Awake()
    {
        base.Awake();
        baseDamage = 26;
        attackCooldown = 1.1f;
        pulseTimer = pulseInterval;
    }

    protected override void Update()
    {
        base.Update();
        UpdatePurificationPulse(Time.deltaTime);
    }

    private void UpdatePurificationPulse(float deltaTime)
    {
        pulseTimer -= deltaTime;
        if (pulseTimer <= 0f)
        {
            pulseTimer = pulseInterval;
            TriggerPurificationPulse();
        }
    }

    private void TriggerPurificationPulse()
    {
        Vector3 pulseCenter = playerTransform != null ? playerTransform.position : transform.position;

        // Quét quái vật xung quanh
        Collider2D[] colliders = Physics2D.OverlapCircleAll(pulseCenter, pulseRadius, enemyLayer);
        int pulseDamage = Mathf.RoundToInt(baseDamage * 1.25f);

        if (colliders != null && colliders.Length > 0)
        {
            foreach (var col in colliders)
            {
                if (col == null) continue;
                EnemyHealth eh = col.GetComponent<EnemyHealth>() ?? col.GetComponentInParent<EnemyHealth>();
                if (eh != null && !eh.IsDead)
                {
                    eh.TakeDamage(pulseDamage);

                    // Hiệu ứng đẩy lùi
                    Rigidbody2D enemyRb = col.GetComponent<Rigidbody2D>() ?? col.GetComponentInParent<Rigidbody2D>();
                    if (enemyRb != null && enemyRb.bodyType == RigidbodyType2D.Dynamic)
                    {
                        Vector2 pushDir = ((Vector2)col.transform.position - (Vector2)pulseCenter).normalized;
                        enemyRb.AddForce(pushDir * knockbackForce, ForceMode2D.Impulse);
                    }
                }
            }
        }

        if (pulseVfxPrefab != null)
        {
            Instantiate(pulseVfxPrefab, pulseCenter, Quaternion.identity);
        }
    }

    protected override void ExecuteAttack(EnemyHealth target)
    {
        if (target == null || target.IsDead) return;

        Vector3 spawnPos = FirePoint.position;
        Vector2 direction = ((Vector2)target.transform.position - (Vector2)spawnPos).normalized;

        if (projectilePrefab != null)
        {
            GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            Projectile proj = projObj.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Setup(baseDamage, 14f, targetDetectionRadius * 1.5f);
                proj.SetDirection(direction);
                proj.SetTarget(target.transform);
                proj.IsHoming = true;
            }

            SpriteRenderer sr = projObj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = stealthWingColor;
            }
        }
        else
        {
            target.TakeDamage(baseDamage);
        }
    }
}
