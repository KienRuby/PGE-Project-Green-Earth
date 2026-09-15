#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(ShopEditModeScroller))]
public class ShopEditModeScrollerEditor : Editor
{
    private ShopEditModeScroller scroller;

    private void OnEnable()
    {
        scroller = (ShopEditModeScroller)target;
        if (scroller != null)
        {
            scroller.AutoFindReferences();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (scroller == null) return;

        // Header Styling
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };

        GUIStyle sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = new Color(0.2f, 0.8f, 1f) }
        };

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🛠️ ĐIỀU KHIỂN CUỘN SHOP (EDIT MODE)", titleStyle);
        EditorGUILayout.LabelField("Cuộn mượt mà & chỉnh sửa toàn bộ sprite trong Scene/Game View không cần Play Mode.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 1. THANH TRƯỢT CUỘN
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("📜 THANH TRƯỢT VỊ TRÍ CUỘN", sectionHeaderStyle);

        float currentPercent = scroller.ScrollPercent;
        EditorGUI.BeginChangeCheck();
        float newPercent = EditorGUILayout.Slider(new GUIContent("Vị trí cuộn (0% - 100%)", "0% = Đỉnh Shop, 100% = Đáy Shop"), currentPercent, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(scroller.Content, "Scroll Shop Content");
            scroller.ScrollPercent = newPercent;
            SceneView.RepaintAll();
        }

        // Quick percent jumps
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔝 Đỉnh (0%)", GUILayout.Height(24)))
        {
            Undo.RecordObject(scroller.Content, "Scroll Shop Top");
            scroller.ResetToTop();
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("25%", GUILayout.Height(24))) { scroller.ScrollPercent = 0.25f; SceneView.RepaintAll(); }
        if (GUILayout.Button("50%", GUILayout.Height(24))) { scroller.ScrollPercent = 0.5f; SceneView.RepaintAll(); }
        if (GUILayout.Button("75%", GUILayout.Height(24))) { scroller.ScrollPercent = 0.75f; SceneView.RepaintAll(); }
        if (GUILayout.Button("🔻 Đáy (100%)", GUILayout.Height(24)))
        {
            Undo.RecordObject(scroller.Content, "Scroll Shop Bottom");
            scroller.ScrollPercent = 1f;
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 2. CÁC NÚT NHẢY NHANH TỚI MỤC (QUICK JUMP BUTTONS)
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("⚡ NHẢY NHANH TỚI KHU VỰC CẦN SỬA", sectionHeaderStyle);
        EditorGUILayout.LabelField("Bấm để tự động cuộn đến vị trí và chọn mục tương ứng:", EditorStyles.miniLabel);

        DrawJumpButton("👑 Gói VIP (Card_VIP_Package)", "Card_VIP_Package");
        DrawJumpButton("⭐ Gói Đặc Biệt / Tân Thủ (Special_Item_Carousel)", "Special_Item_Carousel");
        DrawJumpButton("🛒 Cửa Hàng Hàng Ngày (Daily_Shop_Row)", "Daily_Shop_Row");
        DrawJumpButton("📦 Rương Chipset & Drone (Box_Gacha_Section)", "Box_Gacha_Section");
        DrawJumpButton("🔄 Meta Shop Carousel (Meta_Shop_Carousel)", "Meta_Shop_Carousel");
        DrawJumpButton("🪙 Mua Data Chip / Vàng (Data_Chip_Row)", "Data_Chip_Row");
        DrawJumpButton("💎 Sự Kiện Mua Kim Cương (Gem_Event_Section)", "Gem_Event_Section");

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 3. TÙY CHỌN MẶT NẠ & LAYOUT
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("👁️ CHẾ ĐỘ HIỂN THỊ & CĂN CHỈNH", sectionHeaderStyle);

        // Nút Bật/Tắt Mặt Nạ (Mask)
        bool unmask = scroller.UnmaskInEditMode;
        GUI.backgroundColor = unmask ? new Color(1f, 0.7f, 0.3f) : Color.white;
        string maskBtnText = unmask
            ? "👁️ Đang TẮT Mask (Toàn bộ 6000px đang hiện trên Scene View)"
            : "🎭 Đang BẬT Mask (Chỉ hiện phần trong khung nhìn)";

        if (GUILayout.Button(maskBtnText, GUILayout.Height(30)))
        {
            Undo.RecordObject(scroller, "Toggle Mask");
            scroller.UnmaskInEditMode = !unmask;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        // Bật / Tắt VerticalLayoutGroup
        if (scroller.LayoutGroup != null)
        {
            bool lgEnabled = scroller.LayoutGroup.enabled;
            GUI.backgroundColor = lgEnabled ? Color.white : new Color(1f, 0.5f, 0.5f);
            string lgText = lgEnabled
                ? "📐 VerticalLayoutGroup: ĐANG BẬT (Tự sắp xếp theo thứ tự)"
                : "✋ VerticalLayoutGroup: ĐANG TẮT (Bạn có thể kéo vị trí tự do)";

            if (GUILayout.Button(lgText, GUILayout.Height(26)))
            {
                Undo.RecordObject(scroller.LayoutGroup, "Toggle VerticalLayoutGroup");
                scroller.LayoutGroup.enabled = !lgEnabled;
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 4. MỞ CỬA SỔ NỔI ĐỘC LẬP
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (GUILayout.Button("🚀 Mở Cửa Sổ Nổi Tiện Lợi (Shop Scroller Window)", GUILayout.Height(32)))
        {
            ShopScrollerWindow.OpenWindow();
        }
        EditorGUILayout.LabelField("Gợi ý: Cửa sổ nổi giúp bạn vừa bấm chọn các sprite con để sửa, vừa cuộn màn hình mà không bị mất giao diện điều khiển!", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // Draw default references foldout
        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawJumpButton(string label, string childName)
    {
        if (scroller.Content == null) return;

        Transform childT = scroller.Content.Find(childName);
        if (childT == null)
        {
            // Thử tìm theo tên Header
            string headerName = "Header_" + childName;
            childT = scroller.Content.Find(headerName);
        }

        if (childT != null)
        {
            if (GUILayout.Button(label, GUILayout.Height(24)))
            {
                Undo.RecordObject(scroller.Content, "Jump to Section");
                scroller.ScrollToSection(childT.GetComponent<RectTransform>());
                Selection.activeGameObject = childT.gameObject;
                EditorGUIUtility.PingObject(childT.gameObject);
                SceneView.RepaintAll();
            }
        }
    }

    private void OnSceneGUI()
    {
        if (scroller == null || Application.isPlaying) return;

        Event e = Event.current;
        if (e != null && e.isScrollWheel)
        {
            // Lăn chuột trong Scene View để cuộn Shop
            float delta = e.delta.y * 120f;
            scroller.ScrollByDelta(delta);
            e.Use();
            Repaint();
        }
    }
}
#endif
