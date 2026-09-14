#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ShopCarouselUI))]
public class ShopCarouselUIEditor : Editor
{
    private ShopCarouselUI carousel;

    private void OnEnable()
    {
        carousel = (ShopCarouselUI)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (carousel == null) return;

        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter
        };

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("📑 ĐIỀU KHIỂN CHUYỂN ĐỔI THẺ (EDIT MODE)", headerStyle);
        EditorGUILayout.LabelField("Chuyển qua lại giữa các thẻ ngay trong Edit Mode để xem & chỉnh sửa:", EditorStyles.wordWrappedMiniLabel);

        var pages = carousel.Pages;
        if (pages != null && pages.Length > 0)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] == null) continue;
                string cardName = pages[i].name.Replace("Card_", "").Replace("_", " ");
                bool isCurrent = (i == carousel.CurrentPage && pages[i].gameObject.activeSelf);

                GUI.backgroundColor = isCurrent ? new Color(0.2f, 0.85f, 0.4f) : Color.white;
                string btnText = isCurrent ? $"▶ Thẻ {i + 1}: {cardName}" : $"Thẻ {i + 1}: {cardName}";

                if (GUILayout.Button(btnText, GUILayout.Height(30)))
                {
                    Undo.RecordObject(carousel, "Switch Carousel Page in Editor");
                    carousel.SetPageInEditor(i);
                    Selection.activeGameObject = pages[i].gameObject;
                    EditorGUIUtility.PingObject(pages[i].gameObject);
                    SceneView.RepaintAll();
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            // Nút đồng bộ kích thước
            if (GUILayout.Button("📋 Áp dụng Kích Thước thẻ hiện tại cho các thẻ còn lại", GUILayout.Height(26)))
            {
                Undo.RecordObjects(pages, "Sync Carousel Cards Size");
                carousel.SyncSizeToAllPages(carousel.CurrentPage);
                Debug.Log($"[ShopCarouselUI] Đã đồng bộ kích thước từ thẻ {carousel.CurrentPage + 1} sang tất cả các thẻ còn lại trong Carousel!");
                SceneView.RepaintAll();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Chưa có danh sách Pages được gán trong Carousel.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);

        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
