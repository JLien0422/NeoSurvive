using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 설치형 자동 포탑 생성기 (Spawn Weapon)
  /// </summary>
  public class AutoTurret : MonoBehaviour
  {
    [Header("터렛 생성기 설정")]
    /// <summary>
    /// 설치 터렛 프리팹
    /// </summary>
    public GameObject dronePrefab;

    /// <summary>
    /// 발사체 프리팹
    /// </summary>
    public GameObject projectilePrefab;

    /// <summary>
    /// 드론 터렛 설치 쿨타임
    /// </summary>
    public float installCooldown = 5f;

    [Header("터렛 설정")]
    /// <summary>
    /// 터렛 지속 시간
    /// </summary>
    public float lifeTime = 10f;

    /// <summary>
    /// 터렛 총알 당 데미지
    /// </summary>
    public float damage = 5f;

    /// <summary>
    /// 터렛 공격 범위
    /// </summary>
    public float range = 5f;

    /// <summary>
    /// 터렛 공격 속도
    /// </summary>
    public float fireRate = 1f;

    public float elpased = 0f;

    private void Update()
    {
      elpased += Time.deltaTime;
      if (elpased < installCooldown) return;
      Debug.Log("터렛 생성!");

      Vector2 randomOffset = Random.insideUnitCircle * 0.5f;

      GameObject drone = Instantiate(dronePrefab, transform.position + (Vector3)randomOffset, Quaternion.identity);
      DeployedTurret deployedTurret = drone.GetComponent<DeployedTurret>();
      deployedTurret.Initialize(damage, range, fireRate, lifeTime, projectilePrefab);

      elpased = 0f;
    }
  }
}
