# 멀티플레이어 UI 구현 가이드

## 개요

NeoSurvive 멀티플레이어 로비 및 방 대기실 UI 시스템입니다.

---

## 📁 파일 구조

```
Assets/Scripts/
├── Managers/
│   └── LobbyManager.cs         # 로비 씬 전체 관리 (탭 전환)
└── UI/Multiplayer/
    ├── LobbyUI.cs              # 로비 메인 화면 (제거 가능)
    └── MultiplayerRoomUI.cs    # 방 대기실 UI
```

---

## 🎮 LobbyManager - 중앙 관리자

### 기능:

- **싱글톤 패턴**
- **탭 전환** (로비 ↔ 멀티플레이어 대기실)
- 기존 로비 버튼들 관리
- MultiplayerRoomUI 연동

### 주요 메서드:

```csharp
LobbyManager.Instance.ShowLobbyTab();          // 로비 탭 표시
LobbyManager.Instance.ShowMultiplayerTab();    // 멀티플레이어 탭 표시
```

### Inspector 설정:

- **Lobby Tab**: 메인 로비 UI GameObject
- **Multiplayer Tab**: 멀티플레이어 대기실 UI GameObject
- **Multiplayer Button**: "멀티플레이어" 버튼 연결

---

## 🎮 기능 목록

### 1. 로비 UI (LobbyUI.cs)

#### 기능:

- **싱글 플레이** 버튼
- **멀티플레이어** 버튼 → 대기실로 이동
- **설정** 버튼
- **종료** 버튼
- 플레이어 정보 표시 (Device ID, Level, Gold)

#### 사용법:

```csharp
LobbyUI lobbyUI = FindObjectOfType<LobbyUI>();
lobbyUI.ShowLobby();  // 로비 표시
lobbyUI.HideLobby();  // 로비 숨김
```

---

### 2. 방 대기실 UI (MultiplayerRoomUI.cs)

#### 기능:

- **방 생성** (호스트)
- **방 참가** (방 코드 입력)
- **방 코드 표시 및 복사**
- **유저 목록** (방장 표시)
- **준비 버튼** (클라이언트)
- **게임 시작 버튼** (방장만)
- **방 나가기**

#### 사용법:

```csharp
MultiplayerRoomUI roomUI = FindObjectOfType<MultiplayerRoomUI>();
roomUI.ShowRoomList();  // 방 목록/생성 화면
roomUI.ShowRoom();      // 방 대기실 화면
```

---

## 🔧 Unity 에디터 설정

### LobbyScene 구조:

```
LobbyScene
├── LobbyManager (GameObject)
│   └── LobbyManager.cs (Script)
│       ├── Lobby Tab → LobbyTabPanel
│       ├── Multiplayer Tab → MultiplayerTabPanel
│       ├── Main Start Button → 싱글플레이 버튼
│       ├── Multiplayer Button → 멀티플레이 버튼
│       └── Multiplayer Room UI → MultiplayerRoomUI
│
├── LobbyTabPanel (GameObject) ← Lobby Tab
│   ├── MainStartButton (Button) - 싱글 플레이
│   ├── MultiplayerButton (Button) - 멀티플레이어
│   ├── UpgradeButton (Button)
│   └── ExitButton (Button)
│
└── MultiplayerTabPanel (GameObject) ← Multiplayer Tab
    └── MultiplayerRoomUI 컴포넌트
        ├── RoomListPanel
        └── RoomPanel
```

### 1. 로비 탭 (LobbyTabPanel):

```
LobbyTabPanel (GameObject)
├── Background (Image)
├── ButtonsPanel (GameObject)
│   ├── MainStartButton (Button) - 싱글 플레이
│   ├── MultiplayerButton (Button) - 멀티플레이어 ★
│   ├── UpgradeButton (Button)
│   └── ExitButton (Button)
└── PlayerInfoPanel (GameObject)
    ├── DeviceIdText (TextMeshProUGUI)
    └── GoldText (TextMeshProUGUI)
```

### 2. 멀티플레이어 탭 (MultiplayerTabPanel):

**처음엔 비활성화 상태로 시작**

```
MultiplayerRoomUI (GameObject)
├── RoomListPanel (GameObject)
│   ├── CreateRoomButton (Button)
│   ├── RoomCodeInput (TMP_InputField)
│   ├── JoinRoomButton (Button)
│   ├── CharacterDropdown (TMP_Dropdown)
│   └── BackToLobbyButton (Button)
└── RoomPanel (GameObject)
    ├── RoomCodeText (TextMeshProUGUI)
    ├── CopyRoomCodeButton (Button)
    ├── PlayerListContainer (Transform)
    │   └── PlayerItemPrefab (GameObject)
    │       ├── NameText (TextMeshProUGUI)
    │       └── ReadyIcon (GameObject)
    ├── ReadyButton (Button)
    ├── StartGameButton (Button)
    └── LeaveRoomButton (Button)
```

### 3. PlayerItemPrefab 구조:

```
PlayerItem (GameObject)
├── Background (Image)
├── NameText (TextMeshProUGUI)  # "[방장] Player123" 형식
└── ReadyIcon (Image)           # 준비 상태 아이콘
```

---

## � 설정 순서

### 1. LobbyManager 설정:

1. LobbyScene에 빈 GameObject 생성 → 이름: "LobbyManager"
2. LobbyManager 스크립트 추가
3. Inspector에서 다음 항목 연결:
   - **Lobby Tab**: LobbyTabPanel GameObject
   - **Multiplayer Tab**: MultiplayerTabPanel GameObject
   - **Multiplayer Button**: 멀티플레이어 버튼
   - **Multiplayer Room UI**: MultiplayerRoomUI 컴포넌트

### 2. 탭 GameObject 생성:

1. **LobbyTabPanel** 생성 (기존 로비 UI)
2. **MultiplayerTabPanel** 생성 (방 대기실 UI)
   - MultiplayerRoomUI 스크립트 추가
   - 처음엔 **비활성화** 상태로 설정

### 3. 버튼 연결:

- 멀티플레이어 버튼 onClick → `LobbyManager.ShowMultiplayerTab()`

---

## 📝 사용 흐름

1. **게임 시작** → LobbyTab 활성화
2. **"멀티플레이어" 버튼 클릭** → `LobbyManager.ShowMultiplayerTab()`
   - LobbyTab 비활성화
   - MultiplayerTab 활성화
3. **"뒤로가기" 버튼** → `LobbyManager.ShowLobbyTab()`
   - MultiplayerTab 비활성화
   - LobbyTab 활성화

---

## �🌐 NetworkManager 연동

### 방 생성:

```csharp
yield return NetworkManager.Instance.CreateCoopSession(characterType, stage);

if (NetworkManager.Instance.IsInCoopSession)
{
    string roomCode = NetworkManager.Instance.SessionCode;
    bool isHost = NetworkManager.Instance.IsHost;
}
```

### 방 참가:

```csharp
yield return NetworkManager.Instance.JoinCoopSession(roomCode, characterType);
```

### 방 나가기:

```csharp
yield return NetworkManager.Instance.LeaveCoopSession();
```

---

## ✨ 주요 기능 설명

### 1. 방 코드 복사

```csharp
GUIUtility.systemCopyBuffer = roomCode;  // 클립보드에 복사
```

### 2. 방장 표시

```csharp
string displayName = isHost ? $"[방장] {playerName}" : playerName;
```

### 3. 준비 상태 토글

```csharp
isReady = !isReady;
// TODO: 서버에 준비 상태 전송
```

### 4. 게임 시작 (방장만)

```csharp
if (!isHost) return;  // 방장만 시작 가능
// TODO: 모든 플레이어 준비 확인
StartCoroutine(StartMultiplayerGame());
```

---

## 📝 TODO (향후 구현 필요)

1. **서버 API 연동**
   - 준비 상태 전송/수신
   - 실시간 플레이어 목록 업데이트
   - 게임 시작 신호 전송

2. **WebSocket 메시지 처리**
   - 플레이어 입장/퇴장 이벤트
   - 준비 상태 변경 이벤트
   - 방장 변경 이벤트

3. **UI 개선**
   - 복사 완료 피드백 토스트
   - 방 코드 입력 검증
   - 캐릭터 선택 프리뷰
   - 로딩 인디케이터

4. **게임 시작 로직**
   - 모든 플레이어 준비 확인
   - 게임 씬 로드
   - 초기 동기화

---

## 🐛 디버깅

### 로그 확인:

- `[LobbyUI]` - 로비 관련 로그
- `[MultiplayerRoomUI]` - 방 대기실 관련 로그
- `[NetworkManager]` - 네트워크 연결 로그

### 일반적인 문제:

1. **NetworkManager가 null인 경우**
   - NetworkManager GameObject가 씬에 있는지 확인
   - DontDestroyOnLoad 설정 확인

2. **방 생성/참가 실패**
   - GameServerAPI 로그인 상태 확인
   - 서버 연결 상태 확인
   - 콘솔에서 에러 메시지 확인

3. **UI가 표시되지 않는 경우**
   - Canvas 설정 확인
   - Panel의 Active 상태 확인
   - Inspector에서 모든 참조 연결 확인

---

## 📚 참고 코드

### NetworkManager 주요 메서드:

- `CreateCoopSession()` - 방 생성
- `JoinCoopSession()` - 방 참가
- `LeaveCoopSession()` - 방 나가기
- `IsInCoopSession` - 세션 참가 여부
- `SessionCode` - 현재 방 코드
- `IsHost` - 방장 여부

### GameServerAPI:

- `Instance.DeviceUID` - 디바이스 ID
- `Instance.PlayerId` - 플레이어 ID

---

## 🎨 스타일 가이드

- **버튼 크기**: 200x60
- **텍스트 크기**: 24-36
- **간격**: 10-20px
- **색상 테마**: 다크 모드 기본
