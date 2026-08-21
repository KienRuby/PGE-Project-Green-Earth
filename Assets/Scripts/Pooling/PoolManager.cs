using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [Header("Preconfigured Pools")]
    [Tooltip("Danh sách các ObjectPool cấu hình sẵn (Prefab, kích thước khởi tạo) để khởi tạo ngay khi Awake.")]
    [SerializeField] private List<ObjectPool> pools = new List<ObjectPool>();

    private readonly Dictionary<GameObject, ObjectPool> poolDictionary = new Dictionary<GameObject, ObjectPool>();

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

        InitializePreconfiguredPools();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InitializePreconfiguredPools()
    {
        if (pools == null) return;

        foreach (ObjectPool pool in pools)
        {
            if (pool != null && pool.Prefab != null && !poolDictionary.ContainsKey(pool.Prefab))
            {
                Transform container = new GameObject($"Pool_{pool.Prefab.name}").transform;
                container.SetParent(transform);
                pool.Initialize(container);
                poolDictionary.Add(pool.Prefab, pool);
            }
        }
    }

    /// <summary>
    /// Đăng ký hoặc tạo mới một Pool cho prefab nếu chưa tồn tại.
    /// </summary>
    public ObjectPool GetOrCreatePool(GameObject prefab, int initialSize = 20, bool canGrow = true)
    {
        if (prefab == null) return null;

        if (poolDictionary.TryGetValue(prefab, out ObjectPool existingPool))
        {
            return existingPool;
        }

        Transform container = new GameObject($"Pool_{prefab.name}").transform;
        container.SetParent(transform);

        ObjectPool newPool = new ObjectPool(prefab, initialSize, canGrow, container);
        newPool.Initialize(container);
        poolDictionary.Add(prefab, newPool);
        return newPool;
    }

    /// <summary>
    /// Lấy một đối tượng từ pool hoặc Instantiate dự phòng nếu không có PoolManager trong Scene.
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;

        ObjectPool pool = GetOrCreatePool(prefab);
        if (pool != null)
        {
            return pool.Get(position, rotation, parent);
        }

        return Instantiate(prefab, position, rotation, parent);
    }

    /// <summary>
    /// Generic helper lấy Component từ Object được spawn.
    /// </summary>
    public T Spawn<T>(T prefabComponent, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
    {
        if (prefabComponent == null) return null;

        GameObject spawnedObj = Spawn(prefabComponent.gameObject, position, rotation, parent);
        return spawnedObj != null ? spawnedObj.GetComponent<T>() : null;
    }

    /// <summary>
    /// Trả đối tượng về pool quản lý hoặc Destroy nếu đối tượng không thuộc pool.
    /// </summary>
    public void ReturnToPool(GameObject instance)
    {
        if (instance == null) return;

        PoolMember member = instance.GetComponent<PoolMember>();
        if (member != null && member.Pool != null)
        {
            member.ReturnToPool();
        }
        else
        {
            Destroy(instance);
        }
    }

    /// <summary>
    /// Trả đối tượng về pool sau một khoảng thời gian trễ.
    /// </summary>
    public void ReturnToPool(GameObject instance, float delay)
    {
        if (instance == null) return;
        StartCoroutine(ReturnAfterDelayCoroutine(instance, delay));
    }

    private static readonly Dictionary<float, WaitForSeconds> timeIntervalCache = new Dictionary<float, WaitForSeconds>();

    public static WaitForSeconds GetWaitForSeconds(float seconds)
    {
        if (!timeIntervalCache.TryGetValue(seconds, out WaitForSeconds wfs))
        {
            wfs = new WaitForSeconds(seconds);
            timeIntervalCache[seconds] = wfs;
        }
        return wfs;
    }

    private IEnumerator ReturnAfterDelayCoroutine(GameObject instance, float delay)
    {
        yield return GetWaitForSeconds(delay);
        if (instance != null && instance.activeInHierarchy)
        {
            ReturnToPool(instance);
        }
    }
}
