using System.Collections;
using UnityEngine;

public class PhaseDirector : MonoBehaviour
{
  [Header("Phase 타이밍(초)")]
  [SerializeField] private float phase2Time = 180f;  // 03:00
  [SerializeField] private float phase3Time = 360f;  // 06:00
  [SerializeField] private float phase4Time = 540f;  // 09:00
  [SerializeField] private float phase5Time = 720f;  // 12:00

  [Header("포위 이벤트")]
  [SerializeField] private SiegeEvent siegeEvent;
  [SerializeField] private float postSiegeSpawnSlowDuration = 10f;
  [SerializeField] private float postSiegeSpawnIntervalMultiplier = 5f;

  [Header("해킹 오브젝트 프리팹(5종)")]
  [SerializeField] private GameObject[] hackablePrefabs;

  [Header("해킹 오브젝트 스폰 반경(플레이어 주변)")]
  [SerializeField] private float hackableSpawnRadius = 2.5f;
  [SerializeField] private float hackableMinDistance = 1.2f;

  private Transform player;
  private EnemySpawner enemySpawner;

  // Phase별 특수패턴 "1회만" 실행 플래그
  private bool siege2Done, siege3Done, siege4Done, siege5Done;

  private void Awake()
  {
    GameManager.onPlayerSpawned += RefreshPlayerReference;
  }

  private void OnDestroy()
  {
    GameManager.onPlayerSpawned -= RefreshPlayerReference;
  }

  private void Start()
  {
    RefreshPlayerReference();

    if (siegeEvent == null) siegeEvent = GetComponent<SiegeEvent>();              // ***** 안정화
    enemySpawner = GetComponent<EnemySpawner>();                                  // ***** 안정화
    if (enemySpawner == null) enemySpawner = FindObjectOfType<EnemySpawner>();   // ***** 안정화

    enemySpawner?.SetPhase(1); // ★ 변경
    GameManager.Instance?.SetCurrentPhase(1);
    GameAnalyticsTracker.TrackPhaseReached(1, GameManager.Instance != null ? GameManager.Instance.GetGameTime() : 0f);
    SoundManager.Instance?.PlayPhaseBgm(1);
  }

  private void Update()
  {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    HandlePhaseJumpKeys();
    HandleResetKeys();
#endif

    if (GameManager.Instance == null) return;

    float t = GameManager.Instance.GetGameTime();

    // ============================================================
    // ***** 중요: 높은 Phase부터 검사 + 한 번 트리거하면 return
    // (시간 점프 시 2페이즈가 먼저 터지는 현상 방지)
    // ============================================================

    if (!siege5Done && t >= phase5Time) { siege5Done = true; TriggerSiegeOnce(5); return; } // ***** 변경
    if (!siege4Done && t >= phase4Time) { siege4Done = true; TriggerSiegeOnce(4); return; } // ***** 변경
    if (!siege3Done && t >= phase3Time) { siege3Done = true; TriggerSiegeOnce(3); return; } // ***** 변경
    if (!siege2Done && t >= phase2Time) { siege2Done = true; TriggerSiegeOnce(2); return; } // ***** 변경
  }

  private void TriggerSiegeOnce(int phaseIndex)
  {
      Debug.Log($"[PhaseDirector] Phase{phaseIndex} 특수패턴(포위) 1회 발동");
      GameManager.Instance?.SetCurrentPhase(phaseIndex);
      GameAnalyticsTracker.TrackPhaseReached(phaseIndex, GameManager.Instance != null ? GameManager.Instance.GetGameTime() : 0f);

      // ★ 변경: 포위 이벤트 시작과 동시에 해당 Phase 스폰률 적용
      if (enemySpawner != null)
      {
          enemySpawner.SetPhase(phaseIndex);
          enemySpawner.ApplyTemporarySpawnIntervalMultiplier(postSiegeSpawnIntervalMultiplier, postSiegeSpawnSlowDuration);
          SoundManager.Instance?.PlayPhaseBgm(phaseIndex);
          Debug.Log($"[PhaseDirector] Phase{phaseIndex} CSV 스폰 가중치 즉시 적용 완료");
      }
      else
      {
          Debug.LogWarning("[PhaseDirector] enemySpawner가 null입니다.");
      }

      if (siegeEvent != null)
      {
          siegeEvent.BeginSiege(phaseIndex, () =>
          {
              // ★ 변경: 스폰률 적용은 위에서 이미 했으므로 여기서는 종료 로그만 남김
              Debug.Log($"[PhaseDirector] Phase{phaseIndex} 포위 이벤트 종료");
          });
      }
      else
      {
          Debug.LogWarning("[PhaseDirector] siegeEvent가 null입니다. 같은 오브젝트에 SiegeEvent를 붙이거나 인스펙터 연결을 하세요.");
      }

      StartCoroutine(SpawnRandomHackableAfterFrame());
  }

  private IEnumerator SpawnRandomHackableAfterFrame()
  {
    yield return null;
    SpawnRandomHackableNearPlayer();
  }

  private void SpawnRandomHackableNearPlayer()
  {
    if (player == null)
    {
      RefreshPlayerReference();
    }

    if (player == null)
    {
      Debug.LogWarning("[PhaseDirector] Player를 찾지 못해 해킹 오브젝트를 스폰하지 못했습니다.");
      return;
    }

    if (hackablePrefabs == null || hackablePrefabs.Length == 0)
    {
      Debug.LogWarning("[PhaseDirector] hackablePrefabs가 비었습니다.");
      return;
    }

    GameObject prefab = PickActiveHackablePrefab();
    if (prefab == null)
    {
      Debug.LogWarning("[PhaseDirector] 활성 미니게임(터렛/울타리) 프리팹이 없습니다.");
      return;
    }

    Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(hackableMinDistance, hackableSpawnRadius);
    Vector3 pos = player.position + new Vector3(offset.x, offset.y, 0f);

    Instantiate(prefab, pos, Quaternion.identity);
    GameAnalyticsTracker.TrackHackableSpawned(prefab.name, pos);
    Debug.Log($"[PhaseDirector] Hackable spawned: {prefab.name}");
  }

  private GameObject PickActiveHackablePrefab()
  {
    int allowedCount = 0;
    GameObject picked = null;

    for (int i = 0; i < hackablePrefabs.Length; i++)
    {
      GameObject candidate = hackablePrefabs[i];
      if (candidate == null)
        continue;

      HackableObject hackable =
        candidate.GetComponent<HackableObject>()
        ?? candidate.GetComponentInChildren<HackableObject>(true);

      if (hackable == null || !ActiveHackMinigames.IsActive(hackable.ObjectType))
        continue;

      allowedCount++;
      if (Random.Range(0, allowedCount) == 0)
        picked = candidate;
    }

    return picked;
  }

  private void RefreshPlayerReference()
  {
    GameObject playerObj = GameObject.FindWithTag("Player");
    player = playerObj != null ? playerObj.transform : null;
  }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
  // ============================================================
  // 디버그: 1~6키로 해당 페이즈 시작 시각으로 점프
  // ============================================================
  private void HandlePhaseJumpKeys()
  {
    if (GameManager.Instance == null) return;

    if (Input.GetKeyDown(KeyCode.Alpha1)) JumpToPhase(1);
    if (Input.GetKeyDown(KeyCode.Alpha2)) JumpToPhase(2);
    if (Input.GetKeyDown(KeyCode.Alpha3)) JumpToPhase(3);
    if (Input.GetKeyDown(KeyCode.Alpha4)) JumpToPhase(4);
    if (Input.GetKeyDown(KeyCode.Alpha5)) JumpToPhase(5);
    if (Input.GetKeyDown(KeyCode.Alpha6)) JumpToPhase(6); // 보스 직전 (14분 50초)
  }

  private void JumpToPhase(int phase)
  {
    // ***** 추천: 점프 시 "이전 Phase들은 이미 처리된 것"으로 플래그 세팅
    // (점프한 순간 2가 먼저 터지는 문제를 구조적으로 예방)
    if (phase >= 3) siege2Done = true; // ***** 추가
    if (phase >= 4) siege3Done = true; // ***** 추가
    if (phase >= 5) siege4Done = true; // ***** 추가
    if (phase >= 6) siege5Done = true; // 보스 직전 점프 시 Phase5도 완료 처리
    // siege5Done은 false여야 Phase5가 발동됨 (그대로 둠)

    float targetElapsed = phase switch
    {
      1 => 0f,
      2 => phase2Time,
      3 => phase3Time,
      4 => phase4Time,
      5 => phase5Time,
      6 => 890f,  // 14분 50초 - 보스 스폰 직전 테스트용
      _ => 0f
    };

    // 시작 직후로 살짝 넘겨서 조건 확실히 만족
    GameManager.Instance.DebugSetElapsedTime(targetElapsed + 0.1f);
    if (phase >= 1 && phase <= 5)
    {
      SoundManager.Instance?.PlayPhaseBgm(phase);
    }

    Debug.Log($"[PhaseDirector] JumpToPhase={phase} elapsed={targetElapsed}");
  }

  // ============================================================
  // 디버그: R키로 특수패턴 플래그 리셋
  // ============================================================
  private void HandleResetKeys()
  {
    if (Input.GetKeyDown(KeyCode.R))
    {
      siege2Done = siege3Done = siege4Done = siege5Done = false;
      Debug.Log("[PhaseDirector] DEBUG: siege flags reset (R)");
    }
  }
#endif
}
