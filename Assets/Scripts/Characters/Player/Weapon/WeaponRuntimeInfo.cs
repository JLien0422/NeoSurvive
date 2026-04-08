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
      if (owner == null || weaponManager == null) return;
      if (!owner.IsLocal) return;

      SoundManager.Instance?.PlayWeaponUseById(WeaponId);

      weaponManager.SendWeaponAttack(WeaponId, direction);
    }

    public void ReportProjectile(Vector3 spawnPos, Vector3 direction, float speed, float range = 0f, int penetration = 0, uint targetId = 0)
    {
      if (owner == null || weaponManager == null) return;
      if (!owner.IsLocal) return;

      SoundManager.Instance?.PlayWeaponUseById(WeaponId);
      if (UDPClient.Instance == null) return;

      UDPClient.Instance.SendWeaponAction(
        NeoSurvive.Network.Protocol.ActionType.WeaponUse,
        WeaponId,
        new Vector2(direction.x, direction.y),
        new Vector2(spawnPos.x, spawnPos.y),
        action =>
        {
          action.WeaponProjectile = new NeoSurvive.Network.Protocol.WeaponProjectile
          {
            SpawnX = spawnPos.x,
            SpawnY = spawnPos.y,
            DirX = direction.x,
            DirY = direction.y,
            Speed = speed,
            Range = range,
            Penetration = (uint)penetration,
            TargetId = targetId
          };
        }
      );
    }

    public void ReportBeam(Vector3 startPos, Vector3 direction, float duration, float speed = 0f, int penetration = 0)
    {
      if (owner == null || weaponManager == null) return;
      if (!owner.IsLocal) return;

      SoundManager.Instance?.PlayWeaponUseById(WeaponId);
      if (UDPClient.Instance == null) return;

      UDPClient.Instance.SendWeaponAction(
        NeoSurvive.Network.Protocol.ActionType.WeaponUse,
        WeaponId,
        new Vector2(direction.x, direction.y),
        new Vector2(startPos.x, startPos.y),
        action =>
        {
          action.WeaponBeam = new NeoSurvive.Network.Protocol.WeaponBeam
          {
            StartX = startPos.x,
            StartY = startPos.y,
            DirX = direction.x,
            DirY = direction.y,
            Duration = duration,
            Speed = speed,
            Penetration = (uint)penetration
          };
        }
      );
    }

    public void ReportCone(Vector3 origin, Vector3 direction, float angle, float range, float statusDuration)
    {
      if (owner == null || weaponManager == null) return;
      if (!owner.IsLocal) return;

      SoundManager.Instance?.PlayWeaponUseById(WeaponId);
      if (UDPClient.Instance == null) return;

      UDPClient.Instance.SendWeaponAction(
        NeoSurvive.Network.Protocol.ActionType.WeaponUse,
        WeaponId,
        new Vector2(direction.x, direction.y),
        new Vector2(origin.x, origin.y),
        action =>
        {
          action.WeaponCone = new NeoSurvive.Network.Protocol.WeaponCone
          {
            OriginX = origin.x,
            OriginY = origin.y,
            DirX = direction.x,
            DirY = direction.y,
            Angle = angle,
            Range = range,
            StatusDuration = statusDuration
          };
        }
      );
    }

    public void ReportArea(Vector3 center, float radius, float duration, float tickRate, bool destroyProjectiles)
    {
      if (owner == null || weaponManager == null) return;
      if (!owner.IsLocal) return;

      SoundManager.Instance?.PlayWeaponUseById(WeaponId);
      if (UDPClient.Instance == null) return;

      UDPClient.Instance.SendWeaponAction(
        NeoSurvive.Network.Protocol.ActionType.WeaponUse,
        WeaponId,
        Vector2.zero,
        new Vector2(center.x, center.y),
        action =>
        {
          action.WeaponArea = new NeoSurvive.Network.Protocol.WeaponArea
          {
            CenterX = center.x,
            CenterY = center.y,
            Radius = radius,
            Duration = duration,
            TickRate = tickRate,
            DestroyProjectiles = destroyProjectiles
          };
        }
      );
    }

    public void ReportSummon(Vector3 spawnPos, float duration, float hp, bool spawnPrison, float prisonDuration)
    {
      if (owner == null || weaponManager == null) return;
      if (!owner.IsLocal) return;

      SoundManager.Instance?.PlayWeaponUseById(WeaponId);
      if (UDPClient.Instance == null) return;

      UDPClient.Instance.SendWeaponAction(
        NeoSurvive.Network.Protocol.ActionType.WeaponUse,
        WeaponId,
        Vector2.zero,
        new Vector2(spawnPos.x, spawnPos.y),
        action =>
        {
          action.WeaponSummon = new NeoSurvive.Network.Protocol.WeaponSummon
          {
            SpawnX = spawnPos.x,
            SpawnY = spawnPos.y,
            Duration = duration,
            Hp = (uint)Mathf.Max(0f, hp),
            SpawnPrison = spawnPrison,
            PrisonDuration = prisonDuration
          };
        }
      );
    }

    public void ReportTrap(Vector3 spawnPos, float duration, float activationDelay, float tickRate, float radius)
    {
      if (owner == null || weaponManager == null) return;
      if (!owner.IsLocal) return;

      SoundManager.Instance?.PlayWeaponUseById(WeaponId);
      if (UDPClient.Instance == null) return;

      UDPClient.Instance.SendWeaponAction(
        NeoSurvive.Network.Protocol.ActionType.WeaponUse,
        WeaponId,
        Vector2.zero,
        new Vector2(spawnPos.x, spawnPos.y),
        action =>
        {
          action.WeaponTrap = new NeoSurvive.Network.Protocol.WeaponTrap
          {
            SpawnX = spawnPos.x,
            SpawnY = spawnPos.y,
            Duration = duration,
            ActivationDelay = activationDelay,
            TickRate = tickRate,
            Radius = radius
          };
        }
      );
    }
  }
}