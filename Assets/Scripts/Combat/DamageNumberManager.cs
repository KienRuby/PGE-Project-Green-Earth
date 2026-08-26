using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Quản lý tập trung Object Pool và hiển thị số sát thương trong trận đấu.
/// Hỗ trợ nạp Font Nunito có viền đen sắc nét, tối ưu hóa Zero-GC cho game Survivor.
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

    [Tooltip("Kích thước chữ số (FontSize).")]
    [SerializeField] private float defaultFontSize = 5f;

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "UI";
    [SerializeField] private int sortingOrder = 600;

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
        if (damage <= 0 && type != DamageType.Heal) return;

        if (Instance == null)
        {
            GameObject managerObj = new GameObject("[DamageNumberManager]");
            Instance = managerObj.AddComponent<DamageNumberManager>();
        }

        Instance.SpawnDamage(worldPosition, damage, type, extraScale);
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
        allInstances.Add(instance);
        return instance;
    }

    public void SpawnDamage(Vector3 worldPosition, int damage, DamageType type = DamageType.Normal, float extraScale = 1f)
    {
        DamageNumber instance = GetFromPool();
        if (instance != null)
        {
            instance.Initialize(damage, type, worldPosition, extraScale);
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

        foreach (DamageNumber item in allInstances)
        {
            if (item != null && item.TextComponent != null)
            {
                if (newFont != null) item.TextComponent.font = newFont;
                if (newStrokeMat != null) item.TextComponent.fontSharedMaterial = newStrokeMat;
            }
        }
    }
}
