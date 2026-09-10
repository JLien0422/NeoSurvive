using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NeoSurvive.UI;
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

  [Header("Buttons")]
  [SerializeField] private Button singlePlayButton;
  private const string SinglePlayButtonName = "SinglePlayButton";
  private static readonly string[] TabObjectNames =
  {
    "LobbyTab",
    "SettingsTab",
    "CharacterSelectionTab",
    "TraitTab",
  };

  public List<GameObject> tabList = new List<GameObject>();
  public List<TabType> tabHistory = new List<TabType>();

  public TabType activatedTab;

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  private static void RegisterSinglePlayButtonSceneBinding()
  {
    SceneManager.sceneLoaded -= HandleSceneLoadedForButtonBinding;
    SceneManager.sceneLoaded += HandleSceneLoadedForButtonBinding;
    BindSinglePlayButtonInLoadedLobbyScene();
  }

  private static void HandleSceneLoadedForButtonBinding(Scene scene, LoadSceneMode mode)
  {
    if (!scene.name.Contains("Lobby"))
      return;

    BindSinglePlayButtonInLoadedLobbyScene();
  }

  private static void BindSinglePlayButtonInLoadedLobbyScene()
  {
    LobbyManager manager = FindSceneLobbyManager();
    if (manager == null)
    {
      Debug.LogWarning("[LobbyManager] 로비 씬에서 LobbyManager를 찾지 못했습니다.");
      return;
    }

    manager.RefreshSceneReferences();
  }

  private static LobbyManager FindSceneLobbyManager()
  {
    Scene activeScene = SceneManager.GetActiveScene();
    if (!activeScene.IsValid() || !activeScene.name.Contains("Lobby"))
      return null;

    GameObject[] roots = activeScene.GetRootGameObjects();
    foreach (GameObject root in roots)
    {
      LobbyManager manager = root.GetComponentInChildren<LobbyManager>(true);
      if (manager == null) continue;
      return manager;
    }

    return null;
  }

  private void Awake()
  {
    if (Instance != null && Instance != this && Instance.gameObject.scene != gameObject.scene)
    {
      Destroy(Instance.gameObject);
    }

    Instance = this;
    tabHistory.Clear();
    tabList.Clear();
    RefreshSceneReferences();
  }

  private void OnEnable()
  {
    RefreshSceneReferences();
  }

  private void Start()
  {
    tabHistory.Clear();
    RefreshSceneReferences();
    OpenTab(TabType.Lobby);
  }

  private void OnDestroy()
  {
    if (singlePlayButton != null)
    {
      singlePlayButton.onClick.RemoveListener(OpenCharacterSelectionTab);
    }

    if (Instance == this)
    {
      Instance = null;
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

  private void RefreshSceneReferences()
  {
    EnsureTabList(forceRebuild: true);
    BindSinglePlayButton();
  }

  private void BindSinglePlayButton()
  {
    singlePlayButton = FindSceneButtonByName(SinglePlayButtonName);

    if (singlePlayButton == null)
    {
      Debug.LogWarning($"[LobbyManager] {SinglePlayButtonName}을 찾지 못했습니다.");
      return;
    }

    singlePlayButton.onClick = new Button.ButtonClickedEvent();
    singlePlayButton.onClick.AddListener(OpenCharacterSelectionTab);
    Debug.Log("[LobbyManager] SinglePlayButton OnClick을 코드에서 강제 연결했습니다.");
  }

  private void OpenCharacterSelectionTab()
  {
    Debug.Log("[LobbyManager] SinglePlayButton 클릭 감지: CharacterSelection 탭 열기");
    OpenTab(TabType.CharacterSelection);
  }

  private Button FindSceneButtonByName(string buttonName)
  {
    GameObject buttonObject = FindSceneObjectByName(buttonName);
    return buttonObject != null ? buttonObject.GetComponent<Button>() : null;
  }

  private bool EnsureTabList(bool forceRebuild = false)
  {
    int expectedCount = TabObjectNames.Length;
    bool needsRebuild = forceRebuild || tabList == null || tabList.Count < expectedCount;

    if (!needsRebuild)
    {
      for (int i = 0; i < expectedCount; i++)
      {
        if (tabList[i] == null)
        {
          needsRebuild = true;
          break;
        }
      }
    }

    if (!needsRebuild)
      return true;

    List<GameObject> rebuiltTabs = new List<GameObject>(expectedCount);
    for (int i = 0; i < expectedCount; i++)
    {
      GameObject tab = FindSceneObjectByName(TabObjectNames[i]);
      if (tab == null)
      {
        Debug.LogWarning($"[LobbyManager] 탭 오브젝트를 찾지 못했습니다: {TabObjectNames[i]}");
        return false;
      }

      rebuiltTabs.Add(tab);
    }

    tabList = rebuiltTabs;
    Debug.Log("[LobbyManager] tabList를 현재 로비 씬 오브젝트로 복구했습니다.");
    return true;
  }

  private GameObject FindSceneObjectByName(string objectName)
  {
    Scene activeScene = SceneManager.GetActiveScene();
    GameObject found = FindObjectInScene(activeScene, objectName);
    if (found != null)
      return found;

    Scene ownScene = gameObject.scene;
    if (ownScene.IsValid() && ownScene != activeScene)
      return FindObjectInScene(ownScene, objectName);

    return null;
  }

  private static GameObject FindObjectInScene(Scene scene, string objectName)
  {
    if (!scene.IsValid())
      return null;

    GameObject[] roots = scene.GetRootGameObjects();
    foreach (GameObject root in roots)
    {
      Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
      foreach (Transform child in transforms)
      {
        if (child.name == objectName)
          return child.gameObject;
      }
    }

    return null;
  }

  public void BackTab()
  {
    if (tabHistory.Count >= 2)
    {
      LobbySoundManager.Instance?.PlayBack();
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
    if (!EnsureTabList())
      return;

    if (activatedTab == TabType.Settings && tabType != TabType.Settings)
      CloseLobbySettingsIfNeeded();

    if (activatedTab != tabType)
      LobbySoundManager.Instance?.PlayTabSwitch();

    foreach (var tab in tabList)
    {
      tab.SetActive(false);
    }
    int tabIndex = (int)tabType;
    if (tabIndex < 0 || tabIndex >= tabList.Count || tabList[tabIndex] == null)
    {
      Debug.LogWarning($"[LobbyManager] 열 수 없는 탭입니다: {tabType}");
      return;
    }

    tabList[tabIndex].SetActive(true);
    activatedTab = tabType;

    if (tabType == TabType.Settings)
      OpenLobbySettingsIfNeeded();

    if (!tabHistory.Contains(tabType))
    {
      tabHistory.Add(tabType);
    }
  }

  private void OpenLobbySettingsIfNeeded()
  {
    int settingsIndex = (int)TabType.Settings;
    if (settingsIndex < 0 || settingsIndex >= tabList.Count || tabList[settingsIndex] == null)
      return;

    tabList[settingsIndex].GetComponent<SettingsUI>()?.OpenForLobby();
  }

  private void CloseLobbySettingsIfNeeded()
  {
    int settingsIndex = (int)TabType.Settings;
    if (settingsIndex < 0 || settingsIndex >= tabList.Count || tabList[settingsIndex] == null)
      return;

    tabList[settingsIndex].GetComponent<SettingsUI>()?.CloseForLobby();
  }

  public void CloseTab(TabType tabType)
  {
    if (!EnsureTabList())
      return;

    int tabIndex = (int)tabType;
    if (tabIndex < 0 || tabIndex >= tabList.Count || tabList[tabIndex] == null)
      return;

    tabList[tabIndex].SetActive(false);
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
    LobbySoundManager.Instance?.PlayCancel();
    Debug.Log("Exit Button Clicked: Quitting Application...");
    Application.Quit();
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#endif
  }
}