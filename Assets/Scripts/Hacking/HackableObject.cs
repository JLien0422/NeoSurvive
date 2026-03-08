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

        // 이 오브젝트의 월드 좌표를 전달하여 사이보그 수비 원 위치에 사용
        HackingSystem.Instance.StartHacking(
            objectType,
            OnHackingSuccess,
            OnHackingFailed,
            transform.position
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
    /// 오브젝트 종류별 성공 효과 발동
    ///
    /// [프리팹 파라미터 편집 방법]
    /// 각 HackableObject 프리팹에 해당하는 효과 스크립트를 직접 붙여두면
    /// 인스펙터에서 duration, damage 등 파라미터를 바로 수정할 수 있습니다.
    ///   - 전기울타리 프리팹  → ElectricFenceEffect 컴포넌트 추가
    ///   - 위성통신 프리팹    → SatelliteUplinkEffect 컴포넌트 추가
    ///   - 시냅스서버 프리팹  → SynapseServerEffect 컴포넌트 추가
    ///   - 마그네틱비컨 프리팹→ MagneticBeaconEffect 컴포넌트 추가
    ///   - 보안터렛 프리팹    → SecurityTurretEffect 컴포넌트 추가
    ///
    /// 컴포넌트가 없으면 기본값으로 자동 추가됩니다(하위 호환).
    ///
    /// GameObject.SetActive(false) 대신 시각 컴포넌트만 끄기 때문에
    /// 효과 코루틴이 같은 GameObject 위에서 계속 실행됩니다.
    /// </summary>
    private void ActivateEffect()
    {
        switch (objectType)
        {
            case HackableObjectType.SecurityTurret:
            {
                Debug.Log("[Effect] 보안 터렛 활성화!");
                var turret = GetComponent<SecurityTurretEffect>()
                             ?? gameObject.AddComponent<SecurityTurretEffect>();
                turret.Activate();
                break;
            }

            case HackableObjectType.ElectricFence:
            {
                Debug.Log("[Effect] 전기 울타리 생성!");
                var fence = GetComponent<ElectricFenceEffect>()
                            ?? gameObject.AddComponent<ElectricFenceEffect>();
                fence.Activate();
                break;
            }

            case HackableObjectType.SatelliteUplink:
            {
                Debug.Log("[Effect] 새틀라이트 레이저 발동!");
                var satellite = GetComponent<SatelliteUplinkEffect>()
                                ?? gameObject.AddComponent<SatelliteUplinkEffect>();
                satellite.Activate();
                break;
            }

            case HackableObjectType.SynapseServer:
            {
                Debug.Log("[Effect] 적 절반 아군화!");
                var synapse = GetComponent<SynapseServerEffect>()
                              ?? gameObject.AddComponent<SynapseServerEffect>();
                synapse.Activate();
                break;
            }

            case HackableObjectType.MagneticBeacon:
            {
                Debug.Log("[Effect] 마그네틱 비컨 발동 - 적 견인!");
                var beacon = GetComponent<MagneticBeaconEffect>()
                             ?? gameObject.AddComponent<MagneticBeaconEffect>();
                beacon.Activate();
                break;
            }

            default:
                return;
        }

        // SetActive(false) 대신 시각/물리 컴포넌트만 비활성화
        // → 효과 코루틴이 같은 GameObject에서 계속 실행됨
        HideVisuals();
    }

    /// <summary>
    /// 해킹 완료 후 오브젝트의 시각적 요소와 상호작용을 비활성화합니다.
    /// GameObject 자체는 살려두어 효과 코루틴이 정상 동작하도록 합니다.
    /// </summary>
    private void HideVisuals()
    {
        // 스프라이트 숨김
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        // 콜라이더 비활성화 (재해킹 방지)
        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;

        // HackableObject 스크립트 자체 비활성화 (Update 중단)
        this.enabled = false;
    }

    // 에디터에서 범위 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hackRange);
    }
}
