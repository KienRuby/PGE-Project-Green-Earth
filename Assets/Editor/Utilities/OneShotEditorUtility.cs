#if UNITY_EDITOR
using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bộ công cụ tiêu chuẩn cho các tác vụ Editor chạy 1 lần (One-Shot / Ephemeral).
/// Đảm bảo:
/// 1. Tác vụ chỉ chạy DUY NHẤT 1 LẦN.
/// 2. Tự động xóa sạch file script và .meta sau khi chạy xong.
/// 3. Cô lập GameObject tạm (HideFlags.HideAndDontSave) để không bao giờ làm bẩn Scene.
/// </summary>
public static class OneShotEditorUtility
{
    private const string SessionPrefix = "PGE_OneShot_";

    /// <summary>
    /// Kiểm tra xem một tác vụ đã chạy trong phiên làm việc hiện tại hay chưa.
    /// </summary>
    public static bool HasExecuted(string taskKey)
    {
        return SessionState.GetBool(SessionPrefix + taskKey, false);
    }

    /// <summary>
    /// Đánh dấu tác vụ đã hoàn thành.
    /// </summary>
    public static void MarkExecuted(string taskKey)
    {
        SessionState.SetBool(SessionPrefix + taskKey, true);
    }

    /// <summary>
    /// Tạo một GameObject cô lập an toàn, tuyệt đối không bao giờ bị lưu vào Scene.
    /// </summary>
    public static GameObject CreateIsolatedGameObject(string name = "IsolatedTempObject")
    {
        var go = new GameObject(name);
        go.hideFlags = HideFlags.HideAndDontSave;
        return go;
    }

    /// <summary>
    /// Thực thi một hành động đúng duy nhất 1 lần trong phiên làm việc.
    /// </summary>
    public static bool ExecuteOnce(string taskKey, Action task)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return false;
        }

        if (HasExecuted(taskKey))
        {
            return false;
        }

        MarkExecuted(taskKey);

        try
        {
            task?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OneShotEditorUtility] Lỗi khi chạy tác vụ '{taskKey}': {ex}");
            return false;
        }
    }

    /// <summary>
    /// Thực thi hành động đúng 1 lần và tự động XÓA FILE SCRIPT ngay sau khi hoàn thành.
    /// </summary>
    /// <param name="className">Tên class của script cần tự hủy</param>
    /// <param name="task">Hành động cần thực thi</param>
    public static void ExecuteOnceAndSelfDelete(string className, Action task)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        if (HasExecuted(className))
        {
            return;
        }
        MarkExecuted(className);

        EditorApplication.delayCall += () =>
        {
            try
            {
                task?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OneShotEditorUtility] Lỗi khi chạy '{className}': {ex}");
            }
            finally
            {
                SelfDeleteScript(className);
            }
        };
    }

    /// <summary>
    /// Tìm và xóa file script .cs cùng file .meta tương ứng dựa theo tên class.
    /// </summary>
    public static void SelfDeleteScript(string className)
    {
        EditorApplication.delayCall += () =>
        {
            try
            {
                string[] guids = AssetDatabase.FindAssets($"t:MonoScript {className}");
                int deleted = 0;

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(path) && Path.GetFileNameWithoutExtension(path) == className && path.EndsWith(".cs"))
                    {
                        if (AssetDatabase.DeleteAsset(path))
                        {
                            deleted++;
                            Debug.Log($"[OneShotEditorUtility] 🧹 Đã tự hủy và dọn sạch file script: {path}");
                        }
                    }
                }

                if (deleted > 0)
                {
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OneShotEditorUtility] Cảnh báo khi tự xóa script '{className}': {ex.Message}");
            }
        };
    }
}
#endif
