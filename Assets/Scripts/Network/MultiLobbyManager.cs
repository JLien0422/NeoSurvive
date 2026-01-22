using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Network;
using System.Linq;

namespace NeoSurvive.Network
{
  /// <summary>
  /// 멀티플레이어 로비 관리자 (WebSocket 기반)
  /// 방 생성, 참가, 대기열, 로비 채팅 및 유저 목록 동기화를 담당합니다.
  /// </summary>
  public class MultiLobbyManager : MonoBehaviour
  {
    public static MultiLobbyManager Instance { get; private set; }

    [Header("Components")]
    [SerializeField] private WebSocketManager websocketManager;

    [SerializeField]
    private bool isHost = false;
    [SerializeField]
    private string currentSessionCode;
    [SerializeField]
    private int currentSessionId = -1;

    [SerializeField]
    // 로비 인원 관리
    private PlayerState[] lobbyPlayers = Array.Empty<PlayerState>();

    public bool IsHost => isHost;
    public bool IsInLobby => currentSessionId >= 0;
    public string SessionCode => currentSessionCode;

    public event Action<PlayerState> OnPlayerJoined;
    public event Action<int> OnPlayerLeft;
    public event Action<string, string> OnLobbyChatReceived;

    private void Awake()
    {
      if (Instance == null)
      {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeWebSocket();
      }
      else
      {
        Destroy(gameObject);
      }
    }

    private void InitializeWebSocket()
    {
      if (websocketManager == null)
        websocketManager = gameObject.AddComponent<WebSocketManager>();

      // 로비 관련 핸들러 등록
      websocketManager.RegisterHandler("PlayerJoined", HandlePlayerJoined);
      websocketManager.RegisterHandler("PlayerLeft", HandlePlayerLeft);
      websocketManager.RegisterHandler("PlayerReady", HandlePlayerReady);
      websocketManager.RegisterHandler("GameStart", HandleGameStart);
      websocketManager.RegisterHandler("LobbyChat", HandleLobbyChat);
      websocketManager.RegisterHandler("Chat", HandleChat); // 기존 인게임 채팅 호환용
    }

    public IEnumerator CreateLobby(NeoSurvive.Characters.CharacterType characterType)
    {
      var request = new CreateMultiLobbyRequest
      {
        hostPlayerId = DBManager.Instance.PlayerId,
        characterType = characterType.ToString(),
        stage = 1,
        maxPlayers = 2
      };

      string json = ES3SerializationHelper.SerializeToJson(request);
      using (var webRequest = DBManager.Instance.CreatePostRequest("/multi-lobby/session/create", json))
      {
        yield return webRequest.SendWebRequest();
        if (webRequest.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
          var response = ES3SerializationHelper.DeserializeFromJson<CreateMultiLobbyResponse>(webRequest.downloadHandler.text);
          SetupLobby(response.sessionId, response.sessionCode, true);

          string wsUrl = FormatWSUrl(response.websocketUrl, characterType.ToString());
          yield return websocketManager.Connect(wsUrl, DBManager.Instance.PlayerId, response.sessionId.ToString());
        }
      }
    }

    /// <summary>
    /// [3순위] 지정된 방 코드로 로비 참가를 요청합니다.
    /// HTTP API 호출 후 성공 시 WebSocket 연결까지 수행합니다.
    /// </summary>
    public IEnumerator JoinLobby(string code, NeoSurvive.Characters.CharacterType characterType)
    {
      // [3-1] HTTP API 요청 메시지 생성
      var request = new JoinMultiLobbyRequest
      {
        playerId = DBManager.Instance.PlayerId,
        sessionCode = code,
        characterType = characterType.ToString()
      };

      string json = ES3SerializationHelper.SerializeToJson(request);

      // [3-2] 서버(/multi-lobby/session/join)에 POST 요청 전송
      using (var webRequest = DBManager.Instance.CreatePostRequest("/multi-lobby/session/join", json))
      {
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
          var response = ES3SerializationHelper.DeserializeFromJson<JoinMultiLobbyResponse>(webRequest.downloadHandler.text);

          if (response.success)
          {
            // [3-3] 참가 성공 시 로컬 로비 데이터 설정
            SetupLobby(response.sessionId, code, false);

            // [3-4] 이미 방에 있는 다른 플레이어 목록을 동기화합니다.
            if (response.players != null)
            {
              lobbyPlayers = response.players.ToArray();
              var players = string.Join(", ", lobbyPlayers.Select(p => $"[{p.playerId}: {p.nickname}]"));
              Debug.Log($"[MultiLobbyManager] 로비 참가 완료. 현재 인원 목록 ({lobbyPlayers.Length}): {players}");
            }
            else
            {
              Debug.LogWarning($"[MultiLobbyManager] 로비 참가 응답에 플레이어 목록이 없습니다.");
            }

            // [3-5] 실시간 통신을 위한 WebSocket 연결을 시작합니다 (4순위로 이어짐)
            string wsUrl = FormatWSUrl(response.websocketUrl, characterType.ToString());
            yield return websocketManager.Connect(wsUrl, DBManager.Instance.PlayerId, response.sessionId.ToString());
          }
        }
      }
    }

    public IEnumerator LeaveLobby()
    {
      if (IsInLobby && websocketManager != null)
      {
        yield return websocketManager.Disconnect();
        lobbyPlayers = Array.Empty<PlayerState>();
        currentSessionId = -1;
        currentSessionCode = "";
        isHost = false;
        Debug.Log("[MultiLobbyManager] 로비 퇴장 완료");
      }
    }

    private void SetupLobby(int id, string code, bool host)
    {
      currentSessionId = id;
      currentSessionCode = code;
      isHost = host;
      lobbyPlayers = Array.Empty<PlayerState>();
    }

    private string FormatWSUrl(string url, string charType)
    {
      string formatted = url.Replace("localhost", "nasdac.kro.kr");
      string nickname = DBManager.Instance?.Nickname ?? "Unknown";

      // 스펙에 맞게 쿼리 파라미터 구성
      var wsUrl = $"{formatted}?playerId={DBManager.Instance.PlayerId}&nickname={Uri.EscapeDataString(nickname)}&characterType={charType}";

      if (isHost)
      {
        wsUrl += $"&hostPlayerId={DBManager.Instance.PlayerId}";
      }

      return wsUrl;
    }

    #region WebSocket Handlers

    private void HandlePlayerJoined(string json)
    {
      var msg = ES3SerializationHelper.DeserializeFromJson<PlayerJoinedMessage>(json);
      if (msg.playerId == DBManager.Instance.PlayerId) return;

      PlayerState newState = new PlayerState
      {
        playerId = msg.playerId,
        nickname = msg.nickname,
        characterType = msg.characterType
      };

      // 중복 체크 후 배열에 추가
      if (!lobbyPlayers.Any(p => p.playerId == msg.playerId))
      {
        lobbyPlayers = lobbyPlayers.Append(newState).ToArray();
      }
      OnPlayerJoined?.Invoke(newState);
    }

    private void HandlePlayerLeft(string json)
    {
      var msg = ES3SerializationHelper.DeserializeFromJson<PlayerLeftMessage>(json);
      int leftPlayerId = msg.playerId;

      lobbyPlayers = lobbyPlayers.Where(p => p.playerId != leftPlayerId).ToArray();
      OnPlayerLeft?.Invoke(leftPlayerId);
      Debug.Log($"[MultiLobbyManager] 플레이어 퇴장: {msg.nickname} ({leftPlayerId})");
    }

    private void HandlePlayerReady(string json)
    {
      var msg = ES3SerializationHelper.DeserializeFromJson<PlayerReadyMessage>(json);
      var player = lobbyPlayers.FirstOrDefault(p => p.playerId == msg.playerId);
      if (player != null)
      {
        player.isReady = msg.isReady;
        Debug.Log($"[MultiLobbyManager] 플레이어 준비 상태 변경: {msg.nickname} -> {msg.isReady}");
        // UI 갱신을 위해 이벤트 발생 (OnPlayerJoined 재사용 가능하겠으나 별도 정의 권장)
        OnPlayerJoined?.Invoke(player);
      }
    }

    private void HandleGameStart(string json)
    {
      var msg = ES3SerializationHelper.DeserializeFromJson<GameStartNotificationMessage>(json);
      Debug.Log($"[MultiLobbyManager] 게임 시작 알림 수신! UDP: {msg.udpServerHost}:{msg.udpServerPort}");
      // TODO: Co-op 게임 씬으로 전환 및 UDP 소켓 초기화 로직 구현
    }

    private void HandleLobbyChat(string json)
    {
      var msg = ES3SerializationHelper.DeserializeFromJson<LobbyChatMessage>(json);
      OnLobbyChatReceived?.Invoke(msg.senderNickname, msg.message);
    }

    private void HandleChat(string json)
    {
      var msg = ES3SerializationHelper.DeserializeFromJson<ChatMessage>(json);
      OnLobbyChatReceived?.Invoke(msg.senderNickname, msg.message);
    }

    #endregion

    #region Send Messages

    public void SendReadyStatus(bool ready)
    {
      var msg = new PlayerReadyMessage
      {
        playerId = DBManager.Instance.PlayerId,
        nickname = DBManager.Instance.Nickname,
        isReady = ready
      };
      websocketManager.SendWebSocketMessage(ES3SerializationHelper.SerializeToJson(msg));
    }

    public void SendGameStart()
    {
      if (!isHost) return;

      var msg = new GameStartRequestMessage
      {
        sessionId = currentSessionId
      };
      websocketManager.SendWebSocketMessage(ES3SerializationHelper.SerializeToJson(msg));
    }

    public void SendLobbyChat(string message)
    {
      var msg = new LobbyChatMessage
      {
        senderId = DBManager.Instance.PlayerId,
        senderNickname = DBManager.Instance.Nickname,
        message = message
      };
      websocketManager.SendWebSocketMessage(ES3SerializationHelper.SerializeToJson(msg));
    }

    public void SendLeaveLobby()
    {
      var msg = new PlayerLeftMessage
      {
        playerId = DBManager.Instance.PlayerId,
        nickname = DBManager.Instance.Nickname
      };
      websocketManager.SendWebSocketMessage(ES3SerializationHelper.SerializeToJson(msg));
    }

    #endregion

    public PlayerState[] GetLobbyPlayers() => lobbyPlayers;
  }
}
