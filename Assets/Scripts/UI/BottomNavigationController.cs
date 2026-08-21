using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Điều khiển thanh điều hướng dưới cùng (Bottom Navigation):
/// - Khi bấm vào 1 tab: Tab đó sáng lên (chuyển sang Sprite của 'thanh điều hướng 1').
/// - 4 tab còn lại chuyển về trạng thái tối (chuyển sang Sprite của 'thanh điều hướng 2').
/// - Bật/Tắt panel nội dung tương ứng của từng tab (Shop, Lab, Chapter, Chipset, Buddy).
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

    [Header("Hiệu ứng khi bấm")]
    [Tooltip("Bật hiệu ứng nhấn xuống rồi bật lên cho 5 nút điều hướng.")]
    [SerializeField] private bool animateSelection = true;

    [Tooltip("Độ cao của nút đang được chọn so với vị trí gốc.")]
    [SerializeField] private float selectedYOffset = 14f;

    [Tooltip("Độ hạ xuống của nút trong khoảnh khắc vừa bấm. Giá trị âm làm nút đi xuống.")]
    [SerializeField] private float pressedYOffset = -4f;

    [Tooltip("Tỉ lệ thu nhỏ của nút trong khoảnh khắc vừa bấm.")]
    [Range(0.75f, 1f)]
    [SerializeField] private float pressedScale = 0.94f;

    [Tooltip("Tỉ lệ phóng lớn nhẹ khi nút bật lên.")]
    [Range(1f, 1.2f)]
    [SerializeField] private float popScale = 1.06f;

    [Tooltip("Thời gian nút hạ xuống sau khi bấm, tính bằng giây.")]
    [Min(0f)]
    [SerializeField] private float pressDuration = 0.05f;

    [Tooltip("Thời gian nút bật lên, tính bằng giây.")]
    [Min(0f)]
    [SerializeField] private float popDuration = 0.10f;

    [Tooltip("Thời gian nút ổn định lại sau khi bật lên, tính bằng giây.")]
    [Min(0f)]
    [SerializeField] private float settleDuration = 0.08f;

    [Tooltip("Màu của nút trong khoảnh khắc đang được nhấn.")]
    [SerializeField] private Color pressedColor = new Color(0.65f, 0.82f, 0.85f, 0.8f);

    private int currentIndex = -1;
    private RectTransform[] itemRects;
    private Vector2[] baseAnchoredPositions;
    private Vector3[] baseScales;
    private Color[] baseImageColors;
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

        int initialIndex = Mathf.Clamp(defaultSelectedIndex, 0, items.Length - 1);
        ApplySelectionState(initialIndex);
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
    /// Chuyển đổi tab đang chọn: Nút chọn sẽ dùng activeSprite (thanh 1), các nút khác dùng inactiveSprite (thanh 2).
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

        if (!animateSelection || !isActiveAndEnabled || itemRects == null || itemRects[selectedIndex] == null)
        {
            ApplySelectionState(selectedIndex);
            ApplyRestingVisualState(selectedIndex);
            return;
        }

        if (selectionRoutine != null)
        {
            StopCoroutine(selectionRoutine);
            ApplyRestingVisualState(currentIndex);
        }

        selectionRoutine = StartCoroutine(AnimateSelection(selectedIndex));
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
                baseScales[i] = rect.localScale;
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

        Vector2 pressedPosition = baseAnchoredPositions[selectedIndex] + Vector2.up * pressedYOffset;
        Vector3 pressedLocalScale = baseScales[selectedIndex] * pressedScale;

        yield return TweenButton(
            selectedRect,
            selectedImage,
            selectedRect.anchoredPosition,
            pressedPosition,
            selectedRect.localScale,
            pressedLocalScale,
            GetCurrentColor(selectedIndex),
            pressedColor,
            pressDuration);

        ApplySelectionState(selectedIndex);

        // Những nút không tham gia chuyển tiếp luôn trở về đúng vị trí gốc.
        for (int i = 0; i < itemRects.Length; i++)
        {
            if (i != selectedIndex && i != previousIndex)
            {
                ApplyRestingVisualToItem(i, false);
            }
        }

        selectedRect.anchoredPosition = pressedPosition;
        selectedRect.localScale = pressedLocalScale;
        if (selectedImage != null)
        {
            selectedImage.color = pressedColor;
        }

        RectTransform previousRect = IsValidCachedIndex(previousIndex) ? itemRects[previousIndex] : null;
        Vector2 previousStartPosition = previousRect != null
            ? previousRect.anchoredPosition
            : Vector2.zero;
        Vector3 previousStartScale = previousRect != null
            ? previousRect.localScale
            : Vector3.one;

        Vector2 popPosition = baseAnchoredPositions[selectedIndex] + Vector2.up * (selectedYOffset + 3f);
        Vector3 popLocalScale = baseScales[selectedIndex] * popScale;
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(NormalizedTime(elapsed, popDuration));

            selectedRect.anchoredPosition = Vector2.LerpUnclamped(pressedPosition, popPosition, t);
            selectedRect.localScale = Vector3.LerpUnclamped(pressedLocalScale, popLocalScale, t);
            if (selectedImage != null)
            {
                selectedImage.color = Color.LerpUnclamped(pressedColor, GetRestingColor(selectedIndex), t);
            }

            if (previousRect != null)
            {
                previousRect.anchoredPosition = Vector2.LerpUnclamped(
                    previousStartPosition,
                    baseAnchoredPositions[previousIndex],
                    t);
                previousRect.localScale = Vector3.LerpUnclamped(
                    previousStartScale,
                    baseScales[previousIndex],
                    t);
            }

            yield return null;
        }

        if (previousRect != null)
        {
            ApplyRestingVisualToItem(previousIndex, false);
        }

        Vector2 selectedPosition = baseAnchoredPositions[selectedIndex] + Vector2.up * selectedYOffset;
        yield return TweenButton(
            selectedRect,
            selectedImage,
            popPosition,
            selectedPosition,
            popLocalScale,
            baseScales[selectedIndex],
            GetRestingColor(selectedIndex),
            GetRestingColor(selectedIndex),
            settleDuration);

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
            float t = EaseOutCubic(NormalizedTime(elapsed, duration));
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

    private void ApplySelectionState(int selectedIndex)
    {
        currentIndex = selectedIndex;

        for (int i = 0; i < items.Length; i++)
        {
            NavigationItem item = items[i];
            if (item == null) continue;

            bool isSelected = (i == selectedIndex);

            // 1. Bật/Tắt Panel nội dung tương ứng
            if (item.panel != null)
            {
                item.panel.SetActive(isSelected);
            }

            // 2. Tìm Image hiển thị của Button
            Image targetImage = item.buttonImage;
            if (targetImage == null && item.button != null)
            {
                targetImage = item.button.GetComponent<Image>();
            }

            // 3. Đổi Sprite giữa Thanh 1 (Sáng) và Thanh 2 (Tối)
            if (targetImage != null)
            {
                targetImage.raycastTarget = true; // Đảm bảo luôn nhận click 100%

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

            // 4. Nếu có icon hoặc label tách riêng
            if (item.icon != null)
            {
                item.icon.color = isSelected ? Color.white : new Color(0.6f, 0.8f, 0.85f, 0.8f);
            }

            if (item.label != null)
            {
                item.label.color = isSelected ? Color.white : new Color(0.6f, 0.8f, 0.85f, 0.8f);
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

    private static float EaseOutCubic(float value)
    {
        float inverse = 1f - value;
        return 1f - inverse * inverse * inverse;
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
