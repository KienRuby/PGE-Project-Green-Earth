#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class TowerDefTests
{
    private GameObject rootObj;
    private TowerDefGameManager gameManager;

    [SetUp]
    public void SetUp()
    {
        rootObj = new GameObject("TestRoot");
        gameManager = rootObj.AddComponent<TowerDefGameManager>();
    }

    [TearDown]
    public void TearDown()
    {
        if (rootObj != null)
        {
            Object.DestroyImmediate(rootObj);
        }
    }

    [Test]
    public void Gate_TakesDamageAndRepairsCorrectly()
    {
        GameObject gateObj = new GameObject("TestGate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGate));
        gateObj.transform.SetParent(rootObj.transform);

        GameObject hpFillObj = new GameObject("HpFill", typeof(Image));
        hpFillObj.transform.SetParent(gateObj.transform);

        TowerDefGate gate = gateObj.GetComponent<TowerDefGate>();
        gate.Setup(300f, hpFillObj.GetComponent<Image>(), null, gateObj.GetComponent<Button>());

        Assert.AreEqual(300f, gate.CurrentHp);
        Assert.IsFalse(gate.IsDestroyed);

        // Chịu sát thương 100
        gate.TakeDamage(100f);
        Assert.AreEqual(200f, gate.CurrentHp);

        // Hồi phục 50
        gate.Repair(50f);
        Assert.AreEqual(250f, gate.CurrentHp);

        // Hồi phục vượt trần không quá maxHp
        gate.Repair(200f);
        Assert.AreEqual(300f, gate.CurrentHp);

        // Chịu sát thương hủy diệt
        bool wasDestroyed = false;
        gate.OnGateDestroyed += () => wasDestroyed = true;
        gate.TakeDamage(500f);
        Assert.AreEqual(0f, gate.CurrentHp);
        Assert.IsTrue(gate.IsDestroyed);
        Assert.IsTrue(wasDestroyed);
    }

    [Test]
    public void Gate_UpgradeIncreasesMaxHpAndHeals()
    {
        GameObject gateObj = new GameObject("TestGate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGate));
        gateObj.transform.SetParent(rootObj.transform);

        TowerDefGate gate = gateObj.GetComponent<TowerDefGate>();
        gate.Setup(300f, null, null, null);

        gate.TakeDamage(150f);
        Assert.AreEqual(150f, gate.CurrentHp);

        int gold = 100;
        bool upgraded = gate.TryUpgrade(ref gold);
        Assert.IsTrue(upgraded);
        Assert.AreEqual(50, gold); // 100 - 50 = 50
        Assert.AreEqual(2, gate.GateLevel);
        Assert.AreEqual(450f, gate.MaxHp);
        Assert.AreEqual(450f, gate.CurrentHp); // Hồi đầy máu khi nâng cấp
    }

    [Test]
    public void GridCell_CanPlaceStructureAndDetectOccupancy()
    {
        GameObject cellObj = new GameObject("Cell", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGridCell));
        cellObj.transform.SetParent(rootObj.transform);

        GameObject sRoot = new GameObject("StructureRoot", typeof(RectTransform));
        sRoot.transform.SetParent(cellObj.transform);

        GameObject sImgObj = new GameObject("StructureImg", typeof(Image));
        sImgObj.transform.SetParent(sRoot.transform);

        GameObject gunObj = new GameObject("Gun", typeof(RectTransform), typeof(Image));
        gunObj.transform.SetParent(sRoot.transform);

        GameObject upBadge = new GameObject("Badge", typeof(Image));
        upBadge.transform.SetParent(cellObj.transform);

        TowerDefGridCell cell = cellObj.GetComponent<TowerDefGridCell>();
        cell.SetupCell(2, 1, cellObj.GetComponent<Image>(), cellObj.GetComponent<Button>(), sRoot, sImgObj.GetComponent<Image>(), gunObj.transform, upBadge);

        Assert.IsFalse(cell.IsOccupied);
        Assert.AreEqual(TowerDefStructureType.None, cell.CurrentType);

        cell.PlaceStructure(TowerDefStructureType.Turret, null, null, 1);
        Assert.IsTrue(cell.IsOccupied);
        Assert.AreEqual(TowerDefStructureType.Turret, cell.CurrentType);
        Assert.IsNotNull(cell.Turret);
    }

    [Test]
    public void Turret_CanMoveToEmptyCellWithoutLosingUpgradeOrGold()
    {
        TowerDefGridCell CreateCell(string name)
        {
            GameObject cellObject = new GameObject(name, typeof(RectTransform), typeof(Image),
                typeof(Button), typeof(TowerDefGridCell));
            cellObject.transform.SetParent(rootObj.transform);
            GameObject structure = new GameObject("StructureRoot", typeof(RectTransform));
            structure.transform.SetParent(cellObject.transform);
            GameObject image = new GameObject("StructureImage", typeof(RectTransform), typeof(Image));
            image.transform.SetParent(structure.transform);
            GameObject gun = new GameObject("Gun", typeof(RectTransform), typeof(Image));
            gun.transform.SetParent(structure.transform);
            TowerDefGridCell cell = cellObject.GetComponent<TowerDefGridCell>();
            cell.SetupCell(0, 0, cellObject.GetComponent<Image>(), cellObject.GetComponent<Button>(),
                structure, image.GetComponent<Image>(), gun.transform, null);
            return cell;
        }

        TowerDefGridCell source = CreateCell("Source");
        TowerDefGridCell destination = CreateCell("Destination");
        GameObject gunTurretPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Chipset/GunTurret.prefab");
        Assert.IsNotNull(gunTurretPrefab);
        source.ConfigureTurretPrefab(gunTurretPrefab);
        source.PlaceStructure(TowerDefStructureType.Turret, null);
        GameObject prefabInstance = source.TurretPrefabInstance;
        Assert.IsNotNull(prefabInstance);
        Assert.IsTrue(UnityEditor.PrefabUtility.IsPartOfPrefabInstance(prefabInstance));
        Assert.IsTrue(prefabInstance.transform.Find("BaseSprite").GetComponent<SpriteRenderer>().enabled);
        Assert.IsTrue(prefabInstance.transform.Find("AimPivot/GunSprite").GetComponent<SpriteRenderer>().enabled);
        Assert.IsFalse(source.transform.Find("StructureRoot/StructureImage").gameObject.activeSelf);
        Assert.IsFalse(source.transform.Find("StructureRoot/Gun").gameObject.activeSelf);
        int upgradeGold = 100;
        Assert.IsTrue(source.Turret.TryUpgrade(ref upgradeGold));
        source.UpgradeCurrentStructure();
        int goldBeforeMove = gameManager.Gold;

        Assert.IsTrue(gameManager.TryMoveTurret(source, destination));
        Assert.IsFalse(source.IsOccupied);
        Assert.IsTrue(destination.IsOccupied);
        Assert.AreSame(prefabInstance, destination.TurretPrefabInstance);
        Assert.IsNull(source.TurretPrefabInstance);
        Assert.AreEqual(2, destination.StructureLevel);
        Assert.AreEqual(2, destination.Turret.TurretLevel);
        Assert.AreEqual(40f, destination.Turret.Damage);
        Assert.AreEqual(1.45f, destination.Turret.FireRate, 0.001f);
        Assert.AreEqual(goldBeforeMove, gameManager.Gold);

        source.PlaceStructure(TowerDefStructureType.CoreBed, null);
        Assert.IsFalse(gameManager.TryMoveTurret(destination, source));
        Assert.IsTrue(destination.IsOccupied);
    }

    [Test]
    public void TurretShot_UsesTheProjectileAssignedOnGunTurretPrefab()
    {
        GameObject gunPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Chipset/GunTurret.prefab");
        Assert.IsNotNull(gunPrefab);
        GameObject projectilePrefab = gunPrefab.GetComponent<GunTurret>().ProjectilePrefab;
        Assert.IsNotNull(projectilePrefab);

        GameObject enemyObject = new GameObject("Target", typeof(RectTransform), typeof(TowerDefEnemy));
        enemyObject.transform.SetParent(rootObj.transform);
        GameObject shot = null;
        try
        {
            shot = gameManager.SpawnBullet(Vector3.zero, enemyObject.GetComponent<TowerDefEnemy>(),
                25f, projectilePrefab);
            Assert.IsNotNull(shot);
            Assert.AreEqual(projectilePrefab.GetComponent<SpriteRenderer>().sprite,
                shot.GetComponent<SpriteRenderer>().sprite);
            Assert.IsFalse(shot.GetComponent<Projectile>().enabled);
            Assert.IsFalse(shot.GetComponent<Rigidbody2D>().simulated);
            Assert.IsNotNull(shot.GetComponent<TowerDefProjectile>());
            Assert.IsNull(shot.GetComponent<Image>());
        }
        finally
        {
            if (shot != null) Object.DestroyImmediate(shot);
        }
    }

    [Test]
    public void Economy_StartsAtStandardDesignValues()
    {
        // Khởi đầu theo thiết kế: 50 Gold, 0 Energy
        Assert.AreEqual(50, gameManager.Gold);
        Assert.AreEqual(0, gameManager.Energy);

        gameManager.AddGold(100);
        gameManager.AddEnergy(25);

        Assert.AreEqual(150, gameManager.Gold);
        Assert.AreEqual(25, gameManager.Energy);
    }

    [Test]
    public void Enemy_TakesDamageAndDies()
    {
        GameObject enemyObj = new GameObject("Enemy", typeof(RectTransform), typeof(Image), typeof(TowerDefEnemy));
        enemyObj.transform.SetParent(rootObj.transform);

        TowerDefEnemy enemy = enemyObj.GetComponent<TowerDefEnemy>();
        enemy.Setup(100f, 50f, 10f, 20, null, 0f, null);

        Assert.IsFalse(enemy.IsDead);
        Assert.AreEqual(100f, enemy.CurrentHp);

        enemy.TakeDamage(40f);
        Assert.AreEqual(60f, enemy.CurrentHp);
        Assert.IsFalse(enemy.IsDead);

        bool died = false;
        enemy.OnEnemyDied += (e) => died = true;
        enemy.TakeDamage(70f);

        Assert.IsTrue(enemy.IsDead);
        Assert.IsTrue(died);
    }

    [Test]
    public void Enemy_DoesNotTakeDamageBeforeTouchingWall()
    {
        GameObject enemyObj = new GameObject("EnemyAboveWall", typeof(RectTransform), typeof(Image), typeof(TowerDefEnemy));
        enemyObj.transform.SetParent(rootObj.transform);
        enemyObj.transform.localPosition = new Vector3(0f, 500f, 0f);

        TowerDefEnemy enemy = enemyObj.GetComponent<TowerDefEnemy>();
        enemy.Setup(100f, 50f, 10f, 20, null, 100f, null);

        Assert.IsFalse(enemy.IsTouchingWall);

        // Chưa chạm tường -> không bị trừ máu theo yêu cầu
        enemy.TakeDamage(50f);
        Assert.AreEqual(100f, enemy.CurrentHp);

        // Khi quái đã chạm vào tường thành -> nhận sát thương bình thường
        enemy.SetTouchingWall(true);
        Assert.IsTrue(enemy.IsTouchingWall);

        enemy.TakeDamage(50f);
        Assert.AreEqual(50f, enemy.CurrentHp);
    }

    [Test]
    public void Gate_DestructionTriggersGameOver()
    {
        GameObject gateObj = new GameObject("Gate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGate));
        gateObj.transform.SetParent(rootObj.transform);

        TowerDefGate gate = gateObj.GetComponent<TowerDefGate>();
        gate.Setup(300f, null, null, null);

        // Gán gate cho gameManager bằng reflection hoặc trigger Destroy
        bool gameOverFired = false;
        gameManager.OnLevelDefeat += () => gameOverFired = true;

        gate.OnGateDestroyed += () =>
        {
            var method = typeof(TowerDefGameManager).GetMethod("HandleGateBreached", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(gameManager, null);
        };

        gate.TakeDamage(300f);
        Assert.IsTrue(gate.IsDestroyed);
        Assert.IsTrue(gameManager.IsGameOver);
        Assert.IsTrue(gameOverFired);
    }
}
#endif
