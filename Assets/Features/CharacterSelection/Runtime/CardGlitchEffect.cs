using System.Collections.Generic;
using Coffee.UIEffects;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UIEffectDemo
{
    [DisallowMultipleComponent]
    public sealed class CardGlitchEffect : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private Image characterImage;
        [SerializeField] private RectTransform viewport;

        [Header("Random Mosaic Slices")]
        [SerializeField, Range(3, 12)] private int sliceCount = 7;
        [SerializeField] private Vector2 sliceHeightRange = new Vector2(10f, 52f);
        [SerializeField] private Vector2 horizontalJitterRange = new Vector2(8f, 26f);
        [SerializeField] private Vector2 pixelationRange = new Vector2(0.38f, 0.82f);
        [SerializeField, Range(0.05f, 0.5f)] private float duration = 0.24f;

        [Header("Chromatic Split / Data Noise")]
        [SerializeField, Range(0f, 12f)] private float chromaticOffset = 4.5f;
        [SerializeField, Range(0f, 1f)] private float chromaticChance = 0.72f;
        [SerializeField, Range(0, 5)] private int dataBarCount = 2;
        [SerializeField, Range(0f, 1f)] private float verticalTearChance = 0.35f;

        [Header("Selected Loop")]
        [SerializeField, Min(0.25f)] private float loopInterval = 1.05f;
        [SerializeField, Range(0.1f, 1f)] private float minSpread = 0.3f;
        [SerializeField, Range(0.1f, 1f)] private float maxSpread = 0.96f;

        private readonly List<GameObject> activeSlices = new List<GameObject>();
        private Sequence loopSequence;
        private RectTransform mosaicLayer;
        private float spread;

        public void Configure(Image source, RectTransform maskViewport)
        {
            characterImage = source;
            viewport = maskViewport;
        }

        public void Play()
        {
            if (!characterImage || !characterImage.sprite || !viewport) return;

            Vector2 viewportSize = viewport.rect.size;
            for (int i = 0; i < sliceCount; i++)
                CreateSlice(viewportSize, i);
        }

        public void SetLooping(bool looping, bool immediate = false)
        {
            loopSequence?.Kill();
            loopSequence = null;

            if (!looping)
            {
                ClearSlices();
                ReleaseMosaicLayer();
                return;
            }

            spread = minSpread;
            loopSequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            if (!immediate)
                loopSequence.AppendCallback(Play);
            loopSequence.AppendInterval(loopInterval);
            loopSequence.AppendCallback(() =>
            {
                Play();
                spread = spread >= maxSpread ? minSpread : Mathf.Min(maxSpread, spread + 0.14f);
            });
            loopSequence.SetLoops(-1, LoopType.Restart);
        }

        private void CreateSlice(Vector2 viewportSize, int index)
        {
            EnsureMosaicLayer();

            float height = Random.Range(sliceHeightRange.x, sliceHeightRange.y);
            float halfSpread = viewportSize.y * 0.5f * spread;
            float y = Random.Range(-halfSpread, halfSpread);
            float width = Random.Range(viewportSize.x * 0.38f, viewportSize.x * 0.96f);
            float x = Random.Range(-(viewportSize.x - width) * 0.5f, (viewportSize.x - width) * 0.5f);

            GameObject slice = new GameObject("GlitchSlice_" + index, typeof(RectTransform), typeof(RectMask2D));
            slice.layer = gameObject.layer;
            slice.transform.SetParent(mosaicLayer, false);
            RectTransform sliceRect = slice.GetComponent<RectTransform>();
            sliceRect.anchorMin = sliceRect.anchorMax = sliceRect.pivot = new Vector2(0.5f, 0.5f);
            sliceRect.anchoredPosition = new Vector2(x, y);
            sliceRect.sizeDelta = new Vector2(width, height);

            Image clone = CreatePixelClone(sliceRect, viewportSize, -sliceRect.anchoredPosition,
                index % 2 == 0 ? new Color(0.55f, 1f, 1f, 0.5f) : new Color(1f, 0.38f, 0.3f, 0.46f),
                "PixelatedCharacter");

            if (Random.value <= chromaticChance)
                CreateChromaticGhost(sliceRect, viewportSize, index);
            CreateDataBars(sliceRect, width, height, index);

            activeSlices.Add(slice);
            float jitter = Random.Range(horizontalJitterRange.x, horizontalJitterRange.y)
                           * (index % 2 == 0 ? 1f : -1f);
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(slice);
            sequence.Append(sliceRect.DOAnchorPosX(x + jitter, duration * 0.24f).SetEase(Ease.OutExpo));
            sequence.Append(sliceRect.DOAnchorPosX(x - jitter * 0.55f, duration * 0.24f).SetEase(Ease.InOutQuad));
            sequence.Append(sliceRect.DOAnchorPosX(x + jitter * 0.18f, duration * 0.18f));
            sequence.Append(clone.DOFade(0f, duration * 0.34f));
            sequence.OnComplete(() => DestroySlice(slice));
        }

        private Image CreatePixelClone(RectTransform parent, Vector2 viewportSize, Vector2 position,
            Color color, string objectName)
        {
            Image clone = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(UIEffect)).GetComponent<Image>();
            clone.gameObject.layer = gameObject.layer;
            clone.transform.SetParent(parent, false);
            clone.sprite = characterImage.sprite;
            clone.type = characterImage.type;
            clone.preserveAspect = characterImage.preserveAspect;
            clone.raycastTarget = false;
            clone.color = color;

            RectTransform cloneRect = clone.rectTransform;
            cloneRect.anchorMin = cloneRect.anchorMax = cloneRect.pivot = new Vector2(0.5f, 0.5f);
            cloneRect.sizeDelta = viewportSize;
            cloneRect.anchoredPosition = position;

            UIEffect mosaic = clone.GetComponent<UIEffect>();
            mosaic.effectMode = EffectMode.Pixel;
            mosaic.effectFactor = Random.Range(pixelationRange.x, pixelationRange.y);
            mosaic.colorMode = ColorMode.Multiply;
            mosaic.colorFactor = 0f;
            return clone;
        }

        private void CreateChromaticGhost(RectTransform sliceRect, Vector2 viewportSize, int index)
        {
            float direction = index % 2 == 0 ? 1f : -1f;
            Color color = index % 2 == 0
                ? new Color(0.05f, 0.9f, 1f, 0.32f)
                : new Color(1f, 0.08f, 0.18f, 0.28f);
            Image ghost = CreatePixelClone(sliceRect, viewportSize,
                -sliceRect.anchoredPosition + Vector2.right * chromaticOffset * direction,
                color, index % 2 == 0 ? "ChromaticGhost_Cyan" : "ChromaticGhost_Red");
            ghost.DOFade(0f, duration * 0.72f).SetUpdate(true).SetTarget(sliceRect.gameObject);
        }

        private void CreateDataBars(RectTransform sliceRect, float width, float height, int index)
        {
            for (int i = 0; i < dataBarCount; i++)
            {
                Image bar = new GameObject("DataNoiseBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                    .GetComponent<Image>();
                bar.transform.SetParent(sliceRect, false);
                bar.raycastTarget = false;
                bar.color = (index + i) % 2 == 0
                    ? new Color(0.05f, 0.9f, 1f, 0.46f)
                    : new Color(1f, 0.08f, 0.22f, 0.38f);

                RectTransform rect = bar.rectTransform;
                bool vertical = Random.value <= verticalTearChance;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = vertical
                    ? new Vector2(Random.Range(1f, 3f), height * Random.Range(0.55f, 1f))
                    : new Vector2(width * Random.Range(0.18f, 0.62f), Random.Range(1f, 3f));
                rect.anchoredPosition = new Vector2(
                    Random.Range(-width * 0.4f, width * 0.4f),
                    Random.Range(-height * 0.35f, height * 0.35f));
                bar.DOFade(0f, duration * Random.Range(0.45f, 0.85f)).SetUpdate(true)
                    .SetTarget(sliceRect.gameObject);
            }
        }

        private void EnsureMosaicLayer()
        {
            if (mosaicLayer) return;

            Transform existing = viewport.Find("[FX] MosaicDistortion");
            if (existing)
            {
                mosaicLayer = existing as RectTransform;
                return;
            }

            mosaicLayer = new GameObject("[FX] MosaicDistortion", typeof(RectTransform))
                .GetComponent<RectTransform>();
            mosaicLayer.SetParent(viewport, false);
            mosaicLayer.anchorMin = Vector2.zero;
            mosaicLayer.anchorMax = Vector2.one;
            mosaicLayer.offsetMin = Vector2.zero;
            mosaicLayer.offsetMax = Vector2.zero;
            mosaicLayer.SetSiblingIndex(Mathf.Max(0, characterImage.transform.GetSiblingIndex()));
        }

        private void DestroySlice(GameObject slice)
        {
            activeSlices.Remove(slice);
            if (!slice) return;
            DOTween.Kill(slice);
            Destroy(slice);
        }

        private void ClearSlices()
        {
            for (int i = activeSlices.Count - 1; i >= 0; i--)
            {
                GameObject slice = activeSlices[i];
                if (!slice) continue;
                DOTween.Kill(slice);
                Destroy(slice);
            }
            activeSlices.Clear();
        }

        private void ReleaseMosaicLayer()
        {
            if (!mosaicLayer) return;
            Destroy(mosaicLayer.gameObject);
            mosaicLayer = null;
        }

        private void OnDisable()
        {
            loopSequence?.Kill();
            loopSequence = null;
            ClearSlices();
            ReleaseMosaicLayer();
        }

        private void OnDestroy()
        {
            loopSequence?.Kill();
            loopSequence = null;
            ClearSlices();
            ReleaseMosaicLayer();
        }
    }
}
