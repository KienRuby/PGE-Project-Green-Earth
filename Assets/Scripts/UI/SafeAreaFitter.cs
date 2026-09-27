using UnityEngine;

namespace PGE.UI
{
    /// <summary>
    /// Tự động co giãn RectTransform theo vùng an toàn Screen.safeArea của thiết bị di động (Android / iOS).
    /// Giúp giao diện (HUD, nút Pause, thanh máu, Wave timer) không bao giờ bị camera đục lỗ (Punch-hole),
    /// tai thỏ (Notch) hoặc thanh điều hướng cử chỉ (Home indicator) ở đáy màn hình che lấp.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Header("Options")]
        [Tooltip("Áp dụng vùng an toàn ở đỉnh màn hình (tránh camera tai thỏ/nốt ruồi).")]
        [SerializeField] private bool applyTop = true;

        [Tooltip("Áp dụng vùng an toàn ở đáy màn hình (tránh thanh cử chỉ vuốt Home).")]
        [SerializeField] private bool applyBottom = true;

        [Tooltip("Áp dụng vùng an toàn ở 2 bên cạnh trái/phải.")]
        [SerializeField] private bool applySides = true;

        private RectTransform panel;
        private Rect lastSafeArea = Rect.zero;
        private Vector2Int lastScreenSize = Vector2Int.zero;
        private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

        private void Awake()
        {
            panel = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void OnEnable()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea ||
                lastScreenSize.x != Screen.width ||
                lastScreenSize.y != Screen.height ||
                lastOrientation != Screen.orientation)
            {
                ApplySafeArea();
            }
        }

        /// <summary>
        /// Tính toán và cập nhật anchorMin / anchorMax tương ứng với Screen.safeArea.
        /// </summary>
        public void ApplySafeArea()
        {
            if (panel == null)
            {
                panel = GetComponent<RectTransform>();
                if (panel == null) return;
            }

            Rect safeArea = Screen.safeArea;
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;

            if (screenWidth <= 0 || screenHeight <= 0) return;

            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(screenWidth, screenHeight);
            lastOrientation = Screen.orientation;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= screenWidth;
            anchorMin.y /= screenHeight;
            anchorMax.x /= screenWidth;
            anchorMax.y /= screenHeight;

            // Nếu tùy chọn không áp dụng cho cạnh nào thì giữ nguyên 0 hoặc 1
            if (!applySides)
            {
                anchorMin.x = 0f;
                anchorMax.x = 1f;
            }
            if (!applyBottom)
            {
                anchorMin.y = 0f;
            }
            if (!applyTop)
            {
                anchorMax.y = 1f;
            }

            panel.anchorMin = anchorMin;
            panel.anchorMax = anchorMax;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
        }
    }
}
