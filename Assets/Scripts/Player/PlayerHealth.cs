using System;
using System.Collections;
using System.Collections.Generic;
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

    [Header("Damage Flash Effect")]
    [Tooltip("Bật hiệu ứng nhấp nháy đỏ khi Player nhận sát thương.")]
    [SerializeField] private bool enableDamageFlash = true;

    [Tooltip("Màu chuyển đổi khi nhận sát thương (Mặc định: Đỏ).")]
    [SerializeField] private Color damageFlashColor = Color.red;

    [Tooltip("Thời gian nhấp nháy màu đỏ khi nhận sát thương (giây). 0.15s cho mỗi lần nhận dame.")]
    [SerializeField] private float damageFlashDuration = 0.15f;

    [Tooltip("Material dùng shader Custom/2D/SpriteHitFlash. Nếu để trống sẽ tự động tìm hoặc nạp từ Shader/Assets.")]
    [SerializeField] private Material hitFlashMaterial;

    public bool EnableDamageFlash
    {
        get => enableDamageFlash;
        set => enableDamageFlash = value;
    }

    public Color DamageFlashColor
    {
        get => damageFlashColor;
        set => damageFlashColor = value;
    }

    public float DamageFlashDuration
    {
        get => damageFlashDuration;
        set => damageFlashDuration = Mathf.Max(0f, value);
    }

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

    private static Material sharedHitFlashMaterial;
    private static readonly int FlashAmountPropId = Shader.PropertyToID("_FlashAmount");
    private static readonly int FlashColorPropId = Shader.PropertyToID("_FlashColor");
    private MaterialPropertyBlock flashPropBlock;

    private float invincibleTimer;
    private int damageReduction;
    private SpriteRenderer[] spriteRenderers;
    private Color[] initialSpriteColors;
    private Coroutine flashRoutine;

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

        // Tự động sửa màu nếu Unity serialize giá trị rỗng (alpha = 0)
        if (damageFlashColor.a <= 0.01f || (damageFlashColor.r == 0f && damageFlashColor.g == 0f && damageFlashColor.b == 0f))
        {
            damageFlashColor = Color.red;
        }
        if (damageFlashDuration <= 0.01f)
        {
            damageFlashDuration = 0.15f;
        }

        CacheSpriteRenderers();
    }

    private void OnValidate()
    {
        if (damageFlashDuration <= 0f)
        {
            damageFlashDuration = 0.15f;
        }
        if (damageFlashColor.a <= 0.01f)
        {
            damageFlashColor = Color.red;
        }
    }

    public Material HitFlashMaterial
    {
        get => hitFlashMaterial;
        set
        {
            hitFlashMaterial = value;
            if (hitFlashMaterial != null)
            {
                sharedHitFlashMaterial = hitFlashMaterial;
            }
        }
    }

    public void CacheSpriteRenderers(bool forceRecache = false)
    {
        if (!forceRecache && spriteRenderers != null && initialSpriteColors != null && initialSpriteColors.Length == spriteRenderers.Length)
        {
            return;
        }

        if (flashPropBlock == null)
        {
            flashPropBlock = new MaterialPropertyBlock();
        }

        if (hitFlashMaterial != null)
        {
            sharedHitFlashMaterial = hitFlashMaterial;
        }
        else if (sharedHitFlashMaterial == null)
        {
            Shader hitShader = Shader.Find("Custom/2D/SpriteHitFlash");
            if (hitShader != null)
            {
                sharedHitFlashMaterial = new Material(hitShader);
                sharedHitFlashMaterial.name = "Runtime_SpriteHitFlash_Player_Shared";
            }
        }

        SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (allRenderers == null || allRenderers.Length == 0)
        {
            spriteRenderers = Array.Empty<SpriteRenderer>();
            initialSpriteColors = Array.Empty<Color>();
            return;
        }

        // Lọc bỏ các SpriteRenderer thuộc PlayerWorldHealthBar để thanh máu không bị đổi màu đỏ
        PlayerWorldHealthBar healthBar = GetComponentInChildren<PlayerWorldHealthBar>(true);
        var list = new List<SpriteRenderer>();

        for (int i = 0; i < allRenderers.Length; i++)
        {
            SpriteRenderer sr = allRenderers[i];
            if (sr == null) continue;

            if (healthBar != null && sr.transform.IsChildOf(healthBar.transform))
            {
                continue;
            }

            list.Add(sr);
        }

        spriteRenderers = list.ToArray();
        initialSpriteColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                initialSpriteColors[i] = spriteRenderers[i].color;

                if (sharedHitFlashMaterial != null && (spriteRenderers[i].sharedMaterial == null ||
                    spriteRenderers[i].sharedMaterial.shader == null ||
                    !spriteRenderers[i].sharedMaterial.HasProperty(FlashAmountPropId)))
                {
                    spriteRenderers[i].sharedMaterial = sharedHitFlashMaterial;
                }
            }
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

        // Hiển thị số sát thương đỏ trên đầu Player
        Vector3 spawnPos = transform.position + Vector3.up * 0.6f;
        DamageNumberManager.ShowDamage(spawnPos, effectiveDamage, DamageType.PlayerDamage);

        // Luôn kích hoạt hiệu ứng đỏ cho MỌI đòn nhận sát thương (kể cả đòn chí tử khiến máu về 0)
        TriggerDamageFlash();

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Kích hoạt hiệu ứng chớp đỏ đúng 1 lần duy nhất cho mỗi lần nhận sát thương.
    /// Nhận bao nhiêu lần sát thương sẽ chớp bấy nhiêu lần riêng biệt.
    /// </summary>
    public void TriggerDamageFlash()
    {
        if (!enableDamageFlash || !gameObject.activeInHierarchy)
            return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            RestoreSpriteColors();
        }
        flashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            CacheSpriteRenderers();
        }

        if (flashPropBlock == null)
        {
            flashPropBlock = new MaterialPropertyBlock();
        }

        if (spriteRenderers != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    if (sharedHitFlashMaterial != null && (spriteRenderers[i].sharedMaterial == null || !spriteRenderers[i].sharedMaterial.HasProperty(FlashAmountPropId)))
                    {
                        spriteRenderers[i].sharedMaterial = sharedHitFlashMaterial;
                    }

                    spriteRenderers[i].GetPropertyBlock(flashPropBlock);
                    flashPropBlock.SetFloat(FlashAmountPropId, 1f);
                    flashPropBlock.SetColor(FlashColorPropId, damageFlashColor);
                    spriteRenderers[i].SetPropertyBlock(flashPropBlock);

                    Color orig = (initialSpriteColors != null && i < initialSpriteColors.Length)
                        ? initialSpriteColors[i]
                        : Color.white;
                    spriteRenderers[i].color = new Color(damageFlashColor.r, damageFlashColor.g, damageFlashColor.b, orig.a);
                }
            }
        }

        yield return new WaitForSeconds(damageFlashDuration);

        RestoreSpriteColors();
        flashRoutine = null;
    }

    /// <summary>
    /// Khôi phục lại màu sắc ban đầu của các SpriteRenderer trên Player.
    /// </summary>
    public void RestoreSpriteColors()
    {
        if (flashPropBlock == null)
        {
            flashPropBlock = new MaterialPropertyBlock();
        }

        if (spriteRenderers != null && initialSpriteColors != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].GetPropertyBlock(flashPropBlock);
                    flashPropBlock.SetFloat(FlashAmountPropId, 0f);
                    spriteRenderers[i].SetPropertyBlock(flashPropBlock);

                    if (i < initialSpriteColors.Length)
                    {
                        spriteRenderers[i].color = initialSpriteColors[i];
                    }
                }
            }
        }
    }

    public void Heal(int amount)
    {
        if (IsDead)
            return;

        if (amount <= 0)
            return;

        int prevHp = CurrentHealth;
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, maxHealth);
        int healedAmount = CurrentHealth - prevHp;

        if (healedAmount > 0)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.6f;
            DamageNumberManager.ShowDamage(spawnPos, healedAmount, DamageType.Heal);
        }

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

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
        RestoreSpriteColors();

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

    private void OnDisable()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
    }
}
