using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using NeoSurvive.Network.Protocol;
using NeoSurvive.Network;

public class UDPClient : MonoBehaviour
{
  [Header("서버 설정")]
  [SerializeField] private string serverAddress = "nasdac.kro.kr";
  [SerializeField] private int serverPort = 5158;

  [Header("디버그")]
  [SerializeField] private bool showDebugLog = true;

  private UdpClient client;
  private IPEndPoint serverEndpoint;
  private bool isConnected = false;

  private void Start()
  {
    InitializeUdpClient();
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
      // 패킷 생성
      GamePacket packet = new GamePacket
      {
        PlayerId = 1, // 테스트용 하드코딩 유지
        // 서버 호환성을 위해 Ticks 대신 Milliseconds 사용 권장 (Ticks는 너무 큼)
        Timestamp = DateTimeOffset.UtcNow.Ticks,
        PosX = transform.position.x,
        PosY = transform.position.y
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
