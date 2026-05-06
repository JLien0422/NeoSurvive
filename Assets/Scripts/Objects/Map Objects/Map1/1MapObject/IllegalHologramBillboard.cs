using UnityEngine;
using NeoSurvive.Balancing.Map;

namespace NeoSurvive.Map.Map1.MapObjects
{
    /// <summary>
    /// [Map1 특수 연출 오브젝트]
    /// 불법 개조 홀로그램 광고판.
    ///
    /// [역할]
    /// - Sprite 크기에 맞춰 Trigger 범위를 자동 설정
    /// - 플레이어가 영역 안에 들어오면 카메라 orthographicSize를 줄여 시야를 좁힘
    /// - 광고판 Sprite를 낮은 sorting order로 배치하여 바닥/하부 연출처럼 보이게 함
    ///
    /// [연결 구조]
    /// 1. MapObjectLoader.DB
    ///    -> CSV에서 이 오브젝트의 설정값 로드
    /// 2. SpriteRenderer
    ///    -> Trigger 영역 계산 기준, 렌더 순서 제어
    /// 3. BoxCollider2D
    ///    -> 플레이어 진입/이탈 감지
    /// 4. Camera
    ///    -> 플레이어 진입 시 시야 축소 연출
    ///
    /// [병합 시 중요]
    /// - 이 스크립트는 MapObjectLoader.DB 초기화 순서에 의존함
    /// - Loader가 먼저 올라오지 않으면 Start()에서 CSV 로드 실패 가능
    /// - 팀원이 씬 로딩 순서나 Loader 배치를 바꾸면 null 문제 발생 가능
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class IllegalHologramBillboard : MonoBehaviour
    {
        [Header("CSV")]
        // CSV DB 조회 키
        [SerializeField] private string mapId = "Map1";
        [SerializeField] private string objectId = "illegal_hologram_billboard";

        [Header("Auto Fit")]
        // 시작 시 Trigger를 Sprite 크기에 맞출지 여부
        [SerializeField] private bool autoFitOnAwake = true;
        // Trigger 영역 여유값
        [SerializeField] private Vector2 triggerPadding = Vector2.zero;

        [Header("Detection")]
        // 플레이어 인식 태그
        [SerializeField] private string playerTag = "Player";

        [Header("Camera")]
        // 연출 대상 카메라
        [SerializeField] private Camera targetCamera;
        // 플레이어가 영역 안에 있을 때 줄어든 카메라 크기
        [SerializeField] private float reducedOrthographicSize = 5.5f;
        // 카메라 줌 변화 속도
        [SerializeField] private float zoomLerpSpeed = 4f;

        [Header("Render Order")]
        // 광고판 렌더링 우선순위
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = -10;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        // 캐싱
        private SpriteRenderer sr;
        private BoxCollider2D boxCol;

        // 현재 플레이어가 영역 안에 있는지
        private bool playerInside = false;

        // 카메라 원래 크기 저장용
        private float normalOrthographicSize = 8f;

        private void Awake()
        {
            // 컴포넌트 참조 캐싱
            sr = GetComponent<SpriteRenderer>();
            boxCol = GetComponent<BoxCollider2D>();

            // 카메라가 수동 지정되지 않았으면 Main Camera 사용
            if (targetCamera == null)
                targetCamera = Camera.main;

            // 원래 카메라 크기를 저장해두고
            // 나중에 플레이어가 광고판 영역에서 나가면 복구한다.
            if (targetCamera != null)
                normalOrthographicSize = targetCamera.orthographicSize;
        }

        private void Start()
        {
            // 1) CSV 값으로 설정 불러오기
            LoadFromCSV();

            // 2) Sprite 크기에 맞춰 Trigger 맞추기
            if (autoFitOnAwake)
                FitTriggerToSprite();

            // 3) 렌더 순서 적용
            ApplyRenderOrder();
        }

        private void Update()
        {
            if (targetCamera == null) return;

            // 플레이어가 영역 안에 있으면 축소값, 아니면 원래 크기
            float targetSize = playerInside ? reducedOrthographicSize : normalOrthographicSize;

            // 부드럽게 카메라 줌 변화
            targetCamera.orthographicSize = Mathf.Lerp(
                targetCamera.orthographicSize,
                targetSize,
                Time.deltaTime * zoomLerpSpeed
            );
        }

        /// <summary>
        /// MapObjectLoader.DB에서 CSV 설정을 읽어와 인스펙터 값을 덮어쓴다.
        ///
        /// [중요]
        /// - 이 함수가 정상 작동하려면 MapObjectLoader.DB가 먼저 초기화되어 있어야 함
        /// - 즉, 씬에 Loader가 존재하고 Awake/Start 순서상 먼저 로드되어야 함
        /// </summary>
        private void LoadFromCSV()
        {
            if (MapObjectLoader.DB == null)
            {
                Debug.LogError("[IllegalHologramBillboard] MapObjectLoader.DB가 null임");
                return;
            }

            var row = MapObjectLoader.DB.Get(mapId, objectId);
            if (row == null)
            {
                Debug.LogWarning($"[IllegalHologramBillboard] CSV 데이터 없음 | {mapId}:{objectId}");
                return;
            }

            autoFitOnAwake = row.autoFit;
            triggerPadding = new Vector2(row.triggerPaddingX, row.triggerPaddingY);
            reducedOrthographicSize = row.reducedOrthographicSize;
            zoomLerpSpeed = row.zoomLerpSpeed;

            if (!string.IsNullOrWhiteSpace(row.sortingLayerName) && row.sortingLayerName != "None")
                sortingLayerName = row.sortingLayerName;

            sortingOrder = row.sortingOrder;

            if (debugLog)
            {
                Debug.Log(
                    $"[Map1][IllegalHologramBillboard] CSV 적용 | " +
                    $"autoFit={autoFitOnAwake}, triggerPadding={triggerPadding}, " +
                    $"reducedOrthographicSize={reducedOrthographicSize}, zoomLerpSpeed={zoomLerpSpeed}, " +
                    $"sortingLayerName={sortingLayerName}, sortingOrder={sortingOrder}"
                );
            }
        }

        /// <summary>
        /// Sprite 크기에 맞춰 BoxCollider2D를 Trigger 영역으로 설정.
        ///
        /// [다른 시스템과의 연결]
        /// - Player가 이 영역에 들어오면 OnTriggerEnter2D 호출
        /// - 그 결과 playerInside가 true가 되고, Update에서 카메라 줌 연출 시작
        /// </summary>
        public void FitTriggerToSprite()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (boxCol == null) boxCol = GetComponent<BoxCollider2D>();

            if (sr.sprite == null)
            {
                Debug.LogWarning($"[Map1][IllegalHologramBillboard] Sprite가 없습니다: {gameObject.name}");
                return;
            }

            Vector2 size = (Vector2)sr.sprite.bounds.size + triggerPadding;
            Vector2 offset = (Vector2)sr.sprite.bounds.center;

            boxCol.size = size;
            boxCol.offset = offset;
            boxCol.isTrigger = true;

            if (debugLog)
            {
                Debug.Log($"[Map1][IllegalHologramBillboard] Trigger Fit 완료 | name={gameObject.name} | size={size} | offset={offset}");
            }
        }

        /// <summary>
        /// SpriteRenderer의 sorting layer / order를 적용.
        /// 광고판 이미지를 플레이어보다 아래로 깔리게 만들 때 사용.
        /// </summary>
        public void ApplyRenderOrder()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();

            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrder;

            if (debugLog)
            {
                Debug.Log($"[Map1][IllegalHologramBillboard] Render Order 적용 | layer={sortingLayerName}, order={sortingOrder}");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Player만 반응
            if (!other.CompareTag(playerTag)) return;

            playerInside = true;

            if (debugLog)
                Debug.Log($"[Map1][IllegalHologramBillboard] Player Enter | {gameObject.name}");
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag(playerTag)) return;

            playerInside = false;

            if (debugLog)
                Debug.Log($"[Map1][IllegalHologramBillboard] Player Exit | {gameObject.name}");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 에디터 프리뷰용
            sr = GetComponent<SpriteRenderer>();
            boxCol = GetComponent<BoxCollider2D>();

            if (boxCol != null)
                boxCol.isTrigger = true;

            if (!Application.isPlaying)
            {
                if (autoFitOnAwake && sr != null && boxCol != null && sr.sprite != null)
                    FitTriggerToSprite();

                if (sr != null)
                    ApplyRenderOrder();
            }
        }
#endif
    }
}