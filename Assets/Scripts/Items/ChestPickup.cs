using UnityEngine;
using System;

/// <summary>
/// 상자 픽업 - 획득 시 무기 선택 UI 표시
/// OnTriggerEnter2D 대신 DistanceManager의 거리기반 픽업 시스템을 사용합니다.
/// </summary>
public class ChestPickup : MonoBehaviour, IPickupable
{
    public static event Action OnChestOpened; // UIManager가 구독하는 이벤트

    [SerializeField] private float pickupRange = 0.6f; // 상자는 픽업 범위 약간 크게

    // IPickupable 구현
    public Transform Transform => transform;
    public float PickupRange => pickupRange;

    private bool opened = false;

    private void OnEnable()
    {
        Debug.Log("[ChestPickup] OnEnable");
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
        if (opened || player == null) return;

        opened = true;
        Open();
    }

    private void Open()
    {
        Debug.Log("🎁 Chest Opened!");
        SoundManager.Instance?.PlayChestOpened();
        OnChestOpened?.Invoke();
        Destroy(gameObject);
    }
}
