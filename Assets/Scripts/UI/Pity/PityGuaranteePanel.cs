using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện Popup "BẢO HIỂM LƯỢT ROLL" (PityGuaranteePanel):
/// - Hiển thị tiến độ bảo hiểm của 3 bậc: ELITE, EPIC, LEGEND.
/// - Đọc dữ liệu trực tiếp và độc quyền từ LabUpgradeController (không tạo bộ đếm thứ hai).
/// - Tự động cập nhật theo thời gian thực khi roll, load save hoặc reset.
/// - Hiệu ứng Pop mượt mà khi mở/đóng bằng unscaled time.
/// - Đóng an toàn khi bấm nút X, click vùng nền tối bên ngoài, phím Escape hoặc Android Back.
/// </summary>
public class PityGuaranteePanel : MonoBehaviour
{
    public const int EliteThreshold = 10;
    public const int EpicThreshold = 25;
    public const int LegendThreshold = 50;

    [Header("Controller Reference")]
    [Tooltip("Tham chiếu tới LabUpgradeController đang quản lý hệ thống roll/pity.")]
    [SerializeField] private LabUpgradeController labUpgradeController;

    [Header("Panel Root & Window")]
    [Tooltip("RectTransform của khung cửa sổ popup chính để tạo hiệu ứng Pop.")]
    [SerializeField] private RectTransform windowRect;

    [Tooltip("CanvasGroup của khung cửa sổ để tạo hiệu ứng Fade.")]
    [SerializeField] private CanvasGroup windowCanvasGroup;

    [Tooltip("CanvasGroup của lớp nền tối Dim Background.")]
    [SerializeField] private CanvasGroup dimCanvasGroup;

    [Header("Buttons")]
    [Tooltip("Nút đóng popup (dấu X ở góc trên bên phải).")]
    [SerializeField] private Button closeButton;

    [Tooltip("Nút nền tối bao quanh cửa sổ, click vào sẽ tự động đóng popup.")]
    [SerializeField] private Button dimBackgroundButton;

    [Header("Pity Rows")]
    [Tooltip("Hàng hiển thị tiến độ bảo hiểm bậc ELITE.")]
    [SerializeField] private PityProgressRow eliteRow;

    [Tooltip("Hàng hiển thị tiến độ bảo hiểm bậc EPIC.")]
    [SerializeField] private PityProgressRow epicRow;

    [Tooltip("Hàng hiển thị tiến độ bảo hiểm bậc LEGEND.")]
    [SerializeField] private PityProgressRow legendRow;

    [Header("Texts")]
    [Tooltip("Text tiêu đề popup.")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("Text hướng dẫn / ghi chú quy tắc bảo hiểm phía dưới panel.")]
    [SerializeField] private TMP_Text descriptionText;

    [Header("Animation Settings")]
    [Tooltip("Thời gian mở hiệu ứng Pop (giây).")]
    [SerializeField] private float openDuration = 0.22f;

    [Tooltip("Thời gian đóng hiệu ứng Fade (giây).")]
    [SerializeField] private float closeDuration = 0.12f;

    private Coroutine animateRoutine;
    private bool isOpen = false;
    private float openedTime = 0f;
    private Canvas rootCanvas;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        EnsureComponentsCached();
        SetupListeners();
    }

    private void OnEnable()
    {
        // The panel can be enabled by scene/prefab state or by another controller,
        // so keep the logical state in sync with the active GameObject.
        isOpen = true;

        EnsureComponentsCached();
        SetupListeners();

        if (labUpgradeController != null)
        {
            labUpgradeController.OnPityDataChanged -= HandlePityDataChanged;
            labUpgradeController.OnPityDataChanged += HandlePityDataChanged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        isOpen = false;

        if (labUpgradeController != null)
        {
            labUpgradeController.OnPityDataChanged -= HandlePityDataChanged;
        }

        if (animateRoutine != null)
        {
            StopCoroutine(animateRoutine);
            animateRoutine = null;
        }
    }

    private void Update()
    {
        if (!isOpen) return;

        // 1. Hỗ trợ phím Escape trên PC và nút Back trên Android
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        // 2. Bỏ qua frame vừa mở để tránh trùng sự kiện click mở panel
        if (Time.unscaledTime - openedTime < 0.1f)
        {
            return;
        }

        // 3. Nhận diện thao tác click chuột / chạm màn hình ở bất kỳ đâu trên màn hình
        bool pointerDown = false;
        Vector2 screenPoint = Vector2.zero;

        if (Input.GetMouseButtonDown(0))
        {
            pointerDown = true;
            screenPoint = Input.mousePosition;
        }
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            pointerDown = true;
            screenPoint = Input.GetTouch(0).position;
        }

        if (pointerDown && windowRect != null)
        {
            Camera cam = null;
            if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
            }

            // Nếu vị trí click nằm ngoài khung WindowContainer -> Đóng panel ngay lập tức
            if (!RectTransformUtility.RectangleContainsScreenPoint(windowRect, screenPoint, cam))
            {
                Close();
            }
        }
    }

    private void EnsureComponentsCached()
    {
        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        if (windowRect == null)
        {
            Transform win = transform.Find("WindowContainer") ?? transform.Find("Window");
            if (win != null) windowRect = win.GetComponent<RectTransform>();
            else windowRect = GetComponent<RectTransform>();
        }

        if (windowCanvasGroup == null && windowRect != null)
        {
            windowCanvasGroup = windowRect.GetComponent<CanvasGroup>();
            if (windowCanvasGroup == null) windowCanvasGroup = windowRect.gameObject.AddComponent<CanvasGroup>();
        }

        if (dimCanvasGroup == null)
        {
            Transform dim = transform.Find("DimBackground") ?? transform.Find("Backdrop");
            if (dim != null)
            {
                dimCanvasGroup = dim.GetComponent<CanvasGroup>();
                if (dimCanvasGroup == null) dimCanvasGroup = dim.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (dimBackgroundButton == null)
        {
            Transform dim = transform.Find("DimBackground") ?? transform.Find("Backdrop");
            if (dim != null)
            {
                dimBackgroundButton = dim.GetComponent<Button>();
                if (dimBackgroundButton == null)
                {
                    dimBackgroundButton = dim.gameObject.AddComponent<Button>();
                    dimBackgroundButton.transition = Selectable.Transition.None;
                }
            }
        }
    }

    private void SetupListeners()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (dimBackgroundButton != null)
        {
            dimBackgroundButton.onClick.RemoveListener(Close);
            dimBackgroundButton.onClick.AddListener(Close);
        }
    }

    /// <summary>
    /// Mở giao diện PityGuaranteePanel với hiệu ứng Pop mượt mà.
    /// </summary>
    public void Open()
    {
        if (isOpen) return;

        isOpen = true;
        openedTime = Time.unscaledTime;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        EnsureComponentsCached();
        SetupListeners();

        if (labUpgradeController != null)
        {
            labUpgradeController.OnPityDataChanged -= HandlePityDataChanged;
            labUpgradeController.OnPityDataChanged += HandlePityDataChanged;
        }

        Refresh();

        if (animateRoutine != null)
        {
            StopCoroutine(animateRoutine);
        }
        animateRoutine = StartCoroutine(AnimateOpenRoutine());
    }

    /// <summary>
    /// Đóng giao diện PityGuaranteePanel an toàn.
    /// </summary>
    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;

        if (animateRoutine != null)
        {
            StopCoroutine(animateRoutine);
        }
        animateRoutine = StartCoroutine(AnimateCloseRoutine());
    }

    /// <summary>
    /// Cập nhật toàn bộ thông số bảo hiểm từ LabUpgradeController.
    /// </summary>
    public void Refresh()
    {
        if (labUpgradeController == null)
        {
            labUpgradeController = FindFirstObjectByType<LabUpgradeController>();
        }

        if (labUpgradeController == null)
        {
            return;
        }

        if (titleText != null)
        {
            titleText.text = "BẢO HIỂM LƯỢT ROLL";
        }

        // Cập nhật 3 hàng tương ứng với 3 bậc bảo hiểm trong hệ thống
        if (eliteRow != null)
        {
            eliteRow.Setup(
                "ELITE",
                labUpgradeController.ElitePityCounter,
                labUpgradeController.ElitePityThreshold,
                labUpgradeController.EliteRarityColor
            );
        }

        if (epicRow != null)
        {
            epicRow.Setup(
                "EPIC",
                labUpgradeController.EpicPityCounter,
                labUpgradeController.EpicPityThreshold,
                labUpgradeController.EpicRarityColor
            );
        }

        if (legendRow != null)
        {
            legendRow.Setup(
                "LEGEND",
                labUpgradeController.LegendPityCounter,
                labUpgradeController.LegendPityThreshold,
                labUpgradeController.LegendRarityColor
            );
        }

        if (descriptionText != null)
        {
            descriptionText.text = "• Quay trúng bậc nào sẽ chỉ đặt lại bộ đếm bảo hiểm của bậc đó về 0.\n• Các bậc còn lại tiếp tục tích lũy độc lập và không bị ảnh hưởng!\n• Khi đạt mốc bảo hiểm, lượt quay tiếp theo chắc chắn nhận được bậc đó!";
        }
    }

    private void HandlePityDataChanged()
    {
        if (gameObject.activeInHierarchy)
        {
            Refresh();
        }
    }

    private IEnumerator AnimateOpenRoutine()
    {
        if (dimCanvasGroup != null) dimCanvasGroup.alpha = 0f;
        if (windowCanvasGroup != null) windowCanvasGroup.alpha = 0f;
        if (windowRect != null) windowRect.localScale = new Vector3(0.85f, 0.85f, 1f);

        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);

            // Pop curve: 0.85 -> 1.05 -> 1.0
            float scale;
            if (t < 0.65f)
            {
                float subT = t / 0.65f;
                scale = Mathf.Lerp(0.85f, 1.05f, 1f - (1f - subT) * (1f - subT));
            }
            else
            {
                float subT = (t - 0.65f) / 0.35f;
                scale = Mathf.Lerp(1.05f, 1.00f, subT * subT);
            }

            if (windowRect != null) windowRect.localScale = new Vector3(scale, scale, 1f);
            if (windowCanvasGroup != null) windowCanvasGroup.alpha = Mathf.Clamp01(elapsed / (openDuration * 0.5f));
            if (dimCanvasGroup != null) dimCanvasGroup.alpha = Mathf.Clamp01(elapsed / (openDuration * 0.7f));

            yield return null;
        }

        if (dimCanvasGroup != null) dimCanvasGroup.alpha = 1f;
        if (windowCanvasGroup != null) windowCanvasGroup.alpha = 1f;
        if (windowRect != null) windowRect.localScale = Vector3.one;
        animateRoutine = null;
    }

    private IEnumerator AnimateCloseRoutine()
    {
        float elapsed = 0f;
        Vector3 startScale = windowRect != null ? windowRect.localScale : Vector3.one;

        while (elapsed < closeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / closeDuration);
            float ease = t * t;

            if (windowRect != null) windowRect.localScale = Vector3.Lerp(startScale, new Vector3(0.90f, 0.90f, 1f), ease);
            if (windowCanvasGroup != null) windowCanvasGroup.alpha = 1f - ease;
            if (dimCanvasGroup != null) dimCanvasGroup.alpha = 1f - ease;

            yield return null;
        }

        gameObject.SetActive(false);
        if (windowRect != null) windowRect.localScale = Vector3.one;
        if (windowCanvasGroup != null) windowCanvasGroup.alpha = 1f;
        if (dimCanvasGroup != null) dimCanvasGroup.alpha = 1f;
        animateRoutine = null;
    }

    public void SetReferencesForTesting(
        LabUpgradeController controller,
        PityProgressRow elite,
        PityProgressRow epic,
        PityProgressRow legend,
        Button closeBtn = null,
        Button dimBtn = null)
    {
        labUpgradeController = controller;
        eliteRow = elite;
        epicRow = epic;
        legendRow = legend;
        closeButton = closeBtn;
        dimBackgroundButton = dimBtn;
    }
}
