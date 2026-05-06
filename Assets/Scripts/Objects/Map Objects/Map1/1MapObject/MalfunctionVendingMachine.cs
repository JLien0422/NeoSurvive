using UnityEngine;
using NeoSurvive.Core;
using NeoSurvive.Balancing.Map; 

namespace NeoSurvive.Map.Map1.MapObjects    
{
    /// <summary>
    /// [Map1 파괴형 오브젝트]
    /// 오작동하는 자판기.
    ///
    /// [역할]
    /// - 공격받아 체력이 감소함
    /// - 체력이 0 이하가 되면 파괴되고 드롭 아이템 생성
    /// - 일정 거리 이상 플레이어와 멀어지면 일정 시간 후 자동 삭제
    /// - 랜덤 스파크 연출 가능
    /// - Sprite 크기에 맞춰 Collider 자동 조정 가능
    ///
    /// [연결 구조]
    /// 1. MapObjectLoader.DB
    ///    -> CSV 기반 밸런싱 값 로드
    /// 2. 무기/투사체/근접공격
    ///    -> TakeDamage(float) 호출로 체력 감소
    /// 3. 드롭 프리팹
    ///    -> nutritionTubePrefab, creditPrefab 생성
    /// 4. 플레이어 Transform
    ///    -> 거리 계산 후 자동 삭제 처리
    /// 5. 이펙트 프리팹
    ///    -> 파괴 이펙트, 스파크 이펙트 생성
    ///
    /// [병합 시 주의]
    /// - 공격 시스템이 이 오브젝트를 제대로 인식하고 TakeDamage를 호출해야 함
    /// - Collider/Layer/Tag가 팀원의 무기 판정 구조와 어긋나면 공격이 안 들어갈 수 있음
    /// - MapObjectLoader.DB 초기화 순서가 꼬이면 CSV 적용이 안 되고 인스펙터 기본값으로 동작할 수 있음
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class MalfunctionVendingMachine : MonoBehaviour, IDamageable
    {
        [Header("CSV")]
        [SerializeField] private string mapId = "Map1";
        [SerializeField] private string objectId = "malfunction_vending_machine";

        [Header("내구도")]
        [SerializeField] private float maxHp = 20f;

        [Header("드롭 아이템")]
        [SerializeField] private GameObject nutritionTubePrefab;
        [SerializeField] private GameObject creditPrefab;

        [Header("드롭 방식")]
        [SerializeField] private bool dropRandomOne = true;
        [SerializeField] private bool dropBoth = false;

        [Header("드롭 위치")]
        [SerializeField] private Transform dropPoint;

        [Header("자동 삭제 설정")]
        [SerializeField] private float despawnDistance = 35f;
        [SerializeField] private float despawnDelayMin = 30f;
        [SerializeField] private float despawnDelayMax = 60f;

        [Header("파괴 / 연출")]
        [SerializeField] private GameObject destroyEffectPrefab;
        [SerializeField] private GameObject sparkEffectPrefab;

        [Header("스파크 설정")]
        [SerializeField] private bool enableRandomSpark = true;
        [SerializeField] private float sparkIntervalMin = 1.5f;
        [SerializeField] private float sparkIntervalMax = 4f;
        [SerializeField] private Transform sparkPoint;

        [Header("Collider Auto Fit")]
        [SerializeField] private bool autoFitCollider = true;
        [SerializeField] private Vector2 colliderPadding = Vector2.zero;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        // 현재 체력
        private float currentHp;

        // 플레이어 위치 참조 (거리 기반 despawn 계산용)
        private Transform player;

        // 이미 파괴되었는지 여부
        private bool isDestroyed = false;

        // 자동 삭제용 타이머
        private float despawnTimer = 0f;
        private float targetDespawnDelay = 0f;

        // 랜덤 스파크용 타이머
        private float sparkTimer = 0f;
        private float nextSparkDelay = 0f;

        private void Awake()
        {
            // 현재는 비워져 있음
            // 필요 시 초기 참조 캐싱이나 사전 검증을 여기 넣을 수 있음
        }

        private void Start()
        {
            // 1) CSV 데이터 로드
            LoadFromCSV();

            // 2) 현재 체력 초기화
            currentHp = maxHp;

            // 3) 자동 삭제 목표 시간 랜덤 설정
            targetDespawnDelay = Random.Range(despawnDelayMin, despawnDelayMax);

            // 4) 스파크 타이머 초기화
            ResetSparkTimer();

            // 5) Collider를 Sprite 크기에 맞춤
            if (autoFitCollider)
                FitColliderToSprite();

            // 6) 플레이어 Transform 찾기
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        private void Update()
        {
            if (isDestroyed)
                return;

            // 플레이어와 멀어졌는지 확인 후 자동 삭제 처리
            HandleAutoDespawn();

            // 랜덤 스파크 연출 처리
            HandleRandomSpark();
        }

        /// <summary>
        /// CSV DB에서 자판기 설정값을 읽어와 적용.
        ///
        /// [중요]
        /// - Loader가 준비되지 않으면 DB null
        /// - 이 경우 maxHp 등은 인스펙터 기본값으로 남는다
        /// - 즉, CSV 100으로 설정했는데 실제로 20이 나오는 문제는
        ///   대부분 여기 DB 초기화 순서 문제일 가능성이 높음
        /// </summary>
        private void LoadFromCSV()
        {
            if (MapObjectLoader.DB == null)
            {
                Debug.LogError("[MalfunctionVendingMachine] MapObjectLoader.DB가 null임");
                return;
            }

            var row = MapObjectLoader.DB.Get(mapId, objectId);
            if (row == null)
            {
                Debug.LogWarning($"[MalfunctionVendingMachine] CSV 데이터 없음 | {mapId}:{objectId}");
                return;
            }

            maxHp = row.maxHp;
            dropRandomOne = row.dropRandomOne;
            dropBoth = row.dropBoth;
            despawnDistance = row.despawnDistance;
            despawnDelayMin = row.despawnDelayMin;
            despawnDelayMax = row.despawnDelayMax;
            enableRandomSpark = row.enableRandomSpark;
            sparkIntervalMin = row.sparkIntervalMin;
            sparkIntervalMax = row.sparkIntervalMax;
            autoFitCollider = row.autoFit;
            colliderPadding = new Vector2(row.colliderPaddingX, row.colliderPaddingY);

            if (debugLog)
            {
                Debug.Log(
                    $"[MalfunctionVendingMachine] CSV 적용 | " +
                    $"maxHp={maxHp}, dropRandomOne={dropRandomOne}, dropBoth={dropBoth}, " +
                    $"despawnDistance={despawnDistance}, despawnDelayMin={despawnDelayMin}, despawnDelayMax={despawnDelayMax}, " +
                    $"enableRandomSpark={enableRandomSpark}, sparkIntervalMin={sparkIntervalMin}, sparkIntervalMax={sparkIntervalMax}, " +
                    $"autoFitCollider={autoFitCollider}, colliderPadding={colliderPadding}"
                );
            }
        }

        /// <summary>
        /// 외부에서 플레이어 Transform을 직접 주입하고 싶을 때 사용 가능.
        /// 지금은 FindWithTag를 쓰지만, 나중에 맵 생성 시스템이나 스폰 시스템과 연동 시
        /// SetPlayer 방식이 더 안전할 수 있음.
        /// </summary>
        public void SetPlayer(Transform targetPlayer)
        {
            player = targetPlayer;
        }

        /// <summary>
        /// 무기/투사체/근접 타격 쪽에서 호출하는 핵심 함수.
        ///
        /// [다른 시스템과 연결되는 지점]
        /// - Projectile, Beam, MeleeArea, WeaponHitbox 등이
        ///   이 오브젝트를 감지한 뒤 이 함수를 호출해야 파괴 로직이 작동한다.
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (isDestroyed)
                return;

            currentHp -= damage;

            if (debugLog)
                Debug.Log($"[MalfunctionVendingMachine] Hit | name={name} | damage={damage} | currentHp={currentHp}");

            if (currentHp <= 0f)
            {
                BreakObject();
            }
        }

        /// <summary>
        /// 플레이어와 거리가 멀어졌을 때 일정 시간 누적 후 삭제.
        /// 무한맵/청크맵에서 멀어진 오브젝트 정리를 위한 구조로 볼 수 있음.
        /// </summary>
        private void HandleAutoDespawn()
        {
            if (player == null)
                return;

            float dist = Vector3.Distance(transform.position, player.position);

            if (dist > despawnDistance)
            {
                despawnTimer += Time.deltaTime;

                if (despawnTimer >= targetDespawnDelay)
                {
                    if (debugLog)
                        Debug.Log($"[MalfunctionVendingMachine] Auto Despawn | name={name}");

                    RemoveSelf();
                }
            }
            else
            {
                // 다시 가까워지면 타이머 초기화
                despawnTimer = 0f;
            }
        }

        /// <summary>
        /// 랜덤 간격으로 스파크 이펙트를 생성.
        /// 기능성보다 분위기/연출 목적.
        /// </summary>
        private void HandleRandomSpark()
        {
            if (!enableRandomSpark || sparkEffectPrefab == null)
                return;

            sparkTimer += Time.deltaTime;

            if (sparkTimer < nextSparkDelay)
                return;

            Vector3 spawnPos = sparkPoint != null ? sparkPoint.position : transform.position;
            Instantiate(sparkEffectPrefab, spawnPos, Quaternion.identity);

            if (debugLog)
                Debug.Log($"[MalfunctionVendingMachine] Spark | name={name}");

            ResetSparkTimer();
        }

        private void ResetSparkTimer()
        {
            sparkTimer = 0f;
            nextSparkDelay = Random.Range(sparkIntervalMin, sparkIntervalMax);
        }

        /// <summary>
        /// 체력이 0 이하가 되었을 때 호출되는 파괴 처리.
        ///
        /// [연결 구조]
        /// - 드롭 아이템 생성
        /// - 파괴 이펙트 생성
        /// - 마지막으로 자기 자신 삭제
        ///
        /// [주의]
        /// - dropBoth와 dropRandomOne이 동시에 true일 경우
        ///   현재 구조상 dropBoth가 우선 처리됨
        /// </summary>
        private void BreakObject()
        {
            if (isDestroyed)
                return;

            isDestroyed = true;

            Vector3 spawnPos = dropPoint != null ? dropPoint.position : transform.position;

            if (dropBoth)
            {
                if (nutritionTubePrefab != null)
                {
                    Instantiate(nutritionTubePrefab, spawnPos + new Vector3(-0.3f, 0f, 0f), Quaternion.identity);
                }

                if (creditPrefab != null)
                {
                    Instantiate(creditPrefab, spawnPos + new Vector3(0.3f, 0f, 0f), Quaternion.identity);
                }
            }
            else if (dropRandomOne)
            {
                int rand = Random.Range(0, 2);

                if (rand == 0 && nutritionTubePrefab != null)
                {
                    Instantiate(nutritionTubePrefab, spawnPos, Quaternion.identity);
                }
                else if (rand == 1 && creditPrefab != null)
                {
                    Instantiate(creditPrefab, spawnPos, Quaternion.identity);
                }
            }

            if (destroyEffectPrefab != null)
            {
                Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
            }

            if (debugLog)
                Debug.Log($"[MalfunctionVendingMachine] Destroyed | name={name}");

            RemoveSelf();
        }

        private void RemoveSelf()
        {
            Destroy(gameObject);
        }

        /// <summary>
        /// Sprite 크기에 맞춰 Collider 자동 보정.
        ///
        /// [지원 형태]
        /// - BoxCollider2D
        /// - CircleCollider2D
        ///
        /// [팀 병합 시 의미]
        /// - 프리팹마다 Collider 타입이 다를 수 있으므로 최소한의 유연성 확보
        /// - 다만 PolygonCollider2D 같은 다른 타입은 현재 지원하지 않음
        /// </summary>
        private void FitColliderToSprite()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            Collider2D col = GetComponent<Collider2D>();

            if (sr == null || sr.sprite == null || col == null)
                return;

            Vector2 size = (Vector2)sr.sprite.bounds.size + colliderPadding;
            Vector2 offset = (Vector2)sr.sprite.bounds.center;

            if (col is BoxCollider2D box)
            {
                box.size = size;
                box.offset = offset;
            }
            else if (col is CircleCollider2D circle)
            {
                float radius = Mathf.Max(size.x, size.y) * 0.5f;
                circle.radius = radius;
                circle.offset = offset;
            }

            if (debugLog)
                Debug.Log($"[MalfunctionVendingMachine] Collider Fit | size={size} | offset={offset}");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 프리팹 수정 중에도 Collider 자동 반영
            if (autoFitCollider)
                FitColliderToSprite();
        }
#endif
    }
}