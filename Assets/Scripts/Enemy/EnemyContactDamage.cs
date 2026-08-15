using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("Lượng sát thương gây ra khi quái vật va chạm vào Player.")]
    [SerializeField] private int damage = 10;

    [Header("Attack Cooldown")]
    [Tooltip("Khoảng thời gian tối thiểu giữa các lần gây sát thương va chạm (giây).")]
    [SerializeField] private float damageInterval = 1f;

    private float nextDamageTime;

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamage(collision.collider);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider2D targetCollider)
    {
        if (Time.time < nextDamageTime)
            return;

        PlayerHealth playerHealth =
            targetCollider.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (playerHealth.IsDead)
            return;

        playerHealth.TakeDamage(damage);

        nextDamageTime =
            Time.time + damageInterval;
    }
}