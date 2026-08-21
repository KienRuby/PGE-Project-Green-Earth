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
}
