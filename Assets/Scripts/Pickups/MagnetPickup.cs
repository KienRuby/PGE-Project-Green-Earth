using UnityEngine;

/// <summary>
/// Quản lý bán kính hút vật phẩm (Magnet Pickup Range) xung quanh Player.
/// Liên tục quét các GemPickup và HealthBoxPickup trong phạm vi magnetRadius và kích hoạt lực hút.
/// </summary>
public class MagnetPickup : MonoBehaviour
{
    [Header("Magnet Range Settings")]
    [Tooltip("Bán kính nhặt ngọc cơ bản xung quanh Player (Tầm gần bước chân: 0.45m).")]
    [SerializeField] private float baseMagnetRadius = 0.45f;

    [Tooltip("Hệ số cộng thêm vào bán kính hút ngọc (từ Drone/Buddy hoặc Lab stats).")]
    [SerializeField] private float bonusMagnetRadius = 0f;

    [Tooltip("Tần suất quét ngọc xung quanh (giây).")]
    [SerializeField] private float scanInterval = 0.2f;

    [Tooltip("Có cho phép lực hút nam châm tác dụng lên Hộp Máu (HealthBox) hay không. Mặc định false để người chơi tự bước qua nhặt khi cần.")]
    [SerializeField] private bool attractHealthBoxes = false;

    private float scanTimer;
    private Transform playerTransform;

    public float EffectiveMagnetRadius => baseMagnetRadius + bonusMagnetRadius;
    public bool AttractHealthBoxes
    {
        get => attractHealthBoxes;
        set => attractHealthBoxes = value;
    }

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

        // 2. Hút các HealthBoxPickup (Hộp máu nhỏ & lớn) - Chỉ khi được bật (mặc định false để người chơi tự bước qua nhặt)
        if (attractHealthBoxes)
        {
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
    }

    public static void TriggerGlobalMagnet(Transform target, bool includeHealthBoxes = false)
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

        // 2. Hút toàn bộ Hộp máu nếu được chỉ định
        if (includeHealthBoxes)
        {
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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;
        float radius = EffectiveMagnetRadius;

        // Vòng tròn màu Tím Pha Lê (Neon Violet) biểu diễn vùng nam châm hút ngọc
        Gizmos.color = new Color(0.8f, 0.3f, 1f, 0.95f);
        Gizmos.DrawWireSphere(pos, radius);

        UnityEditor.Handles.color = new Color(0.8f, 0.3f, 1f, 0.08f);
        UnityEditor.Handles.DrawSolidDisc(pos, Vector3.forward, radius);

        GUIStyle style = new GUIStyle();
        style.normal.textColor = new Color(0.9f, 0.5f, 1f);
        style.fontSize = 11;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        UnityEditor.Handles.Label((Vector2)pos + Vector2.up * (radius + 0.25f),
            $"[Vùng Nhặt Bước Chân: {radius:F2}m]", style);
    }
#endif
}
