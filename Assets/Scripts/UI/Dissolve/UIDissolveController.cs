using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controller trung tâm quản lý hiệu ứng Dissolve / Disintegration cho bất kỳ UI Panel, Popup, Dialog nào:
/// - Chuyển cảnh đóng UI bằng Disintegration Shader + Particle Stardust.
/// - Ngăn chặn click spam bằng CanvasGroup (khóa interactable & blocksRaycasts ngay lập tức khi Hide).
/// - An toàn đa trạng thái (State Machine): Xử lý bấm liên tục hoặc đảo chiều Show/Hide giữa chừng.
/// - Hoạt động độc lập bằng Unscaled Time (vẫn chạy mượt khi game tạm dừng Time.timeScale = 0).
/// - Chỉ gọi SetActive(false) SAU KHI shader đã phân rã hoàn toàn 100%.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIDissolveController : MonoBehaviour
{
    public enum DissolveDirection
    {
        Random = 0,
        LeftToRight = 1,
        RightToLeft = 2,
        TopToBottom = 3,
        BottomToTop = 4,
        CenterToOutside = 5,
        OutsideToCenter = 6
    }

    public enum ShowMode
    {
        Instant = 0,
        ReverseDissolve = 1
    }

    public enum TransitionState
    {
        IdleClosed,
        Showing,
        IdleOpened,
        Hiding
    }

    [Header("Transition Timing")]
    [Tooltip("Thời gian phân rã UI (giây). 0.34s giữ đúng cảm giác video nhưng đủ nhanh cho thao tác đóng tab.")]
    [Range(0.1f, 2.0f)]
    [SerializeField] private float duration = 0.34f;

    [Tooltip("Tuyến tính để hiệu ứng đóng phản hồi ngay và không bị chậm ở đầu/cuối.")]
    [SerializeField] private AnimationCurve dissolveCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Tooltip("Sử dụng Unscaled Time để chạy được khi Game Pause.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Stardust Sandification (Match Video Shader.mp4 99%)")]
    [Tooltip("Tự động lấy màu hạt và ánh sáng theo chính màu của UI (giống 99% video tham chiếu).")]
    [SerializeField] private bool useUIColor = true;

    [Tooltip("Độ rộng của dải cát stardust đang tan rã.")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float disintegrationWidth = 0.28f;

    [Tooltip("Kích thước hạt cát stardust theo pixel màn hình (1.5 - 2.2px khớp chuẩn video).")]
    [Range(0.5f, 4.0f)]
    [SerializeField] private float grainSize = 1.45f;

    [Tooltip("Độ trôi dạt bay lên của các hạt stardust.")]
    [Range(0.0f, 3.0f)]
    [SerializeField] private float driftAmount = 0.55f;

    [Tooltip("Cường độ lấp lánh (sparkle glint) của các hạt cát.")]
    [Range(0.0f, 5.0f)]
    [SerializeField] private float sparkleIntensity = 1.3f;

    [Header("Direction & Sweep")]
    [Tooltip("Hướng phân rã của UI.")]
    [SerializeField] private DissolveDirection direction = DissolveDirection.TopToBottom;

    [Tooltip("Mức độ ảnh hưởng của hướng quét so với hoa văn Noise tự do (0..1).")]
    [Range(0f, 1f)]
    [SerializeField] private float directionInfluence = 0.8f;

    [Tooltip("Độ mượt/mờ tại viền phân rã.")]
    [Range(0.001f, 0.1f)]
    [SerializeField] private float dissolveSoftness = 0.01f;

    [Tooltip("Sinh seed ngẫu nhiên mỗi lần phân rã để các lỗ thủng xuất hiện ở vị trí khác nhau.")]
    [SerializeField] private bool randomizeSeed = true;

    [Header("Edge Glow Corona (HDR)")]
    [Tooltip("Độ rộng của viền phát sáng khi đang tan biến.")]
    [Range(0.01f, 0.25f)]
    [SerializeField] private float edgeWidth = 0.035f;

    [Tooltip("Màu viền ngoài phát sáng (HDR).")]
    [ColorUsage(true, true)]
    [SerializeField] private Color edgeColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

    [Tooltip("Màu lõi sáng rực trắng (HDR).")]
    [ColorUsage(true, true)]
    [SerializeField] private Color innerEdgeColor = new Color(1.2f, 1.2f, 1.2f, 1.0f);

    [Tooltip("Cường độ phát sáng và Bloom của viền.")]
    [Range(1f, 10f)]
    [SerializeField] private float edgeIntensity = 1.3f;

    [Header("Noise Pattern")]
    [Tooltip("Tần số chi tiết của Noise (2.0 - 4.0).")]
    [Range(0.5f, 10f)]
    [SerializeField] private float noiseScale = 3.6f;

    [Tooltip("Tốc độ trôi dạt của Noise khi đang tan biến.")]
    [SerializeField] private float noiseSpeed = 0.0f;

    [Tooltip("Bật Screen-Space để toàn bộ popup (Background, Text, Button) đồng bộ 1 thể thống nhất.")]
    [SerializeField] private bool useScreenSpace = true;

    [Header("Show Transition Mode")]
    [Tooltip("Chế độ khi gọi Show(): Instant (hiện ngay) hoặc ReverseDissolve (tụ họp từ các mảnh vỡ).")]
    [SerializeField] private ShowMode defaultShowMode = ShowMode.Instant;

    [Header("Particle Settings")]
    [Tooltip("Kích hoạt hạt bụi phân rã bay ra ngoài.")]
    [SerializeField] private bool enableParticles = true;

    [Header("Sub-Components (Auto-Detected)")]
    [SerializeField] private UIDissolveGroup dissolveGroup;
    [SerializeField] private UIDissolveParticle dissolveParticle;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Events")]
    public UnityEvent OnShowStarted;
    public UnityEvent OnShowCompleted;
    public UnityEvent OnHideStarted;
    public UnityEvent OnHideCompleted;

    private RectTransform rectTransform;
    private Coroutine activeTransitionRoutine;
    private TransitionState currentState = TransitionState.IdleOpened;
    private Vector2 currentNoiseOffset = Vector2.zero;
    private bool isInitialized = false;

    public TransitionState CurrentState => currentState;
    public bool IsTransitioning => currentState == TransitionState.Showing || currentState == TransitionState.Hiding;
    public DissolveDirection Direction { get => direction; set => direction = value; }
    public float Duration { get => duration; set => duration = Mathf.Max(0.05f, value); }
    public bool UseUIColor { get => useUIColor; set => useUIColor = value; }
    public float DisintegrationWidth { get => disintegrationWidth; set => disintegrationWidth = value; }
    public float GrainSize { get => grainSize; set => grainSize = value; }
    public float DriftAmount { get => driftAmount; set => driftAmount = value; }
    public float SparkleIntensity { get => sparkleIntensity; set => sparkleIntensity = value; }

    /// <summary>
    /// Entry point dùng chung cho nút đóng UI: luôn giữ root hoạt động đến khi tan hết.
    /// </summary>
    public static bool HideWithEffect(GameObject target)
    {
        if (target == null || !target.activeSelf) return false;

        UIDissolveController controller = target.GetComponent<UIDissolveController>();
        if (controller == null) controller = target.AddComponent<UIDissolveController>();
        controller.Hide();
        return true;
    }

    /// <summary>
    /// Mở lại một root từng dissolve và khôi phục material/trạng thái tương tác ngay lập tức.
    /// </summary>
    public static bool ShowInstant(GameObject target)
    {
        if (target == null) return false;

        UIDissolveController controller = target.GetComponent<UIDissolveController>();
        if (controller == null) controller = target.AddComponent<UIDissolveController>();
        controller.Show(ShowMode.Instant);
        return true;
    }

    private void Awake()
    {
        InitializeIfNeeded();
        if (!gameObject.activeSelf)
        {
            currentState = TransitionState.IdleClosed;
        }
    }

    public void InitializeIfNeeded()
    {
        if (isInitialized) return;

        rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (dissolveGroup == null)
        {
            dissolveGroup = GetComponent<UIDissolveGroup>();
            if (dissolveGroup == null)
            {
                dissolveGroup = gameObject.AddComponent<UIDissolveGroup>();
            }
        }
        dissolveGroup.InitializeIfNeeded();

        if (enableParticles && dissolveParticle == null)
        {
            dissolveParticle = GetComponentInChildren<UIDissolveParticle>();
            if (dissolveParticle == null)
            {
                dissolveParticle = gameObject.AddComponent<UIDissolveParticle>();
            }
        }

        if (dissolveParticle != null)
        {
            dissolveParticle.InitializeIfNeeded();
            dissolveParticle.SetParticleColor(edgeColor);
        }

        isInitialized = true;
    }

    /// <summary>
    /// Mở Panel / Popup (Hỗ trợ Instant hoặc Reverse Dissolve).
    /// </summary>
    public void Show()
    {
        Show(defaultShowMode);
    }

    /// <summary>
    /// Mở Panel với chế độ chỉ định cụ thể.
    /// </summary>
    public void Show(ShowMode mode)
    {
        InitializeIfNeeded();

        if (currentState == TransitionState.IdleOpened && gameObject.activeSelf)
        {
            return;
        }

        if (activeTransitionRoutine != null)
        {
            StopCoroutine(activeTransitionRoutine);
            activeTransitionRoutine = null;
        }

        gameObject.SetActive(true);

        if (mode == ShowMode.Instant)
        {
            ResetDissolve();
            currentState = TransitionState.IdleOpened;
            OnShowStarted?.Invoke();
            OnShowCompleted?.Invoke();
            return;
        }

        // Reverse Dissolve Mode
        currentState = TransitionState.Showing;
        OnShowStarted?.Invoke();

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        SetupDissolveMaterials();
        activeTransitionRoutine = StartCoroutine(ShowReverseDissolveRoutine());
    }

    /// <summary>
    /// Đóng Panel bằng hiệu ứng Dissolve / Disintegration Shader và phát hạt phân rã.
    /// Khóa tương tác ngay lập tức và chỉ SetActive(false) khi shader đã tan rã 100%.
    /// </summary>
    public void Hide()
    {
        InitializeIfNeeded();

        // Chống spam: Nếu đang trong quá trình Hide hoặc đối tượng đã tắt, bỏ qua
        if (currentState == TransitionState.Hiding || !gameObject.activeSelf)
        {
            return;
        }

        if (activeTransitionRoutine != null)
        {
            StopCoroutine(activeTransitionRoutine);
            activeTransitionRoutine = null;
        }

        currentState = TransitionState.Hiding;

        // 1. Khóa click ngay lập tức để người chơi không spam nút Close hoặc các nút khác
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        OnHideStarted?.Invoke();
        SetupDissolveMaterials();

        activeTransitionRoutine = StartCoroutine(HideDissolveRoutine());
    }

    /// <summary>
    /// Đóng Panel ngay lập tức không qua hiệu ứng shader.
    /// </summary>
    public void HideInstant()
    {
        InitializeIfNeeded();

        if (activeTransitionRoutine != null)
        {
            StopCoroutine(activeTransitionRoutine);
            activeTransitionRoutine = null;
        }

        ResetDissolve();
        currentState = TransitionState.IdleClosed;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Khôi phục toàn bộ trạng thái shader, material và tương tác về ban đầu.
    /// </summary>
    public void ResetDissolve()
    {
        InitializeIfNeeded();

        if (dissolveGroup != null)
        {
            dissolveGroup.SetDissolveProgress(0f);
            dissolveGroup.RestoreOriginalMaterials();
        }

        if (dissolveParticle != null)
        {
            dissolveParticle.ClearParticles();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private void SetupDissolveMaterials()
    {
        if (randomizeSeed)
        {
            currentNoiseOffset = new Vector2(UnityEngine.Random.value * 100f, UnityEngine.Random.value * 100f);
        }

        if (dissolveGroup != null)
        {
            dissolveGroup.CollectAndApplyMaterials();
            dissolveGroup.ConfigureMaterialSettings(
                (int)direction,
                directionInfluence,
                edgeWidth,
                edgeColor,
                innerEdgeColor,
                edgeIntensity,
                noiseScale,
                noiseSpeed,
                currentNoiseOffset,
                useScreenSpace,
                dissolveSoftness,
                disintegrationWidth,
                grainSize,
                driftAmount,
                sparkleIntensity,
                useUIColor
            );
        }

        if (dissolveParticle != null)
        {
            dissolveParticle.SetParticleColor(edgeColor);
        }
    }

    private IEnumerator HideDissolveRoutine()
    {
        float elapsed = 0f;
        float actualDuration = Mathf.Max(duration, 0.05f);

        while (elapsed < actualDuration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;

            float rawT = Mathf.Clamp01(elapsed / actualDuration);
            float evaluatedProgress = dissolveCurve != null ? dissolveCurve.Evaluate(rawT) : rawT;

            // 1. Cập nhật shader dissolve amount
            if (dissolveGroup != null)
            {
                dissolveGroup.SetDissolveProgress(evaluatedProgress);
            }

            // 2. Phát hạt tại viền tan biến
            if (enableParticles && dissolveParticle != null)
            {
                dissolveParticle.EmitAtDissolveEdge(evaluatedProgress, (int)direction, rectTransform);
            }

            yield return null;
        }

        // Đảm bảo tan biến 100%
        if (dissolveGroup != null)
        {
            dissolveGroup.SetDissolveProgress(1f);
        }

        // Một khoảng đệm rất ngắn để hạt cuối cùng vẫn đọc được, không làm thao tác đóng bị ì.
        if (enableParticles && dissolveParticle != null)
        {
            float particleWait = 0.025f;
            while (particleWait > 0f)
            {
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                particleWait -= dt;
                yield return null;
            }
        }

        // 3. Khôi phục trạng thái chuẩn bị cho lần mở sau
        ResetDissolve();
        currentState = TransitionState.IdleClosed;
        activeTransitionRoutine = null;

        // 4. SetActive(false) sau khi hoàn tất toàn bộ
        gameObject.SetActive(false);
        OnHideCompleted?.Invoke();
    }

    private IEnumerator ShowReverseDissolveRoutine()
    {
        float elapsed = 0f;
        float actualDuration = Mathf.Max(duration, 0.05f);

        while (elapsed < actualDuration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;

            float rawT = Mathf.Clamp01(elapsed / actualDuration);
            // Reverse từ 1 -> 0
            float evaluatedProgress = 1f - (dissolveCurve != null ? dissolveCurve.Evaluate(rawT) : rawT);

            if (dissolveGroup != null)
            {
                dissolveGroup.SetDissolveProgress(evaluatedProgress);
            }

            if (enableParticles && dissolveParticle != null)
            {
                dissolveParticle.EmitAtDissolveEdge(evaluatedProgress, (int)direction, rectTransform);
            }

            yield return null;
        }

        ResetDissolve();
        currentState = TransitionState.IdleOpened;
        activeTransitionRoutine = null;

        OnShowCompleted?.Invoke();
    }
}
