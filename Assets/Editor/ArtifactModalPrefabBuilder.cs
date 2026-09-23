#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tool tạo Prefab và đưa giao diện ArtifactFoundModal vào Scene / Prefab
/// với visual hierarchy, typography và layout chuẩn theo mockup reference.
/// </summary>
public static class ArtifactModalPrefabBuilder
{
    private const string PrefabDir = "Assets/Resources/UI";
    private const string PrefabPath = "Assets/Resources/UI/ArtifactFoundModal.prefab";
    private const string SpriteSheetPath = "Assets/Sprites/UI/artifact 1/nút artifact.png";
    private const string GamePlayScenePath = "Assets/Scenes/GamePlay.unity";

    [MenuItem("PGE/UI/1. Create & Edit Artifact Found Modal Prefab", false, 50)]
    [MenuItem("Tools/PGE/1. Create & Edit Artifact Found Modal Prefab", false, 50)]
    public static void CreateAndOpenPrefab()
    {
        if (!Directory.Exists(PrefabDir))
        {
            Directory.CreateDirectory(PrefabDir);
        }

        GameObject root = BuildModalGameObject();

        // Lưu thành Prefab trong Assets/Resources/UI/
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Mở thẳng Prefab để người dùng tự do chỉnh sửa trong Inspector / Scene view
        AssetDatabase.OpenAsset(prefab);
        Selection.activeObject = prefab;
    }

    [MenuItem("PGE/UI/2. Bake Modal directly into GamePlay Scene", false, 51)]
    [MenuItem("Tools/PGE/2. Bake Modal directly into GamePlay Scene", false, 51)]
    public static void BakeModalIntoCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ArtifactModalPrefabBuilder] Không tìm thấy Canvas trong Scene hiện tại!");
            return;
        }

        // Xóa modal cũ nếu đã có trong scene
        ArtifactFoundModalController oldCtrl = UnityEngine.Object.FindObjectOfType<ArtifactFoundModalController>(true);
        if (oldCtrl != null)
        {
            Undo.DestroyObjectImmediate(oldCtrl.gameObject);
        }

        GameObject modalObj = BuildModalGameObject();
        modalObj.name = "ArtifactFoundModal";
        modalObj.transform.SetParent(canvas.transform, false);
        Undo.RegisterCreatedObjectUndo(modalObj, "Bake Artifact Found Modal");

        modalObj.SetActive(false);
        Selection.activeGameObject = modalObj;
        EditorSceneManager.MarkSceneDirty(currentScene);
    }

    private static GameObject BuildModalGameObject()
    {
        // 1. Tải các sprite nút bấm và card
        Dictionary<string, Sprite> sheetSprites = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath)
            .OfType<Sprite>()
            .GroupBy(s => s.name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        Sprite sprThrow = sheetSprites.TryGetValue("Btn_ThrowAway", out var st) ? st : null;
        Sprite sprGet = sheetSprites.TryGetValue("Btn_Get", out var sg) ? sg : null;
        Sprite defaultCardSpr = sheetSprites.TryGetValue("Artifact_Lego", out var sl) ? sl : null;

        TMP_FontAsset defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito/Nunito SDF.asset")
                                 ?? Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")
                                 ?? Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault();

        Material titleMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/Nunito/Nunito SDF - ArtifactTitle.mat")
                              ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/Nunito/Nunito SDF - Stroke.mat");

        Material bodyMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/Nunito/Nunito SDF - ArtifactBody.mat")
                             ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/Nunito/Nunito SDF - Stroke.mat");

        // 2. Root Modal
        GameObject root = new GameObject("ArtifactFoundModal", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        // 3. Dim Background
        GameObject dimObj = new GameObject("DimBackground", typeof(RectTransform), typeof(Image));
        dimObj.transform.SetParent(root.transform, false);
        RectTransform dimRt = dimObj.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        Image dimImg = dimObj.GetComponent<Image>();
        dimImg.color = new Color32(0, 0, 0, 190); // Mờ tối 75%
        dimImg.raycastTarget = true;

        // 4. Sunburst Rays Effect (Center behind the card at y = 180)
        GameObject sunburstObj = new GameObject("SunburstEffect", typeof(RectTransform), typeof(SunburstRayEffect));
        sunburstObj.transform.SetParent(root.transform, false);
        RectTransform sunburstRt = sunburstObj.GetComponent<RectTransform>();
        sunburstRt.anchorMin = new Vector2(0.5f, 0.5f);
        sunburstRt.anchorMax = new Vector2(0.5f, 0.5f);
        sunburstRt.pivot = new Vector2(0.5f, 0.5f);
        sunburstRt.anchoredPosition = new Vector2(0f, 180f);
        sunburstRt.sizeDelta = new Vector2(900f, 900f);
        SunburstRayEffect sunburst = sunburstObj.GetComponent<SunburstRayEffect>();
        sunburst.EnsureRayGraphic();

        // 5. Title Text: "Artifact found"
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(root.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.anchoredPosition = new Vector2(0f, 490f);
        titleRt.sizeDelta = new Vector2(850f, 100f);
        TextMeshProUGUI titleTxt = titleObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) titleTxt.font = defaultFont;
        if (titleMaterial != null) titleTxt.fontSharedMaterial = titleMaterial;
        titleTxt.text = "Artifact found";
        titleTxt.fontSize = 64f;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.color = Color.white;
        titleTxt.raycastTarget = false;

        // 6. Thẻ Cổ vật (Card Icon)
        GameObject cardObj = new GameObject("CardImage", typeof(RectTransform), typeof(Image));
        cardObj.transform.SetParent(root.transform, false);
        RectTransform cardRt = cardObj.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.anchoredPosition = new Vector2(0f, 180f);
        cardRt.sizeDelta = new Vector2(260f, 312f); // Tỷ lệ chuẩn của sprite sheet 700x842
        Image cardImg = cardObj.GetComponent<Image>();
        cardImg.sprite = defaultCardSpr;
        cardImg.preserveAspect = true;
        cardImg.color = Color.white;
        cardImg.raycastTarget = false;

        // 7. Info Container (VerticalLayoutGroup cho hierarchy co giãn mượt mà)
        GameObject infoObj = new GameObject("InfoContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        infoObj.transform.SetParent(root.transform, false);
        RectTransform infoRt = infoObj.GetComponent<RectTransform>();
        infoRt.anchorMin = new Vector2(0.5f, 0.5f);
        infoRt.anchorMax = new Vector2(0.5f, 0.5f);
        infoRt.pivot = new Vector2(0.5f, 1f);
        infoRt.anchoredPosition = new Vector2(0f, -14f);
        infoRt.sizeDelta = new Vector2(850f, 260f);

        VerticalLayoutGroup layout = infoObj.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 16f;

        ContentSizeFitter fitter = infoObj.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 7a. Artifact Name (Vàng cam hoàng kim ấm, Bold, Outline dày)
        GameObject nameObj = new GameObject("ArtifactName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(infoObj.transform, false);
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.sizeDelta = new Vector2(850f, 75f);
        TextMeshProUGUI nameTxt = nameObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) nameTxt.font = defaultFont;
        if (titleMaterial != null) nameTxt.fontSharedMaterial = titleMaterial;
        nameTxt.text = "Signal Rocket";
        nameTxt.fontSize = 58f;
        nameTxt.fontStyle = FontStyles.Bold;
        nameTxt.alignment = TextAlignmentOptions.Center;
        nameTxt.color = new Color32(255, 184, 0, 255); // Warm golden yellow #FFB800
        nameTxt.enableWordWrapping = true;
        nameTxt.raycastTarget = false;

        // 7b. Lore Description (Trắng sáng, Bold, Outline vừa, xuống dòng tự động)
        GameObject loreObj = new GameObject("LoreDescription", typeof(RectTransform), typeof(TextMeshProUGUI));
        loreObj.transform.SetParent(infoObj.transform, false);
        RectTransform loreRt = loreObj.GetComponent<RectTransform>();
        loreRt.sizeDelta = new Vector2(750f, 80f);
        TextMeshProUGUI loreTxt = loreObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) loreTxt.font = defaultFont;
        if (bodyMaterial != null) loreTxt.fontSharedMaterial = bodyMaterial;
        loreTxt.text = "Shoots signal flare to call allied aerial bombardment.";
        loreTxt.fontSize = 32f;
        loreTxt.fontStyle = FontStyles.Bold;
        loreTxt.alignment = TextAlignmentOptions.Center;
        loreTxt.color = Color.white;
        loreTxt.enableWordWrapping = true;
        loreTxt.lineSpacing = -5f;
        loreTxt.raycastTarget = false;

        // Spacer giữa Lore và Stat
        GameObject spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(infoObj.transform, false);
        spacer.GetComponent<LayoutElement>().preferredHeight = 14f;

        // 7c. Stat Buff Text (Xanh neon / mint đặc trưng của hệ thống)
        GameObject statObj = new GameObject("StatBuffText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statObj.transform.SetParent(infoObj.transform, false);
        RectTransform statRt = statObj.GetComponent<RectTransform>();
        statRt.sizeDelta = new Vector2(850f, 50f);
        TextMeshProUGUI statTxt = statObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) statTxt.font = defaultFont;
        if (bodyMaterial != null) statTxt.fontSharedMaterial = bodyMaterial;
        statTxt.text = "Move Speed +12%";
        statTxt.fontSize = 40f;
        statTxt.fontStyle = FontStyles.Bold;
        statTxt.alignment = TextAlignmentOptions.Center;
        statTxt.color = new Color32(0, 255, 136, 255); // Xanh neon mint
        statTxt.enableWordWrapping = true;
        statTxt.raycastTarget = false;

        // 8. Container chứa 2 nút bấm
        GameObject btnContainer = new GameObject("ButtonsContainer", typeof(RectTransform));
        btnContainer.transform.SetParent(root.transform, false);
        RectTransform btnContainerRt = btnContainer.GetComponent<RectTransform>();
        btnContainerRt.anchorMin = new Vector2(0.5f, 0.2f);
        btnContainerRt.anchorMax = new Vector2(0.5f, 0.2f);
        btnContainerRt.pivot = new Vector2(0.5f, 0.5f);
        btnContainerRt.anchoredPosition = Vector2.zero;
        btnContainerRt.sizeDelta = new Vector2(650f, 130f);

        // Nút Throw Away (dùng sprite Btn_ThrowAway + Text Vector)
        GameObject throwBtnObj = new GameObject("ThrowAwayButton", typeof(RectTransform), typeof(Image), typeof(Button));
        throwBtnObj.transform.SetParent(btnContainer.transform, false);
        RectTransform throwRt = throwBtnObj.GetComponent<RectTransform>();
        throwRt.anchorMin = new Vector2(0.5f, 0.5f);
        throwRt.anchorMax = new Vector2(0.5f, 0.5f);
        throwRt.pivot = new Vector2(0.5f, 0.5f);
        throwRt.anchoredPosition = new Vector2(-160f, 0f);
        throwRt.sizeDelta = new Vector2(250f, 114f); // Tỷ lệ chuẩn nút ~654x298
        Image throwImg = throwBtnObj.GetComponent<Image>();
        throwImg.sprite = sprThrow;
        throwImg.preserveAspect = true;
        throwImg.color = Color.white;
        Button throwBtn = throwBtnObj.GetComponent<Button>();
        throwBtn.targetGraphic = throwImg;

        GameObject throwTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        throwTxtObj.transform.SetParent(throwBtnObj.transform, false);
        RectTransform throwTxtRt = throwTxtObj.GetComponent<RectTransform>();
        throwTxtRt.anchorMin = Vector2.zero;
        throwTxtRt.anchorMax = Vector2.one;
        throwTxtRt.pivot = new Vector2(0.5f, 0.5f);
        throwTxtRt.offsetMin = new Vector2(10f, 14f); // Bù khối vát 3D ở đáy nút
        throwTxtRt.offsetMax = new Vector2(-10f, 0f);
        TextMeshProUGUI throwBtnTxt = throwTxtObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) throwBtnTxt.font = defaultFont;
        if (bodyMaterial != null) throwBtnTxt.fontSharedMaterial = bodyMaterial;
        throwBtnTxt.text = "Throw away";
        throwBtnTxt.fontSize = 34f;
        throwBtnTxt.fontStyle = FontStyles.Bold;
        throwBtnTxt.alignment = TextAlignmentOptions.Center;
        throwBtnTxt.color = Color.white;
        throwBtnTxt.raycastTarget = false;

        // Nút Get (dùng sprite Btn_Get + Text Vector)
        GameObject getBtnObj = new GameObject("GetButton", typeof(RectTransform), typeof(Image), typeof(Button));
        getBtnObj.transform.SetParent(btnContainer.transform, false);
        RectTransform getRt = getBtnObj.GetComponent<RectTransform>();
        getRt.anchorMin = new Vector2(0.5f, 0.5f);
        getRt.anchorMax = new Vector2(0.5f, 0.5f);
        getRt.pivot = new Vector2(0.5f, 0.5f);
        getRt.anchoredPosition = new Vector2(160f, 0f);
        getRt.sizeDelta = new Vector2(250f, 114f);
        Image getImg = getBtnObj.GetComponent<Image>();
        getImg.sprite = sprGet;
        getImg.preserveAspect = true;
        getImg.color = Color.white;
        Button getBtn = getBtnObj.GetComponent<Button>();
        getBtn.targetGraphic = getImg;

        GameObject getTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        getTxtObj.transform.SetParent(getBtnObj.transform, false);
        RectTransform getTxtRt = getTxtObj.GetComponent<RectTransform>();
        getTxtRt.anchorMin = Vector2.zero;
        getTxtRt.anchorMax = Vector2.one;
        getTxtRt.pivot = new Vector2(0.5f, 0.5f);
        getTxtRt.offsetMin = new Vector2(10f, 14f); // Bù khối vát 3D ở đáy nút
        getTxtRt.offsetMax = new Vector2(-10f, 0f);
        TextMeshProUGUI getBtnTxt = getTxtObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) getBtnTxt.font = defaultFont;
        if (bodyMaterial != null) getBtnTxt.fontSharedMaterial = bodyMaterial;
        getBtnTxt.text = "Get";
        getBtnTxt.fontSize = 38f;
        getBtnTxt.fontStyle = FontStyles.Bold;
        getBtnTxt.alignment = TextAlignmentOptions.Center;
        getBtnTxt.color = Color.white;
        getBtnTxt.raycastTarget = false;

        // 9. Gắn Controller và liên kết các trường
        ArtifactFoundModalController ctrl = root.AddComponent<ArtifactFoundModalController>();
        ctrl.ConfigureReferences(
            root,
            dimImg,
            sunburst,
            titleTxt,
            null, // Không dùng khung viền đơn sắc lồng ngoài
            cardImg,
            null,
            nameTxt,
            loreTxt,
            statTxt,
            throwBtn,
            getBtn,
            throwBtnTxt,
            getBtnTxt
        );

        throwBtn.onClick.AddListener(ctrl.OnThrowAwayClicked);
        getBtn.onClick.AddListener(ctrl.OnGetClicked);

        return root;
    }
}
#endif
