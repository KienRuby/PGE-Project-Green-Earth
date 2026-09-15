#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public class ShopScrollerWindow : EditorWindow
{
    static ShopScrollerWindow()
    {
        EditorApplication.delayCall += EnsureScrollerAttachedToScene;
    }

    public static void EnsureScrollerAttachedToScene()
    {
        if (Application.isPlaying) return;
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainMenu.unity") return;

        GameObject shopPanel = GameObject.Find("ShopPanel") ?? GameObject.Find("ShopPanel (Scrollable)");
        if (shopPanel == null)
        {
            foreach (var r in scene.GetRootGameObjects())
            {
                var match = r.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == "ShopPanel" || t.name == "ShopPanel (Scrollable)");
                if (match != null) { shopPanel = match.gameObject; break; }
            }
        }

        if (shopPanel != null)
        {
            var scroller = shopPanel.GetComponent<ShopEditModeScroller>();
            if (scroller == null)
            {
                scroller = shopPanel.AddComponent<ShopEditModeScroller>();
                scroller.AutoFindReferences();
                EditorUtility.SetDirty(shopPanel);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[ShopScrollerWindow] Đã tự động gắn ShopEditModeScroller vào ShopPanel trong MainMenu scene!");
            }
        }
    }

    private ShopEditModeScroller scroller;
    private Vector2 windowScrollPos;

    [MenuItem("Tools/PGE/Shop Edit-Mode Scroller Window", false, 20)]
    [MenuItem("Window/PGE/Shop Scroller", false, 20)]
    public static void OpenWindow()
    {
        EnsureScrollerAttachedToScene();
        ShopScrollerWindow window = GetWindow<ShopScrollerWindow>("Shop Scroller");
        window.minSize = new Vector2(320, 520);
        window.Show();
    }

    private void OnEnable()
    {
        FindScrollerInScene();
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void FindScrollerInScene()
    {
        scroller = Object.FindObjectOfType<ShopEditModeScroller>();
        if (scroller == null)
        {
            // Tìm ShopPanel
            GameObject shopPanel = GameObject.Find("ShopPanel") ?? GameObject.Find("ShopPanel (Scrollable)");
            if (shopPanel == null)
            {
                var allRoots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
                foreach (var r in allRoots)
                {
                    var match = r.GetComponentsInChildren<Transform>(true)
                        .FirstOrDefault(t => t.name == "ShopPanel" || t.name == "ShopPanel (Scrollable)");
                    if (match != null)
                    {
                        shopPanel = match.gameObject;
                        break;
                    }
                }
            }

            if (shopPanel != null)
            {
                scroller = shopPanel.GetComponent<ShopEditModeScroller>();
                if (scroller == null)
                {
                    scroller = shopPanel.AddComponent<ShopEditModeScroller>();
                    EditorUtility.SetDirty(shopPanel);
                }
            }
        }

        if (scroller != null)
        {
            scroller.AutoFindReferences();
        }
    }

    private void OnGUI()
    {
        if (scroller == null || scroller.gameObject == null)
        {
            FindScrollerInScene();
        }

        windowScrollPos = EditorGUILayout.BeginScrollView(windowScrollPos);

        // Header
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter
        };

        GUIStyle sectionTitle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = new Color(0.2f, 0.85f, 1f) }
        };

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🎮 BỘ CÔNG CỤ CUỘN & EDIT SHOP", titleStyle);
        EditorGUILayout.LabelField("Chỉnh sửa kích thước, vị trí và sprite trong Edit Mode không cần Play Mode.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndVertical();

        if (scroller == null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("Không tìm thấy ShopPanel trong Scene hiện tại. Hãy mở Scene 'MainMenu'!", MessageType.Warning);
            if (GUILayout.Button("🔄 Tìm lại trong Scene", GUILayout.Height(30)))
            {
                FindScrollerInScene();
            }
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.Space(6);

        // 1. THANH TRƯỢT VỊ TRÍ CUỘN
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("📜 CUỘN SHOP TRONG SCENE / GAME VIEW", sectionTitle);

        float currentPercent = scroller.ScrollPercent;
        EditorGUI.BeginChangeCheck();
        float newPercent = EditorGUILayout.Slider("Vị trí cuộn", currentPercent, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(scroller.Content, "Scroll Shop Slider");
            scroller.ScrollPercent = newPercent;
            SceneView.RepaintAll();
        }

        if (scroller.Content != null)
        {
            EditorGUILayout.LabelField($"Vị trí Y hiện tại: {scroller.Content.anchoredPosition.y:F0}px / Tổng chiều cao: {scroller.Content.rect.height:F0}px", EditorStyles.miniLabel);
        }

        // Stepper buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("▲ -300px", GUILayout.Height(24)))
        {
            scroller.ScrollByDelta(-300f);
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("▼ +300px", GUILayout.Height(24)))
        {
            scroller.ScrollByDelta(300f);
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("🔝 Đỉnh (Top)", GUILayout.Height(24)))
        {
            scroller.ResetToTop();
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("🔻 Đáy (Bottom)", GUILayout.Height(24)))
        {
            scroller.ScrollPercent = 1f;
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 2. NHẢY NHANH TỚI KHU VỰC
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("⚡ NHẢY NHANH ĐẾN MỤC ĐỂ CHỈNH SỬA", sectionTitle);
        EditorGUILayout.LabelField("Bấm nút để cuộn tới mục đó và chọn GameObject tương ứng:", EditorStyles.miniLabel);

        DrawSectionButton("👑 Gói VIP Đặc Quyền", "Card_VIP_Package");
        DrawSectionButton("⭐ Gói Đặc Biệt (Welcome / Intermediate / Adv)", "Special_Item_Carousel");
        DrawSectionButton("🛒 Cửa Hàng Hàng Ngày (Daily Shop)", "Daily_Shop_Row");
        DrawSectionButton("📦 Rương Chipset & Drone (Box Gacha)", "Box_Gacha_Section");
        DrawSectionButton("🔄 Meta Shop Carousel (Súng / Drone)", "Meta_Shop_Carousel");
        DrawSectionButton("🪙 Mua Data Chip (Vàng)", "Data_Chip_Row");
        DrawSectionButton("💎 Sự Kiện Kim Cương Đỏ", "Gem_Event_Section");

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 3. CHUYỂN ĐỔI THẺ CAROUSEL (GÓI TÂN THỦ & META SHOP)
        if (scroller.Content != null)
        {
            var carousels = scroller.Content.GetComponentsInChildren<ShopCarouselUI>(true);
            if (carousels.Length > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("📑 CHUYỂN ĐỔI CÁC THẺ TRONG CAROUSEL", sectionTitle);
                EditorGUILayout.LabelField("Bấm để đổi thẻ đang hiển thị và chọn thẻ đó để chỉnh sửa:", EditorStyles.miniLabel);

                foreach (var c in carousels)
                {
                    string cTitle = c.name == "Special_Item_Carousel" ? "⭐ Gói Đặc Biệt (3 Thẻ Tân Thủ)" : "🔄 Meta Shop (2 Thẻ Súng/Drone)";
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField(cTitle, EditorStyles.boldLabel);

                    var pages = c.Pages;
                    if (pages != null && pages.Length > 0)
                    {
                        EditorGUILayout.BeginHorizontal();
                        for (int p = 0; p < pages.Length; p++)
                        {
                            if (pages[p] == null) continue;
                            string pName = pages[p].name.Replace("Card_", "").Replace("_", " ");
                            bool active = (p == c.CurrentPage && pages[p].gameObject.activeSelf);

                            GUI.backgroundColor = active ? new Color(0.2f, 0.85f, 0.4f) : Color.white;
                            string label = active ? $"▶ {pName}" : pName;
                            if (GUILayout.Button(label, GUILayout.Height(28)))
                            {
                                Undo.RecordObject(c, "Switch Carousel Card");
                                c.SetPageInEditor(p);
                                scroller.ScrollToSection(c.GetComponent<RectTransform>());
                                Selection.activeGameObject = pages[p].gameObject;
                                EditorGUIUtility.PingObject(pages[p].gameObject);
                                SceneView.RepaintAll();
                            }
                        }
                        GUI.backgroundColor = Color.white;
                        EditorGUILayout.EndHorizontal();

                        if (GUILayout.Button($"📋 Đồng bộ kích thước thẻ đang chọn sang các thẻ còn lại", GUILayout.Height(24)))
                        {
                            Undo.RecordObjects(pages, "Sync Sizes");
                            c.SyncSizeToAllPages(c.CurrentPage);
                            Debug.Log($"[ShopScrollerWindow] Đã đồng bộ kích thước các thẻ trong {c.name}!");
                            SceneView.RepaintAll();
                        }
                    }
                    EditorGUILayout.Space(4);
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(6);
            }
        }

        // 4. TÙY CHỌN MẶT NẠ MASK & TỰ DO DI CHUYỂN
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("👁️ CHẾ ĐỘ HIỂN THỊ & CĂN CHỈNH TỰ DO", sectionTitle);

        bool unmask = scroller.UnmaskInEditMode;
        GUI.backgroundColor = unmask ? new Color(1f, 0.7f, 0.2f) : Color.white;
        string maskText = unmask
            ? "👁️ Đang TẮT Mask (Hiện toàn bộ 6000px trong Scene View)"
            : "🎭 Đang BẬT Mask (Chỉ hiện phần trong khung)";
        if (GUILayout.Button(maskText, GUILayout.Height(30)))
        {
            Undo.RecordObject(scroller, "Toggle Mask");
            scroller.UnmaskInEditMode = !unmask;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        if (scroller.LayoutGroup != null)
        {
            bool lgActive = scroller.LayoutGroup.enabled;
            GUI.backgroundColor = lgActive ? Color.white : new Color(1f, 0.5f, 0.5f);
            string lgText = lgActive
                ? "📐 VerticalLayoutGroup: ĐANG BẬT"
                : "✋ VerticalLayoutGroup: ĐANG TẮT (Kéo vị trí tự do)";
            if (GUILayout.Button(lgText, GUILayout.Height(26)))
            {
                Undo.RecordObject(scroller.LayoutGroup, "Toggle VerticalLayoutGroup");
                scroller.LayoutGroup.enabled = !lgActive;
            }
            GUI.backgroundColor = Color.white;

            if (lgActive)
            {
                EditorGUILayout.HelpBox("LƯU Ý: Khi VerticalLayoutGroup đang BẬT, kích thước (Height/Width) của mỗi mục con được điều khiển bởi component LayoutElement của mục đó. Nếu muốn di chuyển vị trí tự do theo ý thích, bạn có thể bấm nút TẮT VerticalLayoutGroup ở trên.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("VerticalLayoutGroup đang TẮT. Bạn có thể tự do kéo RectTransform của bất kỳ mục nào trong Scene View mà không bị giật lại.", MessageType.Warning);
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 4. RESET AN TOÀN TRƯỚC KHI CHẠY GAME
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (GUILayout.Button("🔄 Đưa Shop về Đỉnh (Chuẩn bị Play Game)", GUILayout.Height(28)))
        {
            scroller.ResetToTop();
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();
    }

    private void DrawSectionButton(string label, string childName)
    {
        if (scroller == null || scroller.Content == null) return;

        Transform child = scroller.Content.Find(childName);
        if (child == null) child = scroller.Content.Find("Header_" + childName);

        if (child != null)
        {
            if (GUILayout.Button(label, GUILayout.Height(25)))
            {
                Undo.RecordObject(scroller.Content, "Jump to " + childName);
                scroller.ScrollToSection(child.GetComponent<RectTransform>());
                Selection.activeGameObject = child.gameObject;
                EditorGUIUtility.PingObject(child.gameObject);
                SceneView.RepaintAll();
            }
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (scroller == null || Application.isPlaying) return;

        Event e = Event.current;
        if (e != null && e.isScrollWheel)
        {
            // Cho phép lăn chuột trong Scene View để cuộn Shop khi cửa sổ này đang mở
            float delta = e.delta.y * 120f;
            scroller.ScrollByDelta(delta);
            e.Use();
            Repaint();
            sceneView.Repaint();
        }
    }
}
#endif
