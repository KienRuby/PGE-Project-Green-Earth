using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class GemPoolingOptimizationTests
{
    [SetUp]
    public void SetUp()
    {
        GemPickup.ClearActiveGemsForTesting();
        DropTable.ResetTemplateForTesting();
    }

    [TearDown]
    public void TearDown()
    {
        GemPickup.ClearActiveGemsForTesting();
        DropTable.ResetTemplateForTesting();
    }

    [Test]
    public void Test01_GemPickup_SharedMaterialUsed_NoMaterialClones()
    {
        GameObject gemObj = new GameObject("TestGem_Material", typeof(CircleCollider2D), typeof(GemPickup));
        try
        {
            GemPickup gem = gemObj.GetComponent<GemPickup>();
            gem.Initialize(GemType.BlueExp, 10, Vector3.zero);

            Transform glowTrans = gemObj.transform.Find("GemGlow");
            Transform sparkleTrans = gemObj.transform.Find("GemSparkle");

            Assert.That(glowTrans, Is.Not.Null, "GemGlow child must exist");
            Assert.That(sparkleTrans, Is.Not.Null, "GemSparkle child must exist");

            SpriteRenderer glowRenderer = glowTrans.GetComponent<SpriteRenderer>();
            SpriteRenderer sparkleRenderer = sparkleTrans.GetComponent<SpriteRenderer>();

            Assert.That(glowRenderer.sharedMaterial, Is.Not.Null);
            Assert.That(sparkleRenderer.sharedMaterial, Is.Not.Null);

            // sharedMaterial name should not end with '(Instance)'
            Assert.That(glowRenderer.sharedMaterial.name, Does.Not.Contain("(Instance)"),
                "glowRenderer must use sharedMaterial to avoid runtime material cloning leaks and enable GPU instancing.");
            Assert.That(sparkleRenderer.sharedMaterial.name, Does.Not.Contain("(Instance)"),
                "sparkleRenderer must use sharedMaterial to avoid runtime material cloning leaks and enable GPU instancing.");
        }
        finally
        {
            Object.DestroyImmediate(gemObj);
        }
    }

    [Test]
    public void Test02_GemPickup_AddValue_UpgradesTierAndPreservesExp()
    {
        GameObject gemObj = new GameObject("TestGem_AddValue", typeof(CircleCollider2D), typeof(GemPickup));
        try
        {
            GemPickup gem = gemObj.GetComponent<GemPickup>();
            gem.Initialize(GemType.BlueExp, 10, Vector3.zero);

            Assert.That(gem.Type, Is.EqualTo(GemType.BlueExp));
            Assert.That(gem.Value, Is.EqualTo(10));

            // Upgrade to Purple threshold (>= 20)
            gem.AddValue(15);
            Assert.That(gem.Value, Is.EqualTo(25));
            Assert.That(gem.Type, Is.EqualTo(GemType.PurpleExp));

            // Upgrade to Yellow threshold (>= 50)
            gem.AddValue(30);
            Assert.That(gem.Value, Is.EqualTo(55));
            Assert.That(gem.Type, Is.EqualTo(GemType.YellowExp));

            // Upgrade to Red threshold (>= 100)
            gem.AddValue(50);
            Assert.That(gem.Value, Is.EqualTo(105));
            Assert.That(gem.Type, Is.EqualTo(GemType.RedExp));
        }
        finally
        {
            Object.DestroyImmediate(gemObj);
        }
    }

    [Test]
    public void Test03_GemPickup_PoolRecycling_MaintainsZeroAllocationCycle()
    {
        GameObject template = DropTable.GetOrCreateGemTemplate();
        Assert.That(template, Is.Not.Null);

        GameObject poolContainer = new GameObject("PoolContainer");
        ObjectPool pool = new ObjectPool(template, initialSize: 2, canGrow: true, container: poolContainer.transform);
        pool.Initialize(poolContainer.transform);

        try
        {
            // Spawn from pool
            GameObject spawned1 = pool.Get(Vector3.zero, Quaternion.identity);
            Assert.That(spawned1, Is.Not.Null);
            Assert.That(spawned1.activeInHierarchy, Is.True);

            GemPickup gem = spawned1.GetComponent<GemPickup>();
            Assert.That(gem, Is.Not.Null);
            gem.Initialize(GemType.PurpleExp, 20, Vector3.one);

            // Despawn back to pool
            gem.Despawn();
            Assert.That(spawned1.activeInHierarchy, Is.False, "Despawned gem must be deactivated in pool");

            // Re-spawn from pool
            GameObject spawned2 = pool.Get(new Vector3(5f, 5f, 0f), Quaternion.identity);
            Assert.That(spawned2, Is.EqualTo(spawned1), "Pool should reuse the existing recycled instance");
            Assert.That(spawned2.activeInHierarchy, Is.True);
            Assert.That(spawned2.transform.position, Is.EqualTo(new Vector3(5f, 5f, 0f)));
        }
        finally
        {
            Object.DestroyImmediate(poolContainer);
            Object.DestroyImmediate(template);
        }
    }

    [Test]
    public void Test04_GemCap_TryMergeOrUpgradeGem_MergesFurthestExpGem()
    {
        GameObject gem1 = new GameObject("Gem1", typeof(CircleCollider2D), typeof(GemPickup));
        GameObject gem2 = new GameObject("Gem2", typeof(CircleCollider2D), typeof(GemPickup));

        try
        {
            GemPickup p1 = gem1.GetComponent<GemPickup>();
            GemPickup p2 = gem2.GetComponent<GemPickup>();

            p1.Initialize(GemType.BlueExp, 10, new Vector3(2f, 0f, 0f));
            p2.Initialize(GemType.BlueExp, 10, new Vector3(10f, 0f, 0f)); // Furthest from (0,0,0)

            // Simulate cap hit and merge
            GemPickup merged = DropTable.TryMergeOrUpgradeGem(GemType.BlueExp, 15, Vector3.zero);

            Assert.That(merged, Is.Not.Null);
            Assert.That(merged, Is.EqualTo(p2), "Merging should pick the furthest idle gem from player/center");
            Assert.That(merged.Value, Is.EqualTo(25), "Value should combine (10 + 15 = 25)");
            Assert.That(merged.Type, Is.EqualTo(GemType.PurpleExp), "Gem should upgrade to Purple tier");
        }
        finally
        {
            Object.DestroyImmediate(gem1);
            Object.DestroyImmediate(gem2);
        }
    }

    [Test]
    public void Test05_GemPickup_BobbingKeepsRootPositionFixed()
    {
        GameObject gemObj = new GameObject("TestGem_RootPos", typeof(CircleCollider2D), typeof(GemPickup));
        try
        {
            GemPickup gem = gemObj.GetComponent<GemPickup>();
            Vector3 spawnPos = new Vector3(3f, 4f, 0f);
            gem.Initialize(GemType.BlueExp, 10, spawnPos);

            // Verify Root position is exactly at spawnPos
            Assert.That(gemObj.transform.position, Is.EqualTo(spawnPos));

            Transform visualTrans = gemObj.transform.Find("GemVisual");
            Assert.That(visualTrans, Is.Not.Null);

            // Root position must remain spawnPos (idle bobbing moves local children, never root transform)
            Assert.That(gemObj.transform.position, Is.EqualTo(spawnPos),
                "Root transform position must remain static so PhysX 2D is not dirtied every frame.");
        }
        finally
        {
            Object.DestroyImmediate(gemObj);
        }
    }
}
