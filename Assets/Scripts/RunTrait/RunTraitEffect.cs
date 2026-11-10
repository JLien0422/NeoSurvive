using UnityEngine;

/// <summary>
/// 공유되는 특성 효과 설정과 계산 규칙. 중첩·타이머 등 실행 상태는 저장하지 않습니다.
/// 새 효과는 이 타입을 상속해 필요한 계산 또는 실행 규칙을 추가합니다.
/// </summary>
public abstract class RunTraitEffect : ScriptableObject
{
    public virtual float GetDamageBonus(int stacks) => 0f;
    public virtual float GetStatBonus(RunTraitStat stat, int stacks) => 0f;
    public virtual void ConfigureHit(ref RunTraitHitSettings settings, int stacks) { }
}
