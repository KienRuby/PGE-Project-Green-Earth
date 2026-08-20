using System;
using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable, IPoolable
{
    [Header("Health")]
    [Tooltip("Lượng máu tối đa của quái vật.")]
    [SerializeField] private int maxHealth = 50;

    [Header("Reward")]
    [Tooltip("Lượng điểm kinh nghiệm (EXP) thưởng cho người chơi khi tiêu diệt quái này.")]
    [SerializeField] private int expReward = 10;

    [Header("Death & Animation")]
    [Tooltip("Tên Trigger kích hoạt animation Die trong Animator.")]
    [SerializeField] private string deathAnimationTrigger = "Die";

    [Tooltip("Tên State animation Die trong Animator.")]
    [SerializeField] private string deathAnimationState = "Die";

    [Tooltip("Thời gian phát animation Die tối thiểu dự phòng nếu không tìm thấy clip (giây).")]
    [SerializeField] private float fallbackDeathDuration = 0.5f;

    [Tooltip("Thời gian trễ cộng thêm trước khi quái vật bị thu hồi về Pool sau khi animation kết thúc (giây).")]
    [SerializeField] private float destroyDelay = 0f;

    private int baseMaxHealth;
    private int baseExpReward;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public int BaseMaxHealth => baseMaxHealth > 0 ? baseMaxHealth : maxHealth;
    public int ExpReward => expReward;
    public int BaseExpReward => baseExpReward > 0 ? baseExpReward : expReward;
    public bool IsDead { get; private set; }

    public event Action<int, int> OnHealthChanged;
    public event Action OnEnemyDeath;
    public event Action<EnemyHealth> OnDeath;

    private Collider2D[] colliders;
    private Animator animator;
    private EnemyMovement enemyMovement;
    private BossMovement bossMovement;
    private Rigidbody2D rb;

    private void Awake()
    {
        baseMaxHealth = maxHealth;
        baseExpReward = expReward;
        colliders = GetComponentsInChildren<Collider2D>(true);
        animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        enemyMovement = GetComponent<EnemyMovement>();
        bossMovement = GetComponent<BossMovement>();
        rb = GetComponent<Rigidbody2D>();
        CurrentHealth = maxHealth;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void SetMaxHealth(int newMaxHealth, bool resetCurrentHealth = true)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);
        if (resetCurrentHealth)
        {
            CurrentHealth = maxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }

    public void SetExpReward(int newExpReward)
    {
        expReward = Mathf.Max(0, newExpReward);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead)
            return;

        if (damage <= 0)
            return;

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        // 1. Vô hiệu hóa toàn bộ collider để không nhận thêm sát thương hay va chạm người chơi
        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) colliders[i].enabled = false;
            }
        }

        // 2. Dừng chuyển động, khóa vật lý để không bị quán tính hay lực đẩy xê dịch
        if (enemyMovement != null) enemyMovement.enabled = false;
        if (bossMovement != null) bossMovement.enabled = false;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false; // Ngắt hoàn toàn khỏi hệ thống vật lý để đứng yên 100%
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        // 3. Cấp kinh nghiệm cho Player
        if (PlayerLevelController.Instance != null && expReward > 0)
        {
            PlayerLevelController.Instance.AddEXP(expReward);
        }

        // 4. Phát sự kiện để Spawner ghi nhận tiêu diệt
        OnEnemyDeath?.Invoke();
        OnDeath?.Invoke(this);

        // 5. Khóa chặt vị trí và chạy animation Die trọn vẹn rồi mới thu hồi / destroy
        StartCoroutine(PlayDeathAnimationAndDespawn(transform.position, transform.rotation, transform.localScale));
    }

    private IEnumerator PlayDeathAnimationAndDespawn(Vector3 lockedPos, Quaternion lockedRot, Vector3 lockedScale)
    {
        float animDuration = fallbackDeathDuration;

        if (animator == null)
        {
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        }

        if (animator != null && animator.gameObject.activeInHierarchy)
        {
            animator.applyRootMotion = false;

            if (animator.runtimeAnimatorController != null)
            {
                AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
                foreach (AnimationClip clip in clips)
                {
                    if (clip != null && (string.Equals(clip.name, deathAnimationState, StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(clip.name, "DieBig", StringComparison.OrdinalIgnoreCase)))
                    {
                        animDuration = clip.length;
                        break;
                    }
                }
            }

            animator.speed = 1f;

            if (HasParameter(animator, deathAnimationTrigger, AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger(deathAnimationTrigger);
            }
            else
            {
                animator.Play(deathAnimationState, 0, 0f);
            }
        }

        float totalWait = Mathf.Max(0.1f, animDuration + destroyDelay);
        float elapsed = 0f;

        // Giữ cố định 100% tọa độ tại chỗ trong từng frame cho đến khi animation kết thúc
        while (elapsed < totalWait)
        {
            transform.position = lockedPos;
            transform.rotation = lockedRot;
            transform.localScale = lockedScale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = lockedPos;
        Despawn();
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

    public void Despawn()
    {
        PoolMember member = GetComponent<PoolMember>();
        if (member != null && member.Pool != null)
        {
            member.ReturnToPool();
        }
        else if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnSpawnFromPool()
    {
        IsDead = false;
        CurrentHealth = maxHealth;

        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) colliders[i].enabled = true;
            }
        }

        if (rb != null)
        {
            rb.simulated = true;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (enemyMovement != null) enemyMovement.enabled = true;
        if (bossMovement != null) bossMovement.enabled = true;

        if (animator != null && animator.gameObject.activeInHierarchy)
        {
            animator.applyRootMotion = false;
            animator.Rebind();
            animator.Update(0f);
        }

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void OnReturnToPool()
    {
        IsDead = true;
        StopAllCoroutines();
        OnDeath = null;
        OnEnemyDeath = null;
        OnHealthChanged = null;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        OnDeath = null;
        OnEnemyDeath = null;
        OnHealthChanged = null;
    }
}