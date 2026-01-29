using UnityEngine;
using System.Collections.Generic;
using NeoSurvive.Buff;

// Character 클래스는 플레이어와 적 등 모든 캐릭터의 기반이 되는 추상 클래스입니다.
// MonoBehaviour를 상속받아 유니티 게임 오브젝트에 컴포넌트로 붙일 수 있습니다.
public abstract class Character : MonoBehaviour
{
  // 캐릭터의 체력 능력치입니다. Stat 클래스를 사용합니다.
  // 인스펙터에서 값을 설정할 수 있도록 직렬화합니다.
  [SerializeField]
  protected Stat healthStat;

  // 외부에서 체력 스탯을 참조할 수 있도록 하는 프로퍼티입니다. (읽기 전용)
  public Stat HealthStat => healthStat;

  // 현재 체력을 나타내는 변수입니다. 인스펙터에서 확인 가능합니다.
  [SerializeField]
  protected float currentHealth;

  // 외부에서 현재 체력을 참조할 수 있는 프로퍼티입니다. (읽기 전용)
  public float CurrentHealth => currentHealth;

  // 컴포넌트가 처음 활성화될 때 호출되는 유니티 생명주기 메서드입니다.
  // 체력 변경 알림 이벤트 (현재 체력, 최대 체력)
  public event System.Action<float, float> OnHealthChanged;

  private StatusFlags _baseFlags; // (추가)

  protected virtual void Awake()
  {
    if (healthStat == null)
    {
      healthStat = new Stat(100f);
    }
    currentHealth = healthStat.GetValue();

    _baseFlags = GetComponent<StatusFlags>(); // (추가)
    if (_baseFlags == null) _baseFlags = gameObject.AddComponent<StatusFlags>(); // (추가)
  }

  protected virtual void Start()
  {
    // 초기 체력 값 전송
    OnHealthChanged?.Invoke(currentHealth, healthStat.GetValue());
  }

  // 캐릭터의 사망 여부
  public bool IsDead { get; protected set; } = false;

  public virtual void TakeDamage(float amount)
  {
    if (IsDead) return;

    // (추가) 받는 피해 배율(데미지 2배 디버프 등) 적용
    if (_baseFlags == null) _baseFlags = GetComponent<StatusFlags>(); // (추가)
    if (_baseFlags != null) amount *= _baseFlags.incomingDamageMul;   // (추가)

    currentHealth -= amount;

    // 데미지 텍스트 표시
    if (UIManager.Instance != null)
    {
      UIManager.Instance.ShowDamageText(transform.position, amount);
    }

    // 체력 변경 알림
    OnHealthChanged?.Invoke(currentHealth, healthStat.GetValue());

    if (currentHealth <= 0)
    {
      currentHealth = 0;
      // 0으로 보정 후 다시 알림
      OnHealthChanged?.Invoke(currentHealth, healthStat.GetValue());
      Die();
    }
  }

  // 캐릭터가 죽었을 때 호출되는 가상 메서드입니다.
  protected virtual void Die()
  {
    if (IsDead) return;
    IsDead = true;
    Debug.Log($"{gameObject.name}이(가) 사망했습니다.");
  }

  /// <summary>
  /// 캐릭터를 부활시킵니다.
  /// </summary>
  public virtual void Revive(float healthRatio = 1.0f)
  {
    IsDead = false;
    currentHealth = healthStat.GetValue() * healthRatio;
    NotifyHealthChanged();
    Debug.Log($"{gameObject.name}이(가) 부활했습니다.");
  }

  /// <summary>
  /// 체력을 직접 설정합니다 (네트워크 동기화용)
  /// </summary>
  public virtual void SetHealth(float health)
  {
    currentHealth = health;
    NotifyHealthChanged();

    if (currentHealth <= 0 && !IsDead)
    {
      Die();
    }
    else if (currentHealth > 0 && IsDead)
    {
      Revive(currentHealth / healthStat.GetValue());
    }
  }

  /// <summary>
  /// 체력 변경을 알리는 protected 메서드 (자식 클래스에서 호출 가능)
  /// </summary>
  protected void NotifyHealthChanged()
  {
    OnHealthChanged?.Invoke(currentHealth, healthStat.GetValue());
  }

  // 추후 컨트롤러를 참조하기 위한 플레이스홀더 주석입니다.
  // public CharacterController CharacterController { get; protected set; }
}
