using UnityEngine;

namespace NeoSurvive.Map.Map1.MapObjects
{
    /// <summary>
    /// [Map1 고정 장애물]
    /// 맵1의 호버카 잔해 오브젝트.
    ///
    /// [역할]
    /// - 파괴되지 않는 장애물
    /// - Sprite 크기에 맞춰 BoxCollider2D를 자동 조정
    /// - 플레이어/적의 이동 경로를 막는 지형 역할
    ///
    /// [연결 구조]
    /// - SpriteRenderer: 현재 오브젝트의 이미지 크기 정보를 제공
    /// - BoxCollider2D: 실제 충돌 판정을 담당
    /// - 다른 무기/적/플레이어 시스템은 이 오브젝트를 "충돌체"로만 인식
    ///
    /// [팀 병합 시 의미]
    /// - 이 스크립트는 외부 시스템 의존성이 거의 없음
    /// - CSV, Loader, DB, Drop, UI와 연결되지 않음
    /// - 따라서 충돌 처리 계층에서 비교적 안전한 독립형 오브젝트 스크립트
    ///
    /// [주의]
    /// - Sprite pivot/bounds가 예상과 다르면 Collider 위치도 어긋날 수 있음
    /// - Sprite 교체 시 OnValidate로 에디터에서 Collider가 즉시 다시 맞춰짐
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class HoverCarWreck : MonoBehaviour
    {
        [Header("Auto Fit")]
        // Awake에서 자동으로 Collider를 Sprite 크기에 맞출지 여부
        [SerializeField] private bool autoFitOnAwake = true;

        [Header("Extra Padding (선택)")]
        // Sprite 크기보다 Collider를 조금 더 크게/작게 잡고 싶을 때 사용
        // ex) 차량 잔해가 더 넓게 막아야 한다면 +값
        [SerializeField] private Vector2 sizePadding = Vector2.zero;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        private void Awake()
        {
            // 씬 시작 시 Collider를 Sprite 기준으로 맞춘다.
            // 맵 오브젝트 프리팹이 여러 개 배치되어 있어도
            // Sprite만 정확하면 Collider를 일일이 수동 조절할 필요가 없다.
            if (autoFitOnAwake)
                FitColliderToSprite();
        }

        /// <summary>
        /// 핵심 기능:
        /// SpriteRenderer의 sprite.bounds를 기준으로 BoxCollider2D 크기/오프셋을 맞춘다.
        ///
        /// [작동 원리]
        /// - sprite.bounds.size   : 스프라이트의 로컬 크기
        /// - sprite.bounds.center : 스프라이트 중심 오프셋
        /// - 이를 BoxCollider2D.size / offset에 그대로 반영
        ///
        /// [연결 포인트]
        /// - 다른 시스템이 이 오브젝트와 충돌할 때 실제 충돌 범위가 여기서 결정됨
        /// - Player 이동 차단, Enemy 이동 차단, Projectile 충돌 등은
        ///   모두 Collider 설정 결과에 따라 달라진다.
        /// </summary>
        public void FitColliderToSprite()
        {
            var col = GetComponent<BoxCollider2D>();
            var sr = GetComponent<SpriteRenderer>();

            // Sprite가 없으면 기준 크기를 계산할 수 없으므로 경고 출력 후 중단
            if (sr.sprite == null)
            {
                Debug.LogWarning($"[HoverCarWreck] Sprite가 없습니다: {gameObject.name}");
                return;
            }

            // Sprite의 로컬 bounds 기준
            Vector2 size = sr.sprite.bounds.size;
            Vector2 offset = sr.sprite.bounds.center;

            // 필요시 Collider 여유값 추가
            size += sizePadding;

            // 실제 충돌 범위 적용
            col.size = size;
            col.offset = offset;

            // 이 오브젝트는 "막는 장애물"이므로 Trigger가 아니어야 함
            col.isTrigger = false;

            if (debugLog)
            {
                Debug.Log($"[HoverCarWreck] Collider Fit 완료 | size={size}, offset={offset}");
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 에디터에서 값이 바뀌거나 프리팹이 수정될 때도 즉시 Collider를 재적용
            // -> 디자이너/팀원이 프리팹 수정 시 결과를 바로 눈으로 확인 가능
            if (!Application.isPlaying)
            {
                FitColliderToSprite();
            }
        }
#endif
    }
}