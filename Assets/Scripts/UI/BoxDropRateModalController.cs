using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller điều khiển hiển thị bảng tỷ lệ mở hộp Chipset và Drone trong Shop.
/// Đảm bảo giao diện Cyberpunk đồng bộ, hiển thị chi tiết phần trăm và số lượng mảnh nhận được.
/// </summary>
public class BoxDropRateModalController : MonoBehaviour
{
    private static BoxDropRateModalController runtimeInstance;
    public static BoxDropRateModalController Instance
    {
        get
        {
            if (runtimeInstance == null)
            {
                runtimeInstance = FindObjectOfType<BoxDropRateModalController>(true);
                if (runtimeInstance == null)
                {
                    runtimeInstance = EnsureModalInCanvas();
                }
            }
            return runtimeInstance;
        }
        private set => runtimeInstance = value;
    }

    [Header("UI Elements")]
    [SerializeField] private GameObject modalRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text noteText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundDimButton;
    [SerializeField] private Transform rowsContainer;

    [Header("Row Elements")]
    [SerializeField] private TMP_Text rate7Text;
    [SerializeField] private TMP_Text reward7Text;
    [SerializeField] private TMP_Text rate23Text;
    [SerializeField] private TMP_Text reward23Text;
    [SerializeField] private TMP_Text rate70Text;
    [SerializeField] private TMP_Text reward70Text;

    private void Awake()
    {
        if (runtimeInstance == null)
        {
            runtimeInstance = this;
        }
        else if (runtimeInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }

        if (backgroundDimButton != null)
        {
            backgroundDimButton.onClick.RemoveAllListeners();
            backgroundDimButton.onClick.AddListener(Close);
        }
    }

    public void ShowChipsetRates()
    {
        Show("chipset");
    }

    public void ShowDroneRates()
    {
        Show("drone");
    }

    public void Show(string boxType)
    {
        if (modalRoot != null)
        {
            modalRoot.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }

        bool isChipset = boxType.ToLower().Contains("chipset");

        if (titleText != null)
        {
            titleText.text = isChipset ? "CHIPSET BOX PROBABILITY" : "DRONE BOX PROBABILITY";
        }

        if (subtitleText != null)
        {
            subtitleText.text = isChipset 
                ? "Mỗi hộp Chipset chứa các mảnh Chipset ngẫu nhiên với tỷ lệ:" 
                : "Mỗi hộp Drone chứa các mảnh Drone ngẫu nhiên với tỷ lệ:";
        }

        if (reward7Text != null)
        {
            reward7Text.text = isChipset ? "7 Mảnh Chipset (x7)" : "7 Mảnh Drone (x7)";
        }

        if (reward23Text != null)
        {
            reward23Text.text = isChipset ? "3 Mảnh Chipset (x3)" : "3 Mảnh Drone (x3)";
        }

        if (reward70Text != null)
        {
            reward70Text.text = isChipset ? "1 Mảnh Chipset (x1)" : "1 Mảnh Drone (x1)";
        }

        if (noteText != null)
        {
            noteText.text = "* Lưu ý: Hộp 10 lần (10 times) tương đương mở 10 hộp với cùng tỷ lệ độc lập trên.";
        }
    }

    public void Close()
    {
        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public static BoxDropRateModalController EnsureModalInCanvas(Canvas targetCanvas = null)
    {
        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
        }

        if (targetCanvas == null)
        {
            Debug.LogWarning("[BoxDropRateModal] Không tìm thấy Canvas để khởi tạo modal!");
            return null;
        }

        Transform existing = targetCanvas.transform.Find("BoxDropRateModal");
        if (existing != null)
        {
            var existingCtrl = existing.GetComponent<BoxDropRateModalController>();
            if (existingCtrl != null) return existingCtrl;
        }

        // Tạo root Modal
        GameObject root = new GameObject("BoxDropRateModal", typeof(RectTransform), typeof(BoxDropRateModalController));
        root.transform.SetParent(targetCanvas.transform, false);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;
        rootRt.anchoredPosition = Vector2.zero;

        // Background Dim
        GameObject dimObj = new GameObject("DimBackground", typeof(RectTransform), typeof(Image), typeof(Button));
        dimObj.transform.SetParent(root.transform, false);
        RectTransform dimRt = dimObj.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.sizeDelta = Vector2.zero;
        Image dimImg = dimObj.GetComponent<Image>();
        dimImg.color = new Color(0.02f, 0.05f, 0.09f, 0.85f);
        Button dimBtn = dimObj.GetComponent<Button>();

        // Center Panel Frame
        GameObject panelObj = new GameObject("ContentPanel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(root.transform, false);
        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(580f, 520f);
        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.color = new Color(0.05f, 0.09f, 0.15f, 0.98f);

        // Header Title
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -22f);
        titleRt.sizeDelta = new Vector2(520f, 40f);
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "BOX DROP RATES";
        titleTmp.fontSize = 24f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.18f, 0.9f, 0.98f, 1f);

        // Subtitle
        GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(panelObj.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.5f, 1f);
        subRt.anchorMax = new Vector2(0.5f, 1f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.anchoredPosition = new Vector2(0f, -68f);
        subRt.sizeDelta = new Vector2(500f, 40f);
        TextMeshProUGUI subTmp = subObj.GetComponent<TextMeshProUGUI>();
        subTmp.text = "Mỗi hộp chứa ngẫu nhiên các mảnh với tỷ lệ:";
        subTmp.fontSize = 15f;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = new Color(0.75f, 0.85f, 0.95f, 1f);

        // Rows Container
        GameObject rowsObj = new GameObject("RowsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        rowsObj.transform.SetParent(panelObj.transform, false);
        RectTransform rowsRt = rowsObj.GetComponent<RectTransform>();
        rowsRt.anchorMin = new Vector2(0.5f, 1f);
        rowsRt.anchorMax = new Vector2(0.5f, 1f);
        rowsRt.pivot = new Vector2(0.5f, 1f);
        rowsRt.anchoredPosition = new Vector2(0f, -120f);
        rowsRt.sizeDelta = new Vector2(500f, 240f);
        VerticalLayoutGroup vlg = rowsObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 12f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // Tạo 3 hàng
        CreateRateRow(rowsObj.transform, "Row_7Percent", "7%", new Color(1f, 0.85f, 0.2f, 1f), "7 Mảnh (x7)", "⭐ SIÊU HIẾM", out TMP_Text r7, out TMP_Text rew7);
        CreateRateRow(rowsObj.transform, "Row_23Percent", "23%", new Color(0.35f, 0.8f, 1f, 1f), "3 Mảnh (x3)", "★ HIẾM", out TMP_Text r23, out TMP_Text rew23);
        CreateRateRow(rowsObj.transform, "Row_70Percent", "70%", new Color(0.85f, 0.9f, 0.95f, 1f), "1 Mảnh (x1)", "PHỔ BIẾN", out TMP_Text r70, out TMP_Text rew70);

        // Note
        GameObject noteObj = new GameObject("NoteText", typeof(RectTransform), typeof(TextMeshProUGUI));
        noteObj.transform.SetParent(panelObj.transform, false);
        RectTransform noteRt = noteObj.GetComponent<RectTransform>();
        noteRt.anchorMin = new Vector2(0.5f, 0f);
        noteRt.anchorMax = new Vector2(0.5f, 0f);
        noteRt.pivot = new Vector2(0.5f, 0f);
        noteRt.anchoredPosition = new Vector2(0f, 75f);
        noteRt.sizeDelta = new Vector2(500f, 35f);
        TextMeshProUGUI noteTmp = noteObj.GetComponent<TextMeshProUGUI>();
        noteTmp.text = "* Hộp 10 lần (10 times) tương đương mở 10 hộp độc lập với cùng tỷ lệ trên.";
        noteTmp.fontSize = 13f;
        noteTmp.fontStyle = FontStyles.Italic;
        noteTmp.alignment = TextAlignmentOptions.Center;
        noteTmp.color = new Color(0.6f, 0.7f, 0.8f, 1f);

        // Close Button
        GameObject closeBtnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnObj.transform.SetParent(panelObj.transform, false);
        RectTransform closeBtnRt = closeBtnObj.GetComponent<RectTransform>();
        closeBtnRt.anchorMin = new Vector2(0.5f, 0f);
        closeBtnRt.anchorMax = new Vector2(0.5f, 0f);
        closeBtnRt.pivot = new Vector2(0.5f, 0f);
        closeBtnRt.anchoredPosition = new Vector2(0f, 20f);
        closeBtnRt.sizeDelta = new Vector2(200f, 44f);
        Image closeImg = closeBtnObj.GetComponent<Image>();
        closeImg.color = new Color(0.1f, 0.75f, 0.85f, 1f);
        Button closeBtn = closeBtnObj.GetComponent<Button>();

        GameObject closeTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform closeTxtRt = closeTxtObj.GetComponent<RectTransform>();
        closeTxtRt.anchorMin = Vector2.zero;
        closeTxtRt.anchorMax = Vector2.one;
        closeTxtRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI closeTxt = closeTxtObj.GetComponent<TextMeshProUGUI>();
        closeTxt.text = "ĐÓNG";
        closeTxt.fontSize = 18f;
        closeTxt.fontStyle = FontStyles.Bold;
        closeTxt.alignment = TextAlignmentOptions.Center;
        closeTxt.color = new Color(0.04f, 0.08f, 0.14f, 1f);

        // Nút X nhỏ góc trên bên phải
        GameObject xBtnObj = new GameObject("Button_X", typeof(RectTransform), typeof(Image), typeof(Button));
        xBtnObj.transform.SetParent(panelObj.transform, false);
        RectTransform xBtnRt = xBtnObj.GetComponent<RectTransform>();
        xBtnRt.anchorMin = new Vector2(1f, 1f);
        xBtnRt.anchorMax = new Vector2(1f, 1f);
        xBtnRt.pivot = new Vector2(1f, 1f);
        xBtnRt.anchoredPosition = new Vector2(-12f, -12f);
        xBtnRt.sizeDelta = new Vector2(34f, 34f);
        Image xImg = xBtnObj.GetComponent<Image>();
        xImg.color = new Color(0.2f, 0.28f, 0.38f, 0.9f);
        Button xBtn = xBtnObj.GetComponent<Button>();

        GameObject xTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        xTxtObj.transform.SetParent(xBtnObj.transform, false);
        RectTransform xTxtRt = xTxtObj.GetComponent<RectTransform>();
        xTxtRt.anchorMin = Vector2.zero;
        xTxtRt.anchorMax = Vector2.one;
        xTxtRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI xTxt = xTxtObj.GetComponent<TextMeshProUGUI>();
        xTxt.text = "✕";
        xTxt.fontSize = 18f;
        xTxt.alignment = TextAlignmentOptions.Center;
        xTxt.color = Color.white;

        // Wire Controller
        BoxDropRateModalController ctrl = root.GetComponent<BoxDropRateModalController>();
        ctrl.modalRoot = root;
        ctrl.titleText = titleTmp;
        ctrl.subtitleText = subTmp;
        ctrl.noteText = noteTmp;
        ctrl.closeButton = closeBtn;
        ctrl.backgroundDimButton = dimBtn;
        ctrl.rate7Text = r7;
        ctrl.reward7Text = rew7;
        ctrl.rate23Text = r23;
        ctrl.reward23Text = rew23;
        ctrl.rate70Text = r70;
        ctrl.reward70Text = rew70;

        xBtn.onClick.AddListener(ctrl.Close);
        closeBtn.onClick.AddListener(ctrl.Close);
        dimBtn.onClick.AddListener(ctrl.Close);

        root.SetActive(false);
        return ctrl;
    }

    private static void CreateRateRow(Transform parent, string name, string rateStr, Color rateColor, string rewardStr, string tagStr, out TMP_Text rateOut, out TMP_Text rewardOut)
    {
        GameObject row = new GameObject(name, typeof(RectTransform), typeof(Image));
        row.transform.SetParent(parent, false);
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500f, 56f);
        Image img = row.GetComponent<Image>();
        img.color = new Color(0.08f, 0.14f, 0.22f, 0.95f);

        // Rate Badge
        GameObject rateObj = new GameObject("RateBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
        rateObj.transform.SetParent(row.transform, false);
        RectTransform rateRt = rateObj.GetComponent<RectTransform>();
        rateRt.anchorMin = new Vector2(0f, 0.5f);
        rateRt.anchorMax = new Vector2(0f, 0.5f);
        rateRt.pivot = new Vector2(0f, 0.5f);
        rateRt.anchoredPosition = new Vector2(16f, 0f);
        rateRt.sizeDelta = new Vector2(90f, 40f);
        TextMeshProUGUI rateTmp = rateObj.GetComponent<TextMeshProUGUI>();
        rateTmp.text = rateStr;
        rateTmp.fontSize = 24f;
        rateTmp.fontStyle = FontStyles.Bold;
        rateTmp.color = rateColor;
        rateTmp.alignment = TextAlignmentOptions.Left;
        rateOut = rateTmp;

        // Reward Text
        GameObject rewObj = new GameObject("RewardText", typeof(RectTransform), typeof(TextMeshProUGUI));
        rewObj.transform.SetParent(row.transform, false);
        RectTransform rewRt = rewObj.GetComponent<RectTransform>();
        rewRt.anchorMin = new Vector2(0f, 0.5f);
        rewRt.anchorMax = new Vector2(0f, 0.5f);
        rewRt.pivot = new Vector2(0f, 0.5f);
        rewRt.anchoredPosition = new Vector2(115f, 0f);
        rewRt.sizeDelta = new Vector2(250f, 40f);
        TextMeshProUGUI rewTmp = rewObj.GetComponent<TextMeshProUGUI>();
        rewTmp.text = rewardStr;
        rewTmp.fontSize = 17f;
        rewTmp.fontStyle = FontStyles.Bold;
        rewTmp.color = Color.white;
        rewTmp.alignment = TextAlignmentOptions.Left;
        rewardOut = rewTmp;

        // Tag Text (SIÊU HIẾM, HIẾM, ...)
        GameObject tagObj = new GameObject("TagText", typeof(RectTransform), typeof(TextMeshProUGUI));
        tagObj.transform.SetParent(row.transform, false);
        RectTransform tagRt = tagObj.GetComponent<RectTransform>();
        tagRt.anchorMin = new Vector2(1f, 0.5f);
        tagRt.anchorMax = new Vector2(1f, 0.5f);
        tagRt.pivot = new Vector2(1f, 0.5f);
        tagRt.anchoredPosition = new Vector2(-16f, 0f);
        tagRt.sizeDelta = new Vector2(130f, 40f);
        TextMeshProUGUI tagTmp = tagObj.GetComponent<TextMeshProUGUI>();
        tagTmp.text = tagStr;
        tagTmp.fontSize = 13f;
        tagTmp.color = rateColor;
        tagTmp.alignment = TextAlignmentOptions.Right;
    }
}
