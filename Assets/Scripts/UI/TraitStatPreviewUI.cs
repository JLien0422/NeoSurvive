using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TraitStatPreviewUI : MonoBehaviour
{
  [Serializable]
  public class StatRowBinding
  {
    public GameObject root;
    public TextMeshProUGUI statNameText;
    public TextMeshProUGUI currentValueText;
    public TextMeshProUGUI nextValueText;
    public TextMeshProUGUI deltaText;
    public TextMeshProUGUI arrowText;
    public Image arrowImage;
  }

  private struct PreviewChange
  {
    public string statName;
    public string currentText;
    public string nextText;
    public string deltaText;
    public float delta;

    public PreviewChange(string statName, string currentText, string nextText, string deltaText, float delta)
    {
      this.statName = statName;
      this.currentText = currentText;
      this.nextText = nextText;
      this.deltaText = deltaText;
      this.delta = delta;
    }
  }

  private enum ChangeFormat
  {
    Percent,
    Count,
    Text
  }

  private struct TraitEffectDefinition
  {
    public string statName;
    public float amountPerLevel;
    public ChangeFormat format;
    public int decimals;
    public string inactiveText;
    public string activeText;
    public string deltaText;

    public TraitEffectDefinition(string statName, float amountPerLevel, ChangeFormat format = ChangeFormat.Percent, int decimals = 0)
    {
      this.statName = statName;
      this.amountPerLevel = amountPerLevel;
      this.format = format;
      this.decimals = decimals;
      inactiveText = string.Empty;
      activeText = string.Empty;
      deltaText = string.Empty;
    }

    public TraitEffectDefinition(string statName, string inactiveText, string activeText, string deltaText)
    {
      this.statName = statName;
      this.amountPerLevel = 1f;
      this.format = ChangeFormat.Text;
      this.decimals = 0;
      this.inactiveText = inactiveText;
      this.activeText = activeText;
      this.deltaText = deltaText;
    }
  }

  [Header("Rows")]
  [Tooltip("고정 행을 직접 연결할 때 사용합니다. 남는 행은 자동으로 숨겨집니다.")]
  [SerializeField] private List<StatRowBinding> fixedRows = new List<StatRowBinding>();
  [Tooltip("동적 생성용 부모입니다. rowTemplate을 쓸 때 연결하세요.")]
  [SerializeField] private Transform rowsRoot;
  [Tooltip("동적 생성용 행 템플릿입니다. fixedRows만 쓸 경우 비워도 됩니다.")]
  [SerializeField] private GameObject rowTemplate;

  [Header("Colors")]
  [SerializeField] private Color increaseColor = new Color(0.25f, 1f, 0.35f, 1f);
  [SerializeField] private Color decreaseColor = new Color(1f, 0.25f, 0.25f, 1f);
  [SerializeField] private Color neutralColor = Color.white;

  [Header("Arrows")]
  [SerializeField] private string increaseArrow = "▲";
  [SerializeField] private string decreaseArrow = "▼";
  [SerializeField] private string neutralArrow = "-";

  private readonly List<StatRowBinding> runtimeRows = new List<StatRowBinding>();

  private void Awake()
  {
    if (rowTemplate != null)
      rowTemplate.SetActive(false);
  }

  private string currentTraitId;

  private void OnEnable()
  {
    if (TraitManager.Instance != null)
    {
      TraitManager.Instance.OnTraitUpgraded += HandleTraitChanged;
      TraitManager.Instance.OnTraitsLoaded += HandleTraitsLoaded;
    }
  }

  private void OnDisable()
  {
    if (TraitManager.Instance != null)
    {
      TraitManager.Instance.OnTraitUpgraded -= HandleTraitChanged;
      TraitManager.Instance.OnTraitsLoaded -= HandleTraitsLoaded;
    }
  }

  private static readonly Dictionary<string, TraitEffectDefinition[]> EffectDefinitions = new Dictionary<string, TraitEffectDefinition[]>
  {
    { "plasma_strand", new[] { Percent("공격 속도", 5f), Percent("설치물 공격 속도", 5f), Percent("드론 공격 속도", 5f), Percent("투사체 공격 속도", 5f) } },
    { "compute_optimization", new[] { Percent("쿨타임 감소", 5f), Percent("설치물 주기 가속", 5f), Percent("장판/펄스 주기 가속", 5f) } },
    { "energy_overload", new[] { Percent("투사체 피해", 5f), Percent("스킬 피해", 5f) } },
    { "facility_augmentation", new[] { Percent("설치물 지속시간", 10f), Percent("장판 지속시간", 10f) } },
    { "radar", new[] { Percent("투사체 사거리", 10f), Percent("스킬 범위", 10f) } },
    { "exoskeleton", new[] { Percent("이동 속도", 10f) } },
    { "compile_node_expansion", new[] { Percent("장판 크기", 10f), Percent("범위형 기술 크기", 10f) } },
    { "ricochet", new[] { Count("도탄 횟수", 1f), Percent("도탄 피해 효율", 100f) } },
    { "inertia_charge", new[] { Text("이동속도 연동 공격속도", "비활성", "활성", "이동속도 1%당 +1%"), Text("이동속도 연동 발사속도", "비활성", "활성", "이동속도 1%당 +1%") } },
    { "virus_development", new[] { Percent("상태이상 대상 최종 피해", 35f) } },
    { "bandwidth_expansion", new[] { Count("드론 최대 소환량", 2f), Count("포탑/디코이 분신", 2f), Percent("분신 성능", 70f) } },
    { "hydraulic_motor_amp", new[] { Percent("무기 기본 공격력", 5f), Percent("물리 피해", 5f), Percent("에너지 피해", 5f) } },
    { "nano_armor", new[] { Percent("받는 피해 감소", 5f) } },
    { "output_optimization", new[] { Percent("공격 범위", 5f), Percent("블랙홀 범위", 5f), Percent("검기/장판/브레스 범위", 5f) } },
    { "overcharged_battery", new[] { Percent("다단 히트 주기 감소", 10f), Percent("지속 피해 틱 속도", 10f) } },
    { "superconductive_circuits", new[] { Percent("상태이상 지속시간", 10f), Percent("상태이상 효율", 10f) } },
    { "reactive_exoskeleton", new[] { Percent("반사 충격파 피해", 10f) } },
    { "inertia_frame", new[] { Percent("넉백 저항", 10f), Percent("블랙홀 흡입력", 10f), Percent("넉백 거리", 10f) } },
    { "critical_breakthrough", new[] { Text("잃은 체력당 피해량", "비활성", "활성", "체력 1% 손실당 +1%"), Text("잃은 체력당 공격 범위", "비활성", "활성", "체력 1% 손실당 +1%") } },
    { "chain_discharge", new[] { Percent("연쇄 방전 확률", 30f), Percent("연쇄 번개 피해 계수", 100f) } },
    { "core_fusion", new[] { Text("대시 시 근거리 무기 발동", "비활성", "활성", "활성화") } }
  };

  private static TraitEffectDefinition Percent(string statName, float amountPerLevel, int decimals = 0)
  {
    return new TraitEffectDefinition(statName, amountPerLevel, ChangeFormat.Percent, decimals);
  }

  private static TraitEffectDefinition Count(string statName, float amountPerLevel)
  {
    return new TraitEffectDefinition(statName, amountPerLevel, ChangeFormat.Count, 0);
  }

  private static TraitEffectDefinition Text(string statName, string inactiveText, string activeText, string deltaText)
  {
    return new TraitEffectDefinition(statName, inactiveText, activeText, deltaText);
  }

  public void Show(string traitId)
  {
    if (string.IsNullOrWhiteSpace(traitId))
    {
      Hide();
      return;
    }

    currentTraitId = traitId;
    List<PreviewChange> changes = BuildPreviewChanges(traitId);
    if (changes.Count == 0)
    {
      Hide();
      return;
    }

    gameObject.SetActive(true);
    EnsureRowCount(changes.Count);

    for (int i = 0; i < runtimeRows.Count; i++)
    {
      bool active = i < changes.Count;
      SetRowActive(runtimeRows[i], active);
      if (active)
        ApplyRow(runtimeRows[i], changes[i]);
    }
  }

  public void Hide()
  {
    for (int i = 0; i < runtimeRows.Count; i++)
    {
      SetRowActive(runtimeRows[i], false);
    }

    gameObject.SetActive(false);
  }

  private void HandleTraitChanged(string traitId, int level)
  {
    if (!string.IsNullOrEmpty(currentTraitId) && gameObject.activeSelf)
      Show(currentTraitId);
  }

  private void HandleTraitsLoaded()
  {
    if (!string.IsNullOrEmpty(currentTraitId) && gameObject.activeSelf)
      Show(currentTraitId);
  }

  private List<PreviewChange> BuildPreviewChanges(string traitId)
  {
    List<PreviewChange> changes = new List<PreviewChange>();
    if (!EffectDefinitions.TryGetValue(traitId, out TraitEffectDefinition[] definitions))
      return changes;

    int currentLevel = TraitManager.Instance != null ? TraitManager.Instance.GetTraitLevel(traitId) : 0;
    int maxLevel = GetMaxLevel(traitId);
    int nextLevel = maxLevel > 0 ? Mathf.Min(currentLevel + 1, maxLevel) : currentLevel + 1;

    for (int i = 0; i < definitions.Length; i++)
    {
      changes.Add(BuildChange(definitions[i], currentLevel, nextLevel));
    }

    return changes;
  }

  private int GetMaxLevel(string traitId)
  {
    if (TraitManager.Instance == null)
      return 0;

    TraitData traitData = TraitManager.Instance.GetTraitData(traitId);
    return traitData != null ? traitData.maxLevel : 0;
  }

  private PreviewChange BuildChange(TraitEffectDefinition definition, int currentLevel, int nextLevel)
  {
    if (definition.format == ChangeFormat.Text)
    {
      bool isActive = currentLevel > 0;
      bool willBeActive = nextLevel > 0;
      float delta = isActive == willBeActive ? 0f : 1f;
      return new PreviewChange(
        definition.statName,
        isActive ? definition.activeText : definition.inactiveText,
        willBeActive ? definition.activeText : definition.inactiveText,
        delta > 0f ? definition.deltaText : "변화 없음",
        delta);
    }

    float currentValue = currentLevel * definition.amountPerLevel;
    float nextValue = nextLevel * definition.amountPerLevel;
    float deltaValue = nextValue - currentValue;
    string currentText = FormatValue(currentValue, definition);
    string nextText = FormatValue(nextValue, definition);
    string deltaText = FormatDelta(deltaValue, definition);

    return new PreviewChange(definition.statName, currentText, nextText, deltaText, deltaValue);
  }

  private string FormatValue(float value, TraitEffectDefinition definition)
  {
    string number = value.ToString($"F{definition.decimals}");
    return definition.format == ChangeFormat.Percent ? $"{number}%" : number;
  }

  private string FormatDelta(float value, TraitEffectDefinition definition)
  {
    string sign = value > 0f ? "+" : string.Empty;
    string number = Mathf.Abs(value) < 0.0001f ? 0f.ToString($"F{definition.decimals}") : value.ToString($"F{definition.decimals}");
    return definition.format == ChangeFormat.Percent ? $"{sign}{number}%" : $"{sign}{number}";
  }

  private void EnsureRowCount(int count)
  {
    if (runtimeRows.Count == 0)
    {
      runtimeRows.AddRange(fixedRows);
    }

    while (runtimeRows.Count < count && rowTemplate != null && rowsRoot != null)
    {
      GameObject rowObject = Instantiate(rowTemplate, rowsRoot);
      rowObject.SetActive(true);
      runtimeRows.Add(CreateBindingFromRow(rowObject));
    }
  }

  private StatRowBinding CreateBindingFromRow(GameObject rowObject)
  {
    TextMeshProUGUI[] texts = rowObject.GetComponentsInChildren<TextMeshProUGUI>(true);
    Image[] images = rowObject.GetComponentsInChildren<Image>(true);

    return new StatRowBinding
    {
      root = rowObject,
      statNameText = texts.Length > 0 ? texts[0] : null,
      currentValueText = texts.Length > 1 ? texts[1] : null,
      nextValueText = texts.Length > 2 ? texts[2] : null,
      deltaText = texts.Length > 3 ? texts[3] : null,
      arrowText = texts.Length > 4 ? texts[4] : null,
      arrowImage = images.Length > 0 ? images[0] : null
    };
  }

  private void ApplyRow(StatRowBinding row, PreviewChange change)
  {
    Color color = GetDeltaColor(change.delta);
    string arrow = GetArrow(change.delta);

    if (row.statNameText != null) row.statNameText.text = change.statName;
    if (row.currentValueText != null) row.currentValueText.text = change.currentText;
    if (row.nextValueText != null)
    {
      row.nextValueText.text = change.nextText;
      row.nextValueText.color = color;
    }

    if (row.deltaText != null)
    {
      row.deltaText.text = change.deltaText;
      row.deltaText.color = color;
    }

    if (row.arrowText != null)
    {
      row.arrowText.text = arrow;
      row.arrowText.color = color;
    }

    if (row.arrowImage != null)
    {
      row.arrowImage.color = color;
      row.arrowImage.rectTransform.localRotation = change.delta < 0f ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity;
    }
  }

  private Color GetDeltaColor(float delta)
  {
    if (delta > 0f) return increaseColor;
    if (delta < 0f) return decreaseColor;
    return neutralColor;
  }

  private string GetArrow(float delta)
  {
    if (delta > 0f) return increaseArrow;
    if (delta < 0f) return decreaseArrow;
    return neutralArrow;
  }

  private void SetRowActive(StatRowBinding row, bool active)
  {
    if (row != null && row.root != null)
      row.root.SetActive(active);
  }
}
