using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 해킹 시스템 매니저
/// - 패턴 발동 시 해킹오브젝트 생성
/// - 미니게임 시작/종료 관리
/// - 해커: 게임시간 정지 / 사이보그: 게임시간 유지
/// </summary>
public class HackingSystem : MonoBehaviour
{
    public static HackingSystem Instance { get; private set; }

    [Header("해킹 UI 패널")]
    [SerializeField] private GameObject hackingUIPanel; // HackingUIPanel (Canvas 아래)

    [Header("해킹 오브젝트 프리팹 5종")]
    [SerializeField] private GameObject securityTurretPrefab;    // 보안 터렛
    [SerializeField] private GameObject electricFencePrefab;     // 전기 울타리
    [SerializeField] private GameObject satelliteUplinkPrefab;   // 새틀라이트 통신기
    [SerializeField] private GameObject synapseServerPrefab;     // 시냅스 과부하 서버
    [SerializeField] private GameObject magneticBeaconPrefab;    // 마그네틱 비컨

    [Header("설정")]
    [SerializeField] private float spawnRadius = 3f; // 플레이어 주변 스폰 반경

    // 현재 진행 중인 미니게임
    private HackingMinigameBase currentMinigame;
    private bool isHacking = false;

    // 캐릭터 타입 (해커 vs 사이보그)
    private bool isHacker = true;

    // 각 미니게임 루트 (UI 패널 아래)
    private Transform numberSequenceRoot;
    private Transform commandBypassRoot;
    private Transform synapseSyncRoot;
    private Transform frequencyOverrideRoot;
    private Transform networkBridgeRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (hackingUIPanel == null)
        {
            Debug.LogError("[HackingSystem] hackingUIPanel이 인스펙터에 할당되지 않았습니다!");
            return;
        }

        // 패널을 잠깐 활성화해서 자식을 찾은 뒤 비활성화
        bool wasActive = hackingUIPanel.activeSelf;
        hackingUIPanel.SetActive(true);

        numberSequenceRoot    = hackingUIPanel.transform.Find("NumberSequence_Root");
        commandBypassRoot     = hackingUIPanel.transform.Find("CommandBypass_Root");
        synapseSyncRoot       = hackingUIPanel.transform.Find("SynapseSync_Root");
        frequencyOverrideRoot = hackingUIPanel.transform.Find("FrequencyOverride_Root");
        networkBridgeRoot     = hackingUIPanel.transform.Find("NetworkBridge_Root");

        Debug.Log($"[HackingSystem] 루트 탐색 결과:" +
            $"\n NumberSequence={numberSequenceRoot != null}" +
            $"\n CommandBypass={commandBypassRoot != null}" +
            $"\n SynapseSync={synapseSyncRoot != null}" +
            $"\n FrequencyOverride={frequencyOverrideRoot != null}" +
            $"\n NetworkBridge={networkBridgeRoot != null}");

        // 모든 루트 비활성화 후 패널도 비활성화
        SetAllRootsInactive();
        hackingUIPanel.SetActive(false);

        // 캐릭터 타입 확인
        if (GameManager.Instance != null)
        {
            isHacker = GameManager.Instance.GetSelectedCharacter() == NeoSurvive.Characters.CharacterType.Hacker;
        }
    }

    // ─────────────────────────────────────────────
    // 패턴 발동 시 호출 (GameManager에서 호출)
    // ─────────────────────────────────────────────
    /// <summary>
    /// 포위 패턴 발동 시 랜덤 해킹오브젝트 1개 생성
    /// </summary>
    public void SpawnRandomHackableObject()
    {
        var player = FindObjectOfType<Player>();
        if (player == null) return;

        // 5종 중 랜덤 선택
        GameObject[] prefabs = {
            securityTurretPrefab,
            electricFencePrefab,
            satelliteUplinkPrefab,
            synapseServerPrefab,
            magneticBeaconPrefab
        };

        // null 제거 후 랜덤 선택
        List<GameObject> validPrefabs = new List<GameObject>();
        foreach (var p in prefabs)
            if (p != null) validPrefabs.Add(p);

        if (validPrefabs.Count == 0)
        {
            Debug.LogWarning("[HackingSystem] 해킹오브젝트 프리팹이 하나도 할당되지 않았습니다!");
            return;
        }

        int idx = Random.Range(0, validPrefabs.Count);
        Vector3 spawnPos = player.transform.position + (Vector3)Random.insideUnitCircle.normalized * spawnRadius;
        Instantiate(validPrefabs[idx], spawnPos, Quaternion.identity);
    }

    // ─────────────────────────────────────────────
    // 해킹 시작 (HackableObject에서 호출)
    // ─────────────────────────────────────────────
    /// <summary>
    /// 해킹오브젝트 종류에 따라 미니게임 시작
    /// </summary>
    public void StartHacking(HackableObjectType objectType, System.Action onSuccess, System.Action onFailure)
    {
        if (isHacking) return;
        isHacking = true;

        // 해커: 게임시간 정지
        if (isHacker)
            Time.timeScale = 0f;

        // UI 패널 활성화
        if (hackingUIPanel != null)
            hackingUIPanel.SetActive(true);

        // 오브젝트 종류에 따라 미니게임 선택 및 루트 활성화
        StartMinigame(objectType, onSuccess, onFailure);
    }

    private void StartMinigame(HackableObjectType objectType, System.Action onSuccess, System.Action onFailure)
    {
        // 모든 루트 비활성화
        SetAllRootsInactive();

        System.Action wrappedSuccess = () => { EndHacking(); onSuccess?.Invoke(); };
        System.Action wrappedFailure = () => { EndHacking(); onFailure?.Invoke(); };

        switch (objectType)
        {
            case HackableObjectType.SecurityTurret:
                StartNumberSequence(wrappedSuccess, wrappedFailure);
                break;
            case HackableObjectType.ElectricFence:
                StartCommandBypass(wrappedSuccess, wrappedFailure);
                break;
            case HackableObjectType.SatelliteUplink:
                StartNetworkBridge(wrappedSuccess, wrappedFailure);
                break;
            case HackableObjectType.SynapseServer:
                StartSynapseSync(wrappedSuccess, wrappedFailure);
                break;
            case HackableObjectType.MagneticBeacon:
                StartFrequencyOverride(wrappedSuccess, wrappedFailure);
                break;
        }
    }

    // ─────────────────────────────────────────────
    // 각 미니게임 시작 메서드
    // ─────────────────────────────────────────────
    private void StartNumberSequence(System.Action onSuccess, System.Action onFailure)
    {
        if (numberSequenceRoot == null) { Debug.LogWarning("[HackingSystem] NumberSequence_Root 없음!"); return; }
        numberSequenceRoot.gameObject.SetActive(true);

        var minigame = GetOrAddMinigame<NumberSequenceMinigame>(numberSequenceRoot.gameObject);
        currentMinigame = minigame;

        Transform btnGrid   = numberSequenceRoot.Find("BtnGrid");
        Slider timeGauge    = FindSlider(numberSequenceRoot, "TimeGauge");

        minigame.SetPreMadeUI(btnGrid, timeGauge);
        minigame.Initialize(onSuccess, onFailure);
    }

    private void StartCommandBypass(System.Action onSuccess, System.Action onFailure)
    {
        if (commandBypassRoot == null) { Debug.LogWarning("[HackingSystem] CommandBypass_Root 없음!"); return; }
        commandBypassRoot.gameObject.SetActive(true);

        var minigame = GetOrAddMinigame<CommandBypassMinigame>(commandBypassRoot.gameObject);
        currentMinigame = minigame;

        Transform arrowContainer = commandBypassRoot.Find("ArrowContainer");
        Slider progressGauge     = FindSlider(commandBypassRoot, "ProgressGauge");
        Slider timeGauge         = FindSlider(commandBypassRoot, "TimeGauge");

        minigame.SetPreMadeUI(arrowContainer, progressGauge, null, timeGauge);
        minigame.Initialize(onSuccess, onFailure);
    }

    private void StartSynapseSync(System.Action onSuccess, System.Action onFailure)
    {
        if (synapseSyncRoot == null) { Debug.LogWarning("[HackingSystem] SynapseSync_Root 없음!"); return; }
        synapseSyncRoot.gameObject.SetActive(true);

        var minigame = GetOrAddMinigame<SynapseSyncMinigame>(synapseSyncRoot.gameObject);
        currentMinigame = minigame;

        GameObject centerCircle = FindChild(synapseSyncRoot, "CenterCircle");
        GameObject outerCircle  = FindChild(synapseSyncRoot, "OuterCircle");
        Slider timeGauge        = FindSlider(synapseSyncRoot, "TimeGauge");
        Slider successGauge     = FindSlider(synapseSyncRoot, "SuccessGauge");

        minigame.SetPreMadeUI(centerCircle, outerCircle, timeGauge, successGauge);
        minigame.Initialize(onSuccess, onFailure);
    }

    private void StartFrequencyOverride(System.Action onSuccess, System.Action onFailure)
    {
        if (frequencyOverrideRoot == null) { Debug.LogWarning("[HackingSystem] FrequencyOverride_Root 없음!"); return; }
        frequencyOverrideRoot.gameObject.SetActive(true);

        var minigame = GetOrAddMinigame<FrequencyOverrideMinigame>(frequencyOverrideRoot.gameObject);
        currentMinigame = minigame;

        GameObject targetWave = FindChild(frequencyOverrideRoot, "TargetWave");
        GameObject playerWave = FindChild(frequencyOverrideRoot, "PlayerWave");
        Slider timeGauge      = FindSlider(frequencyOverrideRoot, "TimeGauge");
        Slider matchGauge     = FindSlider(frequencyOverrideRoot, "MatchGauge");
        Slider holdGauge      = FindSlider(frequencyOverrideRoot, "HoldGauge");

        minigame.SetPreMadeUI(targetWave, playerWave, timeGauge, matchGauge, holdGauge);
        minigame.Initialize(onSuccess, onFailure);
    }

    private void StartNetworkBridge(System.Action onSuccess, System.Action onFailure)
    {
        if (networkBridgeRoot == null) { Debug.LogWarning("[HackingSystem] NetworkBridge_Root 없음!"); return; }
        networkBridgeRoot.gameObject.SetActive(true);

        var minigame = GetOrAddMinigame<NetworkBridgeMinigame>(networkBridgeRoot.gameObject);
        currentMinigame = minigame;

        Transform leftContainer  = networkBridgeRoot.Find("LeftContainer");
        Transform rightContainer = networkBridgeRoot.Find("RightContainer");
        Transform linesContainer = networkBridgeRoot.Find("LinesContainer");
        Slider timeGauge         = FindSlider(networkBridgeRoot, "TimeGauge");
        Slider successGauge      = FindSlider(networkBridgeRoot, "SuccessGauge");

        minigame.SetPreMadeUI(leftContainer, rightContainer, linesContainer, timeGauge, successGauge);
        minigame.Initialize(onSuccess, onFailure);
    }

    // ─────────────────────────────────────────────
    // 해킹 종료
    // ─────────────────────────────────────────────
    public void EndHacking()
    {
        isHacking = false;

        // 해커: 게임시간 재개
        if (isHacker)
            Time.timeScale = 1f;

        // 모든 루트 비활성화
        SetAllRootsInactive();

        // UI 패널 비활성화
        if (hackingUIPanel != null)
            hackingUIPanel.SetActive(false);

        currentMinigame = null;
    }

    // ─────────────────────────────────────────────
    // 유틸리티
    // ─────────────────────────────────────────────
    private void SetAllRootsInactive()
    {
        if (numberSequenceRoot != null)   numberSequenceRoot.gameObject.SetActive(false);
        if (commandBypassRoot != null)    commandBypassRoot.gameObject.SetActive(false);
        if (synapseSyncRoot != null)      synapseSyncRoot.gameObject.SetActive(false);
        if (frequencyOverrideRoot != null) frequencyOverrideRoot.gameObject.SetActive(false);
        if (networkBridgeRoot != null)    networkBridgeRoot.gameObject.SetActive(false);
    }

    private T GetOrAddMinigame<T>(GameObject root) where T : HackingMinigameBase
    {
        T minigame = root.GetComponent<T>();
        if (minigame == null)
            minigame = root.AddComponent<T>();
        return minigame;
    }

    private Slider FindSlider(Transform root, string name)
    {
        // 직접 자식에서 먼저 찾기
        Transform t = root.Find(name);
        if (t != null)
        {
            Slider s = t.GetComponent<Slider>();
            if (s != null) return s;
        }

        // 못 찾으면 모든 자손에서 이름으로 검색
        foreach (var slider in root.GetComponentsInChildren<Slider>(true))
        {
            if (slider.gameObject.name == name)
                return slider;
        }

        Debug.LogWarning($"[HackingSystem] '{name}' Slider를 찾을 수 없습니다! ({root.name} 아래 검색)");
        return null;
    }

    private GameObject FindChild(Transform root, string name)
    {
        // 직접 자식에서 먼저 찾기
        Transform t = root.Find(name);
        if (t != null) return t.gameObject;

        // 모든 자손에서 이름으로 검색
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.gameObject.name == name)
                return child.gameObject;
        }

        Debug.LogWarning($"[HackingSystem] '{name}'을 찾을 수 없습니다! ({root.name} 아래 검색)");
        return null;
    }

    public bool IsHacking => isHacking;
}
