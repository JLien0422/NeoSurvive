using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 설치형 자동 포탑
    /// </summary>
    public class AutoTurret : WeaponBase
    {
        public GameObject projectilePrefab;
        public float lifeTime = 10f;

        private void Start()
        {
            Destroy(gameObject, lifeTime);

            // 포탑 비주얼 추가
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = gameObject.AddComponent<SpriteRenderer>();
                sr.color = Color.gray;
                sr.sortingOrder = 4;
            }
        }

        public override void Attack()
        {
            if (projectilePrefab == null) return;

            GameObject target = FindClosestEnemy();
            if (target == null) return;

            Vector3 dir = (target.transform.position - transform.position).normalized;
            GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            obj.SetActive(true);
            Projectile p = obj.GetComponent<Projectile>();
            if (p != null) p.Initialize(dir, damage);
            Debug.Log("포탑 사격!");
        }
    }
}
