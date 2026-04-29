using UnityEngine;

namespace NeoSurvive.Buff
{
    /// <summary>
    /// 데이터 최적화 전용 피해 증가 버프
    /// - 대상의 outgoingDamageMul을 증가시켜 가하는 피해를 강화
    /// - 주로 플레이어 투사체에 적용
    /// </summary>
    public class DataOptimizationDamageBuff : IBuff
    {
        public string Name => "DataOptimizationDamageBuff";
        public float Duration { get; private set; }

        private readonly float multiplier;
        private float prevOutgoingMul = 1f;
        private bool applied = false;

        public DataOptimizationDamageBuff(float multiplier, float duration)
        {
            this.multiplier = Mathf.Max(0f, multiplier);
            Duration = duration;
        }

        public void Apply(GameObject target)
        {
            var f = GetOrAddFlags(target);

            prevOutgoingMul = f.outgoingDamageMul;
            f.outgoingDamageMul = prevOutgoingMul * multiplier;
            applied = true;
        }

        public void Tick(GameObject target, float deltaTime)
        {
        }

        public void Remove(GameObject target)
        {
            if (!applied) return;

            var f = GetOrAddFlags(target);
            f.outgoingDamageMul = prevOutgoingMul;
        }

        private StatusFlags GetOrAddFlags(GameObject target)
        {
            var f = target.GetComponent<StatusFlags>();
            if (f == null) f = target.AddComponent<StatusFlags>();
            return f;
        }
    }
}