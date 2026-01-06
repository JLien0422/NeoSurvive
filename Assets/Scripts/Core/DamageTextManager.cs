using UnityEngine;

namespace NeoSurvive.Core
{
    public class DamageTextManager : MonoBehaviour
    {
        public static DamageTextManager Instance { get; private set; }

        public GameObject damageTextPrefab;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void ShowDamageText(Vector3 position, float damage)
        {
            GameObject obj;
            if (damageTextPrefab != null)
            {
                obj = Instantiate(damageTextPrefab, position, Quaternion.identity);
            }
            else
            {
                obj = new GameObject("DamageText");
                obj.transform.position = position;
                obj.AddComponent<DamageText>();
            }

            DamageText dt = obj.GetComponent<DamageText>();
            if (dt != null) dt.Setup(damage);
        }
    }
}
