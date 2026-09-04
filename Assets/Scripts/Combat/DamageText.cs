using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Quản lý hiển thị chữ/số sát thương nổi (Floating Damage Text / Damage Number):
/// - Tương thích với TextMeshPro trong không gian 2D/3D.
/// - Cung cấp API tĩnh tiện lợi: DamageText.Show(pos, damage, type).
/// - Hỗ trợ hiệu ứng phóng to nảy (Pop Scale), trôi dạt (Float up), và mờ dần (Fade out).
/// - Tương thích chuẩn hệ thống Object Pool (IPoolable).
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class DamageText : MonoBehaviour, IPoolable
{
    [Header("UI & Rendering")]
    [SerializeField] private TMP_Text textComponent;
    [SerializeField] private string sortingLayerName = "UI";
    [SerializeField] private int sortingOrder = 600;

    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.7f;
    [SerializeField] private float floatSpeed = 1.25f;
    [SerializeField] private float popMultiplier = 1.35f;

    [Header("Color Schemes")]
    [SerializeField] private Color normalColor = new Color(1f, 0.72f, 0.18f, 1f);       // Vàng cam
    [SerializeField] private Color criticalColor = new Color(1f, 0.42f, 0.05f, 1f);     // Cam lửa
    [SerializeField] private Color playerDamageColor = new Color(1f, 0.22f, 0.22f, 1f); // Đỏ tươi
    [SerializeField] private Color healColor = new Color(0.18f, 0.9f, 0.45f, 1f);       // Xanh lá

    private Vector3 initialScale = Vector3.one;
    private Vector3 currentVelocity;
    private Color baseColor;
    private float elapsedTime;
    private bool isRunning;
    private MeshRenderer meshRenderer;

    public float Duration
    {
        get => duration;
        set => duration = Mathf.Max(0.1f, value);
    }

    public TMP_Text TextComponent => textComponent != null ? textComponent : (textComponent = GetComponent<TMP_Text>());

    private void Awake()
    {
        EnsureComponents();
    }

    public void EnsureComponents()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TMP_Text>();
        }

        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = sortingLayerName;
            meshRenderer.sortingOrder = sortingOrder;
        }

        if (initialScale == Vector3.zero || initialScale == Vector3.one)
        {
            initialScale = transform.localScale;
            if (initialScale == Vector3.zero) initialScale = Vector3.one;
        }
    }

    public void SetSorting(string layerName, int order)
    {
        sortingLayerName = layerName;
        sortingOrder = order;
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = layerName;
            meshRenderer.sortingOrder = order;
        }
    }

    /// <summary>
    /// API tĩnh tiện lợi gọi hiển thị số sát thương nổi từ bất kỳ hệ thống nào.
    /// </summary>
    public static void Show(Vector3 worldPosition, int damage, DamageType type = DamageType.Normal, float extraScale = 1f)
    {
        DamageNumberManager.ShowDamage(worldPosition, damage, type, extraScale);
    }

    /// <summary>
    /// Khởi tạo và kích hoạt hiệu ứng hiển thị số sát thương.
    /// </summary>
    public void Initialize(int amount, DamageType type, Vector3 startPos, float extraScale = 1f)
    {
        EnsureComponents();

        // 1. Gán nội dung
        if (textComponent != null)
        {
            textComponent.text = amount > 0 ? amount.ToString() : "0";
            if (type == DamageType.Heal)
            {
                textComponent.text = "+" + textComponent.text;
            }
        }

        // 2. Màu sắc
        float scaleFactor = extraScale;
        switch (type)
        {
            case DamageType.Critical:
                baseColor = criticalColor;
                scaleFactor *= 1.3f;
                break;
            case DamageType.PlayerDamage:
                baseColor = playerDamageColor;
                break;
            case DamageType.Heal:
                baseColor = healColor;
                break;
            case DamageType.Normal:
            default:
                baseColor = normalColor;
                break;
        }

        if (textComponent != null)
        {
            textComponent.color = baseColor;
        }

        // 3. Vận tốc và vị trí
        float horizontalSpread = Random.Range(-0.3f, 0.3f);
        transform.position = startPos + new Vector3(horizontalSpread * 0.4f, 0f, 0f);
        currentVelocity = new Vector3(horizontalSpread, floatSpeed, 0f);

        // 4. Bắt đầu diễn hoạt
        transform.localScale = initialScale * scaleFactor;
        elapsedTime = 0f;
        isRunning = true;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!isRunning) return;

        elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsedTime / duration);

        transform.position += currentVelocity * Time.deltaTime;
        currentVelocity.x = Mathf.Lerp(currentVelocity.x, 0f, Time.deltaTime * 3f);

        // Hiệu ứng Pop Scale
        float currentPop;
        if (progress < 0.2f)
        {
            float popT = progress / 0.2f;
            currentPop = Mathf.Lerp(0.8f, popMultiplier, popT);
        }
        else if (progress < 0.45f)
        {
            float popT = (progress - 0.2f) / 0.25f;
            currentPop = Mathf.Lerp(popMultiplier, 1.0f, popT);
        }
        else
        {
            currentPop = Mathf.Lerp(1.0f, 0.85f, (progress - 0.45f) / 0.55f);
        }
        transform.localScale = initialScale * currentPop;

        // Hiệu ứng Fade-out
        if (textComponent != null)
        {
            float alpha = progress < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (progress - 0.5f) / 0.5f);
            Color col = baseColor;
            col.a = alpha;
            textComponent.color = col;
        }

        if (progress >= 1f)
        {
            Despawn();
        }
    }

    public void Despawn()
    {
        isRunning = false;
        if (DamageNumberManager.Instance != null)
        {
            // If registered with DamageNumberManager, return or disable
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void OnSpawnFromPool()
    {
        elapsedTime = 0f;
        isRunning = true;
    }

    public void OnReturnToPool()
    {
        isRunning = false;
        gameObject.SetActive(false);
    }
}
