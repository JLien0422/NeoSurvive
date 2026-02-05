using UnityEngine;
using NeoSurvive.Network;
using NeoSurvive.Network.Protocol;

// 이 스크립트는 플레이어가 수집할 수 있는 골드 아이템을 처리합니다.
// 이 스크립트가 붙은 오브젝트에는 반드시 isTrigger가 활성화된 Collider2D가 있어야 합니다.
[RequireComponent(typeof(Collider2D))]
public class GoldPickup : MonoBehaviour
{
    // 이 골드 아이템이 담고 있는 골드의 양입니다.
    [SerializeField]
    private int goldAmount = 10;

    // 이 오브젝트의 콜라이더에 다른 콜라이더가 들어왔을 때 호출되는 메서드입니다. (isTrigger 필요)
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 충돌한 오브젝트의 태그가 "Player"인지 확인합니다.
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null && player.IsLocal && UDPClient.Instance != null)
            {
                var networkItem = GetComponent<NetworkItem>();
                if (networkItem != null)
                {
                    UDPClient.Instance.SendAction(ActionType.ItemPickup, networkItem.ItemId);
                }
            }

            // GameManager의 인스턴스를 통해 골드를 추가합니다.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddGold(goldAmount);
            }

            // 골드를 획득했으므로, 이 게임 오브젝트를 파괴합니다.
            Destroy(gameObject);
        }
    }

    // 컴포넌트가 추가되거나 리셋될 때 호출됩니다.
    // 콜라이더가 isTrigger로 설정되도록 보장합니다.
    private void Reset()
    {
        // 모든 Collider2D 타입의 컴포넌트를 가져옵니다.
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            // isTrigger를 true로 설정합니다.
            col.isTrigger = true;
        }
    }
}
