using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeoSurvive.Network;
using NeoSurvive.Characters;

namespace NeoSurvive.UI.Multiplayer
{
  /// <summary>
  /// 멀티플레이어 방 대기실 UI
  /// </summary>
  public class MultiplayerRoomUI : MonoBehaviour
  {
    [Header("UI Panels")]
    [SerializeField] private GameObject roomListPanel;
    [SerializeField] private GameObject roomPanel;

    [Header("Room List UI")]
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private Button backToLobbyButton;
    [SerializeField] private TMP_InputField roomCodeInput;

    [Header("Room UI")]
    [SerializeField] private TextMeshProUGUI roomCodeText;
    [SerializeField] private Button copyRoomCodeButton;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerItemPrefab;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveRoomButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;

    [Header("Character Selection")]
    [SerializeField] private TMP_Dropdown characterDropdown;

    private bool isReady = false;
    private bool isHost = false;
    private string currentRoomCode = "";
    private List<GameObject> playerItemInstances = new List<GameObject>();

    private void OnEnable()
    {
      if (NetworkManager.Instance?.Lobby != null)
      {
        NetworkManager.Instance.Lobby.OnPlayerJoined += HandlePlayerEvent;
        NetworkManager.Instance.Lobby.OnPlayerLeft += HandlePlayerLeftEvent;
      }
    }

    private void OnDisable()
    {
      if (NetworkManager.Instance?.Lobby != null)
      {
        NetworkManager.Instance.Lobby.OnPlayerJoined -= HandlePlayerEvent;
        NetworkManager.Instance.Lobby.OnPlayerLeft -= HandlePlayerLeftEvent;
      }
    }

    private void HandlePlayerEvent(PlayerState state)
    {
      Debug.Log($"[MultiplayerRoomUI] 플레이어 이벤트 감지: ID {state.playerId}");
      UpdatePlayerList();
    }

    private void HandlePlayerLeftEvent(int playerId)
    {
      Debug.Log($"[MultiplayerRoomUI] 플레이어 퇴장 감지: ID {playerId}");
      UpdatePlayerList();
    }

    private void Start()
    {
      // 버튼 이벤트 연결
      if (createRoomButton != null)
        createRoomButton.onClick.AddListener(OnCreateRoomClicked);

      if (joinRoomButton != null)
        joinRoomButton.onClick.AddListener(OnJoinRoomClicked);

      if (backToLobbyButton != null)
        backToLobbyButton.onClick.AddListener(OnBackToLobbyClicked);

      if (copyRoomCodeButton != null)
        copyRoomCodeButton.onClick.AddListener(OnCopyRoomCodeClicked);

      if (readyButton != null)
        readyButton.onClick.AddListener(OnReadyClicked);

      if (startGameButton != null)
        startGameButton.onClick.AddListener(OnStartGameClicked);

      if (leaveRoomButton != null)
        leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);

      // 초기 상태
      ShowRoomList();
    }

    public void ShowRoomList()
    {
      if (roomListPanel != null)
        roomListPanel.SetActive(true);

      if (roomPanel != null)
        roomPanel.SetActive(false);
    }

    public void ShowRoom()
    {
      if (roomListPanel != null)
        roomListPanel.SetActive(false);

      if (roomPanel != null)
        roomPanel.SetActive(true);

      UpdateRoomUI();
    }

    private void OnCreateRoomClicked()
    {
      Debug.Log("[MultiplayerRoomUI] 방 생성");

      CharacterType selectedCharacter = GetSelectedCharacter();

      StartCoroutine(CreateRoom(selectedCharacter));
    }

    /// <summary>
    /// [1순위] UI의 '참가' 버튼 클릭 시 호출됩니다.
    /// 입력창의 코드를 확인하고 참가 프로세스를 시작합니다.
    /// </summary>
    private void OnJoinRoomClicked()
    {
      if (roomCodeInput == null || string.IsNullOrEmpty(roomCodeInput.text))
      {
        Debug.LogWarning("[MultiplayerRoomUI] 방 코드를 입력해주세요.");
        return;
      }

      string roomCode = roomCodeInput.text.ToUpper();
      Debug.Log($"[MultiplayerRoomUI] 방 참가 시도: {roomCode}");

      CharacterType selectedCharacter = GetSelectedCharacter();

      // [2순위] Coroutine을 통해 비동기 참가 프로세스 실행
      StartCoroutine(JoinRoom(roomCode, selectedCharacter));
    }

    private void OnBackToLobbyClicked()
    {
      Debug.Log("[MultiplayerRoomUI] 로비로 돌아가기");

      ShowRoomList();

      // LobbyManager를 통해 로비 탭으로 전환
      if (LobbyManager.Instance != null)
      {
        LobbyManager.Instance.ShowLobbyTab();
      }
    }

    private void OnCopyRoomCodeClicked()
    {
      if (!string.IsNullOrEmpty(currentRoomCode))
      {
        GUIUtility.systemCopyBuffer = currentRoomCode;
        Debug.Log($"[MultiplayerRoomUI] 방 코드 복사됨: {currentRoomCode}");

        // TODO: 복사 완료 피드백 UI
      }
    }

    private void OnReadyClicked()
    {
      isReady = !isReady;
      Debug.Log($"[MultiplayerRoomUI] 준비 상태 변경 시도: {isReady}");

      // 준비 상태를 서버에 전송
      if (NetworkManager.Instance != null)
      {
        NetworkManager.Instance.Lobby.SendReadyStatus(isReady);
      }

      UpdateReadyButton();
    }

    private void OnStartGameClicked()
    {
      if (!isHost)
      {
        Debug.LogWarning("[MultiplayerRoomUI] 방장만 게임을 시작할 수 있습니다.");
        return;
      }

      Debug.Log("[MultiplayerRoomUI] 게임 시작 요청 전송");

      // 서버에 게임 시작 신호 전송
      if (NetworkManager.Instance != null)
      {
        NetworkManager.Instance.Lobby.SendGameStart();
      }
    }

    private void OnLeaveRoomClicked()
    {
      Debug.Log("[MultiplayerRoomUI] 방 나가기");

      StartCoroutine(LeaveRoom());
    }

    private CharacterType GetSelectedCharacter()
    {
      if (characterDropdown != null)
      {
        int index = characterDropdown.value;
        return (CharacterType)index;
      }

      return CharacterType.Hacker; // 기본값
    }

    private IEnumerator CreateRoom(CharacterType characterType)
    {
      if (NetworkManager.Instance == null)
      {
        Debug.LogError("[MultiplayerRoomUI] NetworkManager가 없습니다!");
        yield break;
      }

      yield return NetworkManager.Instance.Lobby.CreateLobby(characterType);

      if (NetworkManager.Instance.Lobby.IsInLobby)
      {
        currentRoomCode = NetworkManager.Instance.Lobby.SessionCode;
        isHost = true;
        isReady = true; // 방장은 자동 준비

        ShowRoom();

        Debug.Log($"✅ 방 생성 완료! 코드: {currentRoomCode}");
      }
      else
      {
        Debug.LogError("❌ 방 생성 실패!");
      }
    }

    /// <summary>
    /// [2순위] 로비 참가 로직을 관리하는 코루틴입니다.
    /// NetworkManager(MultiLobbyManager)를 통해 서버에 접속합니다.
    /// </summary>
    private IEnumerator JoinRoom(string roomCode, CharacterType characterType)
    {
      if (NetworkManager.Instance == null)
      {
        Debug.LogError("[MultiplayerRoomUI] NetworkManager가 없습니다!");
        yield break;
      }

      // [3순위] MultiLobbyManager에게 서버 요청 및 연결 처리를 위임합니다.
      yield return NetworkManager.Instance.Lobby.JoinLobby(roomCode, characterType);

      // 성공적으로 로비에 진입했는지 확인합니다.
      if (NetworkManager.Instance.Lobby.IsInLobby)
      {
        currentRoomCode = roomCode;
        isHost = false;
        isReady = false;

        // 방 대기실 UI로 전환합니다.
        ShowRoom();

        Debug.Log($"✅ 방 참가 완료! 코드: {currentRoomCode}");
      }
      else
      {
        Debug.LogError("❌ 방 참가 실패!");
      }
    }

    private IEnumerator LeaveRoom()
    {
      if (NetworkManager.Instance == null)
      {
        yield break;
      }

      yield return NetworkManager.Instance.Lobby.LeaveLobby();

      currentRoomCode = "";
      isHost = false;
      isReady = false;

      ShowRoomList();

      Debug.Log("✅ 방 나가기 완료");
    }

    private IEnumerator StartMultiplayerGame()
    {
      // TODO: 게임 시작 신호를 서버에 전송

      // 게임 씬으로 전환
      yield return new WaitForSeconds(0.5f);

      Debug.Log("[MultiplayerRoomUI] 게임 씬 로드 (구현 필요)");
      // SceneManager.LoadScene("GameScene");
    }

    private void UpdateRoomUI()
    {
      // 방 코드 표시
      if (roomCodeText != null)
      {
        roomCodeText.text = $"Room Code: {currentRoomCode}";
      }

      // 방장 여부에 따라 버튼 표시
      if (startGameButton != null)
      {
        startGameButton.gameObject.SetActive(isHost);
      }

      if (readyButton != null)
      {
        readyButton.gameObject.SetActive(!isHost);
      }

      UpdateReadyButton();
      UpdatePlayerList();
    }

    private void UpdateReadyButton()
    {
      if (readyButtonText != null)
      {
        readyButtonText.text = isReady ? "cancel" : "ready";
      }

      if (readyButton != null)
      {
        ColorBlock colors = readyButton.colors;
        colors.normalColor = isReady ? Color.green : Color.white;
        readyButton.colors = colors;
      }
    }

    private void UpdatePlayerList()
    {
      // 기존 플레이어 아이템 제거
      foreach (GameObject item in playerItemInstances)
      {
        Destroy(item);
      }
      playerItemInstances.Clear();

      if (playerListContainer == null || playerItemPrefab == null)
        return;

      // MultiLobbyManager의 통합된 플레이어 목록(로컬 포함)을 사용하여 UI를 갱신합니다.
      if (NetworkManager.Instance != null && NetworkManager.Instance.Lobby != null)
      {
        var allPlayers = NetworkManager.Instance.Lobby.GetLobbyPlayers();
        int localId = DBManager.Instance?.PlayerId ?? -1;

        foreach (var player in allPlayers)
        {
          bool isMe = (player.playerId == localId);
          string displayName = isMe ? $"{player.nickname} (Me)" : player.nickname;

          // 방장 여부와 준비 상태를 반영하여 아이콘 표시
          CreatePlayerItem(displayName, (isMe && isHost), player.isReady);
        }
      }
    }

    private void CreatePlayerItem(string playerName, bool isHostPlayer, bool isPlayerReady)
    {
      if (playerListContainer == null || playerItemPrefab == null)
        return;

      GameObject playerItem = Instantiate(playerItemPrefab, playerListContainer);
      playerItemInstances.Add(playerItem);

      // 플레이어 정보 설정 (자식 오브젝트 이름이나 GetComponentInChildren으로 찾기)
      TextMeshProUGUI nameText = playerItem.GetComponentInChildren<TextMeshProUGUI>();
      if (nameText == null) // 경로로 찾기 시도
        nameText = playerItem.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();

      if (nameText != null)
      {
        nameText.text = isHostPlayer ? $"<color=yellow>[Host]</color> {playerName}" : playerName;
      }

      // 준비 상태 표시
      Transform readyIconTransform = playerItem.transform.Find("ReadyIcon");
      if (readyIconTransform != null)
      {
        readyIconTransform.gameObject.SetActive(isPlayerReady || isHostPlayer); // 방장은 항상 준비 상태로 표시
      }
    }

    private void OnDestroy()
    {
      // 버튼 이벤트 해제
      if (createRoomButton != null)
        createRoomButton.onClick.RemoveListener(OnCreateRoomClicked);

      if (joinRoomButton != null)
        joinRoomButton.onClick.RemoveListener(OnJoinRoomClicked);

      if (backToLobbyButton != null)
        backToLobbyButton.onClick.RemoveListener(OnBackToLobbyClicked);

      if (copyRoomCodeButton != null)
        copyRoomCodeButton.onClick.RemoveListener(OnCopyRoomCodeClicked);

      if (readyButton != null)
        readyButton.onClick.RemoveListener(OnReadyClicked);

      if (startGameButton != null)
        startGameButton.onClick.RemoveListener(OnStartGameClicked);

      if (leaveRoomButton != null)
        leaveRoomButton.onClick.RemoveListener(OnLeaveRoomClicked);
    }
  }
}
