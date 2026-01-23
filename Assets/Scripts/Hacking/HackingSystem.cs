using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Characters;

/// <summary>
/// 해킹 시스템 관리자
/// 해킹 미니게임을 관리하고 플레이어 상태를 제어합니다.
/// </summary>
public class HackingSystem : MonoBehaviour
{
    public static HackingSystem Instance { get; private set; }

    [Header("해킹 설정")]
    [SerializeField]
    [Tooltip("게이지 자동 충전 속도 (초당 %)")]
    private float gaugeChargeSpeed = 20f; // 초당 20% 충전

    [SerializeField]
    [Tooltip("사이보그 보안 영역 반지름")]
    private float securityFieldRadius = 3f;

    [Header("미니게임 설정")]
    [SerializeField]
    [Tooltip("사용 가능한 미니게임 목록 (체크된 것만 랜덤 선택됨)")]
    private bool enableCommandBypass = true;
    [SerializeField]
    private bool enableNumberSequence = true;
    [SerializeField]
    private bool enableNetworkBridge = true;
    [SerializeField]
    private bool enableSynapseSync = true;
    [SerializeField]
    private bool enableFrequencyOverride = true;

    [Header("미니게임 UI")]
    [SerializeField]
    private GameObject hackingUIPanel; // 해킹 UI 패널 (정사각형 팝업창)

    private Player currentPlayer = null;
    private HackableObject currentHackableObject = null;
    private bool isHacking = false;
    private float hackingGauge = 0f;
    private float maxGauge = 100f;

    // 현재 진행 중인 미니게임
    private HackingMinigameBase currentMinigame = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 해킹 시작
    /// </summary>
    public void StartHacking(HackableObject hackableObject, Player player)
    {
        if (isHacking) return; // 이미 해킹 중이면 무시

        currentPlayer = player;
        currentHackableObject = hackableObject;
        isHacking = true;
        hackingGauge = 0f;

        // 해커만 해킹 가능
        if (GameManager.Instance != null)
        {
            CharacterType characterType = GameManager.Instance.GetSelectedCharacter();
            if (characterType != CharacterType.Hacker)
            {
                Debug.Log("해커만 해킹할 수 있습니다!");
                EndHacking(false);
                return;
            }
        }

        // 플레이어 고정 상태 (이동/공격 불가)
        SetPlayerLockdown(true);

        // 사이보그 보안 영역 생성 (사이보그 플레이 시)
        // TODO: 사이보그 AI가 보안 영역을 지키도록 구현

        // 미니게임 랜덤 선택 (인스펙터에서 활성화된 것만)
        List<HackingMinigameType> availableMinigames = new List<HackingMinigameType>();
        
        if (enableCommandBypass)
            availableMinigames.Add(HackingMinigameType.CommandBypass);
        if (enableNumberSequence)
            availableMinigames.Add(HackingMinigameType.NumberSequence);
        if (enableNetworkBridge)
            availableMinigames.Add(HackingMinigameType.NetworkBridge);
        if (enableSynapseSync)
            availableMinigames.Add(HackingMinigameType.SynapseSync);
        if (enableFrequencyOverride)
            availableMinigames.Add(HackingMinigameType.FrequencyOverride);
        
        if (availableMinigames.Count == 0)
        {
            Debug.LogError("[HackingSystem] 활성화된 미니게임이 없습니다! 기본값으로 넘버 시퀀스를 사용합니다.");
            availableMinigames.Add(HackingMinigameType.NumberSequence);
        }
        
        HackingMinigameType selectedMinigame = availableMinigames[Random.Range(0, availableMinigames.Count)];
        Debug.Log($"[HackingSystem] 선택된 미니게임: {selectedMinigame}");
        StartMinigame(selectedMinigame);

        // 게이지 자동 충전 시작
        StartCoroutine(GaugeChargeCoroutine());

        Debug.Log("해킹 시작!");
    }

    /// <summary>
    /// 미니게임 시작
    /// </summary>
    private void StartMinigame(HackingMinigameType minigameType)
    {
        // 기존 미니게임 정리
        if (currentMinigame != null)
        {
            Destroy(currentMinigame.gameObject);
        }

        // UI 패널이 없으면 자동 생성
        if (hackingUIPanel == null)
        {
            CreateHackingUIPanel();
            Debug.Log($"[HackingSystem] UI 패널 생성: {hackingUIPanel != null}");
        }

        // UI 패널 활성화
        if (hackingUIPanel != null)
        {
            hackingUIPanel.SetActive(true);
            Debug.Log($"[HackingSystem] UI 패널 활성화 완료");
        }
        else
        {
            Debug.LogError("[HackingSystem] UI 패널이 null입니다!");
        }

        // 미니게임 생성
        GameObject minigameObj = new GameObject("HackingMinigame");
        minigameObj.transform.SetParent(hackingUIPanel != null ? hackingUIPanel.transform : transform);

        switch (minigameType)
        {
            case HackingMinigameType.CommandBypass:
                currentMinigame = minigameObj.AddComponent<CommandBypassMinigame>();
                break;
                
            case HackingMinigameType.NumberSequence:
                currentMinigame = minigameObj.AddComponent<NumberSequenceMinigame>();
                // hackingUIPanel을 buttonParent로 전달 (Initialize 전에 설정)
                if (currentMinigame is NumberSequenceMinigame numberSeq)
                {
                    numberSeq.SetButtonParent(hackingUIPanel != null ? hackingUIPanel.transform : null);
                    Debug.Log($"[HackingSystem] buttonParent 설정: {numberSeq.GetButtonParent() != null}");
                }
                break;
                
            case HackingMinigameType.NetworkBridge:
                currentMinigame = minigameObj.AddComponent<NetworkBridgeMinigame>();
                break;
                
            case HackingMinigameType.SynapseSync:
                currentMinigame = minigameObj.AddComponent<SynapseSyncMinigame>();
                break;
                
            case HackingMinigameType.FrequencyOverride:
                currentMinigame = minigameObj.AddComponent<FrequencyOverrideMinigame>();
                break;
        }

        if (currentMinigame != null)
        {
            currentMinigame.Initialize(OnMinigameSuccess, OnMinigameFailure);
            Debug.Log("[HackingSystem] 미니게임 Initialize 완료");
        }
        else
        {
            Debug.LogError("[HackingSystem] 미니게임 생성 실패!");
        }
    }

    /// <summary>
    /// 게이지 자동 충전 코루틴
    /// </summary>
    private IEnumerator GaugeChargeCoroutine()
    {
        while (isHacking && hackingGauge < maxGauge)
        {
            hackingGauge += gaugeChargeSpeed * Time.deltaTime;
            hackingGauge = Mathf.Clamp(hackingGauge, 0f, maxGauge);

            // 게이지가 가득 차면 즉시 실행
            if (hackingGauge >= maxGauge)
            {
                OnGaugeFull();
            }

            yield return null;
        }
    }

    /// <summary>
    /// 게이지가 가득 찼을 때
    /// </summary>
    private void OnGaugeFull()
    {
        // 미니게임 성공 처리
        if (currentMinigame != null)
        {
            currentMinigame.OnSuccess();
        }
    }

    /// <summary>
    /// 미니게임 성공
    /// </summary>
    private void OnMinigameSuccess()
    {
        Debug.Log("해킹 성공!");
        
        // 성공 보상 적용
        ApplySuccessReward();

        EndHacking(true);
    }

    /// <summary>
    /// 미니게임 실패
    /// </summary>
    private void OnMinigameFailure()
    {
        Debug.Log("해킹 실패!");

        // 실패 패널티 적용
        ApplyFailurePenalty();

        EndHacking(false);
    }

    /// <summary>
    /// 성공 보상 적용
    /// </summary>
    private void ApplySuccessReward()
    {
        // 미니게임 타입에 따라 다른 보상 적용
        if (currentHackableObject == null) return;

        // 현재 미니게임 타입 확인
        HackingMinigameType minigameType = GetCurrentMinigameType();
        
        // HackingRewardSystem에 보상 적용 요청
        if (HackingRewardSystem.Instance != null)
        {
            HackingRewardSystem.Instance.ApplyHackingReward(currentHackableObject, minigameType);
        }
        else
        {
            Debug.LogWarning("[HackingSystem] HackingRewardSystem.Instance가 null입니다! 보상이 적용되지 않습니다.");
        }
    }
    
    /// <summary>
    /// 현재 미니게임 타입 가져오기
    /// </summary>
    private HackingMinigameType GetCurrentMinigameType()
    {
        if (currentMinigame == null) return HackingMinigameType.NumberSequence;
        
        if (currentMinigame is CommandBypassMinigame) return HackingMinigameType.CommandBypass;
        if (currentMinigame is NumberSequenceMinigame) return HackingMinigameType.NumberSequence;
        if (currentMinigame is NetworkBridgeMinigame) return HackingMinigameType.NetworkBridge;
        if (currentMinigame is SynapseSyncMinigame) return HackingMinigameType.SynapseSync;
        if (currentMinigame is FrequencyOverrideMinigame) return HackingMinigameType.FrequencyOverride;
        
        return HackingMinigameType.NumberSequence;
    }

    /// <summary>
    /// 실패 패널티 적용
    /// </summary>
    private void ApplyFailurePenalty()
    {
        if (currentPlayer == null) return;

        // 체력 30% 감소
        float healthLoss = currentPlayer.CurrentHealth * 0.3f;
        currentPlayer.TakeDamage(healthLoss);

        // 사이코잠식도 50% 증가
        currentPlayer.AddPsychoCorruption(50f);

        Debug.Log("해킹 실패 패널티: 체력 -30%, 사이코잠식도 +50%");
    }

    /// <summary>
    /// 해킹 종료
    /// </summary>
    private void EndHacking(bool success)
    {
        isHacking = false;
        hackingGauge = 0f;

        // 플레이어 고정 해제
        SetPlayerLockdown(false);

        // 미니게임 정리
        if (currentMinigame != null)
        {
            Destroy(currentMinigame.gameObject);
            currentMinigame = null;
        }

        // UI 패널 비활성화
        if (hackingUIPanel != null)
        {
            hackingUIPanel.SetActive(false);
        }

        // 해킹 가능 오브젝트에 종료 알림
        if (currentHackableObject != null)
        {
            currentHackableObject.OnHackingEnded();
        }

        currentPlayer = null;
        currentHackableObject = null;
    }

    /// <summary>
    /// 플레이어 고정 상태 설정
    /// </summary>
    private void SetPlayerLockdown(bool locked)
    {
        if (currentPlayer == null) return;

        PlayerController controller = currentPlayer.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.SetLockdown(locked);
        }

        // 무기 공격도 비활성화
        NeoSurvive.Weapon.WeaponManager weaponManager = currentPlayer.GetComponent<NeoSurvive.Weapon.WeaponManager>();
        if (weaponManager != null)
        {
            weaponManager.enabled = !locked;
        }
    }

    /// <summary>
    /// 사이보그 보안 영역 체크 (적이 들어오면 진척도 감소)
    /// </summary>
    private void CheckSecurityField()
    {
        if (currentPlayer == null) return;

        // 사이보그만 보안 영역이 있음
        if (GameManager.Instance != null)
        {
            CharacterType characterType = GameManager.Instance.GetSelectedCharacter();
            if (characterType != CharacterType.Cyborg) return;
        }

        // 보안 영역 내 적 감지
        Collider2D[] enemies = Physics2D.OverlapCircleAll(
            currentPlayer.transform.position,
            securityFieldRadius
        );

        foreach (var col in enemies)
        {
            if (col.CompareTag("Enemy"))
            {
                // 진척도 감소 또는 해킹 취소
                hackingGauge = Mathf.Max(0f, hackingGauge - 10f * Time.deltaTime);
                
                // 게이지가 0이 되면 해킹 취소
                if (hackingGauge <= 0f)
                {
                    OnMinigameFailure();
                }
            }
        }
    }

    private void Update()
    {
        if (isHacking)
        {
            CheckSecurityField();
        }
    }

    /// <summary>
    /// 해킹 UI 패널 자동 생성 (없을 경우)
    /// </summary>
    private void CreateHackingUIPanel()
    {
        // Canvas 찾기 또는 생성
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HackingCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log("[HackingSystem] Canvas 자동 생성");
        }
        else
        {
            Debug.Log($"[HackingSystem] 기존 Canvas 사용: {canvas.name}");
        }

        // 해킹 UI 패널 생성
        GameObject panel = new GameObject("HackingUIPanel");
        panel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(500, 500);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        UnityEngine.UI.Image panelImg = panel.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f); // 조금 더 밝게

        // Canvas의 Sort Order를 높여서 다른 UI 위에 표시
        if (canvas != null)
        {
            canvas.sortingOrder = 100; // 높은 값으로 설정
        }

        hackingUIPanel = panel;
        Debug.Log($"[HackingSystem] 해킹 UI 패널 생성 완료: {panel.name}, 위치: {panelRect.anchoredPosition}, 크기: {panelRect.sizeDelta}");
    }

    private void OnDrawGizmos()
    {
        if (isHacking && currentPlayer != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(currentPlayer.transform.position, securityFieldRadius);
        }
    }
}

/// <summary>
/// 해킹 미니게임 타입
/// </summary>
public enum HackingMinigameType
{
    CommandBypass,      // 커맨드 바이패스
    NumberSequence,     // 넘버 시퀀스
    NetworkBridge,      // 네트워크 브릿지
    SynapseSync,        // 시냅스 동기화
    FrequencyOverride   // 주파수 오버라이드
}
