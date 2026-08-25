using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Particle UI cơ khí chạy bằng unscaled time. Có thể gắn sprite thật sau;
/// khi chưa có asset sẽ dùng glyph hình học làm placeholder.
/// </summary>
public class ChipsetLevelUpParticleField : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset fallbackFont;
    [SerializeField] private Sprite[] particleSprites;
    [SerializeField, Range(12, 80)] private int particleCount = 42;
    [SerializeField] private Vector2 lifetimeRange = new Vector2(0.9f, 2.1f);
    [SerializeField] private Vector2 sizeRange = new Vector2(22f, 72f);
    [SerializeField] private Vector2 fallSpeedRange = new Vector2(35f, 105f);

    private readonly List<ParticleState> particles = new List<ParticleState>();
    private RectTransform fieldRect;

    private static readonly string[] FallbackGlyphs = { "⚙", "◇", "○", "+", "✦", "#" };
    private static readonly Color32[] Colors =
    {
        new Color32(255, 196, 38, 255),
        new Color32(255, 137, 25, 255),
        new Color32(205, 233, 52, 255),
        new Color32(113, 177, 42, 255)
    };

    private sealed class ParticleState
    {
        public RectTransform Rect;
        public CanvasGroup Group;
        public float Age;
        public float Lifetime;
        public Vector2 Velocity;
        public float RotationSpeed;
        public float BaseScale;
        public float Phase;
    }

    private void Awake()
    {
        fieldRect = transform as RectTransform;
    }

    private void OnEnable()
    {
        Canvas.ForceUpdateCanvases();
        EnsurePool();
        for (int i = 0; i < particles.Count; i++) Respawn(particles[i], true);
    }

    private void Update()
    {
        float delta = Time.unscaledDeltaTime;
        if (delta <= 0f) return;

        for (int i = 0; i < particles.Count; i++)
        {
            ParticleState particle = particles[i];
            particle.Age += delta;
            if (particle.Age >= particle.Lifetime)
            {
                Respawn(particle, false);
                continue;
            }

            particle.Rect.anchoredPosition += particle.Velocity * delta;
            particle.Rect.Rotate(0f, 0f, particle.RotationSpeed * delta);

            float normalizedAge = particle.Age / particle.Lifetime;
            float fade = Mathf.Clamp01(Mathf.Min(normalizedAge * 5f, (1f - normalizedAge) * 4f));
            particle.Group.alpha = fade * 0.92f;
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 3.5f + particle.Phase) * 0.08f;
            particle.Rect.localScale = Vector3.one * particle.BaseScale * pulse;
        }
    }

    private void EnsurePool()
    {
        if (fieldRect == null) fieldRect = transform as RectTransform;
        if (particles.Count > 0) return;

        bool hasSprites = particleSprites != null && particleSprites.Length > 0;
        for (int i = 0; i < particleCount; i++)
        {
            GameObject go = new GameObject($"MechanicalParticle_{i:00}", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            if (hasSprites)
            {
                UnityEngine.UI.Image image = go.AddComponent<UnityEngine.UI.Image>();
                image.sprite = particleSprites[Random.Range(0, particleSprites.Length)];
                image.color = Colors[Random.Range(0, Colors.Length)];
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            else
            {
                TextMeshProUGUI glyph = go.AddComponent<TextMeshProUGUI>();
                if (fallbackFont != null) glyph.font = fallbackFont;
                glyph.text = FallbackGlyphs[Random.Range(0, FallbackGlyphs.Length)];
                glyph.color = Colors[Random.Range(0, Colors.Length)];
                glyph.alignment = TextAlignmentOptions.Center;
                glyph.fontStyle = FontStyles.Bold;
                glyph.raycastTarget = false;
            }

            particles.Add(new ParticleState
            {
                Rect = rect,
                Group = go.GetComponent<CanvasGroup>()
            });
        }
    }

    private void Respawn(ParticleState particle, bool fillWholeField)
    {
        Vector2 fieldSize = fieldRect != null && fieldRect.rect.width > 1f
            ? fieldRect.rect.size
            : new Vector2(1080f, 650f);

        float size = Random.Range(sizeRange.x, sizeRange.y);
        particle.Rect.sizeDelta = Vector2.one * size;
        particle.Rect.anchoredPosition = new Vector2(
            Random.Range(-fieldSize.x * 0.52f, fieldSize.x * 0.52f),
            fillWholeField
                ? Random.Range(-fieldSize.y * 0.48f, fieldSize.y * 0.48f)
                : fieldSize.y * 0.52f + size);
        particle.Rect.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));

        particle.Age = fillWholeField ? Random.Range(0f, lifetimeRange.y * 0.8f) : 0f;
        particle.Lifetime = Random.Range(lifetimeRange.x, lifetimeRange.y);
        float horizontal = Random.Range(8f, 48f) * Mathf.Sign(particle.Rect.anchoredPosition.x == 0f ? Random.value - 0.5f : particle.Rect.anchoredPosition.x);
        particle.Velocity = new Vector2(horizontal, -Random.Range(fallSpeedRange.x, fallSpeedRange.y));
        particle.RotationSpeed = Random.Range(-190f, 190f);
        particle.BaseScale = Random.Range(0.75f, 1.25f);
        particle.Phase = Random.Range(0f, Mathf.PI * 2f);
        particle.Group.alpha = 0f;
    }

    public void SetParticleAssets(TMP_FontAsset font, Sprite[] sprites)
    {
        fallbackFont = font;
        particleSprites = sprites;
    }

    public void SetParticleSprites(Sprite[] sprites)
    {
        particleSprites = sprites;
    }
}
