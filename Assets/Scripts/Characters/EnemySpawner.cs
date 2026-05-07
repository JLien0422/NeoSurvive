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
        Debug.Log("[EnemySpawner] 플레이어가 완전히 스폰된 것을 감지, 스폰 루틴 시작.");
    }

    private void Start()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            float worldWidth = cam.orthographicSize * cam.aspect;
            minSpawnRadius = worldWidth * 1.2f;
            maxSpawnRadius = worldWidth * 1.4f;
        }

        SetPhaseWeights(1); // ★ Phase1: Basic만
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

    // 🔑 Phase별 가중치 설정
    public void SetPhaseWeights(int phase)
    {
        switch (phase)
        {
            case 1: SetWeights(100, 0, 0, 0, 0); break;
            case 2: SetWeights(60, 40, 0, 0, 0); break;
            case 3: SetWeights(50, 30, 20, 0, 0); break;
            case 4: SetWeights(40, 30, 15, 15, 0); break;
            case 5: SetWeights(35, 25, 15, 15, 10); break;
        }
    }

    private void SetWeights(float b, float s, float r, float bo, float t)
    {
        basicWeight = b;
        shooterWeight = s;
        rusherWeight = r;
        bomberWeight = bo;
        tankerWeight = t;
    }

    /// <summary>
    /// 보스 등장 시 호출 - 적 스폰 코루틴을 완전히 중단합니다.
    /// </summary>
    public void StopSpawning()
    {
        StopAllCoroutines();
        Debug.Log("[EnemySpawner] 스폰 중단 완료");
    }
}
