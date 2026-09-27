using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DebugEnhanceText
{
    static DebugEnhanceText()
    {
        EditorApplication.delayCall += Inspect;
    }

    [MenuItem("PGE/Debug/Inspect Enhance Hierarchy")]
    public static void Inspect()
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var r in roots)
        {
            var modal = r.transform.Find("Canvas/BuddyDetailModal") ?? FindDeep(r.transform, "BuddyDetailModal");
            if (modal != null)
            {
                Debug.Log($"=== FOUND BuddyDetailModal at {GetPath(modal)} ===");
                var enhBtn = FindDeep(modal, "EnhanceBtn");
                if (enhBtn != null)
                {
                    DumpTransform(enhBtn, 0);
                }
                else
                {
                    Debug.Log("EnhanceBtn not found under BuddyDetailModal");
                }
            }
        }
    }

    private static void DumpTransform(Transform t, int indent)
    {
        string prefix = new string(' ', indent * 2);
        var rt = t.GetComponent<RectTransform>();
        string rtInfo = rt != null ? $"pos={rt.anchoredPosition}, size={rt.rect.size}, localPos={rt.localPosition}, scale={rt.localScale}, z={rt.position.z}" : "";
        var comps = string.Join(", ", t.GetComponents<Component>().Select(c => c != null ? c.GetType().Name : "null"));
        
        var img = t.GetComponent<Image>();
        string imgInfo = img != null ? $" [Image: sprite={(img.sprite != null ? img.sprite.name : "null")}, color={img.color}, enabled={img.enabled}]" : "";

        var tmp = t.GetComponent<TMP_Text>();
        string tmpInfo = tmp != null ? $" [TMP: text='{tmp.text}', fontSize={tmp.fontSize}, color={tmp.color}, font={(tmp.font != null ? tmp.font.name : "null")}, mat={(tmp.fontSharedMaterial != null ? tmp.fontSharedMaterial.name : "null")}, activeInHierarchy={t.gameObject.activeInHierarchy}, enabled={tmp.enabled}]" : "";

        Debug.Log($"{prefix}- {t.name} (active={t.gameObject.activeSelf}) {rtInfo} [{comps}]{imgInfo}{tmpInfo}");

        for (int i = 0; i < t.childCount; i++)
        {
            DumpTransform(t.GetChild(i), indent + 1);
        }
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var res = FindDeep(root.GetChild(i), name);
            if (res != null) return res;
        }
        return null;
    }

    private static string GetPath(Transform current)
    {
        if (current.parent == null) return current.name;
        return GetPath(current.parent) + "/" + current.name;
    }
}
