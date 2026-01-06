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
        public float followSpeed = 5f;

        private Vector3 offset;

        private void Start()
        {
            offset = Random.insideUnitCircle.normalized * followDistance;
            
            // 드론 비주얼이 없을 경우를 대비해 간단한 스프라이트 추가
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = gameObject.AddComponent<SpriteRenderer>();
                // 기본 사각형 스프라이트 설정 (에디터에서 자동 설정 스크립트가 처리하겠지만 안전장치)
                sr.color = Color.green;
                sr.sortingOrder = 5;
            }
        }

        protected override void Update()
        {
            base.Update();
            // 플레이어 주변 따라다니기
            if (transform.parent != null)
            {
                Vector3 targetPos = transform.parent.position + offset;
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
