using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Camera-space cyber-psycho warning. It builds a low-resolution mosaic,
/// analogue static and scanlines without touching character renderers.
/// </summary>
[DisallowMultipleComponent]
public sealed class PsychoCorruptionScreenEffect : MonoBehaviour
{
  private const int NoiseWidth = 64;
  private const int NoiseHeight = 36;
  private static PsychoCorruptionScreenEffect instance;

  private CanvasGroup canvasGroup;
  private RawImage noiseImage;
  private RawImage scanlineImage;
  private Image[] interferenceBars;
  private Texture2D noiseTexture;
  private Texture2D scanlineTexture;
  private Color32[] noisePixels;
  private float targetIntensity;
  private float intensity;
  private float nextNoiseUpdate;

  public static void SetWarningProgress(float progress)
  {
    EnsureInstance();
    instance.targetIntensity = Mathf.Clamp01(progress);
  }

  private static void EnsureInstance()
  {
    if (instance != null) return;

    GameObject root = new GameObject(
      "[PlayerFeedback] PsychoCorruptionScreen",
      typeof(RectTransform),
      typeof(Canvas),
      typeof(CanvasScaler),
      typeof(CanvasGroup));
    instance = root.AddComponent<PsychoCorruptionScreenEffect>();
    root.layer = LayerMask.NameToLayer("UI");

    Canvas canvas = root.GetComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = 31990;

    CanvasScaler scaler = root.GetComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1920f, 1080f);
  }

  private void Awake()
  {
    if (instance != null && instance != this)
    {
      Destroy(gameObject);
      return;
    }

    instance = this;
    canvasGroup = GetComponent<CanvasGroup>();
    canvasGroup.alpha = 0f;
    canvasGroup.blocksRaycasts = false;
    canvasGroup.interactable = false;
    CreateTextures();
    CreateVisuals();
  }

  private void CreateTextures()
  {
    noiseTexture = new Texture2D(NoiseWidth, NoiseHeight, TextureFormat.RGBA32, false)
    {
      name = "RuntimePsychoMosaic",
      filterMode = FilterMode.Point,
      wrapMode = TextureWrapMode.Repeat
    };
    noisePixels = new Color32[NoiseWidth * NoiseHeight];

    scanlineTexture = new Texture2D(2, 8, TextureFormat.RGBA32, false)
    {
      name = "RuntimePsychoScanlines",
      filterMode = FilterMode.Point,
      wrapMode = TextureWrapMode.Repeat
    };
    Color32 clear = new Color32(0, 0, 0, 0);
    Color32 line = new Color32(20, 220, 235, 105);
    for (int y = 0; y < 8; y++)
    for (int x = 0; x < 2; x++)
      scanlineTexture.SetPixel(x, y, y == 0 || y == 4 ? line : clear);
    scanlineTexture.Apply(false, false);
  }

  private void CreateVisuals()
  {
    noiseImage = CreateRawImage("MosaicStatic", noiseTexture, Color.white);
    scanlineImage = CreateRawImage("CRTScanlines", scanlineTexture, new Color(0.65f, 1f, 1f, 0.7f));
    scanlineImage.uvRect = new Rect(0f, 0f, 240f, 135f);

    interferenceBars = new Image[5];
    for (int i = 0; i < interferenceBars.Length; i++)
    {
      GameObject barObject = new GameObject("SignalTear" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
      barObject.transform.SetParent(transform, false);
      Image bar = barObject.GetComponent<Image>();
      bar.color = i % 2 == 0
        ? new Color(0.1f, 0.95f, 1f, 0.22f)
        : new Color(1f, 0.08f, 0.2f, 0.18f);
      bar.raycastTarget = false;
      interferenceBars[i] = bar;
    }
  }

  private RawImage CreateRawImage(string objectName, Texture texture, Color color)
  {
    GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
    imageObject.transform.SetParent(transform, false);
    RawImage image = imageObject.GetComponent<RawImage>();
    image.texture = texture;
    image.color = color;
    image.raycastTarget = false;
    RectTransform rect = image.rectTransform;
    rect.anchorMin = Vector2.zero;
    rect.anchorMax = Vector2.one;
    rect.offsetMin = Vector2.zero;
    rect.offsetMax = Vector2.zero;
    return image;
  }

  private void Update()
  {
    float speed = targetIntensity > intensity ? 4.5f : 7f;
    intensity = Mathf.MoveTowards(intensity, targetIntensity, Time.unscaledDeltaTime * speed);
    canvasGroup.alpha = Mathf.SmoothStep(0f, 0.72f, intensity);

    if (intensity <= 0.001f)
      return;

    float updateInterval = Mathf.Lerp(0.14f, 0.025f, intensity);
    if (Time.unscaledTime >= nextNoiseUpdate)
    {
      nextNoiseUpdate = Time.unscaledTime + updateInterval;
      UpdateNoise();
      UpdateInterferenceBars();
    }

    float horizontalJitter = Random.Range(-8f, 8f) * intensity;
    noiseImage.rectTransform.anchoredPosition = new Vector2(horizontalJitter, 0f);
    scanlineImage.uvRect = new Rect(0f, Time.unscaledTime * Mathf.Lerp(1.5f, 7f, intensity), 240f, 135f);
  }

  private void UpdateNoise()
  {
    byte alphaMaximum = (byte)Mathf.RoundToInt(Mathf.Lerp(18f, 145f, intensity));
    for (int i = 0; i < noisePixels.Length; i++)
    {
      byte value = (byte)Random.Range(15, 256);
      byte alpha = (byte)Random.Range(0, alphaMaximum + 1);
      if (Random.value < intensity * 0.12f)
        noisePixels[i] = Random.value > 0.5f
          ? new Color32(20, 230, 255, alpha)
          : new Color32(255, 20, 70, alpha);
      else
        noisePixels[i] = new Color32(value, value, value, alpha);
    }
    noiseTexture.SetPixels32(noisePixels);
    noiseTexture.Apply(false, false);
  }

  private void UpdateInterferenceBars()
  {
    for (int i = 0; i < interferenceBars.Length; i++)
    {
      RectTransform rect = interferenceBars[i].rectTransform;
      rect.anchorMin = new Vector2(0f, 0.5f);
      rect.anchorMax = new Vector2(1f, 0.5f);
      rect.pivot = new Vector2(0.5f, 0.5f);
      rect.anchoredPosition = new Vector2(Random.Range(-24f, 24f) * intensity, Random.Range(-520f, 520f));
      rect.sizeDelta = new Vector2(Random.Range(-80f, 140f), Random.Range(2f, 18f) * intensity);
      interferenceBars[i].enabled = Random.value < Mathf.Lerp(0.25f, 0.9f, intensity);
    }
  }

  private void OnDestroy()
  {
    if (instance == this) instance = null;
    if (noiseTexture != null) Destroy(noiseTexture);
    if (scanlineTexture != null) Destroy(scanlineTexture);
  }
}
