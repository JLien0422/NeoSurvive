using System.Collections;
using System.Collections.Generic;
using NeoSurvive.Weapon;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class UIManager : MonoBehaviour
{
  public static UIManager Instance { get; private set; }

  [Header("UI Elements")]
  public GameObject damageTextPrefab;

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

  private Transform playerTransform;
  [Header("Health Bar Positioning")]
  public Vector3 healthBarOffset = new Vector3(0, 1.0f, 0); // 캐릭터 머리 위 오프셋

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
      if (Application.isPlaying)
        DontDestroyOnLoad(gameObject);
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
  }

  private void OnEnable()
  {
    WeaponManager.OnWeaponChanged += RefreshWeaponUI;
    Player.OnExpChanged += UpdateExpUI;
    Player.OnLevelUp += UpdateLevelUI;
    GameManager.OnKillCountChanged += UpdateKillCountUI;

    // Coroutine을 사용하여 ShowGameTime을 호출
    StartCoroutine(ShowGameTimeCoroutine());
  }

  private void OnDisable()
  {
    WeaponManager.OnWeaponChanged -= RefreshWeaponUI;
    Player.OnExpChanged -= UpdateExpUI;
    Player.OnLevelUp -= UpdateLevelUI;
    GameManager.OnKillCountChanged -= UpdateKillCountUI;
  }

  private IEnumerator ShowGameTimeCoroutine()
  {
    while (true)
    {
      ShowGameTime();
      yield return new WaitForSeconds(1f);
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
    if (weapons == null) return;

    Debug.Log($"[UIManager] DrawWeapons called with {weapons.Count} weapons");

    float gap = 80f;
    float padding = 20f;
    float x = padding;
    foreach (WeaponBase weapon in weapons)
    {
      GameObject weaponIcon = Instantiate(weaponIconPrefab, weaponUIPanel.transform);
      // UI 요소이므로 RectTransform을 사용하는 것이 안전합니다.
      if (weaponIcon.TryGetComponent<RectTransform>(out var rect))
      {
        rect.anchoredPosition = new Vector2(x, -padding); // 상단 기준 배치를 가정 (필요시 조정)
                                                          // 만약 weaponUIPanel의 피벗이 중앙이면 좌표 계산이 달라질 수 있습니다.
                                                          // 일단 기존 로직(localPosition)을 유지하되 간격만 넓혀도 됩니다.
                                                          // 안전하게 기존 localPosition 방식을 사용하되 gap만 늘립니다.
        weaponIcon.transform.localPosition = new Vector3(x, 0, 0);
      }
      else
      {
        weaponIcon.transform.localPosition = new Vector3(x, 0, 0);
      }

      if (weaponIcon.TryGetComponent<Image>(out var img)) img.sprite = weapon.weaponIcon;
      TextMeshProUGUI lvlText = weaponIcon.GetComponentInChildren<TextMeshProUGUI>();
      if (lvlText != null) lvlText.text = weapon.level.ToString();
      x += gap;
    }
  }

  private void RefreshWeaponUI(List<WeaponBase> weapons)
  {
    Debug.Log("[UIManager] RefreshWeaponUI Event Received");
    foreach (Transform child in weaponUIPanel.transform)
    {
      Destroy(child.gameObject);
    }
    DrawWeapons(weapons);
  }

  public void ShowDamageText(Vector3 position, float damage)
  {
    if (damageTextPrefab == null || weaponUIPanel == null) return;

    // 월드 좌표를 스크린 좌표로 변환
    Vector2 screenPosition = Camera.main.WorldToScreenPoint(position);

    // Canvas 부모 하위에 생성 (weaponUIPanel의 부모인 Canvas를 쓰거나, 별도 레이어 사용 가능)
    GameObject obj = Instantiate(damageTextPrefab, weaponUIPanel.transform.parent);

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

  void ShowCharacterChoice()
  {
    characterChoicePanel.SetActive(true);
  }
}
