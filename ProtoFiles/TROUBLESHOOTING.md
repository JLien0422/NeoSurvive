# Google.Protobuf DLL 오류 해결 가이드

## ✅ 수행한 작업

1. ✅ Google.Protobuf.dll (.NET 4.5 버전) 설치
2. ✅ .meta 파일 설정 수정 (Editor와 모든 플랫폼에서 활성화)
3. ✅ link.xml 파일 생성 (코드 스트리핑 방지)

---

## 🔧 해결 방법

### 방법 1: Unity 새로고침 (우선 시도)

Unity 에디터에서:
- **Assets > Refresh** 클릭
- 또는 **Ctrl+R** 누르기
- 또는 Unity 재시작

---

### 방법 2: 여전히 오류 발생 시

만약 `Reference has errors 'Google.Protobuf'` 오류가 계속된다면:

#### Step 1: DLL 재설치
```powershell
# 프로젝트 루트에서 실행
Remove-Item -Path "Assets\Plugins\Protobuf\Google.Protobuf.dll*" -Force

# 재다운로드
Invoke-WebRequest -Uri "https://www.nuget.org/api/v2/package/Google.Protobuf/3.25.1" -OutFile "protobuf.zip"
Expand-Archive -Path "protobuf.zip" -DestinationPath "protobuf_temp" -Force
Copy-Item "protobuf_temp\lib\net45\Google.Protobuf.dll" -Destination "Assets\Plugins\Protobuf\" -Force
Remove-Item -Path "protobuf.zip", "protobuf_temp" -Recurse -Force
```

#### Step 2: Unity에서 Import Settings 확인
1. Unity에서 `Assets/Plugins/Protobuf/Google.Protobuf.dll` 선택
2. Inspector에서 확인:
   - ✅ **Any Platform** 체크
   - ✅ **Include Platforms: Editor** 체크
   - ✅ **Include Platforms: Standalone** 체크

#### Step 3: API Compatibility Level 확인
Unity에디터에서:
1. **Edit > Project Settings > Player**
2. **Other Settings > Configuration**
3. **Api Compatibility Level** 을 다음 중 하나로 설정:
   - `.NET Framework` (권장)
   - `.NET Standard 2.1`

---

### 방법 3: 대안 - Protobuf 소스 코드 직접 포함

DLL 대신 소스 코드를 직접 포함하는 방법:

```powershell
# GitHub에서 protobuf-net (C# 전용) 다운로드
git clone https://github.com/protobuf-net/protobuf-net.git
# 또는 Unity용 경량 버전 사용
```

---

### 방법 4: NuGetForUnity 사용 (가장 안전)

1. **NuGetForUnity 패키지 설치**
   - GitHub에서 다운로드: https://github.com/GlitchEnzo/NuGetForUnity
   - `.unitypackage` 파일을 Unity로 Import

2. **Google.Protobuf 설치**
   ```
   Unity > NuGet > Manage NuGet Packages
   검색: Google.Protobuf
   Install 클릭
   ```

---

## 🔍 현재 파일 상태

```
Assets/
├── Plugins/
│   └── Protobuf/
│       ├── Google.Protobuf.dll (472,864 bytes) ✅
│       └── Google.Protobuf.dll.meta          ✅
├── Scripts/
│   └── Multiplayer/
│       └── GameProtocol.cs                   ✅
└── link.xml                                  ✅
```

---

## 💡 확인 사항

Unity Console에서 정확한 오류 메시지 확인:
```
Assembly 'Library/ScriptAssemblies/Assembly-CSharp.dll' will not be loaded due to errors:
Reference has errors 'Google.Protobuf'.
```

이 오류는 보통 다음 원인 중 하나:
1. ❌ DLL이 Unity에서 비활성화됨 → **해결됨**
2. ❌ API Compatibility Level 불일치 → **확인 필요**
3. ❌ DLL 버전이 Unity와 호환되지 않음 → **.NET 4.5 버전 사용 중**

---

## 🚀 다음 단계

Unity를 새로고침한 후:
1. Console 창 확인
2. 오류가 사라졌는지 확인
3. 테스트 코드 작성:

```csharp
using NeoSurvive.Network.Protocol;
using UnityEngine;

public class ProtobufTest : MonoBehaviour
{
    void Start()
    {
        // 패킷 생성 테스트
        var packet = new GamePacket
        {
            PlayerId = 123,
            Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            PosX = 10.5f,
            PosY = 20.3f
        };
        
        // 직렬화 테스트
        byte[] data = packet.ToByteArray();
        Debug.Log($"Packet size: {data.Length} bytes");
        
        // 역직렬화 테스트
        var decoded = GamePacket.Parser.ParseFrom(data);
        Debug.Log($"Player {decoded.PlayerId} at ({decoded.PosX}, {decoded.PosY})");
    }
}
```
