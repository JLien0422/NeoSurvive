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
    // 적이 플레이어를 감지할 수 있는 범위입니다.
    [SerializeField]
    private float detectionRange = 5f;

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
    private Player player; // 플레이어 컴포넌트 참조

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

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.GetComponent<Player>();
        }
    }

    // 게임 시작 시 호출됩니다.
    private void Start()
    {
        if (player != null)
        {
            // 공격 코루틴을 시작합니다.
            StartCoroutine(AttackCoroutine());
        }
    }

    // 일정 주기로 플레이어를 공격하는 코루틴입니다.
    private IEnumerator AttackCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackInterval);

            if (player != null)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
                // 플레이어가 공격 범위 내에 있다면 공격합니다.
                if (distanceToPlayer <= attackRange)
                {
                    player.TakeDamage(attackDamage);
                    Debug.Log($"플레이어에게 {attackDamage}의 데미지를 입혔습니다.");
                }
            }
        }
    }

    // 고정된 시간 간격으로 호출됩니다. 물리 및 AI 계산에 적합합니다.
    private void FixedUpdate()
    {
        // 플레이어가 존재하고 감지 범위 내에 있다면 플레이어를 추적합니다.
        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

            if (distanceToPlayer <= detectionRange)
            {
                // 공격 범위 밖에서만 플레이어를 따라갑니다. (공격 범위 안에 들어오면 멈춤)
                if (distanceToPlayer > attackRange)
                {
                    Vector2 direction = (player.transform.position - transform.position).normalized;
                    rb.velocity = direction * moveSpeed;
                }
                else
                {
                    rb.velocity = Vector2.zero; // 공격 범위에 들어오면 멈춤
                }
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

#if UNITY_EDITOR
    // 에디터에서 감지 범위와 공격 범위를 시각적으로 보여줍니다.
    private void OnDrawGizmos()
    {
        if (showGizmos)
        {
            // 감지 범위 (보라색)
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // 공격 범위 (노란색)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
#endif
}
