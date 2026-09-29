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
    private RectTransform synergyContainer;

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
        RefreshSynergyTags(weapon);

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

    private void RefreshSynergyTags(WeaponBase weapon)
    {
        EnsureSynergyContainer();

        for (int index = synergyContainer.childCount - 1; index >= 0; index--)
            Destroy(synergyContainer.GetChild(index).gameObject);

        if (weapon.synergyTags == null || weapon.synergyTags.Count == 0)
        {
            synergyContainer.gameObject.SetActive(false);
            return;
        }

        synergyContainer.gameObject.SetActive(true);
        WeaponManager manager = WeaponManager.Instance;
        foreach (SynergyTag tag in weapon.synergyTags)
        {
            int current = manager != null ? manager.GetSynergyStackAfterAdding(tag, weapon) : 0;
            int maximum = manager != null ? manager.GetSynergyMaximumStack(tag) : 0;
            CreateSynergyChip(tag, current, maximum);
        }
    }

    private void EnsureSynergyContainer()
    {
        if (synergyContainer != null)
            return;

        GameObject container = new GameObject("SynergyTagContainer", typeof(RectTransform), typeof(GridLayoutGroup));
        container.transform.SetParent(transform, false);
        synergyContainer = container.GetComponent<RectTransform>();
        synergyContainer.anchorMin = new Vector2(0f, 0f);
        synergyContainer.anchorMax = new Vector2(1f, 0f);
        synergyContainer.pivot = new Vector2(0.5f, 0f);
        synergyContainer.anchoredPosition = new Vector2(0f, 58f);
        synergyContainer.sizeDelta = new Vector2(-24f, 62f);

        GridLayoutGroup layout = container.GetComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(126f, 28f);
        layout.spacing = new Vector2(6f, 5f);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.LowerCenter;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 2;
    }

    private void CreateSynergyChip(SynergyTag tag, int current, int maximum)
    {
        GameObject chip = new GameObject(tag + "Tag", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(HorizontalLayoutGroup));
        chip.transform.SetParent(synergyContainer, false);

        Image background = chip.GetComponent<Image>();
        Color tagColor = SynergyPresentation.GetColor(tag);
        background.color = new Color(tagColor.r * 0.18f, tagColor.g * 0.18f, tagColor.b * 0.18f, 0.94f);
        background.raycastTarget = false;

        Outline outline = chip.GetComponent<Outline>();
        outline.effectColor = new Color(tagColor.r, tagColor.g, tagColor.b, 0.72f);
        outline.effectDistance = new Vector2(1f, -1f);

        HorizontalLayoutGroup layout = chip.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 4, 4);
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        iconObject.transform.SetParent(chip.transform, false);
        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = SynergyPresentation.GetIcon(tag);
        icon.color = tagColor;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
        iconLayout.preferredWidth = 16f;
        iconLayout.preferredHeight = 16f;

        Text nameLabel = CreateChipText("Name", chip.transform, SynergyPresentation.GetDisplayName(tag), TextAnchor.MiddleLeft, 48f);
        nameLabel.color = Color.white;

        Text stackLabel = CreateChipText("Stack", chip.transform, $"{current}/{maximum}", TextAnchor.MiddleRight, 34f);
        stackLabel.fontStyle = FontStyle.Bold;
        stackLabel.color = Color.Lerp(tagColor, Color.white, 0.28f);
    }

    private static Text CreateChipText(string objectName, Transform parent, string value, TextAnchor alignment, float width)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = SynergyPresentation.GetKoreanFont();
        text.text = value;
        text.fontSize = 14;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;

        LayoutElement layout = textObject.GetComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.minWidth = width;
        layout.flexibleWidth = 0f;
        return text;
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
