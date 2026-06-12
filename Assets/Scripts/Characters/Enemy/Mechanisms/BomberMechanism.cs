using System.Collections;
using UnityEngine;

/// <summary>
/// 두 가지만 구분해서 보면 됩니다.
/// (A) 플레이어가 유도 반경 안으로 들어옴 → <see cref="Enemy.RequestDeathViaMechanism"/>로 Die() 플로우만 태움 → 폭발 시퀀스
/// (B) 그 전에 다른 이유로 HP가 0 → Die() → 동일 폭발 시퀀스
/// 실제 피해는 항상 <see cref="ApplyExplosionDamageAndDestroy"/> 한 곳입니다.
/// </summary>
public class BomberMechanism : EnemyMechanismBase
{
    private const int FuseOverlapCapacity = 32;
    private const int ExplosionOverlapCapacity = 48;

    [Header("유도 (추가 트리거)")]
    [SerializeField]
    [Tooltip("0 이하면 근접 유도 끔")]
    private float fuseTriggerRadius = 2.75f;

    [SerializeField]
    [Tooltip("근접 판단 주기 — 매 프레임이 아니라 간격 검사만")]
    private float fuseProximityCheckPeriod = 0.1f;

    [SerializeField]
    private LayerMask fuseOverlapMask = ~0;

    [Header("폭발 피해 (공통 로직)")]
    [SerializeField]
    private float explosionRadius = 3f;

    [SerializeField]
    private float explosionDamage = 30f;

    [SerializeField]
    private float explosionDelay = 1f;

    [SerializeField]
    private LayerMask explosionDamageMask = ~0;

    [SerializeField]
    private GameObject explosionEffectPrefab;

    [Header("위험 구역 표시 — 봄버 색이 아니라 폭발 반경 안에 깔림")]
    [SerializeField]
    private GameObject warningEffectPrefab;

    [SerializeField]
    [Tooltip("프리팹 scale (1,1,1)일 때 위험 원의 월드 지름. 0 이하면 스케일 보정 생략(프리팹 그대로)")]
    private float warningDangerZonePrefabWorldDiameter = 1f;

    private readonly Collider2D[] fuseScratch = new Collider2D[FuseOverlapCapacity];
    private readonly Collider2D[] explosionScratch = new Collider2D[ExplosionOverlapCapacity];

    private bool explosionStarted;
    private float fuseProximityTimer;

    private void Update()
    {
        if (fuseTriggerRadius <= 0f) return;
        if (explosionStarted || enemy == null || enemy.IsDead) return;

        fuseProximityTimer += Time.deltaTime;
        if (fuseProximityTimer < fuseProximityCheckPeriod) return;

        fuseProximityTimer = 0f;

        if (!IsTaggedDamageTargetOverlappingCircle(fuseTriggerRadius, fuseOverlapMask))
            return;

        enemy.RequestDeathViaMechanism();
    }

    public override void UpdateMovement()
    {
        // 폭발 카운트다운 중 EnemyController.FixedUpdate 가 계속 타므로 속도 재설정을 막고 정지 유지
        if (explosionStarted)
        {
            if (rb != null)
                rb.velocity = Vector2.zero;
            return;
        }

        if (target == null || rb == null) return;

        Vector2 direction = (target.position - transform.position).normalized;
        float moveSpeed = controller != null ? controller.GetMoveSpeed() : 3f;
        rb.velocity = direction * moveSpeed;
    }

    public override void UpdateAttack()
    {
    }

    public override void OnDeath()
    {
        BeginExplosionCountdownOrIgnore();
    }

    private void BeginExplosionCountdownOrIgnore()
    {
        if (explosionStarted) return;
        explosionStarted = true;
        StartCoroutine(ExplosionCountdownThenDetonate());
    }

    private IEnumerator ExplosionCountdownThenDetonate()
    {
        if (rb != null) rb.velocity = Vector2.zero;

        if (warningEffectPrefab != null)
        {
            SpawnExplosionDangerZoneWarning(transform.position);
        }

        yield return new WaitForSeconds(explosionDelay);

        ApplyExplosionDamageAndDestroy();
    }

    private void ApplyExplosionDamageAndDestroy()
    {
        Vector2 center = transform.position;

        if (explosionEffectPrefab != null)
            Instantiate(explosionEffectPrefab, center, Quaternion.identity);

        int count = Physics2D.OverlapCircleNonAlloc(center, explosionRadius, explosionScratch, explosionDamageMask);
        if (count >= ExplosionOverlapCapacity)
        {
            Debug.LogWarning(
                $"[BomberMechanism] 폭발 겹침 수가 버퍼({ExplosionOverlapCapacity})에 닿았습니다. explosionRadius를 줄이거나 버퍼를 늘리세요.");
        }

        for (int i = 0; i < count; i++)
        {
            Collider2D col = explosionScratch[i];
            if (col == null) continue;

            Character victim = col.GetComponent<Character>();
            if (victim == null || victim == enemy) continue;

            float distance = Vector2.Distance(center, col.transform.position);
            if (distance >= explosionRadius) continue;

            float falloff = 1f - distance / explosionRadius;
            victim.TakeDamage(explosionDamage * falloff);
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 폭발 직후와 동일한 중심·반경(explosionRadius)에 맞춰 바닥/월드 위험 구역만 깔림. 봄버 본체 머티리얼은 변경 안 함.
    /// </summary>
    private void SpawnExplosionDangerZoneWarning(Vector3 worldCenter)
    {
        GameObject zone = Instantiate(warningEffectPrefab, worldCenter, Quaternion.identity);
        Destroy(zone, explosionDelay);

        float targetDiameter = Mathf.Max(0.01f, explosionRadius * 2f);

        if (warningDangerZonePrefabWorldDiameter <= 0f) return;

        float uniform = targetDiameter / Mathf.Max(0.0001f, warningDangerZonePrefabWorldDiameter);
        zone.transform.localScale = new Vector3(uniform, uniform, 1f);
    }

    private bool IsTaggedDamageTargetOverlappingCircle(float radius, LayerMask mask)
    {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, radius, fuseScratch, mask);
        if (count >= FuseOverlapCapacity)
        {
            Debug.LogWarning(
                $"[BomberMechanism] 유도 반경 내 겹침이 버퍼({FuseOverlapCapacity}) 한도를 찼습니다. fuseTriggerRadius/마스크를 조정하세요.");
        }

        for (int i = 0; i < count; i++)
        {
            if (TransformHasDamageTargetTag(fuseScratch[i].transform))
                return true;
        }

        return false;
    }

    private static bool TransformHasDamageTargetTag(Transform t)
    {
        while (t != null)
        {
            if (t.CompareTag("Player") || t.CompareTag("Decoy")) return true;
            t = t.parent;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (fuseTriggerRadius > 0f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, fuseTriggerRadius);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
