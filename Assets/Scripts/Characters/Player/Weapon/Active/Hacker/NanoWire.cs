using UnityEngine;
using System.Collections;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 10번 무기 소환물: 나노 전선 트랩
    /// 설치 후 1초 뒤 활성화되어 닿는 적에게 데미지를 줌.
    /// </summary>
    public class NanoWire : MonoBehaviour
    {
        private float damage;
        private float duration;
        private float activationDelay = 1.0f; // 1초 뒤 활성화

        private bool isActivated = false;

        [SerializeField] private SpriteRenderer spriteRenderer;
        
        private float tickRate = 0.2f;
        private float tickTimer = 0f;

        // DPM 기록용 소스 무기
        private WeaponBase sourceWeapon;

        public void Initialize(float damage, float duration, WeaponBase weaponBase = null)
        {
            this.damage = damage;
            this.duration = duration;
            this.sourceWeapon = weaponBase;

            if (spriteRenderer == null) 
                spriteRenderer = GetComponent<SpriteRenderer>();
                
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 설치 중 (회색/반투명)

            // 전체 수명 시작
            StartCoroutine(LifeCycleRoutine());
        }

        private IEnumerator LifeCycleRoutine()
        {
            // 1. 설치 대기 (1초)
            yield return new WaitForSeconds(activationDelay);

            // 2. 활성화
            isActivated = true;
            if (spriteRenderer != null)
                spriteRenderer.color = Color.red; // 고열 레이저 (빨강)
            
            // 시각적 크기 증가? 전선이 쫙 펴지는 느낌?
            // transform.localScale *= 1.5f;

            // 3. 유지 시간 (duration)
            yield return new WaitForSeconds(duration);

            // 4. 소멸
            Destroy(gameObject);
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (!isActivated) return;

            // 틱 타이머 체크 (너무 빈번한 피격 방지)
            tickTimer += Time.deltaTime;
            if (tickTimer < tickRate) return;
            
            // 모든 적에게 데미지 (OnTriggerStay는 매 프레임 호출되므로 1번만 처리하고 타이머 리셋)
             // 여기서 문제: OnTriggerStay는 개별 객체마다 불리는 게 아니라, 충돌 중인 동안 계속 불림.
             // tickTimer를 공유하면 A적 때리고 B적 못 때릴 수 있음.
             // 하지만 간단 구현을 위해 그냥 공유하거나, 
             // 더 정확히는 적마다 무적시간을 둬야 하는데 Enemy쪽 로직이라 건들기 힘듦.
             // 여기서는 간단히: "활성화된 동안 닿으면 매 프레임 데미지를 주되, EnemyController 내부 무적시간이나 데미지 간격이 없다면 순삭될 수 있음."
             // 안전하게: tickTimer가 찼을 때 범위 내 적들을 OverlapCircle로 긁어서 때리는 방식이 나음.
        }
        
        private void Update()
        {
             if (!isActivated) return;
             
             tickTimer += Time.deltaTime;
             if(tickTimer >= tickRate)
             {
                 DealDamage();
                 tickTimer = 0f;
             }
        }
        
        private void DealDamage()
        {
            // 자신의 Collider 크기만큼? (Radius 0.5f 가정)
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.5f);
            foreach(var hit in hits)
            {
                if (hit.CompareTag("Enemy") && hit.TryGetComponent<Character>(out var character))
                    character.TakeDamage(damage, sourceWeapon);
            }
        }

        private void OnDrawGizmos()
        {
             if(isActivated) Gizmos.color = Color.red;
             else Gizmos.color = Color.gray;
             Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
