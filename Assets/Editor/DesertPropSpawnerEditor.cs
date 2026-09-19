using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DesertPropSpawner))]
public class DesertPropSpawnerEditor : Editor
{
    private SerializedProperty desertChapterNumber;
    private SerializedProperty spawnOnlyInDesertChapter;
    private SerializedProperty props;
    private SerializedProperty chapterPropsList;
    private SerializedProperty obstacleDensity;
    private SerializedProperty decorationDensity;
    private SerializedProperty mapEdgePadding;
    private SerializedProperty obstacleMinSpacing;
    private SerializedProperty decorationMinSpacing;
    private SerializedProperty playerStartClearRadius;
    private SerializedProperty attemptsPerProp;
    private SerializedProperty colliderWidthRatio;
    private SerializedProperty colliderHeightRatio;
    private SerializedProperty allowEnemiesToPassThrough;
    private SerializedProperty useRandomSeed;
    private SerializedProperty randomSeed;
    private SerializedProperty obstacleSortingBase;
    private SerializedProperty decorationSortingOrder;

    private int selectedChapterTab = 0;
    private readonly string[] chapterTabs = new[] { "Ch 1: Sa mạc", "Ch 2: Rừng đột biến", "Ch 3: Đầm lầy độc" };

    private void OnEnable()
    {
        desertChapterNumber = serializedObject.FindProperty("desertChapterNumber");
        spawnOnlyInDesertChapter = serializedObject.FindProperty("spawnOnlyInDesertChapter");
        props = serializedObject.FindProperty("props");
        chapterPropsList = serializedObject.FindProperty("chapterPropsList");
        obstacleDensity = serializedObject.FindProperty("obstacleDensity");
        decorationDensity = serializedObject.FindProperty("decorationDensity");
        mapEdgePadding = serializedObject.FindProperty("mapEdgePadding");
        obstacleMinSpacing = serializedObject.FindProperty("obstacleMinSpacing");
        decorationMinSpacing = serializedObject.FindProperty("decorationMinSpacing");
        playerStartClearRadius = serializedObject.FindProperty("playerStartClearRadius");
        attemptsPerProp = serializedObject.FindProperty("attemptsPerProp");
        colliderWidthRatio = serializedObject.FindProperty("colliderWidthRatio");
        colliderHeightRatio = serializedObject.FindProperty("colliderHeightRatio");
        allowEnemiesToPassThrough = serializedObject.FindProperty("allowEnemiesToPassThrough");
        useRandomSeed = serializedObject.FindProperty("useRandomSeed");
        randomSeed = serializedObject.FindProperty("randomSeed");
        obstacleSortingBase = serializedObject.FindProperty("obstacleSortingBase");
        decorationSortingOrder = serializedObject.FindProperty("decorationSortingOrder");

        EnsureChapterConfigsExist();

        if (!Application.isPlaying)
        {
            EditorApplication.delayCall += GeneratePreviewIfAvailable;
        }
    }

    private void EnsureChapterConfigsExist()
    {
        if (chapterPropsList == null) return;

        bool changed = false;
        changed |= EnsureChapterConfig(2, "Rừng đột biến (Mutant Forest)");
        changed |= EnsureChapterConfig(3, "Đầm lầy độc (Toxic Swamp)");

        if (changed)
        {
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
    }

    private bool EnsureChapterConfig(int chNum, string chName)
    {
        for (int i = 0; i < chapterPropsList.arraySize; i++)
        {
            SerializedProperty elem = chapterPropsList.GetArrayElementAtIndex(i);
            if (elem.FindPropertyRelative("chapterNumber").intValue == chNum)
            {
                SerializedProperty propsArray = elem.FindPropertyRelative("props");
                if (propsArray.arraySize == 0)
                {
                    PopulateDefaultPropsForChapter(propsArray, chNum);
                    propsArray.isExpanded = true;
                    return true;
                }
                return false;
            }
        }

        int newIdx = chapterPropsList.arraySize;
        chapterPropsList.InsertArrayElementAtIndex(newIdx);
        SerializedProperty newElem = chapterPropsList.GetArrayElementAtIndex(newIdx);
        newElem.FindPropertyRelative("chapterNumber").intValue = chNum;
        newElem.FindPropertyRelative("chapterName").stringValue = chName;
        SerializedProperty newPropsArray = newElem.FindPropertyRelative("props");
        newPropsArray.ClearArray();
        PopulateDefaultPropsForChapter(newPropsArray, chNum);
        newPropsArray.isExpanded = true;
        return true;
    }

    private static void PopulateDefaultPropsForChapter(SerializedProperty propsListProp, int chapterNumber)
    {
        propsListProp.ClearArray();

        if (chapterNumber == 2)
        {
            AddProp(propsListProp, "Assets/Prefabs/Map 2 - Mutant Forest/nam_tim.prefab", DesertPropSpawner.PropKind.Obstacle, true);
            AddProp(propsListProp, "Assets/Prefabs/Map 2 - Mutant Forest/cay_hoa_xoan.prefab", DesertPropSpawner.PropKind.Obstacle, true);
            AddProp(propsListProp, "Assets/Prefabs/Map 2 - Mutant Forest/bui_cay.prefab", DesertPropSpawner.PropKind.Obstacle, true);
            AddProp(propsListProp, "Assets/Prefabs/Map 2 - Mutant Forest/bui_co_1.prefab", DesertPropSpawner.PropKind.Decoration, false);
            AddProp(propsListProp, "Assets/Prefabs/Map 2 - Mutant Forest/bui_co_2.prefab", DesertPropSpawner.PropKind.Decoration, false);
            AddProp(propsListProp, "Assets/Prefabs/Map 2 - Mutant Forest/bui_co_doi.prefab", DesertPropSpawner.PropKind.Decoration, false);
        }
        else if (chapterNumber == 3)
        {
            AddProp(propsListProp, "Assets/Prefabs/Map 3 - Toxic Swamp/tang_da.prefab", DesertPropSpawner.PropKind.Obstacle, true);
            AddProp(propsListProp, "Assets/Prefabs/Map 3 - Toxic Swamp/cum_cay_cam.prefab", DesertPropSpawner.PropKind.Obstacle, true);
            AddProp(propsListProp, "Assets/Prefabs/Map 3 - Toxic Swamp/cay_bup_cam.prefab", DesertPropSpawner.PropKind.Obstacle, true);
            AddProp(propsListProp, "Assets/Prefabs/Map 3 - Toxic Swamp/nam_bach_tuoc.prefab", DesertPropSpawner.PropKind.Obstacle, true);
            AddProp(propsListProp, "Assets/Prefabs/Map 3 - Toxic Swamp/mam_vang.prefab", DesertPropSpawner.PropKind.Decoration, false);
            AddProp(propsListProp, "Assets/Prefabs/Map 3 - Toxic Swamp/mam_xanh.prefab", DesertPropSpawner.PropKind.Decoration, false);
            AddProp(propsListProp, "Assets/Prefabs/Map 3 - Toxic Swamp/bui_hoa_xanh.prefab", DesertPropSpawner.PropKind.Decoration, false);
        }
    }

    private static void AddProp(SerializedProperty propsListProp, string assetPath, DesertPropSpawner.PropKind kind, bool blockPlayer)
    {
        GameObject prefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefabObj == null) return;

        int idx = propsListProp.arraySize;
        propsListProp.InsertArrayElementAtIndex(idx);
        SerializedProperty entry = propsListProp.GetArrayElementAtIndex(idx);
        entry.FindPropertyRelative("prefab").objectReferenceValue = prefabObj;
        entry.FindPropertyRelative("kind").enumValueIndex = (int)kind;
        entry.FindPropertyRelative("blockPlayer").boolValue = blockPlayer;
        entry.FindPropertyRelative("weight").floatValue = 1f;
        entry.FindPropertyRelative("scaleMultiplierRange").vector2Value = new Vector2(0.9f, 1.1f);
    }

    private void OnDisable()
    {
        EditorApplication.delayCall -= GeneratePreviewIfAvailable;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EnsureChapterConfigsExist();
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
        EditorGUILayout.LabelField("Bản đồ & Chế độ sinh", EditorStyles.boldLabel);
        Draw(desertChapterNumber, "Số Chapter sa mạc mặc định", "Dùng cho Chapter 1.");
        Draw(spawnOnlyInDesertChapter, "Chỉ sinh ở Chapter sa mạc", "Tắt để cho phép hệ thống tự sinh chướng ngại theo từng Chapter.");
    }

    private void DrawProps()
    {
        EditorGUILayout.LabelField("Cấu hình Prefab theo Chapter", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Chọn Chapter bên dưới để xem và chỉnh sửa danh sách chướng ngại vật / họa tiết tương ứng:", MessageType.None);

        int prevTab = selectedChapterTab;
        selectedChapterTab = GUILayout.Toolbar(selectedChapterTab, chapterTabs, GUILayout.Height(26f));
        if (prevTab != selectedChapterTab && !Application.isPlaying)
        {
            GeneratePreviewIfAvailable();
            SceneView.RepaintAll();
        }
        EditorGUILayout.Space(4f);

        if (selectedChapterTab == 0)
        {
            DrawPropsList(props, "Chapter 1: Sa mạc (Desert)");
        }
        else
        {
            int targetChNum = selectedChapterTab + 1;
            SerializedProperty targetChProps = GetChapterPropsProperty(targetChNum);
            if (targetChProps == null || targetChProps.arraySize == 0)
            {
                EnsureChapterConfigsExist();
                targetChProps = GetChapterPropsProperty(targetChNum);
            }

            if (targetChProps != null)
            {
                DrawPropsList(targetChProps, chapterTabs[selectedChapterTab]);
            }
            else
            {
                EditorGUILayout.HelpBox($"Chưa tìm thấy dữ liệu cấu hình cho Chapter {targetChNum}.", MessageType.Warning);
            }
        }
    }

    private SerializedProperty GetChapterPropsProperty(int chapterNumber)
    {
        for (int i = 0; i < chapterPropsList.arraySize; i++)
        {
            SerializedProperty elem = chapterPropsList.GetArrayElementAtIndex(i);
            if (elem.FindPropertyRelative("chapterNumber").intValue == chapterNumber)
            {
                return elem.FindPropertyRelative("props");
            }
        }
        return null;
    }

    private void DrawPropsList(SerializedProperty propsListProp, string headerLabel)
    {
        if (!propsListProp.isExpanded && propsListProp.arraySize > 0)
        {
            propsListProp.isExpanded = true;
        }

        propsListProp.isExpanded = EditorGUILayout.Foldout(propsListProp.isExpanded, new GUIContent($"Danh sách prefab - {headerLabel} ({propsListProp.arraySize} vật)", "Mỗi dòng quy định loại, khả năng chặn, tỉ lệ xuất hiện và kích thước của một prefab."), true);
        if (!propsListProp.isExpanded)
        {
            return;
        }

        if (selectedChapterTab > 0)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent($"Nạp lại Prefab mặc định cho {chapterTabs[selectedChapterTab]}", "Khôi phục lại danh sách các prefab gốc đã được cắt sẵn cho Chapter này."), GUILayout.Height(24f)))
            {
                PopulateDefaultPropsForChapter(propsListProp, selectedChapterTab + 1);
                propsListProp.isExpanded = true;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
        }

        int newSize = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("Số lượng prefab", "Tổng số prefab được dùng để sinh ngẫu nhiên."), propsListProp.arraySize));
        if (newSize != propsListProp.arraySize)
        {
            propsListProp.arraySize = newSize;
        }

        EditorGUI.indentLevel++;
        for (int i = 0; i < propsListProp.arraySize; i++)
        {
            SerializedProperty entry = propsListProp.GetArrayElementAtIndex(i);
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
        EditorGUILayout.LabelField("Collider phần chân & Va chạm", EditorStyles.boldLabel);
        Draw(allowEnemiesToPassThrough, "Enemy đi xuyên chướng ngại", "Bật để quái vật có thể đi xuyên qua chướng ngại vật (chỉ chặn Player). Tắt nếu muốn chướng ngại vật chặn cả quái vật.");
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
        string currentTabName = chapterTabs[selectedChapterTab];
        if (GUILayout.Button(new GUIContent($"Tạo bản xem trước ({currentTabName})", "Sinh lại toàn bộ chướng ngại và họa tiết ngay trong Scene View cho Chapter đang chọn."), GUILayout.Height(32f)))
        {
            ((DesertPropSpawner)target).GeneratePreview(selectedChapterTab + 1);
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
            ((DesertPropSpawner)target).GeneratePreview(selectedChapterTab + 1);
        }
    }

    private static void Draw(SerializedProperty property, string label, string tooltip)
    {
        EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip));
    }
}
