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

    [Header("이동 속도 설정")]
    [SerializeField]
    private Stat moveSpeed = new Stat(5f); // PlayerController가 참조할 이동 속도 Stat
    public float CurrentMoveSpeed => moveSpeed.GetValue(); // PlayerController가 최종 이동 속도를 가져갈 프로퍼티

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
            case StatType.MoveSpeed:
                moveSpeed.AddFixedModifier(flat);
                moveSpeed.AddPercentModifier(percent);
                break;
        }
    }

    // 게임 시작 시 호출됩니다.
    protected override void Awake()
    {
        base.Awake(); // 부모 Awake 호출
        ApplyUpgrades(); // 업그레이드 적용
    }

    // 업그레이드 매니저로부터 스탯 보너스를 가져와 적용하는 메서드
    private void ApplyUpgrades()
    {
        if (UpgradeManager.Instance != null)
        {
            // 체력 업그레이드 적용 (기본 체력에 보너스 추가)
            healthStat.AddFixedModifier(UpgradeManager.Instance.GetHealthUpgradeBonus());
            currentHealth = healthStat.GetValue(); // 체력 즉시 반영

            // 공격력 업그레이드 적용
            attackDamage.AddFixedModifier(UpgradeManager.Instance.GetDamageUpgradeBonus());

            // 이동 속도 업그레이드 적용
            moveSpeed.AddFixedModifier(UpgradeManager.Instance.GetMoveSpeedUpgradeBonus());

            Debug.Log("플레이어에게 영구 업그레이드 보너스 적용 완료.");
        }
    }

    public void AddExp(int amount)
    {
        experience += amount;
        OnExpChanged?.Invoke(experience, MaxExperience);
        if (experience >= MaxExperience)
        {
            level++;
            experience = 0;
            OnLevelUp?.Invoke(level);
        }
    }

    // 부모 클래스(Character)의 Die 메서드를 오버라이드(재정의)하여
    // 플레이어에게 특화된 죽음 처리 로직을 구현합니다.
    protected override void Die()
    {
        Debug.Log($"{gameObject.name} (플레이어)가 패배했습니다!");

        // GameManager에 플레이어의 죽음을 알리고 골드를 저장합니다.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDeath();
        }

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

    // ===================== Exp Orb 처리 (추가) =====================

    private void OnTriggerEnter2D(Collider2D other) // (추가)
    {
        // Enemy가 만든 Exp Orb인지 확인 (추가)
        if (!other.name.StartsWith("ExpOrb_")) return; // (추가)

        int amount = ParseExpOrbAmount(other.name); // (추가)
        if (amount <= 0) amount = 1; // (추가)

        GainExperience(amount); // 기존 메서드 사용
        CheckLevelUp(); // (추가)

        Destroy(other.gameObject); // (추가)
    }

    private int ParseExpOrbAmount(string orbName) // (추가)
    {
        int idx = orbName.LastIndexOf('_'); // (추가)
        if (idx < 0 || idx == orbName.Length - 1) return 0; // (추가)

        string value = orbName.Substring(idx + 1); // (추가)
        return int.TryParse(value, out int result) ? result : 0; // (추가)
    }

    // ===================== 레벨업 로직 (추가) =====================

    private void CheckLevelUp() // (추가)
    {
        while (experience >= requiredExpForNextLevel) // (추가)
        {
            experience -= requiredExpForNextLevel; // (추가)
            LevelUpInternal(); // (추가)
        }

        OnExpChanged?.Invoke(experience, requiredExpForNextLevel); // (추가)
    }

    private void LevelUpInternal() // (추가)
    {
        level++; // (추가)

        requiredExpForNextLevel =
          Mathf.CeilToInt(requiredExpForNextLevel * growthMultiplier); // (추가)

        Debug.Log($"🎉 레벨업! 현재 레벨: {level}"); // (추가)

        OnLevelUp?.Invoke(level); // (추가)
    }

#if UNITY_EDITOR
    // 에디터에서 공격 범위를 시각적으로 보여주는 기즈모입니다.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        if (attackRange != null)
            Gizmos.DrawWireSphere(transform.position, attackRange.GetValue());
    }
#endif
}
