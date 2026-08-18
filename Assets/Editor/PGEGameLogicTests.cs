using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class PGEGameLogicTests
{
    [Test]
    public void LabUpgrade_KeyGeneration_IsConsistent()
    {
        string key1 = LabUpgradeController.GetItemLevelKey("ATK", 1);
        string key2 = LabUpgradeController.GetItemLevelKey("atk", 1);
        string key3 = LabUpgradeController.GetItemLevelKey("DEF", 0);

        Assert.That(key1, Is.EqualTo("PGE.Lab.ItemLevel.ATK"));
        Assert.That(key2, Is.EqualTo("PGE.Lab.ItemLevel.ATK"));
        Assert.That(key3, Is.EqualTo("PGE.Lab.ItemLevel.DEF"));
    }

    [Test]
    public void PlayerStatsManager_GetStatLevel_ReadsCorrectly()
    {
        PlayerPrefs.SetInt("PGE.Lab.ItemLevel.HP", 5);
        PlayerPrefs.SetInt("PGE.Lab.ItemLevel.SPD", 3);
        PlayerPrefs.Save();

        int hpLevel = PlayerStatsManager.GetStatLevel("HP");
        int spdLevel = PlayerStatsManager.GetStatLevel("SPD");

        Assert.That(hpLevel, Is.EqualTo(5));
        Assert.That(spdLevel, Is.EqualTo(3));
    }

    [Test]
    public void PlayerHealth_DamageReduction_ReducesDamageCorrectly()
    {
        GameObject go = new GameObject("PlayerHealthTest");
        PlayerHealth health = go.AddComponent<PlayerHealth>();

        health.SetDamageReduction(5);
        int initialHp = health.CurrentHealth;

        health.TakeDamage(12);
        // Effective damage should be 12 - 5 = 7
        Assert.That(health.CurrentHealth, Is.EqualTo(initialHp - 7));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void ObjectPool_DoubleReturn_DoesNotDuplicateInQueue()
    {
        GameObject prefab = new GameObject("PoolTestPrefab");
        GameObject container = new GameObject("PoolContainer");

        ObjectPool pool = new ObjectPool(prefab, 1, false, container.transform);
        pool.Initialize(container.transform);

        GameObject instance = pool.Get(Vector3.zero, Quaternion.identity);
        Assert.That(instance, Is.Not.Null);

        // Return first time
        pool.Return(instance);

        // Return second time (attempt double enqueue)
        pool.Return(instance);

        // Get instance once
        GameObject firstGet = pool.Get(Vector3.zero, Quaternion.identity);
        Assert.That(firstGet, Is.EqualTo(instance));

        // Get instance second time (should be null since pool has size 1 and canGrow = false)
        GameObject secondGet = pool.Get(Vector3.zero, Quaternion.identity);
        Assert.That(secondGet, Is.Null);

        Object.DestroyImmediate(prefab);
        Object.DestroyImmediate(container);
    }

    [Test]
    public void PlayerDeathController_TriggersDeath_DisablesMovementAndInvokesEvents()
    {
        GameObject playerGo = new GameObject("PlayerTest");
        PlayerHealth health = playerGo.AddComponent<PlayerHealth>();
        PlayerMovement movement = playerGo.AddComponent<PlayerMovement>();
        PlayerDeathController deathCtrl = playerGo.AddComponent<PlayerDeathController>();

        bool deathStartedInvoked = false;
        deathCtrl.OnDeathStarted += () => { deathStartedInvoked = true; };

        deathCtrl.TriggerDeath();

        Assert.That(deathCtrl.IsDeathSequenceActive, Is.True);
        Assert.That(deathStartedInvoked, Is.True);
        Assert.That(movement.enabled, Is.False);

        Object.DestroyImmediate(playerGo);
    }

    [Test]
    public void DieAnimation_HasNoRootPositionCurves_ToPreventAnimatorTransformLock()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animaton/Player/Die.anim");
        Assert.That(clip, Is.Not.Null);

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        foreach (var binding in bindings)
        {
            if (string.IsNullOrEmpty(binding.path) && binding.propertyName.StartsWith("m_LocalPosition"))
            {
                Assert.Fail($"Die.anim contains root position curve: {binding.propertyName}, which locks Player movement in Animator!");
            }
        }
    }

    [Test]
    public void TargetFrameRate_IsConfiguredForSmoothGameplay()
    {
        Assert.That(Application.targetFrameRate, Is.GreaterThanOrEqualTo(60));
    }

    [Test]
    public void AAAGoldenStarParticleDissolveShader_LoadsAndContainsRequiredProperties()
    {
        Shader shader = Shader.Find("Custom/2D/SpriteDissolve");
        Assert.That(shader, Is.Not.Null, "Custom/2D/SpriteDissolve shader must exist in the project.");

        Material mat = new Material(shader);
        Assert.That(mat.HasProperty("_DissolveAmount"), Is.True);
        Assert.That(mat.HasProperty("_DissolveDirectionMode"), Is.True);
        Assert.That(mat.HasProperty("_ParticleShapeMode"), Is.True);
        Assert.That(mat.HasProperty("_ParticleGridSize"), Is.True);
        Assert.That(mat.HasProperty("_DisperseSpeed"), Is.True);
        Assert.That(mat.HasProperty("_RadialBurstSpread"), Is.True);
        Assert.That(mat.HasProperty("_UpwardDrift"), Is.True);
        Assert.That(mat.HasProperty("_SwirlStrength"), Is.True);
        Assert.That(mat.HasProperty("_DisperseChaos"), Is.True);
        Assert.That(mat.HasProperty("_ParticleShrink"), Is.True);
        Assert.That(mat.HasProperty("_Gravity"), Is.True);
        Assert.That(mat.HasProperty("_EdgeColor"), Is.True);
        Assert.That(mat.HasProperty("_InnerEdgeColor"), Is.True);
        Assert.That(mat.HasProperty("_EdgeIntensity"), Is.True);
        Assert.That(mat.HasProperty("_SupernovaFlash"), Is.True);
        Assert.That(mat.HasProperty("_StarSparkleSpeed"), Is.True);
        Assert.That(mat.HasProperty("_PrismaticShimmer"), Is.True);
        Assert.That(mat.HasProperty("_HaloGlowIntensity"), Is.True);
        Assert.That(mat.HasProperty("_SpriteUVRect"), Is.True);
        Object.DestroyImmediate(mat);
    }
}
