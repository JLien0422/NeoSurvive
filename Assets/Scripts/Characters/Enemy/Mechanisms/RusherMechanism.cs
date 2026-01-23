using UnityEngine;

/// <summary>
/// 고속 돌진형 메커니즘
/// 잡몹보다 1.5배 빠른 속도로 직선 추격합니다. (체력 매우 낮음)
/// 기획서: "직선 추격" / "바퀴 자국을 남기며 빠르게 직선 돌진" / "플레이어 근처까지 빠르게 가속하여 접근"
/// </summary>
public class RusherMechanism : EnemyMechanismBase
{
    [Header("돌진형 설정")]
    [SerializeField]
    [Tooltip("기본 이동 속도 배율 (기본 EnemyController보다 빠름)")]
    private float speedMultiplier = 1.5f;

    [SerializeField]
    [Tooltip("돌진 시작 거리 (이 거리 내에 들어오면 돌진 시작)")]
    private float rushStartDistance = 5f;

    [SerializeField]
    [Tooltip("가속 사용 여부 (true면 거리에 따라 속도 증가)")]
    private bool useAcceleration = true;

    [SerializeField]
    [Tooltip("최대 가속 배율")]
    private float maxAccelerationMultiplier = 2f;

    [SerializeField]
    [Tooltip("방향 고정 여부 (true면 한 번 방향을 정하면 그대로 직진)")]
    private bool lockDirection = false;

    private float baseMoveSpeed;
    private bool isRushing = false;
    private Vector2 rushDirection = Vector2.zero;
    private float rushStartTime = 0f;

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
            baseMoveSpeed = 3f; // 기본값
        }

        // 체력 감소 (체력 매우 낮음)
        if (enemy != null && enemy.HealthStat != null)
        {
            float currentHealth = enemy.HealthStat.GetValue();
            enemy.HealthStat.SetBaseValue(currentHealth * 0.5f); // 체력을 절반으로
            enemy.TakeDamage(0f); // 체력 갱신
        }
    }

    public override void UpdateMovement()
    {
        if (target == null || rb == null) return;

        float distanceToTarget = Vector2.Distance(transform.position, target.position);

        // 돌진 시작 거리 내에 들어오면 돌진 시작
        if (distanceToTarget <= rushStartDistance && !isRushing)
        {
            isRushing = true;
            rushStartTime = Time.time;

            if (lockDirection)
            {
                // 방향 고정: 한 번 방향을 정하면 그대로 직진
                rushDirection = (target.position - transform.position).normalized;
            }
        }

        Vector2 direction;
        if (lockDirection && isRushing)
        {
            // 고정된 방향으로 직진
            direction = rushDirection;
        }
        else
        {
            // 매 프레임 플레이어 방향으로 추적
            direction = (target.position - transform.position).normalized;
        }

        // 속도 계산
        float currentSpeed = baseMoveSpeed * speedMultiplier;

        if (useAcceleration && isRushing)
        {
            // 거리가 가까울수록 더 빠르게 (가속)
            float accelerationFactor = 1f + (1f - Mathf.Clamp01(distanceToTarget / rushStartDistance)) * (maxAccelerationMultiplier - 1f);
            currentSpeed *= accelerationFactor;
        }

        rb.velocity = direction * currentSpeed;
    }

    public override void UpdateAttack()
    {
        // 기본 근접 공격은 EnemyController에서 처리
    }
}
