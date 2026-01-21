using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeoSurvive.Network
{
  #region 플레이어 상태 (Player State)

  /// <summary>
  /// 플레이어의 현재 상태 정보
  /// WebSocket으로 실시간 동기화됩니다.
  /// </summary>
  [Serializable]
  public class PlayerState
  {
    public int playerId;
    public string nickname;
    public Vector2 position;
    public Vector2 velocity;
    public float rotation;

    // 스탯
    public float health;
    public float maxHealth;
    public int level;
    public int experience;

    // 게임 상태
    public bool isAlive;
    public long lastUpdateTimestamp; // Unix timestamp (ms)

    // 무기 및 버프
    public List<WeaponState> activeWeapons;
    public List<BuffState> activeBuffs;

    public PlayerState()
    {
      activeWeapons = new List<WeaponState>();
      activeBuffs = new List<BuffState>();
    }
  }

  /// <summary>
  /// 무기 상태
  /// </summary>
  [Serializable]
  public class WeaponState
  {
    public string weaponId;
    public string weaponType;
    public int level;
    public bool isActive;

    // 독립 무기용 위치 정보
    public Vector2? position;
    public float? rotation;
  }

  /// <summary>
  /// 버프/디버프 상태
  /// </summary>
  [Serializable]
  public class BuffState
  {
    public string buffId;
    public string buffName;
    public float duration;
    public float remainingTime;
    public int stackCount;
  }

  #endregion

  #region 적 상태 (Enemy State)

  /// <summary>
  /// 적의 상태 정보
  /// 호스트가 관리하고 클라이언트에게 전송됩니다.
  /// </summary>
  [Serializable]
  public class EnemyState
  {
    public int enemyId;
    public string enemyType;
    public Vector2 position;
    public Vector2 velocity;
    public float rotation;

    public float health;
    public float maxHealth;

    public bool isAlive;
    public int targetPlayerId; // 타겟 플레이어 ID
  }

  #endregion

  #region 게임 세션 (Game Session)

  /// <summary>
  /// 게임 세션 전체 상태
  /// 주기적으로 동기화되며 세션 저장에도 사용됩니다.
  /// </summary>
  [Serializable]
  public class GameSessionState
  {
    public int sessionId;
    public string hostPlayerId;
    public List<PlayerState> players;
    public List<EnemyState> enemies;
    public List<DropItemState> dropItems;

    // 게임 진행 상황
    public float gameTime;
    public int waveNumber;
    public int totalKills;
    public bool isGameActive;

    public long lastSyncTimestamp; // 마지막 동기화 시간

    public GameSessionState()
    {
      players = new List<PlayerState>();
      enemies = new List<EnemyState>();
      dropItems = new List<DropItemState>();
    }
  }

  /// <summary>
  /// 드랍 아이템 상태
  /// </summary>
  [Serializable]
  public class DropItemState
  {
    public int itemId;
    public string itemType; // "ExpGem", "Chest", etc.
    public Vector2 position;
    public int value; // 경험치량 등
    public bool isCollected;
  }

  #endregion

  #region WebSocket 메시지 타입 (WebSocket Message Types)

  /// <summary>
  /// WebSocket 메시지 베이스 클래스
  /// </summary>
  [Serializable]
  public abstract class WebSocketMessage
  {
    public string messageType; // "PlayerUpdate", "EnemySpawn", "ItemDrop" etc.
    public long timestamp;

    protected WebSocketMessage(string type)
    {
      messageType = type;
      timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
  }

  /// <summary>
  /// 플레이어 위치 업데이트 메시지 (빈번한 전송용 - 경량화)
  /// </summary>
  [Serializable]
  public class PlayerPositionUpdate : WebSocketMessage
  {
    public int playerId;
    public float x;
    public float y;
    public float vx; // velocity x
    public float vy; // velocity y
    public float rot; // rotation

    public PlayerPositionUpdate() : base("PlayerPosition") { }
  }

  /// <summary>
  /// 플레이어 전체 상태 업데이트 메시지
  /// </summary>
  [Serializable]
  public class PlayerStateUpdate : WebSocketMessage
  {
    public PlayerState playerState;

    public PlayerStateUpdate() : base("PlayerState") { }
  }

  /// <summary>
  /// 적 스폰 메시지
  /// </summary>
  [Serializable]
  public class EnemySpawnMessage : WebSocketMessage
  {
    public EnemyState enemy;

    public EnemySpawnMessage() : base("EnemySpawn") { }
  }

  /// <summary>
  /// 적 상태 업데이트 메시지
  /// </summary>
  [Serializable]
  public class EnemyUpdateMessage : WebSocketMessage
  {
    public List<EnemyState> enemies;

    public EnemyUpdateMessage() : base("EnemyUpdate")
    {
      enemies = new List<EnemyState>();
    }
  }

  /// <summary>
  /// 적 사망 메시지
  /// </summary>
  [Serializable]
  public class EnemyDeathMessage : WebSocketMessage
  {
    public int enemyId;
    public int killerPlayerId;

    public EnemyDeathMessage() : base("EnemyDeath") { }
  }

  /// <summary>
  /// 아이템 드랍 메시지
  /// </summary>
  [Serializable]
  public class ItemDropMessage : WebSocketMessage
  {
    public DropItemState item;

    public ItemDropMessage() : base("ItemDrop") { }
  }

  /// <summary>
  /// 아이템 수집 메시지
  /// </summary>
  [Serializable]
  public class ItemCollectMessage : WebSocketMessage
  {
    public int itemId;
    public int collectorPlayerId;

    public ItemCollectMessage() : base("ItemCollect") { }
  }

  /// <summary>
  /// 웨이브 시작 메시지
  /// </summary>
  [Serializable]
  public class WaveStartMessage : WebSocketMessage
  {
    public int waveNumber;
    public int enemyCount;

    public WaveStartMessage() : base("WaveStart") { }
  }

  /// <summary>
  /// 게임 오버 메시지
  /// </summary>
  [Serializable]
  public class GameOverMessage : WebSocketMessage
  {
    public float survivalTime;
    public int totalKills;
    public bool isCleared;

    public GameOverMessage() : base("GameOver") { }
  }

  /// <summary>
  /// 플레이어 데미지 메시지
  /// </summary>
  [Serializable]
  public class PlayerDamageMessage : WebSocketMessage
  {
    public int playerId;
    public float damage;
    public float remainingHealth;
    public int attackerId; // 공격한 적 ID

    public PlayerDamageMessage() : base("PlayerDamage") { }
  }

  /// <summary>
  /// 채팅 메시지
  /// </summary>
  [Serializable]
  public class ChatMessage : WebSocketMessage
  {
    public int senderId;
    public string senderNickname;
    public string message;

    public ChatMessage() : base("Chat") { }
  }

  #endregion

  #region HTTP API 요청/응답 (HTTP API Request/Response)

  /// <summary>
  /// 멀티플레이어 로비 세션 생성 요청
  /// </summary>
  [Serializable]
  public class CreateMultiLobbyRequest
  {
    public int hostPlayerId;
    public string characterType; // "Hacker" or "Cyborg"
    public int stage;
    public int maxPlayers; // 기본 2명
  }

  /// <summary>
  /// 멀티플레이어 로비 세션 생성 응답
  /// </summary>
  [Serializable]
  public class CreateMultiLobbyResponse
  {
    public int sessionId;
    public string websocketUrl; // WebSocket 연결 주소
    public string sessionCode; // 6자리 세션 코드
    public string createdAt; // ISO 8601 문자열 형식
  }

  /// <summary>
  /// 멀티플레이어 로비 세션 참가 요청
  /// </summary>
  [Serializable]
  public class JoinMultiLobbyRequest
  {
    public int playerId;
    public string sessionCode;
    public string characterType;
  }

  /// <summary>
  /// 멀티플레이어 로비 세션 참가 응답
  /// </summary>
  [Serializable]
  public class JoinMultiLobbyResponse
  {
    public bool success;
    public int sessionId;
    public string websocketUrl;
    public GameSessionState currentState; // 현재 세션 상태
    public string errorMessage;
  }

  /// <summary>
  /// 세션 저장 요청 (체크포인트/게임 종료 시)
  /// </summary>
  [Serializable]
  public class SaveSessionRequest
  {
    public int sessionId;
    public GameSessionState sessionState;
  }

  /// <summary>
  /// 세션 저장 응답
  /// </summary>
  [Serializable]
  public class SaveSessionResponse
  {
    public bool success;
    public string checkpointId;
    public string errorMessage;
  }

  #endregion

  #region 유틸리티 확장 (Utility Extensions)

  public static class NetworkDataExtensions
  {
    /// <summary>
    /// PlayerState를 경량화된 PlayerPositionUpdate로 변환
    /// </summary>
    public static PlayerPositionUpdate ToPositionUpdate(this PlayerState state)
    {
      return new PlayerPositionUpdate
      {
        playerId = state.playerId,
        x = state.position.x,
        y = state.position.y,
        vx = state.velocity.x,
        vy = state.velocity.y,
        rot = state.rotation
      };
    }

    /// <summary>
    /// PlayerPositionUpdate를 PlayerState에 적용
    /// </summary>
    public static void ApplyPositionUpdate(this PlayerState state, PlayerPositionUpdate update)
    {
      state.position = new Vector2(update.x, update.y);
      state.velocity = new Vector2(update.vx, update.vy);
      state.rotation = update.rot;
      state.lastUpdateTimestamp = update.timestamp;
    }
  }

  #endregion
}
