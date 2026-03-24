using System.Collections;
using UnityEngine;
using NeoSurvive.Characters;
using NeoSurvive.Network;

// GameManager 클래스는 게임의 전반적인 흐름과 상태를 관리합니다.
public class GameManager : MonoBehaviour
{
  // 싱글톤 인스턴스: 다른 스크립트에서 GameManager에 쉽게 접근할 수 있도록 합니다.
  public static GameManager Instance { get; private set; }

  // 킬 카운트 변경 알림 이벤트
  public static event System.Action<int> OnKillCountChanged;

  [Header("적 스폰 설정")]
  private int killCount = 0;

  [Header("골드 관리")]
  [SerializeField] private int currentRunGold = 0;
  [SerializeField] private int totalGold = 0;
  public int TotalGold => totalGold;

  private Transform playerTransform;
  private const string GOLD_SAVE_KEY = "TotalGold";

  [Header("캐릭터 선택")]
  private CharacterType selectedCharacter = CharacterType.Hacker;

  [Header("서버 연동")]
  [SerializeField] private GameServerAPI serverAPI;

  private float startTime = 0f;

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }
    else
    {
      Destroy(gameObject);
      return;
    }

    startTime = Time.time; // ***** 추가: async Start() 전에 기준 시간 먼저 잡기
  }

  private async void Start()
  {
    await LoadTotalGoldAsync();

    startTime = Time.time; // (기존) 여기서도 다시 잡힘 (유지)

    bool isMultiplayer = UDPClient.Instance != null;

    if (isMultiplayer)
    {
      Debug.Log("[GameManager] 멀티플레이 모드: 카메라는 자동으로 LocalPlayer를 추적합니다");
    }
    else
    {
      Debug.Log("[GameManager] 싱글플레이 모드");
      GameObject playerObject = GameObject.FindWithTag("Player");
      if (playerObject != null)
      {
        playerTransform = playerObject.transform;

        CameraController cam = FindObjectOfType<CameraController>();
        if (cam != null) cam.SetTarget(playerTransform);

        var mapManager = FindObjectOfType<MapManager>();
        if (mapManager != null) mapManager.SetTarget(playerTransform);
      }
      else
      {
        Debug.LogError("플레이어를 찾을 수 없습니다! 'Player' 태그가 설정되었는지 확인해주세요.");
      }
    }
  }

  public void AddGold(int amount)
  {
    currentRunGold += amount;
    Debug.Log($"골드 {amount} 획득! 이번 판 총 골드: {currentRunGold}");
  }

  public void OnPlayerDeath()
  {
    totalGold += currentRunGold;
    SaveTotalGold();
    currentRunGold = 0;
    Debug.Log($"이번 판에 얻은 골드가 총 골드에 합산되었습니다. 현재 총 골드: {totalGold}");
  }

  private void SaveTotalGold()
  {
    ServerSaveSystem.Save(GOLD_SAVE_KEY, totalGold);
    Debug.Log($"총 골드 {totalGold}를 서버에 저장했습니다.");
  }

  private async System.Threading.Tasks.Task LoadTotalGoldAsync()
  {
    totalGold = await ServerSaveSystem.LoadAsync(GOLD_SAVE_KEY, 0);
    Debug.Log($"서버에서 총 골드 {totalGold}를 불러왔습니다.");
  }

  public float GetGameTime()
  {
    return Time.time - startTime;
  }

  /// <summary>
  /// 서버 game_time 기준으로 로컬 타이머를 동기화합니다.
  /// </summary>
  public void SyncGameTime(float serverGameTime)
  {
    if (serverGameTime < 0f) serverGameTime = 0f;
    startTime = Time.time - serverGameTime;
  }

  public void AddKill()
  {
    killCount++;
    OnKillCountChanged?.Invoke(killCount);
  }

  public void SetSelectedCharacter(CharacterType characterType)
  {
    selectedCharacter = characterType;
    Debug.Log($"GameManager: 캐릭터 선택됨 - {characterType}");
  }

  public CharacterType GetSelectedCharacter()
  {
    return selectedCharacter;
  }

  // ============================================================
  // ***** 추가: 디버그/테스트용 시간 점프 API
  // ============================================================

  /// <summary>
  /// "경과 시간"을 강제로 elapsedSeconds로 맞춥니다.
  /// 예: 180을 넣으면 GetGameTime()이 즉시 180초 근처가 됩니다.
  /// </summary>
  public void DebugSetElapsedTime(float elapsedSeconds) // ***** 추가
  {
    startTime = Time.time - Mathf.Max(0f, elapsedSeconds);
    Debug.Log($"[GameManager] DebugSetElapsedTime => elapsed={elapsedSeconds:F1}s");
  }

  /// <summary>
  /// 현재 경과 시간에서 addSeconds만큼 앞으로 점프합니다.
  /// </summary>
  public void DebugAddTime(float addSeconds) // ***** 추가
  {
    startTime -= Mathf.Max(0f, addSeconds);
    Debug.Log($"[GameManager] DebugAddTime => +{addSeconds:F1}s");
  }
}
