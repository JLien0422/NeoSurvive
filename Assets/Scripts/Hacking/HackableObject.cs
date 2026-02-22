using UnityEngine;

/// <summary>
/// 해킹 가능한 오브젝트
/// - 플레이어가 범위 안에서 E키를 누르면 해킹 미니게임 시작
/// - 오브젝트 종류에 따라 미니게임과 성공 효과가 다름
/// </summary>
public class HackableObject : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private HackableObjectType objectType; // 이 오브젝트의 종류
    [SerializeField] private float hackRange = 3f;          // 해킹 가능 범위

    [Header("사이코 잠식도")]
    [SerializeField] private float psychoIncreaseOnFail = 50f; // 실패 시 잠식도 증가량

    private bool canHack = false;    // 플레이어가 범위 안에 있는지
    private bool isHacked = false;   // 이미 해킹 완료됐는지

    private void Update()
    {
        if (isHacked) return;

        CheckForPlayer();

        if (canHack && Input.GetKeyDown(KeyCode.E))
        {
            StartHacking();
        }
    }

    private void CheckForPlayer()
    {
        var player = FindObjectOfType<Player>();
        if (player == null) { canHack = false; return; }

        float dist = Vector2.Distance(transform.position, player.transform.position);
        canHack = dist <= hackRange;
    }

    private void StartHacking()
    {
        if (HackingSystem.Instance == null)
        {
            Debug.LogWarning("[HackableObject] HackingSystem 인스턴스가 없습니다!");
            return;
        }

        if (HackingSystem.Instance.IsHacking) return;

        HackingSystem.Instance.StartHacking(
            objectType,
            OnHackingSuccess,
            OnHackingFailed
        );
    }

    private void OnHackingSuccess()
    {
        isHacked = true;
        Debug.Log($"[HackableObject] {objectType} 해킹 성공! 효과 발동!");
        ActivateEffect();
    }

    private void OnHackingFailed()
    {
        Debug.Log($"[HackableObject] {objectType} 해킹 실패! 사이코잠식도 +{psychoIncreaseOnFail}%");

        var player = FindObjectOfType<Player>();
        if (player != null)
            player.AddPsychoCorruption(psychoIncreaseOnFail);
    }

    /// <summary>
    /// 오브젝트 종류별 성공 효과
    /// </summary>
    private void ActivateEffect()
    {
        switch (objectType)
        {
            case HackableObjectType.SecurityTurret:
                // 보안 터렛 활성화 → 가장 가까운 적 연사
                Debug.Log("[Effect] 보안 터렛 활성화!");
                // TODO: 터렛 활성화 로직 연결
                break;

            case HackableObjectType.ElectricFence:
                // 전기 울타리 생성
                Debug.Log("[Effect] 전기 울타리 생성!");
                // TODO: 울타리 생성 로직 연결
                break;

            case HackableObjectType.SatelliteUplink:
                // 레이저 빔 소사
                Debug.Log("[Effect] 새틀라이트 레이저 발동!");
                // TODO: 레이저 소사 로직 연결
                break;

            case HackableObjectType.SynapseServer:
                // 적 절반 아군화
                Debug.Log("[Effect] 적 절반 아군화!");
                // TODO: 적 아군화 로직 연결
                break;

            case HackableObjectType.MagneticBeacon:
                // 적 전체 우측으로 견인
                Debug.Log("[Effect] 마그네틱 비컨 발동 - 적 견인!");
                // TODO: 적 견인 로직 연결
                break;
        }

        // 오브젝트 제거 또는 비활성화
        gameObject.SetActive(false);
    }

    // 에디터에서 범위 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hackRange);
    }
}
