using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameplayEventDatabase", menuName = "PGE/Gameplay Event Database")]
public class GameplayEventDatabase : ScriptableObject
{
    private static GameplayEventDatabase runtimeInstance;

    public static GameplayEventDatabase Instance
    {
        get
        {
            if (runtimeInstance == null)
            {
                runtimeInstance = Resources.Load<GameplayEventDatabase>("GameplayEventDatabase");
                if (runtimeInstance == null)
                {
                    runtimeInstance = CreateInstance<GameplayEventDatabase>();
                    runtimeInstance.InitializeDefaults();
                }
            }
            return runtimeInstance;
        }
    }

    [SerializeField] private List<GameplayEventData> events = new List<GameplayEventData>();
    public IReadOnlyList<GameplayEventData> Events => events;

    public void InitializeDefaults()
    {
        if (events == null) events = new List<GameplayEventData>();
        if (events.Count > 0) return;

        // 1. Electric Car
        GameplayEventData electricCar = CreateInstance<GameplayEventData>();
        electricCar.eventId = "electric_car";
        electricCar.eventTitle = "Electric Car";
        electricCar.illustrationSprite = Resources.Load<Sprite>("Events/electric_car");
        electricCar.introDialogueText = "An abandoned electric car in the junkyard.\n\nThe battery is drained, but its data core\nmight still be recoverable.";
        electricCar.isInstantResult = false;
        electricCar.options = new List<GameplayEventOption>
        {
            new GameplayEventOption(
                "Consume HP to start engine",
                "*Vzzzzz*\n\nHe has to consume HP to charge it, but he\nis able to start the engine.\nBernard obtains significant data from it.",
                "HP -5%\n[Big Battery] Level +1",
                EventRewardType.Health_ConsumePercent,
                5f,
                "big-battery"
            ),
            new GameplayEventOption(
                "Leave.",
                "Bernard decides not to waste energy and leaves.",
                "",
                EventRewardType.None
            )
        };
        events.Add(electricCar);

        // 2. Underground Bunker
        GameplayEventData bunker = CreateInstance<GameplayEventData>();
        bunker.eventId = "underground_bunker";
        bunker.eventTitle = "Underground Bunker";
        bunker.illustrationSprite = Resources.Load<Sprite>("Events/underground_bunker");
        bunker.introDialogueText = "Entrance of an abandoned underground\nbunker\n\n*Creak*\n\nYou can see two rooms as soon as you\nopen the bunker door.";
        bunker.isInstantResult = false;
        bunker.options = new List<GameplayEventOption>
        {
            new GameplayEventOption(
                "Go to the room on the left",
                "He finds a Artifact Box.",
                "You've obtained Metal Band-aid.",
                EventRewardType.Artifact_Grant,
                0f,
                "metal_band_aid"
            ),
            new GameplayEventOption(
                "Go to the room on the right",
                "He finds a functional medical stash.\nRestoring some energy.",
                "HP +15%",
                EventRewardType.Health_HealPercent,
                15f
            ),
            new GameplayEventOption(
                "Leave.",
                "Bernard leaves the bunker undisturbed.",
                "",
                EventRewardType.None
            )
        };
        events.Add(bunker);

        // 3. Assasinator
        GameplayEventData assassinator = CreateInstance<GameplayEventData>();
        assassinator.eventId = "assasinator";
        assassinator.eventTitle = "Assasinator";
        assassinator.illustrationSprite = Resources.Load<Sprite>("Events/assasinator");
        assassinator.introDialogueText = "\"What's your mission?\"\n\nSays this Android,\nwhose model could not be identified.";
        assassinator.isInstantResult = false;
        assassinator.options = new List<GameplayEventOption>
        {
            new GameplayEventOption(
                "To purge mutants",
                "\"Then you'd better hurry up and get\nmoving.\"\n\nThe creature places its hand on Bernard's\nbody\nand transfers energy to him.",
                "Move Speed +5%",
                EventRewardType.StatBuff_MoveSpeedPercent,
                5f
            ),
            new GameplayEventOption(
                "To annihilate creatures",
                "\"A worthy target. Take this kinetic booster.\"\n\nThe Android adjusts Bernard's servo-motors.",
                "Move Speed +8%",
                EventRewardType.StatBuff_MoveSpeedPercent,
                8f
            ),
            new GameplayEventOption(
                "Leave.",
                "\"Stay out of my way.\"",
                "",
                EventRewardType.None
            )
        };
        events.Add(assassinator);
    }

    public GameplayEventData GetById(string id)
    {
        if (events == null || events.Count == 0) InitializeDefaults();
        return events.Find(e => e != null && string.Equals(e.eventId, id, System.StringComparison.OrdinalIgnoreCase));
    }

    public GameplayEventData GetRandomEvent(IReadOnlyList<string> excludeIds = null)
    {
        if (events == null || events.Count == 0) InitializeDefaults();

        List<GameplayEventData> pool = new List<GameplayEventData>();
        foreach (var ev in events)
        {
            if (ev == null) continue;
            if (excludeIds != null)
            {
                bool isExcluded = false;
                for (int i = 0; i < excludeIds.Count; i++)
                {
                    if (string.Equals(ev.eventId, excludeIds[i], System.StringComparison.OrdinalIgnoreCase))
                    {
                        isExcluded = true;
                        break;
                    }
                }
                if (isExcluded) continue;
            }
            pool.Add(ev);
        }

        if (pool.Count == 0) pool.AddRange(events);
        if (pool.Count == 0) return null;

        int index = Random.Range(0, pool.Count);
        return pool[index];
    }
}
