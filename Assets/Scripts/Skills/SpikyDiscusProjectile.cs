using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đĩa Gai (Spiky Discus Projectile - Chipset ID 7).
/// Bay xoay tròn quanh Player, tự xoay tít quanh trục của chính nó, chém quái vật liên tục,
/// gây hiệu ứng Chảy máu (Cấp 3+) và chém bay đạn kẻ địch (Cấp 5 Tối thượng).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SpikyDiscusProjectile : MonoBehaviour, IPoolable
{
    [Header("Damage Settings")]
    [SerializeField] private int damage = 30;
    [SerializeField] private float hitCooldownPerEnemy = 0.35f;

    [Header("Special Effects")]
    [SerializeField] private int bleedDps = 0;
    [SerializeField] private float bleedDuration = 0f;
    [SerializeField] private bool canDestroyEnemyBullets = false;

    [Header("Self Spin")]
    [SerializeField] private float selfSpinSpeed = 540f;

    [Header("VFX")]
    [SerializeField] private GameObject hitVfxPrefab;

    private readonly Dictionary<int, float> lastHitTimePerEnemy = new Dictionary<int, float>();

    public int Damage => damage;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        if (hitVfxPrefab == null)
        {
            hitVfxPrefab = Resources.Load<GameObject>("Prefabs/VFX Boom");
#if UNITY_EDITOR
            if (hitVfxPrefab == null)
            {
                hitVfxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX Boom.prefab");
            }
#endif
        }
    }

    private void Update()
    {
        // Tự xoay tít quanh trục của đĩa
        transform.Rotate(0f, 0f, selfSpinSpeed * Time.deltaTime);
    }

    public void Setup(
        int damageAmount,
        int bleedDamagePerSec,
        float bleedDur,
        bool destroyBullets,
        float spinSpeed,
        GameObject vfxPrefab = null)
    {
        damage = damageAmount;
        bleedDps = bleedDamagePerSec;
        bleedDuration = bleedDur;
        canDestroyEnemyBullets = destroyBullets;
        selfSpinSpeed = spinSpeed;

        if (vfxPrefab != null)
        {
            hitVfxPrefab = vfxPrefab;
        }

        lastHitTimePerEnemy.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        HandleCollision(other);
    }

    private void HandleCollision(Collider2D other)
    {
        if (other == null) return;
        if (other.CompareTag("Player") || other.CompareTag("BulletPlayer")) return;

        // 1. Chém bay đạn kẻ địch (Cấp 5 Tối thượng)
        if (canDestroyEnemyBullets)
        {
            EnemyProjectile enemyBullet = other.GetComponent<EnemyProjectile>() ?? other.GetComponentInParent<EnemyProjectile>();
            if (enemyBullet != null)
            {
                SpawnHitVFX(enemyBullet.transform.position);
                PoolMember pm = enemyBullet.GetComponent<PoolMember>();
                if (pm != null && pm.Pool != null)
                {
                    pm.ReturnToPool();
                }
                else
                {
                    Destroy(enemyBullet.gameObject);
                }
                return;
            }
        }

        // 2. Chém quái vật gây sát thương và Chảy máu
        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy != null && !enemy.IsDead && enemy.gameObject.activeInHierarchy)
        {
            int enemyId = enemy.gameObject.GetInstanceID();
            if (lastHitTimePerEnemy.TryGetValue(enemyId, out float lastHit))
            {
                if (Time.time - lastHit < hitCooldownPerEnemy)
                {
                    return;
                }
            }

            lastHitTimePerEnemy[enemyId] = Time.time;

            enemy.TakeDamage(damage);
            ChipsetBattleStats.RecordDamage(7, damage);
            EnergyJumperCablesSkill.TriggerLifeSteal(damage, false);

            if (bleedDps > 0 && bleedDuration > 0f)
            {
                enemy.ApplyBleed(bleedDps, bleedDuration);
            }

            SpawnHitVFX(other.transform.position);
        }
    }

    private void SpawnHitVFX(Vector3 pos)
    {
        if (hitVfxPrefab == null) return;

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Spawn(hitVfxPrefab, pos, Quaternion.identity);
        }
        else
        {
            Instantiate(hitVfxPrefab, pos, Quaternion.identity);
        }
    }

    public void OnSpawnFromPool()
    {
        lastHitTimePerEnemy.Clear();
    }

    public void OnReturnToPool()
    {
        lastHitTimePerEnemy.Clear();
        canDestroyEnemyBullets = false;
        bleedDps = 0;
        bleedDuration = 0f;
    }
}
