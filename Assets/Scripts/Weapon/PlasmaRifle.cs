using UnityEngine;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 사이버펑크 테마의 플라즈마 라이플
    /// </summary>
    public class PlasmaRifle : WeaponBase
    {
        public GameObject projectilePrefab;
        public Transform firePoint;
        public int pierceCount = 2;

        public override void Attack()
        {
            if (projectilePrefab == null) return;

            // 가장 가까운 적 찾기
            GameObject closestEnemy = FindClosestEnemy();

            Vector3 fireDirection = Vector3.right;
            if (closestEnemy != null)
            {
                fireDirection = (closestEnemy.transform.position - transform.position).normalized;
            }

            GameObject projectileObj = Instantiate(projectilePrefab, firePoint != null ? firePoint.position : transform.position, Quaternion.identity);
            projectileObj.SetActive(true); // 프리팹이 비활성화 상태일 수 있으므로

            // 관통 투사체 설정
            PiercingProjectile projectile = projectileObj.GetComponent<PiercingProjectile>();
            if (projectile == null) projectile = projectileObj.AddComponent<PiercingProjectile>();

            projectile.pierceCount = pierceCount;
            projectile.Initialize(fireDirection, damage);
            Debug.Log("플라즈마 라이플 관통 사격!");
        }
    }
}
