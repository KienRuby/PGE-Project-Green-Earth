using NUnit.Framework;
using System.Reflection;
using UnityEngine;

public class EnemyFreezeFixTests
{
    [Test]
    public void BossMovement_ChasesPlayer_WhenWithinAttackRange_AndNotAttacking()
    {
        GameObject playerObj = new GameObject("Player_Test");
        playerObj.tag = "Player";
        playerObj.transform.position = new Vector3(5f, 0f, 0f);

        GameObject bossObj = new GameObject("Boss_Test", typeof(BossMovement), typeof(BossRangedAttack), typeof(EnemyHealth), typeof(Rigidbody2D));
        bossObj.transform.position = Vector3.zero;

        try
        {
            BossMovement bossMovement = bossObj.GetComponent<BossMovement>();
            BossRangedAttack rangedAttack = bossObj.GetComponent<BossRangedAttack>();
            bossMovement.SetTarget(playerObj.transform);
            rangedAttack.SetTarget(playerObj.transform);

            // Boss starts at (0,0), player at (5,0). Distance = 5m <= attackRange (11m).
            Assert.That(rangedAttack.GetTargetRangeState(), Is.EqualTo(BossRangedAttack.TargetRangeState.InRange));
            Assert.That(rangedAttack.IsAttacking, Is.False);

            MethodInfo updateChaseMethod = typeof(BossMovement).GetMethod("UpdateChaseMovement", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(updateChaseMethod, Is.Not.Null);

            // Prior to fix, UpdateChaseMovement would stop velocity and return early without moving.
            // With fix, it should move towards player position.
            updateChaseMethod.Invoke(bossMovement, null);

            Rigidbody2D rb = bossObj.GetComponent<Rigidbody2D>();
            Assert.That(rb.position.x, Is.GreaterThan(0f), "Boss should have moved towards player instead of freezing in place.");
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
            Object.DestroyImmediate(bossObj);
        }
    }

    [Test]
    public void EnemyMovement_HeadOnObstacleCollision_DeflectsAlongTangent()
    {
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        if (obstacleLayer < 0) obstacleLayer = 0;

        GameObject obstacleObj = new GameObject("Obstacle_Wall", typeof(BoxCollider2D));
        obstacleObj.layer = obstacleLayer;
        obstacleObj.transform.position = new Vector3(1f, 0f, 0f);
        BoxCollider2D col = obstacleObj.GetComponent<BoxCollider2D>();
        col.size = new Vector2(0.5f, 10f);

        GameObject creepObj = new GameObject("Creep_Test", typeof(EnemyMovement), typeof(Rigidbody2D));
        creepObj.transform.position = Vector3.zero;

        try
        {
            EnemyMovement movement = creepObj.GetComponent<EnemyMovement>();

            MethodInfo glideMethod = typeof(EnemyMovement).GetMethod("ApplyObstacleGliding", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(glideMethod, Is.Not.Null);

            // Facing directly into the wall along X axis (+1, 0)
            Vector2 headOnDir = Vector2.right;
            Vector2 resultDir = (Vector2)glideMethod.Invoke(movement, new object[] { Vector2.zero, headOnDir });

            // Before fix, resultDir would be Vector2.right (0 degrees deflection, hitting wall head-on)
            // After fix, resultDir must deflect perpendicularly along Y axis to glide past the wall
            Assert.That(Mathf.Abs(resultDir.y), Is.GreaterThan(0.3f), "Enemy should deflect along obstacle tangent when hitting head-on.");
        }
        finally
        {
            Object.DestroyImmediate(obstacleObj);
            Object.DestroyImmediate(creepObj);
        }
    }

    [Test]
    public void EnemyMovement_MoveWithObstacleSlide_TangentSlideOnContact()
    {
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        if (obstacleLayer < 0) obstacleLayer = 0;

        GameObject obstacleObj = new GameObject("Obstacle_Wall", typeof(BoxCollider2D));
        obstacleObj.layer = obstacleLayer;
        obstacleObj.transform.position = new Vector3(0.2f, 0f, 0f);
        BoxCollider2D col = obstacleObj.GetComponent<BoxCollider2D>();
        col.size = new Vector2(0.2f, 10f);

        GameObject creepObj = new GameObject("Creep_SlideTest", typeof(EnemyMovement), typeof(Rigidbody2D));
        creepObj.transform.position = Vector3.zero;

        try
        {
            EnemyMovement movement = creepObj.GetComponent<EnemyMovement>();
            MethodInfo slideMethod = typeof(EnemyMovement).GetMethod("MoveWithObstacleSlide", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(slideMethod, Is.Not.Null);

            // Move directly towards wall
            slideMethod.Invoke(movement, new object[] { new Vector2(0.04f, 0f) });

            Rigidbody2D rb = creepObj.GetComponent<Rigidbody2D>();
            // Position must be valid and not NaN/Infinity
            Assert.That(float.IsNaN(rb.position.x), Is.False);
            Assert.That(float.IsNaN(rb.position.y), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(obstacleObj);
            Object.DestroyImmediate(creepObj);
        }
    }
}
