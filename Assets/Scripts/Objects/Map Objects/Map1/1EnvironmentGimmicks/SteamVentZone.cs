using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Buff;
using NeoSurvive.Core;
using NeoSurvive.Balancing.Map;
using NeoSurvive.Characters;

namespace NeoSurvive.Map.Map1.Gimmicks
{
    /// <summary>
    /// [Map1 Gimmick]
    /// 증기 환풍구 구역 기믹
    ///
    /// [역할]
    /// - Trigger 구역 안에 들어온 플레이어/적을 추적
    /// - 일정 주기(pulse)마다 상태 이상 효과 적용
    /// - 기본 상태에서는 슬로우(slow) 적용
    /// - 해킹 성공 후에는 적에게 기절(stun) 효과 적용
    ///
    /// [연결 구조]
    /// - CSV: MapEnvironmentGimmickLoader.DB에서 설정값 로드
    /// - HackableObject + HackingSystem: 해킹 시작 처리
    /// - BuffHandler / SlowDebuff / StunDebuff: 상태 이상 적용
    /// - Player: 해킹 가능한 해커 플레이어 판별
    /// - Enemy: 해킹 후 스턴 대상
    ///
    /// [실행 흐름]
    /// Awake:
    ///   - zoneTrigger / zoneRenderer / hackableObject 캐싱
    ///   - Trigger 설정, Collider 자동 맞춤, 비주얼 적용
    ///
    /// Start:
    ///   - CSV 로드
    ///
    /// Update:
    ///   - 해킹 시작 입력 체크
    ///   - pulse 타이머 누적 및 ExecutePulse 실행
    ///
    /// [핵심 특징]
    /// - 이 환풍구 전용 slowBuffId를 사용하여
    ///   다른 슬로우 버프와 섞이지 않고 "이 환풍구가 건 슬로우만" 제거 가능
    ///
    /// [Git 병합 시 주의]
    /// - MapEnvironmentGimmickLoader.DB가 null이면 CSV 적용이 안 됨
    /// - HackableObject, HackingSystem.Instance가 없으면 해킹 시작 불가
    /// - targetMask 설정이 틀리면 플레이어/적이 구역 안에 있어도 효과가 안 들어갈 수 있음
    /// - 해킹 성공/종료 시 OnHackSucceededFromSystem / OnHackEndedFromSystem을
    ///   실제 해킹 시스템에서 호출해줘야 완전하게 연결됨
    ///
    /// [QA 체크 포인트]
    /// 1. 구역 안 플레이어/적에게 슬로우가 주기적으로 적용되는가
    /// 2. 구역 밖으로 나가면 슬로우가 제거되는가
    /// 3. 해커만 E 입력으로 해킹 시작 가능한가
    /// 4. 해킹 성공 후 적에게 stun이 들어가는가
    /// 5. 해킹 성공 시 비주얼 색상이 바뀌는가
    /// 6. targetMask에 따라 대상 필터링이 정상 동작하는가
    /// </summary>
    [DefaultExecutionOrder(-100)] // [추가] HackableObject보다 먼저 Update가 실행되도록 설정
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SteamVentZone : MonoBehaviour
    {
        [Header("CSV")]
        [SerializeField] private string mapId = "Map1";
        [SerializeField] private string gimmickId = "steam_vent";

        [Header("Zone")]
        // Trigger 영역 콜라이더
        [SerializeField] private Collider2D zoneTrigger;

        [Header("Pulse")]
        // 시작 시 활성화 상태
        [SerializeField] private bool startActive = true;

        // 펄스 간격
        [SerializeField] private float pulseInterval = 0.25f;

        [Header("Normal Effect")]
        // 기본 상태 슬로우 배율
        [SerializeField] private float slowMultiplier = 0.5f;

        // 슬로우 지속 시간
        [SerializeField] private float slowDuration = 0.35f;

        [Header("Hack")]
        // 해킹 시작 키
        [SerializeField] private KeyCode hackKey = KeyCode.E;

        // 플레이어 태그(현재 직접 비교에는 거의 안 쓰지만 유지)
        [SerializeField] private string playerTag = "Player";

        // 해킹 가능한 오브젝트 연결
        [SerializeField] private HackableObject hackableObject;

        [Header("After Hack")]
        // 해킹 후 적에게 적용할 기절 시간
        [SerializeField] private float stunDuration = 1.25f;

        // 적 태그
        [SerializeField] private string enemyTag = "Enemy";

        [Header("Target Filter")]
        // 효과 적용 대상 레이어 필터
        [SerializeField] private LayerMask targetMask;

        [Header("Visual")]
        // 환풍구 영역 표시용 SpriteRenderer
        [SerializeField] private SpriteRenderer zoneRenderer;

        // 기본 상태 색상
        [SerializeField] private Color normalColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);

        // 해킹 후 색상
        [SerializeField] private Color hackedColor = new Color(0.5f, 0.9f, 1f, 0.6f);

        [Header("Auto Fit")]
        // Sprite 크기에 따라 Collider 자동 보정 여부
        [SerializeField] private bool autoFitCollider = true;

        // Collider 여유값
        [SerializeField] private Vector2 colliderPadding = Vector2.zero;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        // 현재 구역 내부에 있는 Collider 추적
        private readonly HashSet<Collider2D> insideTargets = new HashSet<Collider2D>();

        // 펄스 타이머
        private float pulseTimer = 0f;

        // 해킹 완료 여부
        private bool isHacked = false;

        // 해킹 가능 여부(CSV에서 로드)
        private bool canHack = true;

        // 해킹 요청이 이미 진행 중인지
        private bool isHackRequestPending = false;

        // 현재 구역 안에 있는 플레이어 참조
        private Player currentPlayerInZone;

        // 이 환풍구 전용 슬로우 버프 ID
        private uint slowBuffId;

        private void Awake()
        {
            if (zoneTrigger == null)
                zoneTrigger = GetComponent<Collider2D>();

            if (zoneRenderer == null)
                zoneRenderer = GetComponent<SpriteRenderer>();

            if (hackableObject == null)
                hackableObject = GetComponent<HackableObject>();

            if (zoneTrigger != null)
                zoneTrigger.isTrigger = true;

            if (autoFitCollider)
                FitColliderToSprite();

            ApplyVisualState();

            // 인스턴스별 고유 ID 생성
            slowBuffId = (uint)(100000 + Mathf.Abs(GetInstanceID()));
        }

        private void Start()
        {
            LoadFromCSV();
        }

        private void Update()
        {
            // 해킹 입력 처리
            HandleHackStartInput();

            // 펄스 실행 처리
            HandlePulse();
        }

        /// <summary>
        /// CSV 로드
        /// </summary>
        private void LoadFromCSV()
        {
            Debug.Log("[SteamVentZone] LoadFromCSV 진입");

            var row = MapEnvironmentGimmickLoader.DB.Get(mapId, gimmickId);
            if (row == null)
            {
                Debug.LogWarning("[SteamVentZone] DB가 null임");
                return;
            }

            startActive = row.isActive;
            canHack = row.canHack;
            pulseInterval = row.pulseInterval;

            // 기본 상태 효과 타입에 따라 슬로우 값 적용
            if (row.effectType == "slow")
            {
                slowMultiplier = row.effectValue;
                slowDuration = row.effectDuration;
            }

            // 해킹 후 효과 타입에 따라 stun 값 적용
            if (row.hackedEffectType == "stun")
            {
                stunDuration = row.hackedEffectDuration;
            }

            if (debugLog)
            {
                Debug.Log(
                    $"[SteamVentZone] CSV 로드 완료 | " +
                    $"startActive={startActive}, canHack={canHack}, pulseInterval={pulseInterval}, " +
                    $"slowMultiplier={slowMultiplier}, slowDuration={slowDuration}, stunDuration={stunDuration}"
                );
            }
        }

        /// <summary>
        /// 해킹 시작 입력 처리
        /// - 해커 플레이어가 구역 안에 있어야 함
        /// - HackableObject, HackingSystem이 정상 연결돼 있어야 함
        /// - 이미 요청 중이면 중복 입력 방지
        /// </summary>
        private void HandleHackStartInput()
        {
            if (!canHack)
            {
                Debug.Log("[SteamVentZone] canHack=false");
                return;
            }

            if (isHacked)
            {
                Debug.Log("[SteamVentZone] 이미 해킹 완료 상태");
                return;
            }

            if (isHackRequestPending)
            {
                Debug.Log("[SteamVentZone] 이미 해킹 요청 진행 중");
                return;
            }

            if (currentPlayerInZone == null)
            {
                Debug.Log("[SteamVentZone] currentPlayerInZone == null");
                return;
            }

            Debug.Log($"[SteamVentZone] currentPlayerInZone={currentPlayerInZone.name}, CharacterType={currentPlayerInZone.CharacterType}");

            if (!currentPlayerInZone.CharacterType.Equals(CharacterType.Hacker))
            {
                Debug.Log("[SteamVentZone] 플레이어가 해커가 아님");
                return;
            }

            if (hackableObject == null)
            {
                Debug.Log("[SteamVentZone] hackableObject == null");
                return;
            }

            if (HackingSystem.Instance == null)
            {
                Debug.Log("[SteamVentZone] HackingSystem.Instance == null");
                return;
            }

            if (Input.GetKeyDown(hackKey))
            {
                if (HackingSystem.Instance.IsHacking)
                {
                    Debug.Log("[SteamVentZone] 이미 다른 해킹 진행 중");
                    return;
                }

                isHackRequestPending = true;

                HackableObjectType randomType = GetRandomHackableObjectType();

                Debug.Log($"[SteamVentZone] 해킹 시작 | 랜덤 미니게임: {randomType}");

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

        /// <summary>
        /// 해킹 종료/실패 시 해킹 시스템에서 호출해줘야 하는 함수
        /// </summary>
        public void OnHackEndedFromSystem()
        {
            isHackRequestPending = false;
        }

        /// <summary>
        /// [추가]
        /// 해킹 시스템에서 실패 시 호출되는 함수
        /// - 해킹 요청 대기 상태 해제
        /// - 실패 벌칙으로 현재 구역 안의 플레이어에게 사이코 잠식도 증가 적용
        /// </summary>
        private void OnHackFailedFromSystem()
        {
            isHackRequestPending = false;

            if (currentPlayerInZone != null)
            {
                currentPlayerInZone.AddPsychoCorruption(50f);
                Debug.Log("[SteamVentZone] 해킹 실패 -> 사이코잠식도 +50%");
            }
        }

        /// <summary>
        /// 해킹 시스템에서 성공 시 호출해줘야 하는 함수
        /// - 상태를 해킹 완료로 전환
        /// - 현재 구역 안의 슬로우 제거
        /// - 비주얼 상태 변경
        /// </summary>
        public void OnHackSucceededFromSystem()
        {
            if (isHacked) return;

            isHacked = true;
            isHackRequestPending = false;
            ApplyVisualState();

            // 현재 안에 있는 대상의 이 환풍구 슬로우 제거
            ClearSlowFromAllInsideTargets();

            if (debugLog)
                Debug.Log("[SteamVentZone] Hack Success");
        }

        /// <summary>
        /// 펄스 타이머 누적 후 주기마다 ExecutePulse 실행
        /// </summary>
        private void HandlePulse()
        {
            if (!startActive) return;

            pulseTimer += Time.deltaTime;
            if (pulseTimer < pulseInterval) return;

            pulseTimer = 0f;
            ExecutePulse();
        }

        /// <summary>
        /// 현재 구역 안에 있는 대상을 순회하며 효과 적용
        /// 기본 상태: 플레이어/적 슬로우
        /// 해킹 상태: 적 stun
        /// </summary>
        private void ExecutePulse()
        {
            var snapshot = new List<Collider2D>(insideTargets);

            foreach (var hitCol in snapshot)
            {
                if (hitCol == null) continue;

                GameObject hitObject = hitCol.gameObject;

                // 1) 플레이어 처리
                Player player = hitObject.GetComponentInParent<Player>();
                if (player != null)
                {
                    if (!IsInTargetMask(player.gameObject.layer))
                        continue;

                    if (!isHacked)
                    {
                        ApplyOrRefreshSlow(player.gameObject);

                        if (debugLog)
                            Debug.Log($"[SteamVentZone] Player Slow Refresh | {player.name}");
                    }

                    continue;
                }

                // 2) 적 처리
                Transform root = hitObject.transform.root;
                if (root == null) continue;
                if (!root.CompareTag(enemyTag)) continue;
                if (!IsInTargetMask(root.gameObject.layer)) continue;

                BuffHandler enemyBuff = root.GetComponent<BuffHandler>();
                if (enemyBuff == null)
                    enemyBuff = root.gameObject.AddComponent<BuffHandler>();

                if (isHacked)
                {
                    enemyBuff.AddBuff(new StunDebuff(stunDuration));

                    if (debugLog)
                        Debug.Log($"[SteamVentZone] Enemy Stun | {root.name}");
                }
                else
                {
                    ApplyOrRefreshSlow(root.gameObject);

                    if (debugLog)
                        Debug.Log($"[SteamVentZone] Enemy Slow Refresh | {root.name}");
                }
            }
        }

        /// <summary>
        /// 슬로우 버프 적용 또는 갱신
        /// - 같은 slowBuffId를 사용해 동일 환풍구의 슬로우를 덮어씌우도록 구성
        /// </summary>
        private void ApplyOrRefreshSlow(GameObject targetRoot)
        {
            if (targetRoot == null) return;

            BuffHandler buffHandler = targetRoot.GetComponent<BuffHandler>();
            if (buffHandler == null)
                buffHandler = targetRoot.AddComponent<BuffHandler>();

            buffHandler.AddBuffWithId(
                slowBuffId,
                new SlowDebuff(slowMultiplier, slowDuration),
                1,
                slowDuration,
                true
            );
        }

        /// <summary>
        /// 이 환풍구가 건 슬로우만 제거
        /// </summary>
        private void RemoveSlow(GameObject targetRoot)
        {
            if (targetRoot == null) return;

            BuffHandler buffHandler = targetRoot.GetComponent<BuffHandler>();
            if (buffHandler == null) return;

            buffHandler.RemoveBuffById(slowBuffId);

            if (debugLog)
                Debug.Log($"[SteamVentZone] Slow Removed | {targetRoot.name}");
        }

        /// <summary>
        /// 현재 구역 안 대상 전체에서 슬로우 제거
        /// - 해킹 성공 시 사용
        /// </summary>
        private void ClearSlowFromAllInsideTargets()
        {
            var snapshot = new List<Collider2D>(insideTargets);

            foreach (var hitCol in snapshot)
            {
                if (hitCol == null) continue;

                Player player = hitCol.GetComponentInParent<Player>();
                if (player != null)
                {
                    RemoveSlow(player.gameObject);
                    continue;
                }

                Transform root = hitCol.transform.root;
                if (root != null)
                    RemoveSlow(root.gameObject);
            }
        }

        /// <summary>
        /// 현재 해킹 상태에 따라 색상 적용
        /// </summary>
        private void ApplyVisualState()
        {
            if (zoneRenderer != null)
                zoneRenderer.color = isHacked ? hackedColor : normalColor;
        }

        /// <summary>
        /// Sprite 크기에 맞게 Trigger Collider 자동 보정
        /// </summary>
        public void FitColliderToSprite()
        {
            if (zoneRenderer == null || zoneRenderer.sprite == null || zoneTrigger == null)
                return;

            Vector2 size = (Vector2)zoneRenderer.sprite.bounds.size + colliderPadding;
            Vector2 offset = (Vector2)zoneRenderer.sprite.bounds.center;

            if (zoneTrigger is BoxCollider2D box)
            {
                box.size = size;
                box.offset = offset;
            }
            else if (zoneTrigger is CircleCollider2D circle)
            {
                float radius = Mathf.Max(size.x, size.y) * 0.5f;
                circle.radius = radius;
                circle.offset = offset;
            }
        }

        /// <summary>
        /// 구역 진입 시 대상 등록
        /// - 플레이어가 들어오면 해킹 가능 대상 플레이어로 저장
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            insideTargets.Add(other);

            Player player = other.GetComponentInParent<Player>();
            if (player != null)
                currentPlayerInZone = player;
        }

        /// <summary>
        /// 구역 이탈 시 대상 제거
        /// - 플레이어/적에게 남은 슬로우 제거
        /// - 플레이어 이탈 시 해킹 대기 상태 해제
        /// </summary>
        private void OnTriggerExit2D(Collider2D other)
        {
            insideTargets.Remove(other);

            Player player = other.GetComponentInParent<Player>();
            if (player != null)
            {
                RemoveSlow(player.gameObject);

                if (currentPlayerInZone == player)
                {
                    currentPlayerInZone = null;
                    isHackRequestPending = false;
                }

                return;
            }

            Transform root = other.transform.root;
            if (root != null && root.CompareTag(enemyTag))
            {
                RemoveSlow(root.gameObject);
            }
        }

        /// <summary>
        /// targetMask에 포함된 레이어인지 확인
        /// </summary>
        private bool IsInTargetMask(int layer)
        {
            return (targetMask.value & (1 << layer)) != 0;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (zoneTrigger == null)
                zoneTrigger = GetComponent<Collider2D>();

            if (zoneRenderer == null)
                zoneRenderer = GetComponent<SpriteRenderer>();

            if (hackableObject == null)
                hackableObject = GetComponent<HackableObject>();

            if (zoneTrigger != null)
                zoneTrigger.isTrigger = true;

            if (autoFitCollider)
                FitColliderToSprite();
        }
#endif
    }
}