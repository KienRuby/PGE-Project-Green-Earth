using UnityEngine;
using UnityEngine.UI;
#if TMPro_PRESENT || ENABLE_TEXTMESHPRO
using TMPro;
#endif

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Player Reference")]
    [Tooltip("Tham chiếu tới PlayerHealth (tự động tìm Player trong Scene nếu để trống).")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("UI Components")]
    [Tooltip("Thanh máu dạng Slider của Unity UI.")]
    [SerializeField] private Slider healthSlider;

    [Tooltip("Thanh máu dạng Image Fill (Image Type = Filled).")]
    [SerializeField] private Image healthFillImage;

    [Tooltip("UI Text cổ điển hiển thị số máu (ví dụ: 100/100).")]
    [SerializeField] private Text legacyHealthText;

#if TMPro_PRESENT || ENABLE_TEXTMESHPRO
    [Tooltip("TextMeshPro hiển thị số máu (ví dụ: 100/100).")]
    [SerializeField] private TextMeshProUGUI tmpHealthText;
#endif

    [Header("Smoothing")]
    [Tooltip("Bật hiệu ứng chuyển động mượt mà khi thanh máu tăng/giảm.")]
    [SerializeField] private bool smoothTransition = true;

    [Tooltip("Tốc độ chuyển động mượt của thanh máu.")]
    [SerializeField] private float smoothSpeed = 8f;

    [Header("Color Gradient")]
    [Tooltip("Bật tính năng tự động đổi màu thanh máu theo tỷ lệ máu hiện tại.")]
    [SerializeField] private bool useColorGradient = true;

    [Tooltip("Dải màu chuyển tiếp theo % máu (mặc định: Đỏ -> Vàng -> Xanh lá).")]
    [SerializeField] private Gradient healthColorGradient;

    private float targetFill = 1f;
    private float currentFill = 1f;

    private void Awake()
    {
        if (useColorGradient && (healthColorGradient == null || healthColorGradient.colorKeys.Length == 0))
        {
            healthColorGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(Color.red, 0.0f);
            colorKeys[1] = new GradientColorKey(Color.yellow, 0.5f);
            colorKeys[2] = new GradientColorKey(Color.green, 1.0f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);

            healthColorGradient.SetKeys(colorKeys, alphaKeys);
        }
    }

    private void Start()
    {
        if (playerHealth == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerHealth = playerObj.GetComponent<PlayerHealth>();
            }
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHealthChanged;
            UpdateHealthImmediate(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void Update()
    {
        if (smoothTransition && Mathf.Abs(currentFill - targetFill) > 0.001f)
        {
            currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * smoothSpeed);
            ApplyFill(currentFill);
        }
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (maxHealth <= 0) return;

        targetFill = Mathf.Clamp01((float)currentHealth / maxHealth);

        if (!smoothTransition)
        {
            currentFill = targetFill;
            ApplyFill(currentFill);
        }

        UpdateText(currentHealth, maxHealth);
    }

    private void UpdateHealthImmediate(int currentHealth, int maxHealth)
    {
        if (maxHealth <= 0) return;

        targetFill = Mathf.Clamp01((float)currentHealth / maxHealth);
        currentFill = targetFill;
        ApplyFill(currentFill);
        UpdateText(currentHealth, maxHealth);
    }

    private void ApplyFill(float fill)
    {
        if (healthSlider != null)
        {
            healthSlider.value = fill;
        }

        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = fill;

            if (useColorGradient)
            {
                healthFillImage.color = healthColorGradient.Evaluate(fill);
            }
        }
    }

    private void UpdateText(int currentHealth, int maxHealth)
    {
        string text = $"{currentHealth} / {maxHealth}";

        if (legacyHealthText != null)
        {
            legacyHealthText.text = text;
        }

#if TMPro_PRESENT || ENABLE_TEXTMESHPRO
        if (tmpHealthText != null)
        {
            tmpHealthText.text = text;
        }
#endif
    }
}
