using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tự động nhận diện toàn bộ SpriteRenderer trên chính nó và các GameObject con (child objects),
/// tạo và đồng bộ PolygonCollider2D khớp chính xác theo từng frame animation của từng bộ phận (thân, chân, càng, cánh...).
/// Hỗ trợ hoàn hảo cho cả Enemy (Compound Triggers) lẫn Player.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class AnimatedSpriteCollider : MonoBehaviour
{
    [System.Serializable]
    public class ChildColliderEntry
    {
        public SpriteRenderer renderer;
        public PolygonCollider2D collider;
        public Sprite lastSprite;
    }

    [Header("Configuration")]
    [Tooltip("Tự động quét và nhận diện tất cả SpriteRenderer ở các GameObject con.")]
    [SerializeField] private bool autoDetectChildren = true;

    [Tooltip("Trạng thái isTrigger cho các PolygonCollider2D. Enemy nên là true, Player nên là false để chặn tường.")]
    [SerializeField] private bool isTrigger = true;

    [Tooltip("Bỏ qua các sprite bóng đổ (Bong/Shadow) hoặc UI/HealthBar.")]
    [SerializeField] private bool ignoreShadowsAndUI = true;

    [Tooltip("Tự động xóa các collider nguyên thủy cũ (Circle/Capsule/Box) trên root sau khi thiết lập.")]
    [SerializeField] private bool removeLegacyRootColliders = true;

    [Tooltip("Chỉ cập nhật 1 lần khi khởi tạo / bật lên, không cập nhật lại liên tục mỗi frame trong LateUpdate để tránh spike CPU.")]
    [SerializeField] private bool updatePerFrame = false;

    [Header("Tracked Entries")]
    [SerializeField] private List<ChildColliderEntry> entries = new List<ChildColliderEntry>();

    // Cache static toàn cục cho các Polygon Physics Shapes để chia sẻ giữa các instance cùng dùng chung Sprite
    private static readonly Dictionary<Sprite, List<Vector2[]>> PolygonShapeCache = new Dictionary<Sprite, List<Vector2[]>>();

    public IReadOnlyList<ChildColliderEntry> Entries => entries;

    private void Awake()
    {
        if (entries == null || entries.Count == 0)
        {
            RefreshAndSetupColliders();
        }
    }

    private void OnEnable()
    {
        if (entries == null || entries.Count == 0)
        {
            RefreshAndSetupColliders();
        }
        else
        {
            ForceSyncAll();
        }
    }

    private void Reset()
    {
        RefreshAndSetupColliders();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    if (entries == null || entries.Count == 0)
                    {
                        RefreshAndSetupColliders();
                    }
                    else
                    {
                        ForceSyncAll();
                    }
                }
            };
        }
    }
#endif

    /// <summary>
    /// Tự động quét toàn bộ sprite con, tạo PolygonCollider2D ôm sát từng sprite và đồng bộ ban đầu.
    /// </summary>
    [ContextMenu("Auto Setup Child Polygon Colliders")]
    public void RefreshAndSetupColliders()
    {
        if (entries == null) entries = new List<ChildColliderEntry>();
        entries.Clear();

        string rootTag = gameObject.tag;
        int rootLayer = gameObject.layer;

        // Nếu đối tượng là Player thì collider dùng để chặn tường/vật cản (isTrigger = false)
        if (CompareTag("Player") || rootLayer == 6)
        {
            isTrigger = false;
        }
        else if (CompareTag("Enemy") || rootLayer == 7)
        {
            isTrigger = true;
        }

        // Lấy toàn bộ SpriteRenderer trên đối tượng và con của nó
        SpriteRenderer[] renderers = autoDetectChildren 
            ? GetComponentsInChildren<SpriteRenderer>(true) 
            : GetComponents<SpriteRenderer>();

        foreach (var sr in renderers)
        {
            if (sr == null) continue;

            // Bỏ qua bóng (Bong/Shadow) hoặc thanh máu/UI
            if (ignoreShadowsAndUI)
            {
                string lowerName = sr.gameObject.name.ToLower();
                if (lowerName.Contains("bong") || lowerName.Contains("shadow") || lowerName.Contains("health") || lowerName.Contains("bar") || lowerName.Contains("icon"))
                {
                    continue;
                }
            }

            GameObject childObj = sr.gameObject;

            // Đảm bảo Layer và Tag của con đồng bộ với Root để đạn và va chạm nhận diện chính xác
            if (childObj != gameObject)
            {
                if (!string.IsNullOrEmpty(rootTag) && rootTag != "Untagged")
                {
                    childObj.tag = rootTag;
                }
                childObj.layer = rootLayer;
            }

            // Tìm hoặc thêm PolygonCollider2D
            PolygonCollider2D poly = childObj.GetComponent<PolygonCollider2D>();
            if (poly == null)
            {
                poly = childObj.AddComponent<PolygonCollider2D>();
            }

            poly.isTrigger = isTrigger;

            var entry = new ChildColliderEntry
            {
                renderer = sr,
                collider = poly,
                lastSprite = sr.sprite
            };

            if (sr.sprite != null)
            {
                UpdatePolygonShape(poly, sr.sprite);
            }

            entries.Add(entry);
        }

        if (removeLegacyRootColliders)
        {
            CleanLegacyRootColliders();
        }
    }

    /// <summary>
    /// Gỡ bỏ các collider nguyên thủy cũ (Circle/Capsule/Box) trên root nếu đã có polygon trên con
    /// </summary>
    public void CleanLegacyRootColliders()
    {
        // Nếu root không có SpriteRenderer trực tiếp (các sprite nằm ở con)
        if (GetComponent<SpriteRenderer>() == null && entries.Count > 0)
        {
            var circle = GetComponent<CircleCollider2D>();
            if (circle != null) DestroyImmediate(circle);

            var capsule = GetComponent<CapsuleCollider2D>();
            if (capsule != null) DestroyImmediate(capsule);

            var box = GetComponent<BoxCollider2D>();
            if (box != null) DestroyImmediate(box);
        }
    }

    private void LateUpdate()
    {
        if (!updatePerFrame) return;

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || entry.renderer == null || entry.collider == null) continue;

            Sprite current = entry.renderer.sprite;
            if (current != entry.lastSprite)
            {
                entry.lastSprite = current;
                UpdatePolygonShape(entry.collider, current);
            }
        }
    }

    /// <summary>
    /// Buộc đồng bộ lại toàn bộ các PolygonCollider2D của các sprite con ngay lập tức.
    /// </summary>
    [ContextMenu("Force Sync All Colliders")]
    public void ForceSyncAll()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || entry.renderer == null || entry.collider == null) continue;

            entry.lastSprite = entry.renderer.sprite;
            if (entry.lastSprite != null)
            {
                UpdatePolygonShape(entry.collider, entry.lastSprite);
            }
        }
    }

    private void UpdatePolygonShape(PolygonCollider2D poly, Sprite sprite)
    {
        if (poly == null) return;
        if (sprite == null)
        {
            poly.pathCount = 0;
            return;
        }

        if (!PolygonShapeCache.TryGetValue(sprite, out List<Vector2[]> paths))
        {
            paths = new List<Vector2[]>();
            int shapeCount = sprite.GetPhysicsShapeCount();

            List<Vector2> tempPoints = new List<Vector2>();
            for (int i = 0; i < shapeCount; i++)
            {
                tempPoints.Clear();
                sprite.GetPhysicsShape(i, tempPoints);
                if (tempPoints.Count >= 3)
                {
                    paths.Add(tempPoints.ToArray());
                }
            }

            if (paths.Count == 0)
            {
                Bounds b = sprite.bounds;
                paths.Add(new Vector2[]
                {
                    new Vector2(b.min.x, b.min.y),
                    new Vector2(b.min.x, b.max.y),
                    new Vector2(b.max.x, b.max.y),
                    new Vector2(b.max.x, b.min.y)
                });
            }

            PolygonShapeCache[sprite] = paths;
        }

        poly.pathCount = paths.Count;
        for (int i = 0; i < paths.Count; i++)
        {
            poly.SetPath(i, paths[i]);
        }
    }

    public static void ClearShapeCache()
    {
        PolygonShapeCache.Clear();
    }
}
