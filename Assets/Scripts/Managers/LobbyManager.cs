using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
  [Header("Scene Settings")]
  public string gameSceneName = "lhsScene"; // 이동할 게임 씬 이름

  [Header("UI References")]
  public Button startButton;
  public Button exitButton;
  public Button upgradeButton; // 추가

  public UpgradeManager upgradeManager; // 연결 필요

  private void Start()
  {
    // UI가 연결되지 않았다면 이름으로 찾아서 연결 시도
    if (startButton == null)
      startButton = GameObject.Find("StartButton")?.GetComponent<Button>();

    if (exitButton == null)
      exitButton = GameObject.Find("ExitButton")?.GetComponent<Button>();

    if (upgradeButton == null)
      upgradeButton = GameObject.Find("UpgradeButton")?.GetComponent<Button>(); // 이름 추정

    if (upgradeManager == null)
      upgradeManager = FindObjectOfType<UpgradeManager>(true);

    // 이벤트 연결
    if (startButton != null)
      startButton.onClick.AddListener(OnStartButtonClicked);

    if (exitButton != null)
      exitButton.onClick.AddListener(OnExitButtonClicked);

    if (upgradeButton != null && upgradeManager != null)
      upgradeButton.onClick.AddListener(upgradeManager.OpenUpgradeWindow);
  }

  public void OnStartButtonClicked()
  {
    Debug.Log("Start Button Clicked: Loading Game Scene...");
    // 씬 전환
    SceneManager.LoadScene(gameSceneName);
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
