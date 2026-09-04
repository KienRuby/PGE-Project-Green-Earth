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
/// Sử dụng TextMeshPro trong không gian World-Space với chuyển động Parabolic Arc,
/// hiệu ứng Squash & Stretch nảy bùng nổ, Vertex Gradient rực rỡ và stroke viền đen sắc nét.
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
    [SerializeField] private float duration = 0.75f;

    [Tooltip("Tốc độ nảy bùng nổ ban đầu lên trên (Parabolic Arc).")]
    [SerializeField] private float burstSpeedY = 3.2f;

    [Tooltip("Trọng lực kéo trôi xuống êm ái.")]
    [SerializeField] private float arcGravity = 4.8f;

    [Tooltip("Lực cản không khí theo phương ngang.")]
    [SerializeField] private float dragX = 2.5f;

    [Tooltip("Hệ số thu nhỏ kích thước chữ số để tinh gọn, không che quái.")]
    [SerializeField] private float baseScale = 0.65f;

    [Tooltip("Độ nảy phóng to ban đầu (Pop Multiplier).")]
    [SerializeField] private float popMultiplier = 1.22f;

    [Header("Color Schemes - Solid Fallbacks")]
    [SerializeField] private Color normalColor = new Color(1f, 0.92f, 0.35f, 1f);       // Vàng hổ phách tươi
    [SerializeField] private Color criticalColor = new Color(1f, 0.35f, 0.05f, 1f);     // Cam lửa rực
    [SerializeField] private Color playerDamageColor = new Color(1f, 0.2f, 0.2f, 1f);   // Đỏ tươi nguy hiểm
    [SerializeField] private Color healColor = new Color(0.15f, 0.95f, 0.45f, 1f);      // Xanh ngọc hồi phục

    [Header("Vertex Gradients (Top / Bottom)")]
    private static readonly Color NormalGradTop = new Color(1f, 1f, 1f, 1f);             // Trắng tinh khiết sáng rõ
    private static readonly Color NormalGradBottom = new Color(1f, 0.82f, 0.08f, 1f);     // Vàng cam ấm tương phản cao

    private static readonly Color CritGradTop = new Color(1f, 0.98f, 0.3f, 1f);
    private static readonly Color CritGradBottom = new Color(1f, 0.18f, 0.02f, 1f);

    private static readonly Color PlayerGradTop = new Color(1f, 0.65f, 0.65f, 1f);
    private static readonly Color PlayerGradBottom = new Color(0.95f, 0.08f, 0.08f, 1f);

    private static readonly Color HealGradTop = new Color(0.8f, 1f, 0.9f, 1f);
    private static readonly Color HealGradBottom = new Color(0.05f, 0.9f, 0.4f, 1f);

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
    private Color gradTop;
    private Color gradBottom;
    private float targetScaleFactor = 1f;
    private float elapsedTime;
    private bool isRunning;
    private bool isCrit;
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

        if (textComponent != null)
        {
            textComponent.fontStyle = FontStyles.Bold;
            if (textComponent.fontSharedMaterial != null)
            {
                textComponent.fontSharedMaterial.EnableKeyword("OUTLINE_ON");
                textComponent.fontSharedMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.28f);
                textComponent.fontSharedMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0.18f);
                textComponent.fontSharedMaterial.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            }
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
    /// Khởi tạo và kích hoạt hiệu ứng hiển thị số sát thương với chuyển động nảy vòng cung.
    /// </summary>
    public void Initialize(int amount, DamageType type, Vector3 startPos, float extraScale = 1f)
    {
        InitializeWithDirection(amount, type, startPos, Random.Range(-1f, 1f), extraScale);
    }

    /// <summary>
    /// Khởi tạo với hướng tản số (horizontalDir: -1 đến +1) giúp chống trùng đè khi bắn liên thanh.
    /// </summary>
    public void InitializeWithDirection(int amount, DamageType type, Vector3 startPos, float horizontalDir, float extraScale = 1f)
    {
        EnsureComponents();

        isCrit = (type == DamageType.Critical);

        // 1. Định dạng nội dung hiển thị theo phong cách game Survivor
        if (textComponent != null)
        {
            string numStr = amount > 0 ? amount.ToString() : "0";
            switch (type)
            {
                case DamageType.Critical:
                    textComponent.text = "CRIT " + numStr + "!";
                    break;
                case DamageType.PlayerDamage:
                    textComponent.text = "-" + numStr;
                    break;
                case DamageType.Heal:
                    textComponent.text = "+" + numStr;
                    break;
                case DamageType.Normal:
                default:
                    textComponent.text = numStr;
                    break;
            }
        }

        // 2. Thiết lập màu sắc và Vertex Gradient
        float scaleFactor = extraScale;

        // Dynamic scale nhẹ theo độ lớn sát thương
        if (amount >= 300) scaleFactor *= 1.15f;
        else if (amount >= 100) scaleFactor *= 1.08f;

        switch (type)
        {
            case DamageType.Critical:
                baseColor = criticalColor;
                gradTop = CritGradTop;
                gradBottom = CritGradBottom;
                scaleFactor *= 1.35f;
                SetSorting(sortingLayerName, sortingOrder + 10); // Ưu tiên hiển thị đòn chí mạng lên trên
                break;
            case DamageType.PlayerDamage:
                baseColor = playerDamageColor;
                gradTop = PlayerGradTop;
                gradBottom = PlayerGradBottom;
                scaleFactor *= 1.08f;
                SetSorting(sortingLayerName, sortingOrder + 5);
                break;
            case DamageType.Heal:
                baseColor = healColor;
                gradTop = HealGradTop;
                gradBottom = HealGradBottom;
                scaleFactor *= 1.05f;
                SetSorting(sortingLayerName, sortingOrder + 5);
                break;
            case DamageType.Normal:
            default:
                baseColor = normalColor;
                gradTop = NormalGradTop;
                gradBottom = NormalGradBottom;
                SetSorting(sortingLayerName, sortingOrder);
                break;
        }

        targetScaleFactor = scaleFactor;
        ApplyColorAndGradient(1f);

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

        // 4. Khởi tạo quỹ đạo Parabolic Arc (bật nảy vòng cung)
        float clampedDir = Mathf.Clamp(horizontalDir, -1.2f, 1.2f);
        float hSpread = clampedDir * Random.Range(0.7f, 1.2f);
        float startOffsetY = Random.Range(0.2f, 0.4f);

        transform.position = startPos + new Vector3(clampedDir * 0.2f, startOffsetY, 0f);
        currentVelocity = new Vector3(hSpread, burstSpeedY * (isCrit ? 1.15f : 1f), 0f);

        // 4. Kích hoạt trạng thái diễn hoạt (Squash & Stretch ban đầu)
        transform.localScale = new Vector3(initialScale.x * 1.12f, initialScale.y * 0.9f, initialScale.z) * (baseScale * targetScaleFactor);
        elapsedTime = 0f;
        isRunning = true;
        gameObject.SetActive(true);
    }

    private void ApplyColorAndGradient(float alpha)
    {
        if (textComponent == null) return;

        Color c = baseColor;
        c.a = alpha;
        textComponent.color = c;

        textComponent.enableVertexGradient = true;
        Color top = gradTop;
        top.a = alpha;
        Color btm = gradBottom;
        btm.a = alpha;
        textComponent.colorGradient = new VertexGradient(top, top, btm, btm);
    }

    private void Update()
    {
        if (!isRunning) return;

        float dt = Time.deltaTime;
        elapsedTime += dt;
        float progress = Mathf.Clamp01(elapsedTime / duration);

        // 1. Cập nhật vị trí vật lý Parabolic Arc
        transform.position += currentVelocity * dt;

        // Trọng lực kéo trôi xuống dần
        currentVelocity.y -= arcGravity * dt;
        // Giảm dần vận tốc ngang theo lực cản
        currentVelocity.x = Mathf.Lerp(currentVelocity.x, 0f, dt * dragX);

        // 2. Hiệu ứng Squash & Stretch + Elastic Overshoot (Pop)
        float currentMultiplier;
        float peakPop = isCrit ? popMultiplier * 1.18f : popMultiplier;

        if (progress < 0.15f)
        {
            // Bùng nổ phóng to nhanh (Overshoot)
            float t = progress / 0.15f;
            currentMultiplier = Mathf.Lerp(0.92f, peakPop, Mathf.Sin(t * Mathf.PI * 0.5f));
        }
        else if (progress < 0.4f)
        {
            // Đàn hồi co về kích thước chuẩn (Elastic settle)
            float t = (progress - 0.15f) / 0.25f;
            currentMultiplier = Mathf.Lerp(peakPop, 1.0f, t);
        }
        else
        {
            // Giai đoạn cuối hơi thu nhỏ nhẹ nhàng
            float t = (progress - 0.4f) / 0.6f;
            currentMultiplier = Mathf.Lerp(1.0f, 0.9f, t);
        }

        Vector3 finalScale = initialScale * (baseScale * targetScaleFactor * currentMultiplier);

        // Hiệu ứng rung nhẹ điểm nhấn đòn Chí Mạng ở 0.2s đầu
        if (isCrit && progress < 0.25f)
        {
            float shake = Mathf.Sin(elapsedTime * 65f) * 0.03f;
            transform.position += new Vector3(shake, 0f, 0f);
        }

        transform.localScale = finalScale;

        // 3. Hiệu ứng mờ dần (Fade-out): Giữ sắc nét 65% thời gian đầu, chỉ mờ dần ở 35% cuối
        float alpha = progress < 0.65f ? 1f : Mathf.Lerp(1f, 0f, (progress - 0.65f) / 0.35f);
        ApplyColorAndGradient(alpha);

        // 4. Thu hồi về Pool khi hết thời gian
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

