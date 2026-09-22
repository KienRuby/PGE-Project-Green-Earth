#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class OneShot_ApplyPhysics2DLayerOptimization
{
    private const string TaskKey = "OneShot_ApplyPhysics2DLayerOptimization_Executed";

    static OneShot_ApplyPhysics2DLayerOptimization()
    {
        EditorApplication.delayCall += ExecuteOnceAndSelfDestruct;
    }

    [MenuItem("PGE/Performance/Apply Step 1 - Ignore Enemy-Enemy Physics Collision")]
    public static void ManualExecute()
    {
        SessionState.EraseBool(TaskKey);
        ExecuteOnceAndSelfDestruct();
    }

    private static void ExecuteOnceAndSelfDestruct()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        if (SessionState.GetBool(TaskKey, false))
        {
            return;
        }
        SessionState.SetBool(TaskKey, true);

        try
        {
            Debug.Log("[OneShotTask] >>> Executing Step 1: Disable Enemy-Enemy Layer Collision in Physics2D Settings...");

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
            {
                bool wasIgnored = Physics2D.GetIgnoreLayerCollision(enemyLayer, enemyLayer);
                Debug.Log($"[OneShotTask] Previous Enemy <-> Enemy ignore state: {wasIgnored}");

                Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);

                bool isIgnoredNow = Physics2D.GetIgnoreLayerCollision(enemyLayer, enemyLayer);
                Debug.Log($"[OneShotTask] Updated Enemy <-> Enemy ignore state: {isIgnoredNow}");

                AssetDatabase.SaveAssets();
                Debug.Log("[OneShotTask] ✅ Physics2D settings updated & saved successfully!");
            }
            else
            {
                Debug.LogError("[OneShotTask] ❌ Layer 'Enemy' not found!");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OneShotTask] ❌ Error executing task: {ex}");
        }
        finally
        {
            SelfDelete();
        }
    }

    private static void SelfDelete()
    {
        EditorApplication.delayCall += () =>
        {
            try
            {
                string[] guids = AssetDatabase.FindAssets("t:MonoScript OneShot_ApplyPhysics2DLayerOptimization");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(path) && path.EndsWith(".cs"))
                    {
                        AssetDatabase.DeleteAsset(path);
                        Debug.Log($"[OneShotTask] 🧹 Cleaned up one-shot script: {path}");
                    }
                }
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OneShotTask] Failed to self-delete: {ex.Message}");
            }
        };
    }
}
#endif
