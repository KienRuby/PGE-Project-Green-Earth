using System;
using UnityEngine;

/// <summary>
/// Hiển thị FPS và thời gian render frame (ms) thời gian thực trên màn hình.
/// Tự động khởi tạo khi chạy game, hoạt động trên mọi bản build (Editor / Development / Release).
/// Không tạo rác bộ nhớ (0 GC Allocation).
/// Phím tắt F3 trên PC hoặc chạm 3 ngón tay trên điện thoại để bật/tắt.
/// Chạm/Click trực tiếp vào khung FPS để đổi góc hiển thị (Top-Right -> Top-Center -> Top-Left -> Bottom-Right).
/// </summary>
public class FPSDisplay : MonoBehaviour
{
    public enum ScreenAnchorPosition
    {
        TopRight = 0,
        TopCenter = 1,
        TopLeft = 2,
        BottomRight = 3
    }

    private static FPSDisplay instance;
    public static FPSDisplay Instance => instance;

    public const string ShowFpsPrefKey = "PGE.Settings.ShowFPS";
    public const string FpsPositionPrefKey = "PGE.Settings.FPSPosition";

    [Header("Display Settings")]
    [SerializeField] private bool showFPS = true;
    [SerializeField] private ScreenAnchorPosition position = ScreenAnchorPosition.TopRight;
    [SerializeField] private float updateInterval = 0.2f;

    private float accum = 0f;
    private int frames = 0;
    private float timeLeft = 0f;
    private int currentFpsInt = 60;
    private string currentFpsText = "60 FPS (16.7ms)";

    // Zero-allocation precomputed string cache for 0..300 FPS
    private static readonly string[] FpsStringCache = new string[301];

    private GUIStyle textStyle;
    private GUIStyle shadowStyle;
    private Texture2D bgTexture;
    private Texture2D borderTexture;
    private Rect badgeRect;
    private int lastScreenWidth = 0;
    private int lastScreenHeight = 0;
    private int cachedFontSize = 20;
    private float cachedBadgeWidth = 175f;
    private float cachedBadgeHeight = 36f;

    static FPSDisplay()
    {
        // Khởi tạo bảng chuỗi đệm 0 GC cho toàn bộ dải FPS từ 0 đến 300
        for (int i = 0; i <= 300; i++)
        {
            float ms = i > 0 ? (1000f / i) : 99.9f;
            FpsStringCache[i] = $"{i} FPS ({ms:F1}ms)";
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("[FPS_Display]");
            instance = go.AddComponent<FPSDisplay>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        showFPS = PlayerPrefs.GetInt(ShowFpsPrefKey, 1) == 1;
        position = (ScreenAnchorPosition)PlayerPrefs.GetInt(FpsPositionPrefKey, (int)ScreenAnchorPosition.TopRight);

        timeLeft = updateInterval;
        CreateTextures();
    }

    private void CreateTextures()
    {
        if (bgTexture == null)
        {
            bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, new Color(0.06f, 0.09f, 0.14f, 0.82f));
            bgTexture.Apply();
        }

        if (borderTexture == null)
        {
            borderTexture = new Texture2D(1, 1);
            borderTexture.SetPixel(0, 0, new Color(0.28f, 0.40f, 0.52f, 0.65f));
            borderTexture.Apply();
        }
    }

    private void OnDestroy()
    {
        if (bgTexture != null) Destroy(bgTexture);
        if (borderTexture != null) Destroy(borderTexture);
    }

    private void Update()
    {
        // Phím tắt F3 trên bàn phím hoặc chạm 3 ngón tay trên màn hình cảm ứng để bật/tắt
        if (Input.GetKeyDown(KeyCode.F3) || (Input.touchCount >= 3 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            ToggleShowFPS();
        }

        if (!showFPS) return;

        float dt = Time.unscaledDeltaTime;
        if (dt > 0.00001f)
        {
            accum += 1f / dt;
            frames++;
        }
        timeLeft -= dt;

        if (timeLeft <= 0.0f)
        {
            if (frames > 0)
            {
                int avgFps = Mathf.Clamp(Mathf.RoundToInt(accum / frames), 0, 300);
                currentFpsInt = avgFps;
                currentFpsText = FpsStringCache[avgFps];
            }

            timeLeft = updateInterval;
            accum = 0f;
            frames = 0;
        }
    }

    public void ToggleShowFPS()
    {
        showFPS = !showFPS;
        PlayerPrefs.SetInt(ShowFpsPrefKey, showFPS ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void CyclePosition()
    {
        position = (ScreenAnchorPosition)(((int)position + 1) % 4);
        PlayerPrefs.SetInt(FpsPositionPrefKey, (int)position);
        PlayerPrefs.Save();
        RecalculateBadgeRect();
    }

    public void SetPosition(ScreenAnchorPosition newPosition)
    {
        position = newPosition;
        PlayerPrefs.SetInt(FpsPositionPrefKey, (int)position);
        PlayerPrefs.Save();
        RecalculateBadgeRect();
    }

    private void RecalculateBadgeRect()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        // Tự động scale cỡ chữ theo độ phân giải màn hình
        cachedFontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.0165f), 17, 34);
        cachedBadgeWidth = cachedFontSize * 8.6f;
        cachedBadgeHeight = cachedFontSize * 1.75f;

        Rect safe = Screen.safeArea;
        float topInset = Mathf.Max(0f, Screen.height - safe.yMax);
        float leftInset = Mathf.Max(0f, safe.xMin);
        float rightInset = Mathf.Max(0f, Screen.width - safe.xMax);
        float bottomInset = Mathf.Max(0f, safe.yMin);

        float x = 0f;
        float y = 0f;

        switch (position)
        {
            case ScreenAnchorPosition.TopRight:
                x = Screen.width - rightInset - cachedBadgeWidth - 14f;
                y = topInset + 10f;
                break;
            case ScreenAnchorPosition.TopCenter:
                x = (Screen.width - cachedBadgeWidth) * 0.5f;
                y = topInset + 10f;
                break;
            case ScreenAnchorPosition.TopLeft:
                x = leftInset + 14f;
                y = topInset + 10f;
                break;
            case ScreenAnchorPosition.BottomRight:
                x = Screen.width - rightInset - cachedBadgeWidth - 14f;
                y = Screen.height - bottomInset - cachedBadgeHeight - 16f;
                break;
        }

        badgeRect = new Rect(x, y, cachedBadgeWidth, cachedBadgeHeight);
    }

    private void OnGUI()
    {
        if (!showFPS) return;

        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            RecalculateBadgeRect();
        }

        if (textStyle == null)
        {
            textStyle = new GUIStyle();
            textStyle.fontStyle = FontStyle.Bold;
            textStyle.alignment = TextAnchor.MiddleCenter;

            shadowStyle = new GUIStyle(textStyle);
            shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.9f);
        }

        textStyle.fontSize = cachedFontSize;
        shadowStyle.fontSize = cachedFontSize;

        // Xử lý Click / Tap vào khung FPS để đổi góc hiển thị thuận tiện
        Event e = Event.current;
        if (e.type == EventType.MouseDown && badgeRect.Contains(e.mousePosition))
        {
            CyclePosition();
            e.Use();
        }

        if (bgTexture == null || borderTexture == null)
        {
            CreateTextures();
        }

        // 1. Vẽ khung viền mảnh ngoài
        GUI.DrawTexture(badgeRect, borderTexture);

        // 2. Vẽ nền tối trong suốt (Dark Glass Badge)
        Rect innerRect = new Rect(badgeRect.x + 1.5f, badgeRect.y + 1.5f, badgeRect.width - 3f, badgeRect.height - 3f);
        GUI.DrawTexture(innerRect, bgTexture);

        // 3. Đổi màu theo mức FPS:
        // - Xanh ngọc mượt mà (>= 55 FPS)
        // - Vàng cam ổn định (30 - 54 FPS)
        // - Đỏ cảnh báo (< 30 FPS)
        if (currentFpsInt >= 55)
        {
            textStyle.normal.textColor = new Color(0.22f, 1f, 0.45f, 0.98f);
        }
        else if (currentFpsInt >= 30)
        {
            textStyle.normal.textColor = new Color(1f, 0.85f, 0.20f, 0.98f);
        }
        else
        {
            textStyle.normal.textColor = new Color(1f, 0.32f, 0.28f, 0.98f);
        }

        // 4. Vẽ bóng đổ màu đen tạo độ nổi khối (Drop shadow)
        Rect shadowRect = new Rect(badgeRect.x + 1f, badgeRect.y + 1f, badgeRect.width, badgeRect.height);
        GUI.Label(shadowRect, currentFpsText, shadowStyle);

        // 5. Vẽ chữ thông số chính
        GUI.Label(badgeRect, currentFpsText, textStyle);
    }
}
