using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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
    [SerializeField] private float spawnRadius = 3f;        // 플레이어 주변 스폰 반경

    [Header("해킹 상호작용 프롬프트 (E 버튼)")]
    [SerializeField] private GameObject hackInteractPromptPrefab;
    [SerializeField, Min(0.01f), Tooltip("E 버튼 월드 스케일 (픽셀 아트: 1 전후)")]
    private float promptWorldScale = 1f;
    [SerializeField] private Vector2 promptWorldOffset = new Vector2(0.1f, 0.1f);
    [SerializeField] private int promptSortingOrderOffset = 5;
    [SerializeField] private float promptDepthOffset = -0.1f;

    public Vector2 PromptWorldOffset => promptWorldOffset;
    public float PromptDepthOffset => promptDepthOffset;
    public float PromptWorldScale => promptWorldScale;

    [Header("사이보그 해킹 설정")]
    [SerializeField, Range(0.5f, 15f), Tooltip("수비 원 반경 (월드 유닛). 적 감지·바닥 원 크기 공통")]
    private float cyborgZoneRadius = 2.5f;
    [SerializeField, Range(1f, 30f), Tooltip("수비 제한 시간 (초)")]
    private float cyborgTotalTime = 10f;
    [SerializeField, Range(1, 10), Tooltip("원 안 허용 적 수. 이 값 초과 시 즉시 실패")]
    private int cyborgMaxEnemyCount = 3;
    [SerializeField, Range(0.02f, 0.5f), Tooltip("수비 원 테두리 두께")]
    private float cyborgZoneBorderWidth = 0.08f;
    [SerializeField, Range(0f, 1f), Tooltip("바닥 반투명 원 알파")]
    private float cyborgZoneBackgroundAlpha = 0.25f;
    [SerializeField, Range(0f, 1f), Tooltip("시계방향 타이머 채움 알파")]
    private float cyborgZoneTimerFillAlpha = 0.85f;
    [SerializeField, Tooltip("바닥 원 렌더 순서. 맵 타일(-10)보다 높게 (예: -5~-8). -20이면 바닥에 가려져 안 보임")]
    private int cyborgZoneSortingOrder = -5;

    private const string KEY_CYBORG_ZONE_RADIUS = "HackingSystem_CyborgZoneRadius";
    private const string KEY_CYBORG_TOTAL_TIME = "HackingSystem_CyborgTotalTime";
    private const string KEY_CYBORG_MAX_ENEMY = "HackingSystem_CyborgMaxEnemyCount";
    private const string KEY_CYBORG_BORDER_WIDTH = "HackingSystem_CyborgBorderWidth";
    private const string KEY_CYBORG_BG_ALPHA = "HackingSystem_CyborgBackgroundAlpha";
    private const string KEY_CYBORG_FILL_ALPHA = "HackingSystem_CyborgTimerFillAlpha";
    private const string KEY_CYBORG_SORTING_ORDER = "HackingSystem_CyborgSortingOrder";
    private bool cyborgSettingsLoaded;

    // 현재 진행 중인 미니게임
    private HackingMinigameBase currentMinigame;
    private bool isHacking = false;
    private bool pausedTimeForHacking = false;

    // 사이보그 해킹 관련
    private Coroutine cyborgHackingCoroutine; // 사이보그 해킹 코루틴 참조
    private CyborgZoneTimerVisual cyborgZoneVisual; // 바닥 수비 원 + 시계방향 타이머

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
        LoadCyborgSettings();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SaveCyborgSettings();
    }

    private void OnApplicationQuit()
    {
        if (Instance != this)
            return;

        SaveCyborgSettings();
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause || Instance != this)
            return;

        SaveCyborgSettings();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && cyborgSettingsLoaded)
            SaveCyborgSettings();
    }
#endif

    private bool HasSavedCyborgSettings()
    {
        return ES3.KeyExists(KEY_CYBORG_ZONE_RADIUS);
    }

    public void LoadCyborgSettings()
    {
        // ES3에 저장된 적 없으면 씬/빌드에 박힌 인스펙터 값 그대로 사용 (exe 첫 실행 포함)
        if (HasSavedCyborgSettings())
        {
            cyborgZoneRadius = ES3.Load(KEY_CYBORG_ZONE_RADIUS, cyborgZoneRadius);
            cyborgTotalTime = ES3.Load(KEY_CYBORG_TOTAL_TIME, cyborgTotalTime);
            cyborgMaxEnemyCount = ES3.Load(KEY_CYBORG_MAX_ENEMY, cyborgMaxEnemyCount);
            cyborgZoneBorderWidth = ES3.Load(KEY_CYBORG_BORDER_WIDTH, cyborgZoneBorderWidth);
            cyborgZoneBackgroundAlpha = ES3.Load(KEY_CYBORG_BG_ALPHA, cyborgZoneBackgroundAlpha);
            cyborgZoneTimerFillAlpha = ES3.Load(KEY_CYBORG_FILL_ALPHA, cyborgZoneTimerFillAlpha);
            cyborgZoneSortingOrder = ES3.Load(KEY_CYBORG_SORTING_ORDER, cyborgZoneSortingOrder);
        }

        cyborgSettingsLoaded = true;

        Debug.Log(
            $"[HackingSystem] 사이보그 해킹 설정 로드 | " +
            $"fromES3={HasSavedCyborgSettings()}, " +
            $"radius={cyborgZoneRadius:F2}, time={cyborgTotalTime:F0}, " +
            $"maxEnemy={cyborgMaxEnemyCount}, sorting={cyborgZoneSortingOrder}, " +
            $"path={Application.persistentDataPath}"
        );
    }

    public void SaveCyborgSettings()
    {
        ES3.Save(KEY_CYBORG_ZONE_RADIUS, cyborgZoneRadius);
        ES3.Save(KEY_CYBORG_TOTAL_TIME, cyborgTotalTime);
        ES3.Save(KEY_CYBORG_MAX_ENEMY, cyborgMaxEnemyCount);
        ES3.Save(KEY_CYBORG_BORDER_WIDTH, cyborgZoneBorderWidth);
        ES3.Save(KEY_CYBORG_BG_ALPHA, cyborgZoneBackgroundAlpha);
        ES3.Save(KEY_CYBORG_FILL_ALPHA, cyborgZoneTimerFillAlpha);
        ES3.Save(KEY_CYBORG_SORTING_ORDER, cyborgZoneSortingOrder);

        Debug.Log(
            $"[HackingSystem] 사이보그 해킹 설정 저장 | " +
            $"radius={cyborgZoneRadius:F2}, sorting={cyborgZoneSortingOrder}, " +
            $"path={Application.persistentDataPath}"
        );
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

        numberSequenceRoot = hackingUIPanel.transform.Find("NumberSequence_Root");
        commandBypassRoot = hackingUIPanel.transform.Find("CommandBypass_Root");
        synapseSyncRoot = hackingUIPanel.transform.Find("SynapseSync_Root");
        frequencyOverrideRoot = hackingUIPanel.transform.Find("FrequencyOverride_Root");
        networkBridgeRoot = hackingUIPanel.transform.Find("NetworkBridge_Root");

        Debug.Log($"[HackingSystem] 루트 탐색 결과:" +
            $"\n NumberSequence={numberSequenceRoot != null}" +
            $"\n CommandBypass={commandBypassRoot != null}" +
            $"\n SynapseSync={synapseSyncRoot != null}" +
            $"\n FrequencyOverride={frequencyOverrideRoot != null}" +
            $"\n NetworkBridge={networkBridgeRoot != null}");

        // 모든 루트 비활성화 후 패널도 비활성화
        SetAllRootsInactive();
        hackingUIPanel.SetActive(false);
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

        int idx = UnityEngine.Random.Range(0, validPrefabs.Count);
        Vector3 spawnPos = player.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle.normalized * spawnRadius;
        Instantiate(validPrefabs[idx], spawnPos, Quaternion.identity);
    }

    // ─────────────────────────────────────────────
    // 해킹 시작 (HackableObject에서 호출)
    // ─────────────────────────────────────────────
    /// <summary>
    /// 해킹오브젝트 종류에 따라 미니게임 시작
    /// hackPosition: 해킹 오브젝트의 월드 좌표 (사이보그 수비 원 위치에 사용)
    /// </summary>
    public void StartHacking(HackableObjectType objectType, System.Action onSuccess, System.Action onFailure, Vector3 hackPosition = default)
    {
        if (isHacking) return;
        isHacking = true;
        pausedTimeForHacking = false;

        if (IsCurrentPlayerHacker())
        {
            if (hackingUIPanel != null)
                hackingUIPanel.SetActive(true);

            // 해커: 게임 시간 정지 후 미니게임 시작
            pausedTimeForHacking = true;
            Time.timeScale = 0f;
            StartMinigame(objectType, onSuccess, onFailure);
        }
        else
        {
            // 사이보그: 게임 시간 유지, 바닥 수비 원 10초 방어
            StartCyborgHacking(onSuccess, onFailure, hackPosition);
        }
    }

    private bool IsCurrentPlayerHacker()
    {
        Player player = FindObjectOfType<Player>();
        if (player != null)
            return player.CharacterType == NeoSurvive.Characters.CharacterType.Hacker;

        if (GameManager.Instance != null)
            return GameManager.Instance.GetSelectedCharacter() == NeoSurvive.Characters.CharacterType.Hacker;

        return true;
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

        Transform btnGrid = numberSequenceRoot.Find("BtnGrid");
        Slider timeGauge = FindSlider(numberSequenceRoot, "TimeGauge");

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
        Slider progressGauge = FindSlider(commandBypassRoot, "ProgressGauge");
        Slider timeGauge = FindSlider(commandBypassRoot, "TimeGauge");

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
        GameObject outerCircle = FindChild(synapseSyncRoot, "OuterCircle");
        Slider timeGauge = FindSlider(synapseSyncRoot, "TimeGauge");
        Slider successGauge = FindSlider(synapseSyncRoot, "SuccessGauge");

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
        Slider timeGauge = FindSlider(frequencyOverrideRoot, "TimeGauge");
        Slider matchGauge = FindSlider(frequencyOverrideRoot, "MatchGauge");
        Slider holdGauge = FindSlider(frequencyOverrideRoot, "HoldGauge");

        minigame.SetPreMadeUI(targetWave, playerWave, timeGauge, matchGauge, holdGauge);
        minigame.Initialize(onSuccess, onFailure);
    }

    private void StartNetworkBridge(System.Action onSuccess, System.Action onFailure)
    {
        if (networkBridgeRoot == null) { Debug.LogWarning("[HackingSystem] NetworkBridge_Root 없음!"); return; }
        networkBridgeRoot.gameObject.SetActive(true);

        var minigame = GetOrAddMinigame<NetworkBridgeMinigame>(networkBridgeRoot.gameObject);
        currentMinigame = minigame;

        Transform leftContainer = networkBridgeRoot.Find("LeftContainer");
        Transform rightContainer = networkBridgeRoot.Find("RightContainer");
        Transform linesContainer = networkBridgeRoot.Find("LinesContainer");
        Slider timeGauge = FindSlider(networkBridgeRoot, "TimeGauge");
        Slider successGauge = FindSlider(networkBridgeRoot, "SuccessGauge");

        minigame.SetPreMadeUI(leftContainer, rightContainer, linesContainer, timeGauge, successGauge);
        minigame.Initialize(onSuccess, onFailure);
    }

    // ─────────────────────────────────────────────
    // 사이보그 해킹 시스템
    // ─────────────────────────────────────────────

    /// <summary>
    /// 사이보그 해킹 시작: 바닥 수비 원 10초 방어 (미니게임 없음)
    /// </summary>
    private void StartCyborgHacking(System.Action onSuccess, System.Action onFailure, Vector3 zoneCenter)
    {
        var visualSettings = new CyborgZoneTimerVisual.Settings
        {
            radius = cyborgZoneRadius,
            borderWidth = cyborgZoneBorderWidth,
            backgroundAlpha = cyborgZoneBackgroundAlpha,
            timerFillAlpha = cyborgZoneTimerFillAlpha,
            sortingOrder = cyborgZoneSortingOrder,
            sortingLayerName = "Default"
        };

        cyborgZoneVisual = CyborgZoneTimerVisual.Create(zoneCenter, visualSettings);
        cyborgHackingCoroutine = StartCoroutine(CyborgHackingCoroutine(zoneCenter, onSuccess, onFailure));
    }

    /// <summary>
    /// 사이보그 해킹 코루틴
    /// - 10초 동안 원 안 적이 cyborgMaxEnemyCount 이하 유지 시 성공
    /// - 적이 cyborgMaxEnemyCount 초과 시 즉시 실패
    /// - 플레이어 위치는 검사하지 않음
    /// </summary>
    private IEnumerator CyborgHackingCoroutine(Vector3 zoneCenter, System.Action onSuccess, System.Action onFailure)
    {
        float elapsed = 0f;

        while (elapsed < cyborgTotalTime)
        {
            elapsed += Time.deltaTime;

            int enemyCount = CountEnemiesInZone(zoneCenter, cyborgZoneRadius);

            if (enemyCount > cyborgMaxEnemyCount)
            {
                cyborgHackingCoroutine = null;
                EndHacking();
                onFailure?.Invoke();
                yield break;
            }

            if (cyborgZoneVisual != null)
            {
                float remainingNormalized = 1f - (elapsed / cyborgTotalTime);
                cyborgZoneVisual.SetTimeRemaining(remainingNormalized);
                cyborgZoneVisual.SetBorderColor(GetZoneBorderColor(enemyCount));
            }

            yield return null;
        }

        cyborgHackingCoroutine = null;
        EndHacking();
        onSuccess?.Invoke();
    }

    private int CountEnemiesInZone(Vector3 center, float radius)
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(center, radius);
        HashSet<GameObject> uniqueEnemies = new HashSet<GameObject>();

        foreach (Collider2D col in cols)
        {
            if (col == null || !col.CompareTag("Enemy"))
                continue;

            EnemyController enemy =
                col.GetComponent<EnemyController>()
                ?? col.GetComponentInParent<EnemyController>();

            uniqueEnemies.Add(enemy != null ? enemy.gameObject : col.gameObject);
        }

        return uniqueEnemies.Count;
    }

    private Color GetZoneBorderColor(int enemyCount)
    {
        if (enemyCount <= 0)
            return Color.cyan;

        if (enemyCount <= cyborgMaxEnemyCount)
            return new Color(1f, 0.75f, 0.1f);

        return Color.red;
    }

    // ─────────────────────────────────────────────
    // 해킹 종료
    // ─────────────────────────────────────────────
    public void EndHacking()
    {
        isHacking = false;

        // 해커: 게임시간 재개 (사이보그는 시간이 멈추지 않았으므로 해커만 해제)
        if (pausedTimeForHacking)
            Time.timeScale = 1f;

        pausedTimeForHacking = false;

        // 사이보그 해킹 코루틴이 아직 돌고 있으면 강제 중단
        if (cyborgHackingCoroutine != null)
        {
            StopCoroutine(cyborgHackingCoroutine);
            cyborgHackingCoroutine = null;
        }

        // 수비 원 시각화 오브젝트 제거
        if (cyborgZoneVisual != null)
        {
            Destroy(cyborgZoneVisual.gameObject);
            cyborgZoneVisual = null;
        }

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
        if (numberSequenceRoot != null) numberSequenceRoot.gameObject.SetActive(false);
        if (commandBypassRoot != null) commandBypassRoot.gameObject.SetActive(false);
        if (synapseSyncRoot != null) synapseSyncRoot.gameObject.SetActive(false);
        if (frequencyOverrideRoot != null) frequencyOverrideRoot.gameObject.SetActive(false);
        if (networkBridgeRoot != null) networkBridgeRoot.gameObject.SetActive(false);
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

    private TextMeshProUGUI FindTMP(Transform root, string name)
    {
        Transform t = root.Find(name);
        if (t != null)
        {
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp != null) return tmp;
        }
        foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp.gameObject.name == name) return tmp;
        }
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

    public GameObject InstantiateHackInteractPrompt()
    {
        if (hackInteractPromptPrefab == null)
            return null;

        Image image = hackInteractPromptPrefab.GetComponentInChildren<Image>(true);
        TextMeshProUGUI sourceLabel =
            hackInteractPromptPrefab.GetComponentInChildren<TextMeshProUGUI>(true);

        if (image != null && image.sprite != null)
        {
            GameObject spritePrompt = new GameObject("HackInteractPrompt(Runtime)");
            SpriteRenderer spriteRenderer = spritePrompt.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = image.sprite;
            spriteRenderer.color = Color.white;
            spriteRenderer.drawMode = SpriteDrawMode.Simple;
            spritePrompt.transform.localScale = Vector3.one * promptWorldScale;

            if (sourceLabel != null)
            {
                GameObject labelObject = new GameObject("E");
                labelObject.transform.SetParent(spritePrompt.transform, false);
                labelObject.transform.localPosition = Vector3.zero;

                TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
                label.font = sourceLabel.font;
                label.text = string.IsNullOrWhiteSpace(sourceLabel.text) ? "E" : sourceLabel.text;
                label.color = sourceLabel.color;
                label.alignment = TextAlignmentOptions.Center;
                label.enableWordWrapping = false;
                label.overflowMode = TextOverflowModes.Overflow;
                label.fontSize = GetPromptLabelFontSize(
                    image.sprite,
                    sourceLabel,
                    image
                );

                float labelOffsetY = GetPromptLabelOffsetY(image.sprite, sourceLabel, image);
                labelObject.transform.localPosition = new Vector3(0f, labelOffsetY, 0f);
            }

            return spritePrompt;
        }

        GameObject instance = Instantiate(hackInteractPromptPrefab);
        instance.name = "HackInteractPrompt(Runtime)";

        Transform root = instance.transform;
        root.SetParent(null, false);
        root.localScale = Vector3.one;

        Canvas canvas = instance.GetComponentInChildren<Canvas>(true);
        if (canvas != null)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
                scaler.enabled = false;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.localPosition = Vector3.zero;
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one * Mathf.Min(promptWorldScale, 0.05f);
            canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
            canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.anchoredPosition = Vector2.zero;
            canvasRect.sizeDelta = new Vector2(1f, 1f);

            for (int i = 0; i < canvasRect.childCount; i++)
            {
                if (canvasRect.GetChild(i) is not RectTransform childRect)
                    continue;

                childRect.anchorMin = new Vector2(0.5f, 0.5f);
                childRect.anchorMax = new Vector2(0.5f, 0.5f);
                childRect.pivot = new Vector2(0.5f, 0.5f);
                childRect.anchoredPosition = Vector2.zero;
                childRect.localScale = Vector3.one;
            }

            canvas.renderMode = RenderMode.WorldSpace;

            Camera cam = Camera.main;
            if (cam != null)
                canvas.worldCamera = cam;
        }

        return instance;
    }

    public void ApplyPromptSorting(SpriteRenderer promptRenderer, Transform anchor)
    {
        if (promptRenderer == null || anchor == null)
            return;

        ApplyPromptHierarchySorting(promptRenderer.gameObject, anchor);
    }

    public void ApplyPromptHierarchySorting(GameObject promptRoot, Transform anchor)
    {
        if (promptRoot == null || anchor == null)
            return;

        SpriteRenderer baseRenderer = anchor.GetComponent<SpriteRenderer>();
        int sortingLayerId = baseRenderer != null ? baseRenderer.sortingLayerID : 0;
        int baseOrder = baseRenderer != null
            ? baseRenderer.sortingOrder + promptSortingOrderOffset
            : promptSortingOrderOffset + 100;

        SpriteRenderer[] spriteRenderers =
            promptRoot.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (renderer == null)
                continue;

            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = baseOrder;
        }

        MeshRenderer[] meshRenderers =
            promptRoot.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer renderer in meshRenderers)
        {
            if (renderer == null)
                continue;

            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = baseOrder + 1;
        }
    }

    private static float GetPromptLabelFontSize(
        Sprite backgroundSprite,
        TextMeshProUGUI sourceLabel,
        Image sourceImage)
    {
        if (backgroundSprite == null)
            return 3f;

        float spriteHeight = backgroundSprite.bounds.size.y;
        if (sourceLabel == null)
            return spriteHeight * 3.5f;

        float uiButtonHeight = 100f;
        if (sourceImage != null)
        {
            float height = sourceImage.rectTransform.rect.height;
            if (height > 0f)
                uiButtonHeight = height;
        }

        // 프리팹 비율: fontSize(60) / 버튼 높이(100)
        // TMP 월드 텍스트는 UI 포인트와 단위가 달라 보정 배율 적용
        const float tmpWorldSizeCorrection = 6.5f;
        float uiRatio = sourceLabel.fontSize / uiButtonHeight;
        return Mathf.Max(0.5f, spriteHeight * uiRatio * tmpWorldSizeCorrection);
    }

    private static float GetPromptLabelOffsetY(
        Sprite backgroundSprite,
        TextMeshProUGUI sourceLabel,
        Image sourceImage)
    {
        if (backgroundSprite == null || sourceLabel == null || sourceImage == null)
            return 0f;

        float uiButtonHeight = sourceImage.rectTransform.rect.height;
        if (uiButtonHeight <= 0f)
            return 0f;

        float offsetRatio = sourceLabel.rectTransform.anchoredPosition.y / uiButtonHeight;
        return backgroundSprite.bounds.size.y * offsetRatio;
    }

    public void ApplyPromptCanvasSorting(Canvas canvas, Transform anchor)
    {
        if (canvas == null || anchor == null)
            return;

        SpriteRenderer baseRenderer = anchor.GetComponent<SpriteRenderer>();
        if (baseRenderer == null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = promptSortingOrderOffset + 100;
            return;
        }

        canvas.overrideSorting = true;
        canvas.sortingLayerID = baseRenderer.sortingLayerID;
        canvas.sortingOrder = baseRenderer.sortingOrder + promptSortingOrderOffset;
    }

    public bool IsHacking => isHacking;

    /// <summary>에디터 Scene 미리보기·Gizmo용</summary>
    public float CyborgZoneRadius => cyborgZoneRadius;
    public float CyborgTotalTime => cyborgTotalTime;
    public int CyborgMaxEnemyCount => cyborgMaxEnemyCount;
}
