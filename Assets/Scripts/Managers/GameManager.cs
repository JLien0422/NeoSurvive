using System.Collections;
using UnityEngine;
using NeoSurvive.Characters;
using NeoSurvive.Network;

// GameManager 클래스는 게임의 전반적인 흐름과 상태를 관리합니다.
// (예: 게임 시작, 종료, 점수 관리, 적 생성 등)
public class GameManager : MonoBehaviour
{
  public static GameManager Instance { get; private set; }

  // 킬 카운트 변경 알림 이벤트
  public static event System.Action<int> OnKillCountChanged;

  [Header("적 스폰 설정")]
  private int killCount = 0;

  [Header("골드 관리")]
  // 이번 판에서 획득한 골드
  [SerializeField]
  private int currentRunGold = 0;
  // 저장된 총 골드
  private int totalGold = 0;

  [Header("캐릭터 선택")]
  // 선택된 캐릭터 타입
  private CharacterType selectedCharacter = CharacterType.Hacker;

  [Header("서버 연동")]
  [SerializeField] private GameServerAPI serverAPI;
  private int currentSessionId = -1;

  // 참조
  private Transform playerTransform;
  private const string GOLD_SAVE_KEY = "TotalGold"; // PlayerPrefs 키

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

  private float startTime = 0f;

  // 게임이 시작될 때 한 번 호출됩니다.
  private void Start()
  {
    startTime = Time.time;
  }

  public float GetGameTime()
  {
    return Time.time - startTime;
  }

  // 골드를 추가하는 공용 메서드
  public void AddGold(int amount)
  {
    currentRunGold += amount;
    // Debug.Log($"골드 {amount} 획득! 이번 판 총 골드: {currentRunGold}");
  }

  // 플레이어가 죽었을 때 호출될 메서드
  public void OnPlayerDeath()
  {
    // 서버에 게임 종료 전송
    if (serverAPI != null && currentSessionId >= 0)
    {
      float survivalTime = GetGameTime();
      StartCoroutine(serverAPI.EndGame(
        survivalTime: Mathf.RoundToInt(survivalTime),
        enemiesKilled: killCount,
        isCleared: false
      ));
    }
    else
    {
      // 오프라인 모드: 로컬에 골드 저장
      totalGold += currentRunGold;
      SaveGold();
      currentRunGold = 0;
      Debug.Log($"이번 판에 얻은 골드가 총 골드에 합산되었습니다. 현재 총 골드: {totalGold}");
    }
  }

  // 골드를 PlayerPrefs에 저장
  private void SaveGold()
  {
    PlayerPrefs.SetInt(GOLD_SAVE_KEY, totalGold);
    PlayerPrefs.Save(); // 변경사항을 디스크에 즉시 저장
    Debug.Log($"총 골드 {totalGold}를 저장했습니다.");
  }

  // PlayerPrefs에서 골드를 불러옴
  private void LoadGold()
  {
    totalGold = PlayerPrefs.GetInt(GOLD_SAVE_KEY, 0); // 저장된 값이 없으면 0을 기본값으로 사용
    Debug.Log($"저장된 총 골드 {totalGold}를 불러왔습니다.");
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
