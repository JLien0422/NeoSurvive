using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Rendering;
using NeoSurvive.Network;

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

  // 서버에서 받은 업그레이드 목록
  private List<UpgradeDTO> serverUpgrades = new List<UpgradeDTO>();
  private GameServerAPI gameServerAPI;

  private void Start()
  {
    // GameServerAPI 참조 가져오기
    if (GameManager.Instance != null)
    {
      gameServerAPI = GameManager.Instance.GetComponent<GameServerAPI>();
    }

    if (gameServerAPI == null)
    {
      gameServerAPI = FindObjectOfType<GameServerAPI>();
    }

    if (gameServerAPI == null)
    {
      Debug.LogError("UpgradeManager: GameServerAPI를 찾을 수 없습니다.");
      return;
    }

    // 초기화 시엔 윈도우가 닫혀있을 수 있으므로 UI 갱신은 창을 열 때 수행
  }

  public void OpenUpgradeWindow()
  {
    if (upgradeWindow != null)
    {
      upgradeWindow.SetActive(true);
      RefreshDataFromServer();
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

  private void RefreshDataFromServer()
  {
    if (gameServerAPI == null) return;

    StartCoroutine(gameServerAPI.GetPlayerUpgrades((response) =>
    {
      if (response != null)
      {
        serverUpgrades = response.upgrades;
        UpdateGoldUI(response.playerGold);
        CreateUpgradeItems();
      }
    }));
  }

  private void CreateUpgradeItems()
  {
    // 기존 아이템 삭제
    foreach (Transform child in contentRoot)
    {
      Destroy(child.gameObject);
    }

    // 목록 생성
    foreach (var dto in serverUpgrades)
    {
      CreateUpgradeItem(dto);
    }
  }

  private void CreateUpgradeItem(UpgradeDTO dto)
  {
    if (upgradeItemPrefab == null) return;

    GameObject itemObj = Instantiate(upgradeItemPrefab, contentRoot);
    itemObj.SetActive(true);

    UpgradeItemUI itemUI = itemObj.GetComponent<UpgradeItemUI>();
    if (itemUI != null)
    {
      UpgradeDef def = new UpgradeDef
      {
        id = dto.id,
        displayName = dto.displayName,
        baseCost = dto.baseCost,
        costPerLevel = dto.costPerLevel,
        valuePerLevel = dto.valuePerLevel,
        maxLevel = dto.maxLevel
      };

      int cost = dto.nextCost;
      itemUI.Setup(def, dto.currentLevel, cost, this);
    }
  }

  // UI에서 구매 버튼 클릭 시 호출됨 (UpgradeItemUI가 호출)
  public void TryBuyUpgrade(UpgradeDef def)
  {
    if (gameServerAPI == null) return;

    StartCoroutine(gameServerAPI.PurchasePlayerUpgrade(def.id, (response) =>
    {
      if (response.success)
      {
        // 성공 시 UI 갱신 (서버에서 최신 상태를 다시 받아오거나, 로컬에서 예측 갱신)
        // 신뢰성을 위해 다시 받아오는 것을 권장하지만, 반응성을 위해 여기선 일부만 갱신하거나 전체 갱신.
        UpdateGoldUI(response.remainingGold);
        RefreshDataFromServer(); // 전체 목록 갱신 (가장 안전)
      }
      else
      {
        Debug.LogWarning($"구매 실패: {response.errorMessage}");
        // 실패 알림 UI 표시 가능
      }
    }));
  }
}
