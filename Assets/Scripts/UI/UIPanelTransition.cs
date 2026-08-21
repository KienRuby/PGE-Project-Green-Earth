using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component quản lý chuyển cảnh Panel UI chuẩn AAA Mobile:
/// 1. Tầng Motion rõ ràng: Exit (0.16-0.20s), Enter (0.20-0.24s), Content Stagger (Row/Chunk).
/// 2. Hỗ trợ DirectionalSlide (Tab Bar), ScaleFade / PopIn (Dialog / Popup).
/// 3. Zero GC runtime: Không GetComponent, không LINQ, không new List trong animation loop.
/// 4. An toàn gián đoạn (Fast Interruption): Lấy current visual state để nội suy mượt mà, không bị giật/snap.
/// 5. Tự động tương thích LayoutGroup và Sub-Canvas isolation.
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

    [Header("Preset & Transition Mode")]
    [Tooltip("Bộ preset cấu hình chuyển cảnh tối ưu sẵn.")]
    [SerializeField] private TransitionPreset preset = TransitionPreset.Premium;

    [Tooltip("Kiểu chuyển cảnh mặc định cho panel.")]
    [SerializeField] private TransitionType defaultTransitionType = TransitionType.DirectionalSlide;

    [Header("Layer 4 - Panel Enter (Vào màn hình)")]
    [Tooltip("Thời gian xuất hiện của panel mới (0.20s - 0.24s).")]
    [Range(0.10f, 0.40f)]
    [SerializeField] private float enterDuration = 0.22f;

    [Tooltip("Khoảng cách xuất phát trượt vào (80px - 120px).")]
    [Range(40f, 200f)]
    [SerializeField] private float enterDistance = 100f;

    [Tooltip("Tỉ lệ scale ban đầu khi bắt đầu xuất hiện (0.975 - 0.985).")]
    [Range(0.90f, 1.0f)]
    [SerializeField] private float enterScale = 0.98f;

    [Tooltip("Độ nảy lò xo khi vào (cực nhẹ để giữ panel ổn định, không cao su).")]
    [Range(0.0f, 0.5f)]
    [SerializeField] private float enterOvershoot = 0.08f;

    [Header("Layer 3 - Panel Exit (Rời màn hình)")]
    [Tooltip("Thời gian thoát của panel cũ (0.16s - 0.20s).")]
    [Range(0.10f, 0.35f)]
    [SerializeField] private float exitDuration = 0.18f;

    [Tooltip("Khoảng cách trôi ra ngoài khi thoát (50px - 80px).")]
    [Range(30f, 150f)]
    [SerializeField] private float exitDistance = 65f;

    [Tooltip("Tỉ lệ scale lùi về hậu cảnh khi thoát (0.97).")]
    [Range(0.90f, 1.0f)]
    [SerializeField] private float exitScale = 0.97f;

    [Header("Layer 5 - Content Stagger (Thác đổ theo Row/Chunk)")]
    [Tooltip("Kích hoạt hiệu ứng các phần tử con/nhóm thẻ bay lên nối tiếp.")]
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

    private void Awake()
    {
        ApplyPresetValues();
        Initialize();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (preset != TransitionPreset.Custom)
        {
            ApplyPresetValues();
        }
    }
#endif

    private void ApplyPresetValues()
    {
        switch (preset)
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

    /// <summary>
    /// Xây dựng cấu trúc hàng/chunk cho Stagger một lần duy nhất tại Initialize (Zero GC).
    /// Bỏ qua backgrounds, scroll viewports và các element ẩn.
    /// </summary>
    private void CacheStaggerStructure()
    {
        if (!animateChildren)
        {
            cachedRows = Array.Empty<StaggerRow>();
            activeRowRoutines = Array.Empty<Coroutine>();
            return;
        }

        // Đảm bảo layout được tính toán chính xác trước khi cache position
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
            // Tự động tìm kiếm các con trực tiếp hợp lệ (bỏ qua BG, Decor, Viewport)
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
                    if (validTargets.Count >= 12) break; // Giới hạn tối đa 12 target
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
            // Mỗi target là một bước riêng
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
            // Nhóm các phần tử cùng Y (cùng hàng) lại với nhau
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
                    if (rowBuckets.Count < 4) // Tối đa 4 Rows
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
    /// Hiển thị ngay lập tức (không có animation).
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
    /// Ẩn ngay lập tức (không có animation).
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
    /// Bắt đầu hiệu ứng HIỆN Panel (Layer 4 - Enter).
    /// Tự động nội suy từ trạng thái hiển thị hiện tại nếu bị gián đoạn (Proportional Duration).
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
        float targetDuration = duration ?? enterDuration;
        float actualSlide = slideDist ?? enterDistance;

        if (actualType == TransitionType.Instant || targetDuration <= 0f)
        {
            InstantShow();
            onComplete?.Invoke();
            return;
        }

        // Input Safety
        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = false;
        }

        // Sample current state để không bị snap khi người dùng spam tab
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
            case TransitionType.PopIn:
                startScale = baseScale * 0.92f;
                break;
            case TransitionType.Crossfade:
                startPos = baseAnchoredPosition;
                startScale = baseScale;
                break;
        }

        // Nếu vừa chuyển hướng và đang lơ lửng, tiếp tục từ vị trí hiện tại
        if (activeTransitionRoutine != null)
        {
            startPos = currentPos;
            startScale = currentScale;
        }

        StopAllActiveAnimations();

        // Xử lý Dim Overlay nếu có
        if (dimOverlay != null)
        {
            activeDimRoutine = StartCoroutine(AnimateDimOverlay(dimOverlay.alpha, dimTargetAlpha, dimDuration, true));
        }

        // Kích hoạt Stagger Rows cho các phần tử con
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
    /// Bắt đầu hiệu ứng ẨN Panel (Layer 3 - Exit).
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
        float targetDuration = duration ?? exitDuration;
        float actualSlide = slideDist ?? exitDistance;

        if (actualType == TransitionType.Instant || targetDuration <= 0f || !gameObject.activeSelf)
        {
            InstantHide();
            onComplete?.Invoke();
            return;
        }

        // Input Safety: Khóa tương tác ngay lập tức
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
                // Trôi về phía đối diện để tạo cảm giác lùi lại
                targetPos = baseAnchoredPosition + GetSlideOffset(direction, actualSlide);
                break;
            case TransitionType.ScaleFade:
                targetScale = baseScale * exitScale;
                break;
            case TransitionType.PopIn:
                targetScale = baseScale * 0.94f;
                break;
        }

        StopAllActiveAnimations();

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

            // Easing: EaseOutCubic kết hợp micro-overshoot nếu có
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

            // Easing: Exit dứt khoát và sạch với EaseInQuad
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
        // Khởi tạo vị trí bắt đầu cho các item trong row
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

    private static float EaseOutBackMicro(float t, float overshoot)
    {
        float c1 = overshoot * 1.5f;
        float c3 = c1 + 1f;
        float inv = t - 1f;
        return 1f + c3 * inv * inv * inv + c1 * inv * inv;
    }
    #endregion
}
