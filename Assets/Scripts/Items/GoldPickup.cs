using UnityEngine;
using NeoSurvive.Network;
using NeoSurvive.Network.Protocol;

// 거리 기반 베이스 Item을 상속해 골드 획득 동작만 구현합니다.
public class GoldPickup : Item
{
    // 이 골드 아이템이 담고 있는 골드의 양입니다.
    [SerializeField]
    private int goldAmount = 10;

    protected override bool CanBePickedBy(Player player)
    {
        return player != null && player.IsLocal;
    }

    protected override void OnPicked(Player player)
    {
        if (UDPClient.Instance != null)
        {
            var networkItem = GetComponent<NetworkItem>();
            if (networkItem != null)
            {
                UDPClient.Instance.SendAction(ActionType.ItemPickup, networkItem.ItemId);
            }
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(goldAmount);
        }

        Destroy(gameObject);
    }
}
