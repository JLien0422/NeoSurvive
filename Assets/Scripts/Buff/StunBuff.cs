using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Buff
{
    /// <summary>
    /// 적을 일시적으로 멈추게 하는 스턴 버프
    /// </summary>
    public class StunBuff : IBuff
    {
        public string Name => "Stun";
        public float Duration { get; private set; }
        private float originalSpeed;

        public StunBuff(float duration)
        {
            Duration = duration;
        }

        public void Apply(GameObject target)
        {
            var enemy = target.GetComponent<NeoSurvive.Enemy.EnemyBase>();
            if (enemy != null)
            {
                originalSpeed = enemy.moveSpeed;
                enemy.moveSpeed = 0;
            }
            Debug.Log($"{target.name} 스턴 발생!");
        }

        public void Tick(GameObject target, float deltaTime) { }

        public void Remove(GameObject target)
        {
            var enemy = target.GetComponent<NeoSurvive.Enemy.EnemyBase>();
            if (enemy != null)
            {
                enemy.moveSpeed = originalSpeed;
            }
            Debug.Log($"{target.name} 스턴 해제.");
        }
    }
}
