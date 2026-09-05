using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component điều khiển modal Thống Kê Sát Thương Chi Tiết (Damage Details)
/// Hiển thị bảng danh sách các Power-up / Chipset cùng DPS, Tỷ lệ % (thanh tiến trình màu Teal),
/// Tổng sát thương và Thời gian hoạt động.
/// </summary>
public sealed class DamageDetailsPopup : MonoBehaviour
{
    [Header("UI Roots")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private RectTransform modalFrame;
    [SerializeField] private Transform rowsContainer;
    [SerializeField] private GameObject rowPrefab;

    [Header("Header & Close")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image chartIcon;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundCloseButton;

    [Header("Sprite Catalog")]
    [SerializeField] private ChipsetLevelVisualLibrary visualLibrary;
    [SerializeField] private Sprite[] chipIcons;
    [SerializeField] private Sprite defaultFrameSprite;

    private readonly List<DamageDetailRowUI> spawnedRows = new List<DamageDetailRowUI>();

    public bool IsVisible => popupRoot != null && popupRoot.activeSelf;

    private void Awake()
    {
        if (popupRoot == null) popupRoot = gameObject;
        EnsureVisuals();
        BindCloseButtons();
    }

    private void EnsureVisuals()
    {
        if (visualLibrary == null)
        {
            visualLibrary = Resources.Load<ChipsetLevelVisualLibrary>("ChipsetLevelVisualLibrary");
        }
        if ((chipIcons == null || chipIcons.Length == 0 || chipIcons.All(s => s == null)) && visualLibrary != null)
        {
            chipIcons = visualLibrary.primaryChipIcons;
        }
        EnsureHeaderLayout();
    }

    private void EnsureHeaderLayout()
    {
        if (modalFrame == null) return;
        Transform header = modalFrame.Find("TableHeader");
        if (header == null) return;

        Transform hChip = header.Find("H_CHIPSET");
        if (hChip != null) SetRect(hChip.GetComponent<RectTransform>(), new Vector2(-235f, 0f), new Vector2(250f, 40f));

        Transform hDPS = header.Find("H_DPS");
        if (hDPS != null) SetRect(hDPS.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(80f, 40f));

        Transform hD = header.Find("H_DPercent");
        if (hD != null) SetRect(hD.GetComponent<RectTransform>(), new Vector2(95f, 0f), new Vector2(80f, 40f));

        Transform hDmg = header.Find("H_Damage");
        if (hDmg != null) SetRect(hDmg.GetComponent<RectTransform>(), new Vector2(205f, 0f), new Vector2(110f, 40f));

        Transform hTime = header.Find("H_Time");
        if (hTime != null) SetRect(hTime.GetComponent<RectTransform>(), new Vector2(310f, 0f), new Vector2(80f, 40f));
    }

    private void BindCloseButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Hide);
            closeButton.gameObject.SetActive(false);
        }
        if (backgroundCloseButton != null)
        {
            backgroundCloseButton.onClick.RemoveListener(Hide);
            backgroundCloseButton.onClick.AddListener(Hide);
        }
    }

    public void Show()
    {
        if (popupRoot != null)
        {
            UIDissolveController.ShowInstant(popupRoot);
            popupRoot.transform.SetAsLastSibling();
        }
        Refresh();
    }

    public void Hide()
    {
        if (popupRoot != null) UIDissolveController.HideWithEffect(popupRoot);
    }

    public void Refresh()
    {
        ClearRows();

        long grandTotalDamage = ChipsetBattleStats.GrandTotalDamage;
        List<ChipsetBattleStats.Entry> sortedEntries = ChipsetBattleStats.GetSortedEntries();

        if (sortedEntries == null || sortedEntries.Count == 0)
        {
            var defaultEntry = ChipsetBattleStats.GetEntry(1);
            if (defaultEntry != null)
            {
                sortedEntries = new List<ChipsetBattleStats.Entry> { defaultEntry };
            }
        }

        if (sortedEntries != null)
        {
            for (int i = 0; i < sortedEntries.Count; i++)
            {
                var entry = sortedEntries[i];
                float percent = entry.GetDamagePercent(grandTotalDamage);
                Sprite icon = GetChipsetIcon(entry.ChipsetId);

                DamageDetailRowUI row = CreateOrGetRow(i);
                if (row != null)
                {
                    row.Setup(
                        icon,
                        entry.ChipsetName,
                        entry.DPS,
                        percent,
                        entry.TotalDamage,
                        entry.FormattedTime
                    );
                    row.gameObject.SetActive(true);
                }
            }
        }
    }

    private void ClearRows()
    {
        for (int i = 0; i < spawnedRows.Count; i++)
        {
            if (spawnedRows[i] != null)
            {
                spawnedRows[i].gameObject.SetActive(false);
            }
        }
    }

    private DamageDetailRowUI CreateOrGetRow(int index)
    {
        if (index < spawnedRows.Count && spawnedRows[index] != null)
        {
            return spawnedRows[index];
        }

        if (rowsContainer == null) return null;

        GameObject rowObj;
        if (rowPrefab != null)
        {
            rowObj = Instantiate(rowPrefab, rowsContainer, false);
        }
        else
        {
            rowObj = CreateDefaultRowObject(rowsContainer);
        }

        DamageDetailRowUI rowUI = rowObj.GetComponent<DamageDetailRowUI>();
        if (rowUI == null)
        {
            rowUI = rowObj.AddComponent<DamageDetailRowUI>();
        }

        spawnedRows.Add(rowUI);
        return rowUI;
    }

    private Sprite GetChipsetIcon(int chipsetId)
    {
        Sprite[] icons = chipIcons;
        if ((icons == null || icons.Length == 0 || icons.All(s => s == null)) && visualLibrary != null && visualLibrary.primaryChipIcons != null)
        {
            icons = visualLibrary.primaryChipIcons;
        }

        if (icons == null || icons.Length == 0 || icons.All(s => s == null))
        {
            var lib = Resources.Load<ChipsetLevelVisualLibrary>("ChipsetLevelVisualLibrary");
            if (lib != null && lib.primaryChipIcons != null && lib.primaryChipIcons.Length > 0)
            {
                visualLibrary = lib;
                icons = lib.primaryChipIcons;
            }
        }

        if (icons == null || icons.Length == 0) return null;

        string chipName = ChipsetBattleStats.GetChipsetName(chipsetId);
        if (string.IsNullOrEmpty(chipName)) return icons.FirstOrDefault(s => s != null);

        // 1. Direct StartsWith or Equals match (e.g. "Standard Gun" matches "Standard Gun (Súng Tiêu Chuẩn)")
        Sprite match = icons.FirstOrDefault(s => s != null && (
            string.Equals(s.name, chipName, StringComparison.OrdinalIgnoreCase) ||
            s.name.StartsWith(chipName, StringComparison.OrdinalIgnoreCase) ||
            s.name.IndexOf(chipName, StringComparison.OrdinalIgnoreCase) >= 0
        ));
        if (match != null) return match;

        // 2. Normalized name matching
        string cleanName = NormalizeSpriteName(chipName);
        match = icons.FirstOrDefault(s => s != null && (
            NormalizeSpriteName(s.name).StartsWith(cleanName, StringComparison.OrdinalIgnoreCase) ||
            NormalizeSpriteName(s.name).Contains(cleanName)
        ));
        if (match != null) return match;

        // 3. Direct 1-based index mapping into primaryChipIcons array (1..10)
        int idx = chipsetId - 1;
        if (idx >= 0 && idx < icons.Length && icons[idx] != null)
        {
            return icons[idx];
        }

        // 4. Fallback to ChipsetLevelUpPopup.FindMatchingIcon
        return ChipsetLevelUpPopup.FindMatchingIcon(icons, chipName);
    }

    private static string NormalizeSpriteName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        return name.Replace(" ", "").Replace("-", "").Replace("_", "").ToLowerInvariant();
    }

    private static GameObject CreateDefaultRowObject(Transform parent)
    {
        TMP_FontAsset font = FindAnyObjectByType<TMP_Text>()?.font ?? TMP_Settings.defaultFontAsset;

        GameObject row = new GameObject("DamageDetailRow", typeof(RectTransform), typeof(Image), typeof(DamageDetailRowUI), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(768f, 74f);
        row.GetComponent<Image>().color = new Color32(13, 34, 46, 255);
        row.GetComponent<LayoutElement>().minHeight = 74f;
        row.GetComponent<LayoutElement>().preferredHeight = 74f;

        // Icon Frame
        GameObject iconFrame = new GameObject("IconFrame", typeof(RectTransform), typeof(Image));
        iconFrame.transform.SetParent(row.transform, false);
        RectTransform iconFrameRt = iconFrame.GetComponent<RectTransform>();
        iconFrameRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconFrameRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconFrameRt.pivot = new Vector2(0.5f, 0.5f);
        iconFrameRt.anchoredPosition = new Vector2(-325f, 0f);
        iconFrameRt.sizeDelta = new Vector2(52f, 52f);
        iconFrame.GetComponent<Image>().color = new Color32(24, 64, 76, 255);

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(iconFrame.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;
        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.preserveAspect = true;

        // Texts
        TMP_Text nameText = CreateRuntimeText("Name", row.transform, "Standard Gun", 19f, Color.white, font);
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 13f;
        nameText.fontSizeMax = 19f;
        SetRect(nameText.rectTransform, new Vector2(-185f, 10f), new Vector2(210f, 26f));

        // Progress bar track & fill
        GameObject trackObj = new GameObject("ProgressBarTrack", typeof(RectTransform), typeof(Image));
        trackObj.transform.SetParent(row.transform, false);
        SetRect(trackObj.GetComponent<RectTransform>(), new Vector2(-185f, -14f), new Vector2(210f, 8f));
        trackObj.GetComponent<Image>().color = new Color32(10, 24, 34, 255);

        GameObject fillObj = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(trackObj.transform, false);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(0.5f, 1f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        Image fillImg = fillObj.GetComponent<Image>();
        fillImg.color = new Color32(78, 206, 196, 255);

        TMP_Text dpsText = CreateRuntimeText("DPS", row.transform, "357", 22f, Color.white, font);
        SetRect(dpsText.rectTransform, new Vector2(0f, 0f), new Vector2(80f, 50f));

        TMP_Text percentText = CreateRuntimeText("DPercent", row.transform, "59.8%", 22f, Color.white, font);
        SetRect(percentText.rectTransform, new Vector2(95f, 0f), new Vector2(80f, 50f));

        TMP_Text damageText = CreateRuntimeText("Damage", row.transform, "5,108", 22f, Color.white, font);
        SetRect(damageText.rectTransform, new Vector2(205f, 0f), new Vector2(110f, 50f));

        TMP_Text timeText = CreateRuntimeText("Time", row.transform, "00:14", 22f, Color.white, font);
        SetRect(timeText.rectTransform, new Vector2(310f, 0f), new Vector2(80f, 50f));

        DamageDetailRowUI rowUI = row.GetComponent<DamageDetailRowUI>();
        rowUI.EnsureLayout();
        return row;
    }

    public static DamageDetailsPopup CreateRuntimeModal(Transform canvasTransform)
    {
        TMP_FontAsset font = FindAnyObjectByType<TMP_Text>()?.font ?? TMP_Settings.defaultFontAsset;

        GameObject root = new GameObject("DamageDetailsModal", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        root.transform.SetParent(canvasTransform, false);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        Image bgDim = root.GetComponent<Image>();
        bgDim.color = new Color32(4, 9, 13, 235);
        bgDim.raycastTarget = true;

        // Background close button
        GameObject bgCloseObj = new GameObject("BackgroundCloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        bgCloseObj.transform.SetParent(root.transform, false);
        RectTransform bgCloseRt = bgCloseObj.GetComponent<RectTransform>();
        bgCloseRt.anchorMin = Vector2.zero;
        bgCloseRt.anchorMax = Vector2.one;
        bgCloseRt.offsetMin = Vector2.zero;
        bgCloseRt.offsetMax = Vector2.zero;
        Image bgCloseImg = bgCloseObj.GetComponent<Image>();
        bgCloseImg.color = Color.clear;
        bgCloseImg.raycastTarget = true;
        Button bgCloseBtn = bgCloseObj.GetComponent<Button>();

        // Main Frame: 840 x 1060
        GameObject frameObj = new GameObject("MainFrame", typeof(RectTransform), typeof(Image));
        frameObj.transform.SetParent(root.transform, false);
        RectTransform frameRt = frameObj.GetComponent<RectTransform>();
        SetRect(frameRt, Vector2.zero, new Vector2(840f, 1060f));
        frameObj.GetComponent<Image>().color = new Color32(11, 24, 34, 255);

        // Header Title: "Damage Details"
        GameObject titleContainer = new GameObject("TitleContainer", typeof(RectTransform));
        titleContainer.transform.SetParent(frameObj.transform, false);
        RectTransform titleRt = titleContainer.GetComponent<RectTransform>();
        SetRect(titleRt, new Vector2(0f, 475f), new Vector2(500f, 70f));

        TMP_Text titleText = CreateRuntimeText("Title", titleContainer.transform, "Damage Details", 38f, new Color32(255, 160, 32, 255), font);
        titleText.alignment = TextAlignmentOptions.Center;
        SetRect(titleText.rectTransform, Vector2.zero, new Vector2(400f, 60f));

        // Table Header
        GameObject headerRow = new GameObject("TableHeader", typeof(RectTransform), typeof(Image));
        headerRow.transform.SetParent(frameObj.transform, false);
        headerRow.GetComponent<Image>().color = new Color32(16, 42, 56, 255);
        SetRect(headerRow.GetComponent<RectTransform>(), new Vector2(0f, 410f), new Vector2(780f, 46f));

        Color cyan = new Color32(78, 206, 196, 255);
        TMP_Text hChip = CreateRuntimeText("H_CHIPSET", headerRow.transform, "CHIPSET", 22f, cyan, font);
        hChip.alignment = TextAlignmentOptions.Left;
        SetRect(hChip.rectTransform, new Vector2(-235f, 0f), new Vector2(250f, 40f));

        TMP_Text hDPS = CreateRuntimeText("H_DPS", headerRow.transform, "DPS", 22f, cyan, font);
        SetRect(hDPS.rectTransform, new Vector2(0f, 0f), new Vector2(80f, 40f));

        TMP_Text hD = CreateRuntimeText("H_DPercent", headerRow.transform, "D %", 22f, cyan, font);
        SetRect(hD.rectTransform, new Vector2(95f, 0f), new Vector2(80f, 40f));

        TMP_Text hDmg = CreateRuntimeText("H_Damage", headerRow.transform, "Damage", 22f, cyan, font);
        SetRect(hDmg.rectTransform, new Vector2(205f, 0f), new Vector2(110f, 40f));

        TMP_Text hTime = CreateRuntimeText("H_Time", headerRow.transform, "Time", 22f, cyan, font);
        SetRect(hTime.rectTransform, new Vector2(310f, 0f), new Vector2(80f, 40f));

        // Scroll Area
        GameObject scrollObj = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(frameObj.transform, false);
        SetRect(scrollObj.GetComponent<RectTransform>(), new Vector2(0f, -60f), new Vector2(780f, 840f));
        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 25f;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero;
        vpRt.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color = Color.white;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(6, 6, 6, 6);

        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = vpRt;
        scrollRect.content = contentRt;

        DamageDetailsPopup popup = root.AddComponent<DamageDetailsPopup>();
        popup.popupRoot = root;
        popup.popupCanvasGroup = root.GetComponent<CanvasGroup>();
        popup.modalFrame = frameRt;
        popup.rowsContainer = content.transform;
        popup.closeButton = null;
        popup.backgroundCloseButton = bgCloseBtn;
        popup.titleText = titleText;

        popup.BindCloseButtons();
        root.SetActive(false);
        return popup;
    }

    private static TMP_Text CreateRuntimeText(string name, Transform parent, string text, float size, Color color, TMP_FontAsset font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text tmp = go.GetComponent<TMP_Text>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }
}
