using System.Collections.Generic;
using NeoSurvive.Weapon;
using UnityEngine;

/// <summary>원 증폭의 출처 기록과 캐스케이드 전용 증폭. 원 표식/필드와 피해를 중복 적용하지 않습니다.</summary>
public class RunTraitAmplificationReceiver : MonoBehaviour
{
    private struct Entry
    {
        public WeaponBase source;
        public float multiplier, expires;
        public bool transferred, hackerOnly;
    }
    private readonly Dictionary<int, Entry> entries = new Dictionary<int, Entry>();
    private readonly List<int> removedEntries = new List<int>();
    private RunTraitManager manager;
    private int version;
    private bool killed;

    public static bool ShouldReplaceField(Character enemy, float multiplier)
    {
        var receiver = enemy.GetComponent<RunTraitAmplificationReceiver>();
        if (receiver == null) return true;
        receiver.ValidateRun();
        foreach (var entry in receiver.entries.Values)
            if (!entry.transferred && !entry.hackerOnly && entry.expires > Time.time && entry.multiplier > multiplier)
                return false;
        return true;
    }

    public static void Record(Character enemy, WeaponBase source, float multiplier, float duration,
        bool hackerOnly = false, bool transferred = false)
    {
        if (enemy == null || enemy.IsDead || source == null || duration <= 0f) return;
        var receiver = enemy.GetComponent<RunTraitAmplificationReceiver>() ?? enemy.gameObject.AddComponent<RunTraitAmplificationReceiver>();
        receiver.ValidateRun();
        receiver.entries[source.GetInstanceID() * 2 + (transferred ? 1 : 0)] = new Entry {
            source = source, multiplier = multiplier, expires = Time.time + duration,
            transferred = transferred, hackerOnly = hackerOnly
        };
    }
    private void ValidateRun()
    {
        if (manager == RunTraitManager.Instance && manager != null && version == manager.RunVersion) return;
        entries.Clear(); killed = false;
        if (manager != null) manager.OnTraitsChanged -= OnTraitsChanged;
        manager = RunTraitManager.Instance;
        version = manager != null ? manager.RunVersion : 0;
        if (manager != null) manager.OnTraitsChanged += OnTraitsChanged;
    }
    private void OnTraitsChanged()
    {
        if (manager == null || version != manager.RunVersion)
        {
            entries.Clear(); killed = false;
            return;
        }
        removedEntries.Clear();
        foreach (var pair in entries)
            if (pair.Value.transferred && manager.GetSpecialEffect(pair.Value.source, RunTraitSpecialKind.Cascade) == null)
                removedEntries.Add(pair.Key);
        foreach (int key in removedEntries) entries.Remove(key);
    }
    public void RemoveOriginal(WeaponBase source)
    {
        if (source != null) entries.Remove(source.GetInstanceID() * 2);
    }
    public float TransferredMultiplier(WeaponBase incomingWeapon)
    {
        ValidateRun();
        float strongest = 1f;
        foreach (var entry in entries.Values)
            if (entry.transferred && entry.expires > Time.time
                && (!entry.hackerOnly || WeaponManager.IsMarkedDamageBonusSource(incomingWeapon)))
                strongest = Mathf.Max(strongest, entry.multiplier);
        return strongest;
    }
    public void OnKilled()
    {
        ValidateRun();
        if (killed) return;
        killed = true;
        Entry best = default;
        SpecialTraitEffect effect = null;
        foreach (var entry in entries.Values)
        {
            if (entry.transferred || entry.expires <= Time.time || entry.multiplier <= best.multiplier) continue;
            var candidate = RunTraitSpecialRuntime.Effect(entry.source, RunTraitSpecialKind.Cascade);
            if (candidate == null) continue;
            best = entry; effect = candidate;
        }
        if (effect == null) return;
        var target = RunTraitSpecialRuntime.Nearest(transform.position, effect.radius, RunTraitTarget.Find(this));
        if (target == null) return;
        Record(target, best.source, effect.ScaleAmplification(best.multiplier), effect.duration, best.hackerOnly, true);
        RunTraitSpecialRuntime.Line(transform.position, target.transform.position, Color.yellow);
    }
    private void OnDisable()
    {
        entries.Clear(); killed = false;
        if (manager != null) manager.OnTraitsChanged -= OnTraitsChanged;
        manager = null;
    }
}
