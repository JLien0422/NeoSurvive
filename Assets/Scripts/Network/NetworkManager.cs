using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoSurvive.Network
{
  /// <summary>
  /// 네트워크 통신 통합 관리자
  /// HTTP (GameServerAPI) + WebSocket (WebSocketManager)를 통합 관리합니다.
  /// </summary>
  public class NetworkManager : MonoBehaviour
  {
    #region 싱글톤

    public static NetworkManager Instance { get; private set; }

    private void Awake()
    {
      if (Instance == null)
      {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 하위 매니저 초기화
        InitializeManagers();
      }
      else
      {
        Destroy(gameObject);
      }
    }

    #endregion

    #region 변수 및 프로퍼티

    [Header("Network Components")]
    [SerializeField] private GameServerAPI httpAPI;
    [SerializeField] private WebSocketManager websocketManager;

    [Header("Multi-Lobby Settings")]
    [SerializeField] private bool isHost = false;
    [SerializeField] private string currentSessionCode;
    private int currentMultiLobbySessionId = -1;

    [Header("Sync Settings")]
    [SerializeField] private float positionSyncInterval = 0.1f; // 위치 동기화 간격 (초)
    [SerializeField] private float stateSyncInterval = 1.0f; // 전체 상태 동기화 간격 (초)

    private float positionSyncTimer = 0f;
    private float stateSyncTimer = 0f;

    // 프로퍼티
    public bool IsHost => isHost;
    public bool IsInMultiLobby => currentMultiLobbySessionId >= 0;
    public string SessionCode => currentSessionCode;
    public GameServerAPI HttpAPI => httpAPI;
    public WebSocketManager WebSocket => websocketManager;

    // 게임 상태
    private GameSessionState localGameState;
    private Dictionary<int, PlayerState> remotePlayers = new Dictionary<int, PlayerState>();

    #endregion

    #region 이벤트

    // 로비 이벤트

    // 방에 플레이어가 들어옴
    public event Action<PlayerState> OnRemotePlayerJoined;
    // 방에서 플레이어가 나감
    public event Action<int> OnRemotePlayerLeft;
    // 방에서 플레이어 정보가 업데이트됨
    public event Action<PlayerState> OnRemotePlayerUpdated;

    // 게임 이벤트
    public event Action<EnemyState> OnEnemySpawned;
    public event Action<int> OnEnemyDied;
    public event Action<DropItemState> OnItemDropped;
    public event Action<int, int> OnItemCollected;
    public event Action<string> OnChatMessageReceived;

    #endregion

    #region 초기화 (Initialization)

    private void InitializeManagers()
    {
      // GameServerAPI 찾기 또는 생성
      if (httpAPI == null)
      {
        httpAPI = GetComponent<GameServerAPI>();
        if (httpAPI == null)
        {
          httpAPI = gameObject.AddComponent<GameServerAPI>();
        }
      }

      // WebSocketManager 찾기 또는 생성
      if (websocketManager == null)
      {
        websocketManager = GetComponent<WebSocketManager>();
        if (websocketManager == null)
        {
          websocketManager = gameObject.AddComponent<WebSocketManager>();
        }
      }

      // WebSocket 메시지 핸들러 등록
      RegisterWebSocketHandlers();
    }

    private void RegisterWebSocketHandlers()
    {
      if (websocketManager != null)
      {
        websocketManager.RegisterHandler("PlayerPosition", HandlePlayerPositionUpdate);
        websocketManager.RegisterHandler("PlayerState", HandlePlayerStateUpdate);
        websocketManager.RegisterHandler("EnemySpawn", HandleEnemySpawn);
        websocketManager.RegisterHandler("EnemyUpdate", HandleEnemyUpdate);
        websocketManager.RegisterHandler("EnemyDeath", HandleEnemyDeath);
        websocketManager.RegisterHandler("ItemDrop", HandleItemDrop);
        websocketManager.RegisterHandler("ItemCollect", HandleItemCollect);
        websocketManager.RegisterHandler("PlayerDamage", HandlePlayerDamage);
        websocketManager.RegisterHandler("Chat", HandleChatMessage);
        websocketManager.RegisterHandler("GameOver", HandleGameOver);
        websocketManager.RegisterHandler("PlayerJoined", HandlePlayerJoined);
      }
    }

    #endregion

    #region 멀티플레이어 로비 관리 (Multi-Lobby Management)

    /// <summary>
    /// 멀티플레이어 로비를 생성합니다. (호스트)
    /// </summary>
    public IEnumerator CreateMultiLobby(NeoSurvive.Characters.CharacterType characterType, int stage = 1)
    {
      Debug.Log("[NetworkManager] 멀티플레이어 로비 생성 중...");

      var request = new CreateMultiLobbyRequest
      {
        hostPlayerId = httpAPI.PlayerId,
        characterType = characterType.ToString(),
        stage = stage,
        maxPlayers = 2
      };

      string json = ES3SerializationHelper.SerializeToJson(request);

      using (UnityEngine.Networking.UnityWebRequest webRequest = httpAPI.CreatePostRequest("/multi-lobby/session/create", json))
      {
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
          var response = ES3SerializationHelper.DeserializeFromJson<CreateMultiLobbyResponse>(
              webRequest.downloadHandler.text);

          if (response != null)
          {
            currentMultiLobbySessionId = response.sessionId;
            currentSessionCode = response.sessionCode;
            isHost = true;

            Debug.Log($"✅ 멀티플레이어 로비 생성 완료! 코드: {currentSessionCode}");

            // WebSocket URL 보정
            string websocketUrl = FormatWebSocketUrl(response.websocketUrl, characterType.ToString());
            Debug.Log($"[NetworkManager] WebSocket 연결 시도: {websocketUrl}");

            // WebSocket 연결
            yield return websocketManager.Connect(
                websocketUrl,
                httpAPI.PlayerId,
                currentMultiLobbySessionId.ToString());
          }
        }
        else
        {
          Debug.LogError($"❌ 멀티플레이어 로비 생성 실패: {webRequest.error}");
        }
      }
    }

    /// <summary>
    /// 멀티플레이어 로비에 참가합니다. (클라이언트)
    /// </summary>
    public IEnumerator JoinMultiLobby(string sessionCode, NeoSurvive.Characters.CharacterType characterType)
    {
      Debug.Log($"[NetworkManager] 멀티플레이어 로비 참가 중... 코드: {sessionCode}");

      var request = new JoinMultiLobbyRequest
      {
        playerId = httpAPI.PlayerId,
        sessionCode = sessionCode,
        characterType = characterType.ToString()
      };

      string json = ES3SerializationHelper.SerializeToJson(request);

      using (UnityEngine.Networking.UnityWebRequest webRequest = httpAPI.CreatePostRequest("/multi-lobby/session/join", json))
      {
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
          var response = ES3SerializationHelper.DeserializeFromJson<JoinMultiLobbyResponse>(
              webRequest.downloadHandler.text);

          if (response != null && response.success)
          {
            currentMultiLobbySessionId = response.sessionId;
            currentSessionCode = sessionCode;
            isHost = false;

            Debug.Log($"✅ 멀티플레이어 로비 참가 완료! SessionId: {currentMultiLobbySessionId}");

            // 현재 게임 상태 로드
            localGameState = response.currentState;

            // 기존 플레이어 목록 동기화
            if (localGameState != null && localGameState.players != null)
            {
              foreach (var p in localGameState.players)
              {
                // 자기 자신은 제외하고 원격 플레이어 리스트에 추가
                if (p.playerId != httpAPI.PlayerId)
                {
                  remotePlayers[p.playerId] = p;
                }
              }
            }

            // WebSocket URL 보정
            string websocketUrl = FormatWebSocketUrl(response.websocketUrl, characterType.ToString());
            Debug.Log($"[NetworkManager] WebSocket 연결 시도: {websocketUrl}");

            // WebSocket 연결
            yield return websocketManager.Connect(
                websocketUrl,
                httpAPI.PlayerId,
                currentMultiLobbySessionId.ToString());
          }
          else
          {
            Debug.LogError($"❌ 멀티플레이어 로비 참가 실패: {response?.errorMessage}");
          }
        }
        else
        {
          Debug.LogError($"❌ 멀티플레이어 로비 참가 실패: {webRequest.error}");
        }
      }
    }

    /// <summary>
    /// 멀티플레이어 로비를 종료합니다.
    /// </summary>
    public IEnumerator LeaveMultiLobby()
    {
      Debug.Log("[NetworkManager] 멀티플레이어 로비 종료 중...");

      // WebSocket 연결 종료
      if (websocketManager != null && websocketManager.IsConnected)
      {
        yield return websocketManager.Disconnect();
      }

      // 세션 정보 초기화
      currentMultiLobbySessionId = -1;
      currentSessionCode = null;
      isHost = false;
      remotePlayers.Clear();

      Debug.Log("✅ 멀티플레이어 로비 종료 완료");
    }

    /// <summary>
    /// 게임 세션을 서버에 저장합니다.
    /// </summary>
    public IEnumerator SaveGameSession(GameSessionState sessionState)
    {
      if (currentMultiLobbySessionId < 0)
      {
        Debug.LogWarning("[NetworkManager] 멀티플레이어 로비가 없어 저장할 수 없습니다.");
        yield break;
      }

      Debug.Log("[NetworkManager] 게임 세션 저장 중...");

      var request = new SaveSessionRequest
      {
        sessionId = currentMultiLobbySessionId,
        sessionState = sessionState
      };

      string json = ES3SerializationHelper.SerializeToJson(request);

      using (UnityEngine.Networking.UnityWebRequest webRequest = httpAPI.CreatePostRequest("/multi-lobby/session/save", json))
      {
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
          var response = ES3SerializationHelper.DeserializeFromJson<SaveSessionResponse>(
              webRequest.downloadHandler.text);

          if (response != null && response.success)
          {
            Debug.Log($"✅ 세션 저장 완료! CheckpointId: {response.checkpointId}");
          }
          else
          {
            Debug.LogError($"❌ 세션 저장 실패: {response?.errorMessage}");
          }
        }
        else
        {
          Debug.LogError($"❌ 세션 저장 실패: {webRequest.error}");
        }
      }
    }

    #endregion

    #region 유틸리티 (Utilities)

    /// <summary>
    /// 서버에서 받은 WebSocket URL을 현재 환경에 맞게 보정하고 필요한 파라미터를 추가합니다.
    /// </summary>
    private string FormatWebSocketUrl(string originalUrl, string charType)
    {
      if (string.IsNullOrEmpty(originalUrl)) return originalUrl;

      // 1. 도메인 보정
      string formattedUrl = originalUrl.Replace("localhost", "nasdac.kro.kr");

      // 2. 쿼리 스트링 초기화 (서버가 준 URL에 쿼리가 이미 있다면 제거하고 새로 생성)
      if (formattedUrl.Contains("?"))
      {
        int queryIndex = formattedUrl.IndexOf("?");
        formattedUrl = formattedUrl.Substring(0, queryIndex);
      }

      // 3. 사용자 요청 형식 적용: ?playerId={id}&nickname={name}&characterType={type}
      // 현재 닉네임 프로퍼티가 없으므로 임시로 Player_{id} 사용
      string nickname = $"Player_{httpAPI.PlayerId}";

      formattedUrl += $"?playerId={httpAPI.PlayerId}&nickname={nickname}&characterType={charType}";

      Debug.Log($"[NetworkManager] 최종 WebSocket 주소: {formattedUrl}");
      return formattedUrl;
    }

    #endregion

    #region 플레이어 상태 동기화 (Player State Sync)

    /// <summary>
    /// 로컬 플레이어의 위치를 전송합니다.
    /// </summary>
    public void SendLocalPlayerPosition(Vector2 position, Vector2 velocity, float rotation)
    {
      if (!IsInMultiLobby || !websocketManager.IsConnected)
        return;

      websocketManager.SendPlayerPosition(position, velocity, rotation);
    }

    /// <summary>
    /// 로컬 플레이어의 전체 상태를 전송합니다.
    /// </summary>
    public void SendLocalPlayerState(PlayerState state)
    {
      if (!IsInMultiLobby || !websocketManager.IsConnected)
        return;

      websocketManager.SendPlayerState(state);
    }

    /// <summary>
    /// 원격 플레이어 상태를 가져옵니다.
    /// </summary>
    public PlayerState GetRemotePlayerState(int playerId)
    {
      if (remotePlayers.ContainsKey(playerId))
        return remotePlayers[playerId];

      return null;
    }

    /// <summary>
    /// 모든 원격 플레이어 목록을 가져옵니다.
    /// </summary>
    public List<PlayerState> GetAllRemotePlayers()
    {
      return new List<PlayerState>(remotePlayers.Values);
    }

    #endregion

    #region 적 동기화 (Enemy Sync) - 호스트만

    /// <summary>
    /// 적 스폰을 전송합니다. (호스트만)
    /// </summary>
    public void SendEnemySpawn(EnemyState enemy)
    {
      if (!isHost || !websocketManager.IsConnected)
        return;

      websocketManager.SendEnemySpawn(enemy);
    }

    /// <summary>
    /// 적 사망을 전송합니다.
    /// </summary>
    public void SendEnemyDeath(int enemyId, int killerPlayerId)
    {
      if (!websocketManager.IsConnected)
        return;

      websocketManager.SendEnemyDeath(enemyId, killerPlayerId);
    }

    #endregion

    #region 아이템 동기화 (Item Sync)

    /// <summary>
    /// 아이템 드랍을 전송합니다. (호스트만)
    /// </summary>
    public void SendItemDrop(DropItemState item)
    {
      if (!isHost || !websocketManager.IsConnected)
        return;

      websocketManager.SendItemDrop(item);
    }

    /// <summary>
    /// 아이템 수집을 전송합니다.
    /// </summary>
    public void SendItemCollect(int itemId)
    {
      if (!websocketManager.IsConnected)
        return;

      websocketManager.SendItemCollect(itemId);
    }

    #endregion

    #region WebSocket 메시지 핸들러 (WebSocket Message Handlers)

    private void HandlePlayerPositionUpdate(string json)
    {
      var message = ES3SerializationHelper.DeserializeFromJson<PlayerPositionUpdate>(json);
      if (message != null && message.playerId != httpAPI.PlayerId)
      {
        // 원격 플레이어 위치 업데이트
        if (!remotePlayers.ContainsKey(message.playerId))
        {
          remotePlayers[message.playerId] = new PlayerState { playerId = message.playerId };
        }

        remotePlayers[message.playerId].ApplyPositionUpdate(message);
        OnRemotePlayerUpdated?.Invoke(remotePlayers[message.playerId]);
      }
    }

    private void HandlePlayerStateUpdate(string json)
    {
      var message = ES3SerializationHelper.DeserializeFromJson<PlayerStateUpdate>(json);
      if (message?.playerState != null && message.playerState.playerId != httpAPI.PlayerId)
      {
        int playerId = message.playerState.playerId;

        if (!remotePlayers.ContainsKey(playerId))
        {
          // 새로운 플레이어 참가
          OnRemotePlayerJoined?.Invoke(message.playerState);
        }

        remotePlayers[playerId] = message.playerState;
        OnRemotePlayerUpdated?.Invoke(message.playerState);
      }
    }

    private void HandleEnemySpawn(string json)
    {
      var message = ES3SerializationHelper.DeserializeFromJson<EnemySpawnMessage>(json);
      if (message?.enemy != null)
      {
        OnEnemySpawned?.Invoke(message.enemy);
      }
    }

    private void HandleEnemyUpdate(string json)
    {
      var message = ES3SerializationHelper.DeserializeFromJson<EnemyUpdateMessage>(json);
      if (message?.enemies != null)
      {
        // 적 상태 업데이트 처리
        // TODO: EnemyManager에 전달
      }
    }

    private void HandleEnemyDeath(string json)
    {
      var message = ES3SerializationHelper.DeserializeFromJson<EnemyDeathMessage>(json);
      if (message != null)
      {
        OnEnemyDied?.Invoke(message.enemyId);
      }
    }

    private void HandleItemDrop(string json)
    {
      var message = ES3SerializationHelper.DeserializeFromJson<ItemDropMessage>(json);
      if (message?.item != null)
      {
        OnItemDropped?.Invoke(message.item);
      }
    }

    private void HandleItemCollect(string json)
    {
      var message = ES3SerializationHelper.DeserializeFromJson<ItemCollectMessage>(json);
      if (message != null)
      {
        OnItemCollected?.Invoke(message.itemId, message.collectorPlayerId);
      }
    }

    private void HandlePlayerDamage(string json)
    {
      var message = ES3SerializationHelper.DeserializeFromJson<PlayerDamageMessage>(json);
      if (message != null && remotePlayers.ContainsKey(message.playerId))
      {
        remotePlayers[message.playerId].health = message.remainingHealth;
        OnRemotePlayerUpdated?.Invoke(remotePlayers[message.playerId]);
      }
    }

    private void HandleChatMessage(string json)
    {
      var message = ES3SerializationHelper.DeserializeFromJson<ChatMessage>(json);
      if (message != null)
      {
        Debug.Log($"[Chat] {message.senderNickname}: {message.message}");
        // TODO: 채팅 UI 업데이트
      }
    }

    private void HandlePlayerJoined(string json)
    {
      var message = ES3SerializationHelper.DeserializeFromJson<PlayerJoinedMessage>(json);
      if (message != null)
      {
        Debug.Log($"[NetworkManager] 새로운 플레이어 입장: ID {message.PlayerId}, 닉네임 {message.Nickname}");

        // PlayerState로 변환하여 기존 시스템에 통합
        PlayerState newState = new PlayerState
        {
          playerId = message.PlayerId,
          nickname = message.Nickname,
          health = 100,
          isAlive = true
        };

        // 원격 플레이어 리스트에 추가
        if (!remotePlayers.ContainsKey(newState.playerId))
        {
          remotePlayers[newState.playerId] = newState;
          OnRemotePlayerJoined?.Invoke(newState);
        }
      }
    }

    private void HandleGameOver(string json)
    {
      var message = ES3SerializationHelper.DeserializeFromJson<GameOverMessage>(json);
      if (message != null)
      {
        Debug.Log($"[NetworkManager] 게임 오버! 생존 시간: {message.survivalTime}초, 킬: {message.totalKills}");
        // TODO: GameManager에 게임 종료 알림
      }
    }

    #endregion

    #region Unity Lifecycle

    private void Update()
    {
      if (!IsInMultiLobby || !websocketManager.IsConnected)
        return;

      // 위치 동기화 (빈번)
      positionSyncTimer += Time.deltaTime;
      if (positionSyncTimer >= positionSyncInterval)
      {
        positionSyncTimer = 0f;
        SyncLocalPlayerPosition();
      }

      // 전체 상태 동기화 (덜 빈번)
      stateSyncTimer += Time.deltaTime;
      if (stateSyncTimer >= stateSyncInterval)
      {
        stateSyncTimer = 0f;
        SyncLocalPlayerState();
      }
    }

    private void SyncLocalPlayerPosition()
    {
      // TODO: Player 컴포넌트에서 위치/속도 가져오기
      // var player = GameObject.FindWithTag("Player")?.GetComponent<Player>();
      // if (player != null)
      // {
      //     SendLocalPlayerPosition(player.transform.position, player.velocity, player.transform.rotation.eulerAngles.z);
      // }
    }

    private void SyncLocalPlayerState()
    {
      // TODO: 전체 플레이어 상태 수집 및 전송
    }

    #endregion
  }
}
