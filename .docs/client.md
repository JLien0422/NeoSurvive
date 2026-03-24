# NeoSurvive 서버 네트워킹 구현 완료 보고서

> **날짜**: 2026-02-25  
> **상태**: ✅ 빌드 성공 (0 Warning, 0 Error)

---

## 📋 구현 완료 항목

### 1. ✅ 서버 리컨실리에이션 (Server Reconciliation)

**파일**: `Networking/ServerReconciliation.cs`

| 항목 | 설명 |
|------|------|
| 입력 시퀀스 추적 | 클라이언트가 보내는 `input_sequence` 번호를 플레이어별로 추적 |
| 권위적 상태 관리 | 서버의 권위적 상태(`PlayerMove`)를 저장·갱신 |
| 스냅샷 포함 | 매 Tick의 `GameSnapshot`에 `last_processed_inputs` 필드로 포함 |
| 제네릭 설계 | `ServerReconciliation<TState>` — 게임별 상태 타입 교체 가능 |

**Protobuf 변경사항**:
- `PlayerMove`에 `input_sequence` 필드 추가 (field 11)
- `GameSnapshot`에 `repeated PlayerInputAck last_processed_inputs` 추가 (field 7)
- 새 메시지 `PlayerInputAck` 추가 (`player_id`, `last_processed_input`)

---

### 2. ✅ 슬라이딩 윈도우 (Sliding Window)

**파일**: `Networking/SlidingWindow.cs`

| 항목 | 설명 |
|------|------|
| 수신 비트필드 | 32비트 비트맵으로 최근 32개 패킷의 수신 여부 추적 |
| 시퀀스 추적 | 중복·역전 패킷 자동 필터링 |
| ACK 처리 | `ack_number` + `ack_bitfield` 기반 확인 |
| Reliable 재전송 큐 | 미확인 패킷을 150ms 간격으로 최대 10회 재전송 |
| 액션 멱등성 | 최근 256개 ActionId를 HashSet + Queue로 관리 |

> 기존 `CoopService`에 퍼져있던 `ClientSequenceState`, `ProcessIncomingAck`, `TrackReceivedSequence`, `IsActionIdSeen`, `TrackActionId` 로직이 모두 이 클래스로 캡슐화됨.

---

### 3. ✅ MTU 단편화 (MTU Fragmentation)

**파일**: `Networking/MtuFragmenter.cs`

| 항목 | 설명 |
|------|------|
| MTU 크기 | 기본 1200 bytes (IPv4/IPv6 환경에서 안전한 수준) |
| 헤더 형식 | 5 bytes (GroupId 2B + Index 1B + Total 1B + Flags 1B) |
| 비단편화 최적화 | MTU 이하 패킷은 헤더 없이 원본 전달 (오버헤드 0) |
| 재조립 | 수신 측에서 GroupId 기준으로 프래그먼트 수집·조립 |
| TTL 관리 | 불완전한 재조립 그룹은 2초 후 자동 폐기 |
| 최대 크기 | 255 × (MTU - 5) = 약 305KB까지 지원 |

**프래그먼트 헤더 구조**:
```
┌─────────────────────────────────────────────────┐
│ [0..1]  FragmentGroupId  (ushort, Big-Endian)   │
│ [2]     FragmentIndex    (byte, 0-based)        │
│ [3]     FragmentTotal    (byte)                 │
│ [4]     Flags            (byte)                 │
│         bit 0: 1 = 단편화됨, 0 = 비단편화      │
└─────────────────────────────────────────────────┘
```

---

### 4. ✅ 통합 패킷 파이프라인 (PacketPipeline)

**파일**: `Networking/PacketPipeline.cs`

SlidingWindow + MtuFragmenter + INetworkTransport를 결합한 단일 패킷 처리 파이프라인:

```
[송신 흐름]
  GamePacket → Protobuf 직렬화 → 시퀀스 헤더 부여 → MTU 단편화 → UDP 전송

[수신 흐름]
  UDP 수신 → MTU 재조립 → Protobuf 역직렬화 → 시퀀스 필터링 → ACK 처리
```

---

### 5. ✅ 전송 추상화 (Transport Abstraction)

| 파일 | 설명 |
|------|------|
| `Networking/INetworkTransport.cs` | 전송 계층 인터페이스 (`SendAsync`, `Mtu`) |
| `Networking/UdpTransport.cs` | UdpClient 래핑 구현체 |

> UDP 외에도 WebRTC, KCP 등으로 교체 가능.

---

## 📁 파일 구조 변경

```
NeoSurviveServer/
├── Networking/                       ⬅ 신규 디렉토리
│   ├── INetworkTransport.cs          ⬅ 전송 추상화 인터페이스
│   ├── UdpTransport.cs               ⬅ UDP 전송 구현
│   ├── SlidingWindow.cs              ⬅ 슬라이딩 윈도우 프로토콜
│   ├── MtuFragmenter.cs              ⬅ MTU 단편화/재조립
│   ├── ServerReconciliation.cs       ⬅ 서버 리컨실리에이션
│   └── PacketPipeline.cs             ⬅ 통합 파이프라인
├── ProtoFiles/
│   ├── GamePacket.proto              ⬅ 수정 (input_sequence, PlayerInputAck 추가)
│   └── GameProtocol.cs               ⬅ 재생성
├── Services/
│   ├── CoopService.cs                ⬅ 수정 (네트워킹 코드를 Networking 모듈로 이전)
│   ├── ICoopService.cs               ⬅ 수정 (HandleRawPacketAsync 추가)
│   └── UDPGameServer.cs              ⬅ 수정 (원시 바이트 기반으로 전환)
...
```

---

## 🎮 클라이언트 측 구현 가이드

### 1. 서버 리컨실리에이션 (필수)

클라이언트는 `GameSnapshot.last_processed_inputs`를 사용하여 예측-재적용을 수행해야 합니다.

#### 1.1 입력 시퀀스 관리

```csharp
// 클라이언트: 입력마다 단조 증가하는 시퀀스 번호 부여
private uint _inputSequence = 0;

// 입력 버퍼 (시퀀스 → 입력 데이터)
private Queue<(uint seq, PlayerMove input)> _pendingInputs = new();

void SendMove(float posX, float posY, float velX, float velY)
{
    _inputSequence++;
    
    var move = new PlayerMove
    {
        PlayerId = myPlayerId,
        Timestamp = DateTime.UtcNow.Ticks,
        PosX = posX,
        PosY = posY,
        VelX = velX,
        VelY = velY,
        InputSequence = _inputSequence  // ⬅ 핵심: 입력 시퀀스 번호
    };
    
    // 입력 버퍼에 보관
    _pendingInputs.Enqueue((_inputSequence, move));
    
    // 서버로 전송
    SendPacket(new GamePacket { PlayerMove = move });
}
```

#### 1.2 서버 상태 수신 시 재조정 (Reconciliation)

```csharp
void OnGameSnapshotReceived(GameSnapshot snapshot)
{
    // 1. 자신의 last_processed_input 찾기
    var myAck = snapshot.LastProcessedInputs
        .FirstOrDefault(a => a.PlayerId == myPlayerId);
    
    if (myAck == null) return;
    
    uint lastProcessed = myAck.LastProcessedInput;
    
    // 2. 서버가 처리 완료한 입력까지 버퍼에서 제거
    while (_pendingInputs.Count > 0 && _pendingInputs.Peek().seq <= lastProcessed)
    {
        _pendingInputs.Dequeue();
    }
    
    // 3. 서버의 권위적 위치를 기반 상태로 설정
    var myServerState = snapshot.PlayerStates
        .FirstOrDefault(p => p.PlayerId == myPlayerId);
    
    if (myServerState != null)
    {
        // 서버 상태로 리셋
        myPosition = new Vector2(myServerState.PosX, myServerState.PosY);
        
        // 4. 아직 서버가 처리하지 못한 입력을 재적용 (Re-apply)
        foreach (var (_, input) in _pendingInputs)
        {
            ApplyInput(input);  // 클라이언트 측 시뮬레이션 재실행
        }
    }
}
```

### 2. 인터폴레이션 (Interpolation) — 다른 플레이어 표시

```csharp
// 다른 플레이어의 위치를 부드럽게 보간
// 서버 틱(50ms) 간격의 위치를 100ms 과거 시점으로 보간
private float _interpolationDelay = 0.1f;  // 100ms

void UpdateOtherPlayerPosition(uint playerId)
{
    var buffer = _positionBuffers[playerId]; // 시간순 위치 버퍼
    
    float renderTime = Time.time - _interpolationDelay;
    
    // renderTime에 해당하는 두 스냅샷 사이를 선형 보간
    var (before, after) = FindBracketSnapshots(buffer, renderTime);
    float t = (renderTime - before.time) / (after.time - before.time);
    
    position = Vector2.Lerp(before.position, after.position, t);
}
```

### 3. 데드 레코닝 (Dead Reckoning) — 패킷 손실 대비

```csharp
// 마지막 수신된 속도 벡터로 위치를 예측
void PredictOtherPlayer(uint playerId, float deltaTime)
{
    if (TimeSinceLastUpdate(playerId) > packetTimeout)
    {
        var lastState = _lastKnownStates[playerId];
        predictedPos.x += lastState.VelX * deltaTime;
        predictedPos.y += lastState.VelY * deltaTime;
    }
}
```

### 4. MTU 단편화 — 클라이언트 수신 처리

```csharp
// 서버와 동일한 MtuFragmenter를 사용
private MtuFragmenter _fragmenter = new MtuFragmenter(1200);

void OnUdpReceive(byte[] data)
{
    // 단편화된 패킷이면 재조립, 아니면 즉시 반환
    var reassembled = _fragmenter.TryReassemble(data);
    if (reassembled == null) return;  // 아직 불완전
    
    var packet = GamePacket.Parser.ParseFrom(reassembled);
    HandlePacket(packet);
}
```

### 5. 슬라이딩 윈도우 — 클라이언트 송신 처리

```csharp
// 서버와 동일한 SlidingWindow를 사용
private SlidingWindow _window = new SlidingWindow();

void SendPacket(GamePacket packet, ChannelType channel = ChannelType.Unreliable)
{
    var (seq, ack, ackBits) = _window.NextOutgoingHeader();
    packet.SequenceNumber = seq;
    packet.AckNumber = ack;
    packet.AckBitfield = ackBits;
    packet.Channel = channel;
    
    var data = packet.ToByteArray();
    
    if (channel == ChannelType.Reliable)
    {
        _window.EnqueueReliable(seq, data);
    }
    
    // MTU 단편화 후 전송
    foreach (var fragment in _fragmenter.Fragment(data))
    {
        _udpClient.Send(fragment);
    }
}

// 수신 시 ACK 처리
void OnPacketReceived(GamePacket packet)
{
    _window.ProcessIncomingAck(packet.AckNumber, packet.AckBitfield);
    
    if (!_window.TrackReceivedSequence(packet.SequenceNumber))
        return;  // 중복 패킷
    
    HandlePayload(packet);
}

// 매 프레임 재전송 체크
void Update()
{
    var retransmissions = _window.CollectRetransmissions();
    foreach (var entry in retransmissions)
    {
        foreach (var fragment in _fragmenter.Fragment(entry.RawBytes))
        {
            _udpClient.Send(fragment);
        }
    }
}
```

---

## ⚙️ 설정값 참조

| 항목 | 기본값 | 위치 |
|------|--------|------|
| MTU 크기 | 1200 bytes | `UdpTransport.Mtu` |
| 프래그먼트 TTL | 2.0초 | `MtuFragmenter.GroupTtlSeconds` |
| Reliable 재시도 간격 | 150ms | `SlidingWindow.RetryIntervalMs` |
| Reliable 최대 재시도 | 10회 | `SlidingWindow.MaxRetryCount` |
| 멱등성 윈도우 크기 | 256개 | `SlidingWindow.ActionIdWindowSize` |
| 미처리 입력 최대 크기 | 120개 | `ServerReconciliation.MaxPendingInputs` |
| 브로드캐스트 주기 | 50ms (20Hz) | `UDPGameServer.BroadcastLoopAsync` |

---

## 🔄 하위 호환성

- `HandlePlayerUpdateAsync(GamePacket, ...)` 메서드는 유지됨 (하위 호환)
- 기존 클라이언트가 `input_sequence`를 보내지 않아도 정상 동작 (0이면 리컨실리에이션 무시)
- MTU 이하 패킷은 헤더 없이 원본 전달되므로 기존 클라이언트와 호환
