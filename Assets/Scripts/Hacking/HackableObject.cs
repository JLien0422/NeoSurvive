using UnityEngine;

/// <summary>
/// 해킹 가능한 오브젝트
/// 플레이어가 근처에서 E키를 누르면 해킹 미니게임이 시작됩니다.
/// </summary>
public class HackableObject : MonoBehaviour
{
    [Header("해킹 오브젝트 타입(기획서 5종)")]
    public HackableType hackableType;

    [Header("해킹 설정")]
    [SerializeField]
    [Tooltip("해킹 가능한 거리")]
    private float hackRange = 2f;

    [SerializeField]
    [Tooltip("해킹 가능 여부")]
    private bool canHack = true;

    private Player nearbyPlayer = null;
    private bool isHacking = false;

    private void OnEnable()
    {
        // ✅ 미니게임 결과를 구독 (미니게임/오브젝트 완전 분리)
        HackingSystem.OnHackSuccess += HandleHackSuccess;
        HackingSystem.OnHackFail += HandleHackFail;
    }

    private void OnDisable()
    {
        HackingSystem.OnHackSuccess -= HandleHackSuccess;
        HackingSystem.OnHackFail -= HandleHackFail;
    }

    private void Update()
    {
        if (isHacking) return;

        CheckForPlayer();

        if (nearbyPlayer != null && Input.GetKeyDown(KeyCode.E))
        {
            StartHacking();
        }
    }

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
        nearbyPlayer = (distance <= hackRange) ? player : null;
    }

    private void StartHacking()
    {
        if (!canHack || nearbyPlayer == null) return;

        isHacking = true;

        if (HackingSystem.Instance != null)
        {
            HackingSystem.Instance.StartHacking(this, nearbyPlayer);
        }
    }

    /// <summary>
    /// 미니게임 성공 이벤트 핸들러
    /// </summary>
    private void HandleHackSuccess(HackableObject obj)
    {
        // ✅ "나"가 해킹 성공한 경우만 처리
        if (obj != this) return;

        if (HackingObjectRewardSystem.Instance != null)
        {
            HackingObjectRewardSystem.Instance.ApplyObjectReward(this);
        }
        else
        {
            Debug.LogWarning("[HackableObject] HackingObjectRewardSystem이 씬에 없습니다!");
        }
    }

    /// <summary>
    /// 미니게임 실패 이벤트 핸들러
    /// </summary>
    private void HandleHackFail(HackableObject obj)
    {
        if (obj != this) return;

        // 실패 시 오브젝트가 잠김/폭발/재시도 불가 같은 정책을 두고 싶으면 여기서 처리
        // Debug.Log($"[HackableObject] 해킹 실패: {name}");
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
