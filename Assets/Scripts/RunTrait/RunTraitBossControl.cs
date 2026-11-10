using NeoSurvive.Buff;
using UnityEngine;

/// <summary>보스의 추적과 패턴에서 같은 상태 규칙을 읽습니다.</summary>
public static class RunTraitBossControl
{
    public static bool CanMove(StatusFlags flags) => flags == null || (!flags.moveBlocked && !flags.IsRunTraitStaggered);
    public static bool CanAttack(StatusFlags flags) => flags == null || (!flags.attackBlocked && !flags.IsRunTraitStaggered);
    public static float MovementMultiplier(StatusFlags flags) => flags != null ? Mathf.Max(0f, flags.moveSpeedMul) : 1f;
}
