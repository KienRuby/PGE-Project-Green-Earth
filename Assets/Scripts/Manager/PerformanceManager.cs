using System;
using UnityEngine;

/// <summary>
/// Quản lý hiệu năng, tần số quét màn hình (Refresh Rate) và điều tiết thích ứng (Adaptive Throttling).
/// Tự động nhận diện thiết bị và tối ưu Frame Budget theo 3 mức:
/// - Standard: 60 FPS (Frame budget <= 16.67 ms)
/// - High: 90 FPS (Frame budget <= 11.11 ms)
/// - Ultra / PC: 120 FPS (Frame budget <= 8.33 ms)
/// </summary>
public class PerformanceManager : MonoBehaviour
{
    public enum FrameRateMode
    {
        Standard60 = 60,
        High90 = 90,
        Ultra120 = 120,
        NativeDisplay = 0
    }

    public static PerformanceManager Instance { get; private set; }

    public const string FrameRateModePrefKey = "PGE.Settings.FrameRateMode";
    public const string AdaptiveThrottlingPrefKey = "PGE.Settings.AdaptiveThrottling";

    [Header("Frame Rate Settings")]
    [SerializeField] private FrameRateMode currentMode = FrameRateMode.Standard60;
    [SerializeField] private bool enableAdaptiveThrottling = true;

    [Header("Throttling Thresholds")]
    [SerializeField] private float dropEvaluationDuration = 5.0f;
    [SerializeField] private float lowFpsRatioThreshold = 0.80f; // Nếu FPS thực tế < 80% mục tiêu liên tục

    private int activeTargetFps = 60;
    private int detectedRefreshRate = 60;
    private float evaluationTimer = 0f;
    private float fpsAccumulator = 0f;
    private int frameCounter = 0;
    private bool isThrottled = false;

    public int ActiveTargetFps => activeTargetFps;
    public int DetectedRefreshRate => detectedRefreshRate;
    public bool IsThrottled => isThrottled;
    public FrameRateMode CurrentMode => currentMode;

    public static event Action<int> OnTargetFrameRateChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("[PerformanceManager]");
            Instance = go.AddComponent<PerformanceManager>();
            DontDestroyOnLoad(go);
            Instance.InitializeSettings();
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSettings();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSettings()
    {
        // 1. Nhận diện tần số quét thực tế của màn hình
        DetectScreenRefreshRate();

        // 2. Đọc cấu hình đã lưu của người chơi
        int savedModeInt = PlayerPrefs.GetInt(FrameRateModePrefKey, (int)FrameRateMode.Standard60);
        currentMode = Enum.IsDefined(typeof(FrameRateMode), savedModeInt) 
            ? (FrameRateMode)savedModeInt 
            : FrameRateMode.Standard60;

        enableAdaptiveThrottling = PlayerPrefs.GetInt(AdaptiveThrottlingPrefKey, 1) == 1;

        // 3. Áp dụng Target FPS
        ApplyTargetFrameRate();
    }

    private void DetectScreenRefreshRate()
    {
#if UNITY_2022_2_OR_NEWER
        RefreshRate rate = Screen.currentResolution.refreshRateRatio;
        if (rate.denominator > 0)
        {
            detectedRefreshRate = Mathf.RoundToInt((float)rate.numerator / rate.denominator);
        }
        else
        {
            detectedRefreshRate = 60;
        }
#else
        detectedRefreshRate = Screen.currentResolution.refreshRate;
#endif
        if (detectedRefreshRate <= 0)
        {
            detectedRefreshRate = 60;
        }

        Debug.Log($"[PerformanceManager] 🖥️ Đã nhận diện tần số quét màn hình: {detectedRefreshRate}Hz");
    }

    public void SetFrameRateMode(FrameRateMode mode)
    {
        currentMode = mode;
        isThrottled = false;
        evaluationTimer = 0f;
        fpsAccumulator = 0f;
        frameCounter = 0;

        PlayerPrefs.SetInt(FrameRateModePrefKey, (int)mode);
        PlayerPrefs.Save();

        ApplyTargetFrameRate();
    }

    public void SetAdaptiveThrottling(bool enabled)
    {
        enableAdaptiveThrottling = enabled;
        PlayerPrefs.SetInt(AdaptiveThrottlingPrefKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyTargetFrameRate()
    {
        int target;
        switch (currentMode)
        {
            case FrameRateMode.Standard60:
                target = 60;
                break;
            case FrameRateMode.High90:
                target = 90;
                break;
            case FrameRateMode.Ultra120:
                target = 120;
                break;
            case FrameRateMode.NativeDisplay:
            default:
                target = detectedRefreshRate > 0 ? detectedRefreshRate : 60;
                break;
        }

        if (isThrottled && target > 60)
        {
            target = 60;
        }

        activeTargetFps = target;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = target;

        Debug.Log($"[PerformanceManager] 🚀 Thiết lập Target FPS: {target} FPS (Frame Budget: {(1000f / target):F2} ms)");
        OnTargetFrameRateChanged?.Invoke(target);
    }

    private void Update()
    {
        if (!enableAdaptiveThrottling || currentMode == FrameRateMode.Standard60 || isThrottled)
            return;

        float dt = Time.unscaledDeltaTime;
        if (dt > 0.0001f)
        {
            fpsAccumulator += 1f / dt;
            frameCounter++;
        }

        evaluationTimer += dt;
        if (evaluationTimer >= dropEvaluationDuration)
        {
            float averageFps = frameCounter > 0 ? (fpsAccumulator / frameCounter) : activeTargetFps;
            float threshold = activeTargetFps * lowFpsRatioThreshold;

            if (averageFps < threshold && activeTargetFps > 60)
            {
                isThrottled = true;
                Debug.LogWarning($"[PerformanceManager] ⚠️ Phát hiện tụt FPS kéo dài ({averageFps:F1} < {threshold:F1} FPS). Tự động hạ xuống 60 FPS để duy trì độ mượt và tiết kiệm pin!");
                ApplyTargetFrameRate();
            }

            evaluationTimer = 0f;
            fpsAccumulator = 0f;
            frameCounter = 0;
        }
    }
}
