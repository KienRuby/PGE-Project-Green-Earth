using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates rock obstacles from the map's ground tiles.
/// Editor previews are removed before the scene is saved.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class GenMineObstacleDensity : MonoBehaviour
{
    [Header("Prop đất")]
    [SerializeField] private Sprite rockMoundSprite = null;
    [SerializeField] private Sprite rockGemSprite = null;

    [Header("Phân bố")]
    [SerializeField, Range(0f, 3f)]
    [InspectorName("Mật độ (vật cản / 100 đơn vị²)")]
    [Tooltip("Số chướng ngại trung bình trên mỗi 100 đơn vị vuông của bản đồ.")]
    private float densityPerHundredUnits = 1.5f;

    [SerializeField]
    [InspectorName("Seed bố cục")]
    [Tooltip("Cùng một seed và mật độ sẽ tạo lại cùng bố cục.")]
    private int seed = 0;

    [SerializeField, Range(0f, 4f)]
    [InspectorName("Khoảng cách tối thiểu")]
    private float minimumSpacing = 2f;

    [SerializeField, Range(0f, 10f)]
    [InspectorName("Lề bản đồ")]
    private float edgePadding = 1f;

    [SerializeField, Range(0f, 10f)]
    [InspectorName("Vùng trống ở giữa")]
    private float centerClearRadius = 2f;

    [SerializeField, Range(4, 64)]
    [InspectorName("Số đá sinh mỗi frame")]
    [Tooltip("Giới hạn số chướng ngại tạo hoặc cập nhật trong mỗi frame khi chơi.")]
    private int rocksPerFrame = 16;

    private float appliedDensity = float.NaN;
    private int appliedSeed;
    private float appliedSpacing;
    private float appliedPadding;
    private float appliedClearRadius;
    private Sprite appliedMoundSprite;
    private Sprite appliedGemSprite;
    private bool previewActive;
    private bool runtimeInitialized;
    private Coroutine spawnRoutine;
    private readonly List<SpriteRenderer> moundPool = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> gemPool = new List<SpriteRenderer>();

    private struct RockPlacement
    {
        public Vector2 Position;
        public bool Mound;
    }

#if UNITY_EDITOR
    private bool previewQueued;
#endif

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += OnSceneSaving;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += OnSceneSaved;
            SchedulePreview();
            return;
        }
#endif
        appliedDensity = float.NaN;
    }

    private void Start()
    {
        if (Application.isPlaying)
            Regenerate();
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        if (densityPerHundredUnits != appliedDensity || seed != appliedSeed ||
            minimumSpacing != appliedSpacing || edgePadding != appliedPadding ||
            centerClearRadius != appliedClearRadius ||
            rockMoundSprite != appliedMoundSprite || rockGemSprite != appliedGemSprite)
        {
            Regenerate();
        }
    }

    private void OnValidate()
    {
        densityPerHundredUnits = Mathf.Clamp(densityPerHundredUnits, 0f, 3f);
        minimumSpacing = Mathf.Clamp(minimumSpacing, 0f, 4f);
        edgePadding = Mathf.Clamp(edgePadding, 0f, 10f);
        centerClearRadius = Mathf.Clamp(centerClearRadius, 0f, 10f);
        rocksPerFrame = Mathf.Clamp(rocksPerFrame, 4, 64);
#if UNITY_EDITOR
        if (!Application.isPlaying)
            SchedulePreview();
#endif
    }

#if UNITY_EDITOR
    private void SchedulePreview()
    {
        if (previewQueued) return;
        previewQueued = true;
        UnityEditor.EditorApplication.delayCall += GeneratePreview;
    }

    private void GeneratePreview()
    {
        previewQueued = false;
        if (this != null && isActiveAndEnabled && !Application.isPlaying)
            Regenerate();
    }

    private void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
    {
        if (gameObject.scene == scene)
            ClearGenerated();
    }

    private void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
    {
        if (gameObject.scene == scene)
            SchedulePreview();
    }
#endif

    private void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= OnSceneSaving;
        UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= OnSceneSaved;
        if (previewQueued)
        {
            UnityEditor.EditorApplication.delayCall -= GeneratePreview;
            previewQueued = false;
        }
#endif
        if (previewActive || Application.isPlaying)
            ClearGenerated();
        moundPool.Clear();
        gemPool.Clear();
        runtimeInitialized = false;
    }

    public void SetDensity(float value)
    {
        densityPerHundredUnits = Mathf.Clamp(value, 0f, 3f);
        if (Application.isPlaying)
            Regenerate();
#if UNITY_EDITOR
        else
            SchedulePreview();
#endif
    }

    [ContextMenu("Regenerate Rock Obstacles")]
    public void Regenerate()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        if (!Application.isPlaying || !runtimeInitialized)
        {
            ClearGenerated();
            runtimeInitialized = Application.isPlaying;
        }
        RememberSettings();
        previewActive = !Application.isPlaying;

        if (rockMoundSprite == null && rockGemSprite == null)
        {
            DisableUnusedPools();
            Debug.LogWarning("GenMine: chưa gán sprite prop đất.", this);
            return;
        }

        if (!TryGetMapBounds(out Bounds mapBounds))
        {
            DisableUnusedPools();
            Debug.LogWarning("GenMine: không tìm thấy các SpriteRenderer nền trong Mine Map.", this);
            return;
        }

        float safePadding = Mathf.Clamp(edgePadding, 0f, 10f);
        float minX = mapBounds.min.x + safePadding;
        float maxX = mapBounds.max.x - safePadding;
        float minY = mapBounds.min.y + safePadding;
        float maxY = mapBounds.max.y - safePadding;
        if (minX >= maxX || minY >= maxY)
        {
            DisableUnusedPools();
            Debug.LogWarning("GenMine: lề bản đồ quá lớn so với vùng nền.", this);
            return;
        }

        float area = (maxX - minX) * (maxY - minY);
        int targetCount = Mathf.RoundToInt(area * Mathf.Clamp(densityPerHundredUnits, 0f, 3f) / 100f);
        if (targetCount == 0)
        {
            DisableUnusedPools();
            return;
        }

        var random = new System.Random(seed);
        var placements = new List<RockPlacement>(targetCount);
        Vector2 center = new Vector2(mapBounds.center.x, mapBounds.center.y);
        float safeSpacing = Mathf.Clamp(minimumSpacing, 0f, 4f);
        float safeClearRadius = Mathf.Clamp(centerClearRadius, 0f, 10f);
        float spacingSquared = safeSpacing * safeSpacing;
        float clearSquared = safeClearRadius * safeClearRadius;
        Dictionary<long, List<Vector2>> occupiedCells = safeSpacing > 0f
            ? new Dictionary<long, List<Vector2>>(targetCount)
            : null;
        int maxAttempts = Mathf.Max(100, targetCount * 40);
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        int enemyMask = LayerMask.GetMask("Enemy");

        for (int attempt = 0; attempt < maxAttempts && placements.Count < targetCount; attempt++)
        {
            var position = new Vector2(
                Mathf.Lerp(minX, maxX, (float)random.NextDouble()),
                Mathf.Lerp(minY, maxY, (float)random.NextDouble()));

            if ((position - center).sqrMagnitude < clearSquared) continue;
            if (occupiedCells != null && !IsFarEnough(position, occupiedCells, safeSpacing, spacingSquared)) continue;

            bool useMound = random.NextDouble() < 0.5;
            if (rockMoundSprite == null) useMound = false;
            if (rockGemSprite == null) useMound = true;
            if (occupiedCells != null)
                AddPosition(position, occupiedCells, safeSpacing);
            placements.Add(new RockPlacement { Position = position, Mound = useMound });
        }

        if (Application.isPlaying)
        {
            if (placements.Count == 0)
                DisableUnusedPools();
            else
                spawnRoutine = StartCoroutine(SpawnInBatches(placements, obstacleLayer, enemyMask));
        }
        else
        {
            for (int i = 0; i < placements.Count; i++)
                CreateRock(placements[i].Position, placements[i].Mound, i, obstacleLayer, enemyMask);
        }

        if (placements.Count < targetCount)
            Debug.LogWarning($"GenMine: chỉ đặt được {placements.Count}/{targetCount} chướng ngại. Hãy giảm mật độ hoặc khoảng cách tối thiểu.", this);
    }

    private IEnumerator SpawnInBatches(List<RockPlacement> placements, int obstacleLayer, int enemyMask)
    {
        yield return null;
        int moundCount = 0;
        int gemCount = 0;
        int batchSize = Mathf.Clamp(rocksPerFrame, 4, 64);

        for (int i = 0; i < placements.Count; i++)
        {
            RockPlacement placement = placements[i];
            List<SpriteRenderer> pool = placement.Mound ? moundPool : gemPool;
            int poolIndex = placement.Mound ? moundCount++ : gemCount++;
            SpriteRenderer renderer;
            if (poolIndex < pool.Count)
                renderer = pool[poolIndex];
            else
            {
                renderer = CreateRock(placement.Position, placement.Mound, poolIndex, obstacleLayer, enemyMask);
                pool.Add(renderer);
            }

            GameObject rock = renderer.gameObject;
            rock.transform.position = new Vector3(placement.Position.x, placement.Position.y, 0f);
            renderer.sprite = placement.Mound ? rockMoundSprite : rockGemSprite;
            rock.SetActive(true);

            if ((i + 1) % batchSize == 0)
                yield return null;
        }

        if (placements.Count % batchSize != 0)
            yield return null;

        DisableUnused(moundPool, moundCount);
        DisableUnused(gemPool, gemCount);
        spawnRoutine = null;
    }

    private void DisableUnusedPools()
    {
        if (!Application.isPlaying) return;
        DisableUnused(moundPool, 0);
        DisableUnused(gemPool, 0);
    }

    private static void DisableUnused(List<SpriteRenderer> pool, int usedCount)
    {
        for (int i = usedCount; i < pool.Count; i++)
            pool[i].gameObject.SetActive(false);
    }

    private bool TryGetMapBounds(out Bounds mapBounds)
    {
        mapBounds = default;
        Transform mapRoot = transform.parent;
        if (mapRoot == null) return false;

        bool found = false;
        for (int i = 0; i < mapRoot.childCount; i++)
        {
            Transform tile = mapRoot.GetChild(i);
            if (tile == transform) continue;

            SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null) continue;

            if (!found)
            {
                mapBounds = renderer.bounds;
                found = true;
            }
            else
            {
                mapBounds.Encapsulate(renderer.bounds);
            }
        }
        return found;
    }

    private static long CellKey(int x, int y)
    {
        return ((long)x << 32) | (uint)y;
    }

    private static bool IsFarEnough(Vector2 candidate, Dictionary<long, List<Vector2>> cells,
        float cellSize, float spacingSquared)
    {
        int cellX = Mathf.FloorToInt(candidate.x / cellSize);
        int cellY = Mathf.FloorToInt(candidate.y / cellSize);
        for (int x = cellX - 1; x <= cellX + 1; x++)
        {
            for (int y = cellY - 1; y <= cellY + 1; y++)
            {
                if (!cells.TryGetValue(CellKey(x, y), out List<Vector2> positions)) continue;
                for (int i = 0; i < positions.Count; i++)
                    if ((candidate - positions[i]).sqrMagnitude < spacingSquared)
                        return false;
            }
        }
        return true;
    }

    private static void AddPosition(Vector2 position, Dictionary<long, List<Vector2>> cells, float cellSize)
    {
        long key = CellKey(Mathf.FloorToInt(position.x / cellSize), Mathf.FloorToInt(position.y / cellSize));
        if (!cells.TryGetValue(key, out List<Vector2> positions))
        {
            positions = new List<Vector2>(2);
            cells.Add(key, positions);
        }
        positions.Add(position);
    }

    private SpriteRenderer CreateRock(Vector2 position, bool mound, int index, int obstacleLayer, int enemyMask)
    {
        var rock = new GameObject($"Generated Rock {(mound ? "Mound" : "Gem")} {index + 1:000}");
        rock.SetActive(false);
        rock.transform.SetParent(transform, false);
        rock.transform.position = new Vector3(position.x, position.y, 0f);
        rock.transform.localScale = mound ? new Vector3(0.14f, 0.14f, 1f) : new Vector3(0.22f, 0.22f, 1f);
        if (obstacleLayer >= 0) rock.layer = obstacleLayer;
        rock.tag = "Obstacle";

        SpriteRenderer renderer = rock.AddComponent<SpriteRenderer>();
        renderer.sprite = mound ? rockMoundSprite : rockGemSprite;
        renderer.sortingOrder = 10;

        if (mound)
        {
            BoxCollider2D collider = rock.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(9f, 2.4f);
            collider.offset = new Vector2(0f, -0.8f);
            collider.excludeLayers = enemyMask;
        }
        else
        {
            CircleCollider2D collider = rock.AddComponent<CircleCollider2D>();
            collider.radius = 1.25f;
            collider.offset = new Vector2(0f, -0.25f);
            collider.excludeLayers = enemyMask;
        }
        if (!Application.isPlaying)
            rock.SetActive(true);
        return renderer;
    }

    private void ClearGenerated()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (!child.name.StartsWith("Generated Rock ", StringComparison.Ordinal)) continue;
            child.SetActive(false);
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
        previewActive = false;
    }

    private void RememberSettings()
    {
        appliedDensity = densityPerHundredUnits;
        appliedSeed = seed;
        appliedSpacing = minimumSpacing;
        appliedPadding = edgePadding;
        appliedClearRadius = centerClearRadius;
        appliedMoundSprite = rockMoundSprite;
        appliedGemSprite = rockGemSprite;
    }
}
