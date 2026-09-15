using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using UnityEngine;

namespace PGE.Auth
{
    [Serializable]
    public class PlayerCloudData
    {
        public string accountId;
        public string playerId;
        public int dataChips;
        public int redGems;
        public int advanceStones;
        public int energy;
        public int chipsetBoxes;
        public int droneBoxes;
        public string saveTimestampUtc;
        public int saveVersion;
    }

    public sealed class CloudLoadResult
    {
        public GameSaveData Data;
        public string WriteLock;
        public bool Exists => Data != null;
    }

    /// <summary>Real Unity Cloud Save transport. PlayerPrefs is never used as cloud storage.</summary>
    public static class CloudSaveSyncService
    {
        public const string CloudKey = "pge_save_v2";
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        private static string writeLock;
        private static string writeLockPlayerId;

        public static event Action<PlayerCloudData> OnCloudSaveCompleted;
        public static event Action<PlayerCloudData> OnCloudLoadCompleted;
        public static bool IsAnyCloudLoggedIn => AuthenticationServiceManager.Instance != null && AuthenticationServiceManager.Instance.IsCloudAvailable;

        public static UserProfile GetActiveUser()
        {
            if (!IsAnyCloudLoggedIn) return null;
            AuthenticationServiceManager auth = AuthenticationServiceManager.Instance;
            return auth.Provider == AuthenticationProvider.GooglePlayGames ? GoogleAuthManager.Instance?.CurrentUser : AppleAuthManager.Instance?.CurrentUser;
        }

        public static PlayerCloudData CreateCloudPayload(string accountId)
        {
            return new PlayerCloudData
            {
                accountId = accountId,
                playerId = AuthenticationServiceManager.Instance?.PlayerId ?? string.Empty,
                dataChips = PlayerDataService.DataChips,
                redGems = PlayerDataService.RedGems,
                advanceStones = PlayerDataService.AdvanceStones,
                energy = PlayerDataService.Energy,
                chipsetBoxes = PlayerDataService.ChipsetBoxes,
                droneBoxes = PlayerDataService.DroneBoxes,
                saveTimestampUtc = DateTime.UtcNow.ToString("O"),
                saveVersion = GameSaveData.CurrentVersion
            };
        }

        public static async Task<CloudLoadResult> LoadAsync(CancellationToken token = default)
        {
            EnsureAuthorized();
            string playerId = AuthenticationServiceManager.Instance.PlayerId;
            int generation = AuthenticationServiceManager.Instance.AccountGeneration;
            await Gate.WaitAsync(token);
            try
            {
                var results = await RetryAsync(() => CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { CloudKey }), token);
                EnsureSameAccount(playerId, generation);
                if (!results.TryGetValue(CloudKey, out Item item))
                {
                    writeLock = null;
                    writeLockPlayerId = playerId;
                    return new CloudLoadResult();
                }

                string json = item.Value.GetAs<string>();
                GameSaveData data = GameSaveData.Migrate(JsonUtility.FromJson<GameSaveData>(json));
                string error = data == null ? "Cloud payload is empty." : string.Empty;
                if (data == null || !data.Validate(out error))
                    throw new InvalidOperationException("Cloud save validation failed: " + error);
                if (!string.IsNullOrEmpty(data.ownerPlayerId) && data.ownerPlayerId != playerId)
                    throw new InvalidOperationException("Cloud save owner does not match the authenticated player.");
                data.ownerPlayerId = playerId;
                writeLock = item.WriteLock;
                writeLockPlayerId = playerId;
                OnCloudLoadCompleted?.Invoke(ToLegacy(data));
                return new CloudLoadResult { Data = data, WriteLock = item.WriteLock };
            }
            finally { Gate.Release(); }
        }

        public static async Task SaveAsync(GameSaveData data, CancellationToken token = default)
        {
            EnsureAuthorized();
            string playerId = AuthenticationServiceManager.Instance.PlayerId;
            int generation = AuthenticationServiceManager.Instance.AccountGeneration;
            string error = data == null ? "Save payload is empty." : string.Empty;
            if (data == null || !data.Validate(out error)) throw new InvalidOperationException(error);
            if (data.ownerPlayerId != playerId) throw new InvalidOperationException("Refusing to upload save owned by another player.");

            await Gate.WaitAsync(token);
            try
            {
                string json = JsonUtility.ToJson(data);
                Dictionary<string, string> result;
                if (writeLockPlayerId == playerId && !string.IsNullOrEmpty(writeLock))
                {
                    var values = new Dictionary<string, SaveItem> { { CloudKey, new SaveItem(json, writeLock) } };
                    result = await RetryAsync(() => CloudSaveService.Instance.Data.Player.SaveAsync(values), token);
                }
                else
                {
                    var values = new Dictionary<string, object> { { CloudKey, json } };
                    result = await RetryAsync(() => CloudSaveService.Instance.Data.Player.SaveAsync(values), token);
                }
                EnsureSameAccount(playerId, generation);
                writeLock = result[CloudKey];
                writeLockPlayerId = playerId;
                OnCloudSaveCompleted?.Invoke(ToLegacy(data));
            }
            finally { Gate.Release(); }
        }

        public static void SaveToCloud(Action<bool, string> onComplete = null) => SaveCallbackAsync(onComplete);
        public static void LoadFromCloud(Action<bool, string> onComplete = null) => LoadCallbackAsync(onComplete);

        private static async void SaveCallbackAsync(Action<bool, string> callback)
        {
            try
            {
                long revision = SaveSyncManager.Instance?.CurrentRevision + 1 ?? 1;
                GameSaveData data = GameSaveData.Capture(AuthenticationServiceManager.Instance?.PlayerId, revision);
                await SaveAsync(data);
                callback?.Invoke(true, "Cloud synced.");
            }
            catch (Exception ex) { callback?.Invoke(false, ex.Message); }
        }

        private static async void LoadCallbackAsync(Action<bool, string> callback)
        {
            try
            {
                CloudLoadResult result = await LoadAsync();
                if (!result.Exists) { callback?.Invoke(false, "No cloud save exists."); return; }
                result.Data.ApplyToPlayerPrefs();
                callback?.Invoke(true, "Cloud save restored.");
            }
            catch (Exception ex) { callback?.Invoke(false, ex.Message); }
        }

        private static async Task<T> RetryAsync<T>(Func<Task<T>> operation, CancellationToken token)
        {
            Exception last = null;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                token.ThrowIfCancellationRequested();
                try { return await operation(); }
                catch (Exception ex)
                {
                    last = ex;
                    if (attempt < 2) await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), token);
                }
            }
            throw last ?? new InvalidOperationException("Cloud operation failed.");
        }

        private static void EnsureAuthorized()
        {
            if (!IsAnyCloudLoggedIn) throw new InvalidOperationException("A verified Google or Apple account is required for recoverable cloud save.");
        }

        private static void EnsureSameAccount(string playerId, int generation)
        {
            AuthenticationServiceManager auth = AuthenticationServiceManager.Instance;
            if (auth == null || auth.PlayerId != playerId || auth.AccountGeneration != generation)
                throw new OperationCanceledException("Account changed while cloud operation was in progress.");
        }

        private static PlayerCloudData ToLegacy(GameSaveData data)
        {
            return new PlayerCloudData { accountId = data.ownerPlayerId, playerId = data.ownerPlayerId, saveTimestampUtc = data.updatedAtUtc, saveVersion = data.saveVersion };
        }
    }
}
