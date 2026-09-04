using System.Collections.Generic;
using UnityEngine;

public enum GemType
{
    GreenExp,    // Small EXP (10 EXP)
    BlueExp,     // Medium EXP (25 EXP)
    RedExp,      // Large EXP (100 EXP)
    DataChip,    // Blue Data Chip currency
    RedGem,      // Red Gem currency
    Magnet,      // Collect all gems on field
    HealthPack,  // Restore 25% HP
    Bomb         // Blast all on-screen enemies
}

/// <summary>
/// Thực thể vật phẩm rơi khi quái vật bị tiêu diệt (EXP Gem, Currency, Magnet, Powerup).
/// Hỗ trợ:
/// - Lực hút nam châm mượt mà bay về phía người chơi (Magnet Attraction).
/// - Tự động thu thập và cộng điểm khi chạm vào người chơi.
/// - Tương thích 100% với ObjectPool (IPoolable) không cấp phát bộ nhớ rác GC.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GemPickup : MonoBehaviour, IPoolable
{
    private static readonly List<GemPickup> ActiveGems = new List<GemPickup>();
    public static IReadOnlyList<GemPickup> AllActiveGems => ActiveGems;

    [Header("Gem Configuration")]
    [SerializeField] private GemType gemType = GemType.GreenExp;
    [SerializeField] private int value = 10;
    [SerializeField] private float magnetAttractionSpeed = 14f;
    [SerializeField] private float pickupRadius = 0.8f;
    [SerializeField] private float idleFloatSpeed = 3f;
    [SerializeField] private float idleFloatAmount = 0.1f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Transform playerTarget;
    private bool isBeingAttracted;
    private float currentSpeed;
    private Vector3 initialSpawnPosition;
    private float spawnTime;

    public GemType Type => gemType;
    public int Value => value;
    public bool IsBeingAttracted => isBeingAttracted;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnEnable()
    {
        if (!ActiveGems.Contains(this))
        {
            ActiveGems.Add(this);
        }
        spawnTime = Time.time;
        initialSpawnPosition = transform.position;
        currentSpeed = 0f;
    }

    private void OnDisable()
    {
        ActiveGems.Remove(this);
        isBeingAttracted = false;
        playerTarget = null;
    }

    private void Update()
    {
        if (isBeingAttracted && playerTarget != null)
        {
            currentSpeed = Mathf.Min(currentSpeed + magnetAttractionSpeed * 2.5f * Time.deltaTime, magnetAttractionSpeed * 2f);
            Vector3 dir = (playerTarget.position - transform.position).normalized;
            transform.position += dir * (currentSpeed * Time.deltaTime);

            float distSqr = (playerTarget.position - transform.position).sqrMagnitude;
            if (distSqr <= pickupRadius * pickupRadius)
            {
                Collect();
            }
        }
        else
        {
            // Idle floating animation
            float offset = Mathf.Sin((Time.time - spawnTime) * idleFloatSpeed) * idleFloatAmount;
            transform.position = initialSpawnPosition + new Vector3(0f, offset, 0f);
        }
    }

    public void Initialize(GemType type, int amount, Vector3 spawnPosition)
    {
        gemType = type;
        value = amount;
        initialSpawnPosition = spawnPosition;
        transform.position = spawnPosition;
        isBeingAttracted = false;
        playerTarget = null;
        currentSpeed = 0f;
    }

    public void TriggerMagnetAttraction(Transform target)
    {
        if (target == null) return;
        playerTarget = target;
        isBeingAttracted = true;
        currentSpeed = magnetAttractionSpeed * 0.5f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerMovement>() != null)
        {
            Collect();
        }
    }

    public void Collect()
    {
        switch (gemType)
        {
            case GemType.GreenExp:
            case GemType.BlueExp:
            case GemType.RedExp:
                if (PlayerLevelController.Instance != null)
                {
                    PlayerLevelController.Instance.AddEXP(value);
                }
                break;

            case GemType.DataChip:
                ChipManager.AddDataChips(value);
                break;

            case GemType.RedGem:
                ChipManager.AddRedGems(value);
                break;

            case GemType.Magnet:
                CollectAllGemsOnScreen();
                break;

            case GemType.HealthPack:
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    PlayerHealth hp = player.GetComponentInParent<PlayerHealth>();
                    if (hp != null) hp.Heal(value);
                }
                break;

            case GemType.Bomb:
                BlastAllEnemiesOnScreen();
                break;
        }

        Despawn();
    }

    private void CollectAllGemsOnScreen()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        for (int i = ActiveGems.Count - 1; i >= 0; i--)
        {
            if (ActiveGems[i] != null && ActiveGems[i] != this)
            {
                ActiveGems[i].TriggerMagnetAttraction(player.transform);
            }
        }
    }

    private void BlastAllEnemiesOnScreen()
    {
        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();
        foreach (var enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(200, true);
            }
        }

        if (ScreenShakeService.Instance != null)
        {
            ScreenShakeService.Shake(0.5f, 0.4f);
        }
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
        isBeingAttracted = false;
        playerTarget = null;
        currentSpeed = 0f;
    }

    public void OnReturnToPool()
    {
        isBeingAttracted = false;
        playerTarget = null;
    }
}
