using UnityEngine;

/// <summary>
/// Lưỡi Dao Xoay bay liên tục theo quỹ đạo hình tròn xung quanh Player.
/// - Không tự biến mất khi chưa chạm quái.
/// - Khi va chạm với quái vật: Gây sát thương và biến mất (hoặc đâm xuyên nhiều lần / tạo lốc xoáy ở Cấp 5).
/// - Tự xoay quanh trục Z và nhận vị trí quỹ đạo được dàn đều mượt mà từ SpinningBladeSkill.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SpinningBladeProjectile : MonoBehaviour, IPoolable
{
    [Header("Combat Stats")]
    [Tooltip("Sát thương mỗi lần chém trúng quái vật.")]
    [SerializeField] private int damage = 36;

    [Tooltip("Số lần chém quái trước khi biến mất (1 = chạm 1 quái rồi biến mất, >1 = đâm xuyên nhiều quái).")]
    [SerializeField] private int remainingHits = 1;

    [Tooltip("Tạo lốc xoáy dừng lại xoay tại chỗ khi va chạm (Cấp 5 Tối thượng).")]
    [SerializeField] private bool hasVortex = false;

    [Tooltip("Thời gian tồn tại của lốc xoáy khi kích hoạt (giây).")]
    [SerializeField] private float vortexDuration = 2.0f;

    [Tooltip("Bán kính lốc xoáy thiêu đốt (mét).")]
    [SerializeField] private float vortexRadius = 2.0f;

    [Header("Self Spin (Tự xoay của lưỡi dao)")]
    [Tooltip("Tốc độ tự xoay tròn của lưỡi dao quanh trục chính nó (độ/giây). Càng nhỏ xoay càng chậm.")]
    [Range(120f, 1440f)]
    [SerializeField] private float selfSpinSpeed = 480f;

    [Header("VFX & Rendering")]
    [SerializeField] private GameObject hitVfxPrefab;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;
    private float currentSelfSpinAngle = 0f;
    private bool isDestroyed = false;
    private bool isInVortexMode = false;
    private float vortexTimer = 0f;
    private float nextVortexTickTime = 0f;

    private System.Action<SpinningBladeProjectile> onDestroyedCallback;

    private LayerMask enemyLayer;
    private ContactFilter2D contactFilter;
    private readonly Collider2D[] vortexBuffer = new Collider2D[32];

    public bool IsInVortexMode => isInVortexMode;
    public bool IsActive => !isDestroyed;
    public float CurrentOrbitAngle { get; set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.isKinematic = true;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (trailRenderer == null)
        {
            trailRenderer = GetComponentInChildren<TrailRenderer>();
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

        FixTrailMaterial();
    }

    private void FixTrailMaterial()
    {
        if (trailRenderer != null)
        {
            if (trailRenderer.sharedMaterial == null || trailRenderer.sharedMaterial.shader == null || trailRenderer.sharedMaterial.shader.name == "Hidden/InternalErrorShader")
            {
                Shader spriteShader = Shader.Find("Sprites/Default")
                    ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                    ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended");

                if (spriteShader != null)
                {
                    trailRenderer.material = new Material(spriteShader);
                }
            }
        }
    }

    /// <summary>
    /// Khởi tạo thông số cho lưỡi dao xoay.
    /// </summary>
    public void Initialize(
        int bladeDamage,
        int hitCount,
        bool vortexEnabled,
        float vortexTime,
        float spinRotSpeed,
        float initialAngle,
        GameObject hitVfx,
        System.Action<SpinningBladeProjectile> onDestroyCallback)
    {
        damage = bladeDamage;
        remainingHits = Mathf.Max(1, hitCount);
        hasVortex = vortexEnabled;
        vortexDuration = Mathf.Max(0.5f, vortexTime);
        selfSpinSpeed = Mathf.Max(120f, spinRotSpeed);
        CurrentOrbitAngle = initialAngle;
        if (hitVfx != null) hitVfxPrefab = hitVfx;
        onDestroyedCallback = onDestroyCallback;

        isDestroyed = false;
        isInVortexMode = false;
        vortexTimer = 0f;

        FixTrailMaterial();

        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }

    /// <summary>
    /// Cập nhật vị trí trên đường tròn quanh Player do SpinningBladeSkill tính toán điều phối.
    /// </summary>
    public void UpdateOrbitPosition(Vector3 targetPosition, float orbitAngle)
    {
        if (isDestroyed || isInVortexMode) return;

        transform.position = targetPosition;

        // Tự xoay tròn quanh trục Z
        currentSelfSpinAngle += selfSpinSpeed * Time.deltaTime;
        if (currentSelfSpinAngle >= 360f) currentSelfSpinAngle -= 360f;
        transform.rotation = Quaternion.Euler(0f, 0f, currentSelfSpinAngle);
    }

    private void Update()
    {
        if (isDestroyed) return;

        // Nếu đang ở trạng thái Lốc Xoáy tại chỗ (Cấp 5)
        if (isInVortexMode)
        {
            currentSelfSpinAngle += selfSpinSpeed * 1.5f * Time.deltaTime;
            if (currentSelfSpinAngle >= 360f) currentSelfSpinAngle -= 360f;
            transform.rotation = Quaternion.Euler(0f, 0f, currentSelfSpinAngle);

            vortexTimer -= Time.deltaTime;
            if (Time.time >= nextVortexTickTime)
            {
                DamageEnemiesInVortex();
                nextVortexTickTime = Time.time + 0.25f;
            }

            if (vortexTimer <= 0f)
            {
                Despawn();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroyed || isInVortexMode || other == null) return;
        if (other.CompareTag("Player") || other.CompareTag("BulletPlayer")) return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            if (damageable is EnemyHealth enemyHealth && enemyHealth.IsDead) return;

            // 1. Gây sát thương lên quái vật va chạm
            damageable.TakeDamage(damage);
            ChipsetBattleStats.RecordDamage(4, damage);
            SpawnHitVfx(other.transform.position);

            remainingHits--;

            // 2. Kiểm tra nếu hết số lần chém -> Biến mất hoặc tạo Lốc Xoáy (Cấp 5)
            if (remainingHits <= 0)
            {
                if (hasVortex)
                {
                    // Chuyển sang chế độ lốc xoáy tại vị trí va chạm
                    isInVortexMode = true;
                    vortexTimer = vortexDuration;
                    nextVortexTickTime = Time.time;
                    // Báo cho Manager loại khỏi danh sách các dao đang xoay quanh Player
                    onDestroyedCallback?.Invoke(this);
                    onDestroyedCallback = null;
                }
                else
                {
                    Despawn();
                }
            }
        }
    }

    private void DamageEnemiesInVortex()
    {
        Vector2 center = transform.position;
        int hitCount = Physics2D.OverlapCircle(center, vortexRadius, contactFilter, vortexBuffer);
        if (hitCount == 0 && enemyLayer.value != 0)
        {
            ContactFilter2D fallback = new ContactFilter2D { useTriggers = true };
            hitCount = Physics2D.OverlapCircle(center, vortexRadius, fallback, vortexBuffer);
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = vortexBuffer[i];
            if (col == null) continue;

            EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy != null && !enemy.IsDead && enemy.gameObject.activeInHierarchy)
            {
                int vortexDamage = Mathf.Max(5, Mathf.RoundToInt(damage * 0.45f));
                enemy.TakeDamage(vortexDamage);
                ChipsetBattleStats.RecordDamage(4, vortexDamage);
                SpawnHitVfx(col.transform.position);
            }
        }
    }

    private void SpawnHitVfx(Vector3 pos)
    {
        if (hitVfxPrefab != null)
        {
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Spawn(hitVfxPrefab, pos, Quaternion.identity);
            }
            else
            {
                Instantiate(hitVfxPrefab, pos, Quaternion.identity);
            }
        }
    }

    private void Despawn()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        onDestroyedCallback?.Invoke(this);
        onDestroyedCallback = null;

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnToPool(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void OnSpawnFromPool()
    {
        isDestroyed = false;
        isInVortexMode = false;
        vortexTimer = 0f;
        FixTrailMaterial();
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }

    public void OnReturnToPool()
    {
        isDestroyed = true;
        isInVortexMode = false;
        onDestroyedCallback = null;
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, vortexRadius);
    }
}
