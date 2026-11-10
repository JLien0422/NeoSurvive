using System.Collections.Generic;

/// <summary>한 번 시작한 넉백 연쇄 전체가 방문 집합과 남은 전파 횟수를 공유합니다.</summary>
public sealed class RunTraitChainState
{
    private readonly HashSet<int> visited = new HashSet<int>();
    public int Remaining { get; private set; }
    public RunTraitChainState(int originId, int maxHops)
    {
        visited.Add(originId);
        Remaining = System.Math.Max(0, maxHops);
    }
    public bool TryVisit(int targetId)
    {
        if (Remaining <= 0 || !visited.Add(targetId)) return false;
        Remaining--;
        return true;
    }
}
