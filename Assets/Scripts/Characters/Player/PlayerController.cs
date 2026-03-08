using UnityEngine;
using NeoSurvive.Buff; // (추가)

// PlayerController 클래스는 플레이어의 입력을 받아 움직임을 처리합니다.
// 이 컴포넌트는 Player 컴포넌트가 있는 게임 오브젝트에 추가되어야 합니다.
[RequireComponent(typeof(Player))]
public class PlayerController : MonoBehaviour
{
    // 이 컨트롤러가 조종할 Player 컴포넌트에 대한 참조입니다.
    private Player player;
    // 플레이어의 Rigidbody2D 컴포넌트에 대한 참조입니다. (물리 기반 이동을 위해)
    private Rigidbody2D rb;
    // 플레이어의 입력을 저장할 변수입니다.
    private Vector2 moveInput;

    // 애니메이션 및 스프라이트 관련 컴포넌트
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // Animator 파라미터 해시 (문자열 대신 해시 사용 → 성능 최적화)
    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");

    // 해킹 중 고정 상태 (이동 불가)
    private bool isLockedDown = false;
    public bool IsLockedDown => isLockedDown;
    private StatusFlags statusFlags;

    /// <summary>
    /// 고정 상태 설정 (해킹 중 이동/입력 불가)
    /// </summary>
    public void SetLockdown(bool locked)
    {
        isLockedDown = locked;
        if (locked && rb != null)
        {
            rb.velocity = Vector2.zero; // 즉시 정지
        }
    }

    // 컴포넌트가 처음 활성화될 때 호출됩니다.
    private void Awake()
    {
        // 필요한 컴포넌트들을 가져와 변수에 할당합니다.
        player = GetComponent<Player>();
        rb = GetComponent<Rigidbody2D>();

        // Rigidbody2D 컴포넌트가 없다면, 하나 추가해줍니다.
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0; // 2D 탑다운 게임에서는 중력이 필요 없습니다.
        }

        // 애니메이션 컴포넌트 초기화 (없어도 오류 없이 동작)
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 버프/디버프 관련 초기화
        statusFlags = GetComponent<StatusFlags>();
        if (statusFlags == null) statusFlags = gameObject.AddComponent<StatusFlags>();
    }

    // 매 프레임마다 호출됩니다. 입력 처리에 적합합니다.
    private void Update()
    {
        // (추가) 버프/디버프: 이동 불가(속박/기절 등)면 입력 자체를 막음
        if (statusFlags != null && statusFlags.moveBlocked)
        {
            moveInput = Vector2.zero;
            return;
        }

        // 고정 상태면 입력 무시
        if (isLockedDown)
        {
            moveInput = Vector2.zero;
            return;
        }

        // 폭주 상태일 때는 자동 이동
        if (player != null && player.IsBerserk)
        {
            // 가장 가까운 적을 향해 자동 이동
            GameObject closestEnemy = FindClosestEnemy();
            if (closestEnemy != null)
            {
                Vector3 direction = (closestEnemy.transform.position - transform.position).normalized;
                moveInput = new Vector2(direction.x, direction.y);
            }
            else
            {
                // 적이 없으면 랜덤 방향으로 이동
                moveInput = Random.insideUnitCircle.normalized;
            }
        }
        else
        {
            // 수평 및 수직 입력을 받아옵니다. (기본적으로 키보드 화살표 또는 WASD)
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            // 입력 값을 Vector2 형태로 저장하고 정규화(normalize)합니다.
            // 정규화를 통해 대각선 이동 시 속도가 더 빨라지는 것을 방지합니다.
            moveInput = new Vector2(moveX, moveY).normalized;
        }

        // 이동 입력에 따라 애니메이션 및 스프라이트 방향 갱신
        UpdateAnimation();
    }

    /// <summary>
    /// 이동 상태에 따라 Animator 파라미터와 스프라이트 좌우 반전을 갱신합니다.
    /// </summary>
    private void UpdateAnimation()
    {
        bool isMoving = moveInput.sqrMagnitude > 0f;

        // Animator가 있을 때만 파라미터 설정 (없어도 오류 없이 동작)
        if (animator != null)
            animator.SetBool(IsMovingHash, isMoving);

        // 수평 입력이 있을 때만 flipX 갱신 (수직 이동만 할 때 방향 유지)
        if (spriteRenderer != null && Mathf.Abs(moveInput.x) > 0.01f)
            spriteRenderer.flipX = moveInput.x < 0f;
    }
    
    /// <summary>
    /// 가장 가까운 적을 찾습니다.
    /// </summary>
    private GameObject FindClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject closest = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }
        return closest;
    }

    // 고정된 시간 간격으로 호출됩니다. 물리 계산에 적합합니다.
    private void FixedUpdate()
    {
        // (추가) 이동 불가면 즉시 정지
        if (statusFlags != null && statusFlags.moveBlocked)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        // Rigidbody의 속도를 변경하여 플레이어를 움직입니다.
        // 이제 Player 스크립트에 있는 최종 계산된 이동 속도(CurrentMoveSpeed)를 사용합니다.
        if (rb != null && player != null)
        {
            rb.velocity = moveInput * player.CurrentMoveSpeed;
        }
    }
}
