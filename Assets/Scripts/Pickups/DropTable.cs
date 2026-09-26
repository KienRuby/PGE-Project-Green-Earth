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

    public const float SmallHealthBoxDropChance = 0.015f; // 1.5% xuất hiện Hộp máu nhỏ (hồi 10% max HP)
    public const float LargeHealthBoxDropChance = 0.005f; // 0.5% xuất hiện Hộp máu lớn (hồi 20% max HP)
    public const float SmallHealthBoxHealPercent = 0.10f; // Hồi 10% HP tối đa
    public const float LargeHealthBoxHealPercent = 0.20f; // Hồi 20% HP tối đa

    /// <summary>
    /// Xác định loại ngọc EXP rơi dựa trên điểm expReward:
    /// - 🔷 Ngọc Xanh Lam: Giá trị chuẩn 10 EXP (Quái thường, wave 1 - 5)
    /// - 🔮 Ngọc Tím: Giá trị chuẩn 20 EXP (Quái tinh anh, wave 6 - 10)
    /// - 🌟 Ngọc Vàng: Giá trị chuẩn 50 EXP (Wave 10; 5%)
    /// - 🔴 Ngọc Đỏ: Giá trị chuẩn 100 EXP (Wave 10; 0.1%)
    /// </summary>
    public static GemType DetermineExpGemType(int expAmount)
    {
        if (expAmount >= 100) return GemType.RedExp;     // Đỏ (100 EXP)
        if (expAmount >= 50)  return GemType.YellowExp;  // Vàng (50 EXP)
        if (expAmount >= 20)  return GemType.PurpleExp;  // Tím (20 EXP)
        return GemType.BlueExp;                          // Xanh lam (10 EXP)
    }

    /// <summary>
    /// Tính toán loại ngọc rơi và điểm EXP theo chuẩn thiết kế Wave & Quái:
    /// - Boss luôn rơi Pha lê đỏ (RedExp - 100 EXP)
    /// - Wave 10+: Quái thường và quái tinh anh có 0.1% rơi Pha lê đỏ (100 EXP), 1% rơi Pha lê vàng (50 EXP).
    ///   Nếu không trúng tỷ lệ trên: Quái tinh anh rơi Pha lê tím (20 EXP), Quái nhỏ rơi Pha lê xanh (10 EXP).
    /// - Wave 1 - 5: 100% rơi Pha lê xanh (BlueExp - 10 EXP)
    /// - Wave 6 - 9: Quái nhỏ rơi Pha lê xanh (BlueExp - 10 EXP), Quái tinh anh mới rơi Pha lê tím (PurpleExp - 20 EXP)
    /// </summary>
    public static (GemType gemType, int expValue) CalculateWaveExpDrop(int baseExpReward, int waveNumber, bool isElite = false, bool isBoss = false)
    {
        // 1. Boss luôn rơi Ngọc Đỏ (100 EXP)
        if (isBoss)
        {
            return (GemType.RedExp, 100);
        }

        // 2. Tại Wave 10+: 0.1% rơi Pha lê đỏ, 1% rơi Pha lê vàng (áp dụng cho cả quái thường và quái tinh anh)
        if (waveNumber >= 10)
        {
            float roll = UnityEngine.Random.value;
            // 0.1% rơi Ngọc Đỏ (100 EXP)
            if (roll < 0.001f)
            {
                return (GemType.RedExp, 100);
            }
            // 1% rơi Ngọc Vàng (50 EXP): từ 0.001f đến 0.011f (chiếm đúng 1% tỷ lệ)
            if (roll < 0.011f)
            {
                return (GemType.YellowExp, 50);
            }

            // Phần còn lại của Wave 10: Quái tinh anh rơi Pha lê tím, quái nhỏ rơi Pha lê xanh
            if (isElite)
            {
                return (GemType.PurpleExp, 20);
            }
            return (GemType.BlueExp, 10);
        }

        // 3. Wave 1 - 5: 100% rơi Pha lê xanh (10 EXP)
        if (waveNumber <= 5)
        {
            return (GemType.BlueExp, 10);
        }

        // 4. Wave 6 - 9: Quái tinh anh mới rơi Pha lê tím (20 EXP)
        if (isElite)
        {
            return (GemType.PurpleExp, 20);
        }

        // 5. Wave 6 - 9: Quái nhỏ (quái thường) rơi Pha lê xanh (10 EXP)
        return (GemType.BlueExp, 10);
    }

    public static GemPickup SpawnExpGemForEnemy(Vector3 position, int baseExpReward, int waveNumber, bool isElite = false, bool isBoss = false, bool jumpOut = true)
    {
        var (gemType, expValue) = CalculateWaveExpDrop(baseExpReward, waveNumber, isElite, isBoss);
        return SpawnGem(gemType, expValue, position, jumpOut);
    }

    public static GemPickup SpawnExpGem(Vector3 position, int expAmount, bool jumpOut = true)
    {
        if (expAmount <= 0) return null;
        GemType gemType = DetermineExpGemType(expAmount);
        return SpawnGem(gemType, expAmount, position, jumpOut);
    }

    public static void SpawnLootAt(Vector3 position, int expAmount, int dataChips, int redGems, float powerupChance = 0.05f)
    {
        // 1. Sinh EXP Gem
        if (expAmount > 0)
        {
            SpawnExpGem(position, expAmount, true);
        }

        // 2. Sinh Data Chips
        if (dataChips > 0)
        {
            SpawnGem(GemType.DataChip, dataChips, position + (Vector3)UnityEngine.Random.insideUnitCircle * 0.3f, true);
        }

        // 3. Sinh Red Gems
        if (redGems > 0)
        {
            SpawnGem(GemType.RedGem, redGems, position + (Vector3)UnityEngine.Random.insideUnitCircle * 0.3f, true);
        }

        // 4. Cơ hội rơi Powerup hiếm (Magnet / HealthPack / Bomb)
        if (powerupChance > 0f && UnityEngine.Random.value <= powerupChance)
        {
            float roll = UnityEngine.Random.value;
            GemType powerup = roll < 0.4f ? GemType.Magnet : (roll < 0.75f ? GemType.HealthPack : GemType.Bomb);
            int powerupVal = powerup == GemType.HealthPack ? 25 : 1;
            SpawnGem(powerup, powerupVal, position + (Vector3)UnityEngine.Random.insideUnitCircle * 0.4f, true);
        }
    }

    public const int MAX_ACTIVE_GEMS = 150;
    private static GameObject gemPrefabTemplate;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticsOnDomainReload()
    {
        gemPrefabTemplate = null;
    }

    public static void ResetTemplateForTesting()
    {
        gemPrefabTemplate = null;
    }

    public static GameObject GetOrCreateGemTemplate()
    {
        if (gemPrefabTemplate != null && gemPrefabTemplate != null) return gemPrefabTemplate;

        gemPrefabTemplate = Resources.Load<GameObject>("Pickups/GemPickup");
        if (gemPrefabTemplate == null)
        {
            gemPrefabTemplate = new GameObject("GemPickup_Template");
            gemPrefabTemplate.hideFlags = HideFlags.HideAndDontSave;
            var col = gemPrefabTemplate.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;

            var rb = gemPrefabTemplate.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;

            gemPrefabTemplate.AddComponent<GemPickup>();
            gemPrefabTemplate.AddComponent<PoolMember>();

            var visual = new GameObject("GemVisual", typeof(SpriteRenderer));
            visual.hideFlags = HideFlags.HideAndDontSave;
            visual.transform.SetParent(gemPrefabTemplate.transform, false);

            var glow = new GameObject("GemGlow", typeof(SpriteRenderer));
            glow.hideFlags = HideFlags.HideAndDontSave;
            glow.transform.SetParent(gemPrefabTemplate.transform, false);

            var sparkle = new GameObject("GemSparkle", typeof(SpriteRenderer));
            sparkle.hideFlags = HideFlags.HideAndDontSave;
            sparkle.transform.SetParent(gemPrefabTemplate.transform, false);

            gemPrefabTemplate.SetActive(false);
            if (Application.isPlaying)
            {
                UnityEngine.Object.DontDestroyOnLoad(gemPrefabTemplate);
            }
        }
        return gemPrefabTemplate;
    }

    /// <summary>
    /// Gộp giá trị ngọc khi số lượng ngọc trên sàn đạt giới hạn (Gem Cap).
    /// Bảo toàn 100% EXP của người chơi mà không sinh thêm GameObject mới.
    /// </summary>
    public static GemPickup TryMergeOrUpgradeGem(GemType type, int value, Vector3 position)
    {
        var allGems = GemPickup.AllActiveGems;
        if (allGems == null || allGems.Count == 0) return null;

        // Chỉ gộp các loại EXP Gem
        if (!GemPickup.IsExpGemType(type)) return null;

        GemPickup bestCandidate = null;
        float maxDistSqr = -1f;

        Transform playerTrans = null;
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) playerTrans = playerObj.transform;
        Vector3 centerPos = playerTrans != null ? playerTrans.position : position;

        // Ưu tiên tìm viên ngọc EXP nằm ở xa người chơi nhất chưa bị hút
        for (int i = 0; i < allGems.Count; i++)
        {
            GemPickup gem = allGems[i];
            if (gem != null && gem.gameObject.activeInHierarchy && !gem.IsBeingAttracted && !gem.IsCollected && GemPickup.IsExpGemType(gem.Type))
            {
                float distSqr = (gem.transform.position - centerPos).sqrMagnitude;
                if (distSqr > maxDistSqr)
                {
                    maxDistSqr = distSqr;
                    bestCandidate = gem;
                }
            }
        }

        if (bestCandidate != null)
        {
            bestCandidate.AddValue(value);
            return bestCandidate;
        }

        return null;
    }

    public static GemPickup SpawnGem(GemType type, int value, Vector3 position, bool jumpOut = false)
    {
        // 1. Kiểm tra giới hạn số ngọc trên sân (Gem Cap & Merge)
        if (Application.isPlaying && GemPickup.AllActiveGems.Count >= MAX_ACTIVE_GEMS)
        {
            GemPickup merged = TryMergeOrUpgradeGem(type, value, position);
            if (merged != null)
            {
                return merged;
            }
        }

        // 2. Lấy đối tượng từ PoolManager nếu đang trong PlayMode
        GameObject gemObj = null;
        if (Application.isPlaying && PoolManager.Instance != null)
        {
            GameObject template = GetOrCreateGemTemplate();
            gemObj = PoolManager.Instance.Spawn(template, position, Quaternion.identity);
        }

        // 3. Fallback cho EditMode tests hoặc khi PoolManager chưa có trong Scene
        if (gemObj == null)
        {
            gemObj = new GameObject($"Gem_{type}");
            gemObj.transform.position = position;

            CircleCollider2D col = gemObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;

            Rigidbody2D rb = gemObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;

            gemObj.AddComponent<GemPickup>();
        }

        GemPickup gem = gemObj.GetComponent<GemPickup>();
        gem.Initialize(type, value, position);

        if (jumpOut && Application.isPlaying)
        {
            Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
            if (randomDir == Vector2.zero) randomDir = Vector2.up;
            float popDist = UnityEngine.Random.Range(0.45f, 0.9f);
            Vector3 landPos = position + new Vector3(randomDir.x * popDist, randomDir.y * popDist * 0.7f, 0f);
            gem.TriggerJumpOut(position, landPos, 0.35f, 0.6f);
        }

        return gem;
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
    /// - 0.5% rơi Hộp máu lớn (hồi 20% max HP)
    /// - 1.5% rơi Hộp máu nhỏ (hồi 10% max HP)
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
