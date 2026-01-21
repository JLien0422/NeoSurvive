# NeoSurvive 백엔드 서버 API 명세서 (Co-op 멀티플레이어)

## 📋 개요

이 문서는 NeoSurvive Co-op 멀티플레이어 기능을 위한 백엔드 서버 API 명세입니다.

### 통신 방식

- **HTTP REST API**: 캐릭터 정보, 세션 관리, 영구 저장
- **WebSocket**: 실시간 게임 상태 동기화 (위치, 적, 아이템 등)

### 직렬화 방식

- **Easy Save 3 (ES3)** 직렬화 형식 사용
- 포맷: JSON (ES3.Format.JSON)
- 모든 네트워크 데이터는 ES3SerializationHelper를 통해 직렬화/역직렬화

---

## 🔐 인증 (Auth) - 기존 유지

기존 GameServerAPI의 인증 시스템 유지:

- `POST /api/auth/login`
- `GET /api/auth/player/{playerId}`
- `PUT /api/auth/player/{playerId}/nickname`

---

## 🎮 Co-op 세션 관리 (Co-op Session Management)

### 1. Co-op 세션 생성

**엔드포인트:** `POST /api/coop/session/create`

**요청 (Request):**

```json
{
  "hostPlayerId": 123,
  "characterType": "Hacker",
  "stage": 1,
  "maxPlayers": 2
}
```

**응답 (Response):**

```json
{
  "sessionId": 456,
  "websocketUrl": "ws://localhost:5157/ws/game/456",
  "sessionCode": "ABC123",
  "createdAt": "2026-01-20T10:30:00Z"
}
```

**비고:**

- `sessionCode`: 6자리 영문 대문자 (다른 플레이어가 참가할 때 사용)
- `websocketUrl`: WebSocket 연결 주소

---

### 2. Co-op 세션 참가

**엔드포인트:** `POST /api/coop/session/join`

**요청 (Request):**

```json
{
  "playerId": 789,
  "sessionCode": "ABC123",
  "characterType": "Cyborg"
}
```

**응답 (Response):**

```json
{
  "success": true,
  "sessionId": 456,
  "websocketUrl": "ws://localhost:5157/ws/game/456",
  "currentState": {
    "sessionId": 456,
    "hostPlayerId": "123",
    "players": [
      {
        "playerId": 123,
        "nickname": "Player1",
        "position": { "x": 0.0, "y": 0.0 },
        "velocity": { "x": 0.0, "y": 0.0 },
        "rotation": 0.0,
        "health": 100.0,
        "maxHealth": 100.0,
        "level": 1,
        "experience": 0,
        "isAlive": true,
        "lastUpdateTimestamp": 1706342400000,
        "activeWeapons": [],
        "activeBuffs": []
      }
    ],
    "enemies": [],
    "dropItems": [],
    "gameTime": 0.0,
    "waveNumber": 1,
    "totalKills": 0,
    "isGameActive": true,
    "lastSyncTimestamp": 1706342400000
  },
  "errorMessage": null
}
```

**에러 응답:**

```json
{
  "success": false,
  "sessionId": -1,
  "websocketUrl": null,
  "currentState": null,
  "errorMessage": "세션을 찾을 수 없습니다."
}
```

**비고:**

- `currentState`: 현재 진행 중인 게임 상태 (참가 시 동기화)
- 최대 인원 초과 시 에러 반환

---

### 3. 세션 저장 (체크포인트)

**엔드포인트:** `POST /api/coop/session/save`

**요청 (Request):**

```json
{
  "sessionId": 456,
  "sessionState": {
    "sessionId": 456,
    "hostPlayerId": "123",
    "players": [...],
    "enemies": [...],
    "dropItems": [...],
    "gameTime": 120.5,
    "waveNumber": 3,
    "totalKills": 45,
    "isGameActive": true,
    "lastSyncTimestamp": 1706342520000
  }
}
```

**응답 (Response):**

```json
{
  "success": true,
  "checkpointId": "ckpt_abc123def456",
  "errorMessage": null
}
```

**비고:**

- 게임 종료 시 또는 주기적으로 체크포인트 저장
- `sessionState`는 ES3로 직렬화된 전체 게임 상태

---

### 4. 세션 목록 조회

**엔드포인트:** `GET /api/coop/session/list?playerId={playerId}`

**응답 (Response):**

```json
{
  "sessions": [
    {
      "sessionId": 456,
      "sessionCode": "ABC123",
      "hostPlayerId": 123,
      "playerCount": 2,
      "maxPlayers": 2,
      "stage": 1,
      "isActive": true,
      "createdAt": "2026-01-20T10:30:00Z"
    }
  ]
}
```

---

## 🔌 WebSocket 통신 (Real-time Game Sync)

### WebSocket 연결

**URL 형식:** `ws://[서버주소]/ws/game/{sessionId}?playerId={playerId}`

**예시:** `ws://localhost:5157/ws/game/456?playerId=123`

---

### WebSocket 메시지 포맷

모든 메시지는 JSON 형식이며, `messageType` 필드로 메시지 타입을 구분합니다.

**베이스 메시지 구조:**

```json
{
  "messageType": "MessageType",
  "timestamp": 1706342400000
}
```

---

### 1. 플레이어 위치 업데이트 (빈번)

**메시지 타입:** `PlayerPosition`

**Client → Server:**

```json
{
  "messageType": "PlayerPosition",
  "timestamp": 1706342400000,
  "playerId": 123,
  "x": 10.5,
  "y": 20.3,
  "vx": 1.0,
  "vy": 0.5,
  "rot": 45.0
}
```

**Server → Clients (브로드캐스트):**
동일한 메시지를 다른 클라이언트들에게 전송

**전송 빈도:** 0.1초마다 (초당 10회)

---

### 2. 플레이어 전체 상태 업데이트

**메시지 타입:** `PlayerState`

**Client → Server:**

```json
{
  "messageType": "PlayerState",
  "timestamp": 1706342400000,
  "playerState": {
    "playerId": 123,
    "nickname": "Player1",
    "position": { "x": 10.5, "y": 20.3 },
    "velocity": { "x": 1.0, "y": 0.5 },
    "rotation": 45.0,
    "health": 85.0,
    "maxHealth": 100.0,
    "level": 2,
    "experience": 150,
    "isAlive": true,
    "lastUpdateTimestamp": 1706342400000,
    "activeWeapons": [
      {
        "weaponId": "wpn_001",
        "weaponType": "Pistol",
        "level": 2,
        "isActive": true,
        "position": null,
        "rotation": null
      }
    ],
    "activeBuffs": []
  }
}
```

**전송 빈도:** 1초마다

---

### 3. 적 스폰 (호스트만 전송)

**메시지 타입:** `EnemySpawn`

**Host → Server → Clients:**

```json
{
  "messageType": "EnemySpawn",
  "timestamp": 1706342400000,
  "enemy": {
    "enemyId": 1001,
    "enemyType": "Drone",
    "position": { "x": 50.0, "y": 50.0 },
    "velocity": { "x": 0.0, "y": 0.0 },
    "rotation": 0.0,
    "health": 100.0,
    "maxHealth": 100.0,
    "isAlive": true,
    "targetPlayerId": 123
  }
}
```

---

### 4. 적 상태 업데이트 (호스트만 전송)

**메시지 타입:** `EnemyUpdate`

**Host → Server → Clients:**

```json
{
  "messageType": "EnemyUpdate",
  "timestamp": 1706342400000,
  "enemies": [
    {
      "enemyId": 1001,
      "enemyType": "Drone",
      "position": { "x": 55.0, "y": 52.0 },
      "velocity": { "x": 2.0, "y": 1.0 },
      "rotation": 30.0,
      "health": 80.0,
      "maxHealth": 100.0,
      "isAlive": true,
      "targetPlayerId": 123
    }
  ]
}
```

**전송 빈도:** 0.2초마다 (초당 5회)

---

### 5. 적 사망

**메시지 타입:** `EnemyDeath`

**Client → Server → Clients:**

```json
{
  "messageType": "EnemyDeath",
  "timestamp": 1706342400000,
  "enemyId": 1001,
  "killerPlayerId": 123
}
```

---

### 6. 아이템 드랍 (호스트만 전송)

**메시지 타입:** `ItemDrop`

**Host → Server → Clients:**

```json
{
  "messageType": "ItemDrop",
  "timestamp": 1706342400000,
  "item": {
    "itemId": 2001,
    "itemType": "ExpGem",
    "position": { "x": 55.0, "y": 52.0 },
    "value": 10,
    "isCollected": false
  }
}
```

---

### 7. 아이템 수집

**메시지 타입:** `ItemCollect`

**Client → Server → Clients:**

```json
{
  "messageType": "ItemCollect",
  "timestamp": 1706342400000,
  "itemId": 2001,
  "collectorPlayerId": 123
}
```

---

### 8. 플레이어 데미지

**메시지 타입:** `PlayerDamage`

**Client → Server → Clients:**

```json
{
  "messageType": "PlayerDamage",
  "timestamp": 1706342400000,
  "playerId": 123,
  "damage": 15.0,
  "remainingHealth": 70.0,
  "attackerId": 1001
}
```

---

### 9. 웨이브 시작 (호스트만 전송)

**메시지 타입:** `WaveStart`

**Host → Server → Clients:**

```json
{
  "messageType": "WaveStart",
  "timestamp": 1706342400000,
  "waveNumber": 3,
  "enemyCount": 20
}
```

---

### 10. 게임 오버

**메시지 타입:** `GameOver`

**Host → Server → Clients:**

```json
{
  "messageType": "GameOver",
  "timestamp": 1706342400000,
  "survivalTime": 320.5,
  "totalKills": 150,
  "isCleared": false
}
```

---

### 11. 채팅 메시지

**메시지 타입:** `Chat`

**Client → Server → Clients:**

```json
{
  "messageType": "Chat",
  "timestamp": 1706342400000,
  "senderId": 123,
  "senderNickname": "Player1",
  "message": "안녕하세요!"
}
```

---

### 12. 핑 (연결 유지)

**메시지 타입:** `Ping`

**Client → Server (응답 없음):**

```json
{
  "messageType": "Ping",
  "timestamp": 1706342400000
}
```

**전송 빈도:** 30초마다

---

## 🗂️ 데이터베이스 스키마

### 1. CoopSessions 테이블

```sql
CREATE TABLE CoopSessions (
    SessionId INT PRIMARY KEY AUTO_INCREMENT,
    SessionCode VARCHAR(6) UNIQUE NOT NULL,
    HostPlayerId INT NOT NULL,
    Stage INT NOT NULL DEFAULT 1,
    MaxPlayers INT NOT NULL DEFAULT 2,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (HostPlayerId) REFERENCES Players(Id)
);

CREATE INDEX idx_session_code ON CoopSessions(SessionCode);
CREATE INDEX idx_host_player ON CoopSessions(HostPlayerId);
```

---

### 2. SessionCheckpoints 테이블

```sql
CREATE TABLE SessionCheckpoints (
    CheckpointId VARCHAR(50) PRIMARY KEY,
    SessionId INT NOT NULL,
    GameState LONGTEXT NOT NULL, -- ES3 직렬화된 JSON
    GameTime FLOAT NOT NULL,
    WaveNumber INT NOT NULL,
    TotalKills INT NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (SessionId) REFERENCES CoopSessions(SessionId)
);

CREATE INDEX idx_session_checkpoints ON SessionCheckpoints(SessionId);
```

---

### 3. SessionPlayers 테이블 (참가자 기록)

```sql
CREATE TABLE SessionPlayers (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    SessionId INT NOT NULL,
    PlayerId INT NOT NULL,
    CharacterType VARCHAR(20) NOT NULL,
    JoinedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    LeftAt TIMESTAMP NULL,
    FOREIGN KEY (SessionId) REFERENCES CoopSessions(SessionId),
    FOREIGN KEY (PlayerId) REFERENCES Players(Id)
);

CREATE INDEX idx_session_players ON SessionPlayers(SessionId);
```

---

## 📊 서버 구현 체크리스트

### HTTP API 구현

- [ ] **POST /api/coop/session/create**
  - [ ] 세션 생성 (DB 저장)
  - [ ] 6자리 세션 코드 생성 (중복 체크)
  - [ ] WebSocket URL 생성
  - [ ] 세션 ID 반환

- [ ] **POST /api/coop/session/join**
  - [ ] 세션 코드로 세션 검색
  - [ ] 최대 인원 체크
  - [ ] 플레이어 참가 기록
  - [ ] 현재 게임 상태 반환

- [ ] **POST /api/coop/session/save**
  - [ ] 게임 상태 ES3 직렬화 검증
  - [ ] 체크포인트 DB 저장
  - [ ] 체크포인트 ID 생성 및 반환

- [ ] **GET /api/coop/session/list**
  - [ ] 플레이어가 참가한 세션 목록 조회
  - [ ] 활성 세션만 필터링

---

### WebSocket 구현

- [ ] **연결 관리**
  - [ ] WebSocket 서버 설정 (포트: 5157 또는 별도 포트)
  - [ ] 세션별 연결 관리
  - [ ] 플레이어별 연결 매핑

- [ ] **메시지 브로드캐스팅**
  - [ ] 세션 내 모든 클라이언트에게 메시지 전달
  - [ ] 발신자 제외 옵션
  - [ ] 메시지 타입별 라우팅

- [ ] **메시지 핸들러**
  - [ ] `PlayerPosition` - 위치 업데이트 브로드캐스트
  - [ ] `PlayerState` - 전체 상태 브로드캐스트
  - [ ] `EnemySpawn` - 적 스폰 브로드캐스트 (호스트 검증)
  - [ ] `EnemyUpdate` - 적 업데이트 브로드캐스트 (호스트 검증)
  - [ ] `EnemyDeath` - 적 사망 브로드캐스트
  - [ ] `ItemDrop` - 아이템 드랍 브로드캐스트 (호스트 검증)
  - [ ] `ItemCollect` - 아이템 수집 브로드캐스트
  - [ ] `PlayerDamage` - 플레이어 데미지 브로드캐스트
  - [ ] `WaveStart` - 웨이브 시작 브로드캐스트 (호스트 검증)
  - [ ] `GameOver` - 게임 오버 브로드캐스트
  - [ ] `Chat` - 채팅 브로드캐스트
  - [ ] `Ping` - 연결 유지 (응답 안함)

- [ ] **권한 검증**
  - [ ] 호스트 전용 메시지 검증 (EnemySpawn, ItemDrop 등)
  - [ ] 플레이어 ID 일치 검증

---

### 인메모리 상태 관리 (Redis 권장)

- [ ] **세션 상태 캐싱**
  - [ ] 활성 세션 목록
  - [ ] 세션별 플레이어 목록
  - [ ] WebSocket 연결 매핑

- [ ] **게임 상태 캐싱**
  - [ ] 최신 게임 상태 (SessionId → GameSessionState)
  - [ ] TTL 설정 (세션 종료 후 1시간)

---

### 성능 최적화

- [ ] **메시지 압축**
  - [ ] WebSocket 메시지 Gzip 압축 (선택)
  - [ ] ES3 압축 활용

- [ ] **메시지 배칭**
  - [ ] 위치 업데이트 배칭 (100ms 간격)
  - [ ] 적 업데이트 배칭 (200ms 간격)

- [ ] **스로틀링**
  - [ ] 플레이어당 초당 메시지 제한
  - [ ] 비정상 트래픽 감지 및 차단

---

## 🔒 보안 고려사항

### 1. WebSocket 인증

- [ ] 연결 시 플레이어 ID 검증
- [ ] JWT 토큰 사용 (선택)

### 2. 메시지 검증

- [ ] 호스트 권한 검증
- [ ] 메시지 타입 화이트리스트
- [ ] 데이터 크기 제한

### 3. 치팅 방지

- [ ] 호스트가 적/아이템 관리
- [ ] 서버 측 히트 검증 (선택)
- [ ] 비정상 행동 감지

---

## 📦 필요한 서버 라이브러리

### Node.js (Express)

```bash
npm install express
npm install ws # WebSocket
npm install mysql2 # MySQL
npm install ioredis # Redis
npm install joi # 데이터 검증
```

### ASP.NET Core (C#)

```bash
dotnet add package Microsoft.AspNetCore.WebSockets
dotnet add package StackExchange.Redis
dotnet add package MySql.Data
dotnet add package Newtonsoft.Json
```

---

## 🚀 배포 가이드

### 1. 개발 환경

- HTTP: `http://localhost:5157/api`
- WebSocket: `ws://localhost:5157/ws`

### 2. 프로덕션 환경

- HTTP: `https://api.neosurvive.com/api`
- WebSocket: `wss://api.neosurvive.com/ws`
- SSL 인증서 필요

---

## 📚 참고 문서

- [Easy Save 3 문서](https://docs.moodkie.com/easy-save-3/)
- [NativeWebSocket Unity 패키지](https://github.com/endel/NativeWebSocket)
- [Unity UnityWebRequest](https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest.html)

---

## ✅ 테스트 시나리오

### 1. 기본 연결 테스트

- [ ] 세션 생성 및 WebSocket 연결
- [ ] 세션 참가 및 WebSocket 연결
- [ ] 동시 2명 플레이어 연결

### 2. 동기화 테스트

- [ ] 플레이어 위치 동기화
- [ ] 적 스폰 및 이동 동기화
- [ ] 아이템 드랍 및 수집 동기화

### 3. 게임 플레이 테스트

- [ ] 2명 플레이어 협동 게임
- [ ] 웨이브 진행
- [ ] 게임 오버 처리

### 4. 재연결 테스트

- [ ] 네트워크 끊김 후 재연결
- [ ] 게임 상태 복구

---

**작성일:** 2026-01-20  
**버전:** 1.0.0  
**작성자:** NeoSurvive Development Team
