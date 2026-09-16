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
    public void DamageNumber_Initialization_CriticalDamage_HasConfiguredCritScale()
    {
        DamageNumber normalDmg = manager.GetFromPool();
        normalDmg.Initialize(100, DamageType.Normal, Vector3.zero);
        Vector3 normalScale = normalDmg.transform.localScale;

        DamageNumber critDmg = manager.GetFromPool();
        critDmg.Initialize(100, DamageType.Critical, Vector3.zero);
        Vector3 critScale = critDmg.transform.localScale;

        float expectedRatio = critDmg.CritScaleMultiplier;
        Assert.That(critScale.x, Is.EqualTo(normalScale.x * expectedRatio).Within(0.01f));
    }

    [Test]
    public void DamageNumber_Initialization_CriticalDamage_HasCritFormatting()
    {
        DamageNumber critDmg = manager.GetFromPool();
        critDmg.Initialize(120, DamageType.Critical, Vector3.zero);

        Assert.That(critDmg.TextComponent.text, Is.EqualTo("120"));
        Assert.That(critDmg.IsCritActive, Is.True);
        Assert.That(critDmg.CritIconRenderer, Is.Not.Null);
        Assert.That(critDmg.CritIconRenderer.gameObject.activeSelf, Is.True);
    }

    [Test]
    public void DamageNumber_Initialization_PlayerDamage_HasMinusPrefix()
    {
        DamageNumber playerDmg = manager.GetFromPool();
        playerDmg.Initialize(35, DamageType.PlayerDamage, Vector3.zero);

        Assert.That(playerDmg.TextComponent.text, Is.EqualTo("-35"));
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

    [Test]
    public void DamageNumber_CritCustomScale_ShrinksScaleAccordingly()
    {
        DamageNumber dmgNumber = manager.GetFromPool();
        dmgNumber.CritScaleMultiplier = 0.5f;
        dmgNumber.Initialize(100, DamageType.Critical, Vector3.zero);

        Vector3 scaleShrunk = dmgNumber.transform.localScale;

        dmgNumber.CritScaleMultiplier = 1.5f;
        dmgNumber.Initialize(100, DamageType.Critical, Vector3.zero);

        Vector3 scaleEnlarged = dmgNumber.transform.localScale;

        Assert.That(scaleShrunk.x, Is.LessThan(scaleEnlarged.x));
    }

    [Test]
    public void DamageNumberManager_CritSettings_SynchronizesToAllInstances()
    {
        manager.DefaultCritScale = 0.7f;
        manager.DefaultCritIconSize = 0.4f;
        manager.DefaultCritIconSpacing = 0.12f;
        manager.DefaultCritIconOffset = new Vector2(0.05f, -0.02f);
        manager.DefaultCritSpawnOffset = new Vector3(0f, 0.5f, 0f);

        DamageNumber dmgNumber = manager.GetFromPool();
        Assert.That(dmgNumber.CritScaleMultiplier, Is.EqualTo(0.7f).Within(0.001f));
        Assert.That(dmgNumber.CritIconSize, Is.EqualTo(0.4f).Within(0.001f));
        Assert.That(dmgNumber.CritIconSpacing, Is.EqualTo(0.12f).Within(0.001f));
        Assert.That(dmgNumber.CritIconOffset.x, Is.EqualTo(0.05f).Within(0.001f));
        Assert.That(dmgNumber.CritSpawnOffset.y, Is.EqualTo(0.5f).Within(0.001f));
    }

    [Test]
    public void DamageNumber_CritLayout_IconAlwaysStandsBeforeFirstCharacter_WithNoOverlap()
    {
        DamageNumber dmgNumber = manager.GetFromPool();
        dmgNumber.CritIconOffset = Vector2.zero;
        dmgNumber.CritIconSpacing = 0.03f;
        dmgNumber.CritIconSize = 0.42f;

        int[] testAmounts = { 5, 88, 1000, 10000, 150000 };
        foreach (int amount in testAmounts)
        {
            dmgNumber.Initialize(amount, DamageType.Critical, Vector3.zero);

            float iconRightEdge = dmgNumber.CritIconRenderer.transform.localPosition.x + (dmgNumber.CritIconSize * 0.41f);
            float firstCharLeft = dmgNumber.GetFirstCharacterLeftEdge();

            // Mép phải của icon phải luôn nằm trước bên trái của con số đầu tiên
            Assert.That(iconRightEdge, Is.LessThan(firstCharLeft), $"Icon overlapping at amount {amount}!");
            // Khoảng cách tự giãn cách giữa icon và số đúng bằng critIconSpacing (không che khuất)
            float gap = firstCharLeft - iconRightEdge;
            Assert.That(gap, Is.EqualTo(0.03f).Within(0.015f), $"Gap mismatch at amount {amount}!");
        }
    }

    [Test]
    public void CritIconProxy_AdjustingValues_SyncsBidirectionallyWithParent()
    {
        DamageNumber dmgNumber = manager.GetFromPool();
        dmgNumber.Initialize(100, DamageType.Critical, Vector3.zero);

        CritIconProxy proxy = dmgNumber.CritIconRenderer.GetComponent<CritIconProxy>();
        Assert.That(proxy, Is.Not.Null);

        // Adjust via proxy
        proxy.IconSize = 0.38f;
        proxy.IconSpacing = 0.02f;
        proxy.IconOffset = new Vector2(0.01f, -0.01f);

        Assert.That(dmgNumber.CritIconSize, Is.EqualTo(0.38f).Within(0.001f));
        Assert.That(dmgNumber.CritIconSpacing, Is.EqualTo(0.02f).Within(0.001f));
        Assert.That(dmgNumber.CritIconOffset.x, Is.EqualTo(0.01f).Within(0.001f));

        // Adjust via parent
        dmgNumber.CritIconSize = 0.45f;
        proxy.SyncFromParent();
        Assert.That(proxy.IconSize, Is.EqualTo(0.45f).Within(0.001f));
    }

    [Test]
    public void CritIconProxy_TransformScaleChange_UpdatesIconSizeAutomatically()
    {
        DamageNumber dmgNumber = manager.GetFromPool();
        dmgNumber.Initialize(100, DamageType.Critical, Vector3.zero);

        CritIconProxy proxy = dmgNumber.CritIconRenderer.GetComponent<CritIconProxy>();
        Assert.That(proxy, Is.Not.Null);

        // Giả lập người dùng dùng Scale tool trong Unity làm thay đổi localScale của CritIcon
        dmgNumber.CritIconRenderer.transform.localScale = new Vector3(0.061328128f, 0.061328128f, 1f);
        dmgNumber.CritIconRenderer.transform.hasChanged = true;

        proxy.CheckTransformChanges();

        Assert.That(dmgNumber.CritIconSize, Is.EqualTo(0.314f).Within(0.005f));
        Assert.That(proxy.IconSize, Is.EqualTo(0.314f).Within(0.005f));
    }

    [Test]
    public void DamageNumberManager_SyncFromPreviewOrPrefab_AdoptsPreviewSettings()
    {
        GameObject previewObj = new GameObject("[DamageNumber_Preview]");
        try
        {
            DamageNumber previewDn = previewObj.AddComponent<DamageNumber>();
            previewDn.CritIconSize = 0.314f;
            previewDn.CritIconSpacing = 0.03f;
            previewDn.CritScaleMultiplier = 1.15f;

            manager.SyncFromPreviewOrPrefab();

            Assert.That(manager.DefaultCritIconSize, Is.EqualTo(0.314f).Within(0.001f));
            Assert.That(manager.DefaultCritIconSpacing, Is.EqualTo(0.03f).Within(0.001f));
            Assert.That(manager.DefaultCritScale, Is.EqualTo(1.15f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(previewObj);
        }
    }

    [Test]
    public void DamageNumber_ApplyOutline_WithMissingSharedMaterial_DoesNotThrow()
    {
        GameObject testObject = new GameObject("DamageNumber_MissingMaterial_Test");
        try
        {
            TextMeshPro text = testObject.AddComponent<TextMeshPro>();
            DamageNumber damageNumber = testObject.AddComponent<DamageNumber>();
            text.fontSharedMaterial = null;

            Assert.DoesNotThrow(() => damageNumber.ApplyOutline());
        }
        finally
        {
            Object.DestroyImmediate(testObject);
        }
    }

    [Test]
    public void DamageNumber_ApplyOutline_InEditMode_PreservesSerializableSharedMaterial()
    {
        GameObject testObject = new GameObject("DamageNumber_SharedMaterial_Test");
        Material sharedMaterial = null;
        try
        {
            TextMeshPro text = testObject.AddComponent<TextMeshPro>();
            DamageNumber damageNumber = testObject.AddComponent<DamageNumber>();
            sharedMaterial = new Material(Shader.Find("TextMeshPro/Distance Field"));
            text.fontSharedMaterial = sharedMaterial;

            damageNumber.ApplyOutline();

            Assert.That(text.fontSharedMaterial, Is.SameAs(sharedMaterial));
        }
        finally
        {
            Object.DestroyImmediate(testObject);
            if (sharedMaterial != null)
            {
                Object.DestroyImmediate(sharedMaterial);
            }
        }
    }
}
#endif
