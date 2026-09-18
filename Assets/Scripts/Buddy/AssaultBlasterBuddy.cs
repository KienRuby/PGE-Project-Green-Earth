using System.Collections;
using UnityEngine;

/// <summary>
/// Buddy 5: Assault Blaster - Sprite: drone-cross-visor (ID 4).
/// Drone cánh quạt 3 chấu liên tục xoay nòng và xả chùm đạn đôi (Twin Blaster)
/// tốc độ cao với hỏa lực áp đảo kẻ thù.
/// </summary>
public class AssaultBlasterBuddy : BuddyCombatDrone
{
    [Header("Assault Blaster Specifics")]
    [Tooltip("Tốc độ tự xoay của cánh quạt 3 chấu (độ/giây).")]
    [SerializeField] private float propellerSpinSpeed = 360f;

    [Tooltip("Độ trễ giữa 2 viên đạn trong loạt bắn đôi (giây).")]
    [SerializeField] private float twinShotDelay = 0.08f;

    [Tooltip("Prefab viên đạn bắn ra.")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Tốc độ đạn.")]
    [SerializeField] private float projectileSpeed = 16f;

    [SerializeField] private Color blasterColor = new Color(1f, 0.4f, 0.1f, 1f);

    public GameObject ProjectilePrefab
    {
        get => projectilePrefab;
        set => projectilePrefab = value;
    }

    private int burstCount = 0;

    protected override void Awake()
    {
        base.Awake();
        baseDamage = 20;
        attackCooldown = 0.75f; // Tốc độ bắn nhanh
    }

    protected override void ApplyLevelAndTierScaling()
    {
        base.ApplyLevelAndTierScaling();
        int tierLevel = (int)currentTier;

        // Rare (Tier 2): Blaster ATK +20%
        if (tierLevel >= 2)
        {
            baseDamage = Mathf.RoundToInt(baseDamage * 1.20f);
        }

        // Unique (Tier 3): Fire Rate +30%
        if (tierLevel >= 3)
        {
            attackCooldown = Mathf.Max(0.18f, attackCooldown * 0.70f);
        }
    }

    protected override void Update()
    {
        base.Update();

        // Tự xoay cánh quạt 3 chấu quanh trục
        if (spriteRenderer != null)
        {
            spriteRenderer.transform.Rotate(0f, 0f, -propellerSpinSpeed * Time.deltaTime);
        }
    }

    protected override void ExecuteAttack(EnemyHealth target)
    {
        if (target == null || target.IsDead) return;
        StartCoroutine(FireTwinBurstRoutine(target));
    }

    private IEnumerator FireTwinBurstRoutine(EnemyHealth target)
    {
        if (target == null || target.IsDead) yield break;

        burstCount++;
        // Phát đạn 1 (Lệch nhẹ sang trái)
        FireSingleBlasterShot(target, -0.15f, 1.0f);

        // Holo (Tier 5): Overheat Surge (+30% Rapid Fire) - every 4 bursts delay is halved
        float currentDelay = (currentTier >= BuddyTier.Holographic && (burstCount % 4 == 0)) ? (twinShotDelay * 0.5f) : twinShotDelay;
        yield return new WaitForSeconds(currentDelay);

        if (target != null && !target.IsDead)
        {
            // Epic (Tier 4): Dual Shot 2nd Pellet ATK +30%
            float secondShotMultiplier = currentTier >= BuddyTier.Epic ? 1.30f : 1.0f;
            FireSingleBlasterShot(target, 0.15f, secondShotMultiplier);
        }
    }

    private void FireSingleBlasterShot(EnemyHealth target, float lateralOffset, float damageMultiplier = 1.0f)
    {
        Vector3 basePos = FirePoint.position;
        Vector2 dir = ((Vector2)target.transform.position - (Vector2)basePos).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x);
        Vector3 spawnPos = basePos + (Vector3)(normal * lateralOffset);
        int finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);

        if (projectilePrefab != null)
        {
            GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            Projectile proj = projObj.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Setup(finalDamage, projectileSpeed, EffectiveAttackRange);
                proj.SetDirection(dir);
                proj.SetTarget(target.transform);
                proj.IsHoming = true;
            }

            SpriteRenderer sr = projObj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = blasterColor;
            }
        }
        else
        {
            target.TakeDamage(finalDamage);
        }
    }
}
