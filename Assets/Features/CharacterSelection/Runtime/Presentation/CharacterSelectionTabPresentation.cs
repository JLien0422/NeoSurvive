using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace NeoSurvive.UI.CharacterSelection
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class CharacterSelectionTabPresentation : MonoBehaviour
    {
        [Header("Entry Animation")]
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.28f;
        [SerializeField, Min(0.05f)] private float panelDuration = 0.48f;
        [SerializeField, Min(0.05f)] private float cardDuration = 0.42f;
        [SerializeField, Min(0f)] private float cardDelay = 0.10f;
        [SerializeField] private float cardSlideDistance = 34f;

        [Header("Background Accent")]
        [SerializeField] private Color hackerAmbientColor = new Color(0.05f, 0.72f, 1f, 0.12f);
        [SerializeField] private Color cyborgAmbientColor = new Color(1f, 0.08f, 0.04f, 0.10f);

        private RectTransform rootRect;
        private RectTransform backPanel;
        private RectTransform title;
        private RectTransform cardStage;
        private CanvasGroup rootGroup;
        private CanvasGroup ambientGroup;
        private Vector2 titlePosition;
        private Vector2 cardStagePosition;
        private Vector3 backPanelScale = Vector3.one;
        private Sequence entrySequence;
        private Sprite ambientSprite;
        private Texture2D ambientTexture;
        private bool initialized;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (initialized)
                return;

            rootRect = transform as RectTransform;
            rootGroup = GetComponent<CanvasGroup>();
            if (!rootGroup)
                rootGroup = gameObject.AddComponent<CanvasGroup>();
            backPanel = FindChild("BackPanel") as RectTransform;
            title = FindChild("Title") as RectTransform;
            cardStage = FindChild("CardStage") as RectTransform;

            if (backPanel) backPanelScale = backPanel.localScale;
            if (title) titlePosition = title.anchoredPosition;
            if (cardStage) cardStagePosition = cardStage.anchoredPosition;

            EnsureBackdrop();
            initialized = true;
        }

        private void OnEnable()
        {
            PlayEntry();
        }

        public void PlayEntry()
        {
            Initialize();
            if (!rootGroup)
            {
                Debug.LogError("[CharacterSelection] 진입 연출용 CanvasGroup을 준비하지 못했습니다.", this);
                return;
            }

            entrySequence?.Kill();
            DOTween.Kill(this);

            rootGroup.alpha = 0f;
            rootGroup.blocksRaycasts = false;
            if (rootRect) rootRect.localScale = Vector3.one * 0.985f;
            if (ambientGroup) ambientGroup.alpha = 0f;
            if (backPanel) backPanel.localScale = backPanelScale * 0.96f;
            if (title) title.anchoredPosition = titlePosition + Vector2.up * 18f;
            if (cardStage) cardStage.anchoredPosition = cardStagePosition + Vector2.down * cardSlideDistance;

            entrySequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            entrySequence.Append(rootGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad));
            if (rootRect)
                entrySequence.Join(rootRect.DOScale(1f, panelDuration).SetEase(Ease.OutCubic));
            if (ambientGroup)
                entrySequence.Join(ambientGroup.DOFade(1f, panelDuration * 1.25f).SetEase(Ease.OutSine));
            if (backPanel)
                entrySequence.Join(backPanel.DOScale(backPanelScale, panelDuration).SetEase(Ease.OutBack, 1.25f));
            if (title)
                entrySequence.Join(title.DOAnchorPos(titlePosition, panelDuration * 0.82f).SetEase(Ease.OutCubic));
            if (cardStage)
                entrySequence.Insert(cardDelay,
                    cardStage.DOAnchorPos(cardStagePosition, cardDuration).SetEase(Ease.OutCubic));
            entrySequence.OnComplete(() => rootGroup.blocksRaycasts = true);
        }

        private void EnsureBackdrop()
        {
            Transform previous = transform.Find("RuntimeBackdropAccents");
            if (previous)
            {
                ambientGroup = previous.GetComponent<CanvasGroup>();
                return;
            }

            RectTransform layer = new GameObject("RuntimeBackdropAccents", typeof(RectTransform), typeof(CanvasGroup))
                .GetComponent<RectTransform>();
            layer.SetParent(transform, false);
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = layer.offsetMax = Vector2.zero;
            layer.SetSiblingIndex(Mathf.Min(1, transform.childCount - 1));
            ambientGroup = layer.GetComponent<CanvasGroup>();
            ambientGroup.blocksRaycasts = false;
            ambientGroup.interactable = false;

            ambientSprite = CreateAmbientSprite();
            CreateAmbientGlow(layer, "HackerAmbient", new Vector2(-430f, -45f), hackerAmbientColor);
            CreateAmbientGlow(layer, "CyborgAmbient", new Vector2(430f, -45f), cyborgAmbientColor);

            Image horizon = CreateImage("CenterHorizon", layer, new Color(0.35f, 0.8f, 1f, 0.065f));
            RectTransform horizonRect = horizon.rectTransform;
            horizonRect.anchorMin = new Vector2(0.08f, 0.5f);
            horizonRect.anchorMax = new Vector2(0.92f, 0.5f);
            horizonRect.sizeDelta = new Vector2(0f, 1f);
            horizonRect.anchoredPosition = new Vector2(0f, -16f);
        }

        private void CreateAmbientGlow(Transform parent, string objectName, Vector2 position, Color color)
        {
            Image image = CreateImage(objectName, parent, color);
            image.sprite = ambientSprite;
            image.preserveAspect = true;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(820f, 760f);
        }

        private static Image CreateImage(string objectName, Transform parent, Color color)
        {
            Image image = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                .GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Sprite CreateAmbientSprite()
        {
            const int resolution = 64;
            ambientTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                name = "CharacterSelectionAmbientGlow",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[resolution * resolution];
            Vector2 center = Vector2.one * (resolution - 1) * 0.5f;
            float radius = resolution * 0.5f;
            for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Pow(1f - Mathf.Clamp01(distance), 2.4f);
                pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
            }

            ambientTexture.SetPixels(pixels);
            ambientTexture.Apply(false, false);
            return Sprite.Create(ambientTexture, new Rect(0f, 0f, resolution, resolution), Vector2.one * 0.5f);
        }

        private Transform FindChild(string objectName)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
                if (child.name == objectName)
                    return child;
            return null;
        }

        private void OnDisable()
        {
            entrySequence?.Kill();
            entrySequence = null;
            DOTween.Kill(this);
        }

        private void OnDestroy()
        {
            entrySequence?.Kill();
            if (ambientSprite) Destroy(ambientSprite);
            if (ambientTexture) Destroy(ambientTexture);
        }
    }
}
