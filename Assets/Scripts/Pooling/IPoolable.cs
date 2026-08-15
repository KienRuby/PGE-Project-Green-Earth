using UnityEngine;

/// <summary>
/// Interface cho các GameObject được quản lý bởi ObjectPool.
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// Được gọi ngay sau khi Object được lấy ra từ Pool.
    /// </summary>
    void OnSpawnFromPool();

    /// <summary>
    /// Được gọi trước khi Object được thu hồi về Pool.
    /// </summary>
    void OnReturnToPool();
}
