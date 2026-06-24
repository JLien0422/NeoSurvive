using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 사이보그 해킹 수비 존 바닥 비주얼
/// - 반투명 전체 원 (존 가이드)
/// - 불투명 원 시계방향 감소 (남은 시간)
/// - LineRenderer 테두리 (적 수에 따라 색 변경)
/// </summary>
public class CyborgZoneTimerVisual : MonoBehaviour
{
    [System.Serializable]
    public struct Settings
    {
        public float radius;
        public float borderWidth;
        public float backgroundAlpha;
        public float timerFillAlpha;
        public int sortingOrder;
        public string sortingLayerName;
    }

    private const string DefaultSortingLayerName = "Default";
    private const int MinVisibleSortingOrder = -9; // MapManager.DecoTileSortingOrder(-9) 위
    private const int CircleTextureSize = 128;

    private LineRenderer borderLine;
    private Image timerFillImage;

    private static Sprite sharedCircleSprite;

    public static CyborgZoneTimerVisual Create(Vector3 center, Settings settings)
    {
        GameObject root = new GameObject("CyborgHackingZone");
        root.transform.position = center;

        var visual = root.AddComponent<CyborgZoneTimerVisual>();
        visual.Build(settings);
        return visual;
    }

    private void Build(Settings settings)
    {
        float radius = Mathf.Max(0.1f, settings.radius);
        float diameter = radius * 2f;
        Sprite circle = GetOrCreateCircleSprite();
        string sortingLayerName = string.IsNullOrWhiteSpace(settings.sortingLayerName)
            ? DefaultSortingLayerName
            : settings.sortingLayerName;
        int sortingOrder = Mathf.Max(settings.sortingOrder, MinVisibleSortingOrder);

        GameObject canvasObj = new GameObject("ZoneCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = Vector3.zero;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingLayerName = sortingLayerName;
        canvas.sortingOrder = sortingOrder;

        canvasObj.AddComponent<GraphicRaycaster>().enabled = false;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(diameter, diameter);
        canvasRect.localScale = Vector3.one;
        canvasRect.localRotation = Quaternion.identity;

        GameObject backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(canvasRect, false);
        Image backgroundImage = backgroundObj.AddComponent<Image>();
        backgroundImage.sprite = circle;
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.raycastTarget = false;
        backgroundImage.color = new Color(0f, 0.85f, 0.95f, settings.backgroundAlpha);
        StretchRect(backgroundObj.GetComponent<RectTransform>());

        GameObject timerObj = new GameObject("TimerFill");
        timerObj.transform.SetParent(canvasRect, false);
        timerFillImage = timerObj.AddComponent<Image>();
        timerFillImage.sprite = circle;
        timerFillImage.type = Image.Type.Filled;
        timerFillImage.fillMethod = Image.FillMethod.Radial360;
        timerFillImage.fillOrigin = (int)Image.Origin360.Top;
        timerFillImage.fillClockwise = true;
        timerFillImage.raycastTarget = false;
        timerFillImage.color = new Color(0f, 0.85f, 0.95f, settings.timerFillAlpha);
        timerFillImage.fillAmount = 1f;
        StretchRect(timerObj.GetComponent<RectTransform>());

        borderLine = gameObject.AddComponent<LineRenderer>();
        borderLine.useWorldSpace = false;
        borderLine.loop = true;
        borderLine.startWidth = Mathf.Max(0.01f, settings.borderWidth);
        borderLine.endWidth = Mathf.Max(0.01f, settings.borderWidth);
        borderLine.material = new Material(Shader.Find("Sprites/Default"));
        borderLine.sortingLayerName = sortingLayerName;
        borderLine.sortingOrder = sortingOrder;
        borderLine.startColor = Color.cyan;
        borderLine.endColor = Color.cyan;

        int segments = 48;
        borderLine.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            borderLine.SetPosition(
                i,
                new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f)
            );
        }
    }

    private static void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    public void SetTimeRemaining(float normalized)
    {
        if (timerFillImage == null)
            return;

        timerFillImage.fillAmount = Mathf.Clamp01(normalized);
    }

    public void SetBorderColor(Color color)
    {
        if (borderLine == null)
            return;

        borderLine.startColor = color;
        borderLine.endColor = color;
    }

    private static Sprite GetOrCreateCircleSprite()
    {
        if (sharedCircleSprite != null)
            return sharedCircleSprite;

        Texture2D texture = new Texture2D(
            CircleTextureSize,
            CircleTextureSize,
            TextureFormat.RGBA32,
            false
        );
        texture.filterMode = FilterMode.Bilinear;

        float radius = CircleTextureSize * 0.5f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < CircleTextureSize; y++)
        {
            for (int x = 0; x < CircleTextureSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius - dist);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();

        sharedCircleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, CircleTextureSize, CircleTextureSize),
            new Vector2(0.5f, 0.5f),
            CircleTextureSize
        );

        return sharedCircleSprite;
    }
}
