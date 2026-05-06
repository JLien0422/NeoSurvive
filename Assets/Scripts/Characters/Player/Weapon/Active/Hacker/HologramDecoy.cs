using UnityEngine;
using NeoSurvive.Characters;
using System.Collections;
using NeoSurvive.Buff; // ✅ (추가) BuffUtil, RootDebuff

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 9번 무기 소환물: 홀로그램 디코이
    /// 적을 유인하며, 파괴 시 데이터 감옥 발동
    /// </summary>
    public class HologramDecoy : Character
    {
        private bool spawnPrisonOnDeath;
        private float prisonDuration;
        private float prisonRange;

        public void Initialize(float hp, bool spawnPrison, float prisonDur, float prisonRange)
        {
            currentHP = new Stat(hp);

            this.spawnPrisonOnDeath = spawnPrison;
            this.prisonDuration = prisonDur;
            this.prisonRange = prisonRange;

            gameObject.tag = "Decoy";
        }

        private void SpawnDataPrison()
        {
            GameObject prison = new GameObject("DataPrison");
            prison.transform.position = transform.position;

            var prisonScript = prison.AddComponent<DataPrisonEffect>();
            prisonScript.Initialize(prisonRange, prisonDuration);
        }
    }

    /// <summary>
    /// 데이터 감옥 효과: 범위 내 적 이동 불가 (감옥)
    /// </summary>
    public class DataPrisonEffect : MonoBehaviour
    {
        private float range;
        private float duration;

        public void Initialize(float range, float duration)
        {
            this.range = range;
            this.duration = duration;

            // ✅ 생성 즉시 1회만 적용 (원샷형)
            ApplySnareOnce();

            // 시각 효과 (반투명 파란 구체)
            var sr = gameObject.AddComponent<SpriteRenderer>();
            // 스프라이트가 없으면 사각형이라도 나오게... (기본 UI sprite나 null이면 안보임)
            // 보통 프리팹을 쓰지만 스크립트로 생성 중이라.
            // 일단 디버그용 Gizmos로 확인 가능.

            Destroy(gameObject, duration);
        }

        private void ApplySnareOnce()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);

            foreach (var hit in hits)
            {
                if (hit == null) continue;

                // ✅ Enemy는 자식 콜라이더일 수 있으니 부모까지 탐색
                Enemy enemy = hit.GetComponentInParent<Enemy>();
                if (enemy == null) continue;

                // ✅ 혹시 적이 아닌 오브젝트(다른 collider)까지 잡히면 태그로 한번 더 방어
                if (!enemy.CompareTag("Enemy")) continue;

                // ✅ BreathArea와 동일한 방식으로 Root(속박) 적용
                BuffUtil.Apply(enemy.gameObject, new RootDebuff(duration));
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, range);
        }
#endif
    }
}
