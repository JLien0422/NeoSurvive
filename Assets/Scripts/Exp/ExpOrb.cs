using UnityEngine;

namespace NeoSurvive.Exp
{
    /// <summary>
    /// 경험치 오브 (플레이어가 닿으면 경험치 지급 후 파괴)
    /// </summary>
    public class ExpOrb : MonoBehaviour
    {
        [SerializeField] private int amount = 1;

        public void SetAmount(int value)
        {
            amount = Mathf.Max(1, value);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            // ✅ 자식 콜라이더에 닿아도, 부모(플레이어 본체)에서 PlayerExperience를 찾음
            PlayerExperience exp = other.GetComponentInParent<PlayerExperience>();

            if (exp != null)
            {
                exp.AddExp(amount);
                Destroy(gameObject); // ✅ 성공했을 때만 파괴
            }
            else
            {
                Debug.LogWarning(
                    $"PlayerExperience를 찾지 못했습니다. " +
                    $"충돌한 오브젝트: {other.name}, 최상위: {other.transform.root.name}"
                );
                // 실패 시 파괴하지 않음 (원인 파악/재시도 가능)
            }
        }
    }
}

