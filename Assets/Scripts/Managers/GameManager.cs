using System.Collections;
using UnityEngine;

// GameManager 클래스는 게임의 전반적인 흐름과 상태를 관리합니다.
// (예: 게임 시작, 종료, 점수 관리, 적 생성 등)
// GameManager 클래스는 게임의 전반적인 흐름과 상태를 관리합니다.
// (예: 게임 시작, 종료, 점수 관리, 적 생성 등)
public class GameManager : MonoBehaviour
{
  public static GameManager Instance { get; private set; }

  // 킬 카운트 변경 알림 이벤트
  public static event System.Action<int> OnKillCountChanged;

  private int killCount = 0;

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }
    else
    {
      Destroy(gameObject);
    }
  }
  // 인스펙터에서 할당할 적 프리āb입니다.
  [SerializeField]
  private GameObject enemyPrefab;

  // 적이 생성될 주기를 초 단위로 설정합니다.
  [SerializeField]
  private float spawnInterval = 3.0f;

  // 플레이어를 중심으로 적이 생성될 최소/최대 반경입니다.
  [SerializeField]
  private float minSpawnRadius = 5.0f;
  [SerializeField]
  private float maxSpawnRadius = 10.0f;

  // 플레이어의 Transform 컴포넌트에 대한 참조입니다.
  private Transform playerTransform;

  private float startTime = 0f;

  // 게임이 시작될 때 한 번 호출됩니다.
  private void Start()
  {
    startTime = Time.time;
    // 씬에서 플레이어 오브젝트를 찾아 Transform을 저장합니다.
    GameObject playerObject = GameObject.FindWithTag("Player");
    if (playerObject != null)
    {
      playerTransform = playerObject.transform;
    }
    else
    {
      Debug.LogError("플레이어를 찾을 수 없습니다! 'Player' 태그가 설정되었는지 확인해주세요.");
      // 플레이어가 없으면 스포너를 시작하지 않습니다.
      return;
    }

    // 적 생성 코루틴을 시작합니다.
    StartCoroutine(SpawnEnemies());
  }

  public float GetGameTime()
  {
    return Time.time - startTime;
  }

  // 일정 주기로 적을 생성하는 코루틴입니다.
  private IEnumerator SpawnEnemies()
  {
    // 게임이 실행되는 동안 무한히 반복합니다.
    while (true)
    {
      // 다음 생성까지 지정된 시간만큼 기다립니다.
      yield return new WaitForSeconds(spawnInterval);

      // 적을 생성합니다. (플레이어가 존재할 경우에만)
      if (playerTransform != null && enemyPrefab != null)
      {
        SpawnEnemy();
      }
    }
  }

  // 적 하나를 생성하는 메서드입니다.
  private void SpawnEnemy()
  {
    // 플레이어 주변의 랜덤한 위치에 적을 생성합니다.
    // 1. 랜덤 각도를 구합니다. (0 ~ 360도)
    float randomAngle = Random.Range(0f, 360f);
    // 2. 랜덤 거리를 구합니다. (최소 ~ 최대 반경)
    float randomRadius = Random.Range(minSpawnRadius, maxSpawnRadius);
    // 3. 각도와 거리를 사용하여 원형 좌표를 계산하고, 플레이어 위치를 더해 최종 생성 위치를 구합니다.
    Vector2 spawnPosition = playerTransform.position + (Vector3)(new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomRadius);

    // enemyPrefab을 spawnPosition에 생성합니다.
    Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
  }

  public void AddKill()
  {
    killCount++;
    OnKillCountChanged?.Invoke(killCount);
  }
}
