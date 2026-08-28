using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Số liệu độc lập của từng chipset đã chọn trong trận hiện tại.
/// Màn hình thống kê cuối trận có thể đọc trực tiếp Entries hoặc GetEntry(id).
/// </summary>
public static class ChipsetBattleStats
{
    [Serializable]
    public sealed class Entry
    {
        [SerializeField] private int chipsetId;
        [SerializeField] private int runtimeLevel;
        [SerializeField] private int configuredDamage;
        [SerializeField] private int attackCount;
        [SerializeField] private int projectileCount;
        [SerializeField] private long totalDamage;
        [SerializeField] private long totalHealing;

        public int ChipsetId => chipsetId;
        public int RuntimeLevel => runtimeLevel;
        public int ConfiguredDamage => configuredDamage;
        public int AttackCount => attackCount;
        public int ProjectileCount => projectileCount;
        public long TotalDamage => totalDamage;
        public long TotalHealing => totalHealing;

        internal Entry(int id) => chipsetId = id;

        internal void SetLevelAndDamage(int level, int damage)
        {
            runtimeLevel = Mathf.Max(runtimeLevel, level);
            configuredDamage = Mathf.Max(0, damage);
        }

        internal void AddAttack(int projectiles)
        {
            attackCount++;
            projectileCount += Mathf.Max(0, projectiles);
        }

        internal void AddDamage(int amount) => totalDamage += Mathf.Max(0, amount);
        internal void AddHealing(int amount) => totalHealing += Mathf.Max(0, amount);
    }

    private static readonly List<Entry> entries = new List<Entry>();
    private static readonly Dictionary<int, Entry> entriesById = new Dictionary<int, Entry>();

    public static IReadOnlyList<Entry> Entries => entries;

    public static void Reset()
    {
        entries.Clear();
        entriesById.Clear();
    }

    public static void RegisterChipset(int chipsetId, int runtimeLevel, int configuredDamage)
    {
        if (chipsetId <= 0) return;
        GetOrCreate(chipsetId).SetLevelAndDamage(runtimeLevel, configuredDamage);
    }

    public static Entry GetEntry(int chipsetId)
    {
        entriesById.TryGetValue(chipsetId, out Entry entry);
        return entry;
    }

    public static void RecordAttack(int chipsetId, int projectileCount)
    {
        if (chipsetId <= 0) return;
        GetOrCreate(chipsetId).AddAttack(projectileCount);
    }

    public static void RecordDamage(int chipsetId, int amount)
    {
        if (chipsetId <= 0 || amount <= 0) return;
        GetOrCreate(chipsetId).AddDamage(amount);
    }

    public static void RecordHealing(int chipsetId, int amount)
    {
        if (chipsetId <= 0 || amount <= 0) return;
        GetOrCreate(chipsetId).AddHealing(amount);
    }

    private static Entry GetOrCreate(int chipsetId)
    {
        if (entriesById.TryGetValue(chipsetId, out Entry entry)) return entry;

        entry = new Entry(chipsetId);
        entriesById.Add(chipsetId, entry);
        entries.Add(entry);
        return entry;
    }
}
