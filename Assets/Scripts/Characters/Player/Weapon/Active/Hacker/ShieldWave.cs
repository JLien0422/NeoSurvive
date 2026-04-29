using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Buff;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 디지털 실드 웨이브
    /// - 비주얼은 가로 직사각형
    /// - 실제 판정은 원형
    /// - 적에게 데미지 + 넉백
    /// - Lv5면 충돌 캐리어 부착
    /// </summary>
    public class ShieldWave : MonoBehaviour
    {
        [Header("Visual Lifetime")]
        [SerializeField] private float visibleLifetime = 0.18f;

        private Player owner;
        private float damage;
        private float hitRadius;
        private float knockbackForce;
        private float knockbackDuration;
        private bool isMaster;
        private float collisionDamage;
        private float collisionDetectRadius;
        private float collisionImpactRadius;
        private float collisionCarrierDuration;
        private string enemyTag;
        private bool debugLog;

        public void InitializeCircle(
            Player owner,
            float damage,
            float hitRadius,
            float knockbackForce,
            float knockbackDuration,
            bool isMaster,
            float collisionDamage,
            float collisionDetectRadius,
            float collisionImpactRadius,
            float collisionCarrierDuration,
            string enemyTag,
            bool debugLog)
        {
            this.owner = owner;
            this.damage = damage;
            this.hitRadius = hitRadius;
            this.knockbackForce = knockbackForce;
            this.knockbackDuration = knockbackDuration;
            this.isMaster = isMaster;
            this.collisionDamage = collisionDamage;
            this.collisionDetectRadius = collisionDetectRadius;
            this.collisionImpactRadius = collisionImpactRadius;
            this.collisionCarrierDuration = collisionCarrierDuration;
            this.enemyTag = enemyTag;
            this.debugLog = debugLog;

            ProcessCircleHit();
            Destroy(gameObject, visibleLifetime);
        }

        private void ProcessCircleHit()
        {
            HashSet<Enemy> processed = new HashSet<Enemy>();
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius);

            foreach (var hit in hits)
            {
                if (hit == null) continue;

                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy == null)
                    enemy = hit.GetComponentInParent<Enemy>();

                if (enemy == null) continue;
                if (!enemy.CompareTag(enemyTag)) continue;
                if (processed.Contains(enemy)) continue;

                processed.Add(enemy);

                Vector3 origin = (owner != null) ? owner.transform.position : transform.position;
                Vector2 knockDir = (enemy.transform.position - origin).normalized;
                if (knockDir.sqrMagnitude < 0.0001f)
                    knockDir = Vector2.right;

                enemy.TakeDamage(damage, null);

                KnockbackDebuff knockback = enemy.GetComponent<KnockbackDebuff>();

                if (knockback == null)
                    knockback = enemy.gameObject.AddComponent<KnockbackDebuff>();

                knockback.ApplyKnockback(knockDir * knockbackForce, knockbackDuration);

                if (isMaster)
                {
                    ShieldKnockbackCarrier carrier = enemy.GetComponent<ShieldKnockbackCarrier>();
                    if (carrier == null)
                        carrier = enemy.gameObject.AddComponent<ShieldKnockbackCarrier>();

                    carrier.Initialize(
                        enemy,
                        collisionDamage,
                        collisionDetectRadius,
                        collisionImpactRadius,
                        collisionCarrierDuration,
                        enemyTag,
                        debugLog
                    );
                }

                if (debugLog)
                {
                    Debug.Log($"[ShieldWave] 원형 판정 타격 | target={enemy.name} | damage={damage}");
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, hitRadius);
        }
#endif
    }
}