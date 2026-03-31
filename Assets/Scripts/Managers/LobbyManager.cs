using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NeoSurvive.UI;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Linq;

public class LobbyManager : MonoBehaviour
{
  public static LobbyManager Instance { get; private set; }

  public enum TabType
  {
    Lobby,
    Settings,
    CharacterSelection,
    Trait,
  }

  public Dictionary<string, int> TabDomainDict = new Dictionary<string, int>
  {
    ["Lobby"] = (int)TabType.Lobby,
    ["Settings"] = (int)TabType.Settings,
    ["CharacterSelection"] = (int)TabType.CharacterSelection,
    ["Trait"] = (int)TabType.Trait,
  };

  [Header("Managers")]
  public UpgradeManager upgradeManager;
  public CharacterSelector characterSelector;

  public List<GameObject> tabList = new List<GameObject>();
  public List<TabType> tabHistory = new List<TabType>();

  public TabType activatedTab;

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }
    else
    {
      Destroy(gameObject);
      return;
    }
  }

  private void Update()
  {
    // 닫기
    if (Input.GetKeyDown(KeyCode.Escape))
    {
      BackTab();
    }
  }

  public void BackTab()
  {
    if (tabHistory.Count >= 2)
    {
      var before = tabHistory[tabHistory.Count - 2];
      OpenTab(before);
      activatedTab = before;
      tabHistory.RemoveAt(tabHistory.Count - 1);
    }
  }

  public void OpenTab(int tabType)
  {
    OpenTab((TabType)tabType);
  }

  public void OpenTab(string tabName)
  {
    if (System.Enum.TryParse(tabName, out TabType tabType))
    {
      OpenTab(tabType);
    }
    else
    {
      Debug.LogWarning($"[LobbyManager] Invalid tab name: {tabName}");
    }
  }

  public void OpenTab(TabType tabType)
  {
    foreach (var tab in tabList)
    {
      tab.SetActive(false);
    }
    tabList[(int)tabType].SetActive(true);
    activatedTab = tabType;

    if (!tabHistory.Contains(tabType))
    {
      tabHistory.Add(tabType);
    }
  }

  public void CloseTab(TabType tabType)
  {
    tabList[(int)tabType].SetActive(false);
  }

  public void CloseCurrentTab()
  {
    CloseTab(activatedTab);

    if (tabHistory.Count > 0)
    {
      var lastTab = tabHistory.Last();
      OpenTab(lastTab);
      activatedTab = lastTab;
      tabHistory.RemoveAt(tabHistory.Count - 1);
    }
  }

  public void OnExitButtonClicked()
  {
    Debug.Log("Exit Button Clicked: Quitting Application...");
    Application.Quit();
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#endif
  }
}