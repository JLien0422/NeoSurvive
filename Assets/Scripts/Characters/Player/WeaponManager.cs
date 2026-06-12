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
    public static WeaponManager Instance { get; private set; }

    [Header("Weapon Data")]
    public List<WeaponBase> allWeaponDatas = new(); // 모든 가능한 무기 데이터 (DB 역할)

    [Header("Active Weapons")]
    public List<WeaponBase> activeWeapons = new(); // 플레이어가 현재 보유한 무기 데이터
    private Dictionary<WeaponBase, GameObject> spawnedWeapons = new(); // 데이터별 생성된 실제 무기 오브젝트

    public static event System.Action<List<WeaponBase>> OnWeaponChanged;

    private WeaponBase FindWeaponById(int weaponId)
    {
      // F키 등으로 넘어오는 ID가 그대로 데이터의 weaponId(1부터 시작 등)와 일치한다고 가정하고 +1 보정을 제거하거나 유연하게 탐색
      var found = allWeaponDatas.Find(w => w != null && w.weaponId == weaponId);
      if (found == null)
      {
          // 기존처럼 0-index 기반으로 호출한 경우를 대비
          found = allWeaponDatas.Find(w => w != null && w.weaponId == weaponId + 1);
      }
      return found;
    }

    [Header("Start Weapon")]
    public WeaponBase startWeapon;

    private void Awake()
    {
      Instance = this;
      EquipStartWeapon();
    }

    private void OnDestroy()
    {
      if (Instance == this)
      {
        Instance = null;
      }
    }

    public static bool IsMarkedDamageBonusSource(WeaponBase weapon)
    {
      if (Instance == null || weapon == null || Instance.activeWeapons == null)
      {
        return false;
      }

      int index = Instance.activeWeapons.IndexOf(weapon);
      return index >= 1 && index <= 5;
    }

    private void EquipStartWeapon()
    {
      activeWeapons.Clear();
      WeaponDamageStats.Instance.ResetStats();

      if (startWeapon != null)
      {
        AddWeapon(startWeapon, "start");
      }
    }

    /// <summary>
    /// 새로운 무기 추가 또는 레벨업
    /// </summary>
    public void AddWeapon(WeaponBase weaponData)
    {
      AddWeapon(weaponData, "choice");
    }

    private void AddWeapon(WeaponBase weaponData, string source)
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
        GameAnalyticsTracker.TrackWeaponSelected(weaponData, source, isLevelUp: true, activeWeapons.IndexOf(weaponData));
        return;
      }

      // 새로운 무기 추가
      activeWeapons.Add(weaponData);
      WeaponDamageStats.Instance.RegisterWeapon(weaponData);

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

        var weaponSource = newWeaponObj.GetComponent<WeaponSource>();
        if (weaponSource == null) weaponSource = newWeaponObj.AddComponent<WeaponSource>();
        weaponSource.weaponData = weaponData;

        spawnedWeapons.Add(weaponData, newWeaponObj);

        // ★ [추가]
        // 새 무기 생성 직후에도 Lv1 CSV 스탯을 확실히 적용한다.
        // 기존에는 Start()에서만 CSV를 읽는 무기도 있었고,
        // 무기마다 초기화 시점이 달라 적용 누락이 생길 수 있었다.
        newWeaponObj.SendMessage("OnLevelUp", weaponData.level, SendMessageOptions.DontRequireReceiver);
      }

      OnWeaponChanged?.Invoke(activeWeapons);
      GameAnalyticsTracker.TrackWeaponSelected(weaponData, source, isLevelUp: false, activeWeapons.IndexOf(weaponData));
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
        AddWeapon(weapon, "debug");
        return;
      }

      Debug.LogWarning($"[WeaponManager] AddWeapon failed. weaponId={weaponId}, allWeaponDatas.Count={allWeaponDatas.Count}");
    }

    public void AddWeaponById(int weaponId)
    {
      var weapon = FindWeaponById(weaponId);
      if (weapon != null)
      {
        AddWeapon(weapon, "debug");
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
      // 테스트용: 데이터 순서에 의존하지 않고 ID로 직접 무기 찾기 (F1=1, F2=2 ... F10=10)
      if (Input.GetKeyDown(KeyCode.F1)) AddWeaponById(1);
      if (Input.GetKeyDown(KeyCode.F2)) AddWeaponById(2);
      if (Input.GetKeyDown(KeyCode.F3)) AddWeaponById(3);
      if (Input.GetKeyDown(KeyCode.F4)) AddWeaponById(4);
      if (Input.GetKeyDown(KeyCode.F5)) AddWeaponById(5);
      if (Input.GetKeyDown(KeyCode.F6)) AddWeaponById(6);
      if (Input.GetKeyDown(KeyCode.F7)) AddWeaponById(7);
      if (Input.GetKeyDown(KeyCode.F8)) AddWeaponById(8);
      if (Input.GetKeyDown(KeyCode.F9)) AddWeaponById(9);
      if (Input.GetKeyDown(KeyCode.F10)) AddWeaponById(10);
      if (Input.GetKeyDown(KeyCode.F11)) AddWeaponById(11);
      if (Input.GetKeyDown(KeyCode.F12)) AddWeaponById(12);
    }
  }
}
