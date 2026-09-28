#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class RocketPunchHomingTests
{
    private const string CreepMinePrefabPath = "Assets/Prefabs/Gen Mine/creep map mine.prefab";
    private const string BigCreepMinePrefabPath = "Assets/Prefabs/Gen Mine/BigCreep.prefab";
    private const string BossMinePrefabPath = "Assets/Prefabs/Gen Mine/boss map mine.prefab";
    private const string RocketPunchPrefabPath = "Assets/Prefabs/Chipset/RocketPunch.prefab";

    private static GameObject LoadPrefabByName(string name, string fallbackPath)
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:Prefab");
        foreach (string guid in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(p).Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<GameObject>(p);
        }
        return AssetDatabase.LoadAssetAtPath<GameObject>(fallbackPath);
    }

    [Test]
    public void GenMineEnemyPrefabs_MustBeAssignedToEnemyLayer()
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        Assert.That(enemyLayer, Is.EqualTo(7), "Layer 'Enemy' must be Layer 7.");

        GameObject creepPrefab = LoadPrefabByName("creep map mine", CreepMinePrefabPath);
        Assert.That(creepPrefab, Is.Not.Null, "creep map mine.prefab must exist.");
        Assert.That(creepPrefab.layer, Is.EqualTo(enemyLayer), "creep map mine must be on Layer 7 (Enemy).");

        GameObject bigCreepPrefab = LoadPrefabByName("BigCreep", BigCreepMinePrefabPath);
        Assert.That(bigCreepPrefab, Is.Not.Null, "BigCreep.prefab must exist.");
        Assert.That(bigCreepPrefab.layer, Is.EqualTo(enemyLayer), "BigCreep must be on Layer 7 (Enemy).");

        GameObject bossPrefab = LoadPrefabByName("boss map mine", BossMinePrefabPath);
        Assert.That(bossPrefab, Is.Not.Null, "boss map mine.prefab must exist.");
        Assert.That(bossPrefab.layer, Is.EqualTo(enemyLayer), "boss map mine must be on Layer 7 (Enemy).");
    }

    [Test]
    public void RocketPunchPrefab_ColliderRadius_IsSufficientlyLarge()
    {
        GameObject punchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RocketPunchPrefabPath);
        Assert.That(punchPrefab, Is.Not.Null, "RocketPunch prefab must exist.");

        CircleCollider2D col = punchPrefab.GetComponent<CircleCollider2D>();
        Assert.That(col, Is.Not.Null, "RocketPunch must have a CircleCollider2D.");

        float worldRadius = col.radius * Mathf.Abs(punchPrefab.transform.localScale.x);
        Assert.That(worldRadius, Is.GreaterThanOrEqualTo(0.25f), "RocketPunch world collision radius must be at least 0.25m to match visual size.");
    }

    [Test]
    public void RocketPunchProjectile_DistancePointToSegment_CalculatesCorrectly()
    {
        Vector2 start = new Vector2(0f, 0f);
        Vector2 end = new Vector2(2f, 0f);

        // Point directly on segment
        float distOnSeg = RocketPunchProjectile.DistancePointToSegment(new Vector2(1f, 0f), start, end);
        Assert.That(distOnSeg, Is.EqualTo(0f).Within(0.0001f));

        // Point perpendicular to midpoint (offset by 0.3m on Y)
        float distPerp = RocketPunchProjectile.DistancePointToSegment(new Vector2(1f, 0.3f), start, end);
        Assert.That(distPerp, Is.EqualTo(0.3f).Within(0.0001f));

        // Point before start
        float distBefore = RocketPunchProjectile.DistancePointToSegment(new Vector2(-1f, 0f), start, end);
        Assert.That(distBefore, Is.EqualTo(1f).Within(0.0001f));

        // Point after end
        float distAfter = RocketPunchProjectile.DistancePointToSegment(new Vector2(3f, 0f), start, end);
        Assert.That(distAfter, Is.EqualTo(1f).Within(0.0001f));
    }

    [Test]
    public void RocketPunchSkill_LaunchSpeed_DefaultsToFive()
    {
        GameObject playerObj = new GameObject("Test_Player");
        try
        {
            RocketPunchSkill skill = playerObj.AddComponent<RocketPunchSkill>();
            Assert.That(skill.LaunchSpeed, Is.EqualTo(5.0f), "RocketPunchSkill.LaunchSpeed must default to 5 m/s.");
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
        }
    }

    [MenuItem("PGE/Tests/Run RocketPunch Homing Tests")]
    public static void RunTestsMenu()
    {
        AutoRunTestsOnLoad();
    }

    [InitializeOnLoadMethod]
    private static void AutoRunTestsOnLoad()
    {
        try
        {
            var tests = new RocketPunchHomingTests();
            tests.GenMineEnemyPrefabs_MustBeAssignedToEnemyLayer();
            tests.RocketPunchPrefab_ColliderRadius_IsSufficientlyLarge();
            tests.RocketPunchProjectile_DistancePointToSegment_CalculatesCorrectly();
            tests.RocketPunchSkill_LaunchSpeed_DefaultsToFive();
            
            string report = "[RocketPunchHomingTests] ALL 4 TESTS PASSED SUCCESSFULLY!\n" +
                            "1. GenMineEnemyPrefabs_MustBeAssignedToEnemyLayer: PASSED (Layer 7)\n" +
                            "2. RocketPunchPrefab_ColliderRadius_IsSufficientlyLarge: PASSED (Radius >= 0.25m)\n" +
                            "3. RocketPunchProjectile_DistancePointToSegment_CalculatesCorrectly: PASSED\n" +
                            "4. RocketPunchSkill_LaunchSpeed_DefaultsToFive: PASSED (LaunchSpeed = 5.0m/s)\n" +
                            $"Timestamp: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            System.IO.File.WriteAllText("Reports/rocket_punch_test_result.txt", report);
            Debug.Log("<color=green>[RocketPunchHomingTests] ALL 4 TESTS PASSED SUCCESSFULLY!</color>");
        }
        catch (System.Exception ex)
        {
            string err = $"[RocketPunchHomingTests] TEST FAILED: {ex.Message}\n{ex.StackTrace}";
            System.IO.File.WriteAllText("Reports/rocket_punch_test_result.txt", err);
            Debug.LogError(err);
        }
    }
}
#endif
