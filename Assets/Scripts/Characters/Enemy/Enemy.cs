using UnityEngine;

// Enemy 클래스는 적 캐릭터를 나타냅니다.
// Character 클래스를 상속받아 캐릭터의 기본 기능을 모두 가집니다.
public class Enemy : Character
{
    [Header("보상 설정")]
    // 적이 죽었을 때 플레이어에게 줄 경험치입니다.
    [SerializeField]
    private int experienceToGive = 10;
    
    // 골드 드랍 확률 (0 ~ 100)
    [SerializeField]
    [Range(0, 100)]
    private float goldDropChance = 25f;
    
    // 인스펙터에서 할당할 골드 프리팹입니다.
    [SerializeField]
    private GameObject goldPrefab;

    // 부모 클래스(Character)의 Die 메서드를 오버라이드(재정의)하여
    // 적에게 특화된 죽음 처리 로직을 구현합니다.
    protected override void Die()
    {
        // 부모의 Die 메서드를 먼저 호출하여 기본적인 사망 처리를 수행합니다.
        base.Die();

        // 경험치 제공 로직
        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            player.GainExperience(experienceToGive);
        }
        
        // 골드 드랍 로직
        // 0.0 ~ 100.0 사이의 랜덤 값을 뽑습니다.
        float randomValue = Random.Range(0f, 100f);
        // 랜덤 값이 설정된 드랍 확률보다 낮고, 골드 프리팹이 할당되어 있다면
        if (randomValue <= goldDropChance && goldPrefab != null)
        {
            // 현재 적의 위치에 골드 프리팹을 생성합니다.
            Instantiate(goldPrefab, transform.position, Quaternion.identity);
            Debug.Log("골드를 드랍했습니다!");
        }

        // 죽음 처리 후 적 오브젝트를 파괴합니다.
        Destroy(gameObject);
    }
}
