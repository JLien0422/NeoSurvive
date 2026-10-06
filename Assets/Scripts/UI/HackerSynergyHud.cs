using System.Collections.Generic;
using NeoSurvive.Weapon;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private sealed class WeaponSlot
    {
        public GameObject Root;
        public Image Background;
        public Image Icon;
        public Outline Border;
        public Outline Glow;
    }

    private sealed class SynergyRow
    {
        public RectTransform Rect;
        public Image Background;
        public Outline Outline;
        public Image IconBackground;
        public Text NameText;
        public Text StackText;
        public readonly List<WeaponSlot> WeaponSlots = new List<WeaponSlot>();
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

    private const float PanelWidth = 540f;
    private const float HiddenX = -522f;
    private const float OpenX = 24f;
    private const float RevealAreaWidth = 40f;
    private const float SlideSpeed = 12f;
    private const float RowHeight = 42f;
    private const float HeaderHeight = 56f;

    private readonly Dictionary<SynergyTag, SynergyRow> rows = new Dictionary<SynergyTag, SynergyRow>();
    private readonly List<SynergyTag> visibleTags = new List<SynergyTag>();
    private readonly List<WeaponSlot> hoverSlots = new List<WeaponSlot>();

    private WeaponManager weaponManager;
    private Canvas hudCanvas;
    private RectTransform panelRect;
    private RectTransform hoverPanelRect;
    private Text hoverTitle;
    private Font koreanFont;
    private GameObject ownedCanvasObject;
    private bool isPanelOpen;

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

        bool openedThisFrame = false;
        if (!isPanelOpen && Input.mousePosition.x <= RevealAreaWidth)
        {
            isPanelOpen = true;
            openedThisFrame = true;
        }

        if (isPanelOpen && !openedThisFrame && Input.GetMouseButtonDown(0) && !IsPointerInsidePanel(Input.mousePosition))
            ClosePanel();

        float targetX = isPanelOpen ? OpenX : HiddenX;
        Vector2 position = panelRect.anchoredPosition;
        position.x = Mathf.Lerp(position.x, targetX, 1f - Mathf.Exp(-SlideSpeed * Time.unscaledDeltaTime));
        panelRect.anchoredPosition = position;

        if (hoverPanelRect != null && hoverPanelRect.gameObject.activeSelf)
        {
            float pulse = 0.45f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4.5f));
            foreach (WeaponSlot slot in hoverSlots)
            {
                if (slot.Root.activeSelf && slot.Glow.enabled)
                {
                    Color color = slot.Glow.effectColor;
                    color.a = pulse;
                    slot.Glow.effectColor = color;
                }
            }
        }
    }

    private void CreateHud()
    {
        koreanFont = SynergyPresentation.GetKoreanFont();

        hudCanvas = FindHudCanvas();
        if (hudCanvas == null)
        {
            ownedCanvasObject = new GameObject("SynergyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            hudCanvas = ownedCanvasObject.GetComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.overrideSorting = true;
            hudCanvas.sortingOrder = 100;

            CanvasScaler scaler = ownedCanvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        GameObject panelObject = new GameObject("SynergyPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        panelObject.transform.SetParent(hudCanvas.transform, false);

        panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(HiddenX, 0f);
        panelRect.sizeDelta = new Vector2(PanelWidth, HeaderHeight + Tags.Length * RowHeight);

        panelObject.GetComponent<Image>().color = new Color(0.015f, 0.035f, 0.055f, 0.96f);
        Outline panelOutline = panelObject.GetComponent<Outline>();
        panelOutline.effectColor = new Color(0.08f, 0.75f, 0.9f, 0.65f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        Text title = CreateText("Title", panelObject.transform, 25, TextAnchor.MiddleLeft);
        SetRect(title.rectTransform, new Vector2(18f, -10f), new Vector2(488f, 36f));
        title.text = "시너지";
        title.fontStyle = FontStyle.Bold;
        title.color = new Color(0.55f, 0.95f, 1f);

        CreateCloseButton(panelObject.transform);

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
        contentRect.anchoredPosition = new Vector2(0f, -HeaderHeight);
        contentRect.sizeDelta = new Vector2(PanelWidth, Tags.Length * RowHeight);

        foreach (SynergyTag tag in Tags)
            rows.Add(tag, CreateRow(contentRect, tag));

        CreateWeaponHoverPanel(panelObject.transform);
    }

    private void CreateCloseButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-12f, -10f);
        rect.sizeDelta = new Vector2(38f, 34f);

        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color(0.1f, 0.18f, 0.24f, 0.96f);

        Outline outline = buttonObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.25f, 0.85f, 1f, 0.75f);
        outline.effectDistance = new Vector2(1f, -1f);

        Text label = CreateText("Label", buttonObject.transform, 22, TextAnchor.MiddleCenter);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        label.text = "×";
        label.color = new Color(0.72f, 0.95f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(ClosePanel);
    }

    private bool IsPointerInsidePanel(Vector2 screenPosition)
    {
        Camera eventCamera = hudCanvas != null && hudCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? hudCanvas.worldCamera
            : null;

        if (panelRect != null && RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPosition, eventCamera))
            return true;

        return hoverPanelRect != null && hoverPanelRect.gameObject.activeSelf &&
               RectTransformUtility.RectangleContainsScreenPoint(hoverPanelRect, screenPosition, eventCamera);
    }

    private void ClosePanel()
    {
        isPanelOpen = false;
        HideWeaponHover();
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
        row.Rect.sizeDelta = new Vector2(PanelWidth, RowHeight);

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(rowObject.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(6f, 0f);
        iconRect.sizeDelta = new Vector2(32f, 32f);
        row.IconBackground = iconObject.GetComponent<Image>();
        row.IconBackground.sprite = SynergyPresentation.GetIcon(tag);
        row.IconBackground.preserveAspect = true;
        row.IconBackground.color = GetTagColor(tag);

        row.NameText = CreateText("Name", rowObject.transform, 19, TextAnchor.MiddleLeft);
        SetRect(row.NameText.rectTransform, new Vector2(46f, 0f), new Vector2(102f, RowHeight));
        row.NameText.text = GetDisplayName(tag);

        row.StackText = CreateText("Stack", rowObject.transform, 18, TextAnchor.MiddleRight);
        SetRect(row.StackText.rectTransform, new Vector2(150f, 0f), new Vector2(56f, RowHeight));
        row.StackText.fontStyle = FontStyle.Bold;

        for (int index = 0; index < 5; index++)
            row.WeaponSlots.Add(CreateInlineWeaponSlot(rowObject.transform, index));

        SynergyRowHoverTarget hoverTarget = rowObject.AddComponent<SynergyRowHoverTarget>();
        hoverTarget.Configure(this, tag);

        rowObject.SetActive(false);
        return row;
    }

    private static WeaponSlot CreateInlineWeaponSlot(Transform parent, int index)
    {
        GameObject root = new GameObject($"InlineWeapon{index + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        root.transform.SetParent(parent, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(218f + index * 56f, 0f);
        rect.sizeDelta = new Vector2(46f, 34f);

        WeaponSlot slot = new WeaponSlot
        {
            Root = root,
            Background = root.GetComponent<Image>(),
            Border = root.GetComponent<Outline>()
        };
        slot.Background.raycastTarget = false;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(root.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(3f, 2f);
        iconRect.offsetMax = new Vector2(-3f, -2f);
        slot.Icon = iconObject.GetComponent<Image>();
        slot.Icon.preserveAspect = true;
        slot.Icon.raycastTarget = false;
        return slot;
    }

    private void CreateWeaponHoverPanel(Transform parent)
    {
        GameObject panel = new GameObject("SynergyWeaponHover", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(CanvasGroup));
        panel.transform.SetParent(parent, false);
        hoverPanelRect = panel.GetComponent<RectTransform>();
        hoverPanelRect.anchorMin = new Vector2(0f, 0.5f);
        hoverPanelRect.anchorMax = new Vector2(0f, 0.5f);
        hoverPanelRect.pivot = new Vector2(0f, 0.5f);
        hoverPanelRect.anchoredPosition = new Vector2(PanelWidth + 10f, 0f);
        hoverPanelRect.sizeDelta = new Vector2(376f, 112f);

        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.012f, 0.03f, 0.05f, 0.98f);
        background.raycastTarget = false;

        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = new Color(0.12f, 0.72f, 0.88f, 0.75f);
        outline.effectDistance = new Vector2(2f, -2f);

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        hoverTitle = CreateText("Title", panel.transform, 17, TextAnchor.MiddleLeft);
        SetRect(hoverTitle.rectTransform, new Vector2(12f, -6f), new Vector2(350f, 26f));

        for (int index = 0; index < 5; index++)
            hoverSlots.Add(CreateWeaponSlot(panel.transform, index));

        panel.SetActive(false);
    }

    private WeaponSlot CreateWeaponSlot(Transform parent, int index)
    {
        GameObject root = new GameObject($"WeaponSlot{index + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        root.transform.SetParent(parent, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(12f + index * 70f, -36f);
        rect.sizeDelta = new Vector2(62f, 62f);

        WeaponSlot slot = new WeaponSlot
        {
            Root = root,
            Background = root.GetComponent<Image>(),
            Border = root.GetComponent<Outline>(),
            Glow = root.AddComponent<Outline>()
        };
        slot.Background.raycastTarget = false;

        GameObject iconObject = new GameObject("WeaponIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(root.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(5f, 5f);
        iconRect.offsetMax = new Vector2(-5f, -5f);
        slot.Icon = iconObject.GetComponent<Image>();
        slot.Icon.preserveAspect = true;
        slot.Icon.raycastTarget = false;
        return slot;
    }

    internal void ShowWeaponHover(SynergyTag tag)
    {
        if (hoverPanelRect == null || weaponManager == null)
            return;

        Color tagColor = GetTagColor(tag);
        hoverTitle.text = $"{GetDisplayName(tag)}  ·  보유 무기";
        hoverTitle.color = Color.Lerp(tagColor, Color.white, 0.35f);

        List<WeaponBase> matchingWeapons = GetMatchingWeapons(tag);

        for (int index = 0; index < hoverSlots.Count; index++)
        {
            WeaponSlot slot = hoverSlots[index];
            bool hasWeaponData = index < matchingWeapons.Count;
            slot.Root.SetActive(true);

            if (!hasWeaponData)
            {
                slot.Icon.sprite = null;
                slot.Icon.enabled = false;
                slot.Background.color = new Color(0.025f, 0.045f, 0.065f, 0.9f);
                slot.Border.effectColor = new Color(tagColor.r, tagColor.g, tagColor.b, 0.18f);
                slot.Border.effectDistance = new Vector2(1f, -1f);
                slot.Glow.enabled = false;
                continue;
            }

            WeaponBase weapon = matchingWeapons[index];
            bool owned = weaponManager.activeWeapons != null && weaponManager.activeWeapons.Contains(weapon);
            slot.Icon.sprite = weapon.weaponIcon;
            slot.Icon.enabled = weapon.weaponIcon != null;
            slot.Icon.color = owned ? Color.white : new Color(0.35f, 0.4f, 0.46f, 0.42f);
            slot.Background.color = owned
                ? new Color(tagColor.r * 0.2f, tagColor.g * 0.2f, tagColor.b * 0.2f, 0.98f)
                : new Color(0.025f, 0.045f, 0.065f, 0.92f);
            slot.Border.effectColor = owned
                ? new Color(tagColor.r, tagColor.g, tagColor.b, 1f)
                : new Color(tagColor.r, tagColor.g, tagColor.b, 0.28f);
            slot.Border.effectDistance = owned ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
            slot.Glow.enabled = owned;
            slot.Glow.effectColor = new Color(tagColor.r, tagColor.g, tagColor.b, 0.65f);
            slot.Glow.effectDistance = new Vector2(5f, -5f);
        }

        hoverPanelRect.gameObject.SetActive(true);
    }

    internal void HideWeaponHover()
    {
        if (hoverPanelRect != null)
            hoverPanelRect.gameObject.SetActive(false);
    }

    private List<WeaponBase> GetMatchingWeapons(SynergyTag tag)
    {
        List<WeaponBase> matchingWeapons = new List<WeaponBase>();
        if (weaponManager == null || weaponManager.allWeaponDatas == null)
            return matchingWeapons;

        foreach (WeaponBase weapon in weaponManager.allWeaponDatas)
        {
            if (weapon != null && weapon.synergyTags != null && weapon.synergyTags.Contains(tag) && !matchingWeapons.Contains(weapon))
                matchingWeapons.Add(weapon);
        }

        return matchingWeapons;
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

        int visibleCount = visibleTags.Count;
        float contentHeight = visibleCount * RowHeight;
        panelRect.sizeDelta = new Vector2(PanelWidth, HeaderHeight + contentHeight);

        RectTransform rowsRect = rows.Count > 0 ? rows[Tags[0]].Rect.parent as RectTransform : null;
        if (rowsRect != null)
            rowsRect.sizeDelta = new Vector2(PanelWidth, contentHeight);

        for (int index = 0; index < visibleTags.Count; index++)
        {
            SynergyTag tag = visibleTags[index];
            int current = weaponManager.GetSynergyStack(tag);
            int maximum = weaponManager.GetSynergyMaximumStack(tag);
            bool isActive = weaponManager.IsSynergyComplete(tag);
            SynergyRow row = rows[tag];

            row.Rect.gameObject.SetActive(true);
            row.Rect.sizeDelta = new Vector2(PanelWidth, RowHeight);
            row.Rect.anchoredPosition = new Vector2(0f, -index * RowHeight);
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
            RefreshInlineWeaponSlots(row, tag);
        }
    }

    private void RefreshInlineWeaponSlots(SynergyRow row, SynergyTag tag)
    {
        List<WeaponBase> matchingWeapons = GetMatchingWeapons(tag);
        Color tagColor = GetTagColor(tag);

        for (int index = 0; index < row.WeaponSlots.Count; index++)
        {
            WeaponSlot slot = row.WeaponSlots[index];
            if (index >= matchingWeapons.Count)
            {
                slot.Icon.sprite = null;
                slot.Icon.enabled = false;
                slot.Background.color = new Color(0.02f, 0.04f, 0.06f, 0.72f);
                slot.Border.effectColor = new Color(tagColor.r, tagColor.g, tagColor.b, 0.14f);
                slot.Border.effectDistance = new Vector2(1f, -1f);
                continue;
            }

            WeaponBase weapon = matchingWeapons[index];
            bool owned = weaponManager.activeWeapons != null && weaponManager.activeWeapons.Contains(weapon);
            slot.Icon.sprite = weapon.weaponIcon;
            slot.Icon.enabled = weapon.weaponIcon != null;
            slot.Icon.color = owned ? Color.white : new Color(0.3f, 0.34f, 0.4f, 0.38f);
            slot.Background.color = owned
                ? new Color(tagColor.r * 0.22f, tagColor.g * 0.22f, tagColor.b * 0.22f, 0.96f)
                : new Color(0.025f, 0.045f, 0.065f, 0.82f);
            slot.Border.effectColor = owned
                ? new Color(tagColor.r, tagColor.g, tagColor.b, 0.9f)
                : new Color(tagColor.r, tagColor.g, tagColor.b, 0.22f);
            slot.Border.effectDistance = owned ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
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

public sealed class SynergyRowHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private HackerSynergyHud owner;
    private SynergyTag synergyTag;

    public void Configure(HackerSynergyHud targetOwner, SynergyTag targetTag)
    {
        owner = targetOwner;
        synergyTag = targetTag;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        owner?.ShowWeaponHover(synergyTag);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        owner?.HideWeaponHover();
    }
}
