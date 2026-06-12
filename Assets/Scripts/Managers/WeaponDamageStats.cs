using UnityEngine;
using System.Collections.Generic;
using NeoSurvive.Weapon;

/// <summary>
/// 플레이어 무기별로 적에게 가한 총 대미지와 획득 후 초당 대미지(DPS)를 기록합니다.
/// ESC 설정 패널에서 무기별 통계 표시에 사용됩니다.
/// </summary>
public class WeaponDamageStats : MonoBehaviour
{
  private static WeaponDamageStats _instance;
  public static WeaponDamageStats Instance
  {
    get
    {
      if (_instance != null) return _instance;

      _instance = FindObjectOfType<WeaponDamageStats>(true);
      if (_instance == null)
      {
        var go = new GameObject("WeaponDamageStats_Auto");
        _instance = go.AddComponent<WeaponDamageStats>();
        DontDestroyOnLoad(go);
      }

      _instance.InitializeIfNeeded();
      return _instance;
    }
  }

  private Dictionary<WeaponBase, float> totalDamagePerWeapon = new Dictionary<WeaponBase, float>();
  private Dictionary<WeaponBase, float> weaponAcquiredTime = new Dictionary<WeaponBase, float>();
  private Dictionary<WeaponBase, int> debugLogCount = new Dictionary<WeaponBase, int>();
  private const int DEBUG_LOG_LIMIT = 5;
  private float gameStartTime;
  private bool isInitialized;

  private void Awake()
  {
    if (_instance == null)
    {
      _instance = this;
      InitializeIfNeeded();
    }
    else if (_instance != this)
    {
      Destroy(gameObject);
    }
  }

  private void InitializeIfNeeded()
  {
    if (isInitialized) return;
    gameStartTime = Time.time;
    isInitialized = true;
  }

  /// <summary>
  /// 무기를 새로 획득한 시점을 등록합니다. 레벨업 시에는 기존 획득 시각을 유지합니다.
  /// </summary>
  public void RegisterWeapon(WeaponBase weapon)
  {
    if (weapon == null) return;
    InitializeIfNeeded();

    if (!totalDamagePerWeapon.ContainsKey(weapon))
    {
      totalDamagePerWeapon[weapon] = 0f;
      debugLogCount[weapon] = 0;
    }

    if (!weaponAcquiredTime.ContainsKey(weapon))
    {
      weaponAcquiredTime[weapon] = GetCurrentGameSeconds();
      Debug.Log($"[WeaponDamageStats] 새 무기 등록: {weapon.weaponName}");
    }
  }

  /// <summary>
  /// 해당 무기로 가한 대미지를 기록합니다.
  /// </summary>
  public void RecordDamage(WeaponBase weapon, float amount)
  {
    if (weapon == null) return;

    RegisterWeapon(weapon);
    totalDamagePerWeapon[weapon] += amount;

    if (debugLogCount[weapon] < DEBUG_LOG_LIMIT)
    {
      debugLogCount[weapon]++;
      Debug.Log($"[WeaponDamageStats] RecordDamage ({debugLogCount[weapon]}/{DEBUG_LOG_LIMIT}): {weapon.weaponName} +{amount:F1} → 누적={totalDamagePerWeapon[weapon]:F1}");
    }
  }

  /// <summary>
  /// 현재 게임 시간(초). GameManager의 게임 타이머와 동일한 기준 사용.
  /// </summary>
  private float GetCurrentGameSeconds()
  {
    if (GameManager.Instance != null)
      return GameManager.Instance.GetGameTime();
    return Time.time - gameStartTime;
  }

  /// <summary>
  /// 해당 무기의 총 대미지.
  /// </summary>
  public float GetTotalDamage(WeaponBase weapon)
  {
    if (weapon == null) return 0f;
    return totalDamagePerWeapon.TryGetValue(weapon, out float total) ? total : 0f;
  }

  /// <summary>
  /// 해당 무기를 획득한 뒤 지난 시간(초).
  /// </summary>
  public float GetActiveSeconds(WeaponBase weapon)
  {
    if (weapon == null) return 0f;
    if (!weaponAcquiredTime.TryGetValue(weapon, out float acquiredAt))
      return 0f;

    return Mathf.Max(0f, GetCurrentGameSeconds() - acquiredAt);
  }

  /// <summary>
  /// 초당 대미지(DPS). 무기를 획득한 뒤 지난 시간이 0이면 0 반환.
  /// </summary>
  public float GetDPS(WeaponBase weapon)
  {
    float total = GetTotalDamage(weapon);
    float seconds = GetActiveSeconds(weapon);
    if (seconds <= 0f) return 0f;
    return total / seconds;
  }

  /// <summary>
  /// 씬/런 리셋 시 통계 초기화 (선택 호출).
  /// </summary>
  public void ResetStats()
  {
    totalDamagePerWeapon.Clear();
    weaponAcquiredTime.Clear();
    debugLogCount.Clear();
    gameStartTime = Time.time;
  }
}
