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

    [Header("등급별 테두리")]
    [SerializeField] private Image[] borderImages = new Image[0];
    [SerializeField] private Color silverColor = new Color(210f / 255f, 210f / 255f, 210f / 255f, 1f);
    [SerializeField] private Color goldColor = new Color(1f, 237f / 255f, 0f, 1f);
    [SerializeField] private Color platinumColor = new Color(0f, 1f, 210f / 255f, 1f);

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
        clickButton.onClick.RemoveAllListeners();
        clickButton.onClick.AddListener(() => onSelected?.Invoke());
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
