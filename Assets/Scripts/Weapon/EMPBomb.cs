using UnityEngine;
using NeoSurvive.Core;
using NeoSurvive.Buff;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// EMP 폭탄 (투척 무기)
    /// </summary>
    public class EMPBomb : WeaponBase
    {
        public GameObject bombPrefab;

        public override void Attack()
        {
            if (bombPrefab == null) return;

            GameObject target = FindClosestEnemy();
            if (target == null) return;

            GameObject bomb = Instantiate(bombPrefab, transform.position, Quaternion.identity);
            bomb.SetActive(true);
            // 간단한 투척 로직 (목표 지점으로 이동 후 폭발)
            StartCoroutine(ThrowBomb(bomb, target.transform.position));
        }

        private System.Collections.IEnumerator ThrowBomb(GameObject bomb, Vector3 targetPos)
        {
            float elapsed = 0;
            Vector3 startPos = bomb.transform.position;
            while (elapsed < 0.5f)
            {
                if (bomb == null) yield break;
                bomb.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / 0.5f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 폭발 (0.5초 딜레이 후 펄스)
            Explode(targetPos);
            Destroy(bomb);
        }

        private void Explode(Vector3 pos)
        {
            VisualEffectHelper.CreateCircleEffect(pos, 3f, Color.cyan, 0.5f);

            Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 3f);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    IDamageable d = hit.GetComponent<IDamageable>();
                    d?.TakeDamage(damage);

                    BuffHandler bh = hit.GetComponent<BuffHandler>();
                    bh?.AddBuff(new StunBuff(2f)); // 2초 마비
                }
            }
            Debug.Log("EMP 폭발!");
        }
    }
}
