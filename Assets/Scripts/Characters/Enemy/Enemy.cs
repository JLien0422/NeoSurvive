using UnityEngine;

// Enemy 클래스는 적 캐릭터를 나타냅니다.
// Character 클래스를 상속받아 캐릭터의 기본 기능을 모두 가집니다.
public class Enemy : Character
{
  // 적이 죽었을 때 플레이어에게 줄 경험치입니다.
  [SerializeField]
  private int experienceToGive = 10;

  // 경험치 오브 드랍 설정(추가)
  [Header("EXP Orb Drop (추가)")]
  [SerializeField] private GameObject expOrbPrefab;   // "스크립트 없는" ExpOrb 프리팹(콜라이더/스프라이트)
  [SerializeField] private int dropCount = 1;
  [SerializeField] private float scatterRadius = 0.5f;

  // 적이 죽었을 때 드랍할 아이템입니다. (추후 아이템 시스템 구현 시 확장)
  // [SerializeField]
  // private Item lootDrop;

  // 부모 클래스(Character)의 Die 메서드를 오버라이드(재정의)하여
  // 적에게 특화된 죽음 처리 로직을 구현합니다.
  protected override void Die()
  {
    // 부모의 Die 메서드를 먼저 호출하여 기본적인 사망 처리를 수행합니다.
    base.Die();

    // 킬 카운트 증가
    if (GameManager.Instance != null)
    {
      GameManager.Instance.AddKill();
    }

    // 경험치 오브 드랍 메서드 호출 (추가)
    DropExpOrbs();

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

  // 경험치 오브 드랍 메서드 (추가)
  private void DropExpOrbs()
  {
    if (expOrbPrefab == null)
    {
      Debug.LogWarning($"{name}: expOrbPrefab이 연결되지 않았습니다.");
      return;
    }

    // 오브 1개당 얼마를 줄지 (dropCount로 나눠서 떨어뜨리기)
    int perOrb = Mathf.Max(1, experienceToGive / Mathf.Max(1, dropCount));
    int remainder = experienceToGive - (perOrb * dropCount);

    for (int i = 0; i < dropCount; i++)
    {
      Vector2 offset = Random.insideUnitCircle * scatterRadius;
      Vector3 spawnPos = transform.position + (Vector3)offset;

      GameObject orb = Instantiate(expOrbPrefab, spawnPos, Quaternion.identity);

      // 🔥 새 스크립트 없이 값을 전달해야 하므로,
      // 오브 이름에 경험치를 심어둠: "ExpOrb_10"
      int amount = perOrb + (i == 0 ? remainder : 0); // 나머지는 첫 오브에 몰아주기
      orb.name = $"ExpOrb_{amount}";
    }
  }
}
