# 멀티플레이 네트워크 기능 구현 현황 점검

> 작성일: 2026-02-20  
> 대상 코드: `Assets/Scripts/Multiplayer/UDPClient.cs`, `Assets/Scripts/Network/EnemyProxy.cs`, `Assets/Scripts/Network/ItemProxy.cs`, `ProtoFiles/GamePacket.proto` 등

---

## 요약표

| #   | 기능                                        | 상태             | 비고                                         |
| --- | ------------------------------------------- | ---------------- | -------------------------------------------- |
| 1   | 데드 레코닝 (Dead Reckoning)                | ⚠️ 데이터만 준비 | 속도 필드 전송하지만 수신 측 미활용          |
| 2   | 클라이언트 예측 (Client-side Prediction)    | ✅ 사실상 구현   | 클라이언트 권한 이동 (서버 권위 없음)        |
| 3   | 서버 리컨실리에이션 (Server Reconciliation) | ❌ 미구현        | 로컬 플레이어 스냅샷 건너뜀                  |
| 4   | 엔티티 인터폴레이션 (Entity Interpolation)  | ⚠️ 유사 구현     | Lerp chase 방식 (진정한 시간 기반 보간 아님) |
| 5   | 시퀀스 번호 기반 패킷 보정                  | ❌ 미구현        | 패킷에 시퀀스 필드 없음                      |
| 6   | 주요 데이터 중복 전송                       | ❌ 미구현        | 매 패킷 현재 값만 1회 전송                   |
| 7   | 패킷 집합화 (Packet Aggregation)            | ⚠️ 서버→클라만   | 클라→서버는 이벤트별 개별 전송               |
| 8   | 슬라이딩 윈도우 (Flow Control)              | ❌ 미구현        | 흐름 제어 로직 없음                          |
| 9   | MTU 단편화 처리                             | ❌ 미구현        | 분할/재조립 로직 없음                        |
| 10  | 멱등성 설계 (Idempotent Design)             | ⚠️ 부분적        | 스냅샷은 절대값, 일부 액션은 명령형          |
| 11  | Reliable UDP 채널                           | ❌ 미구현        | ACK/재전송 시스템 없음                       |

---

## 상세 분석

### 1. 데드 레코닝 (Dead Reckoning) — ⚠️ 데이터만 준비

**정의**: 마지막으로 수신한 속도/방향을 기반으로 다음 스냅샷이 올 때까지 엔티티의 위치를 예측(외삽)하는 기법.

**현재 상태**:

- ✅ Proto에 속도 필드 정의: `PlayerMove.vel_x/vel_y`에 "추측 항법(Dead Reckoning)을 위한 속도"라는 주석이 있음
- ✅ 전송 시 속도 포함: `UDPClient.SendPlayerData()`에서 `move.VelX = rb.velocity.x` 로 전송
- ❌ **수신 측에서 속도 미활용**: `HandleSnapshot()`은 `pState.PosX/PosY`만 사용하여 `targetPosition` 설정
- ❌ `UpdateRemotePlayerPositions()`는 단순 Lerp만 수행, 속도 기반 외삽(extrapolation) 없음
- ❌ `EnemyProxy.UpdateState()`도 `state.VelX/VelY`를 무시하고 위치만 사용

**근거 코드**:

```csharp
// UDPClient.cs - HandleSnapshot()
remotePlayerData[playerId].targetPosition = new Vector2(pState.PosX, pState.PosY);
// → pState.VelX, pState.VelY는 어디에서도 사용되지 않음

// UDPClient.cs - UpdateRemotePlayerPositions()
player.transform.position = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * lerpSpeed);
// → 목표 위치를 향해 추격(chase)할 뿐, 속도 기반 예측 없음
```

**구현 시 필요한 작업**:

- `RemotePlayerData`에 `velocity`, `lastTimestamp` 필드 추가
- 스냅샷 간 간격 동안 `position += velocity * deltaTime`으로 외삽
- 새 스냅샷 도착 시 예측 위치와 실제 위치 차이를 부드럽게 보정

---

### 2. 클라이언트 예측 (Client-side Prediction) — ✅ 사실상 구현

**정의**: 로컬 플레이어의 입력을 서버 응답 없이 즉시 적용하여 반응성을 높이는 기법.

**현재 상태**:

- ✅ `PlayerController.cs`에서 `Input.GetAxisRaw`로 입력 → `rb.velocity`에 즉시 적용
- ✅ 서버 응답을 기다리지 않고 렌더링
- ⚠️ 단, 진정한 "예측"이 아닌 **클라이언트 완전 권한** 방식

**근거 코드**:

```csharp
// PlayerController.cs
void Update()  { moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); }
void FixedUpdate() { rb.velocity = moveInput * player.CurrentMoveSpeed; }
// → 서버 확인 없이 즉시 이동
```

**참고**: 진정한 클라이언트 예측은 서버가 위치 권위(authority)를 가지면서 클라이언트가 미리 움직이는 것. 현재는 서버가 위치를 교정하지 않으므로 예측/보정이 불필요한 구조. **치트(위치 조작)에 취약함.**

---

### 3. 서버 리컨실리에이션 (Server Reconciliation) — ❌ 미구현

**정의**: 서버로부터 받은 권위적 상태(authoritative state)와 클라이언트 예측 결과를 비교하여 오차를 수정하는 기법.

**현재 상태**:

- ❌ 로컬 플레이어를 스냅샷 보정에서 명시적 제외: `if (player == _localPlayer) continue;`
- ❌ 입력 히스토리(Input Buffer) 없음
- ❌ 시퀀스 번호 기반 입력-상태 매칭 없음
- ❌ 서버 위치 vs 클라이언트 위치 비교/보정 로직 없음

**근거 코드**:

```csharp
// UDPClient.cs - UpdateRemotePlayerPositions()
if (player == _localPlayer) continue; // ← 로컬 플레이어 보정 완전 스킵
```

**구현 시 필요한 작업**:

- 매 입력 프레임에 시퀀스 번호를 부여하고 입력 히스토리 저장
- 서버 스냅샷에 "마지막 처리한 입력 시퀀스 번호" 포함
- 서버 위치와 해당 시퀀스의 클라이언트 예측 위치 비교
- 오차가 임계값 초과 시 입력 히스토리를 재시뮬레이션(replay)하여 보정

---

### 4. 엔티티 인터폴레이션 (Entity Interpolation) — ⚠️ 유사 구현

**정의**: 과거 두 개의 스냅샷 사이를 시간 기반으로 보간하여 부드러운 움직임을 만드는 기법. 버퍼에 스냅샷을 쌓아두고 의도적으로 약간 과거의 상태를 렌더링함.

**현재 상태**:

- ✅ 원격 플레이어/적/아이템 모두 `Vector3.Lerp`로 위치를 부드럽게 보간
- ❌ 진정한 시간 기반 interpolation이 아닌 **exponential smoothing (chase target)** 방식
- ❌ 스냅샷 버퍼 없음 (과거 상태 기록 없음)
- ❌ 렌더 시간(render time = current - interpolation_delay) 개념 없음

**근거 코드**:

```csharp
// UDPClient.cs - 원격 플레이어
player.transform.position = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * lerpSpeed);

// EnemyProxy.cs - 적
transform.position = Vector3.Lerp(transform.position, targetV3, Time.deltaTime * syncSpeed);

// ItemProxy.cs - 아이템
transform.position = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * syncSpeed);
```

**현재 방식의 문제점**:

- `Lerp(current, target, dt * speed)` 는 목표에 가까워질수록 느려지므로 정확히 도달하지 않음
- 스냅샷 간격이 불규칙하면 속도가 불안정해짐
- 진정한 interpolation은 `Lerp(snapshotA, snapshotB, t)` (t = 0→1, 시간비율)

**구현 시 필요한 작업**:

- `SnapshotBuffer` 구현 (최근 N개 스냅샷 저장)
- 렌더 시간 = 현재 서버 시간 - interpolation_delay (예: 100ms)
- 렌더 시간 전후의 두 스냅샷을 찾아 시간 비율로 `Vector3.Lerp`

---

### 5. 시퀀스 번호를 통한 패킷 보정 — ❌ 미구현

**정의**: 각 패킷에 증가하는 시퀀스 번호를 부여하여 패킷 손실 감지, 순서 보정, 중복 필터링을 하는 기법.

**현재 상태**:

- ❌ `GamePacket` proto에 시퀀스 번호 필드 없음
- ❌ 패킷 순서 보정 로직 없음
- ❌ 손실 패킷 감지 또는 재전송 요청 없음
- ❌ 중복 패킷 필터링 없음
- `PlayerMove.timestamp`는 존재하지만 시퀀스 번호 용도로 사용되지 않음

**구현 시 필요한 작업**:

- `GamePacket`에 `uint32 sequence_number` 필드 추가
- 송신 측: 매 패킷 전송 시 시퀀스 번호 증가
- 수신 측: 마지막 처리한 시퀀스 번호 기록
  - 이전 번호보다 낮으면 무시 (순서 역전)
  - 차이가 크면 손실 감지

---

### 6. 주요 데이터 중복 전송 — ❌ 미구현

**정의**: 손실 가능성이 높은 UDP에서 최근 N개 프레임의 데이터를 매 패킷에 함께 보내 손실을 보상하는 기법.

**현재 상태**:

- ❌ 각 `PlayerMove` 패킷은 현재 프레임 데이터만 포함
- ❌ 이전 프레임 데이터를 함께 보내는 로직 없음
- ⚠️ `GameSnapshot`이 전체 상태를 주기적으로 보내는 것이 간접적 보상 역할은 함

**구현 시 필요한 작업**:

- 전송 시 최근 2~3개 입력/상태를 어레이로 묶어 전송
- 예: `repeated PlayerMove recent_moves = N;` (최근 3프레임)
- 수신 측은 시퀀스 번호로 중복 필터링

---

### 7. 패킷 집합화 (Packet Aggregation) — ⚠️ 서버→클라이언트만

**정의**: 여러 이벤트/데이터를 개별 패킷이 아닌 하나의 패킷으로 묶어 전송하여 UDP 헤더(28바이트) 오버헤드를 줄이는 기법.

**현재 상태**:

- ✅ **서버→클라이언트**: `GameSnapshot` 메시지가 `player_states`, `enemy_states`, `item_states`, `player_statuses`를 하나에 묶어 전송
- ❌ **클라이언트→서버**: 이벤트마다 개별 `client.Send()` 호출
  - `SendPlayerData()` → 매 33ms 개별 전송
  - `SendAction()` → 이벤트 발생 시 즉시 개별 전송
  - `SendWeaponAction()` → 이벤트 발생 시 즉시 개별 전송

**근거 코드**:

```csharp
// 클라이언트 → 서버: 3곳에서 각각 개별 전송
client.Send(sendData, sendData.Length, serverEndpoint); // SendPlayerData
client.Send(sendData, sendData.Length, serverEndpoint); // SendAction
client.Send(sendData, sendData.Length, serverEndpoint); // SendWeaponAction
```

**구현 시 필요한 작업**:

- 송신 큐(outgoing queue)에 패킷을 버퍼링
- 매 네트워크 틱(예: 33ms)마다 큐의 모든 메시지를 하나의 패킷으로 직렬화하여 전송
- 수신 측에서 배치 역직렬화

---

### 8. 슬라이딩 윈도우 (Flow Control) — ❌ 미구현

**정의**: 미확인(unacknowledged) 패킷 수를 윈도우 크기로 제한하여 네트워크 혼잡을 방지하는 기법.

**현재 상태**:

- ❌ 윈도우 크기 변수 없음
- ❌ ACK 기반 흐름 제어 없음
- ❌ 혼잡 감지/적응 로직 없음
- 현재는 33ms 고정 주기로 무조건 전송 (`Task.Delay(33)`)

**구현 시 필요한 작업**:

- ACK 시스템 구현 (시퀀스 번호 기반)
- 윈도우 크기 = 한 번에 보낼 수 있는 미확인 패킷 수
- RTT 측정 → 적응적 윈도우 크기 조절
- 손실률에 따라 전송 주기 동적 조절

---

### 9. MTU 단편화 처리 — ❌ 미구현

**정의**: 패킷 크기가 MTU(일반적으로 ~1400바이트)를 초과할 경우 애플리케이션 레벨에서 분할/재조립하여 IP 단편화를 방지하는 기법.

**현재 상태**:

- ❌ MTU 크기 상수 정의 없음
- ❌ 패킷 크기 체크 로직 없음
- ❌ 분할(fragment)/재조립(reassemble) 로직 없음
- 현재 `PlayerMove`나 `PlayerAction`은 작은 크기이므로 당장 문제는 적음
- ⚠️ 다만 `GameSnapshot`(서버→클라)은 플레이어/적/아이템이 많아지면 MTU 초과 가능

**구현 시 필요한 작업**:

- 전송 전 직렬화된 바이트 크기 체크
- MTU 초과 시 fragment_id + fragment_index + total_fragments 헤더를 붙여 분할 전송
- 수신 측에서 fragment 수집 → 모두 도착 시 재조립

---

### 10. 멱등성 설계 (Idempotent Design) — ⚠️ 부분적 구현

**정의**: 동일한 메시지를 여러 번 수신해도 결과가 동일하도록 "명령(command)"이 아닌 "결과 상태(result state)"를 전송하는 설계.

**현재 상태**:

- ✅ **스냅샷은 멱등**: `PlayerStatus.current_hp`, `max_hp`, `is_dead` 등을 절대값으로 전송 → `player.SetHealth(status.CurrentHp)` 로 적용
- ✅ **무기 장착**: `WeaponEquip` 액션은 무기 ID를 전송 → `SyncWeaponEquip(weaponId)` — 이미 장착된 무기는 중복 처리해도 안전
- ⚠️ **대미지**: `PlayerHit` 액션에서 `player.TakeDamage(action.Value)` — **누적 적용**되므로 중복 수신 시 이중 피해 발생 가능 (비멱등)
- ⚠️ **버프**: `BuffApply`는 stacks와 duration을 보내 적용 → 중복 수신 시 중복 적용 가능
- ✅ **사망/부활**: `is_dead` 상태 체크 후 조건부 적용 (`if (!player.IsDead)`) — 멱등

**근거 코드**:

```csharp
// ✅ 멱등: 절대값 설정
player.SetMaxHealth(status.MaxHp, keepRatio: false);
player.SetHealth(status.CurrentHp);

// ⚠️ 비멱등: 누적 적용
player.TakeDamage(action.Value); // 중복 수신 시 이중 피해
```

**개선 방향**:

- 대미지 이벤트에 고유 ID를 부여하여 중복 필터링
- 또는 대미지를 이벤트로 보내지 않고 스냅샷의 HP 절대값에만 의존

---

### 11. Reliable UDP 채널 — ❌ 미구현

**정의**: UDP 위에서 중요한 메시지(무기 장착, 레벨업, 아이템 획득 등)만 TCP처럼 ACK/재전송으로 보장하는 계층.

**현재 상태**:

- ❌ ACK(수신 확인) 시스템 없음
- ❌ 재전송(retransmit) 로직 없음
- ❌ 메시지 유형별 채널(reliable/unreliable) 분류 없음
- 모든 UDP 전송은 **fire-and-forget** 방식
- 중요한 이벤트(아이템 획득, 무기 장착, 레벨업)도 손실 가능

**근거 코드**:

```csharp
// 모든 전송이 동일하게 fire-and-forget
GamePacket packet = new GamePacket { PlayerAction = action };
byte[] sendData = packet.ToByteArray();
client.Send(sendData, sendData.Length, serverEndpoint); // ACK 없음, 재전송 없음
```

**구현 시 필요한 구성 요소**:

1. **채널 분류**:
   - **Unreliable**: `PlayerMove` (위치 동기화, 손실되어도 다음 패킷이 대체)
   - **Reliable**: `PlayerAction` 중 `WeaponEquip`, `LevelUp`, `ItemPickup`, `BuffApply/Remove`

2. **Reliable 채널 핵심 구조**:

   ```
   [PacketHeader]
   - sequence_number: uint32
   - ack_number: uint32       // 마지막으로 확인한 상대방 시퀀스
   - ack_bitfield: uint32     // 최근 32개 패킷의 수신 여부 비트맵
   - channel: enum { UNRELIABLE, RELIABLE }
   ```

3. **재전송 로직**:
   - Reliable 메시지 전송 시 `pendingAcks` 딕셔너리에 저장
   - 상대방의 ACK를 수신하면 해당 메시지 제거
   - 일정 시간(RTT × 1.5) 내 ACK 미수신 시 재전송
   - 최대 재전송 횟수 초과 시 연결 끊김 처리

---

## 현재 아키텍처 요약

```
[Current Architecture]

  Local Player                    Server                    Remote Player
  ┌──────────┐                 ┌──────────┐               ┌──────────┐
  │ Input    │                 │          │               │          │
  │ → Move  │──PlayerMove──→  │ Relay    │──Snapshot──→  │ Lerp to  │
  │ (로컬즉시)│  (33ms 주기)    │ (릴레이)  │  (전체상태)    │ target   │
  │          │                 │          │               │          │
  │ Attack  │──PlayerAction─→ │ Broadcast│──Action───→   │ Execute  │
  │ (로컬실행)│  (즉시, 개별)   │          │  (이벤트)     │ (재실행)  │
  └──────────┘                 └──────────┘               └──────────┘

특징:
- 클라이언트 권한(Client-Authoritative) 이동
- 서버는 릴레이 역할 (위치 보정 없음)
- 모든 UDP 전송은 fire-and-forget
- 스냅샷 기반 전체 상태 동기화 (서버→클라)
```

---

## 우선순위별 구현 로드맵 (권장)

| 순위 | 기능                     | 난이도 | 효과                           |
| ---- | ------------------------ | ------ | ------------------------------ |
| 1    | Reliable UDP 채널        | ★★★    | 중요 이벤트 손실 방지 (필수)   |
| 2    | 시퀀스 번호              | ★★     | 패킷 순서 보장, 중복 필터링    |
| 3    | 패킷 집합화 (클라→서버)  | ★★     | 헤더 오버헤드 절감             |
| 4    | 엔티티 인터폴레이션 개선 | ★★★    | 부드러운 원격 엔티티 움직임    |
| 5    | 데드 레코닝 활성화       | ★★     | 스냅샷 간 부드러운 이동 보간   |
| 6    | 서버 리컨실리에이션      | ★★★★   | 서버 권위 + 반응성 (치트 방지) |
| 7    | 멱등성 보완              | ★★     | 중복 처리 안정성               |
| 8    | 주요 데이터 중복 전송    | ★      | 패킷 손실 보상                 |
| 9    | 슬라이딩 윈도우          | ★★★    | 네트워크 혼잡 제어             |
| 10   | MTU 단편화               | ★★★    | 대규모 스냅샷 안정 전송        |
