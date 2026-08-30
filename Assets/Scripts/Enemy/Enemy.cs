using System;
using UnityEngine;

public enum EnemyType
{
    Creep,
    FastCreep,
    TankCreep,
    EliteCreep,
    Boss
}

/// <summary>
/// Lớp cơ sở tổng quát (Base Class) cho toàn bộ quái vật trong Project Green Earth.
/// Tích hợp và đồng bộ hoàn chỉnh giữa EnemyHealth, EnemyMovement, EnemyContactDamage,
/// hiệu ứng HitFlash, hiệu ứng khống chế (Stun, Slow, Bleed, Knockback) và hệ thống rơi Gem/Currency.
/// </summary>
[DisallowMultipleComponent]
public class Enemy : MonoBehaviour, IDamageable, IPoolable
{
    [Header("Enemy Classification")]
    [Tooltip("Phân loại quái vật.")]
    [SerializeField] private EnemyType enemyType = EnemyType.Creep;

    [Tooltip("Tên định danh của quái vật.")]
    [SerializeField] private string enemyName = "Creep";

    [Header("Base Stats")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private int contactDamage = 10;
    [SerializeField] private int expReward = 10;
    [SerializeField] private int dataChipReward = 1;
    [SerializeField] private int redGemReward = 0;
    [Range(0f, 1f)] [SerializeField] private float currencyDropChance = 1.0f;

    [Header("Components Cache")]
    [SerializeField] private EnemyHealth healthComponent;
    [SerializeField] private EnemyMovement movementComponent;
    [SerializeField] private EnemyContactDamage contactDamageComponent;
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    public EnemyType Type => enemyType;
    public string EnemyName => enemyName;

    public EnemyHealth Health => healthComponent != null ? healthComponent : (healthComponent = GetComponent<EnemyHealth>());
    public EnemyMovement Movement => movementComponent != null ? movementComponent : (movementComponent = GetComponent<EnemyMovement>());
    public EnemyContactDamage ContactDamage => contactDamageComponent != null ? contactDamageComponent : (contactDamageComponent = GetComponent<EnemyContactDamage>());

    public bool IsDead => Health != null ? Health.IsDead : isDeadFallback;
    public int CurrentHealth => Health != null ? Health.CurrentHealth : currentHealthFallback;
    public int MaxHealth => Health != null ? Health.MaxHealth : maxHealth;
    public int ExpReward => Health != null ? Health.ExpReward : expReward;
    public int DataChipReward => Health != null ? Health.DataChipReward : dataChipReward;
    public int RedGemReward => Health != null ? Health.RedGemReward : redGemReward;
    public float MoveSpeed => Movement != null ? Movement.MoveSpeed : moveSpeed;
    public int Damage => ContactDamage != null ? ContactDamage.Damage : contactDamage;

    public event Action<int, int> OnHealthChanged;
    public event Action OnEnemyDeath;
    public event Action<Enemy> OnDeath;

    private bool isDeadFallback;
    private int currentHealthFallback;

    protected virtual void Awake()
    {
        CacheRequiredComponents();
        currentHealthFallback = maxHealth;
    }

    public void CacheRequiredComponents()
    {
        if (healthComponent == null) healthComponent = GetComponent<EnemyHealth>();
        if (movementComponent == null) movementComponent = GetComponent<EnemyMovement>();
        if (contactDamageComponent == null) contactDamageComponent = GetComponent<EnemyContactDamage>();
        if (spriteRenderers == null || spriteRenderers.Length == 0) spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (healthComponent != null)
        {
            healthComponent.OnHealthChanged -= ForwardHealthChanged;
            healthComponent.OnHealthChanged += ForwardHealthChanged;
            healthComponent.OnEnemyDeath -= ForwardEnemyDeath;
            healthComponent.OnEnemyDeath += ForwardEnemyDeath;
        }
    }

    private void ForwardHealthChanged(int current, int max)
    {
        OnHealthChanged?.Invoke(current, max);
    }

    private void ForwardEnemyDeath()
    {
        isDeadFallback = true;
        OnEnemyDeath?.Invoke();
        OnDeath?.Invoke(this);
    }

    public virtual void Initialize(Transform target, float healthMultiplier = 1.0f, float damageMultiplier = 1.0f, float speedMultiplier = 1.0f, float expMultiplier = 1.0f)
    {
        CacheRequiredComponents();
        isDeadFallback = false;

        if (Health != null)
        {
            int scaledHp = Mathf.RoundToInt(Health.BaseMaxHealth * Mathf.Max(0.1f, healthMultiplier));
            Health.SetMaxHealth(scaledHp, true);

            int scaledExp = Mathf.RoundToInt(Health.BaseExpReward * Mathf.Max(0f, expMultiplier));
            Health.SetExpReward(scaledExp);
        }
        else
        {
            maxHealth = Mathf.RoundToInt(maxHealth * Mathf.Max(0.1f, healthMultiplier));
            currentHealthFallback = maxHealth;
            expReward = Mathf.RoundToInt(expReward * Mathf.Max(0f, expMultiplier));
        }

        if (Movement != null)
        {
            Movement.SetTarget(target);
            Movement.MoveSpeed = Movement.BaseMoveSpeed * Mathf.Max(0.1f, speedMultiplier);
        }

        if (ContactDamage != null)
        {
            int scaledDmg = Mathf.RoundToInt(ContactDamage.BaseDamage * Mathf.Max(0.1f, damageMultiplier));
            ContactDamage.SetDamage(scaledDmg);
        }
    }

    public virtual void TakeDamage(int damageAmount)
    {
        TakeDamage(damageAmount, false);
    }

    public virtual void TakeDamage(int damageAmount, bool isCritical)
    {
        if (IsDead || damageAmount <= 0) return;

        if (Health != null)
        {
            Health.TakeDamage(damageAmount, isCritical);
        }
        else
        {
            currentHealthFallback -= damageAmount;
            currentHealthFallback = Mathf.Clamp(currentHealthFallback, 0, maxHealth);
            OnHealthChanged?.Invoke(currentHealthFallback, maxHealth);

            if (currentHealthFallback <= 0)
            {
                Die();
            }
        }
    }

    public virtual void ApplyBleed(int damagePerSec, float duration)
    {
        if (Health != null)
        {
            Health.ApplyBleed(damagePerSec, duration);
        }
    }

    public virtual void ApplyStun(float duration)
    {
        if (Movement != null)
        {
            Movement.ApplyStun(duration);
        }
    }

    public virtual void ApplySlow(float slowPercent, float duration)
    {
        if (Movement != null)
        {
            Movement.ApplySlow(slowPercent, duration);
        }
    }

    public virtual void ApplyKnockback(Vector2 direction, float force, float duration = 0.2f)
    {
        if (Movement != null)
        {
            Movement.ApplyKnockback(direction, force, duration);
        }
    }

    public virtual void Die()
    {
        if (isDeadFallback) return;
        isDeadFallback = true;

        if (Health != null && !Health.IsDead)
        {
            Health.TakeDamage(Health.CurrentHealth + 9999);
        }
        else
        {
            if (PlayerLevelController.Instance != null && expReward > 0)
            {
                PlayerLevelController.Instance.AddEXP(expReward);
            }

            if (currencyDropChance >= 1f || UnityEngine.Random.value <= currencyDropChance)
            {
                if (dataChipReward > 0) ChipManager.AddDataChips(dataChipReward);
                if (redGemReward > 0) ChipManager.AddRedGems(redGemReward);
            }

            OnEnemyDeath?.Invoke();
            OnDeath?.Invoke(this);
            GameEvents.RaiseEnemyKilled(expReward);
        }
    }

    public virtual void OnSpawnFromPool()
    {
        isDeadFallback = false;
        currentHealthFallback = maxHealth;
        CacheRequiredComponents();
    }

    public virtual void OnReturnToPool()
    {
        isDeadFallback = true;
        OnHealthChanged = null;
        OnEnemyDeath = null;
        OnDeath = null;
    }
}
