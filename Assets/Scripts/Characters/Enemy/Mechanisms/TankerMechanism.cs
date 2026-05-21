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

    private float baseMoveSpeed;

    public override void Initialize(Enemy enemyRef, EnemyController controllerRef)
    {
        base.Initialize(enemyRef, controllerRef);

        // EnemyController의 기본 이동 속도 가져오기
        if (controller != null)
        {
            baseMoveSpeed = controller.GetMoveSpeed();
        }
        else
        {
            baseMoveSpeed = 3f;
        }

        // 체력 증가
        if (enemy != null && enemy.CurrentHP != null)
        {
            enemy.CurrentHP.AddPercentModifier(healthMultiplier - 1f); // 예: 2배면 +100%
            enemy.TakeDamage(0f); // 체력 갱신
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
        // 접촉 틱 데미지는 EnemyController에서 처리합니다.
    }

    private void OnDrawGizmosSelected()
    {
        // 탱커의 큰 크기를 시각적으로 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 1.5f);
    }
}
