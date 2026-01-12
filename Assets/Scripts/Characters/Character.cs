using UnityEngine;
using System.Collections.Generic;

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

  protected virtual void Awake()
  {
    if (healthStat == null)
    {
      healthStat = new Stat(100f);
    }
    currentHealth = healthStat.GetValue();
  }

  protected virtual void Start()
  {
    // 초기 체력 값 전송
    OnHealthChanged?.Invoke(currentHealth, healthStat.GetValue());
  }

  public virtual void TakeDamage(float amount)
  {
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
    Debug.Log($"{gameObject.name}이(가) 사망했습니다.");
    // 여기에 기본적인 죽음 처리 로직을 구현합니다. (예: 게임 오브젝트 비활성화, 애니메이션 재생 등)
  }

  // 추후 컨트롤러를 참조하기 위한 플레이스홀더 주석입니다.
  // public CharacterController CharacterController { get; protected set; }
}
