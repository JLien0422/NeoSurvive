using System.Collections;
using UnityEngine;
using NeoSurvive.Buff; // (추가)

// EnemyController 클래스는 적 캐릭터의 인공지능(AI)과 움직임을 처리합니다.
// 이 컴포넌트는 Enemy 컴포넌트가 있는 게임 오브젝트에 추가되어야 합니다.
[RequireComponent(typeof(Enemy))]
public class EnemyController : MonoBehaviour
{
  [Header("이동 및 감지 설정")]
  // 적의 이동 속도입니다.
  [SerializeField]
  private float moveSpeed = 3f;

  [Header("공격 설정")]
  // 적의 공격력입니다.
  [SerializeField]
  private float attackDamage = 5f;
  // 적이 플레이어를 공격할 수 있는 범위입니다.
  [SerializeField]
  private float attackRange = 1.5f;
  // 공격 주기 (3초로 고정)
  private const float attackInterval = 3.0f;

  [Header("기즈모 설정")]
  // 에디터에서 감지 범위 기즈모를 항상 표시할지 여부를 설정합니다.
  [SerializeField]
  private bool showGizmos = true;

  [Header("메커니즘 설정")]
  [SerializeField]
  [Tooltip("적 메커니즘 타입")]
  private EnemyMechanismType mechanismType = EnemyMechanismType.Basic;

  // 참조
  private Enemy enemy;
  private Rigidbody2D rb;
  private Transform target; // 추적 대상 (Player 또는 Decoy)
  private EnemyMechanismBase currentMechanism; // 현재 활성화된 메커니즘

  private float searchTimer;

  private StatusFlags statusFlags; // (추가)

  private KnockbackDebuff knockbackDebuff;

  // 컴포넌트가 처음 활성화될 때 호출됩니다.
  private void Awake()
  {
    enemy = GetComponent<Enemy>();
    rb = GetComponent<Rigidbody2D>();

    if (rb == null)
    {
      rb = gameObject.AddComponent<Rigidbody2D>();
      rb.gravityScale = 0;
    }

    statusFlags = GetComponent<StatusFlags>(); // (추가)
    if (statusFlags == null) statusFlags = gameObject.AddComponent<StatusFlags>(); // (추가)

    knockbackDebuff = GetComponent<KnockbackDebuff>();
    if (knockbackDebuff == null)
        knockbackDebuff = gameObject.AddComponent<KnockbackDebuff>();

    UpdateTarget();
    InitializeMechanism();
  }

  /// <summary>
  /// 메커니즘 초기화
  /// 프리팹에 이미 해당 메커니즘이 붙어 있으면 그걸 재사용(인스펙터 값 유지), 없을 때만 AddComponent.
  /// </summary>
  private void InitializeMechanism()
  {
    // 기존 메커니즘 제거 (단, 같은 타입이면 제거하지 않고 재사용)
    if (currentMechanism != null)
    {
      bool sameType = (mechanismType == EnemyMechanismType.Shooter && currentMechanism is ShooterMechanism)
        || (mechanismType == EnemyMechanismType.Rusher && currentMechanism is RusherMechanism)
        || (mechanismType == EnemyMechanismType.Bomber && currentMechanism is BomberMechanism)
        || (mechanismType == EnemyMechanismType.Tanker && currentMechanism is TankerMechanism);
      if (!sameType)
        Destroy(currentMechanism);
      else
      {
        currentMechanism.Initialize(enemy, this);
        currentMechanism.SetTarget(target);
        return;
      }
    }

    // 메커니즘 타입에 따라 컴포넌트 추가 (이미 있으면 GetComponent로 재사용)
    switch (mechanismType)
    {
      case EnemyMechanismType.Basic:
        currentMechanism = null;
        break;

      case EnemyMechanismType.Shooter:
        currentMechanism = GetComponent<ShooterMechanism>();
        if (currentMechanism == null) currentMechanism = gameObject.AddComponent<ShooterMechanism>();
        break;

      case EnemyMechanismType.Rusher:
        currentMechanism = GetComponent<RusherMechanism>();
        if (currentMechanism == null) currentMechanism = gameObject.AddComponent<RusherMechanism>();
        break;

      case EnemyMechanismType.Bomber:
        currentMechanism = GetComponent<BomberMechanism>();
        if (currentMechanism == null) currentMechanism = gameObject.AddComponent<BomberMechanism>();
        break;

      case EnemyMechanismType.Tanker:
        currentMechanism = GetComponent<TankerMechanism>();
        if (currentMechanism == null) currentMechanism = gameObject.AddComponent<TankerMechanism>();
        break;
    }

    if (currentMechanism != null)
    {
      currentMechanism.Initialize(enemy, this);
      currentMechanism.SetTarget(target);
    }
  }

  // 게임 시작 시 호출됩니다.
  private void Start()
  {
    ApplyEnemyControllerStatsFromCSV(); // (추가) CSV에서 EnemyController 관련 스탯 적용
    StartCoroutine(AttackCoroutine());
  }

  private void Update()
  {
    // 1초마다 타겟 갱신 (비용 절감)
    searchTimer += Time.deltaTime;
    if (searchTimer >= 1.0f)
    {
      UpdateTarget();
      searchTimer = 0f;
    }

    // 메커니즘 공격 로직 업데이트
    if (currentMechanism != null)
    {
      currentMechanism.UpdateAttack();
    }
  }

  private void UpdateTarget()
  {
    // (추가) 광란이면 "플레이어/디코이" 대신 "가장 가까운 Enemy"를 타겟으로
    if (statusFlags != null && statusFlags.frenzy)
    {
      target = FindClosestEnemyTarget();
      if (currentMechanism != null)
        currentMechanism.SetTarget(target);
      return;
    }

    // (추가) 실명(Blind) 상태면 가끔 타겟을 못 찾게(간단 버전)
    if (statusFlags != null && statusFlags.blinded)
    {
      // 50% 확률로 타겟을 놓침 (원하면 수치 조정)
      if (Random.value < 0.5f)
      {
        target = null;
        if (currentMechanism != null) currentMechanism.SetTarget(target);
        return;
      }
    }

    GameObject playerObj = GameObject.FindWithTag("Player");
    GameObject[] decoys = GameObject.FindGameObjectsWithTag("Decoy");

    Transform closest = null;
    float closestDist = float.MaxValue;

    // 플레이어 거리 체크
    if (playerObj != null)
    {
      float d = Vector2.Distance(transform.position, playerObj.transform.position);
      if (d < closestDist)
      {
        closestDist = d;
        closest = playerObj.transform;
      }
    }

    // 디코이 거리 체크
    if (decoys != null)
    {
      foreach (var decoy in decoys)
      {
        float d = Vector2.Distance(transform.position, decoy.transform.position);
        if (d < closestDist)
        {
          closestDist = d;
          closest = decoy.transform;
        }
      }
    }

    target = closest;

    // 메커니즘에 타겟 전달
    if (currentMechanism != null)
    {
      currentMechanism.SetTarget(target);
    }
  }

  // (추가) 광란용: 가장 가까운 적(Enemy) 찾기
  private Transform FindClosestEnemyTarget() // (추가)
  {
    GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
    Transform closest = null;
    float closestDist = float.MaxValue;

    foreach (var e in enemies)
    {
      if (e == null) continue;
      if (e == gameObject) continue; // 자기 자신 제외

      float d = Vector2.Distance(transform.position, e.transform.position);
      if (d < closestDist)
      {
        closestDist = d;
        closest = e.transform;
      }
    }
    return closest;
  }

  // 외부에서 이속 제어 (슬로우 효과)
  public void ApplySlow(float multiplier, float duration)
  {
    StartCoroutine(SlowRoutine(multiplier, duration));
  }

  private IEnumerator SlowRoutine(float mult, float duration)
  {
    float original = moveSpeed;
    moveSpeed *= mult;
    yield return new WaitForSeconds(duration);
    moveSpeed = original;
  }

  // 아군으로 전환 시 적을 타겟으로 설정
  public void SetTargetToEnemies()
  {
    // 적을 찾아서 타겟으로 설정
    UpdateTarget();
    // TODO: 적을 공격하도록 AI 로직 수정 필요
  }

  // 다시 적으로 복귀 시 플레이어를 타겟으로 설정
  public void SetTargetToPlayer()
  {
    UpdateTarget();
  }

  /// <summary>
  /// 적 사망 시 호출 (메커니즘의 OnDeath 처리)
  /// </summary>
  public void OnEnemyDeath()
  {
    if (currentMechanism != null)
    {
      currentMechanism.OnDeath();
    }
  }

  /// <summary>
  /// 이동 속도 가져오기 (메커니즘에서 사용)
  /// </summary>
  public float GetMoveSpeed()
  {
    float mul = (statusFlags != null) ? statusFlags.moveSpeedMul : 1f;
    return moveSpeed * mul;
  }

  /// <summary>
  /// 공격 범위 가져오기 (메커니즘에서 사용)
  /// </summary>
  public float GetAttackRange()
  {
    return attackRange;
  }

  /// <summary>
  /// 현재 메커니즘 타입 가져오기
  /// </summary>
  public EnemyMechanismType GetMechanismType()
  {
    return mechanismType;
  }

  /// <summary>
  /// 현재 활성화된 메커니즘 가져오기 (null이면 Basic)
  /// </summary>
  public EnemyMechanismBase GetCurrentMechanism()
  {
    return currentMechanism;
  }

  // 일정 주기로 플레이어를 공격하는 코루틴입니다.
  private IEnumerator AttackCoroutine()
  {
    while (true)
    {
      yield return new WaitForSeconds(attackInterval);

      // (추가) 기절 등으로 공격 불가면 패스
      if (statusFlags != null && statusFlags.attackBlocked)
        continue;


      if (target != null)
      {
        float distanceToTarget = Vector2.Distance(transform.position, target.position);

        if (distanceToTarget <= attackRange)
        {
          // 대상이 Character(Player, Enemy, Decoy)인지 확인
          if (target.TryGetComponent<Character>(out var character))
          {
            // (추가) 데미지 버프/디버프(가하는 피해) 적용
            float outMul = (statusFlags != null) ? statusFlags.outgoingDamageMul : 1f;
            float finalDamage = attackDamage * outMul;

            character.TakeDamage(finalDamage);
          }
        }
      }
    }
  }

  // 고정된 시간 간격으로 호출됩니다. 물리 및 AI 계산에 적합합니다.
  private void FixedUpdate()
  {
    // (추가) 속박/기절 등 이동 불가면 즉시 정지
    if (statusFlags != null && statusFlags.moveBlocked)
    {
      if (rb != null) rb.velocity = Vector2.zero;
      return;
    }

    // 넉백 중이면 EnemyController의 AI 이동이 velocity를 덮어쓰지 않도록 중단
    if (knockbackDebuff != null && knockbackDebuff.IsKnockbackActive)
    {
        return;
    }

    // 메커니즘이 있으면 메커니즘의 이동 로직 사용
    if (currentMechanism != null)
    {
      // (추가) 메커니즘이 EnemyController.GetMoveSpeed()를 사용한다면 배율이 자동 반영됨.
      currentMechanism.UpdateMovement();
    }
    else
    {
      // 기본 이동 로직
      if (target != null)
      {
        float distanceToTarget = Vector2.Distance(transform.position, target.position);

        if (distanceToTarget > attackRange)
        {
          Vector2 direction = (target.position - transform.position).normalized;
          rb.velocity = direction * GetMoveSpeed();
        }
        else
        {
          rb.velocity = Vector2.zero;
        }
      }
      else
      {
        rb.velocity = Vector2.zero;
      }
    }
  }

#if UNITY_EDITOR
    // 에디터에서 감지 범위와 공격 범위를 시각적으로 보여줍니다.
    private void OnDrawGizmos()
    {
        if (showGizmos)
        {
            // 공격 범위 (노란색)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
#endif
  private void ApplyEnemyControllerStatsFromCSV()
  {
    if (EnemyStatLoader.DB == null)
    {
      Debug.LogWarning("[EnemyController] EnemyStatLoader.DB가 null입니다.");
      return;
    }

    if (enemy == null)
    {
      Debug.LogWarning("[EnemyController] Enemy 참조가 null입니다.");
      return;
    }

    string key = mechanismType.ToString().Trim().ToLowerInvariant();

    if (!EnemyStatLoader.DB.rows.TryGetValue(key, out var row))
    {
      Debug.LogWarning($"[EnemyController] enemy_stats.csv에 '{key}'가 없습니다.");
      return;
    }

    if (row.movespeed > 0f) moveSpeed = row.movespeed;
    if (row.attackdamage > 0f) attackDamage = row.attackdamage;
    if (row.attackrange > 0f) attackRange = row.attackrange;

    Debug.Log($"[EnemyController] CSV 스탯 적용 완료: {key} | moveSpeed={moveSpeed}, attackDamage={attackDamage}, attackRange={attackRange}");
  }
}
