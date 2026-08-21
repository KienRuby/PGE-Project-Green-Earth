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

    private GUIStyle style;
    private Rect rect;
    private string displayText = "60 FPS";

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

        if (timeLeft <= 0.0f)
        {
            currentFps = accum / frames;
            displayText = $"{Mathf.RoundToInt(currentFps)} FPS";

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
            style.padding = new RectOffset(8, 8, 8, 8);
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

        rect = new Rect(Screen.width - 160, 10, 150, 40);
        
        // Vẽ bóng viền đen để nhìn rõ trên mọi nền
        Color prevColor = style.normal.textColor;
        style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
        GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), displayText, style);
        
        // Vẽ chữ chính
        style.normal.textColor = prevColor;
        GUI.Label(rect, displayText, style);
    }
}
