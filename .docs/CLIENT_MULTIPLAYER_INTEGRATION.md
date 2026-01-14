# NeoSurvive 클라이언트 멀티플레이 통합 완료

> **작성일**: 2026-01-14  
> **버전**: 2.0.0  
> **기반**: MULTIPLAYERSPEC.md

---

## ✅ 구현 완료 사항

### 1. GameServerAPI.cs 개선

#### 추가된 기능:
- ✅ **캐릭터 타입 포함 게임 시작**
  ```csharp
  public IEnumerator StartGame(CharacterType characterType, int stage = 1)
  ```
  
- ✅ **닉네임 변경 기능**
  ```csharp
  public IEnumerator ChangeNickname(string newNickname, Action<ChangeNicknameResponse> onSuccess)
  ```

- ✅ **PlayerId 외부 접근**
  ```csharp
  public int PlayerId => playerId;
  ```

- ✅ **PUT 요청 메서드 추가**
  ```csharp
  private UnityWebRequest CreatePutRequest(string endpoint, string json)
  ```

#### 수정된 DTO:
```csharp
// GameStartRequest에 캐릭터 타입 추가
[Serializable]
public class GameStartRequest
{
    public int playerId;
    public string characterType;  // "Hacker" or "Cyborg"
    public int stage;
}

// 닉네임 변경 DTO 추가
[Serializable]
public class ChangeNicknameRequest
{
    public string nickname;
}

[Serializable]
public class ChangeNicknameResponse
{
    public bool success;
    public string newNickname;
    public string error;
}
```

---

### 2. GameManager.cs 서버 연동

#### 추가된 필드:
```csharp
[Header("서버 연동")]
[SerializeField] private GameServerAPI serverAPI;
private int currentSessionId = -1;
```

#### 서버 초기화 (Awake):
```csharp
// GameServerAPI 찾기 또는 추가
if (serverAPI == null)
{
    serverAPI = GetComponent<GameServerAPI>();
    if (serverAPI == null)
    {
        serverAPI = gameObject.AddComponent<GameServerAPI>();
    }
}

// 서버 이벤트 구독
serverAPI.OnLoginSuccess += OnServerLoginSuccess;
serverAPI.OnGameEnded += OnServerGameEnded;
```

#### 게임 시작 메서드 추가:
```csharp
public void StartNewGame()
{
    startTime = Time.time;
    currentRunGold = 0;
    killCount = 0;
    
    // 서버에 게임 시작 알림 (캐릭터 타입 포함)
    if (serverAPI != null)
    {
        StartCoroutine(serverAPI.StartGame(selectedCharacter, stage: 1));
    }
}
```

#### 플레이어 사망 시 서버 전송:
```csharp
public void OnPlayerDeath()
{
    // 서버에 게임 종료 전송
    if (serverAPI != null && currentSessionId >= 0)
    {
        float survivalTime = GetGameTime();
        StartCoroutine(serverAPI.EndGame(
            survivalTime: Mathf.RoundToInt(survivalTime),
            enemiesKilled: killCount,
            isCleared: false
        ));
    }
    else
    {
        // 오프라인 모드: 로컬에 골드 저장
        totalGold += currentRunGold;
        SaveGold();
    }
}
```

#### 서버 이벤트 핸들러:
```csharp
// 로그인 성공 시
private void OnServerLoginSuccess(LoginResponse response)
{
    Debug.Log($"[서버 로그인] PlayerId: {response.playerId}, 닉네임: {response.nickname}");
    totalGold = response.gold;  // 서버 골드로 동기화
}

// 게임 종료 시
private void OnServerGameEnded(GameEndResponse response)
{
    Debug.Log($"[서버 게임 종료] 골드: +{response.goldEarned}, 경험치: +{response.experienceEarned}");
    
    if (response.leveledUp)
        Debug.Log($"🎉 레벨업! Lv.{response.currentLevel}");
    
    if (response.newRecord)
        Debug.Log("🏆 신기록 달성!");
    
    totalGold = response.totalGold;
    currentRunGold = 0;
    currentSessionId = -1;
}
```

---

### 3. CharacterSelector.cs 서버 연동

#### 게임 시작 메서드 수정:
```csharp
private void StartGame()
{
    // GameManager에 선택된 캐릭터 전달
    if (GameManager.Instance != null)
    {
        GameManager.Instance.SetSelectedCharacter(selectedCharacter);
        
        // 서버에 게임 시작 알림
        GameManager.Instance.StartNewGame();  // ← 서버 연동
    }

    // 선택 패널 숨기기
    if (selectionPanel != null)
    {
        selectionPanel.SetActive(false);
        Time.timeScale = 1f; // 게임 시간 재개
    }
}
```

---

## 🔄 게임 플로우

### 1. 게임 시작 시퀀스

```
1. Unity 씬 로드
   └─> GameManager Awake()
       ├─> GameServerAPI 초기화
       └─> 서버 이벤트 구독

2. GameServerAPI Start()
   └─> POST /auth/login (자동)
       └─> OnServerLoginSuccess
           └─> 플레이어 정보 & 골드 동기화

3. 캐릭터 선택 UI 표시
   └─> CharacterSelector ShowCharacterSelection()
       └─> 플레이어가 해커/사이보그 선택

4. "게임 시작" 버튼 클릭
   └─> CharacterSelector.StartGame()
       └─> GameManager.SetSelectedCharacter(selectedCharacter)
       └─> GameManager.StartNewGame()
           └─> POST /game/start
               {
                   playerId: 1,
                   characterType: "Hacker",
                   stage: 1
               }
```

### 2. 게임 진행 시퀀스

```
게임 플레이 (로컬 실행)
├─> 적 처치 → killCount++
├─> 골드 획득 → currentRunGold++
└─> 생존 시간 → gameTime++
```

### 3. 게임 종료 시퀀스

```
플레이어 사망
└─> Player.Die()
    └─> GameManager.OnPlayerDeath()
        └─> POST /game/end
            {
                sessionId: 67890,
                survivalTime: 300,
                enemiesKilled: 127,
                isCleared: false
            }
        └─> OnServerGameEnded(response)
            ├─> 획득 골드: response.goldEarned
            ├─> 획득 경험치: response.experienceEarned
            ├─> 레벨업 여부: response.leveledUp
            ├─> 신기록 여부: response.newRecord
            └─> 총 골드 업데이트: response.totalGold
```

---

## 🌐 클라이언트-서버 통신

### 로그인 (자동)
```http
POST /api/auth/login
{
  "deviceUID": "550e8400-e29b-41d4-a716-446655440000"
}
```

### 게임 시작 (캐릭터 선택 후)
```http
POST /api/game/start
{
  "playerId": 1,
  "characterType": "Hacker",  ← 추가됨!
  "stage": 1
}
```

### 게임 종료 (플레이어 사망 시)
```http
POST /api/game/end
{
  "sessionId": 1,
  "survivalTime": 300,
  "enemiesKilled": 127,
  "isCleared": false
}
```

### 닉네임 변경 (수동 호출)
```csharp
StartCoroutine(GameManager.Instance.GetComponent<GameServerAPI>()
    .ChangeNickname("프로게이머", (response) =>
    {
        if (response.success)
            Debug.Log($"닉네임 변경: {response.newNickname}");
        else
            Debug.LogError($"실패: {response.error}");
    }));
```

---

## 🧪 테스트 방법

### 1. 서버 실행
```bash
# 백엔드 서버 실행 (ASP.NET Core)
cd NeoSurviveServer
dotnet run

# 서버 주소 확인
# http://localhost:5157
```

### 2. Unity 실행
1. Unity 에디터 열기
2. LobbyScene 또는 게임 씬 실행
3. 콘솔 확인:
   ```
   ✅ 로그인 성공! PlayerId: 1, Nickname: 플레이어4437
   Level: 1, Gold: 500, Gems: 10
   ```

### 3. 게임 플레이
1. 캐릭터 선택 (해커 또는 사이보그)
2. "게임 시작" 클릭
3. 콘솔 확인:
   ```
   🎮 게임 시작! SessionId: 1, Character: Hacker, Stage: 1
   ```

4. 게임 진행 (적 처치)
5. 플레이어 사망
6. 콘솔 확인:
   ```
   [서버 게임 종료] 골드: +1850, 경험치: +950
   🎉 레벨업! Lv.5
   🏆 신기록 달성!
   ```

---

## 🔧 오프라인 모드

서버가 없어도 게임 실행 가능:

```csharp
// GameManager.StartNewGame()
if (serverAPI != null)
{
    // 온라인 모드: 서버 통신
    StartCoroutine(serverAPI.StartGame(selectedCharacter, stage: 1));
}
else
{
    // 오프라인 모드: 로컬 실행
    Debug.LogWarning("서버 API가 없습니다. 오프라인 모드로 실행합니다.");
}
```

골드는 PlayerPrefs에 로컬 저장됨.

---

## 📌 주요 변경사항 요약

| 파일 | 변경 내용 |
|------|-----------|
| **GameServerAPI.cs** | • 캐릭터 타입 포함 게임 시작<br>• 닉네임 변경 기능 추가<br>• PUT 요청 메서드 추가<br>• GameStartRequest DTO 수정 |
| **GameManager.cs** | • GameServerAPI 초기화 및 이벤트 구독<br>• StartNewGame() 메서드 추가<br>• OnPlayerDeath() 서버 연동<br>• 서버 이벤트 핸들러 추가 |
| **CharacterSelector.cs** | • StartGame() 메서드 서버 연동 |

### 4. 업그레이드 시스템 연동

#### GameServerAPI 메서드 추가:
```csharp
public IEnumerator GetPlayerUpgrades(Action<UpgradeListResponse> onSuccess)
public IEnumerator PurchasePlayerUpgrade(string upgradeId, Action<UpgradePurchaseResponse> onSuccess)
```

#### UpgradeManager 수정:
- 로컬 `DataManager` 대신 `GameServerAPI`를 사용하도록 변경
- 서버에서 업그레이드 목록(`UpgradeDTO`)을 받아와 UI 표시
- 구매 시 서버에 요청하고, 성공 시 골드 및 레벨 업데이트

### 5. 리더보드 시스템 구현

#### LeaderboardUI.cs 생성:
- `RefreshLeaderboard()`: 서버에서 Top 10 랭킹 조회
- `CharacterSelector`에서 버튼 클릭 시 팝업 오픈

#### CharacterSelector 수정:
- 리더보드 버튼 연결
```csharp
if (leaderboardButton != null)
    leaderboardButton.onClick.AddListener(() => leaderboardUI.Open());
```

---

## 🔄 게임 플로우 (Phase 2 업데이트)

### 4. 업그레이드 흐름

```
업그레이드 창 열기
└─> UpgradeManager.OpenUpgradeWindow()
    └─> GET /upgrade/player/{id}
        └─> UI 갱신 (보유 골드, 업그레이드 레벨/비용)

구매 버튼 클릭
└─> UpgradeManager.TryBuyUpgrade()
    └─> POST /upgrade/player/{id}/purchase
        { "upgradeId": "ATK" }
        └─> 성공: 골드 차감, 레벨 증가 -> UI 갱신
        └─> 실패: 에러 메시지 표시
```

### 5. 리더보드 흐름

```
리더보드 버튼 클릭
└─> LeaderboardUI.Open()
    └─> GET /game/leaderboard
        └─> 랭킹 데이터 수신 -> 스크롤 뷰 아이템 생성
```

---

## 🌐 추가된 API 통신

### 업그레이드 목록
```http
GET /api/upgrade/player/1
```

### 업그레이드 구매
```http
POST /api/upgrade/player/1/purchase
{
  "upgradeId": "ATK"
}
```

### 리더보드 조회
```http
GET /api/game/leaderboard?top=10
```

---

## 📌 변경 이력 업데이트

### v2.1.0 (2026-01-14)
- ✅ **업그레이드 시스템 서버 연동**: 영구 능력치 강화 기능 구현
- ✅ **리더보드 UI**: 상위 랭커 조회 기능 추가
- ✅ **CharacterSelector 개선**: 리더보드 진입점 추가
- ✅ **닉네임 변경 UI**: 닉네임 변경 팝업 및 기능 구현

---

