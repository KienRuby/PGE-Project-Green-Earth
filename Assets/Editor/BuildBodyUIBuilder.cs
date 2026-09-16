#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor Builder tự động dựng giao diện Build Body theo mẫu đúng 100% với ảnh:
/// - Nút Tab Build body có asset khóa và mở (Build body vs Build body locked).
/// - Khung cuộn dọc chứa 4 Card (AD Unit-1, 2, 3, 4).
/// - Thẻ đang trang bị có nền XANH LÁ (Panel_Bottom_Green), nhãn Current version, nút Change skin.
/// - Các thẻ khác có nền XANH ĐẬM (Panel_Top_DarkBlue).
/// - AD Unit-4 có nút Build với giá 500 Ngọc Đỏ.
/// - Kết nối đầy đủ 100% SerializedProperty với BuildBodyController.
/// </summary>
public static class BuildBodyUIBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string BuildRequestPath = "Assets/Editor/PGE_BuildBodyUI_BuildRequest.txt";

    // Sprites Paths
    private const string LabButtonsPath = "Assets/Sprites/UI/Lab/nút màn lab.png";
    private const string GreenCardBgPath = "Assets/Sprites/UI/Buil body/Panel_Bottom_Green.png";
    private const string DarkBlueCardBgPath = "Assets/Sprites/UI/Buil body/Panel_Top_DarkBlue.png";
    private const string SlotFramePath = "Assets/Sprites/UI/Buil body/Slot_1_LightBlue.png";
    private const string ChangeSkinBtnPath = "Assets/Sprites/UI/Buil body/Btn_ChangeSkin.png";
    private const string BuildBtnPath = "Assets/Sprites/UI/Buil body/Btn_BuildBody.png";
    private const string RobotBluePath = "Assets/Sprites/UI/Buil body/Robot_Skin_Blue.png";
    private const string RobotGreenPath = "Assets/Sprites/UI/Buil body/Robot_Skin_Green.png";
    private const string RobotPurplePath = "Assets/Sprites/UI/Buil body/Robot_Skin_Purple.png";
    private const string RobotBlackPath = "Assets/Sprites/UI/Buil body/Robot_Skin_Black.png";
    private const string ResourceIconPath = "Assets/Sprites/UI/icon tài nguyên.png";

    // Font Paths
    private const string NunitoFontPath = "Assets/Fonts/Nunito/Nunito SDF.asset";
    private const string DefaultFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    private static void TryBuildRequestedUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        if (!File.Exists(BuildRequestPath))
        {
            return;
        }

        try
        {
            File.Delete(BuildRequestPath);
            if (File.Exists(BuildRequestPath + ".meta"))
            {
                File.Delete(BuildRequestPath + ".meta");
            }
        }
        catch { }

        BuildUI();
    }

    [MenuItem("PGE/UI/Build Build Body UI (100% Match)")]
    public static void BuildUIFromMenu()
    {
        BuildUI();
    }

    public static void BuildUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[BuildBodyUIBuilder] Stop Play Mode before rebuilding the Build Body UI.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // 1. Tải Font
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NunitoFontPath)
                          ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontPath);

        // 2. Tải Sprites
        Sprite[] labSprites = AssetDatabase.LoadAllAssetsAtPath(LabButtonsPath).OfType<Sprite>().ToArray();
        Sprite statsSprite = labSprites.FirstOrDefault(s => s.name == "Stats");
        Sprite buildBodyUnlockedSprite = labSprites.FirstOrDefault(s => s.name == "Build body");
        Sprite buildBodyLockedSprite = labSprites.FirstOrDefault(s => s.name == "Build body locked");

        Sprite greenCardBg = AssetDatabase.LoadAssetAtPath<Sprite>(GreenCardBgPath);
        Sprite darkBlueCardBg = AssetDatabase.LoadAssetAtPath<Sprite>(DarkBlueCardBgPath);
        Sprite slotFrame = AssetDatabase.LoadAssetAtPath<Sprite>(SlotFramePath);
        Sprite changeSkinBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ChangeSkinBtnPath);
        Sprite buildBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BuildBtnPath);

        Sprite robotBlue = AssetDatabase.LoadAssetAtPath<Sprite>(RobotBluePath);
        Sprite robotGreen = AssetDatabase.LoadAssetAtPath<Sprite>(RobotGreenPath);
        Sprite robotPurple = AssetDatabase.LoadAssetAtPath<Sprite>(RobotPurplePath);
        Sprite robotBlack = AssetDatabase.LoadAssetAtPath<Sprite>(RobotBlackPath);

        Sprite[] resSprites = AssetDatabase.LoadAllAssetsAtPath(ResourceIconPath).OfType<Sprite>().ToArray();
        Sprite redGemIcon = resSprites.FirstOrDefault(s => s.name == "red");

        // 3. Tìm LabPanel
        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[BuildBodyUIBuilder] Canvas not found!");
            return;
        }

        Transform content = canvas.transform.Find("Content");
        if (content == null)
        {
            Debug.LogError("[BuildBodyUIBuilder] Canvas/Content not found!");
            return;
        }

        Transform labPanel = content.Find("LabPanel");
        if (labPanel == null)
        {
            Debug.LogError("[BuildBodyUIBuilder] LabPanel not found under Canvas/Content!");
            return;
        }

        Transform statsPanel = labPanel.Find("StatsPanel");
        Transform topTabs = labPanel.Find("TopTabs");

        // 4. Cấu hình TopTabs
        Button statsButton = null;
        Button buildBodyButton = null;
        Image buildBodyImage = null;

        if (topTabs != null)
        {
            Transform statsBtnTrans = topTabs.Find("StatsButton");
            if (statsBtnTrans != null)
            {
                statsButton = statsBtnTrans.GetComponent<Button>() ?? statsBtnTrans.gameObject.AddComponent<Button>();
                Image img = statsBtnTrans.GetComponent<Image>();
                if (img != null && statsSprite != null)
                {
                    img.sprite = statsSprite;
                    img.color = Color.white;
                }
                // Ẩn text cũ nếu có
                Transform oldLabel = statsBtnTrans.Find("Label");
                if (oldLabel != null) oldLabel.gameObject.SetActive(false);
            }

            Transform buildBtnTrans = topTabs.Find("BuildBodyButton");
            if (buildBtnTrans != null)
            {
                buildBodyButton = buildBtnTrans.GetComponent<Button>() ?? buildBtnTrans.gameObject.AddComponent<Button>();
                buildBodyImage = buildBtnTrans.GetComponent<Image>() ?? buildBtnTrans.gameObject.AddComponent<Image>();
                bool isUnlocked = PlayerDataService.UnlockedChapterIndex >= 3 || ChipManager.IsTestMode;
                if (buildBodyUnlockedSprite != null && buildBodyLockedSprite != null)
                {
                    buildBodyImage.sprite = isUnlocked ? buildBodyUnlockedSprite : buildBodyLockedSprite;
                    buildBodyImage.color = Color.white;
                }
                buildBodyButton.targetGraphic = buildBodyImage;
                buildBodyButton.interactable = true;
                buildBodyImage.raycastTarget = true;

                // Ẩn icon lock và label cũ nếu có
                Transform oldLock = buildBtnTrans.Find("LockIcon");
                if (oldLock != null) oldLock.gameObject.SetActive(false);
                Transform oldLabel = buildBtnTrans.Find("Label");
                if (oldLabel != null) oldLabel.gameObject.SetActive(false);
            }
        }

        // 5. Xây dựng hoặc Làm mới BuildBodyPanel
        Transform existingBuildPanel = labPanel.Find("BuildBodyPanel");
        if (existingBuildPanel != null)
        {
            UnityEngine.Object.DestroyImmediate(existingBuildPanel.gameObject);
        }

        GameObject buildPanelObj = new GameObject("BuildBodyPanel", typeof(RectTransform));
        buildPanelObj.transform.SetParent(labPanel, false);
        RectTransform buildPanelRect = buildPanelObj.GetComponent<RectTransform>();
        buildPanelRect.anchorMin = Vector2.zero;
        buildPanelRect.anchorMax = Vector2.one;
        buildPanelRect.offsetMin = Vector2.zero;
        buildPanelRect.offsetMax = new Vector2(0f, -150f);
        buildPanelObj.SetActive(false);

        // ScrollView
        GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(buildPanelRect, false);
        RectTransform scrollRectTrans = scrollObj.GetComponent<RectTransform>();
        scrollRectTrans.anchorMin = Vector2.zero;
        scrollRectTrans.anchorMax = Vector2.one;
        scrollRectTrans.offsetMin = new Vector2(20f, 15f);
        scrollRectTrans.offsetMax = new Vector2(-20f, -10f);

        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.scrollSensitivity = 30f;

        // Viewport
        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObj.transform.SetParent(scrollRectTrans, false);
        RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        scrollRect.viewport = viewportRect;

        // Content
        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(viewportRect, false);
        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        scrollRect.content = contentRect;

        VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 22f;
        vlg.padding = new RectOffset(0, 0, 10, 160); // 160 padding ở dưới để không bị BottomNav che
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 6. Xây dựng 4 Card View
        BuildBodyController.BodyCardView[] cards = new BuildBodyController.BodyCardView[4];

        // Dữ liệu 4 card
        var cardInfos = new[]
        {
            new {
                unitNum = 1,
                robot = robotBlue,
                title = "Basic Body",
                desc = "Start from <color=#FFE95C>Lv.1</color>",
                status = "",
                isEquipped = false,
                isBuild = true,
                cost = 1000
            },
            new {
                unitNum = 2,
                robot = robotGreen,
                title = "",
                desc = "Bonus <color=#FFE95C>HP+50/DEF+7</color>\nStart from <color=#FFE95C>Lv.2</color>\nAilment Resistance <color=#FFE95C>+10%</color>",
                status = "",
                isEquipped = false,
                isBuild = true,
                cost = 1500
            },
            new {
                unitNum = 3,
                robot = robotPurple,
                title = "",
                desc = "Bonus <color=#FFE95C>HP+100/DEF+15</color>\nStart from <color=#FFE95C>Lv.3</color>\nAilment Resistance <color=#FFE95C>+20%</color>\n<color=#FFE95C>Gem Magnet Lv.1</color>",
                status = "",
                isEquipped = false,
                isBuild = true,
                cost = 2000
            },
            new {
                unitNum = 4,
                robot = robotBlack,
                title = "",
                desc = "Bonus <color=#FFE95C>HP+250/DEF+35</color>\nStart from <color=#FFE95C>Lv.5</color>",
                status = "",
                isEquipped = false,
                isBuild = true,
                cost = 3000
            }
        };

        for (int i = 0; i < 4; i++)
        {
            var info = cardInfos[i];
            cards[i] = CreateBodyCard(
                contentRect,
                i,
                info.unitNum,
                info.robot,
                info.title,
                info.desc,
                info.status,
                info.isEquipped,
                info.isBuild,
                info.cost,
                info.isEquipped ? greenCardBg : darkBlueCardBg,
                slotFrame,
                changeSkinBtnSprite,
                buildBtnSprite,
                redGemIcon,
                font
            );
        }

        // 7. Toast Message (Gắn trực tiếp vào labPanel để hiển thị được trong mọi tình huống)
        Transform oldToast = labPanel.Find("BuildBodyToast");
        if (oldToast != null) UnityEngine.Object.DestroyImmediate(oldToast.gameObject);

        GameObject toastObj = new GameObject("BuildBodyToast", typeof(RectTransform), typeof(Image));
        toastObj.transform.SetParent(labPanel, false);
        RectTransform toastRect = toastObj.GetComponent<RectTransform>();
        toastRect.anchorMin = new Vector2(0.5f, 0.12f);
        toastRect.anchorMax = new Vector2(0.5f, 0.12f);
        toastRect.pivot = new Vector2(0.5f, 0.5f);
        toastRect.sizeDelta = new Vector2(740f, 75f);

        Image toastImg = toastObj.GetComponent<Image>();
        toastImg.color = new Color32(10, 20, 28, 235);
        toastImg.raycastTarget = false;

        GameObject toastTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        toastTextObj.transform.SetParent(toastRect, false);
        RectTransform toastTextRect = toastTextObj.GetComponent<RectTransform>();
        toastTextRect.anchorMin = Vector2.zero;
        toastTextRect.anchorMax = Vector2.one;
        toastTextRect.offsetMin = new Vector2(20f, 5f);
        toastTextRect.offsetMax = new Vector2(-20f, -5f);

        TextMeshProUGUI toastTMP = toastTextObj.GetComponent<TextMeshProUGUI>();
        if (font != null) toastTMP.font = font;
        toastTMP.fontSize = 26f;
        toastTMP.alignment = TextAlignmentOptions.Center;
        toastTMP.color = Color.white;
        toastTMP.text = "";
        toastObj.SetActive(false);

        // 8. Thêm & Kết nối BuildBodyController trên TopTabs (luôn Active khi vào Lab)
        BuildBodyController oldPanelCtrl = buildPanelObj.GetComponent<BuildBodyController>();
        if (oldPanelCtrl != null) UnityEngine.Object.DestroyImmediate(oldPanelCtrl);

        GameObject controllerHost = topTabs != null ? topTabs.gameObject : labPanel.gameObject;
        BuildBodyController controller = controllerHost.GetComponent<BuildBodyController>() ?? controllerHost.AddComponent<BuildBodyController>();
        SerializedObject so = new SerializedObject(controller);

        so.FindProperty("statsTabButton").objectReferenceValue = statsButton;
        so.FindProperty("buildBodyTabButton").objectReferenceValue = buildBodyButton;
        so.FindProperty("buildBodyTabImage").objectReferenceValue = buildBodyImage;
        so.FindProperty("buildBodyUnlockedSprite").objectReferenceValue = buildBodyUnlockedSprite;
        so.FindProperty("buildBodyLockedSprite").objectReferenceValue = buildBodyLockedSprite;
        so.FindProperty("statsPanel").objectReferenceValue = statsPanel != null ? statsPanel.gameObject : null;
        so.FindProperty("buildBodyPanel").objectReferenceValue = buildPanelObj;
        so.FindProperty("greenCardBackground").objectReferenceValue = greenCardBg;
        so.FindProperty("darkBlueCardBackground").objectReferenceValue = darkBlueCardBg;
        so.FindProperty("toastRoot").objectReferenceValue = toastObj;
        so.FindProperty("toastText").objectReferenceValue = toastTMP;

        SerializedProperty cardViewsProp = so.FindProperty("cardViews");
        cardViewsProp.arraySize = 4;
        for (int i = 0; i < 4; i++)
        {
            SerializedProperty cProp = cardViewsProp.GetArrayElementAtIndex(i);
            cProp.FindPropertyRelative("cardRoot").objectReferenceValue = cards[i].cardRoot;
            cProp.FindPropertyRelative("backgroundImage").objectReferenceValue = cards[i].backgroundImage;
            cProp.FindPropertyRelative("selectButton").objectReferenceValue = cards[i].selectButton;
            cProp.FindPropertyRelative("slotFrameImage").objectReferenceValue = cards[i].slotFrameImage;
            cProp.FindPropertyRelative("robotImage").objectReferenceValue = cards[i].robotImage;
            cProp.FindPropertyRelative("unitNameText").objectReferenceValue = cards[i].unitNameText;
            cProp.FindPropertyRelative("titleText").objectReferenceValue = cards[i].titleText;
            cProp.FindPropertyRelative("statsDescriptionText").objectReferenceValue = cards[i].statsDescriptionText;
            cProp.FindPropertyRelative("statusText").objectReferenceValue = cards[i].statusText;
            cProp.FindPropertyRelative("changeSkinButton").objectReferenceValue = cards[i].changeSkinButton;
            cProp.FindPropertyRelative("buildGroup").objectReferenceValue = cards[i].buildGroup;
            cProp.FindPropertyRelative("buildButton").objectReferenceValue = cards[i].buildButton;
            cProp.FindPropertyRelative("buildCostText").objectReferenceValue = cards[i].buildCostText;
            cProp.FindPropertyRelative("buildCostIcon").objectReferenceValue = cards[i].buildCostIcon;
        }

        SerializedProperty buildCostsProp = so.FindProperty("unitBuildCosts");
        if (buildCostsProp != null)
        {
            buildCostsProp.arraySize = 4;
            buildCostsProp.GetArrayElementAtIndex(0).intValue = 1000;
            buildCostsProp.GetArrayElementAtIndex(1).intValue = 1500;
            buildCostsProp.GetArrayElementAtIndex(2).intValue = 2000;
            buildCostsProp.GetArrayElementAtIndex(3).intValue = 3000;
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        // 9. Lưu Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[BuildBodyUIBuilder] ✅ Đã xây dựng hoàn tất giao diện Build Body theo mẫu 100%!");
    }

    private static BuildBodyController.BodyCardView CreateBodyCard(
        RectTransform parent,
        int index,
        int unitNum,
        Sprite robotSprite,
        string title,
        string desc,
        string status,
        bool isEquipped,
        bool hasBuild,
        int cost,
        Sprite bgSprite,
        Sprite slotSprite,
        Sprite changeSkinSprite,
        Sprite buildBtnSprite,
        Sprite redGemIcon,
        TMP_FontAsset font)
    {
        var view = new BuildBodyController.BodyCardView();

        // Card Root
        GameObject cardObj = new GameObject($"Card_Unit{unitNum}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        cardObj.transform.SetParent(parent, false);
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(980f, 330f);

        LayoutElement le = cardObj.GetComponent<LayoutElement>();
        le.minHeight = 330f;
        le.preferredHeight = 330f;

        Image bgImg = cardObj.GetComponent<Image>();
        bgImg.sprite = bgSprite;
        bgImg.type = Image.Type.Simple;
        bgImg.raycastTarget = true;

        Button selectBtn = cardObj.GetComponent<Button>();
        selectBtn.targetGraphic = bgImg;

        view.cardRoot = cardObj;
        view.backgroundImage = bgImg;
        view.selectButton = selectBtn;

        // 1. Khung Slot bên trái
        GameObject slotObj = new GameObject("SlotFrame", typeof(RectTransform), typeof(Image));
        slotObj.transform.SetParent(cardRect, false);
        RectTransform slotRect = slotObj.GetComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0f, 0.5f);
        slotRect.anchorMax = new Vector2(0f, 0.5f);
        slotRect.pivot = new Vector2(0f, 0.5f);
        slotRect.anchoredPosition = new Vector2(25f, 0f);
        slotRect.sizeDelta = new Vector2(220f, 260f);

        Image slotImg = slotObj.GetComponent<Image>();
        slotImg.sprite = slotSprite;
        slotImg.raycastTarget = false;
        view.slotFrameImage = slotImg;

        // Robot Icon trong slot
        GameObject robotObj = new GameObject("RobotIcon", typeof(RectTransform), typeof(Image));
        robotObj.transform.SetParent(slotRect, false);
        RectTransform robotRect = robotObj.GetComponent<RectTransform>();
        robotRect.anchorMin = new Vector2(0.5f, 0.58f);
        robotRect.anchorMax = new Vector2(0.5f, 0.58f);
        robotRect.pivot = new Vector2(0.5f, 0.5f);
        robotRect.sizeDelta = new Vector2(175f, 195f);

        Image robotImg = robotObj.GetComponent<Image>();
        robotImg.sprite = robotSprite;
        robotImg.preserveAspect = true;
        robotImg.raycastTarget = false;
        view.robotImage = robotImg;

        // Tên Unit (AD Unit-X) dưới slot
        GameObject unitNameObj = new GameObject("UnitNameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        unitNameObj.transform.SetParent(slotRect, false);
        RectTransform unitNameRect = unitNameObj.GetComponent<RectTransform>();
        unitNameRect.anchorMin = new Vector2(0.5f, 0f);
        unitNameRect.anchorMax = new Vector2(0.5f, 0f);
        unitNameRect.pivot = new Vector2(0.5f, 0f);
        unitNameRect.anchoredPosition = new Vector2(0f, 12f);
        unitNameRect.sizeDelta = new Vector2(200f, 36f);

        TextMeshProUGUI unitNameTMP = unitNameObj.GetComponent<TextMeshProUGUI>();
        if (font != null) unitNameTMP.font = font;
        unitNameTMP.fontSize = 26f;
        unitNameTMP.fontStyle = FontStyles.Bold;
        unitNameTMP.alignment = TextAlignmentOptions.Center;
        unitNameTMP.color = Color.white;
        unitNameTMP.text = $"AD Unit-<color=#FFE95C>{unitNum}</color>";
        view.unitNameText = unitNameTMP;

        // 2. Khu vực chữ thông tin ở giữa
        GameObject infoObj = new GameObject("InfoContainer", typeof(RectTransform));
        infoObj.transform.SetParent(cardRect, false);
        RectTransform infoRect = infoObj.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0f, 0.5f);
        infoRect.anchorMax = new Vector2(0f, 0.5f);
        infoRect.pivot = new Vector2(0f, 0.5f);
        infoRect.anchoredPosition = new Vector2(270f, 0f);
        infoRect.sizeDelta = new Vector2(440f, 260f);

        // Title (Basic Body)
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(infoRect, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -12f);
        titleRect.sizeDelta = new Vector2(0f, 42f);

        TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        if (font != null) titleTMP.font = font;
        titleTMP.fontSize = 34f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = Color.white;
        titleTMP.alignment = TextAlignmentOptions.TopLeft;
        titleTMP.text = title;
        titleObj.SetActive(!string.IsNullOrEmpty(title));
        view.titleText = titleTMP;

        // Stats Description Text
        GameObject descObj = new GameObject("StatsDescriptionText", typeof(RectTransform), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(infoRect, false);
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0f, 0f);
        descRect.anchorMax = new Vector2(1f, 1f);
        descRect.pivot = new Vector2(0f, 0.5f);
        descRect.anchoredPosition = new Vector2(0f, string.IsNullOrEmpty(title) ? 0f : -25f);
        descRect.sizeDelta = new Vector2(0f, string.IsNullOrEmpty(title) ? 0f : -50f);

        TextMeshProUGUI descTMP = descObj.GetComponent<TextMeshProUGUI>();
        if (font != null) descTMP.font = font;
        descTMP.fontSize = 28f;
        descTMP.fontStyle = FontStyles.Normal;
        descTMP.color = Color.white;
        descTMP.lineSpacing = 15f;
        descTMP.alignment = TextAlignmentOptions.MidlineLeft;
        descTMP.text = desc;
        view.statsDescriptionText = descTMP;

        // 3. Khu vực trạng thái & nút bên phải
        GameObject rightObj = new GameObject("RightActionContainer", typeof(RectTransform));
        rightObj.transform.SetParent(cardRect, false);
        RectTransform rightRect = rightObj.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(1f, 0.5f);
        rightRect.anchorMax = new Vector2(1f, 0.5f);
        rightRect.pivot = new Vector2(1f, 0.5f);
        rightRect.anchoredPosition = new Vector2(-28f, 0f);
        rightRect.sizeDelta = new Vector2(250f, 260f);

        // Status Text (Current version / Previous version)
        GameObject statusObj = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusObj.transform.SetParent(rightRect, false);
        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(1f, 1f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.pivot = new Vector2(1f, 1f);
        statusRect.anchoredPosition = new Vector2(0f, -12f);
        statusRect.sizeDelta = new Vector2(250f, 40f);

        TextMeshProUGUI statusTMP = statusObj.GetComponent<TextMeshProUGUI>();
        if (font != null) statusTMP.font = font;
        statusTMP.fontSize = 25f;
        statusTMP.color = new Color32(230, 245, 245, 255);
        statusTMP.alignment = TextAlignmentOptions.TopRight;
        statusTMP.text = status;
        statusObj.SetActive(!string.IsNullOrEmpty(status));
        view.statusText = statusTMP;

        // Nút Change skin (hồng)
        GameObject changeSkinBtnObj = new GameObject("ChangeSkinButton", typeof(RectTransform), typeof(Image), typeof(Button));
        changeSkinBtnObj.transform.SetParent(rightRect, false);
        RectTransform csRect = changeSkinBtnObj.GetComponent<RectTransform>();
        csRect.anchorMin = new Vector2(1f, 0f);
        csRect.anchorMax = new Vector2(1f, 0f);
        csRect.pivot = new Vector2(1f, 0f);
        csRect.anchoredPosition = new Vector2(0f, 15f);
        csRect.sizeDelta = new Vector2(210f, 85f);

        Image csImg = changeSkinBtnObj.GetComponent<Image>();
        csImg.sprite = changeSkinSprite;
        csImg.raycastTarget = true;
        Button csBtn = changeSkinBtnObj.GetComponent<Button>();
        csBtn.targetGraphic = csImg;

        changeSkinBtnObj.SetActive(isEquipped);
        view.changeSkinButton = csBtn;

        // Nhóm nút Build (cho Unit 4)
        GameObject buildGroupObj = new GameObject("BuildGroup", typeof(RectTransform));
        buildGroupObj.transform.SetParent(rightRect, false);
        RectTransform bgRect = buildGroupObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(1f, 0.5f);
        bgRect.anchorMax = new Vector2(1f, 0.5f);
        bgRect.pivot = new Vector2(1f, 0.5f);
        bgRect.anchoredPosition = new Vector2(0f, 0f);
        bgRect.sizeDelta = new Vector2(210f, 160f);

        // Nút Build
        GameObject buildBtnObj = new GameObject("BuildButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buildBtnObj.transform.SetParent(bgRect, false);
        RectTransform bbRect = buildBtnObj.GetComponent<RectTransform>();
        bbRect.anchorMin = new Vector2(0.5f, 1f);
        bbRect.anchorMax = new Vector2(0.5f, 1f);
        bbRect.pivot = new Vector2(0.5f, 1f);
        bbRect.anchoredPosition = new Vector2(0f, 0f);
        bbRect.sizeDelta = new Vector2(200f, 85f);

        Image bbImg = buildBtnObj.GetComponent<Image>();
        bbImg.sprite = buildBtnSprite;
        bbImg.raycastTarget = true;
        Button bbBtn = buildBtnObj.GetComponent<Button>();
        bbBtn.targetGraphic = bbImg;

        GameObject bbTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        bbTextObj.transform.SetParent(bbRect, false);
        RectTransform bbtRect = bbTextObj.GetComponent<RectTransform>();
        bbtRect.anchorMin = Vector2.zero;
        bbtRect.anchorMax = Vector2.one;
        bbtRect.offsetMin = Vector2.zero;
        bbtRect.offsetMax = Vector2.zero;

        TextMeshProUGUI bbtTMP = bbTextObj.GetComponent<TextMeshProUGUI>();
        if (font != null) bbtTMP.font = font;
        bbtTMP.fontSize = 36f;
        bbtTMP.fontStyle = FontStyles.Bold;
        bbtTMP.color = Color.white;
        bbtTMP.alignment = TextAlignmentOptions.Center;
        bbtTMP.text = "Build";

        // Giá Build (Icon + Text)
        GameObject costObj = new GameObject("CostRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        costObj.transform.SetParent(bgRect, false);
        RectTransform costRect = costObj.GetComponent<RectTransform>();
        costRect.anchorMin = new Vector2(0.5f, 0f);
        costRect.anchorMax = new Vector2(0.5f, 0f);
        costRect.pivot = new Vector2(0.5f, 0f);
        costRect.anchoredPosition = new Vector2(0f, 10f);
        costRect.sizeDelta = new Vector2(150f, 40f);

        HorizontalLayoutGroup hlg = costObj.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 8f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        GameObject costIconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        costIconObj.transform.SetParent(costRect, false);
        RectTransform ciRect = costIconObj.GetComponent<RectTransform>();
        ciRect.sizeDelta = new Vector2(36f, 36f);
        Image ciImg = costIconObj.GetComponent<Image>();
        ciImg.sprite = redGemIcon;
        ciImg.preserveAspect = true;
        ciImg.raycastTarget = false;

        GameObject costTextObj = new GameObject("Price", typeof(RectTransform), typeof(TextMeshProUGUI));
        costTextObj.transform.SetParent(costRect, false);
        RectTransform ctRect = costTextObj.GetComponent<RectTransform>();
        ctRect.sizeDelta = new Vector2(100f, 36f);
        TextMeshProUGUI ctTMP = costTextObj.GetComponent<TextMeshProUGUI>();
        if (font != null) ctTMP.font = font;
        ctTMP.fontSize = 28f;
        ctTMP.fontStyle = FontStyles.Bold;
        ctTMP.color = new Color32(255, 233, 92, 255);
        ctTMP.alignment = TextAlignmentOptions.MidlineLeft;
        ctTMP.text = cost.ToString("N0");

        buildGroupObj.SetActive(hasBuild);
        view.buildGroup = buildGroupObj;
        view.buildButton = bbBtn;
        view.buildCostText = ctTMP;
        view.buildCostIcon = ciImg;

        return view;
    }
}
#endif
