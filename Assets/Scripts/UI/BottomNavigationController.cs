using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Điều khiển thanh điều hướng dưới cùng (Bottom Navigation) chuẩn AAA Mobile:
/// - Layer 1 (Touch feedback ~0.05s): Phản hồi nén nút tức thì (ScaleX: 1.04, ScaleY: 0.94, Y: -3px).
/// - Layer 2 (Tab Settle ~0.14-0.18s): Pop nhẹ qua vị trí đỉnh rồi settle mượt mà về selectedYOffset.
/// - Đổi sprite sáng/tối giữa 'thanh điều hướng 1' và 'thanh điều hướng 2'.
/// - Điều phối song song chuyển cảnh panel với UIPanelTransition (Directional Slide, Zero GC, Fast Interruption).
/// </summary>
public class BottomNavigationController : MonoBehaviour
{
    [Serializable]
    public class NavigationItem
    {
        [Tooltip("Tên gợi nhớ mục điều hướng (Shop, Lab, Chapter, Chipset, Buddy).")]
        public string name = "Tab";

        [Tooltip("Nút bấm dùng để chọn mục điều hướng này.")]
        public Button button;

        [Tooltip("Panel nội dung sẽ được hiển thị khi mục này được chọn.")]
        public GameObject panel;

        [Tooltip("Image của nút để thay đổi Sprite (nếu để trống sẽ tự lấy Image trên button).")]
        public Image buttonImage;

        [Header("Sprites Configuration")]
        [Tooltip("Sprite sáng khi Tab ĐƯỢC CHỌN (cắt từ 'thanh điều hướng 1').")]
        public Sprite activeSprite;

        [Tooltip("Sprite tối khi Tab KHÔNG ĐƯỢC CHỌN (cắt từ 'thanh điều hướng 2').")]
        public Sprite inactiveSprite;

        [Header("Legacy / Optional Tint Colors")]
        [Tooltip("Ảnh nền phụ (nếu có tách lớp).")]
        public Image background;

        [Tooltip("Biểu tượng phụ (nếu có tách icon riêng).")]
        public Image icon;

        [Tooltip("Nhãn chữ (nếu có tách Text riêng).")]
        public TMP_Text label;
    }

    [Tooltip("Danh sách 5 mục trên thanh điều hướng (Shop, Lab, Chapter, Chipset, Buddy).")]
    [SerializeField] private NavigationItem[] items;

    [Tooltip("Vị trí mục được chọn khi khởi động (0: Shop, 1: Lab, 2: Chapter, 3: Chipset, 4: Buddy).")]
    [SerializeField] private int defaultSelectedIndex = 2;

    [Header("Hiển thị")]
    [Tooltip("Tự động đặt màu Image thành trắng khi đổi sprite để sprite hiển thị đúng màu gốc 100%.")]
    [SerializeField] private bool resetImageColorToWhite = true;

    [Header("Layer 1 & 2 - Touch & Tab Motion (Nút điều hướng)")]
    [Tooltip("Bật hiệu ứng phản hồi nút khi bấm.")]
    [SerializeField] private bool animateSelection = true;

    [Tooltip("Độ cao nhô lên của Tab được chọn so với vị trí gốc (12px - 16px).")]
    [SerializeField] private float selectedYOffset = 14f;

    [Tooltip("Overshoot nảy nhẹ khi nút được chọn ổn định vị trí (2px - 4px).")]
    [SerializeField] private float selectedPopOvershoot = 3f;

    [Tooltip("Độ hạ xuống khi ngón tay vừa ấn vào nút (-2px đến -4px).")]
    [SerializeField] private float pressedYOffset = -3f;

    [Tooltip("Scale X khi ấn nút (1.04).")]
    [Range(1.0f, 1.15f)]
    [SerializeField] private float buttonPressScaleX = 1.04f;

    [Tooltip("Scale Y khi ấn nút (0.94).")]
    [Range(0.85f, 1.0f)]
    [SerializeField] private float buttonPressScaleY = 0.94f;

    [Tooltip("Thời gian nén nút (Layer 1 Touch feedback: 0.045s - 0.06s).")]
    [Range(0.03f, 0.10f)]
    [SerializeField] private float buttonPressDuration = 0.05f;

    [Tooltip("Thời gian nút nảy lên (0.10s - 0.14s).")]
    [Range(0.06f, 0.20f)]
    [SerializeField] private float buttonPopDuration = 0.11f;

    [Tooltip("Thời gian nút ổn định (Layer 2 Settle: 0.06s - 0.09s).")]
    [Range(0.04f, 0.15f)]
    [SerializeField] private float buttonSettleDuration = 0.07f;

    [Tooltip("Màu của nút trong khoảnh khắc đang được nhấn.")]
    [SerializeField] private Color pressedColor = new Color(0.70f, 0.88f, 0.92f, 0.9f);

    [Header("Hiệu ứng chuyển cảnh Panel (Screen Transition)")]
    [Tooltip("Bật hiệu ứng chuyển cảnh mượt mà giữa các panel nội dung (Shop, Lab, Chapter, Chipset, Buddy).")]
    [SerializeField] private bool animatePanelTransitions = true;

    [Tooltip("Kiểu chuyển cảnh cho panel: DirectionalSlide (Trượt ngang theo thứ tự), Crossfade, ScaleFade, PopIn.")]
    [SerializeField] private UIPanelTransition.TransitionType panelTransitionType = UIPanelTransition.TransitionType.DirectionalSlide;

    // Optional Event Hooks cho SoundManager / Haptic Feedback
    public event Action<int> OnTabPressed;
    public event Action<int, int> OnTabChanged; // (previousIndex, newIndex)

    private int currentIndex = -1;
    private RectTransform[] itemRects;
    private Vector2[] baseAnchoredPositions;
    private Vector3[] baseScales;
    private Color[] baseImageColors;
    private UIPanelTransition[] panelTransitions;
    private Coroutine selectionRoutine;

    public int CurrentIndex => currentIndex;
    public NavigationItem[] Items => items;

    private void Start()
    {
        if (items == null || items.Length == 0)
        {
            return;
        }

        for (int i = 0; i < items.Length; i++)
        {
            int index = i;
            if (items[i] != null && items[i].button != null)
            {
                items[i].button.onClick.RemoveAllListeners();
                items[i].button.onClick.AddListener(() => Select(index));

                Image img = items[i].buttonImage != null ? items[i].buttonImage : items[i].button.GetComponent<Image>();
                if (img != null)
                {
                    img.raycastTarget = true;
                }
            }
        }

        CacheVisualState();
        InitializePanelTransitions();

        int initialIndex = Mathf.Clamp(defaultSelectedIndex, 0, items.Length - 1);
        ApplySelectionState(initialIndex, animated: false);
        ApplyRestingVisualState(initialIndex);
    }

    private void OnDisable()
    {
        if (selectionRoutine != null)
        {
            StopCoroutine(selectionRoutine);
            selectionRoutine = null;
        }

        if (itemRects != null && currentIndex >= 0)
        {
            ApplyRestingVisualState(currentIndex);
        }
    }

    private void OnDestroy()
    {
        if (items == null) return;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].button != null)
            {
                items[i].button.onClick.RemoveAllListeners();
            }
        }
    }

    /// <summary>
    /// Chuyển đổi tab đang chọn: Xử lý gián đoạn mượt mà, phản hồi tức thì và tính toán hướng trượt tự động.
    /// </summary>
    public void Select(int selectedIndex)
    {
        if (items == null || selectedIndex < 0 || selectedIndex >= items.Length)
        {
            return;
        }

        if (selectedIndex == currentIndex)
        {
            return;
        }

        // Kích hoạt Event hook ngay tức thì (Layer 1)
        OnTabPressed?.Invoke(selectedIndex);

        if (!animateSelection || !isActiveAndEnabled || itemRects == null || itemRects[selectedIndex] == null)
        {
            ApplySelectionState(selectedIndex, animated: animatePanelTransitions);
            ApplyRestingVisualState(selectedIndex);
            return;
        }

        if (selectionRoutine != null)
        {
            StopCoroutine(selectionRoutine);
            selectionRoutine = null;
        }

        selectionRoutine = StartCoroutine(AnimateSelection(selectedIndex));
    }

    private void InitializePanelTransitions()
    {
        if (items == null) return;

        panelTransitions = new UIPanelTransition[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].panel != null)
            {
                UIPanelTransition transition = items[i].panel.GetComponent<UIPanelTransition>();
                if (transition == null)
                {
                    transition = items[i].panel.AddComponent<UIPanelTransition>();
                }
                transition.Initialize();
                panelTransitions[i] = transition;
            }
        }
    }

    private void CacheVisualState()
    {
        itemRects = new RectTransform[items.Length];
        baseAnchoredPositions = new Vector2[items.Length];
        baseScales = new Vector3[items.Length];
        baseImageColors = new Color[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            NavigationItem item = items[i];
            if (item == null || item.button == null)
            {
                continue;
            }

            RectTransform rect = item.button.transform as RectTransform;
            itemRects[i] = rect;
            if (rect != null)
            {
                baseAnchoredPositions[i] = rect.anchoredPosition;
                baseScales[i] = rect.localScale == Vector3.zero ? Vector3.one : rect.localScale;
            }

            Image image = GetButtonImage(item);
            baseImageColors[i] = image != null ? image.color : Color.white;
        }
    }

    private IEnumerator AnimateSelection(int selectedIndex)
    {
        int previousIndex = currentIndex;
        RectTransform selectedRect = itemRects[selectedIndex];
        Image selectedImage = GetButtonImage(items[selectedIndex]);

        // Sample current state để không bị snap khi người dùng spam nút
        Vector2 startPos = selectedRect != null ? selectedRect.anchoredPosition : baseAnchoredPositions[selectedIndex];
        Vector3 startScale = selectedRect != null ? selectedRect.localScale : baseScales[selectedIndex];

        // Layer 1 - Touch Feedback (Squash & Down)
        Vector2 pressedPosition = baseAnchoredPositions[selectedIndex] + Vector2.up * pressedYOffset;
        Vector3 pressedLocalScale = new Vector3(
            baseScales[selectedIndex].x * buttonPressScaleX,
            baseScales[selectedIndex].y * buttonPressScaleY,
            1f);

        yield return TweenButton(
            selectedRect,
            selectedImage,
            startPos,
            pressedPosition,
            startScale,
            pressedLocalScale,
            GetCurrentColor(selectedIndex),
            pressedColor,
            buttonPressDuration);

        // Kích hoạt chuyển cảnh Panel đồng thời
        ApplySelectionState(selectedIndex, animated: animatePanelTransitions);

        // Đưa các nút không tham gia chuyển tiếp về trạng thái nghỉ
        for (int i = 0; i < itemRects.Length; i++)
        {
            if (i != selectedIndex && i != previousIndex)
            {
                ApplyRestingVisualToItem(i, false);
            }
        }

        // Layer 2 - Tab Pop & Overshoot
        RectTransform previousRect = IsValidCachedIndex(previousIndex) ? itemRects[previousIndex] : null;
        Vector2 previousStartPosition = previousRect != null ? previousRect.anchoredPosition : Vector2.zero;
        Vector3 previousStartScale = previousRect != null ? previousRect.localScale : Vector3.one;

        Vector2 popPosition = baseAnchoredPositions[selectedIndex] + Vector2.up * (selectedYOffset + selectedPopOvershoot);
        Vector3 popLocalScale = new Vector3(
            baseScales[selectedIndex].x * 0.96f,
            baseScales[selectedIndex].y * 1.05f,
            1f);

        float elapsed = 0f;
        while (elapsed < buttonPopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = NormalizedTime(elapsed, buttonPopDuration);
            float t = EaseOutQuad(normalized);

            selectedRect.anchoredPosition = Vector2.LerpUnclamped(pressedPosition, popPosition, t);
            selectedRect.localScale = Vector3.LerpUnclamped(pressedLocalScale, popLocalScale, t);
            if (selectedImage != null)
            {
                selectedImage.color = Color.LerpUnclamped(pressedColor, GetRestingColor(selectedIndex), normalized);
            }

            // Tab cũ trôi êm về vị trí gốc
            if (previousRect != null)
            {
                previousRect.anchoredPosition = Vector2.LerpUnclamped(
                    previousStartPosition,
                    baseAnchoredPositions[previousIndex],
                    normalized);
                previousRect.localScale = Vector3.LerpUnclamped(
                    previousStartScale,
                    baseScales[previousIndex],
                    normalized);
            }

            yield return null;
        }

        if (previousRect != null)
        {
            ApplyRestingVisualToItem(previousIndex, false);
        }

        // Layer 2 - Settle về vị trí selected chuẩn
        Vector2 finalSelectedPosition = baseAnchoredPositions[selectedIndex] + Vector2.up * selectedYOffset;
        yield return TweenButton(
            selectedRect,
            selectedImage,
            popPosition,
            finalSelectedPosition,
            popLocalScale,
            baseScales[selectedIndex],
            GetRestingColor(selectedIndex),
            GetRestingColor(selectedIndex),
            buttonSettleDuration);

        ApplyRestingVisualState(selectedIndex);
        selectionRoutine = null;
    }

    private IEnumerator TweenButton(
        RectTransform rect,
        Image image,
        Vector2 fromPosition,
        Vector2 toPosition,
        Vector3 fromScale,
        Vector3 toScale,
        Color fromColor,
        Color toColor,
        float duration)
    {
        if (rect == null) yield break;

        if (duration <= 0f)
        {
            rect.anchoredPosition = toPosition;
            rect.localScale = toScale;
            if (image != null) image.color = toColor;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutQuad(NormalizedTime(elapsed, duration));
            rect.anchoredPosition = Vector2.LerpUnclamped(fromPosition, toPosition, t);
            rect.localScale = Vector3.LerpUnclamped(fromScale, toScale, t);
            if (image != null)
            {
                image.color = Color.LerpUnclamped(fromColor, toColor, t);
            }
            yield return null;
        }

        rect.anchoredPosition = toPosition;
        rect.localScale = toScale;
        if (image != null) image.color = toColor;
    }

    private void ApplySelectionState(int selectedIndex, bool animated = false)
    {
        int previousIndex = currentIndex;
        currentIndex = selectedIndex;

        // 1. Cập nhật nút bấm, icon, label & sprite
        for (int i = 0; i < items.Length; i++)
        {
            NavigationItem item = items[i];
            if (item == null) continue;

            bool isSelected = (i == selectedIndex);

            // Tìm Image hiển thị của Button
            Image targetImage = item.buttonImage;
            if (targetImage == null && item.button != null)
            {
                targetImage = item.button.GetComponent<Image>();
            }

            // Đổi Sprite giữa Thanh 1 (Sáng) và Thanh 2 (Tối)
            if (targetImage != null)
            {
                targetImage.raycastTarget = true;

                Sprite targetSprite = isSelected ? item.activeSprite : item.inactiveSprite;
                if (targetSprite != null)
                {
                    targetImage.sprite = targetSprite;
                }

                if (resetImageColorToWhite)
                {
                    targetImage.color = Color.white;
                }
            }

            if (item.icon != null)
            {
                item.icon.color = isSelected ? Color.white : new Color(0.6f, 0.8f, 0.85f, 0.8f);
            }

            if (item.label != null)
            {
                item.label.color = isSelected ? Color.white : new Color(0.6f, 0.8f, 0.85f, 0.8f);
            }
        }

        // Báo event thay đổi tab
        if (previousIndex != selectedIndex)
        {
            OnTabChanged?.Invoke(previousIndex, selectedIndex);
        }

        // 2. Chuyển cảnh Panel nội dung (Shop, Lab, Chapter, Chipset, Buddy)
        if (panelTransitions == null || panelTransitions.Length != items.Length)
        {
            InitializePanelTransitions();
        }

        if (!animated || previousIndex < 0 || previousIndex == selectedIndex)
        {
            // Chuyển ngay lập tức (không hoạt họa)
            for (int i = 0; i < items.Length; i++)
            {
                if (panelTransitions != null && panelTransitions[i] != null)
                {
                    if (i == selectedIndex)
                    {
                        panelTransitions[i].InstantShow();
                    }
                    else
                    {
                        panelTransitions[i].InstantHide();
                    }
                }
                else if (items[i] != null && items[i].panel != null)
                {
                    items[i].panel.SetActive(i == selectedIndex);
                }
            }
        }
        else
        {
            // Spatial Directionality: Tính toán hướng dựa trên thứ tự index
            // currentIndex < targetIndex (ví dụ Shop 0 -> Chapter 2) => Slide Left
            // currentIndex > targetIndex (ví dụ Chapter 2 -> Shop 0) => Slide Right
            bool movingForward = selectedIndex > previousIndex;
            UIPanelTransition.SlideDirection enterDirection = movingForward 
                ? UIPanelTransition.SlideDirection.FromRight 
                : UIPanelTransition.SlideDirection.FromLeft;
            UIPanelTransition.SlideDirection exitDirection = movingForward 
                ? UIPanelTransition.SlideDirection.FromLeft 
                : UIPanelTransition.SlideDirection.FromRight;

            // Ẩn panel trước đó (Exit)
            if (IsValidCachedIndex(previousIndex))
            {
                if (panelTransitions != null && panelTransitions[previousIndex] != null)
                {
                    panelTransitions[previousIndex].PlayHide(panelTransitionType, exitDirection);
                }
                else if (items[previousIndex]?.panel != null)
                {
                    items[previousIndex].panel.SetActive(false);
                }
            }

            // Hiện panel mới (Enter)
            if (IsValidCachedIndex(selectedIndex))
            {
                if (panelTransitions != null && panelTransitions[selectedIndex] != null)
                {
                    panelTransitions[selectedIndex].PlayShow(panelTransitionType, enterDirection);
                }
                else if (items[selectedIndex]?.panel != null)
                {
                    items[selectedIndex].panel.SetActive(true);
                }
            }

            // Tắt các panel còn lại để tránh trường hợp spam click
            for (int i = 0; i < items.Length; i++)
            {
                if (i != selectedIndex && i != previousIndex)
                {
                    if (panelTransitions != null && panelTransitions[i] != null)
                    {
                        panelTransitions[i].InstantHide();
                    }
                    else if (items[i]?.panel != null)
                    {
                        items[i].panel.SetActive(false);
                    }
                }
            }
        }
    }

    private void ApplyRestingVisualState(int selectedIndex)
    {
        if (itemRects == null)
        {
            return;
        }

        for (int i = 0; i < itemRects.Length; i++)
        {
            ApplyRestingVisualToItem(i, i == selectedIndex);
        }
    }

    private void ApplyRestingVisualToItem(int index, bool isSelected)
    {
        if (!IsValidCachedIndex(index) || itemRects[index] == null)
        {
            return;
        }

        itemRects[index].anchoredPosition = baseAnchoredPositions[index]
            + (isSelected ? Vector2.up * selectedYOffset : Vector2.zero);
        itemRects[index].localScale = baseScales[index];

        Image image = GetButtonImage(items[index]);
        if (image != null)
        {
            image.color = GetRestingColor(index);
        }
    }

    private Image GetButtonImage(NavigationItem item)
    {
        if (item == null)
        {
            return null;
        }

        return item.buttonImage != null
            ? item.buttonImage
            : item.button != null ? item.button.GetComponent<Image>() : null;
    }

    private Color GetCurrentColor(int index)
    {
        Image image = IsValidCachedIndex(index) ? GetButtonImage(items[index]) : null;
        return image != null ? image.color : Color.white;
    }

    private Color GetRestingColor(int index)
    {
        return resetImageColorToWhite || baseImageColors == null || !IsValidCachedIndex(index)
            ? Color.white
            : baseImageColors[index];
    }

    private bool IsValidCachedIndex(int index)
    {
        return items != null && index >= 0 && index < items.Length;
    }

    private static float NormalizedTime(float elapsed, float duration)
    {
        return duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
    }

    private static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

#if UNITY_EDITOR
    [ContextMenu("Tự động gán Sprites từ thanh điều hướng 1 & 2")]
    public void AutoAssignSpritesFromProject()
    {
        Sprite[] activeSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/thanh điều hướng 1.png")
            as Sprite[];
        Sprite[] inactiveSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/thanh điều hướng 2.png")
            as Sprite[];

        if (activeSprites == null || inactiveSprites == null)
        {
            Debug.LogWarning("[BottomNav] Không tìm thấy file 'thanh điều hướng 1.png' hoặc 'thanh điều hướng 2.png' trong Assets/Sprites/UI/");
            return;
        }

        string[] tabNames = { "Shop", "Lab", "Chapter", "Chipset", "Buddy" };

        if (items == null || items.Length != 5)
        {
            items = new NavigationItem[5];
            for (int i = 0; i < 5; i++)
            {
                items[i] = new NavigationItem { name = tabNames[i] };
            }
        }

        for (int i = 0; i < items.Length && i < tabNames.Length; i++)
        {
            string tabName = tabNames[i];
            items[i].name = tabName;

            // Tìm active sprite
            foreach (var s in activeSprites)
            {
                if (s != null && s.name.Equals(tabName, StringComparison.OrdinalIgnoreCase))
                {
                    items[i].activeSprite = s;
                    break;
                }
            }

            // Tìm inactive sprite
            foreach (var s in inactiveSprites)
            {
                if (s != null && s.name.Equals(tabName, StringComparison.OrdinalIgnoreCase))
                {
                    items[i].inactiveSprite = s;
                    break;
                }
            }
        }

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("[BottomNav] ✅ Đã tự động liên kết thành công 5 cặp Sprite cho các tab (Shop, Lab, Chapter, Chipset, Buddy)!");
    }
#endif
}
