using System;
using UnityEngine;

/// <summary>
/// Hệ thống quản lý Máu và Sát thương chuẩn hóa (HealthSystem) cho các thực thể trong PGE:
/// - Tính toán sát thương thực tế (trừ giáp / Damage Reduction, trừ Khiên / Shield trước).
/// - Khung thời gian bất tử (Invulnerability Frames / I-Frames) chống dồn sát thương.
/// - Hồi máu (Heal) và chuyển hồi dư sang Khiên (Overheal to Shield).
/// - Cơ chế Hồi sinh (Revive) và sự kiện chuẩn hóa (OnHealthChanged, OnDamageTaken, OnDeath, OnRevived).
/// </summary>
public class HealthSystem : MonoBehaviour, IDamageable
{
    [Header("Health Configuration")]
    [Tooltip("Lượng máu tối đa ban đầu.")]
    [SerializeField] private int maxHealth = 100;

    [Tooltip("Máu hiện tại.")]
    [SerializeField] private int currentHealth = 100;

    [Header("Shield Configuration")]
    [Tooltip("Lượng khiên tối đa có thể tích lũy.")]
    [SerializeField] private int maxShield = 0;

    [Tooltip("Lượng khiên hiện tại.")]
    [SerializeField] private int currentShield = 0;

    [Header("Defense & Invulnerability")]
    [Tooltip("Lượng sát thương giảm trừ cố định từ giáp.")]
    [SerializeField] private int damageReduction = 0;

    [Tooltip("Thời gian bất tử sau khi nhận sát thương (giây).")]
    [SerializeField] private float invincibleDuration = 0.3f;

    [Header("Feedback")]
    [Tooltip("Tự động hiển thị chữ số sát thương bay (Floating Damage Numbers).")]
    [SerializeField] private bool showFloatingDamage = true;

    [Tooltip("Loại hiển thị sát thương mặc định khi nhận đòn.")]
    [SerializeField] private DamageType damageTypeOnHit = DamageType.PlayerDamage;

    private float invincibleTimer = 0f;
    private bool isDead = false;
    private bool isInitialized = false;

    // Events
    public event Action<int, int> OnHealthChanged; // (currentHealth, maxHealth)
    public event Action<int> OnDamageTaken;        // (damageAmount)
    public event Action<int> OnHealed;             // (healAmount)
    public event Action OnDeath;
    public event Action OnRevived;

    // Properties
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int MaxShield => maxShield;
    public int CurrentShield => currentShield;
    public int DamageReduction => damageReduction;
    public float InvincibleDuration => invincibleDuration;
    public bool IsDead => isDead;
    public bool IsInvincible => invincibleTimer > 0f;
    public float HealthPercent => maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void InitializeIfNeeded()
    {
        if (isInitialized) return;

        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;
        currentShield = Mathf.Clamp(currentShield, 0, maxShield);
        isDead = false;
        invincibleTimer = 0f;
        isInitialized = true;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        if (invincibleTimer > 0f)
        {
            invincibleTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Nhận sát thương theo chuẩn IDamageable với đầy đủ cơ chế trừ giáp, trừ khiên và i-frames.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;
        if (invincibleTimer > 0f) return;

        InitializeIfNeeded();

        // 1. Tính toán giảm trừ sát thương từ giáp
        int effectiveDamage = Mathf.Max(1, damage - damageReduction);

        // 2. Trừ vào khiên trước nếu có
        int damageToHp = effectiveDamage;
        if (currentShield > 0)
        {
            int shieldAbsorb = Mathf.Min(currentShield, effectiveDamage);
            currentShield -= shieldAbsorb;
            damageToHp -= shieldAbsorb;
        }

        // 3. Trừ vào lượng máu thực tế
        if (damageToHp > 0)
        {
            currentHealth = Mathf.Clamp(currentHealth - damageToHp, 0, maxHealth);
        }

        // 4. Kích hoạt thời gian bất tử
        invincibleTimer = invincibleDuration;

        // 5. Hiển thị số sát thương nổi
        if (showFloatingDamage && GameSettings.ShowDamage)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.6f;
            DamageNumberManager.ShowDamage(spawnPos, effectiveDamage, damageTypeOnHit);
        }

        // 6. Phát sự kiện
        OnDamageTaken?.Invoke(effectiveDamage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // 7. Xử lý khi cạn máu
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Hồi phục máu và hỗ trợ tràn máu thành khiên nếu được cấu hình.
    /// </summary>
    public void Heal(int amount, bool overhealToShield = false)
    {
        if (isDead || amount <= 0) return;

        InitializeIfNeeded();

        int prevHp = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        int healedAmount = currentHealth - prevHp;
        int overheal = amount - healedAmount;

        if (healedAmount > 0)
        {
            if (showFloatingDamage && GameSettings.ShowDamage)
            {
                Vector3 spawnPos = transform.position + Vector3.up * 0.6f;
                DamageNumberManager.ShowDamage(spawnPos, healedAmount, DamageType.Heal);
            }

            OnHealed?.Invoke(healedAmount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        if (overhealToShield && overheal > 0 && maxShield > 0)
        {
            AddShield(overheal);
        }
    }

    /// <summary>
    /// Hồi sinh thực thể khi đã tử trận.
    /// </summary>
    public bool Revive(float healthPercent = 0.5f, float invincibilityDuration = 2.0f)
    {
        if (!isDead) return false;

        isDead = false;
        currentHealth = Mathf.Clamp(Mathf.CeilToInt(maxHealth * Mathf.Clamp01(healthPercent)), 1, maxHealth);
        invincibleTimer = Mathf.Max(invincibleTimer, invincibilityDuration);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnRevived?.Invoke();

        return true;
    }

    /// <summary>
    /// Thiết lập chỉ số Máu tối đa.
    /// </summary>
    public void SetMaxHealth(int newMaxHealth, bool resetCurrentHealth = false)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);
        if (resetCurrentHealth)
        {
            currentHealth = maxHealth;
        }
        else
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Thiết lập chỉ số Giảm sát thương.
    /// </summary>
    public void SetDamageReduction(int reduction)
    {
        damageReduction = Mathf.Max(0, reduction);
    }

    /// <summary>
    /// Cấu hình Khiên tối đa.
    /// </summary>
    public void SetMaxShield(int max)
    {
        maxShield = Mathf.Max(0, max);
        currentShield = Mathf.Clamp(currentShield, 0, maxShield);
    }

    /// <summary>
    /// Thêm lượng khiên hiện tại.
    /// </summary>
    public void AddShield(int amount)
    {
        if (isDead || maxShield <= 0 || amount <= 0) return;
        currentShield = Mathf.Clamp(currentShield + amount, 0, maxShield);
    }

    public void FullHeal()
    {
        if (isDead) return;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        OnDeath?.Invoke();
    }
}
