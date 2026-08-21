using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DesertPropSpawner))]
public class DesertPropSpawnerEditor : Editor
{
    private SerializedProperty desertChapterNumber;
    private SerializedProperty spawnOnlyInDesertChapter;
    private SerializedProperty props;
    private SerializedProperty obstacleDensity;
    private SerializedProperty decorationDensity;
    private SerializedProperty mapEdgePadding;
    private SerializedProperty obstacleMinSpacing;
    private SerializedProperty decorationMinSpacing;
    private SerializedProperty playerStartClearRadius;
    private SerializedProperty attemptsPerProp;
    private SerializedProperty colliderWidthRatio;
    private SerializedProperty colliderHeightRatio;
    private SerializedProperty useRandomSeed;
    private SerializedProperty randomSeed;
    private SerializedProperty obstacleSortingBase;
    private SerializedProperty decorationSortingOrder;

    private void OnEnable()
    {
        desertChapterNumber = serializedObject.FindProperty("desertChapterNumber");
        spawnOnlyInDesertChapter = serializedObject.FindProperty("spawnOnlyInDesertChapter");
        props = serializedObject.FindProperty("props");
        obstacleDensity = serializedObject.FindProperty("obstacleDensity");
        decorationDensity = serializedObject.FindProperty("decorationDensity");
        mapEdgePadding = serializedObject.FindProperty("mapEdgePadding");
        obstacleMinSpacing = serializedObject.FindProperty("obstacleMinSpacing");
        decorationMinSpacing = serializedObject.FindProperty("decorationMinSpacing");
        playerStartClearRadius = serializedObject.FindProperty("playerStartClearRadius");
        attemptsPerProp = serializedObject.FindProperty("attemptsPerProp");
        colliderWidthRatio = serializedObject.FindProperty("colliderWidthRatio");
        colliderHeightRatio = serializedObject.FindProperty("colliderHeightRatio");
        useRandomSeed = serializedObject.FindProperty("useRandomSeed");
        randomSeed = serializedObject.FindProperty("randomSeed");
        obstacleSortingBase = serializedObject.FindProperty("obstacleSortingBase");
        decorationSortingOrder = serializedObject.FindProperty("decorationSortingOrder");

        if (!Application.isPlaying)
        {
            EditorApplication.delayCall += GeneratePreviewIfAvailable;
        }
    }

    private void OnDisable()
    {
        EditorApplication.delayCall -= GeneratePreviewIfAvailable;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();

        DrawChapterSettings();
        EditorGUILayout.Space(8f);
        DrawProps();
        EditorGUILayout.Space(8f);
        DrawDensitySettings();
        EditorGUILayout.Space(8f);
        DrawSpacingSettings();
        EditorGUILayout.Space(8f);
        DrawColliderSettings();
        EditorGUILayout.Space(8f);
        DrawRandomAndDisplaySettings();

        bool changed = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(10f);
        DrawPreviewButtons();

        if (changed && !Application.isPlaying)
        {
            GeneratePreviewIfAvailable();
            SceneView.RepaintAll();
        }
    }

    private void DrawChapterSettings()
    {
        EditorGUILayout.LabelField("Bản đồ sa mạc", EditorStyles.boldLabel);
        Draw(desertChapterNumber, "Số Chapter sa mạc", "Chỉ sinh prefab khi Chapter đang chơi có số này.");
        Draw(spawnOnlyInDesertChapter, "Chỉ sinh ở Chapter sa mạc", "Tắt để cho phép hệ thống sinh ở mọi Chapter.");
    }

    private void DrawProps()
    {
        EditorGUILayout.LabelField("Prefab và quyền chặn Player", EditorStyles.boldLabel);
        props.isExpanded = EditorGUILayout.Foldout(props.isExpanded, new GUIContent("Danh sách prefab", "Mỗi dòng quy định loại, khả năng chặn, tỉ lệ xuất hiện và kích thước của một prefab."), true);
        if (!props.isExpanded)
        {
            return;
        }

        int newSize = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("Số lượng prefab", "Tổng số prefab được dùng để sinh ngẫu nhiên."), props.arraySize));
        if (newSize != props.arraySize)
        {
            props.arraySize = newSize;
        }

        EditorGUI.indentLevel++;
        for (int i = 0; i < props.arraySize; i++)
        {
            SerializedProperty entry = props.GetArrayElementAtIndex(i);
            SerializedProperty prefab = entry.FindPropertyRelative("prefab");
            SerializedProperty kind = entry.FindPropertyRelative("kind");
            SerializedProperty blockPlayer = entry.FindPropertyRelative("blockPlayer");
            SerializedProperty weight = entry.FindPropertyRelative("weight");
            SerializedProperty scale = entry.FindPropertyRelative("scaleMultiplierRange");

            string title = prefab.objectReferenceValue != null ? prefab.objectReferenceValue.name : $"Prefab {i + 1}";
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"{i + 1}. {title}", EditorStyles.boldLabel);
            Draw(prefab, "Prefab", "Prefab sẽ được chọn để sinh trên bản đồ.");
            kind.enumValueIndex = EditorGUILayout.Popup(
                new GUIContent("Phân loại", "Chướng ngại có thể chặn Player; họa tiết luôn cho phép đi xuyên."),
                kind.enumValueIndex,
                new[] { "Chướng ngại", "Họa tiết" });

            using (new EditorGUI.DisabledScope(kind.enumValueIndex == (int)DesertPropSpawner.PropKind.Decoration))
            {
                if (kind.enumValueIndex == (int)DesertPropSpawner.PropKind.Decoration)
                {
                    blockPlayer.boolValue = false;
                }
                Draw(blockPlayer, "Chặn Player", "Bật để tự tạo collider ở phần chân của vật này.");
            }

            Draw(weight, "Tỉ lệ xuất hiện", "Số càng lớn thì prefab càng thường được chọn so với prefab cùng loại.");
            Draw(scale, "Khoảng biến đổi kích thước", "Khoảng scale ngẫu nhiên nhân với scale gốc của prefab.");
            EditorGUILayout.EndVertical();
        }
        EditorGUI.indentLevel--;
    }

    private void DrawDensitySettings()
    {
        EditorGUILayout.LabelField("Mật độ (số vật trên 100 đơn vị vuông)", EditorStyles.boldLabel);
        Draw(obstacleDensity, "Mật độ chướng ngại", "Số chướng ngại trung bình trên mỗi 100 đơn vị vuông của bản đồ.");
        Draw(decorationDensity, "Mật độ họa tiết", "Số họa tiết trung bình trên mỗi 100 đơn vị vuông của bản đồ.");
    }

    private void DrawSpacingSettings()
    {
        EditorGUILayout.LabelField("Khoảng cách và vùng trống", EditorStyles.boldLabel);
        Draw(mapEdgePadding, "Khoảng cách với mép map", "Không sinh vật trong khoảng này tính từ mép bản đồ.");
        Draw(obstacleMinSpacing, "Khoảng cách giữa chướng ngại", "Khoảng cách tối thiểu giữa tâm của hai chướng ngại.");
        Draw(decorationMinSpacing, "Khoảng cách giữa họa tiết", "Khoảng cách tối thiểu giữa tâm của hai họa tiết.");
        Draw(playerStartClearRadius, "Vùng trống quanh Player", "Bán kính không sinh vật quanh vị trí bắt đầu của Player.");
        Draw(attemptsPerProp, "Số lần thử cho mỗi vật", "Tăng giá trị này nếu mật độ cao nhưng hệ thống không đặt đủ vật.");
    }

    private void DrawColliderSettings()
    {
        EditorGUILayout.LabelField("Collider phần chân", EditorStyles.boldLabel);
        Draw(colliderWidthRatio, "Tỉ lệ chiều rộng collider", "Chiều rộng collider so với sprite; chỉ áp dụng cho vật bật Chặn Player.");
        Draw(colliderHeightRatio, "Tỉ lệ chiều cao collider", "Chiều cao collider phần chân so với sprite.");
    }

    private void DrawRandomAndDisplaySettings()
    {
        EditorGUILayout.LabelField("Ngẫu nhiên và hiển thị", EditorStyles.boldLabel);
        Draw(useRandomSeed, "Dùng seed cố định", "Bật để cùng một seed luôn tạo lại cùng một bố cục.");
        if (useRandomSeed.boolValue)
        {
            Draw(randomSeed, "Seed ngẫu nhiên", "Đổi số này để tạo một bố cục cố định khác.");
        }
        Draw(obstacleSortingBase, "Thứ tự hiển thị chướng ngại", "Mốc Sorting Order của chướng ngại trước khi cộng thứ tự theo trục Y.");
        Draw(decorationSortingOrder, "Thứ tự hiển thị họa tiết", "Sorting Order cố định của họa tiết, nên lớn hơn nền và nhỏ hơn nhân vật.");
    }

    private void DrawPreviewButtons()
    {
        EditorGUILayout.HelpBox("Bản xem trước chỉ hiển thị trong Scene và không được lưu thành object vào scene.", MessageType.Info);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("Tạo bản xem trước trên Map", "Sinh lại toàn bộ chướng ngại và họa tiết ngay trong Scene View."), GUILayout.Height(32f)))
        {
            GeneratePreviewIfAvailable();
            SceneView.RepaintAll();
        }
        if (GUILayout.Button(new GUIContent("Xóa bản xem trước", "Xóa toàn bộ vật đang được xem trước."), GUILayout.Height(32f)))
        {
            ((DesertPropSpawner)target).ClearGenerated();
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void GeneratePreviewIfAvailable()
    {
        if (target != null && !Application.isPlaying)
        {
            ((DesertPropSpawner)target).GeneratePreview();
        }
    }

    private static void Draw(SerializedProperty property, string label, string tooltip)
    {
        EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip));
    }
}
