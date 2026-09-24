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
    [SerializeField] private float gunVisualAngleOffset = -90f;
    [SerializeField] private GameObject projectilePrefab;

    private float nextFireTime;
    private TowerDefEnemy currentTarget;
    private Canvas owningCanvas;
    private Camera viewCamera;

    public int TurretLevel => turretLevel;
    public float Damage => damage;
    public float FireRate => fireRate;
    public float AttackRange => attackRange;
    public int UpgradeCost => baseUpgradeCost * turretLevel;
    public GameObject ProjectilePrefab => projectilePrefab;

    public event Action<int> OnTurretUpgraded;

    public void Setup(Transform gunTr, Image baseImg, Image gunImg, GameObject upIcon)
    {
        gunTransform = gunTr;
        baseImage = baseImg;
        gunImage = gunImg;
        upgradeIcon = upIcon;
        owningCanvas = GetComponentInParent<Canvas>();
        viewCamera = owningCanvas != null ? owningCanvas.worldCamera : Camera.main;
    }

    public void SetProjectilePrefab(GameObject prefab)
    {
        projectilePrefab = prefab;
    }

    public void SetGunVisualAngleOffset(float angle)
    {
        gunVisualAngleOffset = angle;
    }

    public void CopyProgressFrom(TowerDefTurret source)
    {
        if (source == null) return;
        turretLevel = source.turretLevel;
        damage = source.damage;
        fireRate = source.fireRate;
        attackRange = source.attackRange;
        baseUpgradeCost = source.baseUpgradeCost;
        projectilePrefab = source.projectilePrefab;
        gunVisualAngleOffset = source.gunVisualAngleOffset;
        nextFireTime = source.nextFireTime;
        currentTarget = null;
    }

    private void Update()
    {
        if (TowerDefGameManager.Instance != null && TowerDefGameManager.Instance.IsGameOver)
        {
            return;
        }

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

        // Chỉ chọn kẻ thù đã chạm vào tường thành theo yêu cầu thiết kế
        float closestDist = float.MaxValue;
        if (owningCanvas == null) owningCanvas = GetComponentInParent<Canvas>();
        if (viewCamera == null) viewCamera = owningCanvas != null ? owningCanvas.worldCamera : Camera.main;
        float worldRange = attackRange * (owningCanvas != null ? owningCanvas.transform.lossyScale.x : 1f);
        TowerDefEnemy bestTarget = null;
        Vector3 myPos = transform.position;

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null || e.IsDead || !e.IsTouchingWall) continue;
            if (viewCamera != null)
            {
                Vector3 viewport = viewCamera.WorldToViewportPoint(e.transform.position);
                if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f ||
                    viewport.y < 0f || viewport.y > 1f)
                    continue;
            }

            float dist = Vector3.Distance(myPos, e.transform.position);
            if (dist <= worldRange && dist < closestDist)
            {
                closestDist = dist;
                bestTarget = e;
            }
        }

        currentTarget = bestTarget;

        if (currentTarget != null && gunTransform != null)
        {
            Vector3 dir = currentTarget.transform.position - gunTransform.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + gunVisualAngleOffset;
            gunTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void TryFire()
    {
        if (currentTarget == null || !currentTarget.IsTouchingWall || currentTarget.IsDead || Time.time < nextFireTime) return;

        nextFireTime = Time.time + (1f / Mathf.Max(0.1f, fireRate));
        FireAtTarget(currentTarget);
    }

    private void FireAtTarget(TowerDefEnemy target)
    {
        if (target == null || target.IsDead || !target.IsTouchingWall) return;

        Transform muzzle = gunTransform != null ? gunTransform.Find("FirePoint") : null;
        Vector3 spawnPos = muzzle != null ? muzzle.position :
            gunTransform != null ? gunTransform.position : transform.position;
        TowerDefGameManager.Instance?.SpawnBullet(spawnPos, target, damage, projectilePrefab);
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
