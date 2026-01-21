# Protocol Buffers (Protobuf) 설정 완료 ✅

## 📦 설치된 패키지

### 1. Google.Protobuf.dll (v3.25.1)
- **위치**: `Assets/Plugins/Protobuf/Google.Protobuf.dll`
- **용도**: Protocol Buffers 직렬화/역직렬화 라이브러리

### 2. protoc 컴파일러 (v25.1)
- **위치**: 프로젝트 루트에 임시 다운로드 후 컴파일 완료
- **용도**: `.proto` 파일을 C# 코드로 변환

---

## 📁 생성된 파일

### 1. GamePacket.proto
- **위치**: `ProtoFiles/GamePacket.proto`
- **내용**:
```proto
syntax = "proto3";

option csharp_namespace = "NeoSurvive.Network.Protocol";

message GamePacket {
  uint32 player_id = 1;   // 문자열보다 정수 ID가 효율적
  int64 timestamp = 2;    // 순서 보장을 위한 타임스탬프
  float pos_x = 3;
  float pos_y = 4;
}
```

### 2. GameProtocol.cs (자동 생성됨)
- **위치**: `Assets/Scripts/Multiplayer/GameProtocol.cs`
- **생성 방법**: protoc 컴파일러로 자동 생성
- **네임스페이스**: `NeoSurvive.Network.Protocol`
- **클래스**: `GamePacket`

---

## 🚀 사용 방법

### 패킷 생성 및 직렬화 (전송)

```csharp
using NeoSurvive.Network.Protocol;
using Google.Protobuf;

// 1. 패킷 생성
var packet = new GamePacket
{
    PlayerId = 12345,
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    PosX = transform.position.x,
    PosY = transform.position.y
};

// 2. 직렬화 (바이트 배열로 변환)
byte[] data = packet.ToByteArray();

// 3. UDP로 전송
udpClient.Send(data, data.Length, serverEndpoint);
```

### 패킷 수신 및 역직렬화 (로드)

```csharp
using NeoSurvive.Network.Protocol;
using Google.Protobuf;

// 1. UDP로 데이터 수신
byte[] receivedData = udpClient.Receive(ref remoteEndpoint);

// 2. 역직렬화 (GamePacket 객체로 변환)
var packet = GamePacket.Parser.ParseFrom(receivedData);

// 3. 데이터 사용
Debug.Log($"Player {packet.PlayerId} at ({packet.PosX}, {packet.PosY})");
Debug.Log($"Timestamp: {packet.Timestamp}");
```

---

## 🔧 .proto 파일 수정 시

새로운 필드를 추가하거나 구조를 변경할 경우:

### 1. GamePacket.proto 수정
```proto
message GamePacket {
  uint32 player_id = 1;
  int64 timestamp = 2;
  float pos_x = 3;
  float pos_y = 4;
  float velocity_x = 5;  // 새 필드 추가
  float velocity_y = 6;  // 새 필드 추가
}
```

### 2. 재컴파일 명령어
프로젝트 루트에서 실행:

```powershell
# protoc 다운로드 (최초 1회만)
Invoke-WebRequest -Uri "https://github.com/protocolbuffers/protobuf/releases/download/v25.1/protoc-25.1-win64.zip" -OutFile "protoc.zip"
Expand-Archive -Path "protoc.zip" -DestinationPath "protoc_compiler" -Force

# .proto 파일 컴파일
.\protoc_compiler\bin\protoc.exe --csharp_out=Assets\Scripts\Multiplayer --proto_path=ProtoFiles ProtoFiles\GamePacket.proto

# 생성된 파일 이름 변경
Move-Item -Path "Assets\Scripts\Multiplayer\GamePacket.cs" -Destination "Assets\Scripts\Multiplayer\GameProtocol.cs" -Force
```

---

## 💡 장점

### Protocol Buffers의 장점:
1. **작은 크기**: JSON 대비 3~10배 작은 패킷 크기
2. **빠른 속도**: 직렬화/역직렬화 속도가 매우 빠름
3. **타입 안정성**: 컴파일 타임에 타입 체크
4. **하위 호환성**: 필드 추가/삭제 시 이전 버전과 호환 가능
5. **UDP에 최적화**: 작은 패킷 크기로 UDP 전송에 적합

### 성능 비교:
```
JSON:     {"player_id":12345,"timestamp":1234567890,"pos_x":10.5,"pos_y":20.3}
          → 약 70 bytes

Protobuf: [바이너리 데이터]
          → 약 16-20 bytes (3.5배 작음!)
```

---

## 📚 다음 단계

1. **UDP 클라이언트 구현** - `UdpNetworkManager.cs` 생성
2. **게임 상태 전송** - 플레이어 위치를 주기적으로 서버에 전송
3. **패킷 추가** - 적 상태, 아이템 등 추가 메시지 정의
4. **순서 보장** - timestamp 기반 패킷 순서 처리

---

## 🔗 참고 자료

- [Protocol Buffers 공식 문서](https://developers.google.com/protocol-buffers)
- [C# Tutorial](https://developers.google.com/protocol-buffers/docs/csharptutorial)
- [Proto3 Language Guide](https://developers.google.com/protocol-buffers/docs/proto3)
