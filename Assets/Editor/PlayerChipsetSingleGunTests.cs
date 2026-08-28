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
