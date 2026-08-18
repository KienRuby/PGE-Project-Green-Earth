using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// AAA Game-Ready Death Dissolve VFX and Shader Sequence Controller:
/// 1. Tắt di chuyển, điều khiển và va chạm.
/// 2. Chờ Animator chạy trọn vẹn clip Die đến ĐÚNG FRAME CUỐI CÙNG (nhân vật nằm xuống đất).
/// 3. Khóa tư thế tại frame cuối và bừng sáng Supernova Shockwave rực lửa HDR.
/// 4. Phát nổ tỏa tròn 360 độ thành hàng nghìn ngôi sao 4 cánh màu vàng kim rực rỡ lấp lánh (Golden Stardust Flares) lung linh trong đúng 1 GIÂY.
/// 5. Tự động ánh xạ UV chính xác từng phần cơ thể (chân, tay, thân, súng) trên Sprite Atlas / Spritesheet và mở rộng Mesh Quad để hạt bay văng tự do không bị viền hộp cắt xén.
/// 6. Tiêu tan êm dịu vào sương mù ánh sáng (Alpha dissipation into glowing stardust mist).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerDeathController : MonoBehaviour
{
    public enum DeathVFXPreset
    {
        GoldenStarDisintegration, // Mặc định: Ngôi sao 4 cánh vàng rực rỡ lấp lánh lung linh 360 độ trong 1 giây
        ArcaneCosmicVoid,         // Ma thuật huyền bí tím vũ trụ (Arcane Purple), xoáy vortex lấp lánh
        CrimsonHellfire,          // Lửa địa ngục đỏ rực (Crimson Fire), tàn tro bốc lên
        CyanPlasmaCyber,          // Năng lượng Cyan Cyberpunk, hạt lục giác pixel số hóa
        DarkFantasyAsh,           // Tro tàn u tối (Dark Ash), rã từ trên xuống dưới
        Custom                    // Tự do chỉnh tay mọi thông số
    }

    public enum DissolveDirection
    {
        BottomToTop = 0,    // Dưới lên trên
        TopToBottom = 1,    // Trên xuống dưới
        CenterOutward = 2,  // Từ tâm tỏa tròn 360 độ ra xung quanh
        LeftToRight = 3,    // Trái sang phải
        UniformSimplex = 4  // Nhiễu Simplex đồng nhất mọi hướng
    }

    public enum ParticleShape
    {
        SharpFourPointedStar = 0, // Ngôi sao 4 cánh lấp lánh chói lọi (Star Flare with Cross Beams)
        HexagonalDigitalPixel = 1, // Lục giác / Pixel số hóa (Sci-Fi Cyber)
        AshFlake = 2,              // Mảnh vụn tàn tro u tối (Dark Fantasy)
        DiamondSpark = 3,          // Tinh thể kim cương phát sáng
        StardustCircle = 4         // Giọt bụi sao tròn mờ ảo
    }

    public enum ColorTheme
    {
        MoltenGold,   // Vàng kim rực rỡ & Hổ phách (Golden Yellow)
        ArcanePurple, // Tím ma thuật vũ trụ & Hồng neon
        CrimsonFire,  // Lửa đỏ rực & Cam than hồng
        CyanEnergy,   // Năng lượng Cyan & Xanh băng tuyết
        Custom        // Màu sắc tự chọn
    }

    [Header("=== AAA VFX PRESET SELECTOR ===")]
    [Tooltip("Chọn nhanh phong cách hiệu ứng tử trận mẫu hoặc Custom để tự tinh chỉnh.")]
    [SerializeField] private DeathVFXPreset vfxPreset = DeathVFXPreset.GoldenStarDisintegration;

    [Header("1. Color Theme & HDR Emission (Golden Yellow)")]
    [Tooltip("Bảng màu chủ đạo cho hiệu ứng.")]
    [SerializeField] private ColorTheme colorTheme = ColorTheme.MoltenGold;

    [Tooltip("Màu viền phát sáng ngoài cùng (Molten Gold Corona HDR).")]
    [SerializeField, ColorUsage(true, true)]
    private Color edgeColor = new Color(6.0f, 3.5f, 0.2f, 1.0f);

    [Tooltip("Màu lõi thiêu đốt siêu sáng ở đầu sóng tan biến (Searing Golden-White Core HDR).")]
    [SerializeField, ColorUsage(true, true)]
    private Color innerEdgeColor = new Color(10.0f, 9.0f, 2.5f, 1.0f);

    [Tooltip("Cường độ phát sáng HDR Bloom.")]
    [SerializeField, Range(0.5f, 20f)]
    private float edgeIntensity = 3.0f;

    [Tooltip("Cường độ chớp sáng Supernova Shockwave bùng nổ ban đầu.")]
    [SerializeField, Range(0f, 5f)]
    private float supernovaFlash = 2.5f;

    [Header("2. Dissolve Direction & Erosion")]
    [Tooltip("Hướng sóng phân rã tan biến của cơ thể (Mặc định CenterOutward tỏa tròn 360 độ).")]
    [SerializeField] private DissolveDirection dissolveDirection = DissolveDirection.CenterOutward;

    [Tooltip("Độ rộng của dải viền rực lửa nóng chảy (Erosion Edge Width).")]
    [SerializeField, Range(0.01f, 0.3f)] private float edgeWidth = 0.12f;

    [Tooltip("Tỷ lệ thu phóng nhiễu Simplex Noise.")]
    [SerializeField, Range(0.5f, 10f)] private float noiseScale = 3.0f;

    [Header("3. 360 Burst & Dazzling Golden Particles (1s Explosion)")]
    [Tooltip("Hình dạng của hàng nghìn hạt bắn ra (Mặc định: Ngôi sao 4 cánh lấp lánh).")]
    [SerializeField] private ParticleShape particleShape = ParticleShape.SharpFourPointedStar;

    [Tooltip("Mật độ hạt (Grid Count): Càng lớn hạt càng nhỏ mịn li ti (55-80 là chuẩn siêu mịn).")]
    [SerializeField, Range(15f, 100f)] private float particleGridSize = 60f;

    [Tooltip("Tốc độ bùng nổ văng ra 360 độ của các hạt.")]
    [SerializeField, Range(0.2f, 6f)] private float disperseSpeed = 1.8f;

    [Tooltip("Độ tỏa rộng theo bán kính 360 độ.")]
    [SerializeField, Range(0.5f, 3f)] private float radialBurstSpread = 1.4f;

    [Tooltip("Lực nâng chống trọng lực (Anti-Gravity) giúp hạt bay bổng lơ lửng lên trên.")]
    [SerializeField, Range(0f, 3f)] private float upwardDrift = 0.5f;

    [Tooltip("Lực xoáy bão hỗn loạn (Turbulent Swirl Vortex).")]
    [SerializeField, Range(0f, 3f)] private float swirlStrength = 1.0f;

    [Tooltip("Độ hỗn loạn quỹ đạo của từng hạt.")]
    [SerializeField, Range(0f, 3f)] private float disperseChaos = 1.3f;

    [Tooltip("Tốc độ teo nhỏ kích thước hạt theo thời gian.")]
    [SerializeField, Range(0f, 1f)] private float particleShrink = 0.82f;

    [Tooltip("Lực trọng lực kéo xuống (0 = không trọng lực hoàn toàn).")]
    [SerializeField, Range(0f, 2f)] private float gravity = 0.02f;

    [Tooltip("Tốc độ chớp sáng lấp lánh lung linh của các ngôi sao (Star Glitter Twinkle Speed).")]
    [SerializeField, Range(10f, 80f)] private float starSparkleSpeedValue = 45f;

    [Tooltip("Tán sắc lấp lánh hổ phách kim cương (Prismatic Iridescence).")]
    [SerializeField, Range(0f, 1f)] private float prismaticShimmer = 0.1f;

    [Tooltip("Độ rực của vầng hào quang bao quanh từng ngôi sao (Star Aura Halo Glow).")]
    [SerializeField, Range(0f, 2f)] private float haloGlowIntensity = 0.9f;

    [Header("4. Timing & Animation Settings (Wait For Last Frame Of Die)")]
    [Tooltip("Thời lượng nổ tung và phân tán lung linh toàn bộ hạt (Mặc định CHÍNH XÁC 1.0 GIÂY).")]
    [SerializeField, Min(0.1f)] private float dissolveDuration = 1.0f;

    [Tooltip("Đường cong phân rã: Bùng nổ cực nhanh ban đầu, lấp lánh rực rỡ và êm dịu dần.")]
    [SerializeField] private AnimationCurve dissolveCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 2.2f, 2.2f),
        new Keyframe(0.35f, 0.65f, 1.1f, 1.1f),
        new Keyframe(1f, 1f, 0.4f, 0.4f)
    );

    [Tooltip("Thời gian chờ/nghỉ sau khi animation Die kết thúc ở frame cuối cùng rồi MỚI BẮT ĐẦU phát nổ.")]
    [SerializeField, Min(0f)] private float delayBeforeDissolve = 0.05f;

    [Tooltip("Bật hiệu ứng viền phát sáng bừng lên (Ignite) trước khi nổ tung.")]
    [SerializeField] private bool enableEdgeIgnite = true;
    [SerializeField, Min(0.01f)] private float edgeIgniteDuration = 0.12f;

    [Tooltip("Animator của Player (tự tìm nếu để trống).")]
    [SerializeField] private Animator animator;
    [SerializeField] private string deathTriggerName = "Die";
    [SerializeField] private string deathStateName = "Die";
    [SerializeField] private bool useCustomAnimationDuration = false;
    [SerializeField, Min(0.05f)] private float customAnimationDuration = 1.0f;
    [SerializeField, Min(0.05f)] private float fallbackAnimationDuration = 0.8f;

    [Header("5. Material & Cleanup")]
    [SerializeField] private Material dissolveMaterialPreset;
    [SerializeField] private bool disableColliders = true;
    [SerializeField] private bool disablePhysics = true;
    [SerializeField] private Behaviour[] additionalBehavioursToDisable;
    [SerializeField] private bool destroyOnComplete = true;
    [SerializeField] private bool disableOnComplete = false;
    [SerializeField, Min(0f)] private float completionDelay = 0.05f;

    [Header("Events")]
    [SerializeField] private UnityEvent onDeathStarted;
    [SerializeField] private UnityEvent onEdgeGlowStarted;
    [SerializeField] private UnityEvent onDissolveStarted;
    [SerializeField] private UnityEvent onDeathComplete;

    public event Action OnDeathStarted;
    public event Action OnEdgeGlowStarted;
    public event Action OnDissolveStarted;
    public event Action OnDeathCompleted;

    public ColorTheme ActiveColorTheme => colorTheme;
    public DeathVFXPreset ActivePreset => vfxPreset;

    // Shader Property IDs
    private static readonly int DissolveAmountPropId = Shader.PropertyToID("_DissolveAmount");
    private static readonly int DissolveDirectionModePropId = Shader.PropertyToID("_DissolveDirectionMode");
    private static readonly int ParticleShapeModePropId = Shader.PropertyToID("_ParticleShapeMode");
    private static readonly int ParticleGridSizePropId = Shader.PropertyToID("_ParticleGridSize");
    private static readonly int DisperseSpeedPropId = Shader.PropertyToID("_DisperseSpeed");
    private static readonly int RadialBurstSpreadPropId = Shader.PropertyToID("_RadialBurstSpread");
    private static readonly int UpwardDriftPropId = Shader.PropertyToID("_UpwardDrift");
    private static readonly int SwirlStrengthPropId = Shader.PropertyToID("_SwirlStrength");
    private static readonly int DisperseChaosPropId = Shader.PropertyToID("_DisperseChaos");
    private static readonly int ParticleShrinkPropId = Shader.PropertyToID("_ParticleShrink");
    private static readonly int GravityPropId = Shader.PropertyToID("_Gravity");
    private static readonly int StarSparkleSpeedPropId = Shader.PropertyToID("_StarSparkleSpeed");
    private static readonly int PrismaticShimmerPropId = Shader.PropertyToID("_PrismaticShimmer");
    private static readonly int HaloGlowIntensityPropId = Shader.PropertyToID("_HaloGlowIntensity");
    private static readonly int SupernovaFlashPropId = Shader.PropertyToID("_SupernovaFlash");
    private static readonly int NoiseScalePropId = Shader.PropertyToID("_NoiseScale");
    private static readonly int EdgeWidthPropId = Shader.PropertyToID("_EdgeWidth");
    private static readonly int EdgeColorPropId = Shader.PropertyToID("_EdgeColor");
    private static readonly int InnerEdgeColorPropId = Shader.PropertyToID("_InnerEdgeColor");
    private static readonly int EdgeIntensityPropId = Shader.PropertyToID("_EdgeIntensity");
    private static readonly int SpriteUVRectPropId = Shader.PropertyToID("_SpriteUVRect");

    private bool isDeathSequenceActive;
    private PlayerHealth playerHealth;
    private SpriteRenderer[] cachedRenderers;
    private MaterialPropertyBlock propertyBlock;
    private Material runtimeDissolveMaterial;

    public bool IsDeathSequenceActive => isDeathSequenceActive;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = GetComponentInParent<PlayerHealth>();
        }

        propertyBlock = new MaterialPropertyBlock();
        CacheAllRenderers();
        ApplyPresetSettings();
    }

    private void OnValidate()
    {
        ApplyPresetSettings();
    }

    public void ApplyColorTheme()
    {
        switch (colorTheme)
        {
            case ColorTheme.MoltenGold:
                edgeColor = new Color(6.0f, 3.5f, 0.2f, 1.0f);
                innerEdgeColor = new Color(10.0f, 9.0f, 2.5f, 1.0f);
                break;
            case ColorTheme.ArcanePurple:
                edgeColor = new Color(4.5f, 0.5f, 6.5f, 1.0f);
                innerEdgeColor = new Color(8.5f, 4.0f, 10.0f, 1.0f);
                break;
            case ColorTheme.CrimsonFire:
                edgeColor = new Color(6.0f, 0.8f, 0.1f, 1.0f);
                innerEdgeColor = new Color(10.0f, 3.5f, 0.5f, 1.0f);
                break;
            case ColorTheme.CyanEnergy:
                edgeColor = new Color(0.2f, 4.5f, 6.0f, 1.0f);
                innerEdgeColor = new Color(4.0f, 8.5f, 10.0f, 1.0f);
                break;
            case ColorTheme.Custom:
                break;
        }
    }

    public void ApplyPresetSettings()
    {
        if (vfxPreset != DeathVFXPreset.Custom)
        {
            switch (vfxPreset)
            {
                case DeathVFXPreset.GoldenStarDisintegration:
                    colorTheme = ColorTheme.MoltenGold;
                    edgeColor = new Color(6.0f, 3.5f, 0.2f, 1.0f);
                    innerEdgeColor = new Color(10.0f, 9.0f, 2.5f, 1.0f);
                    dissolveDirection = DissolveDirection.CenterOutward;
                    particleShape = ParticleShape.SharpFourPointedStar;
                    particleGridSize = 60f;
                    disperseSpeed = 1.8f;
                    radialBurstSpread = 1.4f;
                    upwardDrift = 0.5f;
                    swirlStrength = 1.0f;
                    gravity = 0.02f;
                    starSparkleSpeedValue = 45f;
                    prismaticShimmer = 0.1f;
                    haloGlowIntensity = 0.9f;
                    supernovaFlash = 2.5f;
                    edgeIntensity = 3.0f;
                    dissolveDuration = 1.0f;
                    break;

                case DeathVFXPreset.ArcaneCosmicVoid:
                    colorTheme = ColorTheme.ArcanePurple;
                    dissolveDirection = DissolveDirection.CenterOutward;
                    particleShape = ParticleShape.SharpFourPointedStar;
                    particleGridSize = 65f;
                    disperseSpeed = 2.0f;
                    radialBurstSpread = 1.6f;
                    upwardDrift = 0.4f;
                    swirlStrength = 1.6f;
                    gravity = 0.0f;
                    starSparkleSpeedValue = 50f;
                    prismaticShimmer = 0.7f;
                    haloGlowIntensity = 1.0f;
                    supernovaFlash = 3.0f;
                    dissolveDuration = 1.0f;
                    break;

                case DeathVFXPreset.CrimsonHellfire:
                    colorTheme = ColorTheme.CrimsonFire;
                    dissolveDirection = DissolveDirection.BottomToTop;
                    particleShape = ParticleShape.AshFlake;
                    particleGridSize = 48f;
                    disperseSpeed = 1.5f;
                    radialBurstSpread = 1.2f;
                    upwardDrift = 1.2f;
                    swirlStrength = 0.8f;
                    gravity = -0.1f;
                    starSparkleSpeedValue = 35f;
                    prismaticShimmer = 0.2f;
                    haloGlowIntensity = 0.7f;
                    supernovaFlash = 2.0f;
                    dissolveDuration = 1.0f;
                    break;

                case DeathVFXPreset.CyanPlasmaCyber:
                    colorTheme = ColorTheme.CyanEnergy;
                    dissolveDirection = DissolveDirection.CenterOutward;
                    particleShape = ParticleShape.HexagonalDigitalPixel;
                    particleGridSize = 55f;
                    disperseSpeed = 2.2f;
                    radialBurstSpread = 1.8f;
                    upwardDrift = 0.2f;
                    swirlStrength = 0.4f;
                    gravity = 0.0f;
                    starSparkleSpeedValue = 60f;
                    prismaticShimmer = 0.8f;
                    haloGlowIntensity = 0.9f;
                    supernovaFlash = 3.5f;
                    dissolveDuration = 1.0f;
                    break;

                case DeathVFXPreset.DarkFantasyAsh:
                    colorTheme = ColorTheme.Custom;
                    edgeColor = new Color(2.2f, 1.6f, 1.0f, 1.0f);
                    innerEdgeColor = new Color(4.0f, 1.8f, 0.6f, 1.0f);
                    dissolveDirection = DissolveDirection.TopToBottom;
                    particleShape = ParticleShape.AshFlake;
                    particleGridSize = 42f;
                    disperseSpeed = 1.0f;
                    radialBurstSpread = 1.0f;
                    upwardDrift = 0.0f;
                    swirlStrength = 0.5f;
                    gravity = 0.5f;
                    starSparkleSpeedValue = 20f;
                    prismaticShimmer = 0.0f;
                    haloGlowIntensity = 0.4f;
                    supernovaFlash = 1.0f;
                    dissolveDuration = 1.0f;
                    break;
            }

            if (colorTheme != ColorTheme.Custom)
            {
                ApplyColorTheme();
            }
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath -= TriggerDeath;
            playerHealth.OnPlayerDeath += TriggerDeath;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath -= TriggerDeath;
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath -= TriggerDeath;
        }

        if (runtimeDissolveMaterial != null)
        {
            Destroy(runtimeDissolveMaterial);
            runtimeDissolveMaterial = null;
        }
    }

    public void CacheAllRenderers()
    {
        cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    [ContextMenu("Trigger Death Sequence")]
    public void TriggerDeath()
    {
        if (isDeathSequenceActive)
            return;

        isDeathSequenceActive = true;

        onDeathStarted?.Invoke();
        OnDeathStarted?.Invoke();

        DisablePlayerControlAndPhysics();
        StartCoroutine(DeathSequenceCoroutine());
    }

    private void DisablePlayerControlAndPhysics()
    {
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        PlayerAutoShooter shooter = GetComponent<PlayerAutoShooter>();
        if (shooter != null) shooter.enabled = false;

        PlayerAnimatorController animCtrl = GetComponent<PlayerAnimatorController>();
        if (animCtrl != null) animCtrl.enabled = false;

        if (disableColliders)
        {
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
            foreach (Collider2D col in colliders)
            {
                if (col != null) col.enabled = false;
            }
        }

        if (disablePhysics)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.simulated = false;
            }
        }

        if (additionalBehavioursToDisable != null)
        {
            foreach (Behaviour b in additionalBehavioursToDisable)
            {
                if (b != null) b.enabled = false;
            }
        }
    }

    private IEnumerator DeathSequenceCoroutine()
    {
        // =========================================================================
        // BƯỚC 1: ANIMATION DIE - CHỜ CHẠY ĐẾN ĐÚNG FRAME CUỐI CÙNG
        // =========================================================================
        float clipDuration = fallbackAnimationDuration;

        if (useCustomAnimationDuration)
        {
            clipDuration = customAnimationDuration;
        }
        else if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            foreach (AnimationClip clip in clips)
            {
                if (clip != null && string.Equals(clip.name, deathStateName, StringComparison.OrdinalIgnoreCase))
                {
                    clipDuration = clip.length;
                    break;
                }
            }
        }

        if (animator != null && animator.gameObject.activeInHierarchy)
        {
            animator.speed = 1.0f;

            if (HasParameter(animator, deathTriggerName, AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger(deathTriggerName);
            }
            else if (HasParameter(animator, deathStateName, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(deathStateName, true);
            }
            else
            {
                animator.Play(deathStateName, 0, 0f);
            }

            // Chờ 1 frame để Animator hoàn tất chuyển trạng thái sang State Die
            yield return null;

            // Chờ animation Die chạy trọn vẹn 100% đến frame cuối cùng
            float animTimer = 0f;
            while (animTimer < clipDuration)
            {
                animTimer += Time.deltaTime;
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName(deathStateName) && stateInfo.normalizedTime >= 1.0f)
                {
                    break;
                }
                yield return null;
            }

            // Đóng băng Animator ở frame cuối cùng (tư thế nằm xuống)
            animator.speed = 0f;
        }
        else
        {
            yield return new WaitForSeconds(clipDuration);
        }

        // =========================================================================
        // BƯỚC 2: PAUSE TRƯỚC KHI BÙNG NỔ (SAU KHI ĐÃ NẰM XUỐNG ĐẤT)
        // =========================================================================
        if (delayBeforeDissolve > 0f)
        {
            yield return new WaitForSeconds(delayBeforeDissolve);
        }

        // =========================================================================
        // BƯỚC 3: GÁN DISSOLVE MATERIAL & BẬT VIỀN SÁNG IGNITE
        // =========================================================================
        CacheAllRenderers();
        Material dissolveMat = GetOrCreateDissolveMaterial();

        if (dissolveMat != null && cachedRenderers != null)
        {
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                SpriteRenderer sr = cachedRenderers[i];
                if (sr != null)
                {
                    sr.sharedMaterial = dissolveMat;
                }
            }
        }

        onEdgeGlowStarted?.Invoke();
        OnEdgeGlowStarted?.Invoke();

        if (enableEdgeIgnite && edgeIgniteDuration > 0f)
        {
            float initialDissolveForGlow = 0.01f;
            float igniteElapsed = 0f;

            while (igniteElapsed < edgeIgniteDuration)
            {
                igniteElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(igniteElapsed / edgeIgniteDuration);
                float intensity = Mathf.Lerp(0f, edgeIntensity, t);

                SetDissolveAndEdgeIntensity(initialDissolveForGlow, intensity);
                yield return null;
            }
            SetDissolveAndEdgeIntensity(initialDissolveForGlow, edgeIntensity);
        }
        else
        {
            SetDissolveAndEdgeIntensity(0f, edgeIntensity);
        }

        onDissolveStarted?.Invoke();
        OnDissolveStarted?.Invoke();

        // =========================================================================
        // BƯỚC 4: BÙNG NỔ PHÂN RÃ HÀNG NGHÌN NGÔI SAO VÀNG LUNG LINH 360 ĐỘ TRONG 1 GIÂY
        // =========================================================================
        float dissolveElapsed = 0f;
        float safeDuration = Mathf.Max(0.1f, dissolveDuration);

        while (dissolveElapsed < safeDuration)
        {
            dissolveElapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(dissolveElapsed / safeDuration);
            float evaluatedValue = dissolveCurve != null ? dissolveCurve.Evaluate(normalized) : normalized;

            SetDissolveAndEdgeIntensity(evaluatedValue, edgeIntensity);
            yield return null;
        }

        SetDissolveAndEdgeIntensity(1.0f, edgeIntensity);

        if (completionDelay > 0f)
        {
            yield return new WaitForSeconds(completionDelay);
        }

        // =========================================================================
        // BƯỚC 5: HOÀN TẤT VÀ DỌN DẸP
        // =========================================================================
        Debug.Log("[PlayerDeathController] 1-Second Golden Star Particle Explosion VFX hoàn tất.");

        onDeathComplete?.Invoke();
        OnDeathCompleted?.Invoke();

        if (destroyOnComplete)
        {
            Destroy(gameObject);
        }
        else if (disableOnComplete)
        {
            gameObject.SetActive(false);
        }
    }

    private void SetDissolveAndEdgeIntensity(float dissolveAmount, float currentIntensity)
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
            return;

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            SpriteRenderer sr = cachedRenderers[i];
            if (sr != null)
            {
                sr.GetPropertyBlock(propertyBlock);

                Sprite sp = sr.sprite;
                if (sp != null && sp.texture != null)
                {
                    Rect tr = sp.textureRect;
                    float tw = sp.texture.width;
                    float th = sp.texture.height;
                    Vector4 uvRect = new Vector4(tr.xMin / tw, tr.yMin / th, tr.width / tw, tr.height / th);
                    propertyBlock.SetVector(SpriteUVRectPropId, uvRect);
                }
                else
                {
                    propertyBlock.SetVector(SpriteUVRectPropId, new Vector4(0f, 0f, 1f, 1f));
                }

                propertyBlock.SetFloat(DissolveAmountPropId, dissolveAmount);
                propertyBlock.SetFloat(DissolveDirectionModePropId, (float)dissolveDirection);
                propertyBlock.SetFloat(ParticleShapeModePropId, (float)particleShape);
                propertyBlock.SetFloat(ParticleGridSizePropId, particleGridSize);
                propertyBlock.SetFloat(DisperseSpeedPropId, disperseSpeed);
                propertyBlock.SetFloat(RadialBurstSpreadPropId, radialBurstSpread);
                propertyBlock.SetFloat(UpwardDriftPropId, upwardDrift);
                propertyBlock.SetFloat(SwirlStrengthPropId, swirlStrength);
                propertyBlock.SetFloat(DisperseChaosPropId, disperseChaos);
                propertyBlock.SetFloat(ParticleShrinkPropId, particleShrink);
                propertyBlock.SetFloat(GravityPropId, gravity);
                propertyBlock.SetFloat(StarSparkleSpeedPropId, starSparkleSpeedValue);
                propertyBlock.SetFloat(PrismaticShimmerPropId, prismaticShimmer);
                propertyBlock.SetFloat(HaloGlowIntensityPropId, haloGlowIntensity);
                propertyBlock.SetFloat(SupernovaFlashPropId, supernovaFlash);
                propertyBlock.SetFloat(NoiseScalePropId, noiseScale);
                propertyBlock.SetFloat(EdgeWidthPropId, edgeWidth);
                propertyBlock.SetColor(EdgeColorPropId, edgeColor);
                propertyBlock.SetColor(InnerEdgeColorPropId, innerEdgeColor);
                propertyBlock.SetFloat(EdgeIntensityPropId, currentIntensity);
                sr.SetPropertyBlock(propertyBlock);
            }
        }
    }

    private Material GetOrCreateDissolveMaterial()
    {
        if (runtimeDissolveMaterial != null) return runtimeDissolveMaterial;

        if (dissolveMaterialPreset != null)
        {
            runtimeDissolveMaterial = new Material(dissolveMaterialPreset) { name = "Runtime_PlayerAAAParticle_Mat" };
            return runtimeDissolveMaterial;
        }

        Shader shader = Shader.Find("Custom/2D/SpriteDissolve") ?? Shader.Find("Sprites/Default");
        if (shader != null)
        {
            runtimeDissolveMaterial = new Material(shader) { name = "Runtime_PlayerAAAParticle_Mat" };
            return runtimeDissolveMaterial;
        }

        return null;
    }

    private static bool HasParameter(Animator anim, string paramName, AnimatorControllerParameterType type)
    {
        if (anim == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.type == type && param.name == paramName) return true;
        }
        return false;
    }

    public void ResetDissolve()
    {
        StopAllCoroutines();
        isDeathSequenceActive = false;
        SetDissolveAndEdgeIntensity(0f, 1f);
    }
}
