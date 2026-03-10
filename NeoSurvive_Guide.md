# NeoSurvive - AI 작업 가이드 (완전판)

---

## ⚠️ 필수 규칙 (절대 준수)

0. **절대 혼자 독단으로 개발하지 않는다**
   - 코드 수정 전 반드시 알고리즘/수정 내용을 설명하고 사용자 확인을 받는다
   - 수정 전 항상 "이렇게 진행할까요?" 또는 "수정할까요?" 를 먼저 물어본다

1. **폴더 구조와 기존 코드를 확실하고 정확하게 파악한다**
   - 작업 전 관련 파일을 반드시 읽고 이해한다
   - 모르는 부분은 추측하지 말고 파일을 직접 확인한다

2. **모든 코드에 한국어 주석을 반드시 기재한다**

3. **지시와 설명을 명확하게 이해했는지 설명한다**
   - 사용자의 지시를 이해한 내용을 간단히 요약해서 확인받는다

4. **Unity 에디터 수정이 필요할 때 진행 방향을 명확하게 안내한다**
   - 인스펙터, 하이어라키 등 에디터 작업이 필요하면 단계별로 안내한다

5. **항상 한국어로 작성해주고, 변경 사항을 전부 빠짐없이, 명확하게 설명해줘. Bullet point로 작성하고, 매 변경사항마다 어떤 파일이 변경되었는지 언급해줘. (예시) - 어떤 사항이 변경되었습니다\n  - src/code1.tsx\n - src/code2.tsx\n"
---

## 프로젝트 기본 정보

- **프로젝트명**: NeoSurvive (Neo Survive)
- **장르**: 2D 픽셀아트, 사이버펑크, 뱀서류(Vampire Survivors류), 로그라이크 액션 슈터
- **플랫폼**: PC
- **개발 기간**: 2026-01-05 ~ 2026-02-23
- **팀**: 정민균(팀장), 맹진호, 이현승
- **기획서 위치**: `C:\Users\a3435\NeoSurvive\기획서.txt`
- **세계관**: 2077년 Neo City, AI 폭주로 혼란에 빠진 사이버펑크 미래도시
- **플레이어**: 해커(Hacker) or 사이보그(Cyborg) 중 선택

---

## 게임 핵심 구조

### 라운드 시스템
- 한 판 = **15분 고정**
- **5 페이즈 + 보스전** 구성 (3분마다 페이즈 전환)
- 각 페이즈 전환 시: **포위 패턴** 발생 + **해킹 오브젝트 1종 랜덤 생성**
- 해킹 성공 → 해킹 보상 효과 / 실패 → 사이코 잠식도 +50%

### 페이즈별 구성
| 페이즈 | 시간 | 추가 적 유형 | 스폰 가중치 |
|--------|------|------------|------------|
| Phase 1 | 0~3분 | Basic | Basic 100 |
| Phase 2 | 3~6분 | Shooter 추가 | Basic 60 / Shooter 40 |
| Phase 3 | 6~9분 | Rusher 추가 | Basic 50 / Shooter 30 / Rusher 20 |
| Phase 4 | 9~12분 | Bomber 추가 | Basic 40 / Shooter 30 / Rusher 15 / Bomber 15 |
| Phase 5 | 12~15분 | Tanker 추가 | Basic 35 / Shooter 25 / Rusher 15 / Bomber 15 / Tanker 10 |
| Phase 6 | 15분~ | 최종 보스전 | 일반 몬스터 소멸 |

---

## 적 5종 상세

| 유형 | 설명 | 특성 | 스크립트 |
|------|------|------|---------|
| Basic | 직선 이동 추적 | 가장 많은 물량, 낮은 체력 | EnemyController (기본) |
| Shooter | 원거리 투사체 공격 | 3초마다 에너지 탄환 발사, 긴 사거리 | ShooterMechanism.cs |
| Rusher | 고속 돌진 | 1.5배 빠름, 체력 매우 낮음, 가속 돌진 | RusherMechanism.cs |
| Bomber | 사망 시 폭발 | 1초 딜레이 후 원형 폭발, 거리 비례 데미지 감쇠 | BomberMechanism.cs |
| Tanker | 느리고 튼튼한 벽 | 속도 0.3배, 체력 2배, 이동 경로 차단 | TankerMechanism.cs |

---

## 해킹 시스템

### 해커 vs 사이보그 해킹 차이
| 항목 | 해커 | 사이보그 |
|------|------|---------|
| 시간 | 완전 정지 | 계속 진행 |
| 적 | 모두 정지 | 계속 공격 |
| 미니게임 | 플레이어가 직접 | 해커 AI가 자동 진행 |
| 수비 | 없음 (무적) | 플레이어가 원형 수비 영역 방어 |
| 해킹 시간 | 10초 | 10초 (적이 원 안에 있으면 진행 멈춤) |

### 사이보그 해킹 규칙
- 1초마다 진행도 20% 증가
- 원 안에 적이 있으면 진행도 오르지 않음
- 5초 동안 적 없어야 완료 (5초 동안 적이 있으면 실패)

### 해킹 미니게임 5종
1. **커맨드 바이패스** - 10초 안에 방향키 20개 입력
2. **넘버 시퀀스** - 10초 안에 숫자 1~9 순서대로 클릭
3. **네트워크 브릿지** - 10초 안에 색상 단자 5쌍 선으로 연결
4. **시냅스 동기화** - 10초 안에 타이밍 맞춰 버튼 5회 누르기
5. **주파수 오버라이드** - 파형을 목표와 일치시켜 유지

### 해킹 오브젝트 5종 (랜덤 출현)
| 오브젝트 | 효과 | 스크립트 | 프리팹 경로 |
|---------|------|---------|-----------|
| 보안 터렛 (Security Turret) | 가장 가까운 적 고속 연사 섬멸 | SecurityTurretEffect.cs | Prefabs/Objects/Hacking/SecurityTurret.prefab |
| 전기 울타리 (Electric Fence) | 플레이어 주변 사각형 전기벽 생성, 접촉 적 기절+데미지 | ElectricFenceEffect.cs | Prefabs/Objects/Hacking/ElectricFence.prefab |
| 새틀라이트 통신기 (Satellite Uplink) | 가장 가까운 적 자동추적 레이저 빔 (10초) | SatelliteUplinkEffect.cs | Prefabs/Objects/Hacking/SatelliteUplink.prefab |
| 시냅스 서버 (Synapse Server) | 화면 내 적 절반 광란 (서로 공격, 초록색으로 표시) | SynapseServerEffect.cs | Prefabs/Objects/Hacking/SynapseServer.prefab |
| 마그네틱 비컨 (Magnetic Beacon) | 모든 적을 플레이어 옆 한 점으로 집결 견인 | MagneticBeaconEffect.cs | Prefabs/Objects/Hacking/MagneticBeacon.prefab |

**프리팹에 효과 스크립트를 붙이면 인스펙터에서 파라미터 수정 가능**
- HackableObject.ActivateEffect()에서 GetComponent로 가져옴 (없으면 AddComponent 기본값)

---

## 플레이어 시스템

### Player.cs 주요 내용
- **기본 스탯**: moveSpeed(5f), attackDamage(10f), attackRange(3f), attackSpeed(1.0f)
- **경험치/레벨**: GainExperience() → CheckLevelUp() → 레벨업 시 무기 선택 UI
- **IsLocal**: true = 로컬(싱글/로컬 멀티), false = 원격 플레이어

### 사이코 잠식도 (Psycho Corrosion)
- 해킹 실패 시 +50%, 일부 아이템 습득 시 상승
- **30%**: 이동속도/공격속도 +25%
- **60%**: 대미지 +25%, 화면 노이즈 효과
- **100%**: 폭주(Berserk) 15초 - 통제불능, 스탯 +50%, 체력 30% 감소
- 감소: 맵 드롭 아이템('뉴로-진정제') 매우 희귀하게 드롭

### 신경 링크 (Neural Link) - R키 발동
- 데이터칩 획득으로 게이지 충전, 100%일 때 발동
- **해커 플레이**: 사이보그 난입 + 지면 강타 + 화면 내 적 스턴 5초
- **사이보그 플레이**: 해커 데이터 스톰 + 화면 내 적 마비 5초

---

## 무기 시스템

### 해커 무기 10종
| 번호 | 이름 | 공격 방식 | ID |
|------|------|---------|-----|
| 1 | 링크 피스톨 (기본) | 가장 가까운 적 단발 사격 | 1 |
| 2 | 플라즈마 라이플 | 직선 관통 에너지 탄환 | 2 |
| 3 | 데이터 스크램블러 | 부채꼴 범위 교란 신호 | 3 |
| 4 | 전술형 AI 드론 | 주변 적 자동 추적 사격 | 4 |
| 5 | 자동 포탑 | 고정 위치 고속 연사 | 5 |
| 6 | EMP 펄스 생성기 | 주기적 원형 충격파 | 6 |
| 7 | 나노봇 클라우드 | 지속 피해 구름 생성 | - |
| 8 | 궤도 해킹 위성 | 무작위 적에 수직 레이저 | - |
| 9 | 홀로그램 디코이 | 적 유인 분신 소환 | 7 |
| 10 | 나노 전선 | 지나간 자리에 전선 설치 | 8 |

### 사이보그 무기 10종
| 번호 | 이름 | 공격 방식 | ID |
|------|------|---------|-----|
| 1 | 레이저 검 (기본) | 전방 부채꼴 물리 베기 | 11 |
| 2 | 서지 블레이드 | 강력한 한방 단일 타격 | 12 |
| 3 | 에너지 방패 | 회전하며 적/투사체 방어 | 13 |
| 4 | 플라즈마 포톤 | 직선 강력 에너지 빔 | 14 |
| 5 | 중력 해머 | 지면 강타 충격파 | 15 |
| 6 | 부스터 너클 | 로켓 추진 주먹 발사 | 16 |
| 7 | 테슬라 코일 아머 | 몸 주변 전류 방출 | 17 |
| 8 | 볼트 런처 | 다수 유도 에너지 화살 | 18 |
| 9 | 체인 쏘우 | 회전 톱날 지속 다단히트 | 19 |
| 10 | 블래스트 브레스 | 전방 화염 방사 | 20 |

---

## 아이템 시스템

### 아이템 종류
| 아이템 | 스크립트 | 효과 | 프리팹 |
|--------|---------|------|--------|
| 경험치 오브 | ExpOrb.cs | 경험치 지급 | Prefabs/Objects/Player/items/Exp Orb.prefab |
| 골드 코인 | GoldPickup.cs | 골드 추가 | Prefabs/Objects/Player/items/GoldCoin.prefab |
| 데이터칩 | DataChip.cs | 신경링크 게이지 +10% | Prefabs/Objects/Player/items/DataChip.prefab |
| 사이코잠식도 아이템 | PsychoCorruptionItem.cs | 잠식도 +10% | Prefabs/Objects/Player/items/PsychoCorruption.prefab |
| 상자 | ChestPickup.cs | 무기 선택 UI 표시 | Prefabs/Objects/Player/items/Chest.prefab |

### 거리기반 픽업 시스템 (콜라이더 없음)
- **DistanceManager.cs** 싱글톤이 0.1초마다 플레이어와 모든 아이템 거리 체크
- 아이템은 OnEnable에서 Register(), OnDisable에서 Unregister()
- **IPickupable 인터페이스**: Transform, PickupRange, Pickup(Player) 구현 필요
- 아이템 프리팹에서 Trigger Collider2D 제거 완료

### 아이템 이미지
- 위치: `Assets/Resources/Image/Item/`
- 32x32 도트 픽셀아트, Filter Mode → **Point (no filter)** 설정 필요
- 종류: 힐팩(노랑/빨강/파랑), 에너지팩(블루/초록/보라), 칩(레드/블루/청록), 주사기

---

## 버프/디버프 시스템

### StatusFlags.cs (NeoSurvive.Buff 네임스페이스)
```
moveBlocked       - 이동 불가 (속박/기절)
attackBlocked     - 공격 불가 (기절)
blinded           - 실명 (50% 확률로 타겟 놓침)
frenzy            - 광란 (가장 가까운 적을 공격)
isPulled          - 마그네틱 비컨 견인 중 (AI 이동 무시)
pullVelocity      - 견인 방향 벡터
moveSpeedMul      - 이동속도 배율 (기본 1f)
outgoingDamageMul - 가하는 피해 배율 (기본 1f)
incomingDamageMul - 받는 피해 배율 (기본 1f)
```

### EnemyController.cs FixedUpdate 처리 우선순위
1. isPulled → pullVelocity 적용 후 return
2. moveBlocked → velocity = 0 후 return
3. 메커니즘 UpdateMovement() 또는 기본 이동 로직

---

## 최적화 현황 (거리기반)

### 완료된 최적화
| 시스템 | 이전 | 현재 |
|--------|------|------|
| 아이템 픽업 | OnTriggerEnter2D + Collider | DistanceManager 0.1초 간격 |
| HackableObject 범위 | FindObjectOfType 매프레임 + Distance | 플레이어 캐시 + sqrMagnitude + 0.1초 타이머 |
| 적 공격 판정 | Vector2.Distance | sqrMagnitude |
| 적 이동 거리 비교 | Vector2.Distance | sqrMagnitude |
| 타겟 탐색 | Vector2.Distance | sqrMagnitude |

### 콜라이더 유지 대상 (변경 금지)
- **투사체 히트 판정**: 빠른 속도로 인한 터널링 방지
- **물리 충돌 벽/경계**: 물리적 이동 차단 필요
- **BomberMechanism 폭발**: 거리 비례 데미지 감쇠에 실제 거리 필요

---

## 폴더 구조 (전체)

```
Assets/
├── Prefabs/
│   ├── Objects/
│   │   ├── Hacking/              # SecurityTurret, ElectricFence, SatelliteUplink, SynapseServer, MagneticBeacon
│   │   ├── Enemy/                # Basic_Enemy, Tanker, Shooter, Rusher, Bomber
│   │   └── Player/
│   │       ├── Player.prefab
│   │       └── items/            # Exp Orb, GoldCoin, DataChip, PsychoCorruption, Chest
│   ├── Weapons/
│   │   ├── Hacker/               # PlasmaRifle, DataScrambler, AIDrone, AutoTurret 등
│   │   └── Cyborg/               # LaserSword, SurgeBlade, EnergyShield 등
│   ├── Manager/
│   │   ├── MultiplayerIngameManagers.prefab
│   │   └── MultiplayerLobbyManagers.prefab
│   └── UI/
├── Scripts/
│   ├── Characters/
│   │   ├── Player/
│   │   │   ├── Player.cs             # 스탯, 경험치, 잠식도, 신경링크
│   │   │   ├── PlayerController.cs   # 입력, 이동, 애니메이션(isMoving, flipX)
│   │   │   ├── WeaponManager.cs
│   │   │   ├── PlayerClassTag.cs
│   │   │   └── PlayerClassSelection.cs
│   │   ├── Enemy/
│   │   │   ├── Enemy.cs              # 적 스탯, 드롭(ExpOrb, Gold, DataChip, PsychoCorruption)
│   │   │   ├── EnemyController.cs    # AI, 이동(sqrMagnitude), 공격, StatusFlags 처리
│   │   │   ├── EnemyMechanismBase.cs
│   │   │   └── Mechanisms/
│   │   │       ├── ShooterMechanism.cs   # 원거리, sqrMagnitude 최적화 완료
│   │   │       ├── RusherMechanism.cs    # 돌진형, sqrMagnitude + 가속도 실제거리 혼용
│   │   │       ├── BomberMechanism.cs    # 폭발형, 데미지감쇠에 Vector2.Distance 유지
│   │   │       └── TankerMechanism.cs    # 탱커형
│   │   ├── Character.cs              # 베이스 클래스 (HP, TakeDamage, Die)
│   │   └── Buff/
│   │       ├── IBuff.cs
│   │       ├── StatusFlags.cs        # CC, 배율 저장소
│   │       └── BuffHandler.cs
│   ├── Hacking/
│   │   ├── HackingSystem.cs          # 해킹 관리 싱글톤 (해커/사이보그 분기)
│   │   ├── HackableObject.cs         # E키 감지, sqrMagnitude 범위 체크, 플레이어 캐시
│   │   ├── HackableObjectType.cs     # enum
│   │   ├── MiniGame/
│   │   │   ├── HackingMinigameBase.cs
│   │   │   ├── CommandBypassMinigame.cs
│   │   │   ├── NumberSequenceMinigame.cs
│   │   │   ├── NetworkBridgeMinigame.cs
│   │   │   ├── SynapseSyncMinigame.cs
│   │   │   └── FrequencyOverrideMinigame.cs
│   │   └── Effects/
│   │       ├── ElectricFenceEffect.cs      # 기절(moveBlocked+attackBlocked) + 데미지
│   │       ├── SatelliteUplinkEffect.cs    # 자동 적추적 레이저, 10초
│   │       ├── SynapseServerEffect.cs      # HashSet 중복제거, CeilToInt 절반 보장
│   │       ├── MagneticBeaconEffect.cs     # isPulled 플래그, 플레이어 옆 집결
│   │       └── SecurityTurretEffect.cs     # 자동 사격 포탑
│   ├── Items/
│   │   ├── IPickupable.cs            # 픽업 인터페이스
│   │   ├── GoldPickup.cs             # IPickupable 구현, 콜라이더 없음
│   │   ├── DataChip.cs               # IPickupable 구현
│   │   ├── PsychoCorruptionItem.cs   # IPickupable 구현
│   │   └── ChestPickup.cs            # IPickupable 구현
│   ├── Exp/
│   │   └── ExpOrb.cs                 # IPickupable 구현
│   ├── Managers/
│   │   ├── UIManager.cs              # 싱글톤, 모든 UI 관리
│   │   ├── GameManager.cs            # 싱글톤, 골드/킬카운트/캐릭터선택
│   │   ├── DistanceManager.cs        # 싱글톤, 0.1초 거리기반 픽업
│   │   ├── SoundManager.cs
│   │   ├── SettingsManager.cs
│   │   ├── UpgradeManager.cs
│   │   └── DamageTextManager.cs
│   ├── Network/
│   │   ├── ItemManager.cs            # 네트워크 아이템 동기화
│   │   ├── NetworkItem.cs
│   │   └── ItemProxy.cs
│   └── Multiplayer/
│       └── GameProtocol.cs           # Protocol.ItemType enum 등
├── Resources/
│   ├── Image/Item/                   # 32x32 도트 아이템 이미지 10종
│   ├── Animation/
│   │   ├── Character/Player/Hacker/  # Character_1p_run_WH32px.png (6프레임)
│   │   ├── spritesheet_0.controller
│   │   └── New Animation.anim
│   ├── Fonts/                        # Maplestory, Umdot, quaver
│   └── Music/Sprites/
│       ├── HackingObject/            # 미니게임 UI 스프라이트
│       └── UI/
└── Plugins/
    └── Easy Save 3/
```

---

## 네임스페이스 구조

```csharp
NeoSurvive.Exp              // ExpOrb
NeoSurvive.Buff             // StatusFlags, IBuff, BuffHandler
NeoSurvive.Network          // ItemManager, NetworkItem, ItemProxy
NeoSurvive.Network.Protocol // ItemType enum, ActionType enum
NeoSurvive.Weapon           // Projectile 등
NeoSurvive.Characters       // CharacterData, CharacterType
NeoSurvive.UI               // VisualEffectHelper
// 전역 (네임스페이스 없음): Player, Enemy, EnemyController, HackingSystem, UIManager, GameManager 등
```

---

## 주요 컴포넌트 관계

```
Player GameObject
├── Player.cs               스탯/경험치/잠식도/신경링크
├── PlayerController.cs     입력/이동/애니메이션(Animator.isMoving, SpriteRenderer.flipX)
├── WeaponManager.cs        무기 관리
├── StatusFlags.cs          버프/CC 상태
└── Rigidbody2D

Enemy GameObject
├── Enemy.cs                스탯/드롭
├── EnemyController.cs      AI/이동(sqrMagnitude)/공격/타겟
├── [Mechanism].cs          Shooter/Rusher/Bomber/Tanker
├── StatusFlags.cs          isPulled/frenzy/moveBlocked 등
└── Rigidbody2D

HackableObject Prefab
├── HackableObject.cs       E키 감지, 0.1초 거리 체크, 플레이어 캐시
└── [EffectScript].cs       프리팹에 미리 붙이면 인스펙터 파라미터 수정 가능
```

---

## 싱글플레이/멀티플레이 분기

- `player.IsLocal` = true → 로컬 플레이어 (싱글 포함)
- `UDPClient.Instance != null` → 멀티플레이 접속 상태
- 아이템 픽업, 죽음, 신경링크 발동 시 `IsLocal && UDPClient != null` 체크 후 서버 전송
- 데미지 텍스트: 해킹 미니게임 중(`HackingSystem.Instance.IsHacking`) 표시 안 함

---

## Unity 에디터 미완료 작업

### 반드시 해야 할 것
1. **DistanceManager를 씬에 배치**
   - Hierarchy → 빈 GameObject → `DistanceManager` 컴포넌트 추가
   - 또는 `MultiplayerIngameManagers.prefab`에 추가

2. **HackableObject 프리팹 5종에 효과 스크립트 추가** (인스펙터 파라미터 편집용)
   - SecurityTurret.prefab → SecurityTurretEffect
   - ElectricFence.prefab → ElectricFenceEffect
   - SatelliteUplink.prefab → SatelliteUplinkEffect
   - SynapseServer.prefab → SynapseServerEffect
   - MagneticBeacon.prefab → MagneticBeaconEffect

3. **아이템 프리팹 5종 Trigger Collider2D 제거** (거리기반으로 전환됨)
   - Exp Orb, GoldCoin, DataChip, PsychoCorruption, Chest

### 선택적 작업
4. **해커 애니메이션 설정**
   - `Character_1p_run_WH32px.png` → Sprite Mode: Multiple, Filter: Point, 32x32 슬라이싱
   - Animator Controller에 `isMoving` (Bool) 파라미터 추가
   - Idle ↔ Run 전환 설정
   - Player 프리팹에 Animator 컴포넌트 연결

5. **아이템 이미지 설정**
   - `Resources/Image/Item/` 전체 선택 → Filter Mode: Point (no filter) → Apply

---

## 미구현 시스템 (예정)

| 시스템 | 설명 |
|--------|------|
| 페이즈 기반 적 스폰 | 3분마다 포위 패턴 + 해킹 오브젝트 생성 |
| 게임 루프 | 라운드 시작/종료/결과 화면 |
| 보스전 | Phase 6 최종 보스 |
| 맵 3종 | 도심 외곽, 지하 폐기물 처리장, 기업 데이터 타워 |
| 맵 환경 기믹 | 누전 구역, 컨베이어 벨트, 보안 레이저 등 |
| 업그레이드 시스템 (로비) | 영구 스탯 업그레이드 |
| 무기 레벨업 | Lv.5 마스터 임계점 효과 |

---

## 참고사항 (버그/이슈 이력)

- **Random 모호성**: `using System` + `using UnityEngine` 동시 사용 시 `Random` 충돌 → `UnityEngine.Random.Range` 명시
- **데미지 텍스트 UI**: UIManager.ShowDamageText()에서 IsHacking 체크로 해킹 중 숨김 처리
- **한국어 폰트**: CyborgHacking_Root의 StatusText는 Maplestory 폰트가 한글 미지원 → 영어 텍스트 사용 ("HACKING...", "ENEMY DETECTED!")
- **effectHost 코루틴**: gameObject.SetActive(false)는 코루틴 종료시킴 → HideVisuals()로 시각요소만 끄는 방식으로 변경
- **ElectricFence 콜라이더**: BoxCollider2D.size와 localScale 동시 설정 시 크기 제곱됨 → localScale만 사용, size = Vector2.one
