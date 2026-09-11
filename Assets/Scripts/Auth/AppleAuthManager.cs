using System;
using UnityEngine;

namespace PGE.Auth
{
    /// <summary>Compatibility facade. AuthenticationServiceManager is the only source of truth.</summary>
    public sealed class AppleAuthManager : MonoBehaviour
    {
        public static AppleAuthManager Instance { get; private set; }
        public UserProfile CurrentUser => BuildProfile();
        public bool IsLoggedIn => AuthenticationServiceManager.Instance != null &&
                                  AuthenticationServiceManager.Instance.IsCloudAvailable &&
                                  AuthenticationServiceManager.Instance.Provider == AuthenticationProvider.Apple;
        public bool IsAuthenticating => AuthenticationServiceManager.Instance != null &&
                                        AuthenticationServiceManager.Instance.State == AuthenticationState.SigningIn;

        public static event Action<bool, UserProfile> OnAuthStateChanged;
        public static event Action<string> OnAuthError;
        public static event Action<string> OnAuthStatusMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance != null) return;
            var host = new GameObject("[AppleAuthManager]");
            DontDestroyOnLoad(host);
            Instance = host.AddComponent<AppleAuthManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RestoreSession() { }
        public void SignInWithApple(Action<bool, UserProfile> onComplete = null) => RunSignIn(onComplete);

        private async void RunSignIn(Action<bool, UserProfile> onComplete)
        {
            OnAuthStatusMessage?.Invoke("Đang xác thực với Apple và máy chủ...");
            bool success = AuthenticationServiceManager.Instance != null && await AuthenticationServiceManager.Instance.SignInAppleAsync();
            UserProfile profile = BuildProfile();
            if (!success) OnAuthError?.Invoke(AuthenticationServiceManager.Instance?.LastError ?? "Apple authentication unavailable.");
            OnAuthStateChanged?.Invoke(success, profile);
            onComplete?.Invoke(success, profile);
        }

        public void SignOut(Action onComplete = null)
        {
            AuthenticationServiceManager.Instance?.SignOut();
            OnAuthStateChanged?.Invoke(false, BuildProfile());
            onComplete?.Invoke();
        }

        private static UserProfile BuildProfile()
        {
            AuthenticationServiceManager auth = AuthenticationServiceManager.Instance;
            bool connected = auth != null && auth.Provider == AuthenticationProvider.Apple && auth.IsCloudAvailable;
            return new UserProfile
            {
                userId = connected ? auth.PlayerId : string.Empty,
                displayName = connected ? "Apple ID" : "Guest",
                authProvider = connected ? "Apple" : "Guest",
                lastLoginUtc = connected ? DateTime.UtcNow.ToString("O") : string.Empty
            };
        }
    }
}
