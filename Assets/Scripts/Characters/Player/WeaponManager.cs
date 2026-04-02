using UnityEngine;
using System.Collections.Generic;
using System.Linq; // ***** (추가) Distinct/Where/ToList
using NeoSurvive.Network.Protocol;

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

    public List<Passive> activePassives = new();

    public static event System.Action<List<WeaponBase>> OnWeaponChanged;

    private void RaiseWeaponChanged()
    {
      int listenerCount = OnWeaponChanged?.GetInvocationList().Length ?? 0;
      Debug.Log($"[WeaponManager] OnWeaponChanged invoke, listeners={listenerCount}, activeWeapons={activeWeapons.Count}");
      OnWeaponChanged?.Invoke(activeWeapons);
    }

    // [Coop] Player 참조
    private Player player;

    private WeaponBase FindWeaponById(int weaponId)
    {
      return allWeaponDatas.Find(w => w != null && w.weaponId == weaponId);
    }
    // ***** (추가) 클래스별 무기 풀(= All Weapon Datas 템플릿)
    [Header("Class Weapon Sets (Templates)")] // ***** (추가)
    public List<WeaponBase> cyborgAllWeaponDatas = new(); // ***** (추가)
    public List<WeaponBase> hackerAllWeaponDatas = new(); // ***** (추가)

    // ***** (추가) 클래스별 시작 장착 무기(= Active Weapons 템플릿)
    [Header("Class Start Weapons (Templates)")] // ***** (추가)
    public List<WeaponBase> cyborgStartWeapons = new(); // ***** (추가) LaserSword만 넣기
    public List<WeaponBase> hackerStartWeapons = new(); // ***** (추가) LinkPistol 등 넣기

    private void Awake()
    {
      ApplyClassLoadout(); // ***** (추가) 클래스에 맞게 allWeaponDatas / activeWeapons(시작목록) 구성
      EquipStartWeapons(); // ***** (기존) activeWeapons(시작목록)을 AddWeapon으로 실제 장착
    }

    private void ApplyClassLoadout() // ***** (추가)
    {
      // ***** (추가) PlayerClassTag가 있으면 그 값을 쓰고, 없으면 Hacker로 기본
      var classTag = GetComponent<PlayerClassTag>(); // ***** (추가)
      var classType = (classTag != null) ? classTag.classType : PlayerClassType.Hacker; // ***** (추가)

      // ***** (추가) 클래스별 템플릿 선택
      List<WeaponBase> selectedAll = (classType == PlayerClassType.Cyborg)
        ? cyborgAllWeaponDatas
        : hackerAllWeaponDatas;

      List<WeaponBase> selectedStart = (classType == PlayerClassType.Cyborg)
        ? cyborgStartWeapons
        : hackerStartWeapons;

      // ***** (추가) 런타임 allWeaponDatas 구성 (이 순서가 weaponIndex 기준)
      allWeaponDatas.Clear(); // ***** (추가)
      if (selectedAll != null)
        allWeaponDatas.AddRange(selectedAll.Where(w => w != null)); // ***** (추가)

      // ***** (추가) 런타임 시작무기 목록을 activeWeapons에 세팅
      activeWeapons.Clear(); // ***** (추가)
      if (selectedStart != null)
        activeWeapons.AddRange(selectedStart.Where(w => w != null)); // ***** (추가)
    }

    // ***** (기존 + 약간 정리) 인스펙터(또는 ApplyClassLoadout)로 들어온 activeWeapons를 시작 장착
    private void EquipStartWeapons()
    {
      if (activeWeapons == null || activeWeapons.Count == 0) return;

      // ***** (기존) Null 제거 + 중복 제거
      var startList = activeWeapons
        .Where(w => w != null)
        .Distinct()
        .ToList();

      // ***** (기존) activeWeapons는 "실제 보유 무기"로 AddWeapon이 다시 채우게 하기 위해 비움
      activeWeapons.Clear();

      // ***** (기존) 인덱스 상관없이 WeaponBase 자체로 장착
      foreach (var w in startList)
      {
        AddWeapon(w, sendToServer: false); // ***** (기존) 시작 장착은 보통 서버 전송 안 함
      }
    }

    /// <summary>
    /// 새로운 무기 추가 또는 레벨업
    /// </summary>
    public void AddWeapon(WeaponBase weaponData, bool sendToServer = true)
    {
      Debug.Log($"[WeaponManager] AddWeapon called. weaponName={weaponData.weaponName}, prefab={(weaponData.weaponPrefab ? weaponData.weaponPrefab.name : "NULL")}");

      // 이미 보유 중인 무기인지 확인
      if (activeWeapons.Contains(weaponData))
      {
        // 레벨업 로직
        weaponData.level++;
        MainSceneSoundManager.Instance?.PlayWeaponLevelUp(weaponData);
        RaiseWeaponChanged();

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
      MainSceneSoundManager.Instance?.PlayWeaponEquip(weaponData);

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

      RaiseWeaponChanged();
            Debug.Log(string.Join("," , activeWeapons));

      // [Coop] 로컬 플레이어인 경우 서버에 무기 장착 알림
      if (sendToServer)
      {
        if (player == null) player = GetComponent<Player>();
        if (player != null && player.IsLocal && UDPClient.Instance != null)
        {
          UDPClient.Instance.SendAction(
            ActionType.WeaponEquip,
            0,
            Vector2.zero,
            (uint)weaponData.weaponId,
            weaponData.weaponId
          );
        }
      }
    }

    // 기존 호환성을 위한 오버로드 (필요시)
    public void AddWeapon(int index, bool sendToServer = true)
    {
      if (index >= 0 && index < allWeaponDatas.Count)
      {
        AddWeapon(allWeaponDatas[index], sendToServer);
      }
    }

    public void AddWeaponById(int weaponId, bool sendToServer = true)
    {
      var weapon = FindWeaponById(weaponId);
      if (weapon != null)
      {
        AddWeapon(weapon, sendToServer);
      }
    }

    /// <summary>
    /// [Coop] 원격 플레이어의 무기 장착 동기화
    /// </summary>
    public void SyncWeaponEquip(int weaponId)
    {
      // 서버에 재전송하지 않음
      AddWeaponById(weaponId, sendToServer: false);
    }

    /// <summary>
    /// [Coop] 특정 무기의 공격을 실행 (원격 동기화용)
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
        NeoSurvive.Network.NetworkDamageContext.BeginRemoteAction();
        weaponObj.SendMessage("ExecuteAttack", direction, SendMessageOptions.DontRequireReceiver);
        NeoSurvive.Network.NetworkDamageContext.EndRemoteAction();
      }
    }

    /// <summary>
    /// [Coop] 무기 공격을 서버로 전송 (로컬 플레이어용)
    /// </summary>
    public void SendWeaponAttack(int weaponId, Vector3 direction)
    {
      if (player == null) player = GetComponent<Player>();
      if (player != null && player.IsLocal && UDPClient.Instance != null)
      {
        UDPClient.Instance.SendAction(
          ActionType.WeaponUse,
          0,
          new Vector2(direction.x, direction.y),
          (uint)weaponId,
          weaponId
        );
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
      if (Input.GetKeyDown(KeyCode.F9))
      {
        Debug.Log("[WeaponManager] F9 pressed -> AddWeapon(8)");
        AddWeapon(8);
      }
      if (Input.GetKeyDown(KeyCode.F10)) AddWeapon(9);
    }
  }
}
