using UnityEngine;

/// <summary>
/// Quản lý hiển thị bản đồ mặt đất vô tận (Infinite Tiled Ground / Map Background).
/// Tự động định vị và mở rộng mặt sàn theo tầm nhìn Camera của Player,
/// đảm bảo góc nhìn 2D luôn có mặt đất với chi tiết rõ ràng khi di chuyển.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class InfiniteTiledGround : MonoBehaviour
{
    [Header("Tracking Target")]
    [Tooltip("Mục tiêu để mặt đất bám theo (thường là Camera chính hoặc Player).")]
    [SerializeField] private Transform followTarget;

    [Header("Ground Size")]
    [Tooltip("Kích thước vùng sàn bao phủ quanh Camera (Rộng X, Cao Y theo đơn vị Unity).")]
    [SerializeField] private Vector2 mapCoverageSize = new Vector2(40f, 40f);

    [Tooltip("Khoảng cách bước nhảy Snap để giữ họa tiết nền không bị rung giật khi Camera di chuyển.")]
    [SerializeField] private float tileSize = 10.24f;

    [Header("Sorting")]
    [Tooltip("Sorting Layer cho mặt đất (mặc định luôn ở sau cùng).")]
    [SerializeField] private string sortingLayerName = "Default";

    [Tooltip("Order in Layer (ví dụ -100 để chắc chắn nằm dưới Player và Enemy).")]
    [SerializeField] private int orderInLayer = -100;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        if (followTarget == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                followTarget = cam.transform;
            }
        }

        UpdateGroundSizeAndPosition();
    }

    private void LateUpdate()
    {
        // Khi sử dụng hệ thống Fixed Map Boundary, không di chuyển sàn theo Camera nữa
        if (MapBoundary.Instance != null || GetComponent<ChapterMapManager>() != null)
            return;

        if (followTarget == null)
            return;

        Vector3 targetPos = followTarget.position;

        // Snap vị trí sàn theo bội số của tileSize để texture lặp hoàn hảo mà không sinh rác
        float snappedX = Mathf.Floor(targetPos.x / tileSize) * tileSize;
        float snappedY = Mathf.Floor(targetPos.y / tileSize) * tileSize;

        transform.position = new Vector3(snappedX, snappedY, 10f);
    }

    public void Initialize()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.drawMode = SpriteDrawMode.Tiled;
            spriteRenderer.size = mapCoverageSize;
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = orderInLayer;
        }
    }

    private void UpdateGroundSizeAndPosition()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            if (spriteRenderer.drawMode != SpriteDrawMode.Tiled)
            {
                spriteRenderer.drawMode = SpriteDrawMode.Tiled;
            }
            spriteRenderer.size = mapCoverageSize;
            spriteRenderer.sortingOrder = orderInLayer;
        }
    }

#if UNITY_EDITOR
    private UnityEditor.EditorApplication.CallbackFunction editorUpdateCallback;

    private void OnValidate()
    {
        // Coalesce delayCall: hủy callback cũ nếu đang chờ, đăng ký lại callback mới
        if (editorUpdateCallback != null)
        {
            UnityEditor.EditorApplication.delayCall -= editorUpdateCallback;
            editorUpdateCallback = null;
        }

        editorUpdateCallback = OnEditorDelayCall;
        UnityEditor.EditorApplication.delayCall += editorUpdateCallback;
    }

    private void OnEditorDelayCall()
    {
        editorUpdateCallback = null;
        if (this != null && gameObject != null)
        {
            UpdateGroundSizeAndPosition();
        }
    }

    private void OnDisable()
    {
        CleanupEditorDelayCall();
    }

    private void OnDestroy()
    {
        CleanupEditorDelayCall();
    }

    private void CleanupEditorDelayCall()
    {
        if (editorUpdateCallback != null)
        {
            UnityEditor.EditorApplication.delayCall -= editorUpdateCallback;
            editorUpdateCallback = null;
        }
    }
#endif
}
