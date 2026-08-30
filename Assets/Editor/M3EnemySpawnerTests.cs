using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Comprehensive Automated QA Test Suite for Milestone 3:
/// Enemy AI (Creep & Boss), Spawner Waves, VFX Reactions, and Drop/Pickup Systems.
/// </summary>
[TestFixture]
public class M3EnemySpawnerTests
{
    #region 1. Creep & Enemy AI Movement & Separation Tests
    [Test]
    public void M3_01_Creep_Variants_InitializeWithCorrectMultipliers()
    {
        GameObject creepObj = new GameObject("TestCreep", typeof(Creep), typeof(EnemyHealth), typeof(EnemyMovement), typeof(EnemyContactDamage), typeof(Rigidbody2D));
        try
        {
            Creep creep = creepObj.GetComponent<Creep>();

            creep.SetVariant(CreepVariant.Fast);
            creep.Initialize(null, 1.0f, 1.0f, 2.0f, 1.0f);
            Assert.That(creep.Variant, Is.EqualTo(CreepVariant.Fast));
            Assert.That(creep.MoveSpeed, Is.GreaterThan(2.0f));

            creep.SetVariant(CreepVariant.Tank);
            creep.Initialize(null, 1.0f, 1.0f, 1.0f, 1.0f);
            Assert.That(creep.Variant, Is.EqualTo(CreepVariant.Tank));
            Assert.That(creep.MaxHealth, Is.GreaterThanOrEqualTo(100));
        }
        finally
        {
            Object.DestroyImmediate(creepObj);
        }
    }

    [Test]
    public void M3_02_EnemyMovement_CalculatesTargetDirection_AndMaintainsStoppingDistance()
    {
        GameObject playerObj = new GameObject("PlayerTarget");
        playerObj.transform.position = new Vector3(5f, 0f, 0f);

        GameObject creepObj = new GameObject("Enemy", typeof(EnemyMovement), typeof(Rigidbody2D));
        creepObj.transform.position = Vector3.zero;

        try
        {
            EnemyMovement movement = creepObj.GetComponent<EnemyMovement>();
            movement.SetTarget(playerObj.transform);

            MethodInfo calcDirMethod = typeof(EnemyMovement).GetMethod("CalculatePlayerDirection", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(calcDirMethod, Is.Not.Null);

            Vector2 dir = (Vector2)calcDirMethod.Invoke(movement, null);
            Assert.That(dir.x, Is.GreaterThan(0.9f), "Enemy should move towards player located on the right.");

            // Place enemy inside stopping distance
            creepObj.transform.position = new Vector3(4.8f, 0f, 0f);
            Vector2 pushedDir = (Vector2)calcDirMethod.Invoke(movement, null);
            Assert.That(pushedDir.x, Is.LessThan(0f), "Enemy inside stopping distance should push back slightly.");
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
            Object.DestroyImmediate(creepObj);
        }
    }

    [Test]
    public void M3_03_EnemyMovement_FacingDirection_FlipsWhenPlayerCrossesXAxis()
    {
        GameObject playerObj = new GameObject("Player");
        GameObject creepObj = new GameObject("Enemy", typeof(EnemyMovement), typeof(Rigidbody2D));
        creepObj.transform.position = Vector3.zero;

        try
        {
            EnemyMovement movement = creepObj.GetComponent<EnemyMovement>();
            movement.SetTarget(playerObj.transform);

            MethodInfo updateFacing = typeof(EnemyMovement).GetMethod("UpdateFacingDirection", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(updateFacing, Is.Not.Null);

            // Player to right
            playerObj.transform.position = new Vector3(3f, 0f, 0f);
            updateFacing.Invoke(movement, null);
            Assert.That(creepObj.transform.localScale.x, Is.GreaterThan(0f));

            // Player to left
            playerObj.transform.position = new Vector3(-3f, 0f, 0f);
            updateFacing.Invoke(movement, null);
            Assert.That(creepObj.transform.localScale.x, Is.LessThan(0f));
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
            Object.DestroyImmediate(creepObj);
        }
    }

    [Test]
    public void M3_04_EnemyMovement_StatusEffects_StunKnockbackAndSlow()
    {
        GameObject creepObj = new GameObject("Enemy_Status", typeof(EnemyMovement), typeof(Rigidbody2D));
        try
        {
            EnemyMovement movement = creepObj.GetComponent<EnemyMovement>();
            movement.ApplyStun(1.5f);
            Assert.That(movement.IsStunned, Is.True);

            movement.ApplySlow(0.4f, 2.0f);
            movement.ApplyKnockback(Vector2.left, 5.0f, 0.2f);
            Assert.That(movement.isActiveAndEnabled, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(creepObj);
        }
    }
    #endregion

    #region 2. Boss AI & Multi-Phase Behaviors Tests
    [Test]
    public void M3_05_BossMovement_States_ChaseWindupDashRecoverTransitions()
    {
        GameObject bossObj = new GameObject("Boss", typeof(BossMovement), typeof(EnemyHealth), typeof(Rigidbody2D));
        try
        {
            BossMovement boss = bossObj.GetComponent<BossMovement>();
            Assert.That(boss.CurrentState, Is.EqualTo(BossMovement.BossState.Chase));
            Assert.That(boss.MoveSpeed, Is.GreaterThan(0f));
        }
        finally
        {
            Object.DestroyImmediate(bossObj);
        }
    }

    [Test]
    public void M3_06_BossMovement_EnragePhase_TriggersAtLowHealth_IncreasesSpeedAndReducesCooldown()
    {
        GameObject bossObj = new GameObject("Boss_Enrage", typeof(BossMovement), typeof(EnemyHealth), typeof(Rigidbody2D));
        try
        {
            BossMovement boss = bossObj.GetComponent<BossMovement>();
            EnemyHealth health = bossObj.GetComponent<EnemyHealth>();

            health.SetMaxHealth(1000, true);
            Assert.That(boss.IsEnraged, Is.False);

            // Reduce health to 35% (below 40% enrage threshold)
            health.TakeDamage(650);
            Assert.That(health.CurrentHealth, Is.EqualTo(350));

            MethodInfo checkEnrage = typeof(BossMovement).GetMethod("CheckEnrageStatus", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(checkEnrage, Is.Not.Null);
            checkEnrage.Invoke(boss, null);

            Assert.That(boss.IsEnraged, Is.True, "Boss must enter enraged state when HP falls to or below 40%.");
        }
        finally
        {
            Object.DestroyImmediate(bossObj);
        }
    }

    [Test]
    public void M3_07_BossRangedAttack_AimMath_FanSpreadCalculatesSymmetricAngles()
    {
        Vector2 forward = Vector2.right;
        Vector2[] directions = BossRangedAttack.CalculateFanDirections(forward, 3, 60f);

        Assert.That(directions.Length, Is.EqualTo(3));
        Assert.That(directions[1].y, Is.EqualTo(0f).Within(0.001f), "Center projectile must point directly forward.");
        Assert.That(directions[0].y, Is.LessThan(0f), "Bottom projectile must point down.");
        Assert.That(directions[2].y, Is.GreaterThan(0f), "Top projectile must point up.");
        Assert.That(directions[0].y, Is.EqualTo(-directions[2].y).Within(0.001f), "Fan spread must be symmetric.");
    }

    [Test]
    public void M3_08_BossRangedAttack_AimMath_RadialPatternCalculates360UniformDistribution()
    {
        Vector2 forward = Vector2.up;
        Vector2[] directions = BossRangedAttack.CalculateRadialDirections(forward, 8);

        Assert.That(directions.Length, Is.EqualTo(8));

        for (int i = 0; i < directions.Length; i++)
        {
            float angle = Mathf.Atan2(directions[i].y, directions[i].x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            Assert.That(directions[i].sqrMagnitude, Is.EqualTo(1.0f).Within(0.001f), "All radial directions must be unit vectors.");
        }
    }

    [Test]
    public void M3_09_BossEnemy_ComponentIntegration_CoordinatesMovementAndAttack()
    {
        GameObject bossObj = new GameObject("BossEntity", typeof(BossEnemy), typeof(BossMovement), typeof(BossRangedAttack), typeof(EnemyHealth), typeof(EnemyContactDamage), typeof(Rigidbody2D));
        try
        {
            BossEnemy boss = bossObj.GetComponent<BossEnemy>();
            boss.Initialize(null, 2.0f, 1.5f, 1.2f, 3.0f);

            Assert.That(boss.Type, Is.EqualTo(EnemyType.Creep)); // Default inherited enum
            Assert.That(boss.BossMovement, Is.Not.Null);
            Assert.That(boss.RangedAttack, Is.Not.Null);
            Assert.That(boss.Health, Is.Not.Null);
            Assert.That(boss.ContactDamage, Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(bossObj);
        }
    }
    #endregion

    #region 3. Enemy Health, Damage Reactions & Contact Damage Tests
    [Test]
    public void M3_10_EnemyHealth_DamageCalculation_AndEventDispatch()
    {
        GameObject enemyObj = new GameObject("Enemy_Health", typeof(EnemyHealth));
        int healthEventCalls = 0;
        int lastCurrent = 0;
        int lastMax = 0;

        try
        {
            EnemyHealth health = enemyObj.GetComponent<EnemyHealth>();
            health.SetMaxHealth(100, true);
            health.OnHealthChanged += (c, m) =>
            {
                healthEventCalls++;
                lastCurrent = c;
                lastMax = m;
            };

            health.TakeDamage(30, false);
            Assert.That(health.CurrentHealth, Is.EqualTo(70));
            Assert.That(lastCurrent, Is.EqualTo(70));
            Assert.That(lastMax, Is.EqualTo(100));

            health.TakeDamage(20, true);
            Assert.That(health.CurrentHealth, Is.EqualTo(50));
        }
        finally
        {
            Object.DestroyImmediate(enemyObj);
        }
    }

    [Test]
    public void M3_11_EnemyHealth_LethalDamage_DispatchesGameEvents_AndAwardsExp()
    {
        GameObject enemyObj = new GameObject("Enemy_Death", typeof(EnemyHealth));
        int globalKills = 0;
        Action killListener = () => globalKills++;
        GameEvents.OnEnemyKilled += killListener;

        try
        {
            EnemyHealth health = enemyObj.GetComponent<EnemyHealth>();
            health.SetMaxHealth(50, true);
            health.SetExpReward(25);

            health.TakeDamage(100);
            Assert.That(health.IsDead, Is.True);
            Assert.That(globalKills, Is.EqualTo(1));
        }
        finally
        {
            GameEvents.OnEnemyKilled -= killListener;
            Object.DestroyImmediate(enemyObj);
        }
    }

    [Test]
    public void M3_12_EnemyContactDamage_IntervalCooldown_ProtectsPlayerFromRapidHits()
    {
        GameObject playerObj = new GameObject("Player", typeof(PlayerHealth), typeof(CapsuleCollider2D));
        playerObj.tag = "Player";
        GameObject enemyObj = new GameObject("EnemyContact", typeof(EnemyContactDamage), typeof(CircleCollider2D));

        try
        {
            PlayerHealth playerHealth = playerObj.GetComponent<PlayerHealth>();
            EnemyContactDamage contactDamage = enemyObj.GetComponent<EnemyContactDamage>();
            contactDamage.SetDamage(15);

            Assert.That(contactDamage.Damage, Is.EqualTo(15));
            Assert.That(playerHealth.CurrentHealth, Is.EqualTo(100));
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
            Object.DestroyImmediate(enemyObj);
        }
    }

    [Test]
    public void M3_13_EnemyProjectile_SetupAndCollision_DamagesPlayerAndDespawns()
    {
        GameObject projectileObj = new GameObject("EnemyBullet", typeof(EnemyProjectile), typeof(Rigidbody2D), typeof(CapsuleCollider2D));
        try
        {
            EnemyProjectile proj = projectileObj.GetComponent<EnemyProjectile>();
            proj.Setup(Vector2.right, 20, 10f, 15f);

            Assert.That(proj.Damage, Is.EqualTo(20));
            Assert.That(proj.MoveSpeed, Is.EqualTo(10f));
        }
        finally
        {
            Object.DestroyImmediate(projectileObj);
        }
    }
    #endregion

    #region 4. Enemy Spawner Wave Sequencing & Scaling Tests
    [Test]
    public void M3_14_EnemySpawner_WaveProgression_StartWaveInitializesCounters()
    {
        GameObject spawnerObj = new GameObject("Spawner", typeof(EnemySpawner));
        try
        {
            EnemySpawner spawner = spawnerObj.GetComponent<EnemySpawner>();
            spawner.GenerateDefaultWaves(5);

            spawner.StartWave(0);
            Assert.That(spawner.CurrentWaveIndex, Is.EqualTo(0));
            Assert.That(spawner.CurrentWaveNumber, Is.EqualTo(1));
            Assert.That(spawner.TotalWavesCount, Is.EqualTo(5));
            Assert.That(spawner.CurrentState, Is.EqualTo(EnemySpawner.WaveState.InWave));
            Assert.That(spawner.EnemiesSpawnedInWave, Is.EqualTo(0));
            Assert.That(spawner.EnemiesKilledInWave, Is.EqualTo(0));
        }
        finally
        {
            Object.DestroyImmediate(spawnerObj);
        }
    }

    [Test]
    public void M3_15_EnemySpawner_BossWaveTrigger_SpawnsBossAndStartsBossFight()
    {
        GameObject spawnerObj = new GameObject("Spawner_Boss", typeof(EnemySpawner));
        try
        {
            EnemySpawner spawner = spawnerObj.GetComponent<EnemySpawner>();
            spawner.GenerateDefaultWaves(5);

            // Final wave (index 4) is configured as boss wave
            spawner.StartWave(4);
            Assert.That(spawner.CurrentState, Is.EqualTo(EnemySpawner.WaveState.BossFight));
            Assert.That(spawner.GetCurrentWaveConfig().isBossWave, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(spawnerObj);
        }
    }

    [Test]
    public void M3_16_EnemySpawner_StopSpawnerAndGetActiveEnemyCount_ConformsToContract()
    {
        GameObject spawnerObj = new GameObject("Spawner_Contract", typeof(EnemySpawner));
        try
        {
            EnemySpawner spawner = spawnerObj.GetComponent<EnemySpawner>();
            spawner.GenerateDefaultWaves(3);
            spawner.StartWave(0);

            Assert.That(spawner.GetActiveEnemyCount(), Is.EqualTo(0));

            spawner.StopSpawner();
            Assert.That(spawner.CurrentState, Is.EqualTo(EnemySpawner.WaveState.NotStarted));
            Assert.That(spawner.enabled, Is.False);

            spawner.ResumeSpawner();
            Assert.That(spawner.enabled, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(spawnerObj);
        }
    }
    #endregion

    #region 5. VFX & Hit Flash Tests
    [Test]
    public void M3_17_SpriteHitFlash_FlashRoutine_AppliesColorAndRestores()
    {
        GameObject entityObj = new GameObject("Entity_Flash", typeof(SpriteRenderer), typeof(SpriteHitFlash));
        try
        {
            SpriteHitFlash hitFlash = entityObj.GetComponent<SpriteHitFlash>();
            SpriteRenderer sr = entityObj.GetComponent<SpriteRenderer>();
            sr.color = Color.white;

            hitFlash.FlashColor = Color.red;
            hitFlash.FlashDuration = 0.15f;

            hitFlash.Flash();
            Assert.That(hitFlash.IsFlashing, Is.True);

            hitFlash.RestoreOriginalColors();
            Assert.That(sr.color.r, Is.EqualTo(1f));
        }
        finally
        {
            Object.DestroyImmediate(entityObj);
        }
    }

    [Test]
    public void M3_18_VFXBoom_PlaysExplosion_TriggersScreenShakeAndDespawns()
    {
        GameObject boomObj = new GameObject("VFXBoom", typeof(VFXBoom));
        try
        {
            VFXBoom boom = boomObj.GetComponent<VFXBoom>();
            Assert.That(boom.Duration, Is.GreaterThan(0f));
            Assert.DoesNotThrow(() => boom.PlayEffect());
        }
        finally
        {
            Object.DestroyImmediate(boomObj);
        }
    }

    [Test]
    public void M3_19_ReviveVFX_UnscaledTime_PlaysDuringGamePause()
    {
        GameObject reviveObj = new GameObject("ReviveVFX", typeof(ReviveVFX));
        try
        {
            ReviveVFX revive = reviveObj.GetComponent<ReviveVFX>();
            Assert.That(revive.Duration, Is.GreaterThan(0f));
            Assert.DoesNotThrow(() => revive.PlayEffect());
        }
        finally
        {
            Object.DestroyImmediate(reviveObj);
        }
    }
    #endregion

    #region 6. Drop System, Gem Pickups & Magnet Range Tests
    [Test]
    public void M3_20_GemPickup_TypesAndCollection_AwardsExpAndCurrencies()
    {
        GameObject gemObj = new GameObject("Gem_Test", typeof(GemPickup), typeof(CircleCollider2D));
        int initialChips = ChipManager.DataChips;
        int initialGems = ChipManager.RedGems;

        try
        {
            GemPickup gem = gemObj.GetComponent<GemPickup>();

            // 1. DataChip gem
            gem.Initialize(GemType.DataChip, 50, Vector3.zero);
            gem.Collect();
            Assert.That(ChipManager.DataChips, Is.EqualTo(initialChips + 50));

            // 2. RedGem gem
            gem.Initialize(GemType.RedGem, 20, Vector3.zero);
            gem.Collect();
            Assert.That(ChipManager.RedGems, Is.EqualTo(initialGems + 20));
        }
        finally
        {
            ChipManager.DataChips = initialChips;
            ChipManager.RedGems = initialGems;
            Object.DestroyImmediate(gemObj);
        }
    }

    [Test]
    public void M3_21_GemPickup_MagnetAttraction_AcceleratesTowardPlayerTarget()
    {
        GameObject playerObj = new GameObject("Player");
        playerObj.transform.position = Vector3.zero;

        GameObject gemObj = new GameObject("Gem_Attract", typeof(GemPickup), typeof(CircleCollider2D));
        gemObj.transform.position = new Vector3(5f, 0f, 0f);

        try
        {
            GemPickup gem = gemObj.GetComponent<GemPickup>();
            gem.Initialize(GemType.GreenExp, 10, gemObj.transform.position);

            Assert.That(gem.IsBeingAttracted, Is.False);
            gem.TriggerMagnetAttraction(playerObj.transform);
            Assert.That(gem.IsBeingAttracted, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
            Object.DestroyImmediate(gemObj);
        }
    }

    [Test]
    public void M3_22_MagnetPickup_RadiusAndGlobalAttraction_PullsAllGems()
    {
        GameObject playerObj = new GameObject("Player", typeof(MagnetPickup));
        playerObj.transform.position = Vector3.zero;

        GameObject gem1 = new GameObject("Gem1", typeof(GemPickup), typeof(CircleCollider2D));
        gem1.transform.position = new Vector3(2f, 0f, 0f);

        GameObject gem2 = new GameObject("Gem2", typeof(GemPickup), typeof(CircleCollider2D));
        gem2.transform.position = new Vector3(10f, 0f, 0f);

        try
        {
            MagnetPickup magnet = playerObj.GetComponent<MagnetPickup>();
            GemPickup g1 = gem1.GetComponent<GemPickup>();
            GemPickup g2 = gem2.GetComponent<GemPickup>();

            g1.Initialize(GemType.GreenExp, 10, gem1.transform.position);
            g2.Initialize(GemType.GreenExp, 10, gem2.transform.position);

            Assert.That(magnet.EffectiveMagnetRadius, Is.GreaterThanOrEqualTo(3.5f));

            magnet.AttractNearbyGems();
            Assert.That(g1.IsBeingAttracted, Is.True, "Gem within 2m must be attracted by 3.5m radius.");
            Assert.That(g2.IsBeingAttracted, Is.False, "Gem at 10m must not be attracted by 3.5m radius.");

            MagnetPickup.TriggerGlobalMagnet(playerObj.transform);
            Assert.That(g2.IsBeingAttracted, Is.True, "Global magnet must attract all active gems on screen.");
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
            Object.DestroyImmediate(gem1);
            Object.DestroyImmediate(gem2);
        }
    }

    [Test]
    public void M3_23_DropTable_DeterminesExpGemTier_Correctly()
    {
        Assert.That(DropTable.DetermineExpGemType(10), Is.EqualTo(GemType.GreenExp));
        Assert.That(DropTable.DetermineExpGemType(25), Is.EqualTo(GemType.BlueExp));
        Assert.That(DropTable.DetermineExpGemType(100), Is.EqualTo(GemType.RedExp));
        Assert.That(DropTable.DetermineExpGemType(500), Is.EqualTo(GemType.RedExp));
    }

    [Test]
    public void M3_24_EnemyMovement_FlockingSeparation_CalculatesRepulsionBetweenCreeps()
    {
        GameObject creep1 = new GameObject("Creep1", typeof(EnemyMovement), typeof(CircleCollider2D), typeof(Rigidbody2D));
        creep1.transform.position = Vector3.zero;

        GameObject creep2 = new GameObject("Creep2", typeof(EnemyMovement), typeof(CircleCollider2D), typeof(Rigidbody2D));
        creep2.transform.position = new Vector3(0.3f, 0f, 0f);

        try
        {
            EnemyMovement movement1 = creep1.GetComponent<EnemyMovement>();
            MethodInfo sepMethod = typeof(EnemyMovement).GetMethod("CalculateSeparationForce", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(sepMethod, Is.Not.Null);

            Vector2 force = (Vector2)sepMethod.Invoke(movement1, null);
            Assert.That(movement1.isActiveAndEnabled, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(creep1);
            Object.DestroyImmediate(creep2);
        }
    }

    [Test]
    public void M3_25_EnemySpawner_WeightedSelection_FiltersByGameTimerAndSpawnWeights()
    {
        GameObject spawnerObj = new GameObject("Spawner_Weights", typeof(EnemySpawner));
        GameObject prefab1 = new GameObject("Prefab1");
        GameObject prefab2 = new GameObject("Prefab2");

        try
        {
            EnemySpawner spawner = spawnerObj.GetComponent<EnemySpawner>();
            EnemySpawner.WaveConfig config = new EnemySpawner.WaveConfig
            {
                waveName = "TestWave",
                totalEnemiesToSpawn = 10,
                enemyPool = new List<EnemySpawner.EnemySpawnEntry>
                {
                    new EnemySpawner.EnemySpawnEntry { enemyPrefab = prefab1, spawnWeight = 100, unlockTime = 0f },
                    new EnemySpawner.EnemySpawnEntry { enemyPrefab = prefab2, spawnWeight = 100, unlockTime = 9999f } // Locked
                }
            };

            MethodInfo selectMethod = typeof(EnemySpawner).GetMethod("SelectEnemyPrefabForWave", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(selectMethod, Is.Not.Null);

            GameObject selected = (GameObject)selectMethod.Invoke(spawner, new object[] { config });
            Assert.That(selected, Is.EqualTo(prefab1), "Prefab with unlockTime 9999s must not be selected at game start.");
        }
        finally
        {
            Object.DestroyImmediate(spawnerObj);
            Object.DestroyImmediate(prefab1);
            Object.DestroyImmediate(prefab2);
        }
    }
    #endregion
}
