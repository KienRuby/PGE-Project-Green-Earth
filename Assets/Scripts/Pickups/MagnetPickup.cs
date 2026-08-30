using UnityEngine;

/// <summary>
/// Quản lý bán kính hút ngọc (Magnet Pickup Range) xung quanh Player.
/// Liên tục quét các GemPickup trong phạm vi magnetRadius và kích hoạt lực hút.
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
        var allGems = GemPickup.AllActiveGems;
        float radiusSqr = EffectiveMagnetRadius * EffectiveMagnetRadius;
        Vector3 playerPos = playerTransform.position;

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
    }

    public static void TriggerGlobalMagnet(Transform target)
    {
        var allGems = GemPickup.AllActiveGems;
        for (int i = 0; i < allGems.Count; i++)
        {
            if (allGems[i] != null)
            {
                allGems[i].TriggerMagnetAttraction(target);
            }
        }
    }
}
