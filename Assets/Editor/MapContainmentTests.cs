using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class MapContainmentTests
{
    [Test]
    public void AllChapters_FloorCameraAndPropCorridorMatchBounds()
    {
        bool openedAdditively = false;
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/GamePlay.unity");
        if (!scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity", OpenSceneMode.Additive);
            openedAdditively = true;
        }
        string key = PlayerDataService.SelectedChapterIndexKey;
        bool hadKey = PlayerPrefs.HasKey(key);
        int selected = PlayerPrefs.GetInt(key);
        GameObject cameraObject = new GameObject("BoundaryTestCamera", typeof(Camera));
        cameraObject.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            var db = AssetDatabase.LoadAssetAtPath<ChapterDatabase>("Assets/Data/Chapters/ChapterDatabase.asset");
            var resourceDb = Resources.Load<ChapterDatabase>("ChapterDatabase");
            Assert.AreEqual(10, db.Count);
            CollectionAssert.AreEqual(db.Chapters, resourceDb.Chapters);
            var manager = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ChapterMapManager>(true)).Single();
            manager.InitializeMap();
            manager.SetDatabaseForTesting(db);
            var boundary = manager.GetComponent<MapBoundary>();
            typeof(MapBoundary).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
            Assert.AreSame(boundary, MapBoundary.Instance, "Recover after a script reload without Awake.");
            var floor = manager.GetComponent<SpriteRenderer>();
            var spawner = manager.GetComponent<DesertPropSpawner>();
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            for (int chapter = 0; chapter < db.Count; chapter++)
            {
                PlayerPrefs.SetInt(key, chapter);
                manager.ApplyCurrentChapterMap();
                Assert.AreEqual(new Vector2(40, 40), boundary.MapSize, "Chapter " + (chapter + 1));
                Assert.That(floor.bounds.size.x, Is.EqualTo(40).Within(0.001));
                Assert.That(floor.bounds.size.y, Is.EqualTo(40).Within(0.001));
                Assert.That(((Vector2)floor.bounds.center).magnitude, Is.LessThan(0.001));
                Assert.That(boundary.PlayerPadding, Is.EqualTo(0.6f).Within(0.001));
                Assert.AreEqual(new Vector2(19.4f, -19.4f), boundary.ClampPlayerPosition(new Vector2(100, -100)));
                foreach (float aspect in new[] { 9f / 16f, 9f / 20f, 4f / 3f, 16f / 9f, 3f })
                {
                    camera.aspect = aspect;
                    camera.orthographicSize = 25;
                    foreach (var corner in new[] { new Vector2(-100,-100), new Vector2(100,-100), new Vector2(-100,100), new Vector2(100,100) })
                    {
                        Vector2 center = boundary.ClampCameraPosition(corner, camera);
                        Vector2 half = new Vector2(camera.orthographicSize * aspect, camera.orthographicSize);
                        Assert.IsTrue(boundary.IsInsideMap(center - half, -0.001f));
                        Assert.IsTrue(boundary.IsInsideMap(center + half, -0.001f));
                    }
                }
                spawner.GeneratePreview();
                Transform props = manager.transform.Find("Generated Desert Props");
                Assert.Greater(props.childCount, 0);
                foreach (var renderer in props.GetComponentsInChildren<Renderer>())
                {
                    Assert.IsTrue(boundary.IsInsideMap(renderer.bounds.min, 0.999f), renderer.name);
                    Assert.IsTrue(boundary.IsInsideMap(renderer.bounds.max, 0.999f), renderer.name);
                }
                foreach (var collider in props.GetComponentsInChildren<Collider2D>().Where(c => c.enabled))
                {
                    Assert.IsTrue(boundary.IsInsideMap(collider.bounds.min, 0.999f), collider.name);
                    Assert.IsTrue(boundary.IsInsideMap(collider.bounds.max, 0.999f), collider.name);
                }
            }
        }
        finally
        {
            if (hadKey) PlayerPrefs.SetInt(key, selected); else PlayerPrefs.DeleteKey(key);
            Object.DestroyImmediate(cameraObject);
            if (openedAdditively)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    [Test]
    public void Player_ExternalPushClampsAndPreservesWallSliding()
    {
        var map = new GameObject("BoundaryTestMap");
        var player = new GameObject("BoundaryTestPlayer");
        map.hideFlags = player.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            var boundary = map.AddComponent<MapBoundary>();
            Invoke(boundary, "Awake");
            boundary.SetupBounds(Vector2.zero, new Vector2(40, 40), 0.6f);
            var movement = player.AddComponent<PlayerMovement>();
            Invoke(movement, "Awake");
            var body = player.GetComponent<Rigidbody2D>();
            foreach (float sign in new[] { -1f, 1f })
            {
                body.position = new Vector2(sign * 30, 0);
                body.velocity = new Vector2(sign * 20, 3);
                Invoke(movement, "LateUpdate");
                Assert.That(body.position.x, Is.EqualTo(sign * 19.4f).Within(0.001));
                Assert.AreEqual(new Vector2(0, 3), body.velocity, "Keep tangent velocity at vertical wall.");
                body.position = new Vector2(0, sign * 30);
                body.velocity = new Vector2(3, sign * 20);
                Invoke(movement, "LateUpdate");
                Assert.AreEqual(new Vector2(3, 0), body.velocity, "Keep tangent velocity at horizontal wall.");
                body.position = new Vector2(sign * 30, sign * 30);
                body.velocity = Vector2.one * sign * 50;
                Invoke(movement, "LateUpdate");
                Assert.AreEqual(Vector2.one * sign * 19.4f, body.position);
                Assert.AreEqual(Vector2.zero, body.velocity);
                body.velocity = Vector2.one * -sign * 3;
                Invoke(movement, "LateUpdate");
                Assert.AreEqual(Vector2.one * -sign * 3, body.velocity, "Moving back into the map must remain possible.");
            }
        }
        finally { Object.DestroyImmediate(player); Object.DestroyImmediate(map); }
    }

    private static void Invoke(object target, string method)
    {
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }
}
