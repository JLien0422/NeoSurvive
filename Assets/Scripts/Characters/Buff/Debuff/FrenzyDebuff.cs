using UnityEngine;

namespace NeoSurvive.Buff
{
    public class FrenzyDebuff : IBuff
    {
        public string Name => "Frenzy";
        public float Duration { get; private set; }

        public FrenzyDebuff(float duration)
        {
            Duration = duration;
        }

        public void Apply(GameObject target)
        {
            var f = GetOrAddFlags(target);
            f.frenzy = true;
        }

        public void Tick(GameObject target, float deltaTime) { }

        public void Remove(GameObject target)
        {
            var f = GetOrAddFlags(target);
            f.frenzy = false;
        }

        private StatusFlags GetOrAddFlags(GameObject target)
        {
            var f = target.GetComponent<StatusFlags>();
            if (f == null) f = target.AddComponent<StatusFlags>();
            return f;
        }
    }
}
