using System.Collections;
using System.Collections.Generic;
using NeoSurvive.Weapon;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
  public static UIManager instance { get; private set; }

  [Header("UI Elements")]
  public GameObject damageTextPrefab;

  [Header("Weapon UI")]
  public GameObject weaponUIPanel;
  public GameObject weaponIconPrefab;

  [Header("Exp UI")]
  public Slider expSlider;
  public TextMeshProUGUI levelText;

  private void Awake()
  {
    if (instance == null)
    {
      instance = this;
      DontDestroyOnLoad(gameObject);
    }
    else
    {
      Destroy(gameObject);
    }
  }

  private void OnEnable()
  {
    WeaponManager.OnWeaponChanged += RefreshWeaponUI;
    Player.OnExpChanged += UpdateExpUI;
    Player.OnLevelUp += UpdateLevelUI;
  }

  private void OnDisable()
  {
    WeaponManager.OnWeaponChanged -= RefreshWeaponUI;
    Player.OnExpChanged -= UpdateExpUI;
    Player.OnLevelUp -= UpdateLevelUI;
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

    float gap = 5f;
    float padding = 5f;
    float x = padding;
    foreach (WeaponBase weapon in weapons)
    {
      GameObject weaponIcon = Instantiate(weaponIconPrefab, weaponUIPanel.transform);
      weaponIcon.GetComponent<Image>().sprite = weapon.weaponIcon;
      weaponIcon.transform.localPosition = new Vector3(x, padding, 0);
      x += gap;
    }
  }

  private void RefreshWeaponUI(List<WeaponBase> weapons)
  {
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
}
