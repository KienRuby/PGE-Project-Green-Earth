using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HealthBoxDropSystemTests
{
    private List<GameObject> cleanupObjects;

    [SetUp]
    public void SetUp()
    {
        cleanupObjects = new List<GameObject>();
        HealthBoxPickup.ClearActiveBoxesForTesting();
    }

    [TearDown]
    public void TearDown()
    {
        HealthBoxPickup.ClearActiveBoxesForTesting();
        for (int i = 0; i < cleanupObjects.Count; i++)
        {
            if (cleanupObjects[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(cleanupObjects[i]);
            }
        }
        cleanupObjects.Clear();
    }

    [Test]
    public void Test01_SpawnHealthBox_SmallAndLarge_SetsCorrectStatsAndSprites()
    {
        // 1. Kiểm tra hộp máu nhỏ
        Vector3 smallPos = new Vector3(2f, 3f, 0f);
        HealthBoxPickup smallBox = DropTable.SpawnHealthBox(smallPos, HealthBoxType.Small);
        cleanupObjects.Add(smallBox.gameObject);

        Assert.IsNotNull(smallBox, "Hộp máu nhỏ không được null.");
        Assert.AreEqual(HealthBoxType.Small, smallBox.BoxType);
        Assert.AreEqual(0.10f, smallBox.HealPercent, 0.001f, "Hộp máu nhỏ phải hồi 10% HP tối đa.");
        Assert.AreEqual(smallPos, smallBox.transform.position);

        CircleCollider2D smallCol = smallBox.GetComponent<CircleCollider2D>();
        Assert.IsNotNull(smallCol);
        Assert.IsTrue(smallCol.isTrigger);

        Sprite smallSprite = smallBox.CurrentSprite;
        Assert.IsNotNull(smallSprite, "Sprite hộp máu nhỏ phải được nạp thành công.");

        // 2. Kiểm tra hộp máu lớn
        Vector3 largePos = new Vector3(-1f, 4f, 0f);
        HealthBoxPickup largeBox = DropTable.SpawnHealthBox(largePos, HealthBoxType.Large);
        cleanupObjects.Add(largeBox.gameObject);

        Assert.IsNotNull(largeBox, "Hộp máu lớn không được null.");
        Assert.AreEqual(HealthBoxType.Large, largeBox.BoxType);
        Assert.AreEqual(0.20f, largeBox.HealPercent, 0.001f, "Hộp máu lớn phải hồi 20% HP tối đa.");
        Assert.AreEqual(largePos, largeBox.transform.position);

        CircleCollider2D largeCol = largeBox.GetComponent<CircleCollider2D>();
        Assert.IsNotNull(largeCol);
        Assert.IsTrue(largeCol.isTrigger);

        Sprite largeSprite = largeBox.CurrentSprite;
        Assert.IsNotNull(largeSprite, "Sprite hộp máu lớn phải được nạp thành công.");
    }

    [Test]
    public void Test02_CollectHealthBox_HealsPlayerPercentageOfMaxHealth()
    {
        GameObject playerObj = new GameObject("Player_Test");
        playerObj.tag = "Player";
        PlayerHealth playerHealth = playerObj.AddComponent<PlayerHealth>();
        playerHealth.SetMaxHealth(200, true);
        cleanupObjects.Add(playerObj);

        // Giả lập mất máu còn 50 HP (Max: 200 HP)
        playerHealth.TakeDamage(150);
        Assert.AreEqual(50, playerHealth.CurrentHealth);

        // 1. Nhặt hộp máu nhỏ (Hồi 10% của 200 = 20 HP) -> Máu tăng từ 50 lên 70 HP
        HealthBoxPickup smallBox = DropTable.SpawnHealthBox(Vector3.zero, HealthBoxType.Small);
        smallBox.Collect(playerObj);
        Assert.AreEqual(70, playerHealth.CurrentHealth, "Nhặt hộp nhỏ phải hồi đúng 20 HP (10% của 200 HP).");

        // 2. Nhặt hộp máu lớn (Hồi 20% của 200 = 40 HP) -> Máu tăng từ 70 lên 110 HP
        HealthBoxPickup largeBox = DropTable.SpawnHealthBox(Vector3.zero, HealthBoxType.Large);
        largeBox.Collect(playerObj);
        Assert.AreEqual(110, playerHealth.CurrentHealth, "Nhặt hộp lớn phải hồi đúng 40 HP (20% của 200 HP).");

        // 3. Kiểm tra hồi máu không vượt quá Max HP (200)
        playerHealth.TakeDamage(10); // còn 100
        for (int i = 0; i < 5; i++)
        {
            HealthBoxPickup extraBox = DropTable.SpawnHealthBox(Vector3.zero, HealthBoxType.Large);
            extraBox.Collect(playerObj);
        }
        Assert.AreEqual(200, playerHealth.CurrentHealth, "Máu không được vượt quá MaxHealth khi hồi.");
    }

    [Test]
    public void Test03_DropTable_ProbabilitiesRollCorrectly()
    {
        Assert.AreEqual(0.05f, DropTable.SmallHealthBoxDropChance, 0.0001f, "Tỷ lệ rơi hộp nhỏ mặc định là 5% (0.05).");
        Assert.AreEqual(0.03f, DropTable.LargeHealthBoxDropChance, 0.0001f, "Tỷ lệ rơi hộp lớn mặc định là 3% (0.03).");

        int smallCount = 0;
        int largeCount = 0;
        int totalRolls = 10000;

        for (int i = 0; i < totalRolls; i++)
        {
            HealthBoxPickup box = DropTable.TryDropHealthBox(Vector3.zero);
            if (box != null)
            {
                if (box.BoxType == HealthBoxType.Small) smallCount++;
                else if (box.BoxType == HealthBoxType.Large) largeCount++;
                UnityEngine.Object.DestroyImmediate(box.gameObject);
            }
        }

        float smallRatio = (float)smallCount / totalRolls;
        float largeRatio = (float)largeCount / totalRolls;

        // Cho phép dung sai thống kê Monte Carlo ±1.5%
        Assert.GreaterOrEqual(smallRatio, 0.035f, $"Tỷ lệ hộp nhỏ ({smallRatio:P2}) phải tiệm cận 5%.");
        Assert.LessOrEqual(smallRatio, 0.065f, $"Tỷ lệ hộp nhỏ ({smallRatio:P2}) phải tiệm cận 5%.");

        Assert.GreaterOrEqual(largeRatio, 0.018f, $"Tỷ lệ hộp lớn ({largeRatio:P2}) phải tiệm cận 3%.");
        Assert.LessOrEqual(largeRatio, 0.042f, $"Tỷ lệ hộp lớn ({largeRatio:P2}) phải tiệm cận 3%.");
    }

    [Test]
    public void Test04_EnemyDeath_TriggersHealthBoxDrop()
    {
        GameObject enemyObj = new GameObject("Enemy_Test");
        EnemyHealth enemyHealth = enemyObj.AddComponent<EnemyHealth>();
        enemyHealth.SetMaxHealth(100, true);
        cleanupObjects.Add(enemyObj);

        // Ép 100% rơi hộp máu nhỏ
        enemyHealth.SetHealthBoxDropChances(1f, 0f);
        enemyHealth.Die(true);

        IReadOnlyList<HealthBoxPickup> active = HealthBoxPickup.ActiveBoxes;
        Assert.GreaterOrEqual(active.Count, 1, "Enemy chết với tỷ lệ 100% phải sinh ít nhất 1 hộp máu.");
        HealthBoxPickup droppedBox = active[active.Count - 1];
        Assert.AreEqual(HealthBoxType.Small, droppedBox.BoxType);
        cleanupObjects.Add(droppedBox.gameObject);

        // Tạo enemy 2 ép 100% rơi hộp máu lớn
        GameObject enemyObj2 = new GameObject("Enemy_Boss_Test");
        EnemyHealth enemyHealth2 = enemyObj2.AddComponent<EnemyHealth>();
        enemyHealth2.SetMaxHealth(500, true);
        cleanupObjects.Add(enemyObj2);

        enemyHealth2.SetHealthBoxDropChances(0f, 1f);
        enemyHealth2.Die(true);

        active = HealthBoxPickup.ActiveBoxes;
        HealthBoxPickup droppedBox2 = active[active.Count - 1];
        Assert.AreEqual(HealthBoxType.Large, droppedBox2.BoxType);
        cleanupObjects.Add(droppedBox2.gameObject);
    }

    [Test]
    public void Test05_MagnetAttraction_PullsHealthBoxToPlayer()
    {
        GameObject playerObj = new GameObject("Player_Magnet");
        MagnetPickup magnet = playerObj.AddComponent<MagnetPickup>();
        magnet.SetBonusMagnetRadius(2f); // Tổng bán kính 3.5 + 2 = 5.5f
        cleanupObjects.Add(playerObj);

        // Sinh hộp máu trong phạm vi bán kính nam châm (khoảng cách 3.0f)
        Vector3 boxPos = playerObj.transform.position + new Vector3(3f, 0f, 0f);
        HealthBoxPickup box = DropTable.SpawnHealthBox(boxPos, HealthBoxType.Small);
        cleanupObjects.Add(box.gameObject);

        Assert.IsFalse(box.IsBeingAttracted);

        // Kích hoạt quét hút ngọc và hộp máu
        magnet.AttractNearbyGems();

        Assert.IsTrue(box.IsBeingAttracted, "Hộp máu trong tầm nam châm phải được kích hoạt lực hút về phía Player.");
    }

#if UNITY_EDITOR
    public const string ReportPath = "Assets/Editor/HealthBoxDropTestReport.txt";

    [MenuItem("PGE/Tests/Run Health Box Drop Tests")]
    public static void RunFromMenu()
    {
        RunAllTestsAndSaveReport();
    }

    public static string RunAllTestsAndSaveReport()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine("HEALTH BOX DROP SYSTEM TEST REPORT");
        sb.AppendLine($"Timestamp (UTC): {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
        sb.AppendLine("================================================================================");

        int passed = 0;
        int failed = 0;

        void ExecuteTest(string name, Action<HealthBoxDropSystemTests> testAction)
        {
            var testInstance = new HealthBoxDropSystemTests();
            try
            {
                testInstance.SetUp();
                testAction(testInstance);
                passed++;
                sb.AppendLine($"[PASS] {name}");
            }
            catch (Exception ex)
            {
                failed++;
                sb.AppendLine($"[FAIL] {name}: {ex.Message}");
                Debug.LogException(ex);
            }
            finally
            {
                testInstance.TearDown();
            }
        }

        ExecuteTest("Test01_SpawnHealthBox_SmallAndLarge_SetsCorrectStatsAndSprites", t => t.Test01_SpawnHealthBox_SmallAndLarge_SetsCorrectStatsAndSprites());
        ExecuteTest("Test02_CollectHealthBox_HealsPlayerPercentageOfMaxHealth", t => t.Test02_CollectHealthBox_HealsPlayerPercentageOfMaxHealth());
        ExecuteTest("Test03_DropTable_ProbabilitiesRollCorrectly", t => t.Test03_DropTable_ProbabilitiesRollCorrectly());
        ExecuteTest("Test04_EnemyDeath_TriggersHealthBoxDrop", t => t.Test04_EnemyDeath_TriggersHealthBoxDrop());
        ExecuteTest("Test05_MagnetAttraction_PullsHealthBoxToPlayer", t => t.Test05_MagnetAttraction_PullsHealthBoxToPlayer());

        sb.AppendLine("================================================================================");
        sb.AppendLine($"SUMMARY: Total={passed + failed}, Passed={passed}, Failed={failed}");
        sb.AppendLine($"VERDICT: {(failed == 0 ? "CONFIRM_CORRECTNESS" : "HAS_FAILURES")}");
        sb.AppendLine("================================================================================");

        string report = sb.ToString();
        Debug.Log(report);
        try
        {
            File.WriteAllText(ReportPath, report);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[HealthBoxDropSystemTests] Không thể ghi file report: {ex.Message}");
        }
        return report;
    }
#endif
}
