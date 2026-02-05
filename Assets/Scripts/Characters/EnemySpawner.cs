using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{

  [SerializeField] private Transform playerTransform;
  // 다양한 적 프리팹을 스폰하기 위한 배열 (0: Basic, 1: Shooter, 2: Rusher, 3: Bomber, 4: Tanker 권장)
  [SerializeField] private GameObject[] enemyPrefabs;
  [SerializeField] private float spawnInterval = 2f;
  [SerializeField] private float minSpawnRadius = 5f;
  [SerializeField] private float maxSpawnRadius = 10f;

  [Header("적 스폰 확률 (가중치, 합이 1일 필요는 없음)")]
  [SerializeField] private float basicWeight = 0.5f;
  [SerializeField] private float shooterWeight = 0.15f;
  [SerializeField] private float rusherWeight = 0.15f;
  [SerializeField] private float bomberWeight = 0.1f;
  [SerializeField] private float tankerWeight = 0.1f;

  // Start is called before the first frame update
  void Start()
  {
    // 씬에서 플레이어 오브젝트를 찾아 Transform을 저장합니다.
    GameObject playerObject = GameObject.FindWithTag("Player");
    if (playerObject != null)
    {
      playerTransform = playerObject.transform;
      StartCoroutine(SpawnEnemies());
    }
    else
    {
      Debug.LogError("플레이어를 찾을 수 없습니다! 'Player' 태그가 설정되었는지 확인해주세요.");
      // 플레이어가 없으면 스포너를 시작하지 않습니다.
      return;
    }

    Camera cam = Camera.main;
    float worldWidth = cam.orthographicSize * cam.aspect;

    minSpawnRadius = worldWidth * 1.2f;
    maxSpawnRadius = worldWidth * 1.4f;
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
      if (playerTransform != null && enemyPrefabs != null && enemyPrefabs.Length > 0)
      {
        SpawnEnemy();
      }
    }
  }

  // 적 하나를 생성하는 메서드입니다.
  private void SpawnEnemy()
  {
    GameObject prefab = GetRandomEnemyPrefab();
    if (prefab == null) return;

    // 플레이어 주변의 랜덤한 위치에 적을 생성합니다.
    // 1. 랜덤 각도를 구합니다. (0 ~ 360도)
    float randomAngle = Random.Range(0f, 360f);
    // 2. 랜덤 거리를 구합니다. (최소 ~ 최대 반경)
    float randomRadius = Random.Range(minSpawnRadius, maxSpawnRadius);
    // 3. 각도와 거리를 사용하여 원형 좌표를 계산하고, 플레이어 위치를 더해 최종 생성 위치를 구합니다.
    Vector2 spawnPosition = playerTransform.position + (Vector3)(new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomRadius);

    // enemyPrefab을 spawnPosition에 생성합니다.
    Instantiate(prefab, spawnPosition, Quaternion.identity);
  }

  /// <summary>
  /// 적 프리팹 배열에서 하나를 선택합니다.
  /// 인덱스 0~4에 Basic/Shooter/Rusher/Bomber/Tanker를 넣었다고 가정하고
  /// Basic이 가장 자주 나오도록 가중치를 둡니다.
  /// </summary>
  private GameObject GetRandomEnemyPrefab()
  {
    if (enemyPrefabs == null || enemyPrefabs.Length == 0) return null;

    // 프리팹이 5개 이상 세팅되어 있다고 가정 (0: Basic, 1: Shooter, 2: Rusher, 3: Bomber, 4: Tanker)
    if (enemyPrefabs.Length >= 5)
    {
      // 인스펙터에서 조절 가능한 가중치 기반 랜덤
      float w0 = Mathf.Max(0f, basicWeight);
      float w1 = Mathf.Max(0f, shooterWeight);
      float w2 = Mathf.Max(0f, rusherWeight);
      float w3 = Mathf.Max(0f, bomberWeight);
      float w4 = Mathf.Max(0f, tankerWeight);

      float total = w0 + w1 + w2 + w3 + w4;
      if (total <= 0f)
      {
        // 전부 0이면 그냥 Basic만 사용
        return enemyPrefabs[0];
      }

      float r = Random.value * total;

      if (r < w0) return enemyPrefabs[0];       // Basic
      r -= w0;
      if (r < w1) return enemyPrefabs[1];       // Shooter
      r -= w1;
      if (r < w2) return enemyPrefabs[2];       // Rusher
      r -= w2;
      if (r < w3) return enemyPrefabs[3];       // Bomber
      return enemyPrefabs[4];                   // Tanker
    }

    // 5개 미만이면 그냥 균등 랜덤
    int idx = Random.Range(0, enemyPrefabs.Length);
    return enemyPrefabs[idx];
  }
}
