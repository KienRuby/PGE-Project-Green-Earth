using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý lớp phủ đa ngôn ngữ (Localization Overlay) cho Cửa Hàng (ShopPanel):
/// Khi chuyển sang Tiếng Việt, hiển thị lớp phủ text Tiếng Việt sắc nét đè lên các phần hình ảnh tiếng Anh.
/// Khi chuyển về Tiếng Anh, tự động tắt toàn bộ lớp phủ để hiển thị lại đồ họa nguyên bản 100%.
/// </summary>
public sealed class ShopLocalizationOverlay : MonoBehaviour
{
    private readonly List<GameObject> activeOverlays = new List<GameObject>();
    private TMP_FontAsset cachedFont;
    private bool initialized;

    private void Awake()
    {
        CacheFont();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnEnable()
    {
        PGELocalization.OnLanguageChanged -= Refresh;
        PGELocalization.OnLanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        PGELocalization.OnLanguageChanged -= Refresh;
    }

    private void CacheFont()
    {
        if (cachedFont != null) return;

        var existingTmps = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < existingTmps.Length; i++)
        {
            if (existingTmps[i] != null && existingTmps[i].font != null)
            {
                cachedFont = existingTmps[i].font;
                return;
            }
        }

        var anyTmp = FindObjectOfType<TMP_Text>();
        if (anyTmp != null && anyTmp.font != null)
        {
            cachedFont = anyTmp.font;
        }
    }

    public void Refresh()
    {
        CacheFont();
        bool isVi = PGELocalization.IsVietnamese;

        if (isVi)
        {
            EnsureAllOverlaysCreated();
            SetOverlaysActive(true);
        }
        else
        {
            SetOverlaysActive(false);
        }
    }

    private void SetOverlaysActive(bool active)
    {
        for (int i = 0; i < activeOverlays.Count; i++)
        {
            if (activeOverlays[i] != null)
            {
                activeOverlays[i].SetActive(active);
            }
        }
    }

    private void EnsureAllOverlaysCreated()
    {
        if (initialized && activeOverlays.Count > 0 && activeOverlays[0] != null)
        {
            return;
        }

        activeOverlays.Clear();

        // 1. VIP Package Card
        Transform vipCard = FindDeepChild(transform, "Card_VIP_Package");
        if (vipCard != null)
        {
            SetupVipPackageOverlay(vipCard);
        }

        // 2. Special Item Header
        Transform specialHeader = FindDeepChild(transform, "Header_Special_Item");
        if (specialHeader != null)
        {
            SetupHeaderOverlay(specialHeader, "VẬT PHẨM ĐẶC BIỆT");
        }

        // 3. Welcome Package Card
        Transform welcomeCard = FindDeepChild(transform, "Card_Welcome_Package");
        if (welcomeCard != null)
        {
            SetupWelcomePackageOverlay(welcomeCard);
        }

        // 4. Intermediate & Advanced Packs
        Transform intermCard = FindDeepChild(transform, "Card_Intermediate_Pack");
        if (intermCard != null)
        {
            SetupCarouselPackOverlay(intermCard, "GÓI TRUNG CẤP", "Chỉ mua được 1 lần");
        }

        Transform advCard = FindDeepChild(transform, "Card_Advanced_Pack");
        if (advCard != null)
        {
            SetupCarouselPackOverlay(advCard, "GÓI CAO CẤP", "Chỉ mua được 1 lần");
        }

        // 5. Daily Shop Header
        Transform dailyHeader = FindDeepChild(transform, "Header_Daily_Shop");
        if (dailyHeader != null)
        {
            SetupHeaderOverlay(dailyHeader, "CỬA HÀNG HÀNG NGÀY");
        }

        // 6. Box Gacha Header
        Transform boxHeader = FindDeepChild(transform, "Header_Box");
        if (boxHeader != null)
        {
            SetupHeaderOverlay(boxHeader, "RƯƠNG VẬT PHẨM");
        }

        // 7. Meta Shop Header
        Transform metaHeader = FindDeepChild(transform, "Header_Meta_Shop");
        if (metaHeader != null)
        {
            SetupHeaderOverlay(metaHeader, "CỬA HÀNG NÂNG CAO");
        }

        // 8. Data Chip Header
        Transform dataHeader = FindDeepChild(transform, "Header_Data_Chip");
        if (dataHeader != null)
        {
            SetupHeaderOverlay(dataHeader, "CHIP DỮ LIỆU");
        }

        // 9. Gem Event Header
        Transform gemHeader = FindDeepChild(transform, "Header_Gem_Event");
        if (gemHeader != null)
        {
            SetupHeaderOverlay(gemHeader, "SỰ KIỆN NGỌC");
        }

        initialized = true;
    }

    private void SetupVipPackageOverlay(Transform parent)
    {
        Transform existing = parent.Find("VI_Overlay");
        if (existing != null)
        {
            if (!activeOverlays.Contains(existing.gameObject)) activeOverlays.Add(existing.gameObject);
            return;
        }

        GameObject root = CreateOverlayRoot(parent);
        activeOverlays.Add(root);

        // A. Title: "VIP Package" -> "GÓI VIP"
        CreatePatchWithText(
            root.transform,
            "TitleOverlay",
            new Vector2(-235f, 238f),
            new Vector2(440f, 75f),
            new Color32(4, 58, 77, 255),
            "<size=52><b><color=#5EEAD4>GÓI</color> <color=#FFD54F>VIP</color></b></size>",
            TextAlignmentOptions.Left,
            46f);

        // B. Yellow Banner: "AD FREE FOREVER" -> "KHÔNG QUẢNG CÁO VĨNH VIỄN"
        CreatePatchWithText(
            root.transform,
            "BannerOverlay",
            new Vector2(265f, 235f),
            new Vector2(360f, 78f),
            new Color32(245, 206, 66, 255),
            "<size=22><color=#000000><b>KHÔNG QUẢNG CÁO</b></color></size>\n<size=28><color=#006837><b><i>VĨNH VIỄN</i></b></color></size>",
            TextAlignmentOptions.Center,
            24f);

        // C. Left 8 Benefits Block
        string benefitsText =
@"<line-height=128%><size=23>• <color=#FFFFFF>Không quảng cáo</color> <color=#FFC236>Chơi mượt mà</color>
• <color=#FFC236>MỘT Gói nhận</color> <color=#FFFFFF>VÔ VÀN Đặc Quyền</color>
• <color=#FFFFFF>Hồi sinh x3 phần thưởng</color> <color=#FFC236>không xem QC</color>
• <color=#FFC236>+1 ô trang bị</color> <color=#FFFFFF>Drone bổ sung</color>
• <color=#FFC236>+1 lựa chọn Chipset</color> <color=#FFFFFF>khi lên cấp</color>
• <color=#FFFFFF>Nhận ngay</color> <color=#FF6E6E>10.000 ngọc</color>
• <color=#FFFFFF>Thưởng ngọc mỗi ngày:</color> <color=#FF6E6E>+380~580 ngọc</color>
• <color=#FFFFFF>Năng lượng mỗi ngày:</color> <color=#5EEAD4>+300~390</color></size>";

        CreatePatchWithText(
            root.transform,
            "BenefitsOverlay",
            new Vector2(-175f, 15f),
            new Vector2(550f, 360f),
            new Color32(4, 52, 70, 250),
            benefitsText,
            TextAlignmentOptions.Left,
            23f);
    }

    private void SetupHeaderOverlay(Transform parent, string headerTitle)
    {
        Transform existing = parent.Find("VI_Overlay");
        if (existing != null)
        {
            if (!activeOverlays.Contains(existing.gameObject)) activeOverlays.Add(existing.gameObject);
            return;
        }

        GameObject root = CreateOverlayRoot(parent);
        activeOverlays.Add(root);

        CreatePatchWithText(
            root.transform,
            "HeaderPatch",
            Vector2.zero,
            new Vector2(650f, 85f),
            new Color32(218, 57, 70, 255),
            $"<size=46><b>{headerTitle}</b></size>",
            TextAlignmentOptions.Center,
            46f,
            textColor: Color.white,
            outlineColor: new Color32(40, 10, 15, 255),
            outlineWidth: 0.25f);
    }

    private void SetupWelcomePackageOverlay(Transform parent)
    {
        Transform existing = parent.Find("VI_Overlay");
        if (existing != null)
        {
            if (!activeOverlays.Contains(existing.gameObject)) activeOverlays.Add(existing.gameObject);
            return;
        }

        GameObject root = CreateOverlayRoot(parent);
        activeOverlays.Add(root);

        // Title: "Welcome Package" -> "GÓI CHÀO MỪNG"
        CreatePatchWithText(
            root.transform,
            "TitleOverlay",
            new Vector2(0f, 215f),
            new Vector2(580f, 75f),
            new Color32(244, 124, 28, 255),
            "<size=44><b>GÓI CHÀO MỪNG</b></size>",
            TextAlignmentOptions.Center,
            44f,
            textColor: Color.white,
            outlineColor: new Color32(60, 20, 0, 255),
            outlineWidth: 0.25f);

        // Subtitle: "Purchasable 1 time" -> "Chỉ mua được 1 lần"
        CreatePatchWithText(
            root.transform,
            "SubtitleOverlay",
            new Vector2(0f, 160f),
            new Vector2(400f, 42f),
            new Color32(244, 124, 28, 255),
            "<size=26><color=#501400><b>Chỉ mua được 1 lần</b></color></size>",
            TextAlignmentOptions.Center,
            26f);

        // Badge: "More than 5x Value" -> "Giá trị hơn 5x"
        CreatePatchWithText(
            root.transform,
            "BadgeOverlay",
            new Vector2(385f, 175f),
            new Vector2(155f, 105f),
            new Color32(56, 179, 96, 255),
            "<size=18><b>Giá trị<br>hơn <color=#FFEA55>5x</color></b></size>",
            TextAlignmentOptions.Center,
            18f);

        // 4 item labels
        CreatePatchWithText(root.transform, "Label_Gem", new Vector2(-345f, -142f), new Vector2(180f, 36f), new Color32(5, 58, 76, 255), "<b>Ngọc</b>", TextAlignmentOptions.Center, 22f);
        CreatePatchWithText(root.transform, "Label_DataChip", new Vector2(-115f, -142f), new Vector2(180f, 36f), new Color32(5, 58, 76, 255), "<b>Chip Dữ Liệu</b>", TextAlignmentOptions.Center, 22f);
        CreatePatchWithText(root.transform, "Label_StandardGun", new Vector2(115f, -142f), new Vector2(180f, 36f), new Color32(5, 58, 76, 255), "<b>Súng Tiêu Chuẩn</b>", TextAlignmentOptions.Center, 20f);
        CreatePatchWithText(root.transform, "Label_RocketPunch", new Vector2(345f, -142f), new Vector2(180f, 36f), new Color32(5, 58, 76, 255), "<b>Cú Đấm Tên Lửa</b>", TextAlignmentOptions.Center, 20f);
    }

    private void SetupCarouselPackOverlay(Transform parent, string title, string subtitle)
    {
        Transform existing = parent.Find("VI_Overlay");
        if (existing != null)
        {
            if (!activeOverlays.Contains(existing.gameObject)) activeOverlays.Add(existing.gameObject);
            return;
        }

        GameObject root = CreateOverlayRoot(parent);
        activeOverlays.Add(root);

        CreatePatchWithText(
            root.transform,
            "TitleOverlay",
            new Vector2(0f, 215f),
            new Vector2(580f, 75f),
            new Color32(244, 124, 28, 255),
            $"<size=44><b>{title}</b></size>",
            TextAlignmentOptions.Center,
            44f,
            textColor: Color.white,
            outlineColor: new Color32(60, 20, 0, 255),
            outlineWidth: 0.25f);

        CreatePatchWithText(
            root.transform,
            "SubtitleOverlay",
            new Vector2(0f, 160f),
            new Vector2(400f, 42f),
            new Color32(244, 124, 28, 255),
            $"<size=26><color=#501400><b>{subtitle}</b></color></size>",
            TextAlignmentOptions.Center,
            26f);
    }

    private GameObject CreateOverlayRoot(Transform parent)
    {
        GameObject root = new GameObject("VI_Overlay", typeof(RectTransform));
        root.layer = LayerMask.NameToLayer("UI");
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        return root;
    }

    private void CreatePatchWithText(
        Transform parent,
        string name,
        Vector2 anchoredPosition,
        Vector2 size,
        Color32 patchColor,
        string textContent,
        TextAlignmentOptions alignment,
        float fontSize,
        Color? textColor = null,
        Color? outlineColor = null,
        float outlineWidth = 0f)
    {
        GameObject patchObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        patchObj.layer = LayerMask.NameToLayer("UI");
        RectTransform rt = patchObj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        Image img = patchObj.GetComponent<Image>();
        img.color = patchColor;
        img.raycastTarget = false;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.layer = LayerMask.NameToLayer("UI");
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.SetParent(patchObj.transform, false);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(8f, 2f);
        textRt.offsetMax = new Vector2(-8f, -2f);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        if (cachedFont != null) tmp.font = cachedFont;
        tmp.text = textContent;
        tmp.alignment = alignment;
        tmp.fontSize = fontSize;
        tmp.color = textColor ?? Color.white;
        tmp.raycastTarget = false;

        if (outlineWidth > 0f)
        {
            tmp.outlineWidth = outlineWidth;
            tmp.outlineColor = outlineColor ?? Color.black;
        }
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
            Transform found = FindDeepChild(child, childName);
            if (found != null) return found;
        }
        return null;
    }
}
