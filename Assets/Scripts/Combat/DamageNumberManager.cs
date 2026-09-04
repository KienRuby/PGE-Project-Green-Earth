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
            tmp.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
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
        allInstances.Add(instance);
        return instance;
    }

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
}
