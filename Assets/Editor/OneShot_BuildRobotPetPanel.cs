#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class OneShot_BuildRobotPetPanel
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string RobotPetAssetFolder = "Assets/Sprites/UI/Buddy/RobotPet_Sliced/";

    static OneShot_BuildRobotPetPanel()
    {
        if (Application.isBatchMode) return;
        OneShotEditorUtility.ExecuteOnceAndSelfDelete(nameof(OneShot_BuildRobotPetPanel), Build);
    }

    public static void RunFromBatch()
    {
        Build();
    }

    private static void Build()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedAdditively = !scene.IsValid() || !scene.isLoaded;
        if (openedAdditively)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        try
        {
            Transform buddyPanel = FindInScene(scene, "BuddyPanel");
            if (buddyPanel == null)
            {
                throw new InvalidOperationException("BuddyPanel was not found in MainMenu.");
            }

            BuddyController controller = buddyPanel.GetComponent<BuddyController>();
            if (controller == null)
            {
                throw new InvalidOperationException("BuddyPanel has no BuddyController.");
            }

            Transform existing = buddyPanel.Find("RobotPetPanel");
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito/Nunito SDF.asset");
            Material strokeMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/Nunito/Nunito SDF - Stroke.mat");

            Transform topTabs = buddyPanel.Find("TopTabs");
            Transform droneTab = topTabs != null ? topTabs.Find("TabDrone") : null;
            Transform robotTab = topTabs != null ? topTabs.Find("TabRobotPet") : null;
            if (droneTab == null || robotTab == null)
            {
                throw new InvalidOperationException("Buddy mode tabs were not found.");
            }

            UnityEngine.UI.Image droneTabImage = droneTab.GetComponent<UnityEngine.UI.Image>();
            UnityEngine.UI.Image robotTabImage = robotTab.GetComponent<UnityEngine.UI.Image>();
            droneTabImage.sprite = LoadSprite("Tab_Drone.png");
            robotTabImage.sprite = LoadSprite("Tab_RobotPet.png");
            droneTabImage.type = UnityEngine.UI.Image.Type.Simple;
            robotTabImage.type = UnityEngine.UI.Image.Type.Simple;
            droneTabImage.preserveAspect = false;
            robotTabImage.preserveAspect = false;

            SetChildActive(droneTab, "Label", false);
            SetChildActive(droneTab, "Wave", false);
            SetChildActive(robotTab, "Label", false);
            GameObject lockIcon = robotTab.Find("Lock") != null ? robotTab.Find("Lock").gameObject : null;

            RectTransform robotRoot = CreateRect("RobotPetPanel", buddyPanel);
            Stretch(robotRoot);

            UnityEngine.UI.Image detailPanel = CreateImage("PetDetailPanel", robotRoot, LoadSprite("Panel_RobotPet.png"));
            SetTopRect(detailPanel.rectTransform, new Vector2(0f, -150f), new Vector2(920f, 595f));

            CreatePresetButton(robotRoot, "RobotPreset1", new Vector2(-120f, -125f), LoadSubSprite("1 Yellow"));
            CreatePresetButton(robotRoot, "RobotPreset2", new Vector2(0f, -125f), LoadSubSprite("2 Red"));
            CreatePresetButton(robotRoot, "RobotPreset3", new Vector2(120f, -125f), LoadSubSprite("3 Red"));

            UnityEngine.UI.Image selectedCard = CreateImage("SelectedPetCard", detailPanel.rectTransform, LoadSprite("Card_Pet_Dog_Selected.png"));
            SetTopLeftRect(selectedCard.rectTransform, new Vector2(42f, -225f), new Vector2(185f, 232f));
            selectedCard.preserveAspect = true;

            TMP_Text petName = CreateText("PetName", detailPanel.rectTransform, "Cydog", 47f, new Color32(255, 184, 0, 255), font, strokeMaterial, TextAlignmentOptions.Left);
            SetTopLeftRect(petName.rectTransform, new Vector2(255f, -245f), new Vector2(560f, 70f));

            TMP_Text description = CreateText("Description", detailPanel.rectTransform, "Increases Bernard's Max HP.", 31f, Color.white, font, strokeMaterial, TextAlignmentOptions.Left);
            SetTopLeftRect(description.rectTransform, new Vector2(255f, -315f), new Vector2(600f, 90f));

            UnityEngine.UI.Image craft = CreateImage("CraftButton", detailPanel.rectTransform, LoadSprite("Button_Craft.png"));
            SetTopRect(craft.rectTransform, new Vector2(0f, -493f), new Vector2(210f, 92f));
            craft.preserveAspect = true;
            UnityEngine.UI.Button craftButton = craft.gameObject.AddComponent<UnityEngine.UI.Button>();
            craftButton.targetGraphic = craft;
            craftButton.interactable = false;

            Sprite tierSelected = FindSpriteOnSibling(buddyPanel, "SortFilterBar/ByTierBtn");
            Sprite quantityNormal = FindSpriteOnSibling(buddyPanel, "SortFilterBar/ByQtyBtn");
            UnityEngine.UI.Image byTier = CreateImage("RobotByTier", robotRoot, tierSelected);
            SetTopRect(byTier.rectTransform, new Vector2(-135f, -790f), new Vector2(245f, 70f));
            UnityEngine.UI.Image byQuantity = CreateImage("RobotByQuantity", robotRoot, quantityNormal);
            SetTopRect(byQuantity.rectTransform, new Vector2(135f, -790f), new Vector2(255f, 70f));

            CreatePetCard(robotRoot, "RobotCard", new Vector2(-300f, -895f), LoadSprite("Card_Pet_Robot.png"), font, strokeMaterial);
            CreatePetCard(robotRoot, "BatCard", new Vector2(0f, -895f), LoadSprite("Card_Pet_Bat.png"), font, strokeMaterial);
            CreatePetCard(robotRoot, "DogCard", new Vector2(300f, -895f), LoadSprite("Card_Pet_Dog.png"), font, strokeMaterial);

            robotRoot.gameObject.SetActive(false);

            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("robotPetPanel").objectReferenceValue = robotRoot.gameObject;
            serialized.FindProperty("robotPetLockIcon").objectReferenceValue = lockIcon;
            serialized.FindProperty("droneModeBg").objectReferenceValue = droneTabImage;
            serialized.FindProperty("robotPetModeBg").objectReferenceValue = robotTabImage;

            string[] droneRootNames =
            {
                "EquippedBoard",
                "InventoryBgTint",
                "SortFilterBar",
                "SlotIconBuddy",
                "InventoryScrollView"
            };
            SerializedProperty roots = serialized.FindProperty("droneModeContentRoots");
            roots.arraySize = droneRootNames.Length;
            for (int i = 0; i < droneRootNames.Length; i++)
            {
                Transform content = buddyPanel.Find(droneRootNames[i]);
                roots.GetArrayElementAtIndex(i).objectReferenceValue = content != null ? content.gameObject : null;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("Unity could not save MainMenu after building Robot Pet UI.");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[RobotPetUI] Built the Robot Pet panel from existing assets and wired the Chapter 4 unlock condition.");
        }
        finally
        {
            if (openedAdditively && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static Transform FindInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == objectName);
            if (match != null) return match;
        }
        return null;
    }

    private static Sprite LoadSprite(string fileName)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RobotPetAssetFolder + fileName);
        if (sprite == null) throw new InvalidOperationException("Missing existing Robot Pet sprite: " + fileName);
        return sprite;
    }

    private static Sprite LoadSubSprite(string spriteName)
    {
        return AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/Chipset/nút màn chipset.png")
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name.Equals(spriteName, StringComparison.OrdinalIgnoreCase));
    }

    private static Sprite FindSpriteOnSibling(Transform root, string path)
    {
        Transform target = root.Find(path);
        UnityEngine.UI.Image image = target != null ? target.GetComponent<UnityEngine.UI.Image>() : null;
        return image != null ? image.sprite : null;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static UnityEngine.UI.Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float size, Color color, TMP_FontAsset font, Material material, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        if (material != null) text.fontSharedMaterial = material;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private static void CreatePresetButton(Transform parent, string name, Vector2 position, Sprite sprite)
    {
        UnityEngine.UI.Image image = CreateImage(name, parent, sprite);
        SetTopRect(image.rectTransform, position, new Vector2(92f, 92f));
        image.preserveAspect = true;
    }

    private static void CreatePetCard(Transform parent, string name, Vector2 position, Sprite sprite, TMP_FontAsset font, Material material)
    {
        UnityEngine.UI.Image card = CreateImage(name, parent, sprite);
        SetTopRect(card.rectTransform, position, new Vector2(205f, 256f));
        card.preserveAspect = true;

        TMP_Text level = CreateText("Level", card.rectTransform, "LV.01", 30f, Color.white, font, material, TextAlignmentOptions.Center);
        level.rectTransform.anchorMin = new Vector2(0f, 1f);
        level.rectTransform.anchorMax = new Vector2(1f, 1f);
        level.rectTransform.pivot = new Vector2(0.5f, 1f);
        level.rectTransform.anchoredPosition = new Vector2(0f, 0f);
        level.rectTransform.sizeDelta = new Vector2(0f, 48f);

        TMP_Text count = CreateText("Count", card.rectTransform, "0/3", 27f, Color.white, font, material, TextAlignmentOptions.Center);
        count.rectTransform.anchorMin = new Vector2(0f, 0f);
        count.rectTransform.anchorMax = new Vector2(1f, 0f);
        count.rectTransform.pivot = new Vector2(0.5f, 0f);
        count.rectTransform.anchoredPosition = new Vector2(0f, 12f);
        count.rectTransform.sizeDelta = new Vector2(0f, 44f);
    }

    private static void SetChildActive(Transform parent, string childName, bool active)
    {
        Transform child = parent.Find(childName);
        if (child != null) child.gameObject.SetActive(active);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetTopRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetTopLeftRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
#endif
