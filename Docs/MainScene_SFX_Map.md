# MainScene SFX Map (No BGM)

`LobbySoundManager`와 완전히 분리된 인게임 SFX 전용 구조입니다.

## 1) Scene setup

- Scene object: `MainSceneSoundManager` 1개
- Script: `MainSceneSoundManager`
- AudioSource: 2D(one-shot), loop off
- BGM은 기존 `SoundManager`에서 별도 관리 (이 문서 범위 제외)

## 2) Generic SFX slots (필수)

- `PlayerHit`
- `PlayerDeath`
- `EnemyHit`
- `EnemyDeath`
- `ChestOpened`
- `GoldPickup`
- `ExpPickup`
- `DataChipPickup`
- `PsychoCorruptionPickup`
- `WeaponEquipGeneric`
- `WeaponLevelUpGeneric`
- `WeaponUseGeneric`
- `WeaponHitGeneric`
- `Error`

## 3) Weapon SFX slots (해커/사이보그 8~10개씩)

`weaponId` 기준으로 슬롯을 채우면 됩니다.

각 슬롯에서 설정 가능:

- `equipClips`: 무기 획득/장착
- `levelUpClips`: 무기 레벨업
- `useClips`: 무기 사용(발사/시전)
- `hitClips`: 무기 적중

권장 세팅:

- 해커 무기 8~10개: 해당 weaponId들 등록
- 사이보그 무기 8~10개: 해당 weaponId들 등록
- 매칭 실패 시 Generic 슬롯 자동 폴백

## 4) 이미 연결된 코드 포인트

- 피격/사망: `Character.TakeDamage`, `Character.Die`
- 무기 획득/레벨업: `WeaponManager.AddWeapon`
- 무기 사용: `WeaponRuntimeInfo.Report*`
- 무기 적중: `Projectile.OnTriggerEnter2D`
- 상자 개봉: `ChestPickup.Open`
- 픽업: `GoldPickup`, `ExpOrb`, `DataChip`, `PsychoCorruptionItem`

## 5) 운영 팁

- UI 소리와 절대 섞지 말고 `LobbySoundManager`는 로비에서만 사용
- 인게임 SFX는 0.05~0.35초 중심의 짧은 클립 권장
- 반복 액션(피격/발사)은 2~4개 variation + randomPitch로 피로도 감소
