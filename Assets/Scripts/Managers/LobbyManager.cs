using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NeoSurvive.UI;
using NeoSurvive.UI.Multiplayer;
using System.Security.Cryptography.X509Certificates;

public class LobbyManager : MonoBehaviour
{
  public static LobbyManager Instance { get; private set; }

  [Header("Scene Settings")]
  public string gameSceneName = "lhsScene"; // 이동할 게임 씬 이름

  [Header("UI References")]
  public Button mainStartButton;
  public Button multiplayerButton;
  public Button settingButton;
  public Button exitButton;
  public Button upgradeButton;
  public GameObject mainMenuPanel; // 메인 버튼들을 포함하는 패널

  [Header("설정 UI")]
  public SettingsUI settingsUI;

  [Header("Tabs")]
  [SerializeField] private GameObject lobbyTab;
  [SerializeField] private GameObject multiplayerTab;

  [Header("Multiplayer UI")]
  [SerializeField] private MultiplayerRoomUI multiplayerRoomUI;

  [Header("Managers")]
  public UpgradeManager upgradeManager;
  public CharacterSelector characterSelector;

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

    // UI 참조 자동 찾기
    if (multiplayerRoomUI == null)
      multiplayerRoomUI = FindObjectOfType<MultiplayerRoomUI>();
  }

  private void Start()
  {

    if (upgradeManager == null) upgradeManager = FindObjectOfType<UpgradeManager>(true);
    if (characterSelector == null) characterSelector = FindObjectOfType<CharacterSelector>(true);

    // 이벤트 연결
    if (mainStartButton != null)
    {
      mainStartButton.onClick.RemoveAllListeners();
      mainStartButton.onClick.AddListener(characterSelector.ShowCharacterSelection);
    }

    if (multiplayerButton != null)
    {
      multiplayerButton.onClick.RemoveAllListeners();
      multiplayerButton.onClick.AddListener(ShowMultiplayerTab);
    }

    if (settingButton != null)
    {
      if (settingsUI == null) settingsUI = FindObjectOfType<SettingsUI>(true);
      settingButton.onClick.RemoveAllListeners();
      settingButton.onClick.AddListener(OnSettingButtonClicked);
    }

    if (exitButton != null)
    {
      exitButton.onClick.RemoveAllListeners();
      exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    if (upgradeButton != null && upgradeManager != null)
    {
      upgradeButton.onClick.RemoveAllListeners();
      upgradeButton.onClick.AddListener(upgradeManager.OpenUpgradeWindow);
    }

    // 초기 상태: 로비 탭 표시
    ShowLobbyTab();
  }

  private void LoadGameScene()
  {
    Debug.Log($"Loading Game Scene: {gameSceneName}");
    SceneManager.LoadScene(gameSceneName);
  }

  /// <summary>
  /// 로비 탭을 표시합니다
  /// </summary>
  public void ShowLobbyTab()
  {
    if (lobbyTab != null)
      lobbyTab.SetActive(true);

    if (multiplayerTab != null)
      multiplayerTab.SetActive(false);

    Debug.Log("[LobbyManager] 로비 탭 표시");
  }

  /// <summary>
  /// 멀티플레이어 대기실 탭을 표시합니다
  /// </summary>
  public void ShowMultiplayerTab()
  {
    if (lobbyTab != null)
      lobbyTab.SetActive(false);

    if (multiplayerTab != null)
    {
      multiplayerTab.SetActive(true);
    }
    else
    {
      Debug.LogError("[LobbyManager] Multiplayer Tab not found");
    }

    if (multiplayerRoomUI != null)
      multiplayerRoomUI.ShowRoomList();

    Debug.Log("[LobbyManager] 멀티플레이어 탭 표시");
  }

  public void OnSettingButtonClicked()
  {
    if (settingsUI == null)
    {
      Debug.LogWarning("[LobbyManager] SettingsUI가 할당되지 않았습니다!");
      return;
    }

    // 로비 메인 패널 숨기기
    if (mainMenuPanel != null)
      mainMenuPanel.SetActive(false);

    // SettingUi 게임오브젝트 자체를 먼저 활성화
    settingsUI.gameObject.SetActive(true);

    // 설정이 닫힐 때: 로비 패널 보이기 + SettingUi 비활성화
    settingsUI.onSettingsClosed = () =>
    {
      settingsUI.gameObject.SetActive(false);
      if (mainMenuPanel != null)
        mainMenuPanel.SetActive(true);
    };

    settingsUI.OpenSettings();
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