#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Công cụ Editor tự động dựng hoàn chỉnh Scene TowerDef (Assets/Scenes/TowerDef.unity):
/// - Tạo cấu trúc phân cấp chuẩn Canvas 1080x1920 portrait.
/// - Đặt nền tối 7 cột (nền map thủ thành.png).
/// - Dựng tường gạch ngang và Cổng sắt ở cột chính giữa (Gate_Metal) có thanh máu xanh và huy hiệu nâng cấp.
/// - Dựng lưới phòng thủ 7x4 gồm 28 ô [+] với 3 công trình ban đầu khớp 100% Ảnh Thiết Kế 2:
///   + Hàng 3 từ dưới lên, Cột 2: Pháo xanh (Turret Cyan) kèm nòng và icon nâng cấp.
///   + Hàng 2 từ dưới lên, Cột 3: Trụ năng lượng tím (Pawn Tower Purple) kèm icon nâng cấp.
///   + Hàng 2 từ dưới lên, Cột 4: Giường vàng/xanh (Core Bed Green) kèm icon nâng cấp.
/// - Thanh công cụ đỉnh màn hình: Nút Back đỏ, Vàng (50), Năng lượng (0).
/// - Khởi tạo GameManager và gán đầy đủ SerializedField để Scene độc lập và chạy ngay lập tức.
/// </summary>
public static class TowerDefSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/TowerDef.unity";

    [MenuItem("PGE/Tower Def/Build Tower Def Scene")]
    public static void BuildTowerDefScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[TowerDefSceneBuilder] Không thể dựng Scene khi đang trong Play Mode.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        bool isCurrentActive = activeScene.IsValid() && activeScene.path == ScenePath;
        bool needClose = false;

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            needClose = true;
        }

        EditorSceneManager.SetActiveScene(scene);

        // 1. Camera
        Camera cam = FindInScene<Camera>(scene);
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cam = camObj.GetComponent<Camera>();
            camObj.tag = "MainCamera";
            SceneManager.MoveGameObjectToScene(camObj, scene);
        }
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
        cam.transform.position = new Vector3(0f, 0f, -10f);

        // 2. EventSystem
        if (FindInScene<EventSystem>(scene) == null)
        {
            GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            SceneManager.MoveGameObjectToScene(esObj, scene);
        }

        // 3. Canvas
        Canvas canvas = FindInScene<Canvas>(scene);
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("TowerDefCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = canvasObj.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080f, 1920f);
            cs.matchWidthOrHeight = 0f;
            SceneManager.MoveGameObjectToScene(canvasObj, scene);
        }

        // 4. TowerDefGameManager
        TowerDefGameManager gm = FindInScene<TowerDefGameManager>(scene);
        if (gm == null)
        {
            GameObject gmObj = new GameObject("TowerDefGameManager", typeof(TowerDefGameManager));
            gm = gmObj.GetComponent<TowerDefGameManager>();
            SceneManager.MoveGameObjectToScene(gmObj, scene);
        }

        // Nạp và gán các SerializedField Sprite trực tiếp qua SerializedObject
        AssignSerializedSprites(gm);

        // Gọi EnsureSceneMap để dựng cấu trúc chi tiết
        gm.EnsureSceneMap();

        // Đồng bộ lại SerializedObject sau khi EnsureSceneMap tạo UI và Gate
        SyncSerializedReferences(gm, scene);

        EditorUtility.SetDirty(gm);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (needClose && activeScene.IsValid())
        {
            EditorSceneManager.SetActiveScene(activeScene);
            EditorSceneManager.CloseScene(scene, true);
        }

        Debug.Log("[TowerDefSceneBuilder] Đã dựng thành công Scene TowerDef hoàn chỉnh khớp 100% Thiết Kế (Ảnh 2)!");
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var comp = root.GetComponentInChildren<T>(true);
            if (comp != null) return comp;
        }
        return null;
    }

    private static void AssignSerializedSprites(TowerDefGameManager gm)
    {
        SerializedObject so = new SerializedObject(gm);
        string tilesDir = "Assets/Sprites/Mini game/Sliced/Tiles/";
        string uiDir = "Assets/Sprites/Mini game/Sliced/UI/";
        string towersDir = "Assets/Sprites/Mini game/Sliced/Towers/";
        string monstersDir = "Assets/Sprites/Mini game/Sliced/Monsters/";

        Sprite darkTile = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Mini game/nền map thủ thành.png")
                          ?? AssetDatabase.LoadAssetAtPath<Sprite>(tilesDir + "Tile_Floor_Dark.png");
        Sprite wallStrip = AssetDatabase.LoadAssetAtPath<Sprite>(tilesDir + "Wall_Brick_Strip.png");
        Sprite gate = AssetDatabase.LoadAssetAtPath<Sprite>(tilesDir + "Gate_Metal.png");
        Sprite plusTile = AssetDatabase.LoadAssetAtPath<Sprite>(tilesDir + "Tile_Placement_Plus.png");

        Sprite turretBase = AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Base_01_Cyan.png");
        Sprite turretGun = AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Turret_Gun_01_Cyan.png");
        Sprite pawnTower = AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Pawn_Tower_03_Purple.png");
        Sprite corePod = AssetDatabase.LoadAssetAtPath<Sprite>(towersDir + "Core_Pod_Green.png");

        Sprite upgradeCircle = AssetDatabase.LoadAssetAtPath<Sprite>(uiDir + "Icon_Upgrade_Circle.png");
        Sprite greenBar = AssetDatabase.LoadAssetAtPath<Sprite>(uiDir + "Bar_Green.png");
        Sprite backArrow = AssetDatabase.LoadAssetAtPath<Sprite>(uiDir + "Btn_Arrow_Back.png");
        Sprite coinIcon = AssetDatabase.LoadAssetAtPath<Sprite>(uiDir + "Icon_Coin.png");
        Sprite energyIcon = AssetDatabase.LoadAssetAtPath<Sprite>(uiDir + "Icon_Energy.png");

        Sprite creep = AssetDatabase.LoadAssetAtPath<Sprite>(monstersDir + "Creep_Mine.png");
        Sprite boss = AssetDatabase.LoadAssetAtPath<Sprite>(monstersDir + "Boss_Mine.png");

        SetProperty(so, "darkTileSprite", darkTile);
        SetProperty(so, "wallStripSprite", wallStrip);
        SetProperty(so, "gateSprite", gate);
        SetProperty(so, "plusTileSprite", plusTile);

        SetProperty(so, "turretBaseSprite", turretBase);
        SetProperty(so, "turretGunSprite", turretGun);
        SetProperty(so, "pawnTowerSprite", pawnTower);
        SetProperty(so, "corePodSprite", corePod);

        SetProperty(so, "upgradeCircleSprite", upgradeCircle);
        SetProperty(so, "greenBarSprite", greenBar);
        SetProperty(so, "backArrowSprite", backArrow);
        SetProperty(so, "coinIconSprite", coinIcon);
        SetProperty(so, "energyIconSprite", energyIcon);

        SetProperty(so, "creepSprite", creep);
        SetProperty(so, "bossSprite", boss);

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SyncSerializedReferences(TowerDefGameManager gm, Scene scene)
    {
        SerializedObject so = new SerializedObject(gm);

        TowerDefGate gate = FindInScene<TowerDefGate>(scene);
        TowerDefUIController uiCtrl = FindInScene<TowerDefUIController>(scene);

        Transform playArea = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var p = root.transform.Find("PlayArea") ?? (root.name == "PlayArea" ? root.transform : null);
            if (p != null) { playArea = p; break; }
            var childP = root.GetComponentInChildren<Transform>(true);
            if (childP != null && childP.name == "PlayArea") { playArea = childP; break; }
        }

        Transform enemyRoot = playArea != null ? playArea.Find("EnemiesRoot") : null;
        Transform projRoot = playArea != null ? playArea.Find("ProjectilesRoot") : null;

        if (gate != null) SetProperty(so, "gate", gate);
        if (uiCtrl != null) SetProperty(so, "uiController", uiCtrl);
        if (enemyRoot != null) SetProperty(so, "enemySpawnParent", enemyRoot);
        if (projRoot != null) SetProperty(so, "projectileParent", projRoot);

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetProperty(SerializedObject so, string propertyName, Object targetObj)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
        {
            prop.objectReferenceValue = targetObj;
        }
    }
}

[InitializeOnLoad]
public static class TowerDefSceneAutoBaker
{
    private const string AutoBakeKey = "PGE_TowerDef_AutoBaked_v3";

    static TowerDefSceneAutoBaker()
    {
        EditorApplication.delayCall += OnEditorLoaded;
    }

    private static void OnEditorLoaded()
    {
        if (SessionState.GetBool(AutoBakeKey, false)) return;
        SessionState.SetBool(AutoBakeKey, true);

        if (System.IO.File.Exists(TowerDefSceneBuilder.ScenePath))
        {
            TowerDefSceneBuilder.BuildTowerDefScene();
        }
    }
}
#endif
