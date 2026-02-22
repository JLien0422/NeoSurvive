using UnityEngine;

namespace NeoSurvive.Buff
{
    public class VulnerableDebuff : IBuff
    {
        public string Name => "Vulnerable";
        public float Duration { get; private set; }

        private readonly float multiplier;
        private float prevIncomingMul = 1f;
        private bool applied = false;

        public VulnerableDebuff(float multiplier, float duration)
        {
            this.multiplier = Mathf.Max(0f, multiplier);
            Duration = duration;
        }

        public void Apply(GameObject target)
        {
            var f = GetOrAddFlags(target);

            // 현재 값을 저장했다가 Remove에서 복구(단일 인스턴스 기준)
            prevIncomingMul = f.incomingDamageMul;
            f.incomingDamageMul = prevIncomingMul * multiplier;
            applied = true;
        }

        public void Tick(GameObject target, float deltaTime) { }

        public void Remove(GameObject target)
        {
            if (!applied) return;

            var f = GetOrAddFlags(target);
            f.incomingDamageMul = prevIncomingMul;
        }

        private StatusFlags GetOrAddFlags(GameObject target)
        {
            var f = target.GetComponent<StatusFlags>();
            if (f == null) f = target.AddComponent<StatusFlags>();
            return f;
        }
    }
}
