using NeoSurvive.Buff;
using NeoSurvive.Weapon;
using UnityEngine;

public static class RunTraitStatus
{
    public static void ApplySlow(GameObject target, WeaponBase weapon,
        float baseMultiplier = 1f, float baseDuration = 0f, float durationScale = 1f)
    {
        if (target == null) return;
        Character enemy = RunTraitTarget.Find(target);
        if (enemy == null || enemy.IsDead) return;
        RunTraitHitSettings settings = RunTraitManager.Instance != null
            ? RunTraitManager.Instance.GetHitSettings(weapon) : default;
        float duration = baseDuration > 0f ? baseDuration : settings.slowDuration;
        float extra = RunTraitWeaponStats.Bonus(weapon, RunTraitStat.SlowStrength);
        if (duration <= 0f || (baseMultiplier >= 1f && extra <= 0f)) return;
        float movementMultiplier = RunTraitWeaponStats.SlowMultiplier(baseMultiplier, extra);
        duration = RunTraitWeaponStats.Scale(weapon, duration, RunTraitStat.ControlDuration) * durationScale;
        target = enemy.gameObject;
        RunTraitSlowReceiver receiver = target.GetComponent<RunTraitSlowReceiver>();
        if (receiver == null) receiver = target.AddComponent<RunTraitSlowReceiver>();
        int sourceId = weapon != null ? weapon.GetInstanceID() : 0;
        receiver.Refresh(sourceId, movementMultiplier, duration,
            RunTraitDamageContext.Secondary ? null : RunTraitControl.Expiry(enemy, weapon, duration, (next, time) =>
                (next.GetComponent<RunTraitSlowReceiver>() ?? next.gameObject.AddComponent<RunTraitSlowReceiver>())
                    .Refresh(~sourceId, movementMultiplier, time)));
    }

    public static void ApplyKnockback(GameObject target, WeaponBase weapon, Vector2 origin)
    {
        if (target == null || RunTraitManager.Instance == null) return;
        Character enemy = RunTraitTarget.Find(target);
        if (enemy == null || enemy.IsDead) return;
        target = enemy.gameObject;
        var settings = RunTraitManager.Instance.GetHitSettings(weapon);
        if (settings.knockbackSpeed <= 0f || settings.knockbackDuration <= 0f) return;
        Vector2 direction = ((Vector2)target.transform.position - origin).normalized;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;
        KnockbackDebuff knockback = target.GetComponent<KnockbackDebuff>();
        if (knockback == null) knockback = target.AddComponent<KnockbackDebuff>();
        float speed = RunTraitWeaponStats.Scale(weapon, settings.knockbackSpeed, RunTraitStat.KnockbackDistance);
        knockback.ApplyKnockback(direction * speed, settings.knockbackDuration);
        RunTraitChainKnockback.Begin(enemy, weapon, direction * speed, settings.knockbackDuration);
    }
}
