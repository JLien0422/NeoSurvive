using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 관통 기능이 있는 투사체
    /// </summary>
    public class PiercingProjectile : Projectile
    {
        public int pierceCount = 3;
        private int currentPierce = 0;

        protected override void OnTriggerEnter2D(Collider2D collision)
        {
            IDamageable damageable = collision.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                currentPierce++;
                if (currentPierce >= pierceCount)
                {
                    OnHit();
                }
            }
        }
    }
}
