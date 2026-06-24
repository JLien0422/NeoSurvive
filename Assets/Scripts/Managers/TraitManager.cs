using System;
using System.Collections.Generic;
using UnityEngine;

public enum TraitCategory
{
  Hacker,
  Cyborg,
  Common
}

[Serializable]
public class TraitPrerequisite
{
  public string traitId;
  public int requiredLevel;
}

/// <summary>
/// 그룹 내의 조건들은 "모두" 만족해야 합니다 (AND 조건).
/// </summary>
[Serializable]
public class TraitRequirementGroup
{
  [Tooltip("이 리스트 안의 조건들은 모두 달성해야 통과됩니다. (AND)")]
  public List<TraitPrerequisite> conditions = new List<TraitPrerequisite>();
}

[Serializable]
public class TraitData
{
  public const float DefaultPsychoCorruptionIncrease = 10f;

  [Header("기본 정보")]
  [Tooltip("특성의 고유 ID. 다른 스크립트에서 이 ID로 특성 레벨을 조회합니다.")]
  public string traitId;
  public string displayName;
  [TextArea(2, 3)]
  public string description;     // 특성 설명
  [TextArea(2, 3)]
  public string effectDescription; // 특성 효과 
  public TraitCategory category;
  public int maxLevel;

  [Header("사이코 잠식도")]
  [Tooltip("이 특성을 1레벨 찍을 때마다 증가하는 사이코 잠식도입니다.")]
  public float psychoCorruptionIncrease = DefaultPsychoCorruptionIncrease;

  [Header("선행 조건 (OR 묶음)")]
  [Tooltip("이 리스트의 '그룹' 중 하나라도 만족하면 선행 조건이 충족됩니다. (그룹 끼리는 OR, 그룹 내부는 AND)")]
  public List<TraitRequirementGroup> requirementGroups = new List<TraitRequirementGroup>();
}

/// <summary>
/// 게임 내 특성(Trait) 시스템의 전반적인 데이터와 업그레이드 로직을 담당합니다.
/// UI 관련 로직은 TraitUI 스크립트로 분리하여 유연성을 높였습니다.
/// </summary>
public class TraitManager : MonoBehaviour
{
  public static TraitManager Instance { get; private set; }

  private const string KEY_TRAIT_LEVELS = "Save_TraitLevels";
  public const float MaxPsychoCorruption = 100f;

  [Header("특성 업그레이드 비용")]
  public int baseTraitCost = 100;
  public int traitCostIncrement = 100;

  [Header("특성 데이터 (가변적)")]
  [Tooltip("게임 내 존재하는 모든 특성을 이곳에 정의합니다. 기획 변경 시 코드 수정 없이 이곳에서 수정하세요.")]
  public List<TraitData> availableTraits = new List<TraitData>();

  // 런타임 저장 데이터: <traitId, currentLevel>
  private Dictionary<string, int> traitLevels = new Dictionary<string, int>();

  private static readonly Dictionary<string, float> TraitPsychoCostPerLevel = new Dictionary<string, float>
  {
    { "plasma_strand", 8f },
    { "compute_optimization", 10f },
    { "energy_overload", 6f },
    { "facility_augmentation", 10f },
    { "radar", 10f },
    { "exoskeleton", 10f },
    { "compile_node_expansion", 10f },
    { "ricochet", 15f },
    { "inertia_charge", 15f },
    { "virus_development", 15f },
    { "bandwidth_expansion", 15f },
    { "hydraulic_motor_amp", 6f },
    { "nano_armor", 10f },
    { "output_optimization", 10f },
    { "overcharged_battery", 10f },
    { "superconductive_circuits", 10f },
    { "reactive_exoskeleton", 10f },
    { "inertia_frame", 10f },
    { "critical_breakthrough", 15f },
    { "chain_discharge", 15f },
    { "core_fusion", 15f }
  };

  // 특성 업그레이드 시 발생하는 이벤트 (ID, 새 레벨)
  public event Action<string, int> OnTraitUpgraded;
  public event Action OnTraitsLoaded;
  public event Action<float, float> OnPsychoCorruptionChanged;

  private static bool _spawningPersistent;

  private void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(this);
      return;
    }

    if (GetComponent<LobbyManager>() != null)
    {
      SpawnPersistentFromSceneConfig();
      return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);

    if (_spawningPersistent)
      return;

    EnsureDefaultCyborgTraits();
    LoadTraits();
  }

  private void OnDestroy()
  {
    if (Instance == this)
      Instance = null;
  }

  private void SpawnPersistentFromSceneConfig()
  {
    GameObject host = new GameObject("TraitManager");
    DontDestroyOnLoad(host);

    _spawningPersistent = true;
    TraitManager persistent = host.AddComponent<TraitManager>();
    _spawningPersistent = false;

    persistent.baseTraitCost = baseTraitCost;
    persistent.traitCostIncrement = traitCostIncrement;
    persistent.availableTraits = availableTraits != null
      ? new List<TraitData>(availableTraits)
      : new List<TraitData>();

    Instance = persistent;
    persistent.EnsureDefaultCyborgTraits();
    persistent.LoadTraits();

    Destroy(this);
  }

  // 코드 레벨에서 기본 사이보그 특성들을 등록합니다. 인스펙터에 이미 수동 등록된 항목은 덮어쓰지 않습니다.
  private void EnsureDefaultCyborgTraits()
  {
    if (availableTraits == null)
      availableTraits = new List<TraitData>();

    void AddIfMissing(TraitData td)
    {
      if (availableTraits.Find(x => x.traitId == td.traitId) == null)
        availableTraits.Add(td);
    }

    AddIfMissing(new TraitData
    {
      traitId = "hydraulic_motor_amp",
      displayName = "유압 모터 증폭 (5)",
      description = "무기 기본 공격력 증가(물리, 에너지 전부 포함) +5%.",
      effectDescription = "",
      category = TraitCategory.Cyborg,
      maxLevel = 5
    });

    AddIfMissing(new TraitData
    {
      traitId = "nano_armor",
      displayName = "나노 장갑 (3)",
      description = "받는 피해 감소 (인파이팅 생존용) +5%.",
      effectDescription = "",
      category = TraitCategory.Cyborg,
      maxLevel = 3
    });

    AddIfMissing(new TraitData
    {
      traitId = "output_optimization",
      displayName = "출력 최적화 (3)",
      description = "공격 범위 증가 (블랙홀, 검기, 장판, 브레스 전부 포함) +5%.",
      effectDescription = "",
      category = TraitCategory.Cyborg,
      maxLevel = 3
    });

    AddIfMissing(new TraitData
    {
      traitId = "overcharged_battery",
      displayName = "과충전 배터리 (3)",
      description = "다단 히트 주기 감소: 지속 딜링의 틱 간격이 짧아짐 +10%.",
      effectDescription = "",
      category = TraitCategory.Cyborg,
      maxLevel = 3,
      requirementGroups = new List<TraitRequirementGroup>
      {
        new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "hydraulic_motor_amp", requiredLevel = 5 } } },
        new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "output_optimization", requiredLevel = 3 } } }
      }
    });

    AddIfMissing(new TraitData
    {
      traitId = "superconductive_circuits",
      displayName = "초전도 회로 (3)",
      description = "상태이상(빙결, 방어력 감소, 받는 피해 증가 등) 지속 시간 및 효율 증가 +10%.",
      effectDescription = "",
      category = TraitCategory.Cyborg,
      maxLevel = 3,
      requirementGroups = new List<TraitRequirementGroup>
      {
        new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "output_optimization", requiredLevel = 3 } } },
        new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "nano_armor", requiredLevel = 3 } } }
      }
    });

    AddIfMissing(new TraitData
    {
      traitId = "reactive_exoskeleton",
      displayName = "반응형 외골격 (3)",
      description = "피격 시 혹은 근접 공격 적중 시 주변에 충격파 발생 (반사딜) +10%.",
      effectDescription = "",
      category = TraitCategory.Cyborg,
      maxLevel = 3,
      requirementGroups = new List<TraitRequirementGroup>
      {
        new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "hydraulic_motor_amp", requiredLevel = 5 } } },
        new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "nano_armor", requiredLevel = 3 } } },
        new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "output_optimization", requiredLevel = 3 } } }
      }
    });

    AddIfMissing(new TraitData
    {
      traitId = "inertia_frame",
      displayName = "관성 프레임 (3)",
      description = "넉백 저항 및 몹몰이 성능(블랙홀 흡입력, 넉백 거리) 증가 +10%.",
      effectDescription = "",
      category = TraitCategory.Cyborg,
      maxLevel = 3,
      requirementGroups = new List<TraitRequirementGroup>
      {
        new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "hydraulic_motor_amp", requiredLevel = 5 } } }
      }
    });

    AddIfMissing(new TraitData
    {
      traitId = "critical_breakthrough",
      displayName = "임계점 돌파 (1)",
      description = "잃은 체력 1%당 타격 피해량 및 공격 범위 1% 증가.",
      effectDescription = "",
      category = TraitCategory.Cyborg,
      maxLevel = 1,
      requirementGroups = new List<TraitRequirementGroup>
      {
        new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "reactive_exoskeleton", requiredLevel = 3 }, new TraitPrerequisite{ traitId = "nano_armor", requiredLevel = 3 } } }
      }
    });

    AddIfMissing(new TraitData
    {
      traitId = "chain_discharge",
      displayName = "연쇄 방전 (1)",
      description = "몹 처치 시 30% 확률로 주변 적에게 연쇄 번개(기본 피해량의 100%) 방출.",
      effectDescription = "",
      category = TraitCategory.Cyborg,
      maxLevel = 1,
      requirementGroups = new List<TraitRequirementGroup>
      {
        new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "overcharged_battery", requiredLevel = 3 }, new TraitPrerequisite{ traitId = "superconductive_circuits", requiredLevel = 3 } } }
      }
    });

    AddIfMissing(new TraitData
    {
      traitId = "core_fusion",
      displayName = "코어 융합 (1)",
      description = "장착한 모든 근거리 무기 공격이 대시 시 발동됨.",
      effectDescription = "",
      category = TraitCategory.Cyborg,
      maxLevel = 1,
      requirementGroups = new List<TraitRequirementGroup>
      {
        new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "overcharged_battery", requiredLevel = 3 }, new TraitPrerequisite{ traitId = "reactive_exoskeleton", requiredLevel = 3 } } }
      }
    });
  }

  /// <summary>
  /// 특정 특성의 현재 레벨을 가져옵니다. 미해금 시 0을 반환합니다.
  /// 게임 내 각종 효과 계산에서 이 함수를 호출하여 수치를 적용하세요.
  /// </summary>
  public int GetTraitLevel(string traitId)
  {
    if (traitLevels.TryGetValue(traitId, out int level))
    {
      return level;
    }
    return 0;
  }

  public TraitData GetTraitData(string traitId)
  {
    return availableTraits.Find(t => t.traitId == traitId);
  }

  public float GetPsychoCorruptionIncrease(string traitId)
  {
    TraitData traitData = GetTraitData(traitId);
    if (traitData == null)
      return 0f;

    // 공통 탭 특성은 사이코 잠식도를 올리지 않습니다.
    if (traitData.category == TraitCategory.Common)
      return 0f;

    return Mathf.Max(0f, traitData.psychoCorruptionIncrease);
  }

  public float GetCurrentPsychoCorruption()
  {
    return Mathf.Clamp(
      GetCurrentPsychoCorruption(TraitCategory.Hacker) + GetCurrentPsychoCorruption(TraitCategory.Cyborg),
      0f,
      MaxPsychoCorruption);
  }

  public float GetCurrentPsychoCorruption(TraitCategory category)
  {
    if (category == TraitCategory.Common)
      return 0f;

    float total = 0f;

    foreach (TraitData traitData in availableTraits)
    {
      if (traitData == null || string.IsNullOrWhiteSpace(traitData.traitId))
        continue;

      if (traitData.category != category)
        continue;

      int level = GetTraitLevel(traitData.traitId);
      total += level * GetPsychoCorruptionIncrease(traitData.traitId);
    }

    return Mathf.Clamp(total, 0f, MaxPsychoCorruption);
  }

  public float GetPreviewPsychoCorruption(string traitId)
  {
    TraitData traitData = GetTraitData(traitId);
    if (traitData == null)
      return GetCurrentPsychoCorruption();

    float current = GetCurrentPsychoCorruption(traitData.category);
    float increase = CanUpgrade(traitId) ? GetPsychoCorruptionIncrease(traitId) : 0f;
    return Mathf.Clamp(current + increase, 0f, MaxPsychoCorruption);
  }

  /// <summary>
  /// 해당 특성을 업그레이드(혹은 해금) 할 수 있는지 검사합니다.
  /// </summary>
  public bool CanUpgrade(string traitId)
  {
    TraitData td = GetTraitData(traitId);
    if (td == null) return false;

    int currentLevel = GetTraitLevel(traitId);

    // 이미 만렙이면 불가
    if (currentLevel >= td.maxLevel) return false;

    // 사이코 잠식도 상한(100) 초과 시 업그레이드 불가
    float increase = GetPsychoCorruptionIncrease(traitId);
    if (increase > 0f)
    {
      float currentPsycho = GetCurrentPsychoCorruption(td.category);
      if (currentPsycho + increase > MaxPsychoCorruption)
        return false;
    }

    // 선행 조건 검사
    return CheckPrerequisites(td);
  }

  private bool CheckPrerequisites(TraitData td)
  {
    // 요구 조건 그룹이 없으면 무조건 통과
    if (td.requirementGroups == null || td.requirementGroups.Count == 0)
      return true;

    // DNF (Disjunctive Normal Form) 검사
    // 그룹 중 "하나"라도 통과(AND 전부 만족)하면 전체 조건 만족
    foreach (var group in td.requirementGroups)
    {
      if (group.conditions == null || group.conditions.Count == 0)
        continue;

      bool groupPassed = true;
      foreach (var cond in group.conditions)
      {
        if (GetTraitLevel(cond.traitId) < cond.requiredLevel)
        {
          groupPassed = false;
          break; // 하나라도 안 되면 이 그룹은 실패
        }
      }

      if (groupPassed)
      {
        return true; // 그룹 하나가 전부 통과됨 -> OR 조건 만족
      }
    }

    return false; // 모든 그룹 실패 시 해금 불가
  }

  public int GetTotalTraitLevels()
  {
    int total = 0;
    foreach (var level in traitLevels.Values)
    {
      total += level;
    }
    return total;
  }

  public int GetUpgradeCost()
  {
    return baseTraitCost + (GetTotalTraitLevels() * traitCostIncrement);
  }

  /// <summary>
  /// 특성의 레벨을 1 올립니다. UI 클릭 이벤트 시 호출하세요.
  /// 성공 시 true 반환.
  /// </summary>
  public bool TryUpgradeTrait(string traitId)
  {
    if (!CanUpgrade(traitId))
      return false;

    int cost = GetUpgradeCost();
    if (GameManager.Instance != null)
    {
      if (!GameManager.Instance.TrySpendTotalGold(cost))
      {
        Debug.Log("[TraitManager] 골드가 부족하여 업그레이드할 수 없습니다.");
        return false;
      }
    }
    else
    {
      int currentGold = ES3.Load<int>("TotalGold", 0);
      if (currentGold < cost)
      {
        Debug.Log("[TraitManager] 골드가 부족하여 업그레이드할 수 없습니다.");
        return false;
      }
      ES3.Save("TotalGold", currentGold - cost);
      
      var binders = FindObjectsOfType<TotalGoldTextBinder>();
      foreach (var binder in binders) { binder.RefreshNow(); }
    }

    int currentLevel = GetTraitLevel(traitId);
    traitLevels[traitId] = currentLevel + 1;

    SaveTraits();

    OnPsychoCorruptionChanged?.Invoke(GetCurrentPsychoCorruption(), MaxPsychoCorruption);
    OnTraitUpgraded?.Invoke(traitId, traitLevels[traitId]);
    Debug.Log($"[TraitManager] '{traitId}' 특성을 Lv.{traitLevels[traitId]} 로 업그레이드 했습니다.");

    return true;
  }

  public bool CanDowngrade(string traitId)
  {
    TraitData traitData = GetTraitData(traitId);
    if (traitData == null)
      return false;

    int currentLevel = GetTraitLevel(traitId);
    if (currentLevel <= 0)
      return false;

    int nextLevel = currentLevel - 1;

    // 우선순위 개념: 이미 찍혀있는 후행 특성이 요구 조건을 잃는 경우 다운그레이드 금지
    foreach (TraitData other in availableTraits)
    {
      if (other == null || string.IsNullOrWhiteSpace(other.traitId))
        continue;

      int otherLevel = GetTraitLevel(other.traitId);
      if (otherLevel <= 0)
        continue;

      if (!CheckPrerequisitesWithOverride(other, traitId, nextLevel))
        return false;
    }

    return true;
  }

  public bool TryDowngradeTrait(string traitId)
  {
    if (!CanDowngrade(traitId))
      return false;

    int currentTotal = GetTotalTraitLevels();
    int refundAmount = baseTraitCost + ((currentTotal - 1) * traitCostIncrement);

    int currentLevel = GetTraitLevel(traitId);
    int nextLevel = Mathf.Max(0, currentLevel - 1);

    if (nextLevel <= 0)
      traitLevels.Remove(traitId);
    else
      traitLevels[traitId] = nextLevel;

    if (refundAmount > 0)
    {
      if (GameManager.Instance != null)
      {
        GameManager.Instance.AddTotalGold(refundAmount);
      }
      else
      {
        int currentGold = ES3.Load<int>("TotalGold", 0);
        ES3.Save("TotalGold", currentGold + refundAmount);
        
        var binders = FindObjectsOfType<TotalGoldTextBinder>();
        foreach (var binder in binders) { binder.RefreshNow(); }
      }
    }

    SaveTraits();

    OnPsychoCorruptionChanged?.Invoke(GetCurrentPsychoCorruption(), MaxPsychoCorruption);
    OnTraitUpgraded?.Invoke(traitId, nextLevel);
    return true;
  }

  public void ResetTraitsByCategory(TraitCategory category)
  {
    int levelsRemoved = 0;

    foreach (TraitData traitData in availableTraits)
    {
      if (traitData == null || traitData.category != category || string.IsNullOrWhiteSpace(traitData.traitId))
        continue;

      if (traitLevels.TryGetValue(traitData.traitId, out int level))
      {
        levelsRemoved += level;
        traitLevels.Remove(traitData.traitId);
      }
    }

    if (levelsRemoved == 0)
      return;

    int currentTotal = GetTotalTraitLevels() + levelsRemoved;
    int refund = 0;
    for (int i = 0; i < levelsRemoved; i++)
    {
      refund += baseTraitCost + ((currentTotal - 1 - i) * traitCostIncrement);
    }

    if (refund > 0)
    {
      if (GameManager.Instance != null)
      {
        GameManager.Instance.AddTotalGold(refund);
      }
      else
      {
        int currentGold = ES3.Load<int>("TotalGold", 0);
        ES3.Save("TotalGold", currentGold + refund);
        
        var binders = FindObjectsOfType<TotalGoldTextBinder>();
        foreach (var binder in binders) { binder.RefreshNow(); }
      }
    }

    SaveTraits();
    OnTraitsLoaded?.Invoke();
    OnPsychoCorruptionChanged?.Invoke(GetCurrentPsychoCorruption(), MaxPsychoCorruption);
  }

  private bool CheckPrerequisitesWithOverride(TraitData td, string overrideTraitId, int overrideLevel)
  {
    if (td.requirementGroups == null || td.requirementGroups.Count == 0)
      return true;

    foreach (TraitRequirementGroup group in td.requirementGroups)
    {
      if (group.conditions == null || group.conditions.Count == 0)
        continue;

      bool groupPassed = true;

      foreach (TraitPrerequisite cond in group.conditions)
      {
        int level = cond.traitId == overrideTraitId ? overrideLevel : GetTraitLevel(cond.traitId);

        if (level < cond.requiredLevel)
        {
          groupPassed = false;
          break;
        }
      }

      if (groupPassed)
        return true;
    }

    return false;
  }

  /// <summary>
  /// 현재 모든 특성 진행도를 ES3로 로드합니다.
  /// </summary>
  public void LoadTraits()
  {
    if (ES3.KeyExists(KEY_TRAIT_LEVELS))
    {
      traitLevels = ES3.Load<Dictionary<string, int>>(KEY_TRAIT_LEVELS);
    }
    else
    {
      traitLevels = new Dictionary<string, int>();
    }

    OnTraitsLoaded?.Invoke();
    OnPsychoCorruptionChanged?.Invoke(GetCurrentPsychoCorruption(), MaxPsychoCorruption);
  }

  /// <summary>
  /// 현재 변경된 특성 상태를 저장합니다.
  /// </summary>
  public void SaveTraits()
  {
    ES3.Save(KEY_TRAIT_LEVELS, traitLevels);
  }

  // 에디터에서 인스펙터에 기본 특성들이 보이도록 초기값을 채웁니다.
  private void OnValidate()
  {
    if (availableTraits == null || availableTraits.Count == 0)
    {
      PopulateDefaultTraits();
    }
    // OnValidate에서는 자동 비용 적용을 하지 않습니다.
    // "Tools/NeoSurvive/Traits/Reapply Psycho Costs" 메뉴를 사용해 수동으로 갱신하세요.
  }

#if UNITY_EDITOR
  [UnityEditor.MenuItem("Tools/NeoSurvive/Traits/Reapply Psycho Costs")]
  public static void ReapplyPsychoCostsManual()
  {
    TraitManager instance = FindObjectOfType<TraitManager>();
    if (instance == null)
    {
      Debug.LogError("[TraitManager] 씬에서 TraitManager를 찾을 수 없습니다.");
      return;
    }

    instance.ApplyTraitPsychoCorruptionCosts();
    UnityEditor.EditorUtility.SetDirty(instance);
    Debug.Log("[TraitManager] 사이코 비용이 재적용되었습니다.");
  }
#endif

  private void ApplyTraitPsychoCorruptionCosts()
  {
    if (availableTraits == null)
      return;

    foreach (TraitData traitData in availableTraits)
    {
      if (traitData == null || string.IsNullOrWhiteSpace(traitData.traitId))
        continue;

      if (TraitPsychoCostPerLevel.TryGetValue(traitData.traitId, out float costPerLevel))
      {
        traitData.psychoCorruptionIncrease = Mathf.Max(0f, costPerLevel);
      }
      else if (traitData.psychoCorruptionIncrease <= 0f)
      {
        traitData.psychoCorruptionIncrease = TraitData.DefaultPsychoCorruptionIncrease;
      }

      if (traitData.category == TraitCategory.Common)
      {
        traitData.psychoCorruptionIncrease = 0f;
      }
    }
  }

  private void PopulateDefaultTraits()
  {
    availableTraits = new List<TraitData>()
    {
      new TraitData {
        traitId = "plasma_strand",
        displayName = "플라즈마 강선 (3)",
        description = "공격 속도 증가: 무기, 설치물, 드론, 투사체에 +5%.",
        effectDescription = "",
        category = TraitCategory.Hacker,
        maxLevel = 3
      },

      new TraitData {
        traitId = "compute_optimization",
        displayName = "연산 최적화 (2)",
        description = "쿨타임 감소: 설치물 및 장판/펄스 주기 가속 +5%.",
        effectDescription = "",
        category = TraitCategory.Hacker,
        maxLevel = 2
      },

      new TraitData {
        traitId = "energy_overload",
        displayName = "에너지 과부하 (5)",
        description = "투사체 및 스킬 피해량 증가 +5%.",
        effectDescription = "",
        category = TraitCategory.Hacker,
        maxLevel = 5
      },

      new TraitData {
        traitId = "facility_augmentation",
        displayName = "설비 증강 (3)",
        description = "설치물 및 장판 유지 시간 증가 +10%.",
        effectDescription = "",
        category = TraitCategory.Hacker,
        maxLevel = 3,
        requirementGroups = new List<TraitRequirementGroup>
        {
          new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "plasma_strand", requiredLevel = 3 } } },
          new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "compute_optimization", requiredLevel = 2 } } }
        }
      },

      new TraitData {
        traitId = "radar",
        displayName = "레이더 (3)",
        description = "투사체 사거리 및 스킬 범위(장판/펄스/실드) 증가 +10%.",
        effectDescription = "",
        category = TraitCategory.Hacker,
        maxLevel = 3,
        requirementGroups = new List<TraitRequirementGroup>
        {
          new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "plasma_strand", requiredLevel = 3 } } },
          new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "energy_overload", requiredLevel = 5 } } }
        }
      },

      new TraitData {
        traitId = "exoskeleton",
        displayName = "외골격 (3)",
        description = "이동 속도 증가 +10%.",
        effectDescription = "",
        category = TraitCategory.Hacker,
        maxLevel = 3,
        requirementGroups = new List<TraitRequirementGroup>
        {
          new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "plasma_strand", requiredLevel = 3 } } },
          new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "compute_optimization", requiredLevel = 2 } } },
          new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "energy_overload", requiredLevel = 5 } } }
        }
      },

      new TraitData {
        traitId = "compile_node_expansion",
        displayName = "컴파일 노드 확장 (3)",
        description = "장판 및 범위형 기술 크기 증가 +10%.",
        effectDescription = "",
        category = TraitCategory.Hacker,
        maxLevel = 3,
        requirementGroups = new List<TraitRequirementGroup>
        {
          new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "compute_optimization", requiredLevel = 2 } } }
        }
      },

      new TraitData {
        traitId = "ricochet",
        displayName = "도탄 (1)",
        description = "투사체가 적에게 적중 시 주변의 다른 적에게 100% 효율로 1회 튕김.",
        effectDescription = "",
        category = TraitCategory.Hacker,
        maxLevel = 1,
        requirementGroups = new List<TraitRequirementGroup>
        {
          new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "radar", requiredLevel = 3 }, new TraitPrerequisite{ traitId = "compile_node_expansion", requiredLevel = 3 } } }
        }
      },

      new TraitData {
        traitId = "inertia_charge",
        displayName = "관성 에너지 충전 (1)",
        description = "이동 속도 1%당 무기 공격 속도 및 스킬 발사 속도 1% 증가.",
        effectDescription = "",
        category = TraitCategory.Hacker,
        maxLevel = 1,
        requirementGroups = new List<TraitRequirementGroup>
        {
          new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "exoskeleton", requiredLevel = 3 }, new TraitPrerequisite{ traitId = "radar", requiredLevel = 3 } } }
        }
      },

      new TraitData {
        traitId = "virus_development",
        displayName = "바이러스 개발 (1)",
        description = "상태 이상(혼란, 기절, 감속, 데이터 감옥)에 걸린 대상 공격 시 최종 피해 +35%.",
        effectDescription = "",
        category = TraitCategory.Hacker,
        maxLevel = 1,
        requirementGroups = new List<TraitRequirementGroup>
        {
          new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "energy_overload", requiredLevel = 5 } } }
        }
      },

      new TraitData {
        traitId = "bandwidth_expansion",
        displayName = "대역폭 확장 (1)",
        description = "로봇 및 소환수 강화: 드론 최대 소환량 +2. 포탑 및 디코이는 생성 시 70% 성능의 분신 2개 추가 분열.",
        effectDescription = "",
        category = TraitCategory.Hacker,
        maxLevel = 1,
        requirementGroups = new List<TraitRequirementGroup>
        {
          new TraitRequirementGroup { conditions = new List<TraitPrerequisite>{ new TraitPrerequisite{ traitId = "facility_augmentation", requiredLevel = 3 } } }
        }
      }
    };
  }

  /// <summary>
  /// 편의 기능: 모든 특성 초기화 (개발/테스트 용)
  /// </summary>
  [ContextMenu("Reset All Traits")]
  public void ResetAllTraits()
  {
    int currentTotal = GetTotalTraitLevels();
    int refund = 0;
    for (int i = 0; i < currentTotal; i++)
    {
      refund += baseTraitCost + ((currentTotal - 1 - i) * traitCostIncrement);
    }
    
    if (GameManager.Instance != null && refund > 0)
    {
      GameManager.Instance.AddTotalGold(refund);
    }

    traitLevels.Clear();
    SaveTraits();
    OnPsychoCorruptionChanged?.Invoke(GetCurrentPsychoCorruption(), MaxPsychoCorruption);
    Debug.Log("[TraitManager] 모든 특성이 초기화되었습니다.");
  }
}