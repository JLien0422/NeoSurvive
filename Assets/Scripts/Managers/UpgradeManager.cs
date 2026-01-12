using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Rendering;

[System.Serializable]
public class UpgradeDef
{
  public string id;           // 고유 ID (예: "ATK")
  public string displayName;  // 표시 이름 (예: "공격력")
  public int baseCost;        // 기본 비용
  public int costPerLevel;    // 레벨당 증가 비용
  public float valuePerLevel; // 레벨당 증가하는 능력치 양
  public int maxLevel;        // 최대 레벨 (0이면 무제한)
}

public class UpgradeManager : MonoBehaviour
{
  [Header("UI References")]
  public GameObject upgradeWindow;      // 업그레이드 팝업창 (패널)
  public Transform contentRoot;         // Scroll View의 Content
  public GameObject upgradeItemPrefab;  // 업그레이드 항목 프리팹
  public TextMeshProUGUI goldText;      // 현재 골드 표시

  [Header("Upgrade Definitions")]
  public List<UpgradeDef> upgrades = new List<UpgradeDef>();

  private void Start()
  {
    // 초기화
    // if (upgradeWindow != null) upgradeWindow.SetActive(false); // 시작 시 자기 자신을 꺼버리는 문제 방지
    UpdateGoldUI(DataManager.Instance.Gold);
    DataManager.OnGoldChanged += UpdateGoldUI;
  }

  private void OnDestroy()
  {
    DataManager.OnGoldChanged -= UpdateGoldUI;
  }

  public void OpenUpgradeWindow()
  {
    if (upgradeWindow != null)
    {
      Debug.Log("이거임?");
      upgradeWindow.SetActive(true);
      RefreshUpgradeList();
    }
  }

  public void CloseUpgradeWindow()
  {
    if (upgradeWindow != null) upgradeWindow.SetActive(false);
  }

  private void UpdateGoldUI(int gold)
  {
    if (goldText != null)
      goldText.text = $"{gold:N0} G";
  }

  private void RefreshUpgradeList()
  {
    // 기존 아이템 삭제
    foreach (Transform child in contentRoot)
    {
      Destroy(child.gameObject);
    }

    // 목록 생성
    foreach (var def in upgrades)
    {
      CreateUpgradeItem(def);
    }
  }

  private void CreateUpgradeItem(UpgradeDef def)
  {
    if (upgradeItemPrefab == null) return;

    GameObject itemObj = Instantiate(upgradeItemPrefab, contentRoot);
    itemObj.SetActive(true); // 프리팹이 비활성화 상태일 수 있으므로 강제로 켬
    // 여기서 프리팹의 컴포넌트를 가져와서 데이터 세팅
    // (UpgradeItemUI 컴포넌트가 필요함)
    UpgradeItemUI itemUI = itemObj.GetComponent<UpgradeItemUI>();
    if (itemUI != null)
    {
      int currentLevel = DataManager.Instance.GetUpgradeLevel(def.id);
      int cost = CalculateCost(def, currentLevel);
      itemUI.Setup(def, currentLevel, cost, this);
    }
  }

  public int CalculateCost(UpgradeDef def, int level)
  {
    return def.baseCost + (def.costPerLevel * level);
  }

  public void TryBuyUpgrade(UpgradeDef def)
  {
    int currentLevel = DataManager.Instance.GetUpgradeLevel(def.id);
    if (def.maxLevel > 0 && currentLevel >= def.maxLevel) return; // 만렙

    int cost = CalculateCost(def, currentLevel);
    if (DataManager.Instance.SpendGold(cost))
    {
      DataManager.Instance.LevelUpUpgrade(def.id);
      RefreshUpgradeList(); // 목록 갱신 (비용/레벨 변경 반영)
                            // 효과음 재생 등 추가 가능
    }
    else
    {
      Debug.Log("골드가 부족합니다.");
    }
  }
}
