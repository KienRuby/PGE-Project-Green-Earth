using System;
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
public class TowerDefGridCell : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
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
            if (gunTransform != null) gunTransform.gameObject.SetActive(false);
            if (generatorComp == null) generatorComp = gameObject.AddComponent<TowerDefGenerator>();
            generatorComp.Setup(type, initialLevel, structureImage, upgradeIcon, type == TowerDefStructureType.CoreBed ? 1.0f : 2.0f);
        }

        SetUpgradeBadge(true);
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

        // Place the SpriteRenderers just in front of the camera-space UI plane.
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
        if (structureImage != null) structureImage.gameObject.SetActive(false);
        if (gunTransform != null) gunTransform.gameObject.SetActive(false);
    }

    public bool TryMoveTurretTo(TowerDefGridCell destination)
    {
        if (destination == null || destination == this || destination.IsOccupied ||
            currentType != TowerDefStructureType.Turret || structureRoot == null ||
            destination.structureRoot == null)
            return false;

        if (turretComp == null) turretComp = GetComponent<TowerDefTurret>();
        if (turretComp == null) return false;

        bool badgeActive = dragPreview != null ? dragBadgeWasActive :
            upgradeIcon != null && upgradeIcon.activeSelf;
        Image sourceGunImage = gunTransform != null ? gunTransform.GetComponent<Image>() : null;
        Sprite gunSprite = sourceGunImage != null ? sourceGunImage.sprite : null;
        if (turretInstance != null && destination.structureRoot != null)
        {
            destination.turretInstance = turretInstance;
            turretInstance.transform.SetParent(destination.structureRoot.transform, false);
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

        turretComp.enabled = false;
        if (Application.isPlaying) Destroy(turretComp);
        else DestroyImmediate(turretComp);
        turretComp = null;
        currentType = TowerDefStructureType.None;
        structureLevel = 1;
        structureRoot.SetActive(false);
        SetUpgradeBadge(false);
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

        dragPreview = Instantiate(structureRoot, canvas.transform, false);
        dragPreview.name = "Turret Drag Preview";
        dragPreview.SetActive(true);
        dragPreview.transform.SetAsLastSibling();
        CanvasGroup group = dragPreview.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
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

        GameObject hovered = eventData.pointerCurrentRaycast.gameObject;
        TowerDefGridCell destination = hovered != null ? hovered.GetComponentInParent<TowerDefGridCell>() : null;
        bool moved = TowerDefGameManager.Instance != null &&
                     TowerDefGameManager.Instance.TryMoveTurret(this, destination);
        if (!moved)
        {
            if (structureRoot != null) structureRoot.SetActive(true);
            if (upgradeIcon != null) upgradeIcon.SetActive(dragBadgeWasActive);
            if (turretComp != null) turretComp.enabled = true;
        }

        Destroy(dragPreview);
        dragPreview = null;
    }

    private void OnDisable()
    {
        if (dragPreview == null) return;
        if (Application.isPlaying) Destroy(dragPreview);
        else DestroyImmediate(dragPreview);
        dragPreview = null;
        if (structureRoot != null && currentType == TowerDefStructureType.Turret)
            structureRoot.SetActive(true);
        if (upgradeIcon != null && currentType == TowerDefStructureType.Turret)
            upgradeIcon.SetActive(dragBadgeWasActive);
        if (currentType == TowerDefStructureType.Turret && turretComp != null)
            turretComp.enabled = true;
    }

    public void UpgradeCurrentStructure()
    {
        structureLevel++;
        if (turretComp != null)
        {
            // Upgraded via turretComp
        }
        else if (generatorComp != null)
        {
            // Upgraded via generatorComp
        }
    }

    public void SetUpgradeBadge(bool active)
    {
        if (upgradeIcon != null)
        {
            upgradeIcon.SetActive(active);
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
