using UnityEngine;

public class GatlingSpinner : MonoBehaviour
{
    [Header("Gatling Animation Frames")]
    [Tooltip("Danh sách các frame Sprite quay nòng súng theo chu kỳ (Gatling_Spin_0 -> 3).")]
    [SerializeField] private Sprite[] spinFrames;

    [Tooltip("SpriteRenderer của khẩu súng Gatling (tự động lấy nếu để trống).")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Spin Settings")]
    [Tooltip("Tốc độ chuyển frame khi quay tối đa (frame/giây).")]
    [SerializeField] private float maxSpinFPS = 30f;

    [Tooltip("Tốc độ tăng tốc quay nòng súng khi bắt đầu bắn (Spool Up).")]
    [SerializeField] private float spoolUpSpeed = 50f;

    [Tooltip("Tốc độ giảm tốc quay nòng súng khi ngừng bắn (Spool Down).")]
    [SerializeField] private float spoolDownSpeed = 30f;

    private float currentSpinFPS = 0f;
    private float frameTimer = 0f;
    private int currentFrameIndex = 0;
    private bool isFiring = false;
    private Sprite defaultSprite;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            defaultSprite = spriteRenderer.sprite;
        }
    }

    /// <summary>
    /// Bật/tắt trạng thái bắn để nòng súng bắt đầu quay hoặc giảm tốc.
    /// </summary>
    public void SetFiring(bool firing)
    {
        isFiring = firing;
    }

    private void Update()
    {
        // Tăng tốc hoặc giảm tốc độ quay nòng (Spool up / Spool down)
        if (isFiring)
        {
            currentSpinFPS = Mathf.MoveTowards(currentSpinFPS, maxSpinFPS, spoolUpSpeed * Time.deltaTime);
        }
        else
        {
            currentSpinFPS = Mathf.MoveTowards(currentSpinFPS, 0f, spoolDownSpeed * Time.deltaTime);
        }

        // Nếu nòng súng đang quay
        if (currentSpinFPS > 0.5f && spinFrames != null && spinFrames.Length > 0)
        {
            frameTimer += Time.deltaTime * currentSpinFPS;
            while (frameTimer >= 1f)
            {
                frameTimer -= 1f;
                currentFrameIndex = (currentFrameIndex + 1) % spinFrames.Length;

                if (spriteRenderer != null && spinFrames[currentFrameIndex] != null)
                {
                    spriteRenderer.sprite = spinFrames[currentFrameIndex];
                }
            }
        }
        else if (!isFiring && currentSpinFPS <= 0.5f && defaultSprite != null)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = defaultSprite;
            }
        }
    }
}
