using UnityEngine;
using System.Collections.Generic;

public enum WeaponType
{
  AIDrone = 0,
  AutoTurret,
  LinkPistol,
  EMPShield,
  WEAPON_TYPE_COUNT
}

namespace NeoSurvive.Weapon
{

  /// <summary>
  /// 플레이어가 보유한 모든 무기와 패시브 아이템을 관리하는 매니저
  /// </summary>
  public class WeaponManager : MonoBehaviour
  {
    [Header("Weapon Data")]
    public List<WeaponBase> allWeaponDatas = new(); // 모든 가능한 무 데이터 (DB 역할)

    [Header("Active Weapons")]
    public List<WeaponBase> activeWeapons = new(); // 플레이어가 현재 보유한 무기 데이터
    private Dictionary<WeaponBase, GameObject> spawnedWeapons = new(); // 데이터별 생성된 실제 무기 오브젝트

    public List<Passive> activePassives = new();

    public static event System.Action<List<WeaponBase>> OnWeaponChanged;

    /// <summary>
    /// 새로운 무기 추가 또는 레벨업
    /// </summary>
    public void AddWeapon(WeaponBase weaponData)
    {
      Debug.Log($"[WeaponManager] AddWeapon called. weaponName={weaponData.weaponName}, prefab={(weaponData.weaponPrefab ? weaponData.weaponPrefab.name : "NULL")}");
      // 이미 보유 중인 무기인지 확인
      if (activeWeapons.Contains(weaponData))
      {
        // 레벨업 로직
        weaponData.level++;
        OnWeaponChanged?.Invoke(activeWeapons);

        // 생성된 무기 오브젝트에 레벨업 알림
        if (spawnedWeapons.TryGetValue(weaponData, out GameObject existingWeapon))
        {
          existingWeapon.SendMessage("OnLevelUp", weaponData.level, SendMessageOptions.DontRequireReceiver);
        }
        return;
      }

      // 새로운 무기 추가
      activeWeapons.Add(weaponData);
      // 초기 레벨 설정 (혹시 모르니)
      weaponData.level = 1;

      GameObject weaponObj = null;
      if (weaponData.weaponPrefab != null)
      {
        if (weaponData.isIndependent)
        {
          weaponObj = Instantiate(weaponData.weaponPrefab, transform.position, Quaternion.identity);
        }
        else
        {
          weaponObj = Instantiate(weaponData.weaponPrefab, transform);
        }
        spawnedWeapons.Add(weaponData, weaponObj);

        // 초기화 알림
        weaponObj.SendMessage("OnLevelUp", 1, SendMessageOptions.DontRequireReceiver);
      }

      OnWeaponChanged?.Invoke(activeWeapons);
    }

    // 기존 호환성을 위한 오버로드 (필요시)
    public void AddWeapon(int index)
    {
      if (index >= 0 && index < allWeaponDatas.Count)
      {
        AddWeapon(allWeaponDatas[index]);
      }
    }

    
    void Update()
    {
      // 테스트용: allWeaponDatas 리스트의 인덱스를 사용하여 테스트
      if (Input.GetKeyDown(KeyCode.F1))
      {
        AddWeapon(0);
      }
      if (Input.GetKeyDown(KeyCode.F2))
      {
        AddWeapon(1);
      }
      if (Input.GetKeyDown(KeyCode.F3))
      {
        AddWeapon(2);
      }
      if (Input.GetKeyDown(KeyCode.F4))
      {
        AddWeapon(3);
      }
      if (Input.GetKeyDown(KeyCode.F5))
      {
        AddWeapon(4);
      }
      if (Input.GetKeyDown(KeyCode.F6))
      {
        AddWeapon(5);
      }
      if (Input.GetKeyDown(KeyCode.F7))
      {
        AddWeapon(6);
      }
      if (Input.GetKeyDown(KeyCode.F8))
      {
        AddWeapon(7);
      }
      if (Input.GetKeyDown(KeyCode.F9))
      {
        Debug.Log("[WeaponManager] F9 pressed -> AddWeapon(8)");
        AddWeapon(8);
      }
      if (Input.GetKeyDown(KeyCode.F10))
      {
        AddWeapon(9);
      }
    }
  }
}
