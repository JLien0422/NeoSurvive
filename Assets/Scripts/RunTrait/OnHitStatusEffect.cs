using UnityEngine;

[CreateAssetMenu(fileName = "OnHitStatusEffect", menuName = "NeoSurvive/Traits/Effects/On Hit Status")]
public class OnHitStatusEffect : RunTraitEffect
{
    public enum StatusKind { Slow, Knockback }
    [SerializeField] private StatusKind status = StatusKind.Slow;
    [SerializeField, Min(0f)] private float bonusPerStack = 0.1f;
    [SerializeField, Min(0.01f)] private float slowDuration = 1f;
    [SerializeField, Min(0f)] private float knockbackSpeed = 4f;
    [SerializeField, Min(0.01f)] private float knockbackDuration = 0.15f;

    public override float GetStatBonus(RunTraitStat stat, int stacks)
    {
        RunTraitStat channel = status == StatusKind.Slow
            ? RunTraitStat.SlowStrength : RunTraitStat.KnockbackDistance;
        return stat == channel ? Mathf.Max(0f, bonusPerStack) * Mathf.Max(0, stacks) : 0f;
    }

    public override void ConfigureHit(ref RunTraitHitSettings settings, int stacks)
    {
        if (stacks <= 0) return;
        if (status == StatusKind.Slow)
            settings.slowDuration = Mathf.Max(settings.slowDuration, slowDuration);
        else
        {
            settings.knockbackSpeed = Mathf.Max(settings.knockbackSpeed, knockbackSpeed);
            settings.knockbackDuration = Mathf.Max(settings.knockbackDuration, knockbackDuration);
        }
    }
}
