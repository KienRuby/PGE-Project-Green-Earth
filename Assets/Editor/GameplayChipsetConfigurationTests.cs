#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayChipsetConfigurationTests
{
    private const string ScenePath = "Assets/Scenes/GamePlay.unity";

    [Test]
    public void GamePlayPlayer_AllChipsetSkillsHaveBuildSafeRuntimeConfiguration()
    {
        Scene existingScene = SceneManager.GetSceneByPath(ScenePath);
        bool openedForTest = !existingScene.IsValid() || !existingScene.isLoaded;
        Scene scene = openedForTest
            ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
            : existingScene;

        try
        {
            PlayerChipsetSkillManager manager = FindInScene<PlayerChipsetSkillManager>(scene);
            Assert.That(manager, Is.Not.Null, "GamePlay must contain a PlayerChipsetSkillManager.");

            GameObject player = manager.gameObject;
            PlayerAutoShooter shooter = player.GetComponent<PlayerAutoShooter>();
            Assert.That(shooter, Is.Not.Null, "The four gun chipsets require PlayerAutoShooter.");
            AssertObjectReferenceAssigned(shooter, "projectilePrefab", "PlayerAutoShooter projectilePrefab");

            HighExplosiveMineSkill mine = RequireComponent<HighExplosiveMineSkill>(player);
            EnergyJumperCablesSkill cables = RequireComponent<EnergyJumperCablesSkill>(player);
            SpikyDiscusSkill discus = RequireComponent<SpikyDiscusSkill>(player);
            GunTurretSkill turret = RequireComponent<GunTurretSkill>(player);
            RocketPunchSkill punch = RequireComponent<RocketPunchSkill>(player);
            SpinningBladeSkill blade = RequireComponent<SpinningBladeSkill>(player);

            AssertObjectReferenceAssigned(mine, "minePrefab", "High-Explosive Mine prefab");
            AssertObjectReferenceAssigned(discus, "spikyDiscusPrefab", "Spiky Discus prefab");
            AssertObjectReferenceAssigned(turret, "turretPrefab", "Gun Turret prefab");
            AssertObjectReferenceAssigned(punch, "rocketPunchPrefab", "Rocket Punch prefab");
            AssertObjectReferenceAssigned(blade, "spinningBladePrefab", "Spinning Blade prefab");

            SerializedObject managerData = new SerializedObject(manager);
            AssertManagerReference(managerData, "playerAutoShooter", shooter);
            AssertManagerReference(managerData, "highExplosiveMineSkill", mine);
            AssertManagerReference(managerData, "energyJumperCablesSkill", cables);
            AssertManagerReference(managerData, "spikyDiscusSkill", discus);
            AssertManagerReference(managerData, "gunTurretSkill", turret);
            AssertManagerReference(managerData, "rocketPunchSkill", punch);
            AssertManagerReference(managerData, "spinningBladeSkill", blade);
        }
        finally
        {
            if (openedForTest && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null) return component;
        }

        return null;
    }

    private static T RequireComponent<T>(GameObject owner) where T : Component
    {
        T component = owner.GetComponent<T>();
        Assert.That(component, Is.Not.Null, $"Player is missing {typeof(T).Name}; runtime AddComponent loses prefab configuration in builds.");
        return component;
    }

    private static void AssertObjectReferenceAssigned(Object component, string propertyName, string label)
    {
        SerializedProperty property = new SerializedObject(component).FindProperty(propertyName);
        Assert.That(property, Is.Not.Null, $"Serialized field '{propertyName}' was not found.");
        Assert.That(property.objectReferenceValue, Is.Not.Null, $"{label} must be assigned for player builds.");
    }

    private static void AssertManagerReference(SerializedObject managerData, string propertyName, Object expected)
    {
        SerializedProperty property = managerData.FindProperty(propertyName);
        Assert.That(property, Is.Not.Null, $"Manager field '{propertyName}' was not found.");
        Assert.That(property.objectReferenceValue, Is.EqualTo(expected), $"Manager field '{propertyName}' is not wired to the Player component.");
    }
}
#endif
