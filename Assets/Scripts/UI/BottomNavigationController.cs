using System;
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

    [Header("Visual Feedback")]
    [Tooltip("Tự động đặt màu Image thành trắng khi đổi sprite để sprite hiển thị đúng màu gốc 100%.")]
    [SerializeField] private bool resetImageColorToWhite = true;

    private int currentIndex = -1;

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

        Select(defaultSelectedIndex);
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
