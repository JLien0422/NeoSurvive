using System.Collections.Generic;
using NeoSurvive.Buff;
using NeoSurvive.Core;
using NeoSurvive.Weapon;
using UnityEngine;

/// <summary>추가 공격은 저장한 위치/방향/판정을 사용합니다. 원 무기 Attack()이나 만렙 효과를 재호출하지 않습니다.</summary>
public sealed class RunTraitAttackSnapshot
{
    public enum Shape { Circle, Fan, Box, Beam }
    public Shape shape;
    public Vector3 origin, forward = Vector3.right;
    public float range, angle = 360f, width, damage, controlDuration, knockbackSpeed, knockbackDuration;
    public int maxTargets = int.MaxValue;
    public LayerMask mask;
    public WeaponBase weapon;
    public bool confusion, stun, applyPhysicalKnockback;
    public GameObject vfx;
    public float vfxOffset, vfxScale = 1f, vfxLifetime = 0.25f;

    public void Replay(float scale)
    {
        using (new RunTraitDamageContext.Scope(secondary: true))
        {
            SpawnVisual();
            if (shape == Shape.Beam)
            {
                var beamHits = width <= 0f ? Physics2D.RaycastAll(origin, forward, range)
                    : Physics2D.BoxCastAll(origin, new Vector2(0.001f, width),
                        Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg, forward, range);
                System.Array.Sort(beamHits, (a, b) => a.distance.CompareTo(b.distance));
                var ids = new HashSet<int>();
                int hitCount = 0;
                foreach (var hit in beamHits)
                    if (Hit(hit.collider, scale, ids) && ++hitCount >= maxTargets) break;
                RunTraitSpecialRuntime.Line(origin, origin + forward * range, new Color(0f, 1f, 1f, 0.5f));
                return;
            }
            var hits = shape == Shape.Box
                ? Physics2D.OverlapBoxAll(origin + forward * (range * 0.5f), new Vector2(range, width),
                    Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg)
                : Physics2D.OverlapCircleAll(origin, range);
            System.Array.Sort(hits, (a, b) => ((Vector2)a.transform.position - (Vector2)origin).sqrMagnitude
                .CompareTo(((Vector2)b.transform.position - (Vector2)origin).sqrMagnitude));
            var processed = new HashSet<int>();
            int count = 0;
            foreach (var col in hits)
            {
                if (shape == Shape.Fan && Vector3.Angle(forward, col.transform.position - origin) > angle * 0.5f) continue;
                if (Hit(col, scale, processed) && ++count >= maxTargets) break;
            }
        }
    }

    private bool Hit(Collider2D col, float scale, HashSet<int> processed)
    {
        if (col == null) return false;
        var enemy = RunTraitTarget.Find(col);
        if (enemy != null)
        {
            if (enemy.IsDead || (mask.value != 0 && (mask.value & (1 << col.gameObject.layer)) == 0)
                || !processed.Add(enemy.GetInstanceID())) return false;
            enemy.TakeDamage(damage * scale, weapon);
            RunTraitStatus.ApplySlow(enemy.gameObject, weapon, durationScale: scale);
            if (confusion) RunTraitControl.Apply(enemy.gameObject, new FrenzyDebuff(controlDuration * scale), weapon, true);
            if (stun) RunTraitControl.Apply(enemy.gameObject, new StunDebuff(controlDuration * scale), weapon, true);
            if (knockbackSpeed > 0f && !enemy.IsDead)
            {
                var dir = ((Vector2)enemy.transform.position - (Vector2)origin).normalized;
                (enemy.GetComponent<KnockbackDebuff>() ?? enemy.gameObject.AddComponent<KnockbackDebuff>())
                    .ApplyKnockback(dir * knockbackSpeed * scale, knockbackDuration);
            }
            if (applyPhysicalKnockback) RunTraitStatus.ApplyKnockback(enemy.gameObject, weapon, origin);
            return true;
        }
        var target = col.GetComponentInParent<IDamageable>();
        var component = target as Component;
        if (component == null || !processed.Add(component.GetInstanceID())) return false;
        target.TakeDamage(damage * scale);
        return true;
    }
    private void SpawnVisual()
    {
        if (vfx == null)
        {
            if (shape == Shape.Circle)
                RunTraitSpecialRuntime.CircleVisual(origin, range, new Color(0f, 1f, 1f, 0.45f), vfxLifetime);
            return;
        }
        var obj = Object.Instantiate(vfx, origin + forward * vfxOffset,
            Quaternion.Euler(0f, 0f, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg));
        obj.transform.localScale = Vector3.one * vfxScale;
        foreach (var renderer in obj.GetComponentsInChildren<SpriteRenderer>())
        {
            var color = renderer.color; color.a *= 0.45f; renderer.color = color;
        }
        Object.Destroy(obj, vfxLifetime);
    }
}
