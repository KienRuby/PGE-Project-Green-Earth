using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace PGE.Auth
{
    public enum AuthenticationProvider { None, Guest, GooglePlayGames, Apple }
    public enum AuthenticationState { Uninitialized, Initializing, Guest, SigningIn, Authenticated, Offline, Failed }

    public sealed class AuthenticationServiceManager : MonoBehaviour
    {
        public static AuthenticationServiceManager Instance { get; private set; }
        public static event Action StateChanged;

        public AuthenticationState State { get; private set; } = AuthenticationState.Uninitialized;
        public AuthenticationProvider Provider { get; private set; } = AuthenticationProvider.None;
        public string PlayerId => IsAuthenticated ? AuthenticationService.Instance.PlayerId : string.Empty;
        public bool IsAuthenticated => UnityServices.State == ServicesInitializationState.Initialized &&
                                       AuthenticationService.Instance.IsSignedIn &&
                                       AuthenticationService.Instance.IsAuthorized;
        public bool IsGuest => IsAuthenticated && Provider == AuthenticationProvider.Guest;
        public bool IsCloudAvailable => IsAuthenticated &&
            (Provider == AuthenticationProvider.GooglePlayGames || Provider == AuthenticationProvider.Apple);
        public string LastError { get; private set; } = string.Empty;
        public int AccountGeneration { get; private set; }

        private CancellationTokenSource lifetime = new CancellationTokenSource();
        private Task initializationTask;
#if UNITY_IOS && !UNITY_EDITOR
        private TaskCompletionSource<string> appleTokenCompletion;
        [DllImport("__Internal")] private static extern void PGE_StartAppleSignIn(string gameObjectName);
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance != null) return;
            ClearLegacyFakeAuthenticationFlags();
            var host = new GameObject("[AuthenticationServiceManager]");
            DontDestroyOnLoad(host);
            Instance = host.AddComponent<AuthenticationServiceManager>();
            Instance.Observe(Instance.InitializeAsync());
        }

        private static void ClearLegacyFakeAuthenticationFlags()
        {
            PlayerPrefs.DeleteKey("PGE.Auth.GoogleUserSession");
            PlayerPrefs.DeleteKey("PGE.Auth.IsLoggedIn");
            PlayerPrefs.DeleteKey("PGE.Auth.AppleUserSession");
            PlayerPrefs.DeleteKey("PGE.Auth.AppleIsLoggedIn");
            PlayerPrefs.DeleteKey(GameSettings.GoogleAccountKey);
            PlayerPrefs.DeleteKey(GameSettings.AppleAccountKey);
            PlayerPrefs.Save();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            lifetime.Cancel();
            lifetime.Dispose();
            Instance = null;
        }

        public Task InitializeAsync()
        {
            return initializationTask ?? (initializationTask = InitializeCoreAsync());
        }

        private async Task InitializeCoreAsync()
        {
            SetState(AuthenticationState.Initializing);
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    Task init = UnityServices.InitializeAsync();
                    if (await Task.WhenAny(init, Task.Delay(TimeSpan.FromSeconds(15), lifetime.Token)) != init)
                        throw new TimeoutException("Unity Services initialization timed out.");
                    await init;
                    SubscribeAuthenticationEvents();

                    Task signIn = AuthenticationService.Instance.SignInAnonymouslyAsync();
                    if (await Task.WhenAny(signIn, Task.Delay(TimeSpan.FromSeconds(15), lifetime.Token)) != signIn)
                        throw new TimeoutException("Authentication session restore timed out.");
                    await signIn;
                    await RefreshProviderFromServerAsync();
                    SetState(IsCloudAvailable ? AuthenticationState.Authenticated : AuthenticationState.Guest);
                    return;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    LastError = SafeMessage(ex);
                    if (attempt < 2) await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), lifetime.Token);
                }
            }
            Provider = AuthenticationProvider.None;
            SetState(Application.internetReachability == NetworkReachability.NotReachable ? AuthenticationState.Offline : AuthenticationState.Failed);
        }

        public async Task<bool> SignInGoogleAsync()
        {
            await InitializeAsync();
#if UNITY_ANDROID && !UNITY_EDITOR
            if (State == AuthenticationState.SigningIn) return false;
            SetState(AuthenticationState.SigningIn);
            try
            {
                PlayGamesPlatform.Activate();
                var authCompletion = new TaskCompletionSource<SignInStatus>();
                PlayGamesPlatform.Instance.ManuallyAuthenticate(status => authCompletion.TrySetResult(status));
                SignInStatus status = await authCompletion.Task;
                if (status != SignInStatus.Success) throw new InvalidOperationException("Google Play Games sign-in was cancelled or failed: " + status);

                var codeCompletion = new TaskCompletionSource<string>();
                PlayGamesPlatform.Instance.RequestServerSideAccess(false, code => codeCompletion.TrySetResult(code));
                string authCode = await codeCompletion.Task;
                if (string.IsNullOrWhiteSpace(authCode)) throw new InvalidOperationException("Google Play Games did not return a server auth code.");
                bool linked = await LinkGoogleCredentialAsync(authCode);
                if (!linked) return false;
                Provider = AuthenticationProvider.GooglePlayGames;
                AccountGeneration++;
                SetState(AuthenticationState.Authenticated);
                return true;
            }
            catch (Exception ex)
            {
                LastError = SafeMessage(ex);
                SetState(IsAuthenticated ? (IsCloudAvailable ? AuthenticationState.Authenticated : AuthenticationState.Guest) : AuthenticationState.Failed);
                return false;
            }
#else
            LastError = "Google Play Games sign-in is available only in an Android device build. No Editor account was created.";
            SetState(IsAuthenticated ? AuthenticationState.Guest : AuthenticationState.Failed);
            return false;
#endif
        }

        public async Task<bool> SignInAppleAsync()
        {
            await InitializeAsync();
#if UNITY_IOS && !UNITY_EDITOR
            if (State == AuthenticationState.SigningIn) return false;
            SetState(AuthenticationState.SigningIn);
            try
            {
                appleTokenCompletion = new TaskCompletionSource<string>();
                PGE_StartAppleSignIn(gameObject.name);
                Task completed = await Task.WhenAny(appleTokenCompletion.Task, Task.Delay(TimeSpan.FromSeconds(90), lifetime.Token));
                if (completed != appleTokenCompletion.Task) throw new TimeoutException("Apple Sign In timed out.");
                string identityToken = await appleTokenCompletion.Task;
                bool linked = await LinkAppleCredentialAsync(identityToken);
                if (!linked) return false;
                Provider = AuthenticationProvider.Apple;
                AccountGeneration++;
                SetState(AuthenticationState.Authenticated);
                return true;
            }
            catch (Exception ex)
            {
                LastError = SafeMessage(ex);
                SetState(IsAuthenticated ? (IsCloudAvailable ? AuthenticationState.Authenticated : AuthenticationState.Guest) : AuthenticationState.Failed);
                return false;
            }
            finally { appleTokenCompletion = null; }
#else
            LastError = "Sign in with Apple is available only in an iOS device build. No Editor account was created.";
            SetState(IsAuthenticated ? AuthenticationState.Guest : AuthenticationState.Failed);
            return false;
#endif
        }

        public void SignOut()
        {
            AccountGeneration++;
#if UNITY_IOS && !UNITY_EDITOR
            appleTokenCompletion?.TrySetCanceled();
#endif
            if (UnityServices.State == ServicesInitializationState.Initialized)
                AuthenticationService.Instance.SignOut(true);
            Provider = AuthenticationProvider.None;
            LastError = string.Empty;
            SetState(AuthenticationState.Uninitialized);
            initializationTask = null;
        }

        private async Task<bool> LinkGoogleCredentialAsync(string authCode)
        {
            try
            {
                if (IsAuthenticated) await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(authCode);
                else await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);
                return true;
            }
            catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
            {
                LastError = "Google account belongs to another player. Current progress was kept; no save was merged or overwritten.";
                SetState(AuthenticationState.Guest);
                return false;
            }
        }

        private async Task<bool> LinkAppleCredentialAsync(string identityToken)
        {
            try
            {
                if (IsAuthenticated) await AuthenticationService.Instance.LinkWithAppleAsync(identityToken);
                else await AuthenticationService.Instance.SignInWithAppleAsync(identityToken);
                return true;
            }
            catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
            {
                LastError = "Apple ID belongs to another player. Current progress was kept; no save was merged or overwritten.";
                SetState(AuthenticationState.Guest);
                return false;
            }
        }

        private async Task RefreshProviderFromServerAsync()
        {
            Provider = AuthenticationProvider.Guest;
            PlayerInfo info = await AuthenticationService.Instance.GetPlayerInfoAsync();
            if (!string.IsNullOrEmpty(info.GetGooglePlayGamesId())) Provider = AuthenticationProvider.GooglePlayGames;
            else if (!string.IsNullOrEmpty(info.GetAppleId())) Provider = AuthenticationProvider.Apple;
        }

        private void SubscribeAuthenticationEvents()
        {
            AuthenticationService.Instance.Expired -= OnSessionExpired;
            AuthenticationService.Instance.SignedOut -= OnBackendSignedOut;
            AuthenticationService.Instance.Expired += OnSessionExpired;
            AuthenticationService.Instance.SignedOut += OnBackendSignedOut;
        }

        private void OnSessionExpired() { Provider = AuthenticationProvider.None; AccountGeneration++; SetState(AuthenticationState.Offline); }
        private void OnBackendSignedOut() { Provider = AuthenticationProvider.None; AccountGeneration++; SetState(AuthenticationState.Uninitialized); }
        private void SetState(AuthenticationState state) { State = state; StateChanged?.Invoke(); }
        private static string SafeMessage(Exception ex) => ex is AuthenticationException || ex is RequestFailedException || ex is TimeoutException || ex is InvalidOperationException ? ex.Message : "Authentication operation failed.";
        private async void Observe(Task task) { try { await task; } catch (OperationCanceledException) { } catch (Exception ex) { Debug.LogError("[Authentication] " + SafeMessage(ex)); } }

        // Called from the iOS native bridge. Payload never gets logged because it contains a credential.
        public void OnAppleNativeResult(string payload)
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (appleTokenCompletion == null) return;
            int separator = payload == null ? -1 : payload.IndexOf('|');
            string status = separator >= 0 ? payload.Substring(0, separator) : payload;
            string value = separator >= 0 ? payload.Substring(separator + 1) : string.Empty;
            if (status == "success" && !string.IsNullOrEmpty(value)) appleTokenCompletion.TrySetResult(value);
            else appleTokenCompletion.TrySetException(new InvalidOperationException(string.IsNullOrEmpty(value) ? "Apple Sign In was cancelled or denied." : value));
#endif
        }
    }
}
