using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Map.Map1.Gimmicks
{
    /// <summary>
    /// [Map1 Gimmick]
    /// 화물 낙하 실제 충돌/피해 판정 오브젝트
    ///
    /// [역할]
    /// - 생성 즉시 특정 범위(impactSize) 안의 대상을 검사
    /// - 플레이어에게는 playerDamage, 적에게는 enemyDamage를 적용
    /// - 짧은 시간 동안만 존재했다가 자동 삭제되는 1회성 판정 오브젝트
    ///
    /// [연결 구조]
    /// - CargoDropCrane가 impactPrefab으로 Instantiate 후 사용
    /// - CargoDropCrane.SpawnImpact() -> CargoContainerImpact.Initialize() 흐름으로 연결됨
    /// - 생성 후 Start()에서 DoImpact()가 즉시 호출되어 실제 피해가 들어감
    ///
    /// [다른 시스템과의 관계]
    /// - Player: 플레이어 판별 및 ApplyIncomingDamage 사용
    /// - Character: 최종 체력 감소를 실제로 처리
    /// - Enemy(root.CompareTag(enemyTag)): 적 판별용
    /// - hitMask: OverlapBoxAll로 어떤 레이어를 판정할지 결정
    ///
    /// [Git 병합 시 주의]
    /// - impactPrefab에 이 스크립트가 반드시 붙어 있어야 함
    /// - Player 구조가 Player + Character 조합이어야 플레이어 피해가 정상 동작
    /// - Enemy 구조에서 root 태그가 Enemy가 아니면 적 피해가 안 들어갈 수 있음
    /// - hitMask 설정이 바뀌면 맞아야 할 대상이 아예 감지되지 않을 수 있음
    ///
    /// [QA 체크 포인트]
    /// 1. 생성 직후 플레이어가 범위 안에 있으면 피해를 받는가
    /// 2. 적이 범위 안에 있으면 피해를 받는가
    /// 3. hitMask에 없는 레이어는 무시되는가
    /// 4. impactSize가 실제 낙하 범위와 일치하는가
    /// 5. destroyAfter 후 오브젝트가 사라지는가
    /// </summary>
    public class CargoContainerImpact : MonoBehaviour
    {
        [Header("Impact Area")]
        // 낙하 충돌 판정 범위
        [SerializeField] private Vector2 impactSize = new Vector2(2.5f, 2.5f);

        [Header("Damage")]
        // 플레이어에게 줄 피해량
        [SerializeField] private float playerDamage = 9999f;

        // 적에게 줄 피해량
        [SerializeField] private float enemyDamage = 9999f;

        [Header("Filter")]
        // 판정 대상으로 검사할 레이어 마스크
        [SerializeField] private LayerMask hitMask;

        // 적 판별용 태그
        [SerializeField] private string enemyTag = "Enemy";

        [Header("Lifetime")]
        // 판정 후 얼마 뒤 오브젝트를 제거할지
        [SerializeField] private float destroyAfter = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        /// <summary>
        /// CargoDropCrane가 impactPrefab 생성 직후 호출하는 초기화 함수
        ///
        /// [의미]
        /// - 크레인의 CSV/설정값을 실제 피해 판정 오브젝트에 런타임 전달
        /// - Start() 전에 호출되어야 DoImpact()가 원하는 값으로 실행됨
        /// </summary>
        public void Initialize(
            Vector2 impactSize,
            float playerDamage,
            float enemyDamage,
            float destroyAfter)
        {
            this.impactSize = impactSize;
            this.playerDamage = playerDamage;
            this.enemyDamage = enemyDamage;
            this.destroyAfter = destroyAfter;

            Debug.Log($"[DEBUG] Impact 초기화 | playerDamage={this.playerDamage}");
        }

        private void Start()
        {
            // 생성 즉시 1회 범위 판정 수행
            DoImpact();

            // 짧게 남았다가 삭제
            Destroy(gameObject, destroyAfter);
        }

        /// <summary>
        /// 실제 피해 판정 수행
        ///
        /// [작동 순서]
        /// 1. OverlapBoxAll로 impactSize 범위 안 Collider 수집
        /// 2. 먼저 Player 컴포넌트가 있는지 확인
        /// 3. Player면 ApplyIncomingDamage로 최종 피해 보정 후 Character.TakeDamage 호출
        /// 4. Player가 아니면 적(root + Enemy 태그)인지 판정
        /// 5. 적이면 Character.TakeDamage(enemyDamage) 호출
        /// </summary>
        private void DoImpact()
        {
            Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, impactSize, 0f, hitMask);

            foreach (var hit in hits)
            {
                if (hit == null) continue;

                // 1) 플레이어 판별
                Player player = hit.GetComponentInParent<Player>();
                if (player != null)
                {
                    Character playerCharacter = player.GetComponent<Character>();
                    if (playerCharacter != null)
                    {
                        // 플레이어의 피해 경감/증폭 배율을 반영한 최종 피해 계산
                        float finalDamage = player.ApplyIncomingDamage(playerDamage);
                        playerCharacter.TakeDamage(finalDamage);

                        if (debugLog)
                            Debug.Log($"[CargoContainerImpact] Player Hit | damage={finalDamage}");
                    }

                    // 플레이어 처리 후 다음 대상으로
                    continue;
                }

                // 2) 적 판별
                Transform root = hit.transform.root;
                if (root != null && root.CompareTag(enemyTag))
                {
                    Character enemyCharacter = root.GetComponent<Character>();
                    if (enemyCharacter != null)
                    {
                        enemyCharacter.TakeDamage(enemyDamage);

                        if (debugLog)
                            Debug.Log($"[CargoContainerImpact] Enemy Hit | name={root.name} damage={enemyDamage}");
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 에디터에서 낙하 판정 범위를 눈으로 확인하기 위한 Gizmo
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, impactSize);
        }
#endif
    }
}