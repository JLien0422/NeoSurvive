# NeoSurvive 서버 개발 가이드

## 📚 문서 목록

### 1. [GameServerSpecification.md](./GameServerSpecification.md)
**완전한 게임 서버 API 명세서**
- 데이터베이스 스키마 (Player, GameSession, Upgrades 등)
- 모든 REST API 엔드포인트 상세 정의
- 요청/응답 예시
- 게임 플로우 시퀀스 다이어그램
- 보안 고려사항
- 클라이언트-서버 차이점 분석

## 🎮 게임 개요

**NeoSurvive**는 2D 로그라이크 생존 액션 게임입니다.

### 핵심 기능
- **캐릭터 선택**: 해커 (원거리/빠름) vs 사이보그 (근접/탱커)
- **웨이브 서바이벌**: 시간이 지날수록 강해지는 적들을 상대
- **진행 시스템**: 경험치, 레벨업, 골드 획득
- **영구 업그레이드**: 골드로 스탯 강화
- **리더보드**: 생존 시간 경쟁

## 🚀 빠른 시작

### 최소 구현 (MVP)

1. **데이터베이스 설정**
   ```sql
   -- GameServerSpecification.md의 "데이터 모델" 섹션 참조
   CREATE TABLE Player (...);
   CREATE TABLE GameSession (...);
   CREATE TABLE PlayerUpgrade (...);
   ```

2. **필수 API 구현**
   - `POST /auth/login` - 디바이스 UID 기반 로그인
   - `POST /game/start` - 게임 세션 시작
   - `POST /game/end` - 게임 종료 & 보상 지급
   - `GET /game/leaderboard` - 순위표 조회

3. **보상 계산 로직**
   ```javascript
   // 경험치
   exp = (survivalTime * 0.4) + (enemiesKilled * 10)
   
   // 골드 (클라이언트 값 검증, 서버에서 재계산 권장)
   gold = enemiesKilled * 2 + survivalTime * 1
   ```

## ⚠️ 주의사항

### 클라이언트 검증 필수
```javascript
// ❌ 절대 클라이언트 값을 그대로 신뢰하지 말 것!
app.post('/game/end', (req, res) => {
  const { goldEarned, experienceEarned } = req.body
  
  // ✅ 서버에서 재계산
  const calculatedGold = calculateGold(survivalTime, kills)
  const calculatedExp = calculateExp(survivalTime, kills)
  
  // 차이가 크면 경고
  if (Math.abs(goldEarned - calculatedGold) > 50) {
    logSuspiciousActivity(playerId)
  }
})
```

### 현재 클라이언트 미구현 항목
- ❌ 보석(Gem) 화폐
- ❌ 스테이지 시스템 (현재 단일 맵)
- ❌ 무기 서버 연동 (UI는 있음)
- ❌ 닉네임 변경

## 📊 게임 통계 (클라이언트)

### 현재 추적 중인 데이터
```csharp
// GameManager.cs
- killCount           // 적 처치 수
- gameTime           // 생존 시간 (초)
- currentRunGold     // 현재 플레이 골드
- selectedCharacter  // 선택 캐릭터

// Player.cs
- experience         // 경험치
- level             // 레벨
- health            // 체력
```

### 캐릭터 스탯
```
해커 (Hacker):
  체력: 80
  공격력: 15
  공격 범위: 5
  공격 속도: 120%
  이동 속도: 6

사이보그 (Cyborg):
  체력: 150
  공격력: 20
  공격 범위: 2
  공격 속도: 80%
  이동 속도: 4
```

## 🔐 보안 체크리스트

- [ ] Rate limiting 구현
- [ ] 입력 검증 (survivalTime, kills 범위)
- [ ] 세션 타임스탬프 검증
- [ ] 골드/경험치 서버 측 계산
- [ ] 의심스러운 활동 로깅
- [ ] SQL Injection 방지
- [ ] HTTPS 사용

## 🛠️ 개발 도구

### 권장 스택
- **백엔드**: Node.js (Express) 또는 ASP.NET Core
- **데이터베이스**: PostgreSQL 또는 MySQL
- **캐싱**: Redis
- **테스트**: Postman, Thunder Client

### 환경 변수 예시
```env
DATABASE_URL=postgresql://user:pass@localhost:5432/neosurvive
REDIS_URL=redis://localhost:6379
JWT_SECRET=your-secret-key
PORT=5157
```

## 📈 개발 로드맵

### Phase 1: 핵심 (1-2주)
- [x] 데이터베이스 스키마
- [ ] 인증 시스템
- [ ] 게임 세션 관리
- [ ] 기본 리더보드

### Phase 2: 진행 시스템 (1주)
- [ ] 업그레이드 시스템
- [ ] 레벨업 & 경험치
- [ ] 골드 관리

### Phase 3: 추가 기능 (2-3주)
- [ ] Redis 캐싱
- [ ] 상세 통계
- [ ] 치팅 방지
- [ ] 일일 보상

## 🐛 알려진 이슈

### GameServerAPI.cs
1. **캐릭터 타입 누락**: `POST /game/start`에 characterType 파라미터 필요
2. **무기 시스템**: 클라이언트와 연동되지 않음 (UI만 존재)
3. **보석(Gem)**: 서버 API에 있지만 클라이언트 미사용
4. **스테이지**: 항상 stage=1 (다중 스테이지 미구현)

## 📞 지원

문제 발생 시:
1. `GameServerSpecification.md` 상세 문서 확인
2. 클라이언트 로그 분석 (Unity Console)
3. 서버 로그 확인

---

**문서 작성**: 2026-01-14  
**게임 버전**: 1.0.0
