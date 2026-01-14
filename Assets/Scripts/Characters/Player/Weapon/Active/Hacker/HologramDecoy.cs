using UnityEngine;
using NeoSurvive.Characters; 
using System.Collections;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 9번 무기 소환물: 홀로그램 디코이
    /// 적을 유인하며, 파괴 시 데이터 감옥 발동
    /// </summary>
    public class HologramDecoy : Character
    {
        private bool spawnPrisonOnDeath;
        private float prisonDuration;

        public void Initialize(float hp, bool spawnPrison, float prisonDur)
        {
            // Character 클래스의 healthStat 초기화
            // (Character.cs가 protected healthStat을 가지고 있다고 가정)
            healthStat = new Stat(hp);
            currentHealth = hp;

            this.spawnPrisonOnDeath = spawnPrison;
            this.prisonDuration = prisonDur;

            // 태그 설정 (EnemyController가 인식하도록)
            gameObject.tag = "Decoy";
            
            // 레이어 설정 (Enemy와 충돌 가능해야 함) - 보통 "Player" 레이어 사용 권장
            // gameObject.layer = LayerMask.NameToLayer("Player");
        }

        protected override void Die()
        {
            if (spawnPrisonOnDeath)
            {
                SpawnDataPrison();
            }
            
            base.Die(); // base.Die() 호출 (Effect 재생 등 있을 수 있음)
            // base.Die()가 Destroy를 포함한다면 아래 코드는 불필요하지만, 
            // Player.cs를 보면 Destroy(gameObject)를 직접 함.
            // Character.cs의 Die는 보통 virtual이고 Destroy를 하나?
            // 안전하게 base.Die()만 호출하거나, base.Die 내용 모르면 직접 효과 처리.
            // 여기서는 직접 Destroy 함 (Character.cs 열람 불가 상태이므로 안전책)
            Destroy(gameObject);
        }

        private void SpawnDataPrison()
        {
            GameObject prison = new GameObject("DataPrison");
            prison.transform.position = transform.position;
            
            // 감옥 스크립트 추가
            var prisonScript = prison.AddComponent<DataPrisonEffect>();
            prisonScript.Initialize(3.0f, prisonDuration); 
        }
    }

    /// <summary>
    /// 데이터 감옥 효과: 범위 내 적 이동 불가 (감옥)
    /// </summary>
    public class DataPrisonEffect : MonoBehaviour
    {
        private float range;
        private float duration;

        public void Initialize(float range, float duration)
        {
            this.range = range;
            this.duration = duration;
            
            StartCoroutine(PrisonRoutine());
            
            // 시각 효과 (반투명 파란 구체)
            var sr = gameObject.AddComponent<SpriteRenderer>();
            // 스프라이트가 없으면 사각형이라도 나오게... (기본 UI sprite나 null이면 안보임)
            // 보통 프리팹을 쓰지만 스크립트로 생성 중이라.
            // 일단 디버그용 Gizmos로 확인 가능.
            
            Destroy(gameObject, duration);
        }

        private IEnumerator PrisonRoutine()
        {
             // 생성 즉시 범위 내 적 속박
             ApplySnare();
             
             // 지속 시간 동안 반복 체크? (들어오는 적도 속박?)
             // 기획: "주변 적을 3초간 가두는" -> 1회성일 수도 있고 장판일 수도 있음.
             // 장판으로 구현 (매 프레임 체크)
             float elapsed = 0f;
             while(elapsed < duration)
             {
                 ApplySnare();
                 yield return new WaitForSeconds(0.2f); // 0.2초마다 갱신
                 elapsed += 0.2f;
             }
        }

        private void ApplySnare()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    if (hit.TryGetComponent<EnemyController>(out var ai))
                    {
                        // 이동 속도를 0으로 (0.3초 지속 - 반복 갱신되므로 계속 멈춤)
                        ai.ApplySlow(0f, 0.3f); 
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
