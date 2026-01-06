using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Buff
{
    /// <summary>
    /// 지속적인 데미지를 입히는 '과부하(Overheat)' 디버프
    /// </summary>
    public class OverheatBuff : IBuff
    {
        public string Name => "Overheat";
        public float Duration { get; private set; }
        private float damagePerSecond;
        private float timer;

        public OverheatBuff(float duration, float dps)
        {
            Duration = duration;
            damagePerSecond = dps;
        }

        public void Apply(GameObject target)
        {
            Debug.Log($"{target.name}에 과부하 발생!");
        }

        public void Tick(GameObject target, float deltaTime)
        {
            timer += deltaTime;
            float tickInterval = 1f / 6f; // 초당 6회 틱
            if (timer >= tickInterval)
            {
                IDamageable damageable = target.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    // 초당 데미지를 틱 횟수로 나누어 적용
                    damageable.TakeDamage(damagePerSecond * tickInterval);
                }
                timer = 0f;
            }
        }

        public void Remove(GameObject target)
        {
            Debug.Log($"{target.name}의 과부하 해제.");
        }
    }
}
