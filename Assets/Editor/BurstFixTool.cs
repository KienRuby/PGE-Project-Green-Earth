#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PGE.EditorTools
{
    public static class BurstFixTool
    {
        [MenuItem("Tools/PGE/Clear Burst Cache & Recompile")]
        public static void ClearBurstCacheAndRecompile()
        {
            string burstCacheDir = Path.Combine(Directory.GetCurrentDirectory(), "Library", "BurstCache");
            try
            {
                if (Directory.Exists(burstCacheDir))
                {
                    Directory.Delete(burstCacheDir, true);
                    Debug.Log("<color=green>[PGE]</color> Đã xóa sạch thư mục Library/BurstCache!");
                }
                else
                {
                    Debug.Log("<color=yellow>[PGE]</color> Thư mục Library/BurstCache hiện không tồn tại hoặc đã được dọn.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PGE] Một số file Burst cache đang được process giữ: {ex.Message}");
            }

            AssetDatabase.Refresh();
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            Debug.Log("<color=green>[PGE]</color> Đã kích hoạt làm mới Domain Reload & Recompile.");
        }
    }
}
#endif
