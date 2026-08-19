using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BottomNavigationController : MonoBehaviour
{
    [Serializable]
    public class NavigationItem
    {
        [Tooltip("Nút bấm dùng để chọn mục điều hướng này.")]
        public Button button;

        [Tooltip("Panel nội dung sẽ được hiển thị khi mục này được chọn.")]
        public GameObject panel;

        [Tooltip("Ảnh nền của nút, dùng để đổi màu giữa trạng thái thường và đang chọn.")]
        public Image background;

        [Tooltip("Ảnh khung viền ngoài của nút (Frame/Border), tự tìm trên button nếu để trống.")]
        public Image border;

        [Tooltip("Biểu tượng của mục điều hướng, dùng để đổi màu theo trạng thái chọn.")]
        public Image icon;

        [Tooltip("Nhãn chữ của mục điều hướng, dùng để đổi màu theo trạng thái chọn.")]
        public TMP_Text label;
    }

    [Tooltip("Danh sách các mục trên thanh điều hướng dưới cùng theo đúng thứ tự hiển thị.")]
    [SerializeField] private NavigationItem[] items;

    [Tooltip("Vị trí mục được chọn khi màn hình khởi động. Chỉ số bắt đầu từ 0.")]
    [SerializeField] private int defaultSelectedIndex = 1;

    [Tooltip("Màu nền của nút khi mục chưa được chọn.")]
    [SerializeField] private Color normalColor = new Color32(30, 83, 94, 255);

    [Tooltip("Màu nền của nút khi mục đang được chọn.")]
    [SerializeField] private Color selectedColor = new Color32(71, 178, 174, 255);

    [Tooltip("Màu khung viền ngoài khi mục chưa được chọn.")]
    [SerializeField] private Color normalBorderColor = new Color32(39, 105, 110, 255);

    [Tooltip("Màu khung viền ngoài khi mục đang được chọn.")]
    [SerializeField] private Color selectedBorderColor = new Color32(239, 247, 238, 255);

    [Tooltip("Màu biểu tượng và nhãn chữ khi mục chưa được chọn.")]
    [SerializeField] private Color normalContentColor = new Color32(54, 117, 124, 255);

    [Tooltip("Màu biểu tượng và nhãn chữ khi mục đang được chọn.")]
    [SerializeField] private Color selectedContentColor = Color.white;

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

    public void Select(int selectedIndex)
    {
        if (items == null || selectedIndex < 0 || selectedIndex >= items.Length)
        {
            return;
        }

        for (int i = 0; i < items.Length; i++)
        {
            NavigationItem item = items[i];
            if (item == null) continue;

            bool selected = i == selectedIndex;

            if (item.panel != null)
            {
                item.panel.SetActive(selected);
            }

            if (item.background != null)
            {
                item.background.color = selected ? selectedColor : normalColor;
            }

            Image borderImage = item.border != null ? item.border : (item.button != null ? item.button.GetComponent<Image>() : null);
            if (borderImage != null && borderImage != item.background)
            {
                borderImage.color = selected ? selectedBorderColor : normalBorderColor;
            }

            Color contentColor = selected ? selectedContentColor : normalContentColor;
            if (item.icon != null)
            {
                item.icon.color = contentColor;
            }

            if (item.label != null)
            {
                item.label.color = contentColor;
            }
        }
    }
}
