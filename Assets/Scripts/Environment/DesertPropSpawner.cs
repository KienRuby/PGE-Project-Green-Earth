using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sinh chướng ngại và họa tiết từ prefab trong phạm vi MapBoundary.
/// Mật độ được tính theo số vật trên 100 đơn vị vuông để không phụ thuộc kích thước map.
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(MapBoundary))]
public class DesertPropSpawner : MonoBehaviour
{
    public enum PropKind
    {
        Obstacle,
        Decoration
    }

    [Serializable]
    public sealed class PropEntry
    {
        [Tooltip("Prefab sẽ được chọn ngẫu nhiên.")]
        public GameObject prefab;

        [Tooltip("Họa tiết luôn cho phép Player đi xuyên, bất kể Block Player.")]
        public PropKind kind = PropKind.Obstacle;

        [Tooltip("Bật nếu prefab này phải chặn Player.")]
        public bool blockPlayer = true;

        [Min(0.01f), Tooltip("Trọng số xuất hiện tương đối so với các prefab cùng loại.")]
        public float weight = 1f;

        [Tooltip("Khoảng nhân thêm vào scale gốc của prefab.")]
        public Vector2 scaleMultiplierRange = new Vector2(0.9f, 1.1f);
    }

    [SerializeField, Min(1)] private int desertChapterNumber = 1;
    [SerializeField] private bool spawnOnlyInDesertChapter = true;

    [SerializeField] private List<PropEntry> props = new List<PropEntry>();

    [SerializeField, Min(0f)] private float obstacleDensity = 1.5f;
    [SerializeField, Min(0f)] private float decorationDensity = 3f;

    [SerializeField, Min(0f)] private float mapEdgePadding = 1f;
    [SerializeField, Min(0f)] private float obstacleMinSpacing = 1.25f;
    [SerializeField, Min(0f)] private float decorationMinSpacing = 0.75f;
    [SerializeField, Min(0f)] private float playerStartClearRadius = 2.5f;
    [SerializeField, Min(1)] private int attemptsPerProp = 20;

    [SerializeField, Range(0.05f, 1f)] private float colliderWidthRatio = 0.55f;
    [SerializeField, Range(0.05f, 0.5f)] private float colliderHeightRatio = 0.2f;

    [SerializeField] private bool useRandomSeed;
    [SerializeField] private int randomSeed = 12345;
    [SerializeField] private int obstacleSortingBase;
    [SerializeField] private int decorationSortingOrder = -90;

    private readonly List<Vector2> obstaclePositions = new List<Vector2>();
    private readonly List<Vector2> decorationPositions = new List<Vector2>();
    private MapBoundary mapBoundary;
    private ChapterMapManager chapterMapManager;
    private Transform generatedRoot;
    private System.Random random;

    public IReadOnlyList<PropEntry> Props => props;
    public float ObstacleDensity => obstacleDensity;
    public float DecorationDensity => decorationDensity;

    private void Start()
    {
        Generate();
    }

    [ContextMenu("Regenerate Desert Props")]
    public void Generate()
    {
        GenerateInternal(false, false);
    }

    /// <summary>
    /// Tạo bố cục xem trước ngay trong Scene View, không cần vào Play Mode.
    /// Preview không được lưu vào scene và không phụ thuộc chapter đang chạy.
    /// </summary>
    public void GeneratePreview()
    {
        if (Application.isPlaying)
        {
            Generate();
            return;
        }

        GenerateInternal(true, true);
    }

    private void GenerateInternal(bool ignoreChapterFilter, bool isEditorPreview)
    {
        mapBoundary = GetComponent<MapBoundary>();
        chapterMapManager = GetComponent<ChapterMapManager>();

        ClearGenerated();
        if ((!ignoreChapterFilter && !ShouldSpawnForCurrentChapter()) || mapBoundary == null)
        {
            return;
        }

        random = useRandomSeed
            ? new System.Random(randomSeed)
            : new System.Random(unchecked(Environment.TickCount * 397 ^ GetInstanceID()));

        generatedRoot = new GameObject("Generated Desert Props").transform;
        generatedRoot.SetParent(transform, false);
        generatedRoot.localPosition = new Vector3(0f, 0f, -10f);
        if (isEditorPreview)
        {
            generatedRoot.gameObject.hideFlags = HideFlags.DontSaveInEditor;
        }

        float area = mapBoundary.MapSize.x * mapBoundary.MapSize.y;
        SpawnKind(PropKind.Decoration, CalculateSpawnCount(area, decorationDensity), decorationMinSpacing);
        SpawnKind(PropKind.Obstacle, CalculateSpawnCount(area, obstacleDensity), obstacleMinSpacing);
    }

    [ContextMenu("Clear Generated Desert Props")]
    public void ClearGenerated()
    {
        obstaclePositions.Clear();
        decorationPositions.Clear();

        Transform oldRoot = transform.Find("Generated Desert Props");
        if (oldRoot == null)
        {
            generatedRoot = null;
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(oldRoot.gameObject);
        }
        else
        {
            DestroyImmediate(oldRoot.gameObject);
        }

        generatedRoot = null;
    }

    private bool ShouldSpawnForCurrentChapter()
    {
        if (!spawnOnlyInDesertChapter)
        {
            return true;
        }

        ChapterData chapter = chapterMapManager != null ? chapterMapManager.ActiveChapterData : null;
        return chapter != null && chapter.chapterNumber == desertChapterNumber;
    }

    private void SpawnKind(PropKind kind, int targetCount, float minSpacing)
    {
        List<PropEntry> candidates = GetCandidates(kind);
        if (candidates.Count == 0 || targetCount <= 0)
        {
            return;
        }

        List<Vector2> positions = kind == PropKind.Obstacle ? obstaclePositions : decorationPositions;
        int maxAttempts = Mathf.Max(targetCount, targetCount * attemptsPerProp);

        for (int attempt = 0; attempt < maxAttempts && positions.Count < targetCount; attempt++)
        {
            Vector2 position = RandomMapPosition();
            if (Vector2.Distance(position, GetPlayerStartPosition()) < playerStartClearRadius ||
                !IsFarEnough(position, positions, minSpacing))
            {
                continue;
            }

            PropEntry entry = PickWeighted(candidates);
            if (entry == null || entry.prefab == null)
            {
                continue;
            }

            CreateProp(entry, position);
            positions.Add(position);
        }
    }

    private List<PropEntry> GetCandidates(PropKind kind)
    {
        List<PropEntry> result = new List<PropEntry>();
        for (int i = 0; i < props.Count; i++)
        {
            PropEntry entry = props[i];
            if (entry != null && entry.prefab != null && entry.kind == kind && entry.weight > 0f)
            {
                result.Add(entry);
            }
        }

        return result;
    }

    private PropEntry PickWeighted(List<PropEntry> candidates)
    {
        float totalWeight = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            totalWeight += candidates[i].weight;
        }

        double roll = random.NextDouble() * totalWeight;
        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= candidates[i].weight;
            if (roll <= 0d)
            {
                return candidates[i];
            }
        }

        return candidates[candidates.Count - 1];
    }

    private Vector2 RandomMapPosition()
    {
        Vector2 min = mapBoundary.MinBounds + Vector2.one * mapEdgePadding;
        Vector2 max = mapBoundary.MaxBounds - Vector2.one * mapEdgePadding;

        if (min.x > max.x) min.x = max.x = mapBoundary.MapCenter.x;
        if (min.y > max.y) min.y = max.y = mapBoundary.MapCenter.y;

        return new Vector2(
            Mathf.Lerp(min.x, max.x, (float)random.NextDouble()),
            Mathf.Lerp(min.y, max.y, (float)random.NextDouble()));
    }

    private Vector2 GetPlayerStartPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? (Vector2)player.transform.position : mapBoundary.MapCenter;
    }

    private void CreateProp(PropEntry entry, Vector2 position)
    {
        GameObject instance = Instantiate(entry.prefab, new Vector3(position.x, position.y, 0f), Quaternion.identity, generatedRoot);
        instance.name = entry.prefab.name;

        float minScale = Mathf.Min(entry.scaleMultiplierRange.x, entry.scaleMultiplierRange.y);
        float maxScale = Mathf.Max(entry.scaleMultiplierRange.x, entry.scaleMultiplierRange.y);
        float multiplier = Mathf.Lerp(minScale, maxScale, (float)random.NextDouble());
        instance.transform.localScale *= Mathf.Max(0.01f, multiplier);

        bool blocksPlayer = ShouldBlockPlayer(entry.kind, entry.blockPlayer);
        ConfigureCollision(instance, blocksPlayer);
        ConfigureSorting(instance, entry.kind, position.y);
    }

    private void ConfigureCollision(GameObject instance, bool blocksPlayer)
    {
        Collider2D[] colliders = instance.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = blocksPlayer;
            colliders[i].isTrigger = false;
        }

        if (!blocksPlayer || colliders.Length > 0)
        {
            return;
        }

        SpriteRenderer renderer = instance.GetComponentInChildren<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
        {
            return;
        }

        Bounds spriteBounds = renderer.sprite.bounds;
        BoxCollider2D collider = instance.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(
            Mathf.Max(0.01f, spriteBounds.size.x * colliderWidthRatio),
            Mathf.Max(0.01f, spriteBounds.size.y * colliderHeightRatio));
        collider.offset = new Vector2(
            spriteBounds.center.x,
            spriteBounds.min.y + collider.size.y * 0.5f);
    }

    private void ConfigureSorting(GameObject instance, PropKind kind, float y)
    {
        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
        int order = kind == PropKind.Decoration
            ? decorationSortingOrder
            : obstacleSortingBase + Mathf.RoundToInt(-y * 10f);

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = order;
        }
    }

    public static int CalculateSpawnCount(float mapArea, float densityPerHundredUnits)
    {
        return Mathf.Max(0, Mathf.RoundToInt(Mathf.Max(0f, mapArea) * Mathf.Max(0f, densityPerHundredUnits) / 100f));
    }

    public static bool ShouldBlockPlayer(PropKind kind, bool blockPlayer)
    {
        return kind == PropKind.Obstacle && blockPlayer;
    }

    public static bool IsFarEnough(Vector2 candidate, IReadOnlyList<Vector2> existing, float minSpacing)
    {
        float minimumSquared = Mathf.Max(0f, minSpacing) * Mathf.Max(0f, minSpacing);
        for (int i = 0; i < existing.Count; i++)
        {
            if ((candidate - existing[i]).sqrMagnitude < minimumSquared)
            {
                return false;
            }
        }

        return true;
    }
}
