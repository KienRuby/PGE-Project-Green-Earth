using UnityEngine;

public class PlayerAutoShooter : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private LayerMask enemyLayer;

    [Tooltip("Phạm vi Player có thể phát hiện Enemy.")]
    [SerializeField] private float detectionRadius = 15f;

    [Tooltip("Bao lâu tìm lại Enemy gần nhất một lần.")]
    [SerializeField] private float targetRefreshRate = 0.1f;

    [Header("Attack Point")]
    [SerializeField] private Transform attackPoint;

    [Tooltip("Khoảng cách AttackPoint tính từ tâm Player.")]
    [SerializeField] private float attackPointDistance = 0.6f;

    [Header("Shooting")]
    [SerializeField] private GameObject projectilePrefab;

    [SerializeField] private float fireRate = 2f;

    private Transform currentTarget;

    private float nextFireTime;
    private float targetSearchTimer;

    private void Update()
    {
        UpdateTarget();

        UpdateAttackPoint();

        AutoShoot();
    }

    // =====================================================
    // TÌM ENEMY GẦN NHẤT
    // =====================================================

    private void UpdateTarget()
    {
        targetSearchTimer -= Time.deltaTime;

        // Target hiện tại chết / bị Destroy
        if (currentTarget == null)
        {
            targetSearchTimer = 0f;
        }

        if (targetSearchTimer > 0f)
            return;

        targetSearchTimer = targetRefreshRate;

        FindNearestEnemy();
    }

    private void FindNearestEnemy()
    {
        Collider2D[] enemies =
            Physics2D.OverlapCircleAll(
                transform.position,
                detectionRadius,
                enemyLayer
            );

        Transform nearestEnemy = null;

        float nearestDistanceSqr = Mathf.Infinity;

        Vector2 playerPosition = transform.position;

        foreach (Collider2D enemyCollider in enemies)
        {
            EnemyHealth health =
                enemyCollider.GetComponentInParent<EnemyHealth>();

            if (health == null)
                continue;

            if (health.IsDead)
                continue;

            Vector2 difference =
                (Vector2)enemyCollider.transform.position -
                playerPosition;

            float distanceSqr =
                difference.sqrMagnitude;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;

                nearestEnemy =
                    enemyCollider.transform;
            }
        }

        currentTarget = nearestEnemy;
    }

    // =====================================================
    // ATTACK POINT XOAY 360 ĐỘ
    // =====================================================

    private void UpdateAttackPoint()
    {
        if (currentTarget == null)
            return;

        Vector2 direction =
            ((Vector2)currentTarget.position -
             (Vector2)transform.position).normalized;

        // AttackPoint chạy quanh Player
        attackPoint.position =
            transform.position +
            (Vector3)(direction * attackPointDistance);

        // Góc từ Player → Enemy
        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        attackPoint.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );
    }

    // =====================================================
    // AUTO SHOOT
    // =====================================================

    private void AutoShoot()
    {
        if (currentTarget == null)
            return;

        if (Time.time < nextFireTime)
            return;

        Shoot();

        nextFireTime =
            Time.time + (1f / fireRate);
    }

    private void Shoot()
    {
        if (projectilePrefab == null)
            return;

        if (attackPoint == null)
            return;

        if (currentTarget == null)
            return;

        Vector2 direction =
            ((Vector2)currentTarget.position -
             (Vector2)attackPoint.position).normalized;

        GameObject projectile =
            Instantiate(
                projectilePrefab,
                attackPoint.position,
                Quaternion.identity
            );

        Projectile projectileScript =
            projectile.GetComponent<Projectile>();

        if (projectileScript != null)
        {
            projectileScript.SetDirection(direction);
        }
    }

    // =====================================================
    // DEBUG
    // =====================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRadius
        );
    }
}