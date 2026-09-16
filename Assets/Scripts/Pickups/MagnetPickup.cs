using UnityEngine;

/// <summary>
/// Quản lý bán kính hút vật phẩm (Magnet Pickup Range) xung quanh Player.
/// Liên tục quét các GemPickup và HealthBoxPickup trong phạm vi magnetRadius và kích hoạt lực hút.
/// </summary>
public class MagnetPickup : MonoBehaviour
{
    [Header("Magnet Range Settings")]
    [Tooltip("Bán kính hút ngọc cơ bản xung quanh Player.")]
    [SerializeField] private float baseMagnetRadius = 3.5f;

    [Tooltip("Hệ số cộng thêm vào bán kính hút ngọc (từ Drone/Buddy hoặc Lab stats).")]
    [SerializeField] private float bonusMagnetRadius = 0f;

    [Tooltip("Tần suất quét ngọc xung quanh (giây).")]
    [SerializeField] private float scanInterval = 0.2f;

    private float scanTimer;
    private Transform playerTransform;

    public float EffectiveMagnetRadius => baseMagnetRadius + bonusMagnetRadius;

    private void Awake()
    {
        playerTransform = transform;
    }

    public void SetBonusMagnetRadius(float bonus)
    {
        bonusMagnetRadius = Mathf.Max(0f, bonus);
    }

    private void Update()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            scanTimer = scanInterval;
            AttractNearbyGems();
        }
    }

    public void AttractNearbyGems()
    {
        float radiusSqr = EffectiveMagnetRadius * EffectiveMagnetRadius;
        Vector3 playerPos = playerTransform.position;

        // 1. Hút các GemPickup (EXP, Currency, Powerup)
        var allGems = GemPickup.AllActiveGems;
        for (int i = 0; i < allGems.Count; i++)
        {
            GemPickup gem = allGems[i];
            if (gem != null && !gem.IsBeingAttracted)
            {
                float distSqr = (gem.transform.position - playerPos).sqrMagnitude;
                if (distSqr <= radiusSqr)
                {
                    gem.TriggerMagnetAttraction(playerTransform);
                }
            }
        }

        // 2. Hút các HealthBoxPickup (Hộp máu nhỏ & lớn)
        var allBoxes = HealthBoxPickup.ActiveBoxes;
        for (int i = 0; i < allBoxes.Count; i++)
        {
            HealthBoxPickup box = allBoxes[i];
            if (box != null && !box.IsBeingAttracted && !box.IsCollected)
            {
                float distSqr = (box.transform.position - playerPos).sqrMagnitude;
                if (distSqr <= radiusSqr)
                {
                    box.TriggerMagnetAttraction(playerTransform);
                }
            }
        }
    }

    public static void TriggerGlobalMagnet(Transform target)
    {
        // 1. Hút toàn bộ Gem
        var allGems = GemPickup.AllActiveGems;
        for (int i = 0; i < allGems.Count; i++)
        {
            if (allGems[i] != null)
            {
                allGems[i].TriggerMagnetAttraction(target);
            }
        }

        // 2. Hút toàn bộ Hộp máu
        var allBoxes = HealthBoxPickup.ActiveBoxes;
        for (int i = 0; i < allBoxes.Count; i++)
        {
            if (allBoxes[i] != null && !allBoxes[i].IsCollected)
            {
                allBoxes[i].TriggerMagnetAttraction(target);
            }
        }
    }
}
