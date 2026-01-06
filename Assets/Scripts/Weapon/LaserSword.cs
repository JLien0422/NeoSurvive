using UnityEngine;
using NeoSurvive.Core;
using NeoSurvive.Buff;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 플라즈마 에너지를 발산하는 근접 무기
    /// </summary>
    public class LaserSword : WeaponBase
    {
        public override void Attack()
        {
            VisualEffectHelper.CreateCircleEffect(transform.position, range, Color.red, 0.2f);

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    IDamageable d = hit.GetComponent<IDamageable>();
                    d?.TakeDamage(damage);

                    BuffHandler bh = hit.GetComponent<BuffHandler>();
                    bh?.AddBuff(new OverheatBuff(3f, 2f)); // 3초간 초당 2 데미지
                    Debug.Log("레이저 검 타격!");
                }
            }
        }
    }
}
