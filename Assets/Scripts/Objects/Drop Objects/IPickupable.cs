using UnityEngine;

/// <summary>
/// 플레이어가 거리기반으로 획득할 수 있는 아이템이 구현해야 할 인터페이스
/// DistanceManager가 이 인터페이스를 통해 픽업 처리를 수행합니다.
/// </summary>
public interface IPickupable
{
    // 거리 계산에 사용할 Transform
    Transform Transform { get; }

    // 픽업 판정 반경 (sqrMagnitude 비교용)
    float PickupRange { get; }

    // 범위 이내 진입 시 DistanceManager가 호출하는 픽업 메서드
    void Pickup(Player player);
}
