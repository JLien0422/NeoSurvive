using System.Collections;
using System.Collections.Generic;   
using UnityEngine;
using NeoSurvive.Balancing.Map;
using NeoSurvive.Core;
using NeoSurvive.Characters;

namespace NeoSurvive.Map.Map1.Gimmicks
{
    /// <summary>
    /// [Map1 Gimmick]
    /// 화물 크레인 낙하 기믹의 메인 제어 스크립트
    ///
    /// [기본 역할]
    /// - 플레이어가 활성 구역(active area) 안에 들어오면 플레이어 위치를 추적하여 화물 낙하
    /// - 경고 표시(CargoDropWarning)와 실제 피해 판정(CargoContainerImpact)을 순서대로 생성
    ///
    /// [해킹 역할]
    /// - 해커 플레이어가 크레인 본체 근처에서 H 키를 누르면 해킹 모드 진입
    /// - 일정 시간 동안 플레이어 주변의 적을 탐색하여 적 위치를 추적 낙하
    /// - 적이 없으면 플레이어 위치를 대체 타겟으로 사용
    ///
    /// [연결 구조]
    /// - CSV: MapEnvironmentGimmickLoader.DB에서 설정값 로드
    /// - Player: 활성 구역 판정, 해킹 가능 여부 판단, 플레이어 추적 낙하
    /// - Enemy: 해킹 모드에서 가장 가까운 적 탐색
    /// - CargoDropWarning: 낙하 전 경고 표시 하위 오브젝트
    /// - CargoContainerImpact: 실제 범위 피해 하위 오브젝트
    ///
    /// [실행 흐름]
    /// 기본 모드:
    ///   Update -> UpdatePlayerInsideState -> HandleDropRoutineState
    ///   -> DropRoutine_PlayerTrack()
    ///   -> SpawnWarning()
    ///   -> 대기(warningDuration)
    ///   -> SpawnImpact()
    ///
    /// 해킹 모드:
    ///   Update -> HandleHackInput()
    ///   -> StartHackMode()
    ///   -> HackedModeRoutine()
    ///   -> 적 탐색 후 SpawnWarning / SpawnImpact
    ///
    /// [Git 병합 시 주의]
    /// - warningPrefab / impactPrefab 연결이 누락되면 기믹이 반쪽만 동작함
    /// - MapEnvironmentGimmickLoader.DB가 먼저 초기화되어야 CSV 적용 가능
    /// - Player, Enemy 구조가 팀원 코드와 달라지면 추적/판정 실패 가능
    /// - 현재 해킹 키가 H로 되어 있으므로, 다른 E 기반 해킹 기믹과 입력 통일 여부 확인 필요
    ///
    /// [QA 체크 포인트]
    /// 1. 플레이어가 활성 구역에 들어가면 낙하 루틴이 시작되는가
    /// 2. 경고 -> 실제 충돌 순서가 정확한가
    /// 3. 기본 모드에서 플레이어 위치 추적이 정확한가
    /// 4. 해킹 모드에서 적 위치 추적이 정확한가
    /// 5. 적이 없을 때 플레이어 위치로 fallback 되는가
    /// 6. 해킹 종료 후 기본 모드로 정상 복귀하는가
    /// 7. CSV 값(데미지/타이밍/범위)이 정상 반영되는가
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(SpriteRenderer))]
    public class CargoDropCrane : MonoBehaviour
    {
        [Header("CSV")]
        [SerializeField] private string mapId = "Map1";
        [SerializeField] private string gimmickId = "cargo_drop_crane";

        [Header("Spawn Area / Activation")]
        // 플레이어가 이 범위 안에 들어오면 기본 낙하 루틴 시작
        [SerializeField] private Vector2 activeAreaSize = new Vector2(12f, 8f);

        // 낙하 위치 계산 시 Y 오프셋
        [SerializeField] private float trackOffsetY = 0f;

        [Header("Timing")]
        // 첫 낙하까지 지연 시간
        [SerializeField] private float firstDelay = 2f;

        // 경고 표시 유지 시간
        [SerializeField] private float warningDuration = 3f;

        // 다음 낙하까지의 랜덤 간격 최소값
        [SerializeField] private float intervalMin = 6f;

        // 다음 낙하까지의 랜덤 간격 최대값
        [SerializeField] private float intervalMax = 10f;

        [Header("Warning Config")]
        // 경고 마커 깜빡임 여부
        [SerializeField] private bool blink = true;

        // 경고 마커 깜빡임 속도
        [SerializeField] private float blinkSpeed = 8f;

        [Header("Impact Config")]
        // 실제 피해 판정 범위
        [SerializeField] private Vector2 impactSize = new Vector2(2.5f, 2.5f);

        // 플레이어 피해
        [SerializeField] private float playerDamage = 9999f;

        // 적 피해
        [SerializeField] private float enemyDamage = 9999f;

        // 충돌 판정 오브젝트 유지 시간
        [SerializeField] private float impactDestroyAfter = 0.5f;

        [Header("Hack Config")]
        // 해킹 가능 거리
        [SerializeField] private float hackInteractRange = 2.5f;

        // 해킹 유지 최소 시간
        [SerializeField] private float hackedDurationMin = 30f;

        // 해킹 유지 최대 시간
        [SerializeField] private float hackedDurationMax = 50f;

        // 적 탐색 반경
        [SerializeField] private float enemySearchRadius = 10f;

        // 필요시 적 레이어 참고용
        [SerializeField] private LayerMask enemyMask;

        // 디버그/참고용 적 태그
        [SerializeField] private string enemyTag = "Enemy";

        [Header("Prefabs")]
        // 낙하 전 바닥 경고 표시 프리팹
        [SerializeField] private GameObject warningPrefab;

        // 실제 충돌/피해 판정 프리팹
        [SerializeField] private GameObject impactPrefab;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        [Header("Hack Debug")]
        [SerializeField] private bool hackDebugLog = true;

        // 기믹 활성화 여부(CSV에서 로드)
        private bool isActive = true;

        // 현재 플레이어 참조
        private Player currentPlayer;

        // 기본 모드 낙하 코루틴
        private Coroutine dropRoutine;

        // 해킹 모드 낙하 코루틴
        private Coroutine hackedRoutine;

        // 플레이어가 활성 구역 안에 있는지
        private bool playerInsideActiveArea = false;

        // 현재 해킹 상태인지
        private bool isHacked = false;

        // 현재 낙하 루틴이 실행 중인지
        private bool isDropping = false;

        // [추가]
        // Warning이 이미 생성되어 Impact까지 보장해야 하는 상태인지
        private bool attackCycleInProgress = false;

        // 해킹 남은 시간(디버그용)
        private float hackedRemainTime = 0f;

        // 해킹 로그 출력 주기 제어용
        private float hackLogTimer = 0f;

        // 크레인 렌더러
        private SpriteRenderer craneRenderer;

        // 기본 색상
        private Color normalColor = Color.white;

        // 해킹 시 색상
        private Color hackedColor = Color.green;

        private void Awake()
        {
            // 크레인 본체 SpriteRenderer 캐싱
            craneRenderer = GetComponent<SpriteRenderer>();

            if (craneRenderer != null)
                craneRenderer.color = normalColor;
        }

        private void Start()
        {
            // CSV 값 로드
            LoadFromCSV();

            // 씬 내 플레이어 참조 찾기
            FindPlayerReference();
        }

        private void Update()
        {
            // 플레이어가 아직 없거나 씬 전환 후 잃어버렸으면 재탐색
            if (currentPlayer == null)
                FindPlayerReference();

            // 활성 구역 내 플레이어 존재 여부 갱신
            UpdatePlayerInsideState();

            // 기본 낙하 루틴 시작/중지 상태 관리
            HandleDropRoutineState();

            // 해킹 입력 처리
            HandleHackInput();

            // 해킹 중일 때 1초마다 남은 시간 로그 출력
            if (isHacked && hackDebugLog)
            {
                hackLogTimer += Time.deltaTime;
                if (hackLogTimer >= 1f)
                {
                    hackLogTimer = 0f;
                    Debug.Log($"[CargoDropCrane] 해킹 중... 남은 시간: {hackedRemainTime:F1}초");
                }
            }
        }

        /// <summary>
        /// CSV에서 기믹 설정값을 가져온다.
        /// Loader가 먼저 초기화되어 있어야 한다.
        /// </summary>
        private void LoadFromCSV()
        {
            Debug.Log("[CargoDropCrane] LoadFromCSV 진입");

            if (MapEnvironmentGimmickLoader.DB == null)
            {
                Debug.LogWarning("[CargoDropCrane] MapEnvironmentGimmickLoader.DB가 null임");
                return;
            }

            var row = MapEnvironmentGimmickLoader.DB.Get(mapId, gimmickId);
            if (row == null)
            {
                Debug.LogWarning("[CargoDropCrane] row가 null임");
                return;
            }

            Debug.Log($"[DEBUG] Crane CSV 가져옴 | playerDamage={row.playerDamage}");

            isActive = row.isActive;
            activeAreaSize = new Vector2(row.areaWidth, row.areaHeight);

            firstDelay = row.firstDelay;
            warningDuration = row.warningDuration;
            intervalMin = row.intervalMin;
            intervalMax = row.intervalMax;

            blink = row.blink;
            blinkSpeed = row.blinkSpeed;

            impactSize = new Vector2(row.impactWidth, row.impactHeight);
            playerDamage = row.playerDamage;
            enemyDamage = row.enemyDamage;
            impactDestroyAfter = row.impactDestroyAfter;

            if (debugLog)
            {
                Debug.Log(
                    $"[CargoDropCrane] CSV 로드 완료 | " +
                    $"activeArea={activeAreaSize}, firstDelay={firstDelay}, warningDuration={warningDuration}, " +
                    $"intervalMin={intervalMin}, intervalMax={intervalMax}, " +
                    $"blink={blink}, blinkSpeed={blinkSpeed}, " +
                    $"impactSize={impactSize}, playerDamage={playerDamage}, enemyDamage={enemyDamage}, impactDestroyAfter={impactDestroyAfter}"
                );
            }
        }

        /// <summary>
        /// 씬에서 Player를 찾아 currentPlayer에 저장
        /// </summary>
        private void FindPlayerReference()
        {
            currentPlayer = FindObjectOfType<Player>();

            if (debugLog && currentPlayer != null)
                Debug.Log($"[CargoDropCrane] Player 찾음 | {currentPlayer.name}");
        }

        /// <summary>
        /// 플레이어가 activeArea 안에 있는지 매 프레임 계산
        /// </summary>
        private void UpdatePlayerInsideState()
        {
            if (!isActive || currentPlayer == null)
            {
                playerInsideActiveArea = false;
                return;
            }

            Vector2 center = transform.position;
            Vector2 playerPos = currentPlayer.transform.position;
            Vector2 half = activeAreaSize * 0.5f;

            playerInsideActiveArea =
                playerPos.x >= center.x - half.x &&
                playerPos.x <= center.x + half.x &&
                playerPos.y >= center.y - half.y &&
                playerPos.y <= center.y + half.y;
        }

        /// <summary>
        /// 플레이어가 활성 구역에 있으면 기본 낙하 루틴 시작,
        /// 구역을 벗어나면 기본 낙하 루틴 중지
        /// </summary>
        private void HandleDropRoutineState()
        {
            if (!isActive) return;

            // 해킹 중에는 기본 루틴 중지
            if (isHacked) return;

            if (playerInsideActiveArea)
            {
                // 현재 기본 루틴이 없고, 낙하도 실행 중이 아니면 새로 시작
                if (dropRoutine == null && !isDropping)
                {
                    dropRoutine = StartCoroutine(DropRoutine_PlayerTrack());
                }
            }
            else
            {
                // [추가]
                // 이미 Warning이 찍힌 공격 사이클 중이면
                // Impact까지는 실행되어야 하므로 코루틴을 끊지 않음
                if (attackCycleInProgress)
                    return;

                // 활성 구역 밖으로 나가면 기본 낙하 루틴 중지
                if (dropRoutine != null)
                {
                    StopCoroutine(dropRoutine);
                    dropRoutine = null;
                    isDropping = false;

                    if (debugLog)
                        Debug.Log("[CargoDropCrane] 플레이어가 구역 밖으로 나가서 기본 낙하 중지");
                }
            }
        }

        /// <summary>
        /// 해커 플레이어가 가까이 있을 때 H 입력으로 해킹 시작
        /// </summary>
        private void HandleHackInput()
        {
            if (!isActive) return;
            if (currentPlayer == null) return;
            if (isHacked) return;

            if (!currentPlayer.CharacterType.Equals(CharacterType.Hacker)) return;

            float dist = Vector2.Distance(currentPlayer.transform.position, transform.position);
            if (dist > hackInteractRange) return;

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (HackingSystem.Instance == null)
                {
                    Debug.Log("[CargoDropCrane] HackingSystem.Instance == null");
                    return;
                }

                if (HackingSystem.Instance.IsHacking)
                {
                    Debug.Log("[CargoDropCrane] 이미 다른 해킹 진행 중");
                    return;
                }

                HackableObjectType randomType = GetRandomHackableObjectType();

                Debug.Log($"[CargoDropCrane] 해킹 시작 | 랜덤 미니게임: {randomType}");

                HackingSystem.Instance.StartHacking(
                    randomType,
                    OnHackSucceededFromSystem,
                    OnHackFailedFromSystem,
                    transform.position
                );
            }
        }

        private HackableObjectType GetRandomHackableObjectType()
        {
            HackableObjectType[] types =
            {
                HackableObjectType.SecurityTurret,
                HackableObjectType.ElectricFence,
                HackableObjectType.SatelliteUplink,
                HackableObjectType.SynapseServer,
                HackableObjectType.MagneticBeacon
            };

            return types[Random.Range(0, types.Length)];
        }

        private void OnHackSucceededFromSystem()
        {
            float duration = Random.Range(hackedDurationMin, hackedDurationMax);

            Debug.Log($"[CargoDropCrane] 해킹 성공 -> 크레인 해킹 모드 시작 | duration={duration:F1}");

            StartHackMode(duration);
        }

        private void OnHackFailedFromSystem()
        {
            if (currentPlayer != null)
            {
                currentPlayer.AddPsychoCorruption(50f);
                Debug.Log("[CargoDropCrane] 해킹 실패 -> 사이코잠식도 +50%");
            }
        }

        /// <summary>
        /// 해킹 모드 시작
        /// - 기본 루틴 중단
        /// - 해킹 색상 적용
        /// - 해킹 전용 루틴 시작
        /// </summary>
        private void StartHackMode(float duration)
        {
            isHacked = true;
            hackedRemainTime = duration;
            hackLogTimer = 0f;

            if (hackDebugLog)
                Debug.Log($"[CargoDropCrane] 해킹 시작! duration={duration:F1}초");

            if (craneRenderer != null)
                craneRenderer.color = hackedColor;

            // 기본 루틴 정지
            if (dropRoutine != null)
            {
                StopCoroutine(dropRoutine);
                dropRoutine = null;
            }

            if (hackedRoutine != null)
                StopCoroutine(hackedRoutine);

            hackedRoutine = StartCoroutine(HackedModeRoutine(duration));
        }

        /// <summary>
        /// 해킹 모드 코루틴
        /// - 일정 시간 동안 적을 추적해 경고/낙하를 반복
        /// </summary>
        private IEnumerator HackedModeRoutine(float duration)
        {
            isDropping = false;

            float elapsed = 0f;

            yield return new WaitForSeconds(firstDelay);
            elapsed += firstDelay;
            hackedRemainTime = Mathf.Max(0f, duration - elapsed);

            while (elapsed < duration)
            {
                // 플레이어 근처 적을 찾아 낙하 지점 계산
                Vector3 targetPos = GetTrackedEnemyDropPosition();

                SpawnWarning(targetPos);

                if (debugLog)
                    Debug.Log($"[CargoDropCrane] 해킹모드 Warning Spawn | pos={targetPos}");

                yield return new WaitForSeconds(warningDuration);
                elapsed += warningDuration;
                hackedRemainTime = Mathf.Max(0f, duration - elapsed);

                SpawnImpact(targetPos);

                if (debugLog)
                    Debug.Log($"[CargoDropCrane] 해킹모드 Impact Spawn | pos={targetPos}");

                float nextDelay = Random.Range(intervalMin, intervalMax);
                yield return new WaitForSeconds(nextDelay);
                elapsed += nextDelay;
                hackedRemainTime = Mathf.Max(0f, duration - elapsed);
            }

            // 해킹 종료 후 기본 상태 복귀
            isHacked = false;
            hackedRemainTime = 0f;
            hackedRoutine = null;

            if (craneRenderer != null)
                craneRenderer.color = normalColor;

            if (hackDebugLog)
                Debug.Log("[CargoDropCrane] 해킹 종료 → 기본 모드 복귀");
        }

        /// <summary>
        /// 기본 모드 코루틴
        /// - 플레이어 위치를 추적해 경고/낙하 반복
        /// </summary>
        private IEnumerator DropRoutine_PlayerTrack()
        {
            isDropping = true;

            yield return new WaitForSeconds(firstDelay);

            while (playerInsideActiveArea && !isHacked)
            {
                Vector3 targetPos = GetPlayerTrackedDropPosition();

                SpawnWarning(targetPos);

                // [추가]
                // Warning이 찍힌 순간부터는 구역 밖으로 나가도 Impact까지 보장
                attackCycleInProgress = true;

                if (debugLog)
                    Debug.Log($"[CargoDropCrane] 기본모드 Warning Spawn | pos={targetPos}");

                yield return new WaitForSeconds(warningDuration);

                if (isHacked)
                    break;

                // [유지]
                // 공격은 반드시 Warning이 찍힌 위치에 발생
                SpawnImpact(targetPos);

                // [추가]
                // Warning -> Impact 한 사이클 종료
                attackCycleInProgress = false;

                if (debugLog)
                    Debug.Log($"[CargoDropCrane] 기본모드 Impact Spawn | pos={targetPos}");

                // [추가]
                // Impact 후 구역 밖이면 추가 공격 없이 루틴 종료
                if (!playerInsideActiveArea)
                    break;

                float nextDelay = Random.Range(intervalMin, intervalMax);
                yield return new WaitForSeconds(nextDelay);
            }

            // [추가]
            // 루틴이 어떤 이유로 끝나든 상태 초기화
            attackCycleInProgress = false;
            isDropping = false;
            dropRoutine = null;
        }

        /// <summary>
        /// 경고 프리팹 생성 후 CargoDropWarning 초기화
        /// </summary>
        private void SpawnWarning(Vector3 targetPos)
        {
            if (warningPrefab == null) return;

            GameObject warningObj = Instantiate(warningPrefab, targetPos, Quaternion.identity);

            CargoDropWarning warning = warningObj.GetComponent<CargoDropWarning>();
            if (warning != null)
            {
                warning.Initialize(warningDuration, blink, blinkSpeed);
            }
        }

        /// <summary>
        /// 충돌 판정 프리팹 생성 후 CargoContainerImpact 초기화
        /// </summary>
        private void SpawnImpact(Vector3 targetPos)
        {
            if (impactPrefab == null) return;

            GameObject impactObj = Instantiate(impactPrefab, targetPos, Quaternion.identity);

            CargoContainerImpact impact = impactObj.GetComponent<CargoContainerImpact>();
            if (impact != null)
            {
                impact.Initialize(
                    impactSize,
                    playerDamage,
                    enemyDamage,
                    impactDestroyAfter
                );
            }
        }

        /// <summary>
        /// 기본 모드에서 플레이어 위치를 기반으로 낙하 좌표 계산
        /// </summary>
        private Vector3 GetPlayerTrackedDropPosition()
        {
            if (currentPlayer == null)
                return transform.position;

            Vector3 pos = currentPlayer.transform.position;
            pos.z = 0f;
            pos.y += trackOffsetY;
            return pos;
        }

        /// <summary>
        /// 해킹 모드에서 플레이어 근처 적을 탐색하여 낙하 좌표 계산
        /// - 가장 가까운 살아있는 Enemy를 찾음
        /// - 찾지 못하면 플레이어 위치로 대체
        /// </summary>
        private Vector3 GetTrackedEnemyDropPosition()
        {
            if (currentPlayer == null)
                return transform.position;

            Vector3 playerPos = currentPlayer.transform.position;

            Collider2D[] hits = Physics2D.OverlapCircleAll(
                playerPos,
                enemySearchRadius
            );

            List<Enemy> enemies = new List<Enemy>();

            foreach (var hit in hits)
            {
                if (hit == null) continue;

                Enemy enemy = hit.GetComponentInParent<Enemy>();
                if (enemy == null) continue;
                if (enemy.IsDead) continue;

                if (!enemies.Contains(enemy))
                    enemies.Add(enemy);
            }

            if (enemies.Count == 0)
            {
                if (hackDebugLog)
                    Debug.Log("[CargoDropCrane] 적 탐색 실패 → 플레이어 위치 대체");

                return GetPlayerTrackedDropPosition();
            }

            Enemy bestCenterEnemy = null;
            int bestClusterCount = -1;
            float bestDistanceToPlayer = float.MaxValue;

            // 각 적을 중심점 후보로 보고,
            // impactSize 범위 안에 몇 마리의 적이 들어오는지 계산
            foreach (Enemy centerEnemy in enemies)
            {
                Vector3 centerPos = centerEnemy.transform.position;

                int clusterCount = 0;

                foreach (Enemy otherEnemy in enemies)
                {
                    Vector3 otherPos = otherEnemy.transform.position;

                    bool insideX = Mathf.Abs(otherPos.x - centerPos.x) <= impactSize.x * 0.5f;
                    bool insideY = Mathf.Abs(otherPos.y - centerPos.y) <= impactSize.y * 0.5f;

                    if (insideX && insideY)
                        clusterCount++;
                }

                float distanceToPlayer = (centerPos - playerPos).sqrMagnitude;

                bool isBetter =
                    clusterCount > bestClusterCount ||
                    (clusterCount == bestClusterCount && distanceToPlayer < bestDistanceToPlayer);

                if (isBetter)
                {
                    bestClusterCount = clusterCount;
                    bestDistanceToPlayer = distanceToPlayer;
                    bestCenterEnemy = centerEnemy;
                }
            }

            if (bestCenterEnemy != null)
            {
                Vector3 pos = bestCenterEnemy.transform.position;
                pos.z = 0f;
                pos.y += trackOffsetY;

                if (hackDebugLog)
                {
                    Debug.Log(
                        $"[CargoDropCrane] 해킹 타겟 무리 확정 | " +
                        $"centerEnemy={bestCenterEnemy.name}, clusterCount={bestClusterCount}"
                    );
                }

                return pos;
            }

            return GetPlayerTrackedDropPosition();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 활성 구역
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, activeAreaSize);

            // 해킹 가능 거리
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, hackInteractRange);

            // 적 탐색 거리
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, enemySearchRadius);
        }
#endif
    }
}