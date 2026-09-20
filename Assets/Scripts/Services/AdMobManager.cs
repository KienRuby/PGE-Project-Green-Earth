using System;
using UnityEngine;
using GoogleMobileAds.Api;

/// <summary>
/// Quản lý vòng đời và điều phối quảng cáo Google Mobile Ads (AdMob) cho toàn game:
/// - Tự động khởi tạo SDK AdMob khi game bắt đầu (RuntimeInitializeOnLoadMethod).
/// - Tự động tải trước (pre-load) Rewarded Ad để sẵn sàng phát ngay khi người chơi bấm nút.
/// - Đăng ký trực tiếp với AdRewardService.OnRewardedAdRequested.
/// - Trả kết quả (onComplete) khi người chơi xem xong video để AdRewardService trao thưởng và kích hoạt Cooldown 60s.
/// - Tự động tải lại ad mới vào cache khi ad cũ kết thúc.
/// </summary>
public class AdMobManager : MonoBehaviour
{
    private static AdMobManager instance;
    public static AdMobManager Instance => instance;

    // Test Ad Unit IDs chính thức của Google:
#if UNITY_ANDROID
    [SerializeField] private string rewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
    [SerializeField] private string interstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
#elif UNITY_IOS || UNITY_IPHONE
    [SerializeField] private string rewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";
    [SerializeField] private string interstitialAdUnitId = "ca-app-pub-3940256099942544/4411468910";
#else
    [SerializeField] private string rewardedAdUnitId = "unused";
    [SerializeField] private string interstitialAdUnitId = "unused";
#endif

    private RewardedAd rewardedAd;
    private InterstitialAd interstitialAd;
    private Action<bool> currentRewardCallback;
    private Action currentInterstitialCallback;
    private bool isSdkInitialized = false;
    public bool IsSdkInitialized => isSdkInitialized;
    private bool isLoadingRewarded = false;
    private bool isLoadingInterstitial = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (instance != null) return;

        var go = new GameObject("[AdMobManager]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AdMobManager>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeAdMob();
        AdRewardService.OnRewardedAdRequested += HandleRewardedAdRequested;
        AdRewardService.OnInterstitialAdRequested += HandleInterstitialAdRequested;
    }

    private void OnDestroy()
    {
        AdRewardService.OnRewardedAdRequested -= HandleRewardedAdRequested;
        AdRewardService.OnInterstitialAdRequested -= HandleInterstitialAdRequested;
        DestroyRewardedAd();
        DestroyInterstitialAd();
    }

    /// <summary>
    /// Khởi tạo Google Mobile Ads SDK
    /// </summary>
    public void InitializeAdMob()
    {
        MobileAds.Initialize(initStatus =>
        {
            isSdkInitialized = true;
            LoadRewardedAd();
            LoadInterstitialAd();
        });
    }

    // =========================================================================
    // REWARDED ADS (HỒI SINH, DAILY LOGIN)
    // =========================================================================

    /// <summary>
    /// Tải trước một Rewarded Ad vào bộ nhớ đệm
    /// </summary>
    public void LoadRewardedAd()
    {
        if (isLoadingRewarded) return;

        DestroyRewardedAd();

        isLoadingRewarded = true;

        var adRequest = new AdRequest();
        RewardedAd.Load(rewardedAdUnitId, adRequest, (ad, error) =>
        {
            isLoadingRewarded = false;

            if (error != null || ad == null)
            {
                Debug.LogWarning($"[AdMobManager] Failed to load rewarded ad: {error?.GetMessage()}");
                return;
            }

            rewardedAd = ad;

            RegisterRewardedAdEvents(rewardedAd);
        });
    }

    private void RegisterRewardedAdEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError($"[AdMobManager] Rewarded ad failed to show: {error.GetMessage()}");
            NotifyRewardResult(false);
            LoadRewardedAd();
        };
    }

    private void HandleRewardedAdRequested(Action<bool> onComplete)
    {
        ShowRewardedAd(onComplete);
    }

    /// <summary>
    /// Hiển thị quảng cáo nhận thưởng nếu đã sẵn sàng
    /// </summary>
    public void ShowRewardedAd(Action<bool> onComplete)
    {
        if (!AdRewardService.IsNetworkAvailable)
        {
            Debug.LogWarning("[AdMobManager] Cannot show rewarded ad: No internet reachability.");
            onComplete?.Invoke(false);
            return;
        }

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            currentRewardCallback = onComplete;
            bool userEarnedReward = false;

            rewardedAd.Show(reward =>
            {
                userEarnedReward = true;
            });

            // The ad instance is single-use. Attach exactly one completion
            // callback for this show; registering one during Load and another
            // during Show caused duplicate rewards / stuck UI callbacks.
            rewardedAd.OnAdFullScreenContentClosed += () => NotifyRewardResult(userEarnedReward);
        }
        else
        {
            Debug.LogWarning("[AdMobManager] Rewarded ad is not ready yet! Triggering background load...");
            LoadRewardedAd();

#if UNITY_EDITOR
            onComplete?.Invoke(true);
#else
            onComplete?.Invoke(false);
#endif
        }
    }

    private void NotifyRewardResult(bool success)
    {
        if (currentRewardCallback != null)
        {
            var cb = currentRewardCallback;
            currentRewardCallback = null;
            cb.Invoke(success);
        }
    }

    private void DestroyRewardedAd()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }
    }

    // =========================================================================
    // INTERSTITIAL ADS (THẮNG & THUA / RUN END)
    // =========================================================================

    /// <summary>
    /// Tải trước một Interstitial Ad vào bộ nhớ đệm
    /// </summary>
    public void LoadInterstitialAd()
    {
        if (isLoadingInterstitial) return;

        DestroyInterstitialAd();

        isLoadingInterstitial = true;

        var adRequest = new AdRequest();
        InterstitialAd.Load(interstitialAdUnitId, adRequest, (ad, error) =>
        {
            isLoadingInterstitial = false;

            if (error != null || ad == null)
            {
                Debug.LogWarning($"[AdMobManager] Failed to load interstitial ad: {error?.GetMessage()}");
                return;
            }

            interstitialAd = ad;

            RegisterInterstitialEvents(interstitialAd);
        });
    }

    private void RegisterInterstitialEvents(InterstitialAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            NotifyInterstitialResult();
            LoadInterstitialAd();
        };

        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError($"[AdMobManager] Interstitial ad failed to show: {error.GetMessage()}");
            NotifyInterstitialResult();
            LoadInterstitialAd();
        };
    }

    private void HandleInterstitialAdRequested(Action onComplete)
    {
        ShowInterstitialAd(onComplete);
    }

    /// <summary>
    /// Hiển thị quảng cáo Interstitial khi kết thúc trận (Thắng/Thua)
    /// </summary>
    public void ShowInterstitialAd(Action onComplete)
    {
        if (!AdRewardService.IsNetworkAvailable)
        {
            Debug.LogWarning("[AdMobManager] Cannot show interstitial ad: No internet reachability.");
            onComplete?.Invoke();
            return;
        }

        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            currentInterstitialCallback = onComplete;
            interstitialAd.Show();
        }
        else
        {
            Debug.LogWarning("[AdMobManager] Interstitial ad not ready yet! Preloading now...");
            LoadInterstitialAd();

#if UNITY_EDITOR
#endif
            onComplete?.Invoke();
        }
    }

    private void NotifyInterstitialResult()
    {
        if (currentInterstitialCallback != null)
        {
            var cb = currentInterstitialCallback;
            currentInterstitialCallback = null;
            cb.Invoke();
        }
    }

    private void DestroyInterstitialAd()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }
    }
}
