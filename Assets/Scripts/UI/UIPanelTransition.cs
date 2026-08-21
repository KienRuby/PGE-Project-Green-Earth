using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Component quản lý hiệu ứng chuyển cảnh mượt mà cho UI Panel (Show/Hide).
/// Hỗ trợ các kiểu chuyển tiếp: DirectionalSlide, Crossfade, ScaleFade, PopIn, Instant.
/// Tự động quản lý CanvasGroup (Alpha, Raycast blocking) và RectTransform.
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

    [Header("Default Settings")]
    [SerializeField] private TransitionType defaultTransitionType = TransitionType.DirectionalSlide;
    [SerializeField] private float defaultDuration = 0.2f;
    [SerializeField] private float defaultSlideDistance = 140f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 baseAnchoredPosition;
    private Vector3 baseScale = Vector3.one;
    private bool isInitialized = false;
    private Coroutine activeRoutine;

    public bool IsAnimating => activeRoutine != null;
    public RectTransform Rect => rectTransform != null ? rectTransform : (rectTransform = GetComponent<RectTransform>());

    private void Awake()
    {
        Initialize();
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

        isInitialized = true;
    }

    /// <summary>
    /// Hiển thị ngay lập tức (không có animation), dùng khi khởi tạo hoặc reset UI.
    /// </summary>
    public void InstantShow()
    {
        Initialize();
        StopCurrentTransition();

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
    }

    /// <summary>
    /// Ẩn ngay lập tức (không có animation).
    /// </summary>
    public void InstantHide()
    {
        Initialize();
        StopCurrentTransition();

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

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Bắt đầu hiệu ứng HIỆN panel.
    /// </summary>
    public void PlayShow(
        TransitionType? type = null,
        SlideDirection direction = SlideDirection.FromRight,
        float? duration = null,
        float? slideDistance = null,
        Action onComplete = null)
    {
        Initialize();
        StopCurrentTransition();

        TransitionType actualType = type ?? defaultTransitionType;
        float actualDuration = duration ?? defaultDuration;
        float actualSlide = slideDistance ?? defaultSlideDistance;

        if (actualType == TransitionType.Instant || actualDuration <= 0f)
        {
            InstantShow();
            onComplete?.Invoke();
            return;
        }

        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
        }

        activeRoutine = StartCoroutine(ShowRoutine(actualType, direction, actualDuration, actualSlide, onComplete));
    }

    /// <summary>
    /// Bắt đầu hiệu ứng ẨN panel.
    /// </summary>
    public void PlayHide(
        TransitionType? type = null,
        SlideDirection direction = SlideDirection.FromLeft,
        float? duration = null,
        float? slideDistance = null,
        Action onComplete = null)
    {
        Initialize();
        StopCurrentTransition();

        TransitionType actualType = type ?? defaultTransitionType;
        float actualDuration = duration ?? defaultDuration;
        float actualSlide = slideDistance ?? defaultSlideDistance;

        if (actualType == TransitionType.Instant || actualDuration <= 0f || !gameObject.activeSelf)
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

        activeRoutine = StartCoroutine(HideRoutine(actualType, direction, actualDuration, actualSlide, onComplete));
    }

    public void StopCurrentTransition()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }
    }

    private IEnumerator ShowRoutine(
        TransitionType type,
        SlideDirection direction,
        float duration,
        float slideDistance,
        Action onComplete)
    {
        Vector2 startPos = baseAnchoredPosition;
        Vector3 startScale = baseScale;
        float startAlpha = 0f;

        switch (type)
        {
            case TransitionType.DirectionalSlide:
                startPos = baseAnchoredPosition + GetSlideOffset(direction, slideDistance);
                break;
            case TransitionType.ScaleFade:
                startScale = baseScale * 0.95f;
                break;
            case TransitionType.PopIn:
                startScale = baseScale * 0.82f;
                break;
            case TransitionType.Crossfade:
                startPos = baseAnchoredPosition;
                break;
        }

        rectTransform.anchoredPosition = startPos;
        rectTransform.localScale = startScale;
        if (canvasGroup != null) canvasGroup.alpha = startAlpha;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float ease = type == TransitionType.PopIn ? EaseOutBack(normalized) : EaseOutCubic(normalized);

            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPos, baseAnchoredPosition, ease);
            rectTransform.localScale = Vector3.LerpUnclamped(startScale, baseScale, ease);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, EaseOutQuad(normalized));
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

        activeRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator HideRoutine(
        TransitionType type,
        SlideDirection direction,
        float duration,
        float slideDistance,
        Action onComplete)
    {
        Vector2 targetPos = baseAnchoredPosition;
        Vector3 targetScale = baseScale;
        Vector2 initialPos = rectTransform.anchoredPosition;
        Vector3 initialScale = rectTransform.localScale;
        float initialAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

        switch (type)
        {
            case TransitionType.DirectionalSlide:
                // Trượt ra theo hướng đối diện để tạo cảm giác panel bị đẩy đi
                targetPos = baseAnchoredPosition + GetSlideOffset(direction, slideDistance * 0.7f);
                break;
            case TransitionType.ScaleFade:
                targetScale = baseScale * 0.96f;
                break;
            case TransitionType.PopIn:
                targetScale = baseScale * 0.88f;
                break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float ease = EaseInQuad(normalized);

            rectTransform.anchoredPosition = Vector2.LerpUnclamped(initialPos, targetPos, ease);
            rectTransform.localScale = Vector3.LerpUnclamped(initialScale, targetScale, ease);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(initialAlpha, 0f, normalized);
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

        gameObject.SetActive(false);
        activeRoutine = null;
        onComplete?.Invoke();
    }

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

    #region Easing Functions
    private static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3);
    }

    private static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private static float EaseInQuad(float t)
    {
        return t * t;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
    }
    #endregion
}
