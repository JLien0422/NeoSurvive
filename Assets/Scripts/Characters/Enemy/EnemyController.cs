using System.Collections;
using UnityEngine;

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

  // 참조
  private Enemy enemy;
  private Rigidbody2D rb;
  private Transform target; // 추적 대상 (Player 또는 Decoy)

  private float searchTimer;

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

    UpdateTarget();
  }

  // 게임 시작 시 호출됩니다.
  private void Start()
  {
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
  }

  private void UpdateTarget()
  {
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

  // 일정 주기로 플레이어를 공격하는 코루틴입니다.
  private IEnumerator AttackCoroutine()
  {
    while (true)
    {
      yield return new WaitForSeconds(attackInterval);

      if (target != null)
      {
        float distanceToTarget = Vector2.Distance(transform.position, target.position);

        if (distanceToTarget <= attackRange)
        {
          // 대상이 Character(Player, Enemy, Decoy)인지 확인
          if (target.TryGetComponent<Character>(out var character))
          {
            character.TakeDamage(attackDamage);
          }
        }
      }
    }
  }

  // 고정된 시간 간격으로 호출됩니다. 물리 및 AI 계산에 적합합니다.
  private void FixedUpdate()
  {
    if (target != null)
    {
      float distanceToTarget = Vector2.Distance(transform.position, target.position);

      if (distanceToTarget > attackRange)
      {
        Vector2 direction = (target.position - transform.position).normalized;
        rb.velocity = direction * moveSpeed;
      }
      else
      {
        rb.velocity = Vector2.zero;
      }
    }
    else
    {
      rb.velocity = Vector2.zero;
      // 타겟 놓치면 즉시 재검색 권장? 다음 틱 UpdateTarget에서 처리됨
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
}
