using System.Collections;
using UnityEngine;

/// <summary>
/// 해킹 오브젝트를 주기적으로 또는 시작 시 스폰합니다.
/// 씬에 빈 오브젝트를 만들고 이 스크립트를 붙인 뒤, 인스펙터에서 HackableObject 프리팹을 할당하세요.
/// </summary>
public class HackableObjectSpawner : MonoBehaviour
{
    [Header("프리팹")]
    [SerializeField]
    [Tooltip("스폰할 해킹 오브젝트 프리팹 (Assets/Prefabs/Objects/HackableObject)")]
    private GameObject hackableObjectPrefab;

    [Header("스폰 시점")]
    [SerializeField]
    [Tooltip("게임 시작 시 스폰할 개수 (0이면 시작 시 스폰 안 함)")]
    private int spawnCountAtStart = 1;

    [SerializeField]
    [Tooltip("주기 스폰 사용 여부")]
    private bool useIntervalSpawn = true;

    [SerializeField]
    [Tooltip("주기 스폰 간격 (초). 예: 180 = 3분마다")]
    private float spawnInterval = 180f;

    [Header("스폰 위치")]
    [SerializeField]
    [Tooltip("플레이어로부터 최소 거리")]
    private float minSpawnRadius = 6f;

    [SerializeField]
    [Tooltip("플레이어로부터 최대 거리 (화면 안쪽에 나오도록)")]
    private float maxSpawnRadius = 12f;

    [Header("최대 개수")]
    [SerializeField]
    [Tooltip("동시에 존재할 수 있는 해킹 오브젝트 최대 개수. 0이면 제한 없음")]
    private int maxConcurrent = 2;

    private Transform playerTransform;

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;

        if (hackableObjectPrefab == null)
        {
            Debug.LogWarning("[HackableObjectSpawner] hackableObjectPrefab이 할당되지 않았습니다. 인스펙터에서 HackableObject 프리팹을 넣어주세요.");
            return;
        }

        if (spawnCountAtStart > 0)
        {
            for (int i = 0; i < spawnCountAtStart; i++)
                TrySpawnOne();
        }

        if (useIntervalSpawn && spawnInterval > 0f)
            StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            TrySpawnOne();
        }
    }

    private void TrySpawnOne()
    {
        if (hackableObjectPrefab == null) return;
        if (maxConcurrent > 0 && CountHackableObjects() >= maxConcurrent) return;

        Vector3 spawnPos = GetSpawnPosition();
        Instantiate(hackableObjectPrefab, spawnPos, Quaternion.identity);
    }

    private int CountHackableObjects()
    {
        int count = 0;
        foreach (var obj in FindObjectsByType<HackableObject>(FindObjectsSortMode.None))
            if (obj != null && obj.gameObject.scene == gameObject.scene)
                count++;
        return count;
    }

    private Vector3 GetSpawnPosition()
    {
        Vector3 center = playerTransform != null ? playerTransform.position : Vector3.zero;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(minSpawnRadius, maxSpawnRadius);
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        return center + (Vector3)offset;
    }
}
