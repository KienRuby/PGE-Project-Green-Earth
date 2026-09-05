using UnityEngine;

/// <summary>
/// Quản lý hiệu ứng hạt (Disintegration Stardust / Sparkles) dọc theo biên phân rã của UI Panel.
/// - Phát hạt chính xác tại đường quét (sweep edge) khi Panel đang tan biến.
/// - Zero GC per frame: Dùng EmitParams tái sử dụng trong vòng lặp.
/// - Tự động khởi tạo ParticleSystem chuẩn UI nếu chưa được gán sẵn.
/// </summary>
[DisallowMultipleComponent]
public class UIDissolveParticle : MonoBehaviour
{
    [Header("Particle System Reference")]
    [Tooltip("ParticleSystem phát hạt. Nếu để trống, script sẽ tự động tạo một ParticleSystem con chuẩn UI.")]
    [SerializeField] private ParticleSystem targetParticleSystem;

    [Header("Particle Visuals & Emission")]
    [Tooltip("Số lượng hạt phát mỗi bước cập nhật khi đang tan biến.")]
    [Range(1, 25)]
    [SerializeField] private int particlesPerBurst = 11;

    [Tooltip("Màu sắc hạt bụi phát sáng (HDR).")]
    [ColorUsage(true, true)]
    [SerializeField] private Color particleColor = new Color(1.2f, 1.2f, 1.2f, 1.0f);

    [Tooltip("Kích thước hạt nhỏ, dày như dải cát số trong video.")]
    [Range(2f, 30f)]
    [SerializeField] private float particleSize = 3.4f;

    [Tooltip("Thời gian tồn tại của từng hạt (giây).")]
    [Range(0.1f, 1.5f)]
    [SerializeField] private float particleLifetime = 0.24f;

    [Tooltip("Tốc độ bay tản ra ngoài của hạt.")]
    [Range(10f, 200f)]
    [SerializeField] private float disperseSpeed = 58f;

    private ParticleSystem.EmitParams emitParams;
    private bool isInitialized = false;

    public ParticleSystem TargetParticleSystem => targetParticleSystem;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    public void InitializeIfNeeded()
    {
        if (isInitialized) return;

        if (targetParticleSystem == null)
        {
            targetParticleSystem = GetComponentInChildren<ParticleSystem>();
        }

        if (targetParticleSystem == null)
        {
            CreateDefaultUIParticleSystem();
        }

        emitParams = new ParticleSystem.EmitParams();
        isInitialized = true;
    }

    private void CreateDefaultUIParticleSystem()
    {
        GameObject pObj = new GameObject("DissolveParticles", typeof(RectTransform));
        pObj.transform.SetParent(transform, false);

        targetParticleSystem = pObj.AddComponent<ParticleSystem>();
        var main = targetParticleSystem.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = particleLifetime;
        main.startSpeed = 0f;
        main.startSize = particleSize;
        main.startColor = particleColor;
        main.maxParticles = 500;

        var emission = targetParticleSystem.emission;
        emission.enabled = false;

        var shape = targetParticleSystem.shape;
        shape.enabled = false;

        var sizeOverLifetime = targetParticleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.7f, 0.8f),
            new Keyframe(1f, 0f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = targetParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(particleColor, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        ParticleSystemRenderer renderer = targetParticleSystem.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 3000; // Đảm bảo hạt hiển thị trên UI Canvas
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Mobile/Particles/Additive") ?? Shader.Find("UI/Default"));
        }
    }

    /// <summary>
    /// Phát hạt dọc theo đường biên quét (Dissolve sweep edge) dựa trên RectTransform của Panel.
    /// </summary>
    public void EmitAtDissolveEdge(float progress, int directionMode, RectTransform panelRect)
    {
        if (targetParticleSystem == null || panelRect == null) return;
        if (progress <= 0.02f || progress >= 0.98f) return;

        Vector3[] worldCorners = new Vector3[4];
        panelRect.GetWorldCorners(worldCorners);

        // 0: Bottom-Left, 1: Top-Left, 2: Top-Right, 3: Bottom-Right
        Vector3 bl = worldCorners[0];
        Vector3 tl = worldCorners[1];
        Vector3 tr = worldCorners[2];
        Vector3 br = worldCorners[3];

        for (int i = 0; i < particlesPerBurst; i++)
        {
            float randAlongEdge = Random.value;
            float jitter = (Random.value - 0.5f) * 0.08f;
            float sampleProgress = Mathf.Clamp01(progress + jitter);

            Vector3 spawnPos = Vector3.zero;
            Vector3 driftDir = Random.insideUnitCircle.normalized;

            switch (directionMode)
            {
                case 1: // Left -> Right
                    {
                        Vector3 bottomEdgePos = Vector3.Lerp(bl, br, sampleProgress);
                        Vector3 topEdgePos = Vector3.Lerp(tl, tr, sampleProgress);
                        spawnPos = Vector3.Lerp(bottomEdgePos, topEdgePos, randAlongEdge);
                        // Updraft airflow + slight right drift matching video
                        driftDir += (tr - tl).normalized * 0.4f + (tl - bl).normalized * 0.8f;
                    }
                    break;

                case 2: // Right -> Left
                    {
                        Vector3 bottomEdgePos = Vector3.Lerp(br, bl, sampleProgress);
                        Vector3 topEdgePos = Vector3.Lerp(tr, tl, sampleProgress);
                        spawnPos = Vector3.Lerp(bottomEdgePos, topEdgePos, randAlongEdge);
                        driftDir -= (tr - tl).normalized * 0.5f;
                    }
                    break;

                case 3: // Top -> Bottom
                    {
                        Vector3 leftEdgePos = Vector3.Lerp(tl, bl, sampleProgress);
                        Vector3 rightEdgePos = Vector3.Lerp(tr, br, sampleProgress);
                        spawnPos = Vector3.Lerp(leftEdgePos, rightEdgePos, randAlongEdge);
                        driftDir -= (tl - bl).normalized * 0.5f;
                    }
                    break;

                case 4: // Bottom -> Top
                    {
                        Vector3 leftEdgePos = Vector3.Lerp(bl, tl, sampleProgress);
                        Vector3 rightEdgePos = Vector3.Lerp(br, tr, sampleProgress);
                        spawnPos = Vector3.Lerp(leftEdgePos, rightEdgePos, randAlongEdge);
                        driftDir += (tl - bl).normalized * 0.5f;
                    }
                    break;

                case 5: // Center -> Outside
                    {
                        Vector3 center = (bl + tr) * 0.5f;
                        float maxRadius = Vector3.Distance(center, tr);
                        float currentRadius = maxRadius * sampleProgress;
                        float angle = randAlongEdge * Mathf.PI * 2f;
                        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * currentRadius;
                        spawnPos = center + offset;
                        driftDir += offset.normalized;
                    }
                    break;

                case 6: // Outside -> Center
                    {
                        Vector3 center = (bl + tr) * 0.5f;
                        float maxRadius = Vector3.Distance(center, tr);
                        float currentRadius = maxRadius * (1f - sampleProgress);
                        float angle = randAlongEdge * Mathf.PI * 2f;
                        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * currentRadius;
                        spawnPos = center + offset;
                        driftDir -= offset.normalized;
                    }
                    break;

                default: // 0: Random (Toàn bộ mặt phẳng panel)
                    {
                        Vector3 bottomP = Vector3.Lerp(bl, br, Random.value);
                        Vector3 topP = Vector3.Lerp(tl, tr, Random.value);
                        spawnPos = Vector3.Lerp(bottomP, topP, randAlongEdge);
                    }
                    break;
            }

            emitParams.position = spawnPos;
            emitParams.velocity = driftDir.normalized * disperseSpeed;
            emitParams.startSize = particleSize * (0.7f + Random.value * 0.6f);
            emitParams.startLifetime = particleLifetime * (0.8f + Random.value * 0.4f);

            // Tinh the hat stardust lap lanh
            float sparkle = Random.value;
            Color emitCol = (sparkle > 0.45f)
                ? Color.Lerp(particleColor, Color.white, (sparkle - 0.45f) * 1.8f)
                : particleColor;
            emitParams.startColor = emitCol;

            targetParticleSystem.Emit(emitParams, 1);
        }
    }

    public void ClearParticles()
    {
        if (targetParticleSystem != null)
        {
            targetParticleSystem.Clear();
        }
    }

    public void SetParticleColor(Color color)
    {
        particleColor = color;
    }
}
