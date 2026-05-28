using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("스폰 간격")]
    [SerializeField] private float minSpawnInterval = 1.8f;
    [SerializeField] private float maxSpawnInterval = 2.2f;
    [SerializeField] private int minSpawnCount = 1;
    [SerializeField] private int maxSpawnCount = 1;

    [Header("스폰 반경 - 카메라 기준 자동 계산")]
    [SerializeField] private float minSpawnRadius = 5f;
    [SerializeField] private float maxSpawnRadius = 10f;

    [Header("적 스폰 가중치")]
    [SerializeField] private float basicWeight = 1f;
    [SerializeField] private float shooterWeight = 0f;
    [SerializeField] private float rusherWeight = 0f;
    [SerializeField] private float bomberWeight = 0f;
    [SerializeField] private float tankerWeight = 0f;

    private int currentPhase = 1;
    private int activePhase1Segment = -1;

    private void Awake()
    {
        GameManager.onPlayerSpawned += OnPlayerSpawned;
    }

    private void OnDestroy()
    {
        GameManager.onPlayerSpawned -= OnPlayerSpawned;
    }

    private void OnPlayerSpawned()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[EnemySpawner] Player를 찾을 수 없습니다.");
            return;
        }

        playerTransform = player.transform;

        StopAllCoroutines();
        StartCoroutine(SpawnEnemies());
        Debug.Log("[EnemySpawner] 플레이어 스폰 감지 → 스폰 루틴 시작");
    }

    private void Start()
    {
        // ★ 추가: 스폰 반경은 CSV가 아니라 카메라 크기 기준으로 자동 계산
        Camera cam = Camera.main;
        if (cam != null)
        {
            float worldWidth = cam.orthographicSize * cam.aspect;
            minSpawnRadius = worldWidth * 1.2f;
            maxSpawnRadius = worldWidth * 1.4f;
        }
        
        // ★ 변경: CSV 기반으로 Phase 초기화
        SetPhase(1);
    }

    private IEnumerator SpawnEnemies()
    {
        while (true)
        {
            UpdatePhase1SegmentIfNeeded();

            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            UpdatePhase1SegmentIfNeeded();

            int spawnCount = Random.Range(minSpawnCount, maxSpawnCount + 1);
            for (int i = 0; i < spawnCount; i++)
            {
                SpawnEnemy();
            }
        }
    }

    private void SpawnEnemy()
    {
        if (playerTransform == null) return;

        GameObject prefab = GetRandomEnemyPrefab();
        if (prefab == null) return;

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(minSpawnRadius, maxSpawnRadius);

        Vector3 pos = playerTransform.position +
                      new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

        Instantiate(prefab, pos, Quaternion.identity);
    }

    private GameObject GetRandomEnemyPrefab()
    {
        float total =
            basicWeight + shooterWeight + rusherWeight + bomberWeight + tankerWeight;

        if (total <= 0f) return enemyPrefabs[0];

        float r = Random.value * total;

        if (r < basicWeight) return enemyPrefabs[0];
        r -= basicWeight;

        if (r < shooterWeight) return enemyPrefabs[1];
        r -= shooterWeight;

        if (r < rusherWeight) return enemyPrefabs[2];
        r -= rusherWeight;

        if (r < bomberWeight) return enemyPrefabs[3];

        return enemyPrefabs[4];
    }

    // ★ 변경: CSV 기반 Phase 설정
    public void SetPhase(int phase)
    {
        currentPhase = phase;
        activePhase1Segment = -1;

        if (EnemySpawnLoader.DB == null)
        {
            Debug.LogWarning("[EnemySpawner] EnemySpawnLoader.DB가 null입니다.");
            return;
        }

        if (phase == 1 && TryApplyPhase1Segment(forceLog: true))
        {
            return;
        }

        if (!EnemySpawnLoader.DB.TryGetRow(phase, out var row))
        {
            Debug.LogWarning($"[EnemySpawner] CSV에 phase {phase} 없음");
            return;
        }

        ApplySpawnRow(row);

        Debug.Log(
            $"[EnemySpawner] Phase {phase} 적용 완료 | " +
            $"interval={minSpawnInterval}~{maxSpawnInterval} | count={minSpawnCount}~{maxSpawnCount} | " +
            $"radius={minSpawnRadius}~{maxSpawnRadius} | " +
            $"weights={basicWeight}/{shooterWeight}/{rusherWeight}/{bomberWeight}/{tankerWeight}"
        );
    }

    private void UpdatePhase1SegmentIfNeeded()
    {
        if (currentPhase != 1)
            return;

        TryApplyPhase1Segment(forceLog: false);
    }

    private bool TryApplyPhase1Segment(bool forceLog)
    {
        if (EnemySpawnLoader.DB == null)
            return false;

        float gameTime = GameManager.Instance != null ? GameManager.Instance.GetGameTime() : 0f;

        if (!EnemySpawnLoader.DB.TryGetPhase1Segment(gameTime, out var segmentRow))
            return false;

        if (!forceLog && activePhase1Segment == segmentRow.segment)
            return true;

        activePhase1Segment = segmentRow.segment;
        ApplySpawnRow(segmentRow);

        Debug.Log(
            $"[EnemySpawner] Phase1 Segment {segmentRow.segment} 적용 | " +
            $"time={segmentRow.startTime}~{segmentRow.endTime} | " +
            $"interval={minSpawnInterval}~{maxSpawnInterval} | count={minSpawnCount}~{maxSpawnCount}"
        );

        return true;
    }

    private void ApplySpawnRow(EnemySpawnDB.Row row)
    {
        minSpawnInterval = row.minSpawnInterval;
        maxSpawnInterval = Mathf.Max(row.minSpawnInterval, row.maxSpawnInterval);
        minSpawnCount = Mathf.Max(1, row.minSpawnCount);
        maxSpawnCount = Mathf.Max(minSpawnCount, row.maxSpawnCount);

        basicWeight = row.basicWeight;
        shooterWeight = row.shooterWeight;
        rusherWeight = row.rusherWeight;
        bomberWeight = row.bomberWeight;
        tankerWeight = row.tankerWeight;
    }

    /// <summary>
    /// 보스 등장 시 호출 - 적 스폰 코루틴 중단
    /// </summary>
    public void StopSpawning()
    {
        StopAllCoroutines();
        Debug.Log("[EnemySpawner] 스폰 중단 완료");
    }
}