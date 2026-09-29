using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class ShopEditModeScrollInteraction
{
    private static readonly Vector3[] ViewportCorners = new Vector3[4];
    private static ShopEditModeScroller dragScroller;
    private static int dragControl;
    private static float previousLocalY;
    private static ShopEditModeScroller clickScroller;
    private static Vector2 clickPosition;

    static ShopEditModeScrollInteraction()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Event current = Event.current;
        if (current == null) return;

        int control = GUIUtility.GetControlID(FocusType.Passive);
        ShopEditModeScroller hovered = FindHoveredScroller(current.mousePosition);

        if (current.rawType == EventType.MouseDown && current.button == 0 &&
            current.modifiers == EventModifiers.None)
        {
            clickScroller = hovered;
            clickPosition = current.mousePosition;
        }
        else if (current.rawType == EventType.MouseDrag && current.button == 0 &&
                 Vector2.Distance(clickPosition, current.mousePosition) > 3f)
        {
            clickScroller = null;
        }
        else if (current.rawType == EventType.MouseUp && current.button == 0 && clickScroller != null)
        {
            Graphic picked = FindGraphicAt(clickScroller, current.mousePosition);
            clickScroller = null;
            if (picked != null)
            {
                EditorApplication.delayCall += () =>
                {
                    if (picked != null && !EditorApplication.isPlayingOrWillChangePlaymode)
                        Selection.activeGameObject = picked.gameObject;
                };
            }
        }

        bool scrollDragModifier = current.modifiers == EventModifiers.Shift;
        if (current.type == EventType.Layout && hovered != null && scrollDragModifier)
            HandleUtility.AddDefaultControl(control);

        if (current.type == EventType.ScrollWheel && hovered != null)
        {
            Undo.RecordObjects(new Object[] { hovered, hovered.Content }, "Scroll Shop");
            hovered.ScrollByDelta(current.delta.y * hovered.ScrollRectComponent.scrollSensitivity);
            current.Use();
            Repaint(sceneView);
        }
        else if (current.type == EventType.MouseDown && current.button == 0 &&
                 scrollDragModifier && hovered != null &&
                 HandleUtility.nearestControl == control &&
                 TryGetLocalY(hovered.Viewport, current.mousePosition, out previousLocalY))
        {
            dragScroller = hovered;
            dragControl = control;
            GUIUtility.hotControl = control;
            Undo.RecordObjects(new Object[] { hovered, hovered.Content }, "Drag Shop");
            current.Use();
        }
        else if (current.type == EventType.MouseDrag && current.button == 0 &&
                 dragScroller != null && GUIUtility.hotControl == dragControl)
        {
            if (TryGetLocalY(dragScroller.Viewport, current.mousePosition, out float localY))
            {
                float delta = localY - previousLocalY;
                if (Mathf.Abs(delta) > 0.01f)
                {
                    dragScroller.ScrollByDelta(delta);
                    previousLocalY = localY;
                    Repaint(sceneView);
                }
            }
            current.Use();
        }
        else if (current.type == EventType.MouseUp && current.button == 0 &&
                 dragScroller != null && GUIUtility.hotControl == dragControl)
        {
            GUIUtility.hotControl = 0;
            dragScroller = null;
            current.Use();
        }
    }

    private static ShopEditModeScroller FindHoveredScroller(Vector2 mousePosition)
    {
        foreach (ShopEditModeScroller scroller in Object.FindObjectsOfType<ShopEditModeScroller>(true))
        {
            if (!scroller.isActiveAndEnabled || scroller.Viewport == null ||
                scroller.Content == null || scroller.ScrollRectComponent == null) continue;

            RectTransform pickArea = scroller.UnmaskInEditMode ? scroller.Content : scroller.Viewport;
            pickArea.GetWorldCorners(ViewportCorners);
            Vector2 first = HandleUtility.WorldToGUIPoint(ViewportCorners[0]);
            float minX = first.x, maxX = first.x, minY = first.y, maxY = first.y;
            for (int i = 1; i < ViewportCorners.Length; i++)
            {
                Vector2 point = HandleUtility.WorldToGUIPoint(ViewportCorners[i]);
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxY = Mathf.Max(maxY, point.y);
            }

            if (new Rect(minX, minY, maxX - minX, maxY - minY).Contains(mousePosition))
                return scroller;
        }
        return null;
    }

    private static bool TryGetLocalY(RectTransform viewport, Vector2 mousePosition, out float localY)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        Plane plane = new Plane(viewport.forward, viewport.position);
        if (plane.Raycast(ray, out float distance))
        {
            localY = viewport.InverseTransformPoint(ray.GetPoint(distance)).y;
            return true;
        }
        localY = 0f;
        return false;
    }

    private static Graphic FindGraphicAt(ShopEditModeScroller scroller, Vector2 mousePosition)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        Graphic best = null;
        int bestDepth = int.MinValue;
        float bestArea = float.MaxValue;

        foreach (Graphic graphic in scroller.Content.GetComponentsInChildren<Graphic>(false))
        {
            if (!graphic.isActiveAndEnabled || graphic.color.a <= 0f ||
                SceneVisibilityManager.instance.IsPickingDisabled(graphic.gameObject, false)) continue;

            RectTransform rect = graphic.rectTransform;
            Plane plane = new Plane(rect.forward, rect.position);
            if (!plane.Raycast(ray, out float distance)) continue;

            Vector3 localPoint = rect.InverseTransformPoint(ray.GetPoint(distance));
            if (!rect.rect.Contains(new Vector2(localPoint.x, localPoint.y))) continue;

            float area = rect.rect.width * rect.rect.height;
            if (graphic.depth > bestDepth || (graphic.depth == bestDepth && area < bestArea))
            {
                best = graphic;
                bestDepth = graphic.depth;
                bestArea = area;
            }
        }
        return best;
    }

    private static void Repaint(SceneView sceneView)
    {
        sceneView.Repaint();
        EditorApplication.QueuePlayerLoopUpdate();
    }
}
