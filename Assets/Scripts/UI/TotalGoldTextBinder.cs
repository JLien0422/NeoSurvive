using TMPro;
using UnityEngine;

public class TotalGoldTextBinder : MonoBehaviour
{
    private const string GOLD_SAVE_KEY = "TotalGold";

    [SerializeField] private TextMeshProUGUI totalGoldText;
    [SerializeField] private string prefix = "Gold: ";

    private void OnEnable()
    {
        GameManager.OnTotalGoldChanged += HandleTotalGoldChanged;
        RefreshNow();
    }

    private void OnDisable()
    {
        GameManager.OnTotalGoldChanged -= HandleTotalGoldChanged;
    }

    private void HandleTotalGoldChanged(int totalGold)
    {
        if (totalGoldText != null)
        {
            totalGoldText.text = $"{prefix}{totalGold}";
        }
    }

    public void RefreshNow()
    {
        if (GameManager.Instance != null)
        {
            HandleTotalGoldChanged(GameManager.Instance.TotalGold);
        }
        else
        {
            HandleTotalGoldChanged(ES3.Load<int>(GOLD_SAVE_KEY, 0));
        }
    }
}
