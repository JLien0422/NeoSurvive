using UnityEngine;
using UnityEngine.UI; // Button을 사용하기 위해 추가
using TMPro; // TextMeshPro를 사용하기 위해 추가

public class MainMenu_UI : MonoBehaviour
{
    [Header("공통 UI")]
    public TextMeshProUGUI goldText;

    [Header("체력 업그레이드 UI")]
    public TextMeshProUGUI healthLevelText;
    public TextMeshProUGUI healthCostText;
    public Button upgradeHealthButton;

    [Header("공격력 업그레이드 UI")]
    public TextMeshProUGUI damageLevelText;
    public TextMeshProUGUI damageCostText;
    public Button upgradeDamageButton;

    [Header("이동속도 업그레이드 UI")]
    public TextMeshProUGUI moveSpeedLevelText;
    public TextMeshProUGUI moveSpeedCostText;
    public Button upgradeMoveSpeedButton;

    void Start()
    {
        // UI가 처음 켜질 때, 모든 UI 정보를 업데이트합니다.
        UpdateAllUI();
    }

    void Update()
    {
        // UI 정보를 매 프레임 업데이트하여 실시간으로 반영합니다.
        UpdateAllUI();
    }

    // 모든 UI를 새로고침하는 메서드
    public void UpdateAllUI()
    {
        if (UpgradeManager.Instance == null || GameManager.Instance == null)
        {
            // 매니저가 아직 준비되지 않았을 수 있으므로 오류 대신 리턴 처리
            return;
        }

        // 총 골드 표시
        goldText.text = "GOLD: " + GameManager.Instance.TotalGold;

        // 체력 UI 업데이트
        healthLevelText.text = "Lv. " + UpgradeManager.Instance.GetHealthUpgradeLevel();
        healthCostText.text = UpgradeManager.Instance.GetHealthUpgradeCost() + " G";
        upgradeHealthButton.interactable = (GameManager.Instance.TotalGold >= UpgradeManager.Instance.GetHealthUpgradeCost());

        // 공격력 UI 업데이트
        damageLevelText.text = "Lv. " + UpgradeManager.Instance.GetDamageUpgradeLevel();
        damageCostText.text = UpgradeManager.Instance.GetDamageUpgradeCost() + " G";
        upgradeDamageButton.interactable = (GameManager.Instance.TotalGold >= UpgradeManager.Instance.GetDamageUpgradeCost());

        // 이동속도 UI 업데이트
        moveSpeedLevelText.text = "Lv. " + UpgradeManager.Instance.GetMoveSpeedUpgradeLevel();
        moveSpeedCostText.text = UpgradeManager.Instance.GetMoveSpeedUpgradeCost() + " G";
        upgradeMoveSpeedButton.interactable = (GameManager.Instance.TotalGold >= UpgradeManager.Instance.GetMoveSpeedUpgradeCost());
    }

    // '체력 강화' 버튼을 눌렀을 때 실행될 메서드
    public void OnClick_UpgradeHealth()
    {
        UpgradeManager.Instance.BuyHealthUpgrade();
    }

    // '공격력 강화' 버튼을 눌렀을 때 실행될 메서드
    public void OnClick_UpgradeDamage()
    {
        UpgradeManager.Instance.BuyDamageUpgrade();
    }

    // '이동속도 강화' 버튼을 눌렀을 때 실행될 메서드
    public void OnClick_UpgradeMoveSpeed()
    {
        UpgradeManager.Instance.BuyMoveSpeedUpgrade();
    }
}
