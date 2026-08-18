using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour, IPoolable
{
    [Header("Movement")]
    [Tooltip("Tốc độ di chuyển của quái vật khi đuổi theo Player.")]
    [SerializeField] private float moveSpeed = 2f;

    [Tooltip("Khoảng cách tối thiểu với Player (giúp quái dàn hàng bao quanh Player thay vì chui vào trong người chơi).")]
    [SerializeField] private float stoppingDistance = 0.7f;

    [Header("Anti-Stacking / Separation")]
    [Tooltip("Bán kính phát hiện các quái vật khác để tạo lực đẩy tách nhau ra, chống xếp chồng.")]
    [SerializeField] private float separationRadius = 0.9f;

    [Tooltip("Lực đẩy chống xếp chồng (trọng số tách đàn). Càng lớn thì quái càng giữ khoảng cách tốt.")]
    [SerializeField] private float separationWeight = 2.5f;

    [Tooltip("LayerMask của quái vật để quét va chạm tách đàn (tự động gán Layer 'Enemy' nếu để trống).")]
    [SerializeField] private LayerMask enemyLayer;

    private Rigidbody2D rb;
    private Transform player;
    private float nextPlayerSearchTime;

    // Buffer cố định để quét quái xung quanh không tạo rác GC
    private readonly Collider2D[] nearbyCollidersBuffer = new Collider2D[16];
    private ContactFilter2D contactFilter;

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (enemyLayer.value == 0)
        {
            enemyLayer = LayerMask.GetMask("Enemy");
        }

        contactFilter = new ContactFilter2D
        {
            layerMask = enemyLayer,
            useLayerMask = enemyLayer.value != 0,
            useTriggers = true
        };
    }

    private void Start()
    {
        if (player == null)
        {
            FindPlayer();
        }
    }

    public void SetTarget(Transform target)
    {
        player = target;
    }

    private void FindPlayer()
    {
        nextPlayerSearchTime = Time.time + 1.0f;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void FixedUpdate()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            if (Time.time >= nextPlayerSearchTime)
            {
                FindPlayer();
            }
            if (player == null || !player.gameObject.activeInHierarchy)
            {
                if (rb != null) rb.velocity = Vector2.zero;
                return;
            }
        }

        Vector2 playerDirection = CalculatePlayerDirection();
        Vector2 separationForce = CalculateSeparationForce();

        // Kết hợp hướng đuổi Player và lực đẩy chống xếp chồng
        Vector2 finalDirection = playerDirection + separationForce * separationWeight;

        if (finalDirection.sqrMagnitude > 1f)
        {
            finalDirection.Normalize();
        }

        Vector2 newPosition = rb.position + finalDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    /// <summary>
    /// Tính toán hướng di chuyển tới Player và giữ khoảng cách không đè lên Player.
    /// </summary>
    private Vector2 CalculatePlayerDirection()
    {
        if (player == null) return Vector2.zero;

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float distanceToPlayer = toPlayer.magnitude;

        if (distanceToPlayer <= 0.001f)
        {
            // Trùng vị trí 100% -> đẩy ra ngẫu nhiên
            return new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        }

        // Nếu quái đã áp sát quá gần Player (< stoppingDistance), đẩy nhẹ ra để không đè vào lòng Player
        if (distanceToPlayer < stoppingDistance)
        {
            float pushBack = 1f - (distanceToPlayer / stoppingDistance);
            return -toPlayer.normalized * pushBack;
        }

        return toPlayer.normalized;
    }

    /// <summary>
    /// Quét các quái vật xung quanh và tính toán lực đẩy ra xa nhau (Flocking Separation).
    /// </summary>
    private Vector2 CalculateSeparationForce()
    {
        int hitCount = Physics2D.OverlapCircle(
            rb.position,
            separationRadius,
            contactFilter,
            nearbyCollidersBuffer
        );

        // Fallback nếu LayerMask chưa trúng
        if (hitCount == 0 && enemyLayer.value != 0)
        {
            ContactFilter2D fallbackFilter = new ContactFilter2D { useTriggers = true };
            hitCount = Physics2D.OverlapCircle(
                rb.position,
                separationRadius,
                fallbackFilter,
                nearbyCollidersBuffer
            );
        }

        Vector2 separation = Vector2.zero;
        Vector2 myPos = rb.position;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D otherCollider = nearbyCollidersBuffer[i];
            if (otherCollider == null || otherCollider.gameObject == gameObject)
                continue;

            // Bỏ qua nếu collider đó là Player
            if (otherCollider.CompareTag("Player"))
                continue;

            Vector2 otherPos = otherCollider.transform.position;
            Vector2 diff = myPos - otherPos;
            float distance = diff.magnitude;

            if (distance < 0.001f)
            {
                // Nếu 2 quái trùng khít tọa độ, tạo lực đẩy lệch hướng ngẫu nhiên để tách ra ngay
                diff = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * 0.1f;
                distance = diff.magnitude;
            }

            if (distance < separationRadius)
            {
                // Lực đẩy tỷ lệ nghịch với khoảng cách (càng gần đẩy càng mạnh)
                float pushStrength = 1f - (distance / separationRadius);
                separation += diff.normalized * pushStrength;
            }
        }

        return separation;
    }

    public void OnSpawnFromPool()
    {
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }

    public void OnReturnToPool()
    {
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }
}