using UnityEngine;
using NeoSurvive.Network;
using NeoSurvive.Network.Protocol;

/// <summary>
/// 골드 픽업 아이템
/// OnTriggerEnter2D 대신 DistanceManager의 거리기반 픽업 시스템을 사용합니다.
/// </summary>
public class GoldPickup : MonoBehaviour, IPickupable
{
    [SerializeField] private int goldAmount = 10;
    [SerializeField] private float pickupRange = 0.5f; // 픽업 판정 반경

    // IPickupable 구현
    public Transform Transform => transform;
    public float PickupRange => pickupRange;

    private void OnEnable()
    {
        // 스폰 시 DistanceManager에 등록
        if (DistanceManager.Instance != null)
            DistanceManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        // 비활성화/소멸 시 등록 해제
        if (DistanceManager.Instance != null)
            DistanceManager.Instance.Unregister(this);
    }

    /// <summary>
    /// DistanceManager가 범위 이내 진입 시 호출합니다.
    /// </summary>
    public void Pickup(Player player)
    {
        if (player == null) return;

        // 멀티플레이: 로컬 플레이어만 서버에 픽업 전송
        if (player.IsLocal && UDPClient.Instance != null)
        {
            var networkItem = GetComponent<NetworkItem>();
            if (networkItem != null)
                UDPClient.Instance.SendAction(ActionType.ItemPickup, networkItem.ItemId);
        }

        if (GameManager.Instance != null)
            GameManager.Instance.AddGold(goldAmount);

        Destroy(gameObject);
    }
}
