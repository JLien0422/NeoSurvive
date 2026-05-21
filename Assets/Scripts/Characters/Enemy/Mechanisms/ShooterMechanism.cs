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
    [Tooltip("투사체를 발사하기 시작하는 사거리")]
    private float attackRange = 7.5f;

    [SerializeField]
    [Tooltip("켜면 EnemyController가 CSV에서 읽은 attackdamage/attackrange를 우선 사용합니다.")]
    private bool useControllerCombatStats = true;

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
    [Tooltip("발사 위치 오프셋 (적 루트 로컬 공간; localScale.x 반전 시 TransformPoint로 같이 미러링됨)")]
    private Vector2 shootOffset = Vector2.zero;

    [SerializeField]
    [Tooltip("투사체 스프라이트 기본 전방 각도 보정값(도). 기본 아트가 오른쪽(+X)을 향하면 0")]
    private float projectileForwardAngleOffset = 0f;

    [SerializeField]
    [Tooltip("공격 애니메이션을 재생할 총 Animator (미할당 시 자동 탐색)")]
    private Animator gunAnimator;

    [SerializeField]
    [Tooltip("자동 탐색 시 사용할 총 오브젝트 이름")]
    private string gunObjectName = "Shooter's Gun";

    [SerializeField]
    [Tooltip("몸 Walk 애니 재생 속도 제어용 (비우면 이 오브젝트의 Animator 사용)")]
    private Animator bodyAnimator;

    [SerializeField]
    [Tooltip("이 속도 제곱 이하면 몸 Animator.speed = 0 (정지로 간주)")]
    private float bodySpeedStopSqrThreshold = 0.0001f;

    [SerializeField]
    [Tooltip("정지 중에도 Walk 프레임이 어색하면 끕니다. Idle 상태가 생기기 전까지는 false 권장")]
    private bool pauseBodyAnimationWhenStopped = true;

    [SerializeField]
    [Tooltip("타겟이 좌우로 이동할 때 루트 스케일 X를 반전합니다. 기본 아트가 오른쪽을 본다는 전제입니다.")]
    private bool faceTargetOnX = true;

    [SerializeField]
    [Tooltip("타겟과 X 차이가 이 값보다 작으면 좌우 반전을 유지합니다.")]
    private float faceTargetDeadZone = 0.02f;

    [SerializeField]
    [Tooltip("애니메이션 이벤트가 누락되었을 때 대기 후 발사하는 안전장치 시간")]
    private float attackEventFallbackDelay = 0.35f;

    private static readonly int ShooterAttackStateHash = Animator.StringToHash("Shooter_Attack");

    private float lastAttackTime = float.NegativeInfinity;
    private bool pendingProjectileFromAttackAnim;
    private float defaultFacingScaleX = 1f;
    private Coroutine attackEventFallbackRoutine;

    public override void Initialize(Enemy enemyRef, EnemyController controllerRef)
    {
        base.Initialize(enemyRef, controllerRef);
        defaultFacingScaleX = Mathf.Abs(transform.localScale.x);
        ResolveGunAnimatorIfNeeded();
        ResolveBodyAnimatorIfNeeded();
    }

    public override void UpdateMovement()
    {
        if (target == null || rb == null)
        {
            ApplyBodyAnimatorPausedState(true);
            return;
        }

        FaceTargetIfNeeded();

        // sqrMagnitude로 제곱근 없이 거리 비교
        float sqrDist = (transform.position - target.position).sqrMagnitude;
        float currentAttackRange = GetEffectiveAttackRange();
        float sqrRange = currentAttackRange * currentAttackRange;

        if (sqrDist > sqrRange)
        {
            Vector2 direction = (target.position - transform.position).normalized;
            float moveSpeed = controller != null ? controller.GetMoveSpeed() : 3f;
            rb.velocity = direction * moveSpeed;
        }
        else
        {
            rb.velocity = Vector2.zero;
        }

        ApplyBodyAnimatorPausedState(rb.velocity.sqrMagnitude <= bodySpeedStopSqrThreshold);
    }

    public override void UpdateAttack()
    {
        if (target == null)
        {
#if UNITY_EDITOR
            Debug.Log("[ShooterMechanism] 공격 불가: target이 null입니다.", this);
#endif
            return;
        }

        FaceTargetIfNeeded();

        if (!CanAttack())
        {
            pendingProjectileFromAttackAnim = false;
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.LogWarning("[ShooterMechanism] projectilePrefab이 비어 있어 투사체를 발사할 수 없습니다.", this);
            return;
        }

        // sqrMagnitude로 제곱근 없이 거리 비교
        float sqrDist = (transform.position - target.position).sqrMagnitude;
        float currentAttackRange = GetEffectiveAttackRange();
        float sqrRange = currentAttackRange * currentAttackRange;

        if (sqrDist <= sqrRange && Time.time - lastAttackTime >= attackInterval)
        {
            pendingProjectileFromAttackAnim = true;
            lastAttackTime = Time.time;
            Shoot();
        }
    }

    private void Shoot()
    {
        // 발사 타이밍에 총 공격 애니메이션 재생
        if (gunAnimator != null)
        {
            gunAnimator.Play(ShooterAttackStateHash, 0, 0f);
            RestartAttackEventFallback();
            return;
        }

        // 총 Animator가 없으면 기존처럼 즉시 발사
        FireProjectileNowFromAnimationEvent();
    }

    /// <summary>
    /// Shooter_Attack 애니메이션 이벤트 프레임에서 호출.
    /// pending 상태일 때만 1발 발사되어 중복 발사를 방지합니다.
    /// </summary>
    public void FireProjectileNowFromAnimationEvent()
    {
        if (!pendingProjectileFromAttackAnim)
            return;

        pendingProjectileFromAttackAnim = false;

        if (attackEventFallbackRoutine != null)
        {
            StopCoroutine(attackEventFallbackRoutine);
            attackEventFallbackRoutine = null;
        }

        if (!CanAttack())
            return;

        if (target == null || projectilePrefab == null)
            return;

        // 로컬 오프셋 → 월드 (scale.x 부호 반전 시 총구 쪽도 같이 뒤집힘)
        Vector3 shootWorld = transform.TransformPoint(new Vector3(shootOffset.x, shootOffset.y, 0f));
        Vector2 shootPosition = new Vector2(shootWorld.x, shootWorld.y);
        Vector2 direction = ((Vector2)target.position - shootPosition).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + projectileForwardAngleOffset;
        Quaternion projectileRotation = Quaternion.Euler(0f, 0f, angle);
        float damage = GetEffectiveProjectileDamage();

        // 투사체 생성
        GameObject projectile = Instantiate(projectilePrefab, shootPosition, projectileRotation);

        // 투사체 초기화
        NeoSurvive.Weapon.Projectile proj = projectile.GetComponent<NeoSurvive.Weapon.Projectile>();
        if (proj != null)
        {
            proj.Initialize(direction, damage, projectileSpeed);
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
            enemyProj.damage = damage;
        }

        Debug.Log($"[ShooterMechanism] {gameObject.name}이(가) 투사체를 발사했습니다.");
    }

    private void ResolveGunAnimatorIfNeeded()
    {
        if (gunAnimator != null) return;

        if (!string.IsNullOrWhiteSpace(gunObjectName))
        {
            Transform gun = transform.Find(gunObjectName);
            if (gun != null)
            {
                gunAnimator = gun.GetComponent<Animator>();
            }
        }

        if (gunAnimator == null)
        {
            Animator[] animators = GetComponentsInChildren<Animator>(true);
            foreach (Animator anim in animators)
            {
                if (anim == null) continue;
                if (anim == GetComponent<Animator>()) continue; // 본체 Animator는 제외
                gunAnimator = anim;
                break;
            }
        }
    }

    private void ResolveBodyAnimatorIfNeeded()
    {
        if (bodyAnimator != null) return;
        bodyAnimator = GetComponent<Animator>();
    }

    private void ApplyBodyAnimatorPausedState(bool paused)
    {
        if (bodyAnimator == null) return;
        bodyAnimator.speed = pauseBodyAnimationWhenStopped && paused ? 0f : 1f;
    }

    private bool CanAttack()
    {
        return enemy == null || enemy.CanAttack;
    }

    private float GetEffectiveAttackRange()
    {
        if (useControllerCombatStats && controller != null)
        {
            float controllerAttackRange = controller.GetAttackRange();
            if (controllerAttackRange > 0f)
                return controllerAttackRange;
        }

        return attackRange;
    }

    private float GetEffectiveProjectileDamage()
    {
        if (useControllerCombatStats && controller != null)
        {
            float controllerDamage = controller.GetAttackDamage();
            if (controllerDamage > 0f)
                return controllerDamage;
        }

        return projectileDamage;
    }

    private void FaceTargetIfNeeded()
    {
        if (!faceTargetOnX || target == null) return;

        float deltaX = target.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) <= faceTargetDeadZone) return;

        Vector3 scale = transform.localScale;
        float facingSign = deltaX >= 0f ? 1f : -1f;
        scale.x = defaultFacingScaleX * facingSign;
        transform.localScale = scale;
    }

    private void RestartAttackEventFallback()
    {
        if (attackEventFallbackDelay <= 0f) return;

        if (attackEventFallbackRoutine != null)
        {
            StopCoroutine(attackEventFallbackRoutine);
        }

        attackEventFallbackRoutine = StartCoroutine(FireFromAnimationEventFallback());
    }

    private IEnumerator FireFromAnimationEventFallback()
    {
        yield return new WaitForSeconds(attackEventFallbackDelay);
        attackEventFallbackRoutine = null;

        if (pendingProjectileFromAttackAnim)
        {
            FireProjectileNowFromAnimationEvent();
        }
    }
}

/// <summary>
/// 적이 발사하는 투사체 컴포넌트
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    public float damage = 5f;
    [SerializeField] private float lifeTime = 5f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

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
