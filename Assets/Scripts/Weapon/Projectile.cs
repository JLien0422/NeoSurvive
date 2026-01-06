using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 투사체의 기본 클래스
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public float speed;
        public float damage;
        public float lifeTime = 5f;

        protected Vector3 direction;

        public virtual void Initialize(Vector3 dir, float dmg)
        {
            direction = dir.normalized;
            damage = dmg;
            Destroy(gameObject, lifeTime);
        }

        protected virtual void Update()
        {
            transform.position += direction * speed * Time.deltaTime;
        }

        protected virtual void OnTriggerEnter2D(Collider2D collision)
        {
            IDamageable damageable = collision.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                OnHit();
            }
        }

        protected virtual void OnHit()
        {
            // 명중 이펙트
            VisualEffectHelper.CreateCircleEffect(transform.position, 0.3f, Color.white, 0.1f);

            // 기본적으로 명중 시 소멸
            Destroy(gameObject);
        }
    }
}
