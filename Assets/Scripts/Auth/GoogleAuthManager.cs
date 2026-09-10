using System;
using UnityEngine;

namespace PGE.Auth
{
    [Serializable]
    public class UserProfile
    {
        public string userId = string.Empty;
        public string displayName = "Guest";
        public string email = string.Empty;
        public string avatarUrl = string.Empty;
        [Obsolete("OAuth credentials must never be retained in a client profile.")]
        public string idToken = string.Empty;
        public string authProvider = "Guest";
        public string lastLoginUtc = string.Empty;
    }

    /// <summary>Compatibility facade. AuthenticationServiceManager is the only source of truth.</summary>
    public sealed class GoogleAuthManager : MonoBehaviour
    {
        public static GoogleAuthManager Instance { get; private set; }
        public UserProfile CurrentUser => BuildProfile();
        public bool IsLoggedIn => AuthenticationServiceManager.Instance != null &&
                                  AuthenticationServiceManager.Instance.IsCloudAvailable &&
                                  AuthenticationServiceManager.Instance.Provider == AuthenticationProvider.GooglePlayGames;
        public bool IsAuthenticating => AuthenticationServiceManager.Instance != null &&
                                        AuthenticationServiceManager.Instance.State == AuthenticationState.SigningIn;

        public static event Action<bool, UserProfile> OnAuthStateChanged;
        public static event Action<string> OnAuthError;
        public static event Action<string> OnAuthStatusMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance != null) return;
            var host = new GameObject("[GoogleAuthManager]");
            DontDestroyOnLoad(host);
            Instance = host.AddComponent<GoogleAuthManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RestoreSession() { }
        public void SignInWithGoogle(Action<bool, UserProfile> onComplete = null) => RunSignIn(onComplete);

        private async void RunSignIn(Action<bool, UserProfile> onComplete)
        {
            OnAuthStatusMessage?.Invoke("Đang xác thực với Google Play Games và máy chủ...");
            bool success = AuthenticationServiceManager.Instance != null && await AuthenticationServiceManager.Instance.SignInGoogleAsync();
            UserProfile profile = BuildProfile();
            if (!success) OnAuthError?.Invoke(AuthenticationServiceManager.Instance?.LastError ?? "Google authentication unavailable.");
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
            bool connected = auth != null && auth.Provider == AuthenticationProvider.GooglePlayGames && auth.IsCloudAvailable;
            return new UserProfile
            {
                userId = connected ? auth.PlayerId : string.Empty,
                displayName = connected ? "Google Play Games" : "Guest",
                authProvider = connected ? "Google" : "Guest",
                lastLoginUtc = connected ? DateTime.UtcNow.ToString("O") : string.Empty
            };
        }
    }
}
