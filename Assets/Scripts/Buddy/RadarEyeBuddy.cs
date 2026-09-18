using UnityEngine;

/// <summary>
/// Buddy 4: Radar Eye - Sprite: drone-antenna-eye (ID 3).
/// Quét rada khóa mục tiêu, chiếu tia laser quét điểm yếu gây sát thương liên tục
/// và cung cấp nội tại tăng Tỷ lệ Chí mạng (+CRIT Rate) cho người chơi.
/// </summary>
public class RadarEyeBuddy : BuddyCombatDrone
{
    [Header("Radar Specifics")]
    [Tooltip("Dây Laser quét mục tiêu.")]
    [SerializeField] private LineRenderer laserBeam;

    [Tooltip("Tỷ lệ chí mạng tăng thêm cho người chơi (0.05 = +5%).")]
    [SerializeField] private float bonusCritRate = 0.05f;

    [Tooltip("Thời gian tồn tại của tia laser mỗi lần bắn (giây).")]
    [SerializeField] private float beamDuration = 0.4f;

    [SerializeField] private Color radarColor = new Color(0.2f, 0.9f, 1f, 0.9f);

    private float beamTimer;
    public static EnemyHealth CurrentWeakpointTarget { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        baseDamage = 32;
        attackCooldown = 0.9f;

        if (laserBeam == null)
        {
            laserBeam = GetComponent<LineRenderer>();
            if (laserBeam == null)
            {
                laserBeam = gameObject.AddComponent<LineRenderer>();
                laserBeam.startWidth = 0.08f;
                laserBeam.endWidth = 0.04f;
                laserBeam.material = new Material(Shader.Find("Sprites/Default"));
                laserBeam.startColor = radarColor;
                laserBeam.endColor = new Color(radarColor.r, radarColor.g, radarColor.b, 0.2f);
                laserBeam.positionCount = 2;
                laserBeam.enabled = false;
            }
        }
    }

    protected override void ApplyLevelAndTierScaling()
    {
        base.ApplyLevelAndTierScaling();
        int tierLevel = (int)currentTier;

        // Unique (Tier 3): Scan Range +30%
        if (tierLevel >= 3)
        {
            targetDetectionRadius = 9.0f * 1.30f;
        }

        // Holo (Tier 5): Target Lock +30% (Beam duration +30%, Cooldown -30%)
        if (tierLevel >= 5)
        {
            beamDuration = 0.4f * 1.30f;
            attackCooldown = Mathf.Max(0.2f, attackCooldown * 0.70f);
        }
    }

    protected override void Update()
    {
        base.Update();

        if (beamTimer > 0f)
        {
            beamTimer -= Time.deltaTime;
            if (laserBeam != null)
            {
                if (currentTarget != null && currentTarget.gameObject.activeInHierarchy && !currentTarget.IsDead)
                {
                    laserBeam.SetPosition(0, FirePoint.position);
                    laserBeam.SetPosition(1, currentTarget.transform.position);
                }
                else
                {
                    laserBeam.enabled = false;
                    if (CurrentWeakpointTarget == currentTarget) CurrentWeakpointTarget = null;
                }
            }

            if (beamTimer <= 0f)
            {
                if (laserBeam != null) laserBeam.enabled = false;
                if (CurrentWeakpointTarget == currentTarget) CurrentWeakpointTarget = null;
            }
        }
    }

    protected override void ExecuteAttack(EnemyHealth target)
    {
        if (target == null || target.IsDead) return;

        // Kích hoạt tia laser quét mục tiêu
        if (laserBeam != null)
        {
            laserBeam.SetPosition(0, FirePoint.position);
            laserBeam.SetPosition(1, target.transform.position);
            laserBeam.enabled = true;
            beamTimer = beamDuration;
        }

        int tierLevel = (int)currentTier;
        if (tierLevel >= 4)
        {
            CurrentWeakpointTarget = target;
        }

        // Gây sát thương quét điểm yếu (có tỷ lệ chí mạng cao)
        bool isCrit = Random.value < (0.25f + bonusCritRate);
        float critMult = isCrit ? 1.5f : 1.0f;
        float weakpointMult = tierLevel >= 4 ? 1.30f : 1.0f;
        int finalDamage = Mathf.RoundToInt(baseDamage * critMult * weakpointMult);
        target.TakeDamage(finalDamage, isCrit);
    }

    private void OnDestroy()
    {
        if (CurrentWeakpointTarget != null)
        {
            CurrentWeakpointTarget = null;
        }
        if (laserBeam != null && laserBeam.material != null)
        {
            Destroy(laserBeam.material);
        }
    }
}
