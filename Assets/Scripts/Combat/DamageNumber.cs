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
/// Tích hợp icon chí mạng tự động nhận diện con số đầu tiên và giãn cách an toàn.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class DamageNumber : MonoBehaviour, IPoolable
{
    [Header("UI & Rendering")]
    [Tooltip("Tham chiếu TextMeshPro hiển thị số.")]
    [SerializeField] private TMP_Text textComponent;

    [Header("Critical Hit Sizing & Position (Tùy Chỉnh Sát Thương Chí Mạng)")]
    [Tooltip("SpriteRenderer hiển thị icon ngôi sao chí mạng.")]
    [SerializeField] private SpriteRenderer critIconRenderer;

    [Tooltip("Sprite icon chí mạng (mặc định tự nạp từ Resources/UI/CritDamageIcon).")]
    [SerializeField] private Sprite critIconSprite;

    [Tooltip("Hệ số phóng to/thu nhỏ riêng cho đòn chí mạng (Số + Icon). Mặc định 1.25. Giảm xuống nếu muốn thu nhỏ sát thương chí mạng.")]
    [Range(0.5f, 2.5f)]
    [SerializeField] private float critScaleMultiplier = 1.25f;

    [Tooltip("Kích thước icon chí mạng (Icon Size). Chỉnh ~0.38 - 0.45 để nhỏ bằng với chiều cao con số sát thương.")]
    [Range(0.1f, 1.5f)]
    [SerializeField] private float critIconSize = 0.42f;

    [Tooltip("Khoảng cách giữa icon chí mạng và dãy số sát thương. Càng nhỏ thì icon càng dịch sát vào số.")]
    [Range(-0.1f, 0.4f)]
    [SerializeField] private float critIconSpacing = 0.03f;

    [Tooltip("Độ lệch vị trí riêng của icon chí mạng (Offset X, Y). Dùng để chỉnh icon lên/xuống/trái/phải.")]
    [SerializeField] private Vector2 critIconOffset = new Vector2(0f, 0.01f);

    [Tooltip("Độ lệch vị trí xuất hiện ban đầu của sát thương chí mạng so với mục tiêu.")]
    [SerializeField] private Vector3 critSpawnOffset = new Vector3(0f, 0.35f, 0f);

    [Tooltip("Sorting Layer cho MeshRenderer để hiển thị trên quái và đạn.")]
    [SerializeField] private string sortingLayerName = "UI";

    [Tooltip("Order in Layer.")]
    [SerializeField] private int sortingOrder = 600;

    [Header("Animation Settings")]
    [Tooltip("Thời gian hiển thị (giây) trước khi tự thu hồi về Pool.")]
    [SerializeField] private float duration = 0.75f;

    [Tooltip("Tốc độ nảy bùng nổ ban đầu lên trên (Parabolic Arc).")]
    [SerializeField] private float burstSpeedY = 3.8f;

    [Tooltip("Trọng lực kéo trôi xuống êm ái.")]
    [SerializeField] private float arcGravity = 4.8f;

    [Tooltip("Lực cản không khí theo phương ngang.")]
    [SerializeField] private float dragX = 2.5f;

    [Tooltip("Hệ số kích thước chữ số cơ bản.")]
    [SerializeField] private float baseScale = 1.30f;

    [Tooltip("Hệ số kích thước riêng cho số hồi máu xanh lá (+HP). Mặc định 0.63f (đã giảm 40% so với kích thước cũ 1.05f).")]
    [Range(0.2f, 2.0f)]
    [SerializeField] private float healScaleMultiplier = 0.63f;

    [Tooltip("Độ nảy phóng to ban đầu (Pop Multiplier).")]
    [SerializeField] private float popMultiplier = 1.22f;

    [Header("Color Schemes - Solid Fallbacks")]
    [SerializeField] private Color normalColor = new Color(1f, 0.92f, 0.35f, 1f);       // Vàng hổ phách tươi
    [SerializeField] private Color criticalColor = new Color(0.94f, 0.42f, 0.30f, 1f);   // Cam san hô rực rỡ (#F06749)
    [SerializeField] private Color playerDamageColor = new Color(1f, 0.2f, 0.2f, 1f);   // Đỏ tươi nguy hiểm
    [SerializeField] private Color healColor = new Color(0.15f, 0.95f, 0.45f, 1f);      // Xanh ngọc hồi phục

    [Header("Vertex Gradients (Top / Bottom)")]
    private static readonly Color NormalGradTop = new Color(1f, 1f, 1f, 1f);             // Trắng tinh khiết sáng rõ
    private static readonly Color NormalGradBottom = new Color(1f, 0.82f, 0.08f, 1f);     // Vàng cam ấm tương phản cao

    private static readonly Color CritGradTop = new Color(0.96f, 0.48f, 0.36f, 1f);
    private static readonly Color CritGradBottom = new Color(0.88f, 0.36f, 0.24f, 1f);

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

    public SpriteRenderer CritIconRenderer => critIconRenderer;
    public bool IsCritActive => isCrit;

    public float CritScaleMultiplier
    {
        get => critScaleMultiplier;
        set
        {
            critScaleMultiplier = Mathf.Max(0.1f, value);
            SyncPreviewToManager();
        }
    }

    public float CritIconSize
    {
        get => critIconSize;
        set
        {
            critIconSize = Mathf.Max(0.05f, value);
            UpdateCritLayout();
            SyncPreviewToManager();
        }
    }

    public float CritIconSpacing
    {
        get => critIconSpacing;
        set
        {
            critIconSpacing = value;
            UpdateCritLayout();
            SyncPreviewToManager();
        }
    }

    public Vector2 CritIconOffset
    {
        get => critIconOffset;
        set
        {
            critIconOffset = value;
            UpdateCritLayout();
            SyncPreviewToManager();
        }
    }

    public Vector3 CritSpawnOffset
    {
        get => critSpawnOffset;
        set
        {
            critSpawnOffset = value;
            SyncPreviewToManager();
        }
    }

    public float HealScaleMultiplier
    {
        get => healScaleMultiplier;
        set => healScaleMultiplier = Mathf.Max(0.1f, value);
    }

    public void ConfigureCritVisuals(float scaleMultiplier, float iconSize, float iconSpacing, Vector2 iconOffset, Vector3 spawnOffset)
    {
        critScaleMultiplier = scaleMultiplier;
        critIconSize = iconSize;
        critIconSpacing = iconSpacing;
        critIconOffset = iconOffset;
        critSpawnOffset = spawnOffset;
        UpdateCritLayout();
    }

    public void SyncPreviewToManager()
    {
#if UNITY_EDITOR
        if (gameObject.name == "[DamageNumber_Preview]")
        {
            DamageNumberManager mgr = DamageNumberManager.Instance != null ? DamageNumberManager.Instance : Object.FindObjectOfType<DamageNumberManager>();
            if (mgr != null)
            {
                mgr.RecordCritVisualSettingsFromPreview(critScaleMultiplier, critIconSize, critIconSpacing, critIconOffset, critSpawnOffset);
            }
        }
#endif
    }

    private Vector3 initialScale = Vector3.one;
    private Vector3 currentVelocity;
    private Color baseColor;
    private Color gradTop;
    private Color gradBottom;
    private float targetScaleFactor = 1f;
    private float elapsedTime;
    private bool isRunning;
    private float lastAppliedAlpha = -1f;
    private bool isCrit;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        EnsureComponents();
    }

    public static string GetResolvedSortingLayer(string preferredLayer)
    {
        if (!string.IsNullOrEmpty(preferredLayer) && SortingLayer.NameToID(preferredLayer) != 0)
        {
            return preferredLayer;
        }

        if (SortingLayer.NameToID("UI") != 0) return "UI";
        if (SortingLayer.NameToID("VFX ") != 0) return "VFX ";
        if (SortingLayer.NameToID("Player") != 0) return "Player";

        return "Default";
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
            string resolvedLayer = GetResolvedSortingLayer(sortingLayerName);
            meshRenderer.sortingLayerName = resolvedLayer;
            meshRenderer.sortingOrder = sortingOrder;
        }

        if (initialScale == Vector3.zero || initialScale == Vector3.one)
        {
            initialScale = transform.localScale;
            if (initialScale == Vector3.zero) initialScale = Vector3.one;
        }

        // Tự động kiểm tra / tạo child CritIcon
        if (critIconRenderer == null)
        {
            Transform iconTr = transform.Find("CritIcon");
            if (iconTr != null)
            {
                critIconRenderer = iconTr.GetComponent<SpriteRenderer>();
            }
            else
            {
                GameObject iconObj = new GameObject("CritIcon");
                iconObj.transform.SetParent(transform, false);
                critIconRenderer = iconObj.AddComponent<SpriteRenderer>();
            }
        }

        if (critIconSprite == null)
        {
            critIconSprite = Resources.Load<Sprite>("UI/CritDamageIcon");
        }

        if (critIconRenderer != null)
        {
            if (critIconRenderer.GetComponent<CritIconProxy>() == null)
            {
                critIconRenderer.gameObject.AddComponent<CritIconProxy>();
            }
            if (critIconRenderer.sprite == null && critIconSprite != null)
            {
                critIconRenderer.sprite = critIconSprite;
            }
            string resolvedLayer = GetResolvedSortingLayer(sortingLayerName);
            critIconRenderer.sortingLayerName = resolvedLayer;
            critIconRenderer.sortingOrder = sortingOrder + 12;
            if (Application.isPlaying && !isCrit)
            {
                critIconRenderer.gameObject.SetActive(false);
            }
        }

        ApplyOutline();
    }

    public void SetSorting(string layerName, int order)
    {
        string resolvedLayer = GetResolvedSortingLayer(layerName);
        sortingLayerName = resolvedLayer;
        sortingOrder = order;
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = resolvedLayer;
            meshRenderer.sortingOrder = order;
        }

        if (critIconRenderer != null)
        {
            critIconRenderer.sortingLayerName = resolvedLayer;
            critIconRenderer.sortingOrder = order + 12;
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

    private static Material sharedNormalMaterial;
    private static Material sharedCritMaterial;

    /// <summary>
    /// Áp dụng trực tiếp thiết lập màu viền lên TextMeshPro Material dùng chung.
    /// </summary>
    public void ApplyOutline()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TMP_Text>();
        }

        if (textComponent == null || textComponent.fontSharedMaterial == null) return;
        if (!Application.isPlaying) return;

        if (isCrit)
        {
            if (sharedCritMaterial == null)
            {
                sharedCritMaterial = new Material(textComponent.fontSharedMaterial);
                sharedCritMaterial.name = "TMP_Crit_SharedMaterial";
                sharedCritMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
                sharedCritMaterial.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
                sharedCritMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
                sharedCritMaterial.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                sharedCritMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.96f, 0.96f, 0.52f, 0.85f));
                sharedCritMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.25f);
                sharedCritMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.45f);
                sharedCritMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
                sharedCritMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
            }
            textComponent.fontSharedMaterial = sharedCritMaterial;
        }
        else
        {
            if (sharedNormalMaterial == null)
            {
                sharedNormalMaterial = new Material(textComponent.fontSharedMaterial);
                sharedNormalMaterial.name = "TMP_Normal_SharedMaterial";
                sharedNormalMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
                sharedNormalMaterial.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
                sharedNormalMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
                sharedNormalMaterial.DisableKeyword(ShaderUtilities.Keyword_Underlay);
            }
            textComponent.fontSharedMaterial = sharedNormalMaterial;
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

        // 1. Định dạng nội dung hiển thị
        if (textComponent != null)
        {
            int safeAmount = Mathf.Max(0, amount);
            switch (type)
            {
                case DamageType.Critical:
                    textComponent.SetText("{0}", safeAmount);
                    textComponent.fontStyle = FontStyles.Bold | FontStyles.Italic;
                    break;
                case DamageType.PlayerDamage:
                    textComponent.SetText("-{0}", safeAmount);
                    textComponent.fontStyle = FontStyles.Bold;
                    break;
                case DamageType.Heal:
                    textComponent.SetText("+{0}", safeAmount);
                    textComponent.fontStyle = FontStyles.Bold;
                    break;
                case DamageType.Normal:
                default:
                    textComponent.SetText("{0}", safeAmount);
                    textComponent.fontStyle = FontStyles.Bold;
                    break;
            }
        }

        // 2. Thiết lập màu sắc và Vertex Gradient
        float scaleFactor = extraScale;

        if (amount >= 300) scaleFactor *= 1.15f;
        else if (amount >= 100) scaleFactor *= 1.08f;

        switch (type)
        {
            case DamageType.Critical:
                baseColor = criticalColor;
                gradTop = CritGradTop;
                gradBottom = CritGradBottom;
                scaleFactor *= critScaleMultiplier;
                SetSorting(sortingLayerName, sortingOrder + 10);
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
                scaleFactor *= healScaleMultiplier;
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

        // 3. Áp dụng viền
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

        // 4. Bố cục hiển thị liên hợp cho đòn chí mạng
        if (isCrit)
        {
            UpdateCritLayout();
        }
        else
        {
            if (critIconRenderer != null)
            {
                critIconRenderer.gameObject.SetActive(false);
            }
            if (textComponent != null)
            {
                textComponent.alignment = TextAlignmentOptions.Center;
                textComponent.transform.localPosition = Vector3.zero;
            }
        }

        // 5. Khởi tạo quỹ đạo Parabolic Arc
        float clampedDir = Mathf.Clamp(horizontalDir, -1.2f, 1.2f);
        float hSpread = clampedDir * Random.Range(0.7f, 1.2f);
        float startOffsetY = Random.Range(0.2f, 0.4f);
        Vector3 spawnOffset = isCrit ? critSpawnOffset : new Vector3(0f, startOffsetY, 0f);

        float centerCompensation = isCrit ? -GetGroupHorizontalCenterOffset() : 0f;
        transform.position = startPos + spawnOffset + new Vector3(centerCompensation + clampedDir * 0.2f, 0f, 0f);
        currentVelocity = new Vector3(hSpread, burstSpeedY * (isCrit ? 1.15f : 1f), 0f);

        transform.localScale = new Vector3(initialScale.x * 1.12f, initialScale.y * 0.9f, initialScale.z) * (baseScale * targetScaleFactor);
        elapsedTime = 0f;
        isRunning = true;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Thiết lập hiển thị tĩnh để xem trước trực tiếp trên màn Game (không bị trôi, nảy hay biến mất).
    /// </summary>
    public void SetupPreview(int amount, DamageType type = DamageType.Critical)
    {
        EnsureComponents();
        isCrit = (type == DamageType.Critical);
        isRunning = false;

        if (textComponent != null)
        {
            textComponent.text = amount.ToString();
            textComponent.fontStyle = isCrit ? (FontStyles.Bold | FontStyles.Italic) : FontStyles.Bold;
        }

        targetScaleFactor = isCrit ? critScaleMultiplier : 1f;
        baseColor = isCrit ? criticalColor : normalColor;
        gradTop = isCrit ? CritGradTop : NormalGradTop;
        gradBottom = isCrit ? CritGradBottom : NormalGradBottom;

        ApplyColorAndGradient(1f);
        ApplyOutline();
        UpdateCritLayout();

        Vector3 validInitScale = (initialScale == Vector3.zero ? Vector3.one : initialScale);
        transform.localScale = validInitScale * (baseScale * targetScaleFactor);
    }

    private void ApplyColorAndGradient(float alpha)
    {
        if (Mathf.Abs(lastAppliedAlpha - alpha) < 0.005f) return;
        lastAppliedAlpha = alpha;

        if (textComponent != null)
        {
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

        if (critIconRenderer != null && isCrit)
        {
            critIconRenderer.color = new Color(1f, 1f, 1f, alpha);
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            if (critIconRenderer != null)
            {
                UpdateCritLayout();
            }
            return;
        }

        if (!isRunning) return;

        float dt = Time.deltaTime;
        elapsedTime += dt;
        float progress = Mathf.Clamp01(elapsedTime / duration);

        // 1. Parabolic Arc
        transform.position += currentVelocity * dt;
        currentVelocity.y -= arcGravity * dt;
        currentVelocity.x = Mathf.Lerp(currentVelocity.x, 0f, dt * dragX);

        // 2. Squash & Stretch + Elastic Pop
        float currentMultiplier;
        float peakPop = isCrit ? popMultiplier * 1.18f : popMultiplier;

        if (progress < 0.15f)
        {
            float t = progress / 0.15f;
            currentMultiplier = Mathf.Lerp(0.92f, peakPop, Mathf.Sin(t * Mathf.PI * 0.5f));
        }
        else if (progress < 0.4f)
        {
            float t = (progress - 0.15f) / 0.25f;
            currentMultiplier = Mathf.Lerp(peakPop, 1.0f, t);
        }
        else
        {
            float t = (progress - 0.4f) / 0.6f;
            currentMultiplier = Mathf.Lerp(1.0f, 0.9f, t);
        }

        Vector3 finalScale = initialScale * (baseScale * targetScaleFactor * currentMultiplier);

        if (isCrit && progress < 0.25f)
        {
            float shake = Mathf.Sin(elapsedTime * 65f) * 0.03f;
            transform.position += new Vector3(shake, 0f, 0f);
        }

        transform.localScale = finalScale;

        // 3. Fade-out
        float alpha = progress < 0.65f ? 1f : Mathf.Lerp(1f, 0f, (progress - 0.65f) / 0.35f);
        ApplyColorAndGradient(alpha);

        // 4. Return to pool
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
        lastAppliedAlpha = -1f;
    }

    public void OnReturnToPool()
    {
        isRunning = false;
        if (critIconRenderer != null)
        {
            critIconRenderer.gameObject.SetActive(false);
        }
        if (textComponent != null)
        {
            textComponent.transform.localPosition = Vector3.zero;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontStyle = FontStyles.Bold;
        }
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Tự động nhận diện biên giới bên trái thực tế của con số đầu tiên trong chuỗi số sát thương.
    /// Không gọi ForceMeshUpdate() trên Main Thread để tránh spike CPU.
    /// </summary>
    public float GetFirstCharacterLeftEdge()
    {
        if (textComponent == null) return 0f;

        float prefWidth = textComponent.preferredWidth;
        if (prefWidth > 0.001f)
        {
            return -prefWidth * 0.5f;
        }

        return textComponent.textBounds.min.x;
    }

    /// <summary>
    /// Tính toán độ lệch tâm ngang của toàn bộ tổ hợp (Icon + Chữ số) so với tâm Transform.
    /// </summary>
    public float GetGroupHorizontalCenterOffset()
    {
        if (critIconRenderer == null || !critIconRenderer.gameObject.activeSelf || textComponent == null)
        {
            return 0f;
        }

        float iconLeftEdge = critIconRenderer.transform.localPosition.x - (critIconSize * 0.41f);
        float textRightEdge = textComponent.textBounds.max.x;
        return (iconLeftEdge + textRightEdge) * 0.5f;
    }

    /// <summary>
    /// Tự động nhận diện con số đầu tiên và định vị icon chí mạng luôn đứng trước bên trái,
    /// tự động giãn cách sát số (critIconSpacing) để đẹp mắt mà tuyệt đối không che khuất số sát thương.
    /// </summary>
    public void UpdateCritLayout()
    {
        if (critIconRenderer == null)
        {
            EnsureComponents();
            if (critIconRenderer == null) return;
        }

        bool shouldShow = isCrit;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            shouldShow = true;
        }
#endif

        if (!shouldShow)
        {
            critIconRenderer.gameObject.SetActive(false);
            return;
        }

        if (critIconRenderer.sprite == null && critIconSprite != null)
        {
            critIconRenderer.sprite = critIconSprite;
        }

        critIconRenderer.gameObject.SetActive(true);

        if (critIconRenderer.sprite != null)
        {
            float spriteW = critIconRenderer.sprite.rect.width / critIconRenderer.sprite.pixelsPerUnit;
            float spriteH = critIconRenderer.sprite.rect.height / critIconRenderer.sprite.pixelsPerUnit;
            float maxDim = Mathf.Max(spriteW, spriteH);
            float iconScaleFactor = maxDim > 0f ? (critIconSize / maxDim) : 1f;
            critIconRenderer.transform.localScale = new Vector3(iconScaleFactor, iconScaleFactor, 1f);
        }

        if (textComponent != null)
        {
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.transform.localPosition = Vector3.zero;
            textComponent.margin = Vector4.zero;

            float firstCharLeft = GetFirstCharacterLeftEdge();
            float iconWidth = critIconSize;

            // Bán kính hữu dụng từ tâm icon đến chóp nhọn ngoài cùng bên phải của ngôi sao.
            // Bounding box của texture CritDamageIcon.png là 465/512 (chừa 47px viền trong suốt).
            // (465 - 256) / 512 = 0.4082f => Lấy xấp xỉ 0.41f để chóp sao chạm chuẩn xác mép giãn cách.
            float starRightRadius = iconWidth * 0.41f;

            // Đặt chóp nhọn bên phải của icon cách mép trái con số đầu tiên đúng một khoảng critIconSpacing
            float targetIconCenterX = (firstCharLeft - critIconSpacing) - starRightRadius + critIconOffset.x;
            float targetIconCenterY = textComponent.textBounds.center.y + critIconOffset.y;

            critIconRenderer.transform.localPosition = new Vector3(targetIconCenterX, targetIconCenterY, 0f);

            // Đồng bộ sang proxy nếu có
            CritIconProxy proxy = critIconRenderer.GetComponent<CritIconProxy>();
            if (proxy != null)
            {
                proxy.SyncFromParent();
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureComponents();
        if (textComponent != null)
        {
            UpdateCritLayout();
        }
        SyncPreviewToManager();
    }
#endif
}
