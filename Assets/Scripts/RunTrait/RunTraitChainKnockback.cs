using NeoSurvive.Buff;
using NeoSurvive.Weapon;
using UnityEngine;

public class RunTraitChainKnockback : MonoBehaviour
{
    private RunTraitChainState chain;
    private Vector2 velocity;
    private float expires, pushDuration;
    private Character owner;
    private Collider2D body;
    private RunTraitManager manager;
    private int version;
    private SpecialTraitEffect effect;

    public static void Begin(Character enemy, WeaponBase weapon, Vector2 velocity, float duration)
    {
        var effect = RunTraitSpecialRuntime.Effect(weapon, RunTraitSpecialKind.ChainKnockback);
        if (enemy == null || enemy.IsDead || effect == null || RunTraitDamageContext.Secondary) return;
        var chain = new RunTraitChainState(enemy.GetInstanceID(), effect.maxHops);
        Attach(enemy, chain, velocity, duration, effect);
    }
    private static void Attach(Character enemy, RunTraitChainState chain, Vector2 velocity, float duration, SpecialTraitEffect effect)
    {
        var carrier = enemy.GetComponent<RunTraitChainKnockback>() ?? enemy.gameObject.AddComponent<RunTraitChainKnockback>();
        carrier.owner = enemy;
        carrier.body = enemy.GetComponent<Collider2D>() ?? enemy.GetComponentInChildren<Collider2D>();
        carrier.chain = chain;
        carrier.effect = effect;
        carrier.velocity = velocity;
        carrier.pushDuration = duration;
        carrier.expires = Time.time + duration;
        carrier.manager = RunTraitManager.Instance;
        carrier.version = carrier.manager != null ? carrier.manager.RunVersion : 0;
    }
    private void FixedUpdate()
    {
        if (chain == null || owner == null || owner.IsDead || body == null || Time.time >= expires
            || manager == null || manager != RunTraitManager.Instance || manager.RunVersion != version
            || !manager.HasEffect(effect)) return;
        // 동일 collider의 실제 접촉만 판정합니다. 벽과 넓은 탐색 반경은 전파 조건이 아닙니다.
        var bounds = body.bounds;
        foreach (var col in Physics2D.OverlapBoxAll(bounds.center, (Vector2)bounds.size + Vector2.one * 0.04f, 0f))
        {
            if (chain.Remaining <= 0) break;
            var next = RunTraitTarget.Find(col);
            if (next == null || next.IsDead || body.Distance(col).distance > 0.02f || !chain.TryVisit(next.GetInstanceID())) continue;
            var knockback = next.GetComponent<KnockbackDebuff>() ?? next.gameObject.AddComponent<KnockbackDebuff>();
            knockback.ApplyKnockback(velocity, pushDuration);
            Attach(next, chain, velocity, pushDuration, effect);
        }
    }
    private void OnDisable() => chain = null;
}
