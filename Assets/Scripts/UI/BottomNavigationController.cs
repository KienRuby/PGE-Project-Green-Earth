using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BottomNavigationController : MonoBehaviour
{
    [Serializable]
    public class NavigationItem
    {
        public Button button;
        public GameObject panel;
        public Image background;
        public Image icon;
        public TMP_Text label;
    }

    [SerializeField] private NavigationItem[] items;
    [SerializeField] private int defaultSelectedIndex = 1;
    [SerializeField] private Color normalColor = new Color32(30, 83, 94, 255);
    [SerializeField] private Color selectedColor = new Color32(71, 178, 174, 255);
    [SerializeField] private Color normalContentColor = new Color32(54, 117, 124, 255);
    [SerializeField] private Color selectedContentColor = Color.white;

    private void Start()
    {
        for (int i = 0; i < items.Length; i++)
        {
            int index = i;
            if (items[i].button != null)
            {
                items[i].button.onClick.AddListener(() => Select(index));
            }
        }

        Select(defaultSelectedIndex);
    }

    public void Select(int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= items.Length)
        {
            return;
        }

        for (int i = 0; i < items.Length; i++)
        {
            NavigationItem item = items[i];
            bool selected = i == selectedIndex;

            if (item.panel != null)
            {
                item.panel.SetActive(selected);
            }

            if (item.background != null)
            {
                item.background.color = selected ? selectedColor : normalColor;
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
