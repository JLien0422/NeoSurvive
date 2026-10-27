using System;
using NeoSurvive.Buff;
using NeoSurvive.Weapon;
using UnityEngine;

/// <summary>실제 CC 만료 시점에만 전이합니다. 사망, 강제 삭제, 갱신은 만료 전이로 취급하지 않습니다.</summary>
public static class RunTraitControl
{
    public static void Apply(GameObject target, IBuff buff, WeaponBase weapon, bool transferred = false)
    {
        var enemy = target != null ? RunTraitTarget.Find(target) : null;
        if (enemy == null || enemy.IsDead || buff == null) return;
        var handler = enemy.GetComponent<BuffHandler>() ?? enemy.gameObject.AddComponent<BuffHandler>();
        // 같은 무기의 같은 CC는 시간 갱신. 다중 Collider로 중복 만료를 만들지 않습니다.
        uint id = unchecked((uint)((weapon != null ? weapon.GetInstanceID() : 0) * 397 ^ buff.Name.GetHashCode() ^ (transferred ? 0x5350 : 0)));
        var callback = !transferred && !RunTraitDamageContext.Secondary
            ? Expiry(enemy, weapon, buff.Duration, (next, time) => Apply(next.gameObject, Copy(buff, time), weapon, true)) : null;
        handler.AddBuffWithId(id, buff, onExpired: callback);
    }
    private static IBuff Copy(IBuff buff, float time)
    {
        if (buff is RootDebuff) return new RootDebuff(time);
        if (buff is StunDebuff) return new StunDebuff(time);
        if (buff is FrenzyDebuff) return new FrenzyDebuff(time);
        return null;
    }
    public static Action Expiry(Character enemy, WeaponBase weapon, float duration, Action<Character, float> transfer)
    {
        var effect = RunTraitSpecialRuntime.Effect(weapon, RunTraitSpecialKind.VirusSpread);
        var manager = RunTraitManager.Instance;
        if (effect == null || manager == null || enemy == null || RunTraitDamageContext.Secondary) return null;
        int version = manager.RunVersion;
        return () => {
            if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled || manager == null
                || manager != RunTraitManager.Instance || manager.RunVersion != version
                || RunTraitSpecialRuntime.Effect(weapon, RunTraitSpecialKind.VirusSpread) != effect) return;
            var next = RunTraitSpecialRuntime.Nearest(enemy.transform.position, effect.radius, enemy);
            if (next == null) return;
            transfer(next, effect.Scale(duration));
            RunTraitSpecialRuntime.Line(enemy.transform.position, next.transform.position, Color.magenta);
        };
    }
}
