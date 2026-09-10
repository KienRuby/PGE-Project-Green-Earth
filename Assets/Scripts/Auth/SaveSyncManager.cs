using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PGE.Auth
{
    public enum SaveSyncState { LocalSaved, NotSignedIn, Syncing, CloudSynced, Offline, Failed, Conflict }

    public sealed class SaveSyncManager : MonoBehaviour
    {
        public static SaveSyncManager Instance { get; private set; }
        public static event Action StateChanged;
        public SaveSyncState State { get; private set; } = SaveSyncState.LocalSaved;
        public string LastMessage { get; private set; } = "Saved locally";
        public long CurrentRevision { get; private set; }

        private CancellationTokenSource accountOperation = new CancellationTokenSource();
        private string lastFingerprint = string.Empty;
        private bool dirty;
        private float scanTimer;
        private float cloudTimer;
        private const float ScanInterval = 1f;
        private const float CloudDebounce = 5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance != null) return;
            var host = new GameObject("[SaveSyncManager]");
            DontDestroyOnLoad(host);
            Instance = host.AddComponent<SaveSyncManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            AuthenticationServiceManager.StateChanged += OnAuthenticationChanged;
            LoadLocalAtStartup();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            AuthenticationServiceManager.StateChanged -= OnAuthenticationChanged;
            accountOperation.Cancel();
            accountOperation.Dispose();
            Instance = null;
        }

        private void Update()
        {
            scanTimer += Time.unscaledDeltaTime;
            if (scanTimer >= ScanInterval)
            {
                scanTimer = 0f;
                string owner = AuthenticationServiceManager.Instance?.IsCloudAvailable == true ? AuthenticationServiceManager.Instance.PlayerId : null;
                GameSaveData probe = GameSaveData.Capture(owner, 0);
                probe.updatedAtUtc = string.Empty;
                probe.ownerPlayerId = string.Empty;
                string fingerprint = JsonUtility.ToJson(probe);
                if (fingerprint != lastFingerprint)
                {
                    lastFingerprint = fingerprint;
                    MarkDirtyAndSaveLocal(owner);
                }
            }

            if (!dirty || AuthenticationServiceManager.Instance?.IsCloudAvailable != true) return;
            cloudTimer += Time.unscaledDeltaTime;
            if (cloudTimer >= CloudDebounce)
            {
                cloudTimer = 0f;
                Observe(UploadCurrentAsync(accountOperation.Token));
            }
        }

        public async Task ResolveAfterAuthenticationAsync()
        {
            AuthenticationServiceManager auth = AuthenticationServiceManager.Instance;
            if (auth == null || !auth.IsCloudAvailable) { SetState(SaveSyncState.NotSignedIn, "Saved locally; sign in required for cloud sync"); return; }
            ResetAccountOperation();
            CancellationToken token = accountOperation.Token;
            string playerId = auth.PlayerId;
            SetState(SaveSyncState.Syncing, "Checking cloud save...");
            try
            {
                GameSaveData local = LocalSaveService.Load(out _);
                CloudLoadResult cloud = await CloudSaveSyncService.LoadAsync(token);
                token.ThrowIfCancellationRequested();
                if (cloud.Exists)
                {
                    if (local == null || local.ownerPlayerId != playerId || cloud.Data.progressRevision > local.progressRevision)
                    {
                        cloud.Data.ApplyToPlayerPrefs();
                        CurrentRevision = cloud.Data.progressRevision;
                        LocalSaveService.Save(cloud.Data, out _);
                    }
                    else if (local.progressRevision > cloud.Data.progressRevision)
                    {
                        await CloudSaveSyncService.SaveAsync(local, token);
                        CurrentRevision = local.progressRevision;
                    }
                    else if (JsonUtility.ToJson(local) != JsonUtility.ToJson(cloud.Data))
                    {
                        SetState(SaveSyncState.Conflict, "Local and cloud saves have the same revision but different content; neither was overwritten.");
                        return;
                    }
                }
                else
                {
                    GameSaveData upload;
                    if (local == null || (!string.IsNullOrEmpty(local.ownerPlayerId) && local.ownerPlayerId != playerId))
                    {
                        upload = GameSaveData.CreateDefaults(playerId);
                        upload.ApplyToPlayerPrefs();
                    }
                    else
                    {
                        upload = local ?? GameSaveData.Capture(playerId, 1);
                        upload.ownerPlayerId = playerId;
                        upload.progressRevision = Math.Max(1, upload.progressRevision);
                        upload.updatedAtUtc = DateTime.UtcNow.ToString("O");
                    }
                    await CloudSaveSyncService.SaveAsync(upload, token);
                    CurrentRevision = upload.progressRevision;
                    LocalSaveService.Save(upload, out _);
                }
                dirty = false;
                SetState(SaveSyncState.CloudSynced, "Cloud synced");
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                dirty = true;
                SetState(Application.internetReachability == NetworkReachability.NotReachable ? SaveSyncState.Offline : SaveSyncState.Failed,
                    "Saved locally; cloud sync failed: " + ex.Message);
            }
        }

        public void MarkDirtyAndSaveLocal(string owner = null)
        {
            CurrentRevision++;
            string verifiedOwner;
            if (AuthenticationServiceManager.Instance?.IsCloudAvailable == true)
            {
                verifiedOwner = AuthenticationServiceManager.Instance.PlayerId;
            }
            else if (owner != null)
            {
                verifiedOwner = owner;
            }
            else
            {
                // Preserve account ownership while signed out. Clearing it here could let
                // the next account adopt the previous account's local progress as legacy data.
                GameSaveData existing = LocalSaveService.Load(out _);
                verifiedOwner = existing?.ownerPlayerId ?? string.Empty;
            }
            GameSaveData data = GameSaveData.Capture(verifiedOwner, CurrentRevision);
            if (LocalSaveService.Save(data, out string error))
            {
                dirty = true;
                cloudTimer = 0f;
                SetState(AuthenticationServiceManager.Instance?.IsCloudAvailable == true ? SaveSyncState.LocalSaved : SaveSyncState.NotSignedIn,
                    AuthenticationServiceManager.Instance?.IsCloudAvailable == true ? "Saved locally; waiting to sync" : "Saved locally");
            }
            else SetState(SaveSyncState.Failed, "Local save failed: " + error);
        }

        private async Task UploadCurrentAsync(CancellationToken token)
        {
            if (State == SaveSyncState.Syncing) return;
            SetState(SaveSyncState.Syncing, "Syncing...");
            try
            {
                string playerId = AuthenticationServiceManager.Instance.PlayerId;
                GameSaveData data = GameSaveData.Capture(playerId, CurrentRevision);
                LocalSaveService.Save(data, out _);
                await CloudSaveSyncService.SaveAsync(data, token);
                dirty = false;
                SetState(SaveSyncState.CloudSynced, "Cloud synced");
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                dirty = true;
                SetState(Application.internetReachability == NetworkReachability.NotReachable ? SaveSyncState.Offline : SaveSyncState.Failed,
                    "Saved locally; cloud sync failed: " + ex.Message);
            }
        }

        private void LoadLocalAtStartup()
        {
            GameSaveData local = LocalSaveService.Load(out _);
            if (local != null)
            {
                local.ApplyToPlayerPrefs();
                CurrentRevision = local.progressRevision;
            }
            else
            {
                // First run after upgrading from PlayerPrefs-only builds: capture legacy
                // progress before any authenticated empty-cloud resolution can occur.
                local = GameSaveData.Capture(string.Empty, 0);
                LocalSaveService.Save(local, out _);
            }
            lastFingerprint = string.Empty;
        }

        private void OnAuthenticationChanged()
        {
            AuthenticationServiceManager auth = AuthenticationServiceManager.Instance;
            if (auth != null && auth.IsCloudAvailable) Observe(ResolveAfterAuthenticationAsync());
            else
            {
                ResetAccountOperation();
                SetState(SaveSyncState.NotSignedIn, "Saved locally; not signed in");
            }
        }

        private void ResetAccountOperation()
        {
            accountOperation.Cancel();
            accountOperation.Dispose();
            accountOperation = new CancellationTokenSource();
        }

        private void OnApplicationPause(bool paused) { if (paused) MarkDirtyAndSaveLocal(); }
        private void OnApplicationFocus(bool focused) { if (!focused) MarkDirtyAndSaveLocal(); }
        private void OnApplicationQuit() { MarkDirtyAndSaveLocal(); }
        private void SetState(SaveSyncState state, string message) { State = state; LastMessage = message; StateChanged?.Invoke(); }
        private async void Observe(Task task) { try { await task; } catch (OperationCanceledException) { } catch (Exception ex) { SetState(SaveSyncState.Failed, ex.Message); } }
    }
}
