using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Characters;

/// <summary>
/// 해킹 시스템 관리자 (미니게임 전용)
/// - 미니게임 랜덤 선택/실행
/// - 성공/실패 판정
/// - 보상(오브젝트 효과)은 절대 여기서 처리하지 않음
/// </summary>
public class HackingSystem : MonoBehaviour
{
    public static HackingSystem Instance { get; private set; }

    // ✅ (추가) 미니게임 결과 이벤트 (오브젝트 보상과 분리)
    public static event System.Action<HackableObject> OnHackSuccess;
    public static event System.Action<HackableObject> OnHackFail;

    [Header("해킹 설정")]
    [SerializeField]
    [Tooltip("게이지 자동 충전 속도 (초당 %)")]
    private float gaugeChargeSpeed = 20f;

    [SerializeField]
    [Tooltip("사이보그 보안 영역 반지름")]
    private float securityFieldRadius = 3f;

    [Header("미니게임 설정")]
    [SerializeField] private bool enableCommandBypass = true;
    [SerializeField] private bool enableNumberSequence = true;
    [SerializeField] private bool enableNetworkBridge = true;
    [SerializeField] private bool enableSynapseSync = true;
    [SerializeField] private bool enableFrequencyOverride = true;

    [Header("미니게임 UI")]
    [SerializeField]
    private GameObject hackingUIPanel;

    private Player currentPlayer = null;
    private HackableObject currentHackableObject = null;
    private bool isHacking = false;
    private float hackingGauge = 0f;
    private float maxGauge = 100f;

    private HackingMinigameBase currentMinigame = null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 해킹 시작
    /// </summary>
    public void StartHacking(HackableObject hackableObject, Player player)
    {
        if (isHacking) return;

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

        // 미니게임 랜덤 선택 (인스펙터에서 활성화된 것만)
        List<HackingMinigameType> availableMinigames = new List<HackingMinigameType>();

        if (enableCommandBypass) availableMinigames.Add(HackingMinigameType.CommandBypass);
        if (enableNumberSequence) availableMinigames.Add(HackingMinigameType.NumberSequence);
        if (enableNetworkBridge) availableMinigames.Add(HackingMinigameType.NetworkBridge);
        if (enableSynapseSync) availableMinigames.Add(HackingMinigameType.SynapseSync);
        if (enableFrequencyOverride) availableMinigames.Add(HackingMinigameType.FrequencyOverride);

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
                if (currentMinigame is CommandBypassMinigame cmdBypass && hackingUIPanel != null)
                {
                    Transform nsRoot = hackingUIPanel.transform.Find("NumberSequence_Root");
                    if (nsRoot != null) nsRoot.gameObject.SetActive(false);

                    Transform cbRoot = hackingUIPanel.transform.Find("CommandBypass_Root");
                    if (cbRoot != null)
                    {
                        cbRoot.gameObject.SetActive(true);
                        Transform arrowContainer = cbRoot.Find("ArrowContainer");
                        Transform statusTrans = cbRoot.Find("StatusText");
                        Transform progressTrans = cbRoot.Find("ProgressGauge");
                        var statusText = statusTrans != null ? statusTrans.GetComponent<TMPro.TextMeshProUGUI>() : null;
                        var progressSlider = progressTrans != null ? progressTrans.GetComponent<UnityEngine.UI.Slider>() : null;
                        UnityEngine.UI.Image progressImage = null;
                        if (progressTrans != null)
                        {
                            var img = progressTrans.GetComponent<UnityEngine.UI.Image>();
                            if (img != null && img.type == UnityEngine.UI.Image.Type.Filled)
                                progressImage = img;
                            else
                            {
                                foreach (var c in progressTrans.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                                    if (c.type == UnityEngine.UI.Image.Type.Filled) { progressImage = c; break; }
                            }
                        }
                        if (arrowContainer != null)
                        {
                            cmdBypass.SetPreMadeUI(arrowContainer, statusText, progressSlider, progressImage);
                            Debug.Log("[HackingSystem] 손으로 만든 CommandBypass UI 사용");
                        }
                    }
                }
                break;

            case HackingMinigameType.NumberSequence:
                currentMinigame = minigameObj.AddComponent<NumberSequenceMinigame>();
                if (currentMinigame is NumberSequenceMinigame numberSeq && hackingUIPanel != null)
                {
                    // 손으로 만든 NumberSequence_Root 사용 (있으면)
                    Transform nsRoot = hackingUIPanel.transform.Find("NumberSequence_Root");
                    Transform cbRoot = hackingUIPanel.transform.Find("CommandBypass_Root");
                    if (cbRoot != null) cbRoot.gameObject.SetActive(false);
                    if (nsRoot != null)
                    {
                        nsRoot.gameObject.SetActive(true);
                        Transform btnGrid = nsRoot.Find("BtnGrid");
                        Transform statusTrans = nsRoot.Find("StatusText");
                        var statusText = statusTrans != null ? statusTrans.GetComponent<TMPro.TextMeshProUGUI>() : null;
                        if (btnGrid != null && btnGrid.childCount >= 9)
                        {
                            numberSeq.SetPreMadeUI(btnGrid, statusText);
                            Debug.Log("[HackingSystem] 손으로 만든 NumberSequence UI 사용");
                        }
                        else
                        {
                            numberSeq.SetButtonParent(hackingUIPanel.transform);
                        }
                    }
                    else
                    {
                        numberSeq.SetButtonParent(hackingUIPanel.transform);
                    }
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

            if (hackingGauge >= maxGauge)
            {
                OnGaugeFull();
            }

            yield return null;
        }
    }

    private void OnGaugeFull()
    {
        if (currentMinigame != null)
        {
            currentMinigame.OnSuccess();
        }
    }

    private void OnMinigameSuccess()
    {
        Debug.Log("해킹 성공!");

        // ✅ 보상 처리 없음. 결과만 알림.
        OnHackSuccess?.Invoke(currentHackableObject);

        EndHacking(true);
    }

    private void OnMinigameFailure()
    {
        Debug.Log("해킹 실패!");

        ApplyFailurePenalty();

        // ✅ 보상 처리 없음. 결과만 알림.
        OnHackFail?.Invoke(currentHackableObject);

        EndHacking(false);
    }

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

        // UI 패널 비활성화 (미니게임 루트도 같이 꺼짐)
        if (hackingUIPanel != null)
        {
            Transform nsRoot = hackingUIPanel.transform.Find("NumberSequence_Root");
            if (nsRoot != null) nsRoot.gameObject.SetActive(false);
            Transform cbRoot = hackingUIPanel.transform.Find("CommandBypass_Root");
            if (cbRoot != null) cbRoot.gameObject.SetActive(false);
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

        if (GameManager.Instance != null)
        {
            CharacterType characterType = GameManager.Instance.GetSelectedCharacter();
            if (characterType != CharacterType.Cyborg) return;
        }

        Collider2D[] enemies = Physics2D.OverlapCircleAll(
            currentPlayer.transform.position,
            securityFieldRadius
        );

        foreach (var col in enemies)
        {
            if (col.CompareTag("Enemy"))
            {
                hackingGauge = Mathf.Max(0f, hackingGauge - 10f * Time.deltaTime);

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
    /// 해킹 UI 패널 자동 생성 (없을 경우). 전체 화면에 보이도록 생성합니다.
    /// </summary>
    private void CreateHackingUIPanel()
    {
        // 씬의 메인 Canvas 사용 (없으면 새로 생성)
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HackingCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            canvas.sortingOrder = 100;
            Debug.Log("[HackingSystem] Canvas 자동 생성");
        }

        // 해킹 UI 패널 = 전체 화면 덮기 (화면에 확실히 보이도록)
        GameObject panel = new GameObject("HackingUIPanel");
        panel.transform.SetParent(canvas.transform, false);
        panel.transform.SetAsLastSibling(); // 다른 UI보다 위에 그리기

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        UnityEngine.UI.Image panelImg = panel.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.96f);

        hackingUIPanel = panel;
        Debug.Log("[HackingSystem] 해킹 UI 패널 생성 완료 (전체 화면)");
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
    CommandBypass,
    NumberSequence,
    NetworkBridge,
    SynapseSync,
    FrequencyOverride
}
