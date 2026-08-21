using UnityEngine;

public class PlayerWorldHealthBar : MonoBehaviour
{
    [Header("Tham chiếu")]
    [Tooltip("Máu của Player. Tự lấy trên cùng GameObject nếu để trống.")]
    [SerializeField] private PlayerHealth playerHealth;

    [Tooltip("Transform gốc chứa hai sprite nền và thanh máu.")]
    [SerializeField] private Transform barRoot;

    [Tooltip("SpriteRenderer nền xanh tối. Nền luôn giữ nguyên kích thước.")]
    [SerializeField] private SpriteRenderer backgroundRenderer;

    [Tooltip("SpriteRenderer thanh xanh sáng. Thanh này giảm theo HP Player.")]
    [SerializeField] private SpriteRenderer fillRenderer;

    [Header("Thứ tự hiển thị")]
    [Tooltip("Order in Layer của nền. Thanh máu sáng tự động cao hơn nền 1 cấp.")]
    [SerializeField] private int sortingOrder = 30;

    [Header("Hiển thị")]
    [Tooltip("Ẩn toàn bộ thanh máu khi Player chết.")]
    [SerializeField] private bool hideWhenDead = true;

    [Tooltip("Hiện thanh máu ngay cả khi HP đang đầy.")]
    [SerializeField] private bool showWhenFull = true;

    private Vector3 fullFillScale = Vector3.one;
    private Vector3 fullFillPosition;

    private void Awake()
    {
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();

        if (fillRenderer != null)
        {
            fullFillScale = fillRenderer.transform.localScale;
            fullFillPosition = fillRenderer.transform.localPosition;
        }

        ApplySortingOrder();
    }

    private void OnEnable()
    {
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.OnHealthChanged -= HandleHealthChanged;
        playerHealth.OnHealthChanged += HandleHealthChanged;
        HandleHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        float ratio = maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;
        SetNormalizedHealth(ratio);

        if (barRoot != null)
        {
            bool visible = !(hideWhenDead && currentHealth <= 0) && (showWhenFull || currentHealth < maxHealth);
            barRoot.gameObject.SetActive(visible);
        }
    }

    public void SetNormalizedHealth(float ratio)
    {
        if (fillRenderer == null) return;

        float clampedRatio = Mathf.Clamp01(ratio);
        Transform fillTransform = fillRenderer.transform;
        Vector3 scale = fullFillScale;
        scale.x = fullFillScale.x * clampedRatio;
        fillTransform.localScale = scale;

        float spriteWidth = fillRenderer.sprite != null ? fillRenderer.sprite.bounds.size.x : 1f;
        Vector3 position = fullFillPosition;
        position.x -= spriteWidth * fullFillScale.x * (1f - clampedRatio) * 0.5f;
        fillTransform.localPosition = position;
        fillRenderer.enabled = clampedRatio > 0f;
    }

    private void ApplySortingOrder()
    {
        if (backgroundRenderer != null) backgroundRenderer.sortingOrder = sortingOrder;
        if (fillRenderer != null) fillRenderer.sortingOrder = sortingOrder + 1;
    }

    private void OnValidate()
    {
        ApplySortingOrder();
    }
}
