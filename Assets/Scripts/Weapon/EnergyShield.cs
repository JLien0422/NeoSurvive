using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 플레이어 주변을 돌며 방어하고 반격하는 에너지 방패
    /// </summary>
    public class EnergyShield : WeaponBase
    {
        public GameObject shieldVisual;
        public float orbitSpeed = 100f;
        public float orbitRadius = 1.5f;

        private void Start()
        {
            if (shieldVisual != null)
            {
                shieldVisual = Instantiate(shieldVisual, transform);
                shieldVisual.transform.localPosition = new Vector3(orbitRadius, 0, 0);
            }
            else
            {
                // 비주얼이 없으면 간단한 원형 라인 생성
                GameObject circle = new GameObject("ShieldCircle");
                circle.transform.SetParent(transform);
                circle.transform.localPosition = Vector3.zero;
                LineRenderer lr = circle.AddComponent<LineRenderer>();
                lr.startWidth = 0.05f;
                lr.endWidth = 0.05f;
                lr.positionCount = 51;
                lr.useWorldSpace = false;
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.startColor = new Color(0, 1, 1, 0.3f);
                lr.endColor = new Color(0, 1, 1, 0.3f);
                for (int i = 0; i <= 50; i++)
                {
                    float angle = i * Mathf.PI * 2f / 50f;
                    lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * orbitRadius, Mathf.Sin(angle) * orbitRadius, 0));
                }
            }
        }

        protected override void Update()
        {
            base.Update();
            if (shieldVisual != null)
            {
                shieldVisual.transform.RotateAround(transform.position, Vector3.forward, orbitSpeed * Time.deltaTime);
            }

            // 장판 데미지 처리 (초당 6회)
            tickTimer += Time.deltaTime;
            float tickInterval = 1f / 6f;
            if (tickTimer >= tickInterval)
            {
                ApplyAreaDamage();
                tickTimer = 0f;
            }
        }

        private void ApplyAreaDamage()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, orbitRadius + 0.5f);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    IDamageable d = hit.GetComponent<IDamageable>();
                    // 틱당 데미지 계산 (초당 데미지 기준일 경우 damage * tickInterval)
                    d?.TakeDamage(damage * (1f / 6f));
                }
            }
        }

        public override void Attack()
        {
            // 방패는 지속형이므로 Attack 시 시각적 피드백만 제공
            VisualEffectHelper.CreateCircleEffect(transform.position, orbitRadius, new Color(0, 1, 1, 0.5f), 0.2f);
        }

        // 방패 비주얼에 붙어있을 스크립트에서 호출하거나 여기서 직접 처리
        public void OnShieldHit(IDamageable target)
        {
            target.TakeDamage(damage);
            Debug.Log("방패 반격!");
        }
    }
}
