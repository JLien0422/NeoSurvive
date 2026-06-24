using System.Collections;
using UnityEngine;
using NeoSurvive.Characters;
#if UNITY_EDITOR
using UnityEditor;
#endif

// GameManager 클래스는 게임의 전반적인 흐름과 상태를 관리합니다.
public class GameManager : MonoBehaviour
{
  // 싱글톤 인스턴스: 다른 스크립트에서 GameManager에 쉽게 접근할 수 있도록 합니다.
  public static GameManager Instance { get; private set; }

  // 킬 카운트 변경 알림 이벤트
  public static event System.Action<int> OnKillCountChanged;
  public static event System.Action onPlayerSpawned;
  public static event System.Action<int> OnRunGoldChanged;
  public static event System.Action<int> OnTotalGoldChanged;
  // 호환용: 기존 구독 코드가 있을 수 있어 유지
  public static event System.Action onGoldChanged;

  [Header("적 스폰 설정")]
  private int killCount = 0;
  public int KillCount => killCount;
  public int CurrentPhase { get; private set; } = 1;

  [Header("골드 관리")]
  [SerializeField] private int currentRunGold = 0;
  [SerializeField] private int totalGold = 0;
  public int TotalGold => totalGold;
  public int CurrentRunGold => currentRunGold;

#if UNITY_EDITOR
  [Header("디버그 골드")]
  [SerializeField] private bool enableDebugGoldCommitKey = true;
  [SerializeField] private KeyCode debugGoldCommitKey = KeyCode.P;

  [InitializeOnLoadMethod]
  private static void RegisterEditorPlayModeCallback()
  {
    EditorApplication.playModeStateChanged -= HandleEditorPlayModeStateChanged;
    EditorApplication.playModeStateChanged += HandleEditorPlayModeStateChanged;
  }

  private static void HandleEditorPlayModeStateChanged(PlayModeStateChange state)
  {
    if (state == PlayModeStateChange.EnteredEditMode)
    {
      EditorApplication.delayCall += RefreshEditorInspectorGoldFromSave;
    }
  }

  private static void RefreshEditorInspectorGoldFromSave()
  {
    int savedTotalGold = ES3.Load<int>(GOLD_SAVE_KEY, 0);

    foreach (GameManager manager in Resources.FindObjectsOfTypeAll<GameManager>())
    {
      if (manager == null || !manager.gameObject.scene.IsValid()) continue;

      manager.currentRunGold = 0;
      manager.totalGold = savedTotalGold;
      EditorUtility.SetDirty(manager);
    }
  }
#endif

  private Transform playerTransform;
  private const string GOLD_SAVE_KEY = "TotalGold";

  [Header("캐릭터 선택")]
  private CharacterType selectedCharacter = CharacterType.Hacker;

  [Header("플레이어 프리팹")]
  [SerializeField] private GameObject hackerPlayerPrefab;
  [SerializeField] private GameObject cyborgPlayerPrefab;

  private float startTime = 0f;

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }
    else
    {
      Destroy(gameObject);
      return;
    }

    startTime = Time.time; // ***** 추가: async Start() 전에 기준 시간 먼저 잡기
  }

  private void Start()
  {
    LoadTotalGold();
    ApplySavedCharacterSelection();

    startTime = Time.time;

    Debug.Log("[GameManager] 로컬 모드");
    GameObject playerObject = GameObject.FindWithTag("Player");

    playerObject = EnsureSelectedPlayerPrefab(playerObject);

    if (playerObject != null)
    {
      playerTransform = playerObject.transform;

      CameraController cam = FindObjectOfType<CameraController>();
      if (cam != null) cam.SetTarget(playerTransform);

      var mapManager = FindObjectOfType<MapManager>();
      if (mapManager != null) mapManager.SetTarget(playerTransform);
    }
    else
    {
      Debug.LogError("플레이어를 찾을 수 없습니다! 'Player' 태그가 설정되었는지 확인해주세요.");
    }
  }

  private void Update()
  {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    if (enableDebugGoldCommitKey && Input.GetKeyDown(debugGoldCommitKey))
    {
      int committedGold = currentRunGold;
      CommitRunGoldToTotal();
      Debug.Log($"[GameManager] Debug gold commit key pressed. committed={committedGold}, totalGold={totalGold}");
    }
#endif
  }

  private void ApplySavedCharacterSelection()
  {
    var savedClass = PlayerClassSelection.Load();
    selectedCharacter = savedClass == PlayerClassType.Cyborg
      ? CharacterType.Cyborg
      : CharacterType.Hacker;
  }

  private GameObject EnsureSelectedPlayerPrefab(GameObject currentPlayer)
  {
    GameObject targetPrefab = selectedCharacter == CharacterType.Cyborg
      ? cyborgPlayerPrefab
      : hackerPlayerPrefab;

    if (targetPrefab == null)
    {
      Debug.LogError($"[GameManager] {selectedCharacter} 플레이어 프리팹이 비어 있습니다.");
      return currentPlayer;
    }

    if (currentPlayer == null)
    {
      return Instantiate(targetPrefab, Vector3.zero, Quaternion.identity);
    }

    Player currentPlayerComponent = currentPlayer.GetComponent<Player>();
    if (currentPlayerComponent != null && currentPlayerComponent.CharacterType == selectedCharacter)
    {
      return currentPlayer;
    }

    Vector3 spawnPosition = currentPlayer.transform.position;
    Quaternion spawnRotation = currentPlayer.transform.rotation;
    Destroy(currentPlayer);

    return Instantiate(targetPrefab, spawnPosition, spawnRotation);
  }

  public void NotifyPlayerSpawned()
  {
    onPlayerSpawned?.Invoke();
    Debug.Log("[GameManager] 플레이어가 생성되었습니다. onPlayerSpawned 이벤트가 호출되었습니다.");
  }

  public void AddGold(int amount)
  {
    currentRunGold += amount;
    OnRunGoldChanged?.Invoke(currentRunGold);
    onGoldChanged?.Invoke();
    Debug.Log($"골드 {amount} 획득! 이번 판 총 골드: {currentRunGold}");
  }

  public bool TrySpendTotalGold(int amount)
  {
    if (amount <= 0) return true;
    if (totalGold < amount) return false;

    totalGold -= amount;
    SaveTotalGold();
    OnTotalGoldChanged?.Invoke(totalGold);
    return true;
  }

  public void AddTotalGold(int amount)
  {
    if (amount <= 0) return;
    totalGold += amount;
    SaveTotalGold();
    OnTotalGoldChanged?.Invoke(totalGold);
  }

  public void CommitRunGoldToTotal()
  {
    if (currentRunGold != 0)
    {
      totalGold += currentRunGold;
      currentRunGold = 0;
      SaveTotalGold();
      OnTotalGoldChanged?.Invoke(totalGold);
      OnRunGoldChanged?.Invoke(currentRunGold);
      onGoldChanged?.Invoke();
    }
  }

  public void OnPlayerDeath()
  {
    CommitRunGoldToTotal();
    Debug.Log($"이번 판에 얻은 골드가 총 골드에 합산되었습니다. 현재 총 골드: {totalGold}");
  }

  private void SaveTotalGold()
  {
    ES3.Save(GOLD_SAVE_KEY, totalGold);
    Debug.Log($"총 골드 {totalGold}를 저장했습니다.");
  }

  private void LoadTotalGold()
  {
    totalGold = ES3.Load<int>(GOLD_SAVE_KEY, 0);
    OnTotalGoldChanged?.Invoke(totalGold);
    Debug.Log($"총 골드 {totalGold}를 불러왔습니다.");
  }

  public float GetGameTime()
  {
    return Time.time - startTime;
  }

  /// <summary>
  /// 서버 game_time 기준으로 로컬 타이머를 동기화합니다.
  /// </summary>
  public void SyncGameTime(float serverGameTime)
  {
    if (serverGameTime < 0f) serverGameTime = 0f;
    startTime = Time.time - serverGameTime;
  }

  public void AddKill()
  {
    killCount++;
    OnKillCountChanged?.Invoke(killCount);
  }

  public void SetCurrentPhase(int phase)
  {
    CurrentPhase = Mathf.Max(1, phase);
  }

  public void SetSelectedCharacter(CharacterType characterType)
  {
    selectedCharacter = characterType;
    Debug.Log($"GameManager: 캐릭터 선택됨 - {characterType}");
  }

  public CharacterType GetSelectedCharacter()
  {
    return selectedCharacter;
  }

  // ============================================================
  // ***** 추가: 디버그/테스트용 시간 점프 API
  // ============================================================

  /// <summary>
  /// "경과 시간"을 강제로 elapsedSeconds로 맞춥니다.
  /// 예: 180을 넣으면 GetGameTime()이 즉시 180초 근처가 됩니다.
  /// </summary>
  public void DebugSetElapsedTime(float elapsedSeconds) // ***** 추가
  {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    startTime = Time.time - Mathf.Max(0f, elapsedSeconds);
    Debug.Log($"[GameManager] DebugSetElapsedTime => elapsed={elapsedSeconds:F1}s");
#endif
  }

  /// <summary>
  /// 현재 경과 시간에서 addSeconds만큼 앞으로 점프합니다.
  /// </summary>
  public void DebugAddTime(float addSeconds) // ***** 추가
  {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    startTime -= Mathf.Max(0f, addSeconds);
    Debug.Log($"[GameManager] DebugAddTime => +{addSeconds:F1}s");
#endif
  }
}
