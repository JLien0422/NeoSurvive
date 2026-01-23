using UnityEngine;

/// <summary>
/// 적 메커니즘 기본 클래스
/// 모든 적 메커니즘은 이 클래스를 상속받아 구현합니다.
/// </summary>
public abstract class EnemyMechanismBase : MonoBehaviour
{
    protected Enemy enemy;
    protected EnemyController controller;
    protected Rigidbody2D rb;
    protected Transform target;

    /// <summary>
    /// 메커니즘 초기화
    /// </summary>
    public virtual void Initialize(Enemy enemyRef, EnemyController controllerRef)
    {
        enemy = enemyRef;
        controller = controllerRef;
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// 타겟 설정
    /// </summary>
    public virtual void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// 이동 로직 (각 메커니즘마다 다르게 구현)
    /// </summary>
    public abstract void UpdateMovement();

    /// <summary>
    /// 공격 로직 (각 메커니즘마다 다르게 구현)
    /// </summary>
    public abstract void UpdateAttack();

    /// <summary>
    /// 사망 시 특수 효과 (Bomber 등)
    /// </summary>
    public virtual void OnDeath()
    {
        // 기본적으로는 아무것도 하지 않음
    }
}
