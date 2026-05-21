using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject[] enemyPrefabs;

    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float minSpawnRadius = 5f;
    [SerializeField] private float maxSpawnRadius = 10f;

    [Header("적 스폰 가중치")]
    [SerializeField] private float basicWeight = 1f;
    [SerializeField] private float shooterWeight = 0f;
    [SerializeField] private float rusherWeight = 0f;
    [SerializeField] private float bomberWeight = 0f;
    [SerializeField] private float tankerWeight = 0f;

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
        // ★ 변경: CSV 기반으로 Phase 초기화
        SetPhase(1);
    }

    private IEnumerator SpawnEnemies()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnEnemy();
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
        if (EnemySpawnLoader.DB == null)
        {
            Debug.LogWarning("[EnemySpawner] EnemySpawnLoader.DB가 null입니다.");
            return;
        }

        if (!EnemySpawnLoader.DB.TryGetRow(phase, out var row))
        {
            Debug.LogWarning($"[EnemySpawner] CSV에 phase {phase} 없음");
            return;
        }

        spawnInterval = row.spawnInterval;
        minSpawnRadius = row.minSpawnRadius;
        maxSpawnRadius = row.maxSpawnRadius;

        basicWeight = row.basicWeight;
        shooterWeight = row.shooterWeight;
        rusherWeight = row.rusherWeight;
        bomberWeight = row.bomberWeight;
        tankerWeight = row.tankerWeight;

        Debug.Log($"[EnemySpawner] Phase {phase} 적용 완료 | interval={spawnInterval}");
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