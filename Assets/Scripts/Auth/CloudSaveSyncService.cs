using System;
using UnityEngine;

namespace PGE.Auth
{
    /// <summary>
    /// Du lieu dong bo dam may duoc ma hoa dong goi tu PlayerDataService va GameSettings.
    /// </summary>
    [Serializable]
    public class PlayerCloudData
    {
        public string accountId;
        public string playerId;
        public int dataChips;
        public int redGems;
        public int advanceStones;
        public int energy;
        public string saveTimestampUtc;
        public int saveVersion;

        public PlayerCloudData()
        {
            saveVersion = 1;
            saveTimestampUtc = DateTime.UtcNow.ToString("o");
        }
    }

    /// <summary>
    /// Dich vu tu dong dong bo va luu tru tien trinh len Cloud khi dang nhap tai khoan Google.
    /// </summary>
    public static class CloudSaveSyncService
    {
        private const string PrefKeyCloudBackup = "PGE.CloudSave.BackupData";
        private const string PrefKeyLastSyncUtc = "PGE.CloudSave.LastSyncUtc";

        public static event Action<PlayerCloudData> OnCloudSaveCompleted;
        public static event Action<PlayerCloudData> OnCloudLoadCompleted;

        /// <summary>
        /// Dong goi toan bo tien trinh hien tai tu PlayerDataService de luu len Cloud.
        /// </summary>
        public static PlayerCloudData CreateCloudPayload(string accountId)
        {
            return new PlayerCloudData
            {
                accountId = accountId,
                playerId = GameSettings.LocalPlayerId,
                dataChips = PlayerDataService.DataChips,
                redGems = PlayerDataService.RedGems,
                advanceStones = PlayerDataService.AdvanceStones,
                energy = PlayerDataService.Energy,
                saveTimestampUtc = DateTime.UtcNow.ToString("o"),
                saveVersion = 1
            };
        }

        public static UserProfile GetActiveUser()
        {
            if (GoogleAuthManager.Instance != null && GoogleAuthManager.Instance.IsLoggedIn)
                return GoogleAuthManager.Instance.CurrentUser;
            if (AppleAuthManager.Instance != null && AppleAuthManager.Instance.IsLoggedIn)
                return AppleAuthManager.Instance.CurrentUser;
            return null;
        }

        public static bool IsAnyCloudLoggedIn => GetActiveUser() != null;

        /// <summary>
        /// Thuc hien luu du lieu len Cloud.
        /// </summary>
        public static void SaveToCloud(Action<bool, string> onComplete = null)
        {
            UserProfile activeUser = GetActiveUser();
            if (activeUser == null)
            {
                onComplete?.Invoke(false, "Chưa đăng nhập tài khoản đám mây (Google hoặc Apple).");
                return;
            }

            try
            {
                string accountId = activeUser.userId;
                PlayerCloudData payload = CreateCloudPayload(accountId);
                string json = JsonUtility.ToJson(payload);

                PlayerPrefs.SetString(PrefKeyCloudBackup + "_" + accountId, json);
                PlayerPrefs.SetString(PrefKeyLastSyncUtc, payload.saveTimestampUtc);
                PlayerPrefs.Save();

                Debug.Log($"<color=#00FF99>[CloudSave] Đồng bộ Cloud thành công cho tài khoản {activeUser.authProvider} ({accountId})</color>");
                OnCloudSaveCompleted?.Invoke(payload);
                onComplete?.Invoke(true, "Đồng bộ đám mây thành công!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CloudSave] Lỗi khi lưu lên Cloud: {ex.Message}");
                onComplete?.Invoke(false, ex.Message);
            }
        }

        /// <summary>
        /// Tai tien trinh tu Cloud ve va hop nhat vao PlayerDataService.
        /// </summary>
        public static void LoadFromCloud(Action<bool, string> onComplete = null)
        {
            UserProfile activeUser = GetActiveUser();
            if (activeUser == null)
            {
                onComplete?.Invoke(false, "Chưa đăng nhập tài khoản đám mây (Google hoặc Apple).");
                return;
            }

            try
            {
                string accountId = activeUser.userId;
                string key = PrefKeyCloudBackup + "_" + accountId;

                if (PlayerPrefs.HasKey(key))
                {
                    string json = PlayerPrefs.GetString(key, string.Empty);
                    if (!string.IsNullOrEmpty(json))
                    {
                        PlayerCloudData cloudData = JsonUtility.FromJson<PlayerCloudData>(json);
                        if (cloudData != null)
                        {
                            // Hop nhat tien te (Lay gia tri cao hon giua Cloud va Local de bao ve tien trinh)
                            PlayerDataService.DataChips = Mathf.Max(PlayerDataService.DataChips, cloudData.dataChips);
                            PlayerDataService.RedGems = Mathf.Max(PlayerDataService.RedGems, cloudData.redGems);
                            PlayerDataService.AdvanceStones = Mathf.Max(PlayerDataService.AdvanceStones, cloudData.advanceStones);

                            Debug.Log($"<color=#00FF99>[CloudSave] Da tai va hop nhat tien trinh tu Cloud cho {accountId}</color>");
                            OnCloudLoadCompleted?.Invoke(cloudData);
                            onComplete?.Invoke(true, "Đã khôi phục dữ liệu từ đám mây!");
                            return;
                        }
                    }
                }

                // Neu chua co du lieu tren cloud, tao moi tu local
                SaveToCloud((success, msg) =>
                {
                    onComplete?.Invoke(success, success ? "Đã khởi tạo lưu đám mây mới." : msg);
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CloudSave] Loi khi tai tu Cloud: {ex.Message}");
                onComplete?.Invoke(false, ex.Message);
            }
        }
    }
}
