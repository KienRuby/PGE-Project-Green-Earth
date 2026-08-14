using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 50;

    [Header("Death")]
    [SerializeField] private float destroyDelay = 0f;

    public int CurrentHealth { get; private set; }

    public int MaxHealth => maxHealth;

    public bool IsDead { get; private set; }

    public event Action<int, int> OnHealthChanged;
    public event Action OnEnemyDeath;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead)
            return;

        if (damage <= 0)
            return;

        CurrentHealth -= damage;

        CurrentHealth = Mathf.Clamp(
            CurrentHealth,
            0,
            maxHealth
        );

        Debug.Log(
            $"{gameObject.name} nhận {damage} damage. " +
            $"HP: {CurrentHealth}/{maxHealth}"
        );

        OnHealthChanged?.Invoke(
            CurrentHealth,
            maxHealth
        );

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        Debug.Log($"{gameObject.name} đã chết!");

        OnEnemyDeath?.Invoke();

        Destroy(gameObject, destroyDelay);
    }
}