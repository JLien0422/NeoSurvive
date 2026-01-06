using UnityEngine;
using NeoSurvive.Core;
using NeoSurvive.Buff;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 기본 에너지 권총
    /// </summary>
    public class Pistol : WeaponBase
    {
        public GameObject projectilePrefab;

        public override void Attack()
        {
            if (projectilePrefab == null) return;

            GameObject target = FindClosestEnemy();
            Vector3 dir = target != null ? (target.transform.position - transform.position).normalized : Vector3.right;

            GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            obj.SetActive(true);
            Projectile p = obj.GetComponent<Projectile>();
            if (p != null) p.Initialize(dir, damage);
        }
    }
}
