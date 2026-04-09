using System.Collections;
using UnityEngine;
using NeoSurvive.Characters;
using NeoSurvive.Weapon;
using NeoSurvive.Buff; // (추가)

// Player 클래스는 플레이어 캐릭터를 나타냅니다.
// Character 클래스를 상속받아 캐릭터의 기본 기능을 모두 가집니다.
public class Player : Character
{
  //************************버프/디버프 관련 헬퍼************************//
  private StatusFlags _playerFlags; // (추가)

  // (추가) 다른 스크립트들이 쉽게 참조하도록
  public StatusFlags Status => _playerFlags != null
   ? _playerFlags
   : (_playerFlags = GetComponent<StatusFlags>() ?? gameObject.AddComponent<StatusFlags>()); // (추가)
  public bool CanMove => !Status.moveBlocked;   // (추가)
  public bool CanAttack => !Status.attackBlocked; // (추가)

  // (추가) "받는 피해 배율"을 적용해주는 헬퍼 (실제 데미지 계산 위치에서 이걸 써주면 됨)
  public float ApplyIncomingDamage(float damage) => damage * Status.incomingDamageMul; // (추가)

  // (추가) StatusFlags 배율을 Stat에 반영하기 위한 캐시
  private float lastMoveSpeedMul = 1f;      // (추가)
  private float lastOutgoingDamageMul = 1f; // (추가)

  // (추가) StatusFlags의 배율을 Stat 퍼센트 모디파이어로 적용/해제(델타 방식)
  private void SyncBuffMultipliersToStats() // (추가)
  {
    // StatusFlags 없으면 붙이고 진행
    var f = Status;

    // 1) 이동속도 배율(moveSpeedMul) -> moveSpeed Stat percent modifier로 반영
    if (!Mathf.Approximately(f.moveSpeedMul, lastMoveSpeedMul))
    {
      // Stat의 AddPercentModifier는 "0.25f = +25%" 방식이므로
      // 배율 1.2 -> +0.2, 0.7 -> -0.3 이 되도록 변환해서 "델타"만큼 추가
      float newPercent = f.moveSpeedMul - 1f;
      float oldPercent = lastMoveSpeedMul - 1f;
      float delta = newPercent - oldPercent;

      moveSpeed.AddPercentModifier(delta);
      lastMoveSpeedMul = f.moveSpeedMul;
    }

    // 2) 가하는 데미지 배율(outgoingDamageMul) -> attackDamage Stat percent modifier로 반영
    if (!Mathf.Approximately(f.outgoingDamageMul, lastOutgoingDamageMul))
    {
      float newPercent = f.outgoingDamageMul - 1f;
      float oldPercent = lastOutgoingDamageMul - 1f;
      float delta = newPercent - oldPercent;

      attackDamage.AddPercentModifier(delta);
      lastOutgoingDamageMul = f.outgoingDamageMul;
    }

    // ⚠ moveBlocked/attackBlocked는 여기서 Stat로 “강제 0” 처리하지 않음.
    // 이유: 기존 이동/공격 로직을 깨지 않기 위해.
    // 대신 CanMove/CanAttack를 추가했으니, 이동/공격 쪽에서 참조하면 CC가 완성됨.
  }
  //************************여기까지 버프/디버프************************//


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
  // 공격 속도 (1.0 = 100%)
  [SerializeField]
  private Stat attackSpeed = new Stat(1.0f);

  [Header("이동 속도 설정")]
  [SerializeField]
  private Stat moveSpeed = new Stat(5f); // PlayerController가 참조할 이동 속도 Stat
  public float CurrentMoveSpeed => moveSpeed.GetValue(); // PlayerController가 최종 이동 속도를 가져갈 프로퍼티

  [Header("현재 스탯 값 (인스펙터 확인용)")]
  [SerializeField]
  [Tooltip("현재 이동속도 최종 값")]
  private float currentMoveSpeedValue;
  [SerializeField]
  [Tooltip("현재 공격속도 최종 값")]
  private float currentAttackSpeedValue;
  [SerializeField]
  [Tooltip("현재 공격력 최종 값")]
  private float currentAttackDamageValue;

  [Header("사이코 잠식도 (과부하) 시스템")]
  [SerializeField]
  [Range(0f, 100f)]
  [Tooltip("현재 사이코 잠식도 (0~100%)")]
  private float psychoCorruption = 0f;

  // 사이코 잠식도 변경 이벤트 (현재 값, 최대 값)
  public static event System.Action<float, float> OnPsychoCorruptionChanged;

  // 폭주 상태 시작/종료 이벤트
  public static event System.Action OnBerserkStarted;
  public static event System.Action OnBerserkEnded;

  // 사이코 잠식도 관련 프로퍼티
  public float PsychoCorruption => psychoCorruption;
  public bool IsBerserk { get; private set; } = false;

  // 사이코 잠식도 효과 적용 여부
  private bool has30PercentEffect = false;
  private bool has60PercentEffect = false;
  private bool has100PercentEffect = false;

  // 원래 스탯 값 저장 (효과 제거 시 복원용)
  private float originalMoveSpeedPercent = 0f;
  private float originalAttackSpeedPercent = 0f;
  private float originalDamagePercent = 0f;

  [Header("신경 링크 (Neural Link) 시스템")]
  [SerializeField]
  [Range(0f, 100f)]
  [Tooltip("현재 신경링크 게이지 (0~100%)")]
  private float neuralLinkGauge = 0f;

  // 신경링크 게이지 변경 이벤트 (현재 값, 최대 값)
  public static event System.Action<float, float> OnNeuralLinkGaugeChanged;

  // 신경링크 발동 이벤트
  public static event System.Action<CharacterType> OnNeuralLinkActivated;

  // 신경링크 관련 프로퍼티
  public float NeuralLinkGauge => neuralLinkGauge;

  // 현재 선택된 캐릭터 타입
  private CharacterType currentCharacterType = CharacterType.Hacker;

  // [Coop] 로컬 플레이어 여부
  public bool IsLocal { get; set; } = true; // 기본값은 true (싱글용)

  public void ApplyStatChange(StatType type, float flat, float percent)
  {
    switch (type)
    {
      case StatType.AttackDamage:
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
    StartCoroutine(ApplyTraitsAfterManagerInit());
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

  /// <summary>
  /// 캐릭터 타입에 따라 스탯을 초기화합니다.
  /// </summary>
  public void InitCharacter(CharacterType type)
  {
    CharacterData data = (type == CharacterType.Hacker) ? hackerData : cyborgData;
    if (data == null)
    {
      Debug.LogError($"[Player] {type}에 해당하는 캐릭터 데이터가 없습니다!");
      return;
    }

    // 기본 스탯 설정
    healthStat.BaseValue = data.baseHealth;
    currentHealth = data.baseHealth;
    attackDamage.BaseValue = data.baseAttackDamage;
    attackRange.BaseValue = data.baseAttackRange;
    attackSpeed.BaseValue = data.baseAttackSpeed;
    moveSpeed.BaseValue = data.baseMoveSpeed;

    Debug.Log($"[Player] {type} 캐릭터 초기화 완료: HP={data.baseHealth}, ATK={data.baseAttackDamage}");

    // TODO: 캐릭터 타입에 따른 스프라이트/애니메이터 교체 로직 추가 필요
  }

  // 부모 클래스(Character)의 Die 메서드를 오버라이드(재정의)하여
  // 플레이어에게 특화된 죽음 처리 로직을 구현합니다.
  protected override void Die()
  {
    if (IsDead) return;
    base.Die();

    Debug.Log($"{gameObject.name} (플레이어)가 패배했습니다!");

    // GameManager에 플레이어의 죽음을 알리고 골드를 저장합니다.
    if (GameManager.Instance != null && IsLocal)
    {
      GameManager.Instance.OnPlayerDeath();
    }

    // 싱글플레이에서는 즉시 제거합니다.
    Destroy(gameObject);
  }

  /// <summary>
  /// 플레이어를 부활시킵니다.
  /// </summary>
  public override void Revive(float healthRatio = 1.0f)
  {
    base.Revive(healthRatio);

    // 컨트롤러 재활성화 (로컬인 경우만)
    var controller = GetComponent<PlayerController>();
    if (controller != null && IsLocal)
    {
      controller.enabled = true;
    }

    // 시각적 복구
    var sprite = GetComponentInChildren<SpriteRenderer>();
    if (sprite != null)
    {
      Color c = sprite.color;
      c.a = 1.0f;
      sprite.color = c;
    }

    Debug.Log($"[Player] {gameObject.name} 부활 완료 (HP Ratio: {healthRatio})");
  }

  // 플레이어가 경험치를 얻었을 때 호출되는 메서드입니다.
  public void GainExperience(int amount)
  {
    experience += amount;
    // 경험치 UI 갱신
    OnExpChanged?.Invoke(experience, requiredExpForNextLevel);

    // 경험치가 충분한지 확인하고 레벨업 처리
    CheckLevelUp();
  }

  // ===================== Exp Orb 처리 =====================

  private void OnTriggerEnter2D(Collider2D other)
  {
    // 로컬 플레이어만 아이템 획득 판정
    if (!IsLocal) return;

    // Enemy가 만든 Exp Orb인지 확인
    if (!other.name.StartsWith("ExpOrb_")) return;

    int amount = ParseExpOrbAmount(other.name);
    if (amount <= 0) amount = 1;

    GainExperience(amount);

    Destroy(other.gameObject);
  }

  private int ParseExpOrbAmount(string orbName)
  {
    int idx = orbName.LastIndexOf('_');
    if (idx < 0 || idx == orbName.Length - 1) return 0;

    string value = orbName.Substring(idx + 1);
    return int.TryParse(value, out int result) ? result : 0;
  }
  // 매 프레임마다 인스펙터 표시용 값 업데이트 및 입력 처리
  private void Update()
  {
    SyncBuffMultipliersToStats(); // (추가) 

    UpdateInspectorStats();

    // R키로 신경링크 발동 (로컬 플레이어만)
    if (IsLocal && Input.GetKeyDown(KeyCode.R))
    {
      ActivateNeuralLink();
    }
  }

  /// <summary>
  /// [Coop] 특정 무기의 공격을 실행 (원격 플레이어 동기화용)
  /// </summary>
  public void ExecuteAttack(int weaponId, Vector3 direction)
  {
    var weaponManager = GetComponent<WeaponManager>();
    if (weaponManager != null)
    {
      weaponManager.ExecuteWeaponAttack(weaponId, direction);
    }
  }

  /// <summary>
  /// [Coop] 원격 플레이어의 무기 장착 동기화
  /// </summary>
  public void SyncWeaponEquip(int weaponId)
  {
    var weaponManager = GetComponent<WeaponManager>();
    if (weaponManager != null)
    {
      weaponManager.SyncWeaponEquip(weaponId);
    }
  }

  // 게임 시작 시 캐릭터 타입 가져오기
  protected override void Start()
  {
    base.Start();

    // GameManager에서 선택된 캐릭터 타입 가져오기
    if (GameManager.Instance != null)
    {
      currentCharacterType = GameManager.Instance.GetSelectedCharacter();
    }
  }

  /// <summary>
  /// 인스펙터에서 확인할 수 있도록 현재 스탯 값들을 업데이트합니다.
  /// </summary>
  private void UpdateInspectorStats()
  {
    if (moveSpeed != null)
      currentMoveSpeedValue = moveSpeed.GetValue();
    if (attackSpeed != null)
      currentAttackSpeedValue = attackSpeed.GetValue();
    if (attackDamage != null)
      currentAttackDamageValue = attackDamage.GetValue();
  }


  // ===================== 레벨업 로직 =====================

  private void CheckLevelUp()
  {
    while (experience >= requiredExpForNextLevel)
    {
      experience -= requiredExpForNextLevel;
      LevelUpInternal();
    }

    OnExpChanged?.Invoke(experience, requiredExpForNextLevel);
  }

  private void LevelUpInternal()
  {
    level++;

    requiredExpForNextLevel =
      Mathf.CeilToInt(requiredExpForNextLevel * growthMultiplier);

    Debug.Log($"🎉 레벨업! 현재 레벨: {level}");

    OnLevelUp?.Invoke(level);
  }

  // ===================== 사이코 잠식도 시스템 =====================

  /// <summary>
  /// 사이코 잠식도를 추가합니다.
  /// </summary>
  /// <param name="amount">증가할 양 (퍼센트)</param>
  public void AddPsychoCorruption(float amount)
  {
    psychoCorruption = Mathf.Clamp(psychoCorruption + amount, 0f, 100f);
    OnPsychoCorruptionChanged?.Invoke(psychoCorruption, 100f);

    UpdatePsychoCorruptionEffects();

    Debug.Log($"사이코 잠식도: {psychoCorruption:F1}%");
  }

  /// <summary>
  /// 사이코 잠식도 효과를 업데이트합니다.
  /// </summary>
  private void UpdatePsychoCorruptionEffects()
  {
    // 30% 효과 적용/제거
    if (psychoCorruption >= 30f && !has30PercentEffect)
    {
      Apply30PercentEffect();
      has30PercentEffect = true;
    }
    else if (psychoCorruption < 30f && has30PercentEffect)
    {
      Remove30PercentEffect();
      has30PercentEffect = false;
    }

    // 60% 효과 적용/제거
    if (psychoCorruption >= 60f && !has60PercentEffect)
    {
      Apply60PercentEffect();
      has60PercentEffect = true;
    }
    else if (psychoCorruption < 60f && has60PercentEffect)
    {
      Remove60PercentEffect();
      has60PercentEffect = false;
    }

    // 100% 효과 적용 (폭주 상태)
    if (psychoCorruption >= 100f && !has100PercentEffect)
    {
      StartBerserkState();
      has100PercentEffect = true;
    }
  }

  /// <summary>
  /// 30% 효과: 이동속도 +25%, 공격속도 +25%
  /// </summary>
  private void Apply30PercentEffect()
  {
    moveSpeed.AddPercentModifier(0.25f);
    attackSpeed.AddPercentModifier(0.25f);
    originalMoveSpeedPercent += 0.25f;
    originalAttackSpeedPercent += 0.25f;

    float newMoveSpeed = moveSpeed.GetValue();
    float newAttackSpeed = attackSpeed.GetValue();
    Debug.Log($"사이코 잠식도 30% 달성: 이동속도/공격속도 +25% | 이동속도: {newMoveSpeed:F2} | 공격속도: {newAttackSpeed:F2}");
  }

  private void Remove30PercentEffect()
  {
    moveSpeed.AddPercentModifier(-0.25f);
    attackSpeed.AddPercentModifier(-0.25f);
    originalMoveSpeedPercent -= 0.25f;
    originalAttackSpeedPercent -= 0.25f;
  }

  /// <summary>
  /// 60% 효과: 대미지 +25%, 화면 노이즈
  /// </summary>
  private void Apply60PercentEffect()
  {
    attackDamage.AddPercentModifier(0.25f);
    originalDamagePercent += 0.25f;

    float newDamage = attackDamage.GetValue();
    Debug.Log($"사이코 잠식도 60% 달성: 대미지 +25%, 화면 노이즈 시작 | 공격력: {newDamage:F2}");

    // 화면 노이즈 효과 시작 (UIManager를 통해)
    if (UIManager.Instance != null)
    {
      UIManager.Instance.SetScreenNoise(true);
    }
  }

  private void Remove60PercentEffect()
  {
    attackDamage.AddPercentModifier(-0.25f);
    originalDamagePercent -= 0.25f;

    // 화면 노이즈 효과 종료
    if (UIManager.Instance != null)
    {
      UIManager.Instance.SetScreenNoise(false);
    }
  }

  /// <summary>
  /// 100% 효과: 폭주 상태 시작
  /// </summary>
  private void StartBerserkState()
  {
    if (IsBerserk) return; // 이미 폭주 상태면 중복 실행 방지

    IsBerserk = true;
    OnBerserkStarted?.Invoke();

    // 현재 체력의 30% 감소
    float healthLoss = currentHealth * 0.3f;
    currentHealth = Mathf.Max(1f, currentHealth - healthLoss);
    NotifyHealthChanged();

    // 이동속도/공격속도/대미지 +50% (기존 효과와 중첩)
    moveSpeed.AddPercentModifier(0.50f);
    attackSpeed.AddPercentModifier(0.50f);
    attackDamage.AddPercentModifier(0.50f);
    originalMoveSpeedPercent += 0.50f;
    originalAttackSpeedPercent += 0.50f;
    originalDamagePercent += 0.50f;

    Debug.Log("⚠️ 폭주 상태 시작! 15초간 통제 불가, 스탯 +50%");

    // 15초 후 폭주 상태 종료
    StartCoroutine(BerserkStateCoroutine());
  }

  /// <summary>
  /// 폭주 상태 코루틴 (15초 후 종료)
  /// </summary>
  private System.Collections.IEnumerator BerserkStateCoroutine()
  {
    yield return new WaitForSeconds(15f);

    EndBerserkState();
  }

  /// <summary>
  /// 폭주 상태 종료
  /// </summary>
  private void EndBerserkState()
  {
    if (!IsBerserk) return;

    IsBerserk = false;
    OnBerserkEnded?.Invoke();

    // 모든 사이코 잠식도 효과 제거
    RemoveAllPsychoCorruptionEffects();

    // 사이코 잠식도 0%로 리셋
    psychoCorruption = 0f;
    OnPsychoCorruptionChanged?.Invoke(psychoCorruption, 100f);

    // 효과 플래그 리셋
    has30PercentEffect = false;
    has60PercentEffect = false;
    has100PercentEffect = false;

    Debug.Log("폭주 상태 종료. 사이코 잠식도 0%로 리셋");
  }

  /// <summary>
  /// 모든 사이코 잠식도 효과를 제거합니다.
  /// </summary>
  private void RemoveAllPsychoCorruptionEffects()
  {
    // 모든 퍼센트 수정자 제거
    if (originalMoveSpeedPercent > 0f)
    {
      moveSpeed.AddPercentModifier(-originalMoveSpeedPercent);
      originalMoveSpeedPercent = 0f;
    }

    if (originalAttackSpeedPercent > 0f)
    {
      attackSpeed.AddPercentModifier(-originalAttackSpeedPercent);
      originalAttackSpeedPercent = 0f;
    }

    if (originalDamagePercent > 0f)
    {
      attackDamage.AddPercentModifier(-originalDamagePercent);
      originalDamagePercent = 0f;
    }

    // 화면 노이즈 종료
    if (UIManager.Instance != null)
    {
      UIManager.Instance.SetScreenNoise(false);
    }
  }

  // ===================== 신경 링크 시스템 =====================

  /// <summary>
  /// 신경링크 게이지를 추가합니다.
  /// </summary>
  /// <param name="amount">증가할 양 (퍼센트)</param>
  public void AddNeuralLinkGauge(float amount)
  {
    neuralLinkGauge = Mathf.Clamp(neuralLinkGauge + amount, 0f, 100f);
    OnNeuralLinkGaugeChanged?.Invoke(neuralLinkGauge, 100f);

    Debug.Log($"신경링크 게이지: {neuralLinkGauge:F1}%");
  }

  /// <summary>
  /// 신경링크를 발동합니다. (R키)
  /// </summary>
  private void ActivateNeuralLink()
  {
    // 게이지가 100%가 아니면 발동 불가
    if (neuralLinkGauge < 100f)
    {
      Debug.Log($"신경링크 게이지 부족: {neuralLinkGauge:F1}% / 100%");
      return;
    }

    ExecuteNeuralLink();
  }

  /// <summary>
  /// 실제 신경링크 로직을 실행 (로컬/원격 공용)
  /// </summary>
  public void ExecuteNeuralLink()
  {
    // 게이지 소모
    neuralLinkGauge = 0f;
    OnNeuralLinkGaugeChanged?.Invoke(neuralLinkGauge, 100f);

    // 캐릭터 타입에 따라 다른 효과 발동
    if (currentCharacterType == CharacterType.Hacker)
    {
      ActivateHackerNeuralLink();
    }
    else if (currentCharacterType == CharacterType.Cyborg)
    {
      ActivateCyborgNeuralLink();
    }

    OnNeuralLinkActivated?.Invoke(currentCharacterType);
    Debug.Log($"신경링크 발동! ({currentCharacterType})");
  }

  /// <summary>
  /// 해커 플레이 시: 사이보그 난입 + 지면 강타 + 화면 내 적 스턴 5초
  /// </summary>
  private void ActivateHackerNeuralLink()
  {
    // 화면 전체 범위 계산
    Camera cam = Camera.main;
    if (cam == null) return;

    float worldHeight = cam.orthographicSize * 2f;
    float worldWidth = worldHeight * cam.aspect;
    Vector3 screenCenter = cam.transform.position;

    // 사이보그 난입 위치 (플레이어 근처)
    Vector3 cyborgSpawnPos = transform.position + Vector3.up * 2f;

    // 지면 강타 이펙트 (플레이어 위치)
    CreateGroundSlamEffect(transform.position);

    // 화면 내 모든 적 스턴 5초
    StunAllEnemiesInScreen(screenCenter, worldWidth, worldHeight, 5f);

    Debug.Log("해커 신경링크: 사이보그 난입 + 지면 강타 + 적 스턴 5초");
  }

  /// <summary>
  /// 사이보그 플레이 시: 해커 데이터 스톰 + 화면 내 적 마비 5초
  /// </summary>
  private void ActivateCyborgNeuralLink()
  {
    // 화면 전체 범위 계산
    Camera cam = Camera.main;
    if (cam == null) return;

    float worldHeight = cam.orthographicSize * 2f;
    float worldWidth = worldHeight * cam.aspect;
    Vector3 screenCenter = cam.transform.position;

    // 데이터 스톰 이펙트 (화면 전체)
    CreateDataStormEffect(screenCenter, worldWidth, worldHeight);

    // 화면 내 모든 적 마비 5초
    ParalyzeAllEnemiesInScreen(screenCenter, worldWidth, worldHeight, 5f);

    Debug.Log("사이보그 신경링크: 해커 데이터 스톰 + 적 마비 5초");
  }

  /// <summary>
  /// 화면 내 모든 적을 스턴시킵니다.
  /// </summary>
  private void StunAllEnemiesInScreen(Vector3 center, float width, float height, float duration)
  {
    // 화면 범위 내의 모든 적 찾기
    Collider2D[] enemies = Physics2D.OverlapBoxAll(center, new Vector2(width, height), 0f);

    foreach (var col in enemies)
    {
      if (col.CompareTag("Enemy"))
      {
        EnemyController enemyController = col.GetComponent<EnemyController>();
        if (enemyController != null)
        {
          // 스턴 효과 적용 (이동 속도 0으로)
          enemyController.ApplySlow(0f, duration);
        }
      }
    }
  }

  /// <summary>
  /// 화면 내 모든 적을 마비시킵니다.
  /// </summary>
  private void ParalyzeAllEnemiesInScreen(Vector3 center, float width, float height, float duration)
  {
    // 화면 범위 내의 모든 적 찾기
    Collider2D[] enemies = Physics2D.OverlapBoxAll(center, new Vector2(width, height), 0f);

    foreach (var col in enemies)
    {
      if (col.CompareTag("Enemy"))
      {
        EnemyController enemyController = col.GetComponent<EnemyController>();
        if (enemyController != null)
        {
          // 마비 효과 적용 (이동 속도 0으로)
          enemyController.ApplySlow(0f, duration);
        }
      }
    }
  }

  /// <summary>
  /// 지면 강타 이펙트 생성
  /// </summary>
  private void CreateGroundSlamEffect(Vector3 position)
  {
    // 시각적 이펙트 (원형 충격파)
    NeoSurvive.UI.VisualEffectHelper.CreateCircleEffect(position, 3f, Color.yellow, 0.5f);
  }

  /// <summary>
  /// 데이터 스톰 이펙트 생성
  /// </summary>
  private void CreateDataStormEffect(Vector3 center, float width, float height)
  {
    // 화면 전체에 데이터 스톰 이펙트 (여러 개의 원형 이펙트)
    int effectCount = 5;
    for (int i = 0; i < effectCount; i++)
    {
      Vector3 randomPos = center + new Vector3(
          Random.Range(-width * 0.4f, width * 0.4f),
          Random.Range(-height * 0.4f, height * 0.4f),
          0f
      );
      NeoSurvive.UI.VisualEffectHelper.CreateCircleEffect(randomPos, 2f, Color.cyan, 1f);
    }
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

  // ===================== 특성 시스템 연동 =====================

  // 모든 특성으로 인한 스탯 보너스를 초기화하는 메서드
  public void ResetTraitBonuses()
  {
    // Stat 클래스에 ClearModifiers 같은 메서드가 있다면 더 효율적입니다.
    // 현재는 캐릭터 기본 데이터로 스탯을 다시 설정하고 업그레이드를 재적용하는 방식입니다.
    InitCharacter(currentCharacterType);
    ApplyUpgrades();
    Debug.Log("Trait bonuses have been reset to character base stats.");
  }

  // TraitManager로부터 모든 특성 효과를 가져와 플레이어 스탯에 적용합니다.
  public void ApplyAllTraitEffects()
  {
    if (TraitManager.Instance == null)
    {
      Debug.LogWarning("TraitManager instance not found. Cannot apply trait effects.");
      return;
    }

    ResetTraitBonuses(); // 먼저 모든 보너스를 초기화

    var acquiredTraits = TraitManager.Instance.playerTraitData.acquiredTraits;
    // TODO: 현재 캐릭터 클래스를 GameManager 등에서 정확히 가져와야 합니다.
    string characterClass = GameManager.Instance != null ? GameManager.Instance.GetSelectedCharacter().ToString() : "Hacker";

    Debug.Log($"Applying all trait effects for {characterClass}. {acquiredTraits.Count} traits acquired.");

    foreach (var entry in acquiredTraits)
    {
      Trait trait = TraitManager.Instance.GetTrait(entry.Key, characterClass);
      if (trait != null && trait.effect != null)
      {
        ApplyTraitEffect(trait.effect, entry.Value);
      }
    }
    UpdateInspectorStats(); // 변경된 스탯을 인스펙터에 반영
  }

  // 개별 특성 효과를 스탯에 적용하는 메서드
  private void ApplyTraitEffect(TraitEffect effect, int level)
  {
    float totalValue = effect.value * level;

    Stat targetStat = null;
    switch (effect.stat)
    {
      // 공통
      case "MaxHealth": targetStat = healthStat; break;
      case "HealthRegen": /* Regen은 별도 처리 필요 */ break;
      case "DodgeChance": /* 회피 로직에 적용 필요 */ break;
      case "CritChance": /* 치명타 로직에 적용 필요 */ break;
      case "CritDamage": /* 치명타 로직에 적용 필요 */ break;
      case "GlobalCooldownReduction": /* 모든 무기 쿨다운에 적용 필요 */ break;
      case "PickupRange": /* 아이템 픽업 스크립트에 적용 필요 */ break;
      case "ExpGain": /* 경험치 획득 로직에 적용 필요 */ break;
      case "CreditGain": /* 크레딧 획득 로직에 적용 필요 */ break;
      case "MoveSpeed": targetStat = moveSpeed; break;
      case "DashCooldown": /* 대시 스킬에 적용 필요 */ break;
      case "DashDistance": /* 대시 스킬에 적용 필요 */ break;
      case "DashCharges": /* 대시 스킬에 적용 필요 */ break;

      // 해커
      case "AttackSpeed": targetStat = attackSpeed; break;
      case "CooldownReduction": /* 스킬 쿨다운 스탯에 적용 필요 */ break;
      case "ProjectileDamage": targetStat = attackDamage; break;
      case "SummonDuration": /* 소환수 지속시간에 적용 필요 */ break;
      case "ProjectileRange": targetStat = attackRange; break;
      case "ProjectileSize": /* 투사체 크기 조절 로직에 적용 필요 */ break;

      // 사이보그
      case "BaseDamage": targetStat = attackDamage; break;
      case "DamageReduction": /* 받는 데미지 계산 시 적용 필요 */ break;
      case "AttackRange": targetStat = attackRange; break;
      case "DotTickSpeed": /* 도트 데미지 로직에 적용 필요 */ break;
      case "StatusEffectUp": /* 상태이상 로직에 적용 필요 */ break;

      default:
        // Stat으로 처리되지 않는 특수 효과들
        ApplySpecialTraitEffect(effect.stat, level);
        return;
    }

    if (targetStat != null)
    {
      if (effect.type == "Percentage")
      {
        targetStat.AddPercentModifier(totalValue / 100f);
        Debug.Log($"Applied {effect.stat}: +{totalValue}% to {targetStat}");
      }
      else // Flat
      {
        targetStat.AddFixedModifier(totalValue);
        Debug.Log($"Applied {effect.stat}: +{totalValue} flat to {targetStat}");
      }
    }
  }

  // Stat으로 직접 처리되지 않는 특수 효과들을 처리하는 메서드
  private void ApplySpecialTraitEffect(string effectStat, int level)
  {
    // TODO: 여기에 각 특수 효과에 대한 실제 로직을 구현해야 합니다.
    // 예를 들어, 도탄 횟수 증가, 소환수 강화, 특정 조건부 데미지 증가 등
    Debug.Log($"Applying special effect: {effectStat}, Level: {level}. Implementation needed.");
    switch (effectStat)
    {
      case "ExtraLife":
        // TODO: 부활 기능 구현
        break;
      case "ProjectileBounce":
        // TODO: 무기 시스템에 튕기는 횟수 전달
        break;
      case "SummonEnhancement":
        // TODO: 소환수 관리 시스템에 강화 효과 전달
        break;
        // ... 기타 등등
    }
  }

  // TraitManager가 초기화된 후 특성 효과를 적용하기 위한 코루틴
  private System.Collections.IEnumerator ApplyTraitsAfterManagerInit()
  {
    // TraitManager 싱글톤 인스턴스가 준비될 때까지 한 프레임 대기
    yield return new WaitUntil(() => TraitManager.Instance != null);
    ApplyAllTraitEffects();
  }
}
