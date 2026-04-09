using UnityEngine;

namespace NeoSurvive.Weapon
{
  public class WeaponRuntimeInfo : MonoBehaviour
  {
    public int WeaponId { get; private set; }
    private Player owner;
    private WeaponManager weaponManager;

    public void Initialize(int weaponId, Player ownerPlayer, WeaponManager manager)
    {
      WeaponId = weaponId;
      owner = ownerPlayer;
      weaponManager = manager;
    }

    public void ReportUse(Vector3 direction)
    {
      // 네트워크 동기화 제거 이후 호환성용 메서드입니다.
    }

    public void ReportProjectile(Vector3 spawnPos, Vector3 direction, float speed, float range = 0f, int penetration = 0, uint targetId = 0)
    {
      // 네트워크 동기화 제거 이후 호환성용 메서드입니다.
    }

    public void ReportBeam(Vector3 startPos, Vector3 direction, float duration, float speed = 0f, int penetration = 0)
    {
      // 네트워크 동기화 제거 이후 호환성용 메서드입니다.
    }

    public void ReportCone(Vector3 origin, Vector3 direction, float angle, float range, float statusDuration)
    {
      // 네트워크 동기화 제거 이후 호환성용 메서드입니다.
    }

    public void ReportArea(Vector3 center, float radius, float duration, float tickRate, bool destroyProjectiles)
    {
      // 네트워크 동기화 제거 이후 호환성용 메서드입니다.
    }

    public void ReportSummon(Vector3 spawnPos, float duration, float hp, bool spawnPrison, float prisonDuration)
    {
      // 네트워크 동기화 제거 이후 호환성용 메서드입니다.
    }

    public void ReportTrap(Vector3 spawnPos, float duration, float activationDelay, float tickRate, float radius)
    {
      // 네트워크 동기화 제거 이후 호환성용 메서드입니다.
    }
  }
}