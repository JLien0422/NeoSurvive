using UnityEngine;

/// <summary>
/// 보스 AI 컨트롤러. 현재는 플레이어 추적 기본 이동만 구현되어 있습니다.
/// 보스 패턴은 추후 이 클래스에 추가합니다.
/// </summary>
[RequireComponent(typeof(Boss))]
[RequireComponent(typeof(Rigidbody2D))]
public class BossController : MonoBehaviour
{
    [Header("이동 설정")]
    [Tooltip("보스 이동 속도")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("기즈모 설정")]
    [SerializeField] private bool showGizmos = true;

    private Boss boss;
    private Rigidbody2D rb;
    private Transform playerTarget;

    // 타겟 갱신 타이머 (1초마다 갱신 - 매 프레임 FindWithTag 방지)
    private float searchTimer = 0f;
    private const float SEARCH_INTERVAL = 1f;

    private void Awake()
    {
        boss = GetComponent<Boss>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        FindPlayer();
    }

    private void Update()
    {
        // 1초마다 타겟 갱신
        searchTimer += Time.deltaTime;
        if (searchTimer >= SEARCH_INTERVAL)
        {
            FindPlayer();
            searchTimer = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (boss.IsDead)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        MoveTowardPlayer();
    }

    /// <summary>
    /// 플레이어를 찾아 타겟으로 설정합니다.
    /// </summary>
    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            playerTarget = playerObj.transform;
    }

    /// <summary>
    /// 플레이어 방향으로 이동합니다.
    /// </summary>
    private void MoveTowardPlayer()
    {
        if (playerTarget == null) return;

        Vector2 direction = (playerTarget.position - transform.position).normalized;
        rb.velocity = direction * moveSpeed;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // 보스 위치 표시 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
#endif
}
