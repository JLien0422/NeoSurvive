using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace NeoSurvive.Network
{
  /// <summary>
  /// NeoSurvive 게임 서버 API 클라이언트
  /// </summary>
  public class GameServerAPI : MonoBehaviour
  {
    // 싱글턴 인스턴스
    public static GameServerAPI Instance { get; private set; }

    [Header("서버 설정")]
    [SerializeField] private string serverUrl = "http://nasdac.kro.kr:5157/api";

    // 현재 플레이어 정보
    private int playerId = -1;
    private int currentSessionId = -1;
    private string deviceUID;

    // PlayerId 외부 접근용
    public int PlayerId => playerId;
    public string DeviceUID => deviceUID;

    // 이벤트
    public event Action<LoginResponse> OnLoginSuccess;
    public event Action<string> OnLoginFailed;
    public event Action<GameEndResponse> OnGameEnded;

    private void Awake()
    {
      // 싱글턴 설정
      if (Instance == null)
      {
        Instance = this;
        DontDestroyOnLoad(gameObject);
      }
      else
      {
        Destroy(gameObject);
        return;
      }

      // 디바이스 UID 생성 또는 로드
      deviceUID = GetOrCreateDeviceUID();
    }

    private void Start()
    {
      // 게임 시작 시 자동 로그인
      StartCoroutine(Login());
    }

    #region 인증 (Auth)

    /// <summary>
    /// 로그인 또는 회원가입
    /// </summary>
    public IEnumerator Login()
    {
      var loginData = new LoginRequest { deviceUID = deviceUID };
      string json = JsonConvert.SerializeObject(loginData);

      using (UnityWebRequest request = CreatePostRequest("/auth/login", json))
      {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
          var response = JsonConvert.DeserializeObject<LoginResponse>(request.downloadHandler.text);
          playerId = response.playerId;

          Debug.Log($"✅ 로그인 성공! PlayerId: {playerId}, DeviceUID: {deviceUID}");
          Debug.Log($"Level: {response.level}, Gold: {response.gold}, Gems: {response.gems}");

          OnLoginSuccess?.Invoke(response);
        }
        else
        {
          Debug.LogError($"❌ 로그인 실패: {request.error}");
          OnLoginFailed?.Invoke(request.error);
        }
      }
    }

    /// <summary>
    /// 플레이어 정보 조회
    /// </summary>
    public IEnumerator GetPlayerInfo(Action<PlayerInfo> onSuccess = null)
    {
      using (UnityWebRequest request = CreateGetRequest($"/auth/player/{playerId}"))
      {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
          var playerInfo = JsonConvert.DeserializeObject<PlayerInfo>(request.downloadHandler.text);
          Debug.Log($"플레이어 정보: {playerInfo.nickname} (Lv.{playerInfo.level})");
          onSuccess?.Invoke(playerInfo);
        }
        else
        {
          Debug.LogError($"플레이어 정보 조회 실패: {request.error}");
        }
      }
    }

    /// <summary>
    /// 닉네임 변경
    /// </summary>
    public IEnumerator ChangeNickname(string newNickname, Action<ChangeNicknameResponse> onSuccess = null)
    {
      var nicknameData = new ChangeNicknameRequest { nickname = newNickname };
      string json = JsonConvert.SerializeObject(nicknameData);

      using (UnityWebRequest request = CreatePutRequest($"/auth/player/{playerId}/nickname", json))
      {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
          var response = JsonConvert.DeserializeObject<ChangeNicknameResponse>(request.downloadHandler.text);

          if (response.success)
          {
            Debug.Log($"✅ 닉네임 변경 성공: {response.newNickname}");
          }
          else
          {
            Debug.LogWarning($"⚠️ 닉네임 변경 실패: {response.error}");
          }

          onSuccess?.Invoke(response);
        }
        else
        {
          Debug.LogError($"❌ 닉네임 변경 요청 실패: {request.error}");
        }
      }
    }

    #endregion

    #region 게임 (Game)

    /// <summary>
    /// 게임 세션 시작 (캐릭터 타입 포함)
    /// </summary>
    public IEnumerator StartGame(NeoSurvive.Characters.CharacterType characterType, int stage = 1)
    {
      if (playerId < 0)
      {
        Debug.LogError("로그인이 필요합니다!");
        yield break;
      }

      var startData = new GameStartRequest
      {
        playerId = playerId,
        characterType = characterType.ToString(),  // "Hacker" or "Cyborg"
        stage = stage
      };
      string json = JsonConvert.SerializeObject(startData);

      using (UnityWebRequest request = CreatePostRequest("/game/start", json))
      {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
          var response = JsonConvert.DeserializeObject<GameStartResponse>(request.downloadHandler.text);
          currentSessionId = response.sessionId;
          Debug.Log($"🎮 게임 시작! SessionId: {currentSessionId}, Character: {characterType}, Stage: {stage}");
        }
        else
        {
          Debug.LogError($"게임 시작 실패: {request.error}");
        }
      }
    }

    /// <summary>
    /// 게임 세션 종료
    /// </summary>
    public IEnumerator EndGame(int survivalTime, int enemiesKilled, bool isCleared)
    {
      if (currentSessionId < 0)
      {
        Debug.LogError("진행 중인 게임 세션이 없습니다!");
        yield break;
      }

      var endData = new GameEndRequest
      {
        sessionId = currentSessionId,
        survivalTime = survivalTime,
        enemiesKilled = enemiesKilled,
        isCleared = isCleared
      };
      string json = JsonConvert.SerializeObject(endData);

      using (UnityWebRequest request = CreatePostRequest("/game/end", json))
      {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
          var response = JsonConvert.DeserializeObject<GameEndResponse>(request.downloadHandler.text);

          Debug.Log($"🏆 게임 종료!");
          Debug.Log($"💰 골드 획득: {response.goldEarned} (총: {response.totalGold})");
          Debug.Log($"⭐ 경험치 획득: {response.experienceEarned}");

          if (response.leveledUp)
          {
            Debug.Log($"🎉 레벨업! Lv.{response.currentLevel}");
          }

          if (response.newRecord)
          {
            Debug.Log("🎊 신기록 달성!");
          }

          OnGameEnded?.Invoke(response);
          currentSessionId = -1;
        }
        else
        {
          Debug.LogError($"게임 종료 처리 실패: {request.error}");
        }
      }
    }

    /// <summary>
    /// 리더보드 조회
    /// </summary>
    public IEnumerator GetLeaderboard(int top = 10, Action<List<LeaderboardEntry>> onSuccess = null)
    {
      using (UnityWebRequest request = CreateGetRequest($"/game/leaderboard?top={top}"))
      {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
          var leaderboard = JsonConvert.DeserializeObject<List<LeaderboardEntry>>(request.downloadHandler.text);
          Debug.Log($"📊 리더보드 ({leaderboard.Count}명):");
          foreach (var entry in leaderboard)
          {
            Debug.Log($"  {entry.rank}. {entry.nickname} - {entry.bestSurvivalTime}초 (Lv.{entry.level})");
          }
          onSuccess?.Invoke(leaderboard);
        }
        else
        {
          Debug.LogError($"리더보드 조회 실패: {request.error}");
        }
      }
    }

    #endregion

    #region 무기 (Weapon)

    /// <summary>
    /// 플레이어 무기 목록 조회
    /// </summary>
    public IEnumerator GetWeapons(Action<List<WeaponDTO>> onSuccess = null)
    {
      using (UnityWebRequest request = CreateGetRequest($"/weapon/player/{playerId}"))
      {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
          var weapons = JsonConvert.DeserializeObject<List<WeaponDTO>>(request.downloadHandler.text);
          Debug.Log($"🔫 보유 무기 ({weapons.Count}개):");
          foreach (var weapon in weapons)
          {
            Debug.Log($"  - {weapon.weaponType} (Lv.{weapon.level})");
          }
          onSuccess?.Invoke(weapons);
        }
        else
        {
          Debug.LogError($"무기 목록 조회 실패: {request.error}");
        }
      }
    }

    /// <summary>
    /// 무기 업그레이드
    /// </summary>
    public IEnumerator UpgradeWeapon(int weaponId, Action<WeaponUpgradeResponse> onSuccess = null)
    {
      var upgradeData = new WeaponUpgradeRequest { weaponId = weaponId };
      string json = JsonConvert.SerializeObject(upgradeData);

      using (UnityWebRequest request = CreatePostRequest($"/weapon/player/{playerId}/upgrade", json))
      {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
          var response = JsonConvert.DeserializeObject<WeaponUpgradeResponse>(request.downloadHandler.text);

          if (response.success)
          {
            Debug.Log($"⚡ 업그레이드 성공! 새 레벨: {response.newLevel}");
            Debug.Log($"💰 소모 골드: {response.goldSpent}, 남은 골드: {response.remainingGold}");
          }
          else
          {
            Debug.LogWarning($"업그레이드 실패: {response.errorMessage}");
          }

          onSuccess?.Invoke(response);
        }
        else
        {
          Debug.LogError($"무기 업그레이드 실패: {request.error}");
        }
      }
    }

    /// <summary>
    /// 새 무기 획득
    /// </summary>
    public IEnumerator AcquireWeapon(string weaponType, Action<WeaponDTO> onSuccess = null)
    {
      string json = JsonConvert.SerializeObject(weaponType);

      using (UnityWebRequest request = CreatePostRequest($"/weapon/player/{playerId}/acquire", json))
      {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
          var weapon = JsonConvert.DeserializeObject<WeaponDTO>(request.downloadHandler.text);
          Debug.Log($"🎁 새 무기 획득! {weapon.weaponType} (Lv.{weapon.level})");
          onSuccess?.Invoke(weapon);
        }
        else
        {
          Debug.LogError($"무기 획득 실패: {request.error}");
        }
      }
    }

    #endregion

    #region 업그레이드 (Upgrades)

    /// <summary>
    /// 플레이어 업그레이드 목록 조회
    /// </summary>
    public IEnumerator GetPlayerUpgrades(Action<UpgradeListResponse> onSuccess = null)
    {
      using (UnityWebRequest request = CreateGetRequest($"/upgrade/player/{playerId}"))
      {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
          var response = JsonConvert.DeserializeObject<UpgradeListResponse>(request.downloadHandler.text);
          Debug.Log($"🔧 업그레이드 목록 로드 완료: {response.upgrades.Count}개 항목");
          onSuccess?.Invoke(response);
        }
        else
        {
          Debug.LogError($"업그레이드 목록 조회 실패: {request.error}");
        }
      }
    }

    /// <summary>
    /// 업그레이드 구매
    /// </summary>
    public IEnumerator PurchasePlayerUpgrade(string upgradeId, Action<UpgradePurchaseResponse> onSuccess = null)
    {
      var purchaseData = new UpgradePurchaseRequest { upgradeId = upgradeId };
      string json = JsonConvert.SerializeObject(purchaseData);

      using (UnityWebRequest request = CreatePostRequest($"/upgrade/player/{playerId}/purchase", json))
      {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
          var response = JsonConvert.DeserializeObject<UpgradePurchaseResponse>(request.downloadHandler.text);

          if (response.success)
          {
            Debug.Log($"✅ 업그레이드 구매 성공: {upgradeId} -> Lv.{response.newLevel}");
          }
          else
          {
            Debug.LogWarning($"⚠️ 업그레이드 구매 실패: {response.errorMessage}");
          }

          onSuccess?.Invoke(response);
        }
        else
        {
          Debug.LogError($"업그레이드 구매 요청 에러: {request.error}");
        }
      }
    }

    #endregion

    #region 유틸리티

    /// <summary>
    /// 디바이스 UID 생성 또는 로드
    /// </summary>
    private string GetOrCreateDeviceUID()
    {
      string key = "DeviceUID";
      if (!PlayerPrefs.HasKey(key))
      {
        string newUID = SystemInfo.deviceUniqueIdentifier;
        if (string.IsNullOrEmpty(newUID))
        {
          newUID = Guid.NewGuid().ToString();
        }
        PlayerPrefs.SetString(key, newUID);
        PlayerPrefs.Save();
        Debug.Log($"새 디바이스 UID 생성: {newUID}");
      }
      return PlayerPrefs.GetString(key);
    }

    /// <summary>
    /// GET 요청 생성
    /// </summary>
    private UnityWebRequest CreateGetRequest(string endpoint)
    {
      var request = UnityWebRequest.Get(serverUrl + endpoint);
      request.SetRequestHeader("Content-Type", "application/json");
      return request;
    }

    /// <summary>
    /// POST 요청 생성 (Public - NetworkManager와 ServerSaveSystem에서 사용)
    /// </summary>
    public UnityWebRequest CreatePostRequest(string endpoint, string json)
    {
      var request = new UnityWebRequest(serverUrl + endpoint, "POST");
      byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
      request.uploadHandler = new UploadHandlerRaw(bodyRaw);
      request.downloadHandler = new DownloadHandlerBuffer();
      request.SetRequestHeader("Content-Type", "application/json");
      return request;
    }

    /// <summary>
    /// PUT 요청 생성
    /// </summary>
    private UnityWebRequest CreatePutRequest(string endpoint, string json)
    {
      var request = new UnityWebRequest(serverUrl + endpoint, "PUT");
      byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
      request.uploadHandler = new UploadHandlerRaw(bodyRaw);
      request.downloadHandler = new DownloadHandlerBuffer();
      request.SetRequestHeader("Content-Type", "application/json");
      return request;
    }

    #endregion
  }

  #region DTO 클래스들

  // 인증
  [Serializable]
  public class LoginRequest
  {
    public string deviceUID;
  }

  [Serializable]
  public class LoginResponse
  {
    public int playerId;
    public string nickname;
    public int level;
    public int experience;
    public int gold;
    public int gems;
    public int highestStage;
    public int bestSurvivalTime;
    public bool isNewPlayer;
  }

  [Serializable]
  public class PlayerInfo
  {
    public int id;
    public string nickname;
    public int level;
    public int experience;
    public int gold;
    public int gems;
    public int highestStage;
    public int bestSurvivalTime;
  }

  // 게임
  [Serializable]
  public class GameStartRequest
  {
    public int playerId;
    public string characterType;  // "Hacker" or "Cyborg"
    public int stage;
  }

  [Serializable]
  public class GameStartResponse
  {
    public int sessionId;
    public DateTime startedAt;
  }

  [Serializable]
  public class GameEndRequest
  {
    public int sessionId;
    public int survivalTime;
    public int enemiesKilled;
    public bool isCleared;
  }

  [Serializable]
  public class GameEndResponse
  {
    public int goldEarned;
    public int experienceEarned;
    public bool leveledUp;
    public int currentLevel;
    public int currentExperience;
    public int totalGold;
    public bool newRecord;
  }

  [Serializable]
  public class LeaderboardEntry
  {
    public int rank;
    public string nickname;
    public int level;
    public int bestSurvivalTime;
    public int highestStage;
  }

  // 무기
  [Serializable]
  public class WeaponDTO
  {
    public int id;
    public string weaponType;
    public int level;
    public DateTime acquiredAt;
  }

  [Serializable]
  public class WeaponUpgradeRequest
  {
    public int weaponId;
  }

  [Serializable]
  public class WeaponUpgradeResponse
  {
    public bool success;
    public int newLevel;
    public int goldSpent;
    public int remainingGold;
    public string errorMessage;
  }

  // 닉네임 변경
  [Serializable]
  public class ChangeNicknameRequest
  {
    public string nickname;
  }

  [Serializable]
  public class ChangeNicknameResponse
  {
    public bool success;
    public string newNickname;
    public string error;
  }

  // 업그레이드 관련 DTO
  [Serializable]
  public class UpgradeListResponse
  {
    public List<UpgradeDTO> upgrades;
    public int playerGold;
  }

  [Serializable]
  public class UpgradeDTO
  {
    public string id;           // "ATK"
    public string displayName;  // "공격력"
    public int currentLevel;
    public int maxLevel;        // 0 = 무제한
    public int baseCost;
    public int costPerLevel;
    public int nextCost;
    public float valuePerLevel;
    public float currentValue;
  }

  [Serializable]
  public class UpgradePurchaseRequest
  {
    public string upgradeId;
  }

  [Serializable]
  public class UpgradePurchaseResponse
  {
    public bool success;
    public string upgradeId;
    public int newLevel;
    public int goldSpent;
    public int remainingGold;
    public float newValue;
    public string errorMessage;
  }

  #endregion
}
