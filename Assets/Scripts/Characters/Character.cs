using UnityEngine;
using System.Collections.Generic;
using NeoSurvive.Buff;
using NeoSurvive.Weapon;

// Character 클래스는 플레이어와 적 등 모든 캐릭터의 기반이 되는 추상 클래스입니다.
// MonoBehaviour를 상속받아 유니티 게임 오브젝트에 컴포넌트로 붙일 수 있습니다.
public abstract class Character : MonoBehaviour
{
  // 캐릭터의 체력 능력치입니다. Stat 클래스를 사용합니다.
  // 인스펙터에서 값을 설정할 수 있도록 직렬화합니다.
  [SerializeField]
  protected Stat currentHP;

  // 외부에서 체력 스탯을 참조할 수 있도록 하는 프로퍼티입니다. (읽기 전용)
  public Stat CurrentHP => currentHP;

  // 캐릭터의 최대 체력을 나타내는 변수입니다. 인스펙터에서 설정할 수 있습니다.
  [SerializeField]
  protected Stat maxHP;
  public Stat MaxHP => maxHP;

  private StatusFlags _baseFlags; // (추가)
  private int _nullWeaponWarnCount = 0;
  private const int NULL_WEAPON_WARN_LIMIT = 5;

  protected virtual void Awake()
  {

    _baseFlags = GetComponent<StatusFlags>(); // (추가)
    if (_baseFlags == null) _baseFlags = gameObject.AddComponent<StatusFlags>(); // (추가)
    EnsureHpInitialized(refillToMax: true);

  }

  // 캐릭터의 사망 여부
  public bool IsDead { get; protected set; } = false;

  public virtual void TakeDamage(float amount)
  {
    TakeDamage(amount, null);
  }

  public virtual void TakeDamage(float amount, WeaponBase sourceWeapon)
  {
    if (IsDead) return;
    EnsureHpInitialized(refillToMax: false);

    // 무기별 대미지 통계 기록 (배율 적용 전 원본 수치로 기록)
    if (sourceWeapon != null && WeaponDamageStats.Instance != null)
    {
      WeaponDamageStats.Instance.RecordDamage(sourceWeapon, amount);
    }
    else if (sourceWeapon == null && _nullWeaponWarnCount < NULL_WEAPON_WARN_LIMIT)
    {
      _nullWeaponWarnCount++;
      Debug.LogWarning($"[DPM 누락 {_nullWeaponWarnCount}/{NULL_WEAPON_WARN_LIMIT}] {gameObject.name}이(가) sourceWeapon=null로 피해를 받음. 대미지={amount:F1}. 투사체나 무기 스크립트의 SetSourceWeapon/GetComponentInParent<WeaponSource>() 를 확인하세요.");
    }

    // (추가) 받는 피해 배율(데미지 2배 디버프 등) 적용
    if (_baseFlags == null) _baseFlags = GetComponent<StatusFlags>(); // (추가)
    if (_baseFlags != null) amount *= _baseFlags.incomingDamageMul;   // (추가)

    currentHP.CurrentValue -= amount;

    // 데미지 텍스트 표시
    if (UIManager.Instance != null)
    {
      UIManager.Instance.ShowDamageText(transform.position, amount);
    }

    if (currentHP.CurrentValue <= 0)
    {
      currentHP.CurrentValue = 0;
      Die();
    }
  }

  /// <summary>
  /// 캐릭터 체력을 회복합니다.
  /// </summary>
  public void Heal(float amount)
  {
    if (IsDead) return;
    EnsureHpInitialized(refillToMax: false);
    currentHP.CurrentValue = Mathf.Min(currentHP.CurrentValue + amount, maxHP.GetValue());

    Debug.Log($"[Player] 체력 회복: +{amount} | 현재 HP: {currentHP.CurrentValue}/{maxHP.GetValue()}");
  }
  // 캐릭터가 죽었을 때 호출되는 가상 메서드입니다.
  protected virtual void Die()
  {
    if (IsDead) return;
    IsDead = true;
  }

  /// <summary>
  /// 캐릭터를 부활시킵니다.
  /// </summary>
  public virtual void Revive(float healthRatio = 1.0f)
  {
    IsDead = false;
    EnsureHpInitialized(refillToMax: false);
    currentHP.CurrentValue = maxHP.GetValue() * healthRatio;
    Debug.Log($"{gameObject.name}이(가) 부활했습니다.");
  }

  /// <summary>
  /// currentHP를 maxHP 기준으로 안전하게 초기화/동기화합니다.
  /// </summary>
  protected void EnsureHpInitialized(bool refillToMax)
  {
    // maxHP가 비어있으면 최소 기본값으로 생성
    if (maxHP == null)
      maxHP = new Stat(1f);

    float maxValue = Mathf.Max(1f, maxHP.GetValue());

    // currentHP가 비어있으면 maxHP를 기준으로 생성
    if (currentHP == null)
      currentHP = new Stat(maxValue, maxValue);

    // 현재 체력을 즉시 최대치와 동기화해야 하는 시점(스폰/초기화)
    if (refillToMax)
    {
      currentHP.BaseValue = maxValue;
      currentHP.CurrentValue = maxValue;
      return;
    }

    // 기존 값이 유효하면 유지하되 범위 보정
    if (currentHP.BaseValue <= 0f)
      currentHP.BaseValue = maxValue;

    currentHP.CurrentValue = Mathf.Clamp(currentHP.CurrentValue, 0f, maxValue);
  }
}
