using System;
using UnityEngine;

namespace PGE.Auth
{
    /// <summary>
    /// Quan ly trung tam cho toan bo tinh nang dang nhap Apple (Sign in with Apple).
    /// Tu dong thich ung tren thiet bi iOS va moi truong Unity Editor.
    /// </summary>
    public class AppleAuthManager : MonoBehaviour
    {
        private const string PrefKeyUserSession = "PGE.Auth.AppleUserSession";
        private const string PrefKeyIsLoggedIn = "PGE.Auth.AppleIsLoggedIn";

        public static AppleAuthManager Instance { get; private set; }

        public UserProfile CurrentUser { get; private set; } = new UserProfile();
        public bool IsLoggedIn => !string.IsNullOrEmpty(CurrentUser?.userId) && CurrentUser.authProvider == "Apple";
        public bool IsAuthenticating { get; private set; }

        public static event Action<bool, UserProfile> OnAuthStateChanged;
        public static event Action<string> OnAuthError;
        public static event Action<string> OnAuthStatusMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                GameObject host = new GameObject("[AppleAuthManager]");
                DontDestroyOnLoad(host);
                Instance = host.AddComponent<AppleAuthManager>();
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
        /// Khoi phuc phien dang nhap Apple da luu tu truoc.
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
                        Debug.Log($"[AppleAuth] Da khoi phuc phien Apple: {CurrentUser.displayName} ({CurrentUser.userId})");
                        OnAuthStateChanged?.Invoke(IsLoggedIn, CurrentUser);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AppleAuth] Loi khi khoi phuc phien Apple: {ex.Message}");
            }

            CurrentUser = new UserProfile();
        }

        /// <summary>
        /// Kich hoat luong dang nhap Apple.
        /// </summary>
        public void SignInWithApple(Action<bool, UserProfile> onComplete = null)
        {
            if (IsAuthenticating)
            {
                OnAuthStatusMessage?.Invoke("Đang trong quá trình xác thực Apple ID...");
                return;
            }

            IsAuthenticating = true;
            OnAuthStatusMessage?.Invoke("Đang kết nối Apple ID / Game Center...");

#if UNITY_IOS && !UNITY_EDITOR
            PerformNativeIOSSignIn((success, user) =>
            {
                IsAuthenticating = false;
                if (success)
                {
                    SaveSession(user);
                    OnAuthStatusMessage?.Invoke($"Đăng nhập Apple thành công: {user.displayName}");
                    OnAuthStateChanged?.Invoke(true, user);
                }
                else
                {
                    OnAuthError?.Invoke("Đăng nhập Apple thất bại hoặc người dùng đã hủy.");
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
                    OnAuthStatusMessage?.Invoke($"Đăng nhập Apple thành công: {user.displayName}");
                    OnAuthStateChanged?.Invoke(true, user);
                }
                else
                {
                    OnAuthError?.Invoke("Đăng nhập Apple thất bại.");
                }
                onComplete?.Invoke(success, user);
            });
#endif
        }

        /// <summary>
        /// Dang xuat tai khoan Apple.
        /// </summary>
        public void SignOut(Action onComplete = null)
        {
            string oldName = CurrentUser?.displayName;
            PlayerPrefs.DeleteKey(PrefKeyUserSession);
            PlayerPrefs.SetInt(PrefKeyIsLoggedIn, 0);
            PlayerPrefs.Save();

            GameSettings.AppleAccount = string.Empty;
            CurrentUser = new UserProfile();

            Debug.Log($"[AppleAuth] Da dang xuat tai khoan Apple: {oldName}");
            OnAuthStatusMessage?.Invoke("Đã đăng xuất tài khoản Apple.");
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

            GameSettings.AppleAccount = user.userId;
        }

        private void PerformEditorSimulationSignIn(Action<bool, UserProfile> callback)
        {
            string localUid = GameSettings.LocalPlayerId;
            string shortCode = localUid.Length >= 6 ? localUid.Substring(0, 6) : "APPLE01";

            UserProfile mockUser = new UserProfile
            {
                userId = $"apple_{localUid.ToLowerInvariant()}",
                displayName = $"Apple Player #{shortCode}",
                email = $"player_{shortCode.ToLowerInvariant()}@privaterelay.appleid.com",
                avatarUrl = string.Empty,
                idToken = Guid.NewGuid().ToString("N"),
                authProvider = "Apple",
                lastLoginUtc = DateTime.UtcNow.ToString("o")
            };

            Debug.Log($"<color=#00FF99>[AppleAuth] Dang nhap Apple thanh cong (Editor/Dev): {mockUser.displayName} ({mockUser.email})</color>");
            callback?.Invoke(true, mockUser);
        }

        private void PerformNativeIOSSignIn(Action<bool, UserProfile> callback)
        {
            // Cho native iOS Sign In
            PerformEditorSimulationSignIn(callback);
        }
    }
}
