using System.Collections.Generic;
using NeoSurvive.Buff;
using UnityEngine;

/// <summary>같은 무기의 반복 적중은 갱신하고, 여러 무기의 감속은 가장 강한 것만 적용합니다.</summary>
public class RunTraitSlowReceiver : MonoBehaviour
{
    private struct Slow { public float multiplier; public float expires; public System.Action onExpired; }
    private readonly Dictionary<int, Slow> sources = new Dictionary<int, Slow>();
    private readonly List<int> expiredSources = new List<int>();
    private StatusFlags flags;
    private float appliedMultiplier = 1f;
    public bool IsActive => sources.Count > 0;

    public void Refresh(int source, float multiplier, float duration, System.Action onExpired = null)
    {
        sources[source] = new Slow { multiplier = Mathf.Clamp(multiplier, 0.1f, 1f), expires = Time.time + duration, onExpired = onExpired };
        Recalculate();
    }

    private void Update()
    {
        expiredSources.Clear();
        foreach (var pair in sources)
            if (pair.Value.expires <= Time.time) expiredSources.Add(pair.Key);
        foreach (int source in expiredSources)
        {
            var callback = sources[source].onExpired;
            sources.Remove(source);
            callback?.Invoke();
        }
        if (expiredSources.Count > 0) Recalculate();
    }

    private void Recalculate()
    {
        if (flags == null)
            flags = GetComponent<StatusFlags>() ?? gameObject.AddComponent<StatusFlags>();
        float strongest = 1f;
        foreach (Slow slow in sources.Values) strongest = Mathf.Min(strongest, slow.multiplier);
        flags.moveSpeedMul = flags.moveSpeedMul / appliedMultiplier * strongest;
        appliedMultiplier = strongest;
    }

    private void OnDisable()
    {
        sources.Clear();
        if (flags != null) flags.moveSpeedMul /= appliedMultiplier;
        appliedMultiplier = 1f;
    }
}
