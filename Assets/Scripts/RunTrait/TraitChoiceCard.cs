using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TraitChoiceCard : MonoBehaviour
{
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

    [Header("프리팹 태그 UI")]
    [SerializeField] private RectTransform tagChip;
    [SerializeField] private Image tagChipBackground;
    [SerializeField] private Outline tagChipOutline;
    [SerializeField] private Image tagChipIcon;
    [SerializeField] private TextMeshProUGUI tagChipLabel;
    [SerializeField] private ScrollRect descriptionScroll;

    public bool IsConfigured => iconImage != null && nameText != null
        && descriptionText != null && levelText != null && clickButton != null;

    public void Setup(RunTraitDefinition trait, string levelInfo, Action onSelected)
    {
        ApplyTierColor(trait.tier);
        iconImage.sprite = trait.icon != null ? trait.icon
            : trait.requiresWeaponTag ? SynergyPresentation.GetIcon(trait.requiredTag) : null;
        iconImage.color = trait.icon != null ? Color.white
            : SynergyPresentation.GetColor(trait.requiredTag);
        iconImage.enabled = iconImage.sprite != null;
        nameText.text = string.IsNullOrWhiteSpace(trait.displayName) ? trait.traitId : trait.displayName;
        descriptionText.text = trait.description ?? string.Empty;
        descriptionText.enableWordWrapping = true;
        descriptionText.overflowMode = TextOverflowModes.Overflow;
        levelText.text = levelInfo;
        levelText.color = ResolveLevelColor(levelInfo);
        clickButton.onClick.RemoveAllListeners();
        clickButton.onClick.AddListener(() => onSelected?.Invoke());
        RefreshTagChip(trait);
        if (descriptionScroll != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(descriptionText.rectTransform);
            descriptionScroll.StopMovement();
            descriptionScroll.verticalNormalizedPosition = 1f;
        }
    }

    private void RefreshTagChip(RunTraitDefinition trait)
    {
        if (trait == null || !trait.requiresWeaponTag)
        {
            if (tagChip != null)
                tagChip.gameObject.SetActive(false);
            return;
        }

        if (tagChip == null || tagChipBackground == null || tagChipOutline == null
            || tagChipIcon == null || tagChipLabel == null)
            return;
        tagChip.gameObject.SetActive(true);

        Color tagColor = SynergyPresentation.GetColor(trait.requiredTag);
        tagChipBackground.color = new Color(tagColor.r * 0.18f, tagColor.g * 0.18f, tagColor.b * 0.18f, 0.94f);
        tagChipOutline.effectColor = new Color(tagColor.r, tagColor.g, tagColor.b, 0.72f);
        tagChipIcon.sprite = SynergyPresentation.GetIcon(trait.requiredTag);
        tagChipIcon.color = tagColor;
        tagChipLabel.text = SynergyPresentation.GetDisplayName(trait.requiredTag);
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
