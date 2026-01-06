using UnityEngine;
using NeoSurvive.Core;
using NeoSurvive.Buff;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 고전압 전기 에너지를 실은 근접 무기
    /// </summary>
    public class SurgeBlade : WeaponBase
    {
        public float attackArc = 90f;

        public override void Attack()
        {
            VisualEffectHelper.CreateSlashEffect(transform.position, transform.right, range, attackArc, Color.yellow, 0.2f);

            // 전방 부채꼴 범위 공격
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    Vector3 dirToEnemy = (hit.transform.position - transform.position).normalized;
                    // 임시로 플레이어가 바라보는 방향(오른쪽) 기준 90도 이내
                    if (Vector3.Angle(transform.right, dirToEnemy) < attackArc / 2f)
                    {
                        IDamageable d = hit.GetComponent<IDamageable>();
                        d?.TakeDamage(damage);

                        BuffHandler bh = hit.GetComponent<BuffHandler>();
                        bh?.AddBuff(new StunBuff(1f)); // 1초 마비
                        Debug.Log("서지 블레이드 타격!");
                    }
                }
            }
        }
    }
}
