using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Quản lý và tính toán số liệu chiến đấu độc lập của từng Chipset/Kỹ năng trong trận hiện tại.
/// Hỗ trợ hiển thị bảng Damage Details (DPS, Tỷ lệ %, Tổng sát thương, Thời gian).
/// </summary>
public static class ChipsetBattleStats
{
    [Serializable]
    public sealed class Entry
    {
        [SerializeField] private int chipsetId;
        [SerializeField] private int runtimeLevel = 1;
        [SerializeField] private int configuredDamage;
        [SerializeField] private int attackCount;
        [SerializeField] private int projectileCount;
        [SerializeField] private long totalDamage;
        [SerializeField] private long totalHealing;
        [SerializeField] private float firstRegisteredTime;
        [SerializeField] private float lastActiveTime;

        public int ChipsetId => chipsetId;
        public int RuntimeLevel => runtimeLevel;
        public int ConfiguredDamage => configuredDamage;
        public int AttackCount => attackCount;
        public int ProjectileCount => projectileCount;
        public long TotalDamage => totalDamage;
        public long TotalHealing => totalHealing;
        public float FirstRegisteredTime => firstRegisteredTime;
        public float LastActiveTime => lastActiveTime;

        public float ActiveDuration
        {
            get
            {
                float now = battleEndTime > 0f ? battleEndTime : (Application.isPlaying ? Time.time : 1f);
                float duration = now - firstRegisteredTime;
                return Mathf.Max(1f, duration);
            }
        }

        public int DPS
        {
            get
            {
                float duration = ActiveDuration;
                if (duration <= 0f) return (int)totalDamage;
                return (int)Math.Round((double)totalDamage / duration);
            }
        }

        public string FormattedTime
        {
            get
            {
                int totalSecs = Mathf.Max(0, (int)ActiveDuration);
                int minutes = totalSecs / 60;
                int seconds = totalSecs % 60;
                return $"{minutes:00}:{seconds:00}";
            }
        }

        public string ChipsetName => GetChipsetName(chipsetId);
        public string IconKey => GetChipsetIconKey(chipsetId);

        public float GetDamagePercent(long grandTotal)
        {
            if (grandTotal <= 0) return totalDamage > 0 ? 100f : 0f;
            return Mathf.Clamp((float)((double)totalDamage / grandTotal * 100.0), 0f, 100f);
        }

        internal Entry(int id, float time)
        {
            chipsetId = id;
            firstRegisteredTime = time;
            lastActiveTime = time;
        }

        internal void SetLevelAndDamage(int level, int damage, float time)
        {
            runtimeLevel = Mathf.Max(runtimeLevel, level);
            configuredDamage = Mathf.Max(0, damage);
            lastActiveTime = time;
        }

        internal void AddAttack(int projectiles, float time)
        {
            attackCount++;
            projectileCount += Mathf.Max(0, projectiles);
            lastActiveTime = time;
        }

        internal void AddDamage(int amount, float time)
        {
            totalDamage += Mathf.Max(0, amount);
            lastActiveTime = time;
        }

        internal void AddHealing(int amount, float time)
        {
            totalHealing += Mathf.Max(0, amount);
            lastActiveTime = time;
        }
    }

    private static readonly List<Entry> entries = new List<Entry>();
    private static readonly Dictionary<int, Entry> entriesById = new Dictionary<int, Entry>();
    private static float battleStartTime;
    private static float battleEndTime;

    public static IReadOnlyList<Entry> Entries => entries;
    public static float BattleStartTime => battleStartTime;
    public static float BattleEndTime => battleEndTime;

    public static long GrandTotalDamage
    {
        get
        {
            long sum = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                sum += entries[i].TotalDamage;
            }
            return sum;
        }
    }

    public static void Reset()
    {
        entries.Clear();
        entriesById.Clear();
        battleStartTime = Application.isPlaying ? Time.time : 0f;
        battleEndTime = 0f;

        // Mặc định luôn khởi tạo Standard Gun (ID 1) ngay từ đầu trận
        RegisterChipset(1, 1, 20);
    }

    public static void FinalizeBattle()
    {
        battleEndTime = Application.isPlaying ? Time.time : (battleStartTime + 1f);
    }

    public static void RegisterChipset(int chipsetId, int runtimeLevel, int configuredDamage)
    {
        if (chipsetId <= 0) return;
        float now = Application.isPlaying ? Time.time : 0f;
        GetOrCreate(chipsetId, now).SetLevelAndDamage(runtimeLevel, configuredDamage, now);
    }

    public static Entry GetEntry(int chipsetId)
    {
        entriesById.TryGetValue(chipsetId, out Entry entry);
        return entry;
    }

    public static Entry GetStats(int chipsetId) => GetEntry(chipsetId);

    public static void RecordAttack(int chipsetId, int projectileCount)
    {
        if (chipsetId <= 0) return;
        float now = Application.isPlaying ? Time.time : 0f;
        GetOrCreate(chipsetId, now).AddAttack(projectileCount, now);
    }

    public static void RecordDamage(int chipsetId, int amount)
    {
        if (amount <= 0) return;
        int safeId = chipsetId > 0 ? chipsetId : 1;
        float now = Application.isPlaying ? Time.time : 0f;
        GetOrCreate(safeId, now).AddDamage(amount, now);
    }

    public static void RecordHealing(int chipsetId, int amount)
    {
        if (chipsetId <= 0 || amount <= 0) return;
        float now = Application.isPlaying ? Time.time : 0f;
        GetOrCreate(chipsetId, now).AddHealing(amount, now);
    }

    public static List<Entry> GetSortedEntries()
    {
        // Ưu tiên hiển thị các chipset đã gây sát thương hoặc đã bắn, sắp xếp giảm dần theo TotalDamage
        return entries
            .Where(e => e.TotalDamage > 0 || e.AttackCount > 0 || e.TotalHealing > 0 || e.RuntimeLevel > 0)
            .OrderByDescending(e => e.TotalDamage)
            .ThenByDescending(e => e.AttackCount)
            .ToList();
    }

    private static Entry GetOrCreate(int chipsetId, float now)
    {
        if (entriesById.TryGetValue(chipsetId, out Entry entry)) return entry;

        entry = new Entry(chipsetId, now);
        entriesById.Add(chipsetId, entry);
        entries.Add(entry);
        return entry;
    }

    public static string GetChipsetName(int chipsetId)
    {
        switch (chipsetId)
        {
            case 1: return "Standard Gun";
            case 2: return "Rifle";
            case 3: return "Rocket Punch";
            case 4: return "Spinning Blade";
            case 5: return "Multigun";
            case 6: return "Gun Turret";
            case 7: return "Spiky Discus";
            case 8: return "Shotgun";
            case 9: return "Energy Jumper Cables";
            case 10: return "High-Explosive Mine";
            case 11: return "Sonic Boom";
            case 12: return "Healing Turret";
            case 13: return "Aiming Lens";
            case 14: return "Ice Turret";
            case 15: return "Flamethrower";
            case 16: return "ATK Module";
            case 17: return "Laser Eye";
            case 18: return "Black Hole Mine";
            case 19: return "Invincible Shield";
            case 20: return "Big Battery";
            case 21: return "Plasma Field";
            case 22: return "Biochemical Mine";
            case 23: return "Tesla Coil";
            case 24: return "Turret Module";
            default: return $"Chipset #{chipsetId}";
        }
    }

    public static string GetChipsetIconKey(int chipsetId)
    {
        switch (chipsetId)
        {
            case 1: return "standard-gun";
            case 2: return "rifle";
            case 3: return "rocket-punch";
            case 4: return "spinning-blade";
            case 5: return "multigun";
            case 6: return "gun-turret";
            case 7: return "spiky-discus";
            case 8: return "shotgun";
            case 9: return "energy-jumper-cables";
            case 10: return "high-explosive-mine";
            default: return chipsetId.ToString();
        }
    }
}
