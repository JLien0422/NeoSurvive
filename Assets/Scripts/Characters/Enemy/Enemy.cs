using UnityEngine;

// Enemy 클래스는 적 캐릭터를 나타냅니다.
// Character 클래스를 상속받아 캐릭터의 기본 기능을 모두 가집니다.
public class Enemy : Character
{
  // 적이 죽었을 때 플레이어에게 줄 경험치입니다.
  [SerializeField]
  private int experienceToGive = 10;

  // 적이 죽었을 때 드랍할 아이템입니다. (추후 아이템 시스템 구현 시 확장)
  // [SerializeField]
  // private Item lootDrop;

  // 부모 클래스(Character)의 Die 메서드를 오버라이드(재정의)하여
  // 적에게 특화된 죽음 처리 로직을 구현합니다.
  protected override void Die()
  {
    // 부모의 Die 메서드를 먼저 호출하여 기본적인 사망 처리를 수행합니다.
    base.Die();

    // 경험치 제공 로직
    // 씬에서 "Player" 태그를 가진 오브젝트를 찾아 Player 컴포넌트를 가져옵니다.
    // 이는 간단한 구현이며, 더 큰 게임에서는 GameManager나 이벤트 시스템을 통해 플레이어를 참조하는 것이 좋습니다.
    Player player = FindObjectOfType<Player>();
    if (player != null)
    {
      player.GainExperience(experienceToGive);
    }

    // 아이템 드랍 로직 (추후 구현)
    // if (lootDrop != null)
    // {
    //     // Instantiate(lootDrop, transform.position, Quaternion.identity);
    // }

    // 죽음 처리 후 적 오브젝트를 파괴합니다.
    Destroy(gameObject);
  }
}
