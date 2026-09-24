using UnityEngine;

/// <summary>
/// Đạn pháo bắn từ Turret về phía quái vật trong chế độ Tower Def.
/// </summary>
public class TowerDefProjectile : MonoBehaviour
{
    private TowerDefEnemy target;
    private float damage;
    private float speed = 1200f; // pixel / s
    private Canvas owningCanvas;
    private Camera viewCamera;
    private float elapsed;

    public void Setup(TowerDefEnemy targetEnemy, float projectileDamage, float projectileSpeed = 1200f)
    {
        target = targetEnemy;
        damage = projectileDamage;
        speed = projectileSpeed;
        owningCanvas = GetComponentInParent<Canvas>();
        viewCamera = owningCanvas != null ? owningCanvas.worldCamera : Camera.main;
        elapsed = 0f;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed > 3f)
        {
            Destroy(gameObject);
            return;
        }
        if (target == null || target.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        if (viewCamera != null)
        {
            Vector3 viewport = viewCamera.WorldToViewportPoint(transform.position);
            if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f ||
                viewport.y < 0f || viewport.y > 1f)
            {
                Destroy(gameObject);
                return;
            }
        }

        Vector3 targetPos = target.transform.position;
        targetPos.z = transform.position.z; // Keep the prefab in front of the camera-space Canvas.
        Vector3 dir = (targetPos - transform.position).normalized;
        float step = speed * (owningCanvas != null ? owningCanvas.transform.lossyScale.x : 1f) * Time.deltaTime;

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
