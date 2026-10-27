using System;

/// <summary>동기 피해 호출 범위에만 적용되는 출처. 중첩 호출 후 이전 출처를 반드시 복원합니다.</summary>
public static class RunTraitDamageContext
{
    public static bool Secondary { get; private set; }
    public static bool DeferRicochet { get; private set; }
    public static bool ForceFieldContact { get; private set; }

    public readonly struct Scope : IDisposable
    {
        private readonly bool secondary, deferred, forceField;
        public Scope(bool secondary = false, bool deferRicochet = false, bool forceFieldContact = false)
        {
            this.secondary = Secondary;
            deferred = DeferRicochet;
            forceField = ForceFieldContact;
            // 추가 피해 안에서 기본 공격용 scope를 열어도 재발동 금지는 유지됩니다.
            Secondary |= secondary;
            DeferRicochet |= deferRicochet;
            ForceFieldContact = !Secondary && forceFieldContact;
        }
        public void Dispose()
        {
            Secondary = secondary;
            DeferRicochet = deferred;
            ForceFieldContact = forceField;
        }
    }
}
