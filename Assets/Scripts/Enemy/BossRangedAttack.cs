using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossRangedAttack : MonoBehaviour, IPoolable
{
    public enum ShotPattern
    {
        AimedBurst,
        Fan,
        Radial
    }

    public enum TargetRangeState
    {
        NoTarget,
        TooClose,
        InRange,
        TooFar
    }

    [Serializable]
    public class ShootSkill
    {
        [Tooltip("Tên kỹ năng hiển thị trong Inspector.")]
        public string skillName = "Bắn thẳng";

        [Tooltip("Kiểu phân bố hướng đạn.")]
        public ShotPattern pattern = ShotPattern.AimedBurst;

        [Tooltip("Số viên trong một loạt. Với bắn thẳng, các viên được bắn nối tiếp.")]
        [Min(1)] public int projectileCount = 3;

        [Tooltip("Góc mở của hình quạt. Không dùng cho bắn thẳng và vòng tròn.")]
        [Range(0f, 360f)] public float spreadAngle = 50f;

        [Tooltip("Khoảng thời gian giữa từng viên của loạt bắn thẳng.")]
        [Min(0f)] public float shotInterval = 0.18f;

        [Tooltip("Thời gian chờ trước khi Boss có thể dùng kỹ năng tiếp theo.")]
        [Min(0.1f)] public float cooldown = 2.5f;
    }

    [Header("Mục tiêu và phạm vi")]
    [Tooltip("Player mục tiêu. Tự tìm bằng Tag Player nếu để trống.")]
    [SerializeField] private Transform target;

    [Tooltip("Boss chỉ bắt đầu kỹ năng khi Player nằm trong bán kính này.")]
    [Min(0.1f)] [SerializeField] private float attackRange = 11f;

    [Tooltip("Nếu Player gần hơn khoảng cách này, Boss sẽ lùi ra xa và tạm ngừng bắn.")]
    [Min(0f)] [SerializeField] private float minimumDistance = 4.5f;

    [Tooltip("Khoảng cách sinh đạn tính từ tâm Boss để đạn không chạm collider của Boss.")]
    [Min(0f)] [SerializeField] private float projectileSpawnDistance = 0.8f;

    [Header("Đạn")]
    [Tooltip("Prefab đạn riêng của Boss, phải có EnemyProjectile.")]
    [SerializeField] private EnemyProjectile projectilePrefab;

    [Tooltip("Sát thương của mỗi viên đạn.")]
    [Min(1)] [SerializeField] private int projectileDamage = 15;

    [Tooltip("Tốc độ bay của đạn.")]
    [Min(0.1f)] [SerializeField] private float projectileSpeed = 7f;

    [Header("Danh sách kỹ năng bắn")]
    [Tooltip("Boss chọn ngẫu nhiên một kỹ năng mỗi lần tấn công và hạn chế lặp liên tiếp.")]
    [SerializeField] private List<ShootSkill> skills = new List<ShootSkill>();

    [Tooltip("Thời gian chờ sau khi Boss xuất hiện trước phát bắn đầu tiên.")]
    [Min(0f)] [SerializeField] private float initialDelay = 1.5f;

    private EnemyHealth health;
    private PlayerHealth targetHealth;
    private Coroutine attackRoutine;
    private float cooldownTimer;
    private float nextTargetSearchTime;
    private int lastSkillIndex = -1;
    private int baseProjectileDamage;

    public float AttackRange => attackRange;
    public float MinimumDistance => minimumDistance;
    public int SkillCount => skills != null ? skills.Count : 0;
    public EnemyProjectile ProjectilePrefab => projectilePrefab;
    public int BaseProjectileDamage => baseProjectileDamage > 0 ? baseProjectileDamage : projectileDamage;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        baseProjectileDamage = projectileDamage;
        EnsureDefaultSkills();
    }

    private void OnEnable()
    {
        cooldownTimer = initialDelay;
    }

    private void Update()
    {
        if (health != null && health.IsDead) return;

        if (!HasValidTarget())
        {
            FindPlayer();
            return;
        }

        cooldownTimer -= Time.deltaTime;
        if (attackRoutine != null || cooldownTimer > 0f || GetTargetRangeState() != TargetRangeState.InRange) return;

        int skillIndex = ChooseSkillIndex();
        if (skillIndex >= 0)
        {
            attackRoutine = StartCoroutine(ExecuteSkill(skills[skillIndex], skillIndex));
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        targetHealth = target != null ? target.GetComponentInParent<PlayerHealth>() : null;
    }

    public void SetProjectileDamage(int damageAmount)
    {
        projectileDamage = Mathf.Max(1, damageAmount);
    }

    public bool IsTargetInRange()
    {
        return GetTargetRangeState() == TargetRangeState.InRange;
    }

    public TargetRangeState GetTargetRangeState()
    {
        if (!HasValidTarget()) return TargetRangeState.NoTarget;

        float distanceSquared = ((Vector2)target.position - (Vector2)transform.position).sqrMagnitude;
        float clampedMinimum = Mathf.Min(minimumDistance, attackRange);
        if (distanceSquared < clampedMinimum * clampedMinimum) return TargetRangeState.TooClose;
        if (distanceSquared <= attackRange * attackRange) return TargetRangeState.InRange;
        return TargetRangeState.TooFar;
    }

    public static Vector2[] CalculateFanDirections(Vector2 centerDirection, int projectileCount, float spreadAngle)
    {
        int count = Mathf.Max(1, projectileCount);
        Vector2 forward = centerDirection.sqrMagnitude > 0f ? centerDirection.normalized : Vector2.right;
        Vector2[] directions = new Vector2[count];

        if (count == 1)
        {
            directions[0] = forward;
            return directions;
        }

        float startAngle = -spreadAngle * 0.5f;
        float step = spreadAngle / (count - 1);
        for (int i = 0; i < count; i++)
        {
            directions[i] = Rotate(forward, startAngle + step * i);
        }
        return directions;
    }

    public static Vector2[] CalculateRadialDirections(Vector2 startDirection, int projectileCount)
    {
        int count = Mathf.Max(1, projectileCount);
        Vector2 forward = startDirection.sqrMagnitude > 0f ? startDirection.normalized : Vector2.right;
        Vector2[] directions = new Vector2[count];
        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            directions[i] = Rotate(forward, step * i);
        }
        return directions;
    }

    private IEnumerator ExecuteSkill(ShootSkill skill, int skillIndex)
    {
        if (skill.pattern == ShotPattern.AimedBurst)
        {
            int count = Mathf.Max(1, skill.projectileCount);
            for (int i = 0; i < count; i++)
            {
                if (!HasValidTarget() || (health != null && health.IsDead)) break;
                SpawnProjectile(((Vector2)target.position - (Vector2)transform.position).normalized);
                if (i < count - 1 && skill.shotInterval > 0f)
                {
                    yield return PoolManager.GetWaitForSeconds(skill.shotInterval);
                }
            }
        }
        else
        {
            Vector2 aimDirection = HasValidTarget()
                ? ((Vector2)target.position - (Vector2)transform.position).normalized
                : Vector2.right;
            Vector2[] directions = skill.pattern == ShotPattern.Fan
                ? CalculateFanDirections(aimDirection, skill.projectileCount, skill.spreadAngle)
                : CalculateRadialDirections(aimDirection, skill.projectileCount);

            for (int i = 0; i < directions.Length; i++)
            {
                SpawnProjectile(directions[i]);
            }
        }

        lastSkillIndex = skillIndex;
        cooldownTimer = Mathf.Max(0.1f, skill.cooldown);
        attackRoutine = null;
    }

    private void SpawnProjectile(Vector2 direction)
    {
        if (projectilePrefab == null || direction.sqrMagnitude <= 0f) return;

        Vector2 normalizedDirection = direction.normalized;
        Vector3 spawnPosition = transform.position + (Vector3)(normalizedDirection * projectileSpawnDistance);
        EnemyProjectile projectile = PoolManager.Instance != null
            ? PoolManager.Instance.Spawn(projectilePrefab, spawnPosition, Quaternion.identity)
            : Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

        if (projectile != null)
        {
            projectile.Setup(normalizedDirection, projectileDamage, projectileSpeed, attackRange);
        }
    }

    private int ChooseSkillIndex()
    {
        if (skills == null || skills.Count == 0) return -1;
        if (skills.Count == 1) return 0;

        int index = UnityEngine.Random.Range(0, skills.Count - 1);
        if (index >= lastSkillIndex) index++;
        return index;
    }

    private bool HasValidTarget()
    {
        return target != null && target.gameObject.activeInHierarchy && (targetHealth == null || !targetHealth.IsDead);
    }

    private void FindPlayer()
    {
        if (Time.time < nextTargetSearchTime) return;
        nextTargetSearchTime = Time.time + 1f;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        SetTarget(playerObject != null ? playerObject.transform : null);
    }

    private void EnsureDefaultSkills()
    {
        if (skills != null && skills.Count > 0) return;

        skills = new List<ShootSkill>
        {
            new ShootSkill
            {
                skillName = "Liên xạ nhắm Player",
                pattern = ShotPattern.AimedBurst,
                projectileCount = 3,
                shotInterval = 0.18f,
                cooldown = 2.2f
            },
            new ShootSkill
            {
                skillName = "Đạn hình quạt",
                pattern = ShotPattern.Fan,
                projectileCount = 5,
                spreadAngle = 55f,
                cooldown = 3.2f
            },
            new ShootSkill
            {
                skillName = "Đạn vòng tròn",
                pattern = ShotPattern.Radial,
                projectileCount = 12,
                cooldown = 4.5f
            }
        };
    }

    private static Vector2 Rotate(Vector2 direction, float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos).normalized;
    }

    private void StopAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
    }

    public void OnSpawnFromPool()
    {
        StopAttack();
        cooldownTimer = initialDelay;
        lastSkillIndex = -1;
        target = null;
        targetHealth = null;
        nextTargetSearchTime = 0f;
    }

    public void OnReturnToPool()
    {
        StopAttack();
        target = null;
        targetHealth = null;
    }

    private void OnDisable()
    {
        StopAttack();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Min(minimumDistance, attackRange));
    }
}
