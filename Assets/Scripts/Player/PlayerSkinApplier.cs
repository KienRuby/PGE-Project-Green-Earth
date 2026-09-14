using System;
using UnityEngine;

[System.Serializable]
public class BodyPartTransformConfig
{
    [Tooltip("Độ dịch chuyển vị trí (Offset) so với gốc.")]
    public Vector2 positionOffset = Vector2.zero;

    [Tooltip("Tỷ lệ thu phóng (Scale) của bộ phận này (1, 1 = kích thước chuẩn).")]
    public Vector2 scaleMultiplier = Vector2.one;

    [Tooltip("Góc xoay cộng thêm (Độ).")]
    public float rotationOffset = 0f;

    public BodyPartTransformConfig()
    {
        positionOffset = Vector2.zero;
        scaleMultiplier = Vector2.one;
        rotationOffset = 0f;
    }

    public BodyPartTransformConfig(Vector2 pos, Vector2 scale, float rot = 0f)
    {
        positionOffset = pos;
        scaleMultiplier = scale;
        rotationOffset = rot;
    }
}

[System.Serializable]
public class PlayerSkinConfig
{
    [Tooltip("Tên định danh của Skin (ví dụ: AD Unit-1, AD Unit-2,...).")]
    public string skinName = "AD Unit";

    [Header("Sprite các bộ phận cơ thể")]
    [Tooltip("Sprite Thân robot.")]
    public Sprite bodySprite;

    [Tooltip("Sprite Cánh tay trên / Khớp vai.")]
    public Sprite armSprite;

    [Tooltip("Sprite Khẩu súng.")]
    public Sprite gunSprite;

    [Tooltip("Sprite Chân trái (Chan 1).")]
    public Sprite leg1Sprite;

    [Tooltip("Sprite Chân phải (chan 2).")]
    public Sprite leg2Sprite;

    [Header("Tùy chỉnh Vị trí & Kích thước từng bộ phận (Không ảnh hưởng Animation)")]
    public BodyPartTransformConfig body = new BodyPartTransformConfig();
    public BodyPartTransformConfig arm = new BodyPartTransformConfig();
    public BodyPartTransformConfig gun = new BodyPartTransformConfig();
    public BodyPartTransformConfig leg1 = new BodyPartTransformConfig();
    public BodyPartTransformConfig leg2 = new BodyPartTransformConfig();

    [Header("Tọa độ nòng súng (Local Position của FirePoint)")]
    [Tooltip("Tọa độ vị trí nòng súng riêng cho skin này (Local đối với cha của FirePoint).")]
    public Vector2 firePointOffset = new Vector2(3.59f, -0.99f);
}

[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)] // Chạy trước PlayerAutoShooter để vị trí súng và nòng súng sẵn sàng
public class PlayerSkinApplier : MonoBehaviour
{
    [Header("Visual Slots (Các đối tượng con chứa SpriteRenderer - Tách biệt khỏi Bone Animation)")]
    [Tooltip("Transform chứa Sprite Thân (con của bone 'thân').")]
    public Transform bodyVisual;

    [Tooltip("Transform chứa Sprite Súng (con của bone 'GunSprite').")]
    public Transform gunVisual;

    [Tooltip("Transform chứa Sprite Cánh tay (con của bone 'Tay').")]
    public Transform armVisual;

    [Tooltip("Transform chứa Sprite Chân trái (con của bone 'Chan 1').")]
    public Transform leg1Visual;

    [Tooltip("Transform chứa Sprite Chân phải (con của bone 'chan 2').")]
    public Transform leg2Visual;

    [Header("Renderers trên Visual Slots")]
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer armRenderer;
    public SpriteRenderer gunRenderer;
    public SpriteRenderer leg1Renderer;
    public SpriteRenderer leg2Renderer;

    [Tooltip("Transform nòng súng (FirePoint).")]
    public Transform firePoint;

    [Header("Danh sách 4 Skin")]
    public PlayerSkinConfig[] skins = new PlayerSkinConfig[4]
    {
        new PlayerSkinConfig { skinName = "AD Unit-1 (Basic Body)", firePointOffset = new Vector2(3.59f, -0.99f) },
        new PlayerSkinConfig {
            skinName = "AD Unit-2",
            gun = new BodyPartTransformConfig(new Vector2(-0.2f, 0f), new Vector2(0.85f, 0.85f)),
            firePointOffset = new Vector2(3.59f, -0.99f)
        },
        new PlayerSkinConfig {
            skinName = "AD Unit-3",
            gun = new BodyPartTransformConfig(new Vector2(-0.3f, -0.1f), new Vector2(0.8f, 0.8f)),
            firePointOffset = new Vector2(3.59f, -0.99f)
        },
        new PlayerSkinConfig {
            skinName = "AD Unit-4",
            gun = new BodyPartTransformConfig(new Vector2(-0.4f, 0f), new Vector2(0.75f, 0.75f)),
            firePointOffset = new Vector2(3.59f, -0.99f)
        }
    };

    [Header("Editor Preview")]
    [Range(0, 3)]
    public int previewSkinIndex = 0;

    public int ActiveAppliedIndex => activeAppliedIndex;
    private int activeAppliedIndex = -1;

    private void Awake()
    {
        AutoEnsureVisualSlots();
        ApplyEquippedSkin();
    }

    private void OnEnable()
    {
        BuildBodyController.OnEquippedSkinChanged += HandleEquippedSkinChanged;
    }

    private void OnDisable()
    {
        BuildBodyController.OnEquippedSkinChanged -= HandleEquippedSkinChanged;
    }

    private void HandleEquippedSkinChanged(int newSkinIndex)
    {
        ApplySkin(newSkinIndex);
    }

    /// <summary>
    /// Đọc skin index đang được trang bị từ BuildBodyController và áp dụng.
    /// </summary>
    public void ApplyEquippedSkin()
    {
        int equippedIndex = BuildBodyController.EquippedSkinIndex;
        ApplySkin(equippedIndex);
    }

    /// <summary>
    /// Áp dụng toàn bộ sprite, vị trí offset và tỷ lệ thu phóng của skin tương ứng.
    /// Hoạt động an toàn 100% không bị Animator ghi đè nhờ kiến trúc Visual Slot.
    /// </summary>
    public void ApplySkin(int skinIndex)
    {
        if (skins == null || skins.Length == 0) return;

        skinIndex = Mathf.Clamp(skinIndex, 0, skins.Length - 1);
        activeAppliedIndex = skinIndex;
        PlayerSkinConfig skin = skins[skinIndex];
        if (skin == null) return;

        AutoEnsureVisualSlots();

        // 1. Áp dụng Sprite cho từng bộ phận
        if (bodyRenderer != null && skin.bodySprite != null)
            bodyRenderer.sprite = skin.bodySprite;

        if (armRenderer != null && skin.armSprite != null)
            armRenderer.sprite = skin.armSprite;

        if (gunRenderer != null && skin.gunSprite != null)
            gunRenderer.sprite = skin.gunSprite;

        if (leg1Renderer != null && skin.leg1Sprite != null)
            leg1Renderer.sprite = skin.leg1Sprite;

        if (leg2Renderer != null && skin.leg2Sprite != null)
            leg2Renderer.sprite = skin.leg2Sprite;

        // 2. Áp dụng Vị trí Offset, Tỷ lệ Scale và Góc xoay lên từng Visual Slot
        if (bodyVisual != null && skin.body != null)
        {
            bodyVisual.localPosition = new Vector3(skin.body.positionOffset.x, skin.body.positionOffset.y, 0f);
            bodyVisual.localScale = new Vector3(
                skin.body.scaleMultiplier.x == 0f ? 1f : skin.body.scaleMultiplier.x,
                skin.body.scaleMultiplier.y == 0f ? 1f : skin.body.scaleMultiplier.y,
                1f
            );
            bodyVisual.localRotation = Quaternion.Euler(0f, 0f, skin.body.rotationOffset);
        }

        if (gunVisual != null && skin.gun != null)
        {
            gunVisual.localPosition = new Vector3(skin.gun.positionOffset.x, skin.gun.positionOffset.y, 0f);
            gunVisual.localScale = new Vector3(
                skin.gun.scaleMultiplier.x == 0f ? 1f : skin.gun.scaleMultiplier.x,
                skin.gun.scaleMultiplier.y == 0f ? 1f : skin.gun.scaleMultiplier.y,
                1f
            );
            gunVisual.localRotation = Quaternion.Euler(0f, 0f, skin.gun.rotationOffset);
        }

        if (armVisual != null && skin.arm != null)
        {
            armVisual.localPosition = new Vector3(skin.arm.positionOffset.x, skin.arm.positionOffset.y, 0f);
            armVisual.localScale = new Vector3(
                skin.arm.scaleMultiplier.x == 0f ? 1f : skin.arm.scaleMultiplier.x,
                skin.arm.scaleMultiplier.y == 0f ? 1f : skin.arm.scaleMultiplier.y,
                1f
            );
            armVisual.localRotation = Quaternion.Euler(0f, 0f, skin.arm.rotationOffset);
        }

        if (leg1Visual != null && skin.leg1 != null)
        {
            leg1Visual.localPosition = new Vector3(skin.leg1.positionOffset.x, skin.leg1.positionOffset.y, 0f);
            leg1Visual.localScale = new Vector3(
                skin.leg1.scaleMultiplier.x == 0f ? 1f : skin.leg1.scaleMultiplier.x,
                skin.leg1.scaleMultiplier.y == 0f ? 1f : skin.leg1.scaleMultiplier.y,
                1f
            );
            leg1Visual.localRotation = Quaternion.Euler(0f, 0f, skin.leg1.rotationOffset);
        }

        if (leg2Visual != null && skin.leg2 != null)
        {
            leg2Visual.localPosition = new Vector3(skin.leg2.positionOffset.x, skin.leg2.positionOffset.y, 0f);
            leg2Visual.localScale = new Vector3(
                skin.leg2.scaleMultiplier.x == 0f ? 1f : skin.leg2.scaleMultiplier.x,
                skin.leg2.scaleMultiplier.y == 0f ? 1f : skin.leg2.scaleMultiplier.y,
                1f
            );
            leg2Visual.localRotation = Quaternion.Euler(0f, 0f, skin.leg2.rotationOffset);
        }

        // 3. Cập nhật vị trí nòng súng (FirePoint)
        if (firePoint != null)
        {
            firePoint.localPosition = new Vector3(skin.firePointOffset.x, skin.firePointOffset.y, firePoint.localPosition.z);
        }

        // 4. Cập nhật cache của PlayerHealth để hiệu ứng chớp trắng flash nhận đúng sprite mới
        PlayerHealth health = GetComponent<PlayerHealth>() ?? GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            health.CacheSpriteRenderers(true);
        }
    }

    /// <summary>
    /// Ghi nhận vị trí và tỷ lệ hiện tại của các Visual slot trên Scene vào cấu hình Skin được chỉ định.
    /// Giúp người dùng chỉnh bằng công cụ Move/Scale của Unity trên Scene View rồi bấm Lưu 1 chạm!
    /// </summary>
    public void CaptureCurrentSceneTransforms(int skinIndex)
    {
        if (skinIndex < 0 || skinIndex >= skins.Length) return;
        PlayerSkinConfig skin = skins[skinIndex];
        if (skin == null) return;

        AutoEnsureVisualSlots();

        if (bodyVisual != null)
        {
            skin.body.positionOffset = bodyVisual.localPosition;
            skin.body.scaleMultiplier = bodyVisual.localScale;
            skin.body.rotationOffset = bodyVisual.localEulerAngles.z;
        }

        if (gunVisual != null)
        {
            skin.gun.positionOffset = gunVisual.localPosition;
            skin.gun.scaleMultiplier = gunVisual.localScale;
            skin.gun.rotationOffset = gunVisual.localEulerAngles.z;
        }

        if (armVisual != null)
        {
            skin.arm.positionOffset = armVisual.localPosition;
            skin.arm.scaleMultiplier = armVisual.localScale;
            skin.arm.rotationOffset = armVisual.localEulerAngles.z;
        }

        if (leg1Visual != null)
        {
            skin.leg1.positionOffset = leg1Visual.localPosition;
            skin.leg1.scaleMultiplier = leg1Visual.localScale;
            skin.leg1.rotationOffset = leg1Visual.localEulerAngles.z;
        }

        if (leg2Visual != null)
        {
            skin.leg2.positionOffset = leg2Visual.localPosition;
            skin.leg2.scaleMultiplier = leg2Visual.localScale;
            skin.leg2.rotationOffset = leg2Visual.localEulerAngles.z;
        }

        if (firePoint != null)
        {
            skin.firePointOffset = firePoint.localPosition;
        }
    }

    /// <summary>
    /// Tự động dò tìm các Bone trong Hierarchy và tạo ra cấu trúc Visual Slot nếu chưa có.
    /// Tách rời SpriteRenderer khỏi Bone để Animator không ghi đè vị trí/kích thước do người dùng tùy biến.
    /// </summary>
    public void AutoEnsureVisualSlots()
    {
        Transform root = transform;

        // 1. Thân
        Transform boneBody = root.Find("thân");
        if (boneBody != null)
        {
            bodyVisual = EnsureVisualSlotChild(boneBody, "BodyVisual", ref bodyRenderer);
        }

        // 2. GunPivot & Súng
        Transform boneGun = boneBody != null ? boneBody.Find("GunPivot/GunSprite") : null;
        if (boneGun == null)
        {
            foreach (Transform tr in root.GetComponentsInChildren<Transform>(true))
            {
                if (tr.name.Equals("GunSprite", StringComparison.OrdinalIgnoreCase))
                {
                    boneGun = tr;
                    break;
                }
            }
        }

        if (boneGun != null)
        {
            gunVisual = EnsureVisualSlotChild(boneGun, "GunVisual", ref gunRenderer);
        }

        // 3. Tay
        Transform boneArm = boneBody != null ? boneBody.Find("GunPivot/Tay") : null;
        if (boneArm == null)
        {
            foreach (Transform tr in root.GetComponentsInChildren<Transform>(true))
            {
                if (tr.name.Equals("Tay", StringComparison.OrdinalIgnoreCase))
                {
                    boneArm = tr;
                    break;
                }
            }
        }

        if (boneArm != null)
        {
            armVisual = EnsureVisualSlotChild(boneArm, "ArmVisual", ref armRenderer);
        }

        // 4. Chân 1 (Chan 1)
        Transform boneLeg1 = root.Find("Chan 1");
        if (boneLeg1 != null)
        {
            leg1Visual = EnsureVisualSlotChild(boneLeg1, "Leg1Visual", ref leg1Renderer);
        }

        // 5. Chân 2 (chan 2)
        Transform boneLeg2 = root.Find("chan 2");
        if (boneLeg2 != null)
        {
            leg2Visual = EnsureVisualSlotChild(boneLeg2, "Leg2Visual", ref leg2Renderer);
        }

        // 6. Nòng súng (FirePoint)
        if (firePoint == null)
        {
            PlayerAutoShooter shooter = GetComponent<PlayerAutoShooter>() ?? GetComponentInChildren<PlayerAutoShooter>();
            if (shooter != null && shooter.FirePoint != null && shooter.FirePoint != shooter.transform)
            {
                firePoint = shooter.FirePoint;
            }
            else
            {
                Transform t = gunVisual != null ? gunVisual.Find("FirePoint") : null;
                if (t == null && boneGun != null) t = boneGun.Find("FirePoint");
                if (t != null) firePoint = t;
            }
        }

        // Đảm bảo FirePoint là con của gunVisual để khi kéo súng nòng súng tự động di chuyển theo
        if (firePoint != null && gunVisual != null && firePoint.parent != gunVisual)
        {
            firePoint.SetParent(gunVisual, true);
        }

        // 7. Đồng bộ sang PlayerAutoShooter nếu có
        PlayerAutoShooter autoShooter = GetComponent<PlayerAutoShooter>() ?? GetComponentInChildren<PlayerAutoShooter>();
        if (autoShooter != null)
        {
            autoShooter.SetRenderers(gunRenderer, new SpriteRenderer[] { bodyRenderer, leg1Renderer, leg2Renderer });
        }
    }

    private Transform EnsureVisualSlotChild(Transform bone, string visualSlotName, ref SpriteRenderer targetRenderer)
    {
        Transform visual = bone.Find(visualSlotName);
        SpriteRenderer boneRenderer = bone.GetComponent<SpriteRenderer>();

        if (visual == null)
        {
            GameObject visualObj = new GameObject(visualSlotName);
            visual = visualObj.transform;
            visual.SetParent(bone, false);
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one;

            SpriteRenderer newRenderer = visualObj.AddComponent<SpriteRenderer>();

            // Chuyển toàn bộ cấu hình từ SpriteRenderer trên bone sang visual slot
            if (boneRenderer != null)
            {
                newRenderer.sprite = boneRenderer.sprite;
                newRenderer.color = boneRenderer.color;
                newRenderer.material = boneRenderer.material;
                newRenderer.sortingLayerID = boneRenderer.sortingLayerID;
                newRenderer.sortingOrder = boneRenderer.sortingOrder;
                newRenderer.flipX = boneRenderer.flipX;
                newRenderer.flipY = boneRenderer.flipY;
                newRenderer.drawMode = boneRenderer.drawMode;
                newRenderer.maskInteraction = boneRenderer.maskInteraction;
                newRenderer.spriteSortPoint = boneRenderer.spriteSortPoint;

                // Tắt / gỡ bỏ SpriteRenderer cũ trên bone để tránh trùng lặp
                boneRenderer.enabled = false;
            }

            targetRenderer = newRenderer;
        }
        else
        {
            if (targetRenderer == null)
            {
                targetRenderer = visual.GetComponent<SpriteRenderer>();
                if (targetRenderer == null) targetRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
            }

            if (boneRenderer != null && boneRenderer.enabled)
            {
                boneRenderer.enabled = false;
            }
        }

        return visual;
    }

    private void OnDrawGizmosSelected()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(firePoint.position, 0.12f);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(firePoint.position, firePoint.right * 0.8f);
        }
    }
}
