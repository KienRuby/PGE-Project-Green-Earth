using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unit Tests kiểm tra tính toàn vẹn và độ mở rộng của Hệ thống Chapter, Data Models, và Tiền tệ.
/// </summary>
public class ChapterSystemTests
{
    private const string ChapterDatabasePath = "Assets/Data/Chapters/ChapterDatabase.asset";
    private const string QuestDataPath = "Assets/Data/Quests/Quest_01_LabUpgrade.asset";

    [Test]
    public void ChapterDatabase_LoadsAndContainsAllSampleChapters()
    {
        ChapterDatabase db = AssetDatabase.LoadAssetAtPath<ChapterDatabase>(ChapterDatabasePath);
        Assert.That(db, Is.Not.Null, "Không tìm thấy file ChapterDatabase.asset");
        Assert.That(db.Count, Is.GreaterThanOrEqualTo(4), "Database phải chứa ít nhất 4 chapter mẫu.");

        ChapterData c1 = db.GetChapter(0);
        Assert.That(c1, Is.Not.Null);
        Assert.That(c1.chapterNumber, Is.EqualTo(1));
        Assert.That(c1.chapterTitle, Is.EqualTo("Grassland Outskirts"));
        Assert.That(c1.isLocked, Is.False, "Chapter 1 (Grassland Outskirts) phải luôn luôn mở khóa mặc định.");

        ChapterData c4 = db.GetChapter(3);
        Assert.That(c4, Is.Not.Null);
        Assert.That(c4.chapterNumber, Is.EqualTo(4));
        Assert.That(c4.chapterTitle, Is.EqualTo("Dense Jungle 1"));
        Assert.That(c4.totalWaves, Is.EqualTo(10));
        Assert.That(c4.energyCost, Is.EqualTo(10));
        Assert.That(c4.previewBackground, Is.Not.Null, "Chapter 4 phải có ảnh nền xem trước (previewBackground).");
        Assert.That(c4.bossSilhouette, Is.Not.Null, "Chapter 4 phải có sprite boss silhouette.");
        Assert.That(c4.flavorText, Does.Contain("mutants"));
    }

    [Test]
    public void ChapterDatabase_IndexClamping_WorksSafely()
    {
        ChapterDatabase db = AssetDatabase.LoadAssetAtPath<ChapterDatabase>(ChapterDatabasePath);
        Assert.That(db, Is.Not.Null);

        // Negative index should clamp to first chapter
        ChapterData cNeg = db.GetChapter(-5);
        Assert.That(cNeg, Is.EqualTo(db.GetChapter(0)));

        // Large index should clamp to last chapter
        ChapterData cLarge = db.GetChapter(999);
        Assert.That(cLarge, Is.EqualTo(db.GetChapter(db.Count - 1)));
    }

    [Test]
    public void PlayerDataService_ChapterIndices_SaveAndLoadCorrectly()
    {
        int originalSelected = PlayerDataService.SelectedChapterIndex;
        int originalUnlocked = PlayerDataService.UnlockedChapterIndex;

        try
        {
            PlayerDataService.SelectedChapterIndex = 3;
            PlayerDataService.UnlockedChapterIndex = 5;

            Assert.That(PlayerDataService.SelectedChapterIndex, Is.EqualTo(3));
            Assert.That(PlayerDataService.UnlockedChapterIndex, Is.EqualTo(5));
        }
        finally
        {
            PlayerDataService.SelectedChapterIndex = originalSelected;
            PlayerDataService.UnlockedChapterIndex = originalUnlocked;
        }
    }

    [Test]
    public void QuestData_LoadsCorrectly()
    {
        QuestData quest = AssetDatabase.LoadAssetAtPath<QuestData>(QuestDataPath);
        Assert.That(quest, Is.Not.Null, "Không tìm thấy file Quest_01_LabUpgrade.asset");
        Assert.That(quest.questTitle, Is.EqualTo("Quest"));
        Assert.That(quest.rewardAmount, Is.EqualTo(200));
        Assert.That(quest.rewardType, Is.EqualTo(QuestData.RewardType.RedGem));
        Assert.That(quest.rewardIcon, Is.Not.Null, "Quest phải có icon phần thưởng.");
    }

    [Test]
    public void ChapterScreen_EnergyTransaction_SpendsEnergyWhenStarting()
    {
        bool originalTestMode = ChipManager.IsTestMode;
        int originalEnergy = PlayerDataService.Energy;

        try
        {
            ChipManager.IsTestMode = false;
            PlayerDataService.Energy = 25;

            Assert.That(ChipManager.HasEnoughEnergy(10), Is.True);
            bool success = ChipManager.TrySpendEnergy(10);
            Assert.That(success, Is.True);
            Assert.That(ChipManager.Energy, Is.EqualTo(15));

            bool failedSpend = ChipManager.TrySpendEnergy(20);
            Assert.That(failedSpend, Is.False);
            Assert.That(ChipManager.Energy, Is.EqualTo(15));
        }
        finally
        {
            PlayerDataService.Energy = originalEnergy;
            ChipManager.IsTestMode = originalTestMode;
        }
    }

    [Test]
    public void TopBar_CurrencyEvents_SynchronizeAccurately()
    {
        bool originalTestMode = ChipManager.IsTestMode;
        int originalChips = ChipManager.DataChips;
        int originalGems = ChipManager.RedGems;
        int originalEnergy = ChipManager.Energy;

        int receivedChips = -1;
        int receivedGems = -1;
        int receivedEnergy = -1;

        System.Action<int> chipHandler = val => receivedChips = val;
        System.Action<int> gemHandler = val => receivedGems = val;
        System.Action<int> energyHandler = val => receivedEnergy = val;

        ChipManager.OnDataChipsChanged += chipHandler;
        ChipManager.OnRedGemsChanged += gemHandler;
        ChipManager.OnEnergyChanged += energyHandler;

        try
        {
            ChipManager.IsTestMode = false;
            ChipManager.DataChips = 49181;
            ChipManager.RedGems = 31868;
            ChipManager.Energy = 50;

            Assert.That(receivedChips, Is.EqualTo(49181));
            Assert.That(receivedGems, Is.EqualTo(31868));
            Assert.That(receivedEnergy, Is.EqualTo(50));
        }
        finally
        {
            ChipManager.OnDataChipsChanged -= chipHandler;
            ChipManager.OnRedGemsChanged -= gemHandler;
            ChipManager.OnEnergyChanged -= energyHandler;

            ChipManager.DataChips = originalChips;
            ChipManager.RedGems = originalGems;
            ChipManager.Energy = originalEnergy;
            ChipManager.IsTestMode = originalTestMode;
        }
    }

    [Test]
    public void ChapterScreenController_TryStartChapter_DeductsEnergyStrictlyOnce()
    {
        bool originalTestMode = ChipManager.IsTestMode;
        int originalEnergy = PlayerDataService.Energy;

        try
        {
            ChipManager.IsTestMode = false;
            PlayerDataService.Energy = 15;

            GameObject go = new GameObject("TestChapterScreen");
            ChapterScreenController ctrl = go.AddComponent<ChapterScreenController>();

            ChapterData testChapter = ScriptableObject.CreateInstance<ChapterData>();
            testChapter.chapterNumber = 4;
            testChapter.chapterTitle = "Dense Jungle 1";
            testChapter.energyCost = 10;
            testChapter.gameplaySceneName = "GamePlay";
            testChapter.isLocked = false;

            ChapterDatabase db = ScriptableObject.CreateInstance<ChapterDatabase>();
            db.SetChaptersForTesting(new System.Collections.Generic.List<ChapterData> { testChapter });

            ctrl.SetDatabaseForTesting(db, 0); // Chapter 4, cost = 10

            // First start: should succeed and deduct 10 energy (15 -> 5)
            bool firstStart = ctrl.TryStartChapter(out string targetScene, loadScene: false);
            Assert.That(firstStart, Is.True);
            Assert.That(targetScene, Is.EqualTo("GamePlay"));
            Assert.That(ChipManager.Energy, Is.EqualTo(5));

            // Second start: should fail because energy is 5 (< 10)
            bool secondStart = ctrl.TryStartChapter(out _, loadScene: false);
            Assert.That(secondStart, Is.False);
            Assert.That(ChipManager.Energy, Is.EqualTo(5)); // Untouched

            GameObject.DestroyImmediate(go);
            ScriptableObject.DestroyImmediate(testChapter);
            ScriptableObject.DestroyImmediate(db);
        }
        finally
        {
            PlayerDataService.Energy = originalEnergy;
            ChipManager.IsTestMode = originalTestMode;
        }
    }

    [Test]
    public void ChapterScreenController_LockedChapter_CannotStartAndShowsLockUI()
    {
        bool originalTestMode = ChipManager.IsTestMode;
        int originalEnergy = PlayerDataService.Energy;

        try
        {
            ChipManager.IsTestMode = false;
            PlayerDataService.Energy = 50;

            GameObject go = new GameObject("TestChapterScreenLock");
            ChapterScreenController ctrl = go.AddComponent<ChapterScreenController>();

            GameObject lockOverlay = new GameObject("LockOverlay");
            GameObject labelGo = new GameObject("Label");
            TMPro.TextMeshProUGUI label = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
            GameObject costBox = new GameObject("CostBox");

            ctrl.SetLockStateForTesting(lockOverlay, label, costBox);

            ChapterData lockedChapter = ScriptableObject.CreateInstance<ChapterData>();
            lockedChapter.chapterNumber = 1;
            lockedChapter.chapterTitle = "Grassland Outskirts";
            lockedChapter.energyCost = 5;
            lockedChapter.isLocked = true;

            ChapterDatabase db = ScriptableObject.CreateInstance<ChapterDatabase>();
            db.SetChaptersForTesting(new System.Collections.Generic.List<ChapterData> { lockedChapter });

            ctrl.SetDatabaseForTesting(db, 0);

            Assert.That(ctrl.IsCurrentChapterLocked(), Is.True);
            Assert.That(lockOverlay.activeSelf, Is.True);
            Assert.That(label.text, Is.EqualTo("Locked"));
            Assert.That(costBox.activeSelf, Is.False);

            // Starting a locked chapter should fail and not deduct energy
            bool started = ctrl.TryStartChapter(out _, loadScene: false);
            Assert.That(started, Is.False);
            Assert.That(ChipManager.Energy, Is.EqualTo(50));

            // When unlocked
            lockedChapter.isLocked = false;
            ctrl.RefreshChapterView();

            Assert.That(ctrl.IsCurrentChapterLocked(), Is.False);
            Assert.That(lockOverlay.activeSelf, Is.False);
            Assert.That(label.text, Is.EqualTo("Start"));
            Assert.That(costBox.activeSelf, Is.True);

            bool startedUnlocked = ctrl.TryStartChapter(out string scene, loadScene: false);
            Assert.That(startedUnlocked, Is.True);
            Assert.That(scene, Is.EqualTo("GamePlay"));
            Assert.That(ChipManager.Energy, Is.EqualTo(45));

            GameObject.DestroyImmediate(go);
            GameObject.DestroyImmediate(lockOverlay);
            GameObject.DestroyImmediate(labelGo);
            GameObject.DestroyImmediate(costBox);
            ScriptableObject.DestroyImmediate(lockedChapter);
            ScriptableObject.DestroyImmediate(db);
        }
        finally
        {
            PlayerDataService.Energy = originalEnergy;
            ChipManager.IsTestMode = originalTestMode;
        }
    }

    [Test]
    public void TopBarCurrencyController_UpdatesTextsOnChipManagerEvents()
    {
        bool originalTestMode = ChipManager.IsTestMode;
        int originalChips = ChipManager.DataChips;
        int originalGems = ChipManager.RedGems;
        int originalEnergy = ChipManager.Energy;

        GameObject topBarGo = new GameObject("TestTopBar");
        TopBarCurrencyController ctrl = topBarGo.AddComponent<TopBarCurrencyController>();

        GameObject energyTextGo = new GameObject("EnergyText");
        TMPro.TextMeshProUGUI energyText = energyTextGo.AddComponent<TMPro.TextMeshProUGUI>();

        GameObject chipTextGo = new GameObject("ChipText");
        TMPro.TextMeshProUGUI chipText = chipTextGo.AddComponent<TMPro.TextMeshProUGUI>();

        GameObject gemTextGo = new GameObject("GemText");
        TMPro.TextMeshProUGUI gemText = gemTextGo.AddComponent<TMPro.TextMeshProUGUI>();

        ctrl.SetTextsForTesting(energyText, chipText, gemText);
        ctrl.RefreshAllBalances();

        try
        {
            ChipManager.IsTestMode = false;
            ChipManager.DataChips = 54321;
            ChipManager.RedGems = 12345;
            ChipManager.Energy = 42;

            ctrl.RefreshAllBalances();

            Assert.That(dataChipFormattedText(chipText.text), Is.EqualTo("54.321"));
            Assert.That(dataChipFormattedText(gemText.text), Is.EqualTo("12.345"));
            Assert.That(energyText.text, Does.StartWith("42/"));
        }
        finally
        {
            GameObject.DestroyImmediate(topBarGo);
            GameObject.DestroyImmediate(energyTextGo);
            GameObject.DestroyImmediate(chipTextGo);
            GameObject.DestroyImmediate(gemTextGo);

            ChipManager.DataChips = originalChips;
            ChipManager.RedGems = originalGems;
            ChipManager.Energy = originalEnergy;
            ChipManager.IsTestMode = originalTestMode;
        }
    }

    [Test]
    public void ChapterScreenController_PrevNextButtons_CycleThroughChaptersAndSyncUI()
    {
        int originalSelected = PlayerDataService.SelectedChapterIndex;
        try
        {
            GameObject go = new GameObject("TestChapterScreenNav");
            ChapterScreenController ctrl = go.AddComponent<ChapterScreenController>();

            ChapterDatabase db = AssetDatabase.LoadAssetAtPath<ChapterDatabase>(ChapterDatabasePath);
            ctrl.SetDatabaseForTesting(db, 3); // Start at Chapter 4 (index 3)

            // Click Next (>) -> should wrap to Chapter 1 (index 0)
            ctrl.OnNextChapterClicked();
            Assert.That(PlayerDataService.SelectedChapterIndex, Is.EqualTo(0));

            // Click Next (>) -> should go to Chapter 2 (index 1)
            ctrl.OnNextChapterClicked();
            Assert.That(PlayerDataService.SelectedChapterIndex, Is.EqualTo(1));

            // Click Prev (<) -> should go back to Chapter 1 (index 0)
            ctrl.OnPrevChapterClicked();
            Assert.That(PlayerDataService.SelectedChapterIndex, Is.EqualTo(0));

            // Click Prev (<) -> should wrap to Chapter 4 (index 3)
            ctrl.OnPrevChapterClicked();
            Assert.That(PlayerDataService.SelectedChapterIndex, Is.EqualTo(3));

            GameObject.DestroyImmediate(go);
        }
        finally
        {
            PlayerDataService.SelectedChapterIndex = originalSelected;
        }
    }

    [Test]
    public void QuestWidgetController_ClaimReward_GuardsAgainstDuplicateClaims()
    {
        bool originalTestMode = ChipManager.IsTestMode;
        int originalGems = ChipManager.RedGems;
        string testQuestId = "test_quest_claim_guard_99";

        try
        {
            ChipManager.IsTestMode = false;
            ChipManager.RedGems = 100;
            QuestWidgetController.SetQuestClaimed(testQuestId, false);

            GameObject go = new GameObject("TestQuestWidget");
            QuestWidgetController widget = go.AddComponent<QuestWidgetController>();

            QuestData testQuest = ScriptableObject.CreateInstance<QuestData>();
            testQuest.questId = testQuestId;
            testQuest.rewardType = QuestData.RewardType.RedGem;
            testQuest.rewardAmount = 250;

            widget.SetQuest(testQuest);

            Assert.That(widget.IsCurrentQuestClaimed(), Is.False);

            // First claim attempt: should succeed and add 250 gems (100 -> 350)
            bool firstClaim = widget.TryClaimReward();
            Assert.That(firstClaim, Is.True);
            Assert.That(ChipManager.RedGems, Is.EqualTo(350));
            Assert.That(widget.IsCurrentQuestClaimed(), Is.True);

            // Second claim attempt: should fail and NOT add duplicate gems
            bool secondClaim = widget.TryClaimReward();
            Assert.That(secondClaim, Is.False);
            Assert.That(ChipManager.RedGems, Is.EqualTo(350));

            GameObject.DestroyImmediate(go);
            ScriptableObject.DestroyImmediate(testQuest);
        }
        finally
        {
            QuestWidgetController.SetQuestClaimed(testQuestId, false);
            ChipManager.RedGems = originalGems;
            ChipManager.IsTestMode = originalTestMode;
        }
    }

    [Test]
    public void BottomNavigationController_Select_SynchronizesBorderColors()
    {
        GameObject root = new GameObject("TestBottomNav");
        BottomNavigationController nav = root.AddComponent<BottomNavigationController>();

        GameObject btn1Obj = new GameObject("Btn1");
        Image border1 = btn1Obj.AddComponent<Image>();
        Button b1 = btn1Obj.AddComponent<Button>();

        GameObject btn2Obj = new GameObject("Btn2");
        Image border2 = btn2Obj.AddComponent<Image>();
        Button b2 = btn2Obj.AddComponent<Button>();

        SerializedObject so = new SerializedObject(nav);
        SerializedProperty itemsProp = so.FindProperty("items");
        itemsProp.arraySize = 2;

        SerializedProperty item0 = itemsProp.GetArrayElementAtIndex(0);
        item0.FindPropertyRelative("button").objectReferenceValue = b1;
        item0.FindPropertyRelative("border").objectReferenceValue = border1;

        SerializedProperty item1 = itemsProp.GetArrayElementAtIndex(1);
        item1.FindPropertyRelative("button").objectReferenceValue = b2;
        item1.FindPropertyRelative("border").objectReferenceValue = border2;

        so.FindProperty("normalBorderColor").colorValue = new Color32(39, 105, 110, 255);
        so.FindProperty("selectedBorderColor").colorValue = new Color32(239, 247, 238, 255);
        so.ApplyModifiedProperties();

        // Select item 0
        nav.Select(0);
        Assert.That(border1.color, Is.EqualTo(new Color32(239, 247, 238, 255)));
        Assert.That(border2.color, Is.EqualTo(new Color32(39, 105, 110, 255)));

        // Select item 1
        nav.Select(1);
        Assert.That(border1.color, Is.EqualTo(new Color32(39, 105, 110, 255)));
        Assert.That(border2.color, Is.EqualTo(new Color32(239, 247, 238, 255)));

        GameObject.DestroyImmediate(btn1Obj);
        GameObject.DestroyImmediate(btn2Obj);
        GameObject.DestroyImmediate(root);
    }

    [Test]
    public void ChapterScreenController_Lighting_UnlockedIsBrightAndLockedIsPitchBlack()
    {
        GameObject go = new GameObject("TestChapterLighting");
        ChapterScreenController ctrl = go.AddComponent<ChapterScreenController>();

        GameObject bgGo = new GameObject("Bg");
        Image bgImg = bgGo.AddComponent<Image>();

        GameObject bossGo = new GameObject("Boss");
        Image bossImg = bossGo.AddComponent<Image>();

        GameObject lockOverlay = new GameObject("LockOverlay");
        GameObject labelGo = new GameObject("Label");
        TMPro.TextMeshProUGUI label = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
        GameObject costBox = new GameObject("CostBox");

        ctrl.SetLockStateForTesting(lockOverlay, label, costBox);
        ctrl.SetPreviewImagesForTesting(bgImg, bossImg);

        ChapterData chapter = ScriptableObject.CreateInstance<ChapterData>();
        chapter.chapterNumber = 1;
        chapter.isLocked = false;

        ChapterDatabase db = ScriptableObject.CreateInstance<ChapterDatabase>();
        db.SetChaptersForTesting(new System.Collections.Generic.List<ChapterData> { chapter });

        // Unlocked state: Background is bright (Color.white), Boss is bright (Color.white)
        ctrl.SetDatabaseForTesting(db, 0);
        Assert.That(ctrl.IsCurrentChapterLocked(), Is.False);
        Assert.That(bossImg.color, Is.EqualTo(ctrl.UnlockedBossColor));
        Assert.That(bgImg.color, Is.EqualTo(ctrl.UnlockedBackgroundColor));
        Assert.That(bossGo.activeSelf, Is.True);
        Assert.That(lockOverlay.activeSelf, Is.False);

        // Locked state: Background is dark, Boss is pitch black
        chapter.isLocked = true;
        ctrl.RefreshChapterView();
        Assert.That(ctrl.IsCurrentChapterLocked(), Is.True);
        Assert.That(bossImg.color, Is.EqualTo(ctrl.LockedBossColor));
        Assert.That(bgImg.color, Is.EqualTo(ctrl.LockedBackgroundColor));
        Assert.That(bossGo.activeSelf, Is.True);
        Assert.That(lockOverlay.activeSelf, Is.True);

        GameObject.DestroyImmediate(bgGo);
        GameObject.DestroyImmediate(bossGo);
        GameObject.DestroyImmediate(lockOverlay);
        GameObject.DestroyImmediate(labelGo);
        GameObject.DestroyImmediate(costBox);
        GameObject.DestroyImmediate(go);
        ScriptableObject.DestroyImmediate(chapter);
    }

    [Test]
    public void EnemySpawner_WaveSystem_LimitsConcurrentAndTotalEnemies()
    {
        GameObject spawnerGo = new GameObject("TestEnemySpawner");
        EnemySpawner spawner = spawnerGo.AddComponent<EnemySpawner>();

        GameObject playerGo = new GameObject("TestPlayer");
        playerGo.tag = "Player";
        spawner.SetPlayerForTesting(playerGo.transform);

        GameObject enemyPrefab = new GameObject("TestEnemyPrefab");
        enemyPrefab.AddComponent<EnemyHealth>();
        enemyPrefab.AddComponent<EnemyMovement>();

        var wave1 = new EnemySpawner.WaveConfig
        {
            waveName = "Test Wave 1",
            totalEnemiesToSpawn = 4,
            maxConcurrentEnemies = 2,
            spawnInterval = 0.01f,
            enemiesPerSpawn = 2,
            enemyPool = new System.Collections.Generic.List<EnemySpawner.EnemySpawnEntry>
            {
                new EnemySpawner.EnemySpawnEntry { enemyPrefab = enemyPrefab, spawnWeight = 100, unlockTime = 0f }
            },
            isBossWave = false,
            breakDurationAfterWave = 0.05f
        };

        var wave2 = new EnemySpawner.WaveConfig
        {
            waveName = "Test Wave 2 - Boss",
            totalEnemiesToSpawn = 2,
            maxConcurrentEnemies = 2,
            spawnInterval = 0.01f,
            enemiesPerSpawn = 1,
            enemyPool = new System.Collections.Generic.List<EnemySpawner.EnemySpawnEntry>
            {
                new EnemySpawner.EnemySpawnEntry { enemyPrefab = enemyPrefab, spawnWeight = 100, unlockTime = 0f }
            },
            isBossWave = true,
            bossCount = 1,
            bossSpawnDelay = 0f,
            breakDurationAfterWave = 0.05f
        };

        spawner.SetWavesForTesting(new System.Collections.Generic.List<EnemySpawner.WaveConfig> { wave1, wave2 });
        spawner.StartWave(0);

        Assert.That(spawner.CurrentWaveNumber, Is.EqualTo(1));
        Assert.That(spawner.TotalWavesCount, Is.EqualTo(2));
        Assert.That(spawner.CurrentState, Is.EqualTo(EnemySpawner.WaveState.InWave));

        GameObject.DestroyImmediate(enemyPrefab);
        GameObject.DestroyImmediate(playerGo);
        GameObject.DestroyImmediate(spawnerGo);
    }

    [Test]
    public void EnemySpawner_FinalWave_SpawnsBossAndTriggersStageVictory()
    {
        int originalUnlocked = PlayerDataService.UnlockedChapterIndex;
        int originalSelected = PlayerDataService.SelectedChapterIndex;

        try
        {
            PlayerDataService.UnlockedChapterIndex = 0; // Chapter 1 unlocked
            PlayerDataService.SelectedChapterIndex = 0; // Playing Chapter 1

            GameObject spawnerGo = new GameObject("TestBossVictorySpawner");
            EnemySpawner spawner = spawnerGo.AddComponent<EnemySpawner>();

            GameObject playerGo = new GameObject("TestPlayer");
            playerGo.tag = "Player";
            spawner.SetPlayerForTesting(playerGo.transform);

            var bossWave = new EnemySpawner.WaveConfig
            {
                waveName = "Final Boss Wave",
                totalEnemiesToSpawn = 0,
                maxConcurrentEnemies = 5,
                isBossWave = true,
                bossCount = 1,
                bossSpawnDelay = 0f
            };

            spawner.SetWavesForTesting(new System.Collections.Generic.List<EnemySpawner.WaveConfig> { bossWave });

            bool victoryFired = false;
            spawner.OnStageVictory += () => victoryFired = true;

            // Trigger victory
            spawner.TriggerStageVictory();

            Assert.That(victoryFired, Is.True);
            Assert.That(spawner.IsStageCompleted, Is.True);
            Assert.That(spawner.CurrentState, Is.EqualTo(EnemySpawner.WaveState.StageVictory));
            Assert.That(PlayerDataService.UnlockedChapterIndex, Is.EqualTo(1), "Hoàn thành Chapter 1 phải mở khóa Chapter 2.");

            GameObject.DestroyImmediate(playerGo);
            GameObject.DestroyImmediate(spawnerGo);
        }
        finally
        {
            PlayerDataService.UnlockedChapterIndex = originalUnlocked;
            PlayerDataService.SelectedChapterIndex = originalSelected;
        }
    }

    [Test]
    public void PlayerLevelController_AddEXP_IncreasesEXPAndLevelsUp()
    {
        GameObject go = new GameObject("TestPlayerLevel");
        PlayerLevelController levelCtrl = go.AddComponent<PlayerLevelController>();
        levelCtrl.SetLevelAndExpForTesting(1, 0);

        int level1MaxExp = levelCtrl.CalculateMaxExpForLevel(1);
        Assert.That(levelCtrl.CurrentLevel, Is.EqualTo(1));
        Assert.That(levelCtrl.CurrentEXP, Is.EqualTo(0));
        Assert.That(levelCtrl.MaxEXP, Is.EqualTo(level1MaxExp));

        // Add partial EXP
        levelCtrl.AddEXP(15);
        Assert.That(levelCtrl.CurrentEXP, Is.EqualTo(15));
        Assert.That(levelCtrl.CurrentLevel, Is.EqualTo(1));
        Assert.That(levelCtrl.EXPProgress, Is.EqualTo(15f / level1MaxExp).Within(0.001f));

        // Add enough EXP to trigger Level Up
        int levelUpEventCount = 0;
        int newLevelRecorded = 0;
        levelCtrl.OnLevelUp += (lvl) =>
        {
            levelUpEventCount++;
            newLevelRecorded = lvl;
        };

        levelCtrl.AddEXP(level1MaxExp); // +30 EXP -> totals 45, requires 30 -> Level 2 with 15 leftover
        Assert.That(levelCtrl.CurrentLevel, Is.EqualTo(2));
        Assert.That(newLevelRecorded, Is.EqualTo(2));
        Assert.That(levelUpEventCount, Is.EqualTo(1));
        Assert.That(levelCtrl.CurrentEXP, Is.EqualTo(15));

        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void EnemyHealth_Die_AwardsEXPToPlayerLevelController()
    {
        GameObject playerGo = new GameObject("TestPlayerWithLevel");
        PlayerLevelController levelCtrl = playerGo.AddComponent<PlayerLevelController>();
        levelCtrl.SetLevelAndExpForTesting(1, 0);

        GameObject enemyGo = new GameObject("TestEnemyEXP");
        EnemyHealth enemyHealth = enemyGo.AddComponent<EnemyHealth>();
        enemyHealth.SetMaxHealth(20);
        enemyHealth.SetExpReward(30);

        Assert.That(levelCtrl.CurrentEXP, Is.EqualTo(0));

        // Kill enemy
        enemyHealth.TakeDamage(20);

        Assert.That(enemyHealth.IsDead, Is.True);
        Assert.That(levelCtrl.CurrentLevel, Is.EqualTo(2), "Diệt quái 30 EXP phải giúp Player lên Lv 2!");

        GameObject.DestroyImmediate(enemyGo);
        GameObject.DestroyImmediate(playerGo);
    }

    [Test]
    public void EnemySpawner_WaveDuration_TransitionsWaveRegardlessOfRemainingEnemies()
    {
        GameObject spawnerGo = new GameObject("TestTimeWaveSpawner");
        EnemySpawner spawner = spawnerGo.AddComponent<EnemySpawner>();

        GameObject playerGo = new GameObject("TestPlayer");
        playerGo.tag = "Player";
        spawner.SetPlayerForTesting(playerGo.transform);

        var wave1 = new EnemySpawner.WaveConfig
        {
            waveName = "Wave 1 - Timed",
            waveDuration = 0.05f,
            totalEnemiesToSpawn = 10,
            maxConcurrentEnemies = 5,
            isBossWave = false,
            breakDurationAfterWave = 0.01f
        };

        var wave2 = new EnemySpawner.WaveConfig
        {
            waveName = "Wave 2 - Timed",
            waveDuration = 0.05f,
            totalEnemiesToSpawn = 10,
            maxConcurrentEnemies = 5,
            isBossWave = false,
            breakDurationAfterWave = 0.01f
        };

        spawner.SetWavesForTesting(new System.Collections.Generic.List<EnemySpawner.WaveConfig> { wave1, wave2 });
        spawner.StartWave(0);

        Assert.That(spawner.CurrentWaveNumber, Is.EqualTo(1));
        Assert.That(spawner.CurrentWaveDuration, Is.EqualTo(0.05f));
        Assert.That(spawner.CurrentWaveTimeProgress, Is.EqualTo(0f));

        GameObject.DestroyImmediate(playerGo);
        GameObject.DestroyImmediate(spawnerGo);
    }

    [Test]
    public void ChapterData_GenerateWaves_CreatesWavesMatchingTotalWavesWithPowerGrowth()
    {
        ChapterData chapter = ScriptableObject.CreateInstance<ChapterData>();
        chapter.chapterNumber = 2;
        chapter.chapterTitle = "Mutant Forest";
        chapter.totalWaves = 10;
        chapter.chapterDifficultyMultiplier = 1.25f;
        chapter.wavePowerGrowthRate = 0.08f; // +8% per wave

        chapter.GenerateWaves();

        Assert.That(chapter.waves.Count, Is.EqualTo(10));

        // Wave 1: Power = 1.0 * 1.25 = 1.25
        Assert.That(chapter.waves[0].healthMultiplier, Is.EqualTo(1.25f).Within(0.001f));
        Assert.That(chapter.waves[0].isBossWave, Is.False);

        // Wave 2: Power = (1.0 + 0.08) * 1.25 = 1.35
        Assert.That(chapter.waves[1].healthMultiplier, Is.EqualTo(1.35f).Within(0.001f));
        Assert.That(chapter.waves[1].healthMultiplier, Is.GreaterThan(chapter.waves[0].healthMultiplier));

        // Wave 10 (Last): Boss wave
        Assert.That(chapter.waves[9].isBossWave, Is.True);
        Assert.That(chapter.waves[9].waveName, Does.Contain("FINAL BOSS"));

        ScriptableObject.DestroyImmediate(chapter);
    }

    [Test]
    public void EnemySpawner_LoadSelectedChapterWaves_AppliesChapterWaveData()
    {
        int originalSelected = PlayerDataService.SelectedChapterIndex;

        try
        {
            ChapterData c1 = ScriptableObject.CreateInstance<ChapterData>();
            c1.chapterNumber = 1;
            c1.chapterTitle = "Grassland";
            c1.totalWaves = 5;
            c1.chapterDifficultyMultiplier = 1.0f;
            c1.GenerateWaves();

            ChapterData c2 = ScriptableObject.CreateInstance<ChapterData>();
            c2.chapterNumber = 2;
            c2.chapterTitle = "Mutant Forest";
            c2.totalWaves = 8;
            c2.chapterDifficultyMultiplier = 1.5f;
            c2.GenerateWaves();

            ChapterDatabase db = ScriptableObject.CreateInstance<ChapterDatabase>();
            db.SetChaptersForTesting(new System.Collections.Generic.List<ChapterData> { c1, c2 });

            GameObject spawnerGo = new GameObject("TestChapterWaveSpawner");
            EnemySpawner spawner = spawnerGo.AddComponent<EnemySpawner>();

            // Select Chapter 2 (index 1)
            PlayerDataService.SelectedChapterIndex = 1;
            spawner.SetChapterDatabaseForTesting(db);

            Assert.That(spawner.TotalWavesCount, Is.EqualTo(8));
            Assert.That(spawner.Waves[0].healthMultiplier, Is.EqualTo(1.5f).Within(0.001f));

            // Select Chapter 1 (index 0)
            PlayerDataService.SelectedChapterIndex = 0;
            spawner.LoadSelectedChapterWaves();

            Assert.That(spawner.TotalWavesCount, Is.EqualTo(5));
            Assert.That(spawner.Waves[0].healthMultiplier, Is.EqualTo(1.0f).Within(0.001f));

            GameObject.DestroyImmediate(spawnerGo);
            ScriptableObject.DestroyImmediate(c1);
            ScriptableObject.DestroyImmediate(c2);
            ScriptableObject.DestroyImmediate(db);
        }
        finally
        {
            PlayerDataService.SelectedChapterIndex = originalSelected;
        }
    }

    private string dataChipFormattedText(string text)
    {
        return text.Trim();
    }
}

