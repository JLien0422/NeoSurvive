using UnityEngine;

/// <summary>
/// Shooter's Gun 애니메이션 이벤트를 부모 ShooterMechanism으로 전달합니다.
/// Shooter_Attack 클립 이벤트에서 OnAttackFireFrame을 호출하세요.
/// </summary>
public class ShooterGunAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private ShooterMechanism shooterMechanism;

    private void Awake()
    {
        if (shooterMechanism == null)
        {
            shooterMechanism = GetComponentInParent<ShooterMechanism>();
        }
    }

    public void OnAttackFireFrame()
    {
        if (shooterMechanism == null)
        {
            shooterMechanism = GetComponentInParent<ShooterMechanism>();
        }

        if (shooterMechanism != null)
        {
            shooterMechanism.FireProjectileNowFromAnimationEvent();
        }
    }
}
