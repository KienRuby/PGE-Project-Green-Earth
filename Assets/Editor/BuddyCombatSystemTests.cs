using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

[TestFixture]
public class BuddyCombatSystemTests
{
    private const string PrefabDir = "Assets/Prefabs/Buddy";

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Đảm bảo các Prefab được sinh ra trước khi chạy test
        BuddyPrefabBuilder.BuildAllBuddyPrefabsAndSetupPlayer();
    }

    [Test]
    public void AllFiveBuddyPrefabs_ExistInAssets()
    {
        string[] requiredPrefabs = new string[]
        {
            $"{PrefabDir}/Buddy_Sloy.prefab",
            $"{PrefabDir}/Buddy_TurretBuffer.prefab",
            $"{PrefabDir}/Buddy_PurifyingDrone.prefab",
            $"{PrefabDir}/Buddy_RadarEye.prefab",
            $"{PrefabDir}/Buddy_AssaultBlaster.prefab"
        };

        foreach (var path in requiredPrefabs)
        {
            Assert.IsTrue(File.Exists(path), $"Prefab file must exist at {path}");
            GameObject loaded = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(loaded, $"Prefab at {path} must load as GameObject");
        }
    }

    [Test]
    public void AllFiveBuddyPrefabs_HaveCorrectSpritesAssigned()
    {
        var expectedSprites = new (string prefabName, string spriteName)[]
        {
            ("Buddy_Sloy", "drone-snowflake"),
            ("Buddy_TurretBuffer", "drone-spider"),
            ("Buddy_PurifyingDrone", "drone-stealth-wing"),
            ("Buddy_RadarEye", "drone-antenna-eye"),
            ("Buddy_AssaultBlaster", "drone-cross-visor")
        };

        foreach (var (prefabName, spriteName) in expectedSprites)
        {
            string path = $"{PrefabDir}/{prefabName}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, $"Prefab {prefabName} could not be loaded.");

            SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(sr, $"Prefab {prefabName} must contain a SpriteRenderer.");
            Assert.IsNotNull(sr.sprite, $"Prefab {prefabName} SpriteRenderer must have a sprite assigned.");
            Assert.AreEqual(spriteName, sr.sprite.name, $"Prefab {prefabName} must use sprite {spriteName}.");
        }
    }

    [Test]
    public void AllFiveBuddyPrefabs_HaveSpecializedCombatComponents()
    {
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Buddy_Sloy.prefab").GetComponent<SloyFrostBuddy>());
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Buddy_TurretBuffer.prefab").GetComponent<TurretBufferBuddy>());
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Buddy_PurifyingDrone.prefab").GetComponent<PurifyingBuddy>());
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Buddy_RadarEye.prefab").GetComponent<RadarEyeBuddy>());
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Buddy_AssaultBlaster.prefab").GetComponent<AssaultBlasterBuddy>());
    }

    [Test]
    public void BuddyCombatManager_SpawnsAtMostThreeEquippedDrones()
    {
        GameObject playerObj = new GameObject("Player_Test");
        try
        {
            // Thiết lập deck test truyền 5 Buddy ID: 1, 2, 10, 3, 4 (vượt quá 3)
            int[] testDeck = new int[] { 1, 2, 10, 3, 4 };
            PlayerDataService.ActiveBuddyDeckIndex = 0;
            PlayerDataService.SaveBuddyDeck(0, testDeck);

            BuddyCombatManager manager = playerObj.AddComponent<BuddyCombatManager>();
            manager.EnsureRegisteredPrefabsLoaded();
            manager.SpawnEquippedBuddies();

            Assert.AreEqual(BuddyCombatManager.MaxCombatDrones, manager.ActiveDrones.Count, "Manager must spawn at most 3 active drones.");

            // Kiểm tra các ID tương ứng (chỉ 3 drone đầu tiên được đưa vào trận)
            int[] spawnedIds = manager.ActiveDrones.Select(d => d.BuddyId).ToArray();
            CollectionAssert.AreEqual(new int[] { 1, 2, 10 }, spawnedIds, "Spawned buddy IDs must match first 3 equipped IDs.");
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
        }
    }

    [Test]
    public void BuddyCombatManager_FiltersOutUnassignedSlots()
    {
        GameObject playerObj = new GameObject("Player_Test2");
        try
        {
            // Thiết lập deck test chỉ có 2 slot có Buddy (ID 1 và 4), các slot khác -1
            int[] testDeck = new int[] { 1, -1, 4, -1, -1 };
            PlayerDataService.ActiveBuddyDeckIndex = 1;
            PlayerDataService.SaveBuddyDeck(1, testDeck);

            BuddyCombatManager manager = playerObj.AddComponent<BuddyCombatManager>();
            manager.EnsureRegisteredPrefabsLoaded();
            manager.SpawnEquippedBuddies();

            Assert.AreEqual(2, manager.ActiveDrones.Count, "Manager must only spawn 2 drones for non-empty slots.");
            Assert.AreEqual(1, manager.ActiveDrones[0].BuddyId);
            Assert.AreEqual(4, manager.ActiveDrones[1].BuddyId);
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
        }
    }

    [Test]
    public void AllBuddyDrones_AttackRange_IsThreeMetersSmallerThanPlayerAttackRange()
    {
        GameObject player = new GameObject("Player_BuddyRangeTest");
        GameObject droneObj = new GameObject("Drone_Test");
        try
        {
            PlayerAutoShooter shooter = player.AddComponent<PlayerAutoShooter>();
            BuddyCombatDrone drone = droneObj.AddComponent<AssaultBlasterBuddy>();

            // Trước khi Initialize với player: dùng fallback targetDetectionRadius = 9m
            Assert.AreEqual(9.0f, drone.EffectiveAttackRange, 0.001f);

            // Sau khi Initialize với player: đọc từ PlayerAutoShooter (12m - 3m = 9m)
            drone.Initialize(player.transform, 0, 1, 1, BuddyTier.Common);
            Assert.AreEqual(9.0f, drone.EffectiveAttackRange, 0.001f);

            // Khi Player được tăng tầm bắn (+3m -> 15m), tầm bắn của Buddy tăng tương ứng lên 12m
            shooter.BonusAttackRange = 3.0f;
            Assert.AreEqual(15.0f, shooter.SharedAttackRange, 0.001f);
            Assert.AreEqual(12.0f, drone.EffectiveAttackRange, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(droneObj);
            Object.DestroyImmediate(player);
        }
    }
}
