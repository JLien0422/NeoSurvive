# 무기 피격 이펙트 추가 — 2026-11-10

## 범위

- 원본 NeoSurvive / dev/synergy에서 작업함. 커밋·푸시하지 않음.
- 기존 발사, 궤적, 장판, 버프, 폭발 연출과 기존 hun0fx 연결은 교체하지 않음.
- 몬스터 피해 처리 직후 미적용 피격 지점 10곳에 추가함. 브레스 냉기 모드를 포함해 11개 설정임.
- 피해량, 사거리, 공격 주기, 넉백, 상태이상, UI 및 캐릭터 선택창은 변경하지 않음.

## 새 연결

아래 무기 프리팹 경로의 기준은 `Assets/Prefabs/Objects/Weapons/`임.

| 무기 / 판정 | 프리팹 | hun0fx | 최대 표시시간 |
| --- | --- | --- | --- |
| EMP 펄스 | Hacker/Effects/GFX_EMP_Pulse.prefab | Hit_01_Electricity | 0.22초 |
| 디지털 실드 | Hacker/Effects/ShieldWave.prefab | Hit_01_water | 0.22초 |
| AI 드론 마스터 레이저망 | Hacker/DroneLaserNet.prefab | Hit_01_Electricity | 0.18초 |
| 체인소 | Cyborg/ChainSaw.prefab | Hit_02_7 | 0.16초 |
| 중력 망치 | Cyborg/GravityHammer.prefab | Hit_01_normal (기존 에셋 재사용) | 0.28초 |
| 서지 블레이드 | Cyborg/SurgeBlade.prefab | Hit_02_7 | 0.22초 |
| 테슬라 오라 / 낙뢰 피해 | Cyborg/TeslaCoilArmor.prefab | Hit_01_Electricity | 0.18초 |
| 에너지 실드 접촉 | Cyborg/Effects/ShieldOrb.prefab | Hit_01_Electricity | 0.08초 |
| 블래스트 브레스 일반 / 냉기 | Cyborg/Effects/BreathArea.prefab | Hit_01_Fire / Hit_01_Ice | 각각 0.18초 |
| 플라즈마 포톤 실제 빔 판정 | 아래 별도 경로 | Hit_01_Crimson (기존 에셋 재사용) | 0.22초 |

포톤 실제 빔은 `Assets/Art/Animations/Objects/Weapons/Cyborg/PlasmaPhoton/Plasma Photon Beam.prefab`임. 기존 `PlasmaPhotonProjectile`의 피격 연결은 그대로 유지함.

## 원본 / 편집 위치

- 원본 루트: `Assets/Art/Animations/Objects/Weapons/Shared/Hun0fxOriginal/`
- `png/`: hun0fx가 내보낸 원본 시트.
- `json/`: 원본 프레임 정보와 시트.
- `unity/`: Unity 슬라이스 메타 포함 시트.
- `unity/Prefabs/`: 새 5종의 `.prefab`, `.anim`, `.controller`.
- 새 효과: Hit_01_Electricity, Hit_01_water, Hit_01_Fire, Hit_01_Ice, Hit_02_7.
- hun0fx 설정: 프레임 96×96, 15fps, 시점 45°, 시드 1, 32 PPU, 원본 팔레트. 전자 4종은 14프레임, Hit_02_7은 16프레임. 시트는 384×384임.
- 원본 이미지를 리사이즈하거나 재색칠하지 않음. 새 무기 연결의 `WeaponHitVfx.scale`은 (0.5, 0.5, 1), tint는 흰색임. Inspector에서 수정 가능함.
- `BreathArea`의 자식 `ColdHitVfx`는 냉기 피격 설정이며, `coldHitVfx`에 연결함.

## 시간 / 기존 코드와 연결

- 기존 `WeaponHitVfx`를 재사용함. 별도 이펙트 매니저나 런타임 UI 생성 코드를 추가하지 않음.
- `Play`에 선택적 `maxLifetime`만 추가함. 기존 2인자 호출은 기존 표시시간을 유지함.
- 지속 피해는 `min(Inspector 표시시간, 실제 타격간격 × 0.8)`로 재생시간을 제한함. 피해 주기는 수정하지 않음.
- 배속은 기존 방식 `max(animationSpeed, 클립 길이 / 표시시간)`임. 새 단발 클립은 마지막 키에서 Sprite를 비우고 루프하지 않음.
- 에너지 실드는 매 물리 프레임 접촉 판정이 발생하므로, 실드 하나당 피격 VFX 생성에만 0.1초 간격을 둠. 동시 다수 적 접촉 시 일부 시각 효과가 생략될 수 있음. 데미지는 생략하지 않음.
- 이펙트는 적 자식으로 붙이지 않고 월드에 생성해, 적 사망 후 따라다니지 않음. 지정 시간이 지나면 제거함.
- 기존 EMP 펄스 프리팹에 실체 없는 컴포넌트 fileID가 있었음. 프리팹 저장 과정에서 끊어진 참조가 제거되고 실제 VFX 컴포넌트가 직렬화됨. 기존 Animator 연결은 유지함.

## 유지한 기존 연출

- 링크 피스톨 / AI 드론 일반 탄 / 터렛: 기존 투사체 연결 유지함. 실제 드론과 터렛은 링크 피스톨 투사체를 공유함.
- 해커 플라즈마 라이플 Beam, EMP 수류탄 Meteor 폭발: 유지함.
- 사이보그 레이저 소드, 볼트, 부스터 너클, 구형 포톤 투사체의 피격: 유지함.
- 데이터 스크램블러 공격·폭발, 데이터 최적화 시작·유지·종료, 홀로그램 디코이 본체·장판: 기존 연출 유지함.
- 그 외 기존 무기 발사·장판·스윙 연출도 유지함. 비공격 패시브에 임의의 피격 판정을 만들지 않음.

## 검증과 남은 확인

- Unity MCP에서 실제 원본 프로젝트, 편집 상태와 연결된 프리팹을 확인함.
- C# 빌드: 오류 0, 경고 6 (어셈블리 버전 참조 및 기존 필드 경고).
- 신규 5종 원본의 알파를 검사함. 불투명 검은 사각형에 해당하는 대량 픽셀 없음.
- 신규 클립 모든 프레임 참조, 마지막 빈 Sprite, 비루프 설정을 확인함.
- `Tools/WeaponVfx/VerifyHitEffects.cs.txt`: Unity MCP execute_code에서 사용하는 Play Mode 자가검사임. Game View를 포커스하고 재생 상태에서 실행함. 결과는 `SessionState.GetString("NeoSurvive.VfxQA", "missing")`로 확인함.
- 최종 Play Mode 격리검사: 11개 설정, 실패 0. 생성 수, 배속, 월드 분리, 자동 소멸 확인함. 처음에는 일시정지/백그라운드 상태로 게임 시간이 멈춰 검사 시간초과가 났고, 정상 재생 후 재검사 통과함. 이는 전체 전투 QA 완료를 의미하지 않음.
- 실제 전투에서 캐릭터별 Lv1~5, 밀집 몬스터, 연속 타격, 사망, 시인성, 프레임 비용을 추가 QA해야 함.
- 기존 테슬라 `puddleAreaPrefab`이 비어 있음. 장판 VFX는 연결돼 있으나 별도 `ElectricPuddle` 피해 생성은 실행되지 않음. 전투 기능 변경이므로 이번 작업에서 임의 연결하지 않음.
- 로비 씬의 저장되지 않은 변경은 저장/폐기하지 않고 보존함. 새 씬을 저장하지 않음.
