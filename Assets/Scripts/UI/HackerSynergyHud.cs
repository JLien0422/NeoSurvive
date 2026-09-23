using System.Collections.Generic;
using System.Text;
using NeoSurvive.Weapon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HackerSynergyHud : MonoBehaviour
{
    private static readonly SynergyTag[] Tags =
    {
        SynergyTag.Shooting,
        SynergyTag.Area,
        SynergyTag.Disruption,
        SynergyTag.Amplification,
        SynergyTag.Deployment
    };

    private WeaponManager weaponManager;
    private TextMeshProUGUI text;

    private void Awake()
    {
        weaponManager = GetComponent<WeaponManager>();
        CreateHud();
    }

    private void OnEnable()
    {
        WeaponManager.OnWeaponChanged += Refresh;
    }

    private void OnDisable()
    {
        WeaponManager.OnWeaponChanged -= Refresh;
    }

    private void Start()
    {
        Refresh(weaponManager != null ? weaponManager.activeWeapons : null);
    }

    private void CreateHud()
    {
        GameObject canvasObject = new GameObject("HackerSynergyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject panelObject = new GameObject("SynergyStackPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(24f, 0f);
        panelRect.sizeDelta = new Vector2(250f, 170f);
        panelObject.GetComponent<Image>().color = new Color(0.02f, 0.08f, 0.12f, 0.9f);

        GameObject textObject = new GameObject("StackText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panelObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 12f);
        textRect.offsetMax = new Vector2(-14f, -12f);

        text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 20f;
        text.alignment = TextAlignmentOptions.TopLeft;
    }

    private void Refresh(List<WeaponBase> activeWeapons)
    {
        if (weaponManager == null || text == null)
            return;

        StringBuilder builder = new StringBuilder("<b>시너지 스택</b>");
        foreach (SynergyTag tag in Tags)
        {
            int current = weaponManager.GetSynergyStack(tag);
            int maximum = weaponManager.GetSynergyMaximumStack(tag);
            string state = maximum > 0 && current >= maximum ? " <color=#7CFF9A>활성</color>" : string.Empty;
            builder.Append('\n').Append(GetDisplayName(tag)).Append("  ").Append(current).Append(" / ").Append(maximum).Append(state);
        }

        text.text = builder.ToString();
    }

    public static string GetDisplayName(SynergyTag tag)
    {
        switch (tag)
        {
            case SynergyTag.Shooting: return "사격";
            case SynergyTag.Area: return "광역";
            case SynergyTag.Disruption: return "교란";
            case SynergyTag.Amplification: return "증폭";
            case SynergyTag.Deployment: return "설치";
            default: return tag.ToString();
        }
    }
}
