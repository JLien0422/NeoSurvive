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

    [Header("Colors")]
    [SerializeField] private Color newWeaponLevelColor = new Color(0.35f, 1f, 0.72f);
    [SerializeField] private Color logicHeaderColor = new Color(0.918f, 0.969f, 1f);
    [SerializeField] private Color statHeaderColor = new Color(0.384f, 0.867f, 0.961f);
    [SerializeField] private Color statValueColor = new Color(0.725f, 0.957f, 1f);

    private WeaponBase currentWeapon;
    private Action<WeaponBase> onClick;
    private RectTransform synergyContainer;
    private Color levelUpLevelColor = new Color(1f, 0.82f, 0.22f);

    private void Awake()
    {
        ValidateReferences();
        if (levelText != null)
            levelUpLevelColor = levelText.color;
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

        descriptionText.text = BuildReadableDescription(description);
        descriptionText.richText = true;

        levelText.text = levelInfo;
        levelText.gameObject.SetActive(true);
        levelText.enabled = true;
        levelText.alpha = 1f;
        levelText.richText = true;
        levelText.color = levelInfo.Contains("신규 무기")
            ? newWeaponLevelColor
            : levelUpLevelColor;
        RefreshSynergyTags(weapon);
        levelText.transform.SetAsLastSibling();

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

    private string BuildReadableDescription(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return string.Empty;

        string normalized = source.Replace("\r\n", "\n").Trim();
        string[] lines = normalized.Split('\n');
        int firstContentLine = lines.Length > 0 && IsLevelHeading(lines[0]) ? 1 : 0;

        if (firstContentLine >= lines.Length)
            return normalized;

        string logic = lines[firstContentLine].Trim();
        string numeric = string.Join("\n", lines, firstContentLine + 1, lines.Length - firstContentLine - 1).Trim();
        string logicColor = "#" + ColorUtility.ToHtmlStringRGB(logicHeaderColor);
        string headerColor = "#" + ColorUtility.ToHtmlStringRGB(statHeaderColor);
        string valueColor = "#" + ColorUtility.ToHtmlStringRGB(statValueColor);

        if (string.IsNullOrEmpty(numeric))
            return $"<color={logicColor}><b>작동 방식</b></color>\n{logic}";

        return
            $"<color={logicColor}><b>작동 방식</b></color>\n" +
            $"{logic}\n\n" +
            $"<color={headerColor}><b>수치 변화</b></color>\n" +
            $"<color={valueColor}>{numeric}</color>";
    }

    private static bool IsLevelHeading(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        string trimmed = line.Trim();
        return trimmed.StartsWith("선택 후 Lv.", StringComparison.Ordinal) ||
               trimmed.StartsWith("5레벨 달성 효과", StringComparison.Ordinal);
    }

    private void RefreshSynergyTags(WeaponBase weapon)
    {
        EnsureSynergyContainer();
        Transform template = synergyContainer.Find("SynergyChipTemplate");

        for (int index = synergyContainer.childCount - 1; index >= 0; index--)
        {
            Transform child = synergyContainer.GetChild(index);
            if (child == template)
                continue;
            Destroy(child.gameObject);
        }

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
            GameObject chip = template != null
                ? Instantiate(template.gameObject, synergyContainer)
                : CreateSynergyChip(tag, current, maximum);
            chip.name = tag + "Tag";
            chip.SetActive(true);
            if (template != null)
                FillSynergyChip(chip, tag, current, maximum);
        }

        if (template != null)
            template.gameObject.SetActive(false);
    }

    private void EnsureSynergyContainer()
    {
        if (synergyContainer != null)
            return;

        Transform existing = transform.Find("SynergyTagContainer");
        if (existing != null)
        {
            synergyContainer = existing as RectTransform;
            return;
        }

        GameObject container = new GameObject("SynergyTagContainer", typeof(RectTransform), typeof(GridLayoutGroup));
        container.transform.SetParent(transform, false);
        synergyContainer = container.GetComponent<RectTransform>();
        synergyContainer.anchorMin = new Vector2(0f, 0f);
        synergyContainer.anchorMax = new Vector2(1f, 0f);
        synergyContainer.pivot = new Vector2(0.5f, 0f);
        synergyContainer.anchoredPosition = new Vector2(0f, 48f);
        synergyContainer.sizeDelta = new Vector2(-24f, 64f);

        GridLayoutGroup layout = container.GetComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(258f, 28f);
        layout.spacing = new Vector2(0f, 6f);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Vertical;
        layout.childAlignment = TextAnchor.LowerCenter;
        layout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        layout.constraintCount = 2;
    }

    private GameObject CreateSynergyChip(SynergyTag tag, int current, int maximum)
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

        Text nameLabel = CreateChipText("Name", chip.transform, SynergyPresentation.GetDisplayName(tag), TextAnchor.MiddleLeft, 142f);
        nameLabel.color = Color.white;

        Text stackLabel = CreateChipText("Stack", chip.transform, $"{current}/{maximum}", TextAnchor.MiddleRight, 52f);
        stackLabel.fontStyle = FontStyle.Bold;
        stackLabel.color = Color.Lerp(tagColor, Color.white, 0.28f);
        return chip;
    }

    private static void FillSynergyChip(GameObject chip, SynergyTag tag, int current, int maximum)
    {
        Color tagColor = SynergyPresentation.GetColor(tag);
        Image background = chip.GetComponent<Image>();
        if (background != null)
            background.color = new Color(tagColor.r * 0.18f, tagColor.g * 0.18f, tagColor.b * 0.18f, 0.94f);

        Outline outline = chip.GetComponent<Outline>();
        if (outline != null)
            outline.effectColor = new Color(tagColor.r, tagColor.g, tagColor.b, 0.72f);

        Transform icon = chip.transform.Find("Icon");
        if (icon != null)
        {
            Image iconImage = icon.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = SynergyPresentation.GetIcon(tag);
                iconImage.color = tagColor;
            }
        }

        SetChipLabel(chip.transform.Find("Name"), SynergyPresentation.GetDisplayName(tag), Color.white, false);
        SetChipLabel(chip.transform.Find("Stack"), current + "/" + maximum, Color.Lerp(tagColor, Color.white, 0.28f), true);
    }

    private static void SetChipLabel(Transform target, string value, Color color, bool bold)
    {
        if (target == null)
            return;

        TextMeshProUGUI tmp = target.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = value;
            tmp.color = color;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            return;
        }

        Text text = target.GetComponent<Text>();
        if (text == null)
            return;

        text.text = value;
        text.color = color;
        text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
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
