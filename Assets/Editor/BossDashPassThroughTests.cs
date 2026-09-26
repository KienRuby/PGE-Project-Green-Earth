#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class BossDashPassThroughTests
{
    private const string BossMinePrefabPath = "Assets/Prefabs/Gen Mine/boss map mine.prefab";

    [Test]
    public void BossMapMinePrefab_MaxHealth_Is100000()
    {
        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossMinePrefabPath);
        Assert.That(bossPrefab, Is.Not.Null, "boss map mine.prefab must exist.");

        EnemyHealth health = bossPrefab.GetComponent<EnemyHealth>();
        Assert.That(health, Is.Not.Null, "boss map mine must have EnemyHealth component.");
        Assert.That(health.MaxHealth, Is.EqualTo(100000), "EnemyHealth.MaxHealth on boss map mine must be 100,000.");

        BossEnemy bossEnemy = bossPrefab.GetComponent<BossEnemy>();
        Assert.That(bossEnemy, Is.Not.Null, "boss map mine must have BossEnemy component.");
        Assert.That(bossEnemy.MaxHealth, Is.EqualTo(100000), "BossEnemy.MaxHealth on boss map mine must be 100,000.");
    }

    [Test]
    public void BossMapMinePrefab_DashSettings_AreConfiguredCorrectly()
    {
        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossMinePrefabPath);
        Assert.That(bossPrefab, Is.Not.Null, "boss map mine.prefab must exist.");

        BossMovement bossMovement = bossPrefab.GetComponent<BossMovement>();
        Assert.That(bossMovement, Is.Not.Null, "boss map mine must have BossMovement component.");
        Assert.That(bossMovement.EnableDashAttack, Is.True, "EnableDashAttack must be enabled.");
        Assert.That(bossMovement.DashPassThroughPlayer, Is.True, "DashPassThroughPlayer must be enabled.");
        Assert.That(bossMovement.DashDamage, Is.EqualTo(100), "DashDamage must be 100.");
    }

    [Test]
    public void BossMovement_DashState_IgnoresPlayerCollisionAndDealsHeavyDamage()
    {
        // Setup Player GameObject
        GameObject playerObj = new GameObject("TestPlayer");
        playerObj.tag = "Player";
        CircleCollider2D playerCol = playerObj.AddComponent<CircleCollider2D>();
        playerCol.radius = 0.5f;
        PlayerHealth playerHealth = playerObj.AddComponent<PlayerHealth>();
        playerHealth.SetMaxHealth(500, true);

        // Setup Boss GameObject
        GameObject bossObj = new GameObject("TestBoss");
        CapsuleCollider2D bossCol = bossObj.AddComponent<CapsuleCollider2D>();
        bossCol.size = new Vector2(2f, 2f);
        Rigidbody2D bossRb = bossObj.GetComponent<Rigidbody2D>();
        BossMovement bossMovement = bossObj.GetComponent<BossMovement>();
        bossMovement.SetTarget(playerObj.transform);
        bossMovement.DashPassThroughPlayer = true;
        bossMovement.DashDamage = 100;

        try
        {
            // Initial state: collision is NOT ignored
            Assert.That(Physics2D.GetIgnoreCollision(bossCol, playerCol), Is.False, "Initially, collision between Boss and Player should not be ignored.");

            // Start Dash via Reflection (private StartDash)
            var startDashMethod = typeof(BossMovement).GetMethod("StartDash", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(startDashMethod, Is.Not.Null, "StartDash method should exist.");
            startDashMethod.Invoke(bossMovement, null);

            // Now in Dash: collision with Player MUST be ignored so Boss can pass through
            Assert.That(bossMovement.CurrentState, Is.EqualTo(BossMovement.BossState.Dash), "Boss should be in Dash state.");
            Assert.That(Physics2D.GetIgnoreCollision(bossCol, playerCol), Is.True, "During Dash, collision between Boss and Player MUST be ignored to pass through.");

            // Position boss overlapping player and check dash damage
            bossObj.transform.position = playerObj.transform.position;
            var checkDashDamageMethod = typeof(BossMovement).GetMethod("CheckDashHitPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(checkDashDamageMethod, Is.Not.Null, "CheckDashHitPlayer method should exist.");
            checkDashDamageMethod.Invoke(bossMovement, null);

            // Player should take 100 damage
            Assert.That(playerHealth.CurrentHealth, Is.EqualTo(400), "Player should have taken 100 dash damage (500 - 100 = 400).");

            // Calling it again during the same dash must not deal damage again
            checkDashDamageMethod.Invoke(bossMovement, null);
            Assert.That(playerHealth.CurrentHealth, Is.EqualTo(350), "Player should not take damage twice during the same dash.");

            // End Dash via StartRecover
            var startRecoverMethod = typeof(BossMovement).GetMethod("StartRecover", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(startRecoverMethod, Is.Not.Null, "StartRecover method should exist.");
            startRecoverMethod.Invoke(bossMovement, null);

            // After dash finishes: collision is restored
            Assert.That(Physics2D.GetIgnoreCollision(bossCol, playerCol), Is.False, "After Dash ends, normal collision between Boss and Player must be restored.");
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
            Object.DestroyImmediate(bossObj);
        }
    }
}
#endif
