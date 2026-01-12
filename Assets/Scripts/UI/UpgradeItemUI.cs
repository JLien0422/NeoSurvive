using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeItemUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI costText;
    public Button buyButton;

    private UpgradeDef _def;
    private UpgradeManager _manager;

    public void Setup(UpgradeDef def, int currentLevel, int cost, UpgradeManager manager)
    {
        _def = def;
        _manager = manager;

        nameText.text = def.displayName;
        levelText.text = $"Lv.{currentLevel}";
        
        if (def.maxLevel > 0 && currentLevel >= def.maxLevel)
        {
            costText.text = "MAX";
            buyButton.interactable = false;
        }
        else
        {
            costText.text = $"{cost:N0} G";
            buyButton.interactable = true;
        }

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    private void OnBuyClicked()
    {
        _manager.TryBuyUpgrade(_def);
    }
}
