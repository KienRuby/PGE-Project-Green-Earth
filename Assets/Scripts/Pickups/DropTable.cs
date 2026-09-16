using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bảng tỷ lệ rơi vật phẩm (Drop Table):
/// Xác định xác suất và số lượng EXP Gem, Chip xanh, Gem đỏ, Powerup và Hộp Máu khi tiêu diệt quái vật.
/// </summary>
public static class DropTable
{
    [Serializable]
    public struct DropEntry
    {
        public GemType type;
        public int value;
        public float weight;
    }

    public const float SmallHealthBoxDropChance = 0.05f; // 5% xuất hiện Hộp máu nhỏ
    public const float LargeHealthBoxDropChance = 0.03f; // 3% xuất hiện Hộp máu lớn
    public const float SmallHealthBoxHealPercent = 0.10f; // Hồi 10% HP tối đa
    public const float LargeHealthBoxHealPercent = 0.20f; // Hồi 20% HP tối đa

    public static GemType DetermineExpGemType(int expAmount)
    {
        if (expAmount >= 100) return GemType.RedExp;
        if (expAmount >= 25) return GemType.BlueExp;
        return GemType.GreenExp;
    }

    public static void SpawnLootAt(Vector3 position, int expAmount, int dataChips, int redGems, float powerupChance = 0.05f)
    {
        // 1. Sinh EXP Gem
        if (expAmount > 0)
        {
            GemType gemType = DetermineExpGemType(expAmount);
            SpawnGem(gemType, expAmount, position + (Vector3)UnityEngine.Random.insideUnitCircle * 0.3f);
        }

        // 2. Sinh Data Chips
        if (dataChips > 0)
        {
            SpawnGem(GemType.DataChip, dataChips, position + (Vector3)UnityEngine.Random.insideUnitCircle * 0.3f);
        }

        // 3. Sinh Red Gems
        if (redGems > 0)
        {
            SpawnGem(GemType.RedGem, redGems, position + (Vector3)UnityEngine.Random.insideUnitCircle * 0.3f);
        }

        // 4. Cơ hội rơi Powerup hiếm (Magnet / HealthPack / Bomb)
        if (powerupChance > 0f && UnityEngine.Random.value <= powerupChance)
        {
            float roll = UnityEngine.Random.value;
            GemType powerup = roll < 0.4f ? GemType.Magnet : (roll < 0.75f ? GemType.HealthPack : GemType.Bomb);
            int powerupVal = powerup == GemType.HealthPack ? 25 : 1;
            SpawnGem(powerup, powerupVal, position + (Vector3)UnityEngine.Random.insideUnitCircle * 0.4f);
        }
    }

    private static void SpawnGem(GemType type, int value, Vector3 position)
    {
        GameObject gemObj = new GameObject($"Gem_{type}", typeof(GemPickup), typeof(CircleCollider2D));
        gemObj.transform.position = position;
        GemPickup gem = gemObj.GetComponent<GemPickup>();
        gem.Initialize(type, value, position);
    }

    /// <summary>
    /// Sinh một Hộp Máu (Health Box) trên sân đấu tại vị trí chỉ định.
    /// </summary>
    public static HealthBoxPickup SpawnHealthBox(Vector3 position, HealthBoxType boxType)
    {
        string name = boxType == HealthBoxType.Small ? "HealthBox_Small" : "HealthBox_Large";
        GameObject boxObj = new GameObject(name);
        boxObj.transform.position = position;

        CircleCollider2D col = boxObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = boxType == HealthBoxType.Small ? 0.35f : 0.45f;

        Rigidbody2D rb = boxObj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;

        HealthBoxPickup pickup = boxObj.AddComponent<HealthBoxPickup>();
        pickup.Initialize(boxType, position);
        return pickup;
    }

    /// <summary>
    /// Thử sinh ngẫu nhiên hộp máu khi tiêu diệt enemy:
    /// - 3% rơi Hộp máu lớn (hồi 20% max HP)
    /// - 5% rơi Hộp máu nhỏ (hồi 10% max HP)
    /// </summary>
    public static HealthBoxPickup TryDropHealthBox(Vector3 position, float smallChance = SmallHealthBoxDropChance, float largeChance = LargeHealthBoxDropChance)
    {
        float roll = UnityEngine.Random.value;
        if (largeChance > 0f && roll < largeChance)
        {
            return SpawnHealthBox(position, HealthBoxType.Large);
        }
        if (smallChance > 0f && roll < largeChance + smallChance)
        {
            return SpawnHealthBox(position, HealthBoxType.Small);
        }
        return null;
    }

    /// <summary>
    /// Sinh một Hộp mù Cổ vật (Artifact Box) tại vị trí chỉ định.
    /// </summary>
    public static GameObject SpawnArtifactBox(Vector3 position, ArtifactData specificArtifact = null)
    {
        GameObject boxObj = new GameObject("ArtifactBoxDrop");
        boxObj.transform.position = position;

        CircleCollider2D col = boxObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f;

        Rigidbody2D rb = boxObj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;

        ArtifactBoxPickup pickup = boxObj.AddComponent<ArtifactBoxPickup>();
        pickup.SetSpawnPosition(position);
        if (specificArtifact != null)
        {
            pickup.SetArtifact(specificArtifact);
        }
        return boxObj;
    }
}
