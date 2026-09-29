using UnityEngine;

public class EnemyContactDamage : MonoBehaviour, IPoolable
{
    private static int obstacleMask;
    private static bool obstacleMaskInitialized;

    [Header("Damage")]
    [Tooltip("Lượng sát thương gây ra khi quái vật va chạm vào Player.")]
    [SerializeField] private int damage = 10;

    private int baseDamage;
    private PlayerHealth contactPlayer;
    private int playerContactCount;

    public int Damage => damage;
    public int BaseDamage => baseDamage > 0 ? baseDamage : damage;

    private void Awake()
    {
        baseDamage = damage;
        if (!obstacleMaskInitialized)
        {
            obstacleMask = LayerMask.GetMask("Obstacle");
            obstacleMaskInitialized = true;
        }
    }

    public void SetDamage(int newDamage)
    {
        damage = Mathf.Max(1, newDamage);
    }

    public void OnSpawnFromPool()
    {
        damage = BaseDamage;
        nextDamageTime = 0f;
        contactPlayer = null;
        playerContactCount = 0;
    }

    public void OnReturnToPool()
    {
        damage = BaseDamage;
        nextDamageTime = 0f;
        contactPlayer = null;
        playerContactCount = 0;
    }

    [Header("Attack Cooldown")]
    [Tooltip("Khoảng thời gian tối thiểu giữa các lần gây sát thương va chạm (giây).")]
    [SerializeField] private float damageInterval = 1f;

    private float nextDamageTime;

    private void Update()
    {
        if (playerContactCount > 0 && Time.time >= nextDamageTime)
            TryDamage(contactPlayer);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        RegisterContact(collision.collider);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        UnregisterContact(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        RegisterContact(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        UnregisterContact(other);
    }

    private void OnDisable()
    {
        contactPlayer = null;
        playerContactCount = 0;
    }

    private void RegisterContact(Collider2D targetCollider)
    {
        if (targetCollider == null || !targetCollider.CompareTag("Player")) return;

        PlayerHealth playerHealth = targetCollider.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) return;

        contactPlayer = playerHealth;
        playerContactCount++;
        TryDamage(playerHealth);
    }

    private void UnregisterContact(Collider2D targetCollider)
    {
        if (targetCollider == null || !targetCollider.CompareTag("Player") || playerContactCount == 0) return;
        if (targetCollider.GetComponentInParent<PlayerHealth>() != contactPlayer) return;

        playerContactCount--;
        if (playerContactCount == 0) contactPlayer = null;
    }

    private void TryDamage(PlayerHealth playerHealth)
    {
        if (playerHealth == null || playerHealth.IsDead || Time.time < nextDamageTime) return;

        // Kiểm tra Line of Sight: Không gây sát thương qua vách chướng ngại vật
        if (obstacleMask != 0 && Physics2D.Linecast(transform.position, playerHealth.transform.position, obstacleMask))
        {
            nextDamageTime = Time.time + 0.1f;
            return;
        }

        playerHealth.TakeDamage(damage);

        nextDamageTime = Time.time + damageInterval;
    }
}
