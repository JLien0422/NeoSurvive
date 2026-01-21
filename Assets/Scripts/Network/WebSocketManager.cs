using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket; // NativeWebSocket 패키지 필요 (Package Manager에서 설치)

namespace NeoSurvive.Network
{
  /// <summary>
  /// WebSocket 기반 실시간 게임 상태 동기화 관리자
  /// Co-op 멀티플레이어 실시간 통신을 담당합니다.
  /// 
  /// 설치 필요: Package Manager -> Add package from git URL
  /// https://github.com/endel/NativeWebSocket.git#upm
  /// </summary>
  public class WebSocketManager : MonoBehaviour
  {
    #region 싱글톤

    public static WebSocketManager Instance { get; private set; }

    private void Awake()
    {
      if (Instance == null)
      {
        Instance = this;
        DontDestroyOnLoad(gameObject);
      }
      else
      {
        Destroy(gameObject);
      }
    }

    #endregion

    #region 변수 및 프로퍼티

    private WebSocket websocket;
    private bool isConnected = false;
    private string sessionId;
    private int localPlayerId;

    // 연결 상태
    public bool IsConnected => isConnected;
    public string SessionId => sessionId;

    // 메시지 핸들러 딕셔너리
    private Dictionary<string, Action<string>> messageHandlers = new Dictionary<string, Action<string>>();

    // 재연결 설정
    [SerializeField] private bool autoReconnect = true;
    [SerializeField] private float reconnectDelay = 3f;
    [SerializeField] private int maxReconnectAttempts = 5;
    private int reconnectAttempts = 0;
    private Coroutine reconnectCoroutine;

    // 핑 설정
    [SerializeField] private float pingInterval = 30f;
    private float lastPingTime = 0f;

    #endregion

    #region 이벤트

    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnError;

    #endregion

    #region 연결 관리 (Connection Management)

    /// <summary>
    /// WebSocket 서버에 연결합니다.
    /// </summary>
    public IEnumerator Connect(string websocketUrl, int playerId, string sessionId)
    {
      if (isConnected)
      {
        Debug.LogWarning("[WebSocket] 이미 연결되어 있습니다.");
        yield break;
      }

      this.sessionId = sessionId;
      this.localPlayerId = playerId;

      Debug.Log($"[WebSocket] 연결 시도: {websocketUrl}");

      websocket = new WebSocket(websocketUrl);

      // 이벤트 핸들러 등록
      websocket.OnOpen += HandleOpen;
      websocket.OnMessage += HandleMessage;
      websocket.OnError += HandleError;
      websocket.OnClose += HandleClose;

      // 연결 시작
      yield return websocket.Connect();
    }

    /// <summary>
    /// WebSocket 연결을 종료합니다.
    /// </summary>
    public IEnumerator Disconnect()
    {
      if (websocket != null && isConnected)
      {
        Debug.Log("[WebSocket] 연결 종료 중...");
        yield return websocket.Close();
        websocket = null;
        isConnected = false;
      }
    }

    /// <summary>
    /// 재연결을 시도합니다.
    /// </summary>
    private IEnumerator AttemptReconnect(string websocketUrl)
    {
      if (!autoReconnect || reconnectAttempts >= maxReconnectAttempts)
      {
        Debug.LogError("[WebSocket] 재연결 실패: 최대 시도 횟수 초과");
        yield break;
      }

      reconnectAttempts++;
      Debug.Log($"[WebSocket] 재연결 시도 {reconnectAttempts}/{maxReconnectAttempts}");

      yield return new WaitForSeconds(reconnectDelay);
      yield return Connect(websocketUrl, localPlayerId, sessionId);
    }

    #endregion

    #region 메시지 전송 (Send Messages)

    /// <summary>
    /// 메시지를 서버에 전송합니다.
    /// </summary>
    private void SendMessage(string json)
    {
      if (!isConnected || websocket == null)
      {
        Debug.LogWarning("[WebSocket] 연결되지 않아 메시지를 전송할 수 없습니다.");
        return;
      }

      try
      {
        websocket.SendText(json);
      }
      catch (Exception e)
      {
        Debug.LogError($"[WebSocket] 메시지 전송 실패: {e.Message}");
      }
    }

    /// <summary>
    /// 플레이어 위치 업데이트 전송 (빈번한 전송용 - 경량화)
    /// </summary>
    public void SendPlayerPosition(Vector2 position, Vector2 velocity, float rotation)
    {
      var message = new PlayerPositionUpdate
      {
        PlayerId = localPlayerId,
        x = position.x,
        y = position.y,
        vx = velocity.x,
        vy = velocity.y,
        rot = rotation
      };

      string json = ES3SerializationHelper.SerializeToJson(message);
      if (json != null)
      {
        SendMessage(json);
      }
    }

    /// <summary>
    /// 플레이어 전체 상태 업데이트 전송
    /// </summary>
    public void SendPlayerState(PlayerState state)
    {
      var message = new PlayerStateUpdate
      {
        playerState = state
      };

      string json = ES3SerializationHelper.SerializeToJson(message);
      if (json != null)
      {
        SendMessage(json);
      }
    }

    /// <summary>
    /// 적 스폰 메시지 전송 (호스트만)
    /// </summary>
    public void SendEnemySpawn(EnemyState enemy)
    {
      var message = new EnemySpawnMessage
      {
        enemy = enemy
      };

      string json = ES3SerializationHelper.SerializeToJson(message);
      if (json != null)
      {
        SendMessage(json);
      }
    }

    /// <summary>
    /// 적 사망 메시지 전송
    /// </summary>
    public void SendEnemyDeath(int enemyId, int killerPlayerId)
    {
      var message = new EnemyDeathMessage
      {
        enemyId = enemyId,
        killerPlayerId = killerPlayerId
      };

      string json = ES3SerializationHelper.SerializeToJson(message);
      if (json != null)
      {
        SendMessage(json);
      }
    }

    /// <summary>
    /// 아이템 드랍 메시지 전송 (호스트만)
    /// </summary>
    public void SendItemDrop(DropItemState item)
    {
      var message = new ItemDropMessage
      {
        item = item
      };

      string json = ES3SerializationHelper.SerializeToJson(message);
      if (json != null)
      {
        SendMessage(json);
      }
    }

    /// <summary>
    /// 아이템 수집 메시지 전송
    /// </summary>
    public void SendItemCollect(int itemId)
    {
      var message = new ItemCollectMessage
      {
        itemId = itemId,
        collectorPlayerId = localPlayerId
      };

      string json = ES3SerializationHelper.SerializeToJson(message);
      if (json != null)
      {
        SendMessage(json);
      }
    }

    /// <summary>
    /// 플레이어 데미지 메시지 전송
    /// </summary>
    public void SendPlayerDamage(int playerId, float damage, float remainingHealth, int attackerId)
    {
      var message = new PlayerDamageMessage
      {
        PlayerId = playerId,
        Damage = damage,
        RemainingHealth = remainingHealth,
        AttackerId = attackerId
      };

      string json = ES3SerializationHelper.SerializeToJson(message);
      if (json != null)
      {
        SendMessage(json);
      }
    }

    /// <summary>
    /// 채팅 메시지 전송
    /// </summary>
    public void SendChatMessage(string message)
    {
      var chatMsg = new ChatMessage
      {
        SenderId = localPlayerId,
        SenderNickname = GameManager.Instance?.GetSelectedCharacter().ToString() ?? "Player",
        Message = message
      };

      string json = ES3SerializationHelper.SerializeToJson(chatMsg);
      if (json != null)
      {
        SendMessage(json);
      }
    }

    #endregion

    #region 메시지 핸들러 등록 (Register Handlers)

    /// <summary>
    /// 메시지 타입별 핸들러를 등록합니다.
    /// </summary>
    public void RegisterHandler(string messageType, Action<string> handler)
    {
      if (!messageHandlers.ContainsKey(messageType))
      {
        messageHandlers[messageType] = handler;
      }
      else
      {
        messageHandlers[messageType] += handler;
      }

      Debug.Log($"[WebSocket] 핸들러 등록: {messageType}");
    }

    /// <summary>
    /// 메시지 핸들러를 제거합니다.
    /// </summary>
    public void UnregisterHandler(string messageType, Action<string> handler)
    {
      if (messageHandlers.ContainsKey(messageType))
      {
        messageHandlers[messageType] -= handler;
      }
    }

    #endregion

    #region WebSocket 이벤트 핸들러 (WebSocket Event Handlers)

    private void HandleOpen()
    {
      Debug.Log("[WebSocket] 연결 성공!");
      isConnected = true;
      reconnectAttempts = 0;
      OnConnected?.Invoke();
    }

    private void HandleMessage(byte[] data)
    {
      try
      {
        string json = System.Text.Encoding.UTF8.GetString(data);

        // 메시지 타입 파싱
        var baseMessage = ES3SerializationHelper.DeserializeFromJson<WebSocketMessage>(json);

        if (baseMessage == null || string.IsNullOrEmpty(baseMessage.MessageType))
        {
          Debug.LogError($"[WebSocket] 메시지 헤더 파싱 실패! (구조나 대소문자 확인 필요)\n원본 데이터: {json}");
          return;
        }

        if (messageHandlers.ContainsKey(baseMessage.MessageType))
        {
          // 등록된 핸들러 호출
          messageHandlers[baseMessage.MessageType]?.Invoke(json);
        }
        else
        {
          Debug.LogWarning($"[WebSocket] 처리 핸들러가 없는 메시지 타입: {baseMessage.MessageType}\n원본 데이터: {json}");
        }
      }
      catch (Exception e)
      {
        Debug.LogError($"[WebSocket] 메시지 처리 실패: {e.Message}");
      }
    }

    private void HandleError(string errorMsg)
    {
      Debug.LogError($"[WebSocket] 에러: {errorMsg}");
      OnError?.Invoke(errorMsg);
    }

    private void HandleClose(WebSocketCloseCode closeCode)
    {
      Debug.Log($"[WebSocket] 연결 종료: {closeCode}");
      isConnected = false;
      OnDisconnected?.Invoke();

      // 비정상 종료 시 재연결 시도
      if (autoReconnect && closeCode != WebSocketCloseCode.Normal)
      {
        if (reconnectCoroutine != null)
          StopCoroutine(reconnectCoroutine);

        // WebSocket URL 재구성 필요
        // reconnectCoroutine = StartCoroutine(AttemptReconnect(websocketUrl));
      }
    }

    #endregion

    #region Unity Lifecycle

    private void Update()
    {
      // WebSocket 메시지 디스패치
#if !UNITY_WEBGL || UNITY_EDITOR
      websocket?.DispatchMessageQueue();
#endif

      // 주기적인 핑 전송
      if (isConnected)
      {
        lastPingTime += Time.deltaTime;
        if (lastPingTime >= pingInterval)
        {
          lastPingTime = 0f;
          SendPing();
        }
      }
    }

    private void OnApplicationQuit()
    {
      if (websocket != null)
      {
        StartCoroutine(Disconnect());
      }
    }

    private void OnDestroy()
    {
      if (websocket != null)
      {
        StartCoroutine(Disconnect());
      }
    }

    #endregion

    #region 유틸리티 (Utilities)

    /// <summary>
    /// 핑 메시지를 전송합니다.
    /// </summary>
    private void SendPing()
    {
      SendMessage("{\"MessageType\":\"Ping\",\"Timestamp\":" +
          DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}");
    }

    /// <summary>
    /// 연결 상태를 확인합니다.
    /// </summary>
    public bool CheckConnection()
    {
      if (websocket == null)
        return false;

      return websocket.State == WebSocketState.Open;
    }

    #endregion
  }
}
