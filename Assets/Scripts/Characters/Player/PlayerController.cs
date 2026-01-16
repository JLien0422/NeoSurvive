using UnityEngine;

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

    // 컴포넌트가 처음 활성화될 때 호출됩니다.
    private void Awake()
    {
        // 필요한 컴포넌트들을 가져와 변수에 할당합니다.
        player = GetComponent<Player>();
        rb = GetComponent<Rigidbody2D>();

        // Rigidbody2D 컴포넌트가 없다면, 하나 추가해줍니다.
        // 이는 물리 시스템과의 상호작용을 위해 필요합니다.
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0; // 2D 탑다운 게임에서는 중력이 필요 없습니다.
        }
    }

    // 매 프레임마다 호출됩니다. 입력 처리에 적합합니다.
    private void Update()
    {
        // 수평 및 수직 입력을 받아옵니다. (기본적으로 키보드 화살표 또는 WASD)
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        // 입력 값을 Vector2 형태로 저장하고 정규화(normalize)합니다.
        // 정규화를 통해 대각선 이동 시 속도가 더 빨라지는 것을 방지합니다.
        moveInput = new Vector2(moveX, moveY).normalized;
    }

    // 고정된 시간 간격으로 호출됩니다. 물리 계산에 적합합니다.
    private void FixedUpdate()
    {
        // Rigidbody의 속도를 변경하여 플레이어를 움직입니다.
        // 이제 Player 스크립트에 있는 최종 계산된 이동 속도(CurrentMoveSpeed)를 사용합니다.
        if (rb != null && player != null)
        {
            rb.velocity = moveInput * player.CurrentMoveSpeed;
        }
    }
}
