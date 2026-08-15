using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [Tooltip("Lượng máu tối đa của Player.")]
    [SerializeField] private int maxHealth = 100;

    [Header("Damage Protection")]
    [Tooltip("Thời gian bất tử sau khi nhận sát thương (giúp tránh bị mất máu dồn dập).")]
    [SerializeField] private float invincibleTime = 0.3f;

    public int CurrentHealth { get; private set; }

    public int MaxHealth => maxHealth;

    public bool IsDead { get; private set; }

    public event Action<int, int> OnHealthChanged;
    public event Action OnPlayerDeath;

    private float invincibleTimer;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
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

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        invincibleTimer = invincibleTime;

        Debug.Log($"Player nhận {damage} damage. HP: {CurrentHealth}/{maxHealth}");

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

        CurrentHealth += amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void FullHeal()
    {
        if (IsDead)
            return;

        CurrentHealth = maxHealth;

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        Debug.Log("Player đã chết!");

        OnPlayerDeath?.Invoke();

        // Sau này có thể:
        // - chạy animation Death
        // - hiện Game Over
        // - disable PlayerMovement
        // - respawn
    }
}