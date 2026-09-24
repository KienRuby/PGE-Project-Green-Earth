using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý quái vật tấn công trong chế độ Tower Def:
/// - Xuất hiện ở phía trên khu vực tối (Arena).
/// - Di chuyển thẳng xuống phía cổng căn cứ (Gate).
/// - Khi chạm cổng, bắt đầu tấn công cổng định kỳ làm giảm HP của cổng.
/// - Nhận sát thương từ đạn pháo (Turret), rơi vàng khi bị tiêu diệt.
/// </summary>
public class TowerDefEnemy : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] private float maxHp = 60f;
    [SerializeField] private float currentHp = 60f;
    [SerializeField] private float moveSpeed = 120f; // pixel / giây trên Canvas
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackInterval = 1.0f;
    [SerializeField] private int goldReward = 12;

    [Header("Visual References")]
    [SerializeField] private Image enemyImage;
    [SerializeField] private Image hpBarFill;
    [SerializeField] private RectTransform hpBarRoot;

    private TowerDefEnemyState state = TowerDefEnemyState.Spawning;
    private TowerDefGate targetGate;
    private float nextAttackTime;
    private float gateYThreshold;
    private bool isTouchingWall = false;

    public bool IsDead => state == TowerDefEnemyState.Dead || currentHp <= 0f;
    public bool IsTouchingWall => isTouchingWall;
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;

    public event Action<TowerDefEnemy> OnEnemyDied;

    public void Setup(float hp, float speed, float damage, int reward, TowerDefGate gate, float gateStopY, Sprite sprite = null)
    {
        maxHp = hp;
        currentHp = maxHp;
        moveSpeed = speed;
        attackDamage = damage;
        goldReward = reward;
        targetGate = gate;
        gateYThreshold = gateStopY;
        state = TowerDefEnemyState.MovingDown;
        isTouchingWall = transform.localPosition.y <= gateYThreshold;
        if (isTouchingWall) state = TowerDefEnemyState.AttackingGate;

        if (enemyImage != null && sprite != null)
        {
            enemyImage.sprite = sprite;
        }

        UpdateHpBar();
    }

    public void SetTouchingWall(bool touching)
    {
        isTouchingWall = touching;
        if (touching && state == TowerDefEnemyState.MovingDown)
        {
            state = TowerDefEnemyState.AttackingGate;
        }
    }

    public void StopAttacking()
    {
        StopAllCoroutines();
        if (enemyImage != null)
        {
            enemyImage.color = Color.white;
        }
    }

    private void Update()
    {
        if (IsDead) return;
        if (TowerDefGameManager.Instance != null && TowerDefGameManager.Instance.IsGameOver) return;

        switch (state)
        {
            case TowerDefEnemyState.MovingDown:
                MoveTowardsGate();
                break;

            case TowerDefEnemyState.AttackingGate:
                AttackGate();
                break;
        }
    }

    private void MoveTowardsGate()
    {
        if (TowerDefGameManager.Instance != null && TowerDefGameManager.Instance.IsGameOver) return;

        transform.localPosition += Vector3.down * (moveSpeed * Time.deltaTime);

        // Kiểm tra xem đã tiếp cận ngưỡng tường thành chưa
        if (transform.localPosition.y <= gateYThreshold)
        {
            transform.localPosition = new Vector3(transform.localPosition.x, gateYThreshold, transform.localPosition.z);
            isTouchingWall = true;
            state = TowerDefEnemyState.AttackingGate;
            nextAttackTime = Time.time + 0.3f;
        }
    }

    private void AttackGate()
    {
        if (targetGate == null || targetGate.IsDestroyed) return;
        if (!isTouchingWall) return;
        if (TowerDefGameManager.Instance != null && TowerDefGameManager.Instance.IsGameOver) return;

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackInterval;
            targetGate.TakeDamage(attackDamage);
            StartCoroutine(PunchAttackVisual());
        }
    }

    private IEnumerator PunchAttackVisual()
    {
        Vector3 origin = transform.localPosition;
        Vector3 punch = origin + new Vector3(0f, -15f, 0f);

        float t = 0f;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(origin, punch, t / 0.1f);
            yield return null;
        }

        t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(punch, origin, t / 0.15f);
            yield return null;
        }

        transform.localPosition = origin;
    }

    public void TakeDamage(float dmg)
    {
        if (IsDead) return;
        // Yêu cầu: Quái phải chạm vào tường thành mới mất máu
        if (!isTouchingWall) return;

        currentHp = Mathf.Max(0f, currentHp - dmg);
        UpdateHpBar();
        StartCoroutine(FlashHit());

        if (currentHp <= 0f)
        {
            Die();
        }
    }

    private IEnumerator FlashHit()
    {
        if (enemyImage != null)
        {
            Color orig = enemyImage.color;
            enemyImage.color = new Color(1f, 0.4f, 0.4f, 1f);
            yield return new WaitForSeconds(0.08f);
            if (enemyImage != null) enemyImage.color = orig;
        }
    }

    private void UpdateHpBar()
    {
        if (hpBarFill != null)
        {
            float ratio = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
            hpBarFill.rectTransform.localScale = new Vector3(ratio, 1f, 1f);
        }
    }

    private void Die()
    {
        state = TowerDefEnemyState.Dead;
        isTouchingWall = false;
        if (TowerDefGameManager.Instance != null)
        {
            TowerDefGameManager.Instance.AddGold(goldReward);
            TowerDefGameManager.Instance.ShowFloatingText(transform.position, $"+{goldReward}G", Color.yellow);
        }

        OnEnemyDied?.Invoke(this);
        StartCoroutine(FadeAndDestroy());
    }

    private IEnumerator FadeAndDestroy()
    {
        float dur = 0.25f;
        float elapsed = 0f;
        Vector3 initialScale = transform.localScale;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / dur;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, p);
            yield return null;
        }

        Destroy(gameObject);
    }
}
