using NUnit.Framework;
using UnityEngine;
using System.Reflection;

public class PGE_ArtifactSystemTests
{
    [Test]
    public void Test01_ArtifactData_FormatsStatsCorrectly()
    {
        ArtifactData battery = ScriptableObject.CreateInstance<ArtifactData>();
        battery.id = "spare_battery";
        battery.artifactName = "Spare Battery";
        battery.statType = ArtifactStatType.MaxHealthPercent;
        battery.statValue = 15f;
        Assert.AreEqual("HP +15%", battery.GetFormattedStatText());

        ArtifactData scales = ScriptableObject.CreateInstance<ArtifactData>();
        scales.statType = ArtifactStatType.RangedDefensePercent;
        scales.statValue = 10f;
        Assert.AreEqual("Ranged DEF +10%", scales.GetFormattedStatText());

        ArtifactData cooler = ScriptableObject.CreateInstance<ArtifactData>();
        cooler.statType = ArtifactStatType.TurretAttackSpeedPercent;
        cooler.statValue = 20f;
        Assert.AreEqual("Turret ATK Speed +20%", cooler.GetFormattedStatText());

        ArtifactData usb = ScriptableObject.CreateInstance<ArtifactData>();
        usb.statType = ArtifactStatType.AllWeaponsDamagePercent;
        usb.statValue = 9f;
        Assert.AreEqual("All Weapons' ATK +9%", usb.GetFormattedStatText());
    }

    [Test]
    public void Test02_ArtifactDatabase_InitializesAndSelectsArtifact()
    {
        ArtifactDatabase db = ScriptableObject.CreateInstance<ArtifactDatabase>();
        db.InitializeDefaults();

        Assert.GreaterOrEqual(db.artifacts.Count, 4);

        ArtifactData foundBattery = db.GetById("spare_battery");
        Assert.IsNotNull(foundBattery);
        Assert.AreEqual("Spare Battery", foundBattery.artifactName);

        ArtifactData randomArt = db.GetRandomArtifact();
        Assert.IsNotNull(randomArt);
    }

    [Test]
    public void Test03_PlayerArtifactInventory_AppliesStatBuffs()
    {
        GameObject playerObj = new GameObject("TestPlayer");
        PlayerHealth health = playerObj.AddComponent<PlayerHealth>();
        health.SetMaxHealth(200, true);

        PlayerAutoShooter shooter = playerObj.AddComponent<PlayerAutoShooter>();
        PlayerArtifactInventory inv = playerObj.AddComponent<PlayerArtifactInventory>();

        // 1. Equip Spare Battery (HP +15%)
        ArtifactData battery = ScriptableObject.CreateInstance<ArtifactData>();
        battery.id = "spare_battery";
        battery.statType = ArtifactStatType.MaxHealthPercent;
        battery.statValue = 15f;

        bool equipped = inv.EquipArtifact(battery);
        Assert.IsTrue(equipped);
        Assert.AreEqual(1, inv.EquippedCount);
        Assert.AreEqual(230, health.MaxHealth); // 200 + 15% of 200 = 230

        // 2. Equip Kung Fu Data USB (All Weapons' ATK +9%)
        ArtifactData usb = ScriptableObject.CreateInstance<ArtifactData>();
        usb.id = "kung_fu_usb";
        usb.statType = ArtifactStatType.AllWeaponsDamagePercent;
        usb.statValue = 9f;

        inv.EquipArtifact(usb);
        Assert.AreEqual(2, inv.EquippedCount);
        Assert.AreEqual(1.09f, shooter.ArtifactDamageMultiplier, 0.001f);

        // 3. Equip Strong Cooler (Turret ATK Speed +20%)
        ArtifactData cooler = ScriptableObject.CreateInstance<ArtifactData>();
        cooler.id = "strong_cooler";
        cooler.statType = ArtifactStatType.TurretAttackSpeedPercent;
        cooler.statValue = 20f;

        inv.EquipArtifact(cooler);
        Assert.AreEqual(1.20f, GunTurret.GlobalTurretFireRateMultiplier, 0.001f);

        // Cleanup
        Object.DestroyImmediate(playerObj);
    }

    [Test]
    public void Test04_ArtifactFoundModal_LifecycleAndCallbacks()
    {
        GameObject canvasObj = new GameObject("TestCanvas", typeof(Canvas));
        Canvas canvas = canvasObj.GetComponent<Canvas>();

        ArtifactFoundModalController modal = ArtifactFoundModalController.CreateRuntimeModal(canvas.transform as RectTransform);
        Assert.IsNotNull(modal);

        ArtifactData battery = ScriptableObject.CreateInstance<ArtifactData>();
        battery.id = "spare_battery";
        battery.artifactName = "Spare Battery";
        battery.loreDescription = "Eco-friendly product you can recharge.";
        battery.statType = ArtifactStatType.MaxHealthPercent;
        battery.statValue = 15f;

        bool getClicked = false;
        bool throwClicked = false;

        modal.Show(battery,
            onGet: () => getClicked = true,
            onThrowAway: () => throwClicked = true
        );

        Assert.AreEqual(0f, Time.timeScale);

        modal.OnGetClicked();
        Assert.IsTrue(getClicked);
        Assert.AreEqual(1f, Time.timeScale);

        // Test throw away
        modal.Show(battery,
            onGet: () => getClicked = true,
            onThrowAway: () => throwClicked = true
        );
        modal.OnThrowAwayClicked();
        Assert.IsTrue(throwClicked);
        Assert.AreEqual(1f, Time.timeScale);

        Object.DestroyImmediate(canvasObj);
    }

    [Test]
    public void Test05_ArtifactBoxPickup_ActiveBoxesTracking()
    {
        ArtifactBoxPickup.ClearActiveBoxesForTesting();
        Assert.AreEqual(0, ArtifactBoxPickup.ActiveBoxes.Count);

        GameObject boxObj1 = new GameObject("Box1", typeof(BoxCollider2D), typeof(ArtifactBoxPickup));
        ArtifactBoxPickup box1 = boxObj1.GetComponent<ArtifactBoxPickup>();
        Assert.AreEqual(1, ArtifactBoxPickup.ActiveBoxes.Count);
        Assert.AreSame(box1, ArtifactBoxPickup.ActiveBoxes[0]);

        GameObject boxObj2 = new GameObject("Box2", typeof(BoxCollider2D), typeof(ArtifactBoxPickup));
        ArtifactBoxPickup box2 = boxObj2.GetComponent<ArtifactBoxPickup>();
        Assert.AreEqual(2, ArtifactBoxPickup.ActiveBoxes.Count);

        Object.DestroyImmediate(boxObj1);
        Assert.AreEqual(1, ArtifactBoxPickup.ActiveBoxes.Count);
        Assert.AreSame(box2, ArtifactBoxPickup.ActiveBoxes[0]);

        Object.DestroyImmediate(boxObj2);
        Assert.AreEqual(0, ArtifactBoxPickup.ActiveBoxes.Count);
    }

    [Test]
    public void Test06_ArtifactBoxIndicator_CalculatesPositionAtScreenEdge()
    {
        Vector2 canvasSize = new Vector2(1080f, 1920f);
        Vector2 indicatorSize = new Vector2(130f, 130f);
        float edgePadding = 15f;

        // Box is offscreen to the right (viewport x = 1.5, y = 0.5)
        Vector3 offscreenRightVp = new Vector3(1.5f, 0.5f, 1f);
        Assert.IsFalse(WaveHUDController.IsViewportPositionVisible(offscreenRightVp, 0.02f));

        Vector2 rightPos = WaveHUDController.CalculateBossIndicatorPosition(
            offscreenRightVp,
            canvasSize,
            indicatorSize,
            edgePadding);

        float expectedHalfWidth = canvasSize.x * 0.5f - indicatorSize.x * 0.5f - edgePadding;
        Assert.AreEqual(expectedHalfWidth, rightPos.x, 0.01f);
        Assert.AreEqual(0f, rightPos.y, 0.01f);

        // Box is offscreen to the top (viewport x = 0.5, y = 1.8)
        Vector3 offscreenTopVp = new Vector3(0.5f, 1.8f, 1f);
        Vector2 topPos = WaveHUDController.CalculateBossIndicatorPosition(
            offscreenTopVp,
            canvasSize,
            indicatorSize,
            edgePadding);

        float expectedHalfHeight = canvasSize.y * 0.5f - indicatorSize.y * 0.5f - edgePadding;
        Assert.AreEqual(0f, topPos.x, 0.01f);
        Assert.AreEqual(expectedHalfHeight, topPos.y, 0.01f);
    }

    [Test]
    public void Test07_EnemySpawner_ArtifactDropConfigurationAndSpawn()
    {
        ArtifactBoxPickup.ClearActiveBoxesForTesting();
        Assert.AreEqual(0, ArtifactBoxPickup.ActiveBoxes.Count);

        GameObject spawnerObj = new GameObject("TestSpawner", typeof(EnemySpawner));
        EnemySpawner spawner = spawnerObj.GetComponent<EnemySpawner>();

        // Verify default drop chances (Eliminated - 0f)
        Assert.AreEqual(0f, spawner.BossArtifactDropChance);
        Assert.AreEqual(0f, spawner.EliteArtifactDropChance);
        Assert.AreEqual(0f, spawner.NormalCreepArtifactDropChance);
        Assert.IsFalse(spawner.DropArtifactOnWaveClear);
        Assert.AreEqual(5, spawner.MaxArtifactDropsPerChapter);

        // Test DropTable spawning directly
        Vector3 spawnPos = new Vector3(5f, 5f, 0f);
        GameObject box = DropTable.SpawnArtifactBox(spawnPos);
        Assert.IsNotNull(box);
        Assert.AreEqual(1, ArtifactBoxPickup.ActiveBoxes.Count);
        Assert.AreEqual(spawnPos, box.transform.position);

        Object.DestroyImmediate(box);
        Object.DestroyImmediate(spawnerObj);
        Assert.AreEqual(0, ArtifactBoxPickup.ActiveBoxes.Count);
    }

    [Test]
    public void Test08_ChapterArtifactMapSpawning_And_PauseModalArtifactDisplay()
    {
        ArtifactBoxPickup.ClearActiveBoxesForTesting();
        Assert.AreEqual(0, ArtifactBoxPickup.ActiveBoxes.Count);

        GameObject spawnerObj = new GameObject("TestSpawner", typeof(EnemySpawner));
        EnemySpawner spawner = spawnerObj.GetComponent<EnemySpawner>();

        // 1. Verify default 5 drops per chapter
        Assert.AreEqual(5, spawner.MaxArtifactDropsPerChapter);
        Assert.AreEqual(0, spawner.ArtifactsSpawnedInChapter);

        // Can be changed to another number
        spawner.MaxArtifactDropsPerChapter = 3;
        Assert.AreEqual(3, spawner.MaxArtifactDropsPerChapter);
        spawner.MaxArtifactDropsPerChapter = 5;

        // 2. Test spawning on map
        GameObject box1 = spawner.SpawnRandomArtifactOnMap();
        Assert.IsNotNull(box1);
        Assert.AreEqual(1, spawner.ArtifactsSpawnedInChapter);
        Assert.AreEqual(1, ArtifactBoxPickup.ActiveBoxes.Count);

        // 3. Test PauseModal Artifact display
        GameObject canvasObj = new GameObject("Canvas", typeof(Canvas));
        GameObject pauseModalObj = new GameObject("PauseModal", typeof(PauseModalController));
        pauseModalObj.transform.SetParent(canvasObj.transform, false);
        PauseModalController pauseCtrl = pauseModalObj.GetComponent<PauseModalController>();

        GameObject artPanel = new GameObject("ArtifactPanel", typeof(RectTransform));
        artPanel.transform.SetParent(pauseModalObj.transform, false);

        // Set up player inventory with 1 artifact
        GameObject playerObj = new GameObject("Player", typeof(PlayerArtifactInventory), typeof(PlayerHealth));
        PlayerArtifactInventory inv = playerObj.GetComponent<PlayerArtifactInventory>();
        ArtifactData testArt = ScriptableObject.CreateInstance<ArtifactData>();
        testArt.id = "test_battery";
        testArt.artifactName = "Spare Battery";
        testArt.statType = ArtifactStatType.MaxHealthPercent;
        testArt.statValue = 15f;
        inv.EquipArtifact(testArt);

        pauseCtrl.AutoWireTabButtonsAndSettings();
        pauseCtrl.RefreshEquippedArtifacts();

        // Verify card was spawned inside artifactPanel
        Transform contentTr = artPanel.transform.Find("Viewport/Content") ?? artPanel.transform.Find("Content");
        Assert.IsNotNull(contentTr);
        Transform spawnedCard = contentTr.Find("ArtifactCard_test_battery");
        Assert.IsNotNull(spawnedCard);

        // Cleanup
        Object.DestroyImmediate(playerObj);
        Object.DestroyImmediate(box1);
        Object.DestroyImmediate(canvasObj);
        Object.DestroyImmediate(spawnerObj);
        ArtifactBoxPickup.ClearActiveBoxesForTesting();
    }

    [Test]
    public void Test09_ArtifactVisibility_And_DamageNumberDisplay()
    {
        ArtifactBoxPickup.ClearActiveBoxesForTesting();

        // 1. Kiểm tra vị trí và Sorting Layer của ArtifactBoxPickup
        Vector3 spawnTarget = new Vector3(12.5f, 7.5f, 0f);
        GameObject box = DropTable.SpawnArtifactBox(spawnTarget);
        Assert.IsNotNull(box);

        ArtifactBoxPickup pickup = box.GetComponent<ArtifactBoxPickup>();
        Assert.IsNotNull(pickup);
        Assert.AreEqual(spawnTarget.x, box.transform.position.x, 0.001f);
        Assert.AreEqual(spawnTarget.y, box.transform.position.y, 0.001f);

        // Kiểm tra Rigidbody2D đã được gắn để đảm bảo physics trigger hoạt động chuẩn
        Rigidbody2D rb = box.GetComponent<Rigidbody2D>();
        Assert.IsNotNull(rb);
        Assert.AreEqual(RigidbodyType2D.Kinematic, rb.bodyType);

        // Kiểm tra Sorting Layer không bao giờ bị rơi vào Default (chìm dưới sàn map)
        SpriteRenderer sr = box.GetComponentInChildren<SpriteRenderer>();
        Assert.IsNotNull(sr);
        Assert.AreNotEqual("Default", sr.sortingLayerName);
        Assert.GreaterOrEqual(sr.sortingOrder, 50);

        // 2. Kiểm tra DamageNumber Sorting Layer resolution
        string resolvedUI = DamageNumber.GetResolvedSortingLayer("UI");
        Assert.IsNotEmpty(resolvedUI);
        Assert.AreNotEqual("Default", resolvedUI);

        // 3. Kiểm tra GameSettings.ShowDamage luôn bật
        GameSettings.ShowDamage = true;
        Assert.IsTrue(GameSettings.ShowDamage);

        // Cleanup
        Object.DestroyImmediate(box);
        ArtifactBoxPickup.ClearActiveBoxesForTesting();
    }

    [Test]
    public void Test10_NoStackOverflow_WhenPickingUpBox_And_OpeningModal()
    {
        // 1. Tạo Canvas cho Scene
        GameObject canvasObj = new GameObject("MainCanvas", typeof(Canvas));
        Canvas canvas = canvasObj.GetComponent<Canvas>();

        // 2. Tạo Player
        GameObject playerObj = new GameObject("Player", typeof(PlayerHealth));

        // 3. Tạo ArtifactBoxPickup
        GameObject boxObj = DropTable.SpawnArtifactBox(new Vector3(5f, 5f, 0f));
        ArtifactBoxPickup pickup = boxObj.GetComponent<ArtifactBoxPickup>();

        // 4. Kích hoạt mở hộp - Trước đây sẽ văng StackOverflowException
        Assert.DoesNotThrow(() =>
        {
            pickup.TriggerOpenArtifact();
        });

        // 5. Kiểm tra modal đã mở và hiển thị thành công
        ArtifactFoundModalController modal = ArtifactFoundModalController.Instance;
        Assert.IsNotNull(modal);

        // 6. Nhấn nút Get để nhận vật phẩm
        modal.OnGetClicked();

        // 7. Kiểm tra inventory người chơi đã nhận được artifact
        PlayerArtifactInventory inv = PlayerArtifactInventory.Instance;
        Assert.IsNotNull(inv);
        Assert.AreEqual(1, inv.EquippedCount);

        // Cleanup
        Object.DestroyImmediate(boxObj);
        Object.DestroyImmediate(playerObj);
        Object.DestroyImmediate(canvasObj);
        ArtifactBoxPickup.ClearActiveBoxesForTesting();
    }

    [Test]
    public void Test11_ArtifactGrid_Arranges4PerRow()
    {
        // Kiểm tra logic sắp xếp lưới 4 cột / 1 hàng
        const int columns = 4;
        for (int i = 0; i < 8; i++)
        {
            int col = i % columns;
            int row = i / columns;

            if (i < 4)
            {
                Assert.AreEqual(0, row, $"Item {i} should be on row 0");
                Assert.AreEqual(i, col, $"Item {i} should be on col {i}");
            }
            else
            {
                Assert.AreEqual(1, row, $"Item {i} should be on row 1");
                Assert.AreEqual(i - 4, col, $"Item {i} should be on col {i - 4}");
            }
        }
    }

    [Test]
    public void Test12_TitaniumFabric_DataAndFormatting()
    {
        ArtifactDatabase db = ScriptableObject.CreateInstance<ArtifactDatabase>();
        db.InitializeDefaults();

        ArtifactData titanium = db.GetById("titanium_fabric");
        Assert.IsNotNull(titanium, "Titanium Fabric should be present in default database");
        Assert.AreEqual("Titanium Fabric", titanium.artifactName);
        Assert.AreEqual("Sturdy titanium. Covers the body.", titanium.loreDescription);
        Assert.AreEqual(ArtifactStatType.DamageReduction, titanium.statType);
        Assert.AreEqual(10f, titanium.statValue);
        Assert.AreEqual("DEF +10", titanium.GetFormattedStatText());
    }

    [Test]
    public void Test13_PauseModal_ArtifactDetailDialog_ShowAndClose()
    {
        GameObject root = new GameObject("PauseModalRoot", typeof(RectTransform));
        PauseModalController pauseCtrl = root.AddComponent<PauseModalController>();

        GameObject detailPanel = new GameObject("ArtifactDetailDialog", typeof(RectTransform));
        detailPanel.transform.SetParent(root.transform);
        detailPanel.SetActive(false);

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        iconObj.transform.SetParent(detailPanel.transform);
        UnityEngine.UI.Image iconImg = iconObj.GetComponent<UnityEngine.UI.Image>();

        GameObject nameObj = new GameObject("NameText", typeof(RectTransform));
        nameObj.transform.SetParent(detailPanel.transform);
        TMPro.TMP_Text nameText = nameObj.AddComponent<TMPro.TextMeshProUGUI>();

        GameObject loreObj = new GameObject("LoreText", typeof(RectTransform));
        loreObj.transform.SetParent(detailPanel.transform);
        TMPro.TMP_Text loreText = loreObj.AddComponent<TMPro.TextMeshProUGUI>();

        GameObject statObj = new GameObject("StatText", typeof(RectTransform));
        statObj.transform.SetParent(detailPanel.transform);
        TMPro.TMP_Text statText = statObj.AddComponent<TMPro.TextMeshProUGUI>();

        GameObject okBtnObj = new GameObject("OkButton", typeof(RectTransform), typeof(UnityEngine.UI.Button));
        okBtnObj.transform.SetParent(detailPanel.transform);
        UnityEngine.UI.Button okBtn = okBtnObj.GetComponent<UnityEngine.UI.Button>();

        pauseCtrl.SetArtifactDetailDialogForTesting(detailPanel, iconImg, nameText, loreText, statText, okBtn);

        ArtifactData titanium = ScriptableObject.CreateInstance<ArtifactData>();
        titanium.id = "titanium_fabric";
        titanium.artifactName = "Titanium Fabric";
        titanium.loreDescription = "Sturdy titanium. Covers the body.";
        titanium.statType = ArtifactStatType.DamageReduction;
        titanium.statValue = 10f;

        // Show
        pauseCtrl.ShowArtifactDetail(titanium);
        Assert.IsTrue(detailPanel.activeSelf);
        Assert.AreEqual("Titanium Fabric", nameText.text);
        Assert.AreEqual("Sturdy titanium. Covers the body.", loreText.text);
        Assert.AreEqual("DEF +10", statText.text);

        // Close with OK button
        okBtn.onClick.Invoke();
        Assert.IsFalse(detailPanel.activeSelf);

        Object.DestroyImmediate(titanium);
        Object.DestroyImmediate(root);
    }

    [Test]
    public void Test14_TimedArtifactSpawning_InitialDelay_10To15Seconds()
    {
        ArtifactBoxPickup.ClearActiveBoxesForTesting();

        GameObject spawnerObj = new GameObject("Spawner", typeof(EnemySpawner));
        EnemySpawner spawner = spawnerObj.GetComponent<EnemySpawner>();

        // Cấu hình timed spawning với giá trị mặc định 10-15s
        spawner.UseTimedArtifactSpawning = true;
        spawner.MinInitialArtifactDelay = 10f;
        spawner.MaxInitialArtifactDelay = 15f;
        spawner.ResetArtifactChapterCount();

        // 1. Kiểm tra thời gian đếm ban đầu nằm trong khoảng 10 - 15 giây
        Assert.IsTrue(spawner.IsInitialArtifactSpawn);
        Assert.GreaterOrEqual(spawner.ArtifactSpawnTimer, 10f);
        Assert.LessOrEqual(spawner.ArtifactSpawnTimer, 15f);

        // 2. Mô phỏng trôi qua 5s: chưa đạt 10s nên chưa được sinh hộp
        spawner.UpdateTimedArtifactSpawning(5f);
        Assert.AreEqual(0, ArtifactBoxPickup.ActiveBoxes.Count);
        Assert.IsTrue(spawner.IsInitialArtifactSpawn);

        // 3. Mô phỏng trôi qua tiếp phần thời gian còn lại (11s nữa, tổng cộng 16s > 15s)
        spawner.UpdateTimedArtifactSpawning(11f);
        Assert.AreEqual(1, ArtifactBoxPickup.ActiveBoxes.Count);
        Assert.IsFalse(spawner.IsInitialArtifactSpawn);
        Assert.AreEqual(1, spawner.ArtifactsSpawnedInChapter);

        // Cleanup
        ArtifactBoxPickup.ClearActiveBoxesForTesting();
        Object.DestroyImmediate(spawnerObj);
    }

    [Test]
    public void Test15_TimedArtifactSpawning_RespawnWait_20To60Seconds()
    {
        ArtifactBoxPickup.ClearActiveBoxesForTesting();

        GameObject spawnerObj = new GameObject("Spawner", typeof(EnemySpawner));
        EnemySpawner spawner = spawnerObj.GetComponent<EnemySpawner>();

        spawner.SetArtifactTimersForTesting(10f, 15f, 20f, 60f, waitPickup: true);

        // Lần đầu xuất hiện sau 15s
        spawner.UpdateTimedArtifactSpawning(15.5f);
        Assert.AreEqual(1, ArtifactBoxPickup.ActiveBoxes.Count);
        Assert.AreEqual(1, spawner.ArtifactsSpawnedInChapter);

        // Hộp đang trên bản đồ, đếm thêm 50s cũng không được sinh hộp thứ 2 khi chưa nhặt
        spawner.UpdateTimedArtifactSpawning(50f);
        Assert.AreEqual(1, ArtifactBoxPickup.ActiveBoxes.Count);

        // Người chơi nhặt hộp -> map trống
        ArtifactBoxPickup.ClearActiveBoxesForTesting();
        Assert.AreEqual(0, ArtifactBoxPickup.ActiveBoxes.Count);

        // Bắt đầu chu kỳ đợi xuất hiện lại (20 - 60s)
        spawner.UpdateTimedArtifactSpawning(0.1f);
        Assert.GreaterOrEqual(spawner.ArtifactSpawnTimer, 20f);
        Assert.LessOrEqual(spawner.ArtifactSpawnTimer, 60f);
        Assert.AreEqual(0, ArtifactBoxPickup.ActiveBoxes.Count);

        // Trôi qua 15s: chưa đủ 20s tối thiểu, chưa được sinh lại
        spawner.UpdateTimedArtifactSpawning(15f);
        Assert.AreEqual(0, ArtifactBoxPickup.ActiveBoxes.Count);

        // Trôi qua thêm 46s (tổng cộng 61s > 60s tối đa): hộp thứ 2 xuất hiện lại!
        spawner.UpdateTimedArtifactSpawning(46f);
        Assert.AreEqual(1, ArtifactBoxPickup.ActiveBoxes.Count);
        Assert.AreEqual(2, spawner.ArtifactsSpawnedInChapter);

        // Cleanup
        ArtifactBoxPickup.ClearActiveBoxesForTesting();
        Object.DestroyImmediate(spawnerObj);
    }

    [Test]
    public void Test16_TimedArtifactSpawning_RespectsChapterLimit()
    {
        ArtifactBoxPickup.ClearActiveBoxesForTesting();

        GameObject spawnerObj = new GameObject("Spawner", typeof(EnemySpawner));
        EnemySpawner spawner = spawnerObj.GetComponent<EnemySpawner>();

        spawner.MaxArtifactDropsPerChapter = 1;
        spawner.SetArtifactTimersForTesting(10f, 15f, 20f, 60f, waitPickup: true);

        // Hộp 1 xuất hiện
        spawner.UpdateTimedArtifactSpawning(16f);
        Assert.AreEqual(1, spawner.ArtifactsSpawnedInChapter);
        Assert.AreEqual(1, ArtifactBoxPickup.ActiveBoxes.Count);

        // Nhặt hộp 1
        ArtifactBoxPickup.ClearActiveBoxesForTesting();

        // Đợi thêm 100s: đã đạt giới hạn 1 hộp của Chapter, không sinh thêm
        spawner.UpdateTimedArtifactSpawning(100f);
        Assert.AreEqual(1, spawner.ArtifactsSpawnedInChapter);
        Assert.AreEqual(0, ArtifactBoxPickup.ActiveBoxes.Count);

        // Cleanup
        Object.DestroyImmediate(spawnerObj);
    }

    [Test]
    public void Test17_EnemySpawner_RadiusAndDelayConfigurations()
    {
        GameObject spawnerObj = new GameObject("Spawner", typeof(EnemySpawner));
        EnemySpawner spawner = spawnerObj.GetComponent<EnemySpawner>();

        // Kiểm tra các giá trị mặc định theo yêu cầu người dùng
        Assert.AreEqual(3f, spawner.MinSpawnRadius, "minSpawnRadius phải là 3m");
        Assert.AreEqual(5f, spawner.MaxSpawnRadius, "maxSpawnRadius phải là 5m");
        Assert.AreEqual(10f, spawner.MaxDespawnDistance, "maxDespawnDistance phải là 10m");
        Assert.AreEqual(2.0f, spawner.DespawnCheckInterval, "despawnCheckInterval phải là 2.0s");
        Assert.AreEqual(1.0f, spawner.InitialWaveSpawnDelay, "initialWaveSpawnDelay phải là 1.0s");

        // Kiểm tra khi bắt đầu Wave, lượt quái đầu tiên xuất hiện sau 1.0s
        spawner.GenerateDefaultWaves(1);
        spawner.StartWave(0);

        FieldInfo spawnTimerField = typeof(EnemySpawner).GetField("spawnTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(spawnTimerField);
        float currentSpawnTimer = (float)spawnTimerField.GetValue(spawner);
        Assert.AreEqual(1.0f, currentSpawnTimer, 0.01f, "Lượt quái đầu tiên của wave phải bắt đầu sau 1.0s");

        Object.DestroyImmediate(spawnerObj);
    }
}
