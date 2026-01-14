# 클라이언트-서버 연동 가이드

## Unity 클라이언트에서 서버 API 사용하기

### 1. GameManager 수정

현재 `GameManager.cs`를 서버와 연동하도록 수정해야 합니다.

#### Before (현재 - 로컬 저장)
```csharp
public class GameManager : MonoBehaviour
{
    private int currentRunGold = 0;
    private int totalGold = 0;
    private const string GOLD_SAVE_KEY = "TotalGold";
    
    private void LoadGold()
    {
        totalGold = PlayerPrefs.GetInt(GOLD_SAVE_KEY, 0);
    }
    
    private void SaveGold()
    {
        PlayerPrefs.SetInt(GOLD_SAVE_KEY, totalGold);
        PlayerPrefs.Save();
    }
}
```

#### After (서버 연동)
```csharp
using NeoSurvive.Network;

public class GameManager : MonoBehaviour
{
    private GameServerAPI serverAPI;
    private int currentSessionId = -1;
    private int currentRunGold = 0;
    private int currentRunKills = 0;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 서버 API 초기화
            serverAPI = gameObject.AddComponent<GameServerAPI>();
            serverAPI.OnLoginSuccess += OnLoginSuccess;
        }
    }
    
    private void OnLoginSuccess(LoginResponse response)
    {
        Debug.Log($"로그인 성공! Level: {response.level}, Gold: {response.gold}");
        // UI 업데이트
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateGoldDisplay(response.gold);
        }
    }
    
    // 게임 시작 시 호출
    public void StartNewGame()
    {
        startTime = Time.time;
        currentRunGold = 0;
        currentRunKills = 0;
        
        // 서버에 게임 시작 알림
        StartCoroutine(serverAPI.StartGame(1));
    }
    
    // 플레이어 사망 시 호출
    public void OnPlayerDeath()
    {
        float survivalTime = Time.time - startTime;
        
        // 서버에 게임 종료 알림
        StartCoroutine(EndGameOnServer(survivalTime));
    }
    
    private IEnumerator EndGameOnServer(float survivalTime)
    {
        yield return serverAPI.EndGame(
            survivalTime: Mathf.RoundToInt(survivalTime),
            enemiesKilled: currentRunKills,
            isCleared: false
        );
        
        // 서버 응답 후 UI 업데이트는 GameServerAPI의 OnGameEnded 이벤트에서 처리
    }
}
```

---

### 2. GameServerAPI 개선

현재 API를 게임과 더 잘 연동하기 위한 개선사항:

#### 캐릭터 타입 추가
```csharp
public IEnumerator StartGame(int stage = 1)
{
    if (playerId < 0)
    {
        Debug.LogError("로그인이 필요합니다!");
        yield break;
    }

    // ✅ 캐릭터 타입 추가
    CharacterType selectedCharacter = CharacterType.Hacker;
    if (GameManager.Instance != null)
    {
        selectedCharacter = GameManager.Instance.GetSelectedCharacter();
    }

    var startData = new GameStartRequest 
    { 
        playerId = playerId, 
        stage = stage,
        characterType = selectedCharacter.ToString()  // ← 추가
    };
    
    string json = JsonConvert.SerializeObject(startData);

    using (UnityWebRequest request = CreatePostRequest("/game/start", json))
    {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonConvert.DeserializeObject<GameStartResponse>(request.downloadHandler.text);
            currentSessionId = response.sessionId;
            Debug.Log($"🎮 게임 시작! SessionId: {currentSessionId}, Character: {selectedCharacter}");
        }
        else
        {
            Debug.LogError($"게임 시작 실패: {request.error}");
        }
    }
}
```

#### DTO 수정
```csharp
[Serializable]
public class GameStartRequest
{
    public int playerId;
    public int stage;
    public string characterType;  // ← 추가: "Hacker" or "Cyborg"
}

[Serializable]
public class GameEndRequest
{
    public int sessionId;
    public int survivalTime;
    public int enemiesKilled;
    public bool isCleared;
    // 골드는 서버에서 계산하므로 제거하는 것이 안전
}
```

---

### 3. UI 업데이트

#### 게임 종료 후 결과 표시
```csharp
public class ResultScreen : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI goldEarnedText;
    public TextMeshProUGUI expEarnedText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI newRecordText;
    
    private void Start()
    {
        // GameServerAPI의 OnGameEnded 이벤트 구독
        var serverAPI = FindObjectOfType<GameServerAPI>();
        if (serverAPI != null)
        {
            serverAPI.OnGameEnded += ShowResults;
        }
    }
    
    private void ShowResults(GameEndResponse response)
    {
        goldEarnedText.text = $"+{response.goldEarned} 골드";
        expEarnedText.text = $"+{response.experienceEarned} 경험치";
        
        if (response.leveledUp)
        {
            levelText.text = $"레벨 업! Lv.{response.currentLevel}";
            levelText.gameObject.SetActive(true);
        }
        
        if (response.newRecord)
        {
            newRecordText.text = "🎊 신기록!";
            newRecordText.gameObject.SetActive(true);
        }
        
        gameObject.SetActive(true);
    }
}
```

---

### 4. 업그레이드 시스템 연동

#### UpgradeManager 수정
```csharp
public class UpgradeManager : MonoBehaviour
{
    private GameServerAPI serverAPI;
    
    private void Start()
    {
        serverAPI = FindObjectOfType<GameServerAPI>();
        
        // 서버에서 업그레이드 목록 가져오기
        StartCoroutine(LoadUpgradesFromServer());
    }
    
    private IEnumerator LoadUpgradesFromServer()
    {
        // TODO: 서버 API에 업그레이드 목록 조회 엔드포인트 추가 필요
        // GET /upgrade/player/{playerId}/list
        
        yield return null; // 임시
    }
    
    public void TryBuyUpgrade(UpgradeDef def)
    {
        int currentLevel = DataManager.Instance.GetUpgradeLevel(def.id);
        if (def.maxLevel > 0 && currentLevel >= def.maxLevel) return;

        int cost = CalculateCost(def, currentLevel);
        
        // 서버에 구매 요청
        StartCoroutine(PurchaseUpgradeOnServer(def, cost));
    }
    
    private IEnumerator PurchaseUpgradeOnServer(UpgradeDef def, int cost)
    {
        // TODO: 서버 API 연동
        // POST /upgrade/player/{playerId}/purchase
        
        var upgradeData = new 
        { 
            upgradeId = def.id 
        };
        
        string json = JsonConvert.SerializeObject(upgradeData);
        
        // 임시로 로컬 처리
        if (DataManager.Instance.SpendGold(cost))
        {
            DataManager.Instance.LevelUpUpgrade(def.id);
            RefreshUpgradeList();
        }
        
        yield return null;
    }
}
```

---

### 5. 리더보드 UI

#### LeaderboardPanel.cs
```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using NeoSurvive.Network;

public class LeaderboardPanel : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentRoot;
    public GameObject leaderboardEntryPrefab;
    public TextMeshProUGUI myRankText;
    
    private GameServerAPI serverAPI;
    
    private void Start()
    {
        serverAPI = FindObjectOfType<GameServerAPI>();
    }
    
    public void ShowLeaderboard()
    {
        gameObject.SetActive(true);
        StartCoroutine(LoadLeaderboard());
    }
    
    private IEnumerator LoadLeaderboard()
    {
        // 기존 항목 제거
        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }
        
        // 서버에서 리더보드 가져오기
        List<LeaderboardEntry> leaderboard = null;
        
        yield return serverAPI.GetLeaderboard(
            top: 50, 
            onSuccess: (data) => { leaderboard = data; }
        );
        
        if (leaderboard != null)
        {
            foreach (var entry in leaderboard)
            {
                CreateLeaderboardEntry(entry);
            }
        }
    }
    
    private void CreateLeaderboardEntry(LeaderboardEntry entry)
    {
        GameObject entryObj = Instantiate(leaderboardEntryPrefab, contentRoot);
        
        var rankText = entryObj.transform.Find("RankText").GetComponent<TextMeshProUGUI>();
        var nicknameText = entryObj.transform.Find("NicknameText").GetComponent<TextMeshProUGUI>();
        var timeText = entryObj.transform.Find("TimeText").GetComponent<TextMeshProUGUI>();
        var levelText = entryObj.transform.Find("LevelText").GetComponent<TextMeshProUGUI>();
        
        rankText.text = $"#{entry.rank}";
        nicknameText.text = entry.nickname;
        timeText.text = FormatTime(entry.bestSurvivalTime);
        levelText.text = $"Lv.{entry.level}";
        
        // 내 순위 강조
        if (serverAPI != null && entry.playerId == serverAPI.PlayerId)
        {
            entryObj.GetComponent<Image>().color = new Color(1f, 1f, 0.5f, 0.3f);
        }
    }
    
    private string FormatTime(int seconds)
    {
        int minutes = seconds / 60;
        int secs = seconds % 60;
        return $"{minutes:00}:{secs:00}";
    }
}
```

---

### 6. 로딩 & 에러 처리

#### LoadingIndicator.cs
```csharp
public class LoadingIndicator : MonoBehaviour
{
    public static LoadingIndicator Instance { get; private set; }
    
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI messageText;
    
    private int activeRequests = 0;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void Show(string message = "로딩 중...")
    {
        activeRequests++;
        messageText.text = message;
        loadingPanel.SetActive(true);
    }
    
    public void Hide()
    {
        activeRequests--;
        if (activeRequests <= 0)
        {
            activeRequests = 0;
            loadingPanel.SetActive(false);
        }
    }
}
```

#### GameServerAPI에 로딩 표시 추가
```csharp
public IEnumerator Login()
{
    LoadingIndicator.Instance?.Show("로그인 중...");
    
    var loginData = new LoginRequest { deviceUID = deviceUID };
    string json = JsonConvert.SerializeObject(loginData);

    using (UnityWebRequest request = CreatePostRequest("/auth/login", json))
    {
        yield return request.SendWebRequest();

        LoadingIndicator.Instance?.Hide();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // ... 기존 코드
        }
        else
        {
            Debug.LogError($"❌ 로그인 실패: {request.error}");
            ShowErrorPopup("로그인에 실패했습니다. 네트워크 연결을 확인해주세요.");
            OnLoginFailed?.Invoke(request.error);
        }
    }
}

private void ShowErrorPopup(string message)
{
    // TODO: ErrorPopup UI 구현
    Debug.LogError(message);
}
```

---

### 7. 오프라인 모드 (선택사항)

네트워크가 끊겼을 때를 대비한 로컬 저장:

```csharp
public class OfflineDataManager : MonoBehaviour
{
    private const string OFFLINE_GOLD_KEY = "OfflineGold";
    private const string OFFLINE_EXP_KEY = "OfflineExp";
    
    public void SaveOfflineProgress(int gold, int exp)
    {
        int currentOfflineGold = PlayerPrefs.GetInt(OFFLINE_GOLD_KEY, 0);
        int currentOfflineExp = PlayerPrefs.GetInt(OFFLINE_EXP_KEY, 0);
        
        PlayerPrefs.SetInt(OFFLINE_GOLD_KEY, currentOfflineGold + gold);
        PlayerPrefs.SetInt(OFFLINE_EXP_KEY, currentOfflineExp + exp);
        PlayerPrefs.Save();
    }
    
    public IEnumerator SyncOfflineProgress(GameServerAPI serverAPI)
    {
        int offlineGold = PlayerPrefs.GetInt(OFFLINE_GOLD_KEY, 0);
        int offlineExp = PlayerPrefs.GetInt(OFFLINE_EXP_KEY, 0);
        
        if (offlineGold > 0 || offlineExp > 0)
        {
            Debug.Log($"오프라인 진행도 동기화: Gold {offlineGold}, Exp {offlineExp}");
            
            // TODO: 서버 API에 오프라인 진행도 동기화 엔드포인트 추가
            // POST /player/{id}/sync-offline
            
            // 성공 시 로컬 데이터 제거
            PlayerPrefs.DeleteKey(OFFLINE_GOLD_KEY);
            PlayerPrefs.DeleteKey(OFFLINE_EXP_KEY);
            PlayerPrefs.Save();
        }
        
        yield return null;
    }
}
```

---

### 8. 테스트 모드

#### 서버 URL 전환
```csharp
public class GameServerAPI : MonoBehaviour
{
    [Header("서버 설정")]
    [SerializeField] private ServerEnvironment environment = ServerEnvironment.Development;
    [SerializeField] private string productionUrl = "https://api.neosurvive.com/api";
    [SerializeField] private string developmentUrl = "http://localhost:5157/api";
    [SerializeField] private string stagingUrl = "https://staging.neosurvive.com/api";
    
    private string serverUrl;
    
    private void Awake()
    {
        serverUrl = environment switch
        {
            ServerEnvironment.Production => productionUrl,
            ServerEnvironment.Staging => stagingUrl,
            ServerEnvironment.Development => developmentUrl,
            _ => developmentUrl
        };
        
        Debug.Log($"Server Environment: {environment} ({serverUrl})");
        
        deviceUID = GetOrCreateDeviceUID();
    }
}

public enum ServerEnvironment
{
    Production,
    Staging,
    Development
}
```

---

## 통합 예시: 전체 게임 플로우

```csharp
// 1. 게임 시작
void Start()
{
    // 자동 로그인
    StartCoroutine(serverAPI.Login());
}

// 2. 로그인 성공 → 캐릭터 선택
void OnLoginSuccess(LoginResponse response)
{
    ShowCharacterSelection();
}

// 3. 캐릭터 선택 → 게임 시작
void OnCharacterSelected(CharacterType type)
{
    GameManager.Instance.SetSelectedCharacter(type);
    StartCoroutine(serverAPI.StartGame(stage: 1));
}

// 4. 게임 플레이
void Update()
{
    // 게임 로직 (로컬 실행)
    // - 적 스폰
    // - 전투
    // - 골드/경험치 획득 (로컬 카운트)
}

// 5. 게임 종료
void OnPlayerDeath()
{
    int survivalTime = Mathf.RoundToInt(GameManager.Instance.GetGameTime());
    int kills = GameManager.Instance.KillCount;
    
    StartCoroutine(serverAPI.EndGame(survivalTime, kills, isCleared: false));
}

// 6. 결과 화면 표시
void OnGameEnded(GameEndResponse response)
{
    ShowResultScreen(response);
}

// 7. 리더보드 확인
void ShowLeaderboard()
{
    StartCoroutine(serverAPI.GetLeaderboard(top: 50));
}

// 8. 업그레이드
void OnUpgradeButtonClicked(UpgradeDef upgrade)
{
    // 로컬에서 비용 확인 후 서버 요청
    StartCoroutine(serverAPI.UpgradePlayer(upgrade.id));
}
```

---

## 체크리스트

### 클라이언트 수정 사항
- [ ] GameManager에 serverAPI 참조 추가
- [ ] 게임 시작 시 `StartGame()` API 호출
- [ ] 게임 종료 시 `EndGame()` API 호출
- [ ] 로그인 성공 시 플레이어 정보 UI 업데이트
- [ ] 리더보드 UI 구현
- [ ] 로딩 인디케이터 추가
- [ ] 에러 핸들링 (네트워크 끊김 등)

### 서버 구현 사항
- [ ] `POST /auth/login` 구현
- [ ] `POST /game/start` 구현 (characterType 포함)
- [ ] `POST /game/end` 구현 (보상 계산)
- [ ] `GET /game/leaderboard` 구현
- [ ] `POST /upgrade/purchase` 구현
- [ ] 입력 검증 & 보안

### 테스트
- [ ] 로그인 → 캐릭터 선택 → 게임 플레이 → 결과 전체 플로우
- [ ] 리더보드 표시
- [ ] 업그레이드 구매
- [ ] 오프라인 → 온라인 전환
- [ ] 네트워크 에러 처리

---

**작성일**: 2026-01-14
