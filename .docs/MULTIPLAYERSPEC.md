# NeoSurvive 멀티플레이 구현 명세서

> **작성일**: 2026-01-14  
> **버전**: 2.0.0  
> **기반**: docs/GameServerSpecification.md

---

## 📋 구현 완료 사항

### ✅ Phase 1: 핵심 기능 (완료)

#### 1. **인증 시스템**
- ✅ 디바이스 UID 기반 로그인/회원가입
- ✅ 플레이어 정보 조회 (통계 포함)
- ✅ 닉네임 변경 기능

#### 2. **게임 세션 관리**
- ✅ 캐릭터 타입 선택 (Hacker/Cyborg)
- ✅ 게임 시작/종료
-  서버 측 보상 계산 (골드, 경험치)
- ✅ 자동 레벨업 시스템
- ✅ 플레이어 통계 업데이트
- ✅ 리더보드

#### 3. **업그레이드 시스템**
- ✅ 업그레이드 목록 조회
- ✅ 업그레이드 구매
- ✅ 서버 측 비용 계산 및 검증
- ✅ 골드 차감 및 레벨 증가

#### 4. **무기 시스템**
- ✅ 무기 목록 조회
- ✅ 무기 업그레이드
- ✅ 신규 무기 획득

---

## 🗂️ 데이터 모델

### Player (플레이어)
```csharp
public class Player
{
    public int Id { get; set; }
    public string DeviceUID { get; set; }  // 고유 인덱스
    public string Nickname { get; set; }
    
    // 진행도
    public int Level { get; set; }
    public int Experience { get; set; }
    public int Gold { get; set; }
    public int Gems { get; set; }
    
    // 최고 기록
    public int HighestStage { get; set; }
    public int BestSurvivalTime { get; set; }
    
    // 통계
    public int TotalGamesPlayed { get; set; }
    public int TotalWins { get; set; }
    public int TotalEnemiesKilled { get; set; }
    
    // 타임스탬프
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    
    // 관계
    public ICollection<PlayerWeapon> Weapons { get; set; }
    public ICollection<PlayerUpgrade> Upgrades { get; set; }
    public ICollection<GameSession> GameSessions { get; set; }
}
```

### GameSession (게임 세션)
```csharp
public class GameSession
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    
    public string CharacterType { get; set; }  // "Hacker" or "Cyborg"
    public int Stage { get; set; }
    
    public int SurvivalTime { get; set; }
    public int EnemiesKilled { get; set; }
    public int GoldEarned { get; set; }
    public int ExperienceEarned { get; set; }
    public bool IsCleared { get; set; }
    
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
```

### PlayerUpgrade (업그레이드)
```csharp
public class PlayerUpgrade
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    
    public string UpgradeId { get; set; }  // "ATK", "HP", "SPD", "RANGE"
    public int Level { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}
```

---

## 🔌 API 엔드포인트

### 인증 (Auth)

#### POST /api/auth/login
**요청:**
```json
{
  "deviceUID": "550e8400-e29b-41d4-a716-446655440000"
}
```

**응답:**
```json
{
  "playerId": 1,
  "nickname": "플레이어4437",
  "level": 1,
  "experience": 0,
  "gold": 500,
  "gems": 10,
  "highestStage": 0,
  "bestSurvivalTime": 0,
  "isNewPlayer": true
}
```

#### GET /api/auth/player/{playerId}
**응답:**
```json
{
  "id": 1,
  "nickname": "프로게이머",
  "level": 5,
  "experience": 50,
  "experienceToNext": 350,
  "gold": 2250,
  "gems": 10,
  "highestStage": 1,
  "bestSurvivalTime": 300,
  "totalGamesPlayed": 1,
  "totalWins": 1,
  "totalEnemiesKilled": 150,
  "createdAt": "2026-01-14T07:44:02Z",
  "lastLoginAt": "2026-01-14T07:44:02Z"
}
```

#### PUT /api/auth/player/{playerId}/nickname
**요청:**
```json
{
  "nickname": "프로게이머"
}
```

**응답:**
```json
{
  "success": true,
  "newNickname": "프로게이머",
  "error": null
}
```

---

### 게임 (Game)

#### POST /api/game/start
**요청:**
```json
{
  "playerId": 1,
  "characterType": "Hacker",  // ← 필수!
  "stage": 1
}
```

**응답:**
```json
{
  "sessionId": 1,
  "startedAt": "2026-01-14T07:44:09Z"
}
```

#### POST /api/game/end
**요청:**
```json
{
  "sessionId": 1,
  "survivalTime": 300,
  "enemiesKilled": 150,
  "isCleared": true
}
```

**응답:**
```json
{
  "goldEarned": 1850,
  "experienceEarned": 950,
  "leveledUp": true,
  "currentLevel": 5,
  "currentExperience": 50,
  "totalGold": 2250,
  "newRecord": true
}
```

**서버 측 계산 로직:**
```csharp
// 골드 계산
goldEarned = (survivalTime * 2) + (enemiesKilled * 5) + (isCleared ? 500 : 0)

// 경험치 계산
experienceEarned = (survivalTime * 1) + (enemiesKilled * 3) + (isCleared ? 200 : 0)

// 레벨업 필요 경험치
experienceToNext = 100 + (currentLevel * 50)
```

#### GET /api/game/leaderboard?top=10
**응답:**
```json
[
  {
    "rank": 1,
    "nickname": "프로게이머",
    "level": 5,
    "bestSurvivalTime": 300,
    "highestStage": 1
  }
]
```

---

### 업그레이드 (Upgrade)

#### GET /api/upgrade/player/{playerId}
**응답:**
```json
{
  "upgrades": [
    {
      "id": "ATK",
      "displayName": "공격력",
      "currentLevel": 1,
      "maxLevel": 0,  // 0 = 무제한
      "baseCost": 100,
      "costPerLevel": 50,
      "nextCost": 150,
      "valuePerLevel": 5.0,
      "currentValue": 5.0
    },
    {
      "id": "HP",
      "displayName": "최대 체력",
      "currentLevel": 0,
      "maxLevel": 10,
      "baseCost": 150,
      "costPerLevel": 75,
      "nextCost": 150,
      "valuePerLevel": 10.0,
      "currentValue": 0.0
    }
  ],
  "playerGold": 400
}
```

#### POST /api/upgrade/player/{playerId}/purchase
**요청:**
```json
{
  "upgradeId": "ATK"
}
```

**응답 (성공):**
```json
{
  "success": true,
  "upgradeId": "ATK",
  "newLevel": 2,
  "goldSpent": 150,
  "remainingGold": 250,
  "newValue": 10.0,
  "errorMessage": null
}
```

**응답 (골드 부족):**
```json
{
  "success": false,
  "upgradeId": "ATK",
  "newLevel": 0,
  "goldSpent": 0,
  "remainingGold": 50,
  "newValue": 0.0,
  "errorMessage": "골드가 부족합니다. (필요: 150, 보유: 50)"
}
```

---

## 🎮 Unity 클라이언트 연동

### 1. 수정된 DTO
```csharp
[Serializable]
public class GameStartRequest
{
    public int playerId;
    public string characterType;  // ← 추가!
    public int stage;
}
```

### 2. 게임 시작 (캐릭터 선택 포함)
```csharp
public IEnumerator StartGame(int playerId, CharacterType character, int stage)
{
    var request = new
    {
        playerId = playerId,
        characterType = character.ToString(),  // "Hacker" or "Cyborg"
        stage = stage
    };

    string json = JsonConvert.SerializeObject(request);

    using (UnityWebRequest webRequest = CreatePostRequest("/game/start", json))
    {
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            var response = JsonConvert.DeserializeObject<GameStartResponse>(
                webRequest.downloadHandler.text);
            currentSessionId = response.sessionId;
            Debug.Log($"게임 시작! SessionId: {currentSessionId}, Character: {character}");
        }
    }
}
```

### 3. 업그레이드 구매
```csharp
public IEnumerator PurchaseUpgrade(int playerId, string upgradeId)
{
    var request = new { upgradeId = upgradeId };
    string json = JsonConvert.SerializeObject(request);

    using (UnityWebRequest webRequest = CreatePostRequest(
        $"/upgrade/player/{playerId}/purchase", json))
    {
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            var response = JsonConvert.DeserializeObject<UpgradePurchaseResponse>(
                webRequest.downloadHandler.text);

            if (response.success)
            {
                Debug.Log($"업그레이드 성공! {response.upgradeId} Lv.{response.newLevel}");
                // UI 업데이트
            }
            else
            {
                Debug.LogWarning($"업그레이드 실패: {response.errorMessage}");
            }
        }
    }
}
```

### 4. 닉네임 변경
```csharp
public IEnumerator ChangeNickname(int playerId, string newNickname)
{
    var request = new { nickname = newNickname };
    string json = JsonConvert.SerializeObject(request);

    using (UnityWebRequest webRequest = CreatePutRequest(
        $"/auth/player/{playerId}/nickname", json))
    {
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            var response = JsonConvert.DeserializeObject<ChangeNicknameResponse>(
                webRequest.downloadHandler.text);

            if (response.success)
            {
                Debug.Log($"닉네임 변경 성공: {response.newNickname}");
            }
        }
    }
}
```

---

## 🔐 보안 구현 사항

### 1. 서버 측 검증
```csharp
// ✅ 골드는 서버에서 계산
// ❌ 클라이언트에서 보낸 goldEarned는 무시

// ✅ 업그레이드 비용은 서버에서 계산
int cost = baseCost + (costPerLevel * currentLevel);

// ✅ 플레이어 골드 확인
if (player.Gold < cost) {
    return error("골드 부족");
}
```

### 2. 입력 검증
```csharp
// 닉네임 길이 확인
if (nickname.Length > 50) {
    return error("닉네임이 너무 깁니다");
}

// 중복 닉네임 확인
if (await _context.Players.AnyAsync(p => p.Nickname == nickname)) {
    return error("이미 사용 중인 닉네임입니다");
}
```

### 3. 고유 인덱스
```csharp
// DeviceUID 중복 방지
entity.HasIndex(p => p.DeviceUID).IsUnique();

// PlayerId + UpgradeId 복합 고유 인덱스
entity.HasIndex(pu => new { pu.PlayerId, pu.UpgradeId }).IsUnique();
```

---

## 📊 게임 밸런스

### 업그레이드 정의
```csharp
{
    "ATK":   { 기본비용: 100, 레벨당비용: 50,  레벨당수치: 5.0,  최대레벨: 무제한 },
    "HP":    { 基本费用: 150, 레벨당비용: 75,  레벨당수치: 10.0, 최대레벨: 10 },
    "SPD":   { 기본비용: 120, 레벨당비용: 60,  레벨당수치: 0.2,  최대레벨: 15 },
    "RANGE": { 기본비용: 200, 레벨당비용: 100, 레벨당수치: 0.5,  최대레벨: 8 }
}
```

### 보상 공식
```
골드 = (생존시간 × 2) + (적처치 × 5) + (클리어보너스 500)
경험치 = (생존시간 × 1) + (적처치 × 3) + (클리어보너스 200)
레벨업 필요 경험치 = 100 + (현재레벨 × 50)
```

---

## ✅ 테스트 결과

### 로그인
```bash
curl -X POST http://localhost:5157/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"deviceUID":"test-001"}'
```
**결과:** ✅ 신규 플레이어 생성, 기본 무기 지급

### 게임 플레이
```bash
# 시작
curl -X POST http://localhost:5157/api/game/start \
  -H "Content-Type: application/json" \
  -d '{"playerId":1,"characterType":"Hacker","stage":1}'

# 종료
curl -X POST http://localhost:5157/api/game/end \
  -H "Content-Type: application/json" \
  -d '{"sessionId":1,"survivalTime":300,"enemiesKilled":150,"isCleared":true}'
```
**결과:** ✅ 레벨 1→5 상승, 골드 1850 획득, 통계 업데이트

### 업그레이드
```bash
curl -X POST http://localhost:5157/api/upgrade/player/1/purchase \
  -H "Content-Type: application/json" \
  -d '{"upgradeId":"ATK"}'
```
**결과:** ✅ 골드 100 차감, ATK 레벨 0→1

### 닉네임 변경
```bash
curl -X PUT http://localhost:5157/api/auth/player/1/nickname \
  -H "Content-Type: application/json" \
  -d '{"nickname":"프로게이머"}'
```
**결과:** ✅ 닉네임 변경 성공

---

## 🚀 다음 개발 단계

### Phase 2: 추가 기능
- [ ] 친구 시스템
- [ ] 일일 보상
- [ ] 업적 시스템
- [ ] 이벤트/시즌 시스템

### Phase 3: 최적화
- [ ] Redis 캐싱 (리더보드)
- [ ] 데이터베이스 인덱스 최적화
- [ ] API Rate Limiting

### Phase 4: 보안 강화
- [ ] JWT 토큰 인증
- [ ] 치팅 탐지 시스템
- [ ] 요청 검증 강화

---

## 📝 변경 이력

### v2.0.0 (2026-01-14)
- ✅ 캐릭터 타입 선택 기능 추가
- ✅ 업그레이드 시스템 구현
- ✅ 플레이어 통계 추가
- ✅ 닉네임 변경 기능
- ✅ 서버 측 보상 계산

### v1.0.0 (2026-01-14)
- ✅ 기본 인증 시스템
- ✅ 게임 세션 관리
- ✅ 리더보드
- ✅ 무기 시스템

---

**문서 버전**: 2.0.0  
**마지막 업데이트**: 2026-01-14  
**서버 상태**: ✅ 실행 중 (http://localhost:5157)
