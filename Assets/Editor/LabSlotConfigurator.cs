#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class LabSlotConfigurator
{
    private const string LabTexturePath = "Assets/Sprites/UI/Lab/nút màn lab 1.png";
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";

    private static readonly string[] StatSpriteNames = new string[]
    {
        "HP",
        "Recovery",
        "Auto Recovery",
        "DEF",
        "ATK",
        "CRIT Rate",
        "CRIT Damage",
        "Obtained Chips",
        "Ranged Defense",
        "Drone ATK",
        "Turret ATK",
        "Turret Duration",
        "Evade",
        "Life Steal",
        "Move Speed",
        "Chipset Selection"
    };

    static LabSlotConfigurator()
    {
        EditorApplication.delayCall += ConfigureAllSlots;
    }

    [MenuItem("PGE/UI/Configure All 16 Lab Slots Like Slot 1")]
    public static void ConfigureAllSlots()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
        {
            if (System.IO.File.Exists(ScenePath))
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            else
            {
                return;
            }
        }

        // 1. Load all sliced sprites from nút màn lab 1.png
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(LabTexturePath).OfType<Sprite>().ToArray();
        if (sprites.Length == 0)
        {
            Debug.LogWarning($"[LabSlotConfigurator] No sprites found in {LabTexturePath}. Running slicer first...");
            LabSpriteSlicer.SliceLabTexture();
            sprites = AssetDatabase.LoadAllAssetsAtPath(LabTexturePath).OfType<Sprite>().ToArray();
        }

        Sprite lockedSprite = sprites.FirstOrDefault(s => s.name == "Locked");
        if (lockedSprite == null)
        {
            Debug.LogError("[LabSlotConfigurator] 'Locked' sprite not found in " + LabTexturePath);
            return;
        }

        Dictionary<string, Sprite> statMap = sprites.ToDictionary(s => s.name, s => s);

        // 2. Find LabUpgradeController
        LabUpgradeController controller = UnityEngine.Object.FindObjectOfType<LabUpgradeController>(true);
        if (controller == null)
        {
            Debug.LogWarning("[LabSlotConfigurator] LabUpgradeController not found in scene.");
            return;
        }

        // 3. Find UpgradeGrid
        Transform upgradeGrid = null;
        Transform labPanel = controller.transform;
        Transform statsPanel = labPanel.Find("StatsPanel") ?? labPanel.Find("Panel_Stats");
        if (statsPanel != null)
        {
            upgradeGrid = statsPanel.Find("UpgradeGrid");
        }
        if (upgradeGrid == null)
        {
            upgradeGrid = UnityEngine.Object.FindObjectsOfType<Transform>(true)
                .FirstOrDefault(t => t.name == "UpgradeGrid");
        }

        if (upgradeGrid == null)
        {
            Debug.LogError("[LabSlotConfigurator] UpgradeGrid not found in MainMenu.");
            return;
        }

        SerializedObject controllerSO = new SerializedObject(controller);
        SerializedProperty itemsProp = controllerSO.FindProperty("items");
        SerializedProperty lockIconSpriteProp = controllerSO.FindProperty("lockIconSprite");
        if (lockIconSpriteProp != null)
        {
            lockIconSpriteProp.objectReferenceValue = lockedSprite;
        }

        SerializedProperty highlightSpriteProp = controllerSO.FindProperty("highlightBorderSprite");
        if (highlightSpriteProp != null)
        {
            highlightSpriteProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Lab/khung_highlight_lab.png");
        }

        for (int i = 0; i < 16; i++)
        {
            string slotName = $"Slot{i + 1:02d}";
            Transform slotTr = upgradeGrid.Find(slotName);
            if (slotTr == null)
            {
                Debug.LogWarning($"[LabSlotConfigurator] {slotName} not found in UpgradeGrid.");
                continue;
            }

            string expectedSpriteName = StatSpriteNames[i];
            statMap.TryGetValue(expectedSpriteName, out Sprite cardSprite);

            // A. Clean up Background & TopHighlight under Slot
            Transform bgTr = slotTr.Find("Background");
            if (bgTr != null)
            {
                UnityEngine.Object.DestroyImmediate(bgTr.gameObject);
            }

            Transform hlTr = slotTr.Find("TopHighlight");
            if (hlTr != null)
            {
                UnityEngine.Object.DestroyImmediate(hlTr.gameObject);
            }

            // B. Configure LockedGroup
            Transform lockedGroupTr = slotTr.Find("LockedGroup");
            if (lockedGroupTr == null)
            {
                GameObject lgGo = new GameObject("LockedGroup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                lockedGroupTr = lgGo.transform;
                lockedGroupTr.SetParent(slotTr, false);
            }

            // Remove any children under LockedGroup (LockIcon, LockedText)
            for (int c = lockedGroupTr.childCount - 1; c >= 0; c--)
            {
                UnityEngine.Object.DestroyImmediate(lockedGroupTr.GetChild(c).gameObject);
            }

            RectTransform lockedRt = lockedGroupTr.GetComponent<RectTransform>();
            lockedRt.anchorMin = Vector2.zero;
            lockedRt.anchorMax = Vector2.one;
            lockedRt.offsetMin = Vector2.zero;
            lockedRt.offsetMax = Vector2.zero;
            lockedRt.pivot = new Vector2(0.5f, 0.5f);
            lockedRt.localScale = Vector3.one;
            lockedRt.localRotation = Quaternion.identity;

            Image lockedImg = lockedGroupTr.GetComponent<Image>();
            if (lockedImg == null) lockedImg = lockedGroupTr.gameObject.AddComponent<Image>();
            lockedImg.sprite = lockedSprite;
            lockedImg.color = Color.white;
            lockedImg.raycastTarget = true;
            lockedImg.preserveAspect = false;

            // C. Configure UnlockedGroup
            Transform unlockedGroupTr = slotTr.Find("UnlockedGroup");
            if (unlockedGroupTr == null)
            {
                GameObject ugGo = new GameObject("UnlockedGroup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                unlockedGroupTr = ugGo.transform;
                unlockedGroupTr.SetParent(slotTr, false);
            }

            // Remove ItemIcon under UnlockedGroup if present
            Transform itemIconTr = unlockedGroupTr.Find("ItemIcon");
            if (itemIconTr != null)
            {
                UnityEngine.Object.DestroyImmediate(itemIconTr.gameObject);
            }

            RectTransform unlockedRt = unlockedGroupTr.GetComponent<RectTransform>();
            unlockedRt.anchorMin = Vector2.zero;
            unlockedRt.anchorMax = Vector2.one;
            unlockedRt.offsetMin = Vector2.zero;
            unlockedRt.offsetMax = Vector2.zero;
            unlockedRt.pivot = new Vector2(0.5f, 0.5f);
            unlockedRt.localScale = Vector3.one;
            unlockedRt.localRotation = Quaternion.identity;

            Image unlockedImg = unlockedGroupTr.GetComponent<Image>();
            if (unlockedImg == null) unlockedImg = unlockedGroupTr.gameObject.AddComponent<Image>();
            if (cardSprite != null) unlockedImg.sprite = cardSprite;
            unlockedImg.color = Color.white;
            unlockedImg.raycastTarget = true;
            unlockedImg.preserveAspect = false;

            // D. Configure Button on Slot
            Button slotBtn = slotTr.GetComponent<Button>();
            if (slotBtn == null)
            {
                slotBtn = slotTr.gameObject.AddComponent<Button>();
            }
            slotBtn.targetGraphic = unlockedImg;

            // Ensure hierarchy order: LockedGroup (0), UnlockedGroup (1)
            lockedGroupTr.SetSiblingIndex(0);
            unlockedGroupTr.SetSiblingIndex(1);

            // E. Link into LabUpgradeController.items[i]
            if (itemsProp != null && i < itemsProp.arraySize)
            {
                SerializedProperty itemEntry = itemsProp.GetArrayElementAtIndex(i);
                itemEntry.FindPropertyRelative("lockedGroup").objectReferenceValue = lockedGroupTr.gameObject;
                itemEntry.FindPropertyRelative("unlockedGroup").objectReferenceValue = unlockedGroupTr.gameObject;
                if (cardSprite != null)
                {
                    itemEntry.FindPropertyRelative("itemIcon").objectReferenceValue = cardSprite;
                }
                itemEntry.FindPropertyRelative("iconImage").objectReferenceValue = null;
                itemEntry.FindPropertyRelative("slotButton").objectReferenceValue = slotBtn;
                itemEntry.FindPropertyRelative("slotBackground").objectReferenceValue = unlockedImg;

                Transform lvlTr = unlockedGroupTr.Find("LevelText");
                if (lvlTr != null)
                {
                    itemEntry.FindPropertyRelative("levelText").objectReferenceValue = lvlTr.GetComponent<TMP_Text>();
                }

                Transform nameTr = unlockedGroupTr.Find("ItemName");
                if (nameTr != null)
                {
                    itemEntry.FindPropertyRelative("nameText").objectReferenceValue = nameTr.GetComponent<TMP_Text>();
                }
            }
        }

        controllerSO.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[LabSlotConfigurator] Successfully configured all 16 slots like Slot01 with full card sprites!");
    }
}
#endif
