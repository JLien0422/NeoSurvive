# 무기 장착 및 공격 동기화 시스템

## 개요
플레이어가 무기를 장착하거나 공격할 때 서버를 통해 다른 플레이어에게 동기화하는 시스템입니다.

## 구현된 기능

### 1. 프로토콜 확장
`ProtoFiles/GamePacket.proto`에 다음 항목이 추가되었습니다:
- `ActionType.WEAPON_EQUIP` (8): 무기 장착 동기화
- `ActionType.WEAPON_ATTACK` (9): 무기 공격 동기화
- `PlayerAction.weapon_id`: 무기 식별자 필드

### 2. WeaponManager 수정사항

#### 무기 장착 시 서버 알림
```csharp
// 로컬 플레이어가 무기를 장착하면 자동으로 서버에 알림
public void AddWeapon(WeaponBase weaponData, bool sendToServer = true)
```

**동작 방식:**
1. 로컬 플레이어가 F1-F10 키로 무기를 장착
2. `AddWeapon()` 호출 시 자동으로 서버에 `WEAPON_EQUIP` 액션 전송
3. 서버가 다른 플레이어들에게 브로드캐스트
4. 다른 플레이어는 `SyncWeaponEquip()`을 통해 동일한 무기 장착

#### 무기 공격 동기화
```csharp
// 무기 공격을 서버로 전송
public void SendWeaponAttack(int weaponIndex, Vector3 direction)

// 원격 플레이어의 공격 실행
public void ExecuteWeaponAttack(int weaponIndex, Vector3 direction)
```

### 3. Player 클래스 수정사항

#### 무기 장착 동기화
```csharp
/// <summary>
/// [Coop] 원격 플레이어의 무기 장착 동기화
/// </summary>
public void SyncWeaponEquip(int weaponIndex)
{
  var weaponManager = GetComponent<WeaponManager>();
  if (weaponManager != null)
  {
    weaponManager.SyncWeaponEquip(weaponIndex);
  }
}
```

### 4. UDPClient 수정사항

#### HandleAction에 추가된 케이스
```csharp
case ActionType.WeaponEquip:
  player.SyncWeaponEquip((int)action.Value);
  break;

case ActionType.WeaponAttack:
  player.ExecuteAttack((int)action.WeaponId, new Vector2(action.DirX, action.DirY));
  break;
```

## 사용 방법

### 무기 장착 동기화
로컬 플레이어가 무기를 장착하면 자동으로 동기화됩니다:
```csharp
// 이미 자동으로 서버에 전송됨
weaponManager.AddWeapon(0); // F1 키 등으로 호출
```

### 무기 공격 동기화 (선택적)
무기 스크립트에서 공격 시 서버에 알리려면:

```csharp
// 무기 스크립트 내부 (예: DataScrambler.cs)
private void Attack()
{
  // 공격 로직...
  
  // [Coop] 로컬 플레이어인 경우 서버에 공격 알림 (선택적)
  var player = GetComponentInParent<Player>();
  if (player != null && player.IsLocal)
  {
    var weaponManager = player.GetComponent<WeaponManager>();
    if (weaponManager != null)
    {
      // 현재 무기의 인덱스 가져오기
      int weaponIndex = GetWeaponIndex(weaponManager);
      if (weaponIndex >= 0)
      {
        // 공격 방향 계산
        Vector3 attackDirection = GetAttackDirection();
        weaponManager.SendWeaponAttack(weaponIndex, attackDirection);
      }
    }
  }
}

private int GetWeaponIndex(WeaponManager manager)
{
  // WeaponBase 데이터 찾기 (구현 필요)
  // 예: manager.allWeaponDatas.IndexOf(myWeaponData);
  return -1;
}

private Vector3 GetAttackDirection()
{
  // 공격 방향 반환
  return transform.right;
}
```

## 주의사항

### 1. 무기 식별
현재 시스템은 `WeaponManager.allWeaponDatas` 리스트의 **인덱스**를 무기 식별자로 사용합니다.
- 모든 클라이언트가 동일한 순서의 `allWeaponDatas` 리스트를 가져야 합니다.
- 리스트 순서가 다르면 잘못된 무기가 동기화될 수 있습니다.

### 2. 공격 동기화 빈도
자동 무기의 경우 굉장히 빈번하게 공격할 수 있으므로:
- **모든 공격을 동기화하면 네트워크 부하가 클 수 있습니다**
- 선택적으로 중요한 공격만 동기화하거나
- 공격 빈도를 제한(throttle)하는 것을 권장합니다

### 3. 서버 권한
현재는 클라이언트가 직접 공격을 보내는 구조입니다:
- 서버에서 검증 로직이 없으면 치팅에 취약할 수 있습니다
- 중요한 게임인 경우 서버 측 검증 추가를 권장합니다

## 테스트 방법

### 멀티플레이어 테스트
1. 서버 실행
2. 두 개의 클라이언트 연결
3. 한 클라이언트에서 F1-F10 키로 무기 장착
4. 다른 클라이언트에서 동일한 무기가 장착되는지 확인
5. 무기 공격이 동기화되는지 확인 (시각적 효과 등)

## 향후 개선 사항

1. **무기 ID 시스템**: 인덱스 대신 고유 ID 사용
2. **공격 쓰로틀링**: 공격 동기화 빈도 제한
3. **서버 검증**: 무기 장착/공격의 유효성 검증
4. **무기 레벨 동기화**: 현재는 장착만 동기화, 레벨업도 동기화 필요
5. **패시브 아이템 동기화**: 현재는 액티브 무기만 지원
