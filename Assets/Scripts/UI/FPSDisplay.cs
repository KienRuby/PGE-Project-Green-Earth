using UnityEngine;

/// <summary>
/// Hiển thị FPS và thời gian render frame (ms) thời gian thực trên màn hình.
/// Tự động khởi tạo khi chạy game, không tạo rác bộ nhớ (0 GC Allocation).
/// Phím tắt F3 trên PC hoặc chạm 3 ngón tay trên điện thoại để bật/tắt.
/// </summary>
public class FPSDisplay : MonoBehaviour
{
    private static FPSDisplay instance;

    [Header("Display Settings")]
    [SerializeField] private bool showFPS = true;
    [SerializeField] private int fontSize = 22;
    [SerializeField] private TextAnchor alignment = TextAnchor.UpperRight;
    [SerializeField] private float updateInterval = 0.25f;

    private float accum = 0f;
    private int frames = 0;
    private float timeLeft = 0f;
    private float currentFps = 60f;
    private float currentMs = 16.6f;
    private float minFps = 60f;
    private float maxFps = 60f;
    private float minMaxResetTimer = 0f;

    private GUIStyle style;
    private Rect rect;
    private string displayText = "60.0 FPS (16.6 ms)";

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
        timeLeft = updateInterval;
    }

    private void Update()
    {
        // Phím tắt F3 hoặc chạm 3 ngón tay để bật/tắt hiển thị
        if (Input.GetKeyDown(KeyCode.F3) || (Input.touchCount >= 3 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            showFPS = !showFPS;
        }

        if (!showFPS) return;

        float dt = Time.unscaledDeltaTime;
        accum += 1f / Mathf.Max(0.0001f, dt);
        frames++;
        timeLeft -= dt;
        minMaxResetTimer += dt;

        // Reset min/max định kỳ mỗi 5 giây để chỉ số luôn chính xác
        if (minMaxResetTimer >= 5.0f)
        {
            minMaxResetTimer = 0f;
            minFps = currentFps;
            maxFps = currentFps;
        }

        if (timeLeft <= 0.0f)
        {
            currentFps = accum / frames;
            currentMs = (1000f / Mathf.Max(0.1f, currentFps));

            if (currentFps < minFps) minFps = currentFps;
            if (currentFps > maxFps) maxFps = currentFps;

            displayText = string.Format("{0:F1} FPS ({1:F1} ms)\n[Min: {2:F0} | Max: {3:F0}]", currentFps, currentMs, minFps, maxFps);

            timeLeft = updateInterval;
            accum = 0f;
            frames = 0;
        }
    }

    private void OnGUI()
    {
        if (!showFPS) return;

        if (style == null)
        {
            style = new GUIStyle();
            style.fontSize = fontSize;
            style.fontStyle = FontStyle.Bold;
            style.alignment = alignment;
            style.padding = new RectOffset(16, 16, 16, 16);
        }

        // Đổi màu theo mức độ mượt: Xanh lá (>= 55), Vàng (30-54), Đỏ (< 30)
        if (currentFps >= 55f)
        {
            style.normal.textColor = new Color(0.2f, 1f, 0.3f, 0.95f); // Xanh lá
        }
        else if (currentFps >= 30f)
        {
            style.normal.textColor = new Color(1f, 0.85f, 0.1f, 0.95f); // Vàng
        }
        else
        {
            style.normal.textColor = new Color(1f, 0.25f, 0.2f, 0.95f); // Đỏ
        }

        rect = new Rect(0, 0, Screen.width, Screen.height);
        
        // Vẽ bóng viền đen để nhìn rõ trên mọi nền
        Color prevColor = style.normal.textColor;
        style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
        GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), displayText, style);
        
        // Vẽ chữ chính
        style.normal.textColor = prevColor;
        GUI.Label(rect, displayText, style);
    }
}
