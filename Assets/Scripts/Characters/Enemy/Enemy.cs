using UnityEngine;
using NeoSurvive.Buff; // (추가)

// Enemy 클래스는 적 캐릭터를 나타냅니다.
// Character 클래스를 상속받아 캐릭터의 기본 기능을 모두 가집니다.
public class Enemy : Character
{
  //************************버프/디버프 관련 헬퍼************************//
  private StatusFlags _enemyFlags; // (추가)

  // (추가) 외부(AI/무기/피해처리)에서 참조하기 쉬운 헬퍼
  public StatusFlags Status => _enemyFlags != null ? _enemyFlags : (_enemyFlags = GetComponent<StatusFlags>() ?? gameObject.AddComponent<StatusFlags>()); // (추가)
  public bool CanMove => !Status.moveBlocked;   // (추가)
  public bool CanAttack => !Status.attackBlocked; // (추가)
  public bool IsBlinded => Status.blinded;      // (추가)
  public bool IsFrenzy => Status.frenzy;        // (추가)

  // (추가) 받는 피해 배율 적용(데미지 2배 디버프 등)
  public float ApplyIncomingDamage(float damage) => damage * Status.incomingDamageMul; // (추가)

  // Character.Awake를 재정의하여 StatusFlags를 초기화합니다. (경고 CS0114 해결)
  protected override void Awake() // (추가)
  {
    base.Awake();
    // StatusFlags 캐싱 (추가)
    _enemyFlags = GetComponent<StatusFlags>();
    if (_enemyFlags == null) _enemyFlags = gameObject.AddComponent<StatusFlags>();
  }

  //************************여기까지 버프/디버프************************//

  [Header("경험치 보상")]
  // 적이 죽었을 때 플레이어에게 줄 총 경험치입니다.
  [SerializeField]
  private int experienceToGive = 10;
  // 경험치 오브 드랍 설정
  [SerializeField]
  private GameObject expOrbPrefab;
  [SerializeField]
  private int dropCount = 1;
  [SerializeField]
  private float scatterRadius = 0.5f;

  [Header("골드 보상")]
  // 골드 드랍 확률 (0 ~ 100)
  [SerializeField]
  [Range(0, 100)]
  private float goldDropChance = 25f;
  // 인스펙터에서 할당할 골드 프리팹입니다.
  [SerializeField]
  private GameObject goldPrefab;

  [Header("사이코 잠식도 아이템")]
  // 사이코 잠식도 아이템 드랍 확률 (0 ~ 100)
  [SerializeField]
  [Range(0, 100)]
  private float psychoCorruptionDropChance = 5f;
  // 인스펙터에서 할당할 사이코 잠식도 아이템 프리팹입니다.
  [SerializeField]
  private GameObject psychoCorruptionItemPrefab;

  [Header("데이터칩 (신경링크)")]
  // 데이터칩 드랍 확률 (0 ~ 100)
  [SerializeField]
  [Range(0, 100)]
  private float dataChipDropChance = 10f;
  // 인스펙터에서 할당할 데이터칩 프리팹입니다.
  [SerializeField]
  private GameObject dataChipPrefab;

  // 부모 클래스(Character)의 Die 메서드를 오버라이드(재정의)하여
  // 적에게 특화된 죽음 처리 로직을 구현합니다.
  protected override void Die()
  {
    if (IsDead) return;

    // 메커니즘의 사망 효과 처리 (Bomber 등)
    EnemyController controller = GetComponent<EnemyController>();
    if (controller != null)
    {
      controller.OnEnemyDeath();
    }

    // 부모의 Die 메서드를 먼저 호출하여 기본적인 사망 처리를 수행합니다.
    base.Die();

    // 싱글플레이: 경험치 오브 드랍 메서드 호출 (추가)
    DropExpOrbs();
    // 골드 드랍 메서드 호출
    DropGold();
    // 사이코 잠식도 아이템 드랍 메서드 호출
    DropPsychoCorruptionItem();
    // 데이터칩 드랍 메서드 호출
    DropDataChip();

    // 킬 카운트 증가
    if (GameManager.Instance != null)
    {
      GameManager.Instance.AddKill();
    }

    // 죽음 처리 후 적 오브젝트를 파괴합니다.
    // ⚠ BomberMechanism은 사망 후 일정 시간 뒤에 폭발해야 하므로
    // 해당 메커니즘일 때는 BomberMechanism 쪽에서 Destroy를 호출하게 둡니다.
    if (controller == null || controller.GetMechanismType() != EnemyMechanismType.Bomber)
    {
      Destroy(gameObject);
    }
  }

  // 경험치 오브 드랍 메서드
  private void DropExpOrbs()
  {
    if (expOrbPrefab == null)
    {
      // 경험치 오브 프리팹이 없는 경우, 경고를 남기고 직접 경험치를 줍니다. (안전 장치)
      Debug.LogWarning($"{name}: expOrbPrefab이 연결되지 않아 경험치를 직접 부여합니다.");
      Player player = FindObjectOfType<Player>();
      if (player != null)
      {
        player.GainExperience(experienceToGive);
      }
      return;
    }

    // 오브 1개당 얼마를 줄지 계산합니다.
    int perOrb = Mathf.Max(1, experienceToGive / Mathf.Max(1, dropCount));
    int remainder = experienceToGive - (perOrb * dropCount);

    for (int i = 0; i < dropCount; i++)
    {
      Vector2 offset = Random.insideUnitCircle * scatterRadius;
      Vector3 spawnPos = transform.position + (Vector3)offset;

      GameObject orb = Instantiate(expOrbPrefab, spawnPos, Quaternion.identity);

      // 오브 이름에 경험치량을 포함시켜 데이터를 전달합니다.
      int amount = perOrb + (i == 0 ? remainder : 0); // 나머지는 첫 오브에 몰아줍니다.
      orb.name = $"ExpOrb_{amount}";
    }
  }

  // 골드 드랍 메서드
  private void DropGold()
  {
    // 0.0 ~ 100.0 사이의 랜덤 값을 뽑습니다.
    float randomValue = Random.Range(0f, 100f);

    // 랜덤 값이 설정된 드랍 확률보다 낮고, 골드 프리팹이 할당되어 있다면
    if (randomValue <= goldDropChance && goldPrefab != null)
    {
      // 현재 적의 위치에 골드 프리팹을 생성합니다.
      Instantiate(goldPrefab, transform.position, Quaternion.identity);
    }
  }

  // 사이코 잠식도 아이템 드랍 메서드
  private void DropPsychoCorruptionItem()
  {
    // 0.0 ~ 100.0 사이의 랜덤 값을 뽑습니다.
    float randomValue = Random.Range(0f, 100f);

    // 랜덤 값이 설정된 드랍 확률보다 낮고, 사이코 잠식도 아이템 프리팹이 할당되어 있다면
    if (randomValue <= psychoCorruptionDropChance && psychoCorruptionItemPrefab != null)
    {
      // 현재 적의 위치에 사이코 잠식도 아이템 프리팹을 생성합니다.
      Instantiate(psychoCorruptionItemPrefab, transform.position, Quaternion.identity);
    }
  }

  // 데이터칩 드랍 메서드
  private void DropDataChip()
  {
    // 0.0 ~ 100.0 사이의 랜덤 값을 뽑습니다.
    float randomValue = Random.Range(0f, 100f);

    // 랜덤 값이 설정된 드랍 확률보다 낮고, 데이터칩 프리팹이 할당되어 있다면
    if (randomValue <= dataChipDropChance && dataChipPrefab != null)
    {
      // 현재 적의 위치에 데이터칩 프리팹을 생성합니다.
      Instantiate(dataChipPrefab, transform.position, Quaternion.identity);
      Debug.Log("데이터칩을 드랍했습니다!");
    }
  }
}
