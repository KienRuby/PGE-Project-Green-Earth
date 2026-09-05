using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tự động quét và gắn UIDissolveController lên toàn bộ các Popup, Modal, Window, Dialog, Panel trong game:
/// - Chạy tự động sau khi Scene load (Runtime).
/// - Tự động tìm và liên kết các nút Close (X, DimBackground, Cancel) để kích hoạt controller.Hide().
/// - Đảm bảo bất kỳ UI nào mới xuất hiện cũng được áp dụng hiệu ứng Dissolve mà không cần gắn tay.
/// </summary>
public static class UIDissolveAutoInstaller
{
    private static readonly string[] TargetNameKeywords = new string[]
    {
        "RewardPopup",
        "SettingsPanel",
        "SettingsModal",
        "PauseModal",
        "BuddyDetailModal",
        "ChipsetDetailModal",
        "BlastFurnaceModal",
        "StagePreviewWindow",
        "PityGuaranteePanel",
        "ChipsetLevelUpPopup",
        "DamageDetailsModal",
        "QuitConfirmDialog",
        "GameOverPanel",
        "VictoryPanel",
        "RevivePanel",
        "LanguageOptionsPanel",
        "ModalBox"
    };

    private static readonly string[] SuffixKeywords = new string[]
    {
        "Popup",
        "Modal",
        "Dialog"
    };

    private static readonly string[] CloseButtonNames = new string[]
    {
        "close",
        "closebtn",
        "closebutton",
        "btn_close",
        "btnclose",
        "btn_x",
        "btnx",
        "dimbackground",
        "dimbg",
        "dim_bg",
        "dim",
        "dismiss",
        "cancel",
        "btn_cancel"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitOnLoad()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        InstallInActiveScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallInActiveScene();
    }

    public static int InstallInActiveScene()
    {
        int count = 0;
        Canvas[] canvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
        if (canvases == null || canvases.Length == 0) return 0;

        foreach (Canvas canvas in canvases)
        {
            RectTransform[] rects = canvas.GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform rt in rects)
            {
                if (rt == null || rt == canvas.transform) continue;

                if (IsEligiblePopup(rt.gameObject.name))
                {
                    if (AttachAndConfigure(rt.gameObject))
                    {
                        count++;
                    }
                }
            }
        }

        if (count > 0)
        {
            Debug.Log($"[UIDissolveAutoInstaller] Đã tự động gắn và cấu hình UIDissolveController cho {count} Popup/Modal trong scene: {SceneManager.GetActiveScene().name}");
        }

        return count;
    }

    public static bool IsEligiblePopup(string objName)
    {
        if (string.IsNullOrEmpty(objName)) return false;

        string cleanName = objName.Trim();

        // 1. Khớp chính xác hoặc chứa từ khóa mục tiêu
        for (int i = 0; i < TargetNameKeywords.Length; i++)
        {
            if (cleanName.IndexOf(TargetNameKeywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        // 2. Kết thúc bằng hậu tố Modal / Popup / Dialog
        for (int i = 0; i < SuffixKeywords.Length; i++)
        {
            if (cleanName.EndsWith(SuffixKeywords[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool AttachAndConfigure(GameObject targetGo)
    {
        if (targetGo == null) return false;

        UIDissolveController controller = targetGo.GetComponent<UIDissolveController>();
        bool wasAdded = false;

        if (controller == null)
        {
            controller = targetGo.AddComponent<UIDissolveController>();
            wasAdded = true;
        }

        controller.InitializeIfNeeded();

        // Tự động tìm và nối các nút Close / Dim Background
        AutoWireCloseButtons(targetGo, controller);

        return wasAdded;
    }

    public static void AutoWireCloseButtons(GameObject rootGo, UIDissolveController controller)
    {
        if (rootGo == null || controller == null) return;

        Button[] buttons = rootGo.GetComponentsInChildren<Button>(true);
        if (buttons == null || buttons.Length == 0) return;

        foreach (Button btn in buttons)
        {
            if (btn == null) continue;

            string bName = btn.name.ToLowerInvariant().Replace(" ", "").Replace("_", "");
            bool isCloseBtn = false;

            for (int i = 0; i < CloseButtonNames.Length; i++)
            {
                string kw = CloseButtonNames[i].Replace("_", "");
                if (bName.Contains(kw))
                {
                    isCloseBtn = true;
                    break;
                }
            }

            if (isCloseBtn)
            {
                // Gỡ các listener cũ nếu đã gắn để tránh gọi đúp
                btn.onClick.RemoveListener(controller.Hide);
                btn.onClick.AddListener(controller.Hide);
            }
        }
    }
}
