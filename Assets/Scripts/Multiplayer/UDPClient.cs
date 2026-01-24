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

  // 플레이어 관리 (ID별 매핑)
  private Dictionary<int, Player> _allPlayers = new Dictionary<int, Player>();
  private Player _localPlayer;

  // 원격 플레이어 위치 동기화
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
    if (Instance == null)
      Instance = this;
    else
      Destroy(gameObject);
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
      if (isLocal) _localPlayer = player;
      Debug.Log($"[UDP] 플레이어 등록 완료: ID {id}, Local: {isLocal}");
    }
  }

  public Player GetPlayer(int id)
  {
    return _allPlayers.TryGetValue(id, out var p) ? p : null;
  }

  private void InitializeUdpClient()
  {
    try
    {
      // UDP 클라이언트 생성
      client = new UdpClient();

      // DNS 주소를 IP로 변환
      IPAddress serverIP = ResolveServerAddress(serverAddress);

      if (serverIP == null)
      {
        Debug.LogError($"[UDP] 서버 주소 '{serverAddress}'를 IP로 변환할 수 없습니다!");
        return;
      }

      // 서버 엔드포인트 설정
      serverEndpoint = new IPEndPoint(serverIP, serverPort);
      isConnected = true;

      if (showDebugLog)
        Debug.Log($"[UDP] 서버 연결 준비 완료: {serverIP}:{serverPort}");
    }
    catch (Exception e)
    {
      Debug.LogError($"[UDP] 초기화 실패: {e.Message}");
      isConnected = false;
    }
  }

  /// <summary>
  /// DNS 주소를 IP 주소로 변환
  /// </summary>
  private IPAddress ResolveServerAddress(string address)
  {
    try
    {
      // IP 주소인지 확인
      if (IPAddress.TryParse(address, out IPAddress ipAddress))
      {
        return ipAddress;
      }

      // DNS 주소면 IP로 변환
      IPAddress[] addresses = Dns.GetHostAddresses(address);

      if (addresses.Length > 0)
      {
        // IPv4 주소 우선 사용
        foreach (var addr in addresses)
        {
          if (addr.AddressFamily == AddressFamily.InterNetwork)
          {
            if (showDebugLog)
              Debug.Log($"[UDP] DNS 변환: {address} -> {addr}");
            return addr;
          }
        }

        // IPv4가 없으면 첫 번째 주소 사용
        return addresses[0];
      }

      Debug.LogError($"[UDP] '{address}' 주소를 찾을 수 없습니다!");
      return null;
    }
    catch (Exception e)
    {
      Debug.LogError($"[UDP] DNS 변환 실패: {e.Message}");
      return null;
    }
  }

  private void Update()
  {
    if (Input.GetKeyDown(KeyCode.Space))
    {
      SendPlayerData();
    }

    // 원격 플레이어 위치 보간
    UpdateRemotePlayerPositions();
  }

  private void UpdateRemotePlayerPositions()
  {
    foreach (var kvp in remotePlayerData)
    {
      uint playerId = kvp.Key;
      RemotePlayerData data = kvp.Value;

      // 해당 플레이어 객체 찾기
      if (_allPlayers.TryGetValue((int)playerId, out Player player))
      {
        // 로컬 플레이어는 스킵
        if (player == _localPlayer) continue;

        // Lerp로 부드럽게 이동
        Vector3 currentPos = player.transform.position;
        Vector3 targetPos = new Vector3(data.targetPosition.x, data.targetPosition.y, currentPos.z);
        player.transform.position = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * lerpSpeed);
      }
    }
  }

  void SendPlayerData()
  {
    if (!isConnected || client == null || serverEndpoint == null)
    {
      Debug.LogWarning("[UDP] 서버가 연결되지 않았습니다. 재연결을 시도합니다...");
      InitializeUdpClient();
      return;
    }

    try
    {
      if (_localPlayer == null) return;

      int myId = DBManager.Instance?.PlayerId ?? 1;

      // 패킷 생성
      GamePacket packet = new GamePacket
      {
        PlayerId = (uint)myId,
        Timestamp = DateTimeOffset.UtcNow.Ticks,
        PosX = _localPlayer.transform.position.x,
        PosY = _localPlayer.transform.position.y
      };

      // 직렬화
      byte[] sendData = packet.ToByteArray();

      // [중요] UDP 송신 주소 재확인 및 전송
      int sentBytes = client.Send(sendData, sendData.Length, serverEndpoint);

      if (showDebugLog)
        Debug.Log($"[UDP] 패킷 송신 시도: {sentBytes} bytes -> {serverEndpoint.Address}:{serverEndpoint.Port}");
    }
    catch (Exception e)
    {
      Debug.LogError($"[UDP] 전송 예외 발생: {e.Message}");
      isConnected = false;
    }
  }

  private async void SendPlayerPosition()
  {
    while (isConnected)
    {
      SendPlayerData();
      await System.Threading.Tasks.Task.Delay(33); // 30 Hz
    }
  }

  async void ReceiveDataLoop()
  {
    while (isConnected)
    {
      try
      {
        UdpReceiveResult result = await client.ReceiveAsync();
        byte[] receivedData = result.Buffer;

        // 수신된 데이터 처리
        GameSnapshot snapshot = GameSnapshot.Parser.ParseFrom(receivedData);

        // 모든 플레이어 상태 업데이트
        foreach (var playerState in snapshot.PlayerStates)
        {
          uint playerId = playerState.PlayerId;

          // 원격 플레이어 데이터 업데이트
          if (!remotePlayerData.ContainsKey(playerId))
          {
            remotePlayerData[playerId] = new RemotePlayerData();
          }

          remotePlayerData[playerId].targetPosition = new Vector2(playerState.PosX, playerState.PosY);
          remotePlayerData[playerId].lastUpdateTime = playerState.Timestamp;
        }

        if (showDebugLog)
        {
          Debug.Log($"[UDP] 스냅샷 수신: TimeStamp={snapshot.Timestamp}, 플레이어 수={snapshot.PlayerStates.Count}");
        }
      }
      catch (Exception e)
      {
        Debug.LogError($"[UDP] 수신 예외 발생: {e.Message}");
        isConnected = false;
      }
    }
  }

  private void OnApplicationQuit()
  {
    CloseConnection();
  }

  private void OnDestroy()
  {
    CloseConnection();
  }

  private void CloseConnection()
  {
    if (client != null)
    {
      try
      {
        client.Close();
        if (showDebugLog)
          Debug.Log("[UDP] 연결 종료");
      }
      catch (Exception e)
      {
        Debug.LogError($"[UDP] 연결 종료 실패: {e.Message}");
      }
      finally
      {
        client = null;
        isConnected = false;
      }
    }
  }
}
