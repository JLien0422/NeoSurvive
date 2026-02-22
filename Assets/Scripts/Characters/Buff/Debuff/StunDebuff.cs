using UnityEngine;

namespace NeoSurvive.Buff
{
    public class StunDebuff : IBuff
    {
        public string Name => "Stun";
        public float Duration { get; private set; }

        public StunDebuff(float duration)
        {
            Duration = duration;
        }

        public void Apply(GameObject target)
        {
            var f = GetOrAddFlags(target);
            f.moveBlocked = true;
            f.attackBlocked = true;
        }

        public void Tick(GameObject target, float deltaTime) { }

        public void Remove(GameObject target)
        {
            var f = GetOrAddFlags(target);
            f.moveBlocked = false;
            f.attackBlocked = false;
        }

        private StatusFlags GetOrAddFlags(GameObject target)
        {
            var f = target.GetComponent<StatusFlags>();
            if (f == null) f = target.AddComponent<StatusFlags>();
            return f;
        }
    }
}
