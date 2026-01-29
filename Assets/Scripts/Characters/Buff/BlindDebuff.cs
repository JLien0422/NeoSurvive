using UnityEngine;

namespace NeoSurvive.Buff
{
    public class BlindDebuff : IBuff
    {
        public string Name => "Blind";
        public float Duration { get; private set; }

        public BlindDebuff(float duration)
        {
            Duration = duration;
        }

        public void Apply(GameObject target)
        {
            var f = GetOrAddFlags(target);
            f.blinded = true;
        }

        public void Tick(GameObject target, float deltaTime) { }

        public void Remove(GameObject target)
        {
            var f = GetOrAddFlags(target);
            f.blinded = false;
        }

        private StatusFlags GetOrAddFlags(GameObject target)
        {
            var f = target.GetComponent<StatusFlags>();
            if (f == null) f = target.AddComponent<StatusFlags>();
            return f;
        }
    }
}
