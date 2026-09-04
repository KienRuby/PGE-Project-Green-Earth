using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LabStatTooltip : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private RectTransform arrowPointer;
    [SerializeField] private Image arrowPointerImage;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private Button backgroundCloseButton;

    [Header("Layout Settings")]
    [SerializeField] private float[] rowYPositions = new float[] { -250f, -485f, -715f, -950f };
    [SerializeField] private float[] colXOffsets = new float[] { -348f, -116f, 116f, 348f };

    private int currentSlotIndex = -1;

    public bool IsShowing => gameObject.activeSelf;
    public int CurrentSlotIndex => currentSlotIndex;

    private void Awake()
    {
        if (panelRect == null)
        {
            panelRect = GetComponent<RectTransform>();
        }

        if (backgroundCloseButton != null)
        {
            backgroundCloseButton.onClick.AddListener(Hide);
        }
    }

    public void Show(int slotIndex, RectTransform slotRect, string statName, int currentLevel, bool isLocked, int maxLevel = 10)
    {
        if (IsShowing && currentSlotIndex == slotIndex)
        {
            Hide();
            return;
        }

        currentSlotIndex = slotIndex;
        gameObject.SetActive(true);

        if (detailText != null)
        {
            detailText.text = FormatStatDetail(statName, currentLevel, isLocked, maxLevel);
        }

        int row = Mathf.Clamp(slotIndex / 4, 0, 3);
        int col = Mathf.Clamp(slotIndex % 4, 0, 3);

        if (panelRect != null)
        {
            float yPos = row < rowYPositions.Length ? rowYPositions[row] : -250f;
            panelRect.anchoredPosition = new Vector2(0f, yPos);
        }

        if (arrowPointer != null)
        {
            float xPos = col < colXOffsets.Length ? colXOffsets[col] : 0f;
            arrowPointer.anchoredPosition = new Vector2(xPos, arrowPointer.anchoredPosition.y);
        }
    }

    public void Hide()
    {
        currentSlotIndex = -1;
        gameObject.SetActive(false);
    }

    public static string FormatStatDetail(string statName, int currentLevel, bool isLocked, int maxLevel = 10)
    {
        if (isLocked || currentLevel <= 0)
        {
            return "???";
        }

        StatInfo info = GetStatInfo(statName);
        int nextLevel = Mathf.Min(maxLevel, currentLevel + 1);
        bool isMax = currentLevel >= maxLevel;

        string curValStr = FormatValue(info.CalculateValue(currentLevel), info.unit);
        string nextValStr = FormatValue(info.CalculateValue(nextLevel), info.unit);

        string line1;
        if (isMax)
        {
            line1 = $"{info.displayName} +{curValStr} <color=#FFCB49>(MAX LEVEL)</color>";
        }
        else
        {
            line1 = $"{info.displayName} +{curValStr} <color=#FFCB49>(LV.{currentLevel:00}>LV.{nextLevel:00} {info.displayName} +{nextValStr})</color>";
        }

        string line2 = info.description;
        return $"{line1}\n{line2}";
    }

    private static string FormatValue(float val, string unit)
    {
        if (string.IsNullOrEmpty(unit))
        {
            return Mathf.RoundToInt(val).ToString(CultureInfo.InvariantCulture);
        }

        if (unit == "%")
        {
            return (val % 1f == 0f)
                ? $"{val:0}%"
                : $"{val:0.0}%".Replace('.', ',');
        }

        if (unit == "/sec")
        {
            return $"{val:0.0}/sec".Replace('.', ',');
        }

        return $"{val:0}{unit}";
    }

    public struct StatInfo
    {
        public string displayName;
        public float baseValue;
        public float stepPerLevel;
        public string unit;
        public string description;

        public float CalculateValue(int level)
        {
            return baseValue + level * stepPerLevel;
        }
    }

    public static StatInfo GetStatInfo(string statName)
    {
        string key = (statName ?? string.Empty).Trim().ToUpperInvariant();

        switch (key)
        {
            case "HP":
                return new StatInfo
                {
                    displayName = "HP",
                    baseValue = 200f,
                    stepPerLevel = 15f,
                    unit = "",
                    description = "Increases HP."
                };

            case "RECOVERY":
                return new StatInfo
                {
                    displayName = "Recovery",
                    baseValue = 10f,
                    stepPerLevel = 5f,
                    unit = "%",
                    description = "Increases Recovery amount."
                };

            case "AUTO RECOVERY":
            case "AUTO_RECOVERY":
            case "AUTO RECAVERY":
                return new StatInfo
                {
                    displayName = "Auto Recovery",
                    baseValue = 0.5f,
                    stepPerLevel = 0.2f,
                    unit = "/sec",
                    description = "Recovers HP automatically every second."
                };

            case "DEF":
                return new StatInfo
                {
                    displayName = "DEF",
                    baseValue = 4f,
                    stepPerLevel = 1f,
                    unit = "",
                    description = "Decreases damage taken."
                };

            case "ATK":
                return new StatInfo
                {
                    displayName = "ATK",
                    baseValue = 1.5f,
                    stepPerLevel = 1.0f,
                    unit = "%",
                    description = "Increases Attack Power."
                };

            case "CRIT RATE":
            case "CRIT_RATE":
                return new StatInfo
                {
                    displayName = "CRIT Rate",
                    baseValue = 1.5f,
                    stepPerLevel = 1.0f,
                    unit = "%",
                    description = "Increases Critical Hit rate."
                };

            case "CRIT DAMAGE":
            case "CRIT_DAMAGE":
                return new StatInfo
                {
                    displayName = "CRIT Damage",
                    baseValue = 110f,
                    stepPerLevel = 10f,
                    unit = "%",
                    description = "Increases Critical Hit damage."
                };

            case "OBTAINED CHIPS":
            case "OBTAINED_CHIPS":
                return new StatInfo
                {
                    displayName = "Obtained Chips",
                    baseValue = 1f,
                    stepPerLevel = 1f,
                    unit = "%",
                    description = "Increases Data Chips obtained."
                };

            case "RANGED DEFENSE":
            case "RANGED_DEFENSE":
            case "RANNGED":
            case "RANGED":
                return new StatInfo
                {
                    displayName = "Ranged Defense",
                    baseValue = 2f,
                    stepPerLevel = 2f,
                    unit = "%",
                    description = "Decreases damage taken from ranged attacks."
                };

            case "DRONE ATK":
            case "DRONE_ATK":
                return new StatInfo
                {
                    displayName = "Drone ATK",
                    baseValue = 5f,
                    stepPerLevel = 5f,
                    unit = "%",
                    description = "Increases Drone attack power."
                };

            case "TURRET ATK":
            case "TURRET_ATK":
                return new StatInfo
                {
                    displayName = "Turret ATK",
                    baseValue = 5f,
                    stepPerLevel = 5f,
                    unit = "%",
                    description = "Increases Turret attack power."
                };

            case "TURRET DURATION":
            case "TURRET_DURATION":
                return new StatInfo
                {
                    displayName = "Turret Duration",
                    baseValue = 3f,
                    stepPerLevel = 3f,
                    unit = "%",
                    description = "Increases Turret active duration."
                };

            case "EVADE":
                return new StatInfo
                {
                    displayName = "Evade",
                    baseValue = 1f,
                    stepPerLevel = 1f,
                    unit = "%",
                    description = "Increases chance to dodge enemy attacks."
                };

            case "LIFE STEAL":
            case "LIFE_STEAL":
            case "LIFT STEAL":
                return new StatInfo
                {
                    displayName = "Life Steal",
                    baseValue = 0.5f,
                    stepPerLevel = 0.5f,
                    unit = "%",
                    description = "Restores HP based on damage dealt."
                };

            case "MOVE SPEED":
            case "MOVE_SPEED":
                return new StatInfo
                {
                    displayName = "Move Speed",
                    baseValue = 1f,
                    stepPerLevel = 1f,
                    unit = "%",
                    description = "Increases Character Movement Speed."
                };

            case "CHIPSET SELECTION":
            case "CHIPSET_SELECTION":
            case "CHIPSET SELECTION +1":
            case "CHIPSET SELECITON +1":
                return new StatInfo
                {
                    displayName = "Chipset Selection +1 Rate",
                    baseValue = 0f,
                    stepPerLevel = 3f,
                    unit = "%",
                    description = "Higher chance to obtain additional Chipset Selection upon leveling up."
                };

            default:
                return new StatInfo
                {
                    displayName = statName,
                    baseValue = 0f,
                    stepPerLevel = 1f,
                    unit = "",
                    description = "Increases stat power."
                };
        }
    }
}
