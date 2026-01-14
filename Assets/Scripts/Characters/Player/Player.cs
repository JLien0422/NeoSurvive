using System.Collections;
using UnityEngine;
using NeoSurvive.Characters;

// Player 클래스는 플레이어 캐릭터를 나타냅니다.
// Character 클래스를 상속받아 캐릭터의 기본 기능을 모두 가집니다.
public class Player : Character
{
  // 플레이어의 경험치를 저장하는 변수입니다.
  [SerializeField]
  private int experience = 0;
  // 외부에서 경험치를 읽을 수 있는 프로퍼티입니다. (읽기 전용)
  public int Experience => experience;

  // 플레이어의 레벨을 저장하는 변수입니다.
  [SerializeField]
  private int level = 1;
  // 외부에서 레벨을 읽을 수 있는 프로퍼티입니다. (읽기 전용)
  public int Level => level;

  public int MaxExperience => level * 100; // 임시 레벨업 필요 경험치량

  public static event System.Action<int, int> OnExpChanged; // (current, max)
  public static event System.Action<int> OnLevelUp; // (new level)

  // 레벨업 관련 설정(추가)
  [Header("Level Up Settings (추가)")]
  // 다음 레벨업에 필요한 경험치
  [SerializeField] private int requiredExpForNextLevel = 5;
  // 경험치 요구량 증가 배율
  [SerializeField] private float growthMultiplier = 1.25f;

  [Header("캐릭터 데이터")]
  [Tooltip("해커 캐릭터 데이터")]
  [SerializeField]
  private CharacterData hackerData;
  [Tooltip("사이보그 캐릭터 데이터")]
  [SerializeField]
  private CharacterData cyborgData;

  [Header("자동 공격 설정")]
  // 공격력
  [SerializeField]
  private Stat attackDamage = new Stat(10f);
  // 공격 범위
  [SerializeField]
  private Stat attackRange = new Stat(3f);
  // 기본 공격 주기 (초)
  [SerializeField]
  private float baseAttackInterval = 1.0f;
  // 공격 속도 (1.0 = 100%)
  [SerializeField]
  private Stat attackSpeed = new Stat(1.0f);

  protected override void Start()
  {
    base.Start();

    // GameManager에서 선택된 캐릭터 타입 가져오기
    if (GameManager.Instance != null)
    {
      CharacterType selectedType = GameManager.Instance.GetSelectedCharacter();
      CharacterData selectedData = selectedType == CharacterType.Hacker ? hackerData : cyborgData;

      if (selectedData != null)
      {
        InitializeFromCharacterData(selectedData);
      }
      else
      {
        Debug.LogWarning($"캐릭터 데이터가 할당되지 않음: {selectedType}");
      }
    }

    if (UIManager.Instance != null)
    {
      UIManager.Instance.SetPlayerHealthBar(this);
    }
  }

  /// <summary>
  /// 캐릭터 데이터로부터 플레이어 스텟을 초기화합니다.
  /// </summary>
  private void InitializeFromCharacterData(CharacterData data)
  {
    Debug.Log($"캐릭터 초기화: {data.characterName} ({data.characterType})");

    // 스텟 초기화
    healthStat = new Stat(data.baseHealth);
    currentHealth = healthStat.GetValue();

    attackDamage = new Stat(data.baseAttackDamage);
    attackRange = new Stat(data.baseAttackRange);
    attackSpeed = new Stat(data.baseAttackSpeed);
    // TODO: moveSpeed는 Character 클래스에 추가되면 활성화
    // moveSpeed = new Stat(data.baseMoveSpeed);

    Debug.Log($"스텟 초기화 완료 - HP: {healthStat.GetValue()}, 공격력: {attackDamage.GetValue()}, 공격 속도: {attackSpeed.GetValue()}");
  }

  public void ApplyStatChange(StatType type, float flat, float percent)
  {
    switch (type)
    {
      case StatType.AttackDamage:
        attackDamage.AddFixedModifier(flat);
        attackDamage.AddPercentModifier(percent);
        break;
      case StatType.AttackRange:
        attackRange.AddFixedModifier(flat);
        attackRange.AddPercentModifier(percent);
        break;
      case StatType.AttackSpeed:
        attackSpeed.AddFixedModifier(flat);
        attackSpeed.AddPercentModifier(percent);
        break;
      case StatType.MaxHP:
        // Character.cs defines healthStat
        healthStat.AddFixedModifier(flat);
        healthStat.AddPercentModifier(percent);
        break;
    }
  }

  // 부모 클래스(Character)의 Die 메서드를 오버라이드(재정의)하여
  // 플레이어에게 특화된 죽음 처리 로직을 구현합니다.
  protected override void Die()
  {
    Debug.Log($"{gameObject.name} (플레이어)가 패배했습니다!");
    // 요청에 따라 게임 오브젝트를 파괴합니다.
    Destroy(gameObject);
  }

  // 플레이어가 경험치를 얻었을 때 호출되는 메서드입니다.
  public void GainExperience(int amount)
  {
    experience += amount;
    // Debug.Log($"플레이어가 경험치 {amount}를 획득했습니다. 현재 경험치: {experience}");
    // 여기에 레벨업 로직을 추가할 수 있습니다.
    // 예: if (experience >= requiredExperienceForNextLevel) { LevelUp(); }
  }

#if UNITY_EDITOR
    // 에디터에서 공격 범위를 시각적으로 보여주는 기즈모입니다.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        if(attackRange != null)
            Gizmos.DrawWireSphere(transform.position, attackRange.GetValue());
    }
#endif
}
