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

  private Vector2 lastMoveDir = Vector2.right;

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
        PosY = _localPlayer.transform.position.y,
        IsDead = _localPlayer.IsDead
      };

      var rb = _localPlayer.GetComponent<Rigidbody2D>();
      if (rb != null)
      {
        move.VelX = rb.velocity.x;
        move.VelY = rb.velocity.y;

        float speed = rb.velocity.magnitude;
        if (speed > 0.001f)
        {
          lastMoveDir = rb.velocity.normalized;
        }

        move.DirX = lastMoveDir.x;
        move.DirY = lastMoveDir.y;
        move.Speed = speed;
      }

      // Wrapper 사용
      GamePacket packet = new GamePacket { PlayerMove = move };
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
          case GamePacket.PayloadOneofCase.GameSnapshot:
            HandleSnapshot(wrapper.GameSnapshot);
            break;
          case GamePacket.PayloadOneofCase.PlayerAction:
            HandleAction(wrapper.PlayerAction);
            break;
          case GamePacket.PayloadOneofCase.PlayerMove:
            // 피어 간 직접 이동 정보 수신 시 (서버 로직에 따라 다름)
            break;
          case GamePacket.PayloadOneofCase.ItemState:
            if (ItemManager.Instance != null && wrapper.ItemState != null)
            {
              ItemManager.Instance.UpdateItem(wrapper.ItemState);
            }
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

      // [Coop] 원격 플레이어 사망 상태 동기화
      if (_allPlayers.TryGetValue((int)playerId, out Player player))
      {
        if (pState.IsDead && !player.IsDead)
        {
          player.SetHealth(0);
        }
        else if (!pState.IsDead && player.IsDead)
        {
          player.Revive();
        }
      }
    }

    // [Coop] 적(Enemy) 위치 동기화 추가
    if (EnemyManager.Instance != null && snapshot.EnemyStates != null)
    {
      EnemyManager.Instance.SyncEnemies(snapshot.EnemyStates);
    }

    // [Coop] 아이템/투사체/체스트 동기화
    if (ItemManager.Instance != null && snapshot.ItemStates != null)
    {
      ItemManager.Instance.SyncItems(snapshot.ItemStates);
    }

    // [Coop] 플레이어 상태/버프 동기화
    if (snapshot.PlayerStatuses != null)
    {
      foreach (var status in snapshot.PlayerStatuses)
      {
        if (_allPlayers.TryGetValue((int)status.PlayerId, out Player player))
        {
          if (status.MaxHp <= 0)
          {
            continue;
          }

          player.SetMaxHealth(status.MaxHp, keepRatio: false);
          player.SetHealth(status.CurrentHp);

          if (status.IsDead && !player.IsDead)
          {
            player.SetHealth(0);
          }
          else if (!status.IsDead && player.IsDead)
          {
            player.Revive();
          }

          NetworkBuffSync.SyncStatus(player.gameObject, status.Buffs);
        }
      }
    }

    // [Coop] 게임 시간 동기화
    if (GameManager.Instance != null)
    {
      GameManager.Instance.SyncGameTime(snapshot.GameTime);
    }
  }

  private void HandleAction(PlayerAction action)
  {
    int myId = DBManager.Instance?.PlayerId ?? 1;
    bool isLocalAction = action.PlayerId == (uint)myId;

    switch (action.ActionType)
    {
      case ActionType.EnemyHit:
        if (EnemyManager.Instance != null)
        {
          EnemyManager.Instance.ApplyEnemyHit(action.TargetId, action.Value);
        }
        return;
      case ActionType.EnemyDead:
        if (EnemyManager.Instance != null)
        {
          EnemyManager.Instance.ApplyEnemyDead(action.TargetId);
        }
        return;
      case ActionType.PlayerHit:
        ApplyPlayerHit(action);
        return;
      case ActionType.EnemyAttack:
        ApplyPlayerHit(action);
        return;
      case ActionType.ItemPickup:
        if (ItemManager.Instance != null)
        {
          ItemManager.Instance.RemoveItem(action.TargetId);
        }
        return;
      case ActionType.ObjectInteract:
        if (ItemManager.Instance != null)
        {
          ItemManager.Instance.RemoveItem(action.TargetId);
        }
        return;
      case ActionType.BuffApply:
        ApplyBuffAction(action, isApply: true);
        return;
      case ActionType.BuffRemove:
        ApplyBuffAction(action, isApply: false);
        return;
    }

    if (_allPlayers.TryGetValue((int)action.PlayerId, out Player player))
    {
      switch (action.ActionType)
      {
        case ActionType.NeuralLink:
          if (isLocalAction) return;
          player.ExecuteNeuralLink();
          break;
        case ActionType.Attack:
          if (isLocalAction) return;
          player.ExecuteAttack((int)action.Value, new Vector2(action.DirX, action.DirY));
          break;
        case ActionType.Skill:
          if (isLocalAction) return;
          player.ExecuteAttack(GetWeaponIdFromAction(action), new Vector2(action.DirX, action.DirY));
          break;
        case ActionType.Dead:
          if (isLocalAction) return;
          player.SetHealth(0);
          break;
        case ActionType.Revive:
          if (isLocalAction) return;
          player.Revive();
          break;
        case ActionType.WeaponEquip:
          player.SyncWeaponEquip(GetWeaponIdFromAction(action));
          break;
        case ActionType.LevelUp:
          SyncWeaponLevelUp(player, GetWeaponIdFromAction(action));
          break;
        case ActionType.WeaponUse:
          if (isLocalAction) return;
          player.ExecuteAttack(GetWeaponIdFromAction(action), new Vector2(action.DirX, action.DirY));
          break;
      }
    }
  }

  private int GetWeaponIdFromAction(PlayerAction action)
  {
    if (action.Value != 0)
    {
      return (int)action.Value;
    }

    if (action.WeaponType != NeoSurvive.Network.Protocol.WeaponType.WeaponUnspecified)
    {
      return (int)action.WeaponType;
    }

    return 0;
  }

  private void ApplyPlayerHit(PlayerAction action)
  {
    uint targetId = action.TargetId != 0 ? action.TargetId : action.PlayerId;

    if (_allPlayers.TryGetValue((int)targetId, out Player player))
    {
      player.TakeDamage(action.Value);
    }
  }

  private void ApplyBuffAction(PlayerAction action, bool isApply)
  {
    uint targetId = action.TargetId != 0 ? action.TargetId : action.PlayerId;

    if (_allPlayers.TryGetValue((int)targetId, out Player player))
    {
      uint buffId = action.Value;
      uint stacks = (uint)Mathf.Max(1, Mathf.RoundToInt(action.DirY));
      float duration = action.DirX;

      if (isApply)
      {
        NetworkBuffSync.ApplyBuff(player.gameObject, buffId, duration, stacks);
      }
      else
      {
        NetworkBuffSync.RemoveBuff(player.gameObject, buffId);
      }
    }
  }

  private void SyncWeaponLevelUp(Player player, int weaponId)
  {
    if (player == null) return;

    var weaponManager = player.GetComponent<NeoSurvive.Weapon.WeaponManager>();
    if (weaponManager != null)
    {
      weaponManager.AddWeaponById(weaponId, sendToServer: false);
    }
  }

  public void SendAction(ActionType type, uint targetId = 0, Vector2 direction = default, uint value = 0, int weaponTypeId = 0)
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
        Value = value,
        WeaponType = (NeoSurvive.Network.Protocol.WeaponType)weaponTypeId
      };
      GamePacket packet = new GamePacket { PlayerAction = action };
      byte[] sendData = packet.ToByteArray();
      client.Send(sendData, sendData.Length, serverEndpoint);
    }
    catch (Exception e) { Debug.LogWarning($"[UDP 액션 실패] {e.Message}"); }
  }

  public void SendWeaponAction(ActionType type, int weaponId, Vector2 direction, Vector2 position, Action<PlayerAction> setPayload)
  {
    if (!isConnected || _localPlayer == null) return;
    try
    {
      PlayerAction action = new PlayerAction
      {
        PlayerId = (uint)(DBManager.Instance?.PlayerId ?? 1),
        ActionType = type,
        TargetId = 0,
        PosX = position.x,
        PosY = position.y,
        DirX = direction.x,
        DirY = direction.y,
        Value = 0,
        WeaponType = (NeoSurvive.Network.Protocol.WeaponType)weaponId
      };

      setPayload?.Invoke(action);

      GamePacket packet = new GamePacket { PlayerAction = action };
      byte[] sendData = packet.ToByteArray();
      client.Send(sendData, sendData.Length, serverEndpoint);
    }
    catch (Exception e) { Debug.LogWarning($"[UDP 무기 액션 실패] {e.Message}"); }
  }

  private void OnApplicationQuit() { Close(); }
  private void OnDestroy() { Close(); }
  private void Close() { isConnected = false; client?.Close(); }
}
