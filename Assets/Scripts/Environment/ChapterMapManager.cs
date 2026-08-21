using UnityEngine;

/// <summary>
/// Quản lý bản đồ và môi trường hiển thị cho từng Chapter:
/// - Gắn trên GameObject 'Background_Ground'.
/// - Tự động nạp cấu hình bản đồ (Sprite sàn, Kích thước, Màu sắc, Chế độ vẽ) từ ChapterData đang chọn.
/// - Đồng bộ thông số trực tiếp sang MapBoundary để Player và Camera được giới hạn hoàn hảo.
/// - Dễ dàng mở rộng cho các Chapter mới trong tương lai thông qua ScriptableObject.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class ChapterMapManager : MonoBehaviour
{
    [Header("Chapter Integration")]
    [Tooltip("Cơ sở dữ liệu Chapter để đọc cấu hình sàn theo Chapter đang chọn (tự tìm nếu để trống).")]
    [SerializeField] private ChapterDatabase chapterDatabase;

    [Header("Default Fallback Settings")]
    [Tooltip("Nếu tích chọn, khi ChapterData chưa gán 'Map Ground Sprite', hệ thống sẽ GIỮ NGUYÊN Sprite mà bạn đang kéo vào SpriteRenderer trong Scene (rất tiện để test nhanh).")]
    [SerializeField] private bool useSceneSpriteIfChapterEmpty = true;

    [Tooltip("Sprite sàn dự phòng (chỉ dùng khi cả Chapter lẫn SpriteRenderer trong Scene đều chưa có sprite).")]
    [SerializeField] private Sprite defaultGroundSprite;

    [Tooltip("Kích thước mặc định của sàn (Rộng X, Cao Y) nếu không tìm thấy dữ liệu Chapter.")]
    [SerializeField] private Vector2 defaultMapSize = new Vector2(40f, 40f);

    [Tooltip("Màu sắc mặc định của sàn.")]
    [SerializeField] private Color defaultMapColor = Color.white;

    [Tooltip("Khoảng đệm an toàn mặc định cho Player.")]
    [SerializeField] private float defaultPlayerPadding = 0.6f;

    [Header("Position & Layering")]
    [Tooltip("Tọa độ Z cố định của mặt sàn (mặc định Z = 10 để luôn nằm sau nhân vật và quái).")]
    [SerializeField] private float groundZPosition = 10f;

    [Tooltip("Order in Layer (mặc định -100 để chắc chắn nằm dưới cùng).")]
    [SerializeField] private int sortingOrder = -100;

    private SpriteRenderer spriteRenderer;
    private MapBoundary mapBoundary;

    public ChapterData ActiveChapterData { get; private set; }

    private void Awake()
    {
        InitializeMap();
    }

    private void Start()
    {
        ApplyCurrentChapterMap();
    }

    public void InitializeMap()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mapBoundary = GetComponent<MapBoundary>();
        if (mapBoundary == null)
        {
            mapBoundary = gameObject.AddComponent<MapBoundary>();
        }

        if (defaultGroundSprite == null && spriteRenderer != null)
        {
            defaultGroundSprite = spriteRenderer.sprite;
        }

        // Cố định vị trí sàn tại tâm (0, 0, Z)
        transform.position = new Vector3(0f, 0f, groundZPosition);
        transform.rotation = Quaternion.identity;
    }

    /// <summary>
    /// Nạp và áp dụng cấu hình bản đồ từ Chapter đang được chọn.
    /// </summary>
    public void ApplyCurrentChapterMap()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (mapBoundary == null)
        {
            mapBoundary = GetComponent<MapBoundary>() ?? gameObject.AddComponent<MapBoundary>();
        }

        FindChapterDatabase();

        ChapterData currentChapter = null;
        if (chapterDatabase != null)
        {
            int selectedIndex = PlayerDataService.SelectedChapterIndex;
            currentChapter = chapterDatabase.GetChapter(selectedIndex);
        }

        ActiveChapterData = currentChapter;

        if (currentChapter != null)
        {
            ApplyChapterConfig(currentChapter);
        }
        else
        {
            ApplyFallbackConfig();
        }
    }

    private void ApplyChapterConfig(ChapterData chapter)
    {
        // 1. Xác định Sprite sàn mục tiêu: Ưu tiên Chapter > Scene Sprite > Default
        Sprite targetSprite = null;
        if (chapter.mapGroundSprite != null)
        {
            targetSprite = chapter.mapGroundSprite;
        }
        else if (useSceneSpriteIfChapterEmpty && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            targetSprite = spriteRenderer.sprite; // Giữ nguyên sprite đang kéo trong Scene để test
        }
        else
        {
            targetSprite = defaultGroundSprite;
        }

        if (spriteRenderer != null)
        {
            if (targetSprite != null)
            {
                spriteRenderer.sprite = targetSprite;
            }
            spriteRenderer.drawMode = chapter.groundDrawMode;
            if (chapter.groundDrawMode == SpriteDrawMode.Tiled)
            {
                spriteRenderer.tileMode = SpriteTileMode.Continuous;
                spriteRenderer.size = chapter.mapSize;
            }
            spriteRenderer.color = chapter.mapColor;
            spriteRenderer.sortingOrder = sortingOrder;
        }

        // 2. Cập nhật MapBoundary
        if (mapBoundary != null)
        {
            mapBoundary.SetupBounds(Vector2.zero, chapter.mapSize, chapter.playerBoundaryPadding);
        }

        // 3. Khóa vị trí sàn tại tâm
        transform.position = new Vector3(0f, 0f, groundZPosition);

        Debug.Log($"[ChapterMapManager] 🗺️ Đã thiết lập Bản đồ cho '{chapter.chapterTitle}' (Sprite: {(targetSprite != null ? targetSprite.name : "None")}, Size: {chapter.mapSize.x}x{chapter.mapSize.y}m, Padding: {chapter.playerBoundaryPadding}m)");
    }

    private void ApplyFallbackConfig()
    {
        if (spriteRenderer != null)
        {
            if (defaultGroundSprite != null)
            {
                spriteRenderer.sprite = defaultGroundSprite;
            }
            spriteRenderer.drawMode = SpriteDrawMode.Tiled;
            spriteRenderer.tileMode = SpriteTileMode.Continuous;
            spriteRenderer.size = defaultMapSize;
            spriteRenderer.color = defaultMapColor;
            spriteRenderer.sortingOrder = sortingOrder;
        }

        if (mapBoundary != null)
        {
            mapBoundary.SetupBounds(Vector2.zero, defaultMapSize, defaultPlayerPadding);
        }

        transform.position = new Vector3(0f, 0f, groundZPosition);
    }

    private void FindChapterDatabase()
    {
        if (chapterDatabase != null) return;

#if UNITY_EDITOR
        chapterDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<ChapterDatabase>("Assets/Data/Chapters/ChapterDatabase.asset");
#endif
        if (chapterDatabase == null)
        {
            chapterDatabase = Resources.Load<ChapterDatabase>("ChapterDatabase");
        }
    }

    public void SetDatabaseForTesting(ChapterDatabase db)
    {
        chapterDatabase = db;
    }
}
