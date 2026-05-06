using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NeoSurvive.Characters;

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
    public List<WeaponBase> allWeaponDatas = new(); // 모든 가능한 무기 데이터 (DB 역할)

    [Header("Active Weapons")]
    public List<WeaponBase> activeWeapons = new(); // 플레이어가 현재 보유한 무기 데이터
    private Dictionary<WeaponBase, GameObject> spawnedWeapons = new(); // 데이터별 생성된 실제 무기 오브젝트

    public static event System.Action<List<WeaponBase>> OnWeaponChanged;

    private WeaponBase FindWeaponById(int weaponId)
    {
      return allWeaponDatas.Find(w => w != null && w.weaponId == weaponId + 1);
    }

    [Header("Class Weapon Sets (Templates)")]
    public List<WeaponBase> cyborgAllWeaponDatas = new();
    public List<WeaponBase> hackerAllWeaponDatas = new();

    [Header("Class Start Weapons (Templates)")]
    public List<WeaponBase> cyborgStartWeapons = new();
    public List<WeaponBase> hackerStartWeapons = new();

    private void Awake()
    {
      ApplyClassLoadout();
      EquipStartWeapons();
    }

    private void ApplyClassLoadout()
    {
      var player = GetComponent<Player>();
      var classType = (player != null && player.CharacterType == CharacterType.Cyborg)
        ? PlayerClassType.Cyborg
        : PlayerClassType.Hacker;

      List<WeaponBase> selectedAll = (classType == PlayerClassType.Cyborg)
        ? cyborgAllWeaponDatas
        : hackerAllWeaponDatas;

      List<WeaponBase> selectedStart = (classType == PlayerClassType.Cyborg)
        ? cyborgStartWeapons
        : hackerStartWeapons;

      allWeaponDatas.Clear();
      if (selectedAll != null)
        allWeaponDatas.AddRange(selectedAll.Where(w => w != null));

      activeWeapons.Clear();
      if (selectedStart != null)
        activeWeapons.AddRange(selectedStart.Where(w => w != null));
    }

    private void EquipStartWeapons()
    {
      if (activeWeapons == null || activeWeapons.Count == 0) return;

      var startList = activeWeapons
        .Where(w => w != null)
        .Distinct()
        .ToList();

      activeWeapons.Clear();

      foreach (var w in startList)
      {
        AddWeapon(w);
      }
    }

    /// <summary>
    /// 새로운 무기 추가 또는 레벨업
    /// </summary>
    public void AddWeapon(WeaponBase weaponData)
    {
      Debug.Log($"[WeaponManager] AddWeapon called. weaponName={weaponData.weaponName}, prefab={(weaponData.weaponPrefab ? weaponData.weaponPrefab.name : "NULL")}");

      // 이미 보유 중인 무기인지 확인
      if (activeWeapons.Contains(weaponData))
      {
        // ★ 추가: 무기 최대 레벨 제한
        const int maxWeaponLevel = 5;

        if (weaponData.level >= maxWeaponLevel)
        {
          Debug.Log($"[WeaponManager] 이미 최대 레벨입니다 | {weaponData.weaponName} Lv.{weaponData.level}");
          OnWeaponChanged?.Invoke(activeWeapons);
          return;
        }

        // 레벨업 로직
        weaponData.level++;

        // ★ [추가] 실제 생성된 무기 오브젝트를 찾기 위한 변수
        GameObject weaponObj;

        if (spawnedWeapons.TryGetValue(weaponData, out weaponObj) && weaponObj != null)
        {
          // ★ [핵심 추가]
          // WeaponManager는 레벨만 올리고,
          // 실제 CSV 스탯 적용은 각 무기의 OnLevelUp(level)에서 처리한다.
          // 예: EMPGrenade.OnLevelUp → ApplyStatsFromCSV(level)
          weaponObj.SendMessage("OnLevelUp", weaponData.level, SendMessageOptions.DontRequireReceiver);
          Debug.Log($"[WeaponManager] 무기 레벨업 적용 | {weaponData.weaponName} Lv.{weaponData.level}");
        }
        else
        {
          Debug.LogWarning($"[WeaponManager] 레벨업할 무기 오브젝트를 찾지 못함 | {weaponData.weaponName}");
        }

        OnWeaponChanged?.Invoke(activeWeapons);
        return;
      }

      // 새로운 무기 추가
      activeWeapons.Add(weaponData);
      // 초기 레벨 설정 (혹시 모르니)
      weaponData.level = 1;

      // ★ [수정] 위 레벨업 구간의 weaponObj와 이름 충돌을 피하기 위해 newWeaponObj 사용
      GameObject newWeaponObj = null;
      if (weaponData.weaponPrefab != null)
      {
        if (weaponData.isIndependent)
        {
          newWeaponObj = Instantiate(weaponData.weaponPrefab, transform.position, Quaternion.identity);
          var sourceIndep = newWeaponObj.GetComponent<WeaponSource>();
          if (sourceIndep == null) sourceIndep = newWeaponObj.AddComponent<WeaponSource>();
          sourceIndep.weaponData = weaponData;
        }
        else
        {
          newWeaponObj = Instantiate(weaponData.weaponPrefab, transform);
        }

        var source = newWeaponObj.GetComponent<WeaponSource>();
        if (source == null) source = newWeaponObj.AddComponent<WeaponSource>();
        source.weaponData = weaponData;

        spawnedWeapons.Add(weaponData, newWeaponObj);

        // ★ [추가]
        // 새 무기 생성 직후에도 Lv1 CSV 스탯을 확실히 적용한다.
        // 기존에는 Start()에서만 CSV를 읽는 무기도 있었고,
        // 무기마다 초기화 시점이 달라 적용 누락이 생길 수 있었다.
        newWeaponObj.SendMessage("OnLevelUp", weaponData.level, SendMessageOptions.DontRequireReceiver);
      }

      OnWeaponChanged?.Invoke(activeWeapons);
      Debug.Log(string.Join(",", activeWeapons));

      // sendToServer는 네트워크 제거 이후 호환성 유지를 위해 유지합니다.
    }

    public void AddWeapon(int weaponId)
    {
      var weapon = FindWeaponById(weaponId);
      if (weapon == null && weaponId >= 0 && weaponId < allWeaponDatas.Count)
      {
        // weaponId 매칭 실패 시, 테스트 키(F1~F10)는 리스트 인덱스로도 동작하도록 fallback
        weapon = allWeaponDatas[weaponId];
      }

      if (weapon != null)
      {
        AddWeapon(weapon);
        return;
      }

      Debug.LogWarning($"[WeaponManager] AddWeapon failed. weaponId={weaponId}, allWeaponDatas.Count={allWeaponDatas.Count}");
    }

    public void AddWeaponById(int weaponId)
    {
      var weapon = FindWeaponById(weaponId);
      if (weapon != null)
      {
        AddWeapon(weapon);
      }
    }

    /// <summary>
    /// 특정 무기의 공격을 실행합니다.
    /// </summary>
    public void ExecuteWeaponAttack(int weaponId, Vector3 direction)
    {
      WeaponBase data = FindWeaponById(weaponId);
      if (data == null && weaponId >= 0 && weaponId < allWeaponDatas.Count)
      {
        data = allWeaponDatas[weaponId];
      }

      if (data != null && spawnedWeapons.TryGetValue(data, out GameObject weaponObj))
      {
        weaponObj.SendMessage("ExecuteAttack", direction, SendMessageOptions.DontRequireReceiver);
      }
    }

    void Update()
    {
      // 테스트용: allWeaponDatas 리스트의 인덱스를 사용하여 테스트
      if (Input.GetKeyDown(KeyCode.F1)) AddWeapon(0);
      if (Input.GetKeyDown(KeyCode.F2)) AddWeapon(1);
      if (Input.GetKeyDown(KeyCode.F3)) AddWeapon(2);
      if (Input.GetKeyDown(KeyCode.F4)) AddWeapon(3);
      if (Input.GetKeyDown(KeyCode.F5)) AddWeapon(4);
      if (Input.GetKeyDown(KeyCode.F6)) AddWeapon(5);
      if (Input.GetKeyDown(KeyCode.F7)) AddWeapon(6);
      if (Input.GetKeyDown(KeyCode.F8)) AddWeapon(7);
      if (Input.GetKeyDown(KeyCode.F9)) AddWeapon(8);
      if (Input.GetKeyDown(KeyCode.F10)) AddWeapon(9);
    }
  }
}
