using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiệu ứng các tia sáng hướng tâm (Sunburst / Radial Light Rays) quay tròn phía sau thẻ Artifact.
/// Hoạt động độc lập bằng Unscaled Time (vẫn quay mượt mà khi game đang Pause Time.timeScale = 0).
/// Tự động sinh texture tia sáng nếu chưa gán Sprite chính thức.
/// </summary>
public class SunburstRayEffect : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Tốc độ quay của các tia sáng (độ/giây).")]
    [SerializeField] private float rotationSpeed = 25f;

    [Tooltip("Đảo ngược chiều quay.")]
    [SerializeField] private bool rotateClockwise = true;

    [Header("Visual Components")]
    [SerializeField] private Image rayImage;
    [SerializeField] private Sprite customRaySprite;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rayImage == null) rayImage = GetComponent<Image>();

        EnsureRayGraphic();
    }

    private void Update()
    {
        if (rectTransform == null) return;

        // Quay đều bằng Time.unscaledDeltaTime để quay được cả khi Pause (timeScale = 0)
        float dir = rotateClockwise ? -1f : 1f;
        rectTransform.Rotate(0f, 0f, dir * rotationSpeed * Time.unscaledDeltaTime);
    }

    /// <summary>
    /// Đảm bảo luôn có đồ họa tia sáng hình rẻ quạt mềm mại nếu chưa có file Sprite chính thức.
    /// </summary>
    public void EnsureRayGraphic()
    {
        if (rayImage == null) rayImage = GetComponent<Image>() ?? gameObject.AddComponent<Image>();

        if (customRaySprite == null && rayImage.sprite == null)
        {
            customRaySprite = Resources.Load<Sprite>("UI/sunburst_ray");
        }

        if (customRaySprite != null)
        {
            rayImage.sprite = customRaySprite;
            rayImage.color = Color.white;
            rayImage.preserveAspect = true;
            return;
        }

        if (rayImage.sprite != null)
        {
            rayImage.color = Color.white;
            rayImage.preserveAspect = true;
            return;
        }
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.5f;
            int numRays = 16;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y) - center;
                    float dist = pos.magnitude;
                    if (dist > radius)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float angle = Mathf.Atan2(pos.y, pos.x) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;

                    // Tính chu kỳ tia sáng rẻ quạt
                    float rayAngle = 360f / numRays;
                    float mod = angle % rayAngle;
                    bool isBright = mod < (rayAngle * 0.5f);

                    float alpha = isBright ? 0.28f : 0.04f;
                    // Làm mờ dần về phía tâm và viền ngoài
                    float radialFade = Mathf.Sin((dist / radius) * Mathf.PI);
                    alpha *= radialFade;

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            rayImage.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            rayImage.color = new Color32(200, 245, 255, 180);
        }
    }
}
