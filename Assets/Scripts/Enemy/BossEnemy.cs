using System;
using UnityEngine;

/// <summary>
/// Đại diện cho trùm cuối màn chơi (Boss Enemy).
/// Mở rộng từ lớp cơ sở Enemy, tích hợp:
/// - Kỹ năng húc tích lực (Charge Dash) và cảnh báo (Telegraph).
/// - Kỹ năng tấn công tầm xa đa dạng (AimedBurst, Fan, Radial).
/// - Trạng thái Cuồng nộ (Enrage Phase khi máu <= 40%).
/// - Tự động liên kết và cập nhật thanh máu Boss (BossHealthBarUI).
/// </summary>
public class BossEnemy : Enemy
{
    [Header("Boss Subsystems")]
    [SerializeField] private BossMovement bossMovementComponent;
    [SerializeField] private BossRangedAttack bossRangedAttackComponent;

    [Header("Boss Boss Phase & Scaling")]
    [Tooltip("Tiêu đề Boss hiển thị trên UI.")]
    [SerializeField] private string bossTitle = "TERRA CONQUEROR";

    [Tooltip("Ngưỡng kích hoạt cuồng nộ (0.4 = 40%).")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float enrageThreshold = 0.4f;

    public BossMovement BossMovement => bossMovementComponent != null ? bossMovementComponent : (bossMovementComponent = GetComponent<BossMovement>());
    public BossRangedAttack RangedAttack => bossRangedAttackComponent != null ? bossRangedAttackComponent : (bossRangedAttackComponent = GetComponent<BossRangedAttack>());

    public bool IsEnraged => BossMovement != null ? BossMovement.IsEnraged : (CurrentHealth <= MaxHealth * enrageThreshold);
    public string BossTitle => bossTitle;

    public event Action OnBossEnraged;
    public event Action OnBossDefeated;

    private bool hasTriggeredEnragedEvent;

    protected override void Awake()
    {
        base.Awake();
        if (bossMovementComponent == null) bossMovementComponent = GetComponent<BossMovement>();
        if (bossRangedAttackComponent == null) bossRangedAttackComponent = GetComponent<BossRangedAttack>();
    }

    public override void Initialize(Transform target, float healthMultiplier = 1.0f, float damageMultiplier = 1.0f, float speedMultiplier = 1.0f, float expMultiplier = 1.0f)
    {
        base.Initialize(target, healthMultiplier, damageMultiplier, speedMultiplier, expMultiplier);
        hasTriggeredEnragedEvent = false;

        if (BossMovement != null)
        {
            BossMovement.SetTarget(target);
            BossMovement.MoveSpeed = BossMovement.BaseMoveSpeed * Mathf.Max(0.1f, speedMultiplier);
        }

        if (RangedAttack != null)
        {
            RangedAttack.SetTarget(target);
            int scaledDmg = Mathf.RoundToInt(RangedAttack.BaseProjectileDamage * Mathf.Max(0.1f, damageMultiplier));
            RangedAttack.SetProjectileDamage(scaledDmg);
        }
    }

    public override void TakeDamage(int damageAmount, bool isCritical)
    {
        base.TakeDamage(damageAmount, isCritical);

        if (!hasTriggeredEnragedEvent && IsEnraged)
        {
            hasTriggeredEnragedEvent = true;
            OnBossEnraged?.Invoke();
        }
    }

    public override void Die()
    {
        base.Die();
        OnBossDefeated?.Invoke();
    }

    public override void OnSpawnFromPool()
    {
        base.OnSpawnFromPool();
        hasTriggeredEnragedEvent = false;
        if (bossMovementComponent != null) bossMovementComponent.OnSpawnFromPool();
        if (bossRangedAttackComponent != null) bossRangedAttackComponent.OnSpawnFromPool();
    }

    public override void OnReturnToPool()
    {
        base.OnReturnToPool();
        hasTriggeredEnragedEvent = false;
        OnBossEnraged = null;
        OnBossDefeated = null;
    }
}
