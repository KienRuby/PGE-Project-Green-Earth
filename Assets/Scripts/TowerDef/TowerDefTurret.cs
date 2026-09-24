using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý tháp pháo phòng thủ (Turret):
/// - Tự động phát hiện quái vật trong tầm bắn (Range).
/// - Xoay nòng pháo (gunTransform) hướng về mục tiêu.
/// - Bắn đạn định kỳ gây sát thương lên quái.
/// - Cho phép nâng cấp (Upgrade) tăng sát thương, tốc bắn và tầm bắn.
/// </summary>
public class TowerDefTurret : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int turretLevel = 1;
    [SerializeField] private float damage = 25f;
    [SerializeField] private float fireRate = 1.2f; // Phát bắn mỗi giây
    [SerializeField] private float attackRange = 850f; // Khoảng cách pixel trong Canvas
    [SerializeField] private int baseUpgradeCost = 50;

    [Header("Visual References")]
    [SerializeField] private Transform gunTransform;
    [SerializeField] private Image baseImage;
    [SerializeField] private Image gunImage;
    [SerializeField] private GameObject upgradeIcon;

    private float nextFireTime;
    private TowerDefEnemy currentTarget;

    public int TurretLevel => turretLevel;
    public float Damage => damage;
    public float FireRate => fireRate;
    public float AttackRange => attackRange;
    public int UpgradeCost => baseUpgradeCost * turretLevel;

    public event Action<int> OnTurretUpgraded;

    public void Setup(Transform gunTr, Image baseImg, Image gunImg, GameObject upIcon)
    {
        gunTransform = gunTr;
        baseImage = baseImg;
        gunImage = gunImg;
        upgradeIcon = upIcon;
    }

    private void Update()
    {
        FindAndTrackTarget();
        TryFire();
    }

    private void FindAndTrackTarget()
    {
        var enemies = TowerDefGameManager.Instance?.ActiveEnemies;
        if (enemies == null || enemies.Count == 0)
        {
            currentTarget = null;
            return;
        }

        // Chọn kẻ thù gần cổng nhất (hoặc gần tháp nhất trong tầm)
        float closestDist = float.MaxValue;
        TowerDefEnemy bestTarget = null;
        Vector3 myPos = transform.position;

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null || e.IsDead) continue;

            float dist = Vector3.Distance(myPos, e.transform.position);
            if (dist <= attackRange && dist < closestDist)
            {
                closestDist = dist;
                bestTarget = e;
            }
        }

        currentTarget = bestTarget;

        if (currentTarget != null && gunTransform != null)
        {
            Vector3 dir = currentTarget.transform.position - gunTransform.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            gunTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void TryFire()
    {
        if (currentTarget == null || Time.time < nextFireTime) return;

        nextFireTime = Time.time + (1f / Mathf.Max(0.1f, fireRate));
        FireAtTarget(currentTarget);
    }

    private void FireAtTarget(TowerDefEnemy target)
    {
        if (target == null || target.IsDead) return;

        Vector3 spawnPos = gunTransform != null ? gunTransform.position : transform.position;
        TowerDefGameManager.Instance?.SpawnBullet(spawnPos, target, damage);
    }

    public bool TryUpgrade(ref int gold)
    {
        int cost = UpgradeCost;
        if (gold < cost) return false;

        gold -= cost;
        turretLevel++;
        damage += 15f;
        fireRate += 0.25f;
        attackRange += 50f;

        OnTurretUpgraded?.Invoke(turretLevel);
        return true;
    }

    public void SetUpgradeBadgeActive(bool active)
    {
        if (upgradeIcon != null)
        {
            upgradeIcon.SetActive(active);
        }
    }
}
