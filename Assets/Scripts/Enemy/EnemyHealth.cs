using System;
using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable, IPoolable
{
    [Header("Health")]
    [Tooltip("Lượng máu tối đa của quái vật.")]
    [SerializeField] private int maxHealth = 50;

    [Header("Death")]
    [Tooltip("Thời gian trễ trước khi quái vật bị thu hồi về Pool sau khi chết (để chờ animation hoặc hiệu ứng).")]
    [SerializeField] private float destroyDelay = 0f;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    public event Action<int, int> OnHealthChanged;
    public event Action OnEnemyDeath;
    public event Action<EnemyHealth> OnDeath;

    private Collider2D[] colliders;

    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider2D>(true);
        CurrentHealth = maxHealth;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void SetMaxHealth(int newMaxHealth, bool resetCurrentHealth = true)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);
        if (resetCurrentHealth)
        {
            CurrentHealth = maxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead)
            return;

        if (damage <= 0)
            return;

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

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

        // Vô hiệu hóa collider để không bị trúng đạn / va chạm thêm trong lúc chờ despawn
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null) colliders[i].enabled = false;
        }

        OnEnemyDeath?.Invoke();
        OnDeath?.Invoke(this);

        if (destroyDelay > 0f)
        {
            StartCoroutine(DelayedDespawn(destroyDelay));
        }
        else
        {
            Despawn();
        }
    }

    private IEnumerator DelayedDespawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        Despawn();
    }

    public void Despawn()
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
        IsDead = false;
        CurrentHealth = maxHealth;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null) colliders[i].enabled = true;
        }

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void OnReturnToPool()
    {
        IsDead = true;
        OnDeath = null;
    }
}