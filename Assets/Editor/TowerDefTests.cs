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
    public void Gate_UpgradeChangesAppearance_ToAllLevels()
    {
        GameObject gateObj = new GameObject("TestGateVisual", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGate));
        gateObj.transform.SetParent(rootObj.transform);

        TowerDefGate gate = gateObj.GetComponent<TowerDefGate>();
        Image img = gateObj.GetComponent<Image>();
        gate.Setup(50f, null, null, null, 10, 20f);

        // Level 1: Gate_Metal
        Assert.AreEqual(1, gate.GateLevel);
        Assert.IsNotNull(img.sprite, "Level 1 gate sprite should not be null");
        Assert.IsTrue(img.sprite.name.Contains("Metal") || img.sprite.name.Contains("Gate"), $"Expected Gate_Metal but got {img.sprite.name}");

        // Level 2: Gate_Cyan_Grid
        int gold = 1000;
        bool up1 = gate.TryUpgrade(ref gold);
        Assert.IsTrue(up1);
        Assert.AreEqual(2, gate.GateLevel);
        Assert.IsNotNull(img.sprite, "Level 2 gate sprite should not be null");
        Assert.IsTrue(img.sprite.name.Contains("Cyan") || img.sprite.name.Contains("Grid"), $"Expected Gate_Cyan_Grid but got {img.sprite.name}");

        // Level 3: Gate_Green_Wood
        bool up2 = gate.TryUpgrade(ref gold);
        Assert.IsTrue(up2);
        Assert.AreEqual(3, gate.GateLevel);
        Assert.IsNotNull(img.sprite, "Level 3 gate sprite should not be null");
        Assert.IsTrue(img.sprite.name.Contains("Green") || img.sprite.name.Contains("Wood"), $"Expected Gate_Green_Wood but got {img.sprite.name}");

        // Level 4: Gate_Blue_Wood
        bool up3 = gate.TryUpgrade(ref gold);
        Assert.IsTrue(up3);
        Assert.AreEqual(4, gate.GateLevel);
        Assert.IsNotNull(img.sprite, "Level 4 gate sprite should not be null");
        Assert.IsTrue(img.sprite.name.Contains("Blue") || img.sprite.name.Contains("Wood"), $"Expected Gate_Blue_Wood but got {img.sprite.name}");
    }

    [Test]
    public void Gate_UpgradeBadgeVisibilityDependsOnGoldAndLevel()
    {
        GameObject gateObj = new GameObject("TestGateBadge", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGate));
        gateObj.transform.SetParent(rootObj.transform);

        GameObject badgeObj = new GameObject("UpgradeBadge", typeof(Image));
        badgeObj.transform.SetParent(gateObj.transform);

        TowerDefGate gate = gateObj.GetComponent<TowerDefGate>();
        gate.Setup(50f, null, badgeObj, null, 12, 20f);

        // Ban đầu chưa đủ vàng (10 < 12) -> Không hiện mũi tên vàng
        gate.RefreshUpgradeBadge(10);
        Assert.IsFalse(badgeObj.activeSelf);

        // Đủ vàng (12 >= 12) -> Hiện mũi tên vàng
        gate.RefreshUpgradeBadge(12);
        Assert.IsTrue(badgeObj.activeSelf);

        // Nâng cấp lên level 2
        int gold = 12;
        gate.TryUpgrade(ref gold);
        Assert.AreEqual(2, gate.GateLevel);
        Assert.AreEqual(70f, gate.MaxHp);
        Assert.AreEqual(24, gate.UpgradeCost); // Cost tăng lên 24

        // Vàng hiện tại = 0 < 24 -> Mũi tên tự ẩn
        gate.RefreshUpgradeBadge(gold);
        Assert.IsFalse(badgeObj.activeSelf);

        // Cung cấp 24 vàng -> Mũi tên lại hiện
        gate.RefreshUpgradeBadge(24);
        Assert.IsTrue(badgeObj.activeSelf);

        // Nâng lên max level (4)
        gate.TryUpgradeFree(); // Lv.3
        gate.TryUpgradeFree(); // Lv.4 (Max)
        Assert.IsTrue(gate.IsMaxLevel);

        // Khi đã max level, dù có 999 vàng cũng không hiện mũi tên nâng cấp
        gate.RefreshUpgradeBadge(999);
        Assert.IsFalse(badgeObj.activeSelf);
    }

    [Test]
    public void Gate_FreeAdUpgradeWorksAndIncreasesLevel()
    {
        GameObject gateObj = new GameObject("TestGateFree", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGate));
        gateObj.transform.SetParent(rootObj.transform);

        TowerDefGate gate = gateObj.GetComponent<TowerDefGate>();
        gate.Setup(50f, null, null, null, 12, 20f);

        gate.TakeDamage(30f);
        Assert.AreEqual(20f, gate.CurrentHp);

        bool upgraded = gate.TryUpgradeFree();
        Assert.IsTrue(upgraded);
        Assert.AreEqual(2, gate.GateLevel);
        Assert.AreEqual(70f, gate.MaxHp);
        Assert.AreEqual(70f, gate.CurrentHp); // Hồi máu đầy khi nâng cấp miễn phí
    }

    [Test]
    public void UIController_OpensGateUpgradeModalSuccessfully()
    {
        GameObject canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(TowerDefUIController));
        canvasObj.transform.SetParent(rootObj.transform);
        TowerDefUIController ui = canvasObj.GetComponent<TowerDefUIController>();

        GameObject gateObj = new GameObject("Gate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGate));
        gateObj.transform.SetParent(rootObj.transform);
        TowerDefGate gate = gateObj.GetComponent<TowerDefGate>();
        gate.Setup(50f, null, null, null, 12, 20f);

        ui.OpenGateModal(gate);

        Transform modal = canvasObj.transform.Find("GateUpgradeModal");
        Assert.IsNotNull(modal);
        Assert.IsTrue(modal.gameObject.activeSelf);

        Transform frame = modal.Find("ModalFrame");
        Assert.IsNotNull(frame);
        Assert.IsNotNull(frame.Find("TitleText"));
        Assert.IsNotNull(frame.Find("Row1_GoldUpgrade"));
        Assert.IsNotNull(frame.Find("Row2_AdsUpgrade"));

        ui.CloseAllModals();
        Assert.IsFalse(modal.gameObject.activeSelf);
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
    public void Turret_MoveClearsSourceCellCompletelyAndPreservesExactOneTurretPerCell()
    {
        TowerDefGridCell CreateCell(string name, int r, int c)
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
            cell.SetupCell(r, c, cellObject.GetComponent<Image>(), cellObject.GetComponent<Button>(),
                structure, image.GetComponent<Image>(), gun.transform, null);
            return cell;
        }

        TowerDefGridCell source = CreateCell("SourceCell", 2, 1);
        TowerDefGridCell destination = CreateCell("DestCell", 2, 2);
        GameObject gunTurretPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Chipset/GunTurret.prefab");
        Assert.IsNotNull(gunTurretPrefab);

        source.ConfigureTurretPrefab(gunTurretPrefab);
        source.PlaceStructure(TowerDefStructureType.Turret, null);

        // Verify initial state: exactly 1 turret on source
        Assert.IsTrue(source.IsOccupied);
        Assert.AreEqual(TowerDefStructureType.Turret, source.CurrentType);
        Assert.IsNotNull(source.TurretPrefabInstance);
        Assert.AreEqual(new Vector3(0f, 0f, -0.1f), source.TurretPrefabInstance.transform.localPosition);
        Assert.AreEqual(1, source.transform.Find("StructureRoot").GetComponentsInChildren<GunTurret>(true).Length);

        // Destination is empty
        Assert.IsFalse(destination.IsOccupied);
        Assert.AreEqual(TowerDefStructureType.None, destination.CurrentType);

        // Move to destination
        bool moveResult = gameManager.TryMoveTurret(source, destination);
        Assert.IsTrue(moveResult);

        // Verify destination has exactly 1 turret, centered properly
        Assert.IsTrue(destination.IsOccupied);
        Assert.AreEqual(TowerDefStructureType.Turret, destination.CurrentType);
        Assert.IsNotNull(destination.TurretPrefabInstance);
        Assert.AreEqual(new Vector3(0f, 0f, -0.1f), destination.TurretPrefabInstance.transform.localPosition);
        Assert.AreEqual(Quaternion.identity, destination.TurretPrefabInstance.transform.localRotation);
        Assert.AreEqual(1, destination.transform.Find("StructureRoot").GetComponentsInChildren<GunTurret>(true).Length);

        // Verify source is completely empty: NO turret instance, NO TowerDefTurret component, IsOccupied = false
        Assert.IsFalse(source.IsOccupied);
        Assert.AreEqual(TowerDefStructureType.None, source.CurrentType);
        Assert.IsNull(source.TurretPrefabInstance);
        Assert.IsNull(source.Turret);
        Assert.AreEqual(0, source.transform.Find("StructureRoot").GetComponentsInChildren<GunTurret>(true).Length);

        // Verify that moving again to an already occupied destination fails (1 ô tối đa 1 turret)
        TowerDefGridCell thirdCell = CreateCell("ThirdCell", 1, 1);
        thirdCell.ConfigureTurretPrefab(gunTurretPrefab);
        thirdCell.PlaceStructure(TowerDefStructureType.Turret, null);
        Assert.IsTrue(thirdCell.IsOccupied);

        bool moveBlocked = gameManager.TryMoveTurret(thirdCell, destination);
        Assert.IsFalse(moveBlocked, "Cannot move into an already occupied cell");
        Assert.IsTrue(thirdCell.IsOccupied);
        Assert.AreEqual(1, destination.transform.Find("StructureRoot").GetComponentsInChildren<GunTurret>(true).Length);
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
        // Khởi đầu theo thiết kế: 100 Gold, 0 Energy
        Assert.AreEqual(100, gameManager.Gold);
        Assert.AreEqual(0, gameManager.Energy);

        gameManager.AddGold(100);
        gameManager.AddEnergy(25);

        Assert.AreEqual(200, gameManager.Gold);
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
    public void Enemy_TakesDamageWhileMovingDown()
    {
        GameObject enemyObj = new GameObject("EnemyAboveWall", typeof(RectTransform), typeof(Image), typeof(TowerDefEnemy));
        enemyObj.transform.SetParent(rootObj.transform);
        enemyObj.transform.localPosition = new Vector3(0f, 500f, 0f);

        GameObject hpBarObj = new GameObject("HpFill", typeof(RectTransform), typeof(Image));
        hpBarObj.transform.SetParent(enemyObj.transform);
        Image fill = hpBarObj.GetComponent<Image>();
        fill.type = Image.Type.Filled;

        TowerDefEnemy enemy = enemyObj.GetComponent<TowerDefEnemy>();
        enemy.Setup(100f, 50f, 10f, 20, null, 100f, null, fill);

        Assert.IsFalse(enemy.IsTouchingWall);

        // Quái đang hành quân trên đường -> trúng đạn lập tức bị trừ máu và thanh máu giảm
        enemy.TakeDamage(40f);
        Assert.AreEqual(60f, enemy.CurrentHp);
        Assert.AreEqual(0.6f, fill.fillAmount, 0.01f);

        // Tiếp tục nhận sát thương khi đến chân tường
        enemy.SetTouchingWall(true);
        Assert.IsTrue(enemy.IsTouchingWall);

        enemy.TakeDamage(30f);
        Assert.AreEqual(30f, enemy.CurrentHp);
        Assert.AreEqual(0.3f, fill.fillAmount, 0.01f);
    }

    [Test]
    public void Bed_UpgradeIncreasesLevelAndProduction()
    {
        GameObject cellObj = new GameObject("BedCell", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGridCell));
        cellObj.transform.SetParent(rootObj.transform);
        GameObject sRoot = new GameObject("StructureRoot", typeof(RectTransform));
        sRoot.transform.SetParent(cellObj.transform);
        GameObject sImgObj = new GameObject("StructureImg", typeof(Image));
        sImgObj.transform.SetParent(sRoot.transform);

        TowerDefGridCell cell = cellObj.GetComponent<TowerDefGridCell>();
        cell.SetupCell(1, 3, cellObj.GetComponent<Image>(), cellObj.GetComponent<Button>(), sRoot, sImgObj.GetComponent<Image>(), null, null);

        cell.PlaceStructure(TowerDefStructureType.CoreBed, null, null, 1);
        Assert.IsNotNull(cell.Generator);
        Assert.AreEqual(1, cell.Generator.StructureLevel);
        Assert.AreEqual(1, cell.Generator.CurrentOutput);
        Assert.AreEqual(50, cell.Generator.UpgradeCost);

        int gold = 100;
        bool ok = cell.Generator.TryUpgrade(ref gold);
        Assert.IsTrue(ok);
        Assert.AreEqual(50, gold);
        Assert.AreEqual(2, cell.Generator.StructureLevel);
        Assert.AreEqual(2, cell.Generator.CurrentOutput);
        Assert.AreEqual(100, cell.Generator.UpgradeCost);
    }

    [Test]
    public void Bed_UpgradeBadgeVisibilityDependsOnGoldAndLevel()
    {
        GameObject bedObj = new GameObject("BedGenerator", typeof(TowerDefGenerator));
        bedObj.transform.SetParent(rootObj.transform);

        GameObject badgeObj = new GameObject("UpgradeBadge");
        badgeObj.transform.SetParent(bedObj.transform);

        TowerDefGenerator gen = bedObj.GetComponent<TowerDefGenerator>();
        gen.Setup(TowerDefStructureType.CoreBed, 1, null, badgeObj, 1.0f);

        // Ban đầu chưa đủ vàng (40 < 50) -> Không hiện mũi tên vàng
        gen.RefreshUpgradeBadge(40);
        Assert.IsFalse(badgeObj.activeSelf);

        // Đủ vàng (50 >= 50) -> Hiện mũi tên vàng
        gen.RefreshUpgradeBadge(50);
        Assert.IsTrue(badgeObj.activeSelf);

        // Nâng cấp lên level 2 (Cost = 100)
        int gold = 50;
        gen.TryUpgrade(ref gold);
        Assert.AreEqual(2, gen.StructureLevel);
        Assert.AreEqual(100, gen.UpgradeCost);

        // Vàng hiện tại = 0 < 100 -> Mũi tên tự ẩn
        gen.RefreshUpgradeBadge(gold);
        Assert.IsFalse(badgeObj.activeSelf);

        // Đủ 100 vàng -> Hiện lại
        gen.RefreshUpgradeBadge(100);
        Assert.IsTrue(badgeObj.activeSelf);

        // Nâng cấp miễn phí lên MAX (Lv 4)
        gen.TryUpgradeFree(); // Lv.3
        gen.TryUpgradeFree(); // Lv.4
        Assert.IsTrue(gen.IsMaxLevel);

        // Đã max level -> Dù có 999 vàng cũng không hiện mũi tên
        gen.RefreshUpgradeBadge(999);
        Assert.IsFalse(badgeObj.activeSelf);
    }

    [Test]
    public void UIController_OpensBedUpgradeModalSuccessfully()
    {
        GameObject canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(TowerDefUIController));
        canvasObj.transform.SetParent(rootObj.transform);
        TowerDefUIController ui = canvasObj.GetComponent<TowerDefUIController>();

        GameObject cellObj = new GameObject("Cell", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGridCell));
        cellObj.transform.SetParent(rootObj.transform);
        GameObject sRoot = new GameObject("StructureRoot", typeof(RectTransform));
        sRoot.transform.SetParent(cellObj.transform);
        GameObject sImgObj = new GameObject("StructureImg", typeof(Image));
        sImgObj.transform.SetParent(sRoot.transform);

        TowerDefGridCell cell = cellObj.GetComponent<TowerDefGridCell>();
        cell.SetupCell(1, 3, cellObj.GetComponent<Image>(), cellObj.GetComponent<Button>(), sRoot, sImgObj.GetComponent<Image>(), null, null);
        cell.PlaceStructure(TowerDefStructureType.CoreBed, null, null, 1);

        ui.OpenUpgradeModal(cell);

        Transform modal = canvasObj.transform.Find("StructureUpgradeModal");
        Assert.IsNotNull(modal);
        Assert.IsTrue(modal.gameObject.activeSelf);

        Transform frame = modal.Find("ModalFrame");
        Assert.IsNotNull(frame);
        Assert.IsNotNull(frame.Find("TitleText"));
        Assert.IsNotNull(frame.Find("Row1_GoldUpgrade"));
        Assert.IsNotNull(frame.Find("Row2_AdsUpgrade"));

        ui.CloseAllModals();
        Assert.IsFalse(modal.gameObject.activeSelf);
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

    [Test]
    public void Gate_UpgradesTransitionGateVisualAndNameAcrossAllLevels()
    {
        GameObject gateObj = new GameObject("Gate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGate));
        gateObj.transform.SetParent(rootObj.transform);
        Image gateImg = gateObj.GetComponent<Image>();

        TowerDefGate gate = gateObj.GetComponent<TowerDefGate>();
        gate.Setup(50f, null, null, null, 12, 20f);

        Assert.AreEqual(1, gate.GateLevel);
        Assert.AreEqual("Cổng sắt", TowerDefGate.GetGateName(1));
        Assert.IsNotNull(gateImg.sprite);
        Assert.AreEqual("Gate_Metal", gateImg.sprite.name);

        // Level 1 -> 2
        bool up2 = gate.TryUpgradeFree();
        Assert.IsTrue(up2);
        Assert.AreEqual(2, gate.GateLevel);
        Assert.AreEqual("Cổng lưới lam", TowerDefGate.GetGateName(2));
        Assert.AreEqual("Gate_Cyan_Grid", gateImg.sprite.name);

        // Level 2 -> 3
        bool up3 = gate.TryUpgradeFree();
        Assert.IsTrue(up3);
        Assert.AreEqual(3, gate.GateLevel);
        Assert.AreEqual("Cổng mạ lục", TowerDefGate.GetGateName(3));
        Assert.AreEqual("Gate_Green_Wood", gateImg.sprite.name);

        // Level 3 -> 4
        bool up4 = gate.TryUpgradeFree();
        Assert.IsTrue(up4);
        Assert.AreEqual(4, gate.GateLevel);
        Assert.AreEqual("Cổng hợp kim", TowerDefGate.GetGateName(4));
        Assert.AreEqual("Gate_Blue_Wood", gateImg.sprite.name);
        Assert.IsTrue(gate.IsMaxLevel);

        // Cannot upgrade beyond max level
        Assert.IsFalse(gate.TryUpgradeFree());
    }

    [Test]
    public void Turret_UpgradesTransitionSpritesAndStatsAcrossAllLevels()
    {
        GameObject cellObj = new GameObject("TurretCell", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGridCell));
        cellObj.transform.SetParent(rootObj.transform);
        GameObject sRoot = new GameObject("StructureRoot", typeof(RectTransform));
        sRoot.transform.SetParent(cellObj.transform);
        GameObject sImgObj = new GameObject("StructureImg", typeof(Image));
        sImgObj.transform.SetParent(sRoot.transform);
        GameObject gunObj = new GameObject("Gun", typeof(RectTransform), typeof(Image));
        gunObj.transform.SetParent(sRoot.transform);

        TowerDefGridCell cell = cellObj.GetComponent<TowerDefGridCell>();
        cell.SetupCell(1, 1, cellObj.GetComponent<Image>(), cellObj.GetComponent<Button>(), sRoot, sImgObj.GetComponent<Image>(), gunObj.transform, null);

        cell.PlaceStructure(TowerDefStructureType.Turret, null, null, 1);
        TowerDefTurret turret = cell.Turret;
        Assert.IsNotNull(turret);

        Assert.AreEqual(1, turret.TurretLevel);
        Assert.AreEqual(20f, turret.Damage);
        Assert.AreEqual(1.15f, turret.FireRate, 0.001f);
        Assert.AreEqual("Pháo cấp 1", TowerDefTurret.GetTurretName(1));
        Assert.AreEqual(40f, turret.NextDamage);
        Assert.AreEqual(1.45f, turret.NextFireRate, 0.001f);

        // Level 1 -> 2
        bool up2 = turret.TryUpgradeFree();
        Assert.IsTrue(up2);
        Assert.AreEqual(2, turret.TurretLevel);
        Assert.AreEqual(40f, turret.Damage);
        Assert.AreEqual(1.45f, turret.FireRate, 0.001f);
        Assert.AreEqual("Pháo cấp 2", TowerDefTurret.GetTurretName(2));

        // Level 2 -> 3
        bool up3 = turret.TryUpgradeFree();
        Assert.IsTrue(up3);
        Assert.AreEqual(3, turret.TurretLevel);
        Assert.AreEqual(60f, turret.Damage);
        Assert.AreEqual(1.75f, turret.FireRate, 0.001f);
        Assert.AreEqual("Pháo cấp 3", TowerDefTurret.GetTurretName(3));

        // Level 3 -> 4
        bool up4 = turret.TryUpgradeFree();
        Assert.IsTrue(up4);
        Assert.AreEqual(4, turret.TurretLevel);
        Assert.AreEqual(80f, turret.Damage);
        Assert.AreEqual(2.05f, turret.FireRate, 0.001f);
        Assert.AreEqual("Pháo cấp 4", TowerDefTurret.GetTurretName(4));

        // Level 4 -> 5 (Max)
        bool up5 = turret.TryUpgradeFree();
        Assert.IsTrue(up5);
        Assert.AreEqual(5, turret.TurretLevel);
        Assert.AreEqual(100f, turret.Damage);
        Assert.AreEqual(2.35f, turret.FireRate, 0.001f);
        Assert.AreEqual("Pháo tối thượng (Lv.5)", TowerDefTurret.GetTurretName(5));
        Assert.IsTrue(turret.IsMaxLevel);

        // Cannot upgrade past max level
        Assert.IsFalse(turret.TryUpgradeFree());
    }

    [Test]
    public void UIController_OpensTurretUpgradeModalSuccessfully()
    {
        GameObject canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(TowerDefUIController));
        canvasObj.transform.SetParent(rootObj.transform);
        TowerDefUIController ui = canvasObj.GetComponent<TowerDefUIController>();

        GameObject cellObj = new GameObject("Cell", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGridCell));
        cellObj.transform.SetParent(rootObj.transform);
        GameObject sRoot = new GameObject("StructureRoot", typeof(RectTransform));
        sRoot.transform.SetParent(cellObj.transform);
        GameObject sImgObj = new GameObject("StructureImg", typeof(Image));
        sImgObj.transform.SetParent(sRoot.transform);
        GameObject gunObj = new GameObject("Gun", typeof(RectTransform), typeof(Image));
        gunObj.transform.SetParent(sRoot.transform);

        TowerDefGridCell cell = cellObj.GetComponent<TowerDefGridCell>();
        cell.SetupCell(2, 2, cellObj.GetComponent<Image>(), cellObj.GetComponent<Button>(), sRoot, sImgObj.GetComponent<Image>(), gunObj.transform, null);
        cell.PlaceStructure(TowerDefStructureType.Turret, null, null, 1);

        ui.OpenUpgradeModal(cell);

        Transform modal = canvasObj.transform.Find("StructureUpgradeModal");
        Assert.IsNotNull(modal);
        Assert.IsTrue(modal.gameObject.activeSelf);

        Transform frame = modal.Find("ModalFrame");
        Assert.IsNotNull(frame);
        Assert.IsNotNull(frame.Find("TitleText"));
        Assert.IsNotNull(frame.Find("Row1_GoldUpgrade"));
        Assert.IsNotNull(frame.Find("Row2_AdsUpgrade"));
        ui.CloseAllModals();
        Assert.IsFalse(modal.gameObject.activeSelf);
    }

    [Test]
    public void Gate_VisualUpdatesOnUpgrade()
    {
        GameObject gateObj = new GameObject("TestGate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGate));
        gateObj.transform.SetParent(rootObj.transform);
        TowerDefGate gate = gateObj.GetComponent<TowerDefGate>();
        Image gateImg = gateObj.GetComponent<Image>();

        Sprite s1 = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
        Sprite s2 = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
        Sprite s3 = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
        Sprite s4 = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);

        gate.SetLevelSprites(new Sprite[] { s1, s2, s3, s4 });
        Assert.AreEqual(s1, gateImg.sprite);

        gate.TryUpgradeFree();
        Assert.AreEqual(2, gate.GateLevel);
        Assert.AreEqual(s2, gateImg.sprite);

        gate.TryUpgradeFree();
        Assert.AreEqual(3, gate.GateLevel);
        Assert.AreEqual(s3, gateImg.sprite);

        gate.TryUpgradeFree();
        Assert.AreEqual(4, gate.GateLevel);
        Assert.AreEqual(s4, gateImg.sprite);
    }

    [Test]
    public void GridCell_ClearStructure_RemovesAllVisualsAndComponents()
    {
        GameObject cellObj = new GameObject("Cell", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGridCell));
        cellObj.transform.SetParent(rootObj.transform);
        GameObject sRoot = new GameObject("StructureRoot", typeof(RectTransform));
        sRoot.transform.SetParent(cellObj.transform);
        GameObject sImgObj = new GameObject("StructureImg", typeof(Image));
        sImgObj.transform.SetParent(sRoot.transform);
        GameObject gunObj = new GameObject("Gun", typeof(RectTransform), typeof(Image));
        gunObj.transform.SetParent(sRoot.transform);
        GameObject dummyChild = new GameObject("DummyChild");
        dummyChild.transform.SetParent(sRoot.transform);

        TowerDefGridCell cell = cellObj.GetComponent<TowerDefGridCell>();
        cell.SetupCell(2, 1, cellObj.GetComponent<Image>(), cellObj.GetComponent<Button>(), sRoot, sImgObj.GetComponent<Image>(), gunObj.transform, null);

        Sprite baseSpr = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
        Sprite gunSpr = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
        cell.PlaceStructure(TowerDefStructureType.Turret, baseSpr, gunSpr, 1);

        Assert.AreEqual(TowerDefStructureType.Turret, cell.CurrentType);
        Assert.IsTrue(sRoot.activeSelf);
        Assert.IsNotNull(cell.Turret);

        cell.ClearStructure();

        Assert.AreEqual(TowerDefStructureType.None, cell.CurrentType);
        Assert.IsFalse(sRoot.activeSelf);
        Assert.IsNull(cell.Turret);
        Assert.IsNull(sImgObj.GetComponent<Image>().sprite);
        Assert.IsNull(gunObj.GetComponent<Image>().sprite);
    }

    [Test]
    public void UpgradeBadge_ShowsOnlyWhenGoldIsSufficientAndNotAtStart()
    {
        // 1. Test GridCell (Empty cell must not show badge, occupied cell shows only if gold >= UpgradeCost)
        GameObject cellObj = new GameObject("Cell", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGridCell));
        cellObj.transform.SetParent(rootObj.transform);
        GameObject sRoot = new GameObject("StructureRoot", typeof(RectTransform));
        sRoot.transform.SetParent(cellObj.transform);
        GameObject sImgObj = new GameObject("StructureImg", typeof(Image));
        sImgObj.transform.SetParent(sRoot.transform);
        GameObject gunObj = new GameObject("Gun", typeof(RectTransform), typeof(Image));
        gunObj.transform.SetParent(sRoot.transform);
        GameObject badgeObj = new GameObject("Badge");
        badgeObj.transform.SetParent(cellObj.transform);

        TowerDefGridCell cell = cellObj.GetComponent<TowerDefGridCell>();
        cell.SetupCell(2, 1, cellObj.GetComponent<Image>(), cellObj.GetComponent<Button>(), sRoot, sImgObj.GetComponent<Image>(), gunObj.transform, badgeObj);

        // Empty cell: badge must be inactive even with plenty of gold
        cell.RefreshUpgradeBadge(9999);
        Assert.IsFalse(badgeObj.activeSelf, "Badge should never be active on an empty cell");

        // Place turret
        cell.PlaceStructure(TowerDefStructureType.Turret, null, null, 1);
        int turretCost = cell.Turret.UpgradeCost;

        // With 0 gold or insufficient gold: badge must be inactive
        cell.RefreshUpgradeBadge(turretCost - 1);
        Assert.IsFalse(badgeObj.activeSelf, "Badge should not be active when player lacks gold");

        // With sufficient gold: badge becomes active
        cell.RefreshUpgradeBadge(turretCost);
        Assert.IsTrue(badgeObj.activeSelf, "Badge should be active when player has sufficient gold");

        // 2. Test Gate (Badge shows only if gold >= UpgradeCost)
        GameObject gateObj = new GameObject("Gate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGate));
        gateObj.transform.SetParent(rootObj.transform);
        GameObject gateBadge = new GameObject("GateBadge");
        gateBadge.transform.SetParent(gateObj.transform);

        TowerDefGate gate = gateObj.GetComponent<TowerDefGate>();
        gate.Setup(50f, null, gateBadge, gateObj.GetComponent<Button>(), 12, 20f);

        // At start or with insufficient gold: badge inactive
        gate.RefreshUpgradeBadge(11);
        Assert.IsFalse(gateBadge.activeSelf, "Gate badge should not be active when player lacks gold");

        // With sufficient gold: badge active
        gate.RefreshUpgradeBadge(12);
        Assert.IsTrue(gateBadge.activeSelf, "Gate badge should be active when player has sufficient gold");
    }

    [Test]
    public void UIController_OpensBuildModalOnEmptyCellClick()
    {
        GameObject canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(TowerDefUIController));
        canvasObj.transform.SetParent(rootObj.transform);
        TowerDefUIController ui = canvasObj.GetComponent<TowerDefUIController>();

        GameObject cellObj = new GameObject("Cell", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TowerDefGridCell));
        cellObj.transform.SetParent(rootObj.transform);
        GameObject sRoot = new GameObject("StructureRoot", typeof(RectTransform));
        sRoot.transform.SetParent(cellObj.transform);
        GameObject sImgObj = new GameObject("StructureImg", typeof(Image));
        sImgObj.transform.SetParent(sRoot.transform);

        TowerDefGridCell cell = cellObj.GetComponent<TowerDefGridCell>();
        cell.SetupCell(2, 3, cellObj.GetComponent<Image>(), cellObj.GetComponent<Button>(), sRoot, sImgObj.GetComponent<Image>(), null, null);

        Assert.IsFalse(cell.IsOccupied);

        ui.OpenBuildModal(cell);

        Transform buildModal = canvasObj.transform.Find("BuildModal");
        Assert.IsNotNull(buildModal);
        Assert.IsTrue(buildModal.gameObject.activeSelf);

        Transform frame = buildModal.Find("ModalFrame");
        Assert.IsNotNull(frame);
        Assert.IsNotNull(frame.Find("TitleText"));
        Assert.IsNotNull(frame.Find("Row1_BuildTurret"));
        Assert.IsNotNull(frame.Find("Row2_BuildGenerator"));

        ui.CloseAllModals();
        Assert.IsFalse(buildModal.gameObject.activeSelf);
    }
}
#endif

