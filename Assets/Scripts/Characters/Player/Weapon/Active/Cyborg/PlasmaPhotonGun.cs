using UnityEngine;
using NeoSurvive.Buff;

namespace NeoSurvive.Weapon
{
    public class PlasmaPhotonGun : MonoBehaviour
    {
        [Header("References")]
        public PlasmaPhotonProjectile projectilePrefab;
        public Transform firePoint;

        [Header("Fire")]
        public float fireInterval = 0.35f;
        public float range = 10f;

        private float timer;
        private StatusFlags ownerFlags;

        private void Awake()
        {
            ownerFlags = GetComponentInParent<StatusFlags>();
            if (ownerFlags == null) ownerFlags = GetComponent<StatusFlags>();
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < fireInterval) return;
            timer = 0f;

            Transform target = FindClosestEnemy();
            if (target == null) return;

            if (InGameSoundManager.Instance != null) InGameSoundManager.Instance.PlayPlasmaPhotonGunFire();

            Vector2 dir = (target.position - (firePoint != null ? firePoint.position : transform.position)).normalized;

            var spawnPos = firePoint != null ? firePoint.position : transform.position;
            var proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            // 플레이어(혹은 무기)가 가진 "가하는 데미지 배율" 반영
            float outMul = (ownerFlags != null) ? ownerFlags.outgoingDamageMul : 1f;
            proj.Init(dir, outMul);
            var src = GetComponentInParent<WeaponSource>();
            if (src != null) proj.SetSourceWeapon(src.weaponData);
        }

        private Transform FindClosestEnemy()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            Transform closest = null;
            float closestDist = float.MaxValue;

            Vector3 origin = firePoint != null ? firePoint.position : transform.position;

            foreach (var e in enemies)
            {
                if (e == null) continue;
                float d = Vector2.Distance(origin, e.transform.position);
                if (d < closestDist && d <= range)
                {
                    closestDist = d;
                    closest = e.transform;
                }
            }
            return closest;
        }
    }
}
