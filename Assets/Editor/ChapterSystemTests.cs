using System.Collections.Generic;
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
    public void ChapterScreenController_StartButton_HiddenWhenLocked_AndVisibleWhenUnlocked()
    {
        GameObject go = new GameObject("TestChapterScreenButton");
        ChapterScreenController ctrl = go.AddComponent<ChapterScreenController>();

        GameObject btnGo = new GameObject("StartButton");
        Button btn = btnGo.AddComponent<Button>();
        btnGo.AddComponent<Image>();

        Sprite normal = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        Sprite pressed = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 4, 4), Vector2.zero);

        ChapterData lockedChapter = ScriptableObject.CreateInstance<ChapterData>();
        lockedChapter.chapterNumber = 2;
        lockedChapter.isLocked = true;

        ChapterDatabase db = ScriptableObject.CreateInstance<ChapterDatabase>();
        db.SetChaptersForTesting(new System.Collections.Generic.List<ChapterData> { lockedChapter });

        ctrl.SetStartButtonForTesting(btn, normal, pressed);
        ctrl.SetDatabaseForTesting(db, 0);

        // Khi Chapter bị khóa -> StartButton phải bị ẩn (!activeSelf)
        Assert.That(btnGo.activeSelf, Is.False, "Nút Start phải ẩn khi chapter chưa mở khóa.");

        // Khi Chapter mở khóa -> StartButton phải hiện (activeSelf)
        lockedChapter.isLocked = false;
        ctrl.RefreshChapterView();
        Assert.That(btnGo.activeSelf, Is.True, "Nút Start phải hiển thị khi chapter đã mở khóa.");

        // Kiểm tra cấu hình SpriteSwap
        Assert.That(btn.transition, Is.EqualTo(Selectable.Transition.SpriteSwap), "Nút Start phải dùng Transition SpriteSwap.");
        Assert.That(btn.spriteState.pressedSprite, Is.EqualTo(pressed), "PressedSprite phải là sprite nhấn.");

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(btnGo);
        ScriptableObject.DestroyImmediate(lockedChapter);
        ScriptableObject.DestroyImmediate(db);
        Object.DestroyImmediate(normal);
        Object.DestroyImmediate(pressed);
    }
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

    [Test]
    public void CustomWaveConfigManager_CloneAndModify_DoesNotAffectOriginalChapterData()
    {
        ChapterData original = ScriptableObject.CreateInstance<ChapterData>();
        original.chapterNumber = 1;
        original.chapterTitle = "Test Grassland";
        original.totalWaves = 5;
        original.GenerateWaves();

        int originalEnemies = original.waves[0].totalEnemiesToSpawn;
        float originalExp = original.waves[0].expMultiplier;

        List<EnemySpawner.WaveConfig> clone = CustomWaveConfigManager.CloneFromChapter(original);
        Assert.That(clone.Count, Is.EqualTo(5));

        clone[0].totalEnemiesToSpawn = 99;
        clone[0].expMultiplier = 5.0f;
        clone[0].waveDuration = 120f;

        Assert.That(original.waves[0].totalEnemiesToSpawn, Is.EqualTo(originalEnemies));
        Assert.That(original.waves[0].expMultiplier, Is.EqualTo(originalExp));

        Assert.That(clone[0].totalEnemiesToSpawn, Is.EqualTo(99));
        Assert.That(clone[0].expMultiplier, Is.EqualTo(5.0f));
        Assert.That(clone[0].waveDuration, Is.EqualTo(120f));

        ScriptableObject.DestroyImmediate(original);
    }

    [Test]
    public void CustomWaveConfigManager_SetAndGetActiveWaves_StoresDataCorrectly()
    {
        CustomWaveConfigManager.ClearAllCustomWaves();
        Assert.That(CustomWaveConfigManager.HasCustomWaves(2), Is.False);

        List<EnemySpawner.WaveConfig> customList = new List<EnemySpawner.WaveConfig>
        {
            new EnemySpawner.WaveConfig
            {
                waveName = "Custom Wave 1",
                totalEnemiesToSpawn = 50,
                expMultiplier = 3.5f,
                waveDuration = 45f,
                breakDurationAfterWave = 5f
            }
        };

        CustomWaveConfigManager.SetActiveCustomWaves(2, customList);

        Assert.That(CustomWaveConfigManager.HasCustomWaves(2), Is.True);
        var retrieved = CustomWaveConfigManager.GetActiveWaves(2);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved.Count, Is.EqualTo(1));
        Assert.That(retrieved[0].waveName, Is.EqualTo("Custom Wave 1"));
        Assert.That(retrieved[0].totalEnemiesToSpawn, Is.EqualTo(50));
        Assert.That(retrieved[0].expMultiplier, Is.EqualTo(3.5f));
        Assert.That(retrieved[0].waveDuration, Is.EqualTo(45f));
        Assert.That(retrieved[0].breakDurationAfterWave, Is.EqualTo(5f));

        CustomWaveConfigManager.ClearAllCustomWaves();
        Assert.That(CustomWaveConfigManager.HasCustomWaves(2), Is.False);
    }

    [Test]
    public void EnemySpawner_LoadSelectedChapterWaves_PrefersCustomWaves()
    {
        int origSelected = PlayerDataService.SelectedChapterIndex;
        try
        {
            PlayerDataService.SelectedChapterIndex = 0;

            List<EnemySpawner.WaveConfig> customWaves = new List<EnemySpawner.WaveConfig>
            {
                new EnemySpawner.WaveConfig { waveName = "Custom 1", totalEnemiesToSpawn = 25, expMultiplier = 4.0f },
                new EnemySpawner.WaveConfig { waveName = "Custom 2", totalEnemiesToSpawn = 35, expMultiplier = 6.0f }
            };

            CustomWaveConfigManager.SetActiveCustomWaves(0, customWaves);

            GameObject spawnerGo = new GameObject("TestSpawner");
            EnemySpawner spawner = spawnerGo.AddComponent<EnemySpawner>();

            spawner.LoadSelectedChapterWaves();

            Assert.That(spawner.Waves, Is.Not.Null);
            Assert.That(spawner.Waves.Count, Is.EqualTo(2));
            Assert.That(spawner.Waves[0].waveName, Is.EqualTo("Custom 1"));
            Assert.That(spawner.Waves[0].totalEnemiesToSpawn, Is.EqualTo(25));
            Assert.That(spawner.Waves[0].expMultiplier, Is.EqualTo(4.0f));
            Assert.That(spawner.Waves[1].totalEnemiesToSpawn, Is.EqualTo(35));

            Object.DestroyImmediate(spawnerGo);
        }
        finally
        {
            CustomWaveConfigManager.ClearAllCustomWaves();
            PlayerDataService.SelectedChapterIndex = origSelected;
        }
    }

    [Test]
    public void EnemyHealth_ExpReward_ScalesCorrectly()
    {
        GameObject enemyGo = new GameObject("EnemyExpTest");
        EnemyHealth health = enemyGo.AddComponent<EnemyHealth>();

        health.SetExpReward(50);
        Assert.That(health.ExpReward, Is.EqualTo(50));

        health.SetExpReward(0);
        Assert.That(health.ExpReward, Is.EqualTo(0));

        Object.DestroyImmediate(enemyGo);
    }

    [Test]
    public void Chapter1_WaveConfig_Wave1IsNotBossWaveAndWave10IsBossWave()
    {
        ChapterData c1 = AssetDatabase.LoadAssetAtPath<ChapterData>("Assets/Data/Chapters/Chapter_01_Grassland.asset");
        Assert.That(c1, Is.Not.Null, "Không tìm thấy Chapter_01_Grassland.asset");
        Assert.That(c1.waves, Is.Not.Null);
        Assert.That(c1.waves.Count, Is.GreaterThanOrEqualTo(10));

        // Wave 1 must NOT be a boss wave
        Assert.That(c1.waves[0].isBossWave, Is.False, "Wave 1 của Chapter 1 không được là Boss Wave.");
        Assert.That(c1.waves[0].customBossPrefab, Is.Null, "Wave 1 của Chapter 1 không được gán customBossPrefab.");

        // Wave 10 MUST be a boss wave
        Assert.That(c1.waves[9].isBossWave, Is.True, "Wave 10 của Chapter 1 phải là Boss Wave.");
        Assert.That(c1.waves[9].customBossPrefab, Is.Not.Null, "Wave 10 của Chapter 1 phải có customBossPrefab.");
    }

    [Test]
    public void EnemyPooling_StatsDoNotCompoundAcrossSpawns()
    {
        GameObject enemyGo = new GameObject("PoolingCompoundingTest");
        EnemyHealth health = enemyGo.AddComponent<EnemyHealth>();
        EnemyMovement movement = enemyGo.AddComponent<EnemyMovement>();
        EnemyContactDamage contact = enemyGo.AddComponent<EnemyContactDamage>();

        int baseHp = health.BaseMaxHealth;
        int baseExp = health.BaseExpReward;
        float baseSpeed = movement.BaseMoveSpeed;
        int baseDmg = contact.BaseDamage;

        // Wave 1: 1.1x scaling
        health.SetMaxHealth(Mathf.RoundToInt(health.BaseMaxHealth * 1.1f), true);
        health.SetExpReward(Mathf.RoundToInt(health.BaseExpReward * 1.1f));
        movement.MoveSpeed = movement.BaseMoveSpeed * 1.1f;
        contact.SetDamage(Mathf.RoundToInt(contact.BaseDamage * 1.1f));

        // Return to pool & spawn again for Wave 2: 1.2x scaling
        health.OnReturnToPool();
        movement.OnReturnToPool();
        contact.OnReturnToPool();

        health.OnSpawnFromPool();
        movement.OnSpawnFromPool();
        contact.OnSpawnFromPool();

        health.SetMaxHealth(Mathf.RoundToInt(health.BaseMaxHealth * 1.2f), true);
        health.SetExpReward(Mathf.RoundToInt(health.BaseExpReward * 1.2f));
        movement.MoveSpeed = movement.BaseMoveSpeed * 1.2f;
        contact.SetDamage(Mathf.RoundToInt(contact.BaseDamage * 1.2f));

        Assert.That(health.MaxHealth, Is.EqualTo(Mathf.RoundToInt(baseHp * 1.2f)));
        Assert.That(health.ExpReward, Is.EqualTo(Mathf.RoundToInt(baseExp * 1.2f)));
        Assert.That(movement.MoveSpeed, Is.EqualTo(baseSpeed * 1.2f).Within(0.001f));
        Assert.That(contact.Damage, Is.EqualTo(Mathf.RoundToInt(baseDmg * 1.2f)));

        Object.DestroyImmediate(enemyGo);
    }

    [Test]
    public void PlayerHealth_ExecutionOrder_BaseHealthNotCorrupted()
    {
        GameObject playerGo = new GameObject("PlayerHealthExecutionOrderTest");
        PlayerHealth health = playerGo.AddComponent<PlayerHealth>();

        // Simulate PlayerStatsManager setting max health before Awake
        health.SetMaxHealth(150, true);

        Assert.That(health.BaseMaxHealth, Is.EqualTo(100), "BaseMaxHealth phải giữ nguyên 100.");
        Assert.That(health.MaxHealth, Is.EqualTo(150));

        // Re-applying bonus (e.g. 50 bonus)
        health.SetMaxHealth(health.BaseMaxHealth + 50, true);
        Assert.That(health.MaxHealth, Is.EqualTo(150), "MaxHealth sau khi tính lại bonus từ BaseMaxHealth phải vẫn là 150 chứ không phải 200.");

        Object.DestroyImmediate(playerGo);
    }

    [Test]
    public void MapBoundary_ClampPlayerPosition_ClampsStrictlyWithinPadding()
    {
        GameObject mapGo = new GameObject("TestMapBoundary");
        MapBoundary boundary = mapGo.AddComponent<MapBoundary>();
        boundary.SetupBounds(Vector2.zero, new Vector2(40f, 40f), 0.5f);

        // Player boundary min = -20 + 0.5 = -19.5, max = 20 - 0.5 = 19.5
        Vector2 clampedCenter = boundary.ClampPlayerPosition(Vector2.zero);
        Assert.That(clampedCenter, Is.EqualTo(Vector2.zero));

        Vector2 clampedFarRight = boundary.ClampPlayerPosition(new Vector2(50f, 0f));
        Assert.That(clampedFarRight.x, Is.EqualTo(19.5f).Within(0.001f));
        Assert.That(clampedFarRight.y, Is.EqualTo(0f));

        Vector2 clampedBottomLeft = boundary.ClampPlayerPosition(new Vector2(-100f, -100f));
        Assert.That(clampedBottomLeft.x, Is.EqualTo(-19.5f).Within(0.001f));
        Assert.That(clampedBottomLeft.y, Is.EqualTo(-19.5f).Within(0.001f));

        Object.DestroyImmediate(mapGo);
    }

    [Test]
    public void MapBoundary_ClampCameraPosition_CalculatesOrthographicAspect()
    {
        GameObject mapGo = new GameObject("TestMapBoundaryCamera");
        MapBoundary boundary = mapGo.AddComponent<MapBoundary>();
        boundary.SetupBounds(Vector2.zero, new Vector2(40f, 40f), 0.5f);

        GameObject camGo = new GameObject("TestCamera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;

        // In Camera, aspect ratio
        float camHalfHeight = 5f;
        float camHalfWidth = 5f * cam.aspect;
        float expectedMaxX = 20f - camHalfWidth;
        float expectedMaxY = 20f - camHalfHeight;

        Vector2 clampedPos = boundary.ClampCameraPosition(new Vector2(100f, 100f), cam);
        Assert.That(clampedPos.x, Is.EqualTo(expectedMaxX).Within(0.01f));
        Assert.That(clampedPos.y, Is.EqualTo(expectedMaxY).Within(0.01f));

        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(mapGo);
    }

    [Test]
    public void ChapterMapManager_AppliesChapterDataCustomSize()
    {
        GameObject mapGo = new GameObject("TestChapterMapManager");
        SpriteRenderer sr = mapGo.AddComponent<SpriteRenderer>();
        ChapterMapManager mgr = mapGo.AddComponent<ChapterMapManager>();
        MapBoundary boundary = mapGo.GetComponent<MapBoundary>();

        ChapterData customChapter = ScriptableObject.CreateInstance<ChapterData>();
        customChapter.chapterTitle = "Custom Jungle Map";
        customChapter.mapSize = new Vector2(60f, 80f);
        customChapter.playerBoundaryPadding = 1.0f;
        customChapter.mapColor = Color.cyan;

        ChapterDatabase db = ScriptableObject.CreateInstance<ChapterDatabase>();
        db.SetChaptersForTesting(new List<ChapterData> { customChapter });
        mgr.SetDatabaseForTesting(db);

        int originalIndex = PlayerDataService.SelectedChapterIndex;
        try
        {
            PlayerDataService.SelectedChapterIndex = 0;
            mgr.ApplyCurrentChapterMap();

            Assert.That(boundary.MapSize.x, Is.EqualTo(60f));
            Assert.That(boundary.MapSize.y, Is.EqualTo(80f));
            Assert.That(boundary.PlayerPadding, Is.EqualTo(1.0f));
            Assert.That(sr.color, Is.EqualTo(Color.cyan));
            Assert.That(sr.size.x, Is.EqualTo(60f));
            Assert.That(sr.size.y, Is.EqualTo(80f));
        }
        finally
        {
            PlayerDataService.SelectedChapterIndex = originalIndex;
            Object.DestroyImmediate(mapGo);
            ScriptableObject.DestroyImmediate(customChapter);
            ScriptableObject.DestroyImmediate(db);
        }
    }

    [Test]
    public void Currency_DefaultStartingBalances_Are1000ChipsAndFullEnergy()
    {
        // Xóa tạm thời các key PlayerPrefs để kiểm tra fallback tài khoản mới
        PlayerPrefs.DeleteKey(PlayerDataService.DataChipsKey);
        PlayerPrefs.DeleteKey(PlayerDataService.RedGemsKey);
        PlayerPrefs.DeleteKey(PlayerDataService.EnergyKey);

        Assert.That(PlayerDataService.DataChips, Is.EqualTo(1000), "Tài khoản mới phải có đúng 1000 Data Chips.");
        Assert.That(PlayerDataService.RedGems, Is.EqualTo(1000), "Tài khoản mới phải có đúng 1000 Red Gems.");
        Assert.That(PlayerDataService.Energy, Is.EqualTo(100), "Tài khoản mới phải có đúng Full 100 Năng Lượng.");
    }

    [Test]
    public void Chapter_StartChapter_DeductsExactEnergy()
    {
        PlayerDataService.Energy = 100;

        int chapterEnergyCost = 15;
        bool spent = ChipManager.TrySpendEnergy(chapterEnergyCost);

        Assert.That(spent, Is.True);
        Assert.That(PlayerDataService.Energy, Is.EqualTo(85), "Sau khi bắt đầu chapter tốn 15 Energy, Energy phải còn 85.");
    }

    [Test]
    public void ChapterScreen_LockedChapter_HidesStartButton_AndUnlockedShows()
    {
        GameObject screenGo = new GameObject("TestChapterScreen");
        ChapterScreenController ctrl = screenGo.AddComponent<ChapterScreenController>();

        GameObject btnGo = new GameObject("StartButton");
        btnGo.transform.SetParent(screenGo.transform);
        Image btnImg = btnGo.AddComponent<Image>();
        Button btn = btnGo.AddComponent<Button>();

        ChapterData unlockedChapter = ScriptableObject.CreateInstance<ChapterData>();
        unlockedChapter.chapterTitle = "Chapter 1";
        unlockedChapter.energyCost = 10;
        unlockedChapter.isLocked = false;

        ChapterData lockedChapter = ScriptableObject.CreateInstance<ChapterData>();
        lockedChapter.chapterTitle = "Chapter 2";
        lockedChapter.energyCost = 10;
        lockedChapter.isLocked = true;

        ChapterDatabase db = ScriptableObject.CreateInstance<ChapterDatabase>();
        db.SetChaptersForTesting(new List<ChapterData> { unlockedChapter, lockedChapter });

        SerializedObject so = new SerializedObject(ctrl);
        so.FindProperty("chapterDatabase").objectReferenceValue = db;
        so.FindProperty("startButton").objectReferenceValue = btn;
        so.ApplyModifiedProperties();

        int originalSelected = PlayerDataService.SelectedChapterIndex;
        int originalUnlocked = PlayerDataService.UnlockedChapterIndex;
        try
        {
            PlayerDataService.UnlockedChapterIndex = 0;

            // Xem Chapter 1 (Mở khóa) -> Nút Start phải hiện
            PlayerDataService.SelectedChapterIndex = 0;
            ctrl.RefreshChapterView();
            Assert.That(btnGo.activeSelf, Is.True, "Chapter đã mở khóa thì nút Start phải HIỆN (activeSelf == true).");

            // Xem Chapter 2 (Bị khóa) -> Nút Start phải ẩn
            PlayerDataService.SelectedChapterIndex = 1;
            ctrl.RefreshChapterView();
            Assert.That(btnGo.activeSelf, Is.False, "Chapter bị khóa thì nút Start phải ẨN (activeSelf == false).");
        }
        finally
        {
            PlayerDataService.SelectedChapterIndex = originalSelected;
            PlayerDataService.UnlockedChapterIndex = originalUnlocked;
            Object.DestroyImmediate(screenGo);
            ScriptableObject.DestroyImmediate(unlockedChapter);
            ScriptableObject.DestroyImmediate(lockedChapter);
            ScriptableObject.DestroyImmediate(db);
        }
    }

    [Test]
    public void ChapterScreen_StartButton_UsesSpriteSwapTransition()
    {
        GameObject screenGo = new GameObject("TestChapterScreen");
        ChapterScreenController ctrl = screenGo.AddComponent<ChapterScreenController>();

        GameObject btnGo = new GameObject("StartButton");
        btnGo.transform.SetParent(screenGo.transform);
        Image btnImg = btnGo.AddComponent<Image>();
        Button btn = btnGo.AddComponent<Button>();

        Sprite sprite1 = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        Sprite sprite2 = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 4, 4), Vector2.zero);

        SerializedObject so = new SerializedObject(ctrl);
        so.FindProperty("startButton").objectReferenceValue = btn;
        so.FindProperty("normalStartSprite").objectReferenceValue = sprite1;
        so.FindProperty("pressedStartSprite").objectReferenceValue = sprite2;
        so.ApplyModifiedProperties();

        try
        {
            ctrl.SetupStartButtonTransition();

            Assert.That(btn.transition, Is.EqualTo(Selectable.Transition.SpriteSwap), "Nút Start phải sử dụng chế độ Transition SpriteSwap.");
            Assert.That(btn.spriteState.pressedSprite, Is.EqualTo(sprite2), "PressedSprite phải là sprite 2 (nút start_1).");
            Assert.That(btnImg.sprite, Is.EqualTo(sprite1), "Sprite mặc định phải là sprite 1 (nút start_0).");
            Assert.That(btnImg.raycastTarget, Is.True, "Image của nút Start phải có raycastTarget = true.");
        }
        finally
        {
            Object.DestroyImmediate(screenGo);
            Object.DestroyImmediate(sprite1);
            Object.DestroyImmediate(sprite2);
        }
    }

    [Test]
    public void PauseModalController_OpensOnFirstClick_AndResumesCorrectly()
    {
        GameObject modalGo = new GameObject("PauseModal");
        modalGo.SetActive(false); // Ban đầu Inactive trong scene

        PauseModalController pauseCtrl = modalGo.AddComponent<PauseModalController>();

        SerializedObject so = new SerializedObject(pauseCtrl);
        so.FindProperty("modalRoot").objectReferenceValue = modalGo;
        so.ApplyModifiedProperties();

        try
        {
            // Lần bấm 1: Nút Pause được bấm
            pauseCtrl.TogglePause();

            Assert.That(pauseCtrl.IsPaused, Is.True, "Lần bấm 1: IsPaused phải là true.");
            Assert.That(modalGo.activeSelf, Is.True, "Lần bấm 1: modalRoot phải HIỆN (activeSelf == true) ngay lập tức!");
            Assert.That(Time.timeScale, Is.EqualTo(0f), "Lần bấm 1: Time.timeScale phải là 0.");

            // Bấm Resume
            pauseCtrl.ResumeGame();

            Assert.That(pauseCtrl.IsPaused, Is.False, "Sau Resume: IsPaused phải là false.");
            Assert.That(modalGo.activeSelf, Is.False, "Sau Resume: modalRoot phải ẨN (activeSelf == false).");
            Assert.That(Time.timeScale, Is.EqualTo(1f), "Sau Resume: Time.timeScale phải là 1.");

            // Lần bấm 2: Nút Pause được bấm lại
            pauseCtrl.TogglePause();

            Assert.That(pauseCtrl.IsPaused, Is.True, "Lần bấm 2: IsPaused phải là true.");
            Assert.That(modalGo.activeSelf, Is.True, "Lần bấm 2: modalRoot phải HIỆN (activeSelf == true).");
            Assert.That(Time.timeScale, Is.EqualTo(0f), "Lần bấm 2: Time.timeScale phải là 0.");
        }
        finally
        {
            Time.timeScale = 1f;
            Object.DestroyImmediate(modalGo);
        }
    }

    [Test]
    public void EnemyHealth_DeathFadeOut_AndPoolReset_RestoresAlpha()
    {
        GameObject enemyGo = new GameObject("TestEnemy");
        SpriteRenderer sr = enemyGo.AddComponent<SpriteRenderer>();
        sr.color = new Color(1f, 1f, 1f, 1f);

        EnemyHealth health = enemyGo.AddComponent<EnemyHealth>();
        health.CacheSpriteRenderers();

        try
        {
            // Kiểm tra màu ban đầu
            Assert.That(sr.color.a, Is.EqualTo(1f), "Alpha ban đầu của SpriteRenderer phải là 1.0.");

            // Mô phỏng hiệu ứng fade out (giảm alpha xuống 0 khi chết)
            sr.color = new Color(1f, 1f, 1f, 0f);
            Assert.That(sr.color.a, Is.EqualTo(0f), "Sau khi fade out, alpha phải về 0.0.");

            // Khi được tái sinh từ Pool (ResetForSpawn)
            health.ResetForSpawn();

            Assert.That(sr.color.a, Is.EqualTo(1f), "Sau khi ResetForSpawn, alpha của SpriteRenderer phải được phục hồi về 1.0 đầy đủ.");
            Assert.That(health.IsDead, Is.False, "Sau khi ResetForSpawn, IsDead phải là false.");
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth), "Sau khi ResetForSpawn, máu phải đầy.");
        }
        finally
        {
            Object.DestroyImmediate(enemyGo);
        }
    }
}



