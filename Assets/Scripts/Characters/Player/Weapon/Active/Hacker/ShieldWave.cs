using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Core;
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
    private float TraitStat(float value, RunTraitStat stat) => RunTraitWeaponStats.Scale(sourceWeapon, value, stat);

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
        private WeaponBase sourceWeapon;

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
            bool debugLog,
            WeaponBase sourceWeapon)
        {
            this.owner = owner;
            this.damage = damage;
            float expansionMul = Player.Instance != null ? Player.Instance.GetCompileNodeExpansionMultiplier() : 1f;
            this.hitRadius = hitRadius * expansionMul;
            this.knockbackForce = knockbackForce;
            this.knockbackDuration = knockbackDuration;
            this.isMaster = isMaster;
            this.collisionDamage = collisionDamage;
            this.collisionDetectRadius = collisionDetectRadius * expansionMul;
            this.collisionImpactRadius = collisionImpactRadius * expansionMul;
            this.collisionCarrierDuration = collisionCarrierDuration;
            this.enemyTag = enemyTag;
            this.debugLog = debugLog;
            this.sourceWeapon = sourceWeapon;

            ProcessCircleHit();
            var replay = new RunTraitAttackSnapshot {
                shape = RunTraitAttackSnapshot.Shape.Circle, origin = transform.position,
                range = TraitStat(this.hitRadius, RunTraitStat.ShieldRadius), damage = damage, weapon = sourceWeapon,
                knockbackSpeed = RunTraitWeaponStats.KnockbackVelocity(knockbackForce,
                    TraitStat(1f, RunTraitStat.KnockbackDistance), TraitStat(1f, RunTraitStat.ControlDuration)),
                knockbackDuration = TraitStat(knockbackDuration, RunTraitStat.ControlDuration),
                vfxLifetime = visibleLifetime
            };
            RunTraitSpecialRuntime.Repeat(sourceWeapon, RunTraitSpecialKind.Echo, visibleLifetime, replay.Replay);
            Destroy(gameObject, visibleLifetime);
        }

        private void ProcessCircleHit()
        {
            HashSet<Character> processed = new HashSet<Character>();
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, TraitStat(hitRadius, RunTraitStat.ShieldRadius));
            bool playedSound = false;

            foreach (var hit in hits)
            {
                if (hit == null) continue;

                // =========================
                // 1. 기존 Enemy 처리
                // =========================
                Character enemy = RunTraitTarget.Find(hit);

                if (enemy != null && enemy.CompareTag(enemyTag))
                {
                    if (!playedSound && InGameSoundManager.Instance != null)
                    {
                        InGameSoundManager.Instance.PlayDigitalShieldAttack();
                        playedSound = true;
                    }

                    if (processed.Contains(enemy)) continue;

                    if (!playedSound && InGameSoundManager.Instance != null)
                    {
                        InGameSoundManager.Instance.PlayDigitalShieldAttack();
                        playedSound = true;
                    }

                    processed.Add(enemy);

                    Vector3 origin = (owner != null) ? owner.transform.position : transform.position;
                    Vector2 knockDir = (enemy.transform.position - origin).normalized;
                    if (knockDir.sqrMagnitude < 0.0001f)
                        knockDir = Vector2.right;

                    enemy.TakeDamage(damage, sourceWeapon);
                    RunTraitStatus.ApplySlow(enemy.gameObject, sourceWeapon);

                    KnockbackDebuff knockback = enemy.GetComponent<KnockbackDebuff>();

                    if (knockback == null)
                        knockback = enemy.gameObject.AddComponent<KnockbackDebuff>();

                    knockback.ApplyKnockback(
                        knockDir * RunTraitWeaponStats.KnockbackVelocity(knockbackForce,
                            TraitStat(1f, RunTraitStat.KnockbackDistance), TraitStat(1f, RunTraitStat.ControlDuration)),
                        TraitStat(knockbackDuration, RunTraitStat.ControlDuration),
                        RunTraitControl.Expiry(enemy, sourceWeapon, TraitStat(knockbackDuration, RunTraitStat.ControlDuration),
                            (next, time) => (next.GetComponent<KnockbackDebuff>() ?? next.gameObject.AddComponent<KnockbackDebuff>())
                                .ApplyKnockback(((Vector2)next.transform.position - (Vector2)enemy.transform.position).normalized
                                    * RunTraitWeaponStats.KnockbackVelocity(knockbackForce,
                                        TraitStat(1f, RunTraitStat.KnockbackDistance), TraitStat(1f, RunTraitStat.ControlDuration)), time)));

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
                            debugLog,
                            sourceWeapon
                        );
                    }

                    if (debugLog)
                    {
                        Debug.Log($"[ShieldWave] 원형 판정 타격 | target={enemy.name} | damage={damage}");
                    }

                    continue;
                }

                // =========================
                // 2. 추가: IDamageable 자판기 처리
                // =========================
                IDamageable damageable = hit.GetComponent<IDamageable>();

                if (damageable == null)
                    damageable = hit.GetComponentInParent<IDamageable>();

                if (damageable == null)
                    continue;

                damageable.TakeDamage(damage);

                if (debugLog)
                {
                    Debug.Log($"[ShieldWave] MapObject 타격 | target={hit.name} | damage={damage}");
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
