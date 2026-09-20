using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HealthBoxType
{
    Small, // Hồi 10% HP tối đa (5% drop chance)
    Large  // Hồi 20% HP tối đa (3% drop chance)
}

/// <summary>
/// Thực thể Hộp Máu (Health Box / First Aid Kit) rơi trong trận đấu khi tiêu diệt enemy:
/// - Hộp máu nhỏ (Small): Hồi 10% HP tối đa (5% drop chance).
/// - Hộp máu lớn (Large): Hồi 20% HP tối đa (3% drop chance).
/// Hỗ trợ:
/// - Tự động nạp Sprite từ Resources hoặc procedural fallback.
/// - Hiệu ứng bay bập bềnh (Idle bobbing) và nhịp thở hào quang nhẹ.
/// - Hút nam châm mượt mà (Magnet Attraction) về phía Player.
/// - Tự động hồi máu cho PlayerHealth và hiển thị số hồi máu xanh lá qua DamageNumberManager.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class HealthBoxPickup : MonoBehaviour
{
    private static readonly List<HealthBoxPickup> activeBoxes = new List<HealthBoxPickup>();
    public static IReadOnlyList<HealthBoxPickup> ActiveBoxes => activeBoxes;

    public static void ClearActiveBoxesForTesting()
    {
        activeBoxes.Clear();
    }

    [Header("Health Box Configuration")]
    [SerializeField] private HealthBoxType boxType = HealthBoxType.Small;
    [Tooltip("Tỷ lệ % máu tối đa hồi phục khi nhặt (0.1 = 10%, 0.2 = 20%).")]
    [Range(0.01f, 1f)] [SerializeField] private float healPercent = 0.10f;

    [Header("Visual & Animation")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite customSprite;
    [SerializeField] private float visualWorldSize = 0.45f;
    [SerializeField] private float idleFloatSpeed = 3f;
    [SerializeField] private float idleFloatAmount = 0.08f;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseAmount = 0.06f;

    [Header("Pickup & Magnet")]
    [SerializeField] private float triggerRadius = 0.35f;
    [SerializeField] private float pickupRadius = 0.8f;
    [SerializeField] private float magnetAttractionSpeed = 14f;

    private static Sprite cachedSmallSprite;
    private static Sprite cachedLargeSprite;

    private Vector3 initialSpawnPosition;
    private Vector3 visualBaseScale = Vector3.one;
    private Transform playerTarget;
    private bool isBeingAttracted = false;
    private float currentSpeed = 0f;
    private float spawnTime;
    private bool isCollected = false;

    public HealthBoxType BoxType => boxType;
    public float HealPercent => healPercent;
    public bool IsBeingAttracted => isBeingAttracted;
    public bool IsCollected => isCollected;
    public Sprite CurrentSprite => spriteRenderer != null ? spriteRenderer.sprite : null;

    private void OnEnable()
    {
        if (!activeBoxes.Contains(this))
        {
            activeBoxes.Add(this);
        }
        spawnTime = Time.time;
        initialSpawnPosition = transform.position;
        currentSpeed = 0f;
        isBeingAttracted = false;
        isCollected = false;
    }

    private void OnDisable()
    {
        activeBoxes.Remove(this);
        isBeingAttracted = false;
        playerTarget = null;
    }

    private void OnDestroy()
    {
        activeBoxes.Remove(this);
    }

    private void Awake()
    {
        initialSpawnPosition = transform.position;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            col.radius = triggerRadius;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        }

        EnsureVisual();
    }

    private void Start()
    {
        if (initialSpawnPosition == Vector3.zero && transform.position != Vector3.zero)
        {
            initialSpawnPosition = transform.position;
        }
    }

    public void Initialize(HealthBoxType type, Vector3 spawnPosition)
    {
        boxType = type;
        initialSpawnPosition = spawnPosition;
        transform.position = spawnPosition;
        isBeingAttracted = false;
        playerTarget = null;
        currentSpeed = 0f;
        isCollected = false;

        if (boxType == HealthBoxType.Small)
        {
            healPercent = 0.10f; // 10% Max HP
            visualWorldSize = 0.30f;
            triggerRadius = 0.35f;
        }
        else
        {
            healPercent = 0.20f; // 20% Max HP
            visualWorldSize = 0.433f;
            triggerRadius = 0.45f;
        }

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.radius = triggerRadius;
        }

        EnsureVisual(true);
    }

    public void SetSpawnPosition(Vector3 position)
    {
        initialSpawnPosition = position;
        transform.position = position;
    }

    public void TriggerMagnetAttraction(Transform target)
    {
        if (target == null || isCollected) return;
        playerTarget = target;
        isBeingAttracted = true;
        currentSpeed = magnetAttractionSpeed * 0.5f;
    }

    private void Update()
    {
        if (isCollected) return;

        if (initialSpawnPosition == Vector3.zero && transform.position != Vector3.zero)
        {
            initialSpawnPosition = transform.position;
        }

        if (isBeingAttracted && playerTarget != null)
        {
            currentSpeed = Mathf.Min(currentSpeed + magnetAttractionSpeed * 2.5f * Time.deltaTime, magnetAttractionSpeed * 2f);
            Vector3 dir = (playerTarget.position - transform.position).normalized;
            transform.position += dir * (currentSpeed * Time.deltaTime);

            float distSqr = (playerTarget.position - transform.position).sqrMagnitude;
            if (distSqr <= pickupRadius * pickupRadius)
            {
                Collect(playerTarget.gameObject);
            }
        }
        else
        {
            // Diễn hoạt bay dập dềnh nhẹ (Idle floating bobbing)
            float offset = Mathf.Sin((Time.time - spawnTime) * idleFloatSpeed) * idleFloatAmount;
            transform.position = initialSpawnPosition + new Vector3(0f, offset, 0f);

            // Nhịp thở kích thước nhẹ (Pulse animation)
            if (spriteRenderer != null && spriteRenderer.transform != transform)
            {
                float pulse01 = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed);
                float pulseScale = 1f + pulse01 * pulseAmount;
                spriteRenderer.transform.localScale = visualBaseScale * pulseScale;
            }

            // Kiểm tra khoảng cách trực tiếp đến Player
            if (playerTarget == null)
            {
                GameObject pObj = GameObject.FindGameObjectWithTag("Player");
                if (pObj != null) playerTarget = pObj.transform;
                else
                {
                    PlayerMovement pm = FindObjectOfType<PlayerMovement>();
                    if (pm != null) playerTarget = pm.transform;
                }
            }

            if (playerTarget != null)
            {
                float dist = Vector2.Distance(transform.position, playerTarget.position);
                if (dist <= triggerRadius)
                {
                    Collect(playerTarget.gameObject);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerHealth>() != null || other.GetComponentInParent<PlayerMovement>() != null)
        {
            Collect(other.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isCollected) return;

        if (collision.collider.CompareTag("Player") || collision.collider.GetComponentInParent<PlayerHealth>() != null || collision.collider.GetComponentInParent<PlayerMovement>() != null)
        {
            Collect(collision.collider.gameObject);
        }
    }

    public void Collect(GameObject collector = null)
    {
        if (isCollected) return;
        isCollected = true;

        PlayerHealth playerHealth = null;
        if (collector != null)
        {
            playerHealth = collector.GetComponent<PlayerHealth>() ?? collector.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null)
            {
                playerHealth = pObj.GetComponent<PlayerHealth>() ?? pObj.GetComponentInParent<PlayerHealth>();
            }
            else
            {
                playerHealth = FindObjectOfType<PlayerHealth>();
            }
        }

        if (playerHealth != null && !playerHealth.IsDead)
        {
            int healAmount = Mathf.Max(1, Mathf.RoundToInt(playerHealth.MaxHealth * healPercent));
            playerHealth.Heal(healAmount);
            AudioManager.Instance?.PlaySFX(SoundIdConst.SFX_PICKUP_ITEM);
        }

        // Tắt collider để tránh kích hoạt lặp lại
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Despawn();
    }

    private void Despawn()
    {
        Destroy(gameObject);
    }

    public void EnsureVisual(bool forceUpdateSprite = false)
    {
        if (spriteRenderer == null)
        {
            GameObject visualObj = new GameObject("BoxVisual", typeof(SpriteRenderer));
            visualObj.transform.SetParent(transform, false);
            spriteRenderer = visualObj.GetComponent<SpriteRenderer>();
        }

        if (forceUpdateSprite || spriteRenderer.sprite == null)
        {
            Sprite targetSprite = GetBoxSprite(boxType);
            if (targetSprite != null)
            {
                spriteRenderer.sprite = targetSprite;
            }
            else
            {
                spriteRenderer.sprite = CreateProceduralFallbackSprite(boxType);
            }
        }

        FitVisualToWorldSize();

        spriteRenderer.color = Color.white;
        spriteRenderer.enabled = true;

        string targetSortingLayer = "UI";
        if (SortingLayer.NameToID("UI") == 0)
        {
            targetSortingLayer = SortingLayer.NameToID("VFX ") != 0 ? "VFX " : "Player";
        }
        spriteRenderer.sortingLayerName = targetSortingLayer;
        spriteRenderer.sortingOrder = 90;
    }

    public static Sprite GetBoxSprite(HealthBoxType type)
    {
        if (type == HealthBoxType.Small)
        {
            if (cachedSmallSprite != null) return cachedSmallSprite;
            cachedSmallSprite = Resources.Load<Sprite>("UI/HealthBox/hop_mau_nho");
#if UNITY_EDITOR
            if (cachedSmallSprite == null)
            {
                cachedSmallSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/artifact 1/hop_mau_nho.png");
            }
#endif
            return cachedSmallSprite;
        }
        else
        {
            if (cachedLargeSprite != null) return cachedLargeSprite;
            cachedLargeSprite = Resources.Load<Sprite>("UI/HealthBox/hop_mau_lon");
#if UNITY_EDITOR
            if (cachedLargeSprite == null)
            {
                cachedLargeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/artifact 1/hop_mau_lon.png");
            }
#endif
            return cachedLargeSprite;
        }
    }

    private void FitVisualToWorldSize()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        if (spriteRenderer.transform == transform)
        {
            visualBaseScale = spriteRenderer.transform.localScale;
            return;
        }

        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
        float longestSide = Mathf.Max(spriteSize.x, spriteSize.y);
        float scale = longestSide > 0f ? visualWorldSize / longestSide : 1f;
        visualBaseScale = Vector3.one * scale;
        spriteRenderer.transform.localScale = visualBaseScale;
    }

    private static Sprite CreateProceduralFallbackSprite(HealthBoxType type)
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color32 boxColor = type == HealthBoxType.Small 
            ? new Color32(240, 240, 245, 255)  // Trắng xám cho hộp nhỏ
            : new Color32(160, 190, 40, 255);   // Vàng xanh lính cho hộp lớn
        Color32 borderColor = new Color32(30, 30, 30, 255);
        Color32 crossRed = new Color32(230, 30, 30, 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Bo viền
                if (x < 2 || x >= size - 2 || y < 2 || y >= size - 2)
                {
                    tex.SetPixel(x, y, borderColor);
                }
                // Dấu cộng đỏ ở giữa
                else if ((Mathf.Abs(x - size / 2) <= 2 && Mathf.Abs(y - size / 2) <= 7) ||
                         (Mathf.Abs(y - size / 2) <= 2 && Mathf.Abs(x - size / 2) <= 7))
                {
                    tex.SetPixel(x, y, crossRed);
                }
                else
                {
                    tex.SetPixel(x, y, boxColor);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }
}
