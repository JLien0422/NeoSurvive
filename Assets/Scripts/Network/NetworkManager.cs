using System;
using UnityEngine;

namespace NeoSurvive.Network
{
  /// <summary>
  /// 네트워크 통합 관리자
  /// DBManager(HTTP), MultiLobbyManager(WebSocket), CoopManager(UDP) 3개의 매니저를 통합 관리합니다.
  /// </summary>
  public class NetworkManager : MonoBehaviour
  {
    public static NetworkManager Instance { get; private set; }

    [Header("Network Managers")]
    [SerializeField] private DBManager dbManager;
    [SerializeField] private MultiLobbyManager multiLobbyManager;
    [SerializeField] private CoopManager coopManager;

    // 퍼블릭 접근자
    public DBManager DB => dbManager;
    public MultiLobbyManager Lobby => multiLobbyManager;
    public CoopManager Coop => coopManager;

    // 레거시 호환성 (기존 코드가 참조하는 경우를 위해)
    [Obsolete("Use DB.PlayerId instead")]
    public int PlayerId => dbManager?.PlayerId ?? -1;

    [Obsolete("Use Lobby.IsInLobby instead")]
    public bool IsInMultiLobby => multiLobbyManager?.IsInLobby ?? false;

    [Obsolete("Use Lobby.SessionCode instead")]
    public string SessionCode => multiLobbyManager?.SessionCode ?? "";

    private void Awake()
    {
      if (Instance == null)
      {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeManagers();
      }
      else
      {
        Destroy(gameObject);
      }
    }

    private void InitializeManagers()
    {
      // 각 매니저 자동 연결 또는 생성
      dbManager = dbManager ?? GetComponent<DBManager>() ?? gameObject.AddComponent<DBManager>();
      multiLobbyManager = multiLobbyManager ?? GetComponent<MultiLobbyManager>() ?? gameObject.AddComponent<MultiLobbyManager>();
      coopManager = coopManager ?? GetComponent<CoopManager>() ?? gameObject.AddComponent<CoopManager>();

      Debug.Log("[NetworkManager] 3계층 네트워크 매니저 초기화 완료");
      Debug.Log("  - DBManager (HTTP): DB 및 계정 관리");
      Debug.Log("  - MultiLobbyManager (WebSocket): 로비 및 대기실");
      Debug.Log("  - CoopManager (UDP): 실시간 게임 동기화");
    }

    /// <summary>
    /// WebSocket URL 포맷팅 유틸리티 (MultiLobbyManager가 사용)
    /// </summary>
    public string FormatWebSocketUrl(string originalUrl, string characterType)
    {
      if (string.IsNullOrEmpty(originalUrl)) return originalUrl;

      string formattedUrl = originalUrl.Replace("localhost", "nasdac.kro.kr");

      if (formattedUrl.Contains("?"))
      {
        int queryIndex = formattedUrl.IndexOf("?");
        formattedUrl = formattedUrl.Substring(0, queryIndex);
      }

      string nickname = $"Player_{dbManager.PlayerId}";
      formattedUrl += $"?playerId={dbManager.PlayerId}&nickname={nickname}&characterType={characterType}";

      return formattedUrl;
    }
  }
}
