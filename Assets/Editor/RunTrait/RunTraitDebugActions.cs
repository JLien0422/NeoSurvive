using UnityEngine;

/// <summary>26-10-22 Xabuna: 디버그 선택도 자판기와 동일한 획득 경로와 중첩 제한을 사용합니다.</summary>
public static class RunTraitDebugActions
{
    public static bool Remove(RunTraitManager manager, RunTraitDefinition trait) =>
        manager != null && manager.TryRemove(trait);

    public static int Reset(RunTraitManager manager) => manager != null ? manager.ResetAcquiredTraits() : 0;

    public static int Acquire(RunTraitManager manager, RunTraitDefinition trait, bool fillToMax)
    {
        if (manager == null || !manager.IsInitialized || trait == null || string.IsNullOrEmpty(trait.traitId))
            return 0;

        int before = manager.GetTraitLevel(trait.traitId);
        int limit = trait.tier == RunTraitTier.Platinum ? 1 : Mathf.Max(1, trait.maxStacks);
        int count = fillToMax ? Mathf.Max(0, limit - before) : 1;
        for (int i = 0; i < count; i++)
            if (!manager.TryAcquire(trait)) break;

        return manager.GetTraitLevel(trait.traitId) - before;
    }
}
