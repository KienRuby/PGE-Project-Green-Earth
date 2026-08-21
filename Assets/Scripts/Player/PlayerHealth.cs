using System;
using UnityEngine;

/// <summary>
/// Interface cho các đối tượng có thể nhận sát thương.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int damage);
}

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [Tooltip("Lượng máu tối đa của Player.")]
    [SerializeField] private int maxHealth = 100;

    [Header("Damage Protection")]
    [Tooltip("Thời gian bất tử sau khi nhận sát thương (giúp tránh bị mất máu dồn dập).")]
    [SerializeField] private float invincibleTime = 0.3f;

    [SerializeField] private int currentHealth = 100;
    private bool isInitialized = false;

    public int CurrentHealth
    {
        get
        {
            if (!isInitialized)
            {
                currentHealth = maxHealth;
                isInitialized = true;
            }
            return currentHealth;
        }
        private set
        {
            currentHealth = value;
            isInitialized = true;
        }
    }

    public int MaxHealth => maxHealth;
    public int BaseMaxHealth
    {
        get => baseMaxHealth > 0 ? baseMaxHealth : maxHealth;
        private set => baseMaxHealth = value;
    }
    private int baseMaxHealth;

    public bool IsDead { get; private set; }

    public event Action<int, int> OnHealthChanged;
    public event Action OnPlayerDeath;

    private float invincibleTimer;
    private int damageReduction;

    private void Awake()
    {
        if (baseMaxHealth <= 0)
        {
            baseMaxHealth = maxHealth;
        }
        if (!isInitialized)
        {
            currentHealth = maxHealth;
            isInitialized = true;
        }
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void SetMaxHealth(int newMaxHealth, bool resetCurrentHealth = false)
    {
        if (baseMaxHealth <= 0)
        {
            baseMaxHealth = maxHealth;
        }
        maxHealth = Mathf.Max(1, newMaxHealth);
        if (resetCurrentHealth)
        {
            CurrentHealth = maxHealth;
        }
        else
        {
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
        }
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void SetDamageReduction(int reduction)
    {
        damageReduction = Mathf.Max(0, reduction);
    }

    private void Update()
    {
        if (invincibleTimer > 0f)
        {
            invincibleTimer -= Time.deltaTime;
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead)
            return;

        if (invincibleTimer > 0f)
            return;

        if (damage <= 0)
            return;

        int effectiveDamage = Mathf.Max(1, damage - damageReduction);

        CurrentHealth = Mathf.Clamp(CurrentHealth - effectiveDamage, 0, maxHealth);

        invincibleTimer = invincibleTime;

        Debug.Log($"Player nhận {effectiveDamage} damage (gốc {damage}, giáp giảm {damageReduction}). HP: {CurrentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead)
            return;

        if (amount <= 0)
            return;

        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, maxHealth);

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void FullHeal()
    {
        if (IsDead)
            return;

        CurrentHealth = maxHealth;

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public bool Revive(float healthPercent = 0.5f, float invincibilityDuration = 2f)
    {
        if (!IsDead)
            return false;

        IsDead = false;
        CurrentHealth = Mathf.Clamp(Mathf.CeilToInt(maxHealth * Mathf.Clamp01(healthPercent)), 1, maxHealth);
        invincibleTimer = Mathf.Max(invincibleTimer, invincibilityDuration);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        return true;
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        Debug.Log("Player đã chết!");

        OnPlayerDeath?.Invoke();
    }
}
