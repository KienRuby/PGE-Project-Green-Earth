#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tool tự động tạo 5 Prefabs Companion Drone từ 5 sub-sprite trong icon buddy.png
/// và cấu hình BuddyCombatManager trên Player trong scene GamePlay.unity.
/// </summary>
[InitializeOnLoad]
public static class BuddyPrefabBuilder
{
    public const string IconSheetPath = "Assets/Sprites/UI/Buddy/icon buddy.png";
    public const string PrefabDir = "Assets/Prefabs/Buddy";
    public const string GamePlayScenePath = "Assets/Scenes/GamePlay.unity";
    public const string ProjectilePrefabPath = "Assets/Prefabs/Projectile.prefab";
    public const string HitVfxPrefabPath = "Assets/Prefabs/VFX Boom.prefab";

    private const string AutoBuiltKey = "PGE.BuddyPrefabBuilder.v1";

    static BuddyPrefabBuilder()
    {
        EditorApplication.delayCall += CheckAndAutoBuild;
    }

    private static void CheckAndAutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorPrefs.GetBool(AutoBuiltKey, false)) return;

        bool allExist = File.Exists($"{PrefabDir}/Buddy_Sloy.prefab") &&
                        File.Exists($"{PrefabDir}/Buddy_TurretBuffer.prefab") &&
                        File.Exists($"{PrefabDir}/Buddy_PurifyingDrone.prefab") &&
                        File.Exists($"{PrefabDir}/Buddy_RadarEye.prefab") &&
                        File.Exists($"{PrefabDir}/Buddy_AssaultBlaster.prefab");

        if (!allExist)
        {
            BuildAllBuddyPrefabsAndSetupPlayer();
        }
    }

    [MenuItem("PGE/Buddy/Build All Buddy Prefabs and Setup Player")]
    public static void BuildAllBuddyPrefabsAndSetupPlayer()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[BuddyPrefabBuilder] Vui lòng dừng Play Mode trước khi tạo Prefab.");
            return;
        }

        if (!Directory.Exists(PrefabDir))
        {
            Directory.CreateDirectory(PrefabDir);
            AssetDatabase.Refresh();
        }

        // 1. Tải toàn bộ sub-sprites từ icon buddy.png
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(IconSheetPath).OfType<Sprite>().ToArray();
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError($"[BuddyPrefabBuilder] Không thể nạp Sprite từ {IconSheetPath}. Vui lòng kiểm tra lại asset.");
            return;
        }

        Sprite spSloy = sprites.FirstOrDefault(s => s.name == "drone-snowflake");
        Sprite spTurretBuffer = sprites.FirstOrDefault(s => s.name == "drone-spider");
        Sprite spPurifying = sprites.FirstOrDefault(s => s.name == "drone-stealth-wing");
        Sprite spRadarEye = sprites.FirstOrDefault(s => s.name == "drone-antenna-eye");
        Sprite spAssaultBlaster = sprites.FirstOrDefault(s => s.name == "drone-cross-visor");

        if (spSloy == null || spTurretBuffer == null || spPurifying == null || spRadarEye == null || spAssaultBlaster == null)
        {
            Debug.LogError("[BuddyPrefabBuilder] Thiếu một hoặc nhiều sub-sprite trong icon buddy.png!");
            return;
        }

        GameObject projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
        GameObject vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HitVfxPrefabPath);

        // 2. Tạo 5 Prefabs
        GameObject pSloy = BuildSingleBuddyPrefab<SloyFrostBuddy>("Buddy_Sloy", 1, "Sloy", spSloy, (comp, go) =>
        {
            comp.ProjectilePrefab = projPrefab;
            comp.HitVfxPrefab = vfxPrefab;
        });

        GameObject pTurret = BuildSingleBuddyPrefab<TurretBufferBuddy>("Buddy_TurretBuffer", 2, "Turret Buffer", spTurretBuffer, (comp, go) =>
        {
            comp.ProjectilePrefab = projPrefab;
        });

        GameObject pPurifying = BuildSingleBuddyPrefab<PurifyingBuddy>("Buddy_PurifyingDrone", 10, "Purifying Drone", spPurifying, (comp, go) =>
        {
            comp.ProjectilePrefab = projPrefab;
            comp.PulseVfxPrefab = vfxPrefab;
        });

        GameObject pRadarEye = BuildSingleBuddyPrefab<RadarEyeBuddy>("Buddy_RadarEye", 3, "Radar Eye", spRadarEye, (comp, go) =>
        {
            // RadarEye sử dụng LineRenderer
        });

        GameObject pAssault = BuildSingleBuddyPrefab<AssaultBlasterBuddy>("Buddy_AssaultBlaster", 4, "Assault Blaster", spAssaultBlaster, (comp, go) =>
        {
            comp.ProjectilePrefab = projPrefab;
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 3. Cấu hình vào Player trong scene GamePlay.unity
        SetupPlayerInGamePlayScene(pSloy, pTurret, pPurifying, pRadarEye, pAssault);

        EditorPrefs.SetBool(AutoBuiltKey, true);
        Debug.Log("[BuddyPrefabBuilder] ✅ Đã tạo thành công 5 Prefab Buddy và liên kết vào Player trong GamePlay.unity!");
    }

    private static GameObject BuildSingleBuddyPrefab<T>(
        string prefabName,
        int buddyId,
        string displayName,
        Sprite sprite,
        Action<T, GameObject> customize) where T : BuddyCombatDrone
    {
        string path = $"{PrefabDir}/{prefabName}.prefab";

        GameObject root = new GameObject(prefabName);
        root.transform.position = Vector3.zero;
        root.transform.localScale = new Vector3(0.12f, 0.12f, 1f);

        // SpriteRenderer
        SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 15; // Hiển thị trên nền sàn đấu và quái
        sr.material = new Material(Shader.Find("Sprites/Default"));

        // FirePoint child
        GameObject firePointObj = new GameObject("FirePoint");
        firePointObj.transform.SetParent(root.transform);
        firePointObj.transform.localPosition = new Vector3(0f, -0.1f, 0f);
        firePointObj.transform.localRotation = Quaternion.identity;
        firePointObj.transform.localScale = Vector3.one;

        // Specialized Component
        T droneComp = root.AddComponent<T>();

        // Set private serialized fields via SerializedObject
        SerializedObject so = new SerializedObject(droneComp);
        so.FindProperty("buddyId").intValue = buddyId;
        so.FindProperty("droneName").stringValue = displayName;
        so.FindProperty("spriteRenderer").objectReferenceValue = sr;
        so.FindProperty("firePoint").objectReferenceValue = firePointObj.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        customize?.Invoke(droneComp, root);

        // Lưu thành Prefab Asset
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);

        return savedPrefab;
    }

    private static void SetupPlayerInGamePlayScene(
        GameObject pSloy,
        GameObject pTurret,
        GameObject pPurifying,
        GameObject pRadarEye,
        GameObject pAssault)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        bool isGamePlayActive = string.Equals(activeScene.path, GamePlayScenePath, StringComparison.OrdinalIgnoreCase);

        Scene gamePlayScene;
        bool openedTemporarily = !isGamePlayActive;

        if (isGamePlayActive)
        {
            gamePlayScene = activeScene;
        }
        else
        {
            gamePlayScene = EditorSceneManager.OpenScene(GamePlayScenePath, OpenSceneMode.Additive);
        }

        try
        {
            GameObject playerObj = gamePlayScene.GetRootGameObjects()
                .FirstOrDefault(g => g.name == "Player" || g.CompareTag("Player"));

            if (playerObj == null)
            {
                Debug.LogWarning("[BuddyPrefabBuilder] Không tìm thấy GameObject Player trong GamePlay.unity!");
                return;
            }

            BuddyCombatManager manager = playerObj.GetComponent<BuddyCombatManager>();
            if (manager == null)
            {
                manager = playerObj.AddComponent<BuddyCombatManager>();
            }

            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("sloyPrefab").objectReferenceValue = pSloy;
            so.FindProperty("turretBufferPrefab").objectReferenceValue = pTurret;
            so.FindProperty("purifyingPrefab").objectReferenceValue = pPurifying;
            so.FindProperty("radarEyePrefab").objectReferenceValue = pRadarEye;
            so.FindProperty("assaultBlasterPrefab").objectReferenceValue = pAssault;

            SerializedProperty listProp = so.FindProperty("registeredPrefabs");
            listProp.ClearArray();
            AddRegisteredEntry(listProp, 1, pSloy);
            AddRegisteredEntry(listProp, 2, pTurret);
            AddRegisteredEntry(listProp, 10, pPurifying);
            AddRegisteredEntry(listProp, 3, pRadarEye);
            AddRegisteredEntry(listProp, 4, pAssault);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(playerObj);
            EditorSceneManager.MarkSceneDirty(gamePlayScene);
            EditorSceneManager.SaveScene(gamePlayScene);
        }
        finally
        {
            if (openedTemporarily && gamePlayScene.IsValid() && gamePlayScene.isLoaded)
            {
                EditorSceneManager.CloseScene(gamePlayScene, true);
            }
        }
    }

    private static void AddRegisteredEntry(SerializedProperty listProp, int id, GameObject prefab)
    {
        int index = listProp.arraySize;
        listProp.InsertArrayElementAtIndex(index);
        SerializedProperty elem = listProp.GetArrayElementAtIndex(index);
        elem.FindPropertyRelative("buddyId").intValue = id;
        elem.FindPropertyRelative("prefab").objectReferenceValue = prefab;
    }
}
#endif
