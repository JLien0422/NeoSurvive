using UnityEngine;
using TMPro;
using System.Text;

public class TraitTooltipUI : MonoBehaviour
{
  [Header("UI 연결 (TextMeshPro)")]
  public TextMeshProUGUI nameText;
  public TextMeshProUGUI descriptionText;
  public TextMeshProUGUI effectText;
  public TextMeshProUGUI prerequisitesText;

  [Header("설정")]
  [Tooltip("마우스 포인터 옆에 따라다닐 때의 오프셋")]
  public Vector2 offset = new Vector2(10f, -10f);

  private RectTransform rectTransform;
  private Canvas parentCanvas;
  private string currentTraitId;

  private void Awake()
  {
    rectTransform = GetComponent<RectTransform>();
  }

  private void Start()
  {
    // 최상위 Canvas 찾기 (마우스 위치 계산용)
    parentCanvas = GetComponentInParent<Canvas>();
  }

  private void Update()
  {
    // 툴팁 활성화 중일 때 마우스 포인터 따라가기
    if (gameObject.activeSelf)
    {
      UpdatePosition();
    }
  }

  private void OnEnable()
  {
    if (TraitManager.Instance != null)
    {
      TraitManager.Instance.OnTraitUpgraded += HandleTraitChanged;
      TraitManager.Instance.OnTraitsLoaded += HandleTraitsLoaded;
      TraitManager.Instance.OnPsychoCorruptionChanged += HandlePsychoChanged;
    }
  }

  private void OnDisable()
  {
    if (TraitManager.Instance != null)
    {
      TraitManager.Instance.OnTraitUpgraded -= HandleTraitChanged;
      TraitManager.Instance.OnTraitsLoaded -= HandleTraitsLoaded;
      TraitManager.Instance.OnPsychoCorruptionChanged -= HandlePsychoChanged;
    }
  }

  /// <summary>
  /// 지정된 특성 ID를 기반으로 툴팁 내용을 세팅하고 보여줍니다.
  /// </summary>
  public void Show(string traitId)
  {
    if (TraitManager.Instance == null) return;

    TraitData td = TraitManager.Instance.GetTraitData(traitId);
    if (td == null) return;

    currentTraitId = traitId;
    int currentLevel = TraitManager.Instance.GetTraitLevel(traitId);

    // 텍스트 세팅
    if (nameText != null)
      nameText.text = $"{td.displayName} (Lv.{currentLevel}/{td.maxLevel})";

    if (descriptionText != null)
      descriptionText.text = string.IsNullOrEmpty(td.description) ? "설명 없음" : td.description;

    if (effectText != null)
      effectText.text = BuildEffectText(td, currentLevel);

    if (prerequisitesText != null)
      prerequisitesText.text = BuildPrerequisitesText(td);

    UpdatePosition();
    gameObject.SetActive(true);
  }

  public void Hide()
  {
    currentTraitId = null;
    gameObject.SetActive(false);
  }

  private void HandleTraitChanged(string traitId, int level)
  {
    // If tooltip is visible for this trait, refresh contents
    if (!string.IsNullOrEmpty(currentTraitId) && gameObject.activeSelf)
    {
      Show(currentTraitId);
    }
  }

  private void HandleTraitsLoaded()
  {
    if (!string.IsNullOrEmpty(currentTraitId) && gameObject.activeSelf)
      Show(currentTraitId);
  }

  private void HandlePsychoChanged(float current, float max)
  {
    if (!string.IsNullOrEmpty(currentTraitId) && gameObject.activeSelf)
      Show(currentTraitId);
  }

  private void UpdatePosition()
  {
    if (parentCanvas == null) return;

    Vector2 localPoint;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        parentCanvas.transform as RectTransform,
        Input.mousePosition,
        parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
        out localPoint);

    // 캔버스 내에서의 위치 적용 및 오프셋 추가
    Vector2 targetPos = localPoint + offset;

    // 툴팁이 화면(Parent Canvas) 밖으로 나가지 않도록 Clamp
    RectTransform parentRect = parentCanvas.transform as RectTransform;

    // 툴팁 앵커(Pivot) 설정에 따라 계산 보정이 필요할 수 있으나, 일반적으로 크기 절반을 여백으로 잡습니다.
    float minX = parentRect.rect.xMin + (rectTransform.rect.width * rectTransform.pivot.x);
    float maxX = parentRect.rect.xMax - (rectTransform.rect.width * (1f - rectTransform.pivot.x));

    float minY = parentRect.rect.yMin + (rectTransform.rect.height * rectTransform.pivot.y);
    float maxY = parentRect.rect.yMax - (rectTransform.rect.height * (1f - rectTransform.pivot.y));

    targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
    targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

    rectTransform.localPosition = targetPos;
  }

  /// <summary>
  /// 사전 조건들을 알기 쉬운 텍스트로 변환합니다.
  /// </summary>
  private string BuildPrerequisitesText(TraitData td)
  {
    if (td.requirementGroups == null || td.requirementGroups.Count == 0)
      return "사전 조건: 없음";

    StringBuilder sb = new StringBuilder();
    sb.AppendLine("사전 조건:");

    bool isFirstGroup = true;
    foreach (var group in td.requirementGroups)
    {
      if (group.conditions == null || group.conditions.Count == 0) continue;

      if (!isFirstGroup)
      {
        sb.AppendLine("<color=#A0A0A0>-- 또는 (OR) --</color>");
      }

      foreach (var cond in group.conditions)
      {
        string reqName = cond.traitId;
        TraitData reqTd = TraitManager.Instance.GetTraitData(cond.traitId);
        if (reqTd != null) reqName = reqTd.displayName;

        int currentLv = TraitManager.Instance.GetTraitLevel(cond.traitId);
        bool isMet = currentLv >= cond.requiredLevel;
        string colorTag = isMet ? "<color=#00FF00>" : "<color=#FF0000>";

        sb.AppendLine($"- {colorTag}{reqName} Lv.{cond.requiredLevel}</color> (현재 Lv.{currentLv})");
      }
      isFirstGroup = false;
    }

    return sb.ToString().TrimEnd();
  }

  private string BuildEffectText(TraitData td, int currentLevel)
  {
    StringBuilder sb = new StringBuilder();

    if (TraitManager.Instance == null)
      return sb.ToString().TrimEnd();

    float current = TraitManager.Instance.GetCurrentPsychoCorruption();

    if (currentLevel < td.maxLevel)
    {
      float psychoCost = TraitManager.Instance.GetPsychoCorruptionIncrease(td.traitId);
      int goldCost = TraitManager.Instance.GetUpgradeCost();
      bool hasGold = GameManager.Instance != null && GameManager.Instance.TotalGold >= goldCost;
      string goldColor = hasGold ? "#FFD700" : "#FF4D4D";

      float next = Mathf.Clamp(current + psychoCost, 0f, TraitManager.MaxPsychoCorruption);

      sb.AppendLine();
      sb.AppendLine($"사이코 잠식도: {FormatPsychoValue(current)} -> <color=#55FF73>{FormatPsychoValue(next)}</color>");
      sb.AppendLine($"사이코 잠식도 비용: <color=#FF4D4D>{FormatPsychoValue(psychoCost)}</color>");
      sb.AppendLine($"골드 비용: <color={goldColor}>{goldCost}</color> 골드");
    }
    else
    {
      sb.AppendLine();
      sb.AppendLine($"사이코 잠식도: {FormatPsychoValue(current)}/100 (최대 레벨)");
    }

    if (currentLevel > 0)
    {
      int currentTotal = TraitManager.Instance.GetTotalTraitLevels();
      int refundAmount = TraitManager.Instance.baseTraitCost + ((currentTotal - 1) * TraitManager.Instance.traitCostIncrement);
      sb.AppendLine($"다운그레이드 시 환급: <color=#55FF73>{refundAmount}</color> 골드");
    }

    return sb.ToString().TrimEnd();
  }

  private static string FormatPsychoValue(float value)
  {
    return Mathf.Approximately(value, Mathf.Round(value)) ? Mathf.RoundToInt(value).ToString() : value.ToString("F1");
  }
}
