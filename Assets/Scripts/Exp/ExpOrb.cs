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

        // 충돌한 오브젝트나 그 부모에서 Player 컴포넌트를 찾습니다.
        Player player = other.GetComponentInParent<Player>();

        if (player != null)
        {
            // Player 컴포넌트를 찾았다면, 경험치를 주고 이 오브젝트를 파괴합니다.
            player.GainExperience(amount);
            Destroy(gameObject);
        }
        else
        {
            // Player 컴포넌트를 찾지 못한 경우 (예: 플레이어의 자식 오브젝트에 다른 콜라이더만 있는 경우 등)
            Debug.LogWarning(
                $"Player 컴포넌트를 찾지 못했습니다. " +
                $"충돌한 오브젝트: {other.name}, 최상위: {other.transform.root.name}"
            );
        }
    }
  }
}

