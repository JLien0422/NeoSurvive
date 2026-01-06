using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 직선형 광역 피해를 입히는 레이저 무기
    /// </summary>
    public class PlasmaPhoton : WeaponBase
    {
        public LineRenderer laserLine;
        public float laserDuration = 0.2f;

        public override void Attack()
        {
            if (laserLine == null)
            {
                laserLine = GetComponent<LineRenderer>();
                if (laserLine == null)
                {
                    laserLine = gameObject.AddComponent<LineRenderer>();
                    laserLine.startWidth = 0.1f;
                    laserLine.endWidth = 0.1f;
                    laserLine.material = new Material(Shader.Find("Sprites/Default"));
                    laserLine.startColor = Color.magenta;
                    laserLine.endColor = new Color(1, 0, 1, 0);
                }
            }

            GameObject target = FindClosestEnemy();
            Vector3 dir = target != null ? (target.transform.position - transform.position).normalized : transform.right;

            // 레이저 발사 시각화 및 판정
            StartCoroutine(FireLaser(dir));
        }

        private System.Collections.IEnumerator FireLaser(Vector3 dir)
        {
            if (laserLine != null)
            {
                laserLine.enabled = true;
                laserLine.SetPosition(0, transform.position);
                laserLine.SetPosition(1, transform.position + dir * range);
            }

            float elapsed = 0;
            float tickInterval = 1f / 6f;
            float lastTickTime = -tickInterval;

            while (elapsed < laserDuration)
            {
                if (elapsed >= lastTickTime + tickInterval)
                {
                    // 직선 범위 내 모든 적 타격
                    RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, dir, range);
                    foreach (var hit in hits)
                    {
                        if (hit.collider.CompareTag("Enemy"))
                        {
                            IDamageable d = hit.collider.GetComponent<IDamageable>();
                            d?.TakeDamage(damage * tickInterval);
                        }
                    }
                    lastTickTime = elapsed;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (laserLine != null) laserLine.enabled = false;
        }
    }
}
