using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 주변 적들에게 주기적으로 전기 충격을 가하는 무기 (테슬라 코일)
    /// </summary>
    public class TeslaCoil : WeaponBase
    {
        public float shockRadius = 3f;
        public GameObject shockEffectPrefab;

        public override void Attack()
        {
            // 주변 모든 적 찾기
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, shockRadius);
            
            foreach (var hit in hitEnemies)
            {
                if (hit.CompareTag("Enemy"))
                {
                    IDamageable damageable = hit.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(damage);
                        // 이펙트 생성 로직 (생략 가능)
                    }
                }
            }
            Debug.Log("테슬라 코일 방전!");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, shockRadius);
        }
    }
}
