# Hacker 무기 이벤트 프로토콜 (서버 개발용)

이 문서는 **Hacker 클래스 무기**에서 서버로 전송하는 무기 이벤트 형식을 정의합니다.
클라이언트는 **로컬에서만 렌더링/피해 판정**을 수행하며, 서버는 **이벤트를 기록/검증/재전파** 용도로 사용합니다.

## 공통 규칙

- 모든 무기 이벤트는 `PlayerAction`에 담김
- `action_type` = `WEAPON_USE`
- `weapon_type` = 무기 ID (`WeaponBase.weaponId`)를 사용
- `pos_x`, `pos_y`는 **발동 기준 위치**
- `dir_x`, `dir_y`는 **발동 기준 방향** (없는 경우 0)
- 무기별 상세 데이터는 `weapon_payload`의 **oneof** 필드로 전송

## Payload 타입 정의

`weapon_payload`는 다음 중 하나만 설정됩니다.

- `weapon_projectile` : 투사체형
- `weapon_beam` : 레이저/빔
- `weapon_cone` : 부채꼴 범위
- `weapon_area` : 장판/영역
- `weapon_summon` : 소환형
- `weapon_trap` : 설치형 트랩

---

## 1) LinkPistol

- 타입: **투사체형**
- payload: `weapon_projectile`

필드:

- `spawn_x`, `spawn_y` : 총알 생성 위치 (현재 무기 위치)
- `dir_x`, `dir_y` : 발사 방향
- `speed` : `bulletSpeed`
- `range` : `range`
- `penetration` : 0
- `target_id` : 0

---

## 2) PlasmaRifle

- 타입: **빔형**
- payload: `weapon_beam`

필드:

- `start_x`, `start_y` : 레이저 시작 위치
- `dir_x`, `dir_y` : 레이저 방향
- `duration` : `PlasmaLazer.duration`
- `speed` : `PlasmaLazer.bulletSpeed`
- `penetration` : `currentPenetration`

---

## 3) AIDrone

- 타입: **투사체형** (드론이 발사)
- payload: `weapon_projectile`

필드:

- `spawn_x`, `spawn_y` : 드론 발사 위치
- `dir_x`, `dir_y` : 드론 → 타깃 방향
- `speed` : 20
- `range` : `detectionRange`
- `penetration` : 0
- `target_id` : 0

---

## 4) AutoTurret

- 타입: **소환형** (터렛 생성기 → DeployedTurret)
- payload: `weapon_summon`

필드:

- `spawn_x`, `spawn_y` : 터렛 생성 위치
- `duration` : `lifeTime`
- `hp` : 0 (터렛 체력 미사용)
- `spawn_prison` : false
- `prison_duration` : 0

---

## 5) DataScrambler

- 타입: **부채꼴 범위형**
- payload: `weapon_cone`

필드:

- `origin_x`, `origin_y` : 시전자 위치
- `dir_x`, `dir_y` : 공격 방향
- `angle` : `angle`
- `range` : `range`
- `status_duration` : `duration` (혼란 지속시간)

---

## 6) EMPPulseGenerator

- 타입: **장판형**
- payload: `weapon_area`

필드:

- `center_x`, `center_y` : 장판 중심
- `radius` : `areaSize`
- `duration` : `duration`
- `tick_rate` : 0.5
- `destroy_projectiles` : `level >= 5`

---

## 7) HologramDecoyGenerator

- 타입: **소환형** (디코이 생성)
- payload: `weapon_summon`

필드:

- `spawn_x`, `spawn_y` : 소환 위치
- `duration` : 0 (디코이는 체력 기반)
- `hp` : `hp`
- `spawn_prison` : `level >= 5`
- `prison_duration` : `prisonDuration`

---

## 8) NanoWireGenerator

- 타입: **트랩형**
- payload: `weapon_trap`

필드:

- `spawn_x`, `spawn_y` : 설치 위치
- `duration` : `duration`
- `activation_delay` : 1.0
- `tick_rate` : 0.2
- `radius` : 0.5

---

## 서버 처리 가이드

- **검증**: 서버는 `weapon_type`과 payload 타입이 일치하는지 확인
- **재전파**: 클라이언트 간 동기화를 원하면 `PlayerAction` 그대로 브로드캐스트
- **이펙트 기록**: 로그/리플레이/치트 탐지에 payload 활용

## 참고

- `PlayerAction`의 `value`는 사용하지 않음 (0)
- 클라이언트는 **로컬에서만 실제 피해 판정** 수행
- 서버는 필요 시 독자적 판정으로 전환 가능
