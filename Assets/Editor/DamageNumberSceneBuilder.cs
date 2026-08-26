#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DamageNumberSceneBuilder
{
    private const string GamePlayScenePath = "Assets/Scenes/GamePlay.unity";
    private const string PrefabPath = "Assets/Prefabs/DamageNumber.prefab";
    private const string FontAssetPath = "Assets/Fonts/Nunito/Nunito SDF.asset";
    private const string StrokeMaterialPath = "Assets/Fonts/Nunito/Nunito SDF - Stroke.mat";

    [MenuItem("PGE/Combat/Cài đặt Damage Numbers vào GamePlay Scene")]
    public static void BuildAndSetup()
    {
        // 1. Tạo hoặc nạp Prefab DamageNumber
        GameObject prefab = CreateOrUpdatePrefab();

        // 2. Mở scene GamePlay và thiết lập DamageNumberManager
        Scene scene = EditorSceneManager.OpenScene(GamePlayScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[Damage Numbers] Không tìm thấy scene tại {GamePlayScenePath}");
            return;
        }

        DamageNumberManager manager = Object.FindObjectOfType<DamageNumberManager>();
        if (manager == null)
        {
            GameObject managerGo = new GameObject("[DamageNumberManager]");
            manager = managerGo.AddComponent<DamageNumberManager>();
            Undo.RegisterCreatedObjectUndo(managerGo, "Tạo DamageNumberManager");
        }

        SerializedObject so = new SerializedObject(manager);
        if (prefab != null)
        {
            so.FindProperty("damageNumberPrefab").objectReferenceValue = prefab.GetComponent<DamageNumber>();
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font != null)
        {
            so.FindProperty("fontAsset").objectReferenceValue = font;
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(StrokeMaterialPath);
        if (mat != null)
        {
            so.FindProperty("strokeMaterial").objectReferenceValue = mat;
        }

        so.FindProperty("initialPoolSize").intValue = 60;
        so.FindProperty("defaultFontSize").floatValue = 5.0f;
        so.FindProperty("sortingLayerName").stringValue = "UI";
        so.FindProperty("sortingOrder").intValue = 600;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[Damage Numbers] Đã cài đặt thành công hệ thống Damage Numbers vào scene GamePlay!");
    }

    public static GameObject CreateOrUpdatePrefab()
    {
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        Material strokeMaterial = AssetDatabase.LoadAssetAtPath<Material>(StrokeMaterialPath);

        GameObject tempObj = new GameObject("DamageNumber");
        TextMeshPro tmp = tempObj.AddComponent<TextMeshPro>();
        tmp.text = "99";
        tmp.fontSize = 5.0f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;

        if (fontAsset != null)
        {
            tmp.font = fontAsset;
        }

        if (strokeMaterial != null)
        {
            tmp.fontSharedMaterial = strokeMaterial;
        }

        MeshRenderer mr = tempObj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingLayerName = "UI";
            mr.sortingOrder = 600;
        }

        DamageNumber damageNumber = tempObj.AddComponent<DamageNumber>();
        damageNumber.EnsureComponents();

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempObj, PrefabPath);
        Object.DestroyImmediate(tempObj);

        AssetDatabase.SaveAssets();
        return savedPrefab;
    }
}
#endif
