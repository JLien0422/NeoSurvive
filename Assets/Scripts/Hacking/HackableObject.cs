using UnityEngine;

/// <summary>
/// 해킹 가능한 오브젝트
/// 플레이어가 근처에서 E키를 누르면 해킹 미니게임이 시작됩니다.
/// </summary>
public class HackableObject : MonoBehaviour
{
    [Header("해킹 설정")]
    [SerializeField]
    [Tooltip("해킹 가능한 거리")]
    private float hackRange = 2f;

    [SerializeField]
    [Tooltip("해킹 가능 여부")]
    private bool canHack = true;

    private Player nearbyPlayer = null;
    private bool isHacking = false;

    private void Update()
    {
        // 해킹 중이면 입력 처리 안 함
        if (isHacking) return;

        // 플레이어 감지
        CheckForPlayer();

        // E키 입력 감지
        if (nearbyPlayer != null && Input.GetKeyDown(KeyCode.E))
        {
            StartHacking();
        }
    }

    /// <summary>
    /// 근처에 플레이어가 있는지 확인
    /// </summary>
    private void CheckForPlayer()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            nearbyPlayer = null;
            return;
        }

        Player player = playerObj.GetComponent<Player>();
        if (player == null)
        {
            nearbyPlayer = null;
            return;
        }

        float distance = Vector3.Distance(transform.position, playerObj.transform.position);
        if (distance <= hackRange)
        {
            nearbyPlayer = player;
        }
        else
        {
            nearbyPlayer = null;
        }
    }

    /// <summary>
    /// 해킹 시작
    /// </summary>
    private void StartHacking()
    {
        if (!canHack || nearbyPlayer == null) return;

        isHacking = true;

        // 해킹 시스템에 해킹 시작 알림
        if (HackingSystem.Instance != null)
        {
            HackingSystem.Instance.StartHacking(this, nearbyPlayer);
        }
    }

    /// <summary>
    /// 해킹 종료 (성공/실패 모두)
    /// </summary>
    public void OnHackingEnded()
    {
        isHacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, hackRange);
    }
}
