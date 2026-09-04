using System.Collections;
using TMPro;
using UnityEngine;

public enum DamageType
{
    Normal,
    Critical,
    PlayerDamage,
    Heal
}

/// <summary>
/// Quản lý hiển thị và diễn hoạt của một số sát thương (Floating Damage Number).
/// Sử dụng TextMeshPro trong không gian 2D/3D (World-Space) với stroke viền đen sắc nét.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class DamageNumber : MonoBehaviour, IPoolable
{
    [Header("UI & Rendering")]
    [Tooltip("Tham chiếu TextMeshPro hiển thị số.")]
    [SerializeField] private TMP_Text textComponent;

    [Tooltip("Sorting Layer cho MeshRenderer để hiển thị trên quái và đạn.")]
    [SerializeField] private string sortingLayerName = "UI";

    [Tooltip("Order in Layer.")]
    [SerializeField] private int sortingOrder = 600;

    [Header("Animation Settings")]
    [Tooltip("Thời gian hiển thị (giây) trước khi tự thu hồi về Pool.")]
    [SerializeField] private float duration = 0.7f;

    [Tooltip("Tốc độ bay trôi lên trên.")]
    [SerializeField] private float floatSpeed = 1.25f;

    [Tooltip("Độ nảy phóng to ban đầu (Pop Multiplier).")]
    [SerializeField] private float popMultiplier = 1.35f;

    [Header("Color Schemes")]
    [SerializeField] private Color normalColor = new Color(1f, 0.72f, 0.18f, 1f);       // Vàng cam rực rỡ
    [SerializeField] private Color criticalColor = new Color(1f, 0.42f, 0.05f, 1f);     // Cam lửa rực
    [SerializeField] private Color playerDamageColor = new Color(1f, 0.22f, 0.22f, 1f); // Đỏ tươi nguy hiểm
    [SerializeField] private Color healColor = new Color(0.18f, 0.9f, 0.45f, 1f);       // Xanh ngọc hồi phục

    [Header("Outline Settings")]
    [Tooltip("Màu viền (Outline Color). Mặc định là viền đen sắc nét (#000000).")]
    [SerializeField] private Color outlineColor = Color.black;

    [Tooltip("Độ dày viền (Outline Width). Khuyên dùng 0.2f - 0.35f.")]
    [Range(0f, 1f)]
    [SerializeField] private float outlineWidth = 0.25f;

    [Tooltip("Cho phép dùng màu viền riêng cho từng loại sát thương.")]
    [SerializeField] private bool useCustomOutlinePerType = false;

    [SerializeField] private Color normalOutlineColor = Color.black;
    [SerializeField] private Color criticalOutlineColor = Color.black;
    [SerializeField] private Color playerDamageOutlineColor = Color.black;
    [SerializeField] private Color healOutlineColor = Color.black;

    public float Duration
    {
        get => duration;
        set => duration = Mathf.Max(0.1f, value);
    }

    public TMP_Text TextComponent => textComponent != null ? textComponent : (textComponent = GetComponent<TMP_Text>());

    public Color OutlineColor
    {
        get => outlineColor;
        set => SetOutlineColor(value);
    }

    public float OutlineWidth
    {
        get => outlineWidth;
        set => SetOutlineWidth(value);
    }

    public bool UseCustomOutlinePerType
    {
        get => useCustomOutlinePerType;
        set => useCustomOutlinePerType = value;
    }

    private Vector3 initialScale = Vector3.one;
    private Vector3 currentVelocity;
    private Color baseColor;
    private float elapsedTime;
    private bool isRunning;
    private MeshRenderer meshRenderer;

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

        ApplyOutline();
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
    /// Thay đổi màu sắc và độ dày viền của số sát thương.
    /// </summary>
    public void SetOutlineColor(Color color, float width = -1f)
    {
        outlineColor = color;
        if (width >= 0f)
        {
            outlineWidth = Mathf.Clamp01(width);
        }
        ApplyOutline();
    }

    /// <summary>
    /// Thay đổi độ dày viền.
    /// </summary>
    public void SetOutlineWidth(float width)
    {
        outlineWidth = Mathf.Clamp01(width);
        ApplyOutline();
    }

    /// <summary>
    /// Cấu hình màu viền riêng cho từng loại sát thương.
    /// </summary>
    public void ConfigureOutlinePerType(bool enable, Color normal, Color crit, Color playerDmg, Color heal)
    {
        useCustomOutlinePerType = enable;
        normalOutlineColor = normal;
        criticalOutlineColor = crit;
        playerDamageOutlineColor = playerDmg;
        healOutlineColor = heal;
    }

    /// <summary>
    /// Áp dụng trực tiếp thiết lập màu viền lên TextMeshPro Material.
    /// </summary>
    public void ApplyOutline()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TMP_Text>();
        }

        if (textComponent == null) return;

        Material mat = textComponent.fontMaterial;
        if (mat != null)
        {
            mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
        }
    }

    /// <summary>
    /// Khởi tạo và kích hoạt hiệu ứng hiển thị số sát thương.
    /// </summary>
    public void Initialize(int amount, DamageType type, Vector3 startPos, float extraScale = 1f)
    {
        EnsureComponents();

        // 1. Gán nội dung chữ
        if (textComponent != null)
        {
            textComponent.text = amount > 0 ? amount.ToString() : "0";
            if (type == DamageType.Heal)
            {
                textComponent.text = "+" + textComponent.text;
            }
        }

        // 2. Thiết lập màu sắc theo loại sát thương
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

        // 3. Áp dụng viền (tùy biến theo loại hoặc viền chuẩn mặc định)
        if (useCustomOutlinePerType)
        {
            Color targetOutline;
            switch (type)
            {
                case DamageType.Critical:
                    targetOutline = criticalOutlineColor;
                    break;
                case DamageType.PlayerDamage:
                    targetOutline = playerDamageOutlineColor;
                    break;
                case DamageType.Heal:
                    targetOutline = healOutlineColor;
                    break;
                case DamageType.Normal:
                default:
                    targetOutline = normalOutlineColor;
                    break;
            }
            SetOutlineColor(targetOutline, outlineWidth);
        }
        else
        {
            ApplyOutline();
        }

        // 3. Thiết lập vị trí và vận tốc trôi bổng (kèm độ lệch ngang ngẫu nhiên nhỏ)
        float horizontalSpread = Random.Range(-0.3f, 0.3f);
        transform.position = startPos + new Vector3(horizontalSpread * 0.4f, 0f, 0f);
        currentVelocity = new Vector3(horizontalSpread, floatSpeed, 0f);

        // 4. Kích hoạt trạng thái diễn hoạt
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

        // Di chuyển trôi lên trên và giảm dần vận tốc ngang
        transform.position += currentVelocity * Time.deltaTime;
        currentVelocity.x = Mathf.Lerp(currentVelocity.x, 0f, Time.deltaTime * 3f);

        // Hiệu ứng Pop Scale: Nảy to trong 20% đầu tiên rồi về kích thước chuẩn
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
            // Giai đoạn cuối hơi co nhỏ lại nhẹ nhàng
            currentPop = Mathf.Lerp(1.0f, 0.85f, (progress - 0.45f) / 0.55f);
        }
        transform.localScale = initialScale * currentPop;

        // Hiệu ứng mờ dần (Fade-out): Giữ alpha 100% trong 50% thời gian đầu, mờ dần về 0 ở nửa sau
        if (textComponent != null)
        {
            float alpha = progress < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (progress - 0.5f) / 0.5f);
            Color col = baseColor;
            col.a = alpha;
            textComponent.color = col;
        }

        // Khi hết thời gian -> Thu hồi về Pool
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
            DamageNumberManager.Instance.ReturnToPool(this);
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
