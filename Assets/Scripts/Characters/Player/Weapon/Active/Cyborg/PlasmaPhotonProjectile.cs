using UnityEngine;
using NeoSurvive.Buff;

namespace NeoSurvive.Weapon
{
    public class PlasmaPhotonProjectile : MonoBehaviour
    {
    private float TraitStat(float value, RunTraitStat stat) => RunTraitWeaponStats.Scale(sourceWeapon, value, stat);

        [Header("Movement")]
        public float speed = 14f;
        public float lifeTime = 3f;

        [Header("Damage")]
        public float baseDamage = 18f;

        [Header("On-Hit Debuffs")]
        public bool applySlow = true;
        [Range(0.05f, 1f)] public float slowMul = 0.7f;
        public float slowDuration = 2.5f;

        public bool applyStun = false;
        public float stunDuration = 1.0f;

        public bool applyRoot = false;
        public float rootDuration = 1.0f;

        public bool applyBlind = false;
        public float blindDuration = 2.0f;

        public bool applyVulnerable = false;
        public float vulnerableMul = 2f;
        public float vulnerableDuration = 2.0f;

        [Header("Hit Settings")]
        public LayerMask hitMask;
        public bool destroyOnHit = true;

        private Vector2 dir = Vector2.right;
        private float outgoingDamageMul = 1f; // 플레이어(또는 무기) 버프 반영용
        private WeaponBase sourceWeapon;

        public void Init(Vector2 direction, float outgoingMul)
        {
            dir = direction.normalized;
            outgoingDamageMul = Mathf.Max(0.01f, outgoingMul);
        }

        public void SetSourceWeapon(WeaponBase weapon)
        {
            sourceWeapon = weapon;
        }

        private void Start()
        {
            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            Vector2 before = transform.position;
            transform.position += (Vector3)(dir * TraitStat(speed, RunTraitStat.ProjectileSpeed) * Time.deltaTime);
            var body = GetComponent<Collider2D>();
            RunTraitProjectileErasure.SweepCollider(sourceWeapon, body, before);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (RunTraitProjectileErasure.TryErase(sourceWeapon, other)) return;
            // 🔒 Enemy만 타격 (Player, Weapon, 벽 전부 차단)
            if (!other.CompareTag("Enemy"))
                return;

            // 레이어 마스크 체크(설정했을 때만)
            if (hitMask.value != 0)
            {
                if (((1 << other.gameObject.layer) & hitMask.value) == 0) return;
            }

            // 자기 자신/투사체/트리거 등 예외는 필요하면 여기서 추가

            // 1) 데미지
            if (other.TryGetComponent<Character>(out var character))
            {
                float finalDamage = baseDamage * outgoingDamageMul;
                character.TakeDamage(finalDamage, sourceWeapon);
            }

            // 2) 디버프 적용(대상이 무엇이든 GameObject에 적용)
            GameObject target = other.gameObject;

            RunTraitStatus.ApplySlow(target, sourceWeapon, applySlow ? slowMul : 1f, applySlow ? slowDuration : 0f);
            if (applyStun)       BuffUtil.Apply(target, new StunDebuff(RunTraitWeaponStats.Scale(sourceWeapon, stunDuration, RunTraitStat.ControlDuration)));
            if (applyRoot)       BuffUtil.Apply(target, new RootDebuff(RunTraitWeaponStats.Scale(sourceWeapon, rootDuration, RunTraitStat.ControlDuration)));
            if (applyBlind)      BuffUtil.Apply(target, new BlindDebuff(RunTraitWeaponStats.Scale(sourceWeapon, blindDuration, RunTraitStat.ControlDuration)));
            if (applyVulnerable) BuffUtil.Apply(target, new VulnerableDebuff(vulnerableMul, vulnerableDuration));

            if (destroyOnHit) Destroy(gameObject);
        }
    }
}
