using UnityEngine;

namespace NeoSurvive.Buff
{
    public class DamageBuff : IBuff
    {
        public string Name => "DamageUp";
        public float Duration { get; private set; }

        private readonly float damageMul;
        private float prevOutMul = 1f;
        private bool applied = false;

        /// <param name="damageMul">예: 1.25f (25% 증가)</param>
        public DamageBuff(float damageMul, float duration)
        {
            this.damageMul = Mathf.Max(1f, damageMul);
            Duration = duration;
        }

        public void Apply(GameObject target)
        {
            var f = GetOrAddFlags(target);
            prevOutMul = f.outgoingDamageMul;
            f.outgoingDamageMul = prevOutMul * damageMul;
            applied = true;
        }

        public void Tick(GameObject target, float deltaTime) { }

        public void Remove(GameObject target)
        {
            if (!applied) return;

            var f = GetOrAddFlags(target);
            f.outgoingDamageMul = prevOutMul;
        }

        private StatusFlags GetOrAddFlags(GameObject target)
        {
            var f = target.GetComponent<StatusFlags>();
            if (f == null) f = target.AddComponent<StatusFlags>();
            return f;
        }
    }
}
