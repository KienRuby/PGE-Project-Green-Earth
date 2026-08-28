using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quả Mìn Nổ (High-Explosive Mine - Chipset ID 10).
/// Đặt cố định trên mặt đất. Kích nổ khi quái vật chạm vào hoặc kích nổ hẹn giờ (đối với mìn con).
/// Gây sát thương AoE, Làm chậm 40% trong 2s (Cấp 3+), và Nổ văng 3 mìn con (Cấp 5 Tối thượng).
/// </summary>
public class HighExplosiveMine : MonoBehaviour, IPoolable
{
    [Header("Mine Base Settings")]
    [SerializeField] private int damage = 30;
    [SerializeField] private float explosionRadius = 2.0f;
    [SerializeField] private float armingDelay = 0.2f;
    [SerializeField] private float maxLifeTime = 60f;

    [Header("Special Effects")]
    [SerializeField] private float slowDuration = 0f;
    [SerializeField] private float slowPercent = 0.40f;
    [SerializeField] private bool canSpawnSubMines = false;
    [SerializeField] private bool isSubMine = false;
    [SerializeField] private float subMineFuseTime = 0.4f;

    [Header("VFX References")]
    [SerializeField] private GameObject explosionVfxPrefab;
    [SerializeField] private GameObject subMinePrefab;

    private bool isArmed = false;
    private bool hasExploded = false;
    private float lifeTimer;
    private readonly Collider2D[] hitBuffer = new Collider2D[64];

    public int Damage => damage;
    public float ExplosionRadius => explosionRadius;

    private void Awake()
    {
        if (explosionVfxPrefab == null)
        {
            explosionVfxPrefab = Resources.Load<GameObject>("Prefabs/VFX Boom");
#if UNITY_EDITOR
            if (explosionVfxPrefab == null)
            {
                explosionVfxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX Boom.prefab");
            }
#endif
        }
    }

    private void OnEnable()
    {
        isArmed = false;
        hasExploded = false;
        lifeTimer = maxLifeTime;

        if (isSubMine)
        {
            StartCoroutine(SubMineFuseRoutine());
        }
        else
        {
            StartCoroutine(ArmingRoutine());
        }
    }

    private IEnumerator ArmingRoutine()
    {
        yield return new WaitForSeconds(armingDelay);
        isArmed = true;
    }

    private IEnumerator SubMineFuseRoutine()
    {
        yield return new WaitForSeconds(subMineFuseTime);
        Explode();
    }

    private void Update()
    {
        if (hasExploded) return;

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Despawn();
        }
    }

    public void Setup(
        int damageAmount,
        float radius,
        float slowDur,
        bool spawnSubMinesOnDeath,
        GameObject explosionVfx = null,
        GameObject subPrefab = null)
    {
        damage = damageAmount;
        explosionRadius = radius;
        slowDuration = slowDur;
        canSpawnSubMines = spawnSubMinesOnDeath;
        isSubMine = false;

        if (explosionVfx != null) explosionVfxPrefab = explosionVfx;
        if (subPrefab != null) subMinePrefab = subPrefab;
    }

    public void SetupAsSubMine(
        int damageAmount,
        float radius,
        float slowDur,
        float fuseTime,
        GameObject explosionVfx = null)
    {
        damage = damageAmount;
        explosionRadius = radius;
        slowDuration = slowDur;
        canSpawnSubMines = false;
        isSubMine = true;
        subMineFuseTime = fuseTime;

        if (explosionVfx != null) explosionVfxPrefab = explosionVfx;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isArmed || hasExploded || isSubMine) return;
        if (other == null) return;

        if (other.CompareTag("Player") || other.CompareTag("BulletPlayer"))
        {
            return;
        }

        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy != null && !enemy.IsDead && enemy.gameObject.activeInHierarchy)
        {
            Explode();
        }
    }

    public void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        Vector3 explosionPos = transform.position;

        // 1. Sinh hiệu ứng nổ VFX Boom
        SpawnExplosionVFX(explosionPos);

        // 2. Gây sát thương AoE diện rộng và Làm chậm
        int hitCount = Physics2D.OverlapCircleNonAlloc(explosionPos, explosionRadius, hitBuffer);
        var hitEnemies = new HashSet<int>();

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = hitBuffer[i];
            if (col == null) continue;
            if (col.CompareTag("Player") || col.CompareTag("BulletPlayer")) continue;

            EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy) continue;

            int id = enemy.gameObject.GetInstanceID();
            if (hitEnemies.Contains(id)) continue;
            hitEnemies.Add(id);

            enemy.TakeDamage(damage);
            ChipsetBattleStats.RecordDamage(10, damage);

            // Hiệu ứng làm chậm (Cấp 3+)
            if (slowDuration > 0.01f)
            {
                EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
                if (movement != null)
                {
                    movement.ApplySlow(slowPercent, slowDuration);
                }
            }
        }

        // 3. Tối thượng Cấp 5: Mìn mẹ nổ văng ra 3 mìn con
        if (canSpawnSubMines)
        {
            SpawnSubMines(explosionPos);
        }

        Despawn();
    }

    private void SpawnExplosionVFX(Vector3 pos)
    {
        if (explosionVfxPrefab == null) return;

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Spawn(explosionVfxPrefab, pos, Quaternion.identity);
        }
        else
        {
            Instantiate(explosionVfxPrefab, pos, Quaternion.identity);
        }
    }

    private void SpawnSubMines(Vector3 centerPos)
    {
        GameObject prefabToUse = subMinePrefab != null ? subMinePrefab : gameObject;
        float scatterDist = 1.4f;
        int subDamage = Mathf.RoundToInt(damage * 0.65f);
        float subRadius = explosionRadius * 0.75f;

        float[] angles = new float[] { 30f, 150f, 270f };
        for (int i = 0; i < 3; i++)
        {
            float rad = (angles[i] + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad) * scatterDist, Mathf.Sin(rad) * scatterDist, 0f);
            Vector3 subPos = centerPos + offset;

            GameObject subObj = null;
            if (PoolManager.Instance != null)
            {
                subObj = PoolManager.Instance.Spawn(prefabToUse, subPos, Quaternion.identity);
            }
            else
            {
                subObj = Instantiate(prefabToUse, subPos, Quaternion.identity);
            }

            if (subObj != null)
            {
                // Thu nhỏ kích thước mìn con
                subObj.transform.localScale = transform.localScale * 0.75f;
                HighExplosiveMine subMineScript = subObj.GetComponent<HighExplosiveMine>();
                if (subMineScript != null)
                {
                    subMineScript.SetupAsSubMine(subDamage, subRadius, slowDuration, 0.35f + (i * 0.08f), explosionVfxPrefab);
                }
            }
        }
    }

    private void Despawn()
    {
        PoolMember member = GetComponent<PoolMember>();
        if (member != null && member.Pool != null)
        {
            member.ReturnToPool();
        }
        else if (PoolManager.Instance != null)
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
        isArmed = false;
        hasExploded = false;
        lifeTimer = maxLifeTime;
    }

    public void OnReturnToPool()
    {
        isArmed = false;
        hasExploded = false;
        canSpawnSubMines = false;
        isSubMine = false;
    }
}
