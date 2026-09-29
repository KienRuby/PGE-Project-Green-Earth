#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RocketPunchVisualTests
{
    private const string PrefabPath = "Assets/Prefabs/Player.prefab";
    private const string GamePlayScenePath = "Assets/Scenes/GamePlay.unity";
    private const string GenMineScenePath = "Assets/Scenes/GenMine.unity";

    [Test]
    public void PlayerPrefab_RocketPunchConfiguration_IsValid()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceSynchronousImport);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }
        Assert.That(prefab, Is.Not.Null, "Player.prefab should exist.");

        RocketPunchSkill punch = prefab.GetComponent<RocketPunchSkill>();
        Assert.That(punch, Is.Not.Null, "Player.prefab should have RocketPunchSkill.");

        SerializedObject so = new SerializedObject(punch);
        SerializedProperty pivotProp = so.FindProperty("shoulderGunPivot");
        SerializedProperty muzzleProp = so.FindProperty("punchMuzzle");

        Debug.Log($"[Test] Prefab shoulderGunPivot: {pivotProp.objectReferenceValue}, punchMuzzle: {muzzleProp.objectReferenceValue}");
        Assert.That(pivotProp.objectReferenceValue, Is.Not.Null, "shoulderGunPivot should be assigned on Player.prefab");
        Assert.That(muzzleProp.objectReferenceValue, Is.Not.Null, "punchMuzzle should be assigned on Player.prefab");
    }

    [Test]
    public void GamePlayScene_RocketPunchConfiguration_IsValid()
    {
        CheckScene(GamePlayScenePath);
    }

    [Test]
    public void GenMineScene_RocketPunchConfiguration_IsValid()
    {
        CheckScene(GenMineScenePath);
    }

    [Test]
    public void RuntimeEnsureShoulderGun_SelfHealsAndActivates()
    {
        GameObject playerObj = new GameObject("Test_Player");
        GameObject bodyObj = new GameObject("thân");
        bodyObj.transform.SetParent(playerObj.transform);

        try
        {
            RocketPunchSkill punch = playerObj.AddComponent<RocketPunchSkill>();
            punch.EnsureShoulderGunReferences();

            SerializedObject so = new SerializedObject(punch);
            Transform pivot = so.FindProperty("shoulderGunPivot").objectReferenceValue as Transform;
            Transform muzzle = so.FindProperty("punchMuzzle").objectReferenceValue as Transform;

            Assert.That(pivot, Is.Not.Null, "shoulderGunPivot should be automatically created/wired by EnsureShoulderGunReferences");
            Assert.That(muzzle, Is.Not.Null, "punchMuzzle should be automatically created/wired by EnsureShoulderGunReferences");

            // Initially before unlock, it should be inactive
            Assert.That(pivot.gameObject.activeSelf, Is.False, "shoulderGunPivot should be inactive before unlock");

            // After unlock, it should be active
            punch.UnlockOrUpgrade(1);
            Assert.That(punch.IsUnlocked, Is.True);
            Assert.That(pivot.gameObject.activeSelf, Is.True, "shoulderGunPivot must be active after unlock");

            MethodInfo getMuzzleMethod = typeof(RocketPunchSkill).GetMethod("GetPunchMuzzle", BindingFlags.Instance | BindingFlags.NonPublic);
            Transform runtimeMuzzle = (Transform)getMuzzleMethod.Invoke(punch, null);
            Assert.That(runtimeMuzzle, Is.EqualTo(muzzle), "GetPunchMuzzle must return the shoulder launcher muzzle");
        }
        finally
        {
            Object.DestroyImmediate(playerObj);
        }
    }

    private void CheckScene(string scenePath)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        bool wasActive = string.Equals(activeScene.path, scenePath, System.StringComparison.OrdinalIgnoreCase);
        Scene scene = wasActive ? activeScene : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            PlayerChipsetSkillManager manager = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                manager = root.GetComponentInChildren<PlayerChipsetSkillManager>(true);
                if (manager != null) break;
            }

            Assert.That(manager, Is.Not.Null, $"PlayerChipsetSkillManager not found in {scenePath}");
            GameObject player = manager.gameObject;

            RocketPunchSkill punch = player.GetComponent<RocketPunchSkill>();
            Assert.That(punch, Is.Not.Null, $"RocketPunchSkill not found on Player in {scenePath}");

            punch.EnsureShoulderGunReferences();

            SerializedObject so = new SerializedObject(punch);
            SerializedProperty pivotProp = so.FindProperty("shoulderGunPivot");
            SerializedProperty muzzleProp = so.FindProperty("punchMuzzle");

            Debug.Log($"[Test] Scene {scenePath} shoulderGunPivot: {pivotProp.objectReferenceValue}, punchMuzzle: {muzzleProp.objectReferenceValue}");

            Assert.That(pivotProp.objectReferenceValue, Is.Not.Null, $"shoulderGunPivot is NULL in {scenePath}");
            Assert.That(muzzleProp.objectReferenceValue, Is.Not.Null, $"punchMuzzle is NULL in {scenePath}");

            Transform pivot = (Transform)pivotProp.objectReferenceValue;
            Transform visual = pivot.Find("GunPunchVisual");
            Assert.That(visual, Is.Not.Null, $"GunPunchVisual not found under shoulderGunPivot in {scenePath}");

            SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
            Assert.That(sr, Is.Not.Null, $"SpriteRenderer not found on GunPunchVisual in {scenePath}");
            Assert.That(sr.sprite, Is.Not.Null, $"Sprite on GunPunchVisual is null in {scenePath}");
            Assert.That(sr.sharedMaterial, Is.Not.Null, $"Material on GunPunchVisual is null in {scenePath}");

            Debug.Log($"[Test] Visual Sprite: {sr.sprite.name}, Material: {sr.sharedMaterial.name}, SortingLayer: {sr.sortingLayerName}, SortingOrder: {sr.sortingOrder}");
        }
        finally
        {
            if (!wasActive && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    [MenuItem("PGE/Tests/Run Rocket Punch Visual Tests")]
    public static void RunVisualTestsMenu()
    {
        AutoRunVisualTestsOnLoad();
    }

    [InitializeOnLoadMethod]
    private static void AutoRunVisualTestsOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            try
            {
                var t = new RocketPunchVisualTests();
                t.PlayerPrefab_RocketPunchConfiguration_IsValid();
                t.RuntimeEnsureShoulderGun_SelfHealsAndActivates();
                t.GamePlayScene_RocketPunchConfiguration_IsValid();
                t.GenMineScene_RocketPunchConfiguration_IsValid();

                string report = "[RocketPunchVisualTests] ALL 4 TESTS PASSED SUCCESSFULLY!\n" +
                                "1. PlayerPrefab_RocketPunchConfiguration_IsValid: PASSED\n" +
                                "2. RuntimeEnsureShoulderGun_SelfHealsAndActivates: PASSED\n" +
                                "3. GamePlayScene_RocketPunchConfiguration_IsValid: PASSED\n" +
                                "4. GenMineScene_RocketPunchConfiguration_IsValid: PASSED\n" +
                                $"Timestamp: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                System.IO.Directory.CreateDirectory("Reports");
                System.IO.File.WriteAllText("Reports/rocket_punch_visual_test_result.txt", report);
                Debug.Log("<color=green>[RocketPunchVisualTests] ALL 4 TESTS PASSED SUCCESSFULLY!</color>");
            }
            catch (System.Exception ex)
            {
                string err = $"[RocketPunchVisualTests] TEST FAILED: {ex.Message}\n{ex.StackTrace}";
                System.IO.Directory.CreateDirectory("Reports");
                System.IO.File.WriteAllText("Reports/rocket_punch_visual_test_result.txt", err);
                Debug.LogError(err);
            }
        };
    }
}
#endif
