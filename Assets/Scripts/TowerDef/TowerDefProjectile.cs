using UnityEngine;

/// <summary>
/// Đạn pháo bắn từ Turret về phía quái vật trong chế độ Tower Def.
/// </summary>
public class TowerDefProjectile : MonoBehaviour
{
    private TowerDefEnemy target;
    private float damage;
    private float speed = 1200f; // pixel / s

    public void Setup(TowerDefEnemy targetEnemy, float projectileDamage, float projectileSpeed = 1200f)
    {
        target = targetEnemy;
        damage = projectileDamage;
        speed = projectileSpeed;
    }

    private void Update()
    {
        if (target == null || target.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPos = target.transform.position;
        Vector3 dir = (targetPos - transform.position).normalized;
        float step = speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, targetPos) <= step)
        {
            target.TakeDamage(damage);
            Destroy(gameObject);
        }
        else
        {
            transform.position += dir * step;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
