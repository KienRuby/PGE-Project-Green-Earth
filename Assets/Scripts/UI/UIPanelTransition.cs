using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component quản lý chuyển cảnh Panel UI chuẩn AAA Mobile:
/// 1. Pop / Bounce 3-Phase (Pop -> Recoil -> Settle) dành cho Popup / Modal.
/// 2. Directional Slide 5-Layer Motion (Exit, Enter, Row Stagger) dành cho Bottom Nav / Tabs.
/// 3. Zero GC Runtime: Không GetComponent, không LINQ, không new List trong animation loop.
/// 4. An toàn gián đoạn (Fast Reopen / Hide During Show): Lấy current visual state để nội suy mượt mà.
/// 5. Tự động tương thích LayoutGroup, Sub-Canvas isolation và Unscaled Time.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIPanelTransition : MonoBehaviour
{
    public enum TransitionType
    {
        DirectionalSlide,
        Crossfade,
        ScaleFade,
        PopIn,
        Instant
    }

    public enum SlideDirection
    {
        FromLeft,
        FromRight,
        FromTop,
        FromBottom,
        None
    }

    public enum TransitionPreset
    {
        Premium,
        Fast,
        Smooth,
        Minimal,
        Custom
    }

    public enum PopPreset
    {
        Premium,
        Subtle,
        Punchy,
        Custom
    }

    [Header("Preset & Mode")]
    [Tooltip("Bộ preset cấu hình chuyển cảnh trượt tab (Directional Slide).")]
    [SerializeField] private TransitionPreset tabPreset = TransitionPreset.Premium;

    [Tooltip("Bộ preset cấu hình hiệu ứng Pop / Bounce cho Popup/Modal.")]
    [SerializeField] private PopPreset popPreset = PopPreset.Premium;

    [Tooltip("Kiểu chuyển cảnh mặc định khi gọi PlayShow().")]
    [SerializeField] private TransitionType defaultTransitionType = TransitionType.DirectionalSlide;

    [Header("Pop / Modal 3-Phase Settings (Hiệu ứng Nảy Popup)")]
    [Tooltip("Tỉ lệ scale bắt đầu xuất hiện (tương đối theo baseScale, mặc định 0.80).")]
    [Range(0.50f, 0.95f)]
    [SerializeField] private float popStartScale = 0.80f;

    [Tooltip("Tỉ lệ scale đỉnh overshoot ở cuối Phase 1 (mặc định 1.08).")]
    [Range(1.01f, 1.25f)]
    [SerializeField] private float popOvershootScale = 1.08f;

    [Tooltip("Tỉ lệ scale co lại ở cuối Phase 2 (mặc định 0.97).")]
    [Range(0.90f, 1.00f)]
    [SerializeField] private float popRecoilScale = 0.97f;

    [Tooltip("Thời gian Phase 1: Pop (0.80 -> 1.08, Alpha 0 -> 1).")]
    [Range(0.05f, 0.30f)]
    [SerializeField] private float phase1PopDuration = 0.14f;

    [Tooltip("Thời gian Phase 2: Recoil (1.08 -> 0.97, Alpha = 1).")]
    [Range(0.04f, 0.20f)]
    [SerializeField] private float phase2RecoilDuration = 0.09f;

    [Tooltip("Thời gian Phase 3: Settle (0.97 -> 1.00, Alpha = 1).")]
    [Range(0.04f, 0.20f)]
    [SerializeField] private float phase3SettleDuration = 0.09f;

    [Tooltip("Thời gian ẩn Popup khi Hide (mặc định 0.18s).")]
    [Range(0.10f, 0.30f)]
    [SerializeField] private float popHideDuration = 0.18f;

    [Tooltip("Scale thu nhỏ khi ẩn Popup (mặc định 0.94).")]
    [Range(0.85f, 0.99f)]
    [SerializeField] private float popHideScale = 0.94f;

    [Header("Layer 4 - Panel Enter (Dành cho Slide Tab)")]
    [Tooltip("Thời gian xuất hiện của panel mới khi trượt (0.20s - 0.24s).")]
    [Range(0.10f, 0.40f)]
    [SerializeField] private float enterDuration = 0.22f;

    [Tooltip("Khoảng cách xuất phát trượt vào (80px - 120px).")]
    [Range(40f, 200f)]
    [SerializeField] private float enterDistance = 100f;

    [Tooltip("Tỉ lệ scale ban đầu khi trượt vào (0.98).")]
    [Range(0.90f, 1.0f)]
    [SerializeField] private float enterScale = 0.98f;

    [Tooltip("Độ nảy lò xo khi vào (cực nhẹ để giữ panel ổn định, không cao su).")]
    [Range(0.0f, 0.5f)]
    [SerializeField] private float enterOvershoot = 0.08f;

    [Header("Layer 3 - Panel Exit (Dành cho Slide Tab)")]
    [Tooltip("Thời gian thoát của panel cũ khi trượt (0.16s - 0.20s).")]
    [Range(0.10f, 0.35f)]
    [SerializeField] private float exitDuration = 0.18f;

    [Tooltip("Khoảng cách trôi ra ngoài khi thoát (50px - 80px).")]
    [Range(30f, 150f)]
    [SerializeField] private float exitDistance = 65f;

    [Tooltip("Tỉ lệ scale lùi về hậu cảnh khi thoát (0.97).")]
    [Range(0.90f, 1.0f)]
    [SerializeField] private float exitScale = 0.97f;

    [Header("Layer 5 - Content Stagger (Thác đổ theo Row/Chunk)")]
    [Tooltip("Kích hoạt hiệu ứng các phần tử con/nhóm thẻ bay lên nối tiếp khi mở panel.")]
    [SerializeField] private bool animateChildren = true;

    [Tooltip("Tự động nhóm các phần tử con cùng độ cao Y thành từng Row (tối đa 3-4 bước).")]
    [SerializeField] private bool groupChildrenByRow = true;

    [Tooltip("Danh sách target thủ công (nếu muốn chỉ định chính xác các Card/Section cụ thể).")]
    [SerializeField] private RectTransform[] customStaggerTargets;

    [Tooltip("Độ trễ giữa các Row/Nhóm (0.02s - 0.035s, tổng trễ <= 0.10s).")]
    [Range(0.01f, 0.05f)]
    [SerializeField] private float childStaggerDelay = 0.025f;

    [Tooltip("Thời gian bay lên của từng nhóm (0.16s - 0.20s).")]
    [Range(0.10f, 0.30f)]
    [SerializeField] private float childDuration = 0.18f;

    [Tooltip("Độ dịch chuyển Y từ dưới lên (16px - 24px).")]
    [Range(10f, 50f)]
    [SerializeField] private float childSlideDistance = 20f;

    [Tooltip("Scale ban đầu của phần tử con (0.96).")]
    [Range(0.90f, 1.0f)]
    [SerializeField] private float childScaleStart = 0.96f;

    [Header("Popup & Overlay Settings")]
    [Tooltip("CanvasGroup cho lớp phủ tối màu Dim Backdrop (nếu là Popup).")]
    [SerializeField] private CanvasGroup dimOverlay;

    [Tooltip("Độ trong suốt tối đa của lớp nền mờ khi mở Popup.")]
    [Range(0f, 1f)]
    [SerializeField] private float dimTargetAlpha = 0.40f;

    [Tooltip("Thời gian mờ dần của lớp nền mờ.")]
    [Range(0.08f, 0.30f)]
    [SerializeField] private float dimDuration = 0.15f;

    [Header("Performance & Sub-Canvas")]
    [Tooltip("Tự động cách ly Sub-Canvas để tránh dirty toàn bộ Canvas chính khi panel chuyển động.")]
    [SerializeField] private bool isolateSubCanvas = false;

    // Optional Event Hooks cho SoundManager / Haptic Feedback
    public event Action OnTransitionStart;
    public event Action OnTransitionComplete;

    // Cache components & visuals
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 baseAnchoredPosition;
    private Vector3 baseScale = Vector3.one;
    private bool isInitialized = false;
    private Coroutine activeTransitionRoutine;
    private Coroutine activeDimRoutine;

    // Cache Stagger Rows/Chunks (Zero-GC execution)
    private struct StaggerItem
    {
        public RectTransform rect;
        public CanvasGroup group;
        public Vector2 basePos;
        public Vector3 baseScale;
    }

    private struct StaggerRow
    {
        public StaggerItem[] items;
        public float delay;
    }

    private StaggerRow[] cachedRows;
    private Coroutine[] activeRowRoutines;

    public bool IsAnimating => activeTransitionRoutine != null;
    public RectTransform Rect => rectTransform != null ? rectTransform : (rectTransform = GetComponent<RectTransform>());
    public Vector3 BaseScale => baseScale;
    public Vector2 BasePosition => baseAnchoredPosition;

    private void Awake()
    {
        ApplyPresetValues();
        Initialize();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (tabPreset != TransitionPreset.Custom || popPreset != PopPreset.Custom)
        {
            ApplyPresetValues();
        }
    }
#endif

    public void ApplyPresetValues()
    {
        // 1. Áp dụng preset cho Tab Transition
        switch (tabPreset)
        {
            case TransitionPreset.Premium:
                enterDuration = 0.22f;
                enterDistance = 100f;
                enterScale = 0.98f;
                enterOvershoot = 0.08f;
                exitDuration = 0.18f;
                exitDistance = 65f;
                exitScale = 0.97f;
                childDuration = 0.18f;
                childStaggerDelay = 0.025f;
                childSlideDistance = 20f;
                childScaleStart = 0.96f;
                break;
            case TransitionPreset.Fast:
                enterDuration = 0.16f;
                enterDistance = 80f;
                enterScale = 0.985f;
                enterOvershoot = 0.04f;
                exitDuration = 0.14f;
                exitDistance = 50f;
                exitScale = 0.98f;
                childDuration = 0.14f;
                childStaggerDelay = 0.018f;
                childSlideDistance = 15f;
                childScaleStart = 0.97f;
                break;
            case TransitionPreset.Smooth:
                enterDuration = 0.26f;
                enterDistance = 120f;
                enterScale = 0.975f;
                enterOvershoot = 0.0f;
                exitDuration = 0.20f;
                exitDistance = 75f;
                exitScale = 0.965f;
                childDuration = 0.20f;
                childStaggerDelay = 0.030f;
                childSlideDistance = 24f;
                childScaleStart = 0.95f;
                break;
            case TransitionPreset.Minimal:
                enterDuration = 0.18f;
                enterDistance = 0f;
                enterScale = 0.99f;
                enterOvershoot = 0.0f;
                exitDuration = 0.15f;
                exitDistance = 0f;
                exitScale = 0.99f;
                animateChildren = false;
                break;
            case TransitionPreset.Custom:
                break;
        }

        // 2. Áp dụng preset cho Pop / Modal Transition
        switch (popPreset)
        {
            case PopPreset.Premium:
                popStartScale = 0.80f;
                popOvershootScale = 1.08f;
                popRecoilScale = 0.97f;
                phase1PopDuration = 0.14f;
                phase2RecoilDuration = 0.09f;
                phase3SettleDuration = 0.09f;
                popHideDuration = 0.18f;
                popHideScale = 0.94f;
                break;
            case PopPreset.Subtle:
                popStartScale = 0.90f;
                popOvershootScale = 1.04f;
                popRecoilScale = 0.985f;
                phase1PopDuration = 0.12f;
                phase2RecoilDuration = 0.07f;
                phase3SettleDuration = 0.07f;
                popHideDuration = 0.16f;
                popHideScale = 0.96f;
                break;
            case PopPreset.Punchy:
                popStartScale = 0.75f;
                popOvershootScale = 1.12f;
                popRecoilScale = 0.95f;
                phase1PopDuration = 0.15f;
                phase2RecoilDuration = 0.10f;
                phase3SettleDuration = 0.10f;
                popHideDuration = 0.20f;
                popHideScale = 0.92f;
                break;
            case PopPreset.Custom:
                break;
        }
    }

    public void Initialize()
    {
        if (isInitialized) return;

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (rectTransform != null)
        {
            baseAnchoredPosition = rectTransform.anchoredPosition;
            baseScale = rectTransform.localScale == Vector3.zero ? Vector3.one : rectTransform.localScale;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (isolateSubCanvas && GetComponent<Canvas>() == null)
        {
            Canvas subCanvas = gameObject.AddComponent<Canvas>();
            subCanvas.overrideSorting = false;
            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        CacheStaggerStructure();
        isInitialized = true;
    }

    private void CacheStaggerStructure()
    {
        if (!animateChildren)
        {
            cachedRows = Array.Empty<StaggerRow>();
            activeRowRoutines = Array.Empty<Coroutine>();
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        List<RectTransform> validTargets = new List<RectTransform>();

        if (customStaggerTargets != null && customStaggerTargets.Length > 0)
        {
            for (int i = 0; i < customStaggerTargets.Length; i++)
            {
                RectTransform target = customStaggerTargets[i];
                if (target != null && target.gameObject.activeSelf)
                {
                    validTargets.Add(target);
                }
            }
        }
        else
        {
            int childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (!child.gameObject.activeSelf) continue;

                string childName = child.name.ToLowerInvariant();
                if (childName.Contains("bg") || childName.Contains("background") ||
                    childName.Contains("shade") || childName.Contains("mask") ||
                    childName.Contains("viewport") || childName.Contains("decor"))
                {
                    continue;
                }

                if (child is RectTransform childRect)
                {
                    validTargets.Add(childRect);
                    if (validTargets.Count >= 12) break;
                }
            }
        }

        if (validTargets.Count == 0)
        {
            cachedRows = Array.Empty<StaggerRow>();
            activeRowRoutines = Array.Empty<Coroutine>();
            return;
        }

        if (!groupChildrenByRow)
        {
            cachedRows = new StaggerRow[Mathf.Min(validTargets.Count, 4)];
            for (int i = 0; i < cachedRows.Length; i++)
            {
                RectTransform rt = validTargets[i];
                CanvasGroup cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
                cachedRows[i] = new StaggerRow
                {
                    delay = i * childStaggerDelay,
                    items = new StaggerItem[]
                    {
                        new StaggerItem { rect = rt, group = cg, basePos = rt.anchoredPosition, baseScale = rt.localScale }
                    }
                };
            }
        }
        else
        {
            List<List<StaggerItem>> rowBuckets = new List<List<StaggerItem>>();
            List<float> rowYValues = new List<float>();

            for (int i = 0; i < validTargets.Count; i++)
            {
                RectTransform rt = validTargets[i];
                CanvasGroup cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
                StaggerItem item = new StaggerItem
                {
                    rect = rt,
                    group = cg,
                    basePos = rt.anchoredPosition,
                    baseScale = rt.localScale == Vector3.zero ? Vector3.one : rt.localScale
                };

                float itemY = item.basePos.y;
                int matchedRow = -1;
                for (int r = 0; r < rowYValues.Count; r++)
                {
                    if (Mathf.Abs(rowYValues[r] - itemY) <= 18f)
                    {
                        matchedRow = r;
                        break;
                    }
                }

                if (matchedRow >= 0)
                {
                    rowBuckets[matchedRow].Add(item);
                }
                else
                {
                    if (rowBuckets.Count < 4)
                    {
                        rowYValues.Add(itemY);
                        rowBuckets.Add(new List<StaggerItem> { item });
                    }
                    else
                    {
                        rowBuckets[rowBuckets.Count - 1].Add(item);
                    }
                }
            }

            cachedRows = new StaggerRow[rowBuckets.Count];
            for (int r = 0; r < rowBuckets.Count; r++)
            {
                cachedRows[r] = new StaggerRow
                {
                    delay = r * childStaggerDelay,
                    items = rowBuckets[r].ToArray()
                };
            }
        }

        activeRowRoutines = new Coroutine[cachedRows.Length];
    }

    /// <summary>
    /// Hiển thị ngay lập tức (không hoạt họa).
    /// </summary>
    public void InstantShow()
    {
        Initialize();
        StopAllActiveAnimations();

        gameObject.SetActive(true);
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = baseAnchoredPosition;
            rectTransform.localScale = baseScale;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (dimOverlay != null)
        {
            dimOverlay.alpha = dimTargetAlpha;
            dimOverlay.blocksRaycasts = true;
        }

        RestoreStaggerChildren();
    }

    /// <summary>
    /// Ẩn ngay lập tức (không hoạt họa).
    /// </summary>
    public void InstantHide()
    {
        Initialize();
        StopAllActiveAnimations();

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = baseAnchoredPosition;
            rectTransform.localScale = baseScale;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (dimOverlay != null)
        {
            dimOverlay.alpha = 0f;
            dimOverlay.blocksRaycasts = false;
        }

        RestoreStaggerChildren();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Bắt đầu hiệu ứng HIỆN Panel (Tự động chuyển tiếp theo kiểu chỉ định).
    /// </summary>
    public void PlayShow(
        TransitionType? type = null,
        SlideDirection direction = SlideDirection.FromRight,
        float? duration = null,
        float? slideDist = null,
        Action onComplete = null)
    {
        Initialize();

        TransitionType actualType = type ?? defaultTransitionType;

        if (actualType == TransitionType.Instant)
        {
            InstantShow();
            onComplete?.Invoke();
            return;
        }

        if (actualType == TransitionType.PopIn)
        {
            PlayPopShow(onComplete);
            return;
        }

        // Xử lý Slide / Fade
        float targetDuration = duration ?? enterDuration;
        float actualSlide = slideDist ?? enterDistance;

        if (targetDuration <= 0f)
        {
            InstantShow();
            onComplete?.Invoke();
            return;
        }

        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = false;
        }

        Vector2 currentPos = rectTransform.anchoredPosition;
        Vector3 currentScale = rectTransform.localScale;
        float currentAlpha = canvasGroup != null ? canvasGroup.alpha : 0f;

        Vector2 startPos = baseAnchoredPosition;
        Vector3 startScale = baseScale * enterScale;

        switch (actualType)
        {
            case TransitionType.DirectionalSlide:
                startPos = baseAnchoredPosition + GetSlideOffset(direction, actualSlide);
                break;
            case TransitionType.ScaleFade:
                startScale = baseScale * enterScale;
                break;
            case TransitionType.Crossfade:
                startPos = baseAnchoredPosition;
                startScale = baseScale;
                break;
        }

        if (activeTransitionRoutine != null)
        {
            startPos = currentPos;
            startScale = currentScale;
        }

        StopAllActiveAnimations();
        OnTransitionStart?.Invoke();

        if (dimOverlay != null)
        {
            activeDimRoutine = StartCoroutine(AnimateDimOverlay(dimOverlay.alpha, dimTargetAlpha, dimDuration, true));
        }

        if (animateChildren && cachedRows != null && cachedRows.Length > 0)
        {
            TriggerStaggerRows();
        }

        activeTransitionRoutine = StartCoroutine(EnterRoutine(
            actualType,
            startPos,
            startScale,
            currentAlpha,
            targetDuration,
            onComplete));
    }

    /// <summary>
    /// Bắt đầu hiệu ứng Pop / Bounce 3-Phase (Pop -> Recoil -> Settle) chuẩn AAA Mobile cho Popup/Modal.
    /// Timeline:
    /// - Phase 1: 0.80 -> 1.08 (0.14s, EaseOutCubic, Alpha 0 -> 1)
    /// - Phase 2: 1.08 -> 0.97 (0.09s, EaseInOutQuad, Alpha = 1)
    /// - Phase 3: 0.97 -> 1.00 (0.09s, EaseOutCubic, Alpha = 1)
    /// Tổng thời gian: ~0.32s
    /// </summary>
    public void PlayPopShow(Action onComplete = null)
    {
        Initialize();

        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = false;
        }

        Vector3 currentScale = rectTransform.localScale;
        float currentAlpha = canvasGroup != null ? canvasGroup.alpha : 0f;

        // Nếu đang trong transition trước đó, lấy trạng thái hiện tại để tiếp tục mượt mà
        Vector3 startScale = baseScale * popStartScale;
        if (activeTransitionRoutine != null)
        {
            startScale = currentScale;
        }

        StopAllActiveAnimations();
        OnTransitionStart?.Invoke();

        if (dimOverlay != null)
        {
            activeDimRoutine = StartCoroutine(AnimateDimOverlay(dimOverlay.alpha, dimTargetAlpha, dimDuration, true));
        }

        activeTransitionRoutine = StartCoroutine(PopIn3PhaseRoutine(startScale, currentAlpha, onComplete));
    }

    /// <summary>
    /// Bắt đầu hiệu ứng ẨN Panel (Tự động nhận diện Pop hoặc Slide).
    /// </summary>
    public void PlayHide(
        TransitionType? type = null,
        SlideDirection direction = SlideDirection.FromLeft,
        float? duration = null,
        float? slideDist = null,
        Action onComplete = null)
    {
        Initialize();

        TransitionType actualType = type ?? defaultTransitionType;

        if (actualType == TransitionType.Instant || !gameObject.activeSelf)
        {
            InstantHide();
            onComplete?.Invoke();
            return;
        }

        if (actualType == TransitionType.PopIn)
        {
            PlayPopHide(onComplete);
            return;
        }

        float targetDuration = duration ?? exitDuration;
        float actualSlide = slideDist ?? exitDistance;

        if (targetDuration <= 0f)
        {
            InstantHide();
            onComplete?.Invoke();
            return;
        }

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        Vector2 currentPos = rectTransform.anchoredPosition;
        Vector3 currentScale = rectTransform.localScale;
        float currentAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

        Vector2 targetPos = baseAnchoredPosition;
        Vector3 targetScale = baseScale * exitScale;

        switch (actualType)
        {
            case TransitionType.DirectionalSlide:
                targetPos = baseAnchoredPosition + GetSlideOffset(direction, actualSlide);
                break;
            case TransitionType.ScaleFade:
                targetScale = baseScale * exitScale;
                break;
        }

        StopAllActiveAnimations();
        OnTransitionStart?.Invoke();

        if (dimOverlay != null)
        {
            activeDimRoutine = StartCoroutine(AnimateDimOverlay(dimOverlay.alpha, 0f, dimDuration, false));
        }

        activeTransitionRoutine = StartCoroutine(ExitRoutine(
            actualType,
            currentPos,
            targetPos,
            currentScale,
            targetScale,
            currentAlpha,
            targetDuration,
            onComplete));
    }

    /// <summary>
    /// Bắt đầu hiệu ứng ẨN cho Popup / Modal: Thu nhỏ nhẹ (0.94x) + Fade Out sạch (0.18s, EaseInQuad).
    /// </summary>
    public void PlayPopHide(Action onComplete = null)
    {
        Initialize();

        if (!gameObject.activeSelf)
        {
            InstantHide();
            onComplete?.Invoke();
            return;
        }

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        Vector3 currentScale = rectTransform.localScale;
        float currentAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        Vector3 targetScale = baseScale * popHideScale;

        StopAllActiveAnimations();
        OnTransitionStart?.Invoke();

        if (dimOverlay != null)
        {
            activeDimRoutine = StartCoroutine(AnimateDimOverlay(dimOverlay.alpha, 0f, dimDuration, false));
        }

        activeTransitionRoutine = StartCoroutine(PopHideRoutine(currentScale, targetScale, currentAlpha, popHideDuration, onComplete));
    }

    public void StopAllActiveAnimations()
    {
        if (activeTransitionRoutine != null)
        {
            StopCoroutine(activeTransitionRoutine);
            activeTransitionRoutine = null;
        }

        if (activeDimRoutine != null)
        {
            StopCoroutine(activeDimRoutine);
            activeDimRoutine = null;
        }

        if (activeRowRoutines != null)
        {
            for (int i = 0; i < activeRowRoutines.Length; i++)
            {
                if (activeRowRoutines[i] != null)
                {
                    StopCoroutine(activeRowRoutines[i]);
                    activeRowRoutines[i] = null;
                }
            }
        }
    }

    #region Coroutines
    private IEnumerator PopIn3PhaseRoutine(Vector3 startScale, float startAlpha, Action onComplete)
    {
        Vector3 targetOvershootScale = baseScale * popOvershootScale;
        Vector3 targetRecoilScale = baseScale * popRecoilScale;

        rectTransform.localScale = startScale;
        rectTransform.anchoredPosition = baseAnchoredPosition;
        if (canvasGroup != null) canvasGroup.alpha = startAlpha;

        // PHASE 1 — POP (0.00s -> 0.14s): Scale: 0.80 -> 1.08, Alpha: 0 -> 1 (EaseOutCubic)
        float elapsed1 = 0f;
        while (elapsed1 < phase1PopDuration)
        {
            elapsed1 += Time.unscaledDeltaTime;
            float t = NormalizedTime(elapsed1, phase1PopDuration);
            float easeScale = EaseOutCubic(t);
            float easeAlpha = EaseOutQuad(t);

            rectTransform.localScale = Vector3.LerpUnclamped(startScale, targetOvershootScale, easeScale);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, 1f, easeAlpha);
            }

            yield return null;
        }

        rectTransform.localScale = targetOvershootScale;
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        // PHASE 2 — RECOIL (0.14s -> 0.23s, 0.09s): Scale: 1.08 -> 0.97, Alpha = 1 (EaseInOutQuad / SmoothStep)
        float elapsed2 = 0f;
        while (elapsed2 < phase2RecoilDuration)
        {
            elapsed2 += Time.unscaledDeltaTime;
            float t = NormalizedTime(elapsed2, phase2RecoilDuration);
            float easeScale = EaseInOutQuad(t);

            rectTransform.localScale = Vector3.LerpUnclamped(targetOvershootScale, targetRecoilScale, easeScale);
            yield return null;
        }

        rectTransform.localScale = targetRecoilScale;

        // PHASE 3 — SETTLE (0.23s -> 0.32s, 0.09s): Scale: 0.97 -> 1.00, Alpha = 1 (EaseOutCubic)
        float elapsed3 = 0f;
        while (elapsed3 < phase3SettleDuration)
        {
            elapsed3 += Time.unscaledDeltaTime;
            float t = NormalizedTime(elapsed3, phase3SettleDuration);
            float easeScale = EaseOutCubic(t);

            rectTransform.localScale = Vector3.LerpUnclamped(targetRecoilScale, baseScale, easeScale);
            yield return null;
        }

        // STEP 14 — FINAL STATE LOCK: Đảm bảo chính xác 100% không lệch floating-point
        rectTransform.localScale = baseScale;
        rectTransform.anchoredPosition = baseAnchoredPosition;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        activeTransitionRoutine = null;

        // STEP 13 — CALLBACK: Fired đúng 1 lần sau khi final state đã lock
        OnTransitionComplete?.Invoke();
        onComplete?.Invoke();
    }

    private IEnumerator PopHideRoutine(Vector3 startScale, Vector3 targetScale, float startAlpha, float duration, Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = NormalizedTime(elapsed, duration);
            float ease = EaseInQuad(t);

            rectTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, ease);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, 0f, ease);
            }

            yield return null;
        }

        rectTransform.localScale = baseScale;
        rectTransform.anchoredPosition = baseAnchoredPosition;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
        activeTransitionRoutine = null;

        OnTransitionComplete?.Invoke();
        onComplete?.Invoke();
    }

    private IEnumerator EnterRoutine(
        TransitionType type,
        Vector2 startPos,
        Vector3 startScale,
        float startAlpha,
        float duration,
        Action onComplete)
    {
        rectTransform.anchoredPosition = startPos;
        rectTransform.localScale = startScale;
        if (canvasGroup != null) canvasGroup.alpha = startAlpha;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = NormalizedTime(elapsed, duration);

            float easePos = enterOvershoot > 0f ? EaseOutBackMicro(t, enterOvershoot) : EaseOutCubic(t);
            float easeScale = EaseOutCubic(t);
            float easeAlpha = EaseOutQuad(t);

            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPos, baseAnchoredPosition, easePos);
            rectTransform.localScale = Vector3.LerpUnclamped(startScale, baseScale, easeScale);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, 1f, easeAlpha);
            }

            yield return null;
        }

        rectTransform.anchoredPosition = baseAnchoredPosition;
        rectTransform.localScale = baseScale;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        activeTransitionRoutine = null;
        OnTransitionComplete?.Invoke();
        onComplete?.Invoke();
    }

    private IEnumerator ExitRoutine(
        TransitionType type,
        Vector2 startPos,
        Vector2 targetPos,
        Vector3 startScale,
        Vector3 targetScale,
        float startAlpha,
        float duration,
        Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = NormalizedTime(elapsed, duration);
            float ease = EaseInQuad(t);

            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, ease);
            rectTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, ease);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, 0f, ease);
            }

            yield return null;
        }

        rectTransform.anchoredPosition = baseAnchoredPosition;
        rectTransform.localScale = baseScale;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        RestoreStaggerChildren();
        gameObject.SetActive(false);
        activeTransitionRoutine = null;
        OnTransitionComplete?.Invoke();
        onComplete?.Invoke();
    }

    private IEnumerator AnimateDimOverlay(float fromAlpha, float toAlpha, float duration, bool enableRaycasts)
    {
        if (dimOverlay == null) yield break;

        dimOverlay.blocksRaycasts = enableRaycasts;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutQuad(NormalizedTime(elapsed, duration));
            dimOverlay.alpha = Mathf.LerpUnclamped(fromAlpha, toAlpha, t);
            yield return null;
        }

        dimOverlay.alpha = toAlpha;
        dimOverlay.blocksRaycasts = enableRaycasts;
        activeDimRoutine = null;
    }
    #endregion

    #region Stagger Execution (Row / Chunk)
    private void TriggerStaggerRows()
    {
        if (cachedRows == null || cachedRows.Length == 0) return;

        for (int r = 0; r < cachedRows.Length; r++)
        {
            StaggerRow row = cachedRows[r];
            if (activeRowRoutines[r] != null)
            {
                StopCoroutine(activeRowRoutines[r]);
            }
            activeRowRoutines[r] = StartCoroutine(AnimateSingleRow(row, r));
        }
    }

    private IEnumerator AnimateSingleRow(StaggerRow row, int rowIndex)
    {
        for (int i = 0; i < row.items.Length; i++)
        {
            StaggerItem item = row.items[i];
            if (item.rect == null) continue;

            item.rect.anchoredPosition = item.basePos + Vector2.down * childSlideDistance;
            item.rect.localScale = item.baseScale * childScaleStart;
            if (item.group != null) item.group.alpha = 0f;
        }

        if (row.delay > 0f)
        {
            yield return new WaitForSecondsRealtime(row.delay);
        }

        float elapsed = 0f;
        while (elapsed < childDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = NormalizedTime(elapsed, childDuration);
            float ease = EaseOutCubic(t);
            float alphaEase = EaseOutQuad(t);

            for (int i = 0; i < row.items.Length; i++)
            {
                StaggerItem item = row.items[i];
                if (item.rect == null) continue;

                Vector2 startPos = item.basePos + Vector2.down * childSlideDistance;
                Vector3 startScale = item.baseScale * childScaleStart;

                item.rect.anchoredPosition = Vector2.LerpUnclamped(startPos, item.basePos, ease);
                item.rect.localScale = Vector3.LerpUnclamped(startScale, item.baseScale, ease);

                if (item.group != null)
                {
                    item.group.alpha = Mathf.LerpUnclamped(0f, 1f, alphaEase);
                }
            }

            yield return null;
        }

        for (int i = 0; i < row.items.Length; i++)
        {
            StaggerItem item = row.items[i];
            if (item.rect == null) continue;

            item.rect.anchoredPosition = item.basePos;
            item.rect.localScale = item.baseScale;
            if (item.group != null) item.group.alpha = 1f;
        }

        activeRowRoutines[rowIndex] = null;
    }

    private void RestoreStaggerChildren()
    {
        if (cachedRows == null) return;

        for (int r = 0; r < cachedRows.Length; r++)
        {
            if (activeRowRoutines != null && activeRowRoutines[r] != null)
            {
                StopCoroutine(activeRowRoutines[r]);
                activeRowRoutines[r] = null;
            }

            StaggerRow row = cachedRows[r];
            if (row.items == null) continue;

            for (int i = 0; i < row.items.Length; i++)
            {
                StaggerItem item = row.items[i];
                if (item.rect != null)
                {
                    item.rect.anchoredPosition = item.basePos;
                    item.rect.localScale = item.baseScale;
                    if (item.group != null) item.group.alpha = 1f;
                }
            }
        }
    }
    #endregion

    private Vector2 GetSlideOffset(SlideDirection direction, float distance)
    {
        switch (direction)
        {
            case SlideDirection.FromLeft:
                return new Vector2(-distance, 0f);
            case SlideDirection.FromRight:
                return new Vector2(distance, 0f);
            case SlideDirection.FromTop:
                return new Vector2(0f, distance);
            case SlideDirection.FromBottom:
                return new Vector2(0f, -distance);
            default:
                return Vector2.zero;
        }
    }

    #region Easing Curves (High Precision, Zero Allocation)
    private static float NormalizedTime(float elapsed, float duration)
    {
        return duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
    }

    private static float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    private static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private static float EaseInQuad(float t)
    {
        return t * t;
    }

    private static float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }

    private static float EaseOutBackMicro(float t, float overshoot)
    {
        float c1 = overshoot * 1.5f;
        float c3 = c1 + 1f;
        float inv = t - 1f;
        return 1f + c3 * inv * inv * inv + c1 * inv * inv;
    }
    #endregion
}
