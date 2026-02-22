using UnityEngine;

namespace NeoSurvive.Buff
{
    public class RootDebuff : IBuff
    {
        public string Name => "Root";
        public float Duration { get; private set; }

        private int appliedCount = 0;

        public RootDebuff(float duration)
        {
            Duration = duration;
        }

        public void Apply(GameObject target)
        {
            var f = GetOrAddFlags(target);
            appliedCount++;
            f.moveBlocked = true;
        }

        public void Tick(GameObject target, float deltaTime) { }

        public void Remove(GameObject target)
        {
            var f = GetOrAddFlags(target);
            appliedCount = Mathf.Max(0, appliedCount - 1);

            // 단일 인스턴스 기준: 바로 해제
            // (중복/스택을 제대로 하고 싶으면 BuffHandler 쪽에서 정책을 추가하는 게 좋음)
            f.moveBlocked = false;
        }

        private StatusFlags GetOrAddFlags(GameObject target)
        {
            var f = target.GetComponent<StatusFlags>();
            if (f == null) f = target.AddComponent<StatusFlags>();
            return f;
        }
    }
}
