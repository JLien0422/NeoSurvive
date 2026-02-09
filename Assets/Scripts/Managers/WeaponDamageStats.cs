using UnityEngine;
using System.Collections.Generic;
using NeoSurvive.Weapon;

/// <summary>
/// 플레이어 무기별로 적에게 가한 총 대미지와 분당 대미지(DPM)를 기록합니다.
/// ESC 설정 패널에서 무기별 통계 표시에 사용됩니다.
/// </summary>
public class WeaponDamageStats : MonoBehaviour
{
  public static WeaponDamageStats Instance { get; private set; }

  private Dictionary<WeaponBase, float> totalDamagePerWeapon = new Dictionary<WeaponBase, float>();
  private float gameStartTime;

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
      gameStartTime = Time.time;
      DontDestroyOnLoad(transform.root.gameObject);
    }
    else
    {
      Destroy(gameObject);
    }
  }

  /// <summary>
  /// 해당 무기로 가한 대미지를 기록합니다.
  /// </summary>
  public void RecordDamage(WeaponBase weapon, float amount)
  {
    if (weapon == null) return;
    if (!totalDamagePerWeapon.ContainsKey(weapon))
      totalDamagePerWeapon[weapon] = 0f;
    totalDamagePerWeapon[weapon] += amount;
  }

  /// <summary>
  /// 경과 시간(분). 게임 시작 시점 기준.
  /// </summary>
  public float GetElapsedMinutes()
  {
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
