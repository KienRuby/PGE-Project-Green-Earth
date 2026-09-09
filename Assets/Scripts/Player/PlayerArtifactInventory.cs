using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý túi đồ Cổ Vật (Artifact) của người chơi trong trận đấu hiện tại (In-Run Inventory).
/// Lưu trữ danh sách các Artifact đã nhặt, tự động tính toán và áp dụng các hiệu ứng buff
/// lên PlayerHealth, PlayerAutoShooter, PlayerMovement và GunTurret.
/// </summary>
public class PlayerArtifactInventory : MonoBehaviour
{
    private static PlayerArtifactInventory runtimeInstance;
    private static bool isInitializing = false;

    public static PlayerArtifactInventory Instance
    {
        get
        {
            if (runtimeInstance == null && !isInitializing)
            {
                isInitializing = true;
                try
                {
                    runtimeInstance = FindObjectOfType<PlayerArtifactInventory>();
                    if (runtimeInstance == null)
                    {
                        PlayerHealth ph = FindObjectOfType<PlayerHealth>();
                        if (ph != null)
                        {
                            runtimeInstance = ph.GetComponent<PlayerArtifactInventory>();
                            if (runtimeInstance == null)
                            {
                                runtimeInstance = ph.gameObject.AddComponent<PlayerArtifactInventory>();
                            }
                        }
                    }
                }
                finally
                {
                    isInitializing = false;
                }
            }
            return runtimeInstance;
        }
        private set => runtimeInstance = value;
    }

    [Header("Equipped Artifacts (Current Run)")]
    [SerializeField] private List<ArtifactData> equippedArtifacts = new List<ArtifactData>();

    [Header("Target Components (Auto-assigned)")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerAutoShooter playerAutoShooter;
    [SerializeField] private PlayerMovement playerMovement;

    public IReadOnlyList<ArtifactData> EquippedArtifacts => equippedArtifacts;
    public int EquippedCount => equippedArtifacts != null ? equippedArtifacts.Count : 0;

    /// <summary>
    /// Sự kiện thông báo khi một Artifact mới được nhặt và trang bị thành công.
    /// </summary>
    public event Action<ArtifactData> OnArtifactEquipped;

    // Tổng hợp các chỉ số buff từ toàn bộ Artifact đã nhặt
    public float TotalHpPercentBonus { get; private set; }
    public float TotalRangedDefPercentBonus { get; private set; }
    public float TotalTurretAtkSpeedPercentBonus { get; private set; }
    public float TotalWeaponDamagePercentBonus { get; private set; }
    public float TotalMoveSpeedPercentBonus { get; private set; }
    public float TotalDamageReductionBonus { get; private set; }
    public float TotalCritRatePercentBonus { get; private set; }

    private void Awake()
    {
        if (runtimeInstance == null)
        {
            runtimeInstance = this;
        }

        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>() ?? GetComponentInParent<PlayerHealth>();
        if (playerAutoShooter == null) playerAutoShooter = GetComponent<PlayerAutoShooter>() ?? GetComponentInParent<PlayerAutoShooter>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>() ?? GetComponentInParent<PlayerMovement>();
    }

    private void OnDestroy()
    {
        if (runtimeInstance == this)
        {
            runtimeInstance = null;
        }

        // Reset các giá trị tĩnh toàn cục khi kết thúc trận
        GunTurret.GlobalTurretFireRateMultiplier = 1f;
    }

    /// <summary>
    /// Nhận và trang bị một Artifact mới vào túi đồ in-run.
    /// </summary>
    public bool EquipArtifact(ArtifactData artifact)
    {
        if (artifact == null) return false;

        if (equippedArtifacts == null)
        {
            equippedArtifacts = new List<ArtifactData>();
        }

        equippedArtifacts.Add(artifact);
        ApplyAllArtifactBuffs();

        OnArtifactEquipped?.Invoke(artifact);
        Debug.Log($"[PlayerArtifactInventory] ✅ Đã nhận Artifact: {artifact.artifactName} ({artifact.GetFormattedStatText()})");

        return true;
    }

    /// <summary>
    /// Quét lại toàn bộ danh sách Artifact đã nhặt để tính toán và cập nhật các chỉ số buff.
    /// </summary>
    public void ApplyAllArtifactBuffs()
    {
        TotalHpPercentBonus = 0f;
        TotalRangedDefPercentBonus = 0f;
        TotalTurretAtkSpeedPercentBonus = 0f;
        TotalWeaponDamagePercentBonus = 0f;
        TotalMoveSpeedPercentBonus = 0f;
        TotalDamageReductionBonus = 0f;
        TotalCritRatePercentBonus = 0f;

        if (equippedArtifacts != null)
        {
            foreach (var art in equippedArtifacts)
            {
                if (art == null) continue;

                switch (art.statType)
                {
                    case ArtifactStatType.MaxHealthPercent:
                        TotalHpPercentBonus += art.statValue;
                        break;
                    case ArtifactStatType.RangedDefensePercent:
                        TotalRangedDefPercentBonus += art.statValue;
                        break;
                    case ArtifactStatType.TurretAttackSpeedPercent:
                        TotalTurretAtkSpeedPercentBonus += art.statValue;
                        break;
                    case ArtifactStatType.AllWeaponsDamagePercent:
                        TotalWeaponDamagePercentBonus += art.statValue;
                        break;
                    case ArtifactStatType.MoveSpeedPercent:
                        TotalMoveSpeedPercentBonus += art.statValue;
                        break;
                    case ArtifactStatType.DamageReduction:
                        TotalDamageReductionBonus += art.statValue;
                        break;
                    case ArtifactStatType.CritRatePercent:
                        TotalCritRatePercentBonus += art.statValue;
                        break;
                }
            }
        }

        // 1. Áp dụng buff Máu tối đa (HP +X%) - Ví dụ: Spare Battery, Energy Butter
        if (playerHealth != null)
        {
            int baseHp = playerHealth.BaseMaxHealth;
            if (baseHp <= 0) baseHp = 100;

            int bonusHp = Mathf.RoundToInt(baseHp * (TotalHpPercentBonus / 100f));
            int newMaxHp = baseHp + bonusHp;

            if (newMaxHp != playerHealth.MaxHealth)
            {
                int hpDiff = newMaxHp - playerHealth.MaxHealth;
                playerHealth.SetMaxHealth(newMaxHp, false);
                if (hpDiff > 0)
                {
                    playerHealth.Heal(hpDiff);
                }
            }

            // 2. Áp dụng buff Kháng đánh xa (Ranged DEF +X%) - Ví dụ: Quantum Microchip
            playerHealth.RangedDefenseBonusPercent = TotalRangedDefPercentBonus;

            // 3. Áp dụng buff Giảm trừ sát thương trực tiếp (DEF +X) - Ví dụ: Modular Brick
            playerHealth.SetDamageReduction(Mathf.RoundToInt(TotalDamageReductionBonus));
        }

        // 3. Áp dụng buff Sát thương mọi vũ khí & Tỉ lệ chí mạng (All Weapons' ATK +X%, Crit Rate +X%) - Ví dụ: Kung Fu Data USB, Data Disc
        if (playerAutoShooter != null)
        {
            playerAutoShooter.ArtifactDamageMultiplier = 1f + (TotalWeaponDamagePercentBonus / 100f);
            playerAutoShooter.ArtifactCritBonus = TotalCritRatePercentBonus / 100f;
        }

        // 4. Áp dụng buff Tốc độ đánh trụ súng (Turret ATK Speed +X%) - Ví dụ: Strong Cooler
        GunTurret.GlobalTurretFireRateMultiplier = 1f + (TotalTurretAtkSpeedPercentBonus / 100f);

        // 5. Áp dụng buff Tốc độ chạy (Move Speed +X%)
        if (playerMovement != null && TotalMoveSpeedPercentBonus > 0f)
        {
            playerMovement.SetMoveSpeedBonus(TotalMoveSpeedPercentBonus * 0.05f);
        }
    }
}
