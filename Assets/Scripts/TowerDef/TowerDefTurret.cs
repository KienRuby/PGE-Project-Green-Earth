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
    [SerializeField] private float attackRange = 1600f; // Khoảng cách pixel trong Canvas bao phủ toàn bộ map quái đi
    [SerializeField] private int baseUpgradeCost = 50;

    [Header("Visual References")]
    [SerializeField] private Transform gunTransform;
    [SerializeField] private Image baseImage;
    [SerializeField] private Image gunImage;
    [SerializeField] private GameObject upgradeIcon;
    [SerializeField] private float gunVisualAngleOffset = -90f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Sprite[] baseLevelSprites;
    [SerializeField] private Sprite[] gunLevelSprites;

    private SpriteRenderer baseSpriteRenderer;
    private SpriteRenderer gunSpriteRenderer;
    private float nextFireTime;
    private TowerDefEnemy currentTarget;
    private Canvas owningCanvas;
    private Camera viewCamera;

    public const int MAX_TURRET_LEVEL = 5;
    public int TurretLevel => turretLevel;
    public bool IsMaxLevel => turretLevel >= MAX_TURRET_LEVEL;
    public int NextLevel => Mathf.Min(MAX_TURRET_LEVEL, turretLevel + 1);
    public float Damage => damage;
    public float NextDamage => damage + 20f;
    public float FireRate => fireRate;
    public float NextFireRate => fireRate + 0.3f;
    public float AttackRange => attackRange;
    public int UpgradeCost => IsMaxLevel ? 0 : baseUpgradeCost * turretLevel;
    public GameObject ProjectilePrefab => projectilePrefab;

    public event Action<int> OnTurretUpgraded;

    private void Awake()
    {
        EnsureSpritesLoaded();
    }

    public static string GetTurretName(int level)
    {
        switch (level)
        {
            case 1: return "Pháo Năng Lượng";
            case 2: return "Pháo Thép Lam";
            case 3: return "Pháo Hoàng Kim";
            case 4: return "Pháo Tử Quang";
            case 5: return "Pháo Hỏa Long";
            default: return $"Pháo cấp {level:D2}";
        }
    }

    public void EnsureSpritesLoaded()
    {
        if (baseLevelSprites == null || baseLevelSprites.Length < 5)
        {
            Sprite[] newBases = new Sprite[5];
            if (baseLevelSprites != null)
            {
                for (int i = 0; i < Mathf.Min(baseLevelSprites.Length, 5); i++)
                    newBases[i] = baseLevelSprites[i];
            }
            baseLevelSprites = newBases;
        }

        if (baseLevelSprites[0] == null) baseLevelSprites[0] = Resources.Load<Sprite>("TowerDef/Turret_Base_01_Cyan");
        if (baseLevelSprites[1] == null) baseLevelSprites[1] = Resources.Load<Sprite>("TowerDef/Turret_Base_02_Blue");
        if (baseLevelSprites[2] == null) baseLevelSprites[2] = Resources.Load<Sprite>("TowerDef/Turret_Base_03_Gold");
        if (baseLevelSprites[3] == null) baseLevelSprites[3] = Resources.Load<Sprite>("TowerDef/Turret_Base_04_Pink");
        if (baseLevelSprites[4] == null) baseLevelSprites[4] = Resources.Load<Sprite>("TowerDef/Turret_Base_05_Red");

#if UNITY_EDITOR
        string towersDir = "Assets/Sprites/Mini game/Sliced/Towers/";
        if (baseLevelSprites[0] == null) baseLevelSprites[0] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Base_01_Cyan.png");
        if (baseLevelSprites[1] == null) baseLevelSprites[1] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Base_02_Blue.png");
        if (baseLevelSprites[2] == null) baseLevelSprites[2] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Base_03_Gold.png");
        if (baseLevelSprites[3] == null) baseLevelSprites[3] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Base_04_Pink.png");
        if (baseLevelSprites[4] == null) baseLevelSprites[4] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Base_05_Red.png");
#endif

        if (gunLevelSprites == null || gunLevelSprites.Length < 5)
        {
            Sprite[] newGuns = new Sprite[5];
            if (gunLevelSprites != null)
            {
                for (int i = 0; i < Mathf.Min(gunLevelSprites.Length, 5); i++)
                    newGuns[i] = gunLevelSprites[i];
            }
            gunLevelSprites = newGuns;
        }

        if (gunLevelSprites[0] == null) gunLevelSprites[0] = Resources.Load<Sprite>("TowerDef/Turret_Gun_01_Cyan");
        if (gunLevelSprites[1] == null) gunLevelSprites[1] = Resources.Load<Sprite>("TowerDef/Turret_Gun_02_Blue");
        if (gunLevelSprites[2] == null) gunLevelSprites[2] = Resources.Load<Sprite>("TowerDef/Turret_Gun_03_Gold");
        if (gunLevelSprites[3] == null) gunLevelSprites[3] = Resources.Load<Sprite>("TowerDef/Turret_Gun_04_Purple");
        if (gunLevelSprites[4] == null) gunLevelSprites[4] = Resources.Load<Sprite>("TowerDef/Turret_Gun_05_Red");

#if UNITY_EDITOR
        if (gunLevelSprites[0] == null) gunLevelSprites[0] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Gun_01_Cyan.png");
        if (gunLevelSprites[1] == null) gunLevelSprites[1] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Gun_02_Blue.png");
        if (gunLevelSprites[2] == null) gunLevelSprites[2] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Gun_03_Gold.png");
        if (gunLevelSprites[3] == null) gunLevelSprites[3] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Gun_04_Purple.png");
        if (gunLevelSprites[4] == null) gunLevelSprites[4] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Gun_05_Red.png");
#endif
    }

    public void SetLevelSprites(Sprite[] bases, Sprite[] guns)
    {
        if (bases != null && bases.Length > 0) baseLevelSprites = bases;
        if (guns != null && guns.Length > 0) gunLevelSprites = guns;
        EnsureSpritesLoaded();
        UpdateTurretVisual();
    }

    public Sprite GetBaseSprite(int level)
    {
        EnsureSpritesLoaded();
        if (baseLevelSprites != null && baseLevelSprites.Length > 0)
        {
            int idx = Mathf.Clamp(level - 1, 0, baseLevelSprites.Length - 1);
            if (idx < baseLevelSprites.Length && baseLevelSprites[idx] != null)
                return baseLevelSprites[idx];
        }
        if (baseSpriteRenderer != null) return baseSpriteRenderer.sprite;
        return baseImage != null ? baseImage.sprite : null;
    }

    public Sprite GetGunSprite(int level)
    {
        EnsureSpritesLoaded();
        if (gunLevelSprites != null && gunLevelSprites.Length > 0)
        {
            int idx = Mathf.Clamp(level - 1, 0, gunLevelSprites.Length - 1);
            if (idx < gunLevelSprites.Length && gunLevelSprites[idx] != null)
                return gunLevelSprites[idx];
        }
        if (gunSpriteRenderer != null) return gunSpriteRenderer.sprite;
        return gunImage != null ? gunImage.sprite : null;
    }

    public void Setup(Transform gunTr, Image baseImg, Image gunImg, GameObject upIcon)
    {
        gunTransform = gunTr;
        baseImage = baseImg;
        gunImage = gunImg;
        upgradeIcon = upIcon;
        owningCanvas = GetComponentInParent<Canvas>();
        viewCamera = owningCanvas != null ? owningCanvas.worldCamera : Camera.main;

        // Auto-detect SpriteRenderers if present (prefab instance mode)
        if (gunTransform != null)
        {
            gunSpriteRenderer = gunTransform.GetComponentInChildren<SpriteRenderer>(true);
            Transform parent = gunTransform.parent;
            if (parent != null)
            {
                Transform baseTr = parent.Find("BaseSprite");
                if (baseTr != null) baseSpriteRenderer = baseTr.GetComponent<SpriteRenderer>();
                if (baseSpriteRenderer == null) baseSpriteRenderer = parent.GetComponentInChildren<SpriteRenderer>(true);
            }
        }
        if (baseSpriteRenderer == null)
        {
            baseSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        EnsureSpritesLoaded();
        UpdateTurretVisual();
        SetUpgradeBadgeActive(false);
    }

    public void UpdateTurretVisual()
    {
        EnsureSpritesLoaded();
        Sprite baseSpr = GetBaseSprite(turretLevel);
        Sprite gunSpr = GetGunSprite(turretLevel);

        if (baseSpriteRenderer != null && baseSpr != null)
        {
            baseSpriteRenderer.sprite = baseSpr;
        }
        if (gunSpriteRenderer != null && gunSpr != null)
        {
            gunSpriteRenderer.sprite = gunSpr;
        }

        if (baseImage != null && baseSpr != null)
        {
            baseImage.sprite = baseSpr;
        }
        if (gunImage != null && gunSpr != null)
        {
            gunImage.sprite = gunSpr;
        }
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
        if (source.baseLevelSprites != null) baseLevelSprites = source.baseLevelSprites;
        if (source.gunLevelSprites != null) gunLevelSprites = source.gunLevelSprites;
        UpdateTurretVisual();
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

        // Chọn kẻ thù trong tầm bắn (quái ở xa cũng bắn được)
        float closestDist = float.MaxValue;
        if (owningCanvas == null) owningCanvas = GetComponentInParent<Canvas>();
        if (viewCamera == null) viewCamera = owningCanvas != null ? owningCanvas.worldCamera : Camera.main;
        float canvasScale = owningCanvas != null ? owningCanvas.transform.lossyScale.x : 1f;
        float worldRange = attackRange * (canvasScale > 0.001f ? canvasScale : 1f);
        TowerDefEnemy bestTarget = null;
        Vector3 myPos = transform.position;

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null || e.IsDead) continue;
            if (viewCamera != null && owningCanvas != null && owningCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
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
        if (currentTarget == null || currentTarget.IsDead || Time.time < nextFireTime) return;

        nextFireTime = Time.time + (1f / Mathf.Max(0.1f, fireRate));
        FireAtTarget(currentTarget);
    }

    private void FireAtTarget(TowerDefEnemy target)
    {
        if (target == null || target.IsDead) return;

        Transform muzzle = gunTransform != null ? gunTransform.Find("FirePoint") : null;
        Vector3 spawnPos = muzzle != null ? muzzle.position :
            gunTransform != null ? gunTransform.position : transform.position;
        TowerDefGameManager.Instance?.SpawnBullet(spawnPos, target, damage, projectilePrefab);
    }

    public bool TryUpgrade(ref int gold)
    {
        if (IsMaxLevel) return false;
        int cost = UpgradeCost;
        if (gold < cost) return false;

        gold -= cost;
        turretLevel++;
        damage += 20f;
        fireRate += 0.3f;
        attackRange += 50f;

        UpdateTurretVisual();
        OnTurretUpgraded?.Invoke(turretLevel);
        return true;
    }

    public bool TryUpgradeFree()
    {
        if (IsMaxLevel) return false;

        turretLevel++;
        damage += 20f;
        fireRate += 0.3f;
        attackRange += 50f;

        UpdateTurretVisual();
        OnTurretUpgraded?.Invoke(turretLevel);
        return true;
    }

    public void SetUpgradeBadgeActive(bool active)
    {
        if (upgradeIcon != null)
        {
            upgradeIcon.SetActive(active && !IsMaxLevel);
        }
    }
}
