using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

    private Player player;

    private int maxWeapon = 6;

    private WeaponBase FindWeaponById(int weaponId)
    {
      return allWeaponDatas.Find(w => w != null && w.weaponId == weaponId);
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
      var classTag = GetComponent<PlayerClassTag>();
      var classType = (classTag != null) ? classTag.classType : PlayerClassType.Hacker;

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
        // 레벨업 로직
        weaponData.level++;
        OnWeaponChanged?.Invoke(activeWeapons);
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
          var sourceIndep = weaponObj.GetComponent<WeaponSource>();
          if (sourceIndep == null) sourceIndep = weaponObj.AddComponent<WeaponSource>();
          sourceIndep.weaponData = weaponData;
        }
        else
        {
          weaponObj = Instantiate(weaponData.weaponPrefab, transform);
        }

        var source = weaponObj.GetComponent<WeaponSource>();
        if (source == null) source = weaponObj.AddComponent<WeaponSource>();
        source.weaponData = weaponData;

        spawnedWeapons.Add(weaponData, weaponObj);

        if (player == null) player = GetComponent<Player>();
        if (player != null)
        {
          var runtimeInfo = weaponObj.GetComponent<WeaponRuntimeInfo>();
          if (runtimeInfo == null) runtimeInfo = weaponObj.AddComponent<WeaponRuntimeInfo>();
          runtimeInfo.Initialize(weaponData.weaponId, player, this);
        }

        // 초기화 알림
        weaponObj.SendMessage("OnLevelUp", 1, SendMessageOptions.DontRequireReceiver);
      }

      OnWeaponChanged?.Invoke(activeWeapons);
      Debug.Log(string.Join(",", activeWeapons));

      // sendToServer는 네트워크 제거 이후 호환성 유지를 위해 유지합니다.
    }

    public void AddWeapon(int weaponId)
    {
      var weapon = FindWeaponById(weaponId);
      if (weapon != null)
      {
        AddWeapon(weapon);
      }
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
