using System;
using System.Collections.Generic;
using UnityEngine;

namespace PGE.Auth
{
    [Serializable]
    public sealed class SaveValue
    {
        public string key;
        public string stringValue;
        public int intValue;
        public bool isString;
    }

    [Serializable]
    public sealed class GameSaveData
    {
        public const int CurrentVersion = 2;
        public int saveVersion = CurrentVersion;
        public long progressRevision;
        public string updatedAtUtc;
        public string ownerPlayerId;
        public List<SaveValue> values = new List<SaveValue>();

        private static readonly string[] RequiredIntKeys =
        {
            PlayerDataService.DataChipsKey, PlayerDataService.RedGemsKey,
            PlayerDataService.EnergyKey, PlayerDataService.AdvanceStonesKey,
            PlayerDataService.ChipsetBoxesKey, PlayerDataService.DroneBoxesKey,
            PlayerDataService.CompletedRollsKey, PlayerDataService.LabPityCounterKey,
            PlayerDataService.LabElitePityCounterKey, PlayerDataService.LabEpicPityCounterKey,
            PlayerDataService.LabLegendPityCounterKey, PlayerDataService.SelectedChapterIndexKey,
            PlayerDataService.UnlockedChapterIndexKey, PlayerDataService.VipOwnedKey,
            PlayerDataService.ChipsetActiveDeckKey, PlayerDataService.BuddyActiveDeckKey,
            DailyLoginManager.CurrentDayKey, DailyLoginManager.ClaimedMaskKey,
            DailyLoginManager.CycleCountKey
        };

        private static readonly int[] RequiredIntDefaults =
        {
            1000, 1000, 100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0
        };

        private static readonly string[] RequiredStringKeys =
        {
            PlayerDataService.NextEnergyUtcKey, PlayerDataService.SelectedWeaponIdKey,
            DailyLoginManager.LastLoginDateUtcKey, DailyLoginManager.LastClaimDateUtcKey,
            DailyLoginManager.LastAdClaimDateUtcKey
        };

        private static readonly string[] LabItems =
        {
            "HP", "RECOVERY", "AUTO RECOVERY", "DEF", "ATK", "CRIT RATE",
            "CRIT DAMAGE", "OBTAINED CHIPS", "RANGED DEFENSE", "DRONE ATK",
            "TURRET ATK", "TURRET DURATION", "EVADE", "LIFE STEAL",
            "MOVE SPEED", "CHIPSET SELECTION"
        };

        private static readonly string[] AchievementIds =
        {
            "login_reward_2", "drone_upgrade_3", "chapter_play_15",
            "enemy_kill_2500", "chapter_clear_5"
        };

        public static GameSaveData Capture(string ownerPlayerId, long revision)
        {
            var data = new GameSaveData
            {
                ownerPlayerId = ownerPlayerId ?? string.Empty,
                progressRevision = Math.Max(0, revision),
                updatedAtUtc = DateTime.UtcNow.ToString("O")
            };

            for (int i = 0; i < RequiredIntKeys.Length; i++)
                data.AddInt(RequiredIntKeys[i], PlayerPrefs.GetInt(RequiredIntKeys[i], RequiredIntDefaults[i]));
            foreach (string key in RequiredStringKeys)
                data.AddString(key, PlayerPrefs.GetString(key, key == PlayerDataService.SelectedWeaponIdKey ? "blaster" : string.Empty));

            foreach (string item in LabItems)
                data.AddInt(PlayerDataService.FormatItemLevelKey(item), PlayerPrefs.GetInt(PlayerDataService.FormatItemLevelKey(item), 0));

            for (int deck = 0; deck < 3; deck++)
            {
                data.AddExistingString(PlayerDataService.GetDeckKey(deck));
                data.AddExistingString(PlayerDataService.GetBuddyDeckKey(deck));
            }

            for (int id = 1; id <= 10; id++)
            {
                string prefix = PlayerDataService.GetChipItemPrefix(id);
                data.AddExistingInt(prefix + "Level");
                data.AddExistingInt(prefix + "Tier");
                data.AddExistingInt(prefix + "Count");
                data.AddExistingInt(prefix + "ReqCount");
                data.AddExistingInt(prefix + "HasStar");
                data.AddExistingInt(prefix + "TierEnhanceCount");
                data.AddExistingInt(prefix + "EnhanceCost");
            }

            for (int id = 1; id <= 12; id++)
            {
                data.AddExistingInt(PlayerDataService.BuddyLevelKeyPrefix + id);
                data.AddExistingInt(PlayerDataService.BuddyTierKeyPrefix + id);
            }

            foreach (string id in AchievementIds)
            {
                data.AddExistingInt(AchievementManager.ProgressKeyPrefix + id);
                data.AddExistingInt(AchievementManager.ClaimedKeyPrefix + id);
            }

            data.AddExistingInt(QuestWidgetController.QuestClaimedKeyPrefix + "quest_lab_upgrade_01");
            data.AddExistingString("PGE.Shop.Daily.free-gem");
            data.AddExistingString("PGE.Shop.Daily.daily-drone-1");
            data.AddExistingString("PGE.Shop.Daily.daily-drone-2");
            return data;
        }

        public static GameSaveData CreateDefaults(string ownerPlayerId, long revision = 1)
        {
            var data = new GameSaveData
            {
                ownerPlayerId = ownerPlayerId ?? string.Empty,
                progressRevision = Math.Max(1, revision),
                updatedAtUtc = DateTime.UtcNow.ToString("O")
            };
            for (int i = 0; i < RequiredIntKeys.Length; i++) data.AddInt(RequiredIntKeys[i], RequiredIntDefaults[i]);
            foreach (string key in RequiredStringKeys) data.AddString(key, key == PlayerDataService.SelectedWeaponIdKey ? "blaster" : string.Empty);
            foreach (string item in LabItems) data.AddInt(PlayerDataService.FormatItemLevelKey(item), 0);
            return data;
        }

        public bool Validate(out string error)
        {
            if (saveVersion < 1 || saveVersion > CurrentVersion)
            {
                error = "Unsupported save version: " + saveVersion;
                return false;
            }
            if (progressRevision < 0 || values == null || values.Count > 512)
            {
                error = "Invalid revision or value count.";
                return false;
            }
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (SaveValue value in values)
            {
                if (value == null || string.IsNullOrWhiteSpace(value.key) || value.key.Length > 160 ||
                    !keys.Add(value.key) || (value.stringValue != null && value.stringValue.Length > 4096))
                {
                    error = "Malformed or duplicate save entry.";
                    return false;
                }
            }
            error = string.Empty;
            return true;
        }

        public void ApplyToPlayerPrefs()
        {
            if (!Validate(out string error))
                throw new InvalidOperationException(error);
            ClearManagedPlayerPrefs();
            foreach (SaveValue value in values)
            {
                if (value.isString) PlayerPrefs.SetString(value.key, value.stringValue ?? string.Empty);
                else PlayerPrefs.SetInt(value.key, value.intValue);
            }
            PlayerPrefs.Save();
        }

        public static void ClearManagedPlayerPrefs()
        {
            foreach (string key in RequiredIntKeys) PlayerPrefs.DeleteKey(key);
            foreach (string key in RequiredStringKeys) PlayerPrefs.DeleteKey(key);
            foreach (string item in LabItems) PlayerPrefs.DeleteKey(PlayerDataService.FormatItemLevelKey(item));

            for (int deck = 0; deck < 3; deck++)
            {
                PlayerPrefs.DeleteKey(PlayerDataService.GetDeckKey(deck));
                PlayerPrefs.DeleteKey(PlayerDataService.GetBuddyDeckKey(deck));
            }

            for (int id = 1; id <= 10; id++)
            {
                string prefix = PlayerDataService.GetChipItemPrefix(id);
                PlayerPrefs.DeleteKey(prefix + "Level");
                PlayerPrefs.DeleteKey(prefix + "Tier");
                PlayerPrefs.DeleteKey(prefix + "Count");
                PlayerPrefs.DeleteKey(prefix + "ReqCount");
                PlayerPrefs.DeleteKey(prefix + "HasStar");
                PlayerPrefs.DeleteKey(prefix + "TierEnhanceCount");
                PlayerPrefs.DeleteKey(prefix + "EnhanceCost");
            }

            for (int id = 1; id <= 12; id++)
            {
                PlayerPrefs.DeleteKey(PlayerDataService.BuddyLevelKeyPrefix + id);
                PlayerPrefs.DeleteKey(PlayerDataService.BuddyTierKeyPrefix + id);
            }

            foreach (string id in AchievementIds)
            {
                PlayerPrefs.DeleteKey(AchievementManager.ProgressKeyPrefix + id);
                PlayerPrefs.DeleteKey(AchievementManager.ClaimedKeyPrefix + id);
            }

            PlayerPrefs.DeleteKey(QuestWidgetController.QuestClaimedKeyPrefix + "quest_lab_upgrade_01");
            PlayerPrefs.DeleteKey("PGE.Shop.Daily.free-gem");
            PlayerPrefs.DeleteKey("PGE.Shop.Daily.daily-drone-1");
            PlayerPrefs.DeleteKey("PGE.Shop.Daily.daily-drone-2");
        }

        public static GameSaveData Migrate(GameSaveData data)
        {
            if (data == null) return null;
            if (data.saveVersion == 1) data.saveVersion = CurrentVersion;
            return data;
        }

        private void AddInt(string key, int value) => values.Add(new SaveValue { key = key, intValue = value });
        private void AddString(string key, string value) => values.Add(new SaveValue { key = key, stringValue = value ?? string.Empty, isString = true });
        private void AddExistingInt(string key) { if (PlayerPrefs.HasKey(key)) AddInt(key, PlayerPrefs.GetInt(key)); }
        private void AddExistingString(string key) { if (PlayerPrefs.HasKey(key)) AddString(key, PlayerPrefs.GetString(key)); }
    }
}
