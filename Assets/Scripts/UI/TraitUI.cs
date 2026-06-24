using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// LobbyManager의 제어(Show/Hide) 아래 동작하는 트레잇 전용 UI 스크립트입니다.
/// 해커, 사이보그, 공통 등 하위 탭 전환 및 업그레이드 클릭 이벤트를 담당합니다.
/// </summary>
public class TraitUI : MonoBehaviour
{
  [Serializable]
  public class TraitButtonBinding
  {
    public string traitId;
    public Button button;
    public Image iconImage;
  }

  [Header("하위 탭 컨테이너 (Sub Panels)")]
  [Tooltip("해커 특성들이 들어갈 하위 UI 패널")]
  public GameObject hackerSubPanel;
  [Tooltip("사이보그 특성들이 들어갈 하위 UI 패널")]
  public GameObject cyborgSubPanel;
  [Tooltip("공통 특성들이 들어갈 하위 UI 패널")]
  public GameObject commonSubPanel;

  [Header("탭 배경화면 (Background Sprites)")]
  [Tooltip("배경화면을 렌더링할 대상 Image 컴포넌트")]
  public Image targetBackgroundImage;
  [Tooltip("해커 탭 배경 스프라이트")]
  public Sprite hackerBackgroundSprite;
  [Tooltip("사이보그 탭 배경 스프라이트")]
  public Sprite cyborgBackgroundSprite;
  [Tooltip("공통 탭 배경 스프라이트")]
  public Sprite commonBackgroundSprite;

  [Header("버튼 참조 (옵션)")]
  [Tooltip("각 탭을 활성화 시키는 버튼 애니메이션 처리 등을 위해 참조")]
  public Button hackerTabBtn;
  public Button cyborgTabBtn;
  public Button commonTabBtn;

  [Header("툴팁 UI 참조")]
  [Tooltip("마우스를 올렸을 때 나타날 툴팁 창 (Prefab 인스턴스)")]
  public TraitTooltipUI tooltipUI;
  [Tooltip("마우스를 올렸을 때 우측 스탯창에 다음 1레벨 상승분을 표시할 UI")]
  public TraitStatPreviewUI statPreviewUI;
  [Tooltip("사이코 잠식도 표시용 슬라이더 (Inspector에 연결하세요)")]
  public Slider psychoSlider;
  [Tooltip("사이코 수치 텍스트(선택)")]
  public TMP_Text psychoValueText;

  [Header("특성 버튼 상태 표시")]
  [Tooltip("비워두면 하위 Button 이름을 traitId 규칙으로 변환해 자동 수집합니다. 예외가 있으면 여기에 직접 연결하세요.")]
  public List<TraitButtonBinding> traitButtons = new List<TraitButtonBinding>();
  [Tooltip("특성이 활성화되었을 때 아이콘 색상")]
  public Color activeIconColor = Color.white;
  [Tooltip("특성이 아직 활성화되지 않았을 때 아이콘 색상")]
  public Color inactiveIconColor = new Color(0.28f, 0.28f, 0.28f, 1f);
  [Tooltip("선행 조건을 만족하지 못해 찍을 수 없을 때 아이콘 색상")]
  public Color lockedIconColor = new Color(0.16f, 0.16f, 0.16f, 1f);

  private TraitCategory currentCategory = TraitCategory.Hacker;
  private readonly Dictionary<Button, ColorBlock> originalButtonColors = new Dictionary<Button, ColorBlock>();

  private static readonly Dictionary<string, string> TraitNameAliases = new Dictionary<string, string>
  {
    { "hydraulic_motor_amplification", "hydraulic_motor_amp" }
  };

  private void Awake()
  {
    CachePreviewUIs();
    CacheTraitButtons();
  }

  private void Start()
  {
    RefreshUI();
  }

  private void OnEnable()
  {
    // Trait 탭이 LobbyManager에 의해 열릴 때, 기본 상태를 세팅
    RefreshUI();
    SwitchCategory(TraitCategory.Hacker);

    if (TraitManager.Instance != null)
    {
      TraitManager.Instance.OnTraitUpgraded += HandleTraitUpgraded;
      TraitManager.Instance.OnTraitsLoaded += RefreshUI;
    }
    GameManager.OnTotalGoldChanged += HandleGoldChanged;
  }

  private void OnDisable()
  {
    // 창이 닫힐 때 툴팁도 강제로 숨깁니다.
    HideTooltip();

    if (TraitManager.Instance != null)
    {
      TraitManager.Instance.OnTraitUpgraded -= HandleTraitUpgraded;
      TraitManager.Instance.OnTraitsLoaded -= RefreshUI;
    }
    GameManager.OnTotalGoldChanged -= HandleGoldChanged;
  }

  private void Update()
  {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    if (Input.GetKeyDown(KeyCode.P))
    {
      if (GameManager.Instance != null)
      {
        GameManager.Instance.AddTotalGold(10000);
        Debug.Log("[TraitUI] 디버그: 특성창에서 P키를 눌러 10000 골드를 획득했습니다.");
      }
      else
      {
        // GameManager가 씬에 없는 경우 (로비 씬 등)
        int totalGold = ES3.Load<int>("TotalGold", 0);
        totalGold += 10000;
        ES3.Save("TotalGold", totalGold);
        Debug.Log($"[TraitUI] 디버그: GameManager가 없어서 ES3(SaveData)에 직접 10000 골드를 추가했습니다. 현재 총 골드: {totalGold}");
        
        RefreshUI(); // 버튼 상태 갱신
        
        // TotalGoldTextBinder 강제 갱신
        var binders = FindObjectsOfType<TotalGoldTextBinder>();
        foreach (var binder in binders)
        {
          binder.RefreshNow();
        }
      }
    }
#endif
  }

  // ============================================
  // 툴팁 연동 로직
  // ============================================

  /// <summary>
  /// 특성 버튼의 UIHoverEventListener -> OnHoverEnter 이벤트에 연결하세요.
  /// </summary>
  public void ShowTooltip(string traitId)
  {
    if (tooltipUI != null)
    {
      tooltipUI.Show(traitId);
    }

    if (statPreviewUI != null)
    {
      statPreviewUI.Show(traitId);
    }

    UpdatePsychoSlider();
  }

  /// <summary>
  /// 특성 버튼의 UIHoverEventListener -> OnHoverExit 이벤트에 연결하세요.
  /// </summary>
  public void HideTooltip()
  {
    if (tooltipUI != null)
    {
      tooltipUI.Hide();
    }

    if (statPreviewUI != null)
    {
      statPreviewUI.Hide();
    }

    UpdatePsychoSlider();
  }

  // ============================================
  // 하위 탭(Category) 전환 로직
  // ============================================

  /// <summary>
  /// 카테고리 탭 버튼들의 onClick 에 연결하세요. (예: 매개변수로 카테고리 Enum 값을 넘기거나 전용 함수 사용)
  /// </summary>
  public void SwitchCategory(TraitCategory newCategory)
  {
    currentCategory = newCategory;

    // 탭 전환 사운드 적용
    if (LobbySoundManager.Instance != null)
      LobbySoundManager.Instance.PlayTabSwitch();

    UpdateSubPanels();
    UpdatePsychoSlider();
  }

  // 유니티 버튼 이벤트 인스펙터 연결용 래퍼
  public void OnClickHackerTab() => SwitchCategory(TraitCategory.Hacker);
  public void OnClickCyborgTab() => SwitchCategory(TraitCategory.Cyborg);
  public void OnClickCommonTab() => SwitchCategory(TraitCategory.Common);

  private void UpdateSubPanels()
  {
    if (hackerSubPanel != null) hackerSubPanel.SetActive(currentCategory == TraitCategory.Hacker);
    if (cyborgSubPanel != null) cyborgSubPanel.SetActive(currentCategory == TraitCategory.Cyborg);
    if (commonSubPanel != null) commonSubPanel.SetActive(currentCategory == TraitCategory.Common);

    // 배경 스프라이트 교체
    if (targetBackgroundImage != null)
    {
      if (currentCategory == TraitCategory.Hacker && hackerBackgroundSprite != null)
        targetBackgroundImage.sprite = hackerBackgroundSprite;
      else if (currentCategory == TraitCategory.Cyborg && cyborgBackgroundSprite != null)
        targetBackgroundImage.sprite = cyborgBackgroundSprite;
      else if (currentCategory == TraitCategory.Common && commonBackgroundSprite != null)
        targetBackgroundImage.sprite = commonBackgroundSprite;
    }

    // 버튼들 시각적 상태 업데이트 (Interactable, Color 등)는 여기서 구현 가능합니다.
  }

  // ============================================
  // 특성 업그레이드 액션 연동
  // ============================================

  public void OnClickUpgradeTrait(string traitId)
  {
    if (TraitManager.Instance == null)
    {
      Debug.LogError("[TraitUI] TraitManager 인스턴스를 찾을 수 없습니다.");
      return;
    }

    bool success = TraitManager.Instance.TryUpgradeTrait(traitId);

    if (success)
    {
      if (LobbySoundManager.Instance != null) LobbySoundManager.Instance.PlayUpgradeBuy();
      // 전체 UI 최신화 (레벨 텍스트, 자물쇠 상태 등 업뎃용)
      RefreshUI();
    }
    else
    {
      if (LobbySoundManager.Instance != null) LobbySoundManager.Instance.PlayError();
      // 실패한 경우 (선행조건 부족, 만렙 등) 안내 메시지나 이펙트 필요 시 구현
      Debug.Log($"[TraitUI] 업그레이드 실패. (MaxLv 이거나 조건 미충족) : {traitId}");
    }
  }

  public void OnRightClickDowngradeTrait(string traitId)
  {
    if (TraitManager.Instance == null)
    {
      Debug.LogError("[TraitUI] TraitManager 인스턴스를 찾을 수 없습니다.");
      return;
    }

    bool success = TraitManager.Instance.TryDowngradeTrait(traitId);

    if (success)
    {
      if (LobbySoundManager.Instance != null) LobbySoundManager.Instance.PlayTraitDownGrade();
      RefreshUI();
    }
    else
    {
      if (LobbySoundManager.Instance != null) LobbySoundManager.Instance.PlayError();
      Debug.Log($"[TraitUI] 다운그레이드 실패. 후행 특성 우선순위 조건 확인 필요: {traitId}");
    }
  }

  public void OnClickResetHackerTraits()
  {
    ResetTraitsByCategory(TraitCategory.Hacker);
  }

  public void OnClickResetCyborgTraits()
  {
    ResetTraitsByCategory(TraitCategory.Cyborg);
  }

  public void OnClickResetCommonTraits()
  {
    ResetTraitsByCategory(TraitCategory.Common);
  }

  private void ResetTraitsByCategory(TraitCategory category)
  {
    if (TraitManager.Instance == null)
      return;

    TraitManager.Instance.ResetTraitsByCategory(category);
    RefreshUI();
  }

  /// <summary>
  /// 특성 상태가 변했을 때 UI 요소들(레벨 텍스트 등)의 갱신을 원한다면 
  /// 이 함수에 자식 UI 스크립트들을 업데이트하는 처리를 작성하세요.
  /// </summary>
  public void RefreshUI()
  {
    if (TraitManager.Instance == null)
      return;

    CacheTraitButtons();

    foreach (TraitButtonBinding binding in traitButtons)
    {
      if (binding == null || string.IsNullOrWhiteSpace(binding.traitId))
        continue;

      TraitData traitData = TraitManager.Instance.GetTraitData(binding.traitId);
      if (traitData == null)
        continue;

      int currentLevel = TraitManager.Instance.GetTraitLevel(binding.traitId);
      bool isActive = currentLevel > 0;
      bool canUpgrade = TraitManager.Instance.CanUpgrade(binding.traitId);
      bool isLocked = !isActive && !canUpgrade;

      if (binding.iconImage != null)
      {
        binding.iconImage.color = isActive ? activeIconColor : isLocked ? lockedIconColor : inactiveIconColor;
      }

      int currentGold = GameManager.Instance != null ? GameManager.Instance.TotalGold : ES3.Load<int>("TotalGold", 0);
      bool hasEnoughGold = currentGold >= TraitManager.Instance.GetUpgradeCost();

      if (binding.button != null)
      {
        binding.button.interactable = currentLevel < traitData.maxLevel && canUpgrade && hasEnoughGold;
        ApplyButtonColors(binding.button);
      }
    }

    if (psychoSlider != null)
      UpdatePsychoSlider();
  }

  private void HandleTraitUpgraded(string traitId, int level)
  {
    RefreshUI();
  }

  private void HandleGoldChanged(int totalGold)
  {
    RefreshUI();
  }

  private void CacheTraitButtons()
  {
    if (TraitManager.Instance == null)
      return;

    if (traitButtons == null)
      traitButtons = new List<TraitButtonBinding>();

    Button[] childButtons = GetComponentsInChildren<Button>(true);
    foreach (Button childButton in childButtons)
    {
      if (childButton == hackerTabBtn || childButton == cyborgTabBtn || childButton == commonTabBtn)
        continue;

      TraitButtonBinding binding = traitButtons.Find(x => x != null && x.button == childButton);
      string resolvedTraitId = ResolveTraitId(childButton.gameObject.name);
      string traitId;

      // Prefer an existing valid binding.traitId if it matches a known TraitData; otherwise fall back to the resolved name.
      if (binding != null && !string.IsNullOrWhiteSpace(binding.traitId) && TraitManager.Instance.GetTraitData(binding.traitId) != null)
      {
        traitId = binding.traitId;
      }
      else
      {
        traitId = resolvedTraitId;
      }

      if (string.IsNullOrEmpty(traitId))
        continue;

      if (binding == null)
      {
        binding = new TraitButtonBinding
        {
          traitId = traitId,
          button = childButton,
          iconImage = childButton.targetGraphic as Image
        };
        traitButtons.Add(binding);
      }
      else
      {
        binding.traitId = string.IsNullOrWhiteSpace(binding.traitId) ? traitId : binding.traitId;
        binding.iconImage = binding.iconImage != null ? binding.iconImage : childButton.targetGraphic as Image;
      }

      if (!originalButtonColors.ContainsKey(childButton))
      {
        originalButtonColors.Add(childButton, childButton.colors);
      }

      TraitButtonRightClickRouter router = childButton.GetComponent<TraitButtonRightClickRouter>();
      if (router == null)
        router = childButton.gameObject.AddComponent<TraitButtonRightClickRouter>();

      router.Bind(this, binding.traitId);
    }
  }

  private void CachePreviewUIs()
  {
    if (statPreviewUI == null)
      statPreviewUI = GetComponentInChildren<TraitStatPreviewUI>(true);
    // Attempt to auto-find a Slider in children if the inspector hasn't assigned one.
    if (psychoSlider == null)
      psychoSlider = GetComponentInChildren<Slider>(true);
  }

  private void UpdatePsychoSlider()
  {
    if (TraitManager.Instance == null)
      return;

    if (psychoSlider == null)
      psychoSlider = GetComponentInChildren<Slider>(true);

    float current = TraitManager.Instance.GetCurrentPsychoCorruption(currentCategory);

    if (psychoSlider != null)
    {
      psychoSlider.minValue = 0f;
      psychoSlider.maxValue = TraitManager.MaxPsychoCorruption;
      psychoSlider.value = current;
    }

    if (psychoValueText != null)
      psychoValueText.text = Mathf.RoundToInt(current).ToString();
  }

  private string ResolveTraitId(string objectName)
  {
    string normalizedName = NormalizeToTraitId(objectName);
    if (TraitNameAliases.TryGetValue(normalizedName, out string aliasTraitId))
      return aliasTraitId;

    TraitData traitData = TraitManager.Instance.GetTraitData(normalizedName);
    return traitData != null ? normalizedName : string.Empty;
  }

  private static string NormalizeToTraitId(string value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return string.Empty;

    return value.Trim().ToLowerInvariant().Replace(' ', '_').Replace('-', '_');
  }

  private void ApplyButtonColors(Button button)
  {
    if (!originalButtonColors.TryGetValue(button, out ColorBlock colors))
      colors = button.colors;

    colors.disabledColor = colors.normalColor;
    button.colors = colors;
  }
}

public class TraitButtonRightClickRouter : MonoBehaviour, IPointerClickHandler
{
  private TraitUI traitUI;
  private string traitId;

  public void Bind(TraitUI ui, string id)
  {
    traitUI = ui;
    traitId = id;
  }

  public void OnPointerClick(PointerEventData eventData)
  {
    if (eventData == null)
      return;

    if (eventData.button != PointerEventData.InputButton.Right)
      return;

    if (traitUI == null || string.IsNullOrWhiteSpace(traitId))
      return;

    traitUI.OnRightClickDowngradeTrait(traitId);
  }
}
