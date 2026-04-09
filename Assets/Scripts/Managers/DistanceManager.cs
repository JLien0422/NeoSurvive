using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 거리기반 픽업 시스템 매니저 (Spatial Hash Grid 버전)
/// 월드를 cellSize 단위 격자로 나누고, 아이템은 자신의 셀에만 등록합니다.
/// 체크 시 플레이어 주변 3×3 셀만 순회하여 불필요한 연산을 제거합니다.
/// 아이템은 스폰 시 Register(), 소멸 시 Unregister()를 호출해야 합니다.
/// </summary>
public class DistanceManager : MonoBehaviour
{
    public static DistanceManager Instance { get; private set; }

    // 격자 한 칸의 크기 (픽업 범위 최대값보다 크게 설정)
    // 픽업 범위가 최대 0.6 유닛이므로 2f면 ±1셀(3×3)로 충분
    // ⚠ 게임 시작 후 변경하면 기존 등록된 아이템 셀이 어긋날 수 있음 → 플레이 전에 설정
    [Tooltip("격자 셀 크기 (픽업 범위보다 크게 설정할 것, 기본 2)")]
    public float cellSize = 2f;

    // 그리드: 셀 좌표 → 해당 셀에 있는 아이템 목록
    private readonly Dictionary<(int, int), List<IPickupable>> grid
        = new Dictionary<(int, int), List<IPickupable>>();

    // 거리 검사 주기 (0.1초마다)
    [SerializeField] private float checkInterval = 0.1f;
    private float checkTimer = 0f;

    // 플레이어 캐시
    private Player cachedPlayer;
    private float playerSearchTimer = 0f;
    private const float PlayerSearchInterval = 1f; // 1초마다 재탐색

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        // 플레이어 캐시 갱신 (1초마다 또는 캐시가 없을 때)
        playerSearchTimer += Time.deltaTime;
        if (playerSearchTimer >= PlayerSearchInterval || cachedPlayer == null)
        {
            playerSearchTimer = 0f;
            cachedPlayer = FindLocalPlayer();
        }

        if (cachedPlayer == null) return;

        // 0.1초마다 거리 검사
        checkTimer += Time.deltaTime;
        if (checkTimer < checkInterval) return;
        checkTimer = 0f;

        CheckPickups();
    }

    /// <summary>
    /// 플레이어 주변 3×3 셀만 순회하여 픽업 거리를 검사합니다.
    /// 전체 아이템 순회 대신 인접 셀 아이템만 처리하므로 부하가 크게 줄어듭니다.
    /// </summary>
    private void CheckPickups()
    {
        Vector3 playerPos = cachedPlayer.transform.position;

        // 플레이어가 속한 셀 좌표
        (int cx, int cy) = GetCell(playerPos);

        // 플레이어 주변 3×3 셀 순회 (±1셀)
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                var cellKey = (cx + dx, cy + dy);
                if (!grid.TryGetValue(cellKey, out List<IPickupable> list)) continue;

                // 역순 순회: Pickup() 내부 Unregister로 인한 리스트 변형 안전 처리
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    IPickupable item = list[i];

                    // 이미 파괴된 아이템 정리
                    if (item == null || item.Transform == null)
                    {
                        list.RemoveAt(i);
                        continue;
                    }

                    // sqrMagnitude로 제곱근 없이 거리 비교
                    float sqrDist = (item.Transform.position - playerPos).sqrMagnitude;
                    float sqrRange = item.PickupRange * item.PickupRange;

                    if (sqrDist <= sqrRange)
                        item.Pickup(cachedPlayer);
                }

                // 빈 셀 정리 (메모리 절약)
                if (list.Count == 0)
                    grid.Remove(cellKey);
            }
        }
    }

    /// <summary>
    /// 아이템 스폰 시 호출 - 아이템 위치에 해당하는 셀에 등록합니다.
    /// </summary>
    public void Register(IPickupable item)
    {
        if (item == null || item.Transform == null) return;

        var cellKey = GetCell(item.Transform.position);

        if (!grid.TryGetValue(cellKey, out List<IPickupable> list))
        {
            list = new List<IPickupable>();
            grid[cellKey] = list;
        }

        // 중복 등록 방지
        if (!list.Contains(item))
            list.Add(item);
    }

    /// <summary>
    /// 아이템 소멸 시 호출 - 해당 셀에서 제거합니다.
    /// </summary>
    public void Unregister(IPickupable item)
    {
        if (item == null || item.Transform == null) return;

        var cellKey = GetCell(item.Transform.position);

        if (grid.TryGetValue(cellKey, out List<IPickupable> list))
        {
            list.Remove(item);

            // 빈 셀 정리
            if (list.Count == 0)
                grid.Remove(cellKey);
        }
    }

    /// <summary>
    /// 월드 좌표를 격자 셀 좌표로 변환합니다.
    /// </summary>
    private (int, int) GetCell(Vector3 pos)
    {
        return (Mathf.FloorToInt(pos.x / cellSize),
                Mathf.FloorToInt(pos.y / cellSize));
    }

    /// <summary>
    /// 로컬 플레이어를 찾습니다. (멀티플레이: IsLocal 우선, 싱글: 첫 번째)
    /// </summary>
    private Player FindLocalPlayer()
    {
        // FindWithTag로 Player 태그 오브젝트만 탐색 (FindObjectsOfType보다 빠름)
        GameObject[] playerObjs = GameObject.FindGameObjectsWithTag("Player");
        Player fallback = null;

        foreach (var obj in playerObjs)
        {
            Player p = obj.GetComponent<Player>();
            if (p == null) continue;
            return p;
        }

        return fallback;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 현재 그리드 상태를 인스펙터에 표시합니다.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || grid == null) return;

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.15f);
        foreach (var kv in grid)
        {
            if (kv.Value.Count == 0) continue;
            Vector3 center = new Vector3(
                (kv.Key.Item1 + 0.5f) * cellSize,
                (kv.Key.Item2 + 0.5f) * cellSize,
                0f
            );
            Gizmos.DrawCube(center, new Vector3(cellSize, cellSize, 0.1f));
        }
    }
#endif
}
