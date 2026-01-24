# NeoSurvive

Unity 2D 로그라이트 서바이벌 게임 프로젝트

## 📋 프로젝트 개요

NeoSurvive는 사이버펑크 테마의 2D 로그라이트 서바이벌 게임입니다. 플레이어는 **해커(Hacker)** 또는 **사이보그(Cyborg)** 캐릭터를 선택하여 끊임없이 몰려오는 적들과 싸우며 생존하는 것이 목표입니다. 싱글플레이어와 멀티플레이어 모드를 모두 지원합니다.

---

## 📁 프로젝트 구조

### **Assets/Scripts/** - 주요 스크립트 폴더

#### **📂 Characters/** - 캐릭터 관련 스크립트
플레이어와 적 캐릭터의 핵심 로직을 관리합니다.

| 파일 | 설명 |
|------|------|
| `Character.cs` | 모든 캐릭터의 기본 클래스 (체력, 이동속도, 스탯 관리) |
| `CharacterData.cs` | 캐릭터의 데이터 구조체 (스탯 정보) |
| `CharacterType.cs` | 캐릭터 타입 열거형 (Hacker, Cyborg) |
| `EnemySpawner.cs` | 적 스폰 시스템 (난이도에 따른 웨이브 관리) |
| `IDamageable.cs` | 데미지를 받을 수 있는 인터페이스 |

##### **📂 Characters/Buff/** - 버프/디버프 시스템
| 파일 | 설명 |
|------|------|
| `BuffHandler.cs` | 버프 효과 관리 시스템 |
| `IBuff.cs` | 버프 인터페이스 |
| `StunBuff.cs` | 스턴 효과 구현 |

##### **📂 Characters/Enemy/** - 적 캐릭터
| 파일 | 설명 |
|------|------|
| `Enemy.cs` | 적 캐릭터의 기본 클래스 |
| `EnemyController.cs` | 적 AI 컨트롤러 (플레이어 추적, 공격) |
| `EnemyMechanismBase.cs` | 적 행동 메커니즘 베이스 클래스 |
| `EnemyMechanismType.cs` | 적 메커니즘 타입 열거형 |

**Mechanisms/** - 적 유형별 행동 패턴
- `BomberMechanism.cs` - 폭탄 투척 적
- `RusherMechanism.cs` - 돌진 공격 적
- `ShooterMechanism.cs` - 원거리 공격 적
- `TankerMechanism.cs` - 탱커 유형 적

##### **📂 Characters/Player/** - 플레이어
| 파일 | 설명 |
|------|------|
| `Player.cs` | 플레이어 캐릭터 로직 (레벨업, 경험치, 스탯 관리) |
| `PlayerController.cs` | 플레이어 입력 처리 및 이동 제어 |
| `WeaponManager.cs` | 무기 시스템 관리 (무기 획득, 레벨업) |
| `ChestDropper.cs` | 플레이어 사망 시 아이템 드랍 |

**Stats/** - 플레이어 스탯 시스템
- `Stat.cs` - 스탯 클래스 (기본값, 고정 증가, 퍼센트 증가)

##### **📂 Characters/Player/Weapon/** - 무기 시스템
| 파일 | 설명 |
|------|------|
| `WeaponBase.cs` | 모든 무기의 기본 클래스 |
| `Projectile.cs` | 투사체 로직 (총알, 레이저 등) |
| `Passive.cs` | 패시브 스탯 증가 무기 |

**Active/Hacker/** - 해커 전용 무기 (9종)
- `PlasmaRifle.cs` - 플라즈마 소총 (F1)
- `LinkPistol.cs` - 링크 권총 (F2)
- `DataScrambler.cs` - 데이터 스크램블러 (F3)
- `PlasmaLazer.cs` - 플라즈마 레이저 (F4)
- `AutoTurret.cs` + `DeployedTurret.cs` - 자동 터렛 (F5)
- `AIDrone.cs` - AI 드론 (F6)
- `EMPField.cs` + `EMPPulseGenerator.cs` - EMP 펄스 (F7)
- `NanoWire.cs` + `NanoWireGenerator.cs` - 나노와이어 (F8)
- `HologramDecoy.cs` + `HologramDecoyGenerator.cs` - 홀로그램 미끼 (F9)

**Active/Cyborg/** - 사이보그 전용 무기 (2종)
- `Fist.cs` - 주먹 공격
- `LaserSword.cs` - 레이저 검

---

#### **📂 Managers/** - 게임 핵심 관리자
| 파일 | 설명 |
|------|------|
| `GameManager.cs` | 게임 전체 흐름 관리 (싱글톤, 골드, 킬 카운트) |
| `UIManager.cs` | UI 전체 관리 (패널 전환, HUD 업데이트) |
| `DataManager.cs` | 게임 데이터 관리 (무기, 아이템 데이터) |
| `SoundManager.cs` | 사운드 및 BGM 관리 |
| `UpgradeManager.cs` | 업그레이드 시스템 (스탯 강화) |
| `LobbyManager.cs` | 로비 화면 관리 |
| `SettingsManager.cs` | 게임 설정 관리 |
| `DamageTextManager.cs` | 데미지 텍스트 표시 풀링 시스템 |

---

#### **📂 Network/** - 네트워크 시스템
멀티플레이어 3단계 아키텍처 구현

| 파일 | 설명 |
|------|------|
| `NetworkManager.cs` | 네트워크 전체 관리 (Obsolete - 레거시) |
| `DBManager.cs` | HTTP API 통신 (인증, 사용자 데이터, 게임 세션) |
| `MultiLobbyManager.cs` | WebSocket 로비 기능 (로비 생성/참가, 채팅) |
| `CoopManager.cs` | UDP 실시간 게임 동기화 (플레이어 위치, 적 정보) |
| `MultiplayManager.cs` | 멀티플레이어 게임 씬 플레이어 생성 및 관리 |
| `WebSocketManager.cs` | WebSocket 저수준 통신 관리 |
| `NetworkDataTypes.cs` | 네트워크 데이터 타입 정의 |

---

#### **📂 Multiplayer/** - 멀티플레이어 프로토콜
| 파일 | 설명 |
|------|------|
| `GameServerAPI.cs` | 게임 서버 API 클라이언트 (Obsolete - DBManager로 대체) |
| `UDPClient.cs` | UDP 클라이언트 (실시간 게임 데이터 송수신) |
| `GameProtocol.cs` | Protobuf 프로토콜 메시지 정의 (자동 생성) |

---

#### **📂 UI/** - 사용자 인터페이스
| 파일 | 설명 |
|------|------|
| `MainMenu_UI.cs` | 메인 메뉴 UI |
| `CharacterSelector.cs` | 캐릭터 선택 화면 |
| `DamageText.cs` | 데미지 텍스트 표시 |
| `EffectAutoDestroy.cs` | 이펙트 자동 파괴 |
| `LeaderboardUI.cs` | 리더보드 UI |
| `NicknameChangeUI.cs` | 닉네임 변경 UI |
| `SettingsUI.cs` | 설정 UI (키바인딩, 사운드 등) |
| `UpgradeItemUI.cs` | 업그레이드 아이템 UI |
| `VisualEffectHelper.cs` | 비주얼 이펙트 헬퍼 |

##### **UI/Map/**
- `MapManager.cs` - 무한 타일맵 시스템

##### **UI/Multiplayer/**
- `MultiplayerRoomUI.cs` - 멀티플레이어 방 UI

---

#### **📂 Hacking/** - 해킹 미니게임 시스템
| 파일 | 설명 |
|------|------|
| `HackingSystem.cs` | 해킹 시스템 메인 관리자 |
| `HackableObject.cs` | 해킹 가능한 오브젝트 |
| `HackingMinigameBase.cs` | 미니게임 베이스 클래스 |
| `HackingRewardSystem.cs` | 해킹 보상 시스템 |

**미니게임 종류:**
- `NumberSequenceMinigame.cs` - 숫자 순서 맞추기
- `CommandBypassMinigame.cs` - 명령어 우회
- `FrequencyOverrideMinigame.cs` - 주파수 오버라이드
- `NetworkBridgeMinigame.cs` - 네트워크 브릿지
- `SynapseSyncMinigame.cs` - 시냅스 동기화

---

#### **📂 Items/** - 아이템 시스템
| 파일 | 설명 |
|------|------|
| `GoldPickup.cs` | 골드 획득 아이템 |
| `ChestPickup.cs` | 상자 아이템 |
| `DataChip.cs` | 데이터 칩 아이템 |
| `PsychoCorruptionItem.cs` | 사이코 잠식도 아이템 |

---

#### **📂 Exp/** - 경험치 시스템
- `ExpOrb.cs` - 경험치 오브 (플레이어 자동 흡수)

---

#### **📂 Utils/** - 유틸리티
| 파일 | 설명 |
|------|------|
| `ServerSaveSystem.cs` | 서버 연동 세이브 시스템 (Easy Save 3 + 서버 API) |
| `ES3SerializationHelper.cs` | Easy Save 3 직렬화 헬퍼 |

---

#### **📂 Editor/** - 에디터 확장
| 파일 | 설명 |
|------|------|
| `MultiplayerUIBuilder.cs` | 멀티플레이어 UI 빌더 에디터 툴 |
| `UnityHttpSecurityFixer.cs` | Unity HTTP 보안 설정 자동 수정 |

---

#### **기타 루트 레벨 스크립트**
- `CameraController.cs` - 카메라 플레이어 추적 시스템

---

## 📂 기타 폴더 구조

### **Assets/** 주요 폴더

| 폴더 | 설명 |
|------|------|
| **BGM/** | 배경 음악 파일 (12개) |
| **Data/** | 게임 데이터 (ScriptableObject, 무기/적 데이터) |
| **Plugins/** | 외부 플러그인 (Easy Save 3, Protobuf 등) |
| **Prefabs/** | 프리팹 (플레이어, 적, 무기, UI 등) |
| **Resources/** | 런타임 로드 리소스 (스프라이트, 이펙트 등) |
| **Scenes/** | Unity 씬 파일 (메인 메뉴, 게임, 로비 등) |
| **ScriptableObjects/** | ScriptableObject 데이터 파일 |
| **Settings/** | 프로젝트 설정 파일 |
| **TextMesh Pro/** | TextMesh Pro 폰트 및 설정 |

### **루트 레벨 폴더**

| 폴더/파일 | 설명 |
|------|------|
| **ProtoFiles/** | Protobuf 프로토콜 정의 파일 (.proto) |
| **protoc_compiler/** | Protobuf 컴파일러 바이너리 |
| **compile_proto.bat** | Windows용 프로토콜 컴파일 스크립트 |
| **compile_proto.ps1** | PowerShell용 프로토콜 컴파일 스크립트 |
| **ProjectSettings/** | Unity 프로젝트 설정 |
| **Packages/** | Unity 패키지 매니페스트 |
| **.docs/** | 프로젝트 문서 |
| **기획서.txt** | 게임 기획 문서 |
| **내가해야할것.txt** | TODO 리스트 |
| **Project_Memo.txt** | 프로젝트 메모 |

---

## 🎮 주요 기능

### **1. 캐릭터 시스템**
- **해커 (Hacker)**: 원거리 공격 중심, 9종의 전용 무기
- **사이보그 (Cyborg)**: 근접 공격 중심, 2종의 전용 무기
- 사이코 잠식도 시스템 (30%, 60%, 100% 단계별 효과)
- 신경링크 게이지 (R키 필살기)

### **2. 무기 시스템**
- Active 무기: 자동 공격 무기 11종
- Passive 무기: 스탯 증가 아이템
- 무기 레벨업 시스템 (최대 5레벨)

### **3. 적 AI 시스템**
- 4가지 적 타입: Rusher, Shooter, Bomber, Tanker
- 난이도 기반 웨이브 스폰 시스템
- 적 메커니즘 기반 행동 패턴

### **4. 멀티플레이어 (3단계 아키텍처)**
1. **DBManager (HTTP)**: 인증, 사용자 데이터
2. **MultiLobbyManager (WebSocket)**: 로비 시스템, 채팅
3. **CoopManager (UDP)**: 실시간 게임 동기화

### **5. 해킹 미니게임**
- 5가지 미니게임 변형
- 보상 시스템 연동

### **6. 업그레이드 시스템**
- 골드 기반 영구 스탯 강화
- 서버 연동 저장 (Easy Save 3)

### **7. 무한 맵 시스템**
- 타일 기반 무한 맵 생성
- 플레이어 추적 카메라

---

## 🛠️ 사용 기술

- **Unity 2022.3 LTS** (Universal Render Pipeline)
- **C# (.NET Standard 2.1)**
- **Easy Save 3** - 데이터 직렬화 및 저장
- **Protobuf (Google)** - 네트워크 프로토콜
- **WebSocket** - 로비 실시간 통신
- **UDP** - 게임 실시간 동기화

---

## 📝 개발 노트

### **네트워크 아키텍처 변경 이력**
- **기존**: `GameServerAPI` + `NetworkManager` 통합 구조
- **변경**: `DBManager`, `MultiLobbyManager`, `CoopManager` 3단계 분리
- **목적**: 역할 명확화 및 유지보수성 향상

### **레거시 코드**
다음 파일들은 Obsolete 처리되었거나 사용되지 않을 가능성이 있습니다:
- `Multiplayer/GameServerAPI.cs` (DBManager로 대체)
- `Network/NetworkManager.cs` (기능 분산)

### **프로토콜 컴파일**
Protobuf 파일 변경 시:
```bash
# Windows
compile_proto.bat

# PowerShell
./compile_proto.ps1
```

---

## 📊 프로젝트 통계

- **총 C# 스크립트**: 87개
- **주요 폴더**: 10개
- **무기 종류**: 11개
- **적 타입**: 4개
- **미니게임**: 5개

---

## 📚 프로젝트 문서

### **ProtoFiles/** - Protobuf 관련 문서
| 파일 | 설명 |
|------|------|
| `GamePacket.proto` | 게임 프로토콜 정의 파일 |
| `README_PROTOBUF.md` | Protobuf 사용 가이드 |
| `INSTALLATION_COMPLETE.md` | 프로토콜 설치 완료 가이드 |
| `TROUBLESHOOTING.md` | 문제 해결 가이드 |
| `UDP_CLIENT_GUIDE.md` | UDP 클라이언트 개발 가이드 |

### **루트 문서 파일**
| 파일 | 설명 |
|------|------|
| `기획서.txt` | 게임 기획 문서 (세계관, 캐릭터, 무기 스펙) |
| `내가해야할것.txt` | 개발 TODO 리스트 |
| `Project_Memo.txt` | 프로젝트 개발 메모 |

---

## 🔍 코드 사용 현황 분석

### **✅ 모든 스크립트가 현재 사용 중**
프로젝트의 모든 87개 C# 스크립트는 현재 사용 중입니다.

#### **Network 폴더 - 모두 사용 중**
- ✅ `NetworkManager.cs` - **사용 중** (MultiplayerRoomUI에서 Lobby 통합 접근용으로 활발히 사용)
- ✅ `DBManager.cs` - **사용 중** (HTTP 인증 및 데이터 관리)
- ✅ `MultiLobbyManager.cs` - **사용 중** (WebSocket 로비 기능)
- ✅ `CoopManager.cs` - **사용 중** (UDP 실시간 동기화)
- ✅ `WebSocketManager.cs` - **사용 중** (WebSocket 저수준 통신)

#### **Multiplayer 폴더 - 모두 사용 중**
- ✅ `GameServerAPI.cs` - **사용 중** (ServerSaveSystem, NicknameChangeUI, LeaderboardUI, GameManager에서 참조)
  - DBManager와 병행 사용 중
  - CreatePostRequest 등 유틸리티 메서드 제공
  
#### **Editor 폴더 - 모두 사용 중**
- ✅ `MultiplayerUIBuilder.cs` - **사용 중** (Unity 에디터 툴)
- ✅ `UnityHttpSecurityFixer.cs` - **사용 중** (개발 환경 HTTP 설정 자동화)

### **📝 네트워크 아키텍처 현황**
현재 프로젝트는 **이중 구조**로 운영 중입니다:

1. **신규 구조 (3단계 분리)**
   - `NetworkManager` → `DBManager`, `MultiLobbyManager`, `CoopManager` 통합 관리
   - UI에서 `NetworkManager.Instance.Lobby` 형태로 접근
   
2. **레거시 API**
   - `GameServerAPI` - 여전히 ServerSaveSystem과 일부 UI에서 직접 사용
   - 완전한 마이그레이션 전 과도기 상태

### **⚠️ 주의사항**
- 현재 `GameServerAPI`와 `DBManager`가 일부 기능 중복
- 향후 리팩토링 시 `GameServerAPI`를 `DBManager`로 완전 통합 검토 필요

---

## 📜 라이선스

이 프로젝트는 개인 프로젝트입니다.

---

## 👤 작성자

**lhs74** - NeoSurvive 개발자

최종 업데이트: 2026-01-24
