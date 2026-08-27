using UnityEngine;

/// <summary>
/// Runtime-safe references for the ten primary chipset icons, five tier-coloured
/// ChipsetLever frames, and five level-progress pips from the source sprite sheets.
/// </summary>
[CreateAssetMenu(fileName = "ChipsetLevelVisualLibrary", menuName = "PGE/UI/Chipset Level Visual Library")]
public sealed class ChipsetLevelVisualLibrary : ScriptableObject
{
    public Sprite[] primaryChipIcons = new Sprite[10];
    public Sprite[] mainMenuTierFrames = new Sprite[5];
    public Sprite[] tierLeverFrames = new Sprite[5];
    public Sprite[] levelPipSprites = new Sprite[5];
}
