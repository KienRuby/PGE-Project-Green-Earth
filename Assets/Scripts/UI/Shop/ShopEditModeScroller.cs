using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Component hỗ trợ cuộn nội dung ShopPanel trực tiếp trong Edit Mode (Scene View & Game View).
/// Cho phép lập trình viên / Designer dễ dàng cuộn xuống các mục bên dưới để chỉnh sửa kích thước,
/// vị trí, thay đổi sprite mà không cần phải bấm Play Mode.
/// Tự động reset an toàn về đỉnh (0, 0) và bật lại Mask khi bấm Play để đảm bảo 100% không ảnh hưởng runtime.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class ShopEditModeScroller : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;
    [SerializeField] private Mask viewportMask;
    [SerializeField] private VerticalLayoutGroup verticalLayoutGroup;

    [Header("Edit-Mode Controls")]
    [Tooltip("Thanh trượt cuộn: 0 = Đỉnh trang (Top), 1 = Đáy trang (Bottom)")]
    [Range(0f, 1f)]
    [SerializeField] private float scrollPercent = 0f;

    [Tooltip("Tắt tạm thời Mask trong Edit Mode để thấy toàn bộ 6000px chiều dài của Shop trên Scene View")]
    [SerializeField] private bool unmaskInEditMode = false;

    public ScrollRect ScrollRectComponent => scrollRect;
    public RectTransform Content => content;
    public RectTransform Viewport => viewport;
    public Mask ViewportMask => viewportMask;
    public VerticalLayoutGroup LayoutGroup => verticalLayoutGroup;

    public float ScrollPercent
    {
        get => scrollPercent;
        set
        {
            scrollPercent = Mathf.Clamp01(value);
            ApplyScroll();
        }
    }

    public bool UnmaskInEditMode
    {
        get => unmaskInEditMode;
        set
        {
            unmaskInEditMode = value;
            ApplyMaskState();
        }
    }

    private void Awake()
    {
        AutoFindReferences();

        if (Application.isPlaying)
        {
            // Trong game thực tế, luôn đảm bảo Mask bật và vị trí bắt đầu từ đầu trang
            ResetToTop();
        }
    }

    private void OnEnable()
    {
        AutoFindReferences();

        if (Application.isPlaying)
        {
            ResetToTop();
        }

#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
    }

    public void AutoFindReferences()
    {
        if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
        if (scrollRect == null) scrollRect = GetComponentInChildren<ScrollRect>(true);

        if (scrollRect != null)
        {
            if (viewport == null) viewport = scrollRect.viewport;
            if (content == null) content = scrollRect.content;
        }

        if (viewport == null)
        {
            Transform vpT = transform.Find("Viewport");
            if (vpT != null) viewport = vpT.GetComponent<RectTransform>();
        }

        if (viewport != null)
        {
            if (viewportMask == null) viewportMask = viewport.GetComponent<Mask>();
            if (content == null)
            {
                Transform cT = viewport.Find("ShopContent") ?? viewport.Find("Content");
                if (cT != null) content = cT.GetComponent<RectTransform>();
            }
        }

        if (content != null && verticalLayoutGroup == null)
        {
            verticalLayoutGroup = content.GetComponent<VerticalLayoutGroup>();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            AutoFindReferences();
            ApplyMaskState();
#if UNITY_EDITOR
            EditorApplication.delayCall -= DeferredApplyScroll;
            EditorApplication.delayCall += DeferredApplyScroll;
#endif
        }
    }

#if UNITY_EDITOR
    private void DeferredApplyScroll()
    {
        if (this == null) return;
        ApplyScroll();
    }
#endif

    /// <summary>
    /// Cuộn ShopContent theo tỉ lệ scrollPercent (0 = Top, 1 = Bottom)
    /// </summary>
    public void ApplyScroll()
    {
        if (content == null || viewport == null) return;

        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;
        float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);

        float targetY = scrollPercent * maxScroll;
        Vector2 pos = content.anchoredPosition;
        if (!Mathf.Approximately(pos.y, targetY))
        {
            pos.y = targetY;
            content.anchoredPosition = pos;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(content);
            }
#endif
        }
    }

    /// <summary>
    /// Cuộn nội dung theo delta (ví dụ lăn chuột)
    /// </summary>
    public void ScrollByDelta(float deltaY)
    {
        if (content == null || viewport == null) return;

        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;
        float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);
        if (maxScroll <= 0.01f) return;

        float currentY = content.anchoredPosition.y;
        float newY = Mathf.Clamp(currentY + deltaY, 0f, maxScroll);
        scrollPercent = newY / maxScroll;
        ApplyScroll();
    }

    /// <summary>
    /// Cuộn trực tiếp đến vị trí của một mục con bất kỳ trong ShopContent
    /// </summary>
    public void ScrollToSection(RectTransform targetRect)
    {
        if (content == null || viewport == null || targetRect == null) return;

        // Tính khoảng cách từ đỉnh content đến đỉnh target
        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;
        float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);

        // anchoredPosition.y của các con trong content thường là âm (khi pivot ở top)
        float targetOffsetFromTop = -targetRect.anchoredPosition.y;
        // Trừ đi một khoảng lùi nhỏ để nhìn thấy cả tiêu đề
        float targetY = Mathf.Clamp(targetOffsetFromTop - 20f, 0f, maxScroll);

        scrollPercent = maxScroll > 0f ? (targetY / maxScroll) : 0f;
        ApplyScroll();
    }

    /// <summary>
    /// Đưa vị trí cuộn về đỉnh (0, 0)
    /// </summary>
    public void ResetToTop()
    {
        scrollPercent = 0f;
        if (content != null)
        {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
#if UNITY_EDITOR
            if (!Application.isPlaying) EditorUtility.SetDirty(content);
#endif
        }

        if (scrollRect != null)
        {
            scrollRect.normalizedPosition = new Vector2(0f, 1f);
            scrollRect.velocity = Vector2.zero;
        }

        if (viewportMask != null)
        {
            viewportMask.enabled = true;
        }
    }

    private void ApplyMaskState()
    {
        if (viewportMask == null) return;

        if (Application.isPlaying)
        {
            viewportMask.enabled = true;
        }
        else
        {
            viewportMask.enabled = !unmaskInEditMode;
        }
    }

#if UNITY_EDITOR
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Tự động reset về đỉnh và bật lại Mask trước khi vào Play Mode
            ResetToTop();
        }
    }
#endif
}
