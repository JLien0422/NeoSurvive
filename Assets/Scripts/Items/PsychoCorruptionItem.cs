using UnityEngine;
using NeoSurvive.Network;
using NeoSurvive.Network.Protocol;

/// <summary>
/// 사이코 잠식도 아이템 - 획득 시 사이코 잠식도 증가
/// OnTriggerEnter2D 대신 DistanceManager의 거리기반 픽업 시스템을 사용합니다.
/// </summary>
public class PsychoCorruptionItem : MonoBehaviour, IPickupable
{
    [Header("사이코 잠식도 증가량")]
    [SerializeField]
    [Tooltip("플레이어가 획득 시 증가할 사이코 잠식도 양 (퍼센트)")]
    private float corruptionAmount = 10f;
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

        if (player.IsLocal && UDPClient.Instance != null)
        {
            var networkItem = GetComponent<NetworkItem>();
            if (networkItem != null)
                UDPClient.Instance.SendAction(ActionType.ItemPickup, networkItem.ItemId);
        }

        player.AddPsychoCorruption(corruptionAmount);
        Destroy(gameObject);
    }
}
