using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObjectPool
{
    [Header("Pool Settings")]
    [Tooltip("Prefab gốc được nhân bản vào Pool để tái sử dụng.")]
    [SerializeField] private GameObject prefab;

    [Tooltip("Số lượng đối tượng được khởi tạo sẵn (Prewarm) khi bắt đầu game.")]
    [SerializeField] private int initialSize = 20;

    [Tooltip("Cho phép Pool tự động khởi tạo thêm đối tượng mới khi hàng đợi hết đối tượng có sẵn.")]
    [SerializeField] private bool canGrow = true;

    private Queue<GameObject> poolQueue;
    private Transform poolContainer;

    public GameObject Prefab => prefab;

    public ObjectPool(GameObject prefab, int initialSize = 20, bool canGrow = true, Transform container = null)
    {
        this.prefab = prefab;
        this.initialSize = initialSize;
        this.canGrow = canGrow;
        this.poolContainer = container;
        EnsureQueueInitialized();
    }

    private void EnsureQueueInitialized()
    {
        if (poolQueue == null)
        {
            poolQueue = new Queue<GameObject>();
        }
    }

    public void Initialize(Transform container)
    {
        this.poolContainer = container;
        EnsureQueueInitialized();

        if (prefab == null)
        {
            Debug.LogWarning("[ObjectPool] Prefab chưa được gán trong Inspector!");
            return;
        }

        for (int i = 0; i < initialSize; i++)
        {
            CreateNewInstance();
        }
    }

    private GameObject CreateNewInstance()
    {
        if (prefab == null) return null;

        EnsureQueueInitialized();

        GameObject obj = Object.Instantiate(prefab, poolContainer);
        obj.name = prefab.name;
        obj.SetActive(false);

        // Đính kèm PoolMember để biết instance thuộc về pool nào
        PoolMember member = obj.GetComponent<PoolMember>();
        if (member == null)
        {
            member = obj.AddComponent<PoolMember>();
        }
        member.Pool = this;

        poolQueue.Enqueue(obj);
        return obj;
    }

    public GameObject Get(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;

        EnsureQueueInitialized();

        GameObject obj = null;

        while (poolQueue.Count > 0 && obj == null)
        {
            obj = poolQueue.Dequeue();
        }

        if (obj == null)
        {
            if (canGrow)
            {
                obj = CreateNewInstance();
                if (poolQueue.Count > 0)
                {
                    poolQueue.Dequeue(); // Lấy đối tượng vừa tạo ra khỏi hàng đợi
                }
            }
            else
            {
                Debug.LogWarning($"[ObjectPool] Pool {prefab.name} đã đầy và canGrow = false!");
                return null;
            }
        }

        if (obj == null) return null;

        obj.transform.SetParent(parent != null ? parent : poolContainer);
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        IPoolable[] poolables = obj.GetComponentsInChildren<IPoolable>(true);
        for (int i = 0; i < poolables.Length; i++)
        {
            poolables[i].OnSpawnFromPool();
        }

        return obj;
    }

    public void Return(GameObject obj)
    {
        if (obj == null) return;

        EnsureQueueInitialized();

        if (poolQueue.Contains(obj))
        {
            return;
        }

        IPoolable[] poolables = obj.GetComponentsInChildren<IPoolable>(true);
        for (int i = 0; i < poolables.Length; i++)
        {
            poolables[i].OnReturnToPool();
        }

        obj.SetActive(false);
        obj.transform.SetParent(poolContainer);
        poolQueue.Enqueue(obj);
    }
}

/// <summary>
/// Component nội bộ giúp một GameObject tự biết pool quản lý nó.
/// </summary>
public class PoolMember : MonoBehaviour
{
    public ObjectPool Pool { get; set; }

    public void ReturnToPool()
    {
        if (Pool != null)
        {
            Pool.Return(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
