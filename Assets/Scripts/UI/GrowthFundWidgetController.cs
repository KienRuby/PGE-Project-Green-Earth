using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý Widget Growth Fund ở góc trên bên phải màn hình Chapter.
/// </summary>
public class GrowthFundWidgetController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button fundButton;
    [SerializeField] private TMP_Text percentageText;

    [Header("Bottom Navigation Quick Link")]
    [SerializeField] private BottomNavigationController bottomNavController;

    private void Awake()
    {
        if (bottomNavController == null)
        {
            bottomNavController = FindObjectOfType<BottomNavigationController>();
        }

        if (fundButton != null)
        {
            fundButton.onClick.AddListener(OnFundClicked);
        }
    }

    public void SetPercentage(string text)
    {
        if (percentageText != null)
        {
            percentageText.text = text;
        }
    }

    private void OnFundClicked()
    {
        Debug.Log("[GrowthFund] Đã mở gói Growth Fund.");
        if (bottomNavController != null)
        {
            bottomNavController.Select(0); // Mở Shop tab
        }
    }
}
