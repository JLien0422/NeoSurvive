using System.Collections;
using UnityEngine;

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

    [Header("자동 공격 설정")]
    // 공격력
    [SerializeField]
    private float attackDamage = 10f;
    // 공격 범위
    [SerializeField]
    private float attackRange = 3f;
    // 공격 주기 (초)
    [SerializeField]
    private float attackInterval = 1.0f;

    // 게임 시작 시 호출됩니다.
    private void Start()
    {
        // 자동 공격 코루틴을 시작합니다.
        StartCoroutine(AttackCoroutine());
    }

    // 일정 주기로 주변의 적을 공격하는 코루틴입니다.
    private IEnumerator AttackCoroutine()
    {
        while (true)
        {
            // 다음 공격까지 기다립니다.
            yield return new WaitForSeconds(attackInterval);

            // 공격 범위 내의 모든 적을 찾습니다. "Enemy" 레이어에 있는 오브젝트만 감지합니다.
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange, LayerMask.GetMask("Enemy"));
            
            // 찾은 모든 적에게 데미지를 줍니다.
            foreach (Collider2D enemyCollider in hitEnemies)
            {
                Enemy enemy = enemyCollider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(attackDamage);
                    Debug.Log($"{enemy.name}에게 {attackDamage}의 데미지를 입혔습니다.");
                }
            }
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
        Debug.Log($"플레이어가 경험치 {amount}를 획득했습니다. 현재 경험치: {experience}");
        // 여기에 레벨업 로직을 추가할 수 있습니다.
        // 예: if (experience >= requiredExperienceForNextLevel) { LevelUp(); }
    }

#if UNITY_EDITOR
    // 에디터에서 공격 범위를 시각적으로 보여주는 기즈모입니다.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}
