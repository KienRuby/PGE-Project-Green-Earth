using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class DesertPropSpawnerTests
{
    private const string PrefabFolder = "Assets/Prefabs/Map sa mac";

    [Test]
    public void CalculateSpawnCount_UsesDensityPerHundredMapUnits()
    {
        Assert.That(DesertPropSpawner.CalculateSpawnCount(1600f, 1.5f), Is.EqualTo(24));
        Assert.That(DesertPropSpawner.CalculateSpawnCount(1600f, 3f), Is.EqualTo(48));
        Assert.That(DesertPropSpawner.CalculateSpawnCount(-1f, 5f), Is.Zero);
    }

    [Test]
    public void Decoration_CanNeverBlockPlayer()
    {
        Assert.That(DesertPropSpawner.ShouldBlockPlayer(DesertPropSpawner.PropKind.Decoration, true), Is.False);
        Assert.That(DesertPropSpawner.ShouldBlockPlayer(DesertPropSpawner.PropKind.Obstacle, true), Is.True);
        Assert.That(DesertPropSpawner.ShouldBlockPlayer(DesertPropSpawner.PropKind.Obstacle, false), Is.False);
    }

    [Test]
    public void IsFarEnough_RejectsPositionsInsideMinimumSpacing()
    {
        List<Vector2> existing = new List<Vector2> { Vector2.zero, new Vector2(5f, 5f) };

        Assert.That(DesertPropSpawner.IsFarEnough(new Vector2(0.5f, 0f), existing, 1f), Is.False);
        Assert.That(DesertPropSpawner.IsFarEnough(new Vector2(2f, 0f), existing, 1f), Is.True);
    }

    [Test]
    public void DesertPrefabFolder_ContainsObstaclesAndFiveDecorations()
    {
        string[] allPrefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
        int decorationCount = 0;
        int obstacleCount = 0;

        foreach (string guid in allPrefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, $"Không thể tải prefab: {path}");
            Assert.That(prefab.GetComponentInChildren<SpriteRenderer>(), Is.Not.Null, $"Prefab thiếu SpriteRenderer: {path}");

            if (prefab.name.ToLowerInvariant().Contains("hoa tiet"))
            {
                decorationCount++;
            }
            else
            {
                obstacleCount++;
            }
        }

        Assert.That(decorationCount, Is.EqualTo(5));
        Assert.That(obstacleCount, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void Map2_MutantForestPrefabFolder_ContainsSixPrefabsWithSpriteRenderers()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Map 2 - Mutant Forest" });
        Assert.That(guids.Length, Is.EqualTo(6), "Map 2 phải có đủ 6 prefab.");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, $"Không thể tải prefab: {path}");
            Assert.That(prefab.GetComponentInChildren<SpriteRenderer>(), Is.Not.Null, $"Prefab thiếu SpriteRenderer: {path}");
        }
    }

    [Test]
    public void Map3_ToxicSwampPrefabFolder_ContainsSevenPrefabsWithSpriteRenderers()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Map 3 - Toxic Swamp" });
        Assert.That(guids.Length, Is.EqualTo(7), "Map 3 phải có đủ 7 prefab.");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, $"Không thể tải prefab: {path}");
            Assert.That(prefab.GetComponentInChildren<SpriteRenderer>(), Is.Not.Null, $"Prefab thiếu SpriteRenderer: {path}");
        }
    }

    [Test]
    public void ConfigureCollision_WhenEmptyPolygonColliderExists_ReplacesWithValidBoxCollider()
    {
        GameObject spawnerGo = new GameObject("TestSpawner", typeof(MapBoundary), typeof(DesertPropSpawner));
        GameObject propGo = new GameObject("TestObstacle");
        try
        {
            SpriteRenderer sr = propGo.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(32, 32);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f);

            PolygonCollider2D emptyPoly = propGo.AddComponent<PolygonCollider2D>();
            emptyPoly.pathCount = 0;

            DesertPropSpawner spawner = spawnerGo.GetComponent<DesertPropSpawner>();
            var method = typeof(DesertPropSpawner).GetMethod("ConfigureCollision", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, "ConfigureCollision method must exist.");

            method.Invoke(spawner, new object[] { propGo, true, 0.55f, 0.2f });

            PolygonCollider2D polyAfter = propGo.GetComponent<PolygonCollider2D>();
            Assert.That(polyAfter, Is.Null, "Empty PolygonCollider2D must be removed.");

            BoxCollider2D boxAfter = propGo.GetComponent<BoxCollider2D>();
            Assert.That(boxAfter, Is.Not.Null, "BoxCollider2D must be created.");
            Assert.That(boxAfter.enabled, Is.True, "BoxCollider2D must be enabled.");
            Assert.That(boxAfter.isTrigger, Is.False, "BoxCollider2D must be a solid collider.");
            Assert.That(boxAfter.size.x, Is.GreaterThan(0f));
            Assert.That(boxAfter.size.y, Is.GreaterThan(0f));

            int obstacleLayer = LayerMask.NameToLayer("Obstacle");
            if (obstacleLayer != -1)
            {
                Assert.That(propGo.layer, Is.EqualTo(obstacleLayer), "Obstacle layer must be applied.");
            }
        }
        finally
        {
            Object.DestroyImmediate(propGo);
            Object.DestroyImmediate(spawnerGo);
        }
    }

    [Test]
    public void CayBupCamPrefab_HasCustomBoxColliderConfiguredForTrunk()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map 3 - Toxic Swamp/cay_bup_cam.prefab");
        Assert.That(prefab, Is.Not.Null, "cay_bup_cam.prefab must exist.");

        BoxCollider2D box = prefab.GetComponent<BoxCollider2D>();
        Assert.That(box, Is.Not.Null, "cay_bup_cam prefab must have a BoxCollider2D on it.");
        Assert.That(box.size.x, Is.InRange(0.7f, 0.85f), "BoxCollider2D width must fit the trunk.");
        Assert.That(box.size.y, Is.InRange(1.45f, 1.8f), "BoxCollider2D height must cover the trunk.");
        Assert.That(box.offset.y, Is.InRange(-1.8f, -1.4f), "BoxCollider2D offset must be centered on the lower trunk.");
        Assert.That(box.isTrigger, Is.False, "BoxCollider2D must be solid.");
    }

    [Test]
    public void TangDaPrefab_HasCustomBoxColliderConfiguredForRock()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map 3 - Toxic Swamp/tang_da.prefab");
        Assert.That(prefab, Is.Not.Null, "tang_da.prefab must exist.");

        BoxCollider2D box = prefab.GetComponent<BoxCollider2D>();
        Assert.That(box, Is.Not.Null, "tang_da prefab must have a BoxCollider2D on it.");
        Assert.That(box.size.x, Is.InRange(1.6f, 1.9f), "BoxCollider2D width must cover the rock.");
        Assert.That(box.size.y, Is.InRange(1.1f, 1.4f), "BoxCollider2D height must cover the rock.");
        Assert.That(box.offset.x, Is.InRange(0.6f, 0.9f), "BoxCollider2D offset X must shift to the rock on the right.");
        Assert.That(box.offset.y, Is.InRange(-0.4f, -0.15f), "BoxCollider2D offset Y must center on the rock.");
        Assert.That(box.isTrigger, Is.False, "BoxCollider2D must be solid.");
    }

    [Test]
    public void NamBachTuocPrefab_HasCustomBoxColliderConfiguredForTrunk()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map 3 - Toxic Swamp/nam_bach_tuoc.prefab");
        Assert.That(prefab, Is.Not.Null, "nam_bach_tuoc.prefab must exist.");

        BoxCollider2D box = prefab.GetComponent<BoxCollider2D>();
        Assert.That(box, Is.Not.Null, "nam_bach_tuoc prefab must have a BoxCollider2D on it.");
        Assert.That(box.size.x, Is.InRange(1.8f, 2.1f), "BoxCollider2D width must fit the trunk and side mushrooms.");
        Assert.That(box.size.y, Is.InRange(1.2f, 1.5f), "BoxCollider2D height must cover the lower trunk.");
        Assert.That(box.offset.y, Is.InRange(-1.3f, -0.9f), "BoxCollider2D offset must be centered on the lower trunk.");
        Assert.That(box.isTrigger, Is.False, "BoxCollider2D must be solid.");
    }

    [Test]
    public void ConfigureSorting_WhenYIsHigh_NeverDropsBelowGroundOrDecoration()
    {
        GameObject spawnerGo = new GameObject("TestSpawner", typeof(MapBoundary), typeof(DesertPropSpawner));
        GameObject propGo = new GameObject("TestObstacle");
        try
        {
            propGo.AddComponent<SpriteRenderer>();
            DesertPropSpawner spawner = spawnerGo.GetComponent<DesertPropSpawner>();
            var method = typeof(DesertPropSpawner).GetMethod("ConfigureSorting", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, "ConfigureSorting method must exist.");

            // With y = 25f (near top edge of 40x40 map)
            method.Invoke(spawner, new object[] { propGo, DesertPropSpawner.PropKind.Obstacle, 25f });

            SpriteRenderer sr = propGo.GetComponent<SpriteRenderer>();
            Assert.That(sr.sortingOrder, Is.GreaterThan(-90), "Obstacle sorting order must stay above decorations (-90) and ground (-100).");
        }
        finally
        {
            Object.DestroyImmediate(propGo);
            Object.DestroyImmediate(spawnerGo);
        }
    }

    [Test]
    public void ConfigureCollision_WhenPrefabAlreadyHasValidBoxCollider_PreservesIt()
    {
        GameObject spawnerGo = new GameObject("TestSpawner", typeof(MapBoundary), typeof(DesertPropSpawner));
        GameObject propGo = new GameObject("TestObstacle");
        try
        {
            BoxCollider2D customBox = propGo.AddComponent<BoxCollider2D>();
            customBox.size = new Vector2(0.76f, 1.64f);
            customBox.offset = new Vector2(-0.09f, -1.635f);

            DesertPropSpawner spawner = spawnerGo.GetComponent<DesertPropSpawner>();
            var method = typeof(DesertPropSpawner).GetMethod("ConfigureCollision", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);

            method.Invoke(spawner, new object[] { propGo, true, 0.55f, 0.2f });

            BoxCollider2D boxAfter = propGo.GetComponent<BoxCollider2D>();
            Assert.That(boxAfter, Is.SameAs(customBox), "Custom BoxCollider2D must be preserved.");
            Assert.That(boxAfter.size.x, Is.EqualTo(0.76f).Within(0.001f));
            Assert.That(boxAfter.size.y, Is.EqualTo(1.64f).Within(0.001f));
            Assert.That(boxAfter.offset.x, Is.EqualTo(-0.09f).Within(0.001f));
            Assert.That(boxAfter.offset.y, Is.EqualTo(-1.635f).Within(0.001f));
            Assert.That(boxAfter.enabled, Is.True);
            Assert.That(boxAfter.isTrigger, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(propGo);
            Object.DestroyImmediate(spawnerGo);
        }
    }

    [Test]
    public void Map2And3_DefaultProps_ContainZeroObstacles()
    {
        GameObject spawnerGo = new GameObject("TestSpawner", typeof(MapBoundary), typeof(DesertPropSpawner));
        try
        {
            DesertPropSpawner spawner = spawnerGo.GetComponent<DesertPropSpawner>();
            spawner.EnsureDefaultChapterConfigs();

            List<DesertPropSpawner.PropEntry> ch2Props = spawner.GetCurrentPropsList(2);
            Assert.That(ch2Props, Is.Not.Null);
            Assert.That(ch2Props.Count, Is.GreaterThan(0));
            Assert.That(ch2Props.Exists(p => p.kind == DesertPropSpawner.PropKind.Obstacle), Is.False, "Chapter 2 không được chứa bất kỳ chướng ngại vật nào.");

            List<DesertPropSpawner.PropEntry> ch3Props = spawner.GetCurrentPropsList(3);
            Assert.That(ch3Props, Is.Not.Null);
            Assert.That(ch3Props.Count, Is.GreaterThan(0));
            Assert.That(ch3Props.Exists(p => p.kind == DesertPropSpawner.PropKind.Obstacle), Is.False, "Chapter 3 không được chứa bất kỳ chướng ngại vật nào.");
        }
        finally
        {
            Object.DestroyImmediate(spawnerGo);
        }
    }

    [Test]
    public void Map2And3_ChapterData_ObstacleDensityIsZero()
    {
        ChapterData c2 = AssetDatabase.LoadAssetAtPath<ChapterData>("Assets/Data/Chapters/Chapter_02_MutantForest.asset");
        Assert.That(c2, Is.Not.Null);
        Assert.That(c2.obstacleDensity, Is.EqualTo(0f), "Chapter 2 obstacleDensity phải bằng 0.");

        ChapterData c3 = AssetDatabase.LoadAssetAtPath<ChapterData>("Assets/Data/Chapters/Chapter_03_ToxicSwamp.asset");
        Assert.That(c3, Is.Not.Null);
        Assert.That(c3.obstacleDensity, Is.EqualTo(0f), "Chapter 3 obstacleDensity phải bằng 0.");
    }

    [Test]
    public void Chapter2And3_GroundScale_IsZeroPointTwo_AndCoversFullMap()
    {
        ChapterData c2 = AssetDatabase.LoadAssetAtPath<ChapterData>("Assets/Data/Chapters/Chapter_02_MutantForest.asset");
        Assert.That(c2, Is.Not.Null);
        Assert.That(c2.GetEffectiveGroundScale(), Is.EqualTo(0.2f).Within(0.001f));

        ChapterData c3 = AssetDatabase.LoadAssetAtPath<ChapterData>("Assets/Data/Chapters/Chapter_03_ToxicSwamp.asset");
        Assert.That(c3, Is.Not.Null);
        Assert.That(c3.GetEffectiveGroundScale(), Is.EqualTo(0.2f).Within(0.001f));

        GameObject floorGo = new GameObject("FloorTest", typeof(SpriteRenderer), typeof(MapBoundary), typeof(ChapterMapManager));
        try
        {
            ChapterMapManager manager = floorGo.GetComponent<ChapterMapManager>();
            SpriteRenderer sr = floorGo.GetComponent<SpriteRenderer>();
            manager.InitializeMap();

            System.Reflection.MethodInfo applyMethod = typeof(ChapterMapManager).GetMethod("ApplyChapterConfig", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            applyMethod.Invoke(manager, new object[] { c2 });
            Assert.That(floorGo.transform.localScale.x, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(floorGo.transform.localScale.y, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(sr.size.x, Is.EqualTo(200f).Within(0.001f));
            Assert.That(sr.size.y, Is.EqualTo(200f).Within(0.001f));
            Assert.That(sr.bounds.size.x, Is.EqualTo(40f).Within(0.001f));
            Assert.That(sr.bounds.size.y, Is.EqualTo(40f).Within(0.001f));

            applyMethod.Invoke(manager, new object[] { c3 });
            Assert.That(floorGo.transform.localScale.x, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(floorGo.transform.localScale.y, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(sr.size.x, Is.EqualTo(200f).Within(0.001f));
            Assert.That(sr.size.y, Is.EqualTo(200f).Within(0.001f));
            Assert.That(sr.bounds.size.x, Is.EqualTo(40f).Within(0.001f));
            Assert.That(sr.bounds.size.y, Is.EqualTo(40f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(floorGo);
        }
    }
}

