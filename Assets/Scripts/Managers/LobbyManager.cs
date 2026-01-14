using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NeoSurvive.UI;

public class LobbyManager : MonoBehaviour
{
  [Header("Scene Settings")]
  public string gameSceneName = "lhsScene"; // 이동할 게임 씬 이름

  [Header("UI References")]
  public Button mainStartButton;
  public Button exitButton;
  public Button upgradeButton;
  public GameObject mainMenuPanel; // 메인 버튼들을 포함하는 패널

  [Header("Managers")]
  public UpgradeManager upgradeManager;
  public CharacterSelector characterSelector;

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
  }

  private void LoadGameScene()
  {
    Debug.Log($"Loading Game Scene: {gameSceneName}");
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
