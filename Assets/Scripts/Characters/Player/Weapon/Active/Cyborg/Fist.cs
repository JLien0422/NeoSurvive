using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 사이보그 기본 주먹 공격 (근접)
    /// - Pistol.cs의 구조(타이머 -> Attack())를 최대한 유지
    /// - 투사체 대신 OverlapBox로 근접 타격 판정
    /// </summary>
    public class Fist : MonoBehaviour
    {
        [Header("근접 공격 설정")]
        public float damage = 8f;
        public float range = 1.6f;        // 타겟 탐색 범위
        public float fireRate = 0.6f;     // 공격 주기(초). 값이 작을수록 빠름

        [Header("히트박스 설정")]
        public Vector2 hitBoxSize = new Vector2(1.2f, 0.9f); // 주먹 판정 박스 크기
        public float hitBoxForwardOffset = 0.8f;             // 플레이어 앞쪽으로 판정 이동
        public LayerMask enemyLayer;                         // Enemy 레이어로 지정 추천

        private float fireTimer;

        private void Update()
        {
            fireTimer += Time.deltaTime;
            if (fireTimer >= fireRate)
            {
                Attack();
                fireTimer = 0f;
            }
        }

        private void Attack()
        {
            // 1) 가장 가까운 적 탐색
            GameObject target = FindClosestEnemy();

            // 2) 방향 결정 (타겟이 없으면 오른쪽)
            Vector3 dir = target != null
              ? (target.transform.position - transform.position).normalized
              : Vector3.right;

            // 3) dir 기준으로 "앞" 방향 오프셋 적용 (2D라서 x축 기준)
            float facing = dir.x >= 0 ? 1f : -1f;

            Vector2 center = (Vector2)transform.position + new Vector2(hitBoxForwardOffset * facing, 0f);

            // 4) 근접 판정(박스)
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, hitBoxSize, 0f, enemyLayer);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null) continue;

                // 5) 데미지 적용 (프로젝트의 데미지 처리 방식에 맞춰 1개만 남기면 됨)

                // (A) IDamageable 같은 인터페이스가 있다면 가장 깔끔
                IDamageable d = hits[i].GetComponent<IDamageable>();
                if (d != null)
                {
                    d.TakeDamage(damage);
                    continue;
                }

                // (B) Enemy/Character에 TakeDamage(float) 같은 메서드가 있다면 SendMessage로도 가능
                hits[i].SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            }
        }

        private GameObject FindClosestEnemy()
        {
            // Pistol.cs 로직 그대로: Tag "Enemy"를 찾아서 range 안에서 가장 가까운 것 반환
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            GameObject closest = null;
            float closestDistance = range > 0 ? range : 10f;

            foreach (GameObject enemy in enemies)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = enemy;
                }
            }
            return closest;
        }

#if UNITY_EDITOR
        // Scene에서 주먹 판정 범위 확인용
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            // (참고) 여기서는 항상 오른쪽 기준으로만 그려짐.
            // 실제 런타임에서는 facing에 따라 좌/우로 이동한 center를 사용함.
            Vector2 center = (Vector2)transform.position + new Vector2(hitBoxForwardOffset, 0f);
            Gizmos.DrawWireCube(center, hitBoxSize);
        }
#endif
    }
}
