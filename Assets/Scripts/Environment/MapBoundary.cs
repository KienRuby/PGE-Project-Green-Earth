using UnityEngine;

/// <summary>
/// Quản lý giới hạn bản đồ (2D Map Boundary):
/// - Giới hạn phạm vi di chuyển của Player (trừ đi bán kính thân / padding).
/// - Giới hạn vùng di chuyển của Camera (không để lộ khoảng đen ngoài map).
/// - Giới hạn tọa độ sinh quái của EnemySpawner nằm gọn bên trong bản đồ.
/// - Cung cấp Gizmos trực quan trên Scene View để căn chỉnh nhanh chóng.
/// </summary>
[DisallowMultipleComponent]
public class MapBoundary : MonoBehaviour
{
    public static MapBoundary Instance { get; private set; }

    [Header("1. Map Bounding Box")]
    [Tooltip("Tự động lấy kích thước từ SpriteRenderer gắn trên GameObject này.")]
    [SerializeField] private bool autoFitFromSpriteRenderer = true;

    [Tooltip("Tâm của bản đồ trong không gian thế giới (World Space).")]
    [SerializeField] private Vector2 mapCenter = Vector2.zero;

    [Tooltip("Tổng kích thước bản đồ (Rộng X, Cao Y theo đơn vị Unity Meter).")]
    [SerializeField] private Vector2 mapSize = new Vector2(40f, 40f);

    [Header("2. Player Boundary Settings")]
    [Tooltip("Khoảng đệm an toàn / Bán kính thân Player để nhân vật không bị ló nửa người ra ngoài mép map.")]
    [SerializeField, Range(0.1f, 3.0f)] private float playerPadding = 0.6f;

    [Header("3. Visual Gizmos Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color mapBoundaryColor = new Color(0f, 1f, 0.5f, 0.9f); // Xanh lá
    [SerializeField] private Color playerLimitColor = new Color(1f, 0.85f, 0.1f, 0.8f); // Vàng
    [SerializeField] private Color cameraLimitColor = new Color(1f, 0.2f, 0.8f, 0.7f); // Hồng tím

    private SpriteRenderer mapSpriteRenderer;

    // Bounds properties
    public Vector2 MinBounds => mapCenter - mapSize * 0.5f;
    public Vector2 MaxBounds => mapCenter + mapSize * 0.5f;
    public Vector2 MapSize => mapSize;
    public Vector2 MapCenter => mapCenter;
    public float PlayerPadding => playerPadding;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        UpdateMapBoundsFromSprite();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnValidate()
    {
        UpdateMapBoundsFromSprite();
    }

    /// <summary>
    /// Thiết lập thông số bản đồ trực tiếp từ mã nguồn hoặc ChapterMapManager.
    /// </summary>
    public void SetupBounds(Vector2 center, Vector2 size, float padding)
    {
        mapCenter = center;
        mapSize = new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
        playerPadding = Mathf.Max(0.05f, padding);
        autoFitFromSpriteRenderer = false;
    }

    /// <summary>
    /// Tự động đọc kích thước thực tế từ SpriteRenderer (nếu có).
    /// </summary>
    public void UpdateMapBoundsFromSprite()
    {
        if (!autoFitFromSpriteRenderer) return;

        if (mapSpriteRenderer == null)
        {
            mapSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (mapSpriteRenderer != null && mapSpriteRenderer.sprite != null)
        {
            Bounds b = mapSpriteRenderer.bounds;
            mapCenter = b.center;
            mapSize = b.size;
        }
    }

    /// <summary>
    /// Giới hạn tọa độ của Player nằm trọn bên trong bản đồ.
    /// </summary>
    public Vector2 ClampPlayerPosition(Vector2 rawPosition)
    {
        Vector2 min = MinBounds + new Vector2(playerPadding, playerPadding);
        Vector2 max = MaxBounds - new Vector2(playerPadding, playerPadding);

        // Trường hợp map quá nhỏ so với padding
        if (min.x > max.x) min.x = max.x = mapCenter.x;
        if (min.y > max.y) min.y = max.y = mapCenter.y;

        float clampedX = Mathf.Clamp(rawPosition.x, min.x, max.x);
        float clampedY = Mathf.Clamp(rawPosition.y, min.y, max.y);

        return new Vector2(clampedX, clampedY);
    }

    /// <summary>
    /// Giới hạn tâm Camera để khung hình không bị nhìn thấy khoảng trống ngoài map.
    /// </summary>
    public Vector2 ClampCameraPosition(Vector2 rawCameraPos, Camera cam)
    {
        if (cam == null) return rawCameraPos;

        // Tính nửa chiều cao và nửa chiều rộng theo đơn vị World Unity
        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth = camHalfHeight * cam.aspect;

        Vector2 min = MinBounds + new Vector2(camHalfWidth, camHalfHeight);
        Vector2 max = MaxBounds - new Vector2(camHalfWidth, camHalfHeight);

        float clampedX;
        float clampedY;

        // Nếu map nhỏ hơn chiều rộng khung nhìn camera -> Cố định camera ở giữa map theo trục X
        if (min.x > max.x)
        {
            clampedX = mapCenter.x;
        }
        else
        {
            clampedX = Mathf.Clamp(rawCameraPos.x, min.x, max.x);
        }

        // Nếu map nhỏ hơn chiều cao khung nhìn camera -> Cố định camera ở giữa map theo trục Y
        if (min.y > max.y)
        {
            clampedY = mapCenter.y;
        }
        else
        {
            clampedY = Mathf.Clamp(rawCameraPos.y, min.y, max.y);
        }

        return new Vector2(clampedX, clampedY);
    }

    /// <summary>
    /// Giới hạn tọa độ spawn của quái vật đảm bảo nằm trọn trong bản đồ.
    /// </summary>
    public Vector2 ClampSpawnPosition(Vector2 rawSpawnPos, float margin = 0.5f)
    {
        Vector2 min = MinBounds + new Vector2(margin, margin);
        Vector2 max = MaxBounds - new Vector2(margin, margin);

        if (min.x > max.x) min.x = max.x = mapCenter.x;
        if (min.y > max.y) min.y = max.y = mapCenter.y;

        return new Vector2(
            Mathf.Clamp(rawSpawnPos.x, min.x, max.x),
            Mathf.Clamp(rawSpawnPos.y, min.y, max.y)
        );
    }

    /// <summary>
    /// Kiểm tra một tọa độ có đang nằm trong phạm vi bản đồ không.
    /// </summary>
    public bool IsInsideMap(Vector2 position, float margin = 0f)
    {
        Vector2 min = MinBounds + new Vector2(margin, margin);
        Vector2 max = MaxBounds - new Vector2(margin, margin);
        return position.x >= min.x && position.x <= max.x && position.y >= min.y && position.y <= max.y;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // 1. Viền ngoài của toàn bộ Map (Màu xanh lá)
        Gizmos.color = mapBoundaryColor;
        Gizmos.DrawWireCube(mapCenter, new Vector3(mapSize.x, mapSize.y, 0f));

        // 2. Phạm vi di chuyển tối đa của Player (Màu vàng - có trừ padding)
        Vector2 playerBoxSize = new Vector2(
            Mathf.Max(0f, mapSize.x - playerPadding * 2f),
            Mathf.Max(0f, mapSize.y - playerPadding * 2f)
        );
        Gizmos.color = playerLimitColor;
        Gizmos.DrawWireCube(mapCenter, new Vector3(playerBoxSize.x, playerBoxSize.y, 0f));

        // 3. Phạm vi di chuyển tối đa của Camera Viewport (Màu hồng tím)
        Camera cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            float camHalfHeight = cam.orthographicSize;
            float camHalfWidth = camHalfHeight * cam.aspect;

            float camBoxW = Mathf.Max(0f, mapSize.x - camHalfWidth * 2f);
            float camBoxH = Mathf.Max(0f, mapSize.y - camHalfHeight * 2f);

            Gizmos.color = cameraLimitColor;
            Gizmos.DrawWireCube(mapCenter, new Vector3(camBoxW, camBoxH, 0f));
        }
    }
}
