using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bảng tỷ lệ rơi vật phẩm (Drop Table):
/// Xác định xác suất và số lượng EXP Gem, Chip xanh, Gem đỏ và Powerup khi tiêu diệt quái vật.
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
}
