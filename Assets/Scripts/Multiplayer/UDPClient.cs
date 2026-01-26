using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using NeoSurvive.Network.Protocol;
using NeoSurvive.Network;
using System.Collections.Generic;

public class UDPClient : MonoBehaviour
{
  public static UDPClient Instance { get; private set; }

  [Header("서버 설정")]
  [SerializeField] private string serverAddress = "nasdac.kro.kr";
  [SerializeField] private int serverPort = 5158;

  [Header("디버그")]
  [SerializeField] private bool showDebugLog = true;

  private UdpClient client;
  private IPEndPoint serverEndpoint;
  private bool isConnected = false;

  // 플레이어 관리
  private Dictionary<int, Player> _allPlayers = new Dictionary<int, Player>();
  private Player _localPlayer;

  private class RemotePlayerData
  {
    public Vector2 targetPosition;
    public long lastUpdateTime;
  }
  private Dictionary<uint, RemotePlayerData> remotePlayerData = new Dictionary<uint, RemotePlayerData>();
  [SerializeField] private float lerpSpeed = 10f;

  public Player LocalPlayer => _localPlayer;

  private void Awake()
  {
    if (Instance == null) Instance = this;
    else Destroy(gameObject);
  }

  private void Start()
  {
    InitializeUdpClient();
    SendPlayerPosition();
    ReceiveDataLoop();
  }

  public void RegisterPlayer(int id, Player player, bool isLocal)
  {
    if (!_allPlayers.ContainsKey(id))
    {
      _allPlayers[id] = player;
      player.IsLocal = isLocal;
      if (isLocal) _localPlayer = player;
      Debug.Log($"[UDP] 플레이어 등록: ID {id}, Local: {isLocal}");
    }
  }

  private void InitializeUdpClient()
  {
    try
    {
      client = new UdpClient();
      IPAddress serverIP = ResolveServerAddress(serverAddress);
      if (serverIP == null) return;
      serverEndpoint = new IPEndPoint(serverIP, serverPort);
      isConnected = true;
      Debug.Log($"[UDP] 서버 연결 완료: {serverIP}:{serverPort}");
    }
    catch (Exception e)
    {
      Debug.LogError($"[UDP] 초기화 실패: {e.Message}");
    }
  }

  private IPAddress ResolveServerAddress(string address)
  {
    try
    {
      if (IPAddress.TryParse(address, out IPAddress ip)) return ip;
      IPAddress[] addresses = Dns.GetHostAddresses(address);
      return (addresses.Length > 0) ? addresses[0] : null;
    }
    catch { return null; }
  }

  private void Update()
  {
    UpdateRemotePlayerPositions();
  }

  private void UpdateRemotePlayerPositions()
  {
    foreach (var kvp in remotePlayerData)
    {
      if (_allPlayers.TryGetValue((int)kvp.Key, out Player player))
      {
        if (player == _localPlayer) continue;
        Vector3 currentPos = player.transform.position;
        Vector3 targetPos = new Vector3(kvp.Value.targetPosition.x, kvp.Value.targetPosition.y, currentPos.z);
        player.transform.position = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * lerpSpeed);
      }
    }
  }

  private void SendPlayerData()
  {
    if (!isConnected || _localPlayer == null) return;

    try
    {
      int myId = DBManager.Instance?.PlayerId ?? 1;
      PlayerMove move = new PlayerMove
      {
        PlayerId = (uint)myId,
        Timestamp = DateTimeOffset.UtcNow.Ticks,
        PosX = _localPlayer.transform.position.x,
        PosY = _localPlayer.transform.position.y
      };

      var rb = _localPlayer.GetComponent<Rigidbody2D>();
      if (rb != null)
      {
        move.VelX = rb.velocity.x;
        move.VelY = rb.velocity.y;
      }

      // Wrapper 사용
      GamePacket packet = new GamePacket { Move = move };
      byte[] sendData = packet.ToByteArray();
      client.Send(sendData, sendData.Length, serverEndpoint);
    }
    catch (Exception e)
    {
      Debug.LogError($"[UDP] 전송 실패: {e.Message}");
    }
  }

  private async void SendPlayerPosition()
  {
    while (isConnected)
    {
      SendPlayerData();
      await System.Threading.Tasks.Task.Delay(33);
    }
  }

  private async void ReceiveDataLoop()
  {
    while (isConnected)
    {
      try
      {
        UdpReceiveResult result = await client.ReceiveAsync();
        byte[] data = result.Buffer;

        // 래퍼 파싱
        GamePacket wrapper = GamePacket.Parser.ParseFrom(data);
        switch (wrapper.PayloadCase)
        {
          case GamePacket.PayloadOneofCase.Snapshot:
            HandleSnapshot(wrapper.Snapshot);
            break;
          case GamePacket.PayloadOneofCase.Action:
            HandleAction(wrapper.Action);
            break;
          case GamePacket.PayloadOneofCase.Move:
            // 피어 간 직접 이동 정보 수신 시 (서버 로직에 따라 다름)
            break;
        }
      }
      catch (Exception e)
      {
        if (isConnected) // 연결 종료 시 발생하는 예외는 무시
          Debug.LogError($"[UDP 수신 오류] {e.Message}");
      }
    }
  }

  private void HandleSnapshot(GameSnapshot snapshot)
  {
    if (snapshot == null || snapshot.PlayerStates == null) return;

    foreach (var pState in snapshot.PlayerStates)
    {
      uint playerId = pState.PlayerId;

      if (!remotePlayerData.ContainsKey(playerId))
        remotePlayerData[playerId] = new RemotePlayerData();

      remotePlayerData[playerId].targetPosition = new Vector2(pState.PosX, pState.PosY);
      remotePlayerData[playerId].lastUpdateTime = pState.Timestamp;

      // 디버그 로그 (선택 사항)
      // if (showDebugLog) Debug.Log($"[UDP] Player {playerId} 위치 수신");
    }

    // [Coop] 적(Enemy) 위치 동기화 추가
    if (EnemyManager.Instance != null && snapshot.EnemyStates != null)
    {
      EnemyManager.Instance.SyncEnemies(snapshot.EnemyStates);
    }
  }

  private void HandleAction(PlayerAction action)
  {
    int myId = DBManager.Instance?.PlayerId ?? 1;
    if (action.PlayerId == (uint)myId) return;

    if (_allPlayers.TryGetValue((int)action.PlayerId, out Player player))
    {
      switch (action.ActionType)
      {
        case ActionType.NeuralLink:
          player.ExecuteNeuralLink();
          break;
        case ActionType.Attack:
          player.ExecuteAttack((int)action.Value, new Vector2(action.DirX, action.DirY));
          break;
      }
    }
  }

  public void SendAction(ActionType type, uint targetId = 0, Vector2 direction = default, uint value = 0)
  {
    if (!isConnected || _localPlayer == null) return;
    try
    {
      PlayerAction action = new PlayerAction
      {
        PlayerId = (uint)(DBManager.Instance?.PlayerId ?? 1),
        ActionType = type,
        TargetId = targetId,
        PosX = _localPlayer.transform.position.x,
        PosY = _localPlayer.transform.position.y,
        DirX = direction.x,
        DirY = direction.y,
        Value = value
      };
      GamePacket packet = new GamePacket { Action = action };
      byte[] sendData = packet.ToByteArray();
      client.Send(sendData, sendData.Length, serverEndpoint);
    }
    catch (Exception e) { Debug.LogWarning($"[UDP 액션 실패] {e.Message}"); }
  }

  private void OnApplicationQuit() { Close(); }
  private void OnDestroy() { Close(); }
  private void Close() { isConnected = false; client?.Close(); }
}
