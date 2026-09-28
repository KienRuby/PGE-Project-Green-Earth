using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RobotPetDetailModal : MonoBehaviour
{
    private Image cardImage;
    private Image petIcon;
    private TMP_Text nameText;
    private TMP_Text descriptionText;
    private TMP_Text levelText;
    private TMP_Text slotText;
    private TMP_Text equipText;
    private Action equipAction;

    public static RobotPetDetailModal GetOrCreate(Canvas canvas, TMP_FontAsset font, Material textMaterial)
    {
        Transform existing = canvas.transform.Find("RobotPetDetailModal");
        if (existing != null) return existing.GetComponent<RobotPetDetailModal>();

        GameObject root = new GameObject("RobotPetDetailModal", typeof(RectTransform), typeof(Image), typeof(Button), typeof(RobotPetDetailModal));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
        Image dim = root.GetComponent<Image>();
        dim.color = new Color32(5, 20, 30, 225);
        root.GetComponent<Button>().onClick.AddListener(() => root.SetActive(false));

        RectTransform box = AddRect(root.transform, "ModalBox", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 1380f));
        Image panel = box.gameObject.AddComponent<Image>();
        panel.color = new Color32(3, 52, 74, 255);
        Outline border = box.gameObject.AddComponent<Outline>();
        border.effectColor = new Color32(205, 165, 240, 255);
        border.effectDistance = new Vector2(4f, 4f);

        RobotPetDetailModal modal = root.GetComponent<RobotPetDetailModal>();
        Button close = AddButton(box, "CloseBtn", new Vector2(0.93f, 0.96f), new Vector2(54f, 54f), new Color32(170, 53, 37, 255));
        close.onClick.AddListener(() => root.SetActive(false));
        AddText(close.transform, "Label", "X", 32f, font, textMaterial);

        RectTransform card = AddRect(box, "TopCard", new Vector2(0.5f, 0.81f), Vector2.zero, new Vector2(285f, 335f));
        modal.cardImage = card.gameObject.AddComponent<Image>();
        modal.cardImage.preserveAspect = true;
        RectTransform icon = AddRect(card, "PetIcon", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(125f, 125f));
        modal.petIcon = icon.gameObject.AddComponent<Image>();
        modal.petIcon.preserveAspect = true;

        modal.nameText = AddText(box, "Name", "", 44f, font, textMaterial, new Vector2(0.5f, 0.665f), new Vector2(800f, 56f));
        AddText(box, "Tier", "ROBOT PET", 28f, font, textMaterial, new Vector2(0.5f, 0.625f), new Vector2(800f, 40f));
        modal.descriptionText = AddText(box, "Description", "", 26f, font, textMaterial, new Vector2(0.5f, 0.58f), new Vector2(800f, 95f));
        modal.levelText = AddText(box, "Level", "", 28f, font, textMaterial, new Vector2(0.5f, 0.51f), new Vector2(800f, 44f));
        modal.slotText = AddText(box, "Slot", "", 26f, font, textMaterial, new Vector2(0.5f, 0.43f), new Vector2(800f, 44f));

        Button equip = AddButton(box, "EquipBtn", new Vector2(0.26f, 0.12f), new Vector2(280f, 135f), new Color32(242, 193, 78, 255));
        modal.equipText = AddText(equip.transform, "Label", "EQUIP", 36f, font, textMaterial);
        equip.onClick.AddListener(modal.Equip);
        Button done = AddButton(box, "DoneBtn", new Vector2(0.71f, 0.12f), new Vector2(360f, 92f), new Color32(73, 198, 79, 255));
        AddText(done.transform, "Label", "CLOSE", 28f, font, textMaterial);
        done.onClick.AddListener(() => root.SetActive(false));

        root.SetActive(false);
        return modal;
    }

    public void Show(PetData pet, int activeSlot, Action onEquip)
    {
        equipAction = onEquip;
        nameText.text = pet.petName;
        descriptionText.text = pet.description;
        levelText.text = pet.levelText;
        int slot = PetService.GetEquippedSlotIndex(pet.id);
        slotText.text = slot >= 0 ? $"EQUIPPED IN SLOT {slot + 1}" : "READY TO EQUIP";
        equipText.text = slot == activeSlot ? "UNEQUIP" : "EQUIP";
        bool hasCard = pet.cardSprite != null && !pet.cardSprite.name.Contains("Empty");
        cardImage.sprite = hasCard ? pet.cardSprite : pet.frameSprite;
        cardImage.color = cardImage.sprite == null ? new Color32(25, 146, 117, 255) : Color.white;
        petIcon.sprite = pet.petIcon;
        petIcon.enabled = !hasCard && pet.petIcon != null;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    private void Equip()
    {
        equipAction?.Invoke();
        gameObject.SetActive(false);
    }

    private static RectTransform AddRect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Button AddButton(Transform parent, string name, Vector2 anchor, Vector2 size, Color color)
    {
        RectTransform rect = AddRect(parent, name, anchor, Vector2.zero, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        Shadow shadow = rect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(0, 14, 24, 210);
        shadow.effectDistance = new Vector2(6f, -6f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static TMP_Text AddText(Transform parent, string name, string value, float size, TMP_FontAsset font, Material material, Vector2? anchor = null, Vector2? dimensions = null)
    {
        RectTransform rect = AddRect(parent, name, anchor ?? new Vector2(0.5f, 0.5f), Vector2.zero, dimensions ?? new Vector2(300f, 70f));
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        if (material != null) text.fontSharedMaterial = material;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }
}
