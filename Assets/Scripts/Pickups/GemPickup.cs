using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GemType
{
    GreenExp = 0, // Legacy fallback -> mapped to Blue
    BlueExp = 1,  // Xanh lam (Tier 1 EXP)
    PurpleExp = 2,// Tím (Tier 2 EXP)
    YellowExp = 3,// Vàng (Tier 3 EXP)
    RedExp = 4,   // Đỏ (Tier 4 EXP)
    DataChip = 5, // Blue Data Chip currency
    RedGem = 6,   // Red Gem currency
    Magnet = 7,   // Collect all gems on field
    HealthPack = 8,// Restore 25% HP
    Bomb = 9      // Blast all on-screen enemies
}

/// <summary>
/// Thực thể vật phẩm rơi khi quái vật bị tiêu diệt (EXP Gem, Currency, Magnet, Powerup).
/// Hỗ trợ:
/// - Hiệu ứng nhảy văng ra từ quái (Jump/Arc bounce) khi bị tiêu diệt.
/// - Hiệu ứng bay bập bềnh (Idle bobbing & pulse) khi nằm trên đất.
/// - Lực hút nam châm mượt mà bay về phía người chơi (Magnet Attraction).
/// - Tự động thu thập và cộng điểm khi chạm vào người chơi.
/// - Nạp tự động bộ sprite tinh thể pha lê 4 màu: Xanh lam, Tím, Vàng, Đỏ.
/// - Tương thích 100% với ObjectPool (IPoolable) không cấp phát bộ nhớ rác GC.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class GemPickup : MonoBehaviour, IPoolable
{
    private static readonly List<GemPickup> ActiveGems = new List<GemPickup>();
    public static IReadOnlyList<GemPickup> AllActiveGems => ActiveGems;

    public static void ClearActiveGemsForTesting()
    {
        ActiveGems.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticsOnDomainReload()
    {
        ActiveGems.Clear();
        cachedPlayerTrans = null;
        nextPlayerSearchTime = 0f;
        cachedGlowMaterial = null;
        cachedBlueExpSprite = null;
        cachedPurpleExpSprite = null;
        cachedYellowExpSprite = null;
        cachedRedExpSprite = null;
        cachedDataChipSprite = null;
        cachedRedGemSprite = null;
        cachedGlowSprite = null;
        cachedSparkleSprite = null;
    }

    public static bool IsExpGemType(GemType type)
    {
        return type == GemType.GreenExp ||
               type == GemType.BlueExp ||
               type == GemType.PurpleExp ||
               type == GemType.YellowExp ||
               type == GemType.RedExp;
    }

    [Header("Gem Configuration")]
    [SerializeField] private GemType gemType = GemType.BlueExp;
    [SerializeField] private int value = 10;
    [SerializeField] private float magnetAttractionSpeed = 14f;
    [SerializeField] private float pickupRadius = 0.8f;
    [SerializeField] private float idleFloatSpeed = 3f;
    [SerializeField] private float idleFloatAmount = 0.02f;

    [Header("Visual & Glow")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer glowRenderer;
    [SerializeField] private SpriteRenderer sparkleRenderer;
    [SerializeField] private float visualWorldSize = 0.105f;

    private static Sprite cachedBlueExpSprite;
    private static Sprite cachedPurpleExpSprite;
    private static Sprite cachedYellowExpSprite;
    private static Sprite cachedRedExpSprite;
    private static Sprite cachedDataChipSprite;
    private static Sprite cachedRedGemSprite;
    private static Sprite cachedGlowSprite;
    private static Sprite cachedSparkleSprite;
    private static Material cachedGlowMaterial;

    private Transform playerTarget;
    private bool isBeingAttracted;
    private bool isCollected;
    private bool isJumping;
    private float currentSpeed;
    private Vector3 initialSpawnPosition;
    private Vector3 visualBaseScale = Vector3.one;
    private Vector3 glowBaseScale = Vector3.one;
    private Vector3 sparkleBaseScale = Vector3.one;
    private Color currentGlowColor = Color.white;
    private Color currentSparkleColor = Color.white;
    private float spawnTime;
    private Coroutine jumpRoutine;

    public GemType Type => gemType;
    public int Value => value;
    public bool IsBeingAttracted => isBeingAttracted;
    public bool IsJumping => isJumping;
    public bool IsCollected => isCollected;
    public SpriteRenderer VisualRenderer => spriteRenderer;

    private void Awake()
    {
        initialSpawnPosition = transform.position;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            if (col is CircleCollider2D circleCol)
            {
                circleCol.radius = 0.35f;
            }
        }

        EnsureVisual();
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
        isBeingAttracted = false;
        isCollected = false;
        isJumping = false;
    }

    private void OnDisable()
    {
        ActiveGems.Remove(this);
        isBeingAttracted = false;
        playerTarget = null;
        if (jumpRoutine != null)
        {
            StopCoroutine(jumpRoutine);
            jumpRoutine = null;
        }
        isJumping = false;
    }

    private void OnDestroy()
    {
        ActiveGems.Remove(this);
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
        isCollected = false;
        isJumping = false;
        spawnTime = Time.time;

        if (jumpRoutine != null)
        {
            StopCoroutine(jumpRoutine);
            jumpRoutine = null;
        }

        EnsureVisual(true);
    }

    public void TriggerJumpOut(Vector3 startPos, Vector3 targetLandPos, float duration = 0.38f, float arcHeight = 0.65f)
    {
        if (jumpRoutine != null)
        {
            StopCoroutine(jumpRoutine);
        }
        jumpRoutine = StartCoroutine(JumpOutRoutine(startPos, targetLandPos, duration, arcHeight));
    }

    private IEnumerator JumpOutRoutine(Vector3 start, Vector3 end, float duration, float arcHeight)
    {
        isJumping = true;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Đường cong parabol nảy lên rồi hạ xuống đất
            float arc = 4f * arcHeight * t * (1f - t);
            Vector3 flat = Vector3.Lerp(start, end, t);
            transform.position = flat + new Vector3(0f, arc, 0f);

            // Co giãn nhẹ tạo cảm giác sống động (Squash & stretch)
            float stretch = 1f + 0.15f * Mathf.Sin(t * Mathf.PI);
            if (spriteRenderer != null)
            {
                spriteRenderer.transform.localScale = new Vector3(visualBaseScale.x / stretch, visualBaseScale.y * stretch, visualBaseScale.z);
            }
            if (glowRenderer != null)
            {
                glowRenderer.transform.localScale = new Vector3(glowBaseScale.x / stretch, glowBaseScale.y * stretch, glowBaseScale.z);
            }
            yield return null;
        }

        transform.position = end;
        initialSpawnPosition = end;
        isJumping = false;
        spawnTime = Time.time;
        if (spriteRenderer != null) spriteRenderer.transform.localScale = visualBaseScale;
        if (glowRenderer != null) glowRenderer.transform.localScale = glowBaseScale;
        if (sparkleRenderer != null) sparkleRenderer.transform.localScale = sparkleBaseScale;
        jumpRoutine = null;
    }

    private static Transform cachedPlayerTrans;
    private static float nextPlayerSearchTime = 0f;

    private static Transform GetPlayerTransform()
    {
        if (cachedPlayerTrans != null && cachedPlayerTrans.gameObject.activeInHierarchy)
            return cachedPlayerTrans;

        if (Time.time < nextPlayerSearchTime)
            return null;

        nextPlayerSearchTime = Time.time + 0.5f;
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            var pMove = UnityEngine.Object.FindObjectOfType<PlayerMovement>();
            if (pMove != null) playerObj = pMove.gameObject;
        }
        if (playerObj != null) cachedPlayerTrans = playerObj.transform;
        return cachedPlayerTrans;
    }

    private void Update()
    {
        if (isCollected) return;

        if (isBeingAttracted && playerTarget != null)
        {
            currentSpeed = Mathf.Min(currentSpeed + magnetAttractionSpeed * 2.5f * Time.deltaTime, magnetAttractionSpeed * 2f);
            Vector3 dir = (playerTarget.position - transform.position).normalized;
            transform.position += dir * (currentSpeed * Time.deltaTime);

            // Khi đang bay về Player: Hào quang sáng bừng lên rực rỡ
            if (glowRenderer != null)
            {
                glowRenderer.transform.localScale = glowBaseScale * 1.35f;
                glowRenderer.color = new Color(currentGlowColor.r, currentGlowColor.g, currentGlowColor.b, Mathf.Min(1f, currentGlowColor.a * 1.25f));
            }

            float distSqr = (playerTarget.position - transform.position).sqrMagnitude;
            if (distSqr <= pickupRadius * pickupRadius)
            {
                Collect();
            }
        }
        else if (!isJumping)
        {
            Transform player = GetPlayerTransform();
            float sqrDistToPlayer = player != null ? (player.position - transform.position).sqrMagnitude : 9999f;

            // Tự động kích hoạt hút nam châm mượt mà khi người chơi di chuyển lại gần
            float naturalAttractRange = 0.45f; // Tầm bước chân tiếp xúc gần (0.45m)
            if (sqrDistToPlayer <= naturalAttractRange * naturalAttractRange)
            {
                TriggerMagnetAttraction(player);
                return;
            }

            // Distance Culling: Nếu ngọc nằm quá xa tầm nhìn camera (> 12m), bỏ qua animation nhấp nhô & đổi màu để tiết kiệm CPU/GPU
            if (sqrDistToPlayer > 144f)
            {
                return;
            }

            // Diễn hoạt bay dập dềnh nhẹ (Idle floating bobbing):
            // TỐI ƯU HÓA: Dịch chuyển localPosition của renderer con, KHÔNG gán transform.position của Root
            // nhằm tránh ép PhysX 2D cập nhật Dynamic Tree / Broadphase của Rigidbody2D liên tục mỗi frame.
            float offset = Mathf.Sin((Time.time - spawnTime) * idleFloatSpeed) * idleFloatAmount;
            Vector3 bobOffset = new Vector3(0f, offset, 0f);

            if (spriteRenderer != null)
            {
                spriteRenderer.transform.localPosition = bobOffset;
            }
            if (glowRenderer != null)
            {
                glowRenderer.transform.localPosition = bobOffset;
            }
            if (sparkleRenderer != null)
            {
                sparkleRenderer.transform.localPosition = new Vector3(0.018f, 0.019f, 0f) + bobOffset;
            }

            float pulseTimer = (Time.time - spawnTime) * 3.5f;

            // 1. Nhịp thở phát sáng nhẹ cho thân tinh thể
            if (spriteRenderer != null && spriteRenderer.transform != transform)
            {
                float pulse = 1f + 0.05f * Mathf.Sin(pulseTimer);
                spriteRenderer.transform.localScale = visualBaseScale * pulse;
            }

            // 2. Hiệu ứng Hào Quang Phát Sáng rực rỡ thở nhịp nhàng (Breathing Aura Glow)
            if (glowRenderer != null)
            {
                float glowPulse = 1f + 0.20f * Mathf.Sin(pulseTimer);
                glowRenderer.transform.localScale = glowBaseScale * glowPulse;

                float alphaPulse = 0.82f + 0.18f * Mathf.Sin(pulseTimer);
                glowRenderer.color = new Color(currentGlowColor.r, currentGlowColor.g, currentGlowColor.b, currentGlowColor.a * alphaPulse);
            }

            // 3. Hiệu ứng Lấp Lánh Ánh Sao xoay nhẹ (Twinkling Star Sparkle)
            if (sparkleRenderer != null)
            {
                float sparkleAngle = (Time.time - spawnTime) * 45f;
                sparkleRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, sparkleAngle);

                float sparkleAlpha = Mathf.Clamp01(0.45f + 0.55f * Mathf.Sin((Time.time - spawnTime) * 4.8f));
                sparkleRenderer.color = new Color(currentSparkleColor.r, currentSparkleColor.g, currentSparkleColor.b, currentSparkleColor.a * sparkleAlpha);
            }
        }
    }

    public void TriggerMagnetAttraction(Transform target)
    {
        if (target == null || isCollected) return;
        playerTarget = target;
        isBeingAttracted = true;
        currentSpeed = magnetAttractionSpeed * 0.5f;

        // Nếu đang nhảy dở mà được hút nam châm thì chuyển sang bay về Player mượt mà
        if (isJumping && jumpRoutine != null)
        {
            StopCoroutine(jumpRoutine);
            jumpRoutine = null;
            isJumping = false;
            if (spriteRenderer != null)
            {
                spriteRenderer.transform.localScale = visualBaseScale;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player") ||
            other.GetComponentInParent<PlayerMovement>() != null ||
            other.GetComponentInParent<PlayerHealth>() != null)
        {
            Collect();
        }
    }

    public void Collect()
    {
        if (isCollected) return;
        isCollected = true;

        AudioManager.Instance?.PlaySFX(SoundIdConst.SFX_PICKUP_ITEM);
        switch (gemType)
        {
            case GemType.GreenExp:
            case GemType.BlueExp:
            case GemType.PurpleExp:
            case GemType.YellowExp:
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
                    PlayerHealth hp = player.GetComponent<PlayerHealth>() ?? player.GetComponentInParent<PlayerHealth>();
                    if (hp != null) hp.Heal(value);
                }
                break;

            case GemType.Bomb:
                BlastAllEnemiesOnScreen();
                break;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

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

    public void EnsureVisual(bool forceUpdate = false)
    {
        // 1. Thân tinh thể chính (GemVisual)
        if (spriteRenderer == null)
        {
            Transform visualTrans = transform.Find("GemVisual");
            if (visualTrans != null)
            {
                spriteRenderer = visualTrans.GetComponent<SpriteRenderer>();
            }
            if (spriteRenderer == null)
            {
                GameObject visualObj = new GameObject("GemVisual", typeof(SpriteRenderer));
                visualObj.transform.SetParent(transform, false);
                spriteRenderer = visualObj.GetComponent<SpriteRenderer>();
            }
        }

        if (forceUpdate || spriteRenderer.sprite == null)
        {
            Sprite targetSprite = GetGemSprite(gemType);
            if (targetSprite == null) targetSprite = CreateProceduralFallbackSprite(gemType);
            spriteRenderer.sprite = targetSprite;
        }

        FitVisualToWorldSize();

        string targetSortingLayer = SortingLayer.NameToID("VFX ") != 0 ? "VFX " : "Default";
        spriteRenderer.sortingLayerName = targetSortingLayer;
        spriteRenderer.sortingOrder = 85;

        // 2. Vầng hào quang phát sáng tương ứng theo màu (GemGlow)
        if (glowRenderer == null)
        {
            Transform glowTrans = transform.Find("GemGlow");
            if (glowTrans != null)
            {
                glowRenderer = glowTrans.GetComponent<SpriteRenderer>();
            }
            if (glowRenderer == null)
            {
                GameObject glowObj = new GameObject("GemGlow", typeof(SpriteRenderer));
                glowObj.transform.SetParent(transform, false);
                glowRenderer = glowObj.GetComponent<SpriteRenderer>();
            }
        }

        if (glowRenderer != null)
        {
            Sprite glowSprite = GetGlowSprite();
            if (glowSprite != null) glowRenderer.sprite = glowSprite;

            Material glowMat = GetGlowMaterial();
            if (glowMat != null) glowRenderer.sharedMaterial = glowMat;

            glowRenderer.sortingLayerName = targetSortingLayer;
            glowRenderer.sortingOrder = spriteRenderer.sortingOrder - 1; // Nằm ngay phía sau tinh thể

            currentGlowColor = GetGlowColor(gemType);
            glowRenderer.color = currentGlowColor;

            glowBaseScale = visualBaseScale * 3.8f;
            glowRenderer.transform.localScale = glowBaseScale;
        }

        // 3. Đốm sáng lấp lánh ánh sao kim cương (GemSparkle)
        if (sparkleRenderer == null)
        {
            Transform sparkleTrans = transform.Find("GemSparkle");
            if (sparkleTrans != null)
            {
                sparkleRenderer = sparkleTrans.GetComponent<SpriteRenderer>();
            }
            if (sparkleRenderer == null)
            {
                GameObject sparkleObj = new GameObject("GemSparkle", typeof(SpriteRenderer));
                sparkleObj.transform.SetParent(transform, false);
                sparkleRenderer = sparkleObj.GetComponent<SpriteRenderer>();
            }
        }

        if (sparkleRenderer != null)
        {
            Sprite sparkleSprite = GetSparkleSprite();
            if (sparkleSprite != null) sparkleRenderer.sprite = sparkleSprite;

            Material glowMat = GetGlowMaterial();
            if (glowMat != null) sparkleRenderer.sharedMaterial = glowMat;

            sparkleRenderer.sortingLayerName = targetSortingLayer;
            sparkleRenderer.sortingOrder = spriteRenderer.sortingOrder + 1; // Nằm trên mặt trước tinh thể

            currentSparkleColor = Color.Lerp(Color.white, currentGlowColor, 0.25f);
            sparkleRenderer.color = currentSparkleColor;

            sparkleRenderer.transform.localPosition = new Vector3(0.018f, 0.019f, 0f);
            sparkleBaseScale = visualBaseScale * 1.15f;
            sparkleRenderer.transform.localScale = sparkleBaseScale;
        }
    }

    private void FitVisualToWorldSize()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;
        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
        float maxSide = Mathf.Max(spriteSize.x, spriteSize.y);
        float scale = maxSide > 0f ? visualWorldSize / maxSide : 1f;
        visualBaseScale = Vector3.one * scale;
        spriteRenderer.transform.localScale = visualBaseScale;
    }

    public static Sprite GetGemSprite(GemType type)
    {
        switch (type)
        {
            case GemType.GreenExp:
            case GemType.BlueExp:
                if (cachedBlueExpSprite != null) return cachedBlueExpSprite;
                cachedBlueExpSprite = Resources.Load<Sprite>("EXP/exp_gem_blue");
#if UNITY_EDITOR
                if (cachedBlueExpSprite == null)
                    cachedBlueExpSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Pickups/EXP/exp_gem_blue.png");
#endif
                return cachedBlueExpSprite;

            case GemType.PurpleExp:
                if (cachedPurpleExpSprite != null) return cachedPurpleExpSprite;
                cachedPurpleExpSprite = Resources.Load<Sprite>("EXP/exp_gem_purple");
#if UNITY_EDITOR
                if (cachedPurpleExpSprite == null)
                    cachedPurpleExpSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Pickups/EXP/exp_gem_purple.png");
#endif
                return cachedPurpleExpSprite;

            case GemType.YellowExp:
                if (cachedYellowExpSprite != null) return cachedYellowExpSprite;
                cachedYellowExpSprite = Resources.Load<Sprite>("EXP/exp_gem_yellow");
#if UNITY_EDITOR
                if (cachedYellowExpSprite == null)
                    cachedYellowExpSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Pickups/EXP/exp_gem_yellow.png");
#endif
                return cachedYellowExpSprite;

            case GemType.RedExp:
                if (cachedRedExpSprite != null) return cachedRedExpSprite;
                cachedRedExpSprite = Resources.Load<Sprite>("EXP/exp_gem_red");
#if UNITY_EDITOR
                if (cachedRedExpSprite == null)
                    cachedRedExpSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Pickups/EXP/exp_gem_red.png");
#endif
                return cachedRedExpSprite;

            case GemType.DataChip:
                if (cachedDataChipSprite != null) return cachedDataChipSprite;
                cachedDataChipSprite = Resources.Load<Sprite>("UI/Reward/Extracted/Icon_Data_Chip");
#if UNITY_EDITOR
                if (cachedDataChipSprite == null)
                    cachedDataChipSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Reward/Extracted/Icon_Data_Chip.png");
#endif
                return cachedDataChipSprite;

            case GemType.RedGem:
                if (cachedRedGemSprite != null) return cachedRedGemSprite;
                cachedRedGemSprite = Resources.Load<Sprite>("UI/GemMine/icon_red_gem");
#if UNITY_EDITOR
                if (cachedRedGemSprite == null)
                    cachedRedGemSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/GemMine/icon_red_gem.png");
#endif
                return cachedRedGemSprite;

            default:
                return null;
        }
    }

    /// <summary>
    /// Màu phát sáng tương ứng cho từng loại pha lê kinh nghiệm & vật phẩm:
    /// - Xanh lam: Phát sáng xanh (Cyan / Neon Blue)
    /// - Tím: Phát sáng tím (Neon Violet / Purple)
    /// - Vàng: Phát sáng vàng (Bright Gold / Warm Yellow)
    /// - Đỏ: Phát sáng đỏ (Ruby Crimson Red)
    /// </summary>
    public static Color GetGlowColor(GemType type)
    {
        switch (type)
        {
            case GemType.GreenExp:
            case GemType.BlueExp:
                // Màu xanh phát sáng xanh neon rực rỡ
                return new Color(0.0f, 0.95f, 1.0f, 1.0f);

            case GemType.PurpleExp:
                // Tím phát sáng tím neon ma thuật
                return new Color(0.95f, 0.22f, 1.0f, 1.0f);

            case GemType.YellowExp:
                // Màu vàng phát sáng vàng kim mặt trời
                return new Color(1.0f, 0.88f, 0.05f, 1.0f);

            case GemType.RedExp:
            case GemType.RedGem:
                // Đỏ phát sáng đỏ ruby rực lửa
                return new Color(1.0f, 0.12f, 0.18f, 1.0f);

            case GemType.DataChip:
                return new Color(0.2f, 0.85f, 1.0f, 1.0f);

            default:
                return new Color(1.0f, 1.0f, 1.0f, 1.0f);
        }
    }

    public static Material GetGlowMaterial()
    {
        if (cachedGlowMaterial != null) return cachedGlowMaterial;
        Shader s = Shader.Find("Custom/2D/SpriteGlowAdditive");
        if (s == null) s = Shader.Find("Sprites/Default");
        if (s != null)
        {
            cachedGlowMaterial = new Material(s);
            cachedGlowMaterial.name = "Runtime_SpriteGlowAdditive_Shared";
            cachedGlowMaterial.hideFlags = HideFlags.DontSave;
        }
        return cachedGlowMaterial;
    }

    public static Sprite GetGlowSprite()
    {
        if (cachedGlowSprite != null) return cachedGlowSprite;
        cachedGlowSprite = Resources.Load<Sprite>("EXP/exp_gem_glow");
#if UNITY_EDITOR
        if (cachedGlowSprite == null)
            cachedGlowSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Pickups/EXP/exp_gem_glow.png");
#endif
        if (cachedGlowSprite == null)
            cachedGlowSprite = CreateProceduralFallbackGlowSprite();
        return cachedGlowSprite;
    }

    public static Sprite GetSparkleSprite()
    {
        if (cachedSparkleSprite != null) return cachedSparkleSprite;
        cachedSparkleSprite = Resources.Load<Sprite>("EXP/exp_gem_sparkle");
#if UNITY_EDITOR
        if (cachedSparkleSprite == null)
            cachedSparkleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Pickups/EXP/exp_gem_sparkle.png");
#endif
        if (cachedSparkleSprite == null)
            cachedSparkleSprite = CreateProceduralFallbackSparkleSprite();
        return cachedSparkleSprite;
    }

    private static Sprite CreateProceduralFallbackGlowSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center)) / center;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.hideFlags = HideFlags.DontSave;
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }

    private static Sprite CreateProceduralFallbackSparkleSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - center);
                float dy = Mathf.Abs(y - center);
                float a = Mathf.Max(0f, 1f - dy / 2.5f) * Mathf.Max(0f, 1f - dx / center) +
                          Mathf.Max(0f, 1f - dx / 2.5f) * Mathf.Max(0f, 1f - dy / center);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
            }
        }
        tex.hideFlags = HideFlags.DontSave;
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }

    private static Sprite CreateProceduralFallbackSprite(GemType type)
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color32 col = Color.cyan;
        switch (type)
        {
            case GemType.PurpleExp: col = new Color32(170, 70, 255, 255); break;
            case GemType.YellowExp: col = new Color32(255, 210, 40, 255); break;
            case GemType.RedExp:
            case GemType.RedGem: col = new Color32(255, 50, 70, 255); break;
            case GemType.DataChip: col = new Color32(50, 180, 255, 255); break;
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Abs(x - 16);
                int dy = Mathf.Abs(y - 16);
                if (dx + dy <= 14)
                {
                    if (dx + dy >= 13) tex.SetPixel(x, y, new Color32(20, 20, 20, 255));
                    else tex.SetPixel(x, y, col);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.hideFlags = HideFlags.DontSave;
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }

    /// <summary>
    /// Cộng thêm giá trị cho viên ngọc khi cơ chế Gem Cap kích hoạt (gộp ngọc).
    /// Tự động nâng cấp màu sắc/sprite nếu là EXP Gem và đạt ngưỡng cấp độ mới.
    /// </summary>
    public void AddValue(int extraValue)
    {
        if (extraValue <= 0) return;
        value += extraValue;

        // Nếu là EXP Gem thì kiểm tra nâng cấp Tier
        if (IsExpGemType(gemType))
        {
            GemType newType = DropTable.DetermineExpGemType(value);
            if (newType != gemType)
            {
                gemType = newType;
                EnsureVisual(true);
            }
        }

        // Hiệu ứng phình to nhẹ báo hiệu vừa được gộp giá trị
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(PulsePopRoutine());
        }
    }

    private IEnumerator PulsePopRoutine()
    {
        float dur = 0.22f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Sin((elapsed / dur) * Mathf.PI);
            float scale = 1f + 0.35f * t;
            if (spriteRenderer != null) spriteRenderer.transform.localScale = visualBaseScale * scale;
            if (glowRenderer != null) glowRenderer.transform.localScale = glowBaseScale * scale;
            yield return null;
        }
        if (spriteRenderer != null) spriteRenderer.transform.localScale = visualBaseScale;
        if (glowRenderer != null) glowRenderer.transform.localScale = glowBaseScale;
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
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(gameObject);
                return;
            }
#endif
            Destroy(gameObject);
        }
    }

    public void OnSpawnFromPool()
    {
        isBeingAttracted = false;
        playerTarget = null;
        currentSpeed = 0f;
        isCollected = false;
        isJumping = false;
        spawnTime = Time.time;
        if (jumpRoutine != null)
        {
            StopCoroutine(jumpRoutine);
            jumpRoutine = null;
        }
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.transform.localPosition = Vector3.zero;
            spriteRenderer.transform.localScale = visualBaseScale;
        }
        if (glowRenderer != null)
        {
            glowRenderer.transform.localPosition = Vector3.zero;
            glowRenderer.transform.localScale = glowBaseScale;
        }
        if (sparkleRenderer != null)
        {
            sparkleRenderer.transform.localPosition = new Vector3(0.018f, 0.019f, 0f);
            sparkleRenderer.transform.localScale = sparkleBaseScale;
        }
    }

    public void OnReturnToPool()
    {
        isBeingAttracted = false;
        playerTarget = null;
        isJumping = false;
        isCollected = false;
        if (jumpRoutine != null)
        {
            StopCoroutine(jumpRoutine);
            jumpRoutine = null;
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.transform.localPosition = Vector3.zero;
        }
        if (glowRenderer != null)
        {
            glowRenderer.transform.localPosition = Vector3.zero;
        }
        if (sparkleRenderer != null)
        {
            sparkleRenderer.transform.localPosition = new Vector3(0.018f, 0.019f, 0f);
        }
    }
}
