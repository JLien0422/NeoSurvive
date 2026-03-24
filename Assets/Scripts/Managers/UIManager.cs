using System.Collections;
using System.Collections.Generic;
using NeoSurvive.Weapon;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
  public static UIManager Instance { get; private set; }

  [Header("UI Elements")]
  public GameObject damageTextPrefab;
  public Transform canvas;

  [Header("Weapon UI")]
  public GameObject weaponUIPanel;
  public GameObject weaponIconPrefab;

  [Header("Exp UI")]
  public Slider expSlider;
  public TextMeshProUGUI levelText;

  public TextMeshProUGUI gameTimeText;
  public TextMeshProUGUI killCountText;

  [Header("Player Health UI")]
  public Slider playerHealthSlider;

  public GameManager gameManager;

  [Header("Character Choice UI")]
  public GameObject characterChoicePanel;

  [Header("사이코 잠식도 UI")]
  public Slider psychoCorruptionSlider;
  public TextMeshProUGUI psychoCorruptionText;

  [Header("화면 노이즈 효과")]
  public GameObject screenNoiseOverlay; // 화면 모서리 노이즈 오버레이 (60% 이상일 때 표시)

  [Header("신경링크 UI")]
  public Slider neuralLinkSlider;
  public TextMeshProUGUI neuralLinkText;

  private Transform playerTransform;
  [Header("Health Bar Positioning")]
  public Vector3 healthBarOffset = new Vector3(0, 1.0f, 0); // 캐릭터 머리 위 오프셋

  // =========================
  // 무기 선택 UI (추가)
  // =========================
  [Header("Weapon Choice UI (추가)")]
  public GameObject weaponChoicePanel;                     // (추가)
  public Button[] weaponChoiceButtons = new Button[3];     // (추가)
  public Image[] weaponChoiceIcons = new Image[3];         // (추가)
  public TextMeshProUGUI[] weaponChoiceTexts = new TextMeshProUGUI[3]; // (추가)

  private Action<int> onWeaponChoicePicked;                // (추가)

  // ===== 상자 보상 선택 저장용 (추가) ===== //
  private NeoSurvive.Weapon.WeaponBase[] currentChoices;
  private bool eventsRegistered = false;


  private void Awake()
  {
    Debug.Log("[UIManager] Awake called");
    if (Instance == null)
    {
      Instance = this;
    }
    else
    {
      Destroy(gameObject);
      return;
    }

    if (gameManager == null)
    {
      gameManager = FindObjectOfType<GameManager>();
      if (gameManager == null)
      {
        Debug.LogError("[UIManager] GameManager not found in the scene!");
      }
    }
  }

  private void Start()
  {
    // Start logic moved to Awake for singleton initialization, keeping Start empty or for other delayed init
    RebindSceneReferences();
    // UI 요소가 null이면 자동 생성
    AutoCreateMissingUI();
    ForceRefreshWeaponUI();
    StartGameTimeCoroutineIfNeeded();
  }

  /// <summary>
  /// 누락된 UI 요소들을 자동으로 생성합니다.
  /// </summary>
  private void AutoCreateMissingUI()
  {
    // Canvas 찾기
    Canvas canvas = FindObjectOfType<Canvas>();
    if (canvas == null)
    {
      Debug.LogError("[UIManager] Canvas를 찾을 수 없습니다!");
      return;
    }

    // 기존 화면 노이즈 오버레이가 있으면 먼저 재연결
    if (screenNoiseOverlay == null)
    {
      var existingOverlay = GameObject.Find("ScreenNoiseOverlay");
      if (existingOverlay != null)
        screenNoiseOverlay = existingOverlay;
    }

    // 화면 노이즈 오버레이 생성
    if (screenNoiseOverlay == null)
    {
      CreateScreenNoiseOverlay(canvas.transform);
    }

    Debug.Log("[UIManager] 누락된 UI 요소 자동 생성 완료");
  }

  /// <summary>
  /// 화면 노이즈 오버레이 생성
  /// </summary>
  private void CreateScreenNoiseOverlay(Transform parent)
  {
    GameObject overlayObj = new GameObject("ScreenNoiseOverlay");
    overlayObj.transform.SetParent(parent, false);

    RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
    overlayRect.anchorMin = Vector2.zero;
    overlayRect.anchorMax = Vector2.one;
    overlayRect.sizeDelta = Vector2.zero;
    overlayRect.anchoredPosition = Vector2.zero;

    Image overlayImg = overlayObj.AddComponent<Image>();
    overlayImg.color = new Color(1f, 0f, 0f, 0.1f); // 빨간색 반투명
    overlayImg.raycastTarget = false; // 클릭 통과 (버튼/UI 가리지 않음)

    screenNoiseOverlay = overlayObj;
    screenNoiseOverlay.SetActive(false); // 초기에는 비활성화
  }


  private void OnEnable()
  {
    RegisterEvents();

    // Coroutine을 사용하여 ShowGameTime을 호출
    StartGameTimeCoroutineIfNeeded();

  }

  private void OnDisable()
  {
    UnregisterEvents();
  }

  private void OnDestroy()
  {
    UnregisterEvents();
    if (Instance == this) Instance = null;
  }

  private void RegisterEvents()
  {
    if (eventsRegistered) return;
    WeaponManager.OnWeaponChanged += RefreshWeaponUI;
    Player.OnExpChanged += UpdateExpUI;
    Player.OnLevelUp += UpdateLevelUI;
    GameManager.OnKillCountChanged += UpdateKillCountUI;
    Player.OnPsychoCorruptionChanged += UpdatePsychoCorruptionUI;
    Player.OnBerserkStarted += OnBerserkStarted;
    Player.OnBerserkEnded += OnBerserkEnded;
    Player.OnNeuralLinkGaugeChanged += UpdateNeuralLinkUI;
    Player.OnNeuralLinkActivated += OnNeuralLinkActivated;
    ChestPickup.OnChestOpened += HandleChestOpened;
    SceneManager.sceneLoaded += OnSceneLoaded;
    eventsRegistered = true;
  }

  private void UnregisterEvents()
  {
    if (!eventsRegistered) return;
    WeaponManager.OnWeaponChanged -= RefreshWeaponUI;
    Player.OnExpChanged -= UpdateExpUI;
    Player.OnLevelUp -= UpdateLevelUI;
    GameManager.OnKillCountChanged -= UpdateKillCountUI;
    Player.OnPsychoCorruptionChanged -= UpdatePsychoCorruptionUI;
    Player.OnBerserkStarted -= OnBerserkStarted;
    Player.OnBerserkEnded -= OnBerserkEnded;
    Player.OnNeuralLinkGaugeChanged -= UpdateNeuralLinkUI;
    Player.OnNeuralLinkActivated -= OnNeuralLinkActivated;
    ChestPickup.OnChestOpened -= HandleChestOpened;
    SceneManager.sceneLoaded -= OnSceneLoaded;
    eventsRegistered = false;
  }

  private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
  {
    // 씬 전환 이후 참조가 끊기는 문제를 방지
    Time.timeScale = 1f;
    RebindSceneReferences();
    AutoCreateMissingUI();
    ForceRefreshWeaponUI();
    ShowGameTime();
  }

  private void StartGameTimeCoroutineIfNeeded()
  {
    if (!isActiveAndEnabled) return;
    StopCoroutine(nameof(ShowGameTimeCoroutine));
    StartCoroutine(nameof(ShowGameTimeCoroutine));
  }

  private void RebindSceneReferences()
  {
    if (gameManager == null) gameManager = FindObjectOfType<GameManager>();

    // if (weaponUIPanel == null)
    // {
    //   GameObject found = GameObject.Find("WeaponUIPanel");
    //   if (found == null) found = GameObject.Find("WeaponUI");
    //   if (found == null) found = GameObject.Find("WeaponPanel");
    //   weaponUIPanel = found;
    // }

    if (expSlider == null) expSlider = FindSliderByNameContains("exp");
    if (playerHealthSlider == null) playerHealthSlider = FindSliderByNameContains("health");
    if (psychoCorruptionSlider == null)
      psychoCorruptionSlider = FindSliderByNameContains("psycho", "overload", "corruption", "과부하");
    if (neuralLinkSlider == null)
      neuralLinkSlider = FindSliderByNameContains("neural", "link", "신경", "링크");

    if (levelText == null) levelText = FindTMPByNameContains("level", "lv");
    if (gameTimeText == null) gameTimeText = FindTMPByNameContains("time", "timer");
    if (killCountText == null) killCountText = FindTMPByNameContains("kill", "count");
    if (psychoCorruptionText == null) psychoCorruptionText = FindTMPByNameContains("psycho", "overload", "과부하");
    if (neuralLinkText == null) neuralLinkText = FindTMPByNameContains("neural", "link", "신경", "링크");
  }

  private Slider FindSliderByNameContains(params string[] keywords)
  {
    var sliders = FindObjectsOfType<Slider>(true);
    foreach (var s in sliders)
    {
      if (s == null) continue;
      string n = s.gameObject.name.ToLowerInvariant();
      foreach (var key in keywords)
      {
        if (!string.IsNullOrEmpty(key) && n.Contains(key.ToLowerInvariant()))
          return s;
      }
    }
    return null;
  }

  private TextMeshProUGUI FindTMPByNameContains(params string[] keywords)
  {
    var tmps = FindObjectsOfType<TextMeshProUGUI>(true);
    foreach (var t in tmps)
    {
      if (t == null) continue;
      string n = t.gameObject.name.ToLowerInvariant();
      foreach (var key in keywords)
      {
        if (!string.IsNullOrEmpty(key) && n.Contains(key.ToLowerInvariant()))
          return t;
      }
    }
    return null;
  }

  private void ForceRefreshWeaponUI()
  {
    var wm = GetBestWeaponManager();
    if (wm != null)
    {
      RefreshWeaponUI(wm.activeWeapons);
    }
  }

  private NeoSurvive.Weapon.WeaponManager GetBestWeaponManager()
  {
    // 1) Player 태그 기준 우선
    GameObject playerObj = GameObject.FindWithTag("Player");
    if (playerObj != null)
    {
      var wmOnPlayer = playerObj.GetComponent<NeoSurvive.Weapon.WeaponManager>();
      if (wmOnPlayer != null) return wmOnPlayer;
    }

    // 2) Local Player 우선
    var players = FindObjectsOfType<Player>(true);
    foreach (var p in players)
    {
      if (p != null && p.IsLocal)
      {
        var wm = p.GetComponent<NeoSurvive.Weapon.WeaponManager>();
        if (wm != null) return wm;
      }
    }

    // 3) 최후 fallback
    return FindObjectOfType<NeoSurvive.Weapon.WeaponManager>();
  }

  // =========================
  // 상자 열림 → 무기 선택 UI 표시 (추가)
  // =========================
  private void HandleChestOpened() // ***
  {
    Debug.Log("[UIManager] Chest opened → Weapon choice UI"); // ***

    // 씬에서 WeaponManager 찾기 // *****
    var wm = GetBestWeaponManager(); // *****
    if (wm == null) // *****
    {
      Debug.LogError("[UIManager] WeaponManager not found in scene!"); // *****
      return; // *****
    }

    // allWeaponDatas에서 랜덤 3개 뽑기 // *****
    var choices = Pick3RandomWeapons(wm.allWeaponDatas); // *****
    if (choices == null) // *****
    {
      Debug.LogError("[UIManager] Not enough weapons in allWeaponDatas (need 3+)"); // *****
      return; // *****
    }

    // UI 띄우기 + 선택하면 WeaponManager.AddWeapon 호출 // *****
    ShowWeaponChoiceByData(choices, (picked) => // *****
    {
      if (picked == null) return; // *****
      wm.AddWeapon(picked); // *****
      Debug.Log($"[UIManager] Picked weapon: {picked.name}"); // *****
    }); // *****
  }

  // allWeaponDatas에서 중복 없이 3개 랜덤 선택 (추가) // *****
  private NeoSurvive.Weapon.WeaponBase[] Pick3RandomWeapons(List<NeoSurvive.Weapon.WeaponBase> all) // *****
  {
    if (all == null || all.Count < 3) return null; // *****

    List<NeoSurvive.Weapon.WeaponBase> temp = new List<NeoSurvive.Weapon.WeaponBase>(all); // *****

    for (int i = 0; i < temp.Count; i++) // *****
    {
      int j = UnityEngine.Random.Range(i, temp.Count); // *****
      var t = temp[i]; temp[i] = temp[j]; temp[j] = t; // *****
    }

    return new NeoSurvive.Weapon.WeaponBase[] { temp[0], temp[1], temp[2] }; // *****
  }

  // WeaponBase 3개를 UI에 표시하고, 선택된 WeaponBase를 콜백으로 전달 (추가) // *****
  public void ShowWeaponChoiceByData(NeoSurvive.Weapon.WeaponBase[] choices, Action<NeoSurvive.Weapon.WeaponBase> onPicked) // *****
  {
    if (weaponChoicePanel == null) return; // *****
    if (choices == null || choices.Length != 3) return; // *****

    currentChoices = choices; // *****

    for (int i = 0; i < 3; i++) // *****
    {
      var w = choices[i]; // *****

      if (weaponChoiceIcons[i] != null) // *****
        weaponChoiceIcons[i].sprite = (w != null) ? w.weaponIcon : null; // *****

      if (weaponChoiceTexts[i] != null) // *****
        weaponChoiceTexts[i].text = (w != null) ? w.name : "NULL"; // *****

      int idx = i; // *****
      weaponChoiceButtons[i].onClick.RemoveAllListeners(); // *****
      weaponChoiceButtons[i].onClick.AddListener(() => // *****
      {
        weaponChoicePanel.SetActive(false); // *****
        Time.timeScale = 1f; // *****

        onPicked?.Invoke(currentChoices[idx]); // *****
        currentChoices = null; // *****
      }); // *****
    }

    weaponChoicePanel.SetActive(true); // *****
    Time.timeScale = 0f; // *****
  }

  private IEnumerator ShowGameTimeCoroutine()
  {
    while (true)
    {
      ShowGameTime();
      yield return new WaitForSecondsRealtime(0.2f);
    }
  }

  private void UpdateExpUI(int currentExp, int maxExp)
  {
    if (expSlider != null)
    {
      expSlider.maxValue = maxExp;
      expSlider.value = currentExp;
    }
  }

  private void UpdateLevelUI(int level)
  {
    if (levelText != null)
    {
      levelText.text = $"Lv. {level}";
    }
  }

  private void DrawWeapons(List<WeaponBase> weapons)
  {
    if (weapons == null || weaponUIPanel == null) return;

    Debug.Log($"[UIManager] DrawWeapons called with {weapons.Count} weapons");

    // WeaponUIPanel 하위에 미리 배치된 WeaponIcon 슬롯(직계 자식)을 순서대로 채운다.
    int slotCount = weaponUIPanel.transform.childCount;
    for (int i = 0; i < slotCount; i++)
    {
      Transform slot = weaponUIPanel.transform.GetChild(i);
      if (slot == null) continue;

      Transform iconSpriteTf = slot.Find("IconSprite");
      Image iconImage = iconSpriteTf != null ? iconSpriteTf.GetComponent<Image>() : null;
      if (iconImage == null)
      {
        // 이름 기반 참조가 실패하면 슬롯 내부 첫 Image로 fallback
        iconImage = slot.GetComponentInChildren<Image>(true);
      }

      Transform levelTf = slot.Find("WeaponLevel");
      TMP_Text weaponLevelText = levelTf != null ? levelTf.GetComponent<TMP_Text>() : null;
      if (weaponLevelText == null)
      {
        // TextMeshProUGUI / TextMeshPro 모두 대응
        weaponLevelText = slot.GetComponentInChildren<TMP_Text>(true);
      }

      WeaponBase weapon = i < weapons.Count ? weapons[i] : null;
      if (weapon == null)
      {
        if (iconImage != null)
        {
          iconImage.sprite = null;
          iconImage.enabled = false;
        }

        if (weaponLevelText != null)
        {
          weaponLevelText.text = string.Empty;
        }

        continue;
      }

      if (iconImage != null)
      {
        iconImage.sprite = weapon.weaponIcon;
        iconImage.enabled = weapon.weaponIcon != null;
      }

      if (weaponLevelText != null)
      {
        weaponLevelText.text = $"Lv.{weapon.level}";
      }
    }
  }

  private void RefreshWeaponUI(List<WeaponBase> weapons)
  {
    if (weaponUIPanel == null) return;
    Debug.Log("[UIManager] RefreshWeaponUI Event Received");
    DrawWeapons(weapons);
  }

  public void ShowDamageText(Vector3 position, float damage)
  {
    // SettingsManager에서 데미지 숫자 표시 옵션 확인
    if (SettingsManager.Instance != null && !SettingsManager.Instance.showDamageNumbers)
    {
      return; // 옵션이 꺼져 있으면 표시하지 않음
    }

    // 해킹 미니게임 진행 중이면 데미지 텍스트를 표시하지 않음
    if (HackingSystem.Instance != null && HackingSystem.Instance.IsHacking)
    {
      return;
    }

    if (damageTextPrefab == null || weaponUIPanel == null) return;

    // 월드 좌표를 스크린 좌표로 변환
    Vector2 screenPosition = Camera.main.WorldToScreenPoint(position);

    // Canvas 부모 하위에 생성 (weaponUIPanel의 부모인 Canvas를 쓰거나, 별도 레이어 사용 가능)
    GameObject obj = Instantiate(damageTextPrefab, canvas);

    if (obj.TryGetComponent<RectTransform>(out var rect))
    {
      rect.position = screenPosition;
    }

    if (obj.TryGetComponent<NeoSurvive.UI.DamageText>(out var dt))
    {
      dt.Setup(damage, position);
    }
  }

  public void ShowGameTime()
  {
    if (gameManager == null)
    {
      // Debug.LogWarning("[UIManager] GameManager is null, cannot show time.");
      return;
    }

    if (gameTimeText == null)
    {
      // Debug.LogWarning("[UIManager] GameTimeText is null/not assigned.");
      return;
    }

    float time = gameManager.GetGameTime();
    // Debug.Log($"[UIManager] ShowGameTime: {time}");
    gameTimeText.text = $"{Mathf.Floor(time / 60):00}:{Mathf.Floor(time % 60):00}";
  }

  private void UpdateKillCountUI(int count)
  {
    if (killCountText != null)
    {
      killCountText.text = count.ToString();
    }
  }

  public void SetPlayerHealthBar(Player player)
  {
    if (playerHealthSlider == null) return;

    // 기존 구독 해제 처리는 생략 (단일 플레이어 가정)
    player.OnHealthChanged += UpdatePlayerHealthUI;

    // 추적 대상 설정
    this.playerTransform = player.transform;

    // 초기값 설정
    UpdatePlayerHealthUI(player.CurrentHealth, player.HealthStat.GetValue());
  }

  private void UpdatePlayerHealthUI(float current, float max)
  {
    if (playerHealthSlider != null)
    {
      playerHealthSlider.maxValue = max;
      playerHealthSlider.value = current;
    }
  }

  private void LateUpdate()
  {
    if (playerHealthSlider != null && playerTransform != null && Camera.main != null)
    {
      // 월드 좌표(캐릭터 + 오프셋)를 스크린 좌표로 변환하여 Slider 위치 갱신
      Vector3 worldPos = playerTransform.position + healthBarOffset;
      playerHealthSlider.transform.position = Camera.main.WorldToScreenPoint(worldPos);
    }
  }

  // =========================
  // 무기 선택 UI (추가)
  // =========================
  public void ShowWeaponChoice(
    Sprite[] icons,
    string[] names,
    Action<int> onPicked
  )
  {
    if (weaponChoicePanel == null) return;

    onWeaponChoicePicked = onPicked;

    for (int i = 0; i < 3; i++)
    {
      weaponChoiceIcons[i].sprite = icons[i];
      weaponChoiceTexts[i].text = names[i];

      int idx = i;
      weaponChoiceButtons[i].onClick.RemoveAllListeners();
      weaponChoiceButtons[i].onClick.AddListener(() => PickWeapon(idx));
    }

    weaponChoicePanel.SetActive(true);
    Time.timeScale = 0f;
  }

  private void PickWeapon(int index)
  {
    weaponChoicePanel.SetActive(false);
    Time.timeScale = 1f;

    onWeaponChoicePicked?.Invoke(index);
    onWeaponChoicePicked = null;
  }

  public void HideWeaponChoice()
  {
    weaponChoicePanel.SetActive(false);
    Time.timeScale = 1f;
    onWeaponChoicePicked = null;
  }

  // =========================
  // 테스트용 (추가)
  // =========================
  private void Update()
  {
    if (Input.GetKeyDown(KeyCode.F8))
    {
      ShowWeaponChoice(
        new Sprite[3] { null, null, null },
        new string[3] { "AIDrone", "EmpField", "LinkPistol" },
        (idx) => Debug.Log($"선택한 무기 인덱스: {idx}")
      );
    }
  }

  // ===================== 사이코 잠식도 UI =====================

  /// <summary>
  /// 사이코 잠식도 UI 업데이트
  /// </summary>
  private void UpdatePsychoCorruptionUI(float current, float max)
  {
    if (psychoCorruptionSlider != null)
    {
      psychoCorruptionSlider.maxValue = max;
      psychoCorruptionSlider.value = current;
    }

    if (psychoCorruptionText != null)
    {
      psychoCorruptionText.text = $"과부하: {current:F1}%";
    }
  }

  /// <summary>
  /// 화면 노이즈 효과 표시/숨김 (60% 이상일 때)
  /// </summary>
  public void SetScreenNoise(bool active)
  {
    if (screenNoiseOverlay != null)
    {
      screenNoiseOverlay.SetActive(active);
      if (active && screenNoiseOverlay.TryGetComponent<Image>(out var img))
        img.raycastTarget = false; // 클릭 통과
    }
  }

  /// <summary>
  /// 폭주 상태 시작 시 호출
  /// </summary>
  private void OnBerserkStarted()
  {
    // 폭주 상태 UI 효과 (예: 화면 빨간색 오버레이 등)
    Debug.Log("[UIManager] 폭주 상태 시작!");
  }

  /// <summary>
  /// 폭주 상태 종료 시 호출
  /// </summary>
  private void OnBerserkEnded()
  {
    // 폭주 상태 UI 효과 제거
    Debug.Log("[UIManager] 폭주 상태 종료!");
  }

  // ===================== 신경링크 UI =====================

  /// <summary>
  /// 신경링크 게이지 UI 업데이트
  /// </summary>
  private void UpdateNeuralLinkUI(float current, float max)
  {
    if (neuralLinkSlider != null)
    {
      neuralLinkSlider.maxValue = max;
      neuralLinkSlider.value = current;
    }

    if (neuralLinkText != null)
    {
      neuralLinkText.text = $"신경링크: {current:F0}%";
    }
  }

  /// <summary>
  /// 신경링크 발동 시 호출
  /// </summary>
  private void OnNeuralLinkActivated(NeoSurvive.Characters.CharacterType characterType)
  {
    Debug.Log($"[UIManager] 신경링크 발동! ({characterType})");
  }
}
