using UnityEngine;

public enum CreepVariant
{
    Standard,
    Fast,
    Tank,
    Swarm
}

/// <summary>
/// Đại diện cho quái vật Creep thông thường (Melee / Swarm / Fast / Tank).
/// Mở rộng từ lớp cơ sở Enemy, hỗ trợ tự động cấu hình các thông số di chuyển,
/// tách đàn (separation) và scaling độ khó.
/// </summary>
public class Creep : Enemy
{
    [Header("Creep Specific Settings")]
    [Tooltip("Biến thể của Creep.")]
    [SerializeField] private CreepVariant variant = CreepVariant.Standard;

    [Tooltip("Hệ số tăng tốc độ riêng của biến thể.")]
    [SerializeField] private float variantSpeedMultiplier = 1.0f;

    [Tooltip("Hệ số tăng máu riêng của biến thể.")]
    [SerializeField] private float variantHealthMultiplier = 1.0f;

    public CreepVariant Variant => variant;

    protected override void Awake()
    {
        base.Awake();
        ApplyVariantPresets();
    }

    public void SetVariant(CreepVariant newVariant)
    {
        variant = newVariant;
        ApplyVariantPresets();
    }

    private void ApplyVariantPresets()
    {
        switch (variant)
        {
            case CreepVariant.Standard:
                variantSpeedMultiplier = 1.0f;
                variantHealthMultiplier = 1.0f;
                break;
            case CreepVariant.Fast:
                variantSpeedMultiplier = 1.45f;
                variantHealthMultiplier = 0.75f;
                break;
            case CreepVariant.Tank:
                variantSpeedMultiplier = 0.70f;
                variantHealthMultiplier = 2.50f;
                break;
            case CreepVariant.Swarm:
                variantSpeedMultiplier = 1.20f;
                variantHealthMultiplier = 0.50f;
                break;
        }
    }

    public override void Initialize(Transform target, float healthMultiplier = 1.0f, float damageMultiplier = 1.0f, float speedMultiplier = 1.0f, float expMultiplier = 1.0f)
    {
        ApplyVariantPresets();
        float effectiveHpMul = healthMultiplier * variantHealthMultiplier;
        float effectiveSpeedMul = speedMultiplier * variantSpeedMultiplier;

        base.Initialize(target, effectiveHpMul, damageMultiplier, effectiveSpeedMul, expMultiplier);
    }
}
