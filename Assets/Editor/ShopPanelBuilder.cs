#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ShopPanelBuilder
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string ShopAtlasPath = "Assets/Sprites/UI/Shop/khung màn shop.png";
    private const string DotSpritePath = "Assets/Sprites/UI/Shop/dot_white.png";

    static ShopPanelBuilder()
    {
        EditorApplication.delayCall += CheckAndBuildIfMissing;
    }

    private static GameObject FindInActiveScene(string name)
    {
        var currentScene = EditorSceneManager.GetActiveScene();
        foreach (var root in currentScene.GetRootGameObjects())
        {
            var match = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
            if (match != null) return match.gameObject;
        }
        return null;
    }

    public static void CheckAndBuildIfMissing()
    {
        if (Application.isPlaying) return;
        var currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.path != MainMenuScenePath) return;

        GameObject vip = FindInActiveScene("Card_VIP_Package");
        RectTransform dailyRt = FindInActiveScene("Daily_Shop_Row")?.GetComponent<RectTransform>();
        GameObject metaCarousel = FindInActiveScene("Meta_Shop_Carousel");
        if (vip == null || dailyRt == null || dailyRt.sizeDelta.y < 200f || metaCarousel == null)
        {
            BuildFullShopPanel();
        }
    }

    [MenuItem("PGE/UI/Rebuild Shop Panel (Full Visual)")]
    public static void RebuildShopPanelMenuItem()
    {
        BuildFullShopPanel();
    }

    public static void BuildFullShopPanel()
    {
        var currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.path != MainMenuScenePath)
        {
            currentScene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        }

        // 1. Load All 28 Sprites
        Sprite[] allSprites = AssetDatabase.LoadAllAssetsAtPath(ShopAtlasPath).OfType<Sprite>().ToArray();
        var spriteMap = allSprites.ToDictionary(s => s.name, StringComparer.OrdinalIgnoreCase);
        Sprite dotSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DotSpritePath);

        Debug.Log($"[ShopPanelBuilder] Loaded {allSprites.Length} shop sprites from atlas.");

        // 2. Find ShopPanel (including inactive)
        GameObject shopPanel = FindInActiveScene("ShopPanel (Scrollable)") ?? FindInActiveScene("ShopPanel");
        if (shopPanel == null)
        {
            // Try searching under Canvas
            GameObject canvas = FindInActiveScene("Canvas");
            if (canvas != null)
            {
                Transform t = canvas.transform.Find("Content/ShopPanel (Scrollable)")
                    ?? canvas.transform.Find("Content/ShopPanel")
                    ?? canvas.transform.Find("Panels/ShopPanel (Scrollable)")
                    ?? canvas.transform.Find("Panels/ShopPanel")
                    ?? canvas.GetComponentsInChildren<Transform>(true).FirstOrDefault(tr => tr.name == "ShopPanel (Scrollable)" || tr.name == "ShopPanel");
                if (t != null) shopPanel = t.gameObject;
            }
        }

        if (shopPanel == null)
        {
            Debug.LogError("[ShopPanelBuilder] Cannot find ShopPanel in MainMenu scene!");
            return;
        }

        shopPanel.name = "ShopPanel";

        // Ensure BottomNavigationController references it
        BottomNavigationController bNav = UnityEngine.Object.FindObjectOfType<BottomNavigationController>();
        if (bNav != null)
        {
            SerializedObject sNav = new SerializedObject(bNav);
            SerializedProperty items = sNav.FindProperty("items");
            if (items != null && items.arraySize > 0)
            {
                items.GetArrayElementAtIndex(0).FindPropertyRelative("panel").objectReferenceValue = shopPanel;
                sNav.ApplyModifiedProperties();
            }
        }

        Undo.RegisterFullObjectHierarchyUndo(shopPanel, "Rebuild Full Visual Shop Panel");

        // 3. Configure ScrollRect
        ScrollRect scrollRect = shopPanel.GetComponent<ScrollRect>();
        if (scrollRect == null) scrollRect = shopPanel.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.12f;
        scrollRect.scrollSensitivity = 55f;

        // 4. Viewport
        Transform viewportT = shopPanel.transform.Find("Viewport");
        if (viewportT == null)
        {
            GameObject vpObj = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            vpObj.transform.SetParent(shopPanel.transform, false);
            viewportT = vpObj.transform;
        }

        RectTransform vpRect = viewportT.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.anchoredPosition = Vector2.zero;
        vpRect.sizeDelta = Vector2.zero;
        vpRect.pivot = new Vector2(0.5f, 0.5f);

        Image vpImage = viewportT.GetComponent<Image>();
        vpImage.color = new Color(1f, 1f, 1f, 0.005f);
        Mask vpMask = viewportT.GetComponent<Mask>();
        vpMask.showMaskGraphic = false;
        scrollRect.viewport = vpRect;

        // 5. ShopContent
        Transform contentT = viewportT.Find("ShopContent");
        if (contentT == null)
        {
            GameObject contentObj = new GameObject("ShopContent", typeof(RectTransform));
            contentObj.transform.SetParent(viewportT, false);
            contentT = contentObj.transform;
        }

        // Clean old children in ShopContent
        for (int i = contentT.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(contentT.GetChild(i).gameObject);
        }

        RectTransform contentRect = contentT.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 6500f);
        scrollRect.content = contentRect;

        VerticalLayoutGroup vLayout = contentT.GetComponent<VerticalLayoutGroup>();
        if (vLayout == null) vLayout = contentT.gameObject.AddComponent<VerticalLayoutGroup>();
        vLayout.padding = new RectOffset(40, 40, 180, 260); // 180px top for TopBar, 260px bottom for BottomNav
        vLayout.spacing = 28f;
        vLayout.childAlignment = TextAnchor.UpperCenter;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;
        vLayout.childForceExpandWidth = false;
        vLayout.childForceExpandHeight = false;

        ContentSizeFitter sizeFitter = contentT.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null) sizeFitter = contentT.gameObject.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        List<Button> offerButtons = new List<Button>();
        List<string> offerNames = new List<string>();

        // -------------------------------------------------------------
        // SECTION 1: VIP Package Card
        // -------------------------------------------------------------
        Sprite spVip = GetSprite(spriteMap, "Card_VIP_Package");
        GameObject vipObj = CreateCardItem("Card_VIP_Package", contentRect, spVip, 980f, 628f, out Button vipBtn);
        offerButtons.Add(vipBtn);
        offerNames.Add("vip-package");

        // -------------------------------------------------------------
        // SECTION 2: Header Special Item
        // -------------------------------------------------------------
        Sprite spHeaderSpecial = GetSprite(spriteMap, "Header_Special_Item");
        CreateHeaderItem("Header_Special_Item", contentRect, spHeaderSpecial, 980f, 136f);

        // -------------------------------------------------------------
        // SECTION 3: Special Item Carousel (Welcome, Intermediate, Advanced)
        // -------------------------------------------------------------
        GameObject carouselObj = new GameObject("Special_Item_Carousel", typeof(RectTransform), typeof(LayoutElement));
        carouselObj.transform.SetParent(contentRect, false);
        RectTransform carouselRect = carouselObj.GetComponent<RectTransform>();
        carouselRect.sizeDelta = new Vector2(980f, 650f);
        LayoutElement carouselLE = carouselObj.GetComponent<LayoutElement>();
        carouselLE.preferredWidth = 980f;
        carouselLE.preferredHeight = 650f;

        // Cards Container
        GameObject cardsContObj = new GameObject("CardsContainer", typeof(RectTransform));
        cardsContObj.transform.SetParent(carouselRect, false);
        RectTransform cardsContRect = cardsContObj.GetComponent<RectTransform>();
        cardsContRect.anchorMin = new Vector2(0.5f, 1f);
        cardsContRect.anchorMax = new Vector2(0.5f, 1f);
        cardsContRect.pivot = new Vector2(0.5f, 1f);
        cardsContRect.anchoredPosition = Vector2.zero;
        cardsContRect.sizeDelta = new Vector2(980f, 587f);

        Sprite spWelcome = GetSprite(spriteMap, "Card_Welcome_Package");
        Sprite spInterm = GetSprite(spriteMap, "Card_Intermediate_Pack");
        Sprite spAdv = GetSprite(spriteMap, "Card_Advanced_Pack");

        GameObject welcomeObj = CreateCardItem("Card_Welcome_Package", cardsContRect, spWelcome, 980f, 587f, out Button welcomeBtn);
        GameObject intermObj = CreateCardItem("Card_Intermediate_Pack", cardsContRect, spInterm, 980f, 587f, out Button intermBtn);
        GameObject advObj = CreateCardItem("Card_Advanced_Pack", cardsContRect, spAdv, 980f, 587f, out Button advBtn);

        intermObj.SetActive(false);
        advObj.SetActive(false);

        offerButtons.Add(welcomeBtn); offerNames.Add("welcome-package");
        offerButtons.Add(intermBtn); offerNames.Add("intermediate-pack");
        offerButtons.Add(advBtn); offerNames.Add("advanced-pack");

        // Pagination Dots Container
        GameObject dotsContObj = new GameObject("PaginationDots", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        dotsContObj.transform.SetParent(carouselRect, false);
        RectTransform dotsContRect = dotsContObj.GetComponent<RectTransform>();
        dotsContRect.anchorMin = new Vector2(0.5f, 0f);
        dotsContRect.anchorMax = new Vector2(0.5f, 0f);
        dotsContRect.pivot = new Vector2(0.5f, 0f);
        dotsContRect.anchoredPosition = new Vector2(0f, 12f);
        dotsContRect.sizeDelta = new Vector2(220f, 36f);

        HorizontalLayoutGroup dotsLayout = dotsContObj.GetComponent<HorizontalLayoutGroup>();
        dotsLayout.spacing = 16f;
        dotsLayout.childAlignment = TextAnchor.MiddleCenter;
        dotsLayout.childControlWidth = false;
        dotsLayout.childControlHeight = false;

        Image[] dots = new Image[3];
        for (int d = 0; d < 3; d++)
        {
            GameObject dotObj = new GameObject($"Dot_{d}", typeof(RectTransform), typeof(Image), typeof(Button));
            dotObj.transform.SetParent(dotsContRect, false);
            RectTransform dotRt = dotObj.GetComponent<RectTransform>();
            dotRt.sizeDelta = new Vector2(20f, 20f);
            Image dotImg = dotObj.GetComponent<Image>();
            dotImg.sprite = dotSprite;
            dotImg.color = (d == 0) ? Color.white : new Color(0.7f, 0.85f, 0.95f, 0.45f);
            dotImg.preserveAspect = true;
            dots[d] = dotImg;
        }

        ShopCarouselUI carouselUI = carouselObj.AddComponent<ShopCarouselUI>();
        carouselUI.Setup(
            new[] { welcomeObj.GetComponent<RectTransform>(), intermObj.GetComponent<RectTransform>(), advObj.GetComponent<RectTransform>() },
            dots
        );

        // -------------------------------------------------------------
        // SECTION 4: Header Daily Shop
        // -------------------------------------------------------------
        Sprite spHeaderDaily = GetSprite(spriteMap, "Header_Daily_Shop");
        CreateHeaderItem("Header_Daily_Shop", contentRect, spHeaderDaily, 980f, 136f);

        // -------------------------------------------------------------
        // SECTION 5: Daily Shop Items Row (3 items)
        // -------------------------------------------------------------
        GameObject dailyRowObj = new GameObject("Daily_Shop_Row", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
        dailyRowObj.transform.SetParent(contentRect, false);
        RectTransform dailyRt = dailyRowObj.GetComponent<RectTransform>();
        dailyRt.anchorMin = new Vector2(0.5f, 1f);
        dailyRt.anchorMax = new Vector2(0.5f, 1f);
        dailyRt.pivot = new Vector2(0.5f, 1f);
        dailyRt.sizeDelta = new Vector2(980f, 365f);
        LayoutElement dailyLe = dailyRowObj.GetComponent<LayoutElement>();
        dailyLe.preferredWidth = 980f;
        dailyLe.preferredHeight = 365f;
        HorizontalLayoutGroup dailyLayout = dailyRowObj.GetComponent<HorizontalLayoutGroup>();
        dailyLayout.spacing = 20f;
        dailyLayout.childAlignment = TextAnchor.MiddleCenter;
        dailyLayout.childControlWidth = false;
        dailyLayout.childControlHeight = false;
        dailyLayout.childForceExpandWidth = false;
        dailyLayout.childForceExpandHeight = false;

        Sprite spDailyGem = GetSprite(spriteMap, "Item_Daily_Gem_Free");
        Sprite spDailyDrone1 = GetSprite(spriteMap, "Item_Daily_Drone_Box_1");
        Sprite spDailyDrone2 = GetSprite(spriteMap, "Item_Daily_Drone_Box_2");

        CreateCardItem("Item_Daily_Gem_Free", dailyRowObj.transform, spDailyGem, 310f, 365f, out Button btnDailyGem);
        CreateCardItem("Item_Daily_Drone_Box_1", dailyRowObj.transform, spDailyDrone1, 310f, 365f, out Button btnDailyDrone1);
        CreateCardItem("Item_Daily_Drone_Box_2", dailyRowObj.transform, spDailyDrone2, 310f, 365f, out Button btnDailyDrone2);

        offerButtons.Add(btnDailyGem); offerNames.Add("free-gem");
        offerButtons.Add(btnDailyDrone1); offerNames.Add("daily-drone-1");
        offerButtons.Add(btnDailyDrone2); offerNames.Add("daily-drone-2");

        // -------------------------------------------------------------
        // SECTION 6: Header Box
        // -------------------------------------------------------------
        Sprite spHeaderBox = GetSprite(spriteMap, "Header_Box");
        CreateHeaderItem("Header_Box", contentRect, spHeaderBox, 980f, 136f);

        // -------------------------------------------------------------
        // SECTION 7: Box Gacha Section (2 rows of 2)
        // -------------------------------------------------------------
        GameObject boxSecObj = new GameObject("Box_Gacha_Section", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
        boxSecObj.transform.SetParent(contentRect, false);
        RectTransform boxSecRt = boxSecObj.GetComponent<RectTransform>();
        boxSecRt.anchorMin = new Vector2(0.5f, 1f);
        boxSecRt.anchorMax = new Vector2(0.5f, 1f);
        boxSecRt.pivot = new Vector2(0.5f, 1f);
        boxSecRt.sizeDelta = new Vector2(980f, 840f);
        LayoutElement boxSecLe = boxSecObj.GetComponent<LayoutElement>();
        boxSecLe.preferredWidth = 980f;
        boxSecLe.preferredHeight = 840f;
        VerticalLayoutGroup boxSecLayout = boxSecObj.GetComponent<VerticalLayoutGroup>();
        boxSecLayout.spacing = 20f;
        boxSecLayout.childAlignment = TextAnchor.UpperCenter;
        boxSecLayout.childControlWidth = true;
        boxSecLayout.childControlHeight = true;
        boxSecLayout.childForceExpandWidth = false;
        boxSecLayout.childForceExpandHeight = false;

        // Row 1: Chipset Box (1x, 10x)
        GameObject rowChipBox = CreateHRow("Chipset_Box_Row", boxSecObj.transform, 980f, 410f, 20f);
        Sprite spChip1 = GetSprite(spriteMap, "Box_Chipset_1x");
        Sprite spChip10 = GetSprite(spriteMap, "Box_Chipset_10x");
        CreateCardItem("Box_Chipset_1x", rowChipBox.transform, spChip1, 475f, 410f, out Button btnChip1);
        CreateCardItem("Box_Chipset_10x", rowChipBox.transform, spChip10, 475f, 410f, out Button btnChip10);
        offerButtons.Add(btnChip1); offerNames.Add("chipset-box-1");
        offerButtons.Add(btnChip10); offerNames.Add("chipset-box-10");

        // Row 2: Drone Box (1x, 10x)
        GameObject rowDroneBox = CreateHRow("Drone_Box_Row", boxSecObj.transform, 980f, 410f, 20f);
        Sprite spDrone1 = GetSprite(spriteMap, "Box_Drone_1x");
        Sprite spDrone10 = GetSprite(spriteMap, "Box_Drone_10x");
        CreateCardItem("Box_Drone_1x", rowDroneBox.transform, spDrone1, 475f, 410f, out Button btnDrone1);
        CreateCardItem("Box_Drone_10x", rowDroneBox.transform, spDrone10, 475f, 410f, out Button btnDrone10);
        offerButtons.Add(btnDrone1); offerNames.Add("drone-box-1");
        offerButtons.Add(btnDrone10); offerNames.Add("drone-box-10");

        // -------------------------------------------------------------
        // SECTION 8: Header Meta Shop
        // -------------------------------------------------------------
        Sprite spHeaderMeta = GetSprite(spriteMap, "Header_Meta_Shop");
        CreateHeaderItem("Header_Meta_Shop", contentRect, spHeaderMeta, 980f, 136f);

        // -------------------------------------------------------------
        // SECTION 9: Meta Shop Carousel (Gun Pack, Drone Pack)
        // -------------------------------------------------------------
        GameObject metaCarouselObj = new GameObject("Meta_Shop_Carousel", typeof(RectTransform), typeof(LayoutElement));
        metaCarouselObj.transform.SetParent(contentRect, false);
        RectTransform metaCarouselRect = metaCarouselObj.GetComponent<RectTransform>();
        metaCarouselRect.anchorMin = new Vector2(0.5f, 1f);
        metaCarouselRect.anchorMax = new Vector2(0.5f, 1f);
        metaCarouselRect.pivot = new Vector2(0.5f, 1f);
        metaCarouselRect.sizeDelta = new Vector2(980f, 650f);
        LayoutElement metaCarouselLE = metaCarouselObj.GetComponent<LayoutElement>();
        metaCarouselLE.preferredWidth = 980f;
        metaCarouselLE.preferredHeight = 650f;

        // Cards Container
        GameObject metaCardsContObj = new GameObject("CardsContainer", typeof(RectTransform));
        metaCardsContObj.transform.SetParent(metaCarouselRect, false);
        RectTransform metaCardsContRect = metaCardsContObj.GetComponent<RectTransform>();
        metaCardsContRect.anchorMin = new Vector2(0.5f, 1f);
        metaCardsContRect.anchorMax = new Vector2(0.5f, 1f);
        metaCardsContRect.pivot = new Vector2(0.5f, 1f);
        metaCardsContRect.anchoredPosition = Vector2.zero;
        metaCardsContRect.sizeDelta = new Vector2(980f, 587f);

        Sprite spGunPack = GetSprite(spriteMap, "Card_Gun_Pack");
        Sprite spDronePack = GetSprite(spriteMap, "Card_Drone_Pack");

        GameObject gunPackObj = CreateCardItem("Card_Gun_Pack", metaCardsContRect, spGunPack, 980f, 587f, out Button btnGunPack);
        GameObject dronePackObj = CreateCardItem("Card_Drone_Pack", metaCardsContRect, spDronePack, 980f, 587f, out Button btnDronePack);

        dronePackObj.SetActive(false);

        offerButtons.Add(btnGunPack); offerNames.Add("gun-pack");
        offerButtons.Add(btnDronePack); offerNames.Add("drone-pack");

        // Pagination Dots Container (2 dots)
        GameObject metaDotsContObj = new GameObject("PaginationDots", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        metaDotsContObj.transform.SetParent(metaCarouselRect, false);
        RectTransform metaDotsContRect = metaDotsContObj.GetComponent<RectTransform>();
        metaDotsContRect.anchorMin = new Vector2(0.5f, 0f);
        metaDotsContRect.anchorMax = new Vector2(0.5f, 0f);
        metaDotsContRect.pivot = new Vector2(0.5f, 0f);
        metaDotsContRect.anchoredPosition = new Vector2(0f, 12f);
        metaDotsContRect.sizeDelta = new Vector2(160f, 36f);

        HorizontalLayoutGroup metaDotsLayout = metaDotsContObj.GetComponent<HorizontalLayoutGroup>();
        metaDotsLayout.spacing = 16f;
        metaDotsLayout.childAlignment = TextAnchor.MiddleCenter;
        metaDotsLayout.childControlWidth = false;
        metaDotsLayout.childControlHeight = false;

        Image[] metaDots = new Image[2];
        for (int d = 0; d < 2; d++)
        {
            GameObject dotObj = new GameObject($"Dot_{d}", typeof(RectTransform), typeof(Image), typeof(Button));
            dotObj.transform.SetParent(metaDotsContRect, false);
            RectTransform dotRt = dotObj.GetComponent<RectTransform>();
            dotRt.sizeDelta = new Vector2(20f, 20f);
            Image dotImg = dotObj.GetComponent<Image>();
            dotImg.sprite = dotSprite;
            dotImg.color = (d == 0) ? Color.white : new Color(0.7f, 0.85f, 0.95f, 0.45f);
            dotImg.preserveAspect = true;
            metaDots[d] = dotImg;
        }

        ShopCarouselUI metaCarouselUI = metaCarouselObj.AddComponent<ShopCarouselUI>();
        metaCarouselUI.Setup(
            new[] { gunPackObj.GetComponent<RectTransform>(), dronePackObj.GetComponent<RectTransform>() },
            metaDots
        );

        // -------------------------------------------------------------
        // SECTION 10: Header Data Chip
        // -------------------------------------------------------------
        Sprite spHeaderData = GetSprite(spriteMap, "Header_Data_Chip");
        CreateHeaderItem("Header_Data_Chip", contentRect, spHeaderData, 980f, 136f);

        // -------------------------------------------------------------
        // SECTION 11: Data Chip Row (3 items)
        // -------------------------------------------------------------
        GameObject dataRowObj = CreateHRow("Data_Chip_Row", contentRect, 980f, 365f, 20f);
        Sprite spData1 = GetSprite(spriteMap, "Item_Data_Chip_1");
        Sprite spData2 = GetSprite(spriteMap, "Item_Data_Chip_2");
        Sprite spData3 = GetSprite(spriteMap, "Item_Data_Chip_3");

        CreateCardItem("Item_Data_Chip_1", dataRowObj.transform, spData1, 310f, 365f, out Button btnData1);
        CreateCardItem("Item_Data_Chip_2", dataRowObj.transform, spData2, 310f, 365f, out Button btnData2);
        CreateCardItem("Item_Data_Chip_3", dataRowObj.transform, spData3, 310f, 365f, out Button btnData3);

        offerButtons.Add(btnData1); offerNames.Add("data-chip-1");
        offerButtons.Add(btnData2); offerNames.Add("data-chip-2");
        offerButtons.Add(btnData3); offerNames.Add("data-chip-3");

        // -------------------------------------------------------------
        // SECTION 12: Header Gem Event
        // -------------------------------------------------------------
        Sprite spHeaderGem = GetSprite(spriteMap, "Header_Gem_Event");
        CreateHeaderItem("Header_Gem_Event", contentRect, spHeaderGem, 980f, 150f);

        // -------------------------------------------------------------
        // SECTION 13: Gem Event Section (2 rows of 3)
        // -------------------------------------------------------------
        GameObject gemSecObj = new GameObject("Gem_Event_Section", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
        gemSecObj.transform.SetParent(contentRect, false);
        RectTransform gemSecRt = gemSecObj.GetComponent<RectTransform>();
        gemSecRt.anchorMin = new Vector2(0.5f, 1f);
        gemSecRt.anchorMax = new Vector2(0.5f, 1f);
        gemSecRt.pivot = new Vector2(0.5f, 1f);
        gemSecRt.sizeDelta = new Vector2(980f, 750f);
        LayoutElement gemSecLe = gemSecObj.GetComponent<LayoutElement>();
        gemSecLe.preferredWidth = 980f;
        gemSecLe.preferredHeight = 750f;
        VerticalLayoutGroup gemSecLayout = gemSecObj.GetComponent<VerticalLayoutGroup>();
        gemSecLayout.spacing = 20f;
        gemSecLayout.childAlignment = TextAnchor.UpperCenter;
        gemSecLayout.childControlWidth = true;
        gemSecLayout.childControlHeight = true;
        gemSecLayout.childForceExpandWidth = false;
        gemSecLayout.childForceExpandHeight = false;

        // Gem Row 1
        GameObject gemRow1 = CreateHRow("Gem_Row_1", gemSecObj.transform, 980f, 365f, 20f);
        Sprite spGem1 = GetSprite(spriteMap, "Item_Gem_1");
        Sprite spGem2 = GetSprite(spriteMap, "Item_Gem_2");
        Sprite spGem3 = GetSprite(spriteMap, "Item_Gem_3");
        CreateCardItem("Item_Gem_1", gemRow1.transform, spGem1, 310f, 365f, out Button btnGem1);
        CreateCardItem("Item_Gem_2", gemRow1.transform, spGem2, 310f, 365f, out Button btnGem2);
        CreateCardItem("Item_Gem_3", gemRow1.transform, spGem3, 310f, 365f, out Button btnGem3);

        // Gem Row 2
        GameObject gemRow2 = CreateHRow("Gem_Row_2", gemSecObj.transform, 980f, 365f, 20f);
        Sprite spGem4 = GetSprite(spriteMap, "Item_Gem_4");
        Sprite spGem5 = GetSprite(spriteMap, "Item_Gem_5");
        Sprite spGem6 = GetSprite(spriteMap, "Item_Gem_6");
        CreateCardItem("Item_Gem_4", gemRow2.transform, spGem4, 310f, 365f, out Button btnGem4);
        CreateCardItem("Item_Gem_5", gemRow2.transform, spGem5, 310f, 365f, out Button btnGem5);
        CreateCardItem("Item_Gem_6", gemRow2.transform, spGem6, 310f, 365f, out Button btnGem6);

        offerButtons.Add(btnGem1); offerNames.Add("gem-1");
        offerButtons.Add(btnGem2); offerNames.Add("gem-2");
        offerButtons.Add(btnGem3); offerNames.Add("gem-3");
        offerButtons.Add(btnGem4); offerNames.Add("gem-4");
        offerButtons.Add(btnGem5); offerNames.Add("gem-5");
        offerButtons.Add(btnGem6); offerNames.Add("gem-6");

        // -------------------------------------------------------------
        // Wire ShopController
        // -------------------------------------------------------------
        ShopController controller = shopPanel.GetComponent<ShopController>();
        if (controller == null) controller = shopPanel.AddComponent<ShopController>();

        SerializedObject sController = new SerializedObject(controller);
        
        // Find Toast
        GameObject toastRoot = GameObject.Find("BuddyToastMessage") ?? GameObject.Find("ToastMessage");
        if (toastRoot != null)
        {
            sController.FindProperty("toastRoot").objectReferenceValue = toastRoot;
            TMP_Text tText = toastRoot.GetComponentInChildren<TMP_Text>(true);
            if (tText != null) sController.FindProperty("toastText").objectReferenceValue = tText;
        }

        // Configure Offers
        SerializedProperty offersProp = sController.FindProperty("offers");
        offersProp.arraySize = offerButtons.Count;

        for (int i = 0; i < offerButtons.Count; i++)
        {
            SerializedProperty offerElem = offersProp.GetArrayElementAtIndex(i);
            string id = offerNames[i];
            Button btn = offerButtons[i];

            offerElem.FindPropertyRelative("id").stringValue = id;
            offerElem.FindPropertyRelative("displayName").stringValue = id.Replace("-", " ").ToUpper();
            offerElem.FindPropertyRelative("button").objectReferenceValue = btn;
            offerElem.FindPropertyRelative("priceText").objectReferenceValue = null;

            if (id == "free-gem")
            {
                offerElem.FindPropertyRelative("currency").enumValueIndex = (int)ShopController.CurrencyType.Free;
                offerElem.FindPropertyRelative("price").intValue = 0;
                offerElem.FindPropertyRelative("reward").enumValueIndex = (int)ShopController.RewardType.RedGem;
                offerElem.FindPropertyRelative("rewardAmount").intValue = 80;
                offerElem.FindPropertyRelative("oncePerDay").boolValue = true;
            }
            else if (id.StartsWith("daily-drone"))
            {
                offerElem.FindPropertyRelative("currency").enumValueIndex = (int)ShopController.CurrencyType.RedGem;
                offerElem.FindPropertyRelative("price").intValue = 180;
                offerElem.FindPropertyRelative("reward").enumValueIndex = (int)ShopController.RewardType.DroneBox;
                offerElem.FindPropertyRelative("rewardAmount").intValue = 1;
                offerElem.FindPropertyRelative("oncePerDay").boolValue = true;
            }
            else if (id == "chipset-box-1")
            {
                offerElem.FindPropertyRelative("currency").enumValueIndex = (int)ShopController.CurrencyType.RedGem;
                offerElem.FindPropertyRelative("price").intValue = 300;
                offerElem.FindPropertyRelative("reward").enumValueIndex = (int)ShopController.RewardType.ChipsetBox;
                offerElem.FindPropertyRelative("rewardAmount").intValue = 1;
                offerElem.FindPropertyRelative("oncePerDay").boolValue = false;
            }
            else if (id == "chipset-box-10")
            {
                offerElem.FindPropertyRelative("currency").enumValueIndex = (int)ShopController.CurrencyType.RedGem;
                offerElem.FindPropertyRelative("price").intValue = 2700;
                offerElem.FindPropertyRelative("reward").enumValueIndex = (int)ShopController.RewardType.ChipsetBox;
                offerElem.FindPropertyRelative("rewardAmount").intValue = 10;
                offerElem.FindPropertyRelative("oncePerDay").boolValue = false;
            }
            else if (id == "drone-box-1")
            {
                offerElem.FindPropertyRelative("currency").enumValueIndex = (int)ShopController.CurrencyType.RedGem;
                offerElem.FindPropertyRelative("price").intValue = 600;
                offerElem.FindPropertyRelative("reward").enumValueIndex = (int)ShopController.RewardType.DroneBox;
                offerElem.FindPropertyRelative("rewardAmount").intValue = 1;
                offerElem.FindPropertyRelative("oncePerDay").boolValue = false;
            }
            else if (id == "drone-box-10")
            {
                offerElem.FindPropertyRelative("currency").enumValueIndex = (int)ShopController.CurrencyType.RedGem;
                offerElem.FindPropertyRelative("price").intValue = 5400;
                offerElem.FindPropertyRelative("reward").enumValueIndex = (int)ShopController.RewardType.DroneBox;
                offerElem.FindPropertyRelative("rewardAmount").intValue = 10;
                offerElem.FindPropertyRelative("oncePerDay").boolValue = false;
            }
            else if (id.StartsWith("data-chip"))
            {
                offerElem.FindPropertyRelative("currency").enumValueIndex = (int)ShopController.CurrencyType.RedGem;
                int price = id.EndsWith("1") ? 200 : id.EndsWith("2") ? 400 : 1000;
                int amount = id.EndsWith("1") ? 2000 : id.EndsWith("2") ? 4250 : 12200;
                offerElem.FindPropertyRelative("price").intValue = price;
                offerElem.FindPropertyRelative("reward").enumValueIndex = (int)ShopController.RewardType.DataChip;
                offerElem.FindPropertyRelative("rewardAmount").intValue = amount;
                offerElem.FindPropertyRelative("oncePerDay").boolValue = false;
            }
            else // VND / IAP Packs
            {
                offerElem.FindPropertyRelative("currency").enumValueIndex = (int)ShopController.CurrencyType.VND;
                offerElem.FindPropertyRelative("price").intValue = 0;
                offerElem.FindPropertyRelative("reward").enumValueIndex = (int)ShopController.RewardType.RedGem;
                offerElem.FindPropertyRelative("rewardAmount").intValue = 1;
                offerElem.FindPropertyRelative("oncePerDay").boolValue = false;
            }
        }

        sController.ApplyModifiedProperties();

        // -------------------------------------------------------------
        // Explicitly compute anchoredPosition for all direct children of ShopContent
        // so that Scene view and YAML display perfectly even before Play Mode!
        // -------------------------------------------------------------
        float currentY = -180f; // top padding for TopBar
        for (int c = 0; c < contentRect.childCount; c++)
        {
            RectTransform childRt = contentRect.GetChild(c) as RectTransform;
            if (childRt == null) continue;

            childRt.anchorMin = new Vector2(0.5f, 1f);
            childRt.anchorMax = new Vector2(0.5f, 1f);
            childRt.pivot = new Vector2(0.5f, 1f);
            childRt.anchoredPosition = new Vector2(0f, currentY);

            float h = childRt.sizeDelta.y;
            currentY -= (h + 28f); // 28px spacing
        }

        float totalContentHeight = Mathf.Abs(currentY) - 28f + 260f; // 260px bottom padding for BottomNav
        contentRect.sizeDelta = new Vector2(0f, totalContentHeight);

        // Force rebuild all layouts from inner to outer so all sub-containers & cards are positioned!
        Canvas.ForceUpdateCanvases();
        foreach (var hlg in contentT.GetComponentsInChildren<HorizontalLayoutGroup>(true))
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(hlg.GetComponent<RectTransform>());
        }
        foreach (var vlg in contentT.GetComponentsInChildren<VerticalLayoutGroup>(true))
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(vlg.GetComponent<RectTransform>());
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        Canvas.ForceUpdateCanvases();

        EditorUtility.SetDirty(shopPanel);
        EditorSceneManager.SaveScene(currentScene);

        Debug.Log($"[ShopPanelBuilder] Successfully built full visual ShopPanel with all 28 sprites and {offerButtons.Count} interactive offers! Total height = {totalContentHeight}px");
    }

    private static Sprite GetSprite(Dictionary<string, Sprite> map, string name)
    {
        if (map.TryGetValue(name, out Sprite s)) return s;
        Debug.LogWarning($"[ShopPanelBuilder] Missing sprite: {name}");
        return null;
    }

    private static GameObject CreateCardItem(string name, Transform parent, Sprite sprite, float w, float h, out Button btn)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);

        LayoutElement le = obj.GetComponent<LayoutElement>();
        le.preferredWidth = w;
        le.preferredHeight = h;

        Image img = obj.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = true;

        btn = obj.GetComponent<Button>();
        btn.targetGraphic = img;

        // Button Animation transition
        btn.transition = Selectable.Transition.ColorTint;
        var cb = btn.colors;
        cb.highlightedColor = new Color(0.92f, 0.96f, 1f, 1f);
        cb.pressedColor = new Color(0.82f, 0.88f, 0.95f, 1f);
        cb.disabledColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = cb;

        return obj;
    }

    private static GameObject CreateHeaderItem(string name, Transform parent, Sprite sprite, float w, float h)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(w, h);

        LayoutElement le = obj.GetComponent<LayoutElement>();
        le.preferredWidth = w;
        le.preferredHeight = h;

        Image img = obj.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        return obj;
    }

    private static GameObject CreateHRow(string name, Transform parent, float w, float h, float spacing)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(w, h);

        LayoutElement le = obj.GetComponent<LayoutElement>();
        le.preferredWidth = w;
        le.preferredHeight = h;

        HorizontalLayoutGroup layout = obj.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return obj;
    }
}
#endif
