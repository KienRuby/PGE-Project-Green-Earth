using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Quản lý tập trung Object Pool và hiển thị số sát thương trong trận đấu.
/// Hỗ trợ nạp Font Nunito có viền đen sắc nét, đổi màu viền linh hoạt, tối ưu hóa Zero-GC.
/// </summary>
public class DamageNumberManager : MonoBehaviour
{
    public static DamageNumberManager Instance { get; private set; }

    [Header("Prefab & Pool Settings")]
    [Tooltip("Prefab của DamageNumber. Nếu để trống, Manager sẽ tự động kiến tạo Prefab tối ưu từ code.")]
    [SerializeField] private DamageNumber damageNumberPrefab;

    [Tooltip("Số lượng đối tượng nạp sẵn ban đầu.")]
    [SerializeField] private int initialPoolSize = 60;

    [Tooltip("Font chữ TextMeshPro (mặc định Nunito SDF).")]
    [SerializeField] private TMP_FontAsset fontAsset;

    [Tooltip("Material có viền Stroke đen (mặc định Nunito SDF - Stroke).")]
    [SerializeField] private Material strokeMaterial;

    [Tooltip("Kích thước chữ số (FontSize) - được thu gọn để hiển thị tinh gọn, sắc nét.")]
    [SerializeField] private float defaultFontSize = 2.4f;

    [Tooltip("Sprite icon chí mạng (nếu để trống, tự nạp từ Resources/UI/CritDamageIcon).")]
    [SerializeField] private Sprite critIconSprite;

    [Header("Critical Hit Sizing & Position (Tùy Chỉnh Sát Thương Chí Mạng)")]
    [Tooltip("Hệ số phóng to/thu nhỏ riêng cho đòn chí mạng (Số + Icon). Mặc định 0.76f theo tinh chỉnh người dùng.")]
    [Range(0.5f, 2.5f)]
    [SerializeField] private float defaultCritScale = 0.76f;

    [Tooltip("Kích thước icon chí mạng (Icon Size). Tinh chỉnh 0.249f theo người dùng.")]
    [Range(0.1f, 1.5f)]
    [SerializeField] private float defaultCritIconSize = 0.249f;

    [Tooltip("Khoảng cách giữa icon chí mạng và dãy số sát thương. Tinh chỉnh 0.021f theo người dùng.")]
    [Range(-0.1f, 0.4f)]
    [SerializeField] private float defaultCritIconSpacing = 0.021f;

    [Tooltip("Độ lệch vị trí riêng của icon chí mạng (Offset X, Y). Dùng để chỉnh icon lên/xuống/trái/phải.")]
    [SerializeField] private Vector2 defaultCritIconOffset = new Vector2(0f, 0.01f);

    [Tooltip("Độ lệch vị trí xuất hiện ban đầu của sát thương chí mạng so với mục tiêu.")]
    [SerializeField] private Vector3 defaultCritSpawnOffset = new Vector3(0f, 0.35f, 0f);

    [Tooltip("Tự động đồng bộ các thông số kích thước/vị trí chí mạng ở trên vào các số sát thương trong trận đấu.")]
    [SerializeField] private bool syncCritSettingsToInstances = true;

    [Header("Game View Live Preview (Xem Trước Trên Màn Game)")]
    [Tooltip("Bật hiển thị mẫu số sát thương chí mạng ngay trên màn hình Game để dễ căn chỉnh kích thước.")]
    [SerializeField] private bool showGameViewPreview = false;

    [Tooltip("Vị trí hiển thị số sát thương xem trước trong màn Game (mặc định ngay trên đầu Player: 0.015, 1.4, 0).")]
    [SerializeField] private Vector3 previewPosition = new Vector3(0.015f, 1.4f, 0f);

    [Tooltip("Con số sát thương hiển thị mẫu.")]
    [SerializeField] private int previewDamageAmount = 10000;

    [Header("Outline Settings")]
    [Tooltip("Màu viền mặc định cho số sát thương (mặc định viền đen sắc nét).")]
    [SerializeField] private Color defaultOutlineColor = Color.black;

    [Tooltip("Độ dày viền mặc định (khuyên dùng 0.2f - 0.35f).")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultOutlineWidth = 0.25f;

    [Tooltip("Bật tùy chọn đổi màu viền theo loại sát thương ở cấp Manager.")]
    [SerializeField] private bool useManagerOutlinePerType = false;

    [SerializeField] private Color normalOutlineColor = Color.black;
    [SerializeField] private Color criticalOutlineColor = Color.black;
    [SerializeField] private Color playerDamageOutlineColor = Color.black;
    [SerializeField] private Color healOutlineColor = Color.black;

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "UI";
    [SerializeField] private int sortingOrder = 600;

    public Color DefaultOutlineColor
    {
        get => defaultOutlineColor;
        set => SetDefaultOutline(value, defaultOutlineWidth);
    }

    public float DefaultOutlineWidth
    {
        get => defaultOutlineWidth;
        set => SetDefaultOutline(defaultOutlineColor, value);
    }

    public bool UseManagerOutlinePerType
    {
        get => useManagerOutlinePerType;
        set => useManagerOutlinePerType = value;
    }

    public float DefaultCritScale
    {
        get => defaultCritScale;
        set
        {
            defaultCritScale = value;
            if (syncCritSettingsToInstances) ApplyCritSettingsToAllInstances();
            UpdateGameViewPreview();
        }
    }

    public float DefaultCritIconSize
    {
        get => defaultCritIconSize;
        set
        {
            defaultCritIconSize = value;
            if (syncCritSettingsToInstances) ApplyCritSettingsToAllInstances();
            UpdateGameViewPreview();
        }
    }

    public float DefaultCritIconSpacing
    {
        get => defaultCritIconSpacing;
        set
        {
            defaultCritIconSpacing = value;
            if (syncCritSettingsToInstances) ApplyCritSettingsToAllInstances();
            UpdateGameViewPreview();
        }
    }

    public Vector2 DefaultCritIconOffset
    {
        get => defaultCritIconOffset;
        set
        {
            defaultCritIconOffset = value;
            if (syncCritSettingsToInstances) ApplyCritSettingsToAllInstances();
            UpdateGameViewPreview();
        }
    }

    public Vector3 DefaultCritSpawnOffset
    {
        get => defaultCritSpawnOffset;
        set
        {
            defaultCritSpawnOffset = value;
            if (syncCritSettingsToInstances) ApplyCritSettingsToAllInstances();
            UpdateGameViewPreview();
        }
    }

    public bool ShowGameViewPreview
    {
        get => showGameViewPreview;
        set
        {
            showGameViewPreview = value;
            UpdateGameViewPreview();
        }
    }

    public Vector3 PreviewPosition
    {
        get => previewPosition;
        set
        {
            previewPosition = value;
            UpdateGameViewPreview();
        }
    }

    public int PreviewDamageAmount
    {
        get => previewDamageAmount;
        set
        {
            previewDamageAmount = value;
            UpdateGameViewPreview();
        }
    }

    public void ApplyCritSettingsToAllInstances()
    {
        foreach (DamageNumber item in allInstances)
        {
            if (item != null)
            {
                item.ConfigureCritVisuals(defaultCritScale, defaultCritIconSize, defaultCritIconSpacing, defaultCritIconOffset, defaultCritSpawnOffset);
            }
        }
    }

    /// <summary>
    /// Ghi nhận và lưu giữ thông số kích thước chí mạng từ đối tượng xem trước hoặc do người dùng kéo trực tiếp.
    /// Đảm bảo khi bấm Play vào trận đấu, các số sát thương trong trận sẽ hiển thị đúng 100% kích thước đã chỉnh.
    /// </summary>
    public void RecordCritVisualSettingsFromPreview(float scale, float iconSize, float spacing, Vector2 offset, Vector3 spawnOffset)
    {
        defaultCritScale = scale;
        defaultCritIconSize = iconSize;
        defaultCritIconSpacing = spacing;
        defaultCritIconOffset = offset;
        defaultCritSpawnOffset = spawnOffset;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    /// <summary>
    /// Đồng bộ thông số từ mẫu xem trước [DamageNumber_Preview] hoặc từ Prefab gốc để không bị ghi đè mất cấu hình.
    /// </summary>
    public void SyncFromPreviewOrPrefab()
    {
        DamageNumber preview = null;
#if UNITY_2023_1_OR_NEWER
        DamageNumber[] all = Object.FindObjectsByType<DamageNumber>(FindObjectsSortMode.None);
#else
        DamageNumber[] all = Object.FindObjectsOfType<DamageNumber>();
#endif
        foreach (var dn in all)
        {
            if (dn != null && dn.gameObject.name == "[DamageNumber_Preview]")
            {
                preview = dn;
                break;
            }
        }

        if (preview != null)
        {
            defaultCritScale = preview.CritScaleMultiplier;
            defaultCritIconSize = preview.CritIconSize;
            defaultCritIconSpacing = preview.CritIconSpacing;
            defaultCritIconOffset = preview.CritIconOffset;
            defaultCritSpawnOffset = preview.CritSpawnOffset;
        }
        else if (damageNumberPrefab != null)
        {
            defaultCritScale = damageNumberPrefab.CritScaleMultiplier;
            defaultCritIconSize = damageNumberPrefab.CritIconSize;
            defaultCritIconSpacing = damageNumberPrefab.CritIconSpacing;
            defaultCritIconOffset = damageNumberPrefab.CritIconOffset;
            defaultCritSpawnOffset = damageNumberPrefab.CritSpawnOffset;
        }
    }

    private readonly Queue<DamageNumber> poolQueue = new Queue<DamageNumber>();
    private readonly List<DamageNumber> allInstances = new List<DamageNumber>();
    private Transform poolContainer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Đảm bảo ShowDamage luôn bật theo mặc định nếu bị PlayerPrefs vô tình lưu là 0
        if (!PlayerPrefs.HasKey(GameSettings.ShowDamageKey) || PlayerPrefs.GetInt(GameSettings.ShowDamageKey, 1) == 0)
        {
            GameSettings.ShowDamage = true;
        }

        SyncFromPreviewOrPrefab();
        HideGameViewPreview();
        InitializePool();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// API tĩnh tiện lợi gọi hiển thị số sát thương từ bất kỳ đâu.
    /// Tự động khởi tạo Manager nếu chưa có sẵn trong Scene.
    /// </summary>
    public static void ShowDamage(Vector3 worldPosition, int damage, DamageType type = DamageType.Normal, float extraScale = 1f)
    {
        if (!GameSettings.ShowDamage) return;
        if (damage <= 0 && type != DamageType.Heal) return;

        if (Instance == null)
        {
            GameObject managerObj = new GameObject("[DamageNumberManager]");
            if (!Application.isPlaying)
            {
                managerObj.hideFlags = HideFlags.DontSave;
            }
            Instance = managerObj.AddComponent<DamageNumberManager>();
        }

        Instance.SpawnDamage(worldPosition, damage, type, extraScale);
    }

    /// <summary>
    /// Đổi màu viền toàn cục cho tất cả số sát thương.
    /// </summary>
    public static void SetGlobalOutlineColor(Color color, float width = -1f)
    {
        if (Instance != null)
        {
            Instance.SetDefaultOutline(color, width);
        }
    }

    /// <summary>
    /// Đổi màu viền mặc định và cập nhật tất cả instance trong pool.
    /// </summary>
    public void SetDefaultOutline(Color color, float width = -1f)
    {
        defaultOutlineColor = color;
        if (width >= 0f)
        {
            defaultOutlineWidth = Mathf.Clamp01(width);
        }

        if (strokeMaterial != null)
        {
            strokeMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
            strokeMaterial.SetColor(ShaderUtilities.ID_OutlineColor, defaultOutlineColor);
            strokeMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, defaultOutlineWidth);
        }

        foreach (DamageNumber item in allInstances)
        {
            if (item != null)
            {
                item.SetOutlineColor(defaultOutlineColor, defaultOutlineWidth);
            }
        }
    }

    public Color GetOutlineForType(DamageType type)
    {
        switch (type)
        {
            case DamageType.Critical:
                return criticalOutlineColor;
            case DamageType.PlayerDamage:
                return playerDamageOutlineColor;
            case DamageType.Heal:
                return healOutlineColor;
            case DamageType.Normal:
            default:
                return normalOutlineColor;
        }
    }

    public void SetOutlineForType(DamageType type, Color color)
    {
        switch (type)
        {
            case DamageType.Critical:
                criticalOutlineColor = color;
                break;
            case DamageType.PlayerDamage:
                playerDamageOutlineColor = color;
                break;
            case DamageType.Heal:
                healOutlineColor = color;
                break;
            case DamageType.Normal:
            default:
                normalOutlineColor = color;
                break;
        }
    }

    public void InitializePool()
    {
        if (poolContainer == null)
        {
            GameObject containerObj = new GameObject("DamageNumberPool");
            containerObj.transform.SetParent(transform);
            poolContainer = containerObj.transform;
        }

        LoadFontAndMaterialResources();

        // Tạo sẵn các instance trong pool
        int currentCount = allInstances.Count;
        for (int i = currentCount; i < initialPoolSize; i++)
        {
            DamageNumber instance = CreateNewInstance();
            instance.gameObject.SetActive(false);
            poolQueue.Enqueue(instance);
        }
    }

    private void LoadFontAndMaterialResources()
    {
#if UNITY_EDITOR
        if (fontAsset == null)
        {
            fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito/Nunito SDF.asset");
        }
        if (strokeMaterial == null)
        {
            strokeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/Nunito/Nunito SDF - Stroke.mat");
        }
#endif
        if (fontAsset == null)
        {
            fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }
        if (fontAsset == null)
        {
            fontAsset = TMP_Settings.defaultFontAsset;
        }

        if (critIconSprite == null)
        {
            critIconSprite = Resources.Load<Sprite>("UI/CritDamageIcon");
        }

        // Đảm bảo material có keyword OUTLINE_ON và cấu hình màu viền
        if (strokeMaterial != null)
        {
            strokeMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
            strokeMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.28f);
            strokeMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0.18f);
            strokeMaterial.SetColor(ShaderUtilities.ID_OutlineColor, defaultOutlineColor);
        }
        else if (fontAsset != null && fontAsset.material != null)
        {
            strokeMaterial = new Material(fontAsset.material);
            strokeMaterial.name = fontAsset.name + " - DynamicStroke";
            strokeMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
            strokeMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.28f);
            strokeMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0.18f);
            strokeMaterial.SetColor(ShaderUtilities.ID_OutlineColor, defaultOutlineColor);
        }
    }

    private DamageNumber CreateNewInstance()
    {
        DamageNumber instance;

        if (damageNumberPrefab != null)
        {
            instance = Instantiate(damageNumberPrefab, poolContainer);
            if (instance != null && instance.TextComponent != null)
            {
                if (fontAsset != null && instance.TextComponent.font == null)
                {
                    instance.TextComponent.font = fontAsset;
                }
                if (strokeMaterial != null && instance.TextComponent.fontSharedMaterial == null)
                {
                    instance.TextComponent.fontSharedMaterial = strokeMaterial;
                }
            }
        }
        else
        {
            GameObject obj = new GameObject("DamageNumber");
            obj.transform.SetParent(poolContainer);

            // Gắn TextMeshPro 3D (World Space)
            TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = defaultFontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableWordWrapping = false;
            string resolvedLayer = DamageNumber.GetResolvedSortingLayer(sortingLayerName);
            tmp.sortingLayerID = SortingLayer.NameToID(resolvedLayer);
            tmp.sortingOrder = sortingOrder;

            if (fontAsset != null)
            {
                tmp.font = fontAsset;
            }

            if (strokeMaterial != null)
            {
                tmp.fontSharedMaterial = strokeMaterial;
            }

            instance = obj.AddComponent<DamageNumber>();
        }

        instance.SetSorting(sortingLayerName, sortingOrder);
        instance.EnsureComponents();
        instance.SetOutlineColor(defaultOutlineColor, defaultOutlineWidth);
        if (syncCritSettingsToInstances)
        {
            instance.ConfigureCritVisuals(defaultCritScale, defaultCritIconSize, defaultCritIconSpacing, defaultCritIconOffset, defaultCritSpawnOffset);
        }
        allInstances.Add(instance);
        return instance;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (syncCritSettingsToInstances && Application.isPlaying && allInstances.Count > 0)
        {
            ApplyCritSettingsToAllInstances();
        }

        if (!Application.isPlaying)
        {
            if (syncCritSettingsToInstances)
            {
                ApplyCritSettingsToAllInstances();
            }
            UpdateGameViewPreview();
        }
    }
#endif

    private Vector3 lastSpawnPos;
    private float lastSpawnTime;
    private int consecutiveHitCount;

    public void SpawnDamage(Vector3 worldPosition, int damage, DamageType type = DamageType.Normal, float extraScale = 1f)
    {
        DamageNumber instance = GetFromPool();
        if (instance != null)
        {
            float now = Time.time;
            if (now - lastSpawnTime < 0.22f && Vector3.Distance(worldPosition, lastSpawnPos) < 0.85f)
            {
                consecutiveHitCount++;
            }
            else
            {
                consecutiveHitCount = 0;
            }

            lastSpawnPos = worldPosition;
            lastSpawnTime = now;

            // Tính hướng tản số theo hình quạt (Alternating fan-out) để chống trùng đè khi xả đạn nhanh
            float dir = 0f;
            if (consecutiveHitCount > 0)
            {
                int step = (consecutiveHitCount % 6);
                float sign = (step % 2 == 1) ? 1f : -1f;
                dir = sign * (0.35f + (step * 0.15f));
            }
            else
            {
                dir = Random.Range(-0.6f, 0.6f);
            }

            instance.InitializeWithDirection(damage, type, worldPosition, dir, extraScale);
            if (useManagerOutlinePerType)
            {
                Color outline = GetOutlineForType(type);
                instance.SetOutlineColor(outline, defaultOutlineWidth);
            }
        }
    }

    public DamageNumber GetFromPool()
    {
        if (poolContainer == null)
        {
            InitializePool();
        }

        DamageNumber instance;
        if (poolQueue.Count > 0)
        {
            instance = poolQueue.Dequeue();
        }
        else
        {
            // Nếu dùng hết, tự động mở rộng thêm
            instance = CreateNewInstance();
        }

        instance.OnSpawnFromPool();
        return instance;
    }

    public void ReturnToPool(DamageNumber instance)
    {
        if (instance == null) return;

        instance.OnReturnToPool();
        if (poolContainer != null)
        {
            instance.transform.SetParent(poolContainer);
        }

        if (!poolQueue.Contains(instance))
        {
            poolQueue.Enqueue(instance);
        }
    }

    public void ConfigureFont(TMP_FontAsset newFont, Material newStrokeMat)
    {
        fontAsset = newFont;
        strokeMaterial = newStrokeMat;

        if (strokeMaterial != null)
        {
            strokeMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
            strokeMaterial.SetColor(ShaderUtilities.ID_OutlineColor, defaultOutlineColor);
            strokeMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, defaultOutlineWidth);
        }

        foreach (DamageNumber item in allInstances)
        {
            if (item != null && item.TextComponent != null)
            {
                if (newFont != null) item.TextComponent.font = newFont;
                if (newStrokeMat != null) item.TextComponent.fontSharedMaterial = newStrokeMat;
                item.SetOutlineColor(defaultOutlineColor, defaultOutlineWidth);
            }
        }
    }

    /// <summary>
    /// Hiển thị đối tượng xem trước trực tiếp trên màn Game (Game View) và Scene View.
    /// Giúp người phát triển dễ dàng quan sát, thu nhỏ/phóng to kích thước sát thương chí mạng
    /// tương quan trực tiếp với nhân vật Player.
    /// </summary>
    public void UpdateGameViewPreview()
    {
        DamageNumber preview = null;
#if UNITY_2023_1_OR_NEWER
        DamageNumber[] all = Object.FindObjectsByType<DamageNumber>(FindObjectsSortMode.None);
#else
        DamageNumber[] all = Object.FindObjectsOfType<DamageNumber>();
#endif
        foreach (var dn in all)
        {
            if (dn != null && dn.gameObject.name == "[DamageNumber_Preview]")
            {
                preview = dn;
                break;
            }
        }

        if (!showGameViewPreview)
        {
            HideGameViewPreview();
            return;
        }

        if (preview == null)
        {
            if (damageNumberPrefab != null)
            {
                GameObject obj = Instantiate(damageNumberPrefab.gameObject);
                obj.name = "[DamageNumber_Preview]";
                preview = obj.GetComponent<DamageNumber>();
            }
            else
            {
                return;
            }
        }

        if (preview != null)
        {
            preview.gameObject.SetActive(true);
            preview.transform.position = previewPosition;

            if (fontAsset != null && preview.TextComponent != null)
            {
                preview.TextComponent.font = fontAsset;
            }
            if (strokeMaterial != null && preview.TextComponent != null)
            {
                preview.TextComponent.fontSharedMaterial = strokeMaterial;
            }
            if (critIconSprite != null && preview.CritIconRenderer != null)
            {
                preview.CritIconRenderer.sprite = critIconSprite;
            }

            preview.ConfigureCritVisuals(defaultCritScale, defaultCritIconSize, defaultCritIconSpacing, defaultCritIconOffset, defaultCritSpawnOffset);
            preview.EnsureComponents();

            preview.SetupPreview(previewDamageAmount, DamageType.Critical);
        }
    }

    /// <summary>
    /// Tắt đối tượng xem trước khi bắt đầu chơi thực tế hoặc dọn sạch trong Editor.
    /// </summary>
    public void HideGameViewPreview()
    {
#if UNITY_2023_1_OR_NEWER
        DamageNumber[] all = Object.FindObjectsByType<DamageNumber>(FindObjectsSortMode.None);
#else
        DamageNumber[] all = Object.FindObjectsOfType<DamageNumber>();
#endif
        foreach (var dn in all)
        {
            if (dn != null && dn.gameObject.name == "[DamageNumber_Preview]")
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Object.DestroyImmediate(dn.gameObject);
                }
                else
                {
                    dn.gameObject.SetActive(false);
                }
#else
                dn.gameObject.SetActive(false);
#endif
            }
        }
    }
}
