using UnityEngine;
using UnityEngine.UI;

public class WorldHealthBar : MonoBehaviour, IPoolable
{
    [Header("Target Health Component")]
    [Tooltip("Tham chiếu tới EnemyHealth của quái vật (tự lấy từ cha/bản thân nếu để trống).")]
    [SerializeField] private EnemyHealth enemyHealth;

    [Header("UI References")]
    [Tooltip("Thanh máu dạng Slider trên World Space Canvas.")]
    [SerializeField] private Slider healthSlider;

    [Tooltip("Image Fill của thanh máu.")]
    [SerializeField] private Image fillImage;

    [Tooltip("CanvasGroup điều khiển ẩn/hiện độ trong suốt của thanh máu.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Behavior Settings")]
    [Tooltip("Tự động ẩn thanh máu khi đầy 100% máu.")]
    [SerializeField] private bool hideWhenFull = true;

    [Tooltip("Tự động ẩn thanh máu sau một khoảng thời gian không nhận thêm sát thương (0 = luôn hiện khi mất máu).")]
    [SerializeField] private float autoHideDelay = 3f;

    [Header("Color Gradient")]
    [Tooltip("Bật tính năng tự đổi màu thanh máu theo tỷ lệ máu hiện tại.")]
    [SerializeField] private bool useColorGradient = true;

    [Tooltip("Dải màu chuyển tiếp từ đầy máu sang hết máu.")]
    [SerializeField] private Gradient colorGradient;

    private float hideTimer;
    private bool isDamaged;

    private void Awake()
    {
        if (enemyHealth == null)
        {
            enemyHealth = GetComponentInParent<EnemyHealth>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (useColorGradient && (colorGradient == null || colorGradient.colorKeys.Length == 0))
        {
            colorGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(Color.red, 0f);
            colorKeys[1] = new GradientColorKey(Color.yellow, 0.5f);
            colorKeys[2] = new GradientColorKey(Color.green, 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            colorGradient.SetKeys(colorKeys, alphaKeys);
        }
    }

    private void Start()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged += HandleHealthChanged;
            ResetHealthBar(enemyHealth.CurrentHealth, enemyHealth.MaxHealth);
        }
    }

    private void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void Update()
    {
        if (autoHideDelay > 0f && isDamaged && hideTimer > 0f)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
            {
                SetVisible(false);
            }
        }
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (maxHealth <= 0) return;

        float ratio = Mathf.Clamp01((float)currentHealth / maxHealth);

        if (healthSlider != null)
        {
            healthSlider.value = ratio;
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = ratio;
            if (useColorGradient)
            {
                fillImage.color = colorGradient.Evaluate(ratio);
            }
        }

        if (currentHealth < maxHealth && currentHealth > 0)
        {
            isDamaged = true;
            hideTimer = autoHideDelay;
            SetVisible(true);
        }
        else if (currentHealth <= 0 || (hideWhenFull && currentHealth >= maxHealth))
        {
            SetVisible(false);
        }
    }

    private void ResetHealthBar(int currentHealth, int maxHealth)
    {
        HandleHealthChanged(currentHealth, maxHealth);
        if (hideWhenFull && currentHealth >= maxHealth)
        {
            SetVisible(false);
        }
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }
        else
        {
            gameObject.SetActive(visible);
        }
    }

    public void OnSpawnFromPool()
    {
        isDamaged = false;
        hideTimer = 0f;
        if (enemyHealth != null)
        {
            ResetHealthBar(enemyHealth.CurrentHealth, enemyHealth.MaxHealth);
        }
    }

    public void OnReturnToPool()
    {
        SetVisible(false);
    }
}
