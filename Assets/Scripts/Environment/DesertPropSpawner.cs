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

    [Serializable]
    public sealed class ChapterPropConfig
    {
        [Tooltip("Số thứ tự Chapter (1: Sa mạc, 2: Rừng đột biến, 3: Đầm lầy độc,...)")]
        public int chapterNumber = 1;

        [Tooltip("Tên gợi nhớ của Chapter")]
        public string chapterName = "Chapter";

        [Tooltip("Danh sách prefab chướng ngại vật & trang trí cho Chapter này.")]
        public List<PropEntry> props = new List<PropEntry>();
    }

    [SerializeField, Min(1)] private int desertChapterNumber = 1;
    [SerializeField] private bool spawnOnlyInDesertChapter = false;

    [Tooltip("Danh sách prefab mặc định (Chapter 1 - Sa mạc)")]
    [SerializeField] private List<PropEntry> props = new List<PropEntry>();

    [Tooltip("Danh sách cấu hình chướng ngại vật theo từng Chapter riêng biệt")]
    [SerializeField] private List<ChapterPropConfig> chapterPropsList = new List<ChapterPropConfig>();

    [SerializeField, Min(0f)] private float obstacleDensity = 1.5f;
    [SerializeField, Min(0f)] private float decorationDensity = 3f;

    [SerializeField, Min(0f)] private float mapEdgePadding = 1f;
    [SerializeField, Min(0f)] private float obstacleMinSpacing = 1.25f;
    [SerializeField, Min(0f)] private float decorationMinSpacing = 0.75f;
    [SerializeField, Min(0f)] private float playerStartClearRadius = 2.5f;
    [SerializeField, Min(1)] private int attemptsPerProp = 20;

    [SerializeField, Range(0.05f, 1f)] private float colliderWidthRatio = 0.55f;
    [SerializeField, Range(0.05f, 0.5f)] private float colliderHeightRatio = 0.2f;
    [Tooltip("Bật để Enemy có thể đi xuyên qua chướng ngại vật (chỉ chặn Player). Tắt nếu muốn chướng ngại vật chặn cả quái vật.")]
    [SerializeField] private bool allowEnemiesToPassThrough = false;

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
    private float maxForestSwampPropDimension = 0.18f;

    public IReadOnlyList<PropEntry> Props => props;
    public IReadOnlyList<ChapterPropConfig> ChapterPropsList => chapterPropsList;
    public List<ChapterPropConfig> ChapterPropsListEditable => chapterPropsList;
    public float ObstacleDensity => obstacleDensity;
    public float DecorationDensity => decorationDensity;

    private void Start()
    {
        Generate();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureDefaultChapterConfigs();
    }

    public void EnsureDefaultChapterConfigs()
    {
        if (chapterPropsList == null)
        {
            chapterPropsList = new List<ChapterPropConfig>();
        }

        EnsureChapterInternal(2, "Rừng đột biến (Mutant Forest)");
        EnsureChapterInternal(3, "Đầm lầy độc (Toxic Swamp)");
    }

    private void EnsureChapterInternal(int chNum, string chName)
    {
        ChapterPropConfig config = chapterPropsList.Find(c => c != null && c.chapterNumber == chNum);
        if (config == null)
        {
            config = new ChapterPropConfig
            {
                chapterNumber = chNum,
                chapterName = chName,
                props = new List<PropEntry>()
            };
            chapterPropsList.Add(config);
        }

        if (config.props == null)
        {
            config.props = new List<PropEntry>();
        }

        if (config.props.Count == 0)
        {
            PopulateDefaultPropsForChapter(config.props, chNum);
        }
    }

    private static void PopulateDefaultPropsForChapter(List<PropEntry> targetProps, int chNum)
    {
        targetProps.Clear();
        if (chNum == 2)
        {
            AddPropEntry(targetProps, "Assets/Prefabs/Map 2 - Mutant Forest/bui_co_1.prefab", PropKind.Decoration, false);
            AddPropEntry(targetProps, "Assets/Prefabs/Map 2 - Mutant Forest/bui_co_2.prefab", PropKind.Decoration, false);
            AddPropEntry(targetProps, "Assets/Prefabs/Map 2 - Mutant Forest/bui_co_doi.prefab", PropKind.Decoration, false);
        }
        else if (chNum == 3)
        {
            AddPropEntry(targetProps, "Assets/Prefabs/Map 3 - Toxic Swamp/mam_vang.prefab", PropKind.Decoration, false);
            AddPropEntry(targetProps, "Assets/Prefabs/Map 3 - Toxic Swamp/mam_xanh.prefab", PropKind.Decoration, false);
            AddPropEntry(targetProps, "Assets/Prefabs/Map 3 - Toxic Swamp/bui_hoa_xanh.prefab", PropKind.Decoration, false);
        }
    }

    private static void AddPropEntry(List<PropEntry> targetProps, string path, PropKind kind, bool blockPlayer)
    {
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return;

        targetProps.Add(new PropEntry
        {
            prefab = prefab,
            kind = kind,
            blockPlayer = blockPlayer,
            weight = 1f,
            scaleMultiplierRange = new Vector2(0.9f, 1.1f)
        });
    }
#endif

    [ContextMenu("Regenerate Desert Props")]
    public void Generate()
    {
        GenerateInternal(false, false, -1);
    }

    /// <summary>
    /// Tạo bố cục xem trước ngay trong Scene View, không cần vào Play Mode.
    /// Preview không được lưu vào scene và có thể chỉ định chapter cụ thể.
    /// </summary>
    public void GeneratePreview(int chapterNum = -1)
    {
        if (Application.isPlaying)
        {
            Generate();
            return;
        }

        GenerateInternal(true, true, chapterNum);
    }

    private void GenerateInternal(bool ignoreChapterFilter, bool isEditorPreview, int previewChapterNum = -1)
    {
        mapBoundary = GetComponent<MapBoundary>();
        chapterMapManager = GetComponent<ChapterMapManager>();

        ClearGenerated();
        if ((!ignoreChapterFilter && !ShouldSpawnForCurrentChapter()) || mapBoundary == null)
        {
            return;
        }

        int activeChapterNum = previewChapterNum > 0
            ? previewChapterNum
            : (chapterMapManager != null && chapterMapManager.ActiveChapterData != null
                ? chapterMapManager.ActiveChapterData.chapterNumber
                : desertChapterNumber);

        maxForestSwampPropDimension = 0.18f;
        if (activeChapterNum == 2 || activeChapterNum == 3)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            PlayerSkinApplier skin = player != null ? player.GetComponent<PlayerSkinApplier>() : null;
            if (skin != null && skin.bodyRenderer != null && skin.bodyRenderer.enabled && skin.bodyRenderer.sprite != null)
            {
                Vector2 bodySize = skin.bodyRenderer.bounds.size;
                if (bodySize.x > 0f && bodySize.y > 0f)
                {
                    maxForestSwampPropDimension = Mathf.Min(maxForestSwampPropDimension, Mathf.Min(bodySize.x, bodySize.y) * 0.65f);
                }
            }
        }

        if (isEditorPreview)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                string groundSpritePath = null;
                if (activeChapterNum == 1) groundSpritePath = "Assets/Sprites/Backround/Map/nền.png";
                else if (activeChapterNum == 2) groundSpritePath = "Assets/Sprites/Backround/Map/nền (1).png";
                else if (activeChapterNum == 3) groundSpritePath = "Assets/Sprites/Backround/Map/nền (2).png";

#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(groundSpritePath))
                {
                    Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(groundSpritePath);
                    if (s != null)
                    {
                        sr.sprite = s;
                    }
                }
#endif
            }
        }

        random = useRandomSeed
            ? new System.Random(randomSeed)
            : new System.Random(unchecked(Environment.TickCount * 397 ^ GetInstanceID()));

        generatedRoot = new GameObject("Generated Desert Props").transform;
        generatedRoot.SetParent(transform, false);
        generatedRoot.localPosition = new Vector3(0f, 0f, -10f);
        float parentScaleX = Mathf.Abs(transform.localScale.x);
        float parentScaleY = Mathf.Abs(transform.localScale.y);
        generatedRoot.localScale = new Vector3(
            parentScaleX > 0.0001f ? 1f / parentScaleX : 1f,
            parentScaleY > 0.0001f ? 1f / parentScaleY : 1f,
            1f);
        if (isEditorPreview)
        {
            generatedRoot.gameObject.hideFlags = HideFlags.DontSaveInEditor;
        }

        float area = mapBoundary.MapSize.x * mapBoundary.MapSize.y;

        ChapterData currentChapter = chapterMapManager != null ? chapterMapManager.ActiveChapterData : null;
        float currentObstacleDensity = (currentChapter != null)
            ? (currentChapter.enableObstacles ? currentChapter.obstacleDensity : 0f)
            : obstacleDensity;
        if (activeChapterNum == 2 || activeChapterNum == 3)
        {
            currentObstacleDensity = 0f;
        }

        float currentDecorationDensity = (currentChapter != null)
            ? (currentChapter.enableObstacles ? currentChapter.decorationDensity : 0f)
            : decorationDensity;
        float currentWidthRatio = (currentChapter != null && currentChapter.obstacleColliderWidthRatio > 0f)
            ? currentChapter.obstacleColliderWidthRatio
            : colliderWidthRatio;
        float currentHeightRatio = (currentChapter != null && currentChapter.obstacleColliderHeightRatio > 0f)
            ? currentChapter.obstacleColliderHeightRatio
            : colliderHeightRatio;

        SpawnKind(PropKind.Decoration, CalculateSpawnCount(area, currentDecorationDensity), decorationMinSpacing, currentWidthRatio, currentHeightRatio, activeChapterNum);
        if (activeChapterNum != 2 && activeChapterNum != 3)
        {
            SpawnKind(PropKind.Obstacle, CalculateSpawnCount(area, currentObstacleDensity), obstacleMinSpacing, currentWidthRatio, currentHeightRatio, activeChapterNum);
        }
    }

    [ContextMenu("Clear Generated Desert Props")]
    public void ClearGenerated()
    {
        obstaclePositions.Clear();
        decorationPositions.Clear();

        List<Transform> toDestroy = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("Generated"))
            {
                toDestroy.Add(child);
            }
        }

        foreach (var t in toDestroy)
        {
            if (Application.isPlaying) Destroy(t.gameObject);
            else DestroyImmediate(t.gameObject);
        }

        generatedRoot = null;
    }

    private bool ShouldSpawnForCurrentChapter()
    {
        ChapterData chapter = chapterMapManager != null ? chapterMapManager.ActiveChapterData : null;

        if (!spawnOnlyInDesertChapter)
        {
            return chapter == null || chapter.enableObstacles;
        }

        return chapter != null && chapter.chapterNumber == desertChapterNumber;
    }

    public List<PropEntry> GetCurrentPropsList(int chapterNum = -1)
    {
        if (chapterNum <= 0)
        {
            ChapterData chapter = chapterMapManager != null ? chapterMapManager.ActiveChapterData : null;
            chapterNum = chapter != null ? chapter.chapterNumber : desertChapterNumber;
        }

        if (chapterPropsList != null)
        {
            for (int i = 0; i < chapterPropsList.Count; i++)
            {
                if (chapterPropsList[i] != null && chapterPropsList[i].chapterNumber == chapterNum)
                {
                    if (chapterPropsList[i].props != null && chapterPropsList[i].props.Count > 0)
                    {
                        return chapterPropsList[i].props;
                    }
                }
            }
        }

        return props;
    }

    private void SpawnKind(PropKind kind, int targetCount, float minSpacing, float widthRatio, float heightRatio, int chapterNum = -1)
    {
        if (kind == PropKind.Obstacle && (chapterNum == 2 || chapterNum == 3))
        {
            return;
        }

        List<PropEntry> candidates = GetCandidates(kind, chapterNum);
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

            if (CreateProp(entry, position, widthRatio, heightRatio, chapterNum)) positions.Add(position);
        }
    }

    private List<PropEntry> GetCandidates(PropKind kind, int chapterNum = -1)
    {
        List<PropEntry> activeProps = GetCurrentPropsList(chapterNum);
        List<PropEntry> result = new List<PropEntry>();
        for (int i = 0; i < activeProps.Count; i++)
        {
            PropEntry entry = activeProps[i];
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

    private bool CreateProp(PropEntry entry, Vector2 position, float widthRatio, float heightRatio, int chapterNum)
    {
        GameObject instance = Instantiate(entry.prefab, new Vector3(position.x, position.y, 0f), Quaternion.identity, generatedRoot);
        instance.name = entry.prefab.name;

        float minScale = Mathf.Min(entry.scaleMultiplierRange.x, entry.scaleMultiplierRange.y);
        float maxScale = Mathf.Max(entry.scaleMultiplierRange.x, entry.scaleMultiplierRange.y);
        float multiplier = Mathf.Lerp(minScale, maxScale, (float)random.NextDouble());
        instance.transform.localScale *= Mathf.Max(0.01f, multiplier);

        if (chapterNum == 2 || chapterNum == 3)
        {
            // Keep props smaller than the player's visible body, including smaller skins.
            SpriteRenderer[] sprites = instance.GetComponentsInChildren<SpriteRenderer>();
            float largestDimension = 0f;
            for (int i = 0; i < sprites.Length; i++)
            {
                largestDimension = Mathf.Max(largestDimension, Mathf.Max(sprites[i].bounds.size.x, sprites[i].bounds.size.y));
            }
            if (largestDimension > maxForestSwampPropDimension)
            {
                instance.transform.localScale *= maxForestSwampPropDimension / largestDimension;
            }
        }

        bool blocksPlayer = chapterNum != 2 && chapterNum != 3 && ShouldBlockPlayer(entry.kind, entry.blockPlayer);
        ConfigureCollision(instance, blocksPlayer, widthRatio, heightRatio);
        // Reserve the corridor for the whole prop, not only its pivot.
        Physics2D.SyncTransforms();
        foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
        {
            if (!FitsInsideMap(renderer.bounds)) return RejectProp(instance);
        }
        foreach (var collider in instance.GetComponentsInChildren<Collider2D>())
        {
            if (collider.enabled && !FitsInsideMap(collider.bounds)) return RejectProp(instance);
        }
        ConfigureSorting(instance, entry.kind, position.y);
        return true;
    }

    private bool FitsInsideMap(Bounds bounds)
    {
        return mapBoundary.IsInsideMap(bounds.min, mapEdgePadding) &&
               mapBoundary.IsInsideMap(bounds.max, mapEdgePadding);
    }

    private static bool RejectProp(GameObject instance)
    {
        instance.SetActive(false);
        if (Application.isPlaying) Destroy(instance);
        else DestroyImmediate(instance);
        return false;
    }

    private void ConfigureCollision(GameObject instance, bool blocksPlayer, float widthRatio, float heightRatio)
    {
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        if (blocksPlayer && obstacleLayer != -1)
        {
            instance.layer = obstacleLayer;
            Transform[] allChildren = instance.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < allChildren.Length; i++)
            {
                allChildren[i].gameObject.layer = obstacleLayer;
            }
        }

        int enemyLayerMask = allowEnemiesToPassThrough ? LayerMask.GetMask("Enemy") : 0;

        Collider2D[] colliders = instance.GetComponentsInChildren<Collider2D>(true);
        bool hasValidCollider = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null) continue;

            if (IsColliderEmptyOrInvalid(col))
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
                continue;
            }

            col.enabled = blocksPlayer;
            col.isTrigger = false;
            col.excludeLayers = enemyLayerMask;
            hasValidCollider = true;
        }

        if (!blocksPlayer || hasValidCollider)
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
            Mathf.Max(0.01f, spriteBounds.size.x * widthRatio),
            Mathf.Max(0.01f, spriteBounds.size.y * heightRatio));
        collider.offset = new Vector2(
            spriteBounds.center.x,
            spriteBounds.min.y + collider.size.y * 0.5f);
        collider.excludeLayers = enemyLayerMask;
    }

    private static bool IsColliderEmptyOrInvalid(Collider2D col)
    {
        if (col == null) return true;
        if (col is PolygonCollider2D poly)
        {
            return poly.pathCount == 0;
        }
        if (col is BoxCollider2D box)
        {
            return box.size.x <= 0.0001f || box.size.y <= 0.0001f;
        }
        if (col is CircleCollider2D circle)
        {
            return circle.radius <= 0.0001f;
        }
        if (col is CapsuleCollider2D capsule)
        {
            return capsule.size.x <= 0.0001f || capsule.size.y <= 0.0001f;
        }
        return false;
    }

    private void ConfigureSorting(GameObject instance, PropKind kind, float y)
    {
        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
        int order = kind == PropKind.Decoration
            ? decorationSortingOrder
            : Mathf.Max(decorationSortingOrder + 1, obstacleSortingBase + Mathf.RoundToInt(-y * 10f));

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
