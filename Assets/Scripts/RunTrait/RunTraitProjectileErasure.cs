using NeoSurvive.Weapon;
using UnityEngine;

public static class RunTraitProjectileErasure
{
    private static readonly System.Collections.Generic.List<RaycastHit2D> swept = new System.Collections.Generic.List<RaycastHit2D>();
    private static readonly System.Collections.Generic.List<Collider2D> overlapping = new System.Collections.Generic.List<Collider2D>();

    public static void SweepCollider(WeaponBase weapon, Collider2D body, Vector2 previous)
    {
        if (body == null || !body.enabled || RunTraitSpecialRuntime.Effect(weapon, RunTraitSpecialKind.ProjectileErasure) == null) return;
        // 이 프로젝트는 Auto Sync Transforms가 꺼져 있습니다. 포톤의 원형 판정은 Transform 기준으로 계산합니다.
        if (body is CircleCollider2D circle)
        {
            Vector2 offset = circle.transform.TransformVector(circle.offset);
            Vector3 scale = circle.transform.lossyScale;
            Sweep(weapon, previous + offset, (Vector2)circle.transform.position + offset,
                circle.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            return;
        }
        Physics2D.SyncTransforms();
        var filter = new ContactFilter2D().NoFilter();
        filter.useTriggers = true;
        Vector2 delta = previous - (Vector2)body.transform.position;
        body.Cast(delta.normalized, filter, swept, delta.magnitude);
        foreach (var hit in swept) TryErase(weapon, hit.collider);
        body.OverlapCollider(filter, overlapping);
        foreach (var col in overlapping) TryErase(weapon, col);
    }
    public static bool TryErase(WeaponBase weapon, Collider2D collider)
    {
        if (collider == null || RunTraitSpecialRuntime.Effect(weapon, RunTraitSpecialKind.ProjectileErasure) == null) return false;
        var bullet = collider.GetComponentInParent<EnemyProjectile>();
        if (bullet == null) return false;
        // Destroy 예약 이후 같은 프레임에 플레이어를 맞히지 않도록 즉시 비활성화합니다.
        bullet.gameObject.SetActive(false);
        Object.Destroy(bullet.gameObject);
        return true;
    }
    public static void Circle(WeaponBase weapon, Vector2 center, float radius)
    {
        if (RunTraitSpecialRuntime.Effect(weapon, RunTraitSpecialKind.ProjectileErasure) == null) return;
        foreach (var col in Physics2D.OverlapCircleAll(center, radius)) TryErase(weapon, col);
    }
    public static void Box(WeaponBase weapon, Vector2 center, Vector2 size, float angle)
    {
        if (RunTraitSpecialRuntime.Effect(weapon, RunTraitSpecialKind.ProjectileErasure) == null) return;
        foreach (var col in Physics2D.OverlapBoxAll(center, size, angle)) TryErase(weapon, col);
    }
    public static void Sweep(WeaponBase weapon, Vector2 start, Vector2 end, float radius)
    {
        if (RunTraitSpecialRuntime.Effect(weapon, RunTraitSpecialKind.ProjectileErasure) == null) return;
        Vector2 delta = end - start;
        foreach (var hit in Physics2D.CircleCastAll(start, radius, delta.normalized, delta.magnitude))
            TryErase(weapon, hit.collider);
        Circle(weapon, end, radius);
    }
}
