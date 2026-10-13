using Coffee.UIEffects;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIEffectDemo
{
    [DisallowMultipleComponent]
    public sealed class CardView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private RectTransform cardTransform;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image frame;
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image characterImage;
        [SerializeField] private UIShiny frameScanner;
        [SerializeField] private CharacterUIGlow glow;
        [SerializeField] private CharacterSilhouetteLight silhouetteLight;
        [SerializeField] private CardGlitchEffect glitchEffect;
        [SerializeField] private CardCyberpunkAura cardAura;
        [SerializeField] private CardSelectionFeedback selectionFeedback;
        [SerializeField] private CardSelectionController selectionController;
        [SerializeField] private int cardIndex;
        [SerializeField] private Color selectedFrameColor = new Color(0.18f, 0.92f, 1f, 1f);
        [SerializeField] private Color idleFrameColor = new Color(0.18f, 0.25f, 0.34f, 1f);
        [SerializeField] private Color selectedPanelColor = new Color(0.015f, 0.09f, 0.13f, 1f);
        [SerializeField] private Color idlePanelColor = new Color(0.01f, 0.012f, 0.016f, 1f);
        [SerializeField] private Color selectedOuterGlowColor = new Color(0.1f, 0.75f, 1f, 0.24f);

        [Header("Selected Hover")]
        [SerializeField, Min(0f)] private float hoverDistance = 4f;
        [SerializeField, Min(0.2f)] private float hoverHalfCycle = 1.35f;

        private Vector2 authoredPosition;
        private Tween hoverTween;
        private Tween confirmTween;
        private bool hasAppliedState;
        private bool currentSelected;
        private float currentTargetScale = 1f;

        private void Awake()
        {
            authoredPosition = cardTransform.anchoredPosition;

            if (!cardBackground)
                cardBackground = transform.Find("ArtworkViewport/CardBackground")?.GetComponent<Image>();

            if (!frameScanner && frame)
                frameScanner = frame.GetComponent<UIShiny>() ?? frame.gameObject.AddComponent<UIShiny>();

            if (frameScanner)
            {
                frameScanner.width = 0.075f;
                frameScanner.rotation = -18f;
                frameScanner.softness = 0.35f;
                frameScanner.brightness = 1f;
                frameScanner.gloss = 0.82f;
                frameScanner.effectPlayer.duration = 1.4f;
                frameScanner.effectPlayer.loop = true;
                frameScanner.effectPlayer.play = true;
            }


            if (glow)
            {
                Color source = glow.CurrentGlowColor;
                selectedFrameColor = source;
                selectedOuterGlowColor = new Color(source.r, source.g, source.b, 0.18f);
                selectedPanelColor = idlePanelColor;
                glow.SetIntensity(1.18f);
            }

            if (!glitchEffect)
                glitchEffect = GetComponent<CardGlitchEffect>() ?? gameObject.AddComponent<CardGlitchEffect>();
            if (!characterImage && glow)
                characterImage = glow.GetComponent<Image>();
            if (glitchEffect && characterImage)
                glitchEffect.Configure(characterImage,
                    transform.Find("ArtworkViewport") as RectTransform);

            if (!silhouetteLight)
                silhouetteLight = GetComponent<CharacterSilhouetteLight>() ?? gameObject.AddComponent<CharacterSilhouetteLight>();
            if (silhouetteLight && characterImage)
                silhouetteLight.Configure(characterImage, selectedFrameColor);

            if (!cardAura)
                cardAura = GetComponent<CardCyberpunkAura>() ?? gameObject.AddComponent<CardCyberpunkAura>();
            cardAura.Configure(cardTransform, selectedFrameColor);

            if (!selectionFeedback)
                selectionFeedback = GetComponent<CardSelectionFeedback>() ?? gameObject.AddComponent<CardSelectionFeedback>();
            selectionFeedback.Configure(cardTransform, transform.Find("ArtworkViewport") as RectTransform, selectedFrameColor);
        }

        private void Start()
        {
            if (cardIndex == 0 && glow)
                glow.SetPointEnabled("CoreGlow", false);
        }

        public void ApplyState(float selectedLift, float scale, float alpha, bool selected,
            float duration, Ease ease, bool immediate)
        {
            if (hasAppliedState && currentSelected == selected && !immediate)
                return;

            hasAppliedState = true;
            currentSelected = selected;
            currentTargetScale = scale;

            float tweenDuration = immediate ? 0f : duration;
            confirmTween?.Kill();
            confirmTween = null;
            cardTransform.DOKill();
            hoverTween?.Kill();
            hoverTween = null;
            canvasGroup.DOKill();
            frame.DOKill();
            if (cardBackground) cardBackground.DOKill();

            Vector2 targetPosition = authoredPosition + (selected ? Vector2.up * selectedLift : Vector2.zero);
            cardTransform.DOAnchorPos(targetPosition, tweenDuration).SetEase(ease).SetUpdate(true);
            cardTransform.DOScale(scale, tweenDuration).SetEase(ease).SetUpdate(true);
            canvasGroup.DOFade(alpha, tweenDuration).SetEase(ease).SetUpdate(true);
            frame.DOColor(selected ? selectedFrameColor : idleFrameColor, tweenDuration).SetUpdate(true);
            if (cardBackground)
                cardBackground.DOColor(selected ? selectedPanelColor : idlePanelColor, tweenDuration).SetUpdate(true);

            if (frameScanner)
            {
                frameScanner.effectPlayer.loop = true;
                frameScanner.effectPlayer.play = true;
                if (selected && !immediate)
                    frameScanner.Play(true);
            }

            glow.SetSelected(selected);
            if (silhouetteLight)
                silhouetteLight.SetSelected(selected, immediate);
            if (cardAura)
                cardAura.SetSelected(selected);
            if (selectionFeedback)
                selectionFeedback.SetSelected(selected, immediate);
            if (glitchEffect)
                glitchEffect.SetLooping(selected, immediate);

            if (selected)
            {
                float delay = immediate ? 0f : tweenDuration;
                hoverTween = cardTransform
                    .DOAnchorPosY(targetPosition.y + hoverDistance, hoverHalfCycle)
                    .SetDelay(delay)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetTarget(this);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                selectionController.SelectCard(cardIndex);
            }
        }

        public void PlayConfirmFeedback()
        {
            confirmTween?.Kill();
            cardTransform.localScale = Vector3.one * currentTargetScale;
            confirmTween = cardTransform
                .DOPunchScale(Vector3.one * 0.08f, 0.24f, 6, 0.5f)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    cardTransform.localScale = Vector3.one * currentTargetScale;
                    confirmTween = null;
                });
            glow.PlayBoost();
            if (frameScanner) frameScanner.Play(true);
            if (glitchEffect) glitchEffect.Play();
        }

        private void OnDisable()
        {
            cardTransform?.DOKill();
            canvasGroup?.DOKill();
            frame?.DOKill();
            cardBackground?.DOKill();
            hoverTween?.Kill();
            hoverTween = null;
            confirmTween?.Kill();
            confirmTween = null;
        }
    }

    [DisallowMultipleComponent]
    public sealed class CardSelectionFeedback : MonoBehaviour
    {
        [SerializeField] private RectTransform cardRect;
        [SerializeField] private RectTransform viewport;
        [SerializeField, ColorUsage(true, true)] private Color accent = Color.cyan;
        [SerializeField, Min(0.25f)] private float scanDuration = 1.55f;

        private RectTransform feedbackRoot;
        private CanvasGroup feedbackGroup;
        private Image scanline;
        private Sequence scanSequence;
        private Tween visibilityTween;

        public void Configure(RectTransform card, RectTransform artworkViewport, Color color)
        {
            cardRect = card;
            viewport = artworkViewport;
            accent = color;
            EnsureVisuals();
        }

        public void SetSelected(bool selected, bool immediate)
        {
            EnsureVisuals();
            if (!feedbackRoot) return;

            KillSequences();
            feedbackRoot.gameObject.SetActive(true);
            if (scanline) scanline.gameObject.SetActive(selected);
            float targetAlpha = selected ? 1f : 0f;
            if (immediate)
                feedbackGroup.alpha = targetAlpha;
            else
                visibilityTween = feedbackGroup.DOFade(targetAlpha, selected ? 0.16f : 0.12f)
                    .SetUpdate(true).SetTarget(this);

            if (!selected)
            {
                if (immediate) feedbackRoot.gameObject.SetActive(false);
                else visibilityTween.OnComplete(() => feedbackRoot.gameObject.SetActive(false));
                return;
            }

            StartLoops();
        }

        private void EnsureVisuals()
        {
            if (!cardRect) cardRect = transform as RectTransform;
            if (!cardRect || feedbackRoot) return;

            feedbackRoot = new GameObject("SelectionFeedback", typeof(RectTransform), typeof(CanvasGroup))
                .GetComponent<RectTransform>();
            feedbackRoot.SetParent(cardRect, false);
            feedbackRoot.anchorMin = Vector2.zero;
            feedbackRoot.anchorMax = Vector2.one;
            feedbackRoot.offsetMin = feedbackRoot.offsetMax = Vector2.zero;
            feedbackRoot.SetAsLastSibling();
            feedbackGroup = feedbackRoot.GetComponent<CanvasGroup>();
            feedbackGroup.blocksRaycasts = false;
            feedbackGroup.interactable = false;

            if (viewport)
            {
                scanline = CreateImage("SelectionScanline", viewport, WithAlpha(accent, 0.38f));
                RectTransform lineRect = scanline.rectTransform;
                lineRect.anchorMin = new Vector2(0f, 0.5f);
                lineRect.anchorMax = new Vector2(1f, 0.5f);
                lineRect.pivot = new Vector2(0.5f, 0.5f);
                lineRect.anchoredPosition = Vector2.zero;
                lineRect.sizeDelta = new Vector2(0f, 2f);
                scanline.gameObject.SetActive(false);
            }

            feedbackRoot.gameObject.SetActive(false);
        }

        private void StartLoops()
        {
            feedbackGroup.alpha = 1f;

            if (!scanline || !viewport) return;
            float halfHeight = viewport.rect.height * 0.5f;
            scanline.rectTransform.anchoredPosition = new Vector2(0f, -halfHeight);
            scanSequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            scanSequence.Append(scanline.rectTransform.DOAnchorPosY(halfHeight, scanDuration).SetEase(Ease.Linear));
            scanSequence.AppendInterval(0.25f);
            scanSequence.SetLoops(-1, LoopType.Restart);
        }

        private void KillSequences()
        {
            scanSequence?.Kill();
            visibilityTween?.Kill();
            scanSequence = null;
            visibilityTween = null;
        }

        private void OnDisable() => KillSequences();

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                .GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }

    [DisallowMultipleComponent]
    public sealed class CharacterSilhouetteLight : MonoBehaviour
    {
        [SerializeField] private Image targetImage;
        [SerializeField, ColorUsage(true, true)] private Color reflectionColor = Color.cyan;
        [SerializeField, Range(0f, 1f)] private float idleAlpha = 0.025f;
        [SerializeField, Range(0f, 1f)] private float selectedAlpha = 0.19f;
        [SerializeField, Range(0f, 2f)] private float blur = 0.72f;
        [SerializeField, Range(0f, 8f)] private float outlineSpread = 2.4f;
        [SerializeField, Min(0.05f)] private float transitionDuration = 0.22f;

        private UIShadow shadow;
        private Tween colorTween;

        public void Configure(Image image, Color color)
        {
            targetImage = image;
            reflectionColor = color;
            EnsureEffect();
        }

        public void SetSelected(bool selected, bool immediate)
        {
            EnsureEffect();
            if (!shadow) return;

            colorTween?.Kill();
            Color target = WithAlpha(reflectionColor, selected ? selectedAlpha : idleAlpha);
            if (immediate)
            {
                shadow.effectColor = target;
                return;
            }

            colorTween = DOTween.To(() => shadow.effectColor, value => shadow.effectColor = value,
                target, transitionDuration).SetUpdate(true).SetTarget(this);
        }

        private void EnsureEffect()
        {
            if (!targetImage) return;
            UIEffect effect = targetImage.GetComponent<UIEffect>() ?? targetImage.gameObject.AddComponent<UIEffect>();
            effect.effectMode = EffectMode.None;
            effect.blurMode = BlurMode.FastBlur;
            effect.blurFactor = blur;

            shadow = targetImage.GetComponent<UIShadow>() ?? targetImage.gameObject.AddComponent<UIShadow>();
            shadow.style = ShadowStyle.Outline8;
            shadow.effectDistance = Vector2.one * outlineSpread;
            shadow.blurFactor = blur;
            shadow.useGraphicAlpha = true;
            if (shadow.effectColor.a <= 0f)
                shadow.effectColor = WithAlpha(reflectionColor, idleAlpha);
        }

        private void OnDisable()
        {
            colorTween?.Kill();
            colorTween = null;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }

    [DisallowMultipleComponent]
    public sealed class CardCyberpunkAura : MonoBehaviour
    {
        [Header("Card Aura")]
        [SerializeField] private RectTransform cardRect;
        [SerializeField, ColorUsage(true, true)] private Color primaryColor = new Color(0.05f, 0.85f, 1f, 1f);
        [SerializeField, ColorUsage(true, true)] private Color secondaryColor = new Color(1f, 0.04f, 0.42f, 1f);
        [SerializeField] private Vector2 primaryExpansion = new Vector2(155f, 190f);
        [SerializeField] private Vector2 secondaryExpansion = new Vector2(108f, 142f);
        [SerializeField] private Vector2 primaryOffset = new Vector2(-24f, -10f);
        [SerializeField] private Vector2 secondaryOffset = new Vector2(30f, 14f);
        [SerializeField, Range(0f, 1f)] private float idleAlpha = 0.035f;
        [SerializeField, Range(0f, 1f)] private float selectedPrimaryAlpha = 0.27f;
        [SerializeField, Range(0f, 1f)] private float selectedSecondaryAlpha = 0.13f;

        [Header("Aura Loop")]
        [SerializeField, Min(0.25f)] private float pulseHalfCycle = 1.9f;
        [SerializeField, Range(1f, 1.15f)] private float pulseScale = 1.035f;

        private readonly Image[] auraImages = new Image[2];
        private readonly Image[] railImages = new Image[2];
        private Sequence auraLoop;
        private Sprite auraSprite;
        private Texture2D auraTexture;
        private bool selected;

        public void Configure(RectTransform source, Color accent)
        {
            cardRect = source;
            primaryColor = accent;
            // 해커(시안)는 냉색 반사광만, 사이보그(적색)는 온색 반사광만 사용한다.
            secondaryColor = accent.b > accent.r
                ? new Color(0.08f, 0.42f, 1f, 1f)
                : new Color(1f, 0.16f, 0.04f, 1f);
            EnsureAura();
        }

        public void SetSelected(bool value)
        {
            if (selected == value && auraLoop != null && auraLoop.IsActive()) return;
            selected = value;
            RestartLoop();
        }

        private void Awake() => EnsureAura();

        private void EnsureAura()
        {
            if (!cardRect) cardRect = transform as RectTransform;
            if (!cardRect) return;
            if (!auraSprite) CreateAuraSprite();

            auraImages[0] = CreateAuraLayer("CardAura_Cyan", primaryColor, primaryExpansion, primaryOffset, 0);
            auraImages[1] = CreateAuraLayer("CardAura_Magenta", secondaryColor, secondaryExpansion, secondaryOffset, 1);
            railImages[0] = CreateRail("CardAura_RailPrimary", new Vector2(0f, 0.68f),
                new Vector2(-15f, 0f), new Vector2(3f, 88f), 2);
            railImages[1] = CreateRail("CardAura_RailSecondary", new Vector2(1f, 0.3f),
                new Vector2(15f, 0f), new Vector2(2f, 52f), 3);
        }

        private Image CreateAuraLayer(string layerName, Color color, Vector2 expansion, Vector2 offset, int sibling)
        {
            Transform existing = transform.Find(layerName);
            Image image = existing ? existing.GetComponent<Image>() : null;
            if (!image)
            {
                image = new GameObject(layerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                    .GetComponent<Image>();
                image.transform.SetParent(transform, false);
            }

            image.transform.SetSiblingIndex(sibling);
            image.sprite = auraSprite;
            image.raycastTarget = false;
            image.color = WithAlpha(color, idleAlpha);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(-expansion.x * 0.5f, -expansion.y * 0.5f) + offset;
            rect.offsetMax = new Vector2(expansion.x * 0.5f, expansion.y * 0.5f) + offset;
            rect.localScale = Vector3.one;
            return image;
        }

        private Image CreateRail(string layerName, Vector2 anchor, Vector2 offset, Vector2 size, int sibling)
        {
            Transform existing = transform.Find(layerName);
            Image image = existing ? existing.GetComponent<Image>() : null;
            if (!image)
            {
                image = new GameObject(layerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                    .GetComponent<Image>();
                image.transform.SetParent(transform, false);
            }

            image.transform.SetSiblingIndex(sibling);
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            return image;
        }

        private void RestartLoop()
        {
            auraLoop?.Kill();
            auraLoop = null;
            EnsureAura();

            float primaryPeak = selected ? selectedPrimaryAlpha : idleAlpha;
            float secondaryPeak = selected ? selectedSecondaryAlpha : idleAlpha * 0.7f;
            SetLayer(auraImages[0], primaryColor, primaryPeak, Vector3.one);
            SetLayer(auraImages[1], secondaryColor, secondaryPeak, Vector3.one);
            SetLayer(railImages[0], BrightColor(primaryColor), selected ? 0.94f : 0.12f, Vector3.one);
            SetLayer(railImages[1], BrightColor(secondaryColor), selected ? 0.7f : 0.06f, Vector3.one);

            if (!isActiveAndEnabled) return;
            auraLoop = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            auraLoop.Join(auraImages[0].DOFade(primaryPeak * 0.62f, pulseHalfCycle).SetEase(Ease.InOutSine));
            auraLoop.Join(auraImages[1].DOFade(secondaryPeak * 0.48f, pulseHalfCycle).SetEase(Ease.InOutSine));
            auraLoop.Join(auraImages[0].rectTransform.DOScale(pulseScale, pulseHalfCycle).SetEase(Ease.InOutSine));
            auraLoop.Join(auraImages[1].rectTransform.DOScale(pulseScale * 0.985f, pulseHalfCycle).SetEase(Ease.InOutSine));
            auraLoop.Join(railImages[0].DOFade(selected ? 0.48f : 0.06f, pulseHalfCycle).SetEase(Ease.InOutSine));
            auraLoop.Join(railImages[1].DOFade(selected ? 0.26f : 0.03f, pulseHalfCycle).SetEase(Ease.InOutSine));
            auraLoop.SetLoops(-1, LoopType.Yoyo);
        }

        private void CreateAuraSprite()
        {
            const int resolution = 128;
            auraTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "RuntimeCardAura"
            };
            Color[] pixels = new Color[resolution * resolution];
            for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
            {
                float nx = (x / (resolution - 1f) - 0.5f) * 2f;
                float ny = (y / (resolution - 1f) - 0.5f) * 2f;
                float radialDistance = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - radialDistance), 2.15f);
                pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
            }
            auraTexture.SetPixels(pixels);
            auraTexture.Apply(false, false);
            auraSprite = Sprite.Create(auraTexture, new Rect(0f, 0f, resolution, resolution), new Vector2(0.5f, 0.5f));
        }

        private static void SetLayer(Image image, Color color, float alpha, Vector3 scale)
        {
            if (!image) return;
            image.DOKill();
            image.rectTransform.DOKill();
            image.color = WithAlpha(color, alpha);
            image.rectTransform.localScale = scale;
        }

        private void OnDisable()
        {
            auraLoop?.Kill();
            auraLoop = null;
        }

        private void OnDestroy()
        {
            if (auraSprite) Destroy(auraSprite);
            if (auraTexture) Destroy(auraTexture);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static Color BrightColor(Color color)
        {
            return Color.Lerp(color, Color.white, 0.58f);
        }
    }
}
