using NeoSurvive.Weapon;
using UnityEngine;

/// <summary>기본 수치를 덮어쓰지 않고 실행 시 현재 런의 보정값을 읽습니다.</summary>
public static class RunTraitWeaponStats
{
    public static float Bonus(WeaponBase weapon, RunTraitStat stat) =>
        RunTraitManager.Instance == null ? 0f : RunTraitManager.Instance.GetWeaponStatBonus(weapon, stat);

    public static float SlowMultiplier(float baseMovementMultiplier, float extraSlowPoints) =>
        Mathf.Clamp(baseMovementMultiplier - extraSlowPoints, 0.1f, 1f);

    public static float KnockbackVelocity(float baseSpeed, float distanceMultiplier, float durationMultiplier) =>
        baseSpeed * distanceMultiplier / Mathf.Max(0.01f, durationMultiplier);

    public static float Amplification(WeaponBase weapon, float baseMultiplier) =>
        Mathf.Max(0f, baseMultiplier + Bonus(weapon, RunTraitStat.AmplificationStrength));

    public static float Scale(WeaponBase weapon, float baseValue, RunTraitStat stat)
    {
        return RunTraitManager.Instance == null ? baseValue
            : baseValue * RunTraitManager.Instance.GetWeaponStatMultiplier(weapon, stat);
    }

    public static float Scale(Component owner, ref WeaponSource cachedSource,
        float baseValue, RunTraitStat stat)
    {
        if (RunTraitManager.Instance == null)
            return baseValue;
        if (cachedSource == null)
            cachedSource = owner.GetComponentInParent<WeaponSource>();
        return Scale(cachedSource != null ? cachedSource.weaponData : null, baseValue, stat);
    }
}
