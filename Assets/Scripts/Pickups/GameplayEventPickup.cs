using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thực thể điểm sự kiện tương tác xuất hiện trên bản đồ trong gameplay.
/// Tương tự ArtifactBoxPickup, có biểu tượng dấu hỏi '?' và báo hiệu radar ở mép màn hình khi ở xa.
/// Khi người chơi chạm vào, game tạm dừng và mở GameplayEventModalController để đưa ra các lựa chọn.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class GameplayEventPickup : MonoBehaviour
{
    [Header("Event Configuration")]
    [Tooltip("Sự kiện cụ thể được gán. Nếu để trống, hệ thống sẽ bốc ngẫu nhiên từ GameplayEventDatabase.")]
    [SerializeField] private GameplayEventData assignedEvent;

    [Header("Visual & Animation")]
    [Tooltip("SpriteRenderer hiển thị biểu tượng dấu hỏi/beacon sự kiện trên bản đồ.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Tốc độ bay dập dềnh (Idle bobbing speed).")]
    [SerializeField] private float bobbingSpeed = 3.5f;

    [Tooltip("Biên độ bay dập dềnh (Idle bobbing amount).")]
    [SerializeField] private float bobbingAmount = 0.2f;

    [Tooltip("Bán kính phát hiện va chạm với Player.")]
    [SerializeField] private float triggerRadius = 0.9f;

    private static readonly List<GameplayEventPickup> activeEvents = new List<GameplayEventPickup>();
    public static IReadOnlyList<GameplayEventPickup> ActiveEvents => activeEvents;

    public static void ClearActiveEventsForTesting()
    {
        activeEvents.Clear();
    }

    private Vector3 initialPosition;
    private bool isTriggered = false;

    public GameplayEventData AssignedEvent
    {
        get => assignedEvent;
        set => assignedEvent = value;
    }

    private void OnEnable()
    {
        if (!activeEvents.Contains(this))
        {
            activeEvents.Add(this);
        }
    }

    private void OnDisable()
    {
        activeEvents.Remove(this);
    }

    private void OnDestroy()
    {
        activeEvents.Remove(this);
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
        if (initialPosition == Vector3.zero && transform.position != Vector3.zero)
        {
            initialPosition = transform.position;
        }
    }

    public void SetSpawnPosition(Vector3 position)
    {
        initialPosition = position;
        transform.position = position;
    }

    private void EnsureVisual()
    {
        if (spriteRenderer == null)
        {
            GameObject visualObj = new GameObject("EventVisual", typeof(SpriteRenderer));
            visualObj.transform.SetParent(transform, false);
            spriteRenderer = visualObj.GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer.sprite == null)
        {
            // Tạo Sprite biểu tượng dấu hỏi '?' Cyberpunk neon 48x48
            int size = 48;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color32 cyanNeon = new Color32(46, 229, 240, 255);    // Cyan dạ quang
            Color32 goldYellow = new Color32(255, 215, 0, 255);   // Vàng kim chói
            Color32 darkBg = new Color32(14, 26, 42, 230);        // Nền tối xanh than
            Color32 clear = new Color32(0, 0, 0, 0);

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = 22f;

            // Ma trận vẽ dấu '?' 11x15
            string[] questionGlyph = new string[]
            {
                "  XXXXXX  ",
                " XX    XX ",
                "XX      XX",
                "        XX",
                "        XX",
                "       XX ",
                "     XXX  ",
                "    XX    ",
                "    XX    ",
                "    XX    ",
                "          ",
                "    XX    ",
                "   XXXX   ",
                "    XX    "
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist > radius)
                    {
                        tex.SetPixel(x, y, clear);
                    }
                    else if (dist >= radius - 2.5f)
                    {
                        tex.SetPixel(x, y, cyanNeon);
                    }
                    else if (dist >= radius - 4f)
                    {
                        tex.SetPixel(x, y, goldYellow);
                    }
                    else
                    {
                        tex.SetPixel(x, y, darkBg);
                    }
                }
            }

            // Vẽ glyph dấu '?' vào tâm
            int startX = (size - 10) / 2;
            int startY = (size - 14) / 2;
            for (int row = 0; row < questionGlyph.Length; row++)
            {
                string line = questionGlyph[row];
                int y = startY + (questionGlyph.Length - 1 - row);
                for (int col = 0; col < line.Length; col++)
                {
                    if (line[col] == 'X')
                    {
                        int x = startX + col;
                        if (x >= 0 && x < size && y >= 0 && y < size)
                        {
                            tex.SetPixel(x, y, goldYellow);
                        }
                    }
                }
            }

            tex.Apply();
            spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }

        string targetSortingLayer = "UI";
        if (SortingLayer.NameToID("UI") == 0)
        {
            targetSortingLayer = SortingLayer.NameToID("VFX ") != 0 ? "VFX " : "Player";
        }
        spriteRenderer.sortingLayerName = targetSortingLayer;
        spriteRenderer.sortingOrder = 105;
    }

    private void Update()
    {
        if (isTriggered) return;

        if (initialPosition == Vector3.zero && transform.position != Vector3.zero)
        {
            initialPosition = transform.position;
        }

        float newY = initialPosition.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount;
        transform.position = new Vector3(initialPosition.x, newY, initialPosition.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTriggered) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerHealth>() != null)
        {
            TriggerEvent();
        }
    }

    public void TriggerEvent()
    {
        if (isTriggered) return;
        isTriggered = true;

        if (assignedEvent == null)
        {
            assignedEvent = GameplayEventDatabase.Instance.GetRandomEvent();
        }

        if (assignedEvent == null)
        {
            Debug.LogWarning("[GameplayEventPickup] Không tìm thấy sự kiện nào trong database!");
            Destroy(gameObject);
            return;
        }

        if (spriteRenderer != null) spriteRenderer.enabled = false;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        GameplayEventModalController modal = GameplayEventModalController.Instance;
        if (modal != null)
        {
            modal.Show(assignedEvent, onCompleted: () =>
            {
                Destroy(gameObject);
            });
        }
        else
        {
            Debug.LogWarning("[GameplayEventPickup] Không tìm thấy GameplayEventModalController trong Canvas!");
            Destroy(gameObject);
        }
    }
}
