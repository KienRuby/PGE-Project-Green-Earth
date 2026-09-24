using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Đại diện cho một ô (Cell) trên lưới phòng thủ 7x4:
/// - Khi trống: Hiển thị ảnh Tile_Placement_Plus với dấu cộng [+].
/// - Khi đặt công trình: Hiển thị Tháp pháo (Turret), Giường (Core Bed), hoặc Trụ điện (Pawn Tower).
/// - Có huy hiệu nâng cấp Icon_Upgrade_Circle khi công trình đủ điều kiện hoặc sẵn sàng.
/// - Nhận sự kiện chạm (Click/Tap) để mở popup xây mới hoặc nâng cấp.
/// </summary>
public class TowerDefGridCell : MonoBehaviour
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

    private TowerDefTurret turretComp;
    private TowerDefGenerator generatorComp;

    public int Row => row;
    public int Col => col;
    public TowerDefStructureType CurrentType => currentType;
    public int StructureLevel => structureLevel;
    public bool IsOccupied => currentType != TowerDefStructureType.None;
    public TowerDefTurret Turret => turretComp;
    public TowerDefGenerator Generator => generatorComp;

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
        if (structureImage != null && baseSprite != null)
        {
            structureImage.sprite = baseSprite;
            structureImage.gameObject.SetActive(true);
        }

        if (type == TowerDefStructureType.Turret)
        {
            if (gunTransform != null)
            {
                gunTransform.gameObject.SetActive(true);
                Image gunImg = gunTransform.GetComponent<Image>();
                if (gunImg != null && gunSprite != null) gunImg.sprite = gunSprite;
            }

            if (turretComp == null) turretComp = gameObject.AddComponent<TowerDefTurret>();
            turretComp.Setup(gunTransform, structureImage, gunTransform != null ? gunTransform.GetComponent<Image>() : null, upgradeIcon);
        }
        else if (type == TowerDefStructureType.CoreBed || type == TowerDefStructureType.EnergyGenerator)
        {
            if (gunTransform != null) gunTransform.gameObject.SetActive(false);
            if (generatorComp == null) generatorComp = gameObject.AddComponent<TowerDefGenerator>();
            generatorComp.Setup(type, initialLevel, structureImage, upgradeIcon, type == TowerDefStructureType.CoreBed ? 1.0f : 2.0f);
        }

        SetUpgradeBadge(true);
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
