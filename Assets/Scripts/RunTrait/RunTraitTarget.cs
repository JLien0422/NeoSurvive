using UnityEngine;

/// <summary>26-10-22 Xabuna: 특성의 전투 대상. Enemy 태그를 공유하는 쓰레기·소환물은 포함하지 않습니다.</summary>
public static class RunTraitTarget
{
    public static bool IsCombatant(Character target) => target is Enemy || target is Boss;

    public static bool CanBossDamage(Character owner, Character target, bool confused) =>
        target != null && !target.IsDead && target != owner && (confused ? IsCombatant(target) : target is Player);

    public static Character Find(Component owner) => owner != null ? Filter(owner.GetComponentInParent<Character>()) : null;
    public static Character Find(GameObject owner) => owner != null ? Filter(owner.GetComponentInParent<Character>()) : null;

    private static Character Filter(Character target) => IsCombatant(target) ? target : null;
}
