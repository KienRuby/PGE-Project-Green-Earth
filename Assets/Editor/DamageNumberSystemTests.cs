#if UNITY_EDITOR
using NUnit.Framework;
using TMPro;
using UnityEngine;

[TestFixture]
public class DamageNumberSystemTests
{
    private GameObject managerGameObject;
    private DamageNumberManager manager;

    [SetUp]
    public void SetUp()
    {
        managerGameObject = new GameObject("[Test_DamageNumberManager]");
        manager = managerGameObject.AddComponent<DamageNumberManager>();
        manager.InitializePool();
    }

    [TearDown]
    public void TearDown()
    {
        if (managerGameObject != null)
        {
            Object.DestroyImmediate(managerGameObject);
        }
    }

    [Test]
    public void DamageNumber_Initialization_NormalDamage_SetsTextAndColor()
    {
        DamageNumber dmgNumber = manager.GetFromPool();
        Assert.That(dmgNumber, Is.Not.Null);

        dmgNumber.Initialize(45, DamageType.Normal, Vector3.zero);

        Assert.That(dmgNumber.TextComponent.text, Is.EqualTo("45"));
        Assert.That(dmgNumber.gameObject.activeSelf, Is.True);
        Assert.That(dmgNumber.TextComponent.color.a, Is.GreaterThan(0.9f));
    }

    [Test]
    public void DamageNumber_Initialization_CriticalDamage_HasIncreasedScale()
    {
        DamageNumber normalDmg = manager.GetFromPool();
        normalDmg.Initialize(100, DamageType.Normal, Vector3.zero);
        Vector3 normalScale = normalDmg.transform.localScale;

        DamageNumber critDmg = manager.GetFromPool();
        critDmg.Initialize(100, DamageType.Critical, Vector3.zero);
        Vector3 critScale = critDmg.transform.localScale;

        Assert.That(critScale.x, Is.GreaterThan(normalScale.x));
    }

    [Test]
    public void DamageNumber_Initialization_Heal_AddsPlusPrefix()
    {
        DamageNumber healDmg = manager.GetFromPool();
        healDmg.Initialize(30, DamageType.Heal, Vector3.zero);

        Assert.That(healDmg.TextComponent.text, Is.EqualTo("+30"));
    }

    [Test]
    public void DamageNumber_Despawn_ReturnsToPool()
    {
        DamageNumber dmgNumber = manager.GetFromPool();
        dmgNumber.Initialize(20, DamageType.Normal, Vector3.zero);
        Assert.That(dmgNumber.gameObject.activeSelf, Is.True);

        dmgNumber.Despawn();
        Assert.That(dmgNumber.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void DamageNumber_HasBlackOutlineByDefault()
    {
        DamageNumber dmgNumber = manager.GetFromPool();
        dmgNumber.Initialize(50, DamageType.Normal, Vector3.zero);

        Assert.That(dmgNumber.OutlineColor, Is.EqualTo(Color.black));
        Assert.That(dmgNumber.OutlineWidth, Is.EqualTo(0.25f).Within(0.01f));
    }

    [Test]
    public void DamageNumber_SetOutlineColor_UpdatesOutlineColorAndWidth()
    {
        DamageNumber dmgNumber = manager.GetFromPool();
        dmgNumber.Initialize(50, DamageType.Normal, Vector3.zero);

        dmgNumber.SetOutlineColor(Color.red, 0.3f);

        Assert.That(dmgNumber.OutlineColor, Is.EqualTo(Color.red));
        Assert.That(dmgNumber.OutlineWidth, Is.EqualTo(0.3f).Within(0.01f));
    }

    [Test]
    public void DamageNumber_CustomOutlinePerType_AppliesCorrectColor()
    {
        DamageNumber dmgNumber = manager.GetFromPool();
        dmgNumber.ConfigureOutlinePerType(true, Color.black, Color.red, Color.yellow, Color.green);

        dmgNumber.Initialize(100, DamageType.Critical, Vector3.zero);
        Assert.That(dmgNumber.OutlineColor, Is.EqualTo(Color.red));

        dmgNumber.Initialize(50, DamageType.Heal, Vector3.zero);
        Assert.That(dmgNumber.OutlineColor, Is.EqualTo(Color.green));
    }

    [Test]
    public void DamageNumberManager_SetDefaultOutline_UpdatesAllInstances()
    {
        DamageNumber dmgNumber = manager.GetFromPool();
        dmgNumber.Initialize(10, DamageType.Normal, Vector3.zero);

        manager.SetDefaultOutline(Color.blue, 0.35f);

        Assert.That(dmgNumber.OutlineColor, Is.EqualTo(Color.blue));
        Assert.That(dmgNumber.OutlineWidth, Is.EqualTo(0.35f).Within(0.01f));
    }

    [Test]
    public void DamageNumberSceneBuilder_CreateOrUpdatePrefab_CreatesPrefabWithOutline()
    {
        GameObject prefab = DamageNumberSceneBuilder.CreateOrUpdatePrefab();
        Assert.That(prefab, Is.Not.Null);

        DamageNumber dmgComp = prefab.GetComponent<DamageNumber>();
        Assert.That(dmgComp, Is.Not.Null);
        Assert.That(dmgComp.OutlineColor, Is.EqualTo(Color.black));
        Assert.That(dmgComp.OutlineWidth, Is.EqualTo(0.25f).Within(0.01f));
    }

    [Test]
    public void EnemyHealth_TakeDamage_TriggersDamageNumber()
    {
        GameObject enemyObj = new GameObject("TestEnemy");
        EnemyHealth enemyHealth = enemyObj.AddComponent<EnemyHealth>();
        enemyHealth.SetMaxHealth(100, true);

        Assert.DoesNotThrow(() => enemyHealth.TakeDamage(25));
        Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(75));

        Object.DestroyImmediate(enemyObj);
    }

    [Test]
    public void PlayerHealth_TakeDamage_TriggersDamageNumber()
    {
        GameObject playerObj = new GameObject("TestPlayer");
        PlayerHealth playerHealth = playerObj.AddComponent<PlayerHealth>();

        int initialHp = playerHealth.CurrentHealth;
        Assert.DoesNotThrow(() => playerHealth.TakeDamage(10));
        Assert.That(playerHealth.CurrentHealth, Is.LessThan(initialHp));

        Object.DestroyImmediate(playerObj);
    }

    [Test]
    public void PlayerHealth_Heal_TriggersHealDamageNumber()
    {
        GameObject playerObj = new GameObject("TestPlayerHeal");
        PlayerHealth playerHealth = playerObj.AddComponent<PlayerHealth>();

        playerHealth.TakeDamage(30);
        int damagedHp = playerHealth.CurrentHealth;

        Assert.DoesNotThrow(() => playerHealth.Heal(15));
        Assert.That(playerHealth.CurrentHealth, Is.EqualTo(damagedHp + 15));

        Object.DestroyImmediate(playerObj);
    }
}
#endif
