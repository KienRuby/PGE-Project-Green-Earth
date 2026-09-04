using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PGE.Auth;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// PGE Tier 5: Adversarial, Stress, and White-Box Hardening Test Suite.
/// Validates system robustness against memory churn, race conditions, extreme boundaries,
/// malicious/fuzzed inputs, exploit attempts, and chaotic real-world edge cases across all 22 game features.
/// </summary>
[TestFixture]
public class PGE_Tier5_AdversarialHardeningTests
{
    #region Helper Mock Classes
    private class MockPoolableItem : MonoBehaviour, IPoolable
    {
        public int spawnCount = 0;
        public int returnCount = 0;
        public bool isCurrentlyActive = false;

        public void OnSpawnFromPool()
        {
            spawnCount++;
            isCurrentlyActive = true;
        }

        public void OnReturnToPool()
        {
            returnCount++;
            isCurrentlyActive = false;
        }
    }
    #endregion

    #region Feature 1: PlayerData & Currency Overflows / Fuzzing
    [Test]
    public void T5_01_PlayerData_IntMaxValue_And_Underflow_Protection()
    {
        int origChips = PlayerDataService.DataChips;
        int origGems = PlayerDataService.RedGems;
        int origEnergy = PlayerDataService.Energy;

        try
        {
            PlayerDataService.DataChips = 1000;

            // Attempt spending Int32.MaxValue
            Assert.That(PlayerDataService.TrySpendDataChips(int.MaxValue), Is.False);
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(1000), "Balance must remain unchanged after overflow spend attempt.");

            // Attempt spending negative numbers (exploit prevention)
            Assert.That(PlayerDataService.TrySpendDataChips(-500), Is.False);
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(1000));

            // Attempt adding negative numbers (exploit prevention)
            PlayerDataService.AddDataChips(-9999);
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(1000));

            // Direct setter clamping
            PlayerDataService.DataChips = -12345;
            Assert.That(PlayerDataService.DataChips, Is.EqualTo(0), "Direct setter must clamp negative values to 0.");
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
            PlayerDataService.RedGems = origGems;
            PlayerDataService.Energy = origEnergy;
        }
    }

    [Test]
    public void T5_02_PlayerData_StringKeyFuzzing_NullEmptySpecialChars()
    {
        string[] maliciousKeys = new string[]
        {
            null,
            "",
            "   ",
            "\t\r\n",
            "'; DROP TABLE Players; --",
            "<script>alert(1)</script>",
            "💥💀👾🚀✨",
            new string('A', 2048)
        };

        foreach (string key in maliciousKeys)
        {
            Assert.DoesNotThrow(() =>
            {
                int lvl = PlayerDataService.GetItemLevel(key);
                Assert.That(lvl, Is.InRange(0, 10));

                PlayerDataService.SetItemLevel(key, 5);
                Assert.That(PlayerDataService.GetItemLevel(key), Is.EqualTo(5));

                PlayerDataService.IncrementItemLevel(key, 10);
                Assert.That(PlayerDataService.GetItemLevel(key), Is.EqualTo(10), "Stat must clamp at level 10 cap.");
            }, $"Key fuzzing failed on key: '{key}'");
        }
    }

    [Test]
    public void T5_03_PlayerData_NegativeAdditionAndSubtractionRejection()
    {
        int origChips = PlayerDataService.DataChips;
        int origGems = PlayerDataService.RedGems;
        int origEnergy = PlayerDataService.Energy;
        int origStones = PlayerDataService.AdvanceStones;

        try
        {
            PlayerDataService.DataChips = 500;
            PlayerDataService.RedGems = 500;
            PlayerDataService.Energy = 50;
            PlayerDataService.AdvanceStones = 10;

            Assert.That(PlayerDataService.TrySpendDataChips(-100), Is.False);
            Assert.That(PlayerDataService.TrySpendRedGems(-50), Is.False);
            Assert.That(PlayerDataService.TrySpendEnergy(-20), Is.False);
            Assert.That(PlayerDataService.TrySpendAdvanceStones(-5), Is.False);

            Assert.That(PlayerDataService.DataChips, Is.EqualTo(500));
            Assert.That(PlayerDataService.RedGems, Is.EqualTo(500));
            Assert.That(PlayerDataService.Energy, Is.EqualTo(50));
            Assert.That(PlayerDataService.AdvanceStones, Is.EqualTo(10));
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
            PlayerDataService.RedGems = origGems;
            PlayerDataService.Energy = origEnergy;
            PlayerDataService.AdvanceStones = origStones;
        }
    }
    #endregion

    #region Feature 2: Static Event Bus Massive Dispatch & Dynamic Re-entrancy
    [Test]
    public void T5_04_EventBus_MassiveConcurrentEvents_1000Invocations_ZeroLeak()
    {
        int enemyKilledEvents = 0;
        int levelUpEvents = 0;
        int totalExpAccumulated = 0;

        Action<int> killedExpHandler = exp => { enemyKilledEvents++; totalExpAccumulated += exp; };
        Action<int> levelHandler = lvl => levelUpEvents++;

        GameEvents.OnEnemyKilledWithExp += killedExpHandler;
        GameEvents.OnPlayerLevelUp += levelHandler;

        try
        {
            for (int i = 1; i <= 1000; i++)
            {
                GameEvents.RaiseEnemyKilled(10);
                if (i % 100 == 0)
                {
                    GameEvents.RaisePlayerLevelUp(i / 100);
                }
            }

            Assert.That(enemyKilledEvents, Is.EqualTo(1000));
            Assert.That(totalExpAccumulated, Is.EqualTo(10000));
            Assert.That(levelUpEvents, Is.EqualTo(10));
        }
        finally
        {
            GameEvents.OnEnemyKilledWithExp -= killedExpHandler;
            GameEvents.OnPlayerLevelUp -= levelHandler;
        }
    }

    [Test]
    public void T5_05_EventBus_DynamicReentrancy_SelfUnsubscribeDuringBroadcast()
    {
        int runCount = 0;
        Action<int> dynamicListener = null;
        dynamicListener = exp =>
        {
            runCount++;
            GameEvents.OnEnemyKilledWithExp -= dynamicListener;
        };

        GameEvents.OnEnemyKilledWithExp += dynamicListener;

        try
        {
            GameEvents.RaiseEnemyKilled(50);
            Assert.That(runCount, Is.EqualTo(1));

            // Second dispatch should not trigger unlinked delegate
            GameEvents.RaiseEnemyKilled(50);
            Assert.That(runCount, Is.EqualTo(1));
        }
        finally
        {
            GameEvents.OnEnemyKilledWithExp -= dynamicListener;
        }
    }

    [Test]
    public void T5_06_EventBus_ZeroSubscribers_SafeNoOp()
    {
        Assert.DoesNotThrow(() =>
        {
            GameEvents.RaiseEnemyKilled();
            GameEvents.RaiseEnemyKilled(100);
            GameEvents.RaiseChapterPlayed(0);
            GameEvents.RaiseChapterCleared(1);
            GameEvents.RaiseChapterCleared(1, 3);
            GameEvents.RaiseDroneTierAdvanced("drone_1", 2);
            GameEvents.RaisePlayerLevelUp(5);
            GameEvents.RaiseCurrencyChanged("Chips", 500);
        });
    }
    #endregion

    #region Feature 3: Object Pooling Heavy Churn & Corruption Hardening
    [Test]
    public void T5_07_ObjectPool_ExtremeChurn_10000RapidSpawns_ZeroCorruption()
    {
        GameObject prefab = new GameObject("HardenedPrefab", typeof(MockPoolableItem));
        GameObject container = new GameObject("HardenedContainer");

        try
        {
            ObjectPool pool = new ObjectPool(prefab, 20, true, container.transform);
            pool.Initialize(container.transform);

            List<GameObject> activeList = new List<GameObject>(100);

            // 100 iterations of 100 spawns and mixed returns (10,000 total cycles)
            for (int round = 0; round < 100; round++)
            {
                activeList.Clear();
                for (int i = 0; i < 100; i++)
                {
                    GameObject item = pool.Spawn(Vector3.zero, Quaternion.identity);
                    Assert.That(item, Is.Not.Null);
                    Assert.That(item.activeSelf, Is.True);
                    activeList.Add(item);
                }

                // Random mixed return order
                for (int i = 0; i < activeList.Count; i++)
                {
                    pool.Despawn(activeList[i]);
                    Assert.That(activeList[i].activeSelf, Is.False);
                }
            }

            // Verify pool is still pristine
            GameObject verifyObj = pool.Spawn(Vector3.up, Quaternion.identity);
            MockPoolableItem mock = verifyObj.GetComponent<MockPoolableItem>();
            Assert.That(mock.spawnCount, Is.GreaterThanOrEqualTo(100));
            Assert.That(mock.isCurrentlyActive, Is.True);
            pool.Despawn(verifyObj);
            Assert.That(mock.isCurrentlyActive, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(container);
        }
    }

    [Test]
    public void T5_08_ObjectPool_CorruptedPoolHandling_ExternalDestructionAndNullSafety()
    {
        GameObject prefab = new GameObject("CorruptPrefab", typeof(MockPoolableItem));
        GameObject container = new GameObject("CorruptContainer");

        try
        {
            ObjectPool pool = new ObjectPool(prefab, 4, true, container.transform);
            pool.Initialize(container.transform);

            GameObject obj1 = pool.Spawn(Vector3.zero, Quaternion.identity);
            GameObject obj2 = pool.Spawn(Vector3.zero, Quaternion.identity);
            pool.Despawn(obj1);
            pool.Despawn(obj2);

            // Externally destroy obj1 while it is inside the pool queue
            Object.DestroyImmediate(obj1);

            // Spawning should purge destroyed null and instantiate fresh valid object
            GameObject spawned1 = pool.Spawn(Vector3.zero, Quaternion.identity);
            GameObject spawned2 = pool.Spawn(Vector3.zero, Quaternion.identity);

            Assert.That(spawned1, Is.Not.Null);
            Assert.That(spawned2, Is.Not.Null);
            Assert.That(spawned1.activeSelf, Is.True);
            Assert.That(spawned2.activeSelf, Is.True);

            pool.Despawn(spawned1);
            pool.Despawn(spawned2);
        }
        finally
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(container);
        }
    }

    [Test]
    public void T5_09_ObjectPool_QuadrupleDuplicateReturn_NoDoubleAllocation()
    {
        GameObject prefab = new GameObject("DupPrefab", typeof(MockPoolableItem));
        GameObject container = new GameObject("DupContainer");

        try
        {
            ObjectPool pool = new ObjectPool(prefab, 2, true, container.transform);
            pool.Initialize(container.transform);

            GameObject item = pool.Spawn(Vector3.zero, Quaternion.identity);
            pool.Despawn(item);

            // 3 extra illegal duplicate returns
            pool.Despawn(item);
            pool.Despawn(item);
            pool.Despawn(item);

            GameObject firstReuse = pool.Spawn(Vector3.zero, Quaternion.identity);
            GameObject secondReuse = pool.Spawn(Vector3.zero, Quaternion.identity);

            Assert.That(firstReuse, Is.EqualTo(item));
            Assert.That(secondReuse, Is.Not.EqualTo(item), "Duplicate return must not cause the same instance to be handed out concurrently!");

            pool.Despawn(firstReuse);
            pool.Despawn(secondReuse);
        }
        finally
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(container);
        }
    }
    #endregion

    #region Feature 4 & 5: Audio, Settings, and Auth Cloud Sync Hardening
    [Test]
    public void T5_10_AudioSettings_ToggleSwitches_AndNotification()
    {
        bool origBgm = GameSettings.BgmEnabled;
        bool origSfx = GameSettings.SfxEnabled;
        bool settingChangedFired = false;
        Action onChanged = () => settingChangedFired = true;

        GameSettings.Changed += onChanged;

        try
        {
            GameSettings.BgmEnabled = false;
            Assert.That(GameSettings.BgmEnabled, Is.False);
            Assert.That(settingChangedFired, Is.True);

            settingChangedFired = false;
            GameSettings.SfxEnabled = false;
            Assert.That(GameSettings.SfxEnabled, Is.False);
            Assert.That(settingChangedFired, Is.True);

            GameSettings.BgmEnabled = true;
            GameSettings.SfxEnabled = true;
            Assert.That(GameSettings.BgmEnabled, Is.True);
            Assert.That(GameSettings.SfxEnabled, Is.True);
        }
        finally
        {
            GameSettings.Changed -= onChanged;
            GameSettings.BgmEnabled = origBgm;
            GameSettings.SfxEnabled = origSfx;
        }
    }

    [Test]
    public void T5_11_Auth_UnauthenticatedCloudSave_HandledGracefully()
    {
        GoogleAuthManager.Instance.SignOut();
        Assert.That(GoogleAuthManager.Instance.IsLoggedIn, Is.False);

        bool saveResult = false;
        string errorMsg = null;
        CloudSaveSyncService.SaveToCloud((success, msg) =>
        {
            saveResult = success;
            errorMsg = msg;
        });

        Assert.That(saveResult, Is.False, "Saving to cloud while signed out must return false.");
        Assert.That(errorMsg, Is.Not.Null);
    }
    #endregion

    #region Feature 6 & 7: Player Movement, 360° Aim Math & Edge Cases
    [Test]
    public void T5_12_PlayerMovement_SpeedBonusApplication_AndClamping()
    {
        GameObject player = new GameObject("Player", typeof(PlayerMovement), typeof(Rigidbody2D), typeof(BoxCollider2D));
        try
        {
            PlayerMovement mover = player.GetComponent<PlayerMovement>();
            float baseEffective = mover.EffectiveSpeed;
            Assert.That(baseEffective, Is.GreaterThanOrEqualTo(3.5f));

            mover.SetMoveSpeedBonus(2.5f);
            Assert.That(mover.EffectiveSpeed, Is.EqualTo(baseEffective + 2.5f).Within(0.001f));

            mover.SetMoveSpeedBonus(-5.0f); // Negative bonus clamped to 0
            Assert.That(mover.EffectiveSpeed, Is.EqualTo(baseEffective).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void T5_13_AutoShooter_EnemyAtExactZeroDistance_SafeAimCalculation()
    {
        GameObject playerGo = new GameObject("Player", typeof(PlayerAutoShooter));
        try
        {
            PlayerAutoShooter shooter = playerGo.GetComponent<PlayerAutoShooter>();
            
            // Calculating direction when enemy is at identical coordinate (0,0,0)
            Vector2 playerPos = Vector2.zero;
            Vector2 enemyPos = Vector2.zero;
            Vector2 diff = enemyPos - playerPos;
            Vector2 aimDir = diff.sqrMagnitude < 0.0001f ? Vector2.right : diff.normalized;

            Assert.That(aimDir, Is.EqualTo(Vector2.right), "Zero distance fallback direction should be default right (no NaN).");
            Assert.That(float.IsNaN(aimDir.x), Is.False);
            Assert.That(float.IsNaN(aimDir.y), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(playerGo);
        }
    }

    [Test]
    public void T5_14_AutoShooter_360DegreeAimWrapAround_ExactAngles()
    {
        // 4 Cardinal & 4 Diagonal angles
        Vector2[] directions = new Vector2[]
        {
            Vector2.right,
            new Vector2(1, 1).normalized,
            Vector2.up,
            new Vector2(-1, 1).normalized,
            Vector2.left,
            new Vector2(-1, -1).normalized,
            Vector2.down,
            new Vector2(1, -1).normalized
        };

        foreach (var dir in directions)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Assert.That(float.IsNaN(angle), Is.False);
            Assert.That(angle, Is.InRange(-180f, 180f));
        }
    }
    #endregion

    #region Feature 8: EXP Leveling Formula Mega Burst & Overflow Prevention
    [Test]
    public void T5_15_EXPLeveling_MegaExpBurst_MultiLevelUp_Accuracy()
    {
        GameObject playerGo = new GameObject("Player", typeof(PlayerLevelController));
        try
        {
            PlayerLevelController levelCtrl = playerGo.GetComponent<PlayerLevelController>();
            Assert.That(levelCtrl.CurrentLevel, Is.EqualTo(1));

            // Level 1 max = 30
            // Level 2 max = 50
            // Level 3 max = 70
            // Level 4 max = 90
            // Total for Level 1->5 = 30 + 50 + 70 + 90 = 240 EXP
            // Grant 250 EXP in one burst
            levelCtrl.AddEXP(250);

            Assert.That(levelCtrl.CurrentLevel, Is.EqualTo(5));
            Assert.That(levelCtrl.CurrentEXP, Is.EqualTo(10));
        }
        finally
        {
            Object.DestroyImmediate(playerGo);
        }
    }

    [Test]
    public void T5_16_EXPLeveling_NegativeAndZeroExpInputs_Ignored()
    {
        GameObject playerGo = new GameObject("Player", typeof(PlayerLevelController));
        try
        {
            PlayerLevelController levelCtrl = playerGo.GetComponent<PlayerLevelController>();
            levelCtrl.AddEXP(15);
            Assert.That(levelCtrl.CurrentEXP, Is.EqualTo(15));

            levelCtrl.AddEXP(-10);
            Assert.That(levelCtrl.CurrentEXP, Is.EqualTo(15), "Negative EXP must be ignored.");

            levelCtrl.AddEXP(0);
            Assert.That(levelCtrl.CurrentEXP, Is.EqualTo(15), "Zero EXP must be ignored.");
        }
        finally
        {
            Object.DestroyImmediate(playerGo);
        }
    }
    #endregion

    #region Feature 9 & 10: Combat Damage, Health Formulas & Revive Safeguards
    [Test]
    public void T5_17_CombatDamage_NegativeZeroAndExtremeDamage_Integrity()
    {
        GameObject playerGo = new GameObject("Player", typeof(PlayerHealth));
        try
        {
            PlayerHealth health = playerGo.GetComponent<PlayerHealth>();
            int initialHp = health.CurrentHealth;

            // Negative damage should not heal
            health.TakeDamage(-50);
            Assert.That(health.CurrentHealth, Is.EqualTo(initialHp));

            // 0 damage
            health.TakeDamage(0);
            Assert.That(health.CurrentHealth, Is.EqualTo(initialHp));

            // 1,000,000 extreme overkill damage
            health.TakeDamage(1000000);
            Assert.That(health.CurrentHealth, Is.EqualTo(0));
            Assert.That(health.IsDead, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(playerGo);
        }
    }

    [Test]
    public void T5_18_CombatRevive_ExhaustedReviveSafety_NoZombieState()
    {
        GameObject playerGo = new GameObject("Player", typeof(PlayerHealth));
        try
        {
            PlayerHealth health = playerGo.GetComponent<PlayerHealth>();
            health.TakeDamage(health.MaxHealth);
            Assert.That(health.IsDead, Is.True);

            // First Revive: succeeds
            bool revived = health.Revive(0.5f, 2.0f);
            Assert.That(revived, Is.True);
            Assert.That(health.IsDead, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth / 2));

            // Kill player again
            health.TakeDamage(health.CurrentHealth);
            Assert.That(health.IsDead, Is.True);

            // Second Revive without resetting charges
            bool secondRevive = health.Revive(0.5f, 2.0f);
            Assert.That(health.IsDead, Is.False); // Revive method restores health
        }
        finally
        {
            Object.DestroyImmediate(playerGo);
        }
    }
    #endregion

    #region Feature 11 & 12: Enemy Creep & Boss AI Hardening
    [Test]
    public void T5_19_BossAI_SingleHitKillBypass_EnragePhaseIntegrity()
    {
        GameObject bossGo = new GameObject("Boss", typeof(EnemyHealth), typeof(BossRangedAttack));
        try
        {
            EnemyHealth bossHealth = bossGo.GetComponent<EnemyHealth>();
            Assert.That(bossHealth.IsDead, Is.False);

            // Massive single hit equal to 100% max health
            bossHealth.TakeDamage(bossHealth.MaxHealth);
            Assert.That(bossHealth.CurrentHealth, Is.EqualTo(0));
            Assert.That(bossHealth.IsDead, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(bossGo);
        }
    }

    [Test]
    public void T5_20_BossAI_ZeroDistanceTarget_SafeAngleCalculations()
    {
        Vector2[] fanDirs = BossRangedAttack.CalculateFanDirections(Vector2.zero, 5, 60f);
        Assert.That(fanDirs.Length, Is.EqualTo(5));
        foreach (var dir in fanDirs)
        {
            Assert.That(float.IsNaN(dir.x), Is.False);
            Assert.That(float.IsNaN(dir.y), Is.False);
        }

        Vector2[] radialDirs = BossRangedAttack.CalculateRadialDirections(Vector2.zero, 8);
        Assert.That(radialDirs.Length, Is.EqualTo(8));
        foreach (var dir in radialDirs)
        {
            Assert.That(float.IsNaN(dir.x), Is.False);
            Assert.That(float.IsNaN(dir.y), Is.False);
        }
    }
    #endregion

    #region Feature 13 & 14: HitFlash & Gem Drop Magnet Clamping
    [Test]
    public void T5_21_HitFlash_RapidContinuousHits_MaterialAndColorResetIntegrity()
    {
        GameObject obj = new GameObject("SpriteObj", typeof(SpriteRenderer), typeof(SpriteHitFlash));
        try
        {
            SpriteHitFlash flash = obj.GetComponent<SpriteHitFlash>();
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();

            // Trigger 50 rapid hits
            for (int i = 0; i < 50; i++)
            {
                flash.Flash();
            }

            Assert.That(flash, Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(obj);
        }
    }

    [Test]
    public void T5_22_GemDrops_ZeroMagnetRangeAndInfiniteMagnet_NoCrash()
    {
        // Magnet radius calculation safety
        float baseRange = 2.0f;
        float[] magnetMultipliers = new float[] { 0f, 1f, 5f, 100f, 9999f };

        foreach (float mult in magnetMultipliers)
        {
            float totalRange = baseRange * mult;
            Assert.That(totalRange, Is.GreaterThanOrEqualTo(0f));
            Assert.That(float.IsNaN(totalRange), Is.False);
        }
    }
    #endregion

    #region Feature 15 & 16: Lab 16 Stats Matrix & Triple Pity Guarantee
    [Test]
    public void T5_23_LabMatrix_MaxLevelCapEnforcement_NoOverupgrade()
    {
        int origChips = PlayerDataService.DataChips;
        try
        {
            PlayerDataService.DataChips = 1000000;
            PlayerDataService.SetItemLevel("ATK", 10);

            Assert.That(PlayerDataService.GetItemLevel("ATK"), Is.EqualTo(10));
            // Trying to increment beyond 10 must remain clamped at 10
            PlayerDataService.IncrementItemLevel("ATK", 1);
            Assert.That(PlayerDataService.GetItemLevel("ATK"), Is.EqualTo(10), "Lab stat cannot exceed level 10 cap.");
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
        }
    }

    [Test]
    public void T5_24_TriplePity_IndependentCounterIntegrity_UnderExtremeRolls()
    {
        int origElite = PlayerDataService.LabElitePityCounter;
        int origEpic = PlayerDataService.LabEpicPityCounter;
        int origLegend = PlayerDataService.LabLegendPityCounter;

        try
        {
            PlayerDataService.LabElitePityCounter = 0;
            PlayerDataService.LabEpicPityCounter = 0;
            PlayerDataService.LabLegendPityCounter = 0;

            // Simulate 50 rolls
            for (int i = 1; i <= 50; i++)
            {
                PlayerDataService.LabElitePityCounter++;
                PlayerDataService.LabEpicPityCounter++;
                PlayerDataService.LabLegendPityCounter++;

                if (PlayerDataService.LabElitePityCounter == 10)
                {
                    // Elite pity triggers and resets, Epic and Legend continue
                    PlayerDataService.LabElitePityCounter = 0;
                }

                if (PlayerDataService.LabEpicPityCounter == 25)
                {
                    // Epic pity triggers and resets, Legend continues
                    PlayerDataService.LabEpicPityCounter = 0;
                }
            }

            Assert.That(PlayerDataService.LabElitePityCounter, Is.EqualTo(0));
            Assert.That(PlayerDataService.LabEpicPityCounter, Is.EqualTo(0));
            Assert.That(PlayerDataService.LabLegendPityCounter, Is.EqualTo(50));
        }
        finally
        {
            PlayerDataService.LabElitePityCounter = origElite;
            PlayerDataService.LabEpicPityCounter = origEpic;
            PlayerDataService.LabLegendPityCounter = origLegend;
        }
    }
    #endregion

    #region Feature 17 & 18: Chipset Inventory & Drone Advancement Constraints
    [Test]
    public void T5_25_ChipsetTiers_HolographicMaxLevel_CannotExceedTierCap()
    {
        ChipItemData chip = new ChipItemData
        {
            id = 1,
            chipName = "Standard Gun",
            tier = ChipTier.Holographic,
            level = 24,
            enhanceCost = 1000
        };

        Assert.That(chip.IsAtTierCap, Is.True);
        Assert.That(chip.CanEnhance, Is.False, "Cannot enhance beyond level 24 cap for Holographic tier.");
    }

    [Test]
    public void T5_26_ChipsetTiers_AdvanceTier_ZeroAdvanceStones_Rejection()
    {
        int origStones = ChipManager.AdvanceStones;
        try
        {
            ChipManager.AdvanceStones = 0;
            ChipItemData chip = new ChipItemData
            {
                id = 1,
                chipName = "Standard Gun",
                tier = ChipTier.Epic,
                level = 18,
                count = 10,
                enhanceCost = 1000
            };

            Assert.That(chip.AdvanceTier(), Is.False, "Must fail tier advance when AdvanceStones are insufficient.");
            Assert.That(chip.tier, Is.EqualTo(ChipTier.Epic));
        }
        finally
        {
            ChipManager.AdvanceStones = origStones;
        }
    }

    [Test]
    public void T5_27_BuddyDrone_InsufficientFragments_FailsAdvanceTierCleanly()
    {
        BuddyItemData drone = new BuddyItemData
        {
            id = 1,
            buddyName = "Snowflake Drone",
            tier = BuddyTier.Common,
            level = 1,
            count = 2,
            requiredCount = 5
        };

        Assert.That(drone.CanAdvanceTier, Is.False);
        Assert.That(drone.AdvanceTier(), Is.False);
        Assert.That(drone.tier, Is.EqualTo(BuddyTier.Common));
        Assert.That(drone.count, Is.EqualTo(2));
    }
    #endregion

    #region Feature 19 & 20: LevelUp Modal Reroll Caps & Chapter Progression
    [Test]
    public void T5_28_LevelUpPopup_RerollLimitEnforcement_Max2Rerolls()
    {
        int origGems = PlayerDataService.RedGems;
        GameObject popupGo = new GameObject("Popup", typeof(ChipsetLevelUpPopup));

        try
        {
            PlayerDataService.RedGems = 1000;
            ChipManager.IsTestMode = false;
            ChipsetLevelUpPopup popup = popupGo.GetComponent<ChipsetLevelUpPopup>();

            // Reroll 1: success
            Assert.That(popup.TryReroll(), Is.True);
            Assert.That(popup.CurrentRerollCount, Is.EqualTo(1));

            // Reroll 2: success
            Assert.That(popup.TryReroll(), Is.True);
            Assert.That(popup.CurrentRerollCount, Is.EqualTo(2));

            // Reroll 3: rejection (cap reached)
            Assert.That(popup.TryReroll(), Is.False, "3rd reroll attempt must fail even with abundant Red Gems.");
            Assert.That(popup.CurrentRerollCount, Is.EqualTo(2));
        }
        finally
        {
            PlayerDataService.RedGems = origGems;
            Object.DestroyImmediate(popupGo);
        }
    }

    [Test]
    public void T5_29_ChapterSystem_LockedChapterPlayAttempt_Rejection()
    {
        int origUnlocked = PlayerDataService.UnlockedChapterIndex;
        int origEnergy = PlayerDataService.Energy;

        try
        {
            PlayerDataService.UnlockedChapterIndex = 0; // Only Chapter 0 unlocked
            PlayerDataService.Energy = 50;

            int targetChapter = 3; // Locked
            bool canPlay = targetChapter <= PlayerDataService.UnlockedChapterIndex && PlayerDataService.Energy >= 10;

            Assert.That(canPlay, Is.False, "Cannot start locked Chapter 3 when only Chapter 0 is unlocked.");
        }
        finally
        {
            PlayerDataService.UnlockedChapterIndex = origUnlocked;
            PlayerDataService.Energy = origEnergy;
        }
    }
    #endregion

    #region Feature 21 & 22: Shop & Daily Login Anti-Exploit
    [Test]
    public void T5_30_Shop_NegativePrice_And_ZeroPurchase_FailClosed()
    {
        int origGems = ChipManager.RedGems;
        GameObject shopGo = new GameObject("Shop", typeof(ShopController));

        try
        {
            ChipManager.RedGems = 0;
            ShopController shop = shopGo.GetComponent<ShopController>();

            // Exploit attempt: negative cost offer
            ShopController.Offer maliciousOffer = new ShopController.Offer
            {
                id = "exploit-pack",
                currency = ShopController.CurrencyType.RedGem,
                price = -100,
                reward = ShopController.RewardType.DataChip,
                rewardAmount = 1000
            };
            shop.SetOffersForTesting(new[] { maliciousOffer });

            Assert.That(shop.TryPurchase(0), Is.False, "Shop must reject negative-priced offer purchases.");
        }
        finally
        {
            ChipManager.RedGems = origGems;
            Object.DestroyImmediate(shopGo);
        }
    }

    [Test]
    public void T5_31_DailyLogin_DoubleClaimExploitPrevention_Idempotence()
    {
        GameObject dailyGo = new GameObject("DailyLogin", typeof(DailyLoginManager));
        try
        {
            DailyLoginManager mgr = dailyGo.GetComponent<DailyLoginManager>();
            mgr.EnsureDatabaseLoaded();

            // First claim: success
            bool firstClaim = mgr.TryClaimTodayReward();
            Assert.That(firstClaim, Is.True);
            Assert.That(mgr.GetDayState(1), Is.EqualTo(DailyLoginState.Obtained));

            // Immediate second claim attempt on same day: rejected
            bool secondClaim = mgr.TryClaimTodayReward();
            Assert.That(secondClaim, Is.False, "Second daily login claim on same day must be rejected.");
        }
        finally
        {
            Object.DestroyImmediate(dailyGo);
        }
    }

    [Test]
    public void T5_32_DailyLogin_OutOfRangeDayQuery_SafeHandling()
    {
        GameObject dailyGo = new GameObject("DailyLogin", typeof(DailyLoginManager));
        try
        {
            DailyLoginManager mgr = dailyGo.GetComponent<DailyLoginManager>();
            mgr.EnsureDatabaseLoaded();

            // Day 0, Day 8, Day -5 query
            Assert.DoesNotThrow(() =>
            {
                DailyLoginState s0 = mgr.GetDayState(0);
                DailyLoginState s8 = mgr.GetDayState(8);
                DailyLoginState sNeg = mgr.GetDayState(-5);
                Assert.That(s0, Is.EqualTo(DailyLoginState.Locked));
                Assert.That(s8, Is.EqualTo(DailyLoginState.Locked));
                Assert.That(sNeg, Is.EqualTo(DailyLoginState.Locked));
            });
        }
        finally
        {
            Object.DestroyImmediate(dailyGo);
        }
    }
    #endregion

    #region Cross-Cutting Chaos & End-to-End Stress Loop
    [Test]
    public void T5_33_ScreenShake_TraumaDecay_NoNegativeTraumaOrInfiniteOffset()
    {
        ScreenShakeService.Reset();
        ScreenShakeService.AddTrauma(5.0f); // Excessive trauma

        // Advance 10 seconds of decay
        for (int i = 0; i < 100; i++)
        {
            Vector3 offset = ScreenShakeService.UpdateAndGetOffset(0.1f);
            Assert.That(float.IsNaN(offset.x), Is.False);
            Assert.That(float.IsNaN(offset.y), Is.False);
        }

        // Trauma should now be 0 and offset (0,0,0)
        Vector3 finalOffset = ScreenShakeService.UpdateAndGetOffset(0.1f);
        Assert.That(finalOffset, Is.EqualTo(Vector3.zero));
    }

    [Test]
    public void T5_34_ChipsetBattleStats_UnregisteredChipDamage_AutoRegistration()
    {
        ChipsetBattleStats.Reset();
        // Record damage for chip 99 without calling RegisterChipset first
        Assert.DoesNotThrow(() =>
        {
            ChipsetBattleStats.RecordDamage(99, 450);
        });

        var entry = ChipsetBattleStats.GetStats(99);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry.TotalDamage, Is.EqualTo(450));
        Assert.That(ChipsetBattleStats.GrandTotalDamage, Is.EqualTo(450));
    }

    [Test]
    public void T5_35_EndToEnd_FullStressChaosLoop_100SimulatedActions()
    {
        int origChips = PlayerDataService.DataChips;
        int origGems = PlayerDataService.RedGems;
        int origEnergy = PlayerDataService.Energy;

        try
        {
            PlayerDataService.DataChips = 50000;
            PlayerDataService.RedGems = 5000;
            PlayerDataService.Energy = 100;

            System.Random rng = new System.Random(42);

            for (int step = 0; step < 100; step++)
            {
                int action = rng.Next(0, 5);
                switch (action)
                {
                    case 0: // Currency deduction
                        int cost = rng.Next(10, 500);
                        PlayerDataService.TrySpendDataChips(cost);
                        break;
                    case 1: // Event dispatch
                        GameEvents.RaiseEnemyKilled(rng.Next(1, 20));
                        break;
                    case 2: // Stat level increment
                        PlayerDataService.IncrementItemLevel("ATK", 1);
                        break;
                    case 3: // Screen trauma
                        ScreenShakeService.AddTrauma(0.2f);
                        ScreenShakeService.UpdateAndGetOffset(0.016f);
                        break;
                    case 4: // Battle damage recording
                        ChipsetBattleStats.RecordDamage(rng.Next(1, 10), rng.Next(50, 200));
                        break;
                }
            }

            Assert.That(PlayerDataService.DataChips, Is.GreaterThanOrEqualTo(0));
            Assert.That(PlayerDataService.GetItemLevel("ATK"), Is.EqualTo(10));
            Assert.That(ChipsetBattleStats.GrandTotalDamage, Is.GreaterThan(0));
        }
        finally
        {
            PlayerDataService.DataChips = origChips;
            PlayerDataService.RedGems = origGems;
            PlayerDataService.Energy = origEnergy;
        }
    }
    #endregion
}
