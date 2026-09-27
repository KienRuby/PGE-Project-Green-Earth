#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Project Green Earth - Loading Screen Builder
public static class LoadingScreenBuilder
{
    private const string BackgroundPath = "Assets/Sprites/Backround/nền (4).png";

    // Creep 2 (Big creep 2)
    private const string ThaanSpritePath = "Assets/Sprites/Enemy/Creep 2/big creep2- thaan.png";
    private const string ChanTrcSpritePath = "Assets/Sprites/Enemy/Creep 2/big creep2- chân trc.png";
    private const string ChanSauSpritePath = "Assets/Sprites/Enemy/Creep 2/big creep2- chân sau.png";
    private const string CreepControllerPath = "Assets/Animaton/Enemy/Creep 2/Big creep 2.controller";

    // Creep 3
    private const string Creep3ThanPath = "Assets/Sprites/Enemy/Creep 3/creep 3-thân.png";
    private const string Creep3KhopTrenPath = "Assets/Sprites/Enemy/Creep 3/creep 3-khớp trên.png";
    private const string Creep3KhopDuoiPath = "Assets/Sprites/Enemy/Creep 3/creep 3-khớp dưới.png";
    private const string Creep3ControllerPath = "Assets/Animaton/Enemy/Creep3/Creep3.controller";

    // Creep 1
    private const string Creep1ThanPath = "Assets/Sprites/Enemy/Creep 1/creep 1- thân.png";
    private const string Creep1ChanSauPath = "Assets/Sprites/Enemy/Creep 1/creep 1- chân sau.png";
    private const string Creep1ChanTrcPath = "Assets/Sprites/Enemy/Creep 1/creep 1- chân trước.png";
    private const string Creep1ControllerPath = "Assets/Animaton/Enemy/Creep 1/Creep.controller";

    private const string PrefabDir = "Assets/Resources/UI";
    private const string PrefabPath = "Assets/Resources/UI/LoadingScreen.prefab";
    private const string ScenePath = "Assets/Scenes/Loading.unity";

    [MenuItem("PGE/Build Loading Screen Prefab and Scene")]
    public static void BuildLoadingScreen()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[LoadingScreenBuilder] Vui lòng dừng Play Mode trước khi tạo Loading Screen.");
            return;
        }

        if (!Directory.Exists(PrefabDir))
        {
            Directory.CreateDirectory(PrefabDir);
            AssetDatabase.Refresh();
        }

        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        if (bgSprite == null)
        {
            Debug.LogError($"[LoadingScreenBuilder] Không tìm thấy sprite nền tại: {BackgroundPath}");
            return;
        }

        // 1. Tải tài nguyên Creep 2
        Sprite thaanSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ThaanSpritePath);
        Sprite[] chanTrcSprites = AssetDatabase.LoadAllAssetsAtPath(ChanTrcSpritePath).OfType<Sprite>().ToArray();
        Sprite[] chanSauSprites = AssetDatabase.LoadAllAssetsAtPath(ChanSauSpritePath).OfType<Sprite>().ToArray();
        RuntimeAnimatorController creepController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CreepControllerPath);

        Sprite chanTrc0 = chanTrcSprites.FirstOrDefault(s => s.name.Contains("chân trc_0"));
        Sprite chanTrc1 = chanTrcSprites.FirstOrDefault(s => s.name.Contains("chân trc_1"));
        Sprite chanSau0 = chanSauSprites.FirstOrDefault(s => s.name.Contains("chân sau_0"));
        Sprite chanSau1 = chanSauSprites.FirstOrDefault(s => s.name.Contains("chân sau_1"));

        if (thaanSprite == null || chanTrc0 == null || chanTrc1 == null || chanSau0 == null || chanSau1 == null || creepController == null)
        {
            Debug.LogError($"[LoadingScreenBuilder] Không thể nạp đầy đủ sprite hoặc controller cho Creep 2.");
            return;
        }

        // 2. Tải tài nguyên Creep 3
        Sprite creep3ThanSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Creep3ThanPath);
        Sprite[] khopTrenSprites = AssetDatabase.LoadAllAssetsAtPath(Creep3KhopTrenPath).OfType<Sprite>().ToArray();
        Sprite[] khopDuoiSprites = AssetDatabase.LoadAllAssetsAtPath(Creep3KhopDuoiPath).OfType<Sprite>().ToArray();
        RuntimeAnimatorController creep3Controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Creep3ControllerPath);

        Sprite khopTren0 = khopTrenSprites.FirstOrDefault(s => s.name.Contains("khớp trên_0"));
        Sprite khopTren1 = khopTrenSprites.FirstOrDefault(s => s.name.Contains("khớp trên_1"));
        Sprite khopDuoi0 = khopDuoiSprites.FirstOrDefault(s => s.name.Contains("khớp dưới_0"));
        Sprite khopDuoi1 = khopDuoiSprites.FirstOrDefault(s => s.name.Contains("khớp dưới_1"));

        if (creep3ThanSprite == null || khopTren0 == null || khopTren1 == null || khopDuoi0 == null || khopDuoi1 == null || creep3Controller == null)
        {
            Debug.LogError($"[LoadingScreenBuilder] Không thể nạp đầy đủ sprite hoặc controller cho Creep 3.");
            return;
        }

        // 3. Tải tài nguyên Creep 1
        Sprite[] creep1ThanSprites = AssetDatabase.LoadAllAssetsAtPath(Creep1ThanPath).OfType<Sprite>().ToArray();
        Sprite thanSprite1 = creep1ThanSprites.FirstOrDefault(s => s.name == "Than") ?? AssetDatabase.LoadAssetAtPath<Sprite>(Creep1ThanPath);
        Sprite[] creep1ChanSauSprites = AssetDatabase.LoadAllAssetsAtPath(Creep1ChanSauPath).OfType<Sprite>().ToArray();
        Sprite chan1 = creep1ChanSauSprites.FirstOrDefault(s => s.name == "chan1");
        Sprite chan2 = creep1ChanSauSprites.FirstOrDefault(s => s.name == "chan2");
        Sprite[] creep1ChanTrcSprites = AssetDatabase.LoadAllAssetsAtPath(Creep1ChanTrcPath).OfType<Sprite>().ToArray();
        Sprite chanTruoc = creep1ChanTrcSprites.FirstOrDefault(s => s.name == "chan truoc");
        RuntimeAnimatorController creep1Controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Creep1ControllerPath);

        if (thanSprite1 == null || chan1 == null || chan2 == null || chanTruoc == null || creep1Controller == null)
        {
            Debug.LogError($"[LoadingScreenBuilder] Không thể nạp đầy đủ sprite hoặc controller cho Creep 1.");
            return;
        }

        // 1. Tạo Canvas Root
        GameObject root = new GameObject("LoadingScreen");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Trên cùng màn hình

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f; // Khớp theo chiều ngang chuẩn mobile portrait

        root.AddComponent<GraphicRaycaster>();
        CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        LoadingScreenUI loadingUI = root.AddComponent<LoadingScreenUI>();

        // 2. Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(root.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.sprite = bgSprite;
        bgImage.color = Color.white;
        bgImage.raycastTarget = false;

        // 3. ProgressBar Container
        GameObject barObj = new GameObject("ProgressBar");
        barObj.transform.SetParent(root.transform, false);
        RectTransform barRect = barObj.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 0f);
        barRect.anchorMax = new Vector2(0.5f, 0f);
        barRect.pivot = new Vector2(0.5f, 0.5f);
        barRect.anchoredPosition = new Vector2(0f, 270f);
        barRect.sizeDelta = new Vector2(750f, 60f);

        // 3.1. Border (Viền xanh ngọc nhạt #73B6BE)
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(barObj.transform, false);
        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;
        borderRect.anchoredPosition = Vector2.zero;

        Image borderImage = borderObj.AddComponent<Image>();
        borderImage.color = new Color(0.451f, 0.714f, 0.745f, 1f); // #73B6BE
        borderImage.raycastTarget = false;

        // 3.2. Background ruột thanh (#1B0B20)
        GameObject barBgObj = new GameObject("BarBackground");
        barBgObj.transform.SetParent(barObj.transform, false);
        RectTransform barBgRect = barBgObj.AddComponent<RectTransform>();
        barBgRect.anchorMin = Vector2.zero;
        barBgRect.anchorMax = Vector2.one;
        barBgRect.sizeDelta = new Vector2(-6f, -6f); // 3px viền mỗi bên
        barBgRect.anchoredPosition = Vector2.zero;

        Image barBgImage = barBgObj.AddComponent<Image>();
        barBgImage.color = new Color(0.106f, 0.043f, 0.125f, 1f); // #1B0B20
        barBgImage.raycastTarget = false;

        // 3.3. Fill Bar (Màu hồng kẹo ngọt #FBAADD)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barObj.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = new Vector2(-6f, -6f);
        fillRect.anchoredPosition = Vector2.zero;

        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.984f, 0.667f, 0.867f, 1f); // #FBAADD
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 0.5f; // Preview
        fillImage.raycastTarget = false;

        // 3.4. Mascot Container (Di chuyển dọc theo đầu thanh nạp)
        GameObject mascotObj = new GameObject("Mascot");
        mascotObj.transform.SetParent(barObj.transform, false);
        RectTransform mascotRect = mascotObj.AddComponent<RectTransform>();
        mascotRect.anchorMin = new Vector2(0.5f, 1f);
        mascotRect.anchorMax = new Vector2(0.5f, 1f);
        mascotRect.pivot = new Vector2(0.5f, 0f);
        mascotRect.anchoredPosition = new Vector2(0f, 0f);
        mascotRect.sizeDelta = new Vector2(180f, 120f);

        // 3.4.1. Creep2_Visual (Chứa Animator chạy animation Walk của Creep 2)
        GameObject creepObj = new GameObject("Creep2_Visual");
        creepObj.transform.SetParent(mascotObj.transform, false);
        RectTransform creepRect = creepObj.AddComponent<RectTransform>();
        creepRect.anchorMin = new Vector2(0.5f, 0f);
        creepRect.anchorMax = new Vector2(0.5f, 0f);
        creepRect.pivot = new Vector2(0.5f, 0f);
        creepRect.anchoredPosition = new Vector2(0f, 40f); // Bàn chân chạm mép trên thanh
        creepRect.localScale = new Vector3(0.35f, 0.35f, 1f);

        Animator creepAnimator = creepObj.AddComponent<Animator>();
        creepAnimator.runtimeAnimatorController = creepController;
        creepAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        creepAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // Tạo 5 bộ phận chính xác theo đường dẫn của Walk.anim trong Creep 2:
        // Thứ tự uGUI vẽ từ sau ra trước: chân sau -> thân -> chân trước
        CreateLimbImage(creepObj, "big creep2- chân sau_1", chanSau1, new Vector2(123f, 104f), new Vector2(0.7914007f, 0.7741438f), new Vector2(-90.00f, -14.40f));
        CreateLimbImage(creepObj, "big creep2- chân sau_0", chanSau0, new Vector2(125f, 103f), new Vector2(0.21326147f, 0.753079f), new Vector2(92.60f, -18.25f));
        CreateLimbImage(creepObj, "big creep2- thaan", thaanSprite, new Vector2(500f, 500f), new Vector2(0.5f, 0.5f), Vector2.zero);
        CreateLimbImage(creepObj, "big creep2- chân trc_1", chanTrc1, new Vector2(130f, 125f), new Vector2(0.860676f, 0.61376035f), new Vector2(-120.10f, -32.50f));
        CreateLimbImage(creepObj, "big creep2- chân trc_0", chanTrc0, new Vector2(134f, 125f), new Vector2(0.13574994f, 0.6875514f), new Vector2(115.83f, -26.75f));

        // 3.4.2. Creep3_Visual (Quái vật gai Creep 3 với animation Walk)
        GameObject creep3Obj = new GameObject("Creep3_Visual");
        creep3Obj.transform.SetParent(mascotObj.transform, false);
        RectTransform creep3Rect = creep3Obj.AddComponent<RectTransform>();
        creep3Rect.anchorMin = new Vector2(0.5f, 0f);
        creep3Rect.anchorMax = new Vector2(0.5f, 0f);
        creep3Rect.pivot = new Vector2(0.5f, 0f);
        creep3Rect.anchoredPosition = new Vector2(0f, 40f);
        creep3Rect.localScale = new Vector3(0.35f, 0.35f, 1f);

        Animator creep3Animator = creep3Obj.AddComponent<Animator>();
        creep3Animator.runtimeAnimatorController = creep3Controller;
        creep3Animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        creep3Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        GameObject khopTren1Obj = CreateLimbImage(creep3Obj, "creep 3-khớp trên_1", khopTren1, new Vector2(64f, 73f), new Vector2(0.7318306f, 0.88919884f), new Vector2(22.3f, -36.4f), new Vector3(-1f, 1f, 1f));
        CreateLimbImage(khopTren1Obj, "creep 3-khớp dưới_1", khopDuoi1, new Vector2(78f, 104f), new Vector2(0.42941636f, 0.79777426f), new Vector2(-20.4f, -48.7f));
        CreateLimbImage(creep3Obj, "creep 3-thân (1)", creep3ThanSprite, new Vector2(500f, 500f), new Vector2(0.5f, 0.5f), Vector2.zero);
        GameObject khopTren0Obj = CreateLimbImage(creep3Obj, "creep 3-khớp trên_0", khopTren0, new Vector2(75f, 69f), new Vector2(0.89986575f, 0.82940894f), new Vector2(57.5f, -36.6f), new Vector3(-1f, 1f, 1f));
        CreateLimbImage(khopTren0Obj, "creep 3-khớp dưới_0", khopDuoi0, new Vector2(88f, 96f), new Vector2(0.691598f, 0.890691f), new Vector2(-35.3f, -40.8f));

        // 3.4.3. Creep1_Visual (Quái vật 4 chân Creep 1 với animation Run)
        GameObject creep1Obj = new GameObject("Creep1_Visual");
        creep1Obj.transform.SetParent(mascotObj.transform, false);
        RectTransform creep1Rect = creep1Obj.AddComponent<RectTransform>();
        creep1Rect.anchorMin = new Vector2(0.5f, 0f);
        creep1Rect.anchorMax = new Vector2(0.5f, 0f);
        creep1Rect.pivot = new Vector2(0.5f, 0f);
        creep1Rect.anchoredPosition = new Vector2(0f, 40f);
        // Sprite gốc vẽ hướng sang trái -> scale.x âm để quay mặt sang phải (hướng chạy tới)
        creep1Rect.localScale = new Vector3(-1.35f, 1.35f, 1f);

        Animator creep1Animator = creep1Obj.AddComponent<Animator>();
        creep1Animator.runtimeAnimatorController = creep1Controller;
        creep1Animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        creep1Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        CreateLimbImage(creep1Obj, "chan1", chan1, new Vector2(186f, 187f), new Vector2(0.8859076f, 0.74619496f), new Vector2(-19.4f, -47f), new Vector3(0.1453062f, 0.1453062f, 1f));
        CreateLimbImage(creep1Obj, "chan2", chan2, new Vector2(150f, 170f), new Vector2(0.15942138f, 0.7861809f), new Vector2(9.5f, -45f), new Vector3(0.1453062f, 0.1453062f, 1f));
        CreateLimbImage(creep1Obj, "Than", thanSprite1, new Vector2(899f, 746f), new Vector2(0.5677123f, 0.09200125f), new Vector2(0.3f, -42f), new Vector3(0.1453062f, 0.1453062f, 1f));
        CreateLimbImage(creep1Obj, "chan truoc", chanTruoc, new Vector2(128f, 218f), new Vector2(0.4086876f, 0.7466279f), new Vector2(-5.6f, -48.5f), new Vector3(0.1453062f, 0.1453062f, 1f));

        // Khởi tạo trạng thái ban đầu: Creep 2 bật, các con khác ẩn
        creepObj.SetActive(true);
        creep3Obj.SetActive(false);
        creep1Obj.SetActive(false);

        // 4. Gán Serialized Fields vào LoadingScreenUI
        SerializedObject so = new SerializedObject(loadingUI);
        so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        so.FindProperty("fillImage").objectReferenceValue = fillImage;
        so.FindProperty("progressBarRect").objectReferenceValue = barRect;
        so.FindProperty("mascotRect").objectReferenceValue = mascotRect;
        so.FindProperty("mascotAnimator").objectReferenceValue = creepAnimator;

        SerializedProperty creepVariantsProp = so.FindProperty("creepVariants");
        creepVariantsProp.arraySize = 3;
        creepVariantsProp.GetArrayElementAtIndex(0).objectReferenceValue = creepObj;
        creepVariantsProp.GetArrayElementAtIndex(1).objectReferenceValue = creep3Obj;
        creepVariantsProp.GetArrayElementAtIndex(2).objectReferenceValue = creep1Obj;

        so.FindProperty("progressText").objectReferenceValue = null;
        so.FindProperty("minDisplayDuration").floatValue = 1.0f;
        so.FindProperty("fadeDuration").floatValue = 0.25f;
        so.FindProperty("bobFrequency").floatValue = 8f;
        so.FindProperty("bobAmplitude").floatValue = 4f;
        so.FindProperty("squashAmount").floatValue = 0f;
        so.FindProperty("mascotOffset").floatValue = 35f;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 5. Đặt layer 5 (UI) cho toàn bộ cây phân cấp
        SetLayerRecursively(root, 5);

        // 6. Lưu thành Prefab
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Debug.Log($"[LoadingScreenBuilder] Đã lưu Prefab tại: {PrefabPath}");

        // 7. Tạo Scene Loading.unity độc lập
        CreateLoadingScene(savedPrefab);

        // Xóa đối tượng tạm trên scene hiện tại
        GameObject.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateLoadingScene(GameObject prefab)
    {
        if (File.Exists(ScenePath))
        {
            AddSceneToBuildSettings(ScenePath);
            return;
        }

        Scene targetScene = default;
        bool foundUntitled = false;

        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            Scene s = EditorSceneManager.GetSceneAt(i);
            if (string.IsNullOrEmpty(s.path))
            {
                targetScene = s;
                foundUntitled = true;
                break;
            }
        }

        if (!foundUntitled)
        {
            targetScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        }

        // Tạo EventSystem cho scene nếu chưa có
        bool hasEventSystem = false;
        foreach (GameObject rootObj in targetScene.GetRootGameObjects())
        {
            if (rootObj.GetComponent<UnityEngine.EventSystems.EventSystem>() != null)
            {
                hasEventSystem = true;
                break;
            }
        }

        if (!hasEventSystem)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            SceneManager.MoveGameObjectToScene(esObj, targetScene);
        }

        // Instantiate Prefab vào scene
        GameObject sceneInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, targetScene);
        LoadingScreenUI ui = sceneInstance.GetComponent<LoadingScreenUI>();
        if (ui != null)
        {
            SerializedObject so = new SerializedObject(ui);
            var testModeProp = so.FindProperty("testInStandaloneMode");
            if (testModeProp != null) testModeProp.boolValue = true;
            var targetSceneProp = so.FindProperty("testTargetScene");
            if (targetSceneProp != null) targetSceneProp.stringValue = "GamePlay";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Lưu scene
        EditorSceneManager.SaveScene(targetScene, ScenePath);
        Debug.Log($"[LoadingScreenBuilder] Đã tạo Scene tại: {ScenePath}");

        if (!foundUntitled)
        {
            EditorSceneManager.CloseScene(targetScene, true);
        }

        // Thêm vào Build Settings nếu chưa có
        AddSceneToBuildSettings(ScenePath);
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.All(s => s.path != scenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[LoadingScreenBuilder] Đã thêm {scenePath} vào EditorBuildSettings.");
        }
    }

    private static GameObject CreateLimbImage(GameObject parent, string name, Sprite sprite, Vector2 size, Vector2 pivot, Vector2 anchoredPos, Vector3? scale = null)
    {
        GameObject limb = new GameObject(name);
        limb.transform.SetParent(parent.transform, false);
        RectTransform rt = limb.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        rt.localRotation = Quaternion.identity;
        rt.localScale = scale ?? Vector3.one;

        Image img = limb.AddComponent<Image>();
        img.sprite = sprite;
        img.color = Color.white;
        img.raycastTarget = false;
        return limb;
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
#endif
