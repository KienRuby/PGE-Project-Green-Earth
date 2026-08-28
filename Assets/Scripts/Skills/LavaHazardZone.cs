using UnityEngine;

/// <summary>
/// Vùng dung nham nóng chảy xuất hiện tại tâm vụ nổ Rocket Punch Cấp 5 (Tối thượng).
/// Tồn tại trong 3 giây và liên tục thiêu đốt (gây sát thương định kỳ) tất cả quái vật bước qua.
/// </summary>
public class LavaHazardZone : MonoBehaviour, IPoolable
{
    [Header("Lava Settings")]
    [Tooltip("Thời gian tồn tại của vùng dung nham (giây).")]
    [SerializeField] private float duration = 3.0f;

    [Tooltip("Bán kính vùng thiêu đốt dung nham (mét).")]
    [SerializeField] private float radius = 2.5f;

    [Tooltip("Sát thương mỗi lần thiêu đốt (Tick Damage).")]
    [SerializeField] private int tickDamage = 35;

    [Tooltip("Khoảng cách giữa các lần gây sát thương (giây).")]
    [SerializeField] private float tickInterval = 0.5f;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer lavaSpriteRenderer;

    private float lifeTimer;
    private float nextTickTime;
    private LayerMask enemyLayer;
    private ContactFilter2D contactFilter;
    private readonly Collider2D[] hitBuffer = new Collider2D[32];
    private bool isInitialized = false;

    private void Awake()
    {
        if (lavaSpriteRenderer == null)
        {
            lavaSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (enemyLayer.value == 0)
        {
            enemyLayer = LayerMask.GetMask("Enemy");
            if (enemyLayer.value == 0) enemyLayer = 1 << 7;
        }

        contactFilter = new ContactFilter2D
        {
            layerMask = enemyLayer,
            useLayerMask = enemyLayer.value != 0,
            useTriggers = true
        };

        SetupVisuals();
    }

    private void SetupVisuals()
    {
        if (lavaSpriteRenderer == null)
        {
            lavaSpriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Tạo sprite vòng tròn dung nham cam rực lửa nếu chưa có sprite
        if (lavaSpriteRenderer.sprite == null)
        {
            Texture2D tex = CreateCircleTexture(64, new Color(1f, 0.35f, 0.05f, 0.65f), new Color(1f, 0.15f, 0f, 0.9f));
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32);
            lavaSpriteRenderer.sprite = sprite;
        }

        lavaSpriteRenderer.sortingOrder = -5; // Hiển thị dưới đất
    }

    private Texture2D CreateCircleTexture(int size, Color innerColor, Color edgeColor)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radiusPixel = size * 0.5f;
        Vector2 center = new Vector2(radiusPixel, radiusPixel);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radiusPixel)
                {
                    float t = dist / radiusPixel;
                    Color c = Color.Lerp(innerColor, edgeColor, t);
                    // Làm mờ ở viền ngoài
                    if (t > 0.8f) c.a *= (1f - (t - 0.8f) / 0.2f);
                    tex.SetPixel(x, y, c);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    public void Initialize(int damagePerTick, float effectRadius, float zoneDuration = 3.0f)
    {
        tickDamage = Mathf.Max(1, damagePerTick);
        radius = Mathf.Max(0.5f, effectRadius);
        duration = Mathf.Max(0.5f, zoneDuration);

        lifeTimer = duration;
        nextTickTime = Time.time + tickInterval;
        transform.localScale = Vector3.one * (radius * 0.8f);
        isInitialized = true;

        if (lavaSpriteRenderer != null)
        {
            lavaSpriteRenderer.color = new Color(1f, 1f, 1f, 0.8f);
        }
    }

    private void OnEnable()
    {
        if (!isInitialized)
        {
            lifeTimer = duration;
            nextTickTime = Time.time + tickInterval;
        }
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Despawn();
            return;
        }

        // Hiệu ứng mờ dần khi sắp hết 3 giây
        if (lavaSpriteRenderer != null && lifeTimer < 0.8f)
        {
            float alpha = Mathf.Clamp01(lifeTimer / 0.8f) * 0.8f;
            Color c = lavaSpriteRenderer.color;
            c.a = alpha;
            lavaSpriteRenderer.color = c;
        }

        // Thiêu đốt định kỳ
        if (Time.time >= nextTickTime)
        {
            BurnEnemies();
            nextTickTime = Time.time + tickInterval;
        }
    }

    private void BurnEnemies()
    {
        Vector2 center = transform.position;
        int hitCount = Physics2D.OverlapCircle(center, radius, contactFilter, hitBuffer);

        if (hitCount == 0 && enemyLayer.value != 0)
        {
            ContactFilter2D fallback = new ContactFilter2D { useTriggers = true };
            hitCount = Physics2D.OverlapCircle(center, radius, fallback, hitBuffer);
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = hitBuffer[i];
            if (col == null) continue;

            EnemyHealth health = col.GetComponentInParent<EnemyHealth>();
            if (health != null && !health.IsDead && health.gameObject.activeInHierarchy)
            {
                health.TakeDamage(tickDamage);
                ChipsetBattleStats.RecordDamage(3, tickDamage);
            }
        }
    }

    private void Despawn()
    {
        isInitialized = false;
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnSpawnFromPool()
    {
        lifeTimer = duration;
        nextTickTime = Time.time + tickInterval;
        if (lavaSpriteRenderer != null)
        {
            lavaSpriteRenderer.color = new Color(1f, 1f, 1f, 0.8f);
        }
    }

    public void OnReturnToPool()
    {
        isInitialized = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
