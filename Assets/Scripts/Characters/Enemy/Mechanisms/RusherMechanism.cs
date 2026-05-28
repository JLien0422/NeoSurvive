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
    [Tooltip("가속 사용 여부 (true면 돌진 시간에 따라 속도 증가)")]
    private bool useAcceleration = true;

    [SerializeField]
    [Tooltip("최대 가속 배율")]
    private float maxAccelerationMultiplier = 2f;

    [SerializeField]
    [Tooltip("한 번 돌진할 때 일직선으로 이동하는 시간")]
    private float rushDuration = 0.8f;

    [SerializeField]
    [Tooltip("돌진 종료 후 다음 돌진까지 최소 대기 시간")]
    private float rushCooldown = 0.5f;

    private float baseMoveSpeed;
    private bool isRushing = false;
    private Vector2 rushDirection = Vector2.zero;
    private float rushStartTime = 0f;
    private float nextRushTime = 0f;

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

    }

    public override void UpdateMovement()
    {
        if (target == null || rb == null) return;

        if (isRushing)
        {
            float elapsed = Time.time - rushStartTime;
            if (elapsed < rushDuration)
            {
                float currentSpeed = baseMoveSpeed * speedMultiplier;
                if (useAcceleration)
                {
                    float accelerationFactor = Mathf.Lerp(1f, maxAccelerationMultiplier, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, rushDuration)));
                    currentSpeed *= accelerationFactor;
                }

                rb.velocity = rushDirection * currentSpeed;
                return;
            }

            isRushing = false;
            nextRushTime = Time.time + rushCooldown;
        }

        Vector2 toTarget = target.position - transform.position;
        float sqrDist = toTarget.sqrMagnitude;

        if (Time.time >= nextRushTime && sqrDist <= rushStartDistance * rushStartDistance)
        {
            isRushing = true;
            rushStartTime = Time.time;
            rushDirection = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.right;

            rb.velocity = rushDirection * baseMoveSpeed * speedMultiplier;
            return;
        }

        // 돌진 중이 아닐 때는 플레이어를 계속 추적합니다.
        rb.velocity = toTarget.normalized * baseMoveSpeed * speedMultiplier;
    }

    public override void UpdateAttack()
    {
        // 접촉 틱 데미지는 EnemyController에서 처리합니다.
    }
}
