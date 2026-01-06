using UnityEngine;

namespace NeoSurvive.Core
{
    /// <summary>
    /// 데미지 숫자를 화면에 띄워주는 스크립트
    /// </summary>
    public class DamageText : MonoBehaviour
    {
        public float moveSpeed = 1.5f;
        public float alphaSpeed = 1.5f;
        public float destroyTime = 1.0f;

        private TextMesh textMesh;
        private Color color;

        void Awake()
        {
            textMesh = GetComponent<TextMesh>();
            if (textMesh == null)
            {
                textMesh = gameObject.AddComponent<TextMesh>();
                textMesh.anchor = TextAnchor.MiddleCenter;
                textMesh.alignment = TextAlignment.Center;
                textMesh.fontSize = 24;
                textMesh.characterSize = 0.1f;
                textMesh.color = Color.yellow;
            }

            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = 100; // 다른 스프라이트보다 앞에 표시
            }

            color = textMesh.color;
        }

        public void Setup(float damage)
        {
            textMesh.text = Mathf.RoundToInt(damage).ToString();
            Destroy(gameObject, destroyTime);
        }

        void Update()
        {
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

            color.a = Mathf.Lerp(color.a, 0, alphaSpeed * Time.deltaTime);
            textMesh.color = color;
        }
    }
}
