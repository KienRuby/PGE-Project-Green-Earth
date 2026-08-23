using UnityEngine;

public class EnemyContactDamage : MonoBehaviour, IPoolable
{
    [Header("Damage")]
    [Tooltip("Lượng sát thương gây ra khi quái vật va chạm vào Player.")]
    [SerializeField] private int damage = 10;

    private int baseDamage;

    public int Damage => damage;
    public int BaseDamage => baseDamage > 0 ? baseDamage : damage;

    private void Awake()
    {
        baseDamage = damage;
    }

    public void SetDamage(int newDamage)
    {
        damage = Mathf.Max(1, newDamage);
    }

    public void OnSpawnFromPool()
    {
        damage = BaseDamage;
        nextDamageTime = 0f;
    }

    public void OnReturnToPool()
    {
        damage = BaseDamage;
        nextDamageTime = 0f;
    }

    [Header("Attack Cooldown")]
    [Tooltip("Khoảng thời gian tối thiểu giữa các lần gây sát thương va chạm (giây).")]
    [SerializeField] private float damageInterval = 1f;

    private float nextDamageTime;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamage(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamage(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider2D targetCollider)
    {
        if (targetCollider == null || Time.time < nextDamageTime)
            return;

        // Fast path: Nếu không phải Player thì bỏ qua ngay lập tức
        if (!targetCollider.CompareTag("Player"))
            return;

        PlayerHealth playerHealth = targetCollider.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (playerHealth.IsDead)
            return;

        playerHealth.TakeDamage(damage);

        nextDamageTime = Time.time + damageInterval;
    }
}