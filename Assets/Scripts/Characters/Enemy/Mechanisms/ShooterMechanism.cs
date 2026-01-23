using System.Collections;
using UnityEngine;

/// <summary>
/// 원거리 공격형 메커니즘
/// 일반 Enemy처럼 이동하지만, 공격 사거리가 길고 공격 시 투사체를 발사합니다.
/// </summary>
public class ShooterMechanism : EnemyMechanismBase
{
    [Header("원거리 공격 설정")]
    [SerializeField]
    [Tooltip("공격 사거리 배율 (기본 EnemyController보다 길게)")]
    private float attackRangeMultiplier = 5f;

    [SerializeField]
    [Tooltip("공격 주기 (초)")]
    private float attackInterval = 2f;

    [SerializeField]
    [Tooltip("발사할 투사체 프리팹")]
    private GameObject projectilePrefab;

    [SerializeField]
    [Tooltip("투사체 속도")]
    private float projectileSpeed = 5f;

    [SerializeField]
    [Tooltip("투사체 대미지")]
    private float projectileDamage = 5f;

    [SerializeField]
    [Tooltip("발사 위치 오프셋")]
    private Vector2 shootOffset = Vector2.zero;

    private float baseAttackRange;
    private float extendedAttackRange;
    private float lastAttackTime = 0f;

    public override void Initialize(Enemy enemyRef, EnemyController controllerRef)
    {
        base.Initialize(enemyRef, controllerRef);
        
        // 기본 공격 범위 가져오기
        if (controller != null)
        {
            baseAttackRange = controller.GetAttackRange();
            extendedAttackRange = baseAttackRange * attackRangeMultiplier;
        }
        else
        {
            baseAttackRange = 1.5f;
            extendedAttackRange = baseAttackRange * attackRangeMultiplier;
        }
    }

    public override void UpdateMovement()
    {
        // 일반 Enemy처럼 이동 (기본 이동 로직 사용)
        if (target == null || rb == null) return;

        float distanceToTarget = Vector2.Distance(transform.position, target.position);

        // 확장된 공격 범위 밖에 있으면 이동
        if (distanceToTarget > extendedAttackRange)
        {
            Vector2 direction = (target.position - transform.position).normalized;
            float moveSpeed = controller != null ? controller.GetMoveSpeed() : 3f;
            rb.velocity = direction * moveSpeed;
        }
        else
        {
            // 공격 범위 내에 있으면 정지
            rb.velocity = Vector2.zero;
        }
    }

    public override void UpdateAttack()
    {
        if (target == null || projectilePrefab == null) return;

        float distanceToTarget = Vector2.Distance(transform.position, target.position);

        // 확장된 공격 범위 내에 있고, 공격 주기가 지났으면 투사체 발사
        if (distanceToTarget <= extendedAttackRange && Time.time - lastAttackTime >= attackInterval)
        {
            Shoot();
            lastAttackTime = Time.time;
        }
    }

    private void Shoot()
    {
        if (target == null) return;

        Vector2 shootPosition = (Vector2)transform.position + shootOffset;
        Vector2 direction = ((Vector2)target.position - shootPosition).normalized;

        // 투사체 생성
        GameObject projectile = Instantiate(projectilePrefab, shootPosition, Quaternion.identity);
        
        // 투사체 초기화
        NeoSurvive.Weapon.Projectile proj = projectile.GetComponent<NeoSurvive.Weapon.Projectile>();
        if (proj != null)
        {
            proj.Initialize(direction, projectileDamage, projectileSpeed);
        }
        else
        {
            // Projectile 컴포넌트가 없으면 직접 이동 처리
            Rigidbody2D projRb = projectile.GetComponent<Rigidbody2D>();
            if (projRb == null)
            {
                projRb = projectile.AddComponent<Rigidbody2D>();
                projRb.gravityScale = 0;
            }
            projRb.velocity = direction * projectileSpeed;

            // 대미지 처리용 컴포넌트 추가
            EnemyProjectile enemyProj = projectile.GetComponent<EnemyProjectile>();
            if (enemyProj == null)
            {
                enemyProj = projectile.AddComponent<EnemyProjectile>();
            }
            enemyProj.damage = projectileDamage;
        }

        Debug.Log($"[ShooterMechanism] {gameObject.name}이(가) 투사체를 발사했습니다.");
    }
}

/// <summary>
/// 적이 발사하는 투사체 컴포넌트
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    public float damage = 5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Character character = other.GetComponent<Character>();
            if (character != null)
            {
                character.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
        else if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            Destroy(gameObject);
        }
    }
}
