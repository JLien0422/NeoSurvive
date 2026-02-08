using UnityEngine;

/// <summary>
/// 페이즈 특수패턴 시작 시, 해킹 오브젝트(5종) 중 1개를
/// 플레이어 주변(포위망 내부)에 랜덤 생성하는 스포너
/// </summary>
public class HackingObjectSpawner : MonoBehaviour
{
    public static HackingObjectSpawner Instance { get; private set; }

    [Header("Hackable Prefabs (5종)")]
    [Tooltip("HackableObject가 붙어있는 프리팹 5개를 넣으세요 (SecurityTurret, ElectricFence, SatelliteUplink, SynapseOverloadServer, MagneticBeacon)")]
    [SerializeField] private GameObject[] hackablePrefabs;

    [Header("Spawn 위치 (포위망 내부)")]
    [Tooltip("플레이어 기준 최소 거리")]
    [SerializeField] private float minRadius = 1.8f;

    [Tooltip("플레이어 기준 최대 거리")]
    [SerializeField] private float maxRadius = 3.5f;

    [Tooltip("스폰 시 벽/장애물에 겹치지 않게 체크할 레이어(선택)")]
    [SerializeField] private LayerMask obstacleMask;

    [Tooltip("겹침 체크 반지름(선택)")]
    [SerializeField] private float overlapCheckRadius = 0.4f;

    [Header("중복 스폰 방지")]
    [Tooltip("특수패턴 한 번에 1개만. 이미 존재하면 새로 안 만듦")]
    [SerializeField] private bool preventDuplicateWhileExists = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// 플레이어 주변에 해킹 오브젝트 1개 랜덤 생성
    /// - 특수 패턴 발동 시 1번 호출용
    /// </summary>
    public HackableObject SpawnRandomNearPlayer(Transform player)
    {
        if (player == null)
        {
            Debug.LogError("[HackingObjectSpawner] player가 null입니다.");
            return null;
        }

        if (hackablePrefabs == null || hackablePrefabs.Length == 0)
        {
            Debug.LogError("[HackingObjectSpawner] hackablePrefabs가 비어있습니다!");
            return null;
        }

        if (preventDuplicateWhileExists)
        {
            // 씬에 HackableObject가 이미 있으면 추가 생성하지 않음
            var existing = FindObjectOfType<HackableObject>();
            if (existing != null)
            {
                Debug.Log("[HackingObjectSpawner] 이미 해킹 오브젝트가 존재하여 스폰 생략");
                return existing;
            }
        }

        // 랜덤 프리팹 선택
        GameObject prefab = hackablePrefabs[Random.Range(0, hackablePrefabs.Length)];
        if (prefab == null)
        {
            Debug.LogError("[HackingObjectSpawner] 선택된 프리팹이 null입니다!");
            return null;
        }

        // 스폰 위치 찾기 (최대 12번 시도)
        Vector3 spawnPos = player.position;
        bool found = false;

        for (int i = 0; i < 12; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(minRadius, maxRadius);
            Vector3 candidate = player.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

            // 장애물/벽 겹침 체크(선택)
            if (obstacleMask.value != 0)
            {
                Collider2D hit = Physics2D.OverlapCircle(candidate, overlapCheckRadius, obstacleMask);
                if (hit != null) continue;
            }

            spawnPos = candidate;
            found = true;
            break;
        }

        if (!found)
        {
            Debug.LogWarning("[HackingObjectSpawner] 안전한 위치를 못찾아 플레이어 근처 기본 위치로 스폰합니다.");
        }

        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        HackableObject hackable = obj.GetComponent<HackableObject>();

        if (hackable == null)
        {
            Debug.LogError("[HackingObjectSpawner] 프리팹에 HackableObject 컴포넌트가 없습니다!");
            Destroy(obj);
            return null;
        }

        Debug.Log($"[HackingObjectSpawner] 해킹 오브젝트 스폰: {obj.name} at {spawnPos}");
        return hackable;
    }
}
