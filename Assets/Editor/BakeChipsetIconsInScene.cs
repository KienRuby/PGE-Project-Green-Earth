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
public static class BakeChipsetIconsInScene
{
    static BakeChipsetIconsInScene()
    {
        EditorApplication.delayCall += ExecuteBakeIfOpen;
    }

    private static void ExecuteBakeIfOpen()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.isLoaded && activeScene.name == "MainMenu")
        {
            BakeAllChipsetIcons();
            BakeAllBuddyIcons();
        }
    }

    [MenuItem("PGE/Bake All Panels In Scene")]
    public static void BakeAllPanels()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        BakeAllChipsetIcons();
        BakeAllBuddyIcons();
    }

    [MenuItem("PGE/Bake Chipset Icons In Scene")]
    public static void BakeAllChipsetIcons()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        ChipsetLevelVisualLibrary visualLibrary = Resources.Load<ChipsetLevelVisualLibrary>("ChipsetLevelVisualLibrary")
            ?? AssetDatabase.LoadAssetAtPath<ChipsetLevelVisualLibrary>("Assets/Resources/ChipsetLevelVisualLibrary.asset");

        Sprite[] iconSprites = null;
        if (visualLibrary != null && visualLibrary.primaryChipIcons != null && visualLibrary.primaryChipIcons.Length > 0)
        {
            iconSprites = visualLibrary.primaryChipIcons;
        }
        else
        {
            string iconAtlasPath = "Assets/Sprites/UI/Chipset/icon chipset.png";
            iconSprites = AssetDatabase.LoadAllAssetsAtPath(iconAtlasPath).OfType<Sprite>().ToArray();
        }

        string buttonAtlasPath = "Assets/Sprites/UI/Chipset/nút màn chipset.png";
        Sprite[] buttonSprites = AssetDatabase.LoadAllAssetsAtPath(buttonAtlasPath).OfType<Sprite>().ToArray();

        Sprite[] frameSprites = null;
        if (visualLibrary != null && visualLibrary.mainMenuTierFrames != null && visualLibrary.mainMenuTierFrames.Length > 0)
        {
            frameSprites = visualLibrary.mainMenuTierFrames;
        }
        else
        {
            string frameAtlasPath = "Assets/Sprites/UI/Chipset/khung chipset (1).png";
            frameSprites = AssetDatabase.LoadAllAssetsAtPath(frameAtlasPath).OfType<Sprite>().ToArray();
        }

        GameObject chipsetPanelObj = GameObject.Find("ChipsetPanel");
        if (chipsetPanelObj == null)
        {
            var allGo = Resources.FindObjectsOfTypeAll<GameObject>();
            chipsetPanelObj = allGo.FirstOrDefault(g => g.name == "ChipsetPanel" && g.scene.isLoaded);
        }

        if (chipsetPanelObj == null) return;

        ChipsetController controller = chipsetPanelObj.GetComponent<ChipsetController>();
        if (controller != null)
        {
            controller.LoadVisualLibraryIfMissing();
            EditorUtility.SetDirty(controller);
        }

        var database = ChipsetController.CreateSavedDatabase();

        // 1. Bake Deck Preset Buttons
        if (buttonSprites != null && buttonSprites.Length > 0)
        {
            Sprite p1Yellow = buttonSprites.FirstOrDefault(s => s.name.Equals("1 Yellow", StringComparison.OrdinalIgnoreCase));
            Sprite p2Red = buttonSprites.FirstOrDefault(s => s.name.Equals("2 Red", StringComparison.OrdinalIgnoreCase));
            Sprite p3Red = buttonSprites.FirstOrDefault(s => s.name.Equals("3 Red", StringComparison.OrdinalIgnoreCase));

            Transform p1 = chipsetPanelObj.transform.Find("PresetBar/Preset1") ?? chipsetPanelObj.transform.Find("Preset1Btn");
            if (p1 != null && p1Yellow != null)
            {
                var img = p1.GetComponent<Image>();
                if (img != null) { img.sprite = p1Yellow; img.color = Color.white; EditorUtility.SetDirty(img); }
                var txt = p1.GetComponentInChildren<TMP_Text>(true);
                if (txt != null) { txt.text = string.Empty; EditorUtility.SetDirty(txt); }
            }

            Transform p2 = chipsetPanelObj.transform.Find("PresetBar/Preset2") ?? chipsetPanelObj.transform.Find("Preset2Btn");
            if (p2 != null && p2Red != null)
            {
                var img = p2.GetComponent<Image>();
                if (img != null) { img.sprite = p2Red; img.color = Color.white; EditorUtility.SetDirty(img); }
                var txt = p2.GetComponentInChildren<TMP_Text>(true);
                if (txt != null) { txt.text = string.Empty; EditorUtility.SetDirty(txt); }
            }

            Transform p3 = chipsetPanelObj.transform.Find("PresetBar/Preset3") ?? chipsetPanelObj.transform.Find("Preset3Btn");
            if (p3 != null && p3Red != null)
            {
                var img = p3.GetComponent<Image>();
                if (img != null) { img.sprite = p3Red; img.color = Color.white; EditorUtility.SetDirty(img); }
                var txt = p3.GetComponentInChildren<TMP_Text>(true);
                if (txt != null) { txt.text = string.Empty; EditorUtility.SetDirty(txt); }
            }

            // Sort buttons
            Sprite byTileYellow = buttonSprites.FirstOrDefault(s => s.name.Equals("By TileYellow", StringComparison.OrdinalIgnoreCase));
            Sprite byQtyGreen = buttonSprites.FirstOrDefault(s => s.name.Equals("ByQuantityGreen", StringComparison.OrdinalIgnoreCase));

            Transform byTile = chipsetPanelObj.transform.Find("SortBar/ByTierBtn") ?? chipsetPanelObj.transform.Find("ByTierBtn") ?? chipsetPanelObj.transform.Find("ByTier");
            if (byTile != null && byTileYellow != null)
            {
                var img = byTile.GetComponent<Image>();
                if (img != null) { img.sprite = byTileYellow; img.color = Color.white; EditorUtility.SetDirty(img); }
            }

            Transform byQty = chipsetPanelObj.transform.Find("SortBar/ByQtyBtn") ?? chipsetPanelObj.transform.Find("ByQuantityBtn") ?? chipsetPanelObj.transform.Find("ByQty");
            if (byQty != null && byQtyGreen != null)
            {
                var img = byQty.GetComponent<Image>();
                if (img != null) { img.sprite = byQtyGreen; img.color = Color.white; EditorUtility.SetDirty(img); }
            }
        }

        // 2. Bake 10 Equipped Slots
        Transform equippedGrid = chipsetPanelObj.transform.Find("EquippedBoard/EquippedGrid") ?? chipsetPanelObj.transform.Find("EquippedGrid");
        if (equippedGrid != null)
        {
            for (int i = 0; i < equippedGrid.childCount && i < database.Count; i++)
            {
                Transform slot = equippedGrid.GetChild(i);
                ChipItemData chip = database[i];
                BakeCard(slot, chip, iconSprites, frameSprites);
            }
        }

        // 3. Bake Inventory Slots
        Transform invContent = chipsetPanelObj.transform.Find("InventoryScroll/Viewport/Content") ?? chipsetPanelObj.transform.Find("InventoryContent");
        if (invContent != null)
        {
            for (int i = 0; i < invContent.childCount; i++)
            {
                Transform slot = invContent.GetChild(i);
                if (slot.name == "CardTemplate") continue;
                ChipItemData chip = database[i % database.Count];
                BakeCard(slot, chip, iconSprites, frameSprites);
            }
        }

        if (controller != null)
        {
            controller.LoadChipIconsIfMissing();
            controller.LoadPresetSpritesIfMissing();
            controller.LoadSortSpritesIfMissing();
            EditorUtility.SetDirty(controller);
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        EditorSceneManager.MarkSceneDirty(chipsetPanelObj.scene);
        Debug.Log("[BakeChipsetIcons] ✅ Đã gắn thành công toàn bộ 10 Icon, Khung và Nút bấm trực tiếp vào Scene MainMenu!");
    }

    [MenuItem("PGE/Bake Buddy Icons In Scene")]
    public static void BakeAllBuddyIcons()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        string buddyIconAtlasPath = "Assets/Sprites/UI/Buddy/icon buddy.png";
        Sprite[] buddyIcons = AssetDatabase.LoadAllAssetsAtPath(buddyIconAtlasPath).OfType<Sprite>().ToArray();

        string buddyButtonAtlasPath = "Assets/Sprites/UI/Buddy/nút màn buddy.png";
        Sprite[] buddyButtons = AssetDatabase.LoadAllAssetsAtPath(buddyButtonAtlasPath).OfType<Sprite>().ToArray();

        string chipsetButtonAtlasPath = "Assets/Sprites/UI/Chipset/nút màn chipset.png";
        Sprite[] chipsetButtons = AssetDatabase.LoadAllAssetsAtPath(chipsetButtonAtlasPath).OfType<Sprite>().ToArray();

        GameObject buddyPanelObj = GameObject.Find("BuddyPanel");
        if (buddyPanelObj == null)
        {
            var allGo = Resources.FindObjectsOfTypeAll<GameObject>();
            buddyPanelObj = allGo.FirstOrDefault(g => g.name == "BuddyPanel" && g.scene.isLoaded);
        }

        if (buddyPanelObj == null) return;

        BuddyController controller = buddyPanelObj.GetComponent<BuddyController>();

        // 1. Top Tabs (Drone vs Robot Pet)
        if (buddyButtons != null && buddyButtons.Length > 0)
        {
            Sprite droneTab = buddyButtons.FirstOrDefault(s => s.name.Equals("Drone", StringComparison.OrdinalIgnoreCase));
            Sprite robotPetOff = buddyButtons.FirstOrDefault(s => s.name.Equals("Robot Pet OFF", StringComparison.OrdinalIgnoreCase));

            Transform tabDrone = buddyPanelObj.transform.Find("TopTabs/TabDrone");
            if (tabDrone != null && droneTab != null)
            {
                var img = tabDrone.GetComponent<Image>();
                if (img != null) { img.sprite = droneTab; img.color = Color.white; EditorUtility.SetDirty(img); }
            }

            Transform tabRobotPet = buddyPanelObj.transform.Find("TopTabs/TabRobotPet");
            if (tabRobotPet != null && robotPetOff != null)
            {
                var img = tabRobotPet.GetComponent<Image>();
                if (img != null) { img.sprite = robotPetOff; img.color = Color.white; EditorUtility.SetDirty(img); }
            }
        }

        // 2. Preset Decks 1, 2, 3
        if (chipsetButtons != null && chipsetButtons.Length > 0)
        {
            Sprite p1Yellow = chipsetButtons.FirstOrDefault(s => s.name.Equals("1 Yellow", StringComparison.OrdinalIgnoreCase));
            Sprite p2Red = chipsetButtons.FirstOrDefault(s => s.name.Equals("2 Red", StringComparison.OrdinalIgnoreCase));
            Sprite p3Red = chipsetButtons.FirstOrDefault(s => s.name.Equals("3 Red", StringComparison.OrdinalIgnoreCase));

            Transform p1 = buddyPanelObj.transform.Find("PresetBar/Preset1") ?? buddyPanelObj.transform.Find("Preset1Btn");
            if (p1 != null && p1Yellow != null)
            {
                var img = p1.GetComponent<Image>();
                if (img != null) { img.sprite = p1Yellow; img.color = Color.white; EditorUtility.SetDirty(img); }
                var txt = p1.GetComponentInChildren<TMP_Text>(true);
                if (txt != null) { txt.text = string.Empty; EditorUtility.SetDirty(txt); }
            }

            Transform p2 = buddyPanelObj.transform.Find("PresetBar/Preset2") ?? buddyPanelObj.transform.Find("Preset2Btn");
            if (p2 != null && p2Red != null)
            {
                var img = p2.GetComponent<Image>();
                if (img != null) { img.sprite = p2Red; img.color = Color.white; EditorUtility.SetDirty(img); }
                var txt = p2.GetComponentInChildren<TMP_Text>(true);
                if (txt != null) { txt.text = string.Empty; EditorUtility.SetDirty(txt); }
            }

            Transform p3 = buddyPanelObj.transform.Find("PresetBar/Preset3") ?? buddyPanelObj.transform.Find("Preset3Btn");
            if (p3 != null && p3Red != null)
            {
                var img = p3.GetComponent<Image>();
                if (img != null) { img.sprite = p3Red; img.color = Color.white; EditorUtility.SetDirty(img); }
                var txt = p3.GetComponentInChildren<TMP_Text>(true);
                if (txt != null) { txt.text = string.Empty; EditorUtility.SetDirty(txt); }
            }

            // Sort Buttons
            Sprite byTileYellow = chipsetButtons.FirstOrDefault(s => s.name.Equals("By TileYellow", StringComparison.OrdinalIgnoreCase));
            Sprite byQtyGreen = chipsetButtons.FirstOrDefault(s => s.name.Equals("ByQuantityGreen", StringComparison.OrdinalIgnoreCase));

            Transform byTile = buddyPanelObj.transform.Find("SortBar/ByTierBtn") ?? buddyPanelObj.transform.Find("ByTierBtn") ?? buddyPanelObj.transform.Find("ByTier");
            if (byTile != null && byTileYellow != null)
            {
                var img = byTile.GetComponent<Image>();
                if (img != null) { img.sprite = byTileYellow; img.color = Color.white; EditorUtility.SetDirty(img); }
            }

            Transform byQty = buddyPanelObj.transform.Find("SortBar/ByQtyBtn") ?? buddyPanelObj.transform.Find("ByQuantityBtn") ?? buddyPanelObj.transform.Find("ByQty");
            if (byQty != null && byQtyGreen != null)
            {
                var img = byQty.GetComponent<Image>();
                if (img != null) { img.sprite = byQtyGreen; img.color = Color.white; EditorUtility.SetDirty(img); }
            }
        }

        // 3. Bake 3 Equipped Slots (Slot 0: drone-antenna-eye, Slot 1: drone-spider, Slot 2: drone-cross-visor)
        Sprite khungSprite = buddyButtons?.FirstOrDefault(s => s.name.Equals("khung", StringComparison.OrdinalIgnoreCase));
        Sprite droneAntennaEye = buddyIcons?.FirstOrDefault(s => s.name.Equals("drone-antenna-eye", StringComparison.OrdinalIgnoreCase));
        Sprite droneSpider = buddyIcons?.FirstOrDefault(s => s.name.Equals("drone-spider", StringComparison.OrdinalIgnoreCase));
        Sprite droneCrossVisor = buddyIcons?.FirstOrDefault(s => s.name.Equals("drone-cross-visor", StringComparison.OrdinalIgnoreCase));
        Sprite droneStealthWing = buddyIcons?.FirstOrDefault(s => s.name.Equals("drone-stealth-wing", StringComparison.OrdinalIgnoreCase));

        Sprite[] equippedDrones = new Sprite[] { droneAntennaEye, droneSpider, droneCrossVisor };

        Transform equippedBoard = buddyPanelObj.transform.Find("EquippedBoard/EquippedGrid") ?? buddyPanelObj.transform.Find("EquippedGrid");
        if (equippedBoard != null)
        {
            for (int i = 0; i < equippedBoard.childCount && i < 3; i++)
            {
                Transform slot = equippedBoard.GetChild(i);
                Sprite icon = i < equippedDrones.Length ? equippedDrones[i] : droneAntennaEye;
                BakeBuddyCard(slot, icon, khungSprite, "LV.01", "0/3");
            }
        }

        // 4. Bake Inventory Slots (Slot 0: drone-stealth-wing)
        Transform invContent = buddyPanelObj.transform.Find("InventoryScroll/Viewport/Content") ?? buddyPanelObj.transform.Find("InventoryContent");
        if (invContent != null)
        {
            for (int i = 0; i < invContent.childCount; i++)
            {
                Transform slot = invContent.GetChild(i);
                if (slot.name == "CardTemplate") continue;
                BakeBuddyCard(slot, droneStealthWing, khungSprite, "LV.01", "0/3");
            }
        }

        if (controller != null)
        {
            controller.LoadPresetSpritesIfMissing();
            controller.LoadSortSpritesIfMissing();
            EditorUtility.SetDirty(controller);
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        EditorSceneManager.MarkSceneDirty(buddyPanelObj.scene);
        Debug.Log("[BakeBuddyIcons] ✅ Đã gắn thành công toàn bộ Icon, Khung và Nút bấm trực tiếp cho màn hình Buddy!");
    }

    private static void BakeBuddyCard(Transform cardTransform, Sprite iconSprite, Sprite frameSprite, string level, string progress)
    {
        if (cardTransform == null) return;

        Transform normalGroup = cardTransform.Find("NormalContentGroup");
        if (normalGroup != null) { normalGroup.gameObject.SetActive(true); EditorUtility.SetDirty(normalGroup.gameObject); }

        Transform emptyGroup = cardTransform.Find("EmptySlotGroup");
        if (emptyGroup != null) { emptyGroup.gameObject.SetActive(false); EditorUtility.SetDirty(emptyGroup.gameObject); }

        Transform lockedGroup = cardTransform.Find("LockedSlotGroup");
        if (lockedGroup != null) { lockedGroup.gameObject.SetActive(false); EditorUtility.SetDirty(lockedGroup.gameObject); }

        // 1. Frame
        Image frameImg = cardTransform.GetComponent<Image>();
        if (frameImg != null && frameSprite != null)
        {
            frameImg.sprite = frameSprite;
            frameImg.color = Color.white;
            EditorUtility.SetDirty(frameImg);
        }

        // 2. Icon
        Transform iconTransform = cardTransform.Find("NormalContentGroup/DroneIcon")
                               ?? cardTransform.Find("NormalContentGroup/Icon")
                               ?? cardTransform.Find("DroneIcon")
                               ?? cardTransform.Find("Icon");

        if (iconTransform != null && iconSprite != null)
        {
            Image iconImg = iconTransform.GetComponent<Image>();
            if (iconImg != null)
            {
                iconImg.sprite = iconSprite;
                iconImg.color = Color.white;
                iconImg.gameObject.SetActive(true);
                EditorUtility.SetDirty(iconImg);
            }
        }

        // 3. Level Text
        Transform levelTransform = cardTransform.Find("NormalContentGroup/LevelText") ?? cardTransform.Find("LevelText");
        if (levelTransform != null)
        {
            TMP_Text levelTxt = levelTransform.GetComponent<TMP_Text>();
            if (levelTxt != null)
            {
                levelTxt.text = level;
                EditorUtility.SetDirty(levelTxt);
            }
        }

        // 4. Progress Text
        Transform progTransform = cardTransform.Find("NormalContentGroup/BottomBar/ProgressText") ?? cardTransform.Find("BottomBar/ProgressText") ?? cardTransform.Find("ProgressText");
        if (progTransform != null)
        {
            TMP_Text progTxt = progTransform.GetComponent<TMP_Text>();
            if (progTxt != null)
            {
                progTxt.text = progress;
                EditorUtility.SetDirty(progTxt);
            }
        }

        // 5. Upgrade Arrow
        Transform arrowTransform = cardTransform.Find("NormalContentGroup/UpgradeArrowGroup") ?? cardTransform.Find("UpgradeArrowGroup");
        if (arrowTransform != null)
        {
            arrowTransform.gameObject.SetActive(false);
            EditorUtility.SetDirty(arrowTransform.gameObject);
        }

        EditorUtility.SetDirty(cardTransform.gameObject);
    }

    private static void BakeCard(Transform cardTransform, ChipItemData chip, Sprite[] iconSprites, Sprite[] frameSprites)
    {
        if (cardTransform == null || chip == null) return;

        Transform normalGroup = cardTransform.Find("NormalContentGroup");
        if (normalGroup != null) { normalGroup.gameObject.SetActive(true); EditorUtility.SetDirty(normalGroup.gameObject); }

        Transform emptyGroup = cardTransform.Find("EmptySlotGroup");
        if (emptyGroup != null) { emptyGroup.gameObject.SetActive(false); EditorUtility.SetDirty(emptyGroup.gameObject); }

        Transform lockedGroup = cardTransform.Find("LockedSlotGroup");
        if (lockedGroup != null) { lockedGroup.gameObject.SetActive(false); EditorUtility.SetDirty(lockedGroup.gameObject); }

        // 1. Icon
        Transform iconTransform = cardTransform.Find("NormalContentGroup/Icon")
                               ?? cardTransform.Find("NormalContentGroup/DroneIcon")
                               ?? cardTransform.Find("NormalContentGroup/ChipIcon")
                               ?? cardTransform.Find("Icon")
                               ?? cardTransform.Find("DroneIcon")
                               ?? cardTransform.Find("ChipIcon");

        if (iconTransform != null)
        {
            Image iconImg = iconTransform.GetComponent<Image>();
            if (iconImg != null)
            {
                Sprite matchingIcon = FindMatchingIcon(iconSprites, chip.iconKey, chip.chipName);
                if (matchingIcon != null)
                {
                    iconImg.sprite = matchingIcon;
                    iconImg.color = Color.white;
                    iconImg.gameObject.SetActive(true);
                    EditorUtility.SetDirty(iconImg);
                }
            }
        }

        // 2. Level Text
        Transform levelTransform = cardTransform.Find("NormalContentGroup/LevelText") ?? cardTransform.Find("LevelText");
        if (levelTransform != null)
        {
            TMP_Text levelTxt = levelTransform.GetComponent<TMP_Text>();
            if (levelTxt != null)
            {
                levelTxt.text = $"LV.{chip.level:00}";
                EditorUtility.SetDirty(levelTxt);
            }
        }

        // 3. Progress Text
        Transform progTransform = cardTransform.Find("NormalContentGroup/BottomBar/ProgressText") ?? cardTransform.Find("ProgressText");
        if (progTransform != null)
        {
            TMP_Text progTxt = progTransform.GetComponent<TMP_Text>();
            if (progTxt != null)
            {
                progTxt.text = chip.requiredCount > 0 ? $"{chip.count}/{chip.requiredCount}" : $"{chip.count}";
                EditorUtility.SetDirty(progTxt);
            }
        }

        // 4. Upgrade Arrow
        Transform arrowTransform = cardTransform.Find("NormalContentGroup/UpgradeArrowGroup") ?? cardTransform.Find("UpgradeArrowGroup");
        if (arrowTransform != null)
        {
            Image arrowImg = arrowTransform.GetComponent<Image>();
            if (arrowImg == null || arrowImg.sprite == null)
            {
                arrowTransform.gameObject.SetActive(false);
            }
            EditorUtility.SetDirty(arrowTransform.gameObject);
        }

        // 5. Card Frame - Quy tắc nâng cấp khung theo Tier của thẻ
        Image frameImg = cardTransform.GetComponent<Image>();
        if (frameImg != null && frameSprites != null && frameSprites.Length > 0)
        {
            string frameName = chip.tier == ChipTier.Holographic ? "ChipsetRed" :
                               chip.tier == ChipTier.Epic ? "ChipsetYelloe" :
                               chip.tier == ChipTier.Unique ? "ChipsetPurple" :
                               chip.tier == ChipTier.Rare ? "ChipsetBlue" : "ChipsetGreen";

            Sprite frame = frameSprites.FirstOrDefault(s => s.name.Equals(frameName, StringComparison.OrdinalIgnoreCase))
                        ?? frameSprites.FirstOrDefault(s => s.name.Equals("ChipsetGreen", StringComparison.OrdinalIgnoreCase))
                        ?? frameSprites[0];

            if (frame != null)
            {
                frameImg.sprite = frame;
                frameImg.color = Color.white;
                EditorUtility.SetDirty(frameImg);
            }
        }

        EditorUtility.SetDirty(cardTransform.gameObject);
    }

    private static Sprite FindMatchingIcon(Sprite[] source, string key, string name)
    {
        if (source == null || source.Length == 0) return null;
        string cleanKey = (key ?? string.Empty).ToLowerInvariant();
        string cleanName = (name ?? string.Empty).ToLowerInvariant();

        if (cleanKey.Contains("standard") || cleanName.Contains("standard") || cleanName.Contains("tiêu chuẩn"))
            return FindIconIn(source, "Standard Gun", "Tiêu Chuẩn");
        if (cleanKey.Contains("rifle") || cleanName.Contains("rifle") || cleanName.Contains("trường"))
            return FindIconIn(source, "Rifle", "Trường");
        if (cleanKey.Contains("punch") || cleanKey.Contains("rocket") || cleanName.Contains("punch") || cleanName.Contains("tên lửa"))
            return FindIconIn(source, "Rocket Punch", "Tên Lửa");
        if (cleanKey.Contains("blade") || cleanKey.Contains("spinning") || cleanName.Contains("blade") || cleanName.Contains("lưỡi dao"))
            return FindIconIn(source, "Spinning Blade", "Lưỡi Dao");
        if (cleanKey.Contains("multi") || cleanName.Contains("multi") || cleanName.Contains("đa tia"))
            return FindIconIn(source, "Multigun", "Đa Tia");
        if (cleanKey.Contains("turret") || cleanName.Contains("turret") || cleanName.Contains("tháp"))
            return FindIconIn(source, "Gun Turret", "Tháp Súng");
        if (cleanKey.Contains("discus") || cleanKey.Contains("spiky") || cleanName.Contains("discus") || cleanName.Contains("đĩa gai"))
            return FindIconIn(source, "Spiky Discus", "Đĩa Gai");
        if (cleanKey.Contains("shotgun") || cleanName.Contains("shotgun") || cleanName.Contains("săn"))
            return FindIconIn(source, "Shotgun", "Súng Săn");
        if (cleanKey.Contains("cable") || cleanKey.Contains("jumper") || cleanName.Contains("cable") || cleanName.Contains("hồi máu"))
            return FindIconIn(source, "Energy Jumper", "Cáp Hồi Máu");
        if (cleanKey.Contains("mine") || cleanName.Contains("mine") || cleanName.Contains("mìn"))
            return FindIconIn(source, "High-Explosive", "Mìn Nổ");

        return source[0];
    }

    private static Sprite FindIconIn(Sprite[] source, params string[] keywords)
    {
        if (source == null) return null;
        foreach (var s in source)
        {
            if (s == null) continue;
            string sName = s.name.ToLowerInvariant();
            foreach (var kw in keywords)
            {
                if (sName.Contains(kw.ToLowerInvariant()))
                    return s;
            }
        }
        return source.FirstOrDefault(s => s != null);
    }
}
#endif
