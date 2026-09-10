using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NeoSurvive.Weapon;

public class WeaponChoiceCard : MonoBehaviour
{
    [Header("Card UI")]
    [SerializeField] private Image iconImage;                  // 아이콘 표시만 담당
    [SerializeField] private TextMeshProUGUI nameText;        // 이름 표시만 담당
    [SerializeField] private TextMeshProUGUI descriptionText; // 설명 표시만 담당
    [SerializeField] private TextMeshProUGUI levelText;       // 레벨 문구 표시만 담당
    [SerializeField] private Button clickButton;              // 카드 클릭만 담당

    private WeaponBase currentWeapon;
    private Action<WeaponBase> onClick;

    private void Awake()
    {
        ValidateReferences();
    }

    /// <summary>
    /// 카드에 무기 정보를 표시하는 기능만 담당
    /// </summary>
    public void Setup(
        WeaponBase weapon,
        string displayName,
        string description,
        string levelInfo,
        Action<WeaponBase> clickCallback)
    {
        ValidateReferences();

        if (weapon == null)
        {
            Debug.LogError("[WeaponChoiceCard] weapon is null.");
            return;
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            Debug.LogError($"[WeaponChoiceCard] displayName is empty. asset={weapon.name}");
            return;
        }

        if (description == null)
        {
            Debug.LogError($"[WeaponChoiceCard] description is null. asset={weapon.name}");
            return;
        }

        if (string.IsNullOrWhiteSpace(levelInfo))
        {
            Debug.LogError($"[WeaponChoiceCard] levelInfo is empty. asset={weapon.name}");
            return;
        }

        currentWeapon = weapon;
        onClick = clickCallback;

        iconImage.sprite = weapon.weaponIcon;
        iconImage.enabled = weapon.weaponIcon != null;

        nameText.text = displayName;
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 14;
        nameText.fontSizeMax = 28;
        nameText.enableWordWrapping = false;
        nameText.overflowMode = TextOverflowModes.Ellipsis;

        descriptionText.text = description;
        descriptionText.enableWordWrapping = true;
        descriptionText.overflowMode = TextOverflowModes.Overflow;

        levelText.text = levelInfo;

        clickButton.onClick.RemoveAllListeners();
        clickButton.onClick.AddListener(HandleClick);
    }

    /// <summary>
    /// 카드 클릭 시 선택된 무기를 전달하는 기능만 담당
    /// </summary>
    private void HandleClick()
    {
        if (currentWeapon == null)
        {
            Debug.LogError("[WeaponChoiceCard] currentWeapon is null.");
            return;
        }

        onClick?.Invoke(currentWeapon);
    }

    private void ValidateReferences()
    {
        if (iconImage == null)
            Debug.LogError($"[WeaponChoiceCard] iconImage is not assigned. object={name}");

        if (nameText == null)
            Debug.LogError($"[WeaponChoiceCard] nameText is not assigned. object={name}");

        if (descriptionText == null)
            Debug.LogError($"[WeaponChoiceCard] descriptionText is not assigned. object={name}");

        if (levelText == null)
            Debug.LogError($"[WeaponChoiceCard] levelText is not assigned. object={name}");

        if (clickButton == null)
            Debug.LogError($"[WeaponChoiceCard] clickButton is not assigned. object={name}");
    }
}