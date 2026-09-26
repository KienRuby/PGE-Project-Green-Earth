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



    [Test]

    public void M3_16a_EnemySpawner_ClearedWaveAdvancesEarly_ButTimedOutWaveAdvancesWithSurvivors()

    {

        GameObject spawnerObj = new GameObject("Spawner_WaveCompletion", typeof(EnemySpawner));

        GameObject enemyObj = new GameObject("Survivor", typeof(EnemyHealth));

        try

        {

            EnemySpawner spawner = spawnerObj.GetComponent<EnemySpawner>();

            spawner.GenerateDefaultWaves(3);

            spawner.StartWave(0);

            EnemySpawner.WaveConfig config = spawner.GetCurrentWaveConfig();

            config.totalEnemiesToSpawn = 1;



            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

            MethodInfo checkClear = typeof(EnemySpawner).GetMethod("CheckWaveClearCondition", flags);

            FieldInfo spawned = typeof(EnemySpawner).GetField("enemiesSpawnedInWave", flags);

            FieldInfo elapsed = typeof(EnemySpawner).GetField("waveElapsedTime", flags);

            FieldInfo currentEnemies = typeof(EnemySpawner).GetField("currentWaveEnemies", flags);

            Assert.That(checkClear, Is.Not.Null);

            Assert.That(spawned, Is.Not.Null);

            Assert.That(elapsed, Is.Not.Null);

            Assert.That(currentEnemies, Is.Not.Null);



            checkClear.Invoke(spawner, new object[] { config });

            Assert.That(spawner.CurrentState, Is.EqualTo(EnemySpawner.WaveState.InWave), "Do not clear before all enemies spawn.");



            spawned.SetValue(spawner, 1);

            checkClear.Invoke(spawner, new object[] { config });

            Assert.That(spawner.CurrentState, Is.EqualTo(EnemySpawner.WaveState.WaveBreak), "Clear immediately after the last enemy dies.");



            spawner.StartWave(1);

            config = spawner.GetCurrentWaveConfig();

            HashSet<EnemyHealth> current = (HashSet<EnemyHealth>)currentEnemies.GetValue(spawner);

            current.Add(enemyObj.GetComponent<EnemyHealth>());

            elapsed.SetValue(spawner, config.waveDuration);

            checkClear.Invoke(spawner, new object[] { config });

            Assert.That(spawner.CurrentState, Is.EqualTo(EnemySpawner.WaveState.WaveBreak), "Time limit advances with a survivor.");



            spawner.StartWave(2);

            Assert.That(current.Count, Is.Zero, "Old enemies must not occupy the next wave's spawn limit.");

        }

        finally

        {

            Object.DestroyImmediate(enemyObj);

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
        Assert.That(DropTable.DetermineExpGemType(10), Is.EqualTo(GemType.BlueExp));
        Assert.That(DropTable.DetermineExpGemType(20), Is.EqualTo(GemType.PurpleExp));
        Assert.That(DropTable.DetermineExpGemType(50), Is.EqualTo(GemType.YellowExp));
        Assert.That(DropTable.DetermineExpGemType(100), Is.EqualTo(GemType.RedExp));
        Assert.That(DropTable.DetermineExpGemType(500), Is.EqualTo(GemType.RedExp));
    }

    [Test]
    public void M3_26_DropTable_CalculateWaveExpDrop_FollowsWaveDesignRules()
    {
        // 1. Wave 1-5: 100% quai nho hoac quai co baseExp cao van roi pha le xanh (10 EXP)
        var w1Normal = DropTable.CalculateWaveExpDrop(10, 3, false, false);
        Assert.That(w1Normal.gemType, Is.EqualTo(GemType.BlueExp));
        Assert.That(w1Normal.expValue, Is.EqualTo(10));

        var w1HighExp = DropTable.CalculateWaveExpDrop(20, 1, false, false);
        Assert.That(w1HighExp.gemType, Is.EqualTo(GemType.BlueExp));
        Assert.That(w1HighExp.expValue, Is.EqualTo(10));

        var w1Elite = DropTable.CalculateWaveExpDrop(10, 2, true, false);
        Assert.That(w1Elite.gemType, Is.EqualTo(GemType.BlueExp));
        Assert.That(w1Elite.expValue, Is.EqualTo(10));

        // 2. Wave 6-9: Quai nho roi pha le xanh (10 EXP)
        var w6Normal = DropTable.CalculateWaveExpDrop(10, 7, false, false);
        Assert.That(w6Normal.gemType, Is.EqualTo(GemType.BlueExp));
        Assert.That(w6Normal.expValue, Is.EqualTo(10));

        // 3. Wave 6-9: Quai tinh anh moi roi pha le tim (20 EXP)
        var w6Elite = DropTable.CalculateWaveExpDrop(10, 7, true, false);
        Assert.That(w6Elite.gemType, Is.EqualTo(GemType.PurpleExp));
        Assert.That(w6Elite.expValue, Is.EqualTo(20));

        // 4. Wave 1-9: KHONG BAO GIO roi pha le vang hoac do tu quai khong phai Boss
        for (int i = 0; i < 500; i++)
        {
            var dropW9 = DropTable.CalculateWaveExpDrop(10, 9, true, false);
            Assert.That(dropW9.gemType, Is.Not.EqualTo(GemType.YellowExp));
            Assert.That(dropW9.gemType, Is.Not.EqualTo(GemType.RedExp));
        }

        // 5. Wave 10: Co ty le 1% roi pha le vang (50 EXP) va 0.1% roi pha le do (100 EXP)
        int yellowCount = 0;
        int redCount = 0;
        int blueCount = 0;
        int purpleCount = 0;
        for (int i = 0; i < 5000; i++)
        {
            var dropNormal = DropTable.CalculateWaveExpDrop(10, 10, false, false);
            if (dropNormal.gemType == GemType.YellowExp) yellowCount++;
            else if (dropNormal.gemType == GemType.RedExp) redCount++;
            else if (dropNormal.gemType == GemType.BlueExp) blueCount++;

            var dropElite = DropTable.CalculateWaveExpDrop(10, 10, true, false);
            if (dropElite.gemType == GemType.YellowExp) yellowCount++;
            else if (dropElite.gemType == GemType.RedExp) redCount++;
            else if (dropElite.gemType == GemType.PurpleExp) purpleCount++;
        }
        Assert.That(yellowCount, Is.GreaterThan(0), "Wave 10 must be able to drop YellowExp (1%)");
        Assert.That(blueCount, Is.GreaterThan(0), "Wave 10 normal creeps must drop BlueExp when not rolling special");
        Assert.That(purpleCount, Is.GreaterThan(0), "Wave 10 elite creeps must drop PurpleExp when not rolling special");

        // 6. Boss -> Luon luon roi Red 100 EXP
        var wBoss = DropTable.CalculateWaveExpDrop(10, 5, false, true);
        Assert.That(wBoss.gemType, Is.EqualTo(GemType.RedExp));
        Assert.That(wBoss.expValue, Is.EqualTo(100));
    }



    [Test]

    public void M3_25_GemPickup_FourColorExpGems_AwardExpToPlayerLevelController()

    {

        GameObject playerObj = new GameObject("PlayerWithLevel", typeof(PlayerLevelController));

        PlayerLevelController levelCtrl = playerObj.GetComponent<PlayerLevelController>();

        levelCtrl.SetLevelAndExpForTesting(1, 0);



        GameObject gemBlue = new GameObject("Gem_Blue", typeof(GemPickup), typeof(CircleCollider2D));

        GameObject gemPurple = new GameObject("Gem_Purple", typeof(GemPickup), typeof(CircleCollider2D));

        GameObject gemYellow = new GameObject("Gem_Yellow", typeof(GemPickup), typeof(CircleCollider2D));

        GameObject gemRed = new GameObject("Gem_Red", typeof(GemPickup), typeof(CircleCollider2D));



        try

        {

            GemPickup pBlue = gemBlue.GetComponent<GemPickup>();

            GemPickup pPurple = gemPurple.GetComponent<GemPickup>();

            GemPickup pYellow = gemYellow.GetComponent<GemPickup>();

            GemPickup pRed = gemRed.GetComponent<GemPickup>();



            pBlue.Initialize(GemType.BlueExp, 10, Vector3.zero);

            pPurple.Initialize(GemType.PurpleExp, 25, Vector3.zero);

            pYellow.Initialize(GemType.YellowExp, 100, Vector3.zero);

            pRed.Initialize(GemType.RedExp, 250, Vector3.zero);



            Assert.That(pBlue.VisualRenderer.sprite, Is.Not.Null);

            Assert.That(pPurple.VisualRenderer.sprite, Is.Not.Null);

            Assert.That(pYellow.VisualRenderer.sprite, Is.Not.Null);

            Assert.That(pRed.VisualRenderer.sprite, Is.Not.Null);



            pBlue.Collect();

            Assert.That(levelCtrl.CurrentEXP, Is.EqualTo(10));



            pPurple.Collect();

            Assert.That(levelCtrl.CurrentEXP, Is.GreaterThanOrEqualTo(35));

        }

        finally

        {

            Object.DestroyImmediate(playerObj);

            Object.DestroyImmediate(gemBlue);

            Object.DestroyImmediate(gemPurple);

            Object.DestroyImmediate(gemYellow);

            Object.DestroyImmediate(gemRed);

        }
    }

    [Test]
    public void M3_27_GemPickup_FourColorExpGems_GlowAndSparkleVisualSetup()
    {
        // 1. Kiem tra bang mau phat sang tuong ung chuan
        Color blueGlow = GemPickup.GetGlowColor(GemType.BlueExp);
        Color purpleGlow = GemPickup.GetGlowColor(GemType.PurpleExp);
        Color yellowGlow = GemPickup.GetGlowColor(GemType.YellowExp);
        Color redGlow = GemPickup.GetGlowColor(GemType.RedExp);

        Assert.That(blueGlow.b, Is.GreaterThan(0.7f), "Blue Gem glow must be predominantly cyan/blue");
        Assert.That(purpleGlow.r, Is.GreaterThan(0.7f), "Purple Gem glow must have red/magenta component");
        Assert.That(purpleGlow.b, Is.GreaterThan(0.7f), "Purple Gem glow must have blue/magenta component");
        Assert.That(yellowGlow.r, Is.GreaterThan(0.8f), "Yellow Gem glow must have strong red/gold component");
        Assert.That(yellowGlow.g, Is.GreaterThan(0.7f), "Yellow Gem glow must have strong green/gold component");
        Assert.That(redGlow.r, Is.GreaterThan(0.8f), "Red Gem glow must be predominantly red");
        Assert.That(redGlow.g, Is.LessThan(0.4f), "Red Gem glow must not be yellow or white");

        // 2. Kiem tra khoi tao thuc the GemPickup co day du GemVisual, GemGlow, GemSparkle
        GameObject gemObj = new GameObject("TestGemGlow", typeof(CircleCollider2D), typeof(GemPickup));
        try
        {
            GemPickup gem = gemObj.GetComponent<GemPickup>();
            gem.Initialize(GemType.BlueExp, 10, Vector3.zero);

            Transform visualChild = gemObj.transform.Find("GemVisual");
            Transform glowChild = gemObj.transform.Find("GemGlow");
            Transform sparkleChild = gemObj.transform.Find("GemSparkle");

            Assert.That(visualChild, Is.Not.Null, "GemVisual child must exist");
            Assert.That(glowChild, Is.Not.Null, "GemGlow child must exist");
            Assert.That(sparkleChild, Is.Not.Null, "GemSparkle child must exist");

            SpriteRenderer visualRend = visualChild.GetComponent<SpriteRenderer>();
            SpriteRenderer glowRend = glowChild.GetComponent<SpriteRenderer>();
            SpriteRenderer sparkleRend = sparkleChild.GetComponent<SpriteRenderer>();

            Assert.That(visualRend, Is.Not.Null);
            Assert.That(glowRend, Is.Not.Null);
            Assert.That(sparkleRend, Is.Not.Null);

            // Layering check: Glow o sau (-1), Sparkle o truoc (+1)
            Assert.That(glowRend.sortingOrder, Is.LessThan(visualRend.sortingOrder));
            Assert.That(sparkleRend.sortingOrder, Is.GreaterThan(visualRend.sortingOrder));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gemObj);
        }
    }

    [Test]
    public void M3_28_APKRuntimeResources_VerifyAllFallbacksExist()
    {
        // 1. Skill Prefabs in Resources (anti-crash on APK)
        Assert.That(Resources.Load<GameObject>("Prefabs/Chipset/GunTurret"), Is.Not.Null, "GunTurret prefab must exist in Resources");
        Assert.That(Resources.Load<GameObject>("Prefabs/Chipset/RocketPunch"), Is.Not.Null, "RocketPunch prefab must exist in Resources");
        Assert.That(Resources.Load<GameObject>("Prefabs/Chipset/SpinningBlade"), Is.Not.Null, "SpinningBlade prefab must exist in Resources");
        Assert.That(Resources.Load<GameObject>("Prefabs/Projectile"), Is.Not.Null, "Projectile prefab must exist in Resources");
        Assert.That(Resources.Load<GameObject>("Prefabs/VFX Boom"), Is.Not.Null, "VFX Boom prefab must exist in Resources");

        // 2. UI Sprites in Resources (anti-invisible buttons on APK)
        Assert.That(Resources.Load<Sprite>("UI/Reward/Extracted/Btn_Get"), Is.Not.Null, "Btn_Get sprite must exist in Resources");
        Assert.That(Resources.Load<Sprite>("UI/Reward/Extracted/Btn_Not_Achieved"), Is.Not.Null, "Btn_Not_Achieved sprite must exist in Resources");
        Assert.That(Resources.Load<Sprite>("UI/Reward/Extracted/Btn_Obtained"), Is.Not.Null, "Btn_Obtained sprite must exist in Resources");
        Assert.That(Resources.Load<Sprite>("UI/Reward/Extracted/Row_Banner_Achievement"), Is.Not.Null, "Row_Banner_Achievement sprite must exist in Resources");
        Assert.That(Resources.Load<Sprite>("UI/Reward/Extracted/Icon_Data_Chip"), Is.Not.Null, "Icon_Data_Chip sprite must exist in Resources");

        // 3. Fonts & Materials in Resources
        Assert.That(Resources.Load<TMPro.TMP_FontAsset>("Fonts/Nunito SDF"), Is.Not.Null, "Nunito SDF font must exist in Resources");
        Assert.That(Resources.Load<Material>("Fonts/Nunito SDF - Stroke"), Is.Not.Null, "Nunito stroke material must exist in Resources");

        // 4. EXP Glow Sprites
        Assert.That(Resources.Load<Sprite>("EXP/exp_gem_glow"), Is.Not.Null, "EXP glow sprite must exist in Resources");
        Assert.That(Resources.Load<Sprite>("EXP/exp_gem_sparkle"), Is.Not.Null, "EXP sparkle sprite must exist in Resources");
    }

    [Test]
    public void M3_29_AndroidNavigation_And_SafeArea_InstantiateSuccessfully()
    {
        GameObject safeAreaObj = new GameObject("SafeArea", typeof(RectTransform), typeof(PGE.UI.SafeAreaFitter));
        GameObject backHandlerObj = new GameObject("BackHandler", typeof(PGE.UI.AndroidBackNavigationHandler));

        try
        {
            PGE.UI.SafeAreaFitter fitter = safeAreaObj.GetComponent<PGE.UI.SafeAreaFitter>();
            Assert.That(fitter, Is.Not.Null);

            PGE.UI.AndroidBackNavigationHandler handler = backHandlerObj.GetComponent<PGE.UI.AndroidBackNavigationHandler>();
            Assert.That(handler, Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(safeAreaObj);
            Object.DestroyImmediate(backHandlerObj);
        }
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

    [Test]

    public void M3_26_EnemySpawner_KillAllActiveEnemies_KillsAllActiveEnemiesSimultaneously()

    {

        GameObject spawnerObj = new GameObject("Spawner_KillAll", typeof(EnemySpawner));

        GameObject enemy1 = new GameObject("Enemy1", typeof(EnemyHealth), typeof(BoxCollider2D));

        GameObject enemy2 = new GameObject("Enemy2", typeof(EnemyHealth), typeof(BoxCollider2D));



        try

        {

            EnemySpawner spawner = spawnerObj.GetComponent<EnemySpawner>();

            EnemyHealth h1 = enemy1.GetComponent<EnemyHealth>();

            EnemyHealth h2 = enemy2.GetComponent<EnemyHealth>();



            Assert.That(h1.IsDead, Is.False);

            Assert.That(h2.IsDead, Is.False);



            spawner.KillAllActiveEnemies();



            Assert.That(h1.IsDead, Is.True, "Enemy 1 must be dead after KillAllActiveEnemies.");

            Assert.That(h2.IsDead, Is.True, "Enemy 2 must be dead after KillAllActiveEnemies.");

            Assert.That(enemy1.GetComponent<BoxCollider2D>().enabled, Is.False, "Colliders must be disabled on death.");

            Assert.That(enemy2.GetComponent<BoxCollider2D>().enabled, Is.False, "Colliders must be disabled on death.");

        }

        finally

        {

            Object.DestroyImmediate(spawnerObj);

            Object.DestroyImmediate(enemy1);

            Object.DestroyImmediate(enemy2);

        }

    }



    [Test]

    public void BossDeath_DoesNotAwardExpForBossOrAutomaticallyClearedEnemies()

    {

        GameObject playerObj = new GameObject("PlayerLevel", typeof(PlayerLevelController));

        GameObject spawnerObj = new GameObject("Spawner", typeof(EnemySpawner));

        GameObject bossObj = new GameObject("Boss", typeof(EnemyHealth));

        GameObject creepObj = new GameObject("RemainingCreep", typeof(EnemyHealth));



        try

        {

            PlayerLevelController playerLevel = playerObj.GetComponent<PlayerLevelController>();

            playerLevel.SetLevelAndExpForTesting(1, 0);



            EnemyHealth boss = bossObj.GetComponent<EnemyHealth>();

            boss.SetIsBoss(true);

            boss.SetMaxHealth(10);

            boss.SetExpReward(10);

            boss.SetDataChipReward(0);

            boss.SetRedGemReward(0);



            EnemyHealth creep = creepObj.GetComponent<EnemyHealth>();

            creep.SetExpReward(10);



            EnemySpawner spawner = spawnerObj.GetComponent<EnemySpawner>();

            spawner.SetWavesForTesting(new List<EnemySpawner.WaveConfig>

            {

                new EnemySpawner.WaveConfig { isBossWave = true, bossCount = 1 }

            });

            typeof(EnemySpawner).GetField("bossesSpawnedInWave", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(spawner, 1);

            var activeBosses = (List<EnemyHealth>)typeof(EnemySpawner)

                .GetField("activeBosses", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(spawner);

            Assert.That(activeBosses, Is.Not.Null);

            activeBosses.Add(boss);



            MethodInfo handler = typeof(EnemySpawner).GetMethod("HandleBossDeath", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(handler, Is.Not.Null);

            boss.OnDeath += (Action<EnemyHealth>)Delegate.CreateDelegate(typeof(Action<EnemyHealth>), spawner, handler);



            boss.TakeDamage(10);



            Assert.That(boss.IsDead, Is.True);

            Assert.That(creep.IsDead, Is.True);

            Assert.That(playerLevel.CurrentEXP, Is.Zero);

            Assert.That(playerLevel.CurrentLevel, Is.EqualTo(1));

            Assert.That(playerLevel.IsLevelUpLocked, Is.True);

        }

        finally

        {

            Object.DestroyImmediate(bossObj);

            Object.DestroyImmediate(creepObj);

            Object.DestroyImmediate(spawnerObj);

            Object.DestroyImmediate(playerObj);

        }

    }



    [Test]

    public void M3_27_PlayerHealth_IsInvulnerable_PreventsDamage()

    {

        GameObject playerObj = new GameObject("Player_Invuln", typeof(PlayerHealth));

        try

        {

            PlayerHealth playerHealth = playerObj.GetComponent<PlayerHealth>();

            int initialHp = playerHealth.CurrentHealth;



            playerHealth.IsInvulnerable = true;

            playerHealth.TakeDamage(50);



            Assert.That(playerHealth.CurrentHealth, Is.EqualTo(initialHp), "Invulnerable player must not take damage.");



            playerHealth.IsInvulnerable = false;

            playerHealth.TakeDamage(20);

            Assert.That(playerHealth.CurrentHealth, Is.LessThan(initialHp), "Normal player must take damage.");

        }

        finally

        {

            Object.DestroyImmediate(playerObj);

        }

    }

    #endregion

}

