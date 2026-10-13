using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UIEffectDemo
{
    [DisallowMultipleComponent]
    public sealed class CardGlowController : MonoBehaviour
    {
        [Header("Glow Targets")]
        [SerializeField] private Image[] glowImages;

        [Header("Inspector Tuning")]
        [SerializeField, ColorUsage(true, true)] private Color glowColor = new Color(0.1f, 0.85f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float idleAlpha = 0f;
        [SerializeField, Range(0f, 1f)] private float selectedAlpha = 0.85f;
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.2f;
        [SerializeField, Min(0.1f)] private float pulseDuration = 0.85f;
        [SerializeField, Range(1f, 1.5f)] private float pulseScale = 1.12f;

        private Sequence pulseSequence;

        public void SetSelected(bool selected, bool immediate = false)
        {
            KillTween();
            float duration = immediate ? 0f : fadeDuration;

            foreach (Image glow in glowImages)
            {
                if (!glow) continue;
                glow.color = WithAlpha(glowColor, glow.color.a);
                glow.DOFade(selected ? selectedAlpha : idleAlpha, duration).SetUpdate(true);
                glow.rectTransform.DOScale(Vector3.one, duration).SetUpdate(true);
            }

            if (selected && !immediate)
                StartPulse();
            else if (selected)
                StartPulse();
        }

        private void OnDisable() => KillTween();

        private void OnValidate()
        {
            if (glowImages == null) return;
            foreach (Image glow in glowImages)
            {
                if (!glow) continue;
                glow.color = WithAlpha(glowColor, glow.color.a);
            }
        }

        private void StartPulse()
        {
            pulseSequence = DOTween.Sequence().SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetTarget(this);
            foreach (Image glow in glowImages)
            {
                if (!glow) continue;
                pulseSequence.Join(glow.rectTransform.DOScale(pulseScale, pulseDuration).SetEase(Ease.InOutSine));
                pulseSequence.Join(glow.DOFade(selectedAlpha * 0.65f, pulseDuration).SetEase(Ease.InOutSine));
            }
        }

        private void KillTween()
        {
            pulseSequence?.Kill();
            pulseSequence = null;
            DOTween.Kill(this);
            if (glowImages == null) return;
            foreach (Image glow in glowImages)
            {
                if (!glow) continue;
                glow.DOKill();
                glow.rectTransform.DOKill();
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
