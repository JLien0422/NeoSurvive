using UnityEngine;
using System;
using System.Collections.Generic;

// Stat 클래스는 캐릭터의 다양한 능력치(체력, 공격력 등)를 나타냅니다.
// 이 클래스는 직렬화가 가능하여 인스펙터에서 값을 편집할 수 있습니다.
[Serializable]
public class Stat
{
  // 인스펙터에서 설정할 수 있는 기본 능력치 값입니다.
  [SerializeField]
  private float baseValue;

  // 능력치 수정을 위한 변수들
  private float fixedModifier = 0f;
  private float percentModifier = 0f;

  // 기본 능력치 값을 가져오거나 설정하는 프로퍼티입니다.
  public float BaseValue
  {
    get { return baseValue; }
    set { baseValue = value; }
  }

  // 현재 퍼센트 수정자 값을 가져오는 프로퍼티 (인스펙터 확인용)
  public float PercentModifier => percentModifier;

  // 현재 고정 수정자 값을 가져오는 프로퍼티 (인스펙터 확인용)
  public float FixedModifier => fixedModifier;

  // 생성자: 기본 능력치 값으로 Stat 객체를 초기화합니다.
  public Stat(float baseValue)
  {
    this.baseValue = baseValue;
  }

  public void AddFixedModifier(float value)
  {
    fixedModifier += value;
  }

  public void AddPercentModifier(float value)
  {
    percentModifier += value;
  }


  public void SetBaseValue(float value)
  {
    baseValue = value;
  }

  // 최종 능력치 값을 반환하는 가상 메서드입니다.
  // 기본값 * (1 + 퍼센트) + 고정수치
  public virtual float GetValue()
  {
    return baseValue * (1.0f + percentModifier) + fixedModifier;
  }
}
