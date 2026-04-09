using UnityEngine;

/// <summary>
/// 탱커/벽형 메커니즘
/// 매우 느리지만 덩치가 커서 길목을 가로막습니다.
/// </summary>
public class TankerMechanism : EnemyMechanismBase
{
    [Header("탱커 설정")]
    [SerializeField]
    [Tooltip("기본 이동 속도 배율 (기본 EnemyController보다 매우 느림)")]
    private float speedMultiplier = 0.3f;

    [SerializeField]
    [Tooltip("체력 배율 (기본 Enemy보다 높음)")]
    private float healthMultiplier = 2f;

    [SerializeField]
    [Tooltip("공격 범위 배율 (기본보다 넓음) - EnemyController 공격 판정 시 적용 예정")]
    private float attackRangeMultiplier = 1.5f;

    private float baseMoveSpeed;
    private float baseHealth;
    private float baseAttackRange;

    public override void Initialize(Enemy enemyRef, EnemyController controllerRef)
    {
        base.Initialize(enemyRef, controllerRef);

        // EnemyController의 기본 이동 속도 가져오기
        if (controller != null)
        {
            baseMoveSpeed = controller.GetMoveSpeed();
            // 탱커 공격 범위는 기본 범위에 배율을 적용하여 계산
            baseAttackRange = controller.GetAttackRange() * attackRangeMultiplier;
        }
        else
        {
            baseMoveSpeed = 3f;
            baseAttackRange = 1.5f * attackRangeMultiplier;
        }

        // 체력 증가
        if (enemy != null && enemy.CurrentHP != null)
        {
            baseHealth = enemy.CurrentHP.GetValue();
            enemy.CurrentHP.AddPercentModifier(healthMultiplier - 1f); // 예: 2배면 +100%
            enemy.TakeDamage(0f); // 체력 갱신
        }

        // 공격 범위 증가
        if (controller != null)
        {
            // EnemyController의 attackRange를 직접 수정할 수 없으므로
            // 메커니즘에서 공격 범위를 체크할 때 배율을 적용해야 함
        }
    }

    public override void UpdateMovement()
    {
        if (target == null || rb == null) return;

        // 플레이어를 향해 매우 느리게 이동
        Vector2 direction = (target.position - transform.position).normalized;
        float currentSpeed = baseMoveSpeed * speedMultiplier;

        rb.velocity = direction * currentSpeed;
    }

    public override void UpdateAttack()
    {
        // 기본 근접 공격은 EnemyController에서 처리
        // 탱커는 공격 범위가 넓음
    }

    private void OnDrawGizmosSelected()
    {
        // 탱커의 큰 크기를 시각적으로 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 1.5f);
    }
}
