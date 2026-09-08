using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thực thể "Hộp Mù" (Mystery Box / Cổ vật) rơi trên bản đồ trong gameplay.
/// Khi nhân vật Player chạm vào, trò chơi sẽ tạm dừng và mở Popup "Artifact found".
/// Hỗ trợ tự sinh đồ họa placeholder (hòm báu/hộp quà) nếu chưa gán Sprite chính thức.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class ArtifactBoxPickup : MonoBehaviour
{
    [Header("Artifact Drop Configuration")]
    [Tooltip("Artifact cụ thể chứa trong hộp. Nếu để trống, hệ thống sẽ bốc ngẫu nhiên từ ArtifactDatabase khi người chơi chạm vào.")]
    [SerializeField] private ArtifactData assignedArtifact;

    [Header("Visual & Animation")]
    [Tooltip("SpriteRenderer hiển thị chiếc hộp/rương trên bản đồ.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Tốc độ bay dập dềnh (Idle bobbing speed).")]
    [SerializeField] private float bobbingSpeed = 3f;

    [Tooltip("Biên độ bay dập dềnh (Idle bobbing amount).")]
    [SerializeField] private float bobbingAmount = 0.15f;

    [Tooltip("Bán kính phát hiện va chạm với Player.")]
    [SerializeField] private float triggerRadius = 0.8f;

    private static readonly List<ArtifactBoxPickup> activeBoxes = new List<ArtifactBoxPickup>();
    public static IReadOnlyList<ArtifactBoxPickup> ActiveBoxes => activeBoxes;

    public static void ClearActiveBoxesForTesting()
    {
        activeBoxes.Clear();
    }

    private Vector3 initialPosition;
    private bool isCollected = false;

    private void OnEnable()
    {
        if (!activeBoxes.Contains(this))
        {
            activeBoxes.Add(this);
        }
    }

    private void OnDisable()
    {
        activeBoxes.Remove(this);
    }

    private void OnDestroy()
    {
        activeBoxes.Remove(this);
    }

    private void Awake()
    {
        initialPosition = transform.position;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            if (col is CircleCollider2D circle)
            {
                circle.radius = triggerRadius;
            }
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        }

        EnsureVisual();
    }

    private void Start()
    {
        // Đảm bảo nếu vị trí được gán sau Awake thì initialPosition tự cập nhật chuẩn xác
        if (initialPosition == Vector3.zero && transform.position != Vector3.zero)
        {
            initialPosition = transform.position;
        }
    }

    /// <summary>
    /// Thiết lập tọa độ xuất hiện chuẩn cho Hộp Cổ Vật và cập nhật initialPosition chống trôi/giật vị trí.
    /// </summary>
    public void SetSpawnPosition(Vector3 position)
    {
        initialPosition = position;
        transform.position = position;
    }

    /// <summary>
    /// Đảm bảo hộp luôn có hình ảnh hiển thị đẹp mắt trên bản đồ (procedural visual fallback)
    /// kể cả khi bạn chưa bổ sung file Sprite nghệ thuật chính thức.
    /// </summary>
    private void EnsureVisual()
    {
        if (spriteRenderer == null)
        {
            GameObject visualObj = new GameObject("BoxVisual", typeof(SpriteRenderer));
            visualObj.transform.SetParent(transform, false);
            spriteRenderer = visualObj.GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer.sprite == null)
        {
            // Tạo Sprite rương báu Cyberpunk 48x48 rực rỡ (viền Cyan, thân Vàng kim & ngọc dạ quang)
            int size = 48;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color32 cyanNeon = new Color32(46, 229, 240, 255);    // Cyan dạ quang
            Color32 goldMetal = new Color32(255, 196, 20, 255);   // Vàng kim loại rực
            Color32 darkGold = new Color32(180, 130, 10, 255);    // Vàng đổ bóng
            Color32 coreCyan = new Color32(100, 255, 255, 255);   // Lõi ngọc sáng chói
            Color32 darkChassis = new Color32(16, 28, 44, 255);   // Thân hộp hợp kim tối

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Bo viền ngoài
                    if (x < 3 || x >= size - 3 || y < 3 || y >= size - 3)
                    {
                        tex.SetPixel(x, y, cyanNeon);
                    }
                    else if (x < 6 || x >= size - 6 || y < 6 || y >= size - 6)
                    {
                        tex.SetPixel(x, y, goldMetal);
                    }
                    // Lõi ngọc trung tâm tỏa sáng
                    else if (Mathf.Abs(x - size / 2) + Mathf.Abs(y - size / 2) <= 8)
                    {
                        tex.SetPixel(x, y, (Mathf.Abs(x - size / 2) + Mathf.Abs(y - size / 2) <= 4) ? coreCyan : cyanNeon);
                    }
                    // Vạch đai nẹp hộp
                    else if (x >= size / 2 - 2 && x <= size / 2 + 1 || y >= size / 2 - 2 && y <= size / 2 + 1)
                    {
                        tex.SetPixel(x, y, darkGold);
                    }
                    else
                    {
                        tex.SetPixel(x, y, darkChassis);
                    }
                }
            }
            tex.Apply();
            spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }

        // Đảm bảo Sorting Layer luôn hiển thị trên sàn map (Ground)
        string targetSortingLayer = "UI";
        if (SortingLayer.NameToID("UI") == 0)
        {
            targetSortingLayer = SortingLayer.NameToID("VFX ") != 0 ? "VFX " : "Player";
        }
        spriteRenderer.sortingLayerName = targetSortingLayer;
        spriteRenderer.sortingOrder = 100;
    }

    private void Update()
    {
        if (isCollected) return;

        // Nếu initialPosition vô tình bằng 0 khi vừa start, chốt lại vị trí thực tế hiện tại
        if (initialPosition == Vector3.zero && transform.position != Vector3.zero)
        {
            initialPosition = transform.position;
        }

        // Hiệu ứng bay bập bềnh nhẹ trên không trung
        float newY = initialPosition.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount;
        transform.position = new Vector3(initialPosition.x, newY, initialPosition.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerHealth>() != null)
        {
            TriggerOpenArtifact();
        }
    }

    /// <summary>
    /// Kích hoạt mở hộp: Dừng game và hiển thị Popup "Artifact found".
    /// </summary>
    public void TriggerOpenArtifact()
    {
        if (isCollected) return;
        isCollected = true;

        // Nếu chưa chỉ định Artifact cụ thể, bốc ngẫu nhiên từ kho dữ liệu
        if (assignedArtifact == null)
        {
            IReadOnlyList<string> ownedIds = null;
            if (PlayerArtifactInventory.Instance != null)
            {
                var list = new List<string>();
                foreach (var a in PlayerArtifactInventory.Instance.EquippedArtifacts)
                {
                    if (a != null) list.Add(a.id);
                }
                ownedIds = list;
            }
            assignedArtifact = ArtifactDatabase.Instance.GetRandomArtifact(ownedIds);
        }

        if (assignedArtifact == null)
        {
            Debug.LogWarning("[ArtifactBoxPickup] Không tìm thấy Artifact để hiển thị!");
            Destroy(gameObject);
            return;
        }

        // Ẩn hình ảnh chiếc hộp và tắt collider ngay khi mở popup
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Mở Popup thông qua Controller
        ArtifactFoundModalController modal = ArtifactFoundModalController.Instance;
        if (modal != null)
        {
            modal.Show(assignedArtifact,
                onGet: () =>
                {
                    if (PlayerArtifactInventory.Instance != null)
                    {
                        PlayerArtifactInventory.Instance.EquipArtifact(assignedArtifact);
                    }
                    Destroy(gameObject);
                },
                onThrowAway: () =>
                {
                    Debug.Log($"[ArtifactBoxPickup] Người chơi đã bỏ qua (Throw away): {assignedArtifact.artifactName}");
                    Destroy(gameObject);
                }
            );
        }
        else
        {
            // Fallback nếu trong Scene chưa có modal controller, tự tạo hoặc nhận luôn
            Debug.LogWarning("[ArtifactBoxPickup] ArtifactFoundModalController chưa được gắn trong Canvas!");
            if (PlayerArtifactInventory.Instance != null)
            {
                PlayerArtifactInventory.Instance.EquipArtifact(assignedArtifact);
            }
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Khởi tạo Artifact cụ thể cho chiếc hộp khi sinh ra từ code.
    /// </summary>
    public void SetArtifact(ArtifactData artifact)
    {
        assignedArtifact = artifact;
    }
}
