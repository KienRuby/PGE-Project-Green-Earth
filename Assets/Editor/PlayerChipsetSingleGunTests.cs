using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class PlayerChipsetSingleGunTests
{
    [Test]
    public void GunChipsets_UpgradeOneAutoShooter_WithoutAddingLegacyShooters()
    {
        GameObject player = new GameObject("Player");

        try
        {
            PlayerAutoShooter shooter = player.AddComponent<PlayerAutoShooter>();
            PlayerChipsetSkillManager manager = player.AddComponent<PlayerChipsetSkillManager>();

            InvokePrivate(manager, "Awake");
            InvokePrivate(manager, "HandleChipsetSelected", new ChipItemData
            {
                id = 8,
                chipName = "Shotgun",
                iconKey = "shotgun"
            }, 1);

            Assert.That(player.GetComponent<StandardGunSkill>(), Is.Null);
            Assert.That(player.GetComponent<RifleSkill>(), Is.Null);
            Assert.That(player.GetComponent<ShotgunSkill>(), Is.Null);
            Assert.That(player.GetComponent<MultigunSkill>(), Is.Null);
            Assert.That(shooter.GetChipsetWeaponLevel(8), Is.EqualTo(1));
            Assert.That(shooter.GetChipsetWeaponProjectileCount(8), Is.EqualTo(5));
            Assert.That(shooter.GetChipsetWeaponDamage(8), Is.GreaterThan(0));
            Assert.That(ChipsetBattleStats.GetEntry(8), Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void ShotgunLevelFive_EnablesDoubleTap_FiresTwoSequencesOfFivePellets()
    {
        GameObject player = new GameObject("Player");

        try
        {
            PlayerAutoShooter shooter = player.AddComponent<PlayerAutoShooter>();
            ChipsetBattleStats.Reset();
            shooter.ApplyChipsetWeaponUpgrade(8, 5);

            Assert.That(shooter.GetChipsetWeaponLevel(8), Is.EqualTo(5));
            Assert.That(shooter.GetChipsetWeaponDamage(8), Is.GreaterThanOrEqualTo(42));
            Assert.That(shooter.GetChipsetWeaponFireInterval(8), Is.EqualTo(0.7f).Within(0.01f));
            Assert.That(shooter.GetChipsetWeaponProjectileCount(8), Is.EqualTo(10));

            ChipsetBattleStats.RecordAttack(8, 5);
            ChipsetBattleStats.RecordAttack(8, 5);
            ChipsetBattleStats.RecordDamage(8, 210);

            ChipsetBattleStats.Entry entry = ChipsetBattleStats.GetEntry(8);
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.RuntimeLevel, Is.EqualTo(5));
            Assert.That(entry.AttackCount, Is.EqualTo(2));
            Assert.That(entry.ProjectileCount, Is.EqualTo(10));
            Assert.That(entry.TotalDamage, Is.EqualTo(210));
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void MultigunLevelFive_KeepsIndependentDamageAndBattleStats()
    {
        GameObject player = new GameObject("Player");

        try
        {
            PlayerAutoShooter shooter = player.AddComponent<PlayerAutoShooter>();
            ChipsetBattleStats.Reset();
            shooter.ApplyChipsetWeaponUpgrade(5, 5);

            Assert.That(shooter.GetChipsetWeaponDamage(5), Is.GreaterThanOrEqualTo(70));
            Assert.That(shooter.GetChipsetWeaponFireInterval(5), Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(shooter.GetChipsetWeaponProjectileCount(5), Is.GreaterThanOrEqualTo(13));

            ChipsetBattleStats.RecordAttack(5, 13);
            ChipsetBattleStats.RecordDamage(5, 70);

            ChipsetBattleStats.Entry entry = ChipsetBattleStats.GetEntry(5);
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.RuntimeLevel, Is.EqualTo(5));
            Assert.That(entry.ConfiguredDamage, Is.GreaterThanOrEqualTo(70));
            Assert.That(entry.AttackCount, Is.EqualTo(1));
            Assert.That(entry.ProjectileCount, Is.EqualTo(13));
            Assert.That(entry.TotalDamage, Is.EqualTo(70));
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void ShootingSkills_UsePlayerFirePointAndSharedTarget()
    {
        GameObject player = new GameObject("Player");
        GameObject gunPivot = new GameObject("GunPivot");
        GameObject gunSprite = new GameObject("GunSprite");
        GameObject firePoint = new GameObject("FirePoint");
        GameObject enemy = new GameObject("Enemy");
        GameObject turretObject = new GameObject("Turret");

        try
        {
            gunPivot.transform.SetParent(player.transform);
            gunSprite.transform.SetParent(gunPivot.transform);
            firePoint.transform.SetParent(gunSprite.transform);

            PlayerAutoShooter shooter = player.AddComponent<PlayerAutoShooter>();
            SetPrivateField(shooter, "currentTarget", enemy.transform);

            RocketPunchSkill rocketPunch = player.AddComponent<RocketPunchSkill>();
            GunTurret turret = turretObject.AddComponent<GunTurret>();
            turret.Initialize(10, 1f, 10f, 10f, 0f, false, 100, null, null, null, targetProvider: shooter);
            InvokePrivate(turret, "UpdateTarget");

            Assert.That(shooter.FirePoint, Is.EqualTo(firePoint.transform));
            Assert.That(InvokePrivateWithResult<Transform>(rocketPunch, "FindTargetEnemy"), Is.EqualTo(enemy.transform));
            Assert.That(InvokePrivateWithResult<Transform>(rocketPunch, "GetSharedFirePoint"), Is.EqualTo(firePoint.transform));
            Assert.That(GetPrivateField<Transform>(turret, "currentTarget"), Is.EqualTo(enemy.transform));
            Assert.That(GetPrivateField<PlayerAutoShooter>(turret, "sharedTargetProvider"), Is.EqualTo(shooter));
        }
        finally
        {
            Object.DestroyImmediate(turretObject);
            Object.DestroyImmediate(enemy);
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void GunTurret_AutoShoot_FiresBulletFromMuzzleAndRecordsAttack()
    {
        GameObject turretObject = new GameObject("Turret");
        GameObject aimPivot = new GameObject("AimPivot");
        aimPivot.transform.SetParent(turretObject.transform);
        aimPivot.transform.localPosition = Vector3.zero;

        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(aimPivot.transform);
        firePoint.transform.localPosition = new Vector3(1f, 0f, 0f);

        GameObject enemy = new GameObject("EnemyTarget");
        enemy.transform.position = new Vector3(5f, 0f, 0f);

        GameObject bulletPrefab = new GameObject("TestBullet", typeof(Rigidbody2D), typeof(Projectile));

        try
        {
            ChipsetBattleStats.Reset();
            GunTurret turret = turretObject.AddComponent<GunTurret>();
            turret.Initialize(35, 2f, 15f, 10f, 0f, false, 200, bulletPrefab, null, null);
            SetPrivateField(turret, "currentTarget", enemy.transform);
            SetPrivateField(turret, "nextFireTime", 0f);

            InvokePrivate(turret, "AutoShoot");

            ChipsetBattleStats.Entry entry = ChipsetBattleStats.GetEntry(6);
            Assert.That(entry, Is.Not.Null, "ChipsetBattleStats entry cho GunTurret (id 6) phải tồn tại.");
            Assert.That(entry.AttackCount, Is.GreaterThanOrEqualTo(1), "GunTurret AutoShoot phải gọi Shoot() và ghi nhận đòn đánh.");
        }
        finally
        {
            Object.DestroyImmediate(bulletPrefab);
            Object.DestroyImmediate(enemy);
            Object.DestroyImmediate(turretObject);
        }
    }

    [Test]
    public void AllChipsetWeapons_AttackRange_IsThreeMetersSmallerThanPlayerAttackRange()
    {
        GameObject player = new GameObject("Player_RangeTest");
        GameObject gunPivot = new GameObject("GunPivot");
        GameObject gunSprite = new GameObject("GunSprite");
        GameObject firePoint = new GameObject("FirePoint");

        try
        {
            gunPivot.transform.SetParent(player.transform);
            gunSprite.transform.SetParent(gunPivot.transform);
            firePoint.transform.SetParent(gunSprite.transform);

            PlayerAutoShooter shooter = player.AddComponent<PlayerAutoShooter>();
            // Mặc định SharedAttackRange = 12.0f
            Assert.That(shooter.SharedAttackRange, Is.EqualTo(12.0f).Within(0.001f));
            Assert.That(shooter.ChipsetAttackRange, Is.EqualTo(9.0f).Within(0.001f));

            // Test GunTurret (Chipset ID 6)
            GameObject turretObj = new GameObject("Turret");
            try
            {
                GunTurret turret = turretObj.AddComponent<GunTurret>();
                turret.Initialize(10, 1f, 10f, 10f, 0f, false, 100, null, null, null, targetProvider: shooter);
                Assert.That(turret.EffectiveAttackRange, Is.EqualTo(9.0f).Within(0.001f));

                shooter.BonusAttackRange = 3.0f;
                Assert.That(turret.EffectiveAttackRange, Is.EqualTo(12.0f).Within(0.001f));
                shooter.BonusAttackRange = 0f;
            }
            finally
            {
                Object.DestroyImmediate(turretObj);
            }

            // Test RocketPunchSkill (Chipset ID 3)
            RocketPunchSkill rocketPunch = player.AddComponent<RocketPunchSkill>();
            Assert.That(rocketPunch.EffectiveLaunchRange, Is.EqualTo(9.0f).Within(0.001f));

            // Test Legacy Skills (IDs 1, 2, 8, 5)
            StandardGunSkill stdGun = player.AddComponent<StandardGunSkill>();
            RifleSkill rifle = player.AddComponent<RifleSkill>();
            ShotgunSkill shotgun = player.AddComponent<ShotgunSkill>();
            MultigunSkill multigun = player.AddComponent<MultigunSkill>();

            Assert.That(stdGun.EffectiveAttackRange, Is.EqualTo(9.0f).Within(0.001f));
            Assert.That(rifle.EffectiveAttackRange, Is.EqualTo(9.0f).Within(0.001f));
            Assert.That(shotgun.EffectiveAttackRange, Is.EqualTo(9.0f).Within(0.001f));
            Assert.That(multigun.EffectiveAttackRange, Is.EqualTo(9.0f).Within(0.001f));

            // Dynamic scaling test: Tăng tầm bắn Player lên 15m (+3m bonus)
            shooter.BonusAttackRange = 3.0f;
            Assert.That(shooter.SharedAttackRange, Is.EqualTo(15.0f).Within(0.001f));
            Assert.That(shooter.ChipsetAttackRange, Is.EqualTo(12.0f).Within(0.001f));
            Assert.That(rocketPunch.EffectiveLaunchRange, Is.EqualTo(12.0f).Within(0.001f));
            Assert.That(stdGun.EffectiveAttackRange, Is.EqualTo(12.0f).Within(0.001f));
            Assert.That(rifle.EffectiveAttackRange, Is.EqualTo(12.0f).Within(0.001f));
            Assert.That(shotgun.EffectiveAttackRange, Is.EqualTo(12.0f).Within(0.001f));
            Assert.That(multigun.EffectiveAttackRange, Is.EqualTo(12.0f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    private static void InvokePrivate(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Không tìm thấy hàm {methodName} để chạy kiểm thử.");
        method.Invoke(target, arguments);
    }

    private static T InvokePrivateWithResult<T>(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Không tìm thấy hàm {methodName} để chạy kiểm thử.");
        return (T)method.Invoke(target, arguments);
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Không tìm thấy field {fieldName}.");
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Không tìm thấy field {fieldName}.");
        return (T)field.GetValue(target);
    }
}
