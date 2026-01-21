# ✅ UDP 클라이언트 수정 완료!

## 🔧 해결된 문제

### 오류:
```
SocketException: 소켓이 연결되어 있지 않거나 주소가 제공되지 않아서 데이터를 보낼 수 없습니다.
```

### 원인:
1. ❌ `IPAddress.Parse("nasdac.kro.kr")` - **DNS 주소를 직접 파싱할 수 없음**
   - `IPAddress.Parse()`는 IP 주소만 파싱 가능 (예: "192.168.1.1")
   - 도메인 이름은 `Dns.GetHostAddresses()`로 변환 필요

2. ❌ 에러 처리 부족

### 해결:
✅ **DNS → IP 변환 로직 추가**
✅ **에러 처리 및 재연결 로직 추가**
✅ **디버그 로그 추가**

---

## 🎯 수정된 코드 주요 기능

### 1️⃣ DNS 자동 변환
```csharp
private IPAddress ResolveServerAddress(string address)
{
    // IP 주소면 그대로 사용
    if (IPAddress.TryParse(address, out IPAddress ipAddress))
        return ipAddress;

    // DNS 주소면 IP로 변환
    IPAddress[] addresses = Dns.GetHostAddresses(address);
    
    // IPv4 주소 우선 선택
    foreach (var addr in addresses)
    {
        if (addr.AddressFamily == AddressFamily.InterNetwork)
            return addr;
    }
}
```

### 2️⃣ 연결 상태 확인
```csharp
void SendPlayerData()
{
    // 연결 확인
    if (!isConnected || client == null || serverEndpoint == null)
    {
        Debug.LogWarning("서버가 연결되지 않았습니다. 재연결 시도...");
        InitializeUdpClient();
        return;
    }
    
    // 패킷 전송...
}
```

### 3️⃣ Inspector 설정 가능
Unity Inspector에서 직접 설정 가능:
- **Server Address**: `nasdac.kro.kr` (또는 IP 주소)
- **Server Port**: `5158`
- **Show Debug Log**: 디버그 로그 on/off

---

## 🚀 사용 방법

### 1️⃣ Unity 설정
1. 빈 GameObject 생성 (이름: "UDP Network Manager")
2. `UDPClient.cs` 스크립트 추가
3. Inspector에서 설정:
   ```
   Server Address: nasdac.kro.kr
   Server Port: 5158
   Show Debug Log: ✓
   ```

### 2️⃣ 테스트
1. Unity Play 모드 실행
2. **Space bar** 키 누르기
3. Console 확인:
   ```
   [UDP] DNS 변환: nasdac.kro.kr -> 123.456.789.012
   [UDP] 서버 연결 준비 완료: 123.456.789.012:5158
   [UDP] 패킷 전송 완료: 18 bytes (PlayerId: 1, Pos: 0, 0)
   ```

---

## 📊 전송되는 데이터

```protobuf
message GamePacket {
  uint32 player_id = 1;   // 플레이어 ID
  int64 timestamp = 2;    // 현재 시간 (밀리초)
  float pos_x = 3;        // X 위치
  float pos_y = 4;        // Y 위치
}
```

### 패킷 크기:
- **약 16-20 bytes** (Protobuf 압축)
- JSON 대비 **3~5배 작음**

---

## 🔍 디버그 로그 예시

### 성공 시:
```
[UDP] DNS 변환: nasdac.kro.kr -> 123.456.789.012
[UDP] 서버 연결 준비 완료: 123.456.789.012:5158
[UDP] 패킷 전송 완료: 18 bytes (PlayerId: 12345, Pos: 10.5, 20.3)
```

### 실패 시:
```
[UDP] DNS 변환 실패: 호스트를 찾을 수 없습니다
[UDP] 서버 주소 'wrongaddress.com'를 IP로 변환할 수 없습니다!
```

### 재연결 시:
```
[UDP] 서버가 연결되지 않았습니다. 재연결을 시도합니다...
[UDP] 서버 연결 준비 완료: 123.456.789.012:5158
```

---

## 🛠️ 추가 기능 구현

### 자동 전송 (주기적)
```csharp
[SerializeField] private float sendInterval = 0.1f; // 100ms마다
private float sendTimer = 0f;

void Update()
{
    sendTimer += Time.deltaTime;
    
    if (sendTimer >= sendInterval)
    {
        SendPlayerData();
        sendTimer = 0f;
    }
}
```

### 수신 기능 추가
```csharp
private void Start()
{
    InitializeUdpClient();
    
    // 수신 스레드 시작
    System.Threading.Thread receiveThread = new System.Threading.Thread(ReceiveData);
    receiveThread.IsBackground = true;
    receiveThread.Start();
}

private void ReceiveData()
{
    while (isConnected)
    {
        try
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = client.Receive(ref remoteEP);
            
            // 패킷 파싱
            GamePacket packet = GamePacket.Parser.ParseFrom(data);
            
            Debug.Log($"수신: Player {packet.PlayerId} at ({packet.PosX}, {packet.PosY})");
        }
        catch (Exception e)
        {
            Debug.LogError($"수신 오류: {e.Message}");
        }
    }
}
```

---

## 💡 다음 단계

1. ✅ UDP 전송 기능 완료
2. ⏳ UDP 수신 기능 추가
3. ⏳ 주기적 자동 전송 (tick rate 설정)
4. ⏳ 패킷 타입 확장 (적, 아이템, 공격 등)
5. ⏳ 패킷 순서 보장 (timestamp 기반)

---

## 🔐 보안 고려사항

UDP는 연결 없는 프로토콜이므로:
- ❌ 전송 보장 없음
- ❌ 순서 보장 없음
- ❌ 암호화 없음

해결 방법:
1. **순서 보장**: `timestamp` 필드로 최신 패킷만 적용
2. **전송 확인**: 중요한 데이터는 ACK 패킷으로 확인
3. **암호화**: 민감한 데이터는 암호화 후 전송

---

## 📝 요약

- ✅ DNS → IP 자동 변환
- ✅ 에러 처리 및 재연결
- ✅ Protobuf 직렬화
- ✅ Inspector에서 설정 가능
- ✅ 디버그 로그

이제 Space bar를 누르면 서버로 UDP 패킷이 정상적으로 전송됩니다! 🎮
