using UnityEngine;
using System.Collections.Generic;
using NeoSurvive.Weapon;

/// <summary>
/// 플레이어 무기별로 적에게 가한 총 대미지와 분당 대미지(DPM)를 기록합니다.
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
  /// 해당 무기로 가한 대미지를 기록합니다.
  /// </summary>
  public void RecordDamage(WeaponBase weapon, float amount)
  {
    if (weapon == null) return;
    if (!totalDamagePerWeapon.ContainsKey(weapon))
    {
      totalDamagePerWeapon[weapon] = 0f;
      debugLogCount[weapon] = 0;
      Debug.Log($"[WeaponDamageStats] 새 무기 등록: {weapon.weaponName}");
    }
    totalDamagePerWeapon[weapon] += amount;

    if (debugLogCount[weapon] < DEBUG_LOG_LIMIT)
    {
      debugLogCount[weapon]++;
      Debug.Log($"[WeaponDamageStats] RecordDamage ({debugLogCount[weapon]}/{DEBUG_LOG_LIMIT}): {weapon.weaponName} +{amount:F1} → 누적={totalDamagePerWeapon[weapon]:F1}");
    }
  }

  /// <summary>
  /// 경과 시간(분). GameManager의 게임 타이머와 동일한 기준 사용.
  /// </summary>
  public float GetElapsedMinutes()
  {
    if (GameManager.Instance != null)
      return GameManager.Instance.GetGameTime() / 60f;
    float elapsed = Time.time - gameStartTime;
    return elapsed / 60f;
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
  /// 분당 대미지 (DPM). 경과 시간이 0이면 0 반환.
  /// </summary>
  public float GetDPM(WeaponBase weapon)
  {
    float total = GetTotalDamage(weapon);
    float minutes = GetElapsedMinutes();
    if (minutes <= 0f) return 0f;
    return total / minutes;
  }

  /// <summary>
  /// 씬/런 리셋 시 통계 초기화 (선택 호출).
  /// </summary>
  public void ResetStats()
  {
    totalDamagePerWeapon.Clear();
    gameStartTime = Time.time;
  }
}
