using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TraitChoiceCard : MonoBehaviour
{
    private const float TagChipHeight = 36f;
    private const float TagChipBottomMargin = 18f;
    private const float DescriptionReserve = 56f;

    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button clickButton;

    [Header("레벨 색")]
    [SerializeField] private Color newLevelColor = new Color(0.35f, 1f, 0.72f, 1f);
    [SerializeField] private Color levelUpLevelColor = new Color(1f, 0.86f, 0.2f, 1f);
    [SerializeField] private Color masterLevelColor = new Color(1f, 0.28f, 0.28f, 1f);

    [Header("등급별 테두리")]
    [SerializeField] private Image[] borderImages = new Image[0];
    [SerializeField] private Color silverColor = new Color(210f / 255f, 210f / 255f, 210f / 255f, 1f);
    [SerializeField] private Color goldColor = new Color(1f, 237f / 255f, 0f, 1f);
    [SerializeField] private Color platinumColor = new Color(0f, 1f, 210f / 255f, 1f);

    private RectTransform tagChip;
    private Image tagChipBackground;
    private Outline tagChipOutline;
    private Image tagChipIcon;
    private TextMeshProUGUI tagChipLabel;
    private bool descriptionSpaceReserved;

    public bool IsConfigured => iconImage != null && nameText != null
        && descriptionText != null && levelText != null && clickButton != null;

    public void Setup(RunTraitDefinition trait, string levelInfo, Action onSelected)
    {
        ApplyTierColor(trait.tier);
        iconImage.sprite = trait.icon;
        iconImage.enabled = trait.icon != null;
        nameText.text = string.IsNullOrWhiteSpace(trait.displayName) ? trait.traitId : trait.displayName;
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 14;
        nameText.fontSizeMax = 28;
        nameText.enableWordWrapping = false;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        descriptionText.text = trait.description ?? string.Empty;
        descriptionText.enableWordWrapping = true;
        descriptionText.overflowMode = TextOverflowModes.Overflow;
        levelText.text = levelInfo;
        levelText.color = ResolveLevelColor(levelInfo);
        clickButton.onClick.RemoveAllListeners();
        clickButton.onClick.AddListener(() => onSelected?.Invoke());
        RefreshTagChip(trait);
    }

    private void RefreshTagChip(RunTraitDefinition trait)
    {
        if (trait == null || !trait.requiresWeaponTag)
        {
            if (tagChip != null)
                tagChip.gameObject.SetActive(false);
            return;
        }

        EnsureTagChip();
        ReserveDescriptionSpace();
        tagChip.gameObject.SetActive(true);
        tagChip.SetAsLastSibling();

        Color tagColor = SynergyPresentation.GetColor(trait.requiredTag);
        tagChipBackground.color = new Color(tagColor.r * 0.18f, tagColor.g * 0.18f, tagColor.b * 0.18f, 0.94f);
        tagChipOutline.effectColor = new Color(tagColor.r, tagColor.g, tagColor.b, 0.72f);
        tagChipIcon.sprite = SynergyPresentation.GetIcon(trait.requiredTag);
        tagChipIcon.color = tagColor;
        tagChipLabel.text = SynergyPresentation.GetDisplayName(trait.requiredTag);
    }

    private void EnsureTagChip()
    {
        if (tagChip != null)
            return;

        GameObject chip = new GameObject(
            "SynergyTagChip",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline),
            typeof(HorizontalLayoutGroup));
        chip.transform.SetParent(transform, false);

        tagChip = chip.GetComponent<RectTransform>();
        tagChip.anchorMin = new Vector2(0.5f, 0f);
        tagChip.anchorMax = new Vector2(0.5f, 0f);
        tagChip.pivot = new Vector2(0.5f, 0f);
        tagChip.anchoredPosition = new Vector2(0f, TagChipBottomMargin);
        tagChip.sizeDelta = new Vector2(168f, TagChipHeight);

        tagChipBackground = chip.GetComponent<Image>();
        tagChipBackground.raycastTarget = false;

        tagChipOutline = chip.GetComponent<Outline>();
        tagChipOutline.effectDistance = new Vector2(1f, -1f);

        HorizontalLayoutGroup layout = chip.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 6, 6);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        iconObject.transform.SetParent(chip.transform, false);
        tagChipIcon = iconObject.GetComponent<Image>();
        tagChipIcon.preserveAspect = true;
        tagChipIcon.raycastTarget = false;
        LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
        iconLayout.preferredWidth = 16f;
        iconLayout.preferredHeight = 16f;

        GameObject labelObject = new GameObject("Name", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        labelObject.transform.SetParent(chip.transform, false);
        tagChipLabel = labelObject.GetComponent<TextMeshProUGUI>();
        if (descriptionText != null)
            tagChipLabel.font = descriptionText.font;
        tagChipLabel.fontSize = 18f;
        tagChipLabel.color = Color.white;
        tagChipLabel.alignment = TextAlignmentOptions.MidlineLeft;
        tagChipLabel.enableWordWrapping = false;
        tagChipLabel.overflowMode = TextOverflowModes.Ellipsis;
        tagChipLabel.raycastTarget = false;
        LayoutElement labelLayout = labelObject.GetComponent<LayoutElement>();
        labelLayout.preferredWidth = 96f;
        labelLayout.flexibleWidth = 1f;
    }

    private void ReserveDescriptionSpace()
    {
        if (descriptionSpaceReserved || descriptionText == null)
            return;

        ScrollRect scroll = descriptionText.GetComponentInParent<ScrollRect>();
        if (scroll == null)
            return;

        RectTransform rect = scroll.transform as RectTransform;
        if (rect == null)
            return;

        rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.y - DescriptionReserve);
        rect.anchoredPosition += new Vector2(0f, DescriptionReserve * 0.5f);
        descriptionSpaceReserved = true;
    }

    private Color ResolveLevelColor(string levelInfo)
    {
        if (string.IsNullOrEmpty(levelInfo))
            return newLevelColor;
        if (levelInfo.Contains("마스터"))
            return masterLevelColor;
        if (levelInfo.Contains("신규"))
            return newLevelColor;
        return levelUpLevelColor;
    }

    private void ApplyTierColor(RunTraitTier tier)
    {
        Color tint = tier == RunTraitTier.Gold ? goldColor
            : tier == RunTraitTier.Platinum ? platinumColor : silverColor;
        foreach (Image border in borderImages)
        {
            if (border == null)
                continue;
            // 테두리 원본의 투명도는 유지합니다.
            tint.a = border.color.a;
            border.color = tint;
        }
    }
}
