using UnityEngine;

namespace NeoSurvive.Core
{
    public class EffectAutoDestroy : MonoBehaviour
    {
        public float duration = 0.5f;
        private float timer = 0f;
        private LineRenderer lr;
        private Color startColor;
        private Color endColor;

        void Start()
        {
            lr = GetComponent<LineRenderer>();
            if (lr != null)
            {
                startColor = lr.startColor;
                endColor = lr.endColor;
            }
            Destroy(gameObject, duration);
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (lr != null)
            {
                float t = timer / duration;
                Color c1 = startColor;
                Color c2 = endColor;
                c1.a *= (1 - t);
                c2.a *= (1 - t);
                lr.startColor = c1;
                lr.endColor = c2;
            }
        }
    }
}
