using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Đại diện cho một ô (Cell) trên lưới phòng thủ 7x4:
/// - Khi trống: Hiển thị ảnh Tile_Placement_Plus với dấu cộng [+].
/// - Khi đặt công trình: Hiển thị Tháp pháo (Turret), Giường (Core Bed), hoặc Trụ điện (Pawn Tower).
/// - Có huy hiệu nâng cấp Icon_Upgrade_Circle khi công trình đủ điều kiện hoặc sẵn sàng.
/// - Nhận sự kiện chạm (Click/Tap) để mở popup xây mới hoặc nâng cấp.
/// </summary>
public class TowerDefGridCell : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("Grid Position")]
    [SerializeField] private int row;
    [SerializeField] private int col;

    [Header("Current Structure State")]
    [SerializeField] private TowerDefStructureType currentType = TowerDefStructureType.None;
    [SerializeField] private int structureLevel = 1;

    [Header("References")]
    [SerializeField] private Image slotTileImage;
    [SerializeField] private Button cellButton;
    [SerializeField] private GameObject structureRoot;
    [SerializeField] private Image structureImage;
    [SerializeField] private Transform gunTransform;
    [SerializeField] private GameObject upgradeIcon;
    [SerializeField] private GameObject turretPrefab;

    private TowerDefTurret turretComp;
    private TowerDefGenerator generatorComp;
    private GameObject dragPreview;
    private bool dragBadgeWasActive;
    private GameObject turretInstance;

    public int Row => row;
    public int Col => col;
    public TowerDefStructureType CurrentType => currentType;
    public int StructureLevel => structureLevel;
    public bool IsOccupied => currentType != TowerDefStructureType.None;
    public TowerDefTurret Turret => turretComp;
    public TowerDefGenerator Generator => generatorComp;
    public GameObject TurretPrefabInstance => turretInstance;

    public event Action<TowerDefGridCell> OnCellClicked;

    private void Awake()
    {
        if (cellButton != null)
        {
            cellButton.onClick.RemoveListener(HandleClick);
            cellButton.onClick.AddListener(HandleClick);
        }
    }

    public void SetupCell(
        int r,
        int c,
        Image slotImg,
        Button btn,
        GameObject sRoot,
        Image sImg,
        Transform gunTr,
        GameObject upIcon)
    {
        row = r;
        col = c;
        slotTileImage = slotImg;
        cellButton = btn;
        structureRoot = sRoot;
        structureImage = sImg;
        gunTransform = gunTr;
        upgradeIcon = upIcon;

        if (cellButton != null)
        {
            cellButton.onClick.RemoveListener(HandleClick);
            cellButton.onClick.AddListener(HandleClick);
        }

        UpdateVisuals();
    }

    public void PlaceStructure(TowerDefStructureType type, Sprite baseSprite, Sprite gunSprite = null, int initialLevel = 1)
    {
        currentType = type;
        structureLevel = initialLevel;

        if (structureRoot != null) structureRoot.SetActive(true);
        if (structureImage != null && baseSprite != null && type != TowerDefStructureType.Turret)
        {
            structureImage.sprite = baseSprite;
            structureImage.gameObject.SetActive(true);
        }

        if (type == TowerDefStructureType.Turret)
        {
            if (generatorComp != null)
            {
                if (Application.isPlaying) Destroy(generatorComp);
                else DestroyImmediate(generatorComp);
                generatorComp = null;
            }

            if (turretPrefab != null)
            {
                EnsureTurretPrefabInstance();
                HideLegacyTurretImages();
            }
            else if (gunTransform != null)
            {
                gunTransform.gameObject.SetActive(true);
                Image gunImg = gunTransform.GetComponent<Image>();
                if (gunImg != null && gunSprite != null) gunImg.sprite = gunSprite;
            }

            if (turretComp == null) turretComp = gameObject.AddComponent<TowerDefTurret>();
            Transform aim = turretInstance != null ? turretInstance.transform.Find("AimPivot") : gunTransform;
            turretComp.Setup(aim, turretInstance != null ? null : structureImage,
                turretInstance == null && gunTransform != null ? gunTransform.GetComponent<Image>() : null,
                upgradeIcon);
            if (turretInstance != null)
                turretComp.SetProjectilePrefab(turretInstance.GetComponent<GunTurret>()?.ProjectilePrefab);
        }
        else if (type == TowerDefStructureType.CoreBed || type == TowerDefStructureType.EnergyGenerator)
        {
            if (turretComp != null)
            {
                if (Application.isPlaying) Destroy(turretComp);
                else DestroyImmediate(turretComp);
                turretComp = null;
            }
            if (turretInstance != null)
            {
                if (Application.isPlaying) Destroy(turretInstance);
                else DestroyImmediate(turretInstance);
                turretInstance = null;
            }

            if (gunTransform != null) gunTransform.gameObject.SetActive(false);
            if (generatorComp == null) generatorComp = gameObject.AddComponent<TowerDefGenerator>();
            generatorComp.Setup(type, initialLevel, structureImage, upgradeIcon, type == TowerDefStructureType.CoreBed ? 1.0f : 2.0f);
        }

        RefreshUpgradeBadge(TowerDefGameManager.Instance != null ? TowerDefGameManager.Instance.Gold : 0);
    }

    public void ApplyTurretVisual(Sprite baseSprite, Sprite gunSprite, float gunAngleOffset)
    {
        if (currentType != TowerDefStructureType.Turret) return;
        if (turretInstance != null)
        {
            HideLegacyTurretImages();
            if (turretComp == null) turretComp = GetComponent<TowerDefTurret>();
            if (turretComp != null)
            {
                turretComp.Setup(turretInstance.transform.Find("AimPivot"), null, null, upgradeIcon);
                turretComp.SetGunVisualAngleOffset(0f);
                turretComp.SetProjectilePrefab(turretInstance.GetComponent<GunTurret>()?.ProjectilePrefab);
            }
            return;
        }
        if (structureImage != null && baseSprite != null) structureImage.sprite = baseSprite;
        Image gunImage = gunTransform != null ? gunTransform.GetComponent<Image>() : null;
        if (gunImage != null && gunSprite != null) gunImage.sprite = gunSprite;
        if (gunTransform is RectTransform gunRect && structureRoot != null &&
            structureRoot.transform is RectTransform structureRect)
        {
            gunRect.anchoredPosition = new Vector2(0f, structureRect.sizeDelta.y * 0.26f);
        }
        if (turretComp == null) turretComp = GetComponent<TowerDefTurret>();
        if (turretComp != null) turretComp.SetGunVisualAngleOffset(gunAngleOffset);
    }

    public void ConfigureTurretPrefab(GameObject prefab)
    {
        turretPrefab = prefab;
        if (currentType == TowerDefStructureType.Turret)
            EnsureTurretPrefabInstance();
    }

    private void EnsureTurretPrefabInstance()
    {
        if (turretPrefab == null || structureRoot == null) return;

        if (turretInstance == null)
        {
            GunTurret existing = structureRoot.GetComponentInChildren<GunTurret>(true);
            if (existing != null) turretInstance = existing.gameObject;
        }

        // Đảm bảo trong structureRoot chỉ có duy nhất tối đa 1 instance GunTurret
        GunTurret[] allExisting = structureRoot.GetComponentsInChildren<GunTurret>(true);
        if (allExisting != null && allExisting.Length > 0)
        {
            if (turretInstance == null) turretInstance = allExisting[0].gameObject;
            for (int i = 0; i < allExisting.Length; i++)
            {
                if (allExisting[i] != null && allExisting[i].gameObject != turretInstance)
                {
                    if (Application.isPlaying) Destroy(allExisting[i].gameObject);
                    else DestroyImmediate(allExisting[i].gameObject);
                }
            }
        }

        if (turretInstance == null)
        {
            bool wasActive = structureRoot.activeSelf;
            structureRoot.SetActive(false);
#if UNITY_EDITOR
            if (!Application.isPlaying && UnityEditor.PrefabUtility.IsPartOfPrefabAsset(turretPrefab))
                turretInstance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(turretPrefab);
            else
#endif
                turretInstance = Instantiate(turretPrefab);
            turretInstance.transform.SetParent(structureRoot.transform, false);
            structureRoot.SetActive(wasActive);
        }

        // Căn chuẩn tâm ô phòng thủ (0, 0)
        turretInstance.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        turretInstance.transform.localRotation = Quaternion.identity;
        RectTransform rootRect = structureRoot.transform as RectTransform;
        Transform basePart = turretInstance.transform.Find("BaseSprite");
        Transform gunPart = turretInstance.transform.Find("AimPivot/GunSprite");
        SpriteRenderer baseRenderer = basePart != null ? basePart.GetComponent<SpriteRenderer>() : null;
        SpriteRenderer gunRenderer = gunPart != null ? gunPart.GetComponent<SpriteRenderer>() : null;
        if (rootRect != null && baseRenderer != null && baseRenderer.sprite != null)
        {
            Vector2 spriteSize = baseRenderer.sprite.bounds.size;
            Vector3 partScale = basePart.localScale;
            float width = spriteSize.x * Mathf.Abs(partScale.x);
            float height = spriteSize.y * Mathf.Abs(partScale.y);
            float scale = Mathf.Min(rootRect.rect.width * 0.84f / width,
                rootRect.rect.height * 0.78f / height);
            turretInstance.transform.localScale = Vector3.one * scale;
        }
        turretInstance.transform.SetAsFirstSibling();

        // TowerDef combat is handled by TowerDefTurret; the prefab SpriteRenderers are the visual.
        GunTurret prefabBehaviour = turretInstance.GetComponent<GunTurret>();
        if (prefabBehaviour != null) prefabBehaviour.enabled = false;
        foreach (SpriteRenderer renderer in turretInstance.GetComponentsInChildren<SpriteRenderer>(true))
            renderer.enabled = true;
        if (baseRenderer != null) baseRenderer.sortingOrder = 10;
        if (gunRenderer != null) gunRenderer.sortingOrder = 11;
        HideLegacyTurretImages();
        if (turretComp == null) turretComp = GetComponent<TowerDefTurret>();
        if (turretComp != null)
        {
            turretComp.Setup(turretInstance.transform.Find("AimPivot"), null, null, upgradeIcon);
            turretComp.SetProjectilePrefab(prefabBehaviour != null ? prefabBehaviour.ProjectilePrefab : null);
        }
        if (upgradeIcon != null && upgradeIcon.transform is RectTransform badge)
        {
            badge.anchorMin = badge.anchorMax = new Vector2(1f, 1f);
            badge.pivot = new Vector2(1f, 1f);
            badge.anchoredPosition = new Vector2(-4f, -4f);
        }
    }

    private void HideLegacyTurretImages()
    {
        if (structureImage != null)
        {
            structureImage.sprite = null;
            structureImage.gameObject.SetActive(false);
        }
        if (gunTransform != null)
        {
            Image gunImg = gunTransform.GetComponent<Image>();
            if (gunImg != null) gunImg.sprite = null;
            gunTransform.gameObject.SetActive(false);
        }
    }

    public void ClearStructure()
    {
        currentType = TowerDefStructureType.None;
        structureLevel = 1;

        if (turretComp != null)
        {
            turretComp.enabled = false;
            if (Application.isPlaying) Destroy(turretComp);
            else DestroyImmediate(turretComp);
            turretComp = null;
        }

        if (generatorComp != null)
        {
            if (Application.isPlaying) Destroy(generatorComp);
            else DestroyImmediate(generatorComp);
            generatorComp = null;
        }

        if (structureRoot != null)
        {
            for (int i = structureRoot.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = structureRoot.transform.GetChild(i);
                if (child.gameObject != structureImage?.gameObject &&
                    child.gameObject != gunTransform?.gameObject)
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }
            structureRoot.SetActive(false);
        }

        turretInstance = null;
        HideLegacyTurretImages();
        SetUpgradeBadge(false);
        UpdateVisuals();
    }

    public bool TryMoveTurretTo(TowerDefGridCell destination)
    {
        // 1 ô tối đa chỉ có 1 công trình; không được di chuyển đè lên ô đã có
        if (destination == null || destination == this || destination.IsOccupied ||
            currentType != TowerDefStructureType.Turret || structureRoot == null ||
            destination.structureRoot == null)
            return false;

        if (turretComp == null) turretComp = GetComponent<TowerDefTurret>();
        if (turretComp == null) return false;

        // Đảm bảo tìm thấy turretInstance ở ô nguồn nếu có
        if (turretInstance == null)
        {
            GunTurret existing = structureRoot.GetComponentInChildren<GunTurret>(true);
            if (existing != null) turretInstance = existing.gameObject;
        }

        bool badgeActive = dragPreview != null ? dragBadgeWasActive :
            upgradeIcon != null && upgradeIcon.activeSelf;
        Image sourceGunImage = gunTransform != null ? gunTransform.GetComponent<Image>() : null;
        Sprite gunSprite = sourceGunImage != null ? sourceGunImage.sprite : null;

        // Dọn sạch destination.structureRoot trước khi nhận tháp mới để đảm bảo ô đích chỉ có 1 tháp
        for (int i = destination.structureRoot.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = destination.structureRoot.transform.GetChild(i);
            if (child.gameObject != destination.structureImage?.gameObject &&
                child.gameObject != destination.gunTransform?.gameObject)
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        if (turretInstance != null)
        {
            destination.turretInstance = turretInstance;
            turretInstance.transform.SetParent(destination.structureRoot.transform, false);
            turretInstance.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            turretInstance.transform.localRotation = Quaternion.identity;
            turretInstance = null;
        }

        destination.ConfigureTurretPrefab(turretPrefab);
        destination.PlaceStructure(TowerDefStructureType.Turret,
            structureImage != null ? structureImage.sprite : null, gunSprite, structureLevel);
        if (destination.turretComp == null) return false;

        destination.turretComp.CopyProgressFrom(turretComp);
        destination.SetUpgradeBadge(badgeActive);
        if (destination.turretInstance == null && gunTransform is RectTransform sourceGunRect &&
            destination.gunTransform is RectTransform destGunRect)
        {
            destGunRect.anchoredPosition = sourceGunRect.anchoredPosition;
            destGunRect.localRotation = sourceGunRect.localRotation;
        }

        ClearStructure();
        return true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentType != TowerDefStructureType.Turret || structureRoot == null ||
            eventData.button != PointerEventData.InputButton.Left)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        if (turretComp == null) turretComp = GetComponent<TowerDefTurret>();
        if (turretComp == null) return;

        // Dọn preview cũ nếu có
        if (dragPreview != null)
        {
            if (Application.isPlaying) Destroy(dragPreview);
            else DestroyImmediate(dragPreview);
            dragPreview = null;
        }

        dragPreview = Instantiate(structureRoot, canvas.transform, false);
        dragPreview.name = "Turret Drag Preview";
        dragPreview.SetActive(true);
        dragPreview.transform.SetAsLastSibling();

        CanvasGroup group = dragPreview.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        foreach (Collider2D col in dragPreview.GetComponentsInChildren<Collider2D>(true))
            col.enabled = false;
        foreach (Graphic g in dragPreview.GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;
        foreach (GunTurret gt in dragPreview.GetComponentsInChildren<GunTurret>(true))
            gt.enabled = false;

        dragBadgeWasActive = upgradeIcon != null && upgradeIcon.activeSelf;
        if (upgradeIcon != null) upgradeIcon.SetActive(false);
        structureRoot.SetActive(false);
        turretComp.enabled = false;
        eventData.eligibleForClick = false;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragPreview == null) return;
        Canvas canvas = dragPreview.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        RectTransform previewRect = dragPreview.transform as RectTransform;
        if (canvasRect == null || previewRect == null) return;

        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, camera, out Vector2 position))
        {
            previewRect.anchorMin = previewRect.anchorMax = new Vector2(0.5f, 0.5f);
            previewRect.anchoredPosition = position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragPreview == null) return;

        try
        {
            // Chỉ xử lý nếu ô này vẫn đang chứa tháp (chưa bị OnDrop của ô đích di chuyển trước)
            if (currentType == TowerDefStructureType.Turret)
            {
                TowerDefGridCell destination = null;

                // 1. RaycastAll tìm ô GridCell
                if (EventSystem.current != null)
                {
                    List<RaycastResult> results = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(eventData, results);
                    for (int i = 0; i < results.Count; i++)
                    {
                        TowerDefGridCell cell = results[i].gameObject.GetComponentInParent<TowerDefGridCell>();
                        if (cell != null && cell != this)
                        {
                            destination = cell;
                            break;
                        }
                    }
                }

                // 2. Fallback pointerCurrentRaycast
                if (destination == null && eventData.pointerCurrentRaycast.gameObject != null)
                {
                    TowerDefGridCell cell = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<TowerDefGridCell>();
                    if (cell != null && cell != this)
                    {
                        destination = cell;
                    }
                }

                // 3. Fallback tìm theo tọa độ màn hình gần nhất (hỗ trợ thả lệch mép ô)
                if (destination == null && TowerDefGameManager.Instance != null)
                {
                    TowerDefGridCell cell = TowerDefGameManager.Instance.FindClosestGridCell(eventData.position);
                    if (cell != null && cell != this)
                    {
                        destination = cell;
                    }
                }

                bool moved = false;
                if (destination != null && !destination.IsOccupied)
                {
                    moved = TowerDefGameManager.Instance != null &&
                            TowerDefGameManager.Instance.TryMoveTurret(this, destination);
                }

                if (!moved && currentType == TowerDefStructureType.Turret)
                {
                    if (structureRoot != null) structureRoot.SetActive(true);
                    if (upgradeIcon != null) upgradeIcon.SetActive(dragBadgeWasActive);
                    if (turretComp != null) turretComp.enabled = true;
                }
            }
        }
        finally
        {
            // Luôn luôn hủy dragPreview trong finally để không bao giờ bị kẹt lại trên màn hình
            if (dragPreview != null)
            {
                if (Application.isPlaying) Destroy(dragPreview);
                else DestroyImmediate(dragPreview);
                dragPreview = null;
            }
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null) return;
        TowerDefGridCell source = eventData.pointerDrag.GetComponent<TowerDefGridCell>();
        if (source != null && source != this && !IsOccupied && source.CurrentType == TowerDefStructureType.Turret)
        {
            TowerDefGameManager.Instance?.TryMoveTurret(source, this);
        }
    }

    private void OnDisable()
    {
        if (dragPreview != null)
        {
            if (Application.isPlaying) Destroy(dragPreview);
            else DestroyImmediate(dragPreview);
            dragPreview = null;
        }
        if (structureRoot != null && currentType == TowerDefStructureType.Turret)
            structureRoot.SetActive(true);
        if (upgradeIcon != null && currentType == TowerDefStructureType.Turret)
            upgradeIcon.SetActive(dragBadgeWasActive);
        if (currentType == TowerDefStructureType.Turret && turretComp != null)
            turretComp.enabled = true;
    }

    public void UpgradeCurrentStructure()
    {
        if (turretComp != null)
        {
            structureLevel = turretComp.TurretLevel;
            turretComp.UpdateTurretVisual();
        }
        else if (generatorComp != null)
        {
            structureLevel = generatorComp.StructureLevel;
        }
        else
        {
            structureLevel++;
        }
        RefreshUpgradeBadge(TowerDefGameManager.Instance != null ? TowerDefGameManager.Instance.Gold : 0);
    }

    public void SetUpgradeBadge(bool active)
    {
        if (upgradeIcon != null)
        {
            upgradeIcon.SetActive(active);
        }
    }

    public void RefreshUpgradeBadge(int currentGold)
    {
        if (!IsOccupied)
        {
            SetUpgradeBadge(false);
            return;
        }

        if (generatorComp != null)
        {
            generatorComp.RefreshUpgradeBadge(currentGold);
        }
        else if (turretComp != null)
        {
            bool can = (currentGold >= turretComp.UpgradeCost) && !turretComp.IsMaxLevel;
            SetUpgradeBadge(can);
        }
    }

    private void UpdateVisuals()
    {
        if (structureRoot != null)
        {
            structureRoot.SetActive(IsOccupied);
        }
        if (upgradeIcon != null)
        {
            upgradeIcon.SetActive(IsOccupied);
        }
    }

    private void HandleClick()
    {
        OnCellClicked?.Invoke(this);
        if (!IsOccupied)
        {
            TowerDefGameManager.Instance?.OpenBuildPopup(this);
        }
        else
        {
            TowerDefGameManager.Instance?.OpenStructureUpgradePopup(this);
        }
    }
}
