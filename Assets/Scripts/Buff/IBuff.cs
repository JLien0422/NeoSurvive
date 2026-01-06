using UnityEngine;

namespace NeoSurvive.Buff
{
    /// <summary>
    /// 버프 및 디버프를 위한 인터페이스
    /// </summary>
    public interface IBuff
    {
        string Name { get; }
        float Duration { get; }
        void Apply(GameObject target);
        void Tick(GameObject target, float deltaTime);
        void Remove(GameObject target);
    }
}
