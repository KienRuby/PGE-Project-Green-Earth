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
/// để người dùng có thể tự do mở ra chỉnh sửa bằng tay 100% theo ý muốn.
/// </summary>
public static class ArtifactModalPrefabBuilder
{
    private const string PrefabDir = "Assets/Resources/UI";
    private const string PrefabPath = "Assets/Resources/UI/ArtifactFoundModal.prefab";
    private const string SpriteSheetPath = "Assets/Sprites/UI/nút artifact.png";
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

        Debug.Log($"[ArtifactModalPrefabBuilder] ✅ Đã tạo Prefab thành công tại: {PrefabPath}. Bạn có thể tự do chỉnh sửa font chữ, kích thước, vị trí trong cửa sổ Inspector!");
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

        // Chọn modal và highlight để người dùng thấy ngay trên Hierarchy
        Selection.activeGameObject = modalObj;
        EditorSceneManager.MarkSceneDirty(currentScene);

        Debug.Log("[ArtifactModalPrefabBuilder] ✅ Đã đặt GameObject 'ArtifactFoundModal' vào Canvas của Scene! Bạn có thể bật Active lên và dùng chuột kéo thả căn chỉnh tự do!");
    }

    private static GameObject BuildModalGameObject()
    {
        // 1. Tải các sprite nút bấm
        Dictionary<string, Sprite> sheetSprites = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath)
            .OfType<Sprite>()
            .GroupBy(s => s.name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        Sprite sprThrow = sheetSprites.TryGetValue("Btn_ThrowAway", out var st) ? st : null;
        Sprite sprGet = sheetSprites.TryGetValue("Btn_Get", out var sg) ? sg : null;
        Sprite defaultCardSpr = sheetSprites.TryGetValue("Artifact_Lego", out var sl) ? sl : null;

        TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")
                                 ?? Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault();

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

        // 4. Sunburst Rays Effect
        GameObject sunburstObj = new GameObject("SunburstEffect", typeof(RectTransform), typeof(SunburstRayEffect));
        sunburstObj.transform.SetParent(root.transform, false);
        RectTransform sunburstRt = sunburstObj.GetComponent<RectTransform>();
        sunburstRt.anchorMin = new Vector2(0.5f, 0.5f);
        sunburstRt.anchorMax = new Vector2(0.5f, 0.5f);
        sunburstRt.pivot = new Vector2(0.5f, 0.5f);
        sunburstRt.anchoredPosition = new Vector2(0f, 150f);
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
        titleRt.anchoredPosition = new Vector2(0f, 480f);
        titleRt.sizeDelta = new Vector2(800f, 90f);
        TextMeshProUGUI titleTxt = titleObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) titleTxt.font = defaultFont;
        titleTxt.text = "Artifact found";
        titleTxt.fontSize = 54f;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.color = Color.white;

        // 6. Thẻ Cổ vật (Card Icon) - Tỉ lệ chuẩn hiển thị trọn vẹn cả khung và icon
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

        // 7. Tên Artifact
        GameObject nameObj = new GameObject("ArtifactName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(root.transform, false);
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0.5f, 0.5f);
        nameRt.anchorMax = new Vector2(0.5f, 0.5f);
        nameRt.pivot = new Vector2(0.5f, 0.5f);
        nameRt.anchoredPosition = new Vector2(0f, -25f);
        nameRt.sizeDelta = new Vector2(800f, 65f);
        TextMeshProUGUI nameTxt = nameObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) nameTxt.font = defaultFont;
        nameTxt.text = "Modular Brick";
        nameTxt.fontSize = 44f;
        nameTxt.fontStyle = FontStyles.Bold;
        nameTxt.alignment = TextAlignmentOptions.Center;
        nameTxt.color = new Color32(255, 184, 28, 255); // Màu vàng cam nổi bật

        // 8. Mô tả Lore / Flavor Text
        GameObject loreObj = new GameObject("LoreDescription", typeof(RectTransform), typeof(TextMeshProUGUI));
        loreObj.transform.SetParent(root.transform, false);
        RectTransform loreRt = loreObj.GetComponent<RectTransform>();
        loreRt.anchorMin = new Vector2(0.5f, 0.5f);
        loreRt.anchorMax = new Vector2(0.5f, 0.5f);
        loreRt.pivot = new Vector2(0.5f, 0.5f);
        loreRt.anchoredPosition = new Vector2(0f, -85f);
        loreRt.sizeDelta = new Vector2(750f, 60f);
        TextMeshProUGUI loreTxt = loreObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) loreTxt.font = defaultFont;
        loreTxt.text = "Interlocking plastic toy brick. Incredibly durable construction.";
        loreTxt.fontSize = 24f;
        loreTxt.alignment = TextAlignmentOptions.Center;
        loreTxt.color = new Color32(220, 220, 220, 255);

        // 9. Chỉ số Buff
        GameObject statObj = new GameObject("StatBuffText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statObj.transform.SetParent(root.transform, false);
        RectTransform statRt = statObj.GetComponent<RectTransform>();
        statRt.anchorMin = new Vector2(0.5f, 0.5f);
        statRt.anchorMax = new Vector2(0.5f, 0.5f);
        statRt.pivot = new Vector2(0.5f, 0.5f);
        statRt.anchoredPosition = new Vector2(0f, -155f);
        statRt.sizeDelta = new Vector2(750f, 60f);
        TextMeshProUGUI statTxt = statObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) statTxt.font = defaultFont;
        statTxt.text = "DEF +12";
        statTxt.fontSize = 36f;
        statTxt.fontStyle = FontStyles.Bold;
        statTxt.alignment = TextAlignmentOptions.Center;
        statTxt.color = new Color32(90, 255, 160, 255); // Xanh neon buff

        // 10. Container chứa 2 nút bấm
        GameObject btnContainer = new GameObject("ButtonsContainer", typeof(RectTransform));
        btnContainer.transform.SetParent(root.transform, false);
        RectTransform btnContainerRt = btnContainer.GetComponent<RectTransform>();
        btnContainerRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnContainerRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnContainerRt.pivot = new Vector2(0.5f, 0.5f);
        btnContainerRt.anchoredPosition = new Vector2(0f, -270f);
        btnContainerRt.sizeDelta = new Vector2(650f, 130f);

        // Nút Throw Away (dùng sprite Btn_ThrowAway)
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

        // Nút Get (dùng sprite Btn_Get)
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

        // 11. Gắn Controller và liên kết các trường
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
            getBtn
        );

        throwBtn.onClick.AddListener(ctrl.OnThrowAwayClicked);
        getBtn.onClick.AddListener(ctrl.OnGetClicked);

        return root;
    }
}
#endif
