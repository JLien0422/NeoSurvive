using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player-only hit feedback. Screen shake is reinforced by a short red edge
/// flash and a body tint, making the cause of the shake immediately readable.
/// </summary>
public static class PlayerDamageFeedback
{
  public static void Play(Character target, PlayerHealthVisualState state, float damageRatio)
  {
    if (target == null) return;

    PlayerDamageOverlay.Show(state, damageRatio);
    PlayerDamageTint tint = target.GetComponent<PlayerDamageTint>();
    if (tint == null)
      tint = target.gameObject.AddComponent<PlayerDamageTint>();
    tint.Play(state);
  }
}

[DisallowMultipleComponent]
public sealed class PlayerDamageOverlay : MonoBehaviour
{
  private static PlayerDamageOverlay instance;

  private CanvasGroup canvasGroup;
  private Image edgeImage;
  private Texture2D edgeTexture;
  private Sprite edgeSprite;
  private Coroutine flashRoutine;

  public static void Show(PlayerHealthVisualState state, float damageRatio)
  {
    EnsureInstance();
    instance.PlayFlash(state, damageRatio);
  }

  private static void EnsureInstance()
  {
    if (instance != null) return;

    GameObject root = new GameObject(
      "[PlayerFeedback] DamageOverlay",
      typeof(RectTransform),
      typeof(Canvas),
      typeof(CanvasScaler),
      typeof(CanvasGroup));
    instance = root.AddComponent<PlayerDamageOverlay>();
    root.layer = LayerMask.NameToLayer("UI");

    Canvas canvas = root.GetComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = 32000;

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
    CreateEdgeVisual();
  }

  private void CreateEdgeVisual()
  {
    edgeTexture = CreateEdgeTexture(128);
    edgeSprite = Sprite.Create(edgeTexture, new Rect(0f, 0f, 128f, 128f), new Vector2(0.5f, 0.5f));

    edgeImage = new GameObject("DamageEdgeFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
      .GetComponent<Image>();
    edgeImage.transform.SetParent(transform, false);
    edgeImage.sprite = edgeSprite;
    edgeImage.color = new Color(1f, 0.035f, 0.02f, 1f);
    edgeImage.raycastTarget = false;

    RectTransform rect = edgeImage.rectTransform;
    rect.anchorMin = Vector2.zero;
    rect.anchorMax = Vector2.one;
    rect.offsetMin = Vector2.zero;
    rect.offsetMax = Vector2.zero;
  }

  private void PlayFlash(PlayerHealthVisualState state, float damageRatio)
  {
    if (flashRoutine != null)
      StopCoroutine(flashRoutine);
    flashRoutine = StartCoroutine(FlashRoutine(state, damageRatio));
  }

  private IEnumerator FlashRoutine(PlayerHealthVisualState state, float damageRatio)
  {
    float stateAlpha = state == PlayerHealthVisualState.Critical ? 0.42f
      : state == PlayerHealthVisualState.Danger ? 0.34f
      : state == PlayerHealthVisualState.Caution ? 0.27f
      : 0.21f;
    float peakAlpha = Mathf.Clamp(stateAlpha + Mathf.Clamp01(damageRatio * 2.5f) * 0.12f, 0.18f, 0.52f);
    float holdDuration = state == PlayerHealthVisualState.Critical ? 0.055f : 0.035f;
    float fadeDuration = state == PlayerHealthVisualState.Critical ? 0.28f : 0.2f;

    canvasGroup.alpha = peakAlpha;
    yield return new WaitForSecondsRealtime(holdDuration);

    float elapsed = 0f;
    while (elapsed < fadeDuration)
    {
      elapsed += Time.unscaledDeltaTime;
      float t = Mathf.Clamp01(elapsed / fadeDuration);
      canvasGroup.alpha = Mathf.Lerp(peakAlpha, 0f, t * t);
      yield return null;
    }

    canvasGroup.alpha = 0f;
    flashRoutine = null;
  }

  private static Texture2D CreateEdgeTexture(int resolution)
  {
    Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
    {
      name = "RuntimeDamageEdge",
      wrapMode = TextureWrapMode.Clamp,
      filterMode = FilterMode.Bilinear
    };
    Color[] pixels = new Color[resolution * resolution];
    for (int y = 0; y < resolution; y++)
    for (int x = 0; x < resolution; x++)
    {
      float nx = Mathf.Abs((x / (resolution - 1f) - 0.5f) * 2f);
      float ny = Mathf.Abs((y / (resolution - 1f) - 0.5f) * 2f);
      float edge = Mathf.Max(nx, ny);
      float alpha = Mathf.Pow(Mathf.InverseLerp(0.28f, 1f, edge), 2.1f);
      pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
    }
    texture.SetPixels(pixels);
    texture.Apply(false, false);
    return texture;
  }

  private void OnDestroy()
  {
    if (instance == this) instance = null;
    if (edgeSprite != null) Destroy(edgeSprite);
    if (edgeTexture != null) Destroy(edgeTexture);
  }
}

[DisallowMultipleComponent]
public sealed class PlayerDamageTint : MonoBehaviour
{
  private SpriteRenderer[] renderers;
  private Color[] originalColors;
  private Coroutine tintRoutine;

  public void Play(PlayerHealthVisualState state)
  {
    CacheRenderers();
    if (tintRoutine != null)
    {
      StopCoroutine(tintRoutine);
      RestoreColors();
    }
    tintRoutine = StartCoroutine(TintRoutine(state));
  }

  private void CacheRenderers()
  {
    if (renderers == null || renderers.Length == 0)
      renderers = GetComponentsInChildren<SpriteRenderer>(true);
    if (originalColors == null || originalColors.Length != renderers.Length)
      originalColors = new Color[renderers.Length];
    for (int i = 0; i < renderers.Length; i++)
      if (renderers[i] != null)
        originalColors[i] = renderers[i].color;
  }

  private IEnumerator TintRoutine(PlayerHealthVisualState state)
  {
    Color hitColor = state == PlayerHealthVisualState.Critical
      ? new Color(1f, 0.12f, 0.08f, 1f)
      : new Color(1f, 0.42f, 0.36f, 1f);
    for (int i = 0; i < renderers.Length; i++)
      if (renderers[i] != null)
        renderers[i].color = hitColor;

    yield return new WaitForSecondsRealtime(0.065f);
    RestoreColors();
    tintRoutine = null;
  }

  private void RestoreColors()
  {
    if (renderers == null || originalColors == null) return;
    for (int i = 0; i < renderers.Length; i++)
      if (renderers[i] != null)
        renderers[i].color = originalColors[i];
  }

  private void OnDisable()
  {
    if (tintRoutine != null) StopCoroutine(tintRoutine);
    tintRoutine = null;
    RestoreColors();
  }
}
