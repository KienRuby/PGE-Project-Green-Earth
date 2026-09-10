using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Quản lý việc đưa các Companion Drone (Buddy) vào trận đấu (GamePlay).
/// Đọc danh sách Drone đang trang bị từ PlayerDataService, khởi tạo các Prefab
/// tương ứng và kết nối chúng bay hộ tống người chơi.
/// </summary>
public class BuddyCombatManager : MonoBehaviour
{
    [System.Serializable]
    public struct BuddyPrefabEntry
    {
        public int buddyId;
        public GameObject prefab;
    }

    [Header("Registered Buddy Prefabs")]
    [Tooltip("Danh mục Prefab của từng loại Buddy theo ID (1: Sloy, 2: Turret Buffer, 10: Purifying, 3: Radar Eye, 4: Assault Blaster).")]
    [SerializeField] private List<BuddyPrefabEntry> registeredPrefabs = new List<BuddyPrefabEntry>();

    [Header("Fallback References (If list empty)")]
    [SerializeField] private GameObject sloyPrefab;
    [SerializeField] private GameObject turretBufferPrefab;
    [SerializeField] private GameObject purifyingPrefab;
    [SerializeField] private GameObject radarEyePrefab;
    [SerializeField] private GameObject assaultBlasterPrefab;

    [Header("Runtime Spawned Buddies")]
    [SerializeField] private List<BuddyCombatDrone> activeDrones = new List<BuddyCombatDrone>();

    public static BuddyCombatManager Instance { get; private set; }
    public IReadOnlyList<BuddyCombatDrone> ActiveDrones => activeDrones;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        EnsureRegisteredPrefabsLoaded();
        SpawnEquippedBuddies();
    }

    public void EnsureRegisteredPrefabsLoaded()
    {
        if (registeredPrefabs == null)
        {
            registeredPrefabs = new List<BuddyPrefabEntry>();
        }

        // Tự động thêm từ các trường lẻ nếu registeredPrefabs trống
        AddEntryIfMissing(1, sloyPrefab);
        AddEntryIfMissing(2, turretBufferPrefab);
        AddEntryIfMissing(10, purifyingPrefab);
        AddEntryIfMissing(3, radarEyePrefab);
        AddEntryIfMissing(4, assaultBlasterPrefab);

#if UNITY_EDITOR
        // Tự động tìm trong Assets/Prefabs/Buddy nếu đang trong Editor và còn thiếu
        TryLoadEditorPrefab(1, "Assets/Prefabs/Buddy/Buddy_Sloy.prefab");
        TryLoadEditorPrefab(2, "Assets/Prefabs/Buddy/Buddy_TurretBuffer.prefab");
        TryLoadEditorPrefab(10, "Assets/Prefabs/Buddy/Buddy_PurifyingDrone.prefab");
        TryLoadEditorPrefab(3, "Assets/Prefabs/Buddy/Buddy_RadarEye.prefab");
        TryLoadEditorPrefab(4, "Assets/Prefabs/Buddy/Buddy_AssaultBlaster.prefab");
#endif
    }

    private void AddEntryIfMissing(int id, GameObject prefab)
    {
        if (prefab == null) return;
        if (!registeredPrefabs.Any(e => e.buddyId == id))
        {
            registeredPrefabs.Add(new BuddyPrefabEntry { buddyId = id, prefab = prefab });
        }
    }

#if UNITY_EDITOR
    private void TryLoadEditorPrefab(int id, string path)
    {
        if (registeredPrefabs.Any(e => e.buddyId == id && e.prefab != null)) return;
        GameObject p = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (p != null)
        {
            registeredPrefabs.RemoveAll(e => e.buddyId == id);
            registeredPrefabs.Add(new BuddyPrefabEntry { buddyId = id, prefab = p });
        }
    }
#endif

    /// <summary>
    /// Đọc cấu hình trang bị từ PlayerDataService và spawn toàn bộ drone của người chơi.
    /// </summary>
    public void SpawnEquippedBuddies()
    {
        ClearAllDrones();

        int activeDeck = PlayerDataService.ActiveBuddyDeckIndex;
        int[] equippedIds = PlayerDataService.LoadBuddyDeck(activeDeck, new int[] { 1, 2, 10, 3, 4 });

        if (equippedIds == null || equippedIds.Length == 0)
        {
            Debug.Log("[BuddyCombatManager] Không có Buddy nào được trang bị trong Deck hiện tại.");
            return;
        }

        // Lọc danh sách ID hợp lệ (> 0)
        List<int> validIds = new List<int>();
        for (int i = 0; i < equippedIds.Length; i++)
        {
            if (equippedIds[i] > 0)
            {
                validIds.Add(equippedIds[i]);
            }
        }

        if (validIds.Count == 0) return;

        Debug.Log($"[BuddyCombatManager] Bắt đầu đưa {validIds.Count} Buddy vào trận đấu (Deck {activeDeck + 1}): [{string.Join(", ", validIds)}]");

        for (int i = 0; i < validIds.Count; i++)
        {
            int buddyId = validIds[i];
            GameObject prefab = GetPrefabForBuddy(buddyId);
            if (prefab == null)
            {
                Debug.LogWarning($"[BuddyCombatManager] Chưa tìm thấy Prefab cho Buddy ID: {buddyId}");
                continue;
            }

            // Đọc thông số level và tier của Buddy từ PlayerPrefs
            BuddyItemData itemData = new BuddyItemData { id = buddyId, level = 1, tier = BuddyTier.Common };
            PlayerDataService.LoadBuddyProgress(itemData);

            GameObject droneObj = Instantiate(prefab, transform.position, Quaternion.identity);
            droneObj.name = $"Buddy_{prefab.name}_{i}";

            BuddyCombatDrone droneComponent = droneObj.GetComponent<BuddyCombatDrone>();
            if (droneComponent != null)
            {
                droneComponent.Initialize(transform, i, validIds.Count, itemData.level, itemData.tier);
                activeDrones.Add(droneComponent);
            }
        }

        ApplyPassiveBuffs(validIds);
    }

    private void ApplyPassiveBuffs(List<int> validIds)
    {
        PlayerAutoShooter autoShooter = GetComponent<PlayerAutoShooter>();
        if (autoShooter == null) return;

        float critBonus = 0f;
        float damageBonus = 1f;

        foreach (int id in validIds)
        {
            if (id == 3) // Radar Eye: +5% CRIT Rate
            {
                critBonus += 0.05f;
            }
            else if (id == 4) // Assault Blaster: +12% All Weapons ATK
            {
                damageBonus += 0.12f;
            }
        }

        autoShooter.ArtifactCritBonus += critBonus;
        autoShooter.ArtifactDamageMultiplier *= damageBonus;
    }

    public GameObject GetPrefabForBuddy(int buddyId)
    {
        var entry = registeredPrefabs.FirstOrDefault(e => e.buddyId == buddyId);
        return entry.prefab;
    }

    public void ClearAllDrones()
    {
        for (int i = activeDrones.Count - 1; i >= 0; i--)
        {
            if (activeDrones[i] != null)
            {
                Destroy(activeDrones[i].gameObject);
            }
        }
        activeDrones.Clear();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        ClearAllDrones();
    }
}
