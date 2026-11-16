using System;
using System.Collections;
using System.Collections.Generic;
using NeoSurvive.Weapon;
using UnityEngine;

/// <summary>26-10-21 Xabuna: 플래티넘의 지연 실행과 출처 제한. SO에는 실행 상태를 쓰지 않습니다.</summary>
public class RunTraitSpecialRuntime : MonoBehaviour
{
    private int runVersion;
    private RunTraitManager manager;

    public static SpecialTraitEffect Effect(WeaponBase weapon, RunTraitSpecialKind kind) =>
        RunTraitManager.Instance != null ? RunTraitManager.Instance.GetSpecialEffect(weapon, kind) : null;

    public static RunTraitSpecialRuntime Host
    {
        get
        {
            var manager = RunTraitManager.Instance;
            if (manager == null || !manager.IsInitialized) return null;
            var host = manager.GetComponent<RunTraitSpecialRuntime>() ?? manager.gameObject.AddComponent<RunTraitSpecialRuntime>();
            if (host.manager != manager || host.runVersion != manager.RunVersion)
            {
                host.StopAllCoroutines();
                host.manager = manager;
                host.runVersion = manager.RunVersion;
            }
            return host;
        }
    }

    public static void Later(float seconds, Action action)
    {
        var host = Host;
        if (host != null) host.StartCoroutine(host.Delayed(seconds, action, host.runVersion));
    }

    private IEnumerator Delayed(float seconds, Action action, int version)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, seconds));
        if (manager != null && manager.RunVersion == version && Player.Instance != null && !Player.Instance.IsDead)
            action?.Invoke();
    }

    public static Character Nearest(Vector2 center, float radius, Character excluded, HashSet<int> excludedIds = null)
    {
        Character nearest = null;
        float best = float.PositiveInfinity;
        foreach (var col in Physics2D.OverlapCircleAll(center, radius))
        {
            var enemy = RunTraitTarget.Find(col);
            if (enemy == null || enemy == excluded || enemy.IsDead || !enemy.isActiveAndEnabled
                || (excludedIds != null && excludedIds.Contains(enemy.GetInstanceID()))) continue;
            float distance = ((Vector2)enemy.transform.position - center).sqrMagnitude;
            if (distance < best) { best = distance; nearest = enemy; }
        }
        return nearest;
    }

    public static void OnDamage(Character enemy, WeaponBase weapon, float rawDamage, float actualDamage, bool lethal)
    {
        if (actualDamage <= 0f) return;
        if (RunTraitDamageContext.Secondary) return;
        if (!RunTraitDamageContext.DeferRicochet)
            Ricochet(enemy.transform.position, enemy, weapon, rawDamage);

        if (!lethal)
        {
            var stagger = Effect(weapon, RunTraitSpecialKind.Stagger);
            if (stagger != null)
                (enemy.GetComponent<RunTraitStaggerReceiver>() ?? enemy.gameObject.AddComponent<RunTraitStaggerReceiver>())
                    .TryStagger(stagger.duration, stagger.cooldown);
        }
        if (RunTraitDamageContext.ForceFieldContact && Player.Instance != null)
        {
            var absorption = Effect(weapon, RunTraitSpecialKind.ForceFieldAbsorption);
            if (absorption != null)
                (Player.Instance.GetComponent<RunTraitShield>() ?? Player.Instance.gameObject.AddComponent<RunTraitShield>())
                    .Charge(actualDamage, absorption);
        }
    }

    public static void Ricochet(Vector2 origin, Character original, WeaponBase weapon, float damage, HashSet<int> exclusions = null)
    {
        var effect = Effect(weapon, RunTraitSpecialKind.Ricochet);
        if (effect == null || RunTraitDamageContext.Secondary) return;
        var next = Nearest(origin, effect.radius, original, exclusions);
        if (next == null) return;
        using (new RunTraitDamageContext.Scope(secondary: true))
            next.TakeDamage(effect.Scale(damage), weapon);
        Line(origin, next.transform.position, Color.cyan);
    }

    public static void Line(Vector3 start, Vector3 end, Color color)
    {
        var go = new GameObject("RunTraitArc");
        var line = go.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, start); line.SetPosition(1, end);
        line.startWidth = line.endWidth = 0.06f;
        line.startColor = line.endColor = color;
        line.sortingOrder = 20;
        var shader = Shader.Find("Sprites/Default");
        if (shader != null) line.sharedMaterial = SharedLineMaterial(shader);
        Destroy(go, 0.15f);
    }
    private static Material lineMaterial;
    public static void CircleVisual(Vector3 origin, float radius, Color color, float lifetime)
    {
        var obj = new GameObject("RunTraitPulse");
        var line = obj.AddComponent<LineRenderer>();
        line.positionCount = 48; line.loop = true;
        line.startWidth = line.endWidth = 0.06f;
        line.startColor = line.endColor = color;
        line.sortingOrder = 20;
        var shader = Shader.Find("Sprites/Default");
        if (shader != null) line.sharedMaterial = SharedLineMaterial(shader);
        for (int i = 0; i < 48; i++)
        {
            float angle = i * Mathf.PI * 2f / 48;
            line.SetPosition(i, origin + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
        Destroy(obj, lifetime);
    }
    private static Material SharedLineMaterial(Shader shader)
    {
        if (lineMaterial == null) lineMaterial = new Material(shader);
        return lineMaterial;
    }

    // 판정 모양과 위치를 원 공격에서 캡처합니다. 추가 공격에는 마스터 부가 공격을 호출하지 않습니다.
    public static void Repeat(WeaponBase weapon, RunTraitSpecialKind kind, float attackLifetime,
        Action<float> replay)
    {
        var effect = Effect(weapon, kind);
        if (effect == null || RunTraitDamageContext.Secondary) return;
        float scale = effect.efficiency;
        Later(attackLifetime + effect.delay, () =>
        {
            if (Effect(weapon, kind) != effect) return;
            using (new RunTraitDamageContext.Scope(secondary: true)) replay(scale);
        });
    }
}
