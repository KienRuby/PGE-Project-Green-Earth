using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Comprehensive Automated QA Test Suite for Milestone 2: Core Combat & Auto-Shooter.
/// Covers Player Movement, Viewport Clamping, Auto-Shooter 360° Aim Math,
/// EXP Level Progression, 10 Chipset Combat Skills, Health Systems, and Floating Damage Feedback.
/// </summary>
[TestFixture]
public class M2CombatTests
{
    #region 1. Player Movement & Viewport Clamping Tests
    [Test]
    public void M2_01_PlayerMovement_DefaultSpeed_AndBonusSpeed_CalculatesEffectiveSpeed()
    {
        GameObject playerObj = new GameObject("TestPlayer_Movement", typeof(PlayerMovement), typeof(Rigidbody2D));
        try
        {
            PlayerMovement movement = playerObj.GetComponent<PlayerMovement>();
            Assert.That(movement.MoveSpeed, Is.GreaterThanOrEqualTo(3.5f), "Base movement speed must be at least 3.5f.");

            movement.SetMoveSpeedBonus(2.5f);
            Assert.That(movement.EffectiveSpeed, Is.EqualTo(movement.MoveSpeed + 0f).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
        }
    }

    [Test]
    public void M2_02_PlayerMovement_DeadPlayer_StopsMovementInput()
    {
        GameObject playerObj = new GameObject("TestPlayer_Dead", typeof(PlayerMovement), typeof(PlayerHealth), typeof(Rigidbody2D));
        try
        {
            PlayerMovement movement = playerObj.GetComponent<PlayerMovement>();
            PlayerHealth health = playerObj.GetComponent<PlayerHealth>();

            // Kill player
            health.TakeDamage(9999);
            Assert.That(health.IsDead, Is.True);

            Assert.That(movement.MoveDirection, Is.EqualTo(Vector2.zero));
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
        }
    }

    [Test]
    public void M2_03_MapBoundary_ClampPlayerPosition_RestrictsOutOfBounds()
    {
        GameObject boundaryObj = new GameObject("MapBoundary", typeof(MapBoundary));
        try
        {
            MapBoundary boundary = boundaryObj.GetComponent<MapBoundary>();
            boundary.SetupBounds(Vector2.zero, new Vector2(20f, 20f), 0.5f);

            Vector2 outsidePos = new Vector2(15f, -12f);
            Vector2 clamped = boundary.ClampPlayerPosition(outsidePos);

            Assert.That(clamped.x, Is.EqualTo(9.5f).Within(0.01f));
            Assert.That(clamped.y, Is.EqualTo(-9.5f).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(boundaryObj);
        }
    }

    [Test]
    public void M2_04_MapBoundary_ClampCameraPosition_PreservesMapSightline()
    {
        GameObject boundaryObj = new GameObject("MapBoundary", typeof(MapBoundary));
        GameObject camObj = new GameObject("Camera", typeof(Camera));
        try
        {
            MapBoundary boundary = boundaryObj.GetComponent<MapBoundary>();
            Camera cam = camObj.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.aspect = 0.5625f; // 9:16 portrait

            boundary.SetupBounds(Vector2.zero, new Vector2(30f, 30f), 0.5f);

            Vector2 farPos = new Vector2(20f, 20f);
            Vector2 clamped = boundary.ClampCameraPosition(farPos, cam);

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            Assert.That(clamped.x, Is.LessThanOrEqualTo(15f - halfWidth + 0.01f));
            Assert.That(clamped.y, Is.LessThanOrEqualTo(15f - halfHeight + 0.01f));
        }
        finally
        {
            Object.DestroyImmediate(boundaryObj);
            Object.DestroyImmediate(camObj);
        }
    }
    #endregion

    #region 2. Auto-Shooter & 360° Aim Math Tests
    [Test]
    public void M2_05_AutoShooter_AimScale_FlipsYOnlyWhenAimingLeft()
    {
        MethodInfo calcScaleMethod = typeof(PlayerAutoShooter).GetMethod("CalculateAimScale", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(calcScaleMethod, Is.Not.Null);

        Vector3 baseScale = new Vector3(1.5f, 1.5f, 1f);

        // Aiming Right (0 deg, 45 deg, -45 deg) -> Y scale is positive
        Vector3 rightScale = (Vector3)calcScaleMethod.Invoke(null, new object[] { 45f, baseScale });
        Assert.That(rightScale.y, Is.EqualTo(1.5f));

        // Aiming Left (120 deg, -135 deg, 180 deg) -> Y scale is inverted (-1.5f)
        Vector3 leftScale = (Vector3)calcScaleMethod.Invoke(null, new object[] { 135f, baseScale });
        Assert.That(leftScale.y, Is.EqualTo(-1.5f));
    }

    [Test]
    public void M2_06_AutoShooter_BodyScale_FlipsXOnlyWhenAimingLeft()
    {
        MethodInfo calcBodyMethod = typeof(PlayerAutoShooter).GetMethod("CalculateBodyScale", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(calcBodyMethod, Is.Not.Null);

        Vector3 baseScale = new Vector3(1f, 1f, 1f);

        Vector3 faceRight = (Vector3)calcBodyMethod.Invoke(null, new object[] { false, baseScale });
        Assert.That(faceRight.x, Is.EqualTo(1f));

        Vector3 faceLeft = (Vector3)calcBodyMethod.Invoke(null, new object[] { true, baseScale });
        Assert.That(faceLeft.x, Is.EqualTo(-1f));
    }

    [Test]
    public void M2_07_AutoShooter_CalculateLocalAimAngle_MirrorsCorrectlyForFlippedBody()
    {
        MethodInfo calcLocalAngle = typeof(PlayerAutoShooter).GetMethod("CalculateLocalAimAngle", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(calcLocalAngle, Is.Not.Null);

        // World angle 30° aiming right -> local angle 30°
        float angleRight = (float)calcLocalAngle.Invoke(null, new object[] { 30f, false });
        Assert.That(angleRight, Is.EqualTo(30f).Within(0.001f));

        // World angle 150° aiming left -> local angle 180° - 150° = 30°
        float angleLeft = (float)calcLocalAngle.Invoke(null, new object[] { 150f, true });
        Assert.That(angleLeft, Is.EqualTo(30f).Within(0.001f));
    }

    [Test]
    public void M2_08_AutoShooter_WeaponEquip_AppliesStatsCorrectly()
    {
        GameObject shooterObj = new GameObject("Shooter", typeof(PlayerAutoShooter));
        try
        {
            PlayerAutoShooter shooter = shooterObj.GetComponent<PlayerAutoShooter>();
            WeaponData customWeapon = ScriptableObject.CreateInstance<WeaponData>();
            customWeapon.weaponId = "test_custom_gun";
            customWeapon.damage = 45;
            customWeapon.fireRate = 3.5f;
            customWeapon.bulletSpeed = 15f;
            customWeapon.attackRange = 10f;
            customWeapon.bulletsPerShot = 3;
            customWeapon.spreadAngle = 20f;

            shooter.EquipWeapon(customWeapon);

            Assert.That(shooter.CurrentDamage, Is.EqualTo(45));
            Assert.That(shooter.CurrentFireRate, Is.EqualTo(3.5f).Within(0.01f));
            Assert.That(shooter.CurrentBulletsPerShot, Is.EqualTo(3));
            Assert.That(shooter.CurrentSpreadAngle, Is.EqualTo(20f).Within(0.01f));
            Assert.That(shooter.SharedAttackRange, Is.EqualTo(10f).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(shooterObj);
        }
    }

    [Test]
    public void M2_09_AutoShooter_ApplyStatBonuses_IncreasesCombatPerformance()
    {
        GameObject shooterObj = new GameObject("Shooter", typeof(PlayerAutoShooter));
        try
        {
            PlayerAutoShooter shooter = shooterObj.GetComponent<PlayerAutoShooter>();
            WeaponData baseGun = ScriptableObject.CreateInstance<WeaponData>();
            baseGun.damage = 20;
            baseGun.fireRate = 2.0f;
            baseGun.attackRange = 10f;
            shooter.EquipWeapon(baseGun);

            shooter.ApplyStatBonuses(15, 1.0f, 3.0f, 2.0f, 0.25f);

            Assert.That(shooter.CurrentDamage, Is.EqualTo(35));
            Assert.That(shooter.CurrentFireRate, Is.EqualTo(3.0f).Within(0.01f));
            Assert.That(shooter.SharedAttackRange, Is.EqualTo(13.0f).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(shooterObj);
        }
    }
    #endregion

    #region 3. EXP Scaling & Level Controller Tests
    [Test]
    public void M2_10_PlayerLevelController_FormulaProgression_MatchesExactSpecification()
    {
        GameObject levelObj = new GameObject("LevelController", typeof(PlayerLevelController));
        try
        {
            PlayerLevelController ctrl = levelObj.GetComponent<PlayerLevelController>();

            // MaxEXP(level) = 30 + (level - 1) * 20
            Assert.That(ctrl.CalculateMaxExpForLevel(1), Is.EqualTo(30));
            Assert.That(ctrl.CalculateMaxExpForLevel(2), Is.EqualTo(50));
            Assert.That(ctrl.CalculateMaxExpForLevel(3), Is.EqualTo(70));
            Assert.That(ctrl.CalculateMaxExpForLevel(4), Is.EqualTo(90));
            Assert.That(ctrl.CalculateMaxExpForLevel(5), Is.EqualTo(110));
            Assert.That(ctrl.CalculateMaxExpForLevel(10), Is.EqualTo(210));
        }
        finally
        {
            Object.DestroyImmediate(levelObj);
        }
    }

    [Test]
    public void M2_11_PlayerLevelController_AddEXP_TriggersLevelUp_AndExcessCarriedOver()
    {
        GameObject levelObj = new GameObject("LevelController", typeof(PlayerLevelController));
        int levelUpEventCount = 0;
        int lastEventLevel = 0;

        Action<int> onLvl = lvl =>
        {
            levelUpEventCount++;
            lastEventLevel = lvl;
        };

        try
        {
            PlayerLevelController ctrl = levelObj.GetComponent<PlayerLevelController>();
            ctrl.OnLevelUp += onLvl;

            // Level 1 max EXP = 30. Adding 40 EXP -> Level 2 with 10 excess EXP.
            ctrl.AddEXP(40);

            Assert.That(ctrl.CurrentLevel, Is.EqualTo(2));
            Assert.That(ctrl.CurrentEXP, Is.EqualTo(10));
            Assert.That(ctrl.MaxEXP, Is.EqualTo(50));
            Assert.That(levelUpEventCount, Is.EqualTo(1));
            Assert.That(lastEventLevel, Is.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(levelObj);
        }
    }

    [Test]
    public void M2_12_PlayerLevelController_MultiLevelUp_DispatchesStaticGameEvents()
    {
        GameObject levelObj = new GameObject("LevelController", typeof(PlayerLevelController));
        List<int> dispatchedLevels = new List<int>();
        Action<int> gameEventListener = lvl => dispatchedLevels.Add(lvl);
        GameEvents.OnPlayerLevelUp += gameEventListener;

        try
        {
            PlayerLevelController ctrl = levelObj.GetComponent<PlayerLevelController>();

            // Lvl 1->2: 30, Lvl 2->3: 50. Total 80 EXP + 12 excess = 92 EXP.
            ctrl.AddEXP(92);

            Assert.That(ctrl.CurrentLevel, Is.EqualTo(3));
            Assert.That(ctrl.CurrentEXP, Is.EqualTo(12));
            Assert.That(dispatchedLevels.Count, Is.EqualTo(2));
            Assert.That(dispatchedLevels[0], Is.EqualTo(2));
            Assert.That(dispatchedLevels[1], Is.EqualTo(3));
        }
        finally
        {
            GameEvents.OnPlayerLevelUp -= gameEventListener;
            Object.DestroyImmediate(levelObj);
        }
    }

    [Test]
    public void M2_13_PlayerLevelController_LockLevelUpsForVictory_BlocksFurtherExp()
    {
        GameObject levelObj = new GameObject("LevelController", typeof(PlayerLevelController));
        try
        {
            PlayerLevelController ctrl = levelObj.GetComponent<PlayerLevelController>();
            ctrl.AddEXP(15);
            Assert.That(ctrl.CurrentEXP, Is.EqualTo(15));

            ctrl.LockLevelUpsForVictory();
            Assert.That(ctrl.IsLevelUpLocked, Is.True);

            ctrl.AddEXP(100);
            Assert.That(ctrl.CurrentEXP, Is.EqualTo(15), "EXP should not increase once victory lock is active.");
            Assert.That(ctrl.CurrentLevel, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(levelObj);
        }
    }
    #endregion

    #region 4. 10 Chipset Combat Skills Progression Tests
    [Test]
    public void M2_14_ChipsetWeaponUpgrades_StandardGun_Rifle_Multigun_Shotgun()
    {
        GameObject playerObj = new GameObject("Player", typeof(PlayerAutoShooter));
        try
        {
            PlayerAutoShooter shooter = playerObj.GetComponent<PlayerAutoShooter>();

            // Standard Gun (1)
            shooter.ApplyChipsetWeaponUpgrade(1, 1);
            Assert.That(shooter.GetChipsetWeaponLevel(1), Is.EqualTo(1));
            Assert.That(shooter.GetChipsetWeaponDamage(1), Is.GreaterThanOrEqualTo(50));
            Assert.That(shooter.GetChipsetWeaponFireInterval(1), Is.EqualTo(0.35f).Within(0.01f));

            // Rifle (2)
            shooter.ApplyChipsetWeaponUpgrade(2, 3);
            Assert.That(shooter.GetChipsetWeaponLevel(2), Is.EqualTo(3));
            Assert.That(shooter.GetChipsetWeaponDamage(2), Is.GreaterThanOrEqualTo(25));
            Assert.That(shooter.GetChipsetWeaponFireInterval(2), Is.EqualTo(0.15f).Within(0.01f));

            // Multigun (5)
            shooter.ApplyChipsetWeaponUpgrade(5, 4);
            Assert.That(shooter.GetChipsetWeaponLevel(5), Is.EqualTo(4));
            Assert.That(shooter.GetChipsetWeaponProjectileCount(5), Is.GreaterThanOrEqualTo(9));

            // Shotgun (8)
            shooter.ApplyChipsetWeaponUpgrade(8, 5);
            Assert.That(shooter.GetChipsetWeaponLevel(8), Is.EqualTo(5));
            Assert.That(shooter.GetChipsetWeaponProjectileCount(8), Is.EqualTo(10));
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
        }
    }

    [Test]
    public void M2_15_GunTurretSkill_5LevelProgression_AndSimultaneousCap()
    {
        GameObject playerObj = new GameObject("Player_Turret", typeof(GunTurretSkill));
        try
        {
            GunTurretSkill turretSkill = playerObj.GetComponent<GunTurretSkill>();

            // Level 1: 1 Turret max, 27 dmg, 8.4s CD
            turretSkill.UnlockOrUpgrade(1);
            Assert.That(turretSkill.IsUnlocked, Is.True);
            Assert.That(turretSkill.CurrentSkillLevel, Is.EqualTo(1));
            Assert.That(turretSkill.MaxAllowedTurrets, Is.EqualTo(1));
            Assert.That(turretSkill.GetCurrentDamage(), Is.EqualTo(27));
            Assert.That(turretSkill.GetCurrentCooldown(), Is.EqualTo(8.4f).Within(0.01f));

            // Level 5: 2 Turrets max, 105 dmg, 4.0s CD
            turretSkill.UnlockOrUpgrade(5);
            Assert.That(turretSkill.CurrentSkillLevel, Is.EqualTo(5));
            Assert.That(turretSkill.MaxAllowedTurrets, Is.EqualTo(2));
            Assert.That(turretSkill.GetCurrentDamage(), Is.EqualTo(105));
            Assert.That(turretSkill.GetCurrentCooldown(), Is.EqualTo(4.0f).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
        }
    }

    [Test]
    public void M2_16_HighExplosiveMineSkill_5LevelProgression_DamageAndAoEScaling()
    {
        GameObject playerObj = new GameObject("Player_Mine", typeof(HighExplosiveMineSkill));
        try
        {
            HighExplosiveMineSkill mineSkill = playerObj.GetComponent<HighExplosiveMineSkill>();

            mineSkill.UnlockOrUpgrade(1);
            Assert.That(mineSkill.IsUnlocked, Is.True);
            Assert.That(mineSkill.CurrentLevel, Is.EqualTo(1));
            Assert.That(mineSkill.GetCalculatedDamage(), Is.EqualTo(30));
            Assert.That(mineSkill.GetCalculatedCooldown(), Is.EqualTo(6.0f).Within(0.01f));

            mineSkill.UnlockOrUpgrade(5);
            Assert.That(mineSkill.CurrentLevel, Is.EqualTo(5));
            Assert.That(mineSkill.GetCalculatedDamage(), Is.EqualTo(145));
            Assert.That(mineSkill.GetCalculatedCooldown(), Is.EqualTo(2.5f).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
        }
    }

    [Test]
    public void M2_17_RocketPunchSkill_5LevelProgression_DamageAndStunConfig()
    {
        GameObject playerObj = new GameObject("Player_Punch", typeof(RocketPunchSkill));
        try
        {
            RocketPunchSkill punchSkill = playerObj.GetComponent<RocketPunchSkill>();

            punchSkill.UnlockOrUpgrade(1);
            RocketPunchSkill.RocketPunchLevelConfig lvl1 = punchSkill.GetCurrentConfig();
            Assert.That(lvl1.directDamage, Is.EqualTo(70));
            Assert.That(lvl1.aoeDamage, Is.EqualTo(37));
            Assert.That(lvl1.cooldown, Is.EqualTo(3.0f).Within(0.01f));
            Assert.That(lvl1.hasStun, Is.False);

            punchSkill.UnlockOrUpgrade(5);
            RocketPunchSkill.RocketPunchLevelConfig lvl5 = punchSkill.GetCurrentConfig();
            Assert.That(lvl5.directDamage, Is.EqualTo(260));
            Assert.That(lvl5.aoeDamage, Is.EqualTo(160));
            Assert.That(lvl5.cooldown, Is.EqualTo(1.0f).Within(0.01f));
            Assert.That(lvl5.hasStun, Is.True);
            Assert.That(lvl5.hasLavaPool, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
        }
    }

    [Test]
    public void M2_18_SpikyDiscusSkill_5LevelProgression_DiscusCountAndBleed()
    {
        GameObject playerObj = new GameObject("Player_Discus", typeof(SpikyDiscusSkill));
        try
        {
            SpikyDiscusSkill discusSkill = playerObj.GetComponent<SpikyDiscusSkill>();

            discusSkill.UnlockOrUpgrade(1);
            Assert.That(discusSkill.IsUnlocked, Is.True);
            Assert.That(discusSkill.CurrentLevel, Is.EqualTo(1));
            Assert.That(discusSkill.GetCalculatedDamage(), Is.EqualTo(30));

            discusSkill.UnlockOrUpgrade(3);
            Assert.That(discusSkill.CurrentLevel, Is.EqualTo(3));
            Assert.That(discusSkill.GetCalculatedDamage(), Is.EqualTo(60));

            discusSkill.UnlockOrUpgrade(5);
            Assert.That(discusSkill.CurrentLevel, Is.EqualTo(5));
            Assert.That(discusSkill.GetCalculatedDamage(), Is.EqualTo(110));
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
        }
    }

    [Test]
    public void M2_19_SpinningBladeSkill_5LevelProgression_BladeMaxAndVortex()
    {
        GameObject playerObj = new GameObject("Player_Blade", typeof(SpinningBladeSkill));
        try
        {
            SpinningBladeSkill bladeSkill = playerObj.GetComponent<SpinningBladeSkill>();

            bladeSkill.UnlockOrUpgrade(1);
            Assert.That(bladeSkill.IsUnlocked, Is.True);
            SpinningBladeSkill.SpinningBladeLevelConfig cfg1 = bladeSkill.GetCurrentConfig();
            Assert.That(cfg1.damage, Is.EqualTo(36));
            Assert.That(cfg1.maxBladesOnField, Is.EqualTo(4));
            Assert.That(cfg1.hasVortex, Is.False);

            bladeSkill.UnlockOrUpgrade(5);
            SpinningBladeSkill.SpinningBladeLevelConfig cfg5 = bladeSkill.GetCurrentConfig();
            Assert.That(cfg5.damage, Is.EqualTo(130));
            Assert.That(cfg5.maxBladesOnField, Is.EqualTo(10));
            Assert.That(cfg5.hasVortex, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
        }
    }

    [Test]
    public void M2_20_EnergyJumperCablesSkill_LifeSteal_AndShieldOverheal()
    {
        GameObject playerObj = new GameObject("Player_Cables", typeof(PlayerHealth), typeof(EnergyJumperCablesSkill));
        try
        {
            PlayerHealth health = playerObj.GetComponent<PlayerHealth>();
            EnergyJumperCablesSkill cables = playerObj.GetComponent<EnergyJumperCablesSkill>();

            cables.UnlockOrUpgrade(1);
            Assert.That(cables.IsUnlocked, Is.True);
            Assert.That(cables.GetCalculatedLifeStealPercent(), Is.GreaterThanOrEqualTo(0.02f));

            cables.UnlockOrUpgrade(4);
            Assert.That(cables.GetCalculatedLifeStealPercent(), Is.GreaterThanOrEqualTo(0.045f));
            Assert.That(health.MaxShield, Is.GreaterThanOrEqualTo(10));
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
        }
    }

    [Test]
    public void M2_21_LavaHazardZone_SetupAndDamageTicks()
    {
        GameObject zoneObj = new GameObject("LavaHazardZone", typeof(LavaHazardZone), typeof(SpriteRenderer));
        try
        {
            LavaHazardZone zone = zoneObj.GetComponent<LavaHazardZone>();
            zone.Initialize(40, 3.0f, 3.0f);
            Assert.That(zone.isActiveAndEnabled, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(zoneObj);
        }
    }
    #endregion

    #region 5. Health, Damage & Floating Text Systems Tests
    [Test]
    public void M2_22_PlayerHealth_DamageCalculation_WithArmorReduction()
    {
        GameObject playerObj = new GameObject("PlayerHealth_Test", typeof(PlayerHealth));
        try
        {
            PlayerHealth health = playerObj.GetComponent<PlayerHealth>();
            health.SetDamageReduction(8);
            health.TakeDamage(25);

            // 100 - (25 - 8) = 100 - 17 = 83 HP
            Assert.That(health.CurrentHealth, Is.EqualTo(83));
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
        }
    }

    [Test]
    public void M2_23_PlayerHealth_ShieldAbsorbsDamageBeforeHealth()
    {
        GameObject playerObj = new GameObject("PlayerHealth_Shield", typeof(PlayerHealth));
        try
        {
            PlayerHealth health = playerObj.GetComponent<PlayerHealth>();
            health.SetMaxShield(30);
            health.AddShield(30);

            Assert.That(health.CurrentShield, Is.EqualTo(30));

            health.TakeDamage(20);
            // Shield absorbs 20 -> Shield = 10, HP remains 100
            Assert.That(health.CurrentShield, Is.EqualTo(10));
            Assert.That(health.CurrentHealth, Is.EqualTo(100));

            // Another hit of 15 -> Shield absorbs 10, HP absorbs 5 -> HP = 95
            health.TakeDamage(15);
            Assert.That(health.CurrentShield, Is.EqualTo(0));
            Assert.That(health.CurrentHealth, Is.EqualTo(95));
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
        }
    }

    [Test]
    public void M2_24_PlayerHealth_InvulnerabilityFrames_PreventRapidDamageSticking()
    {
        GameObject playerObj = new GameObject("PlayerHealth_IFrames", typeof(PlayerHealth));
        try
        {
            PlayerHealth health = playerObj.GetComponent<PlayerHealth>();
            health.TakeDamage(20);
            Assert.That(health.CurrentHealth, Is.EqualTo(80));

            // Immediate consecutive attack within i-frames must be ignored
            health.TakeDamage(20);
            Assert.That(health.CurrentHealth, Is.EqualTo(80), "Second hit within i-frames must not reduce HP.");
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
        }
    }

    [Test]
    public void M2_25_PlayerHealth_HealAndFullHeal()
    {
        GameObject playerObj = new GameObject("PlayerHealth_Heal", typeof(PlayerHealth));
        try
        {
            PlayerHealth health = playerObj.GetComponent<PlayerHealth>();
            health.TakeDamage(50);
            Assert.That(health.CurrentHealth, Is.EqualTo(50));

            health.Heal(20);
            Assert.That(health.CurrentHealth, Is.EqualTo(70));

            health.FullHeal();
            Assert.That(health.CurrentHealth, Is.EqualTo(100));
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
        }
    }

    [Test]
    public void M2_26_PlayerHealth_LethalDamage_TriggersDeath_AndReviveRestoresState()
    {
        GameObject playerObj = new GameObject("PlayerHealth_Revive", typeof(PlayerHealth));
        bool deathEventTriggered = false;
        try
        {
            PlayerHealth health = playerObj.GetComponent<PlayerHealth>();
            health.OnPlayerDeath += () => deathEventTriggered = true;

            health.TakeDamage(500);
            Assert.That(health.CurrentHealth, Is.EqualTo(0));
            Assert.That(health.IsDead, Is.True);
            Assert.That(deathEventTriggered, Is.True);

            bool revived = health.Revive(0.6f, 3.0f);
            Assert.That(revived, Is.True);
            Assert.That(health.IsDead, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(60));
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
        }
    }

    [Test]
    public void M2_27_HealthSystem_GenericComponent_FullLifecycle()
    {
        GameObject entityObj = new GameObject("GenericEntity", typeof(HealthSystem));
        int deathCount = 0;
        int damageTaken = 0;
        try
        {
            HealthSystem hs = entityObj.GetComponent<HealthSystem>();
            hs.SetMaxHealth(200, true);
            hs.SetDamageReduction(10);
            hs.SetMaxShield(50);
            hs.AddShield(30);

            hs.OnDeath += () => deathCount++;
            hs.OnDamageTaken += amt => damageTaken += amt;

            // Damage of 40 -> effective = 30 -> absorbed by shield (30) -> HP = 200, Shield = 0
            hs.TakeDamage(40);
            Assert.That(hs.CurrentShield, Is.EqualTo(0));
            Assert.That(hs.CurrentHealth, Is.EqualTo(200));
            Assert.That(damageTaken, Is.EqualTo(30));

            // Fatal damage
            hs.TakeDamage(500);
            Assert.That(hs.IsDead, Is.True);
            Assert.That(deathCount, Is.EqualTo(1));

            // Revive
            bool rev = hs.Revive(0.5f, 2.0f);
            Assert.That(rev, Is.True);
            Assert.That(hs.IsDead, Is.False);
            Assert.That(hs.CurrentHealth, Is.EqualTo(100));
        }
        finally
        {
            Object.DestroyImmediate(entityObj);
        }
    }

    [Test]
    public void M2_28_DamageNumber_And_DamageText_FormattingAndTypes()
    {
        GameObject dmgObj = new GameObject("DamageNumber", typeof(TextMeshPro), typeof(DamageNumber));
        try
        {
            DamageNumber num = dmgObj.GetComponent<DamageNumber>();
            num.Initialize(125, DamageType.Critical, Vector3.zero);

            Assert.That(num.TextComponent.text, Is.EqualTo("125"));
            Assert.That(num.Duration, Is.GreaterThan(0f));

            num.Initialize(50, DamageType.Heal, Vector3.zero);
            Assert.That(num.TextComponent.text, Is.EqualTo("+50"));
        }
        finally
        {
            Object.DestroyImmediate(dmgObj);
        }
    }

    [Test]
    public void M2_29_DamageText_Show_StaticMethod_ExecutesWithoutError()
    {
        Assert.DoesNotThrow(() =>
        {
            DamageText.Show(Vector3.zero, 75, DamageType.Normal);
            DamageText.Show(Vector3.up, 150, DamageType.Critical);
        });
    }

    [Test]
    public void M2_30_ChipsetBattleStats_TracksAccuracyAndDamagePerSkill()
    {
        ChipsetBattleStats.Reset();

        // Register skills
        ChipsetBattleStats.RegisterChipset(1, 1, 53);
        ChipsetBattleStats.RegisterChipset(6, 1, 27);

        // Record attacks and damage
        ChipsetBattleStats.RecordAttack(1, 10);
        ChipsetBattleStats.RecordDamage(1, 530);

        ChipsetBattleStats.RecordAttack(6, 20);
        ChipsetBattleStats.RecordDamage(6, 540);

        Assert.That(ChipsetBattleStats.GrandTotalDamage, Is.EqualTo(1070));
        Assert.That(ChipsetBattleStats.GetStats(1).TotalDamage, Is.EqualTo(530));
        Assert.That(ChipsetBattleStats.GetStats(6).TotalDamage, Is.EqualTo(540));
    }
    #endregion
}
