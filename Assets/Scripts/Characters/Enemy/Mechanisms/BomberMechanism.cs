using System.Collections;
using UnityEngine;

/// <summary>
/// 폭발 사망형 메커니즘
/// 사망 시 1초 뒤 원형 범위 폭발을 일으킵니다.
/// </summary>
public class BomberMechanism : EnemyMechanismBase
{
    [Header("폭발 설정")]
    [SerializeField]
    [Tooltip("폭발 범위")]
    private float explosionRadius = 3f;

    [SerializeField]
    [Tooltip("폭발 대미지")]
    private float explosionDamage = 30f;

    [SerializeField]
    [Tooltip("폭발 딜레이 (사망 후 몇 초 뒤)")]
    private float explosionDelay = 1f;

    [SerializeField]
    [Tooltip("폭발 이펙트 프리팹 (선택사항)")]
    private GameObject explosionEffectPrefab;

    [SerializeField]
    [Tooltip("폭발 전 경고 효과 (선택사항)")]
    private GameObject warningEffectPrefab;

    private bool explosionTriggered = false;

    public override void Initialize(Enemy enemyRef, EnemyController controllerRef)
    {
        base.Initialize(enemyRef, controllerRef);
    }

    public override void UpdateMovement()
    {
        // 기본 Enemy처럼 플레이어를 향해 이동 (별도 패턴 없음)
        if (target == null || rb == null) return;

        Vector2 direction = (target.position - transform.position).normalized;
        float moveSpeed = controller != null ? controller.GetMoveSpeed() : 3f;
        rb.velocity = direction * moveSpeed;
    }

    public override void UpdateAttack()
    {
        // 기본 근접 공격은 EnemyController에서 처리
    }

    public override void OnDeath()
    {
        if (!explosionTriggered)
        {
            explosionTriggered = true;
            StartCoroutine(ExplosionRoutine());
        }
    }

    private IEnumerator ExplosionRoutine()
    {
        // 경고 효과 표시
        if (warningEffectPrefab != null)
        {
            GameObject warning = Instantiate(warningEffectPrefab, transform.position, Quaternion.identity);
            Destroy(warning, explosionDelay);
        }

        // 경고음 효과 (TODO: 사운드 매니저 연동)
        Debug.Log($"[BomberMechanism] {gameObject.name} 폭발 경고! {explosionDelay}초 후 폭발!");

        yield return new WaitForSeconds(explosionDelay);

        // 폭발 실행
        Explode();
    }

    private void Explode()
    {
        Vector2 explosionPos = transform.position;

        // 폭발 이펙트 생성
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, explosionPos, Quaternion.identity);
        }

        // 범위 내 모든 캐릭터에게 대미지
        Collider2D[] hits = Physics2D.OverlapCircleAll(explosionPos, explosionRadius);

        foreach (var hit in hits)
        {
            Character character = hit.GetComponent<Character>();
            if (character != null && character != enemy) // 자기 자신은 제외
            {
                float distance = Vector2.Distance(explosionPos, hit.transform.position);
                float damageMultiplier = 1f - (distance / explosionRadius); // 거리에 따라 대미지 감소
                float finalDamage = explosionDamage * damageMultiplier;
                
                character.TakeDamage(finalDamage);
            }
        }

        Debug.Log($"[BomberMechanism] {gameObject.name} 폭발! 범위: {explosionRadius}, 대미지: {explosionDamage}");

        // 오브젝트 파괴
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
