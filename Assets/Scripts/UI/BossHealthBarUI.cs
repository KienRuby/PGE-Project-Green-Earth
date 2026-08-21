using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý hiển thị thanh máu của Boss trên Canvas (nằm ngay dưới thanh Level/EXP).
/// - Tự động ẩn khi chưa có Boss (bằng CanvasGroup alpha = 0, không tắt GameObject để tránh ngắt script).
/// - Tự động phát hiện và kết nối với Boss khi Boss xuất hiện.
/// - Cập nhật thanh máu mượt mà (smooth lerp) và thanh bóng máu (ghost bar).
/// - Tự động ẩn đi khi Boss bị tiêu diệt.
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    [Header("Spawner & Target Reference")]
    [Tooltip("Tham chiếu tới EnemySpawner (tự động tìm nếu để trống).")]
    [SerializeField] private EnemySpawner enemySpawner;

    [Header("UI Containers")]
    [Tooltip("CanvasGroup để điều khiển ẩn / hiện và làm hiệu ứng Fade-in / Fade-out mượt mà.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("UI Elements")]
    [Tooltip("Text hiển thị tên Boss (ví dụ: 'MUTANT TITAN', 'FINAL BOSS').")]
    [SerializeField] private TMP_Text bossNameText;

    [Tooltip("Image hiển thị thanh máu chính (Image Type = Filled, Fill Method = Horizontal).")]
    [SerializeField] private Image healthFillImage;

    [Tooltip("Image hiệu ứng thanh máu tụt từ từ (Damage Ghost Fill - tùy chọn).")]
    [SerializeField] private Image damageGhostFillImage;

    [Tooltip("Text hiển thị số máu chi tiết (ví dụ: '1,500 / 1,500').")]
    [SerializeField] private TMP_Text healthNumberText;

    [Header("Animation & Smooth Settings")]
    [Tooltip("Bật hiệu ứng thanh máu tụt mượt.")]
    [SerializeField] private bool smoothTransition = true;

    [Tooltip("Tốc độ chuyển động của thanh máu.")]
    [SerializeField] private float smoothSpeed = 10f;

    [Tooltip("Tốc độ thanh bóng tụt chậm (Damage Ghost Bar).")]
    [SerializeField] private float ghostSpeed = 3f;

    private EnemyHealth currentBossHealth;
    private float targetFill = 1f;
    private float currentFill = 1f;
    private float ghostFill = 1f;
    private float searchTimer = 0f;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (bossNameText != null)
        {
            bossNameText.text = SanitizeBossDisplayName(bossNameText.text);
        }
        EnsureReferences();
        SetAlphaImmediate(0f);
    }

    private void OnEnable()
    {
        EnsureReferences();
        SubscribeSpawnerEvents();
    }

    private void Start()
    {
        EnsureReferences();
        SubscribeSpawnerEvents();
        CheckExistingBoss();
    }

    private void OnDisable()
    {
        UnsubscribeSpawnerEvents();
        UnsubscribeBossEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeSpawnerEvents();
        UnsubscribeBossEvents();
    }

    private void EnsureReferences()
    {
        if (enemySpawner == null)
        {
            enemySpawner = FindObjectOfType<EnemySpawner>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>() ?? GetComponentInChildren<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void SubscribeSpawnerEvents()
    {
        if (enemySpawner != null)
        {
            enemySpawner.OnBossSpawned -= HandleBossSpawned;
            enemySpawner.OnBossSpawned += HandleBossSpawned;

            enemySpawner.OnBossDefeated -= HandleBossDefeated;
            enemySpawner.OnBossDefeated += HandleBossDefeated;
        }
    }

    private void UnsubscribeSpawnerEvents()
    {
        if (enemySpawner != null)
        {
            enemySpawner.OnBossSpawned -= HandleBossSpawned;
            enemySpawner.OnBossDefeated -= HandleBossDefeated;
        }
    }

    private void Update()
    {
        // 1. Nếu chưa có Boss, định kỳ quét tìm Boss trong scene (tự động hồi phục nếu lỡ event)
        if (currentBossHealth == null)
        {
            searchTimer -= Time.deltaTime;
            if (searchTimer <= 0f)
            {
                searchTimer = 0.25f;
                CheckExistingBoss();
            }
            return;
        }

        // 2. Nếu Boss đã chết hoặc bị hủy
        if (currentBossHealth.IsDead || !currentBossHealth.gameObject.activeInHierarchy)
        {
            HandleBossDefeated();
            return;
        }

        // 3. Smooth fill animation
        if (smoothTransition)
        {
            if (Mathf.Abs(currentFill - targetFill) > 0.001f)
            {
                currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * smoothSpeed);
                if (healthFillImage != null)
                {
                    healthFillImage.fillAmount = currentFill;
                }
            }

            // Ghost bar (tụt chậm theo sau)
            if (damageGhostFillImage != null)
            {
                if (ghostFill > targetFill)
                {
                    ghostFill = Mathf.Lerp(ghostFill, targetFill, Time.deltaTime * ghostSpeed);
                    damageGhostFillImage.fillAmount = ghostFill;
                }
                else
                {
                    ghostFill = targetFill;
                    damageGhostFillImage.fillAmount = ghostFill;
                }
            }
        }
    }

    private void CheckExistingBoss()
    {
        if (currentBossHealth != null) return;

        // Ưu tiên 1: Lấy từ Spawner
        if (enemySpawner != null && enemySpawner.CurrentActiveBoss != null && !enemySpawner.CurrentActiveBoss.IsDead)
        {
            HookBoss(enemySpawner.CurrentActiveBoss.gameObject);
            return;
        }

        // Ưu tiên 2: Tìm BossMovement trong Scene
        BossMovement bossMove = FindObjectOfType<BossMovement>();
        if (bossMove != null && bossMove.gameObject.activeInHierarchy)
        {
            EnemyHealth health = bossMove.GetComponent<EnemyHealth>();
            if (health != null && !health.IsDead)
            {
                HookBoss(bossMove.gameObject);
                return;
            }
        }

        // Ưu tiên 3: Tìm GameObject có tên chứa "Boss" và có EnemyHealth
        EnemyHealth[] allEnemies = FindObjectsOfType<EnemyHealth>();
        foreach (EnemyHealth eh in allEnemies)
        {
            if (eh != null && eh.gameObject.activeInHierarchy && !eh.IsDead &&
                eh.gameObject.name.ToLower().Contains("boss"))
            {
                HookBoss(eh.gameObject);
                return;
            }
        }
    }

    private void HandleBossSpawned(GameObject bossObj)
    {
        if (bossObj == null) return;
        HookBoss(bossObj);
    }

    public void HookBoss(GameObject bossObj)
    {
        if (bossObj == null) return;

        EnemyHealth health = bossObj.GetComponent<EnemyHealth>();
        if (health == null || health.IsDead) return;

        if (currentBossHealth == health) return;

        UnsubscribeBossEvents();
        currentBossHealth = health;

        currentBossHealth.OnHealthChanged += HandleBossHealthChanged;
        currentBossHealth.OnDeath += HandleBossDead;

        string bossName = bossObj.name.Replace("(Clone)", "").Trim();
        if (bossName.StartsWith("Boss_")) bossName = bossName.Substring(5);
        if (string.IsNullOrEmpty(bossName)) bossName = "BOSS";

        if (bossNameText != null)
        {
            bossNameText.text = SanitizeBossDisplayName(bossName);
        }

        targetFill = currentBossHealth.MaxHealth > 0 ? Mathf.Clamp01((float)currentBossHealth.CurrentHealth / currentBossHealth.MaxHealth) : 1f;
        currentFill = targetFill;
        ghostFill = targetFill;

        if (healthFillImage != null) healthFillImage.fillAmount = currentFill;
        if (damageGhostFillImage != null) damageGhostFillImage.fillAmount = ghostFill;

        UpdateHealthText(currentBossHealth.CurrentHealth, currentBossHealth.MaxHealth);
        FadeVisible(true, 0.35f);

        Debug.Log($"[BossHealthBarUI] 🎯 Đã kết nối thanh máu với Boss: {bossObj.name} (HP: {currentBossHealth.CurrentHealth}/{currentBossHealth.MaxHealth})");
    }

    public static string SanitizeBossDisplayName(string value)
    {
        string safeName = (value ?? string.Empty)
            .Replace("\u26A0", string.Empty)
            .Replace("\uFE0F", string.Empty)
            .Trim();
        return string.IsNullOrEmpty(safeName) ? "BOSS" : safeName.ToUpperInvariant();
    }

    private void HandleBossHealthChanged(int currentHp, int maxHp)
    {
        if (maxHp <= 0) return;

        targetFill = Mathf.Clamp01((float)currentHp / maxHp);

        if (!smoothTransition)
        {
            currentFill = targetFill;
            ghostFill = targetFill;
            if (healthFillImage != null) healthFillImage.fillAmount = targetFill;
            if (damageGhostFillImage != null) damageGhostFillImage.fillAmount = targetFill;
        }

        UpdateHealthText(currentHp, maxHp);
    }

    private void HandleBossDead(EnemyHealth health)
    {
        HandleBossDefeated();
    }

    private void HandleBossDefeated()
    {
        UnsubscribeBossEvents();
        FadeVisible(false, 0.8f);
    }

    private void UnsubscribeBossEvents()
    {
        if (currentBossHealth != null)
        {
            currentBossHealth.OnHealthChanged -= HandleBossHealthChanged;
            currentBossHealth.OnDeath -= HandleBossDead;
            currentBossHealth = null;
        }
    }

    private void UpdateHealthText(int currentHp, int maxHp)
    {
        if (healthNumberText != null)
        {
            healthNumberText.text = $"{currentHp:N0} / {maxHp:N0}";
        }
    }

    private void SetAlphaImmediate(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = (alpha > 0.01f);
            canvasGroup.interactable = (alpha > 0.01f);
        }
    }

    private void FadeVisible(bool visible, float duration)
    {
        if (canvasGroup == null) return;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeRoutine(visible, duration));
    }

    private IEnumerator FadeRoutine(bool visible, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float endAlpha = visible ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
        fadeCoroutine = null;
    }
}
