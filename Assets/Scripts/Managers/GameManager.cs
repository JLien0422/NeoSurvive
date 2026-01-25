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
  // 이번 판에서 획득한 골드 (디버깅용으로 인스펙터에 표시)
  [SerializeField]
  private int currentRunGold = 0;

  [SerializeField]
  // 저장된 총 골드
  private int totalGold = 0;
  // UI 등에서 총 골드를 참조하기 위한 public 프로퍼티
  public int TotalGold => totalGold;

  // 참조
  private Transform playerTransform;
  private const string GOLD_SAVE_KEY = "TotalGold"; // Easy Save 키

  [Header("캐릭터 선택")]
  // 선택된 캐릭터 타입
  private CharacterType selectedCharacter = CharacterType.Hacker;

  [Header("서버 연동")]
  [SerializeField] private GameServerAPI serverAPI;

  private float startTime = 0f;

  // 컴포넌트가 처음 활성화될 때 호출됩니다.
  private void Awake()
  {
    // 싱글톤 패턴 구현
    if (Instance == null)
    {
      Instance = this;
      DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않도록 설정
    }
    else
    {
      Destroy(gameObject); // 이미 인스턴스가 있다면 이 오브젝트는 파괴
    }
  }

  private async void Start()
  {
    // GameServerAPI 초기화 대기 후 데이터 로드
    await LoadTotalGoldAsync();

    // 멀티플레이 환경인지 확인
    bool isMultiplayer = UDPClient.Instance != null;

    if (isMultiplayer)
    {
      Debug.Log("[GameManager] 멀티플레이 모드: 카메라는 자동으로 LocalPlayer를 추적합니다");
      // 멀티플레이에서는 CameraController가 자동으로 LocalPlayer를 추적합니다
      // 별도의 설정이 필요 없습니다
    }
    else
    {
      Debug.Log("[GameManager] 싱글플레이 모드");
      GameObject playerObject = GameObject.FindWithTag("Player");
      if (playerObject != null)
      {
        playerTransform = playerObject.transform;

        // 카메라 타겟 설정 (싱글플레이용)
        CameraController cam = FindObjectOfType<CameraController>();
        if (cam != null) cam.SetTarget(playerTransform);

        // 맵 매니저 타겟 설정 (싱글플레이용)
        var mapManager = FindObjectOfType<NeoSurvive.UI.Map.MapManager>();
        if (mapManager != null) mapManager.SetTarget(playerTransform);
      }
      else
      {
        Debug.LogError("플레이어를 찾을 수 없습니다! 'Player' 태그가 설정되었는지 확인해주세요.");
      }
    }
  }

  // 골드를 추가하는 공용 메서드
  public void AddGold(int amount)
  {
    currentRunGold += amount;
    Debug.Log($"골드 {amount} 획득! 이번 판 총 골드: {currentRunGold}");
  }

  // 플레이어가 죽었을 때 호출될 메서드
  public void OnPlayerDeath()
  {
    totalGold += currentRunGold;
    SaveTotalGold();
    currentRunGold = 0; // 현재 판 골드 초기화
    Debug.Log($"이번 판에 얻은 골드가 총 골드에 합산되었습니다. 현재 총 골드: {totalGold}");
  }

  // 골드를 서버에 저장
  private void SaveTotalGold()
  {
    ServerSaveSystem.Save(GOLD_SAVE_KEY, totalGold);
    Debug.Log($"총 골드 {totalGold}를 서버에 저장했습니다.");
  }

  // 서버에서 골드를 불러옴 (비동기)
  private async System.Threading.Tasks.Task LoadTotalGoldAsync()
  {
    // "TotalGold" 키로 저장된 값이 있으면 불러오고, 없으면 0을 기본값으로 사용합니다.
    totalGold = await ServerSaveSystem.LoadAsync(GOLD_SAVE_KEY, 0);
    Debug.Log($"서버에서 총 골드 {totalGold}를 불러왔습니다.");
  }

  public float GetGameTime()
  {
    return Time.time - startTime;
  }

  public void AddKill()
  {
    killCount++;
    OnKillCountChanged?.Invoke(killCount);
  }

  /// <summary>
  /// 선택된 캐릭터를 설정합니다.
  /// </summary>
  public void SetSelectedCharacter(CharacterType characterType)
  {
    selectedCharacter = characterType;
    Debug.Log($"GameManager: 캐릭터 선택됨 - {characterType}");
  }

  /// <summary>
  /// 선택된 캐릭터 타입을 반환합니다.
  /// </summary>
  public CharacterType GetSelectedCharacter()
  {
    return selectedCharacter;
  }
}
