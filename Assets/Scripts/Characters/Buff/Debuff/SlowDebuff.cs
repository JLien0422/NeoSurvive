using UnityEngine;

namespace NeoSurvive.Buff
{
    public class SlowDebuff : IBuff
    {
        public string Name => "Slow";
        public float Duration { get; private set; }

        private readonly float slowMul;
        private float prevMoveMul = 1f;
        private bool applied = false;

        /// <param name="slowMul">예: 0.7f (30% 느려짐)</param>
        public SlowDebuff(float slowMul, float duration)
        {
            this.slowMul = Mathf.Clamp(slowMul, 0.05f, 1f);
            Duration = duration;
        }

        public void Apply(GameObject target)
        {
            var f = GetOrAddFlags(target);
            prevMoveMul = f.moveSpeedMul;
            f.moveSpeedMul = prevMoveMul * slowMul;
            applied = true;
        }

        public void Tick(GameObject target, float deltaTime) { }

        public void Remove(GameObject target)
        {
            if (!applied) return;

            var f = GetOrAddFlags(target);
            f.moveSpeedMul = prevMoveMul;
        }

        private StatusFlags GetOrAddFlags(GameObject target)
        {
            var f = target.GetComponent<StatusFlags>();
            if (f == null) f = target.AddComponent<StatusFlags>();
            return f;
        }
    }
}
