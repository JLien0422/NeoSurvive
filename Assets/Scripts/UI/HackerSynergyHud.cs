using System.Collections.Generic;
using NeoSurvive.Weapon;
using UnityEngine;
using UnityEngine.UI;

public static class SynergyPresentation
{
    private static Sprite placeholderIcon;
    private static Font koreanFont;

    public static Sprite GetIcon(SynergyTag tag)
    {
        if (placeholderIcon == null)
            placeholderIcon = CreateDiamondIcon();
        return placeholderIcon;
    }

    public static Font GetKoreanFont()
    {
        if (koreanFont != null)
            return koreanFont;

        koreanFont = Resources.Load<Font>("fonts/Stardust/PF스타더스트 3.0 ExtraBold");
        if (koreanFont == null)
            koreanFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕" }, 20);
        return koreanFont != null ? koreanFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static Sprite CreateDiamondIcon()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "SynergyDiamondIcon",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Mathf.Abs(x - center.x) + Mathf.Abs(y - center.y);
                bool fill = distance <= 10.5f;
                bool border = distance > 10.5f && distance <= 13.5f;
                pixels[y * size + x] = fill
                    ? Color.white
                    : border ? new Color(1f, 1f, 1f, 0.55f) : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
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
            case SynergyTag.Melee: return "근접";
            case SynergyTag.Ranged: return "원거리";
            case SynergyTag.Physical: return "물리";
            case SynergyTag.Energy: return "에너지";
            case SynergyTag.ForceField: return "역장";
            default: return tag.ToString();
        }
    }

    public static Color GetColor(SynergyTag tag)
    {
        switch (tag)
        {
            case SynergyTag.Shooting: return new Color(0.18f, 0.75f, 1f);
            case SynergyTag.Area: return new Color(0.45f, 0.35f, 1f);
            case SynergyTag.Disruption: return new Color(0.95f, 0.25f, 0.65f);
            case SynergyTag.Amplification: return new Color(1f, 0.68f, 0.18f);
            case SynergyTag.Deployment: return new Color(0.25f, 0.85f, 0.55f);
            case SynergyTag.Melee: return new Color(1f, 0.32f, 0.24f);
            case SynergyTag.Ranged: return new Color(0.2f, 0.72f, 1f);
            case SynergyTag.Physical: return new Color(0.85f, 0.62f, 0.28f);
            case SynergyTag.Energy: return new Color(0.42f, 0.92f, 1f);
            case SynergyTag.ForceField: return new Color(0.68f, 0.38f, 1f);
            default: return Color.gray;
        }
    }
}

public sealed class HackerSynergyHud : MonoBehaviour
{
    private sealed class SynergyRow
    {
        public RectTransform Rect;
        public Image Background;
        public Outline Outline;
        public Image IconBackground;
        public Text NameText;
        public Text StackText;
    }

    private static readonly SynergyTag[] Tags =
    {
        SynergyTag.Shooting,
        SynergyTag.Area,
        SynergyTag.Disruption,
        SynergyTag.Amplification,
        SynergyTag.Deployment,
        SynergyTag.Melee,
        SynergyTag.Ranged,
        SynergyTag.Physical,
        SynergyTag.Energy,
        SynergyTag.ForceField
    };

    private const float PanelWidth = 300f;
    private const float HiddenX = -282f;
    private const float OpenX = 24f;
    private const float RevealAreaWidth = 40f;
    private const float SlideSpeed = 12f;
    private const float RowHeight = 34f;
    private const float RowGap = 6f;

    private readonly Dictionary<SynergyTag, SynergyRow> rows = new Dictionary<SynergyTag, SynergyRow>();
    private readonly List<SynergyTag> visibleTags = new List<SynergyTag>();

    private WeaponManager weaponManager;
    private RectTransform panelRect;
    private Font koreanFont;
    private GameObject ownedCanvasObject;

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

    private void OnDestroy()
    {
        if (ownedCanvasObject != null)
            Destroy(ownedCanvasObject);
    }

    private void Start()
    {
        Refresh(weaponManager != null ? weaponManager.activeWeapons : null);
    }

    private void Update()
    {
        if (panelRect == null)
            return;

        float targetX = Input.mousePosition.x <= RevealAreaWidth ? OpenX : HiddenX;
        Vector2 position = panelRect.anchoredPosition;
        position.x = Mathf.Lerp(position.x, targetX, 1f - Mathf.Exp(-SlideSpeed * Time.unscaledDeltaTime));
        panelRect.anchoredPosition = position;
    }

    private void CreateHud()
    {
        koreanFont = SynergyPresentation.GetKoreanFont();

        Canvas canvas = FindHudCanvas();
        if (canvas == null)
        {
            ownedCanvasObject = new GameObject("SynergyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = ownedCanvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = ownedCanvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        GameObject panelObject = new GameObject("SynergyPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        panelObject.transform.SetParent(canvas.transform, false);

        panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(HiddenX, 0f);
        panelRect.sizeDelta = new Vector2(PanelWidth, 60f + Tags.Length * (RowHeight + RowGap));

        panelObject.GetComponent<Image>().color = new Color(0.015f, 0.035f, 0.055f, 0.96f);
        Outline panelOutline = panelObject.GetComponent<Outline>();
        panelOutline.effectColor = new Color(0.08f, 0.75f, 0.9f, 0.65f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        Text title = CreateText("Title", panelObject.transform, 22, TextAnchor.MiddleLeft);
        SetRect(title.rectTransform, new Vector2(16f, -10f), new Vector2(250f, 30f));
        title.text = "시너지";
        title.fontStyle = FontStyle.Bold;
        title.color = new Color(0.55f, 0.95f, 1f);

        Text handle = CreateText("Handle", panelObject.transform, 20, TextAnchor.MiddleCenter);
        RectTransform handleRect = handle.rectTransform;
        handleRect.anchorMin = new Vector2(1f, 0.5f);
        handleRect.anchorMax = new Vector2(1f, 0.5f);
        handleRect.pivot = new Vector2(1f, 0.5f);
        handleRect.anchoredPosition = new Vector2(-1f, 0f);
        handleRect.sizeDelta = new Vector2(18f, 70f);
        handle.text = ">";
        handle.color = new Color(0.35f, 0.9f, 1f);

        GameObject contentObject = new GameObject("Rows", typeof(RectTransform));
        contentObject.transform.SetParent(panelObject.transform, false);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(0f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.anchoredPosition = new Vector2(16f, -48f);
        contentRect.sizeDelta = new Vector2(268f, 194f);

        foreach (SynergyTag tag in Tags)
            rows.Add(tag, CreateRow(contentRect, tag));
    }

    private SynergyRow CreateRow(RectTransform parent, SynergyTag tag)
    {
        GameObject rowObject = new GameObject(tag + "Row", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        rowObject.transform.SetParent(parent, false);

        SynergyRow row = new SynergyRow
        {
            Rect = rowObject.GetComponent<RectTransform>(),
            Background = rowObject.GetComponent<Image>(),
            Outline = rowObject.GetComponent<Outline>()
        };
        row.Rect.anchorMin = new Vector2(0f, 1f);
        row.Rect.anchorMax = new Vector2(0f, 1f);
        row.Rect.pivot = new Vector2(0f, 1f);
        row.Rect.sizeDelta = new Vector2(268f, RowHeight);

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(rowObject.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(4f, 0f);
        iconRect.sizeDelta = new Vector2(28f, 28f);
        row.IconBackground = iconObject.GetComponent<Image>();
        row.IconBackground.sprite = SynergyPresentation.GetIcon(tag);
        row.IconBackground.preserveAspect = true;
        row.IconBackground.color = GetTagColor(tag);

        row.NameText = CreateText("Name", rowObject.transform, 17, TextAnchor.MiddleLeft);
        SetRect(row.NameText.rectTransform, new Vector2(42f, 0f), new Vector2(135f, RowHeight));
        row.NameText.text = GetDisplayName(tag);

        row.StackText = CreateText("Stack", rowObject.transform, 17, TextAnchor.MiddleRight);
        SetRect(row.StackText.rectTransform, new Vector2(182f, 0f), new Vector2(72f, RowHeight));
        row.StackText.fontStyle = FontStyle.Bold;

        rowObject.SetActive(false);
        return row;
    }

    private Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text result = textObject.GetComponent<Text>();
        result.font = koreanFont;
        result.fontSize = fontSize;
        result.alignment = alignment;
        result.color = Color.white;
        result.raycastTarget = false;
        return result;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static Canvas FindHudCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        Canvas best = null;
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || !canvas.isActiveAndEnabled || !canvas.isRootCanvas ||
                canvas.renderMode == RenderMode.WorldSpace)
                continue;

            if (best == null || canvas.sortingOrder > best.sortingOrder)
                best = canvas;
        }

        return best;
    }

    private void Refresh(List<WeaponBase> activeWeapons)
    {
        if (weaponManager == null)
            return;

        visibleTags.Clear();
        foreach (SynergyTag tag in Tags)
        {
            int maximum = weaponManager.GetSynergyMaximumStack(tag);
            if (maximum > 0)
                visibleTags.Add(tag);
            else
                rows[tag].Rect.gameObject.SetActive(false);
        }

        visibleTags.Sort(CompareTags);

        for (int index = 0; index < visibleTags.Count; index++)
        {
            SynergyTag tag = visibleTags[index];
            int current = weaponManager.GetSynergyStack(tag);
            int maximum = weaponManager.GetSynergyMaximumStack(tag);
            bool isActive = weaponManager.IsSynergyComplete(tag);
            SynergyRow row = rows[tag];

            row.Rect.gameObject.SetActive(true);
            row.Rect.anchoredPosition = new Vector2(0f, -index * (RowHeight + RowGap));
            row.Background.color = isActive
                ? new Color(0.03f, 0.28f, 0.34f, 0.98f)
                : new Color(0.05f, 0.09f, 0.13f, 0.94f);
            row.Outline.effectColor = isActive
                ? new Color(0.2f, 1f, 0.85f, 1f)
                : new Color(0.12f, 0.32f, 0.4f, 0.55f);
            row.Outline.effectDistance = isActive ? new Vector2(3f, -3f) : new Vector2(1f, -1f);
            row.IconBackground.color = isActive ? Color.Lerp(GetTagColor(tag), Color.white, 0.25f) : GetTagColor(tag);
            row.NameText.color = isActive ? new Color(0.75f, 1f, 0.9f) : Color.white;
            row.StackText.color = isActive ? new Color(0.3f, 1f, 0.75f) : new Color(0.7f, 0.88f, 0.95f);
            row.StackText.text = current + " / " + maximum;
        }
    }

    private int CompareTags(SynergyTag left, SynergyTag right)
    {
        bool leftActive = IsActive(left);
        bool rightActive = IsActive(right);
        if (leftActive != rightActive)
            return leftActive ? -1 : 1;

        return ((int)left).CompareTo((int)right);
    }

    private bool IsActive(SynergyTag tag)
    {
        return weaponManager.IsSynergyComplete(tag);
    }

    private static Color GetTagColor(SynergyTag tag)
    {
        return SynergyPresentation.GetColor(tag);
    }

    public static string GetDisplayName(SynergyTag tag)
    {
        return SynergyPresentation.GetDisplayName(tag);
    }
}
