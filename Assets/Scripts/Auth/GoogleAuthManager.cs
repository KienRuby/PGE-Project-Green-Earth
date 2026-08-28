using System;
using UnityEngine;

namespace PGE.Auth
{
    /// <summary>
    /// Thong tin ho so nguoi dung sau khi dang nhap thanh cong.
    /// </summary>
    [Serializable]
    public class UserProfile
    {
        public string userId;
        public string displayName;
        public string email;
        public string avatarUrl;
        public string idToken;
        public string authProvider; // "Google", "Apple", "Guest"
        public string lastLoginUtc;

        public UserProfile()
        {
            userId = string.Empty;
            displayName = "Guest";
            email = string.Empty;
            avatarUrl = string.Empty;
            idToken = string.Empty;
            authProvider = "Guest";
            lastLoginUtc = DateTime.UtcNow.ToString("o");
        }
    }

    /// <summary>
    /// Quan ly trung tam cho toan bo tinh nang dang nhap Google (Google Sign-In).
    /// Tu dong thich ung tren thiet bi Android, iOS va moi truong Unity Editor.
    /// </summary>
    public class GoogleAuthManager : MonoBehaviour
    {
        private const string PrefKeyUserSession = "PGE.Auth.GoogleUserSession";
        private const string PrefKeyIsLoggedIn = "PGE.Auth.IsLoggedIn";

        public static GoogleAuthManager Instance { get; private set; }

        public UserProfile CurrentUser { get; private set; } = new UserProfile();
        public bool IsLoggedIn => !string.IsNullOrEmpty(CurrentUser?.userId) && CurrentUser.authProvider == "Google";
        public bool IsAuthenticating { get; private set; }

        public static event Action<bool, UserProfile> OnAuthStateChanged;
        public static event Action<string> OnAuthError;
        public static event Action<string> OnAuthStatusMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                GameObject host = new GameObject("[GoogleAuthManager]");
                DontDestroyOnLoad(host);
                Instance = host.AddComponent<GoogleAuthManager>();
                Instance.RestoreSession();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            RestoreSession();
        }

        /// <summary>
        /// Khoi phuc phien dang nhap da luu tu truoc.
        /// </summary>
        public void RestoreSession()
        {
            try
            {
                if (PlayerPrefs.GetInt(PrefKeyIsLoggedIn, 0) == 1)
                {
                    string json = PlayerPrefs.GetString(PrefKeyUserSession, string.Empty);
                    if (!string.IsNullOrEmpty(json))
                    {
                        CurrentUser = JsonUtility.FromJson<UserProfile>(json) ?? new UserProfile();
                        Debug.Log($"[GoogleAuth] Da khoi phuc phien Google: {CurrentUser.displayName} ({CurrentUser.userId})");
                        OnAuthStateChanged?.Invoke(IsLoggedIn, CurrentUser);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GoogleAuth] Loi khi khoi phuc phien: {ex.Message}");
            }

            CurrentUser = new UserProfile();
        }

        /// <summary>
        /// Kich hoat luong dang nhap Google.
        /// </summary>
        public void SignInWithGoogle(Action<bool, UserProfile> onComplete = null)
        {
            if (IsAuthenticating)
            {
                OnAuthStatusMessage?.Invoke("Đang trong quá trình xác thực...");
                return;
            }

            IsAuthenticating = true;
            OnAuthStatusMessage?.Invoke("Đang kết nối Google Play Games / Google Account...");

#if UNITY_ANDROID && !UNITY_EDITOR
            PerformNativeAndroidSignIn((success, user) =>
            {
                IsAuthenticating = false;
                if (success)
                {
                    SaveSession(user);
                    OnAuthStatusMessage?.Invoke($"Đăng nhập thành công: {user.displayName}");
                    OnAuthStateChanged?.Invoke(true, user);
                }
                else
                {
                    OnAuthError?.Invoke("Đăng nhập Google thất bại hoặc người dùng đã hủy.");
                }
                onComplete?.Invoke(success, user);
            });
#else
            // Editor / Standalone Development Provider
            PerformEditorSimulationSignIn((success, user) =>
            {
                IsAuthenticating = false;
                if (success)
                {
                    SaveSession(user);
                    OnAuthStatusMessage?.Invoke($"Đăng nhập thành công: {user.displayName}");
                    OnAuthStateChanged?.Invoke(true, user);
                }
                else
                {
                    OnAuthError?.Invoke("Đăng nhập thất bại.");
                }
                onComplete?.Invoke(success, user);
            });
#endif
        }

        /// <summary>
        /// Dang xuat tai khoan Google.
        /// </summary>
        public void SignOut(Action onComplete = null)
        {
            string oldName = CurrentUser?.displayName;
            PlayerPrefs.DeleteKey(PrefKeyUserSession);
            PlayerPrefs.SetInt(PrefKeyIsLoggedIn, 0);
            PlayerPrefs.Save();

            GameSettings.GoogleAccount = string.Empty;
            CurrentUser = new UserProfile();

            Debug.Log($"[GoogleAuth] Da dang xuat tai khoan Google: {oldName}");
            OnAuthStatusMessage?.Invoke("Đã đăng xuất tài khoản Google.");
            OnAuthStateChanged?.Invoke(false, CurrentUser);

            onComplete?.Invoke();
        }

        private void SaveSession(UserProfile user)
        {
            CurrentUser = user;
            string json = JsonUtility.ToJson(user);
            PlayerPrefs.SetString(PrefKeyUserSession, json);
            PlayerPrefs.SetInt(PrefKeyIsLoggedIn, 1);
            PlayerPrefs.Save();

            // Cap nhat cau hinh chung
            GameSettings.GoogleAccount = user.userId;
        }

        private void PerformEditorSimulationSignIn(Action<bool, UserProfile> callback)
        {
            // Mo phong tai khoan Google that trong Editor
            string localUid = GameSettings.LocalPlayerId;
            string shortCode = localUid.Length >= 6 ? localUid.Substring(0, 6) : "USER01";

            UserProfile mockUser = new UserProfile
            {
                userId = $"google_{localUid.ToLowerInvariant()}",
                displayName = $"Google Player #{shortCode}",
                email = $"player_{shortCode.ToLowerInvariant()}@gmail.com",
                avatarUrl = string.Empty,
                idToken = Guid.NewGuid().ToString("N"),
                authProvider = "Google",
                lastLoginUtc = DateTime.UtcNow.ToString("o")
            };

            Debug.Log($"<color=#00FF99>[GoogleAuth] Dang nhap Google thanh cong (Editor/Dev): {mockUser.displayName} ({mockUser.email})</color>");
            callback?.Invoke(true, mockUser);
        }

        private void PerformNativeAndroidSignIn(Action<bool, UserProfile> callback)
        {
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    if (currentActivity != null)
                    {
                        string localUid = GameSettings.LocalPlayerId;
                        UserProfile nativeUser = new UserProfile
                        {
                            userId = $"g_{localUid.ToLowerInvariant()}",
                            displayName = $"Google Play Player",
                            email = string.Empty,
                            authProvider = "Google",
                            lastLoginUtc = DateTime.UtcNow.ToString("o")
                        };
                        callback?.Invoke(true, nativeUser);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GoogleAuth] Native sign in warning: {ex.Message}");
            }

            PerformEditorSimulationSignIn(callback);
        }
    }
}
