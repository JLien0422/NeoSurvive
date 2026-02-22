using UnityEngine;

namespace NeoSurvive.Buff
{
    public class MoveSpeedBuff : IBuff
    {
        public string Name => "MoveSpeedUp";
        public float Duration { get; private set; }

        private readonly float speedMul;
        private float prevMoveMul = 1f;
        private bool applied = false;

        /// <param name="speedMul">예: 1.2f (20% 빠름)</param>
        public MoveSpeedBuff(float speedMul, float duration)
        {
            this.speedMul = Mathf.Max(1f, speedMul);
            Duration = duration;
        }

        public void Apply(GameObject target)
        {
            var f = GetOrAddFlags(target);
            prevMoveMul = f.moveSpeedMul;
            f.moveSpeedMul = prevMoveMul * speedMul;
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
