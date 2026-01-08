using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 자동 전투 드론
    /// </summary>
    public class AIDrone : WeaponBase
    {
        public GameObject projectilePrefab;
        public float followDistance = 2f;
        public float followSpeed = 1f;

        public GameObject player;
        public Vector3 offset;

        private void Start()
        {
            offset = Random.insideUnitCircle.normalized * followDistance;
            
            player = GameObject.FindWithTag("Player");
        }

        protected override void Update()
        {
            base.Update();
            // 플레이어 주변 따라다니기
            if (player != null)
            {
                Vector3 targetPos = player.transform.position + offset;
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
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
            Debug.Log("드론 지원 사격!");
        }
    }
}
