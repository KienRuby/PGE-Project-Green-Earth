using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn vào nút Info trên các ô mở hộp Chipset/Drone để kích hoạt mở Modal hiển thị tỷ lệ.
/// </summary>
[RequireComponent(typeof(Button))]
public class BoxDropRateInfoTrigger : MonoBehaviour
{
    public enum BoxCategory
    {
        Chipset,
        Drone
    }

    [Tooltip("Loại hộp để hiển thị tỷ lệ tương ứng.")]
    public BoxCategory category = BoxCategory.Chipset;

    private void Awake()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(TriggerModal);
        }
    }

    public void TriggerModal()
    {
        if (BoxDropRateModalController.Instance != null)
        {
            if (category == BoxCategory.Chipset)
            {
                BoxDropRateModalController.Instance.ShowChipsetRates();
            }
            else
            {
                BoxDropRateModalController.Instance.ShowDroneRates();
            }
        }
        else
        {
            Debug.LogWarning("[BoxDropRateInfoTrigger] Không tìm thấy BoxDropRateModalController trong Scene!");
        }
    }
}
