using System.Collections;
using System.Collections.Generic;
using NeoSurvive.Weapon;
using UnityEngine;

// Define the Stat Types available for modification
public enum StatType
{
  MaxHP,
  MoveSpeed,
  AttackDamage,
  AttackRange,
  AttackSpeed
}

[System.Serializable]
public class PassiveEffect
{
  public StatType statType;
  [Tooltip("고정 수치 만큼 증가합니다.")]
  public float fixedIncrease;
  [Tooltip("퍼센트 만큼 증가합니다.")]
  public float percentIncrease;
}

public class Passive : WeaponBase
{

  /// <summary>
  /// 패시브 무기가 어떤 스탯에 영향을 줄지 정의합니다.
  /// </summary>
  [Tooltip("패시브 무기가 어떤 스탯에 영향을 줄지 정의합니다.")]
  public List<PassiveEffect> effects;

  /// <summary>
  /// 플레이어에게 패시브 효과를 적용합니다.
  /// </summary>
  /// <param name="player"></param>
  public void Apply(Player player)
  {
    if (player == null) return;

    foreach (var effect in effects)
    {
      player.ApplyStatChange(effect.statType, effect.fixedIncrease, effect.percentIncrease);
      Debug.Log($"Applied Passive {weaponName}: {effect.statType} +{effect.fixedIncrease} (Fixed), +{effect.percentIncrease:P0} (%)");
    }
  }
}
