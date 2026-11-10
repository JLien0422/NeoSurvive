using UnityEngine;

public enum RunTraitSpecialKind
{
    Ricochet, Echo, VirusSpread, Cascade, DeploymentClone,
    Afterimage, ProjectileErasure, ChainKnockback, Stagger, ForceFieldAbsorption
}

/// <summary>공유 SO는 규칙과 수치만 보관합니다. 대상, 쿨타임, 보호막은 런타임 컴포넌트가 소유합니다.</summary>
[CreateAssetMenu(menuName = "NeoSurvive/Run Trait/Platinum Special Effect")]
public class SpecialTraitEffect : RunTraitEffect
{
    public RunTraitSpecialKind kind;
    [Min(0f)] public float radius;
    [Min(0f)] public float efficiency = 0.5f;
    [Min(0f)] public float delay = 0.2f;
    [Min(0f)] public float duration = 3f;
    [Min(0f)] public float cooldown = 0.5f;
    [Min(1)] public int maxHops = 3;
    [Range(0f, 1f)] public float shieldMaxHealthRatio = 0.2f;
    [Min(0f)] public float shieldDecayPerSecond = 5f;
    [Min(0f)] public float shieldDecayDelay = 3f;

    public float Scale(float value) => Mathf.Max(0f, value * efficiency);
    // 증폭 효율 50%: 1.2배 -> 1.1배. 기본 1배는 절반으로 줄이지 않습니다.
    public float ScaleAmplification(float multiplier) => 1f + Scale(Mathf.Max(0f, multiplier - 1f));
    public float ShieldGain(float actualDamage, float currentShield, float maxHealth) =>
        Mathf.Min(Scale(actualDamage), Mathf.Max(0f, maxHealth * shieldMaxHealthRatio - currentShield));
}
