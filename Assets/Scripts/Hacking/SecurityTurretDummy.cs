using UnityEngine;

/// <summary>
/// 보안 터렛 더미
/// - 실제 공격 로직은 없음
/// - 시각적 기준점 + 발사 위치만 제공
/// </summary>
public class SecurityTurretDummy : MonoBehaviour
{
    public Transform firePoint;

    private void Awake()
    {
        if (firePoint == null)
            firePoint = transform;
    }
}
