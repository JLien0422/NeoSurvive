using System.Collections.Generic;
using UnityEngine;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 디지털 실드 Lv5 전용
    /// - 넉백된 적에 붙어서 실제 이동 중 다른 적과 충돌했는지 검사
    /// - 충돌 시 주변 적에게 추가 피해를 주고 종료
    /// </summary>
    public class ShieldKnockbackCarrier : MonoBehaviour
    {
        private Enemy ownerEnemy;
        private Rigidbody2D rb;

        private float collisionDamage;
        private float collisionDetectRadius;
        private float collisionImpactRadius;
        private float duration;
        private string enemyTag;
        private bool debugLog;

        private float timer = 0f;
        private bool triggered = false;

        public void Initialize(
            Enemy ownerEnemy,
            float collisionDamage,
            float collisionDetectRadius,
            float collisionImpactRadius,
            float duration,
            string enemyTag,
            bool debugLog)
        {
            this.ownerEnemy = ownerEnemy;
            this.collisionDamage = collisionDamage;
            this.collisionDetectRadius = collisionDetectRadius;
            this.collisionImpactRadius = collisionImpactRadius;
            this.duration = duration;
            this.enemyTag = enemyTag;
            this.debugLog = debugLog;

            rb = GetComponent<Rigidbody2D>();
            timer = 0f;
            triggered = false;

            if (debugLog)
            {
                Debug.Log($"[ShieldCarrier] 부착 | owner={gameObject.name} | dmg={collisionDamage}");
            }
        }

        private void FixedUpdate()
        {
            if (triggered)
            {
                Destroy(this);
                return;
            }

            timer += Time.fixedDeltaTime;
            if (timer >= duration)
            {
                Destroy(this);
                return;
            }

            if (rb == null)
                rb = GetComponent<Rigidbody2D>();

            // 너무 느리면 충돌 상태 종료
            if (rb == null || rb.velocity.sqrMagnitude < 0.05f)
            {
                Destroy(this);
                return;
            }

            CheckCollisionWhileMoving();
        }

        private void CheckCollisionWhileMoving()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, collisionDetectRadius);

            foreach (var hit in hits)
            {
                if (hit == null) continue;

                Enemy other = hit.GetComponent<Enemy>();
                if (other == null)
                    other = hit.GetComponentInParent<Enemy>();

                if (other == null) continue;
                if (other == ownerEnemy) continue;
                if (!other.CompareTag(enemyTag)) continue;

                TriggerImpact(other.transform.position);
                return;
            }
        }

        private void TriggerImpact(Vector3 impactCenter)
        {
            triggered = true;

            HashSet<Enemy> impacted = new HashSet<Enemy>();
            Collider2D[] hits = Physics2D.OverlapCircleAll(impactCenter, collisionImpactRadius);

            foreach (var hit in hits)
            {
                if (hit == null) continue;

                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy == null)
                    enemy = hit.GetComponentInParent<Enemy>();

                if (enemy == null) continue;
                if (!enemy.CompareTag(enemyTag)) continue;
                if (enemy == ownerEnemy) continue;
                if (impacted.Contains(enemy)) continue;

                impacted.Add(enemy);
                enemy.TakeDamage(collisionDamage, null);

                if (debugLog)
                {
                    Debug.Log($"[ShieldCarrier] 충돌 피해 | target={enemy.name} | damage={collisionDamage}");
                }
            }

            if (debugLog)
            {
                Debug.Log($"[ShieldCarrier] 충돌 발생 | owner={gameObject.name} | impactCount={impacted.Count}");
            }

            Destroy(this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, collisionDetectRadius);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, collisionImpactRadius);
        }
#endif
    }
}