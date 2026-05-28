using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Buff;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
    public class PlasmaPhotonBeam : MonoBehaviour
    {
        [Header("Damage")]
        public float baseDamage = 18f;

        [Header("Hit")]
        public LayerMask hitMask;
        public bool destroyOnHit = false;
        public int maxPenetration = 2;

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

        private const string weaponId = "plasmaphotongun";
        private int currentLevel = 1;

        private readonly HashSet<int> hitIds = new HashSet<int>();

        public void ApplyStatsFromCSV(int level)
        {
            currentLevel = Mathf.Clamp(level, 1, 5);

            if (WeaponStatLoader.DB == null)
            {
                Debug.LogWarning("[PlasmaPhotonBeam] WeaponStatLoader.DB 없음");
                return;
            }

            if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var levelDict))
            {
                Debug.LogWarning($"[PlasmaPhotonBeam] weaponId 없음: {weaponId}");
                return;
            }

            if (!levelDict.TryGetValue(currentLevel, out var row))
            {
                Debug.LogWarning($"[PlasmaPhotonBeam] level 데이터 없음: {currentLevel}");
                return;
            }

            if (row.basedamage > 0f)
                baseDamage = row.basedamage;

            applySlow = row.applyslow >= 1f;
            if (row.slowmul > 0f) slowMul = row.slowmul;
            if (row.slowduration > 0f) slowDuration = row.slowduration;

            applyStun = row.applystun >= 1f;
            if (row.stunduration > 0f) stunDuration = row.stunduration;

            applyRoot = row.applyroot >= 1f;
            if (row.rootduration > 0f) rootDuration = row.rootduration;

            applyBlind = row.applyblind >= 1f;
            if (row.blindduration > 0f) blindDuration = row.blindduration;

            applyVulnerable = row.applyvulnerable >= 1f;
            if (row.vulnerablemul > 0f) vulnerableMul = row.vulnerablemul;
            if (row.vulnerableduration > 0f) vulnerableDuration = row.vulnerableduration;

            destroyOnHit = row.destroyonhit >= 1f;
        }

        public void Fire(Vector2 dir, float range, float outgoingMul, Player owner, WeaponBase sourceWeapon)
        {
            hitIds.Clear();

            Vector3 origin = transform.position;
            float finalDamage = baseDamage * Mathf.Max(0.01f, outgoingMul);

            RaycastHit2D[] hits = hitMask.value == 0
                ? Physics2D.RaycastAll(origin, dir, range)
                : Physics2D.RaycastAll(origin, dir, range, hitMask);

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            int hitCount = 0;

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null)
                    continue;

                if (TryHitTarget(hit.collider, finalDamage, owner, sourceWeapon))
                {
                    hitCount++;

                    if (destroyOnHit || hitCount > maxPenetration)
                        break;
                }
            }

            Destroy(gameObject);
        }

        private bool TryHitTarget(Collider2D other, float damage, Player owner, WeaponBase sourceWeapon)
        {
            Character character = other.GetComponent<Character>();
            if (character == null)
                character = other.GetComponentInParent<Character>();

            if (character != null)
            {
                int id = character.GetInstanceID();
                if (hitIds.Contains(id))
                    return false;

                character.TakeDamage(damage, sourceWeapon);
                ApplyDebuffs(character.gameObject);

                hitIds.Add(id);
                return true;
            }

            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null)
                damageable = other.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                MonoBehaviour mb = damageable as MonoBehaviour;
                if (mb == null)
                    return false;

                int id = mb.GetInstanceID();
                if (hitIds.Contains(id))
                    return false;

                damageable.TakeDamage(damage);

                hitIds.Add(id);
                return true;
            }

            return false;
        }

        private void ApplyDebuffs(GameObject target)
        {
            if (target == null) return;

            if (applySlow) BuffUtil.Apply(target, new SlowDebuff(slowMul, slowDuration));
            if (applyStun) BuffUtil.Apply(target, new StunDebuff(stunDuration));
            if (applyRoot) BuffUtil.Apply(target, new RootDebuff(rootDuration));
            if (applyBlind) BuffUtil.Apply(target, new BlindDebuff(blindDuration));
            if (applyVulnerable) BuffUtil.Apply(target, new VulnerableDebuff(vulnerableMul, vulnerableDuration));
        }
    }
}