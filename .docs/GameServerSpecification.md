# NeoSurvive 게임 서버 API 명세서

> **작성일**: 2026-01-14  
> **버전**: 1.0.0  
> **게임**: NeoSurvive (로그라이크 생존 게임)

---

## 📋 목차

1. [개요](#개요)
2. [클라이언트 현황 분석](#클라이언트-현황-분석)
3. [데이터 모델](#데이터-모델)
4. [API 엔드포인트](#api-엔드포인트)
5. [게임 플로우](#게임-플로우)
6. [클라이언트-서버 차이점](#클라이언트-서버-차이점)
7. [보안 고려사항](#보안-고려사항)
8. [추가 구현 권장사항](#추가-구현-권장사항)

---

## 개요

### 게임 특징
- **장르**: 2D 로그라이크 생존 액션
- **플랫폼**: Unity (PC/Mobile)
- **핵심 메커니즘**: 
  - 캐릭터 선택 (해커/사이보그)
  - 웨이브 기반 적 생존
  - 실시간 경험치/레벨업
  - 골드 기반 업그레이드
  - 리더보드

### 서버 기술 스택 권장
- **백엔드**: ASP.NET Core 또는 Node.js (Express)
- **데이터베이스**: PostgreSQL 또는 MySQL
- **캐싱**: Redis (리더보드, 세션 관리)
- **인증**: JWT 또는 세션 기반

---

## 클라이언트 현황 분석

### 1. 캐릭터 시스템

#### 캐릭터 타입
```csharp
public enum CharacterType
{
    Hacker,   // 해커 - 원거리, 빠른 이동
    Cyborg    // 사이보그 - 근접, 높은 체력
}
```

#### 캐릭터 데이터 구조
```csharp
public class CharacterData : ScriptableObject
{
    public CharacterType characterType;
    public string characterName;
    public string description;
    
    // 기본 스탯
    public float baseHealth;           // 해커: 80, 사이보그: 150
    public float baseAttackDamage;     // 해커: 15, 사이보그: 20
    public float baseAttackRange;      // 해커: 5, 사이보그: 2
    public float baseAttackSpeed;      // 해커: 1.2, 사이보그: 0.8
    public float baseMoveSpeed;        // 해커: 6, 사이보그: 4
    
    public string specialAbilityDescription;
}
```

### 2. 플레이어 진행 시스템

#### 클라이언트에서 관리하는 데이터
```csharp
// 플레이어 경험치 & 레벨
- int experience          // 현재 경험치
- int level              // 현재 레벨
- int MaxExperience      // level * 100 (임시 공식)

// 골드 시스템
- int currentRunGold     // 현재 플레이 중 획득 골드
- int totalGold          // 저장된 총 골드 (PlayerPrefs)

// 게임 통계
- int killCount          // 현재 플레이 적 처치 수
- float gameTime         // 생존 시간 (초)
- CharacterType selected // 선택된 캐릭터
```

### 3. 업그레이드 시스템

#### 업그레이드 정의
```csharp
public class UpgradeDef
{
    public string id;              // "ATK", "HP", "SPD" 등
    public string displayName;     // "공격력", "체력" 등
    public int baseCost;           // 기본 비용
    public int costPerLevel;       // 레벨당 추가 비용
    public float valuePerLevel;    // 레벨당 증가 수치
    public int maxLevel;           // 최대 레벨 (0 = 무제한)
}

// 비용 계산 공식
Cost = baseCost + (costPerLevel * currentLevel)
```

#### 스탯 타입
```csharp
public enum StatType
{
    AttackDamage,  // 공격력
    AttackRange,   // 공격 범위
    AttackSpeed,   // 공격 속도
    MaxHP         // 최대 체력
}
```

### 4. 현재 클라이언트의 제한사항

❌ **미구현 항목**:
- 스테이지 시스템 (현재 단일 맵)
- 보석(Gem) 화폐
- 무기 획득/업그레이드 (웨이브 진행 시 선택 UI는 있으나 저장 X)
- 닉네임 설정
- 실제 경험치 획득 로직

✅ **현재 동작하는 항목**:
- 골드 획득 및 저장 (PlayerPrefs)
- 적 킬 카운트
- 생존 시간 측정
- 캐릭터 선택
- 업그레이드 구매 (골드 소모)

---

## 데이터 모델

### ERD (Entity Relationship Diagram)

```
┌─────────────┐         ┌──────────────┐         ┌─────────────┐
│   Player    │────1:N──│ GameSession  │         │  Character  │
├─────────────┤         ├──────────────┤         ├─────────────┤
│ id (PK)     │         │ id (PK)      │         │ id (PK)     │
│ deviceUID   │         │ playerId FK  │         │ type (enum) │
│ nickname    │         │ characterType│         │ name        │
│ level       │         │ stage        │         │ baseHealth  │
│ experience  │         │ survivalTime │         │ baseAttack  │
│ gold        │         │ enemiesKilled│         │ ...         │
│ gems        │         │ goldEarned   │         └─────────────┘
│ highestStage│         │ expEarned    │
│ bestSurvival│         │ isCleared    │
│ createdAt   │         │ startedAt    │
│ updatedAt   │         │ endedAt      │
└─────────────┘         └──────────────┘
       │
       │1:N
       ▼
┌──────────────┐        ┌──────────────┐
│ PlayerUpgrade│        │ PlayerWeapon │
├──────────────┤        ├──────────────┤
│ id (PK)      │        │ id (PK)      │
│ playerId FK  │        │ playerId FK  │
│ upgradeId    │        │ weaponType   │
│ level        │        │ level        │
│ updatedAt    │        │ acquiredAt   │
└──────────────┘        └──────────────┘
```

### 1. Player (플레이어)

```sql
CREATE TABLE Player (
    id INT PRIMARY KEY AUTO_INCREMENT,
    deviceUID VARCHAR(255) UNIQUE NOT NULL,
    nickname VARCHAR(50) DEFAULT 'Player',
    
    -- 진행도
    level INT DEFAULT 1,
    experience INT DEFAULT 0,
    gold INT DEFAULT 0,
    gems INT DEFAULT 0,
    
    -- 최고 기록
    highestStage INT DEFAULT 0,
    bestSurvivalTime INT DEFAULT 0,  -- 초 단위
    totalEnemiesKilled INT DEFAULT 0,
    totalGamesPlayed INT DEFAULT 0,
    totalWins INT DEFAULT 0,
    
    -- 타임스탬프
    createdAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    updatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    lastLoginAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_deviceUID (deviceUID),
    INDEX idx_bestSurvivalTime (bestSurvivalTime DESC)
);
```

### 2. GameSession (게임 세션)

```sql
CREATE TABLE GameSession (
    id INT PRIMARY KEY AUTO_INCREMENT,
    playerId INT NOT NULL,
    
    -- 게임 설정
    characterType ENUM('Hacker', 'Cyborg') NOT NULL,
    stage INT DEFAULT 1,
    
    -- 게임 결과
    survivalTime INT DEFAULT 0,      -- 초
    enemiesKilled INT DEFAULT 0,
    goldEarned INT DEFAULT 0,
    experienceEarned INT DEFAULT 0,
    isCleared BOOLEAN DEFAULT FALSE,
    
    -- 추가 통계 (선택사항)
    damageDealt INT DEFAULT 0,
    damageTaken INT DEFAULT 0,
    powerupsCollected INT DEFAULT 0,
    
    -- 타임스탬프
    startedAt DATETIME NOT NULL,
    endedAt DATETIME,
    
    FOREIGN KEY (playerId) REFERENCES Player(id) ON DELETE CASCADE,
    INDEX idx_playerId (playerId),
    INDEX idx_startedAt (startedAt DESC),
    INDEX idx_survivalTime (survivalTime DESC)
);
```

### 3. PlayerUpgrade (업그레이드)

```sql
CREATE TABLE PlayerUpgrade (
    id INT PRIMARY KEY AUTO_INCREMENT,
    playerId INT NOT NULL,
    
    -- 업그레이드 정보
    upgradeId VARCHAR(50) NOT NULL,  -- "ATK", "HP", "SPD", "RANGE" 등
    level INT DEFAULT 0,
    
    updatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    FOREIGN KEY (playerId) REFERENCES Player(id) ON DELETE CASCADE,
    UNIQUE KEY unique_player_upgrade (playerId, upgradeId),
    INDEX idx_playerId (playerId)
);
```

### 4. PlayerWeapon (무기)

```sql
CREATE TABLE PlayerWeapon (
    id INT PRIMARY KEY AUTO_INCREMENT,
    playerId INT NOT NULL,
    
    -- 무기 정보
    weaponType VARCHAR(50) NOT NULL,  -- "Pistol", "Rifle", "Sword" 등
    level INT DEFAULT 1,
    
    acquiredAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (playerId) REFERENCES Player(id) ON DELETE CASCADE,
    UNIQUE KEY unique_player_weapon (playerId, weaponType),
    INDEX idx_playerId (playerId)
);
```

### 5. Character (캐릭터 마스터 데이터) - 선택사항

```sql
CREATE TABLE Character (
    id INT PRIMARY KEY AUTO_INCREMENT,
    type ENUM('Hacker', 'Cyborg') UNIQUE NOT NULL,
    name VARCHAR(50) NOT NULL,
    description TEXT,
    
    -- 기본 스탯
    baseHealth FLOAT NOT NULL,
    baseAttackDamage FLOAT NOT NULL,
    baseAttackRange FLOAT NOT NULL,
    baseAttackSpeed FLOAT NOT NULL,
    baseMoveSpeed FLOAT NOT NULL,
    
    specialAbilityDescription TEXT,
    
    -- 선택 통계
    timesSelected INT DEFAULT 0,
    totalWins INT DEFAULT 0
);

-- 초기 데이터
INSERT INTO Character (type, name, description, baseHealth, baseAttackDamage, 
                       baseAttackRange, baseAttackSpeed, baseMoveSpeed, 
                       specialAbilityDescription) VALUES
('Hacker', '해커', '빠르고 민첩한 원거리 공격 전문가', 
 80, 15, 5, 1.2, 6, 
 '원거리에서 높은 데미지를 입히고 빠르게 이동할 수 있습니다.'),
('Cyborg', '사이보그', '강력한 체력과 근접 공격력을 가진 탱커', 
 150, 20, 2, 0.8, 4, 
 '높은 체력과 강력한 근접 공격력으로 적을 압도합니다.');
```

---

## API 엔드포인트

### Base URL
```
Production:  https://api.neosurvive.com/api
Development: http://localhost:5157/api
```

### 인증 헤더
```http
Content-Type: application/json
Authorization: Bearer {JWT_TOKEN}  # 선택사항
```

---

### 1. 인증 (Authentication)

#### 1.1 로그인/회원가입

**Endpoint**: `POST /auth/login`

**Request**:
```json
{
  "deviceUID": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response** (200 OK):
```json
{
  "playerId": 12345,
  "nickname": "Player12345",
  "level": 5,
  "experience": 250,
  "gold": 1500,
  "gems": 50,
  "highestStage": 3,
  "bestSurvivalTime": 450,
  "isNewPlayer": false,
  "token": "eyJhbGciOiJIUzI1NiIs..."  // JWT (선택사항)
}
```

**로직**:
1. `deviceUID`로 기존 플레이어 조회
2. 없으면 새 플레이어 생성 (`isNewPlayer: true`)
3. 있으면 기존 정보 반환 (`isNewPlayer: false`)
4. `lastLoginAt` 업데이트

---

#### 1.2 플레이어 정보 조회

**Endpoint**: `GET /auth/player/{playerId}`

**Response** (200 OK):
```json
{
  "id": 12345,
  "nickname": "Player12345",
  "level": 5,
  "experience": 250,
  "experienceToNext": 500,
  "gold": 1500,
  "gems": 50,
  "highestStage": 3,
  "bestSurvivalTime": 450,
  "totalEnemiesKilled": 1250,
  "totalGamesPlayed": 42,
  "totalWins": 8,
  "createdAt": "2026-01-01T00:00:00Z",
  "lastLoginAt": "2026-01-14T16:00:00Z"
}
```

---

#### 1.3 닉네임 변경

**Endpoint**: `PUT /auth/player/{playerId}/nickname`

**Request**:
```json
{
  "nickname": "CoolGamer123"
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "newNickname": "CoolGamer123"
}
```

**에러**:
```json
{
  "success": false,
  "error": "Nickname already taken"
}
```

---

### 2. 게임 (Game)

#### 2.1 게임 시작

**Endpoint**: `POST /game/start`

**Request**:
```json
{
  "playerId": 12345,
  "characterType": "Hacker",  // or "Cyborg"
  "stage": 1
}
```

**Response** (200 OK):
```json
{
  "sessionId": 67890,
  "startedAt": "2026-01-14T16:00:00Z",
  "characterStats": {
    "health": 80,
    "attackDamage": 15,
    "attackRange": 5,
    "attackSpeed": 1.2,
    "moveSpeed": 6
  }
}
```

**로직**:
1. 새 `GameSession` 레코드 생성
2. `startedAt` 타임스탬프 기록
3. 선택한 캐릭터 스탯 반환
4. Character 테이블의 `timesSelected` 증가 (선택사항)

---

#### 2.2 게임 종료

**Endpoint**: `POST /game/end`

**Request**:
```json
{
  "sessionId": 67890,
  "survivalTime": 450,      // 초
  "enemiesKilled": 127,
  "goldEarned": 250,
  "isCleared": false,
  
  // 선택사항
  "damageDealt": 5000,
  "damageTaken": 300,
  "powerupsCollected": 15
}
```

**Response** (200 OK):
```json
{
  "success": true,
  
  // 보상
  "goldEarned": 250,
  "experienceEarned": 180,
  
  // 레벨업 여부
  "leveledUp": true,
  "currentLevel": 6,
  "currentExperience": 30,
  "experienceToNext": 600,
  
  // 최종 재화
  "totalGold": 1750,
  "totalGems": 50,
  
  // 기록 갱신
  "newRecord": true,
  "previousBestTime": 420,
  "newBestTime": 450,
  
  // 순위 (선택사항)
  "leaderboardRank": 42
}
```

**로직**:
1. `GameSession` 업데이트 (생존시간, 킬 수 등)
2. `endedAt` 타임스탬프 기록
3. **경험치 계산 및 지급**:
   ```
   baseExp = survivalTime * 0.4
   killBonus = enemiesKilled * 10
   clearBonus = isCleared ? 500 : 0
   totalExp = baseExp + killBonus + clearBonus
   ```
4. **레벨업 확인**:
   ```
   experienceToNext = currentLevel * 100
   if (currentExp >= experienceToNext) {
     level++
     currentExp -= experienceToNext
     leveledUp = true
   }
   ```
5. **골드 지급**:
   ```
   player.gold += goldEarned
   ```
6. **최고 기록 갱신**:
   ```
   if (survivalTime > bestSurvivalTime) {
     bestSurvivalTime = survivalTime
     newRecord = true
   }
   ```
7. 플레이어 통계 업데이트
8. Character 통계 업데이트 (승리 시)

---

#### 2.3 리더보드 조회

**Endpoint**: `GET /game/leaderboard?top=100&type=survival`

**Query Parameters**:
- `top`: 상위 몇 명 (기본값: 100)
- `type`: `survival` (생존시간) 또는 `stage` (최고 스테이지)

**Response** (200 OK):
```json
[
  {
    "rank": 1,
    "playerId": 999,
    "nickname": "ProGamer",
    "level": 25,
    "bestSurvivalTime": 3600,
    "highestStage": 10,
    "characterType": "Hacker"
  },
  {
    "rank": 2,
    "playerId": 888,
    "nickname": "Survivor",
    "level": 22,
    "bestSurvivalTime": 3200,
    "highestStage": 8,
    "characterType": "Cyborg"
  }
  // ... 더 많은 항목
]
```

**최적화**:
- Redis 캐싱 (5분 TTL)
- Sorted Set 사용 권장

---

#### 2.4 내 순위 조회

**Endpoint**: `GET /game/leaderboard/{playerId}/rank`

**Response** (200 OK):
```json
{
  "playerId": 12345,
  "nickname": "Player12345",
  "rank": 127,
  "bestSurvivalTime": 450,
  "percentile": 15.3,  // 상위 15.3%
  "nearbyPlayers": [
    { "rank": 125, "nickname": "Player1", "bestSurvivalTime": 455 },
    { "rank": 126, "nickname": "Player2", "bestSurvivalTime": 452 },
    { "rank": 127, "nickname": "Player12345", "bestSurvivalTime": 450 },
    { "rank": 128, "nickname": "Player3", "bestSurvivalTime": 448 },
    { "rank": 129, "nickname": "Player4", "bestSurvivalTime": 445 }
  ]
}
```

---

### 3. 업그레이드 (Upgrade)

#### 3.1 업그레이드 목록 조회

**Endpoint**: `GET /upgrade/player/{playerId}`

**Response** (200 OK):
```json
{
  "upgrades": [
    {
      "id": "ATK",
      "displayName": "공격력",
      "currentLevel": 5,
      "maxLevel": 0,  // 0 = 무제한
      "baseCost": 100,
      "costPerLevel": 50,
      "nextCost": 350,  // 100 + (50 * 5)
      "valuePerLevel": 5.0,
      "currentValue": 25.0  // 5.0 * 5
    },
    {
      "id": "HP",
      "displayName": "최대 체력",
      "currentLevel": 3,
      "maxLevel": 10,
      "baseCost": 150,
      "costPerLevel": 75,
      "nextCost": 375,
      "valuePerLevel": 10.0,
      "currentValue": 30.0
    }
    // ... 더 많은 업그레이드
  ],
  "playerGold": 1500
}
```

---

#### 3.2 업그레이드 구매

**Endpoint**: `POST /upgrade/player/{playerId}/purchase`

**Request**:
```json
{
  "upgradeId": "ATK"
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "upgradeId": "ATK",
  "newLevel": 6,
  "goldSpent": 350,
  "remainingGold": 1150,
  "newValue": 30.0
}
```

**에러** (400 Bad Request):
```json
{
  "success": false,
  "error": "Insufficient gold",
  "required": 350,
  "available": 300
}
```

**에러** (400 Bad Request):
```json
{
  "success": false,
  "error": "Max level reached",
  "currentLevel": 10,
  "maxLevel": 10
}
```

**로직**:
1. 현재 레벨 조회 (없으면 0)
2. 최대 레벨 확인
3. 비용 계산: `baseCost + (costPerLevel * currentLevel)`
4. 골드 충분한지 확인
5. 골드 차감
6. 업그레이드 레벨 증가
7. PlayerUpgrade 테이블 업데이트 (없으면 INSERT, 있으면 UPDATE)

---

### 4. 무기 (Weapon)

#### 4.1 보유 무기 목록

**Endpoint**: `GET /weapon/player/{playerId}`

**Response** (200 OK):
```json
[
  {
    "id": 1,
    "weaponType": "Pistol",
    "level": 3,
    "acquiredAt": "2026-01-10T12:00:00Z"
  },
  {
    "id": 2,
    "weaponType": "Rifle",
    "level": 1,
    "acquiredAt": "2026-01-12T15:30:00Z"
  }
]
```

---

#### 4.2 무기 획득

**Endpoint**: `POST /weapon/player/{playerId}/acquire`

**Request**:
```json
{
  "weaponType": "Sword"
}
```

**Response** (200 OK):
```json
{
  "id": 3,
  "weaponType": "Sword",
  "level": 1,
  "acquiredAt": "2026-01-14T16:00:00Z",
  "isNew": true
}
```

**에러** (400):
```json
{
  "success": false,
  "error": "Weapon already owned"
}
```

---

#### 4.3 무기 업그레이드

**Endpoint**: `POST /weapon/player/{playerId}/upgrade`

**Request**:
```json
{
  "weaponId": 1
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "weaponId": 1,
  "weaponType": "Pistol",
  "newLevel": 4,
  "goldSpent": 500,
  "remainingGold": 1000
}
```

---

### 5. 통계 (Statistics) - 선택사항

#### 5.1 플레이어 상세 통계

**Endpoint**: `GET /stats/player/{playerId}`

**Response**:
```json
{
  "playerId": 12345,
  "overview": {
    "totalPlayTime": 36000,  // 초
    "totalGamesPlayed": 150,
    "totalWins": 45,
    "winRate": 30.0,
    "averageSurvivalTime": 240,
    "totalEnemiesKilled": 5400
  },
  "characterStats": {
    "Hacker": {
      "gamesPlayed": 90,
      "wins": 30,
      "bestSurvivalTime": 450,
      "totalKills": 3200
    },
    "Cyborg": {
      "gamesPlayed": 60,
      "wins": 15,
      "bestSurvivalTime": 380,
      "totalKills": 2200
    }
  },
  "recentGames": [
    {
      "sessionId": 67890,
      "characterType": "Hacker",
      "survivalTime": 450,
      "enemiesKilled": 127,
      "isCleared": false,
      "playedAt": "2026-01-14T16:00:00Z"
    }
    // ... 최근 10게임
  ]
}
```

---

## 게임 플로우

### 전체 시퀀스 다이어그램

```
Client                          Server                         Database
  │                               │                                │
  │ 1. POST /auth/login           │                                │
  ├──────────────────────────────>│ SELECT * FROM Player           │
  │                               ├───────────────────────────────>│
  │                               │<───────────────────────────────┤
  │<──────────────────────────────┤ (player info)                  │
  │                               │                                │
  │ 2. Character Selection        │                                │
  │    (로컬 UI)                   │                                │
  │                               │                                │
  │ 3. POST /game/start           │                                │
  ├──────────────────────────────>│ INSERT INTO GameSession        │
  │                               ├───────────────────────────────>│
  │<──────────────────────────────┤ (sessionId, stats)             │
  │                               │                                │
  │ 4. 게임 플레이                 │                                │
  │    (로컬 실행)                 │                                │
  │    - 적 처치                   │                                │
  │    - 골드 획득                 │                                │
  │    - 생존 시간 측정             │                                │
  │                               │                                │
  │ 5. POST /game/end             │                                │
  ├──────────────────────────────>│ UPDATE GameSession             │
  │                               ├───────────────────────────────>│
  │                               │ UPDATE Player (exp, gold)      │
  │                               ├───────────────────────────────>│
  │                               │ Calculate rewards & level up   │
  │<──────────────────────────────┤ (rewards, new level, rank)     │
  │                               │                                │
  │ 6. GET /game/leaderboard      │                                │
  ├──────────────────────────────>│ Check Redis cache              │
  │                               │ SELECT FROM Player ORDER BY... │
  │<──────────────────────────────┤ (top players)                  │
  │                               │                                │
  │ 7. GET /upgrade/player/:id    │                                │
  ├──────────────────────────────>│ SELECT * FROM PlayerUpgrade    │
  │<──────────────────────────────┤ (upgrade list)                 │
  │                               │                                │
  │ 8. POST /upgrade/.../purchase │                                │
  ├──────────────────────────────>│ UPDATE Player (gold)           │
  │                               │ UPSERT PlayerUpgrade           │
  │<──────────────────────────────┤ (new level, remaining gold)    │
```

---

## 클라이언트-서버 차이점

### ⚠️ 현재 GameServerAPI의 문제점

#### 1. **불필요하거나 미구현된 기능**

현재 클라이언트 API에 있지만 실제 게임에서 사용하지 않는 것:

```csharp
// ❌ 클라이언트에 없음
public int gems;                    // 보석 화폐
public int highestStage;            // 스테이지 시스템
public bool isNewPlayer;            // 사용되지 않음

// ❌ 무기 시스템 (UI는 있으나 서버 연동 X)
public IEnumerator GetWeapons(...)
public IEnumerator UpgradeWeapon(...)
public IEnumerator AcquireWeapon(...)
```

#### 2. **누락된 필드**

서버 API에 추가해야 할 것:

```csharp
// ✅ 필요한 항목
POST /game/start
{
  "characterType": "Hacker"  // ← 현재 API에 없음!
}

POST /game/end
{
  "goldEarned": 250  // ← 클라이언트에서 계산 필요
}
```

#### 3. **비용 계산 공식 불일치**

```javascript
// 서버는 자체 공식 사용해야 함
// 클라이언트는 표시용으로만 참고

// 업그레이드 비용
serverCost = baseCost + (costPerLevel * currentLevel)

// 경험치 계산
baseExp = survivalTime * 0.4
killBonus = enemiesKilled * 10
clearBonus = isCleared ? 500 : 0
```

---

## 보안 고려사항

### 1. 치팅 방지

#### 🔴 **높은 우선순위**

```csharp
// ❌ 클라이언트에서 이런 값들을 신뢰하면 안 됨!
{
  "goldEarned": 999999,      // 조작 가능
  "experienceEarned": 50000, // 조작 가능
  "survivalTime": 10000      // 검증 필요
}
```

**해결책**:
1. **서버 측 계산**: 골드, 경험치는 절대 클라이언트 값 신뢰 X
2. **최대값 검증**: 
   ```javascript
   // 서버에서 확인
   if (survivalTime > sessionDuration * 1.1) {
     // 의심스러운 플레이
     logAnomaly(playerId, "Suspicious survival time")
   }
   
   if (enemiesKilled > survivalTime * 3) {
     // 초당 3킬 이상은 비정상
     logAnomaly(playerId, "Impossible kill rate")
   }
   ```

3. **세션 타임스탬프 검증**:
   ```javascript
   const sessionDuration = endedAt - startedAt
   const reportedDuration = survivalTime
   
   if (Math.abs(sessionDuration - reportedDuration) > 60) {
     // 1분 이상 차이나면 의심
     flagForReview(playerId)
   }
   ```

### 2. Rate Limiting

```javascript
// API별 제한
const rateLimits = {
  '/auth/login': '10 requests per 15 minutes',
  '/game/start': '60 requests per hour',
  '/game/end': '60 requests per hour',
  '/upgrade/purchase': '30 requests per minute'
}
```

### 3. 데이터 검증

```javascript
// 필수 검증 항목
function validateGameEnd(request) {
  const { survivalTime, enemiesKilled, goldEarned } = request
  
  // 범위 검증
  if (survivalTime < 0 || survivalTime > 7200) {
    throw new ValidationError('Invalid survival time')
  }
  
  if (enemiesKilled < 0 || enemiesKilled > 10000) {
    throw new ValidationError('Invalid enemy count')
  }
  
  // 상관관계 검증
  const maxGold = enemiesKilled * 5 + survivalTime * 2
  if (goldEarned > maxGold * 1.5) {
    throw new ValidationError('Suspicious gold amount')
  }
  
  return true
}
```

---

## 추가 구현 권장사항

### 1. 캐싱 전략 (Redis)

```javascript
// 리더보드 캐싱
const leaderboardKey = 'leaderboard:survival:top100'
const TTL = 300  // 5분

async function getLeaderboard() {
  // 캐시 확인
  const cached = await redis.get(leaderboardKey)
  if (cached) return JSON.parse(cached)
  
  // DB 조회
  const leaderboard = await db.query(`
    SELECT playerId, nickname, level, bestSurvivalTime
    FROM Player
    ORDER BY bestSurvivalTime DESC
    LIMIT 100
  `)
  
  // 캐시 저장
  await redis.setex(leaderboardKey, TTL, JSON.stringify(leaderboard))
  
  return leaderboard
}
```

### 2. 일일 보상 시스템

```sql
CREATE TABLE DailyReward (
    id INT PRIMARY KEY AUTO_INCREMENT,
    playerId INT NOT NULL,
    lastClaimDate DATE NOT NULL,
    consecutiveDays INT DEFAULT 1,
    
    FOREIGN KEY (playerId) REFERENCES Player(id),
    UNIQUE KEY unique_player_date (playerId, lastClaimDate)
);
```

**Endpoint**: `POST /reward/daily/claim`

### 3. 업적 시스템

```sql
CREATE TABLE Achievement (
    id INT PRIMARY KEY AUTO_INCREMENT,
    achievementId VARCHAR(50) UNIQUE NOT NULL,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    rewardGold INT DEFAULT 0,
    rewardGems INT DEFAULT 0
);

CREATE TABLE PlayerAchievement (
    id INT PRIMARY KEY AUTO_INCREMENT,
    playerId INT NOT NULL,
    achievementId VARCHAR(50) NOT NULL,
    unlockedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (playerId) REFERENCES Player(id),
    UNIQUE KEY unique_player_achievement (playerId, achievementId)
);
```

### 4. 이벤트/시즌 시스템

```sql
CREATE TABLE Event (
    id INT PRIMARY KEY AUTO_INCREMENT,
    eventName VARCHAR(100) NOT NULL,
    eventType ENUM('DOUBLE_EXP', 'DOUBLE_GOLD', 'SPECIAL_BOSS') NOT NULL,
    multiplier FLOAT DEFAULT 1.0,
    startDate DATETIME NOT NULL,
    endDate DATETIME NOT NULL,
    isActive BOOLEAN DEFAULT TRUE
);
```

### 5. 친구 시스템

```sql
CREATE TABLE Friendship (
    id INT PRIMARY KEY AUTO_INCREMENT,
    playerId INT NOT NULL,
    friendId INT NOT NULL,
    status ENUM('PENDING', 'ACCEPTED', 'BLOCKED') DEFAULT 'PENDING',
    createdAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (playerId) REFERENCES Player(id),
    FOREIGN KEY (friendId) REFERENCES Player(id),
    UNIQUE KEY unique_friendship (playerId, friendId),
    CHECK (playerId != friendId)
);
```

---

## 개발 체크리스트

### Phase 1: 핵심 기능 (MVP)
- [ ] 데이터베이스 스키마 생성
- [ ] 플레이어 인증 (deviceUID 기반)
- [ ] 게임 세션 시작/종료
- [ ] 경험치 & 레벨업 시스템
- [ ] 골드 획득 & 저장
- [ ] 리더보드 (생존시간 기준)
- [ ] 기본 보안 (입력 검증, rate limiting)

### Phase 2: 진행 시스템
- [ ] 업그레이드 시스템
- [ ] 캐릭터 통계 (선택 빈도 등)
- [ ] 플레이어 상세 통계

### Phase 3: 추가 기능
- [ ] 무기 시스템 (획득/업그레이드)
- [ ] 일일 보상
- [ ] 업적 시스템
- [ ] 친구 시스템

### Phase 4: 최적화 & 고도화
- [ ] Redis 캐싱
- [ ] 치팅 탐지 로직
- [ ] 분석 대시보드
- [ ] 로그 시스템
- [ ] 모니터링 (Prometheus + Grafana)

---

## API 테스트 예시

### Postman Collection

```json
{
  "info": {
    "name": "NeoSurvive API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Login",
      "request": {
        "method": "POST",
        "header": [{"key": "Content-Type", "value": "application/json"}],
        "body": {
          "mode": "raw",
          "raw": "{\"deviceUID\": \"test-device-123\"}"
        },
        "url": {
          "raw": "{{baseUrl}}/auth/login",
          "host": ["{{baseUrl}}"],
          "path": ["auth", "login"]
        }
      }
    },
    {
      "name": "Start Game",
      "request": {
        "method": "POST",
        "header": [{"key": "Content-Type", "value": "application/json"}],
        "body": {
          "mode": "raw",
          "raw": "{\"playerId\": 1, \"characterType\": \"Hacker\", \"stage\": 1}"
        },
        "url": {
          "raw": "{{baseUrl}}/game/start",
          "host": ["{{baseUrl}}"],
          "path": ["game", "start"]
        }
      }
    }
  ]
}
```

---

## 참고 자료

### 유사 게임 API 구조
- **Vampire Survivors**: 스팀 리더보드 API
- **Brotato**: 세션 기반 진행 저장
- **Slay the Spire**: 덱 빌딩 저장 시스템

### 추천 라이브러리

**Node.js/Express**:
- `express`: 웹 프레임워크
- `pg` / `mysql2`: 데이터베이스
- `ioredis`: Redis 클라이언트
- `joi`: 입력 검증
- `jsonwebtoken`: JWT 인증
- `rate-limiter-flexible`: Rate limiting

**ASP.NET Core**:
- `Entity Framework Core`: ORM
- `StackExchange.Redis`: Redis
- `FluentValidation`: 검증
- `Serilog`: 로깅
- `AspNetCoreRateLimit`: Rate limiting

---

## 연락처 & 지원

게임 서버 개발 중 문제가 발생하면:
1. 클라이언트 로그 확인 (Unity Console)
2. 서버 로그 확인
3. 데이터베이스 쿼리 로그 분석

**문서 버전**: 1.0.0  
**최종 업데이트**: 2026-01-14
